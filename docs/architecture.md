# Fire Discipline — Kiến trúc và luật bất di bất dịch

> **Tài liệu nóng.** Đây là trạng thái **hiện tại** của code, không phải ý định thiết kế.
> Đọc file này trước khi sửa bất cứ thứ gì.
> Lịch sử, lý lẽ và các hướng đi sai nằm trong [`1.0/`](1.0/).

---

## 1. Định danh

| | |
|---|---|
| packageId | `william.firediscipline` |
| RimWorld | 1.6 only |
| Ngôn ngữ output | **Tiếng Anh** — code, comment, commit, chuỗi UI, mô tả Workshop |
| Ngôn ngữ tài liệu | Tiếng Việt |
| Root | `FireDiscipline/` |
| Source | `FireDiscipline/Source/FireDiscipline/` |
| Assembly đích | `FireDiscipline/1.6/Assemblies/FireDiscipline.dll` |
| Defs | `FireDiscipline/1.6/Defs/` |

```bash
dotnet build                     # build + tự deploy vào Mods folder
dotnet build -p:SkipDeploy=true  # build không deploy
```

RimWorld nạp assembly lúc khởi động — **luôn phải restart game để thấy thay đổi code**.

---

## 2. Mười luật bất di bất dịch

Vi phạm bất kỳ luật nào = dừng lại và hỏi, không tự quyết.

1. **Không thay class gốc.** Không set `verbClass`, `thingClass`, `projectile.thingClass`, không thay `DamageWorker`. Chỉ Harmony postfix/prefix lên hàm tính toán.
2. **Suy ra, đừng khai báo.** Giá trị cho vũ khí/giáp/công trình của mod khác phải tính từ stat hoặc field Def của vanilla. **Cấm** hardcode danh sách `defName`. **Cấm** khớp chuỗi `defName`/`label`. **Cấm** file patch XML riêng cho từng mod.
3. **Cộng thêm bằng Hediff / Comp / StatPart.** Gỡ mod không được vỡ save.
4. **Đăng ký Harmony thủ công** qua `PatchRegistry` + `IModule`. **Không** `PatchAll()`. **Không để lại attribute `[HarmonyPatch]` mồ côi.** Feature tắt → patch không được đăng ký **và không có tác dụng phụ nào**.
5. **Không chạm Pathfinding / ThinkTree / JobGiver.** Thêm JobDef/JobDriver mới thì được; patch cái có sẵn thì không. **Đặc biệt: không Prefix trả `false` lên bất kỳ hàm nào của vanilla.**
6. **Không hard dependency.** Phát hiện mod khác qua `ModsConfig.IsActive`.
7. **Transpiler là nợ kỹ thuật.** Ưu tiên postfix.
8. **Không mutate `verbProps` hay bất kỳ object cấp Def nào.** Ngoại lệ duy nhất đã duyệt: tiêm StatPart vào `StatDef.parts` trong `OnStartup()`, có guard idempotent.
9. **Mọi hằng số cân bằng phải nằm trong mod settings hoặc hằng số đặt tên rõ.** Không magic number rải trong code. Áp dụng cho cả XML Def.
10. **UI không được nói dối.** Slider phải nối vào code thật. Debug action phải in đúng thứ nó ghi nhãn. `About.xml` không quảng cáo tính năng không chạy.

### Luật 10 là luật bị vi phạm nhiều nhất

Không phải vì ai cố tình, mà vì **một câu đúng lúc viết không tự cập nhật khi thứ nó mô tả thay đổi**. Xem `1.0/lessons-and-wrong-turns.md` §3 — chín trường hợp đã ghi nhận.

Quy tắc thực hành: **sửa hành vi thì grep luôn chuỗi mô tả hành vi đó.**

---

## 3. Hệ quả kiến trúc đã trả giá để biết

- **StatPart không truy cập được khoảng cách.** Mọi modifier phụ thuộc cự ly buộc phải đi qua postfix `ShotReport.HitReportFor`.
- **StatPart đã tiêm thì không gỡ ra được.** Mỗi StatPart phải tự kiểm `PatchRegistry.IsModuleEnabled(...)` ở đầu `TransformValue`. Đây là cách duy nhất để tắt giữa phiên có hiệu lực.
- **Tắt là tức thì, bật cần restart.** Patch chỉ đăng ký lúc khởi động.
- **Mỗi module tự sở hữu StatPart của mình.** Dùng chung sẽ tạo phụ thuộc ẩn: module tiêm bị tắt thì module kia mất hiệu ứng im lặng.
- **`MoveSpeed` `statOffsets` trong XML là cộng thẳng**, không nhân. Base người ≈ 4.6 ô/s, nên `-0.55` chỉ là `-12%`. Muốn nhân thì phải qua StatPart.
- **`Hediff.set_Severity` clamp về `def.minSeverity`.** Nếu comp gỡ hediff khi `Severity <= 0` thì `minSeverity` **phải bằng 0**.

---

## 4. Sáu module

| Module | Mặc định | Sở hữu gì |
|---|---|---|
| `AimStanceModule` | BẬT | 4 tư thế; StatPart `AimingDelayFactor` + `ShootingAccuracyPawn`; postfix `ShotReport.HitReportFor`; phạt độ chính xác embrasure |
| `EncumbranceModule` | BẬT | StatPart nhân vào `MoveSpeed` theo `MassUtility.Capacity` (chỉ vũ khí + túi, **không** tính giáp) |
| `SuppressionCoreModule` | BẬT¹ | `FD_Suppressed`, ma trận tư thế, cover kháng suppression, StatPart `MoveSpeed` theo stage |
| `GrazeModule` | BẬT | Hạ sát thương phát trúng khó; chuyển hướng đòn vào nội tạng ra chi ngoài |
| `ShockModule` | BẬT | `FD_CombatShock` (đồng đội chết/downed), `FD_ShellShock` (vụ nổ) |
| `ShotgunAoEModule` | **TẮT** | Nêm splash + overlay cảnh báo vùng nguy hiểm |

¹ Lần chạy đầu tự đặt TẮT nếu phát hiện mod suppression khác hoặc CE; sau đó người chơi sở hữu công tắc, dò mod không bao giờ ghi đè nữa.

`enableEmbrasureInteraction` **TẮT** mặc định — chỉ thêm phạt độ chính xác, kháng suppression đến từ cover chung.

### Trục suppression

Suppression trừng phạt **di chuyển**, không phải ngắm bắn. Lý do: người phòng thủ không cần di chuyển, kẻ tấn công thì bắt buộc — nên cùng một cơ chế lại giúp bên ít quân giữ đất.

```
severity += 0.25 mỗi phát đạn qua gần   (×tư thế bắn ×tư thế nhận ×cover)
severity -= 0.10 mỗi giây, sau 120 tick ân hạn
stage:  0.5 shaken · 1.0 wavering · 2.0 ducking · 5.5 cowering   (thang 0–9)
MoveSpeed nhân:  ×0.95 · ×0.80 · ×0.50 · ×0.15,  sàn tuyệt đối 0.7 ô/s
```

Cover: `amount *= clamp(1 - blockChance × 0.85, 0.25, 1)` — áp cho **mọi** vật cản, không ngoại lệ.

---

## 5. Bất biến — quét được bằng lệnh

Chạy sau mỗi đợt sửa. Tiêu chí là **không có vi phạm MỚI**, không phải bằng 0.

```bash
grep -rn "return false" FireDiscipline/Source/**/Patch_*.cs      # kỳ vọng: 0 trong Prefix
grep -rn "\[HarmonyPatch" FireDiscipline/Source/                 # kỳ vọng: 0
grep -rn "new \w*Module()" FireDiscipline/Source/                # chỉ trong FireDisciplineMod.RegisterModule
grep -rn "static readonly Dictionary" FireDiscipline/Source/     # mỗi cái phải có đường dọn
grep -rn "ToList()" FireDiscipline/Source/**/Patch_Projectile*   # kỳ vọng: 0 (đường nóng)
```

Và: `dotnet build` phải **0 warning, 0 error**.

---

## 6. Định nghĩa "xong"

1. **Regression pass:** tắt hết feature → restart → ma trận harness khớp **tuyệt đối** với vanilla, mọi ô. Snap Shot được định nghĩa là vanilla.
2. Chỉ tiêu pass/fail tương ứng ở `1.0/master-design.md` §7.3 đã chạy và đạt.
3. Có toggle riêng, và **tắt toggle phải vô hiệu hoá ngay giữa phiên** (guard runtime).
4. Không thêm transpiler mới, không thêm attribute `[HarmonyPatch]` mồ côi.
5. Test gỡ mod giữa save **với pawn đang mang hediff**.
6. Đo bằng Dubs Performance Analyzer nếu chạm `Patch_ShotReport`, `Patch_Projectile_Impact`, `Patch_Explosion`, hoặc bất kỳ thứ gì chạy mỗi phát bắn.
7. Không để lại hằng số mới mà không đặt tên hoặc không đưa vào settings.
8. **Debug action phải gọi code sản xuất, không phải bản sao của nó.** Một action tự tính lại heuristic chỉ kiểm chứng chính nó.

---

## 7. Quy tắc làm việc

**Được tự làm:** đọc, phân tích, báo cáo · sửa lỗi rõ ràng (null check thiếu, exception chưa bắt) · thêm test/debug action · thêm comment tiếng Anh · xoá code chết đã được duyệt.

**Phải hỏi trước:** thêm hoặc đổi hằng số cân bằng · thêm transpiler · refactor cấu trúc file/namespace · thêm dependency · đổi Def XML · bất kỳ việc gì thuộc Đợt B.

**Cấm:** vi phạm 10 luật · "tiện tay" refactor ngoài phạm vi · sửa nhiều module trong một commit · đổi con số cân bằng mà không ghi lại giá trị cũ.

**Commit:** tiếng Anh, một module một commit, prefix `stance:` `suppress:` `graze:` `shock:` `encumber:` `shotgun:` `harness:` `infra:` `docs:`.
