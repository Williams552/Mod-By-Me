# TASK SPEC — B1 (Suppression Stage Marker) & B3 (Evacuate Downed Ally)

> **Dành cho agent thực thi.** Tài liệu này **tự chứa** — đọc xong là làm được, không cần hội thoại trước đó.
> Bối cảnh quyết định: [`../1.1-adoption-plan.md`](../1.1-adoption-plan.md) §4. Luật bắt buộc: [`../architecture.md`](../architecture.md) §2.

---

## 0. Bối cảnh tối thiểu cần biết

**Mod:** Fire Discipline — mod chiến thuật bắn súng cho RimWorld 1.6.
**packageId:** `william.firediscipline` · **Root:** `FireDiscipline/` · **Source:** `FireDiscipline/Source/FireDiscipline/`
**Defs:** `FireDiscipline/1.6/Defs/` · **Assembly đích:** `FireDiscipline/1.6/Assemblies/FireDiscipline.dll`

```bash
dotnet build
```

Build tự deploy vào thư mục Mods. **RimWorld nạp assembly lúc khởi động — luôn phải restart game để thấy thay đổi code.**

**Ngôn ngữ output:** code, comment, commit message, chuỗi UI đều bằng **tiếng Anh**. Chỉ tài liệu trong `docs/` mới là tiếng Việt.

### Kiến trúc module — bắt buộc theo

Mod dùng **đăng ký Harmony thủ công**, không bao giờ `PatchAll()`. Mỗi tính năng là một `IModule` ([`Core/IModule.cs`](../../../FireDiscipline/Source/FireDiscipline/Core/IModule.cs)):

```csharp
public interface IModule
{
    string ModuleId { get; }
    string DisplayName { get; }
    string Description { get; }
    bool DefaultEnabled { get; }
    bool IsEnabled { get; set; }
    bool ShouldEnable();      // đọc từ FireDisciplineMod.Settings
    void OnStartup();         // setup không phải patch; CHỈ chạy khi ShouldEnable() true
    void ApplyPatches(Harmony harmony);   // đăng ký patch thủ công
}
```

Mẫu tham chiếu đầy đủ: [`Suppression/SuppressionCoreModule.cs`](../../../FireDiscipline/Source/FireDiscipline/Suppression/SuppressionCoreModule.cs).
Module phải được `PatchRegistry.RegisterModule(...)` trong [`FireDisciplineMod.cs`](../../../FireDiscipline/Source/FireDiscipline/FireDisciplineMod.cs).

### Trạng thái suppression hiện có — dữ liệu B1 và B3 cùng đọc

Hediff `FD_Suppressed`, `minSeverity 0` / `maxSeverity 9.0`, định nghĩa tại `1.6/Defs/HediffDefs/Hediffs_FireDiscipline.xml`.

| Stage (label trong XML) | minSeverity |
|---|---|
| `unsettled` | 0 |
| `shaken` | 0.5 |
| `wavering` | 1.0 |
| `ducking` | 2.0 |
| `cowering` | 5.5 |
| **Pinned** (không phải stage XML — ngưỡng code) | `Settings.pinnedSeverityThreshold = 7.0` |

Đọc severity bằng **`SuppressionEngine.GetSeverity(Pawn)`** ([`Suppression/SuppressionEngine.cs:201`](../../../FireDiscipline/Source/FireDiscipline/Suppression/SuppressionEngine.cs)). **Không tự truy hediff bằng tay.**

---

## 1. MƯỜI LUẬT — vi phạm bất kỳ luật nào thì DỪNG LẠI VÀ HỎI, không tự quyết

1. **Không thay class gốc.** Không set `verbClass`, `thingClass`, `projectile.thingClass`, không thay `DamageWorker`. Chỉ Harmony postfix/prefix lên hàm tính toán.
2. **Suy ra, đừng khai báo.** Giá trị cho vũ khí/giáp/công trình của mod khác phải tính từ stat hoặc field Def vanilla. **Cấm** hardcode danh sách `defName`. **Cấm** khớp chuỗi `defName`/`label`. **Cấm** file patch XML riêng cho từng mod.
3. **Cộng thêm bằng Hediff / Comp / StatPart.** Gỡ mod không được vỡ save.
4. **Đăng ký Harmony thủ công** qua `PatchRegistry` + `IModule`. **Không** `PatchAll()`. **Không để lại attribute `[HarmonyPatch]` mồ côi.** Feature tắt → patch không được đăng ký **và không có tác dụng phụ nào**.
5. **Không chạm Pathfinding / ThinkTree / JobGiver.** Thêm JobDef/JobDriver **mới** thì được; patch cái có sẵn thì không. **Đặc biệt: không Prefix trả `false` lên bất kỳ hàm nào của vanilla.**
6. **Không hard dependency.** Phát hiện mod khác qua `ModsConfig.IsActive`.
7. **Transpiler là nợ kỹ thuật.** Ưu tiên postfix. *(Cả B1 và B3 đều KHÔNG được dùng transpiler.)*
8. **Không mutate `verbProps` hay bất kỳ object cấp Def nào.** Ngoại lệ duy nhất đã duyệt: tiêm StatPart vào `StatDef.parts` trong `OnStartup()`, có guard idempotent.
9. **Mọi hằng số cân bằng phải nằm trong mod settings hoặc hằng số đặt tên rõ.** Không magic number rải trong code. Áp dụng cho cả XML Def.
10. **UI không được nói dối.** Slider phải nối vào code thật. Debug action phải in đúng thứ nó ghi nhãn. `About.xml` không quảng cáo tính năng không chạy.

> **Luật 10 là luật bị vi phạm nhiều nhất** — không phải vì ai cố tình, mà vì *một câu đúng lúc viết không tự cập nhật khi thứ nó mô tả thay đổi*. Quy tắc thực hành: **sửa hành vi thì grep luôn chuỗi mô tả hành vi đó.**

---

## TASK B1 — Suppression Stage Marker

### Mục tiêu

Hiện một chỉ báo nhỏ phía trên pawn đang bị áp chế, cho biết **stage hiện tại**. Thuần hiển thị, **không đổi một con số cân bằng nào**.

### Ràng buộc thực thi

⛔ **KHÔNG patch `PawnRenderer`.** Đây là điểm cốt lõi của thiết kế này. Vẽ bằng `MapComponent.MapComponentOnGUI()` + `GenMapUI.LabelDrawPosFor(pawn)`. Hệ quả: **module này không đăng ký một Harmony patch nào cả** — `ApplyPatches` để trống, và `Reference Mods/Yayo's Combat 3 (Continued)` / `Yayo's Shooting 2` không thể xung đột.

### Việc phải làm

| # | Việc | Nơi |
|---|---|---|
| 1 | `SuppressionMarkerModule : IModule`, `ModuleId = "SuppressionMarker"`, `DefaultEnabled => false` | `Suppression/SuppressionMarkerModule.cs` (mới) |
| 2 | Đăng ký module vào `PatchRegistry` | `FireDisciplineMod.cs` |
| 3 | `MapComponent_SuppressionMarker : MapComponent`, override `MapComponentOnGUI()` | `Suppression/MapComponent_SuppressionMarker.cs` (mới) |
| 4 | Settings: `enableSuppressionMarker` (bool, default `false`), `suppressionMarkerMinSeverity` (float, default `1.0`), `suppressionMarkerScale` (float, default `1.0`) | `FireDisciplineSettings.cs` — nhớ cả field **và** `Scribe_Values.Look` **cùng giá trị mặc định** |
| 5 | Slider + checkbox trong settings window | `FireDisciplineMod.cs` |
| 6 | Debug action `Print Suppression Marker State` — in bảng mọi pawn trên map: tên, severity, stage được chọn, có vẽ hay không | `Core/DebugHarness.cs` |

### Quy tắc hiển thị

- Chỉ vẽ khi `PatchRegistry.IsModuleEnabled("SuppressionMarker")` **và** `Find.CurrentMap != null` **và** không đang ở màn hình khác.
- Chỉ vẽ cho pawn **spawned**, **không `Dead`**, có `SuppressionEngine.GetSeverity(pawn) >= suppressionMarkerMinSeverity`.
- Stage → nhãn/màu suy ra từ **ngưỡng trong bảng §0**, đọc `pinnedSeverityThreshold` từ settings. **Không hardcode `7.0f`** (Luật 9).
- Bỏ qua pawn không nhìn thấy được (`pawn.Position.Fogged(map)`).

### Acceptance Criteria — B1

- [ ] **AC-B1-1** — `dotnet build` sạch, không warning mới.
- [ ] **AC-B1-2** — Debug action `Print Patch Registration Audit` cho thấy `SuppressionMarker` **đã đăng ký, đang TẮT, 0 Harmony patch**.
- [ ] **AC-B1-3** — Với module TẮT (mặc định): grep toàn source không có `[HarmonyPatch]` mồ côi thuộc module này; vào game không thấy marker nào; `MapComponentOnGUI` thoát ngay ở dòng guard đầu tiên.
- [ ] **AC-B1-4** — Bật module + restart: bắn cho một pawn tích severity, marker hiện lên, **đổi nhãn đúng ngưỡng** khi severity vượt 1.0 → 2.0 → 5.5 → `pinnedSeverityThreshold`.
- [ ] **AC-B1-5** — Kéo slider `suppressionMarkerMinSeverity` lên 3.0 → pawn ở severity 2.0 **hết** marker ngay, **không cần restart** (Luật: tắt/chỉnh là tức thì, chỉ bật mới cần restart).
- [ ] **AC-B1-6** — Debug action `Print Suppression Marker State` in ra số liệu **khớp với thứ đang thấy trên màn hình** (Luật 10). Pawn có marker trên màn hình phải có cột "đang vẽ = true".
- [ ] **AC-B1-7** — `Regression: Capture Baseline` → `Compare To Baseline`: **không có một con số nào đổi**. Đây là tính năng thuần hiển thị; bất kỳ sai lệch nào cũng là lỗi.
- [ ] **AC-B1-8** — Bật cùng lúc với `Yayo's Combat 3 (Continued)`: không lỗi đỏ, pawn vẫn vẽ bình thường.
- [ ] **AC-B1-9** — Grep `suppressionMarker` trong `FireDisciplineSettings.cs`: giá trị mặc định ở **field** và ở **`Scribe_Values.Look`** phải **giống hệt nhau**. *(Đây là lỗi đã từng xảy ra với `coverSuppressionFactor` — xem §6.2 của adoption plan.)*

---

## TASK B3 — Evacuate Downed Ally

### Mục tiêu

Người chơi ra lệnh cho một pawn **cõng đồng đội đã gục ra khỏi vùng hoả lực**, tới một ô do người chơi chỉ định. Khác `Rescue` của vanilla: vanilla cõng về **giường**, cái này cõng tới **một ô bất kỳ**.

Lý do tồn tại: hiện khi một pawn gục giữa đồng trống, lựa chọn duy nhất là Rescue — kéo suốt quãng đường về giường, xuyên qua hoả lực. Không có cách nào bảo "lôi nó ra sau bức tường kia, ngay bây giờ".

### Ràng buộc thực thi — ĐỌC KỸ, ĐÂY LÀ CHỖ DỄ VI PHẠM LUẬT 5

✅ **Được:** thêm `JobDef` **mới**, `JobDriver` **mới**, postfix `FloatMenuMakerMap.AddHumanlikeOrders` để thêm mục chuột phải.
⛔ **Cấm tuyệt đối:** đụng vào ThinkTree, JobGiver, `Pawn_JobTracker`, hay bất cứ thứ gì khiến **AI tự động** đi cõng. Đây **chỉ** là lệnh người chơi ra tay. Nếu thấy mình đang định patch một `ThinkNode` — **dừng lại và hỏi**.
⛔ **Cấm** Prefix trả `false` lên bất kỳ hàm vanilla nào.

### Việc phải làm

| # | Việc | Nơi |
|---|---|---|
| 1 | `JobDef` `FD_EvacuatePawn` — `driverClass = FireDiscipline.Rescue.JobDriver_EvacuatePawn`, `reportString = "evacuating TargetA."` | `1.6/Defs/JobDefs/Jobs_FireDiscipline.xml` (mới) |
| 2 | `JobDriver_EvacuatePawn` — targetA = pawn gục, targetB = ô đích | `Rescue/JobDriver_EvacuatePawn.cs` (mới) |
| 3 | `Patch_FloatMenuMakerMap` — postfix thêm option `"Evacuate {0}"`, mở `Targeter` chọn ô đích | `Rescue/Patch_FloatMenuMakerMap.cs` (mới) |
| 4 | `EvacuationModule : IModule`, `ModuleId = "Evacuation"`, `DefaultEnabled => false` | `Rescue/EvacuationModule.cs` (mới) |
| 5 | Đăng ký module vào `PatchRegistry` | `FireDisciplineMod.cs` |
| 6 | Settings: `enableEvacuation` (bool, default `false`), `evacuationRequiresLowerSuppression` (bool, default `true`), `evacuationMaxDistance` (float, default `30`) | `FireDisciplineSettings.cs` |
| 7 | Keyed strings tiếng Anh cho mọi chuỗi UI | `1.6/Languages/English/Keyed/` |
| 8 | Debug action `Print Evacuation Eligibility` — chọn 2 pawn, in ra từng điều kiện gate và kết quả | `Core/DebugHarness.cs` |

### Điều kiện cho phép ra lệnh (gate)

Mỗi điều kiện **fail** phải cho ra một `FloatMenuOption` **bị disable kèm lý do**, không phải biến mất im lặng — đây là yêu cầu Luật 10.

| Điều kiện | Lý do hiển thị khi fail |
|---|---|
| Mục tiêu là pawn, `Downed`, không thù địch | `"hostile target"` |
| Người cõng không đang mang gì | `"already carrying something"` |
| Người cõng có `Manipulation` đủ | `"incapable of manipulation required for evacuation"` |
| `CanReach` + `CanReserve` mục tiêu | `"cannot reach or reserve"` |
| **Nếu `evacuationRequiresLowerSuppression`:** `GetSeverity(carrier) < GetSeverity(target)` và cách nhau ≥ 1 stage | `"carrier must be at least one suppression stage lower"` |
| Ô đích trong `evacuationMaxDistance`, đứng được, có đường tới | `"destination unreachable"` |

*(Bộ gate và cách diễn đạt tham chiếu `Reference Mods/Misstall's Combat Tweaks/Languages/English/Keyed/StagedSuppression.xml` — cùng bài toán, đã có lời giải tốt.)*

### Hành vi JobDriver

1. `Toils_Goto` tới targetA, fail nếu target hết `Downed` hoặc bị người khác giữ chỗ.
2. `Toils_Haul.StartCarryThing` tương đương — dùng `pawn.carryTracker.TryStartCarry(targetPawn)`.
3. `Toils_Goto` tới targetB.
4. Thả xuống: `pawn.carryTracker.TryDropCarriedThing(targetB.Cell, ThingPlaceMode.Direct, out _)`.
5. **`FailOn`**: target hồi tỉnh, target chết, ô đích bị chặn, người cõng gục hoặc bị `Pinned`.

⚠️ **Tương tác với Pinned:** severity ≥ `pinnedSeverityThreshold` khoá `Verb.Available` (không bắn được). Job này **phải fail** nếu người cõng rơi vào Pinned giữa chừng — nếu không sẽ có cảnh pawn bị ghim chặt vẫn thong dong cõng người đi. Ghi rõ điều này trong comment.

### Acceptance Criteria — B3

- [ ] **AC-B3-1** — `dotnet build` sạch, không warning mới.
- [ ] **AC-B3-2** — Debug action `Print Patch Registration Audit` cho thấy `Evacuation` đã đăng ký, đang TẮT, **đúng 1 patch** (`FloatMenuMakerMap`), và patch đó **không tồn tại** khi module TẮT.
- [ ] **AC-B3-3** — **Kiểm Luật 5 (bắt buộc, không được bỏ):** grep toàn bộ source mới thêm — **không có** `ThinkNode`, `JobGiver`, `ThinkTree`, và **không có** `Prefix` nào `return false`. Dán kết quả grep vào báo cáo.
- [ ] **AC-B3-4** — Module TẮT: chuột phải lên đồng đội gục **không** thấy mục Evacuate.
- [ ] **AC-B3-5** — Module BẬT + restart: chuột phải lên đồng đội gục → thấy `"Evacuate {tên}"` → chọn → con trỏ chuyển sang chế độ chọn ô → chọn ô → pawn đi tới, cõng lên, mang tới ô đó, đặt xuống.
- [ ] **AC-B3-6** — **Mỗi** điều kiện fail ở bảng gate cho ra option **bị disable kèm đúng lý do đó**, không im lặng biến mất. Kiểm đủ **cả 6 dòng**, liệt kê từng dòng trong báo cáo.
- [ ] **AC-B3-7** — `evacuationRequiresLowerSuppression = false` → gate stage biến mất, 5 gate còn lại vẫn chạy.
- [ ] **AC-B3-8** — Người cõng bị bắn tới Pinned giữa đường → job **fail**, pawn thả người xuống, **không** có lỗi đỏ.
- [ ] **AC-B3-9** — Target hồi tỉnh giữa chừng → job fail sạch, không lỗi đỏ, không pawn nào kẹt trạng thái carry.
- [ ] **AC-B3-10** — **Save / load giữa lúc đang cõng**: load lại không lỗi đỏ, không mất pawn. *(Đây là chỗ JobDriver mới hay vỡ nhất — `Scribe` của target phải đúng.)*
- [ ] **AC-B3-11** — **Gỡ mod giữa save khi có pawn từng dùng job này**: load được, không lỗi đỏ (Luật 3).
- [ ] **AC-B3-12** — `Regression: Capture Baseline` → `Compare To Baseline`: không con số nào đổi.
- [ ] **AC-B3-13** — Mọi chuỗi UI đều qua `Keyed`, **không** literal tiếng Anh nhúng thẳng trong C#.

---

## 3. Bắt buộc trước khi báo cáo xong — cho cả hai task

Chạy đủ và **dán output vào báo cáo**, không tóm tắt:

```bash
dotnet build
```

Trong game (Debug menu → Fire Discipline):
- `Print Patch Registration Audit`
- `Regression: Capture Baseline` → `Compare To Baseline`
- `Print Suppression Marker State` (B1) · `Print Evacuation Eligibility` (B3)

**Không được báo "xong" nếu chưa chạy game.** Build sạch không chứng minh được điều gì về AC-B1-4 trở đi, AC-B3-5 trở đi. Nếu không chạy được game, hãy nói thẳng là chưa kiểm được mục nào, đừng đánh dấu xong.

### Quy tắc commit

Một module một commit. Commit message tiếng Anh. **Cấm:** vi phạm 10 luật · "tiện tay" refactor ngoài phạm vi · sửa nhiều module trong một commit · đổi con số cân bằng mà không ghi lại giá trị cũ.

### Khi nào phải DỪNG LẠI VÀ HỎI

- Cần patch bất cứ thứ gì thuộc ThinkTree / JobGiver / Pathfinding
- Cần transpiler
- Cần mutate một object cấp Def
- Phát hiện một AC không thể thoả mãn như đã viết
- Phát hiện một con số trong tài liệu này **mâu thuẫn với code thật** — code là sự thật, tài liệu có thể cũ; báo lại chỗ lệch thay vì tự chọn một bên
