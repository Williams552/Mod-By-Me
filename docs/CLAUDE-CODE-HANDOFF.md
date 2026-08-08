# Fire Discipline — Claude Code Session Handoff

> **Đọc file này đầu tiên trong mỗi phiên Claude Code.**
> Phiên bản: **v5** — cập nhật cuối phiên 2026-08-07. **Đợt A hoàn tất trừ A9.** Regression PASS lần đầu.
> v1 mô tả trạng thái sai. v2 đúng tại thời điểm viết nhưng A1/A1b/A2/A4 đã hoàn thành từ đó.
> Tài liệu thiết kế đầy đủ: `docs/fire-discipline-master-design.md`. File này KHÔNG lặp lại nó.
> Reality Report gốc: `docs/reality-report-2026-08-05.md`.

---

## 0. Trạng thái hiện tại

Đợt A: **A1–A8 xong. A10 phần chặn đã PASS.** Chỉ còn **A9** và phần còn lại của A10.

**🎯 Mốc quan trọng: bài regression lần đầu tiên chạy được và ĐẠT.**
16/16 ô hit chance + `AimingDelayFactor` + `ShootingAccuracyPawn` khớp tuyệt đối với vanilla khi bật đủ 6 module. Định nghĩa "xong" #1 đã thoả. Nó bị chặn từ đầu dự án vì bug A1. Thứ đổi nhiều nhất so với v3:

| v2 nói | Giờ |
|---|---|
| Tắt hết feature không khớp vanilla được | ✅ **Sửa xong.** `OnStartup()` chỉ chạy khi module bật; StatPart có guard runtime |
| B4 Embrasure và B5 Pinned đang BẬT không toggle | ✅ **Sửa xong.** Cả hai có toggle riêng, mặc định TẮT |
| Luật 2 chưa từng được kiểm chứng | ✅ **Đã kiểm chứng trên 163 vũ khí / 6 nguồn.** Heuristic cũ sai ~73%, đã viết lại, giờ **0 false positive** |
| Debug harness thiếu 5 action | ⚠️ Còn thiếu **D, F, H**. Đã có A, B, C, E, G, I — tổng **16 action** |
| Engine suppression chưa từng chạy | ✅ **Chạy thật.** Xác minh bằng log từng phát bắn trong game |
| Không ai biết phương sai sát thương là bao nhiêu | ✅ **Đã đo.** CV 0.31 (LMG) / 0.77 (súng phát một) trong cửa sổ 10 giây |

**Rủi ro lớn nhất đã gỡ.** Vòng lặp Rapid ghim → vu hồi → Sharpshot dứt điểm giờ chạy được: `Patch_Projectile_Impact` đã đăng ký (dạng Prefix), ma trận tư thế tồn tại, suppression tích luỹ và suy giảm đúng.

**Rủi ro lớn nhất hiện tại:** chưa ai **chơi thử** vòng lặp đó. Nó chạy về mặt kỹ thuật, chưa được xác nhận là *vui*. Và **D1** (`Patch_Pawn_PathFollower` prefix trả `false`) vẫn là blocker phát hành.

**Quy tắc vận hành:** sau mỗi phiên, mục 3 và 5 phải được **sinh lại từ code**, không chỉnh tay từ bản cũ.

---

## 1. Bối cảnh tối thiểu

**Fire Discipline** là mod RimWorld 1.6 thêm một **lớp chiến thuật** lên combat vanilla: tư thế tác chiến, suppression, encumbrance, graze, shell shock.

> Không viết lại hệ thống combat. Không yêu cầu save mới. Không đòi patch riêng cho từng mod vũ khí. Chạy độc lập, tự phối hợp khi phát hiện mod khác.

Đây là phản đề của Combat Extended. Mỗi lần phân vân giữa "làm cho đúng về mô phỏng" và "làm cho tương thích", **chọn tương thích**.

| | |
|---|---|
| packageId | `william.firediscipline` |
| RimWorld | 1.6 only |
| Ngôn ngữ output | **Tiếng Anh** — code, comment, commit, chuỗi UI, mô tả Workshop |
| Ngôn ngữ tài liệu | Tiếng Việt |
| Root thật | `D:\Games\Rimworld\Mod By Me\FireDiscipline\` |
| Source | `Source/FireDiscipline/` (34 file `.cs`) |
| Assembly đích | `1.6/Assemblies/FireDiscipline.dll` |
| Defs | `1.6/Defs/HediffDefs/` + `1.6/Defs/EffecterDefs/` |
| Mods folder | `D:\SteamLibrary\steamapps\common\RimWorld\Mods\FireDiscipline` |

⚠️ Root **sâu hơn một cấp** so với v1/v2 (`Mod By Me/FireDiscipline/`, không phải `Mod By Me/`). Không phải repo git — **không có lịch sử commit, không rollback được.**

### Build & deploy
```
dotnet build                        # build + tự deploy vào Mods folder
dotnet build /p:SkipDeploy=true     # build không deploy
.\deploy.ps1 -NoBuild               # chỉ deploy
.\deploy.ps1 -WhatIf                # thử khô
```
`deploy.ps1` ở root mod, được gọi tự động qua target `DeployToRimWorld` trong csproj.
**RimWorld nạp assembly lúc khởi động — luôn phải restart game để thấy thay đổi.**

---

## 2. LUẬT BẤT DI BẤT DỊCH

Vi phạm bất kỳ luật nào = dừng lại và hỏi, không tự quyết.

1. **Không thay class gốc.** Không set `verbClass`, `thingClass`, `projectile.thingClass`, không thay `DamageWorker`. Chỉ Harmony postfix/prefix lên hàm tính toán.
2. **Suy ra, đừng khai báo.** Giá trị cho vũ khí/giáp/công trình của mod khác phải tính từ stat vanilla. **Cấm** hardcode danh sách defName. **Cấm** khớp chuỗi `defName`/`label`. **Cấm** file patch XML riêng cho từng mod.
3. **Cộng thêm bằng Hediff / Comp / StatPart.** Gỡ mod không được vỡ save.
4. **Đăng ký Harmony thủ công** qua `PatchRegistry` + `IModule`. **Không** `PatchAll()`. **Không để lại attribute `[HarmonyPatch]` mồ côi.** Feature tắt → patch không được đăng ký **và không có tác dụng phụ nào**.
5. **Không chạm Pathfinding / ThinkTree / JobGiver.** Thêm JobDef/JobDriver mới thì được; patch cái có sẵn thì không. **Đặc biệt: không Prefix trả `false` lên bất kỳ hàm nào của vanilla.**
6. **Không hard dependency.** Phát hiện mod khác qua `ModsConfig.IsActive`.
7. **Transpiler là nợ kỹ thuật.** Ưu tiên postfix.
8. **Không mutate `verbProps` hay bất kỳ object cấp Def nào.** Ngoại lệ duy nhất đã duyệt: tiêm StatPart vào `StatDef.parts` trong `OnStartup()`, có guard idempotent.
9. **Mọi hằng số cân bằng phải nằm trong mod settings hoặc hằng số đặt tên rõ.** Không magic number rải trong code. Áp dụng cho cả XML Def.
10. **UI không được nói dối.** Slider phải nối vào code thật. Debug action phải in đúng thứ nó ghi nhãn. `About.xml` không quảng cáo tính năng không chạy.

### Hệ quả kiến trúc đã biết

- **StatPart không truy cập được khoảng cách.** Mọi modifier phụ thuộc cự ly buộc phải đi qua postfix `ShotReport.HitReportFor`. Đã có toggle riêng.
- **StatPart đã tiêm thì không gỡ ra được.** Nên mỗi StatPart phải tự kiểm `PatchRegistry.IsModuleEnabled(...)` ở đầu `TransformValue`. Đây là cách duy nhất để tắt giữa phiên có hiệu lực.
- **Tắt là tức thì, bật cần restart.** Patch chỉ đăng ký lúc khởi động. Settings window log cảnh báo khi người chơi bật module giữa phiên.
- **Ba giả định của tài liệu thiết kế: 2 đã xác nhận, 1 còn lại.** ✅ Tên hàm cover = `Verse.CoverUtility.CalculateOverallBlockChance`, đã gộp trọng số hướng. ✅ Khói và ánh sáng **không** lẫn vào cover — chúng là field riêng trên `ShotReport`. ⚠️ Bảng `coverPercent` 30/40/55/75% **vẫn là ước lượng** — cần đo `BaseBlockChance` trong game trước khi tune B3.
- **Không có con số nào là giá trị chốt** cho tới khi qua bảng test mục 7.3 tài liệu thiết kế.

---

## 3. Trạng thái đã xác minh *(sinh lại từ code 2026-08-07)*

| Module | Trạng thái thật |
|---|---|
| 1. Aim Stances | ⚠️ 4 tư thế ✅, passive raider ✅, guard runtime ✅, code chết đã dọn (A5) ✅. Còn: `Patch_Pawn_PathFollower` vi phạm luật 5 (**D1**); magic `×3.0` transition chưa duyệt (**D4**). Ứng viên cải tiến: chuyển Prone sang `ShotReport.FactorFromPosture` (ILSpy 6.5) |
| 2. Encumbrance | ✅ Đo theo `MassUtility.Capacity` (số tab Gear hiện), **chỉ tính vũ khí + túi, không tính giáp** — vanilla đã tính phí giáp rồi. Dải 0% → −35% tuỳ vũ khí |
| 3. Suppression | ✅ **Chạy thật lần đầu.** `SuppressionCoreModule` sở hữu engine, người chơi tự bật/tắt (phương án E). Ma trận ×1.5/×2.0/×0.5 lần đầu tồn tại trong code. Có effecter hiển thị. Hediff thang 0–9, 5 stage, suy giảm dần |
| 4. Graze | ✅ Khớp. Công thức tách thành `CalculateGrazeChance()` public. **Đo được: gần như không giảm phương sai** (chỉ chạm 11.9% số phát trúng). Còn khớp chuỗi body part (**D2**) |
| 5. Shock | ⚠️ ShellShock đủ 5 cổng lọc ✅. CombatShock **chỉ kích hoạt khi chết, không khi downed** (**D3**) |
| 6. ShotgunAoE | ✅ **B8 xong** — overlay vùng nguy hiểm, tô đỏ ô có đồng đội. Hình **nêm từ nòng** (kiểu Fire Spew), không phải đĩa. ⚠️ Đang BẬT trong config dù `DefaultEnabled = false` |
| Phân loại vũ khí | ✅ **Đã kiểm chứng thực nghiệm.** 163 vũ khí / 6 nguồn → 12 shotgun, **0 false positive**, 1 false negative (`Gun_Scattergun`, đã chấp nhận) |
| Embrasure | ✅ **A8 xong.** Chỉ còn `Impassable && fill ∈ [0.65, 1.0)`. Xoá khớp chuỗi defName/label và `!isStuffableAirtight` |

### Hạ tầng — đã xác minh
- `PatchRegistry` + `IModule` có thật, không `PatchAll` ✅
- `OnStartup()` **chỉ chạy khi module bật** ✅ (A1)
- `IModule.IsEnabled` được gán thật, đồng bộ với settings window ✅ (A1b)
- Guard runtime ở `StatPart_AimingDelay`, `StatPart_ShootingAccuracy`, `StatPart_Encumbrance`, `Patch_ShotReport`, `Patch_Pawn_GetGizmos`, `Patch_Pawn_PathFollower` ✅ (A1b)
- Embrasure + Pinned có toggle riêng, **mặc định TẮT**; Pinned tắt → `Verb.Available` không được patch ✅ (A2)
- `FieldInfo` cache bằng `StructFieldRef` ✅ · cột Skill 20 ✅ · toggle `ShotReport` riêng ✅
- `DebugHarness`: **18 action**, có `try/finally` bảo vệ pawn test ✅
- 0 transpiler · 0 mutate `verbProps` · Hediff không custom class (gỡ mod không vỡ save) ✅
- Build sạch 0 warning · auto-deploy sau build ✅

### Nợ đã ghi nhận
- `Patch_Verb_AdjustedCooldownTicks` vẫn chưa đăng ký — **cố ý**, thuộc B6, comment mở đầu bằng `NOT REGISTERED`. Kiểm bằng action `Print Patch Registration Audit`
- ~18 hằng số cân bằng trong `Hediffs_FireDiscipline.xml` nằm ngoài tài liệu và ngoài settings → **A9**
- `d0` đổi từ nhị phân sang liên tục → **đổi đường phạt cự ly Rapid trên toàn bộ modlist**, chưa đo cân bằng → **A10**
- `AimStanceTracker` giữ 3 Dictionary không bao giờ dọn; `ClearCache()` không nơi nào gọi
- `new XModule()` cấp phát mỗi lần sát thương / mỗi vụ nổ / mỗi va chạm đạn

### Hằng số đã đổi trong phiên này *(giá trị cũ ghi lại theo mục 8)*

| Nơi | Cũ | Mới |
|---|---|---|
| `CalculateD0` | nhị phân `12f` / `5f` theo `accTouch >= accMedium` | `4 + closeBias × 12`, dải [4, 16] |
| `StatPart_WeaponAccuracy.CalculateD0` | bản sao của công thức nhị phân | uỷ quyền cho `Patch_ShotReport.CalculateD0` |
| `HasShotgunProfile` | `(accTouch >= accMedium) && range <= 25f` | 5 gate suy-ra |
| `shotgunMaxRange` | 25 → 20 | **17** |
| `shotgunMinRange` | không có | 8 |
| `shotgunMinPeakAccuracy` | không có | 0.55 |
| `shotgunMinLongShortRatio` | không có | 0.50 |
| `d0Base` / `d0Span` | không có | 4 / 12 |
| `weaponFilterMaxRange` | không lọc | 100 |
| `pinnedSeverityThreshold` | `0.80f` hardcode | 0.80 (settings, **giá trị không đổi**) |
| Vùng cuộn settings | 1050f | 1750f |
| Bộ lọc `destroyOnDrop` | có (loại mất vũ khí mech) | **bỏ** — mech là pawn và có tham chiến |

### Bug production đã sửa *(phiên 2026-08-07)*

Cả bốn đều là **code chạy sai từ đầu**, không phải cân bằng lệch. Tìm được bằng debug action, không phải bằng đọc code.

| # | Bug | Hệ quả thật |
|---|---|---|
| 1 | `Patch_Projectile_Impact` là **Postfix** trên `Projectile.Impact` | `Impact` huỷ viên đạn trước khi trả về → `__instance.Map` là `null` → hàm return ngay dòng đầu, **mỗi viên đạn**. Engine "chạy" nhưng đóng góp bằng 0. Đổi sang **Prefix** |
| 2 | Recoil Rapid đọc `burstShotsLeft` ngoài loạt bắn | Ngoài loạt `burstShotsLeft = 0` → `shotIndex = burstCount` → phạt **×0.93⁶ = ×0.65 vĩnh viễn**. Đo được: Rapid @3ô = 24% trong khi SnapShot = 37%. Giờ chỉ áp khi `burstShotsLeft > 0` |
| 3 | Cooldown đọc từ `verbProps.defaultCooldownTime` | Field đó **bằng 0 trên hầu hết vũ khí**; RimWorld lấy từ stat `RangedWeapon_Cooldown`. Sai ở **3 chỗ**: bảng suppression, ma trận DPS, và `CalculateRapidWarmupRatio` |
| 4 | Hệ quả của #3 trong `CalculateRapidWarmupRatio` | `cooldown = 0` → `rawRatio = 0` → **clamp về đáy 0.30 cho mọi vũ khí**. Công thức warmup của Rapid đáng lẽ suy ra từ nhịp bắn từng khẩu, thực tế là hằng số. Cả dải `0.30–0.75` chưa bao giờ được dùng |
| 5 | `HediffComp_Disappears` không refresh khi bị bắn tiếp | Suppression tan sau 3–5 giây kể từ viên **đầu**. Đo được: 219→214→…→186 tick rồi hết hạn giữa loạt. Đã thay bằng suy giảm dần + `NotifyApplied()` |

Bài học chung: **ba trong năm bug đọc sai nguồn dữ liệu** (`Map` sau khi despawn, `burstShotsLeft` ngoài ngữ cảnh, `defaultCooldownTime` thay vì stat). Không cái nào lộ ra khi đọc code — chỉ lộ khi đo.

### Kết quả kiểm chứng luật 2 *(2026-08-06, modlist thật)*
163 vũ khí ranged / Core + Anomaly + Biotech + Odyssey + VWE + Yayo → 148 vũ khí pawn, 15 bị lọc (turret/artillery/beam, kiểm hết, **không sót vũ khí pawn nào**).

**12 shotgun-like, tất cả đều là shotgun thật.** False positive: **0**. False negative: **1** — `Gun_Scattergun` (range 19.9, bị ngưỡng 17 cắt).

⚠️ `Gun_Scattergun` và `VWE_Gun_GaussMagnum` có range **giống hệt 19.9** và không tách được bằng 5 gate hiện tại. Đã chọn giữ 17: false positive (súng lục gây splash AoE) là lỗi người chơi thấy được, false negative (shotgun thiếu splash) thì không. Đây là ca mẫu cho white/blacklist sau v1.0.

**Biên mỏng cần theo dõi:** `VWE_Gun_ChargeShotgun` (l/s 0.536) và `VWE_TrenchGun` (l/s 0.533) chỉ cách gate 0.50 đúng ~0.03. Một bản cập nhật VWE hạ AccuracyLong là chúng rơi khỏi phân loại.

---

## 4. QUYẾT ĐỊNH ĐANG TREO

### 4.1 Cổng gate Suppression — **ĐÃ GIẢI QUYẾT bằng phương án E**

> Không đảo cổng, mà **bỏ cổng**. Engine luôn có mặt; `ShouldEnable()` chỉ đọc `settings.enableSuppressionEngine`.
> Dò mod ngoài chỉ dùng cho (a) đặt mặc định lần chạy đầu, (b) cảnh báo hai chiều trong settings.
> Xác minh trên modlist thật: `mlie.suppression` được dò đúng, engine mặc định TẮT khi có nó.
>
> *Ghi chép gốc của vấn đề giữ lại bên dưới.*

#### Bối cảnh gốc

`SuppressionIntegrationModule.cs:32-35` hiện là:
```csharp
return IsExternalSuppressionModActive() && (Settings?.IsModuleEnabled(this) ?? DefaultEnabled);
```
→ module bật **khi CÓ** mod suppression ngoài. Người chơi standalone: module tắt hoàn toàn.

`fire-discipline-master-design.md:106` nói ngược lại:
> *"Không có → engine nội bộ (`FD_Suppressed` + `Patch_Projectile_Impact.cs`). Có → Dormant, 0% overhead."*

Bảng test `:465` cũng ghi *"Suppression (Continued) bật → Module nội bộ tắt hoàn toàn, không double-suppress"*.

⚠️ **Đây là hai khiếm khuyết độc lập, không phải một.** Cổng gate ngược KHÔNG phải nguyên nhân `Patch_Projectile_Impact` chết — patch đó không được đăng ký ở bất kỳ đâu, kể cả khi module bật đầy đủ. Sửa cổng sẽ **không** hồi sinh nó.

**Cần quyết trước khi bắt đầu A3.** Đảo cổng là đổi hành vi module → thuộc mục "phải hỏi trước".

### 4.2 Giảm phương sai sát thương — **đã đo, đã quyết: HOÃN**

Mục tiêu người chơi đặt ra: combat kiểu RTS, sát thương dễ đoán, không có màn quá may hoặc quá xui.

**Đã mô phỏng 4 mô hình** (action `Compare Variance Models`, 20 000 cửa sổ 10 giây mỗi ô, gọi `ShotReport` và `CalculateGrazeChance` thật):

| Mô hình | Bảo toàn kỳ vọng? | CV (LMG) | CV (súng phát một, 3–12 ô) |
|---|---|---|---|
| `independent` *(hiện tại)* | — | 0.31 | 0.77 |
| `quota-carry` | ✅ chính xác | **0.07** | **0.31** |
| `pity-oneway` | ❌ 37%→44% | 0.22 | 0.64 |
| `pity-symmetric` | ❌ 37%→45%, **3%→26%** | 0.13 | 0.62 |

**Kết luận 1 — `pity` bị loại bằng bằng chứng.** Nâng accuracy sau mỗi phát trượt **không tách được** khỏi việc buff accuracy: cùng một cần gạt điều khiển cả hai. Khi `p` thấp, pawn trượt liên tục → bonus nằm lì ở trần → buff lớn nhất **đúng nơi tỉ lệ trúng tệ nhất**. Ở 40 ô: `3% → 26%`.

**Kết luận 2 — `quota-carry` hoạt động nhưng đắt.** Nó không đổi xác suất, chỉ đổi **thứ tự** các phát trúng, nên bảo toàn kỳ vọng chính xác. Nhưng nó phải chặn `Verb_LaunchProjectile.TryCastShot` — nơi vanilla quyết định viên đạn bay đi đâu. **Đó là viết lại giải quyết chiến đấu**, đúng thứ định vị mod từ chối.

**Kết luận 3 — vấn đề nhỏ hơn tưởng.** Con số `CV = 0.54` báo cáo ban đầu là **mỗi loạt bắn**. Trong cửa sổ 10 giây — thứ người chơi thật sự cảm nhận — LMG đã ở **0.31**. Toàn bộ độ hên xui tập trung ở **vũ khí bắn phát một**, và quota chỉ kéo chúng về ngang mức LMG đang có sẵn.

**Kết luận 4 — không mô hình nào cứu được tầm xa.** Ở 25–40 ô, số phát trúng kỳ vọng dưới 1 (0.57 và 0.09). Không thể làm một sự kiện xảy ra 0.57 lần trở nên đều đặn. Muốn ổn định ở đó phải **nâng accuracy**, tức đổi cân bằng vũ khí.

**Quyết định: hoãn.** Nếu làm sau v1.0 thì là module Đợt B riêng, mặc định TẮT, và cần **A6 (ILSpy)** trước để biết chặn ở đâu cho an toàn.

⚠️ **Cạm bẫy phương pháp đã gặp, ghi lại để khỏi lặp:**
- Đo **mỗi loạt** làm quota trông vô dụng với súng phát một (CV `1.34 → 1.33`). Đổi sang **cửa sổ 10 giây** mới lộ ra `0.77 → 0.31`. Metric sai không bác bỏ được gì.
- Quota khởi tạo `carry = 0` mỗi cửa sổ gây **lệch xuống** (33% so với 37%) và ở `p` thấp cho **0% tuyệt đối**. Phải khởi tạo **phase ngẫu nhiên**.
- Cột `hit%` so với `baseP` là bài kiểm tra bắt buộc cho mọi mô hình. Không có nó thì `pity` đã lọt.

### 4.3 White/blacklist vũ khí — **hoãn tới sau v1.0**

Đã bàn, đã chốt: **nên làm, nhưng là lớp ghi đè, không phải cơ chế phân loại.** Bốn điều kiện để không phá luật 2:
1. Mặc định rỗng tuyệt đối
2. Lưu trong mod settings (per-user), không bao giờ trong XML của mod
3. Debug action phải in **cả** kết quả suy ra **và** trạng thái ghi đè
4. Log cảnh báo khi có override hoạt động — mỗi entry là **một báo cáo lỗi heuristic**, không phải một cấu hình

Hoãn vì: heuristic vừa ổn định lần đầu. Mở van sớm thì phản hồi Workshop sẽ đến dưới dạng "tôi tự sửa bằng whitelist" thay vì "khẩu X phân loại sai".

---

## 5. Hàng đợi công việc *(sinh lại từ code 2026-08-06)*

### ✅ Đã xong trong phiên 2026-08-06
| # | Việc |
|---|---|
| A1 | `PatchRegistry` gọi `ShouldEnable()` trước `OnStartup()`. `IsEnabled` được gán thật. `try/finally` cho `DebugHarness` |
| A1b | Guard runtime cho 3 StatPart + 3 patch của AimStance. Cảnh báo restart khi bật module giữa phiên |
| A1c | Báo cáo cổng gate Suppression (kết quả → mục 4.1) |
| A2 | Toggle riêng Embrasure + Pinned, mặc định TẮT. Pinned tắt → không đăng ký `Verb.Available` |
| A4 | Action E `Print Weapon Classification`. Sửa action I hết nói dối + COVER API PROBE. Viết lại `HasShotgunProfile` (5 gate) và `CalculateD0` (liên tục). Bộ lọc vũ khí chung + báo cáo lý do lọc |
| — | `deploy.ps1` + auto-deploy sau build |

### ✅ Đã xong trong phiên 2026-08-07
| # | Việc |
|---|---|
| A3 | Tách `SuppressionCoreModule` + `ShotgunAoEModule`, xoá `SuppressionIntegrationModule`. Phương án E: engine luôn có mặt, người chơi tự chọn; dò mod ngoài chỉ đặt mặc định lần đầu + cảnh báo hai chiều. `About.xml` viết lại, bỏ chữ "tích hợp" |
| A8 | `EmbrasureUtility` chỉ còn `Impassable && fill ∈ [0.65, 1.0)`. Xoá khớp chuỗi defName/label và `!isStuffableAirtight`. Thêm action `Print Embrasure Detection` |
| A5 | Xoá `Patch_Verb_WarmupTicks` + `StatPart_WeaponAccuracy` (cả hai **nhân đôi** hiệu ứng đã có). Gỡ 2 attribute `[HarmonyPatch]` mồ côi — giờ codebase không còn attribute nào. Thêm action `Print Patch Registration Audit` chống tái phát |
| A7 | Gỡ 4 setting chết. Nối 2 slider nói dối vào tham số công thức thật (`grazeHitChanceCeiling`/`Span`, `shellShockRadiusCoefficient`/`Cap`). Sửa câu "take effect immediately" |
| A6 | `docs/ilspy-findings.md` — trả lời 6.1–6.9 bằng reflection. **B3 mở khoá** |
| — | Sửa 5 bug production (bảng ở mục 3) |
| — | Hediff suppression: thang 0–9, 5 stage, `HediffComp_SuppressionDecay`, effecter hiển thị (linh kiện vanilla) |
| — | Harness lên **16 action**, gồm 3 công cụ đo mới: `Suppression Output Matrix`, `Damage Distribution`, `Compare Variance Models` |

### ✅ Đã xong trong phiên 2026-08-07 (đợt sau)
| # | Việc |
|---|---|
| A5 | Xoá `Patch_Verb_WarmupTicks` + `StatPart_WeaponAccuracy` (**cả hai nhân đôi hiệu ứng đã có**). Gỡ 2 attribute `[HarmonyPatch]` mồ côi — codebase giờ **không còn attribute nào**. Thêm action `Print Patch Registration Audit` |
| A6 | `docs/ilspy-findings.md` — trả lời 6.1–6.9 **bằng reflection, không cần ILSpy**. B3 mở khoá |
| A7 | Gỡ 4 setting chết. Nối 2 slider nói dối vào tham số công thức thật. Sửa câu "take effect immediately" |
| A8 | `EmbrasureUtility` chỉ còn `Impassable && fill ∈ [0.65, 1.0)`. Thêm action `Print Embrasure Detection` |
| A10 | 2 action `Regression: Capture Baseline` / `Compare To Baseline` — **máy kiểm, không nhìn bằng mắt**. Phân loại lệch theo module sở hữu. **ĐÃ PASS** |
| — | Shotgun: sửa tự bắn mình → nửa đĩa → **hình nêm kiểu Fire Spew**. Thêm **B8** overlay vùng nguy hiểm |
| — | Encumbrance: `CarryingCapacity` → `MassUtility.Capacity`, rồi **bỏ giáp khỏi phép tính** |
| — | `master-design` §5.10 — trụ cột thiết kế "tuyến phòng thủ thay thế killbox" |

### 🔎 Phát hiện quan trọng của phiên (đọc trước khi làm tiếp)

**`ShotReport` đã mang sẵn cover.** `coversOverallBlockChance` là field trên struct ta **đã** postfix. Khói (`factorFromCoveringGas`) và ánh sáng (`offsetFromDarkness`) là field **riêng** → giả định ⚠ 5.8 được xác nhận đúng. B3 không còn bị chặn.

**Vanilla có sẵn `ShotReport.FactorFromPosture`.** Prone đang mượn `factorFromTargetSize`. Có kênh đúng để chuyển sang — nhưng phải đọc thân hàm trước (ILSpy 6.5).

**`Pawn_PathFollower.StopDead()` tồn tại.** Ứng viên thay cho Prefix trả `false` của **D1**.

**`ShieldBelt` không còn trong 1.6** — cơ chế khiên giờ là `RimWorld.CompShield` (`Energy`, `EnergyMax`, `IsBuiltIn`). Nhận diện bằng comp = đúng luật 2.

**Yayo Combat 3 không dùng inventory làm kho đạn.** Chỉ có job `EjectAmmo`; pawn tự nạp từ đạn nằm trong bán kính `supplyAmmoDist`. **Không có cách bảo pawn nhặt đạn vào túi — cách đó không tồn tại.**

**Lựu đạn đang tính hai lần**: không `flyOverhead` nên qua prefix suppression, rồi vụ nổ lại kích `Patch_Explosion` → nhận **cả `FD_Suppressed` lẫn `FD_ShellShock`**. Chưa quyết là chủ ý hay lỗi.

**`FD_ShellShock` chưa được hiện đại hoá.** `FD_Suppressed` đã sang thang 0–9, 5 stage, suy giảm dần. ShellShock **vẫn** 0–1, 2 stage, và **vẫn dùng `HediffComp_Disappears` không refresh** — đúng lỗi đã sửa cho suppression.

---

### Đợt A còn lại — trước phát hành v1.0

| # | Việc | Ghi chú |
|---|---|---|
| **A9** | Đưa ~18 hằng số trong `Hediffs_FireDiscipline.xml` vào tài liệu; cân nhắc chuyển sang settings | Nơi cân bằng thật sự sống, ngoài tầm với của cả tài liệu lẫn người chơi (luật 9) |
| **A10** | ✅ Regression PASS. Còn **~30 chỉ tiêu 7.3 cần chơi trong game** | Đo bằng action đã có: DPS matrix · Damage Distribution · Weapon Classification. Cần dựng cảnh: boomalope, firefoam, mortar ngoài tường, raid 80 pawn. **Và dòng "chơi 3 raid liên tiếp — bực hay căng" không action nào thay được** |
| — | **Chơi thử.** Vòng lặp chiến thuật chạy về mặt kỹ thuật, **chưa ai xác nhận là vui** | Đây là việc quan trọng nhất còn lại |

### Debug action còn thiếu *(mục 7.1 tài liệu thiết kế)*
Đã có: A (gộp HitReport+DPS) · B · E · G một phần · I
Còn thiếu: **C** `Print Graze Distribution` · **D** `Simulate Explosion Table` · **F** `Test Pinned Cycle` · **H** `Print Shotgun Spread Damage`

### Cần bàn riêng — không tự sửa

| # | Vấn đề | Ghi chú |
|---|---|---|
| D1 | `Patch_Pawn_PathFollower.cs` Prefix trả `false` trên `StartPath` | Vi phạm luật 5. **Blocker phát hành.** Nuốt `StartPath`, mà `RunAndGun` nằm trong modlist khuyến nghị. Đã có guard `IsModuleEnabled` nhưng thiết kế thay thế vẫn cần bàn (JobDef mới? `Pawn.stances` cooldown thuần?). Xem ILSpy 6.9 |
| D2 | Khớp chuỗi body part trong `Patch_DamageWorker_AddInjury.cs:83-98, 106-111` | Vi phạm luật 2 nhẹ. Hỏng với race mod (Androids, Alien Framework) và client ngôn ngữ khác |
| D3 | `CombatShock` chỉ kích hoạt khi chết, không khi downed | Lệch tên class, comment, và mô tả module. Trong RimWorld phần lớn pawn bị downed chứ không chết → tính năng hiếm khi chạy |
| D4 | Magic `×3.0` AimingDelay khi transition (`StatPart_AimStance.cs:23`) | Không có trong tài liệu, chưa ai duyệt. Kèm theo: `SetStance` tính phí transition rộng hơn thiết kế (5.2 nói "về SnapShot luôn miễn phí") |

### Đợt B — sau v1.0, mỗi cái là module riêng mặc định TẮT

| # | Việc | Phụ thuộc |
|---|---|---|
| B1 | Suppression stance (tư thế thứ 5) | ✅ A3 xong — sẵn sàng làm |
| B2 | Shotgun spread AoE | ✅ Module riêng đã tách, mặc định TẮT. Còn thiếu **B8** (cảnh báo UI). Friendly fire giờ có toggle, mặc định BẬT theo thiết kế 5.5(a) |
| B3 | Cover ảnh hưởng suppression (5.8) | **Chặn bởi ILSpy 6.8** |
| B4 | Embrasure (5.7) — ✅ A8 xong, nhận diện đã sạch | B3 |
| B5 | Suppression Pinned — code đã có, toggle TẮT | ✅ A3 xong — `FD_Suppressed` giờ sinh ra thật, Pinned test được |
| B6 | Full-auto cho Rapid — code đã có trong 2 patch class chết | B5 + ILSpy 6.1–6.4 |
| B7 | White/blacklist vũ khí | Sau v1.0, xem 4.2 |
| B8 | Cảnh báo UI tô vùng nguy hiểm shotgun | B2. Tài liệu 5.5(b) ghi **"Bắt buộc"** |

### Đã quyết định KHÔNG làm
Tách trục fire mode/aim mode · tầng điều khiển RTS (Overwatch, Attack-Move, Fireteams) · bản 1.5 · embrasure miễn nhiễm suppression.

---

## 6. Câu hỏi ILSpy — ✅ `docs/ilspy-findings.md` ĐÃ CÓ

> **Đọc `docs/ilspy-findings.md` để lấy chi tiết.** Trả lời bằng **reflection trên `Assembly-CSharp.dll`**, không dùng ILSpy — chạy được ngay, lặp lại được, bám đúng bản game đang cài.
>
> **6.8 đã mở khoá B3:** `Verse.CoverUtility.CalculateOverallBlockChance(target, shooterLoc, map)` — nhận vị trí người bắn nên **đã gộp trọng số hướng**; khói (`factorFromCoveringGas`) và ánh sáng (`offsetFromDarkness`) là **field riêng**, không lẫn vào cover. `ShotReport` còn mang sẵn `coversOverallBlockChance`, có thể khỏi gọi lại.
>
> **6.5 bất ngờ:** vanilla **đã có** `ShotReport.FactorFromPosture`. Prone đang mượn `factorFromTargetSize` — có kênh đúng để chuyển sang.
>
> **6.9 có ứng viên sửa D1:** `Pawn_PathFollower.StopDead()` — API vanilla thay cho Prefix trả `false`.
>
> **Còn 3 câu cần đọc thân hàm:** `FactorFromPosture` tính từ gì · `StopDead()` làm gì với job · cache `cachedBurstShotCount` xoá lúc nào.

Decompile `Assembly-CSharp.dll`. **Xem mod `Vanilla Fire Modes` trước — có thể trả lời sẵn 6.1–6.4.**

| # | Câu hỏi | Chặn việc gì |
|---|---|---|
| 6.1 | `Verb.ShotsPerBurst` có phải virtual property không? | B1, B6 |
| 6.2 | `TryCastNextBurstShot` đọc `ticksBetweenBurstShots` từ đâu? | B1, B6 |
| 6.3 | `AdjustedCooldownTicks` có bị mod khác patch không? | B1, B6 |
| 6.4 | `verb.burstShotsLeft` có accessible từ ShotReport context không? | B6 |
| 6.5 | Vanilla đã có `factorFromPosture` chưa? | Prone |
| 6.6 | `Verb.Available()` có được gọi đủ thường xuyên để chặn bắn không? | B5 |
| 6.7 | Vanilla xác định "shoot through" cho embrasure thế nào? | B4, A8 |
| **6.8** | **Tên thật của hàm tính cover** và giá trị trả về: float đã gộp trọng số hướng, hay phải tự tổng hợp từ 8 ô? Khói / ánh sáng / shield belt có bị gộp vào không? | **B3 — chặn toàn bộ** |
| 6.9 | `Pawn_PathFollower.StartPath` được gọi từ đâu, có đường thay thế nào an toàn hơn Prefix `false` không? | D1 |

**Về 6.8:** action I có COVER API PROBE quét mọi assembly đã nạp, liệt kê mọi type chứa "Cover", dump field/property của `ShotReport`, và tra 5 tên ứng viên. Nó **trả lời được nửa "tên hàm"** — nhưng không trả lời được ngữ nghĩa (trọng số hướng, khói/ánh sáng/shield). Phần đó vẫn phải đọc thân hàm.

⚠️ Bản probe đầu tiên tra đúng chuỗi `"RimWorld.CoverUtility"` và báo NOT FOUND — **kết quả đó không dùng được**, nó không phân biệt được "không tồn tại" với "nằm namespace khác". Đã sửa nhưng **chưa chạy lại lần nào**.

---

## 7. Định nghĩa "xong"

1. **Regression pass:** tắt hết feature → **restart game** → ma trận harness khớp **tuyệt đối** với vanilla, mọi ô. SnapShot cũng phải khớp tuyệt đối.
2. Chỉ tiêu pass/fail tương ứng ở mục 7.3 tài liệu thiết kế đã chạy và đạt.
3. Có toggle riêng trong mod settings, và **tắt toggle phải thật sự vô hiệu hoá ngay giữa phiên** (guard runtime), không chỉ bỏ đăng ký patch.
4. Không thêm transpiler mới, không thêm attribute `[HarmonyPatch]` mồ côi.
5. Test gỡ mod giữa save **với pawn đang mang hediff**. *(Giờ đã khả thi — `FD_Suppressed` sinh ra được từ gameplay. **Chưa chạy.**)*
6. Đo bằng Dubs Performance Analyzer nếu chạm `Patch_ShotReport`, `Patch_Projectile_Impact`, `Patch_Explosion`, `Verb.Available`, hoặc bất kỳ thứ gì chạy mỗi phát bắn.
7. Không để lại hằng số mới mà không đặt tên hoặc không đưa vào settings.
8. **Debug action phải gọi code sản xuất, không phải bản sao của nó.** Một action kiểm chứng tự tính lại heuristic chỉ kiểm chứng chính nó.

---

## 8. Quy tắc làm việc trong phiên

**Được tự làm:**
- Đọc, phân tích, báo cáo
- Sửa lỗi rõ ràng (null check thiếu, exception chưa bắt)
- Thêm test / debug action
- Thêm comment tiếng Anh
- Xoá code chết **đã được duyệt trong hàng đợi**

**Phải hỏi trước:**
- Thêm hoặc đổi bất kỳ hằng số cân bằng nào
- Thêm transpiler
- Refactor cấu trúc file/namespace
- Thêm dependency
- Đổi Def XML
- Bất kỳ việc gì trong Đợt B hoặc mục "Cần bàn riêng"

**Cấm:**
- Vi phạm 10 luật ở mục 2
- "Tiện tay" refactor ngoài phạm vi
- Sửa nhiều module trong một commit
- Đổi con số cân bằng mà không ghi lại giá trị cũ
- Gộp lại suppression và Shotgun AoE vào một patch (A3 đã tách chúng có lý do)

**Commit:** tiếng Anh, một module một commit, prefix: `stance:`, `suppress:`, `graze:`, `shock:`, `encumber:`, `harness:`, `infra:`.

---

## 9. Cuối mỗi phiên

1. Cập nhật mục 3 và mục 5 của file này theo những gì **thực sự** đã thay đổi trong code.
2. Nếu phát hiện lệch giữa code và tài liệu thiết kế, ghi vào một reality report mới thay vì sửa ngầm.
3. Nếu trả lời được câu hỏi ILSpy nào, ghi ngay vào `docs/ilspy-findings.md`.

Tạo `CLAUDE.md` ở root chứa mục 1, 2, 8 — Claude Code đọc tự động mỗi phiên. File này giữ phần thay đổi theo thời gian.
