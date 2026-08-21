# TASK SPEC — A2 (Suppression StatDefs) & A3 (Derived Suppression Resistance)

> **Dành cho agent thực thi.** Tài liệu này **tự chứa** — đọc xong là làm được, không cần hội thoại trước đó.
> Bối cảnh quyết định: [`../1.1-adoption-plan.md`](../1.1-adoption-plan.md) §3. Luật bắt buộc: [`../architecture.md`](../architecture.md) §2.
>
> ⚠️ **Hai task này phải làm theo đúng thứ tự A2 → A3.** A3 tiêu thụ StatDef do A2 tạo ra. Không làm A3 trước.

---

## 0. Bối cảnh tối thiểu cần biết

**Mod:** Fire Discipline — mod chiến thuật bắn súng cho RimWorld 1.6.
**packageId:** `william.firediscipline` · **Root:** `FireDiscipline/` · **Source:** `FireDiscipline/Source/FireDiscipline/`
**Defs:** `FireDiscipline/1.6/Defs/` · **Assembly đích:** `FireDiscipline/1.6/Assemblies/FireDiscipline.dll`

```bash
cd FireDiscipline/Source/FireDiscipline && dotnet build
```

Build tự deploy vào thư mục Mods; thêm `-p:SkipDeploy=true` để không deploy. **RimWorld nạp assembly lúc khởi động — luôn phải restart game để thấy thay đổi code.**

**Ngôn ngữ output:** code, comment, commit message, chuỗi UI đều bằng **tiếng Anh**. Chỉ tài liệu trong `docs/` mới là tiếng Việt.

### Kiến trúc module

Mod dùng **đăng ký Harmony thủ công**, không bao giờ `PatchAll()`. Mỗi tính năng là một `IModule` ([`Core/IModule.cs`](../../../FireDiscipline/Source/FireDiscipline/Core/IModule.cs)). Mẫu tham chiếu: [`Suppression/SuppressionCoreModule.cs`](../../../FireDiscipline/Source/FireDiscipline/Suppression/SuppressionCoreModule.cs).

**A2 và A3 KHÔNG tạo module mới.** Cả hai mở rộng `SuppressionCoreModule` (`ModuleId = "SuppressionCore"`, `DefaultEnabled => true`) đã có sẵn.

### Điểm sửa trung tâm

Toàn bộ A3 nằm trong **một hàm**: `SuppressionEngine.CalculateSuppressionAmount(Pawn shooter, Pawn victim)` — [`Suppression/SuppressionEngine.cs:48`](../../../FireDiscipline/Source/FireDiscipline/Suppression/SuppressionEngine.cs).

Cấu trúc hiện tại của hàm đó (đọc kỹ trước khi sửa):

```csharp
// 1. amount = base (settings.suppressionPerShot)
// 2. Nếu shooter dùng Rapid stance      -> amount *= rapidSuppressionBonus
// 3. Nếu shooter dùng vũ khí burst >= 5 -> amount *= heavyWeaponSuppressionMultiplier (2.00)
// 4. Nếu victim dùng Sharpshot          -> amount *= sharpshotSuppressionVulnerability (2.00)
// 5. Nếu victim IsDugIn                 -> amount *= proneSuppressionResistance (0.50)
// 6. Nếu enableCoverSuppression         -> amount *= Clamp(1 - block*factor, floor, 1)
// 7. return amount;
```

A3 chèn **một bước mới vào giữa bước 5 và 6**. Không đụng các bước còn lại.

### Hằng số cover vừa được đồng bộ — đừng đụng

`coverSuppressionFactor = 1.00` ở **cả 5 chỗ** (field, `Scribe_Values.Look`, fallback trong `SuppressionEngine`, fallback trong `DebugHarness`, nhãn UI). Đây là kết quả của một lần sửa nợ vừa xong. **Không đổi giá trị này, không đổi công thức cover ở bước 6** — đó là task A4, không thuộc phạm vi ở đây.

---

## 1. MƯỜI LUẬT — vi phạm bất kỳ luật nào thì DỪNG LẠI VÀ HỎI, không tự quyết

1. **Không thay class gốc.** Không set `verbClass`, `thingClass`, `projectile.thingClass`, không thay `DamageWorker`. Chỉ Harmony postfix/prefix lên hàm tính toán.
2. **Suy ra, đừng khai báo.** Giá trị cho vũ khí/giáp/công trình của mod khác phải tính từ stat hoặc field Def vanilla. **Cấm** hardcode danh sách `defName`. **Cấm** khớp chuỗi `defName`/`label`. **Cấm** file patch XML riêng cho từng mod.
3. **Cộng thêm bằng Hediff / Comp / StatPart.** Gỡ mod không được vỡ save.
4. **Đăng ký Harmony thủ công** qua `PatchRegistry` + `IModule`. **Không** `PatchAll()`. **Không để lại attribute `[HarmonyPatch]` mồ côi.** Feature tắt → patch không được đăng ký **và không có tác dụng phụ nào**.
5. **Không chạm Pathfinding / ThinkTree / JobGiver.** Thêm JobDef/JobDriver mới thì được; patch cái có sẵn thì không. **Đặc biệt: không Prefix trả `false` lên bất kỳ hàm nào của vanilla.**
6. **Không hard dependency.** Phát hiện mod khác qua `ModsConfig.IsActive`.
7. **Transpiler là nợ kỹ thuật.** Ưu tiên postfix. *(A2 và A3 KHÔNG được dùng transpiler — cả hai không cần patch mới nào cả.)*
8. **Không mutate `verbProps` hay bất kỳ object cấp Def nào.** Ngoại lệ duy nhất đã duyệt: tiêm StatPart vào `StatDef.parts` trong `OnStartup()`, có guard idempotent.
9. **Mọi hằng số cân bằng phải nằm trong mod settings hoặc hằng số đặt tên rõ.** Không magic number rải trong code. Áp dụng cho cả XML Def.
10. **UI không được nói dối.** Slider phải nối vào code thật. Debug action phải in đúng thứ nó ghi nhãn. `About.xml` không quảng cáo tính năng không chạy.

> **Luật 10 là luật bị vi phạm nhiều nhất** — không phải vì ai cố tình, mà vì *một câu đúng lúc viết không tự cập nhật khi thứ nó mô tả thay đổi*. Quy tắc thực hành: **sửa hành vi thì grep luôn chuỗi mô tả hành vi đó.**

### ⚠️ Bẫy đã có người dính hai lần trong dự án này — đọc trước khi viết settings

Khi thêm một field settings, phải đặt **cùng một giá trị mặc định** ở **tất cả** các nơi sau:

1. Khởi tạo field: `public float x = 1.00f;`
2. `Scribe_Values.Look(ref x, "x", 1.00f);` ← **rất hay bị bỏ sót khi retune**
3. Mọi fallback `Settings?.x ?? 1.00f` trong code
4. Nhãn `(Default: 1.00)` trong settings window

Đã có **hai** trường hợp lệch trong dự án: `coverSuppressionFactor` (lệch số, vừa sửa) và `proneAccuracyMultiplier` (**lệch dấu** — field `1.10f` là thưởng, fallback trong `StatPart_ShootingAccuracy.cs:41` là `0.85f` là phạt, chưa sửa). Xem `1.1-adoption-plan.md` §6.2 và §6.3.

---

## TASK A2 — Hai StatDef cho suppression

### Mục tiêu

Biến khả năng kháng áp chế từ **số cứng trong code** thành **stat có thể can thiệp**, để trait / gene / giáp / mod khác tác động được mà FD không cần biết chúng tồn tại. Đây là hạ tầng cho A3; bản thân A2 **không đổi một con số cân bằng nào**.

Tham chiếu thiết kế: `Reference Mods/Misstall's Combat Tweaks/Defs/StagedSuppressionStats.xml`.

### Việc phải làm

| # | Việc | Nơi |
|---|---|---|
| 1 | `StatDef` `FD_SuppressionResistance` — `category PawnCombat`, `defaultBaseValue 1.0`, `minValue 0`, `maxValue 2`, `toStringStyle PercentZero` | `1.6/Defs/StatDefs/Stats_FireDiscipline.xml` (mới) |
| 2 | `StatDef` `FD_SuppressionRecoverySpeed` — `category PawnCombat`, `defaultBaseValue 1.0`, `minValue 0`, `toStringStyle PercentZero` | cùng file |
| 3 | `StatDefOf`-style cache tĩnh, tra bằng `DefDatabase<StatDef>.GetNamedSilentFail` | `Suppression/SuppressionStatDefOf.cs` (mới) |
| 4 | Nối `FD_SuppressionResistance` vào bước 5.5 của `CalculateSuppressionAmount` | `Suppression/SuppressionEngine.cs` |
| 5 | Nối `FD_SuppressionRecoverySpeed` vào tốc độ hồi phục | `Suppression/HediffComp_SuppressionDecay.cs` |
| 6 | Debug action `Print Suppression Stat Values` — in cho pawn đang chọn: cả hai stat + giải thích nguồn | `Core/DebugHarness.cs` |

### Ngữ nghĩa — phải đúng chiều

- `FD_SuppressionResistance`: **cao = khó bị áp chế hơn**. Áp dụng bằng **chia**: `amount /= Mathf.Max(0.01f, resistance)`.
  *Vì sao chia chứ không nhân:* để `2.0` đọc là "kháng gấp đôi" chứ không phải "ăn gấp đôi". Đặt tên `Resistance` mà nhân vào là bẫy Luật 10 kiểu đặt tên.
- `FD_SuppressionRecoverySpeed`: **cao = hồi phục nhanh hơn**. Áp dụng bằng **nhân** vào lượng decay mỗi tick.

⚠️ **Guard bắt buộc:** cả hai StatDef phải tra bằng `GetNamedSilentFail` và **null-check**. Nếu def thiếu (người dùng xoá file XML, hoặc mod load lỗi), code phải chạy tiếp với giá trị `1.0` chứ không văng exception mỗi viên đạn.

### Acceptance Criteria — A2

- [ ] **AC-A2-1** — `dotnet build` sạch, **0 warning mới**.
- [ ] **AC-A2-2** — Vào game, mở tab Stats của một pawn: thấy cả hai stat trong nhóm `PawnCombat`, đều hiển thị `100%`.
- [ ] **AC-A2-3** — `Print Suppression Stat Values` in ra giá trị **khớp với thứ tab Stats hiển thị** (Luật 10).
- [ ] **AC-A2-4** — **Không có Harmony patch mới nào được đăng ký.** `Print Patch Registration Audit` trước/sau A2 phải cho ra **cùng một số lượng patch**. Dán cả hai output vào báo cáo.
- [ ] **AC-A2-5** — `Regression: Capture Baseline` → `Compare To Baseline`: **không một con số nào đổi.** A2 là hạ tầng thuần; mọi sai lệch đều là lỗi.
- [ ] **AC-A2-6** — Test guard: tạm đổi tên `FD_SuppressionResistance` trong XML thành tên khác, khởi động game, bắn nhau một lúc → **không lỗi đỏ**, suppression vẫn chạy như cũ. Khôi phục lại tên sau khi kiểm. Dán log vào báo cáo.
- [ ] **AC-A2-7** — Dùng Dev mode gán một hediff/trait nâng `FD_SuppressionResistance` lên `2.0` → pawn đó tích severity **đúng bằng một nửa** pawn đối chứng trong cùng tình huống. Đo bằng `Print Suppression Output Matrix`.
- [ ] **AC-A2-8** — Gỡ mod giữa save: load lại không lỗi đỏ (Luật 3).

---

## TASK A3 — Kháng suppression suy ra từ stat vanilla

### Mục tiêu

Hiện tại mọi pawn nhận áp chế **như nhau**: lính kỳ cựu, người mới, và một đứa trẻ đều tích `+0.25` mỗi viên đạn sượt qua. A3 làm cho khả năng chịu đựng **suy ra từ chính pawn đó**, dùng stat vanilla có sẵn — không hardcode, không danh sách defName (Luật 2).

Tham chiếu tham số: `Reference Mods/Misstall's Combat Tweaks/Defs/StagedSuppressionDefs.xml`.

### Bốn hệ số — công thức và giá trị mặc định

Chèn vào giữa bước 5 và bước 6 của `CalculateSuppressionAmount`. Tất cả nhân dồn vào `amount`.

| Hệ số | Nguồn vanilla | Công thức | Kẹp trong | Ý nghĩa |
|---|---|---|---|---|
| **Đau** | `StatDefOf.PainShockThreshold` | `basePainThreshold / pawnValue` | `[0.25, 2.00]` | Ngưỡng chịu đau cao → khó bị áp chế |
| **Tinh thần** | `StatDefOf.MentalBreakThreshold` | `pawnValue / baseMentalThreshold` | `[0.50, 1.50]` | Ngưỡng suy sụp thấp → khó bị áp chế |
| **Kỹ năng** | `SkillDefOf.Shooting` và `SkillDefOf.Melee`, lấy **max** | nội suy tuyến tính từ `1.00` ở lv0 tới `maxSkillSuppressionMultiplier` ở lv `skillLevelForMaxResistance` | — | Lính kỳ cựu bình tĩnh hơn |
| **Loạng choạng** | `pawn.stances.stagger.Staggered` | nếu đang stagger: `× staggerSuppressionFactor` | — | Đang loạng choạng thì dễ bị ghim |

Hằng số mặc định (**tất cả phải vào settings, Luật 9**):

| Setting | Mặc định |
|---|---|
| `enableDerivedSuppressionResistance` | `true` |
| `basePainShockThreshold` | `0.80` |
| `minPainSuppressionFactor` | `0.25` |
| `maxPainSuppressionFactor` | `2.00` |
| `baseMentalBreakThreshold` | `0.35` |
| `minMentalSuppressionFactor` | `0.50` |
| `maxMentalSuppressionFactor` | `1.50` |
| `skillLevelForMaxResistance` | `20` |
| `maxSkillSuppressionMultiplier` | `0.75` |
| `staggerSuppressionFactor` | `1.50` |

### Ràng buộc

- ⛔ **Không dùng `pawn.def.defName` hay `label` để phân loại gì cả** (Luật 2). Tất cả phải đọc qua `GetStatValue` / `skills.GetSkill`.
- ⛔ **Không đụng** bước 6 (cover) — đó là A4.
- Pawn không có `skills` (động vật, mechanoid) phải **bỏ qua hệ số kỹ năng**, không crash. Null-check `pawn.skills`.
- Toàn bộ khối phải nằm trong `if (settings?.enableDerivedSuppressionResistance ?? true)` để tắt được mà không cần restart.
- Thứ tự áp dụng: bốn hệ số A3 **trước** `FD_SuppressionResistance` của A2, để stat A2 là lời cuối (mod khác override được).

### Việc phải làm

| # | Việc | Nơi |
|---|---|---|
| 1 | 10 field settings + `Scribe_Values.Look` **cùng giá trị mặc định** | `FireDisciplineSettings.cs` |
| 2 | Hàm `CalculateDerivedResistance(Pawn victim)` trả về hệ số nhân gộp | `Suppression/SuppressionEngine.cs` |
| 3 | Gọi nó ở bước 5.5 | `Suppression/SuppressionEngine.cs` |
| 4 | Checkbox + 9 slider trong settings window, nhóm dưới mục Suppression | `FireDisciplineMod.cs` |
| 5 | Debug action `Print Derived Resistance Breakdown` — chọn pawn, in **từng hệ số riêng** + tích cuối cùng | `Core/DebugHarness.cs` |

### Acceptance Criteria — A3

- [ ] **AC-A3-1** — `dotnet build` sạch, 0 warning mới.
- [ ] **AC-A3-2** — **Không có Harmony patch mới.** `Print Patch Registration Audit` giữ nguyên số lượng so với trước A3.
- [ ] **AC-A3-3** — **Kiểm Luật 2 (bắt buộc):** grep code mới thêm — **không** xuất hiện `defName`, `.label`, hay bất kỳ so sánh chuỗi nào. Dán kết quả grep vào báo cáo.
- [ ] **AC-A3-4** — `Print Derived Resistance Breakdown` in **từng hệ số riêng biệt**, và **tích của chúng phải bằng đúng** hệ số gộp mà engine dùng. Đây là kiểm Luật 10: debug action phải in đúng thứ code thật đang tính, không phải tính lại theo cách khác.
- [ ] **AC-A3-5** — Pawn Shooting lv20 tích severity **chậm hơn rõ rệt** pawn lv0 trong cùng tình huống. Đo bằng `Print Suppression Output Matrix`, dán số cụ thể — không viết "có vẻ chậm hơn".
- [ ] **AC-A3-6** — Pawn có `PainShockThreshold` cao (dev-mode gán trait/hediff) kháng tốt hơn pawn thường, đúng chiều bảng trên.
- [ ] **AC-A3-7** — **Động vật và mechanoid** (không có `skills`): bắn vào chúng **không lỗi đỏ**, hệ số kỹ năng bị bỏ qua chứ không tính bằng 0.
- [ ] **AC-A3-8** — `enableDerivedSuppressionResistance = false` → severity quay về **đúng bằng** giá trị trước khi làm A3, **không cần restart**. So bằng `Regression: Compare To Baseline` với baseline chụp trước A3.
- [ ] **AC-A3-9** — **Mọi** hằng số trong bảng đều có slider nối vào code thật; kéo mỗi slider và xác nhận `Print Derived Resistance Breakdown` đổi theo. Liệt kê từng slider trong báo cáo (Luật 9 + Luật 10).
- [ ] **AC-A3-10** — Grep 10 field mới: giá trị mặc định ở **field** và ở **`Scribe_Values.Look`** giống hệt nhau. Dán bảng đối chiếu vào báo cáo. *(Xem bẫy ở §1.)*
- [ ] **AC-A3-11** — Gỡ mod giữa save: load lại không lỗi đỏ.

---

## 3. Bắt buộc trước khi báo cáo xong

Chạy đủ và **dán output vào báo cáo**, không tóm tắt:

```bash
cd FireDiscipline/Source/FireDiscipline && dotnet build
```

Trong game (Debug menu → Fire Discipline):
- `Print Patch Registration Audit` — **trước và sau**, để chứng minh không thêm patch nào
- `Regression: Capture Baseline` → `Compare To Baseline`
- `Print Suppression Output Matrix`
- `Print Suppression Stat Values` (A2) · `Print Derived Resistance Breakdown` (A3)

**Không được báo "xong" nếu chưa chạy game.** Build sạch không chứng minh được gì về AC-A2-2 trở đi. Nếu không chạy được game, nói thẳng mục nào chưa kiểm, đừng đánh dấu xong.

### Quy tắc commit

**A2 một commit, A3 một commit riêng** — không gộp. Commit message tiếng Anh. **Cấm:** vi phạm 10 luật · "tiện tay" refactor ngoài phạm vi · sửa nhiều module trong một commit · đổi con số cân bằng mà không ghi lại giá trị cũ.

### Sau khi xong — cập nhật tài liệu, đây là bắt buộc không phải tuỳ chọn

Dự án có luật: **sửa hành vi thì grep luôn chuỗi mô tả hành vi đó.** Cụ thể:

- `docs/FireDiscipline/actual_features_report.md` mục **#6** (Suppression Engine) — hiện mô tả `+0.25 severity mỗi phát đạn` **phẳng cho mọi pawn**. Sau A3 câu đó **sai**. Phải sửa.
- `docs/FireDiscipline/1.1-adoption-plan.md` §7 — thêm dòng nhật ký.

### Khi nào phải DỪNG LẠI VÀ HỎI

- Cần thêm bất kỳ Harmony patch nào (cả A2 và A3 đều **không** cần patch mới — nếu thấy mình cần, tức là đang đi sai đường)
- Cần transpiler, hoặc cần mutate object cấp Def
- Cần phân biệt pawn/vũ khí bằng `defName` hoặc chuỗi
- Phát hiện một AC không thể thoả mãn như đã viết
- Phát hiện một con số trong tài liệu này **mâu thuẫn với code thật** — code là sự thật, tài liệu có thể cũ; báo lại chỗ lệch thay vì tự chọn một bên
