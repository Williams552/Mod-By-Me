# Reality Report — Fire Discipline
**Ngày:** 2026-08-05
**Phạm vi:** đọc toàn bộ `FireDiscipline/Source/FireDiscipline/` + Defs + About + csproj. Không sửa một dòng nào.

> **Kết luận một câu:** code build sạch và kiến trúc module/PatchRegistry là thật, nhưng **khoảng một phần ba lượng code đã viết chưa bao giờ được đăng ký nên không chạy** — trong đó có toàn bộ engine suppression. Bảng "trạng thái được tuyên bố" ở mục 3 handoff lệch nặng nhất đúng ở Module 3.

---

## 4.1 Tồn tại & cấu trúc

### Lệch đường dẫn gốc — ⚠️
Handoff ghi `Root = d:\Games\Rimworld\Mod By Me\`, `Source = Source/FireDiscipline/`.
Thực tế mod nằm sâu thêm một cấp: `D:\Games\Rimworld\Mod By Me\FireDiscipline\`.

| Handoff | Thực tế |
|---|---|
| `Source/FireDiscipline/` | `FireDiscipline/Source/FireDiscipline/` |
| `1.6/Assemblies/FireDiscipline.dll` | `FireDiscipline/1.6/Assemblies/FireDiscipline.dll` ✅ tồn tại |
| `1.6/Defs/HediffDefs/Hediffs_FireDiscipline.xml` | `FireDiscipline/1.6/Defs/...` ✅ tồn tại |

`Mod By Me/` root còn chứa `FireDiscipline.rar`, `Reference Mods/`, `RunAndGun - Continued/`, `docs/`. Không phải repo git (`git rev-parse` fail) → **không có lịch sử commit, không rollback được**. Quy tắc "một module một commit" ở mục 8 handoff hiện không thực thi được.

### Danh sách file (28 file `.cs`, 2 file sinh tự động trong `obj/` bỏ qua)

| File | Dòng |
|---|---|
| `AimStance/AimStanceMode.cs` | 10 |
| `AimStance/AimStanceModule.cs` | 84 |
| `AimStance/AimStanceTracker.cs` | 129 |
| `AimStance/PassiveStanceEvaluator.cs` | 35 |
| `AimStance/Patch_Pawn_GetGizmos.cs` | 60 |
| `AimStance/Patch_Pawn_PathFollower.cs` | 35 |
| `AimStance/Patch_ShotReport.cs` | 115 |
| `AimStance/Patch_Verb_AdjustedCooldownTicks.cs` | 30 |
| `AimStance/Patch_Verb_WarmupTicks.cs` | 26 |
| `AimStance/StatPart_AimStance.cs` | 82 |
| `AimStance/StatPart_ShootingAccuracy.cs` | 53 |
| `AimStance/StatPart_WeaponAccuracy.cs` | 97 |
| `Core/DebugHarness.cs` | 406 |
| `Core/EmbrasureUtility.cs` | 60 |
| `Core/IModule.cs` | 32 |
| `Core/PatchRegistry.cs` | 56 |
| `Encumbrance/EncumbranceModule.cs` | 45 |
| `Encumbrance/StatPart_Encumbrance.cs` | 78 |
| `FireDisciplineMod.cs` | 190 |
| `FireDisciplineSettings.cs` | 143 |
| `Graze/GrazeModule.cs` | 37 |
| `Graze/Patch_DamageWorker_AddInjury.cs` | 121 |
| `Shock/Patch_Explosion.cs` | 109 |
| `Shock/Patch_Pawn_Kill_Down.cs` | 57 |
| `Shock/ShockModule.cs` | 47 |
| `Suppression/Patch_Projectile_Impact.cs` | 149 |
| `Suppression/Patch_Verb_Available.cs` | 31 |
| `Suppression/SuppressionIntegrationModule.cs` | 67 |

**Tổng ≈ 2 384 dòng.**

### File trong bảng mục 3 nhưng KHÔNG tồn tại
- `StatPart_AimingDelay.cs` — ❌ không có file tên đó, nhưng **class `StatPart_AimingDelay` có thật**, nằm trong `AimStance/StatPart_AimStance.cs:14`. Chỉ lệch tên file, không lệch chức năng. ✅ về mặt nội dung.
- Ngoài ra tất cả file còn lại trong bảng đều tồn tại.

### File tồn tại nhưng KHÔNG có trong bảng mục 3 (12 file)
`AimStanceMode.cs` · `Patch_Pawn_GetGizmos.cs` · `Patch_Verb_AdjustedCooldownTicks.cs` · `Patch_Verb_WarmupTicks.cs` · `StatPart_ShootingAccuracy.cs` · `StatPart_WeaponAccuracy.cs` · `DebugHarness.cs` · `EmbrasureUtility.cs` · `IModule.cs` · `PatchRegistry.cs` · `FireDisciplineMod.cs` / `FireDisciplineSettings.cs` · `Patch_Verb_Available.cs`

Đáng chú ý: `EmbrasureUtility.cs` + `Patch_Verb_Available.cs` là **công việc Đợt B (B4 Embrasure, B5 Pinned) đã được viết và đang chạy**, dù handoff mục 5 ghi Đợt B "sau v1.0, mặc định TẮT" và B4 bị chặn bởi B3/ILSpy 6.8. Xem 4.2 luật 5 & mục "Rủi ro quy trình".

### `PatchRegistry` và `IModule` — ✅ CÓ THẬT
- `Core/PatchRegistry.cs:12` — static class, `RegisterModule()` + `InitializeAll()`, mỗi module bọc trong `try/catch` riêng (`:49`), có log ENABLED/DISABLED. Không hề gọi `PatchAll`.
- `Core/IModule.cs:9` — interface đầy đủ: `ModuleId`, `DisplayName`, `Description`, `DefaultEnabled`, `IsEnabled`, `ShouldEnable()`, `OnStartup()`, `ApplyPatches(Harmony)`.
- `FireDisciplineMod.cs:176-188` — `[StaticConstructorOnStartup]` đăng ký đủ 5 module rồi `InitializeAll()`.

Đây là phần khớp tài liệu tốt nhất trong toàn bộ dự án.

---

## 4.2 Tuân thủ luật mục 2

### ❌ VI PHẠM NGHIÊM TRỌNG — Luật 5: "Không chạm Pathfinding / ThinkTree / JobGiver"

`AimStance/Patch_Pawn_PathFollower.cs:15-33` là **Prefix trả về `false`** trên `Pawn_PathFollower.StartPath`:

```csharp
if (AimStanceTracker.IsInTransition(___pawn))
{
    return false; // Suppress movement StartPath
}
```

Đây không phải "thêm JobDriver mới" — đây là **huỷ hoàn toàn** một lời gọi pathfinding gốc. Hệ quả:
- Job đã cấp cho pawn vẫn tồn tại nhưng `PathFollower` không bao giờ khởi động → pawn kẹt cho tới khi job tự timeout hoặc bị huỷ. Không có cơ chế retry sau khi transition hết hạn.
- `StartPath` được gọi từ trong lòng `JobDriver`/`Pawn_JobTracker`; nuốt nó là điểm xung đột trực tiếp với mọi mod di chuyển (`RunAndGun` — vốn nằm ngay cạnh trong `Mod By Me/`, `Giddy-Up`, `Vanilla Expanded`).
- `Messages.Message` ở `:23` bắn mỗi lần pawn Prone được lệnh đi → spam thông báo khi ra lệnh hàng loạt.

Đây là hook rủi ro cao **thứ hai** mà tài liệu thiết kế không hề nhắc tới (tài liệu chỉ cảnh báo về `ShotReport`). Nó cũng **không có toggle riêng** — đi kèm module AimStance.

### ⚠️ VI PHẠM — Luật 2: "Suy ra, đừng khai báo. Cấm hardcode danh sách defName"

`Core/EmbrasureUtility.cs:47-54`:
```csharp
string defName = b.def.defName.ToLower();
string label = b.def.label.ToLower();
if (defName.Contains("embrasure") || label.Contains("embrasure") ||
    (b.def.building != null && !b.def.building.isStuffableAirtight))
    return true;
```
Hai vấn đề chồng lên nhau:
1. **Khớp chuỗi defName/label** — chính xác là thứ luật 2 cấm. Không hoạt động với modlist không phải tiếng Anh (`label` đã bị dịch).
2. **`!isStuffableAirtight` là cửa mở toang.** Cờ này mặc định `false` trên phần lớn building. Nghĩa là **gần như mọi công trình Impassable đều được tính là embrasure**, kể cả tường đá granite tự nhiên, cửa đóng, tủ lạnh sâu. Pawn đứng cạnh bất kỳ bức tường nào sẽ nhận `accuracy ×0.85` (phạt) — mà người chơi không có cách nào biết.

Nhánh chính `:39-44` (`Impassable && fill >= 0.60 && < 1.0`) thì đúng tinh thần suy-ra, nhưng ngưỡng **0.60** lệch tài liệu (5.7 ghi `>= 0.65`).

Cùng loại smell, nhẹ hơn (bộ phận cơ thể chứ không phải vũ khí/mod): `Graze/Patch_DamageWorker_AddInjury.cs:83-98` (`IsVitalOrganOrHead`) và `:106-111` (`FindOuterLimb`) khớp chuỗi `"brain"/"heart"/"arm"/"leg"/"torso"`. Không hoạt động với race mod (Androids, Hybrid, Alien Framework) và không hoạt động với client tiếng khác qua nhánh `label`.

Điểm tốt: `Patch_ShotReport.CalculateD0()` (`:102-113`), `StatPart_WeaponAccuracy.CalculateD0()` (`:83-95`) và `Patch_Projectile_Impact.IsShotgun()` (`:136-147`) **suy ra hoàn toàn từ `AccuracyTouch`/`AccuracyMedium`/`range`/`explosionRadius`** — đúng luật 2 hoàn hảo. Đây là phần code tốt nhất của dự án.

### ✅ Luật 1 — Không thay class gốc
Grep `verbClass` / `thingClass` / `projectile.thingClass`: **0 kết quả**. Không có `DamageWorker` subclass nào; Graze chỉ Prefix lên `DamageWorker_AddInjury.Apply` (`GrazeModule.cs:28-32`) và **luôn `return true`** (`Patch_DamageWorker_AddInjury.cs:77`) — chỉ mutate `dinfo`, không chặn. Đúng luật.

### ✅ Luật 3 — Hediff / StatPart / Comp
3 Hediff (`FD_Suppressed`, `FD_CombatShock`, `FD_ShellShock`) đều `HediffWithComps` + `HediffCompProperties_Disappears`, tự tan sau 180–600 tick. Không có custom `hediffClass` → **gỡ mod không vỡ save** ✅. StatPart tiêm động vào `MoveSpeed`, `AimingDelayFactor`, `ShootingAccuracyPawn` lúc `OnStartup()` → cũng không rò rỉ qua save ✅.

### ⚠️ Luật 4 — "Đăng ký thủ công, không PatchAll"
`PatchAll` : **0 kết quả** ✅.

Nhưng có **2 attribute `[HarmonyPatch]` mồ côi** còn sót:
- `AimStance/Patch_Verb_AdjustedCooldownTicks.cs:12`
- `Suppression/Patch_Verb_Available.cs:12`

Hiện vô hại (không ai gọi `PatchAll` nên attribute không kích hoạt). Nhưng chúng là **bom hẹn giờ**: chỉ cần một ngày nào đó có người thêm `PatchAll()`, hai patch này sẽ đăng ký kèm và bỏ qua toàn bộ tầng toggle. `Patch_Verb_Available` còn tệ hơn — nó vừa có attribute vừa được đăng ký thủ công ở `SuppressionIntegrationModule.cs:62` → sẽ patch **hai lần**.

### ✅ Luật 6 — Không hard dependency
`SuppressionIntegrationModule.cs:27-29` dùng `ModsConfig.IsActive` cho 3 packageId. `About.xml` chỉ có `<loadAfter>`, không có `<modDependencies>` ✅.

### ✅ Luật 7 — Transpiler
Grep `Transpiler`: **0 kết quả**. Toàn bộ là postfix/prefix. Không nợ kỹ thuật loại này.

### ✅ Luật 8 — Không mutate `verbProps` / Def
Grep các phép gán vào `verbProps.*`: **0 kết quả**. `verbProps` chỉ được **đọc** (`StatPart_AimStance.cs:69-70`, `Patch_ShotReport.cs:63-65`, `Patch_Verb_AdjustedCooldownTicks.cs:19`, `DebugHarness.cs:50-52`) ✅.

Ngoại lệ có bàn: `AimStanceModule.OnStartup()` và `EncumbranceModule.OnStartup()` **có ghi vào `StatDef.parts`** — tức mutate Def cấp global. Đây là kỹ thuật tiêm StatPart tiêu chuẩn của RimWorld, idempotent (`.Any(p => p is ...)` guard), và không rò rỉ qua save. Tôi coi là **hợp lệ**, nhưng cần ghi nhận: nó chạy trong `OnStartup()` — được gọi **trước** `ShouldEnable()` trong `PatchRegistry.cs:37-39`, nên **StatPart vẫn bị tiêm ngay cả khi module bị TẮT**. Xem "Bug" 4.5-#1.

---

## ❌❌ PHÁT HIỆN LỚN NHẤT: code chết — đã viết, chưa bao giờ đăng ký

Đối chiếu mọi `harmony.Patch(...)` thực sự được gọi với mọi file `Patch_*.cs` tồn tại:

| Patch class | Được đăng ký ở | Có chạy? |
|---|---|---|
| `Patch_Pawn_GetGizmos` | `AimStanceModule.cs:58` | ✅ |
| `Patch_ShotReport` | `AimStanceModule.cs:65` | ✅ |
| `Patch_Pawn_PathFollower` | `AimStanceModule.cs:77` | ✅ |
| `Patch_DamageWorker_AddInjury` | `GrazeModule.cs:32` | ✅ |
| `Patch_Pawn_Kill_Down` | `ShockModule.cs:33` | ✅ |
| `Patch_Explosion` | `ShockModule.cs:42` | ✅ |
| `Patch_Verb_Available` | `SuppressionIntegrationModule.cs:62` | ✅ (chỉ khi có mod suppression ngoài) |
| **`Patch_Projectile_Impact`** | **KHÔNG Ở ĐÂU** | ❌ **CHẾT** |
| **`Patch_Verb_AdjustedCooldownTicks`** | **KHÔNG Ở ĐÂU** | ❌ **CHẾT** |
| **`Patch_Verb_WarmupTicks`** | **KHÔNG Ở ĐÂU** | ❌ **CHẾT** |
| **`StatPart_WeaponAccuracy`** | **KHÔNG tiêm vào StatDef nào** | ❌ **CHẾT** |

`Patch_Projectile_Impact.cs` là **149 dòng chết** — chứa toàn bộ:
- engine suppression nội bộ (`FD_Suppressed` buildup, `baseSuppression = 0.25`)
- nhân suppression Rapid ×1.5
- reset warmup Sharpshot khi bị bắn
- kháng suppression embrasure ×0.30
- **toàn bộ Shotgun Spread AoE (B2)** — R=2.5, `e = lerp(0.15, 0.55, skill/20)`, splash limb, suppression ×0.4

→ **Module 3 (Suppression) trong bảng mục 3 handoff về cơ bản là không đúng.** Cái duy nhất còn sống của module đó là `Patch_Verb_Available` (Pinned), tức là **đúng thứ Đợt B5 đáng lẽ chưa được làm và phải mặc định TẮT**.

Hệ quả dây chuyền: `FD_Suppressed` **không bao giờ được tạo ra bởi gameplay**. Nguồn duy nhất tạo nó là debug action `TestSuppressionImpact`. Vậy nên `Patch_Verb_Available` (Pinned >= 0.80) trên thực tế **không bao giờ kích hoạt trong game thật** — nó chỉ tốn một postfix trên `Verb.Available` (hàm gọi cực nhiều) để luôn trả về cùng kết quả.

---

## 4.3 Trạng thái hạ tầng

### `FieldInfo` trong `Patch_ShotReport` — ✅ ĐÃ CACHE THẬT, cách làm tốt hơn tuyên bố
`Patch_ShotReport.cs:20-21`:
```csharp
private static readonly AccessTools.StructFieldRef<ShotReport, float> shooterFactorRef =
    AccessTools.StructFieldRefAccess<ShotReport, float>("factorFromShooterAndDist");
private static readonly AccessTools.StructFieldRef<ShotReport, float> targetSizeRef =
    AccessTools.StructFieldRefAccess<ShotReport, float>("factorFromTargetSize");
```
Không phải `FieldInfo` + `SetValue` (vốn boxing struct), mà là **delegate `StructFieldRef` static readonly** — zero boxing, zero allocation, ghi thẳng qua `ref`. Đây là cách đúng nhất có thể. ✅ vượt yêu cầu.

**Nhưng cùng file `:66` lại phá vỡ điều đó:**
```csharp
int shotsLeft = Traverse.Create(verb).Field("burstShotsLeft").GetValue<int>();
```
`Traverse` là reflection **chậm nhất** trong Harmony, không cache, **chạy mỗi lần `HitReportFor` được gọi** cho pawn Rapid với burst ≥ 3. `HitReportFor` chạy mỗi phát bắn **và** mỗi frame khi người chơi rê chuột nhắm. Đây là mâu thuẫn trực tiếp: tối ưu cẩn thận 2 field rồi đặt một `Traverse` ngay giữa cùng hàm.

Tương tự `Patch_Projectile_Impact.cs:26` (`Traverse...Field("launcher")`) — hiện chưa chạy vì code chết, nhưng sẽ thành vấn đề ngay khi đăng ký.

### `DebugHarness` — 8 action, thiếu 5 trong 9 action bắt buộc

| # | Tên action trong code | Dòng | Ánh xạ tài liệu 7.1 |
|---|---|---|---|
| 1 | `Print HitReport & DPS Matrix` | `:24` | **A** ✅ (gộp A vào chung) |
| 2 | `Print Incoming Target Hit Matrix (Prone Verification)` | `:107` | **B** ✅ |
| 3 | `Test Suppression Impact on Selected Pawn` | `:179` | — (ngoài danh sách) |
| 4 | `Clear Suppression & Stances` | `:236` | — (ngoài danh sách) |
| 5 | `Test Graze Shot on Selected Pawn` | `:261` | — (không phải C; chỉ 1 lần, không phân phối) |
| 6 | `Test Proportional Shell Shock Wave` | `:307` | — (không phải D; 1 pawn, hardcode mortar 4.9) |
| 7 | `Test Embrasure Interaction on Selected Pawn` | `:351` | **G một phần** ⚠️ (1 pawn, không quét bản đồ) |
| 8 | `Print Cover Values` | `:377` | **I** ⚠️ — xem cảnh báo dưới |

**Thiếu hoàn toàn:** C `Print Graze Distribution` · D `Simulate Explosion Table` · **E `Print Weapon Classification`** · F `Test Pinned Cycle` · H `Print Shotgun Spread Damage`.

Tài liệu nói thẳng "**E là action giá trị nhất**" và A1 xếp E ưu tiên cao nhất. E hiện **không tồn tại**.

**Action I hiện đang nói dối.** `DebugHarness.cs:397`:
```csharp
sb.AppendLine($"{def.defName,-35} | {fill,12:P0} | {fill,12:P0} | ...");
```
Cột nhãn `coverPercent` in ra **chính `fillPercent`** — cùng một biến, in hai lần. Mà toàn bộ lý do tồn tại của action I là để trả lời câu hỏi ILSpy 6.8 (`coverPercent` thật là gì, tên hàm tính cover là gì). Ở dạng hiện tại nó **không thể trả lời được câu hỏi đó**, chỉ tạo ảo giác đã trả lời. B3/B4 vẫn bị chặn nguyên vẹn.

### Cột Skill 20 — ✅ CÓ
`DebugHarness.cs:20`: `skills = new int[] { 4, 10, 16, 20 }`. Cả hai ma trận (`:59`, `:138`) đều in đủ 4 cột. Khớp tuyên bố.

### Mod settings — 5 toggle module + 20 slider + 2 checkbox
- **Toggle module:** sinh động từ `PatchRegistry.Modules` (`FireDisciplineMod.cs:39-48`) → 5 toggle: `Encumbrance`, `AimStance`, `SuppressionIntegration`, `Graze`, `Shock`. Lưu theo `ModuleId` string trong `Dictionary<string,bool>` ✅ (`FireDisciplineSettings.cs:72-88`).
- **Toggle riêng cho nhóm patch `ShotReport`** — ✅ **CÓ**: `enableHighPrecisionShotReportPatch` (`FireDisciplineSettings.cs:70`, UI `FireDisciplineMod.cs:159`, kiểm tra tại `Patch_ShotReport.cs:25-26`). Đúng yêu cầu handoff mục 2.

⚠️ Nhưng toggle này chỉ **early-return trong postfix**, không gỡ patch. Patch vẫn đăng ký, vẫn nằm trên call stack của `HitReportFor`, vẫn xung đột với Yayo/CE ở tầng Harmony. Với mục đích "tránh xung đột" thì đây là nửa vời; với mục đích regression test (mục 7 định nghĩa xong #1) thì đủ.

⚠️ Không có toggle nào cho `Patch_Pawn_PathFollower` — hook rủi ro cao thứ hai.
⚠️ Không có toggle cho Embrasure và Pinned dù cả hai là công việc Đợt B ("mỗi cái là module riêng mặc định TẮT").

---

## 4.4 Hằng số thực tế — **mục quan trọng nhất**

Định dạng: `file:dòng | tên | giá trị code | giá trị tài liệu | khớp?`

### Hằng số CÓ trong settings (đúng luật "không rải magic number")

| file:dòng | tên | code | tài liệu | khớp? |
|---|---|---|---|---|
| `Settings.cs:18` | `encumbranceThreshold` | 0.15 | 15% | ✅ |
| `Settings.cs:19` | `encumbranceMaxPenalty` | 0.35 | −35% | ✅ |
| `Settings.cs:26` | `rapidMinWarmupRatio` | 0.30 | 0.30 | ✅ |
| `Settings.cs:27` | `rapidMaxWarmupRatio` | 0.75 | 0.75 | ✅ |
| `Settings.cs:30` | `rapidSuppressionMultiplier` | 1.50 | ×1.5 | ✅ |
| `Settings.cs:33` | `sharpshotWarmupMultiplier` | 1.40 | ×1.40 | ✅ |
| `Settings.cs:35` | `sharpshotDistanceExponentFactor` | 0.80 | d×0.80 | ✅ |
| `Settings.cs:36` | `sharpshotCloseRangePenalty` | 0.70 | <5ô ×0.70 | ✅ |
| `Settings.cs:37` | `sharpshotSuppressionVulnerability` | 2.00 | ×2.0 | ✅ giá trị / ❌ **không dùng** |
| `Settings.cs:40` | `proneTargetSizeFactor` | 0.65 | ×0.65 | ✅ |
| `Settings.cs:42` | `proneAccuracyMultiplier` | 0.85 | ×0.85 phẳng | ✅ |
| `Settings.cs:43` | `proneSuppressionResistance` | 0.50 | ×0.50 | ✅ giá trị / ❌ **không dùng** |
| `Settings.cs:46` | `stanceTransitionTicks` | 45 | 45 tick | ✅ |
| `Settings.cs:52` | `grazeDamageMultiplier` | 0.35 | 35% giữ lại | ✅ |
| `Settings.cs:58` | `allyShockRadius` | 6.0 | 6.0ô | ✅ |
| `Settings.cs:64` | `embrasureSuppressionMultiplier` | **0.30** | **0.35** (5.7 & bảng 5.8 "sàn cứng") | ❌ **LỆCH** |
| `Settings.cs:65` | `embrasureAccuracyMultiplier` | 0.85 | ×0.85 | ✅ |

### Hằng số MỒ CÔI trong settings — có UI, có Scribe, không code nào đọc

| file:dòng | tên | code | ghi chú |
|---|---|---|---|
| `Settings.cs:20` | `encumbranceMoveSpeedMultiplier` | 1.0 | ❌ không nơi nào đọc |
| `Settings.cs:28` | `rapidAccuracyPenaltyOther` | 0.80 | ❌ không nơi nào đọc |
| `Settings.cs:29` | `rapidAccuracyPenaltyShotgun` | 1.00 | ❌ không nơi nào đọc |
| `Settings.cs:34` | `sharpshotAccuracyBonusMultiplier` | 1.25 | ❌ không nơi nào đọc |
| `Settings.cs:51` | `grazeBaseChance` | 0.25 | ❌ **có slider UI** (`Mod.cs:109`) nhưng công thức v3 không dùng — người chơi kéo slider, không có gì xảy ra |
| `Settings.cs:59` | `shellShockRadiusMultiplier` | 2.0 | ❌ **có slider UI** (`Mod.cs:127`) — công thức v3 `min(20, r+2√r)` đã thay thế; chỉ còn `DebugHarness.cs:318` đọc rồi cũng không dùng |
| `Settings.cs:41` | `proneMoveSpeedMultiplier` | 0.60 | ⚠️ có đọc, nhưng **fallback lệch**: `StatPart_Encumbrance.cs:24,37` ghi `?? 0.50f` trong khi field khai báo `0.60f` |

Hai slider `grazeBaseChance` và `shellShockRadiusMultiplier` **hiển thị nhãn "(Default: 25%)" / "(Mortar 4.9c → 9.8c)"** cho người chơi mà không nối vào gì cả. Đây là lời hứa sai trong UI.

### MAGIC NUMBER rải trong code — ❌ vi phạm mục 2 ("Đưa hết vào mod settings hoặc hằng số đặt tên rõ")

| file:dòng | ý nghĩa | code | tài liệu | khớp? |
|---|---|---|---|---|
| `StatPart_AimStance.cs:23` | nhân AimingDelay khi transition | **3.0** | ❌ **không có trong tài liệu** | ❌ mới hoàn toàn |
| `Patch_ShotReport.cs:58` | hệ số phạt cự ly Rapid | 0.93 | `0.93^(d−d₀)` | ✅ giá trị / ❌ magic |
| `Patch_ShotReport.cs:63` | ngưỡng burst full-auto | 3 | `burstShotCount ≥ 3` | ✅ / ❌ magic |
| `Patch_ShotReport.cs:71` | giật nòng | 0.93 | `Pow(0.93, N)` | ✅ / ❌ magic |
| `Patch_ShotReport.cs:105-112` | `d₀` | 12 / 5 | 12 nếu Touch≥Medium, ngược lại 5 | ✅ / ❌ magic |
| `StatPart_WeaponAccuracy.cs:34,90-94` | như trên | 0.93 / 12 / 5 | như trên | ✅ / ❌ magic (**và code chết**) |
| `StatPart_Encumbrance.cs:75` | sàn clamp tốc độ | **0.65** | ❌ không có trong tài liệu | ⚠️ ràng buộc ẩn: nếu người chơi kéo `encumbranceMaxPenalty` lên 0.70 thì clamp 0.65 âm thầm chặn ở 0.35 |
| `Patch_Verb_AdjustedCooldownTicks.cs:25` | cooldown Rapid full-auto | 1.6 | `cooldown ×1.6` (5.4) | ✅ / ❌ magic (**và code chết**) |
| `Patch_DamageWorker_AddInjury.cs:38` | fallback `p` | 0.425 | fallback grazeChance = 0.5 → `p=0.425` đúng toán | ✅ |
| `Patch_DamageWorker_AddInjury.cs:49` | công thức graze | `(0.65−p)/0.45` | `clamp(0,1,(0.65−p)/0.45)` | ✅ **A2 ĐÃ LÀM RỒI** |
| `Patch_Projectile_Impact.cs:34` | suppression cơ sở | 0.25 | ❌ không có trong tài liệu | ❌ magic (code chết) |
| `Patch_Projectile_Impact.cs:45` | bán kính lan suppression | 3.5 | ❌ không có trong tài liệu | ❌ magic (code chết) |
| `Patch_Projectile_Impact.cs:85` | shotgun R | 2.5 | `R = 2.5 ô` | ✅ / ❌ magic (code chết) |
| `Patch_Projectile_Impact.cs:87` | viền shotgun `e` | `lerp(0.15,0.55,skill/20)` | `lerp(0.15,0.55,skill/20)` | ✅ (code chết) |
| `Patch_Projectile_Impact.cs:88` | damage chính shotgun | 0.70 | `primaryDamage ×0.70` | ✅ / ❌ magic (code chết) |
| `Patch_Projectile_Impact.cs:122,127` | suppression splash | 0.10 | tài liệu ghi `×0.4` (tương đối), code ghi `0.10` (tuyệt đối) | ⚠️ **không so sánh được** — đổi đơn vị |
| `Patch_Verb_Available.cs:24` | ngưỡng Pinned | 0.80 | `severity > 0.8` | ✅ / ❌ magic + **là B5, chưa được duyệt** |
| `Patch_Pawn_Kill_Down.cs:40,46` | severity CombatShock | 0.35 | ❌ không có trong tài liệu | ❌ magic |
| `Patch_Explosion.cs:26` | sàn damAmount | 10 | `damAmount < 10` (gate b) | ✅ / ❌ magic |
| `Patch_Explosion.cs:39` | bán kính shock | `min(20, r+2√r)` | `min(20, r+2√r)` | ✅ **A3 ĐÃ LÀM RỒI** |
| `Patch_Explosion.cs:40` | powerFactor | `clamp(dam/50, 0.4, 1.0)` | `clamp(0.4,1.0,damAmount/50)` | ✅ |
| `Patch_Explosion.cs:50` | trần pawn | 40 | trần 40 pawn (gate e) | ✅ / ❌ magic |
| `Patch_Explosion.cs:64` | severity vùng nổ trực tiếp | 0.85 | ❌ không có trong tài liệu | ❌ magic |
| `Patch_Explosion.cs:77` | nhân pawn không drafted | 0.30 | `×0.3` (gate c) | ✅ / ❌ magic |
| `Patch_Explosion.cs:80` | sàn cắt severity | 0.15 | `< 0.15` (gate a) | ✅ / ❌ magic |
| `AimStanceTracker.cs:32` | throttle passive stance | **45 tick** | A4 yêu cầu 30–60 tick | ✅ **A4 ĐÃ LÀM RỒI** |
| `PassiveStanceEvaluator.cs:22,26` | ngưỡng cự ly passive | 6 / 30 | ❌ không có trong tài liệu | ❌ magic |
| `EmbrasureUtility.cs:41` | ngưỡng fillPercent | **0.60** | `>= 0.65` (5.7 & B4) | ❌ **LỆCH** |
| `DebugHarness.cs:387-388` | k / floor cover | 0.40 / 0.35 | `k=0.40`, sàn `0.35` | ✅ |

### Hằng số trong XML Def — hoàn toàn không có trong tài liệu thiết kế
`Hediffs_FireDiscipline.xml` chứa ~18 con số cân bằng chưa từng được tài liệu hoá và **không thể chỉnh qua mod settings**:
- `FD_Suppressed`: tan sau `180~300` tick; stage light (≥0.1) `AimingDelayFactor +0.20`; stage heavy (≥0.5) `+0.45`, `MoveSpeed −0.35`
- `FD_CombatShock`: `240~420` tick; `AimingDelayFactor +0.30`
- `FD_ShellShock`: `300~600` tick; concussed `+0.25 / −0.20`; shell-shocked (≥0.5) `+0.60 / −0.45`

Đây là **nơi cân bằng thực sự sống**, và nó nằm ngoài tầm với của cả tài liệu lẫn người chơi.

### Ba việc trong hàng đợi Đợt A **đã được làm rồi** mà handoff vẫn ghi là chưa
- **A2** (công thức graze v3) — xong tại `Patch_DamageWorker_AddInjury.cs:49`, có gọi lại `HitReportFor` (`:44`), không chuyền state qua projectile. ✅ đúng như yêu cầu.
- **A3** (`min(20, r+2√r)` + 5 cổng lọc) — xong tại `Patch_Explosion.cs:39` và cả 5 gate `:26, :50, :55, :77, :80`. ✅
- **A4** (throttle `PassiveStanceEvaluator` 30–60 tick) — xong tại `AimStanceTracker.cs:32` (45 tick). ✅

Handoff mục 5 đang mô tả một quá khứ đã bị vượt qua. **Hàng đợi Đợt A thực chất chỉ còn A1 (debug action) và A5 (chạy test).**

---

## 4.5 Bug và rủi ro

### Nghiêm trọng

**#1 — StatPart bị tiêm kể cả khi module TẮT (phá vỡ regression test).**
`PatchRegistry.cs:37-39` gọi `module.OnStartup()` **trước** rồi mới kiểm tra `ShouldEnable()`. Nhưng `AimStanceModule.OnStartup()` (`:26-48`) và `EncumbranceModule.OnStartup()` (`:25-37`) chính là nơi tiêm StatPart. Kết quả: tắt module Encumbrance trong settings → `StatPart_Encumbrance` **vẫn nằm trong `StatDefOf.MoveSpeed.parts` và vẫn chạy `TransformValue`**. Nó không kiểm tra `IsModuleEnabled` ở đâu cả.
→ **Định nghĩa "xong" #1 ở mục 7 handoff (tắt hết feature phải khớp tuyệt đối vanilla) hiện KHÔNG THỂ ĐẠT.** Đây phải là thứ sửa trước khi chạy bất kỳ test nào, vì mọi số đo baseline đều sai.

**#2 — `DebugHarness` gán thẳng `pawn.Position`.**
`DebugHarness.cs:155` và `:167`: `selectedPawn.Position = originalPos + new IntVec3(dist, 0, 0);`
Setter `Thing.Position` **không cập nhật thingGrid/regionGrid** khi thing đã spawn — đó là việc của `Position` setter chỉ khi despawn. Dịch chuyển pawn đã spawn theo cách này để lại pawn trong ô cũ ở lưới không gian trong khi toạ độ logic ở ô mới → lỗi region, lỗi pathfinding, có thể nhân bản pawn trong grid. Và nếu bất kỳ dòng nào giữa `:155` và `:167` throw, **pawn kẹt vĩnh viễn ở vị trí sai**. Cùng vấn đề với `skills.Level` (`:72`, `:151`) — nếu throw giữa chừng, skill của pawn bị hỏng vĩnh viễn, không có `try/finally`.

**#3 — Rò rỉ bộ nhớ: 3 Dictionary không bao giờ dọn.**
`AimStanceTracker.cs:15-17` giữ `pawnStances`, `passiveCache`, `transitionEndTicks` khoá theo `thingIDNumber`. Không có hook `Notify_Despawned` / `Pawn.Kill` / `Pawn.Destroy` nào xoá entry. `ClearCache()` (`:122`) tồn tại nhưng **không nơi nào gọi**. Trong một save chạy dài với hàng nghìn raider, `passiveCache` phình vô hạn — và nó nhận **mọi pawn không thuộc player**, kể cả thú rừng, muffalo, chuột. Không nghiêm trọng về TPS (chỉ tra dictionary) nhưng là rò rỉ thật và tồn tại qua nhiều lần load map.
Ngoài ra `pawnStances` **không được Scribe** → mọi tư thế reset về SnapShot sau khi load. Có thể là chủ ý, nhưng chưa ghi ở đâu.

**#4 — Cấp phát rác trên hot path.**
`new GrazeModule()` mỗi lần một pawn nhận sát thương (`Patch_DamageWorker_AddInjury.cs:21`) · `new ShockModule()` mỗi lần pawn chết (`Patch_Pawn_Kill_Down.cs:16`) và mỗi vụ nổ (`Patch_Explosion.cs:19`) · `new SuppressionIntegrationModule()` mỗi va chạm đạn (`Patch_Projectile_Impact.cs:32`).
Chỉ để đọc `ModuleId` là một string hằng. Trong một raid lớn, đây là hàng nghìn object rác/giây đẩy vào GC — thứ Dubs Performance Analyzer sẽ bắt ngay. Sửa: cache instance static trong module hoặc so sánh trực tiếp bằng string literal.

**#5 — Graze gọi lại `HitReportFor` bên trong `DamageWorker.Apply`.**
`Patch_DamageWorker_AddInjury.cs:44`. Hai vấn đề:
- **Đệ quy vào chính patch của mình** — `HitReportFor` đã bị `Patch_ShotReport` postfix, nên mỗi đòn vital chạy lại toàn bộ chuỗi tính stance + `Traverse` reflection + quét embrasure 8 ô. Không vô hạn, nhưng đắt gấp đôi dự kiến.
- **Sai ngữ nghĩa:** `p` được tính lại tại **thời điểm đạn chạm**, không phải thời điểm bắn. Với đạn bay 1–2 giây, shooter có thể đã đổi tư thế, đã di chuyển, đã bị suppress, hoặc **đã ngã**. Tài liệu 5.1 ghi rõ "`p = TotalEstimatedHitChance tại thời điểm bắn`". Đây là lệch có chủ đích được ghi trong A2 ("gọi lại `HitReportFor`, không chuyền state qua projectile") nên **chấp nhận được** — nhưng cần biết là con số đo được sẽ không khớp lý thuyết.
- Nếu `shooter` đã chết/despawn, `equipment` có thể null → rơi về fallback `p=0.425` một cách im lặng (`:38`). Không crash ✅.

### Trung bình

**#6 — `Patch_Pawn_GetGizmos` NRE nếu `__result` null.** `:17` `foreach (var g in __result)` không có null-guard. Vanilla luôn trả non-null, nhưng một mod khác postfix trước và trả null sẽ làm nổ toàn bộ UI gizmo. Rẻ để sửa.

**#7 — `ContentFinder<Texture2D>.Get` trong vòng lặp gizmo.** `:49` chạy mỗi lần `GetGizmos()` được gọi (nhiều lần/giây khi có pawn được chọn). `ContentFinder` có cache nội bộ nên không thảm hoạ, nhưng nên là `static readonly`.

**#8 — `EmbrasureUtility.IsUsingEmbrasure` chạy mỗi shot report, không cache.** `Patch_ShotReport.cs:84` + `StatPart_ShootingAccuracy.cs:26`. Mỗi lần: 8 lần `GetEdifice` + với mỗi edifice là 2 lần `ToLower()` **cấp phát string mới** (`EmbrasureUtility.cs:48-49`). `HitReportFor` chạy mỗi frame khi rê chuột nhắm → hàng chục string rác mỗi frame. Comment ở `:20` khẳng định "<0.00002ms" — con số đó không đúng khi có `ToLower()` trong vòng lặp.

**#9 — `DefDatabase<HediffDef>.GetNamedSilentFail` trong hot path.** `Patch_Verb_Available.cs:20` (mỗi lần kiểm tra verb — rất nhiều), `Patch_Projectile_Impact.cs:61,116`, `Patch_Pawn_Kill_Down.cs:28`, `Patch_Explosion.cs:42`. Nên là `[DefOf]` static hoặc cache static readonly sau khi DefDatabase load xong.

**#10 — `Patch_Explosion` cho phép `Cut` và `Blunt` qua cổng lọc.** `:27` — gate b của tài liệu ghi "damType phi vật lý thì bỏ qua". `Cut`/`Blunt` không phải nổ; một `Explosion` với damType Cut (một số mod dùng cho mảnh văng) với `damAmount >= 10` sẽ gây shell shock. Cần xác nhận đây là chủ ý.

**#11 — `Patch_Pawn_Kill_Down` chỉ patch `Kill`, không patch downed.** Tên class, comment (`:10`), và mô tả module (`ShockModule.cs:12` "when a pawn is downed/killed") đều nói "downed", nhưng `ShockModule.cs:29` chỉ patch `Pawn.Kill`. **Pawn bị hạ gục (downed) không kích hoạt Combat Shock.** Trong RimWorld phần lớn pawn bị downed chứ không chết → tính năng này hiếm khi kích hoạt so với mô tả. Bảng mục 3 handoff ghi "CombatShock bán kính 6.0" mà không nói rõ điều kiện, nên đây là lệch giữa **code và chính tên của nó**.

**#12 — `Log.Message` mỗi lần graze** (`Patch_DamageWorker_AddInjury.cs:74`) và **mỗi lần đổi stance** (`AimStanceTracker.cs:100`) và **mỗi lần reset warmup** (`:112`). Trong raid lớn đây là spam log nặng — `Log.Message` ở RimWorld không rẻ (nó ghi vào buffer UI). Nên bọc sau một cờ `devMode`.

**#13 — `SetStance` tính phí transition sai so với tài liệu.**
Tài liệu 5.2 (`:92`): *"ra lệnh di chuyển khi Prone → tự về SnapShot + 45 ticks. **Về SnapShot luôn miễn phí.**"*
Code `AimStanceTracker.cs:78-84`: áp `Stance_Cooldown(45)` khi vào **bất kỳ** stance non-Snap nào (Rapid, Sharpshot, Prone). Và `:66-73` áp phí khi Prone→SnapShot, tức **về SnapShot KHÔNG miễn phí** khi đang Prone.
Cộng với `StatPart_AimStance.cs:23` nhân `AimingDelayFactor ×3.0` trong suốt transition. Tổng chi phí đổi tư thế cao hơn thiết kế đáng kể, và ×3.0 là con số chưa từng được duyệt.

**#14 — `PassiveStanceEvaluator` đọc `pawn.mindState.enemyTarget`.** Có `?.` guard ✅ và `IsValid` check ✅ nên an toàn. Tần suất: **45 tick (0.75s) mỗi pawn không thuộc player**, cache tại `AimStanceTracker.cs:32`. Đạt yêu cầu A4 (30–60 tick) ✅. Nhưng cache áp cho **mọi** pawn non-player kể cả thú vật — nên gate bằng `RaceProps.Humanlike` để giảm rác dictionary.

### Nhẹ / cần xác nhận

**#15 — Shotgun splash không loại trừ đồng đội.** `Patch_Projectile_Impact.cs:94` comment ghi rõ "No faction exemption (friendly fire splash possible)". Tài liệu 5.5 mục (a) ghi câu hỏi này **⏸ Chưa quyết**. Code đã tự quyết. (Hiện là code chết nên chưa gây hại.)

**#16 — Không có cảnh báo UI vùng nguy hiểm shotgun.** Tài liệu 5.5 mục (b) ghi **"Bắt buộc — không có, người chơi sẽ nghĩ mod bị lỗi"**. Chưa tồn tại.

**#17 — `0Harmony.dll` được ship trong `1.6/Assemblies/`** (bản 2022-07-20). RimWorld 1.6 tự nạp Harmony; ship kèm DLL riêng là nguồn xung đột version kinh điển. Cách chuẩn là khai `<modDependencies>` tới `brrainz.harmony` — `About.xml` hiện **không có** `modDependencies`, chỉ có `loadAfter`.

**#18 — Tuyên bố "đã test gỡ mod giữa save".** Định nghĩa "xong" #5 yêu cầu test **với pawn đang mang hediff**. Vì `FD_Suppressed` hiện không bao giờ sinh ra từ gameplay (code chết), test này chưa từng được chạy đúng điều kiện với hediff đó. `FD_CombatShock`/`FD_ShellShock` thì có thể.

---

## 4.6 Build

| Hạng mục | Kết quả |
|---|---|
| Lệnh | `dotnet build` tại `Source/FireDiscipline/` |
| Kết quả | ✅ **Build succeeded — 0 Warning, 0 Error** (10.2s) |
| Target framework | `net472` |
| Tham chiếu Assembly-CSharp | `Krafs.Rimworld.Ref` **1.6.\*** (NuGet, `PrivateAssets="all"`) — không cần copy DLL tay |
| Harmony | `Lib.Harmony` **2.2.2** (NuGet, `PrivateAssets="all"`) |
| Output | `..\..\1.6\Assemblies\`, `AppendTargetFrameworkToOutputPath=false` ✅ |
| DLL hiện tại | `FireDiscipline.dll` 62 464 bytes, 2026-08-05 22:53 — **mới hơn mọi file .cs** (mới nhất: `DebugHarness.cs` 22:21) → DLL đang khớp source ✅ |

Không có warning nào cả — kể cả với `StatPart_WeaponAccuracy` và 3 patch class chết. Đó là vì chúng là `public` class nên compiler không coi là unused. **Trình biên dịch sẽ không bao giờ bắt được lỗi "quên đăng ký patch" cho anh.**

`Source/lib/README.txt` mô tả 2 cách build thủ công (`/p:RimWorldPath=...` hoặc copy DLL vào `Source/lib/`) — cả hai đều **thừa**: csproj hiện dùng `Krafs.Rimworld.Ref` nên không tham chiếu `Source/lib/` hay `RimWorldPath` ở đâu cả. README này đã lạc hậu và sẽ đánh lừa người build tiếp theo.

---

## Tổng kết đối chiếu bảng mục 3

| Module | Tuyên bố | Thực tế |
|---|---|---|
| 1. Aim Stances | 4 tư thế, 45 tick, passive raider | ⚠️ **Phần lớn khớp.** 4 tư thế ✅, passive ✅ có throttle 45t. Nhưng: chi phí transition rộng hơn thiết kế + ×3.0 không duyệt (#13); `Patch_Verb_WarmupTicks`, `Patch_Verb_AdjustedCooldownTicks`, `StatPart_WeaponAccuracy` **chết**; hook PathFollower vi phạm luật 5 |
| 2. Encumbrance | <15% miễn, tuyến tính tới −35% | ✅ **Khớp gần như hoàn toàn.** Module sạch nhất. Chỉ vướng bug #1 (không tắt được) và sàn clamp 0.65 ẩn |
| 3. Suppression | Dormant khi có mod ngoài, engine `FD_Suppressed`, ma trận ×1.5/×2.0/×0.5 | ❌ **SAI VỀ CƠ BẢN.** Phát hiện mod ngoài ✅. Nhưng engine ở trong `Patch_Projectile_Impact` — **chưa đăng ký, không chạy**. ×1.5 nằm trong code chết; **×2.0 và ×0.5 không tồn tại trong bất kỳ đường code sản xuất nào**, chỉ có trong debug action. `FD_Suppressed` không bao giờ sinh ra từ gameplay. Thay vào đó lại có Pinned (B5) đang chạy |
| 4. Graze | Vital → 35%, bẻ sang chi ngoại vi | ✅ **Khớp, và đã vượt lên bản v3 (A2 xong)**. Vướng chi phí `HitReportFor` lồng nhau (#5) và khớp chuỗi body part (luật 2) |
| 5. Shock | CombatShock r=6.0, ShellShock r×2.0 | ⚠️ CombatShock ✅ nhưng **chỉ kích hoạt khi chết, không khi downed** (#11). ShellShock **đã vượt lên v3** — `min(20, r+2√r)` + đủ 5 cổng lọc (A3 xong), khiến `r×2.0` trong bảng và slider settings đều lạc hậu |
| Hạ tầng: `FieldInfo` cached | ✅ | ✅ **Đúng, và làm tốt hơn** (`StructFieldRef`). Nhưng có `Traverse` không cache ngay cạnh |
| Hạ tầng: cột Skill 20 | ✅ | ✅ Đúng |
| Hạ tầng: toggle settings | ✅ | ✅ 5 toggle module + toggle `ShotReport` riêng. Nhưng 6 setting mồ côi, 2 trong đó có slider lừa người chơi |
| Hạ tầng: test gỡ mod | ✅ (chiến trường sạch) | ⚠️ chưa test được với `FD_Suppressed` vì hediff đó không sinh ra |

---

## 3 việc cần làm ngay nhất — theo đánh giá của tôi

### 1. Sửa `PatchRegistry` để `OnStartup()` chỉ chạy khi module được bật — **trước mọi việc khác**

`PatchRegistry.cs:37-39` gọi `OnStartup()` vô điều kiện rồi mới hỏi `ShouldEnable()`. Vì `OnStartup()` chính là nơi tiêm StatPart, **module tắt vẫn tác động lên stat**.

Lập luận vì sao đây là việc số một, không phải việc số hai:

Mục 7 handoff định nghĩa "xong" bắt đầu bằng *"tắt hết feature → ma trận harness khớp **tuyệt đối** với vanilla, mọi ô. Lệch một chữ số = có patch chạy khi không nên chạy."* Ngay lúc này điều kiện đó **được đảm bảo là sẽ fail** — không phải vì có bug ngẫu nhiên nào đó, mà vì kiến trúc khởi tạo được viết như vậy. Nghĩa là:
- A5 ("chạy đủ bảng test, regression chạy đầu tiên") không thể bắt đầu.
- Mọi con số baseline đo được từ `DebugHarness` hôm nay đều là baseline **đã nhiễm Fire Discipline**, không phải vanilla.
- Nếu tune cân bằng dựa trên baseline nhiễm đó, mọi hằng số chốt về sau đều sai — và sẽ sai theo cách rất khó truy ngược, vì nó sai một cách nhất quán.

Sửa rất nhỏ (đảo thứ tự, hoặc tách `OnStartup` thành `OnStartupAlways`/`OnEnable`) nhưng nó là **cánh cửa mở ra toàn bộ Đợt A**. Mọi việc khác trong báo cáo này đều có thể chờ; việc này thì không, vì nó làm hỏng công cụ đo.

Kèm theo: thêm `try/finally` cho `DebugHarness` (`:155`, `:72`) để một exception giữa chừng không phá hỏng vĩnh viễn pawn test. Cùng một lý do — bảo vệ công cụ đo.

### 2. Quyết định số phận của `Patch_Projectile_Impact` — đăng ký hay xoá — và đừng để nó nằm đó

149 dòng suppression + shotgun AoE đang tồn tại trong repo mà không chạy. Đây không đơn thuần là "code chết"; nó là **nguồn thông tin sai**. Nó khiến bảng mục 3 handoff ghi Module 3 đã xong, khiến `FireDisciplineSettings` có slider suppression, khiến `About.xml` quảng cáo tích hợp Suppression cho người chơi Workshop. Ba tài liệu và một UI đang mô tả một tính năng không tồn tại.

Nhưng **không nên đăng ký nó ngay**, và đây là điểm tôi muốn nói rõ:
- Nội dung của nó gộp hai thứ khác hẳn nhau về mức độ được duyệt: suppression stance (Đợt A, hợp lệ) và **Shotgun Spread AoE = B2** (Đợt B, "sau v1.0, mặc định TẮT", và câu hỏi friendly fire còn treo — mà code `:94` đã tự quyết là BẬT).
- Đăng ký nguyên khối = đẩy B2 vào v1.0 kèm một quyết định thiết kế chưa ai duyệt, cộng thêm `Traverse` reflection (`:26`) trên hot path va chạm đạn.

Đề xuất: **tách file**, đăng ký phần suppression stance dưới `SuppressionIntegrationModule` với toggle riêng, và đưa phần shotgun AoE vào module B2 riêng biệt mặc định TẮT — đúng như hàng đợi quy định. Cùng lúc, xử lý dứt điểm 3 patch class chết còn lại (`Patch_Verb_WarmupTicks`, `Patch_Verb_AdjustedCooldownTicks`, `StatPart_WeaponAccuracy`): hai cái sau thuộc B6 full-auto, `Patch_Verb_WarmupTicks` thì trùng chức năng với `StatPart_AimingDelay` đang chạy — nếu đăng ký cả hai, Sharpshot sẽ ăn ×1.4 **hai lần**.

Và xoá 2 attribute `[HarmonyPatch]` mồ côi. Chúng chỉ chờ một lần ai đó thêm `PatchAll()`.

### 3. Viết debug action **E `Print Weapon Classification`** — và sửa action I cho nó thôi nói dối

Tài liệu tự nói "E là action giá trị nhất", A1 xếp E ưu tiên cao nhất, và nó vẫn chưa tồn tại.

Lý do E quan trọng hơn vẻ ngoài của nó: **luật 2 (suy ra, đừng khai báo) hiện là một giả thuyết chưa được kiểm chứng.** Toàn bộ định vị của mod — "không đòi patch riêng cho từng mod vũ khí" — dựa vào việc `AccuracyTouch >= AccuracyMedium` thật sự phân loại đúng shotgun, và `d₀ = 12 / 5` thật sự đúng cho vũ khí mod. Hiện chưa ai từng chạy nó trên một modlist thật. Nếu heuristic đó sai — nếu một khẩu sniper nào đó của Vanilla Weapons Expanded thoả `Touch >= Medium` — thì nó không sai ở một chỗ, nó sai ở **cả `Patch_ShotReport.CalculateD0`, `StatPart_WeaponAccuracy.CalculateD0`, và `IsShotgun`** cùng lúc. Đó là ba tính năng đổ vì một giả định.

E rẻ để viết (một vòng lặp `DefDatabase<ThingDef>` + in bảng, không cần patch gì) và nó kiểm chứng nguyên tắc kiến trúc đắt nhất của dự án. Tỉ lệ giá trị trên công sức cao nhất trong toàn bộ hàng đợi.

Đi kèm, sửa `DebugHarness.cs:397` — action I đang in `fillPercent` dưới nhãn `coverPercent`. Nó đang tạo cảm giác câu hỏi ILSpy 6.8 đã có công cụ trả lời, trong khi thực tế B3 và B4 vẫn bị chặn hoàn toàn. Một action nói dối còn tệ hơn một action chưa có — vì action chưa có thì không ai tin nhầm.

---

## Ba việc *không* nên làm ngay (ghi lại để khỏi bị cám dỗ)

- **Đừng sửa `Patch_Pawn_PathFollower` vội** dù nó vi phạm luật 5. Nó vi phạm thật, cần một cuộc bàn riêng về cách thay thế (JobDef mới? `Pawn.stances` cooldown thuần?), và sửa sai còn nguy hiểm hơn để nguyên. Cần chỉ thị.
- **Đừng đổi hằng số nào** — mục 8 handoff cấm, và bảng 4.4 đã ghi lại toàn bộ giá trị hiện tại để khi được duyệt thì có cái đối chiếu.
- **Đừng động vào Embrasure/Pinned** dù chúng là Đợt B đang chạy sai chỗ. Rút chúng ra là thay đổi cân bằng lớn, cần quyết định.

---

## Rủi ro quy trình cần nêu

Hai tính năng Đợt B (**B4 Embrasure**, **B5 Suppression Pinned**) hiện **đã được viết, đã đăng ký, đang chạy, mặc định BẬT, không có toggle riêng** — trong khi handoff quy định Đợt B là "sau v1.0, mỗi cái là module riêng mặc định TẮT", và B4 phụ thuộc B3 vốn *"chặn bởi ILSpy 6.8 — không bắt đầu trước khi có câu trả lời"*.

Tôi không sửa gì. Nhưng nó có nghĩa là **hàng đợi công việc không phản ánh cái đang chạy trong game**, và tài liệu thiết kế cũng không. Trước khi nhận chỉ thị tiếp theo, hai câu hỏi nên được trả lời:

1. Embrasure và Pinned được đưa vào có chủ đích (và handoff cần cập nhật), hay lọt vào ngoài ý muốn (và cần rút ra sau toggle mặc định TẮT)?
2. `docs/ilspy-findings.md` **chưa tồn tại**. Cả 8 câu hỏi mục 6 vẫn chưa có câu trả lời nào được ghi lại. B3/B4/B6 vẫn bị chặn nguyên trạng — kể cả khi B4 đang chạy trong code.

**Báo cáo kết thúc. Không có file nào bị sửa. Chờ chỉ thị.**
