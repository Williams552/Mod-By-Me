# Fire Discipline — Master Design Document

> Tài liệu thiết kế tổng hợp. Thay thế `rimworld-combat-mod-definition.md`, `project_handoff.md`, `fire-discipline-v3-execution-spec.md`.
> Backlog tầng RTS giữ ở `tactical-expansion-features.md`, tóm tắt tại mục 9.

---

## 1. Định danh & định vị

| | |
|---|---|
| **Tên** | Fire Discipline |
| **packageId** | `william.firediscipline` |
| **RimWorld** | 1.6 **only** (không làm 1.5 — nhớ set `supportedVersions` chỉ `1.6`) |
| **Ngôn ngữ** | Tiếng Anh |
| **Assembly** | `1.6/Assemblies/FireDiscipline.dll` |
| **Trạng thái** | 5 module lõi hoàn thành · chưa phát hành |

**Một câu định nghĩa:**

> Một **lớp chiến thuật** bổ sung cho combat RimWorld, tạo ra lý do để pawn di chuyển và cho người chơi quyền quyết định trong từng pha bắn — **không viết lại** hệ thống combat, không yêu cầu save mới, không đòi patch riêng cho từng mod vũ khí.

**Mô tả Workshop:**

> A tactical layer for RimWorld combat. No new save required. Works standalone, integrates automatically with Yayo's Combat 3 and Suppression.

**Triết lý stance:** một tư thế là **một vai trò + công cụ để làm vai trò đó**. Không phải một preset chỉ số. Đây là tiêu chí duy nhất để chấp nhận hay từ chối một tư thế mới.

### Cấu trúc thư mục

```text
d:\Games\Rimworld\Mod By Me\
├── About/
│   ├── About.xml
│   └── Preview.png
├── 1.6/
│   ├── Assemblies/FireDiscipline.dll
│   └── Defs/HediffDefs/Hediffs_FireDiscipline.xml
├── Source/FireDiscipline/
├── docs/
│   ├── fire-discipline-master-design.md   <-- TÀI LIỆU NÀY
│   └── tactical-expansion-features.md     <-- backlog RTS
└── README.md
```

---

## 2. Vấn đề đang giải

| # | Vấn đề vanilla | Hệ quả |
|---|---|---|
| 1 | Cover chỉ là modifier %, không có suppression | Không có lý do cơ động; đấu súng = đua DPS thuần |
| 2 | Người chơi không có hành động nào ảnh hưởng một pha bắn | Chỉ có xác suất, không có counterplay → save-scum |
| 3 | Không có hậu cần/sức mang | Vũ khí chỉ khác nhau ở DPS/tầm → luôn có "vũ khí tối ưu duy nhất" |

**Ngoài phạm vi:** AI ngu, killbox meta, tính swingy của pawn gục vì một viên may mắn (chỉ giảm nhẹ qua Graze).

**Bối cảnh:** combat vanilla được thiết kế như *story generator*, không phải game chiến thuật. Mod đi ngược lại một cách có ý thức, không phải sửa lỗi.

---

## 3. Nguyên tắc kiến trúc cứng

1. **Không thay class gốc.** Không set `verbClass`, `thingClass`, `projectile.thingClass`, `DamageWorker`. Chỉ Harmony postfix/prefix.
2. **Suy ra, đừng khai báo.** Mọi giá trị cho vũ khí/giáp/công trình của mod khác phải tính từ stat vanilla (`Mass`, `AccuracyTouch`, `CarryingCapacity`, `explosion.radius`, `fillPercent`). **Không** patch XML riêng cho từng mod.
3. **Cộng thêm bằng Hediff / Comp / StatPart.** Gỡ mod không được vỡ save.
4. **Đăng ký Harmony thủ công** qua `PatchRegistry` + `IModule`. Không `PatchAll()`.
5. **Không chạm Pathfinding / ThinkTree / JobGiver.**
6. **Không hard dependency.** Tự phối hợp qua `ModsConfig.IsActive`.
7. **Transpiler là nợ kỹ thuật.** Ưu tiên postfix.

### Hai hệ quả đã phát hiện qua thực nghiệm

- **StatPart không truy cập được khoảng cách.** `StatRequest` không mang cự ly → mọi modifier phụ thuộc khoảng cách **buộc phải** đi qua postfix `ShotReport.HitReportFor`. Hook rủi ro cao duy nhất, điểm xung đột số một với Yayo/CE. Có toggle riêng.
- **Thiết kế theo kết quả, không theo hệ số.** Độ chính xác pawn suy giảm theo hàm mũ theo khoảng cách, hệ số súng thì tuyến tính → hệ số nhân không dự đoán được kết quả. Khai báo *chỉ tiêu*, **đo ngược ra hệ số**.

---

## 4. Trạng thái hiện tại — 5 module đã hoàn thành

### 4.1 Aim Mode & Tactical Stances — `AimStanceModule.cs`

`SnapShot` (0), `Rapid` (1), `Sharpshot` (2), `Prone` (3). Tracking qua `AimStanceTracker.cs` theo `thingIDNumber`. *(Tư thế thứ 5 — Suppression — xem 5.6.)*

| Tư thế | Vai trò | Cơ chế |
|---|---|---|
| **SnapShot** | Baseline; **tư thế duy nhất không có chi phí chuyển** | Không hediff, không patch |
| **Rapid** | Dồn hoả lực cự ly gần | Warmup ratio = `clamp(0.30, 0.75, cooldown/(warmup+cooldown))`; phạt `0.93^(d−d₀)` khi `d > d₀` (`d₀ = 12` nếu `Touch ≥ Medium`, ngược lại `5`) |
| **Sharpshot** | Sát thương tầm xa, dễ vỡ | Số mũ `d × 0.80`; phạt `<5ô → ×0.70`; warmup `×1.40` |
| **Prone** | Chịu đựng hoả lực | Người bắn `×0.85` **phẳng**; mục tiêu `factorFromTargetSize ×0.65` |

**Chi phí chuyển:** ra lệnh di chuyển khi Prone → tự về SnapShot + 45 ticks `Stance_Cooldown`. Về SnapShot luôn miễn phí.
*Vì sao:* nếu đổi tư thế miễn phí, lối chơi tối ưu là swap mỗi phát bắn — tái tạo đúng micro tedium mà vấn đề #2 liệt là thứ cần diệt.

**`PassiveStanceEvaluator.cs`** gán tư thế thụ động cho raider theo cự ly + vũ khí.
*Vì sao:* raider không dùng tư thế → power creep một chiều. Đây là stat modifier theo ngữ cảnh, **không** phải thay đổi hành vi.

**`Patch_ShotReport.cs`** — postfix cấp thấp, struct boxing, `FieldInfo` **đã cache**.

### 4.2 Encumbrance — `EncumbranceModule.cs`

`StatPart_Encumbrance.cs` → `StatDefOf.MoveSpeed`. Dưới 15% tải không phạt; trên đó tuyến tính đến −35% ở 100% tải.

⚠️ **Đo theo `MassUtility.Capacity`, KHÔNG phải `StatDefOf.CarryingCapacity`** *(đổi 2026-08-07)*.

`MassUtility.Capacity` chính là con số tab Gear hiện cho người chơi — *"Mass carried: 22.65 / 35 kg"*. `CarryingCapacity` là stat khác, ~72 kg cho người trưởng thành.

Vì sao đổi:
- Người chơi **không có cách nào đọc được** tỉ lệ mà mod đang dùng, nên không tinh chỉnh loadout theo được
- Đường cong chỉ chạm phạt tối đa ở 100% tải. Với mốc 72 kg thì pawn phải mang 72 kg mới cảm nhận được −35% — **không bao giờ xảy ra**, nên module sống vĩnh viễn ở đoạn nông của chính đường cong nó định nghĩa

⚠️ **Và chỉ tính thứ pawn MANG, không tính giáp đang mặc** *(đổi 2026-08-07)*.

Vanilla **đã** tính phí di chuyển cho giáp: flak vest / pants / jacket mỗi món `−0.12 c/s`, và tooltip của pawn liệt kê từng món theo tên. Cộng khối lượng của chúng vào đây là **tính phí hai lần cho cùng một quyết định** — và lần thứ hai nặng hơn: đo trên một thiếu niên mặc flak đầy đủ, vanilla lấy 7.8%, module này lấy thêm 23%.

Tệ hơn độ lớn là **hình dạng**. Khối lượng giáp chiếm ưu thế trong tải của pawn chiến đấu — **83%** trong phép đo đó — nên mọi colonist mặc giáp đều trả xấp xỉ cùng một khoản, bất kể đang cầm gì. **Thuế phẳng không ai tránh được thì không phải một quyết định, chỉ là một trò chơi chậm hơn.**

Chỉ tính thứ pawn **mang** thì khôi phục được lựa chọn:

| Vũ khí | kg | Phạt |
|---|---|---|
| Sniper rifle | 4.0 | 0% |
| LMG | 8.5 | −3.8% |
| HMG | 12 | −7.9% |
| Charge LMG | 20 | −17.4% |
| Autocannon | 30 | −29.1% |
| Uranium slug rifle | 40 | −35% *(sàn)* |
| LMG + 2 sidearm | ~13 | −9.5% |

**Vanilla sở hữu chi phí của thứ anh MẶC. Module này sở hữu chi phí của thứ anh MANG.**

Hai thay đổi trên đi cùng nhau nhưng ngược chiều: đổi mẫu số làm phạt nặng hơn, bỏ giáp làm phạt nhẹ đi nhiều. Xạ thủ LMG đi **6.8% → 20% → 3.8%** qua hai bước.

### 4.3 Suppression Integration — `SuppressionIntegrationModule.cs`

`IsExternalSuppressionModActive()` phát hiện `Mlie.Suppression`, `suppression.mod`, `CombatExtended`. Không có → engine nội bộ (`FD_Suppressed` + `Patch_Projectile_Impact.cs`). Có → Dormant, 0% overhead.

| Tư thế | Nhận suppression | Gây suppression |
|---|---|---|
| Snap | ×1.0 | ×1.0 |
| Rapid | ×1.0 | **×1.50** |
| Sharpshot | **×2.00** + reset warmup | ×1.0 |
| Prone | **×0.50** | ×1.0 |

*Vì sao:* biến 4 tư thế từ 4 preset accuracy thành một **vòng lặp** — Rapid ghim → đồng đội vu hồi → Sharpshot dứt điểm → địch phản áp chế khiến Sharpshot sụp.

### 4.4 Graze — `GrazeModule.cs`

`Patch_DamageWorker_AddInjury.cs`. Đạn ranged vào bộ phận sống còn → 35% sát thương, bẻ `HitPart` sang chi ngoại vi, mote `Graze (-65%)`.
⚠ Điều kiện kích hoạt đổi ở 5.1.

### 4.5 Shock & Shell Shock — `ShockModule.cs`

- **Ally Downed Shock:** `Patch_Pawn_Kill_Down.cs` → `FD_CombatShock` (+30% AimingDelay) trong 6.0 ô.
- **Proportional Shell Shock:** `Patch_Explosion.cs`. Severity giảm dần từ tâm. Reset ngắm cho Sharpshot.
⚠ Bán kính và cổng lọc đổi ở 5.2.

### 4.6 Debug Harness — `DebugHarness.cs`

Xem mục 7 để biết cần bổ sung gì.

---

## 5. Thay đổi cần thực thi

### 5.0 Hạ tầng

| # | Việc | Trạng thái |
|---|---|---|
| 5.0.1 | Cache `FieldInfo` static readonly | ✅ **Đã xong** |
| 5.0.2 | Sửa cột Skill 20 trong harness | ✅ **Đã xong** |
| 5.0.3 | Ma trận DPS | ⬜ Xem 7.1 |
| 5.0.4 | Ma trận "bị bắn vào" | ⬜ Xem 7.1 |
| 5.0.5 | Test 4 vũ khí (shotgun, AR, SMG, sniper) | ⬜ Xem 7.2 |
| 5.0.6 | Throttle `PassiveStanceEvaluator` 30–60 tick | ⬜ |

### 5.1 Graze — đổi điều kiện kích hoạt

```
p = TotalEstimatedHitChance tại thời điểm bắn
grazeChance = clamp(0, 1, (0.65 − p) / 0.45)
```

`p ≥ 0.65` → không graze · `p ≤ 0.20` → luôn graze · fallback `0.5` nếu launcher chết/không phải pawn.

**Cài đặt:** trong `Patch_DamageWorker_AddInjury`, khi phát hiện đòn vital, **gọi lại** `ShotReport.HitReportFor`. **Không chuyền state qua projectile.**

**Vì sao:**
- "Hit roll vượt ngưỡng" không cài được sạch — RimWorld roll nhị phân, không lưu biên độ.
- Stance exemption cho 1 cần gạt; `p` cho **tất cả** — vì `p` đã chứa tư thế, cự ly, cover, ánh sáng, skill, suppression, shell shock. Người chơi không "bật chế độ sát thương", họ **tạo điều kiện**.
- Đối xứng — `p` tính cho cả raider.

### 5.2 Shell Shock — giới hạn

```
shockRadius = min(20, r + 2 × sqrt(r))
powerFactor = clamp(0.4, 1.0, damAmount / 50)
```

| # | Cổng lọc | Vì sao |
|---|---|---|
| a | Sàn cắt severity < **0.15** | Cứu TPS; siege nhân hediff vô nghĩa lên liên tục |
| b | Bỏ qua nếu `damAmount < 10` hoặc damType phi vật lý | `StartExplosion` bắn cho mọi vụ nổ: firefoam, smoke, EMP, extinguish. Không lọc → boomalope chết gần bếp shock đầu bếp |
| c | Pawn **không drafted**: severity ×0.3 | Siege kéo dài nhiều ngày in-game; nếu không, cả colony bị debuff liên tục |
| d | Theo LOS như vanilla rải sát thương nổ | Nếu không, mortar ngoài sân shock người đang ngủ trong phòng kín |
| e | **Refresh** severity không cộng dồn; trần 40 pawn/vụ nổ | Loạt mortar đẩy severity lên trần rất lâu; trần pawn chặn mech cluster nổ dây chuyền |

| Nguồn | `r` | Cũ (×2) | Mới |
|---|---|---|---|
| Grenade | 2.9 | 5.8 | 6.3 |
| Mortar HE | 4.9 | 9.8 | **9.3** |
| Rocket lớn | 9.0 | 18.0 | 15.0 |
| Doomsday | 13.9 | 27.8 | 20 (cap) |
| Vũ khí mod | 20–30 | **40–60** ⚠ | 20 (cap) |

Trần 20 ô ≈ nửa tầm bắn tối đa → **luôn tồn tại vị trí ngoài vùng shock**.

### 5.3 Suppression Pinned *(REVISED — mô hình vô hiệu hoá, không phải bản án tử)*

```
Pinned (severity > 0.8):
  - không bắn được            → chặn qua Verb.Available()
  - factorFromTargetSize ×0.50 → rất khó bắn trúng
  - vẫn di chuyển được         → không chạm JobGiver/ThinkTree
  - kháng tích luỹ: vừa thoát pinned → tăng ngưỡng trong ~10 giây
  - trần thời lượng
```

**Vì sao có `×0.50` — đây là phần sửa quan trọng nhất:** trong CE, pawn bị áp chế **chạy đi tìm cover và nằm rạp**, tức rời khỏi đường ngắm. Trong Fire Discipline, pawn bị pinned **đứng yên tại chỗ, phơi mình**. Nếu chỉ chặn bắn, pinned = **mục tiêu bất động miễn phí** → một khẩu LMG ghim lối tiếp cận là toàn bộ raid đứng chờ bị bắn. Đó là một killbox mới, dựng bằng chỉ số thay vì bằng kiến trúc.

`×0.50` biến pinned thành **vô hiệu hoá tạm thời**: ghim để vu hồi hoặc để rút lui, không thể ghim rồi bắn tỉa từng đứa. Đối xứng — colonist bị pinned cũng sống sót lâu hơn.

⚠ Thay đổi cân bằng lớn nhất. **Toggle riêng, mặc định TẮT ở v1.1.**

### 5.4 Full-auto — Rapid + vũ khí burst

```
Rapid + súng có burstShotCount ≥ 3:
  burstShotCount    ×2.0    (3 → 6)
  cooldown          ×1.6
  ticksBetweenBurst ×0.6

Giật nòng: phát thứ N trong loạt → accuracy ×Pow(0.93, N)
```

**Điểm móc:** cooldown → postfix `VerbProperties.AdjustedCooldownTicks(verb, pawn)` (method có sẵn pawn → chỉnh theo từng pawn/tư thế mà không đụng Def). Giật nòng → `Patch_ShotReport.cs` đã ở đúng chỗ, đọc `verb.burstShotsLeft`.

⚠ **TUYỆT ĐỐI KHÔNG** mutate `verbProps` — object cấp Def, sửa một lần là mọi khẩu cùng loại đổi theo kể cả của raider, và rò rỉ qua save.

**Vì sao:** tổng DPS không tăng, **hình dạng** đổi — dồn hoả lực 1 giây rồi phơi mình 1.5 giây. Giật nòng tự cân bằng loạt dài: đuôi loạt vô dụng ở tầm xa nhưng hiệu quả ở tầm gần, đúng bản sắc Rapid. CE có hiệu ứng này miễn phí nhờ đạn đạo thật; ta khai báo tay, mua kết quả mà không mua chi phí kiến trúc.

### 5.5 Shotgun spread AoE

⚠️ **Cập nhật 2026-08-09 (Geometry & Falloff): hình nón quét hêt tầm bắn thực tế.**
* **Giá trị CŨ (bị loại bỏ):** Splash damage đạt 100% ở mọi cự ly dọc theo trục (chỉ giảm ở 2 bên rìa). Độ dài nón (length) bị giới hạn cứng `min(8, khoảng_cách_tới_mục_tiêu)`.
* **Giá trị MỚI:** Damage giảm dần (falloff) theo cự ly dựa trên **Mật độ (Density)** do nón loe ra. Nón dài bằng 100% tầm xa thực của vũ khí (VD: 15.9 ô cho Pump Shotgun), xuyên qua mục tiêu chính để trúng kẻ địch núp sau.

**Hình dạng.** Đĩa tròn tâm điểm chạm **với tới cả phía sau nòng súng**: bắn mục tiêu gần hơn bán kính thì người bắn nằm trong vụ nổ của chính mình, và đồng đội đứng sau lưng ăn mảnh bay ngược chiều. Hiện tại đã đổi sang **hình nón (wedge) từ miệng nòng súng**.

Người bắn **luôn** được loại, không phụ thuộc toggle friendly fire. Toggle đó chỉ quyết định pawn **khác** cùng phe.

**Đã cân nhắc và loại bỏ: hình nón thật theo góc từ nòng.** Nửa góc của nó là `atan(R / cự_ly)` — 9° ở 15 ô nhưng **40° ở 3 ô**, nên bắn sát mặt còn loe rộng hơn cả đĩa nó thay thế. Hiện tại độ rộng được nội suy theo chiều dài ô tuyệt đối từ một cự ly tham chiếu cố định (8 ô) để không bị hẹp lại ở tầm gần khi súng có tầm cực xa.

⚠️ Chưa đo lại chỉ tiêu 7.3 (*"Shotgun 3 mục tiêu cụm ≤ Snap × 2.0"*) sau thay đổi này.

```
WidthEndRef = 3.0 ô (tại reference_range = 8 ô)
e = lerp(0.15, 0.55, shootingSkill / 20)
densityFactor(d) = HalfWidthAtMuzzle / widthAtDistance
dmgFactor(d, lateral) = lerp(1.0, e, lateral / halfWidth) * densityFactor(d)
primaryDamage ×0.70
```

**Nhận diện shotgun:** dùng lại `d₀` của Rapid — `AccuracyTouch ≥ AccuracyMedium`. Loại trừ `Projectile_Explosive` và `range > 25`.
**Cài đặt:** `Patch_Projectile_Impact.cs` đã làm đúng dạng tính toán này. Không cần projectile class mới.

**Vì sao:** skill điều khiển **viền**, không điều khiển chiều dài — nếu cả hai cùng scale thì thành bậc hai.

| # | Quyết định kèm | Trạng thái |
|---|---|---|
| a | Friendly fire trong vùng splash | ⏸ **Chưa quyết** — cài cả hai, mặc định BẬT, có toggle, đọc phản hồi Workshop sau v1.1 |
| b | Cảnh báo UI tô vùng nguy hiểm khi rê chuột nhắm | Bắt buộc — không có, người chơi sẽ nghĩ mod bị lỗi |
| c | Splash **không graze, không trúng bộ phận sống còn** | Nạn nhân splash không có hit roll → không có `p` |
| d | Splash gây suppression ×0.4 | Không giảm thì shotgun thành cỗ máy áp chế mạnh nhất game |

### 5.6 Suppression Stance *(tư thế thứ 5 — MỚI)*

**Vai trò: area denial / hoả lực hỗ trợ.** Không vi phạm quyết định "không tách trục" — đây là giá trị thứ 5 trên **cùng một trục**.

```
Accuracy      ×0.45
Damage        ×0.55          ← đòn bẩy chính
Aim time      ×0.50
Cooldown      ×1.50
Extra shots   burst ≤ 5:  extra = round(10 × burst / 5)
              burst > 5:  extra = round(10 × 5 / burst)
Suppression gây ra  ×2.50
Không bao giờ trúng bộ phận sống còn
```

**Vì sao khác bộ số gốc của Vanilla Fire Modes** (Accuracy 50 / Aim 50 / Cooldown 120 / Extra shots): tính thử với AR burst 3, `extra = +6` → trúng hiệu dụng `9 × 0.50 = 4.5` so với `3 × 1.0 = 3.0` = **×1.5**, chu kỳ lại ngắn hơn. **DPS thực tế tăng, không giảm** — đúng lỗi Sharpshot v1: mode không có chi phí thật. Cooldown 120% không đủ bù việc số phát tăng gấp ba. Trong Vanilla Fire Modes điều đó chấp nhận được vì mod đó không có suppression; ở đây tư thế sẽ nuốt chửng ba tư thế còn lại.

**Vì sao dùng damage thay vì accuracy làm đòn bẩy:** phạt accuracy nặng hơn khiến đạn **trượt**, mà trượt thì không gây suppression trong `Patch_Projectile_Impact` — anh sẽ vô tình làm tư thế áp chế trở nên tệ ở việc áp chế. Giảm damage giữ số viên trúng cao: nhiều đạn chạm, ít sát thương, suppression tối đa.

**Điểm gộp:** đây là chỗ ở tự nhiên của **Suppressing Area Fire** trong backlog. Bật stance + right-click xuống đất = area denial. Thu một tính năng backlog thành một phần của tư thế.

### 5.7 Embrasure Interaction *(MỚI)*

```
Đứng nấp sau embrasure (kề bên ô embrasure):  nhận suppression ×0.35
                                              accuracy khi bắn ra ×0.85
```

**Vì sao KHÔNG miễn nhiễm:** embrasure vốn đã là meta phòng thủ mạnh nhất RimWorld. Cộng miễn nhiễm suppression thì không còn lý do rời khỏi embrasure — biến mod chống killbox thành mod **tài trợ** killbox. Kháng mạnh nhưng không tuyệt đối, và có chi phí (góc bắn hẹp) là lý do có thật, dễ hiểu.

**Nhận diện không hard dependency (Trường hợp A):**
Pawn đứng ở ô đất lân cận (Adjacent 8-way) kề bên một công trình Embrasure:
```
là Building && def.passability == Impassable && def.fillPercent >= 0.65 && def.fillPercent < 1.0
```

Đó là định nghĩa vật lý của embrasure: vật cản cao nhưng đi/bắn qua được. Mọi mod embrasure đều thoả. **Ghi tiêu chí này trong mô tả Workshop** để tác giả mod khác biết cách làm cho tương thích.

### 5.8 Cover-Based Suppression Resistance *(MỚI)*

> **Lý do tồn tại của mục này nằm ở 5.10.** Đọc 5.10 trước khi tune bất kỳ con số nào ở đây — nếu không sẽ tune mù, không biết đang nhắm tới kết quả gì.

**Nguyên tắc kiến trúc số 2:** Không gán chỉ số thủ công theo từng loại cover (`sandbag = X`, `barricade = Y`). Đọc trực tiếp `coverPercent` mà Vanilla đã tính toán từ góc bắn (`CoverUtility.CalculateOverallCover`), nhân một hệ số $k = 0.40$ duy nhất:

$$\text{suppressionMult} = \text{clamp}(1.0 - (\text{coverPercent} \times k), 0.35, 1.0)$$

- **Hệ số tuning $k = 0.40$:** Cho dải giảm áp chế từ $0.70$ (Tường) đến $0.88$ (Bụi cây), phân biệt rõ ràng các mức nấp nhưng luôn đảm bảo **nấp sau cover tốt nhất (tường, x0.70) vẫn yếu hơn rõ rệt so với Nằm rạp (Prone, x0.50)**.
- **Sàn cứng $\times 0.35$:** Không bất kỳ tổ hợp nấp / giáp / hediff nào được giảm áp chế nhận vào thấp hơn 35% (tránh tình trạng miễn nhiễm áp chế).

#### Bảng Thang Bậc Cân Bằng Đầy Đủ

| Vị thế | `coverPercent` | Kháng suppression (`suppressionMult`) | Ghi chú |
|---|---|---|---|
| Trống trải, đứng | 0% | ×1.00 | Không nấp |
| Bụi cây | ~30% | ×0.88 | Cover nhẹ |
| Chunk đá | ~40% | ×0.84 | Cover tự nhiên |
| Bao cát / barricade | ~55% | ×0.78 | Cover phòng thủ |
| Tường (bắn hé) | ~75% | ×0.70 | Cover công trình tốt nhất |
| **Embrasure** | Impassable | **×0.35** | Kháng mạnh nhất — chạm sàn cứng, không xuống thấp hơn |
| **Prone, trống trải** | 0% | **×0.50** | Nằm rạp đồng trống |
| **Prone + bao cát** | ~55% | **×0.39** | Prone nấp sau bao cát |
| **Sàn cứng tối đa** | - | **×0.35** | Cáp sàn tuyệt đối |

#### Ba Thứ KHÔNG Được Tính Là Cover Chặn Áp Chế

| Yếu tố | Giảm Hit Chance? | Giảm Suppression? | Lý do kỹ thuật & Gameplay |
|---|---|---|---|
| **Khói (Smoke)** | Có | ❌ **Không** | Khói che tầm nhìn nhưng đạn vẫn bay qua đầu $\rightarrow$ Vẫn bị áp chế |
| **Bóng tối (Darkness)** | Có | ❌ **Không** | Giống khói — che tầm nhìn, không chặn đạn |
| **Shield Belt** | Không | ❌ **Không** | Chặn sát thương vật lý nhưng đạn nổ/sức nổ vẫn tạo áp lực. Giữ đúng bản chất suppression sinh ra để ngăn Shield Belt càn quét |

*Lưu ý triển khai:* Kỳ vọng là đọc `coverPercent` sẽ tự động loại bỏ Khói, Bóng tối và Shield Belt vì Vanilla tính chúng ở nhánh riêng. **⚠ ĐÂY LÀ GIẢ ĐỊNH CHƯA KIỂM CHỨNG** — nếu chúng bị gộp chung vào cùng một giá trị trả về thì toàn bộ mục 5.8 phải thiết kế lại. Xem câu hỏi ILSpy 6.8.

*Cảnh báo cân bằng:* embrasure (×0.35) hiện kháng áp chế **mạnh hơn Prone** (×0.50). Có thể biện minh về mặt vật lý (chỉ hở một khe bắn), nhưng nó đưa lối chơi cố thủ lên vị trí phòng thủ mạnh nhất ở mọi chiều. Cần xác nhận đây là chủ ý, không phải hệ quả ngoài ý muốn.

### 5.9 Định vị của 5.4, 5.5, 5.6

Ba module này **đổi cân bằng vũ khí**, khác bản chất với 5 module đầu vốn chỉ *cộng thêm một tầng*. Đó là lãnh địa Yayo Combat 3.
→ Module riêng, **mặc định TẮT**, nói rõ trong mô tả.

---

### 5.10 Tuyến phòng thủ như giải pháp thay thế Killbox *(MỚI — TRỤ CỘT THIẾT KẾ)*

> Mục này không phải một tính năng. Nó là **lý do tồn tại** của 5.7 và 5.8, và là thước đo để biết hai mục đó đã tune đúng chưa.

#### Luận điểm

Killbox không thắng nhờ an toàn. Nó thắng nhờ **số phát bắn ta có được trên mỗi kẻ địch trước khi nó bắn trả được**.

Nó đạt điều đó bằng **hình học**: ép địch vào hành lang dài, chỉ 2–3 tên bắn trả được cùng lúc trong khi cả tuyến súng của ta đều nhắm được. Một tuyến tường thường thua vì mặt trận rộng — cả 20 tên cùng bắn trả, 6 chọi 20 theo đúng nghĩa đen.

**Suppression cung cấp cùng một đại lượng bằng con đường khác:**

| | Mua thời gian bằng cách |
|---|---|
| Killbox | **Kéo dài đường đi** của địch |
| Tuyến phòng thủ + suppression | **Làm địch đi chậm lại** |

Cùng một phép toán — *số giây địch phơi mình dưới hoả lực* — khác nhau ở cách đạt được. Đó là toàn bộ lập luận, và nó quyết định trọng tâm của suppression phải đặt ở đâu.

⚠️ **Hệ quả trực tiếp:** hiệu ứng chính của suppression lên bên tấn công phải là **tốc độ di chuyển**, không phải độ chính xác. Bản Def hiện tại đặt trọng tâm ngược lại.

#### Vì sao suppression thô làm phe thủ thiệt

Hai bất đối xứng, cả hai đều nghiêng về bên tấn công:

1. **Suppression thưởng cho số đông.** Nó tích luỹ theo số viên đạn rơi gần. Raid 20 tay súng ghim 6 colonist nhanh hơn nhiều so với chiều ngược lại. Suppression về bản chất là **vũ khí của bên đông quân** — mà trong RimWorld bên đông quân luôn là raider.
2. **Nó phạt bên phải HÀNH ĐỘNG, tha bên chỉ cần CHỊU ĐỰNG.** Việc của phe thủ là bắn liên tục từ một vị trí; bị ghim là mất tất cả. Việc của phe công là băng qua khoảng trống; bị ghim chỉ là chậm hơn, mà họ vốn đã chậm.

**Và nghịch lý:** suppression thô **củng cố killbox**. Trong killbox raider chết trước khi kịp bắn, nên colonist gần như không bao giờ bị suppress — người chơi hưởng lợi một chiều. Ở tuyến tường thì ngược lại. Suppression thô **chỉ phạt đúng lối chơi ta muốn khuyến khích.**

#### Ba cơ chế, theo thứ tự bắt buộc

**Cơ chế 1 — Chặn tốc độ tích suppression mỗi nạn nhân *(ĐIỀU KIỆN CẦN, chưa tồn tại ở đâu)***

```
takenThisSecond   tích luỹ severity pawn đã nhận trong 1 giây gần nhất
remaining         = max(0, intakeSoftCap − takenThisSecond)
applied           = raw × (remaining / intakeSoftCap)

intakeSoftCap = 0.60 severity/giây      ⚠ giả thuyết
```

Viên đầu tiên cộng đầy đủ; khi đã chạm trần trong giây đó, viên tiếp theo cộng gần bằng 0. Một khẩu LMG đủ bão hoà; tay súng thứ hai, thứ ba, thứ mười đóng góp rất ít.

Đây là cơ chế **duy nhất trực tiếp huỷ lợi thế quân số**. Không có nó, 20 raider ghim cứng tuyến trong vài giây và hai cơ chế còn lại vô nghĩa.

**Cơ chế 2 — Cover giảm suppression nhận vào, có hướng *(chính là 5.8)***

Điểm mấu chốt về cân bằng: cần gạt này trả công **bằng 0** cho người chơi killbox, vì trong killbox colonist không bị bắn. Nó **chỉ** thưởng cho lối chơi phải đứng chịu hoả lực.

Và vì `CoverUtility.CalculateOverallBlockChance` nhận `shooterLoc`, **vu hồi trở thành cách phá tuyến có cơ sở cơ học** — địch phải trả bằng thời gian và phơi mình để vô hiệu hoá cover.

**Cơ chế 3 — Suppression làm địch chậm, dùng NHÂN chứ không CỘNG**

```
wavering   MoveSpeed ×0.80
ducking    MoveSpeed ×0.50
cowering   MoveSpeed ×0.30      ⚠ đều là giả thuyết
```

⚠️ **Phải là hệ số nhân, không phải statOffset tuyệt đối.** Offset tính bằng ô/giây sẽ vỡ với race mod có tốc độ nền khác. Nhân thì đúng với mọi race — cùng tinh thần luật kiến trúc số 2.

Cài qua `StatPart` trên `MoveSpeed` (đã có sẵn `StatPart_Encumbrance` ở đó), không qua `statOffsets` trong Def.

#### Đối trọng — ba đường phá pháo đài

Một tuyến phòng thủ mạnh mà không có điểm yếu thì chỉ là killbox mang tên khác. Ba đường phá dưới đây phải cùng tồn tại, và **cơ chế 1 (chặn tốc độ tích) là thứ ngăn "đông quân" trở thành đường thứ tư** — vì nếu số đông giải quyết được mọi thứ thì ba đường này không ai dùng.

**Đường 1 — Vu hồi.** Đã có sẵn về mặt cơ học: `CalculateOverallBlockChance` nhận vị trí người bắn, nên đổi góc là cover của mục tiêu tụt. Không cần code thêm. Cái giá địch phải trả là thời gian và phơi mình.

**Đường 2 — Chất nổ.** Xem tiểu mục riêng bên dưới. Vanilla đã cung cấp sẵn, chỉ cần không vô hiệu hoá nó.

**Đường 3 — Xung phong có khiên** *(cơ chế mới, đang bàn)*

Dưới §5.10, hiệu ứng chính của suppression là **làm chậm**. Shield belt vanilla chặn **sát thương** đạn nhưng không chặn suppression. Hệ quả nếu để nguyên:

> Lính cận chiến đeo khiên **không thể bị bắn chết**, nhưng vẫn **bò lê** qua bãi trống — chết vì hết giờ, không phải vì trúng đạn.

Tức pháo đài MG **không còn đối trọng cận chiến nào**. Vì vậy khiên phải kháng suppression.

⚠️ **Nhưng không phải miễn nhiễm phẳng.** Kháng theo **năng lượng khiên còn lại**:

```
shieldPct = comp.Energy / comp.EnergyMax
mult      = lerp(1.0, shieldSuppressionFloor, shieldPct)

shieldSuppressionFloor = 0.15      ⚠ giả thuyết
```

| | Miễn nhiễm phẳng | Theo năng lượng |
|---|---|---|
| Trạng thái | Nhị phân, vô hình | **Đọc được** — thanh khiên là chỉ báo có sẵn |
| Cửa sổ thời gian | Không có | Khiên vỡ → bị ghim ngay, đúng lúc đang ở giữa bãi trống |
| Tự giới hạn | Không | Có — năng lượng cạn theo đạn hứng |

⚠️ **Chỉ ĐỌC năng lượng, không trừ.** Trừ năng lượng vì suppression là đổi hành vi vanilla — người chơi sẽ thấy khiên vỡ nhanh bất thường mà không hiểu vì sao.

**API (đã xác minh bằng reflection, 1.6):**

```
RimWorld.CompShield : ThingComp
    Single       Energy
    Single       EnergyMax
    ShieldState  ShieldState
    Boolean      IsApparel
    Boolean      IsBuiltIn
    Pawn         PawnOwner
```

⚠️ **Class `ShieldBelt` KHÔNG còn tồn tại trong 1.6.** Cơ chế khiên giờ là một `ThingComp`. Nhận diện bằng `TryGetComp<CompShield>()` — suy ra từ **hành vi**, không phải từ defName, nên đúng luật 2 và tự động bắt được khiên của mod khác nếu chúng dùng cùng comp. Cờ `IsBuiltIn` còn cho biết đây là khiên mech hay khiên mặc.

**Bất đối xứng phải nói thẳng:** người chơi **chủ động** trang bị một tổ khiên để xung phong; raider chỉ **ngẫu nhiên** có khiên tuỳ thành phần raid, và AI không dùng nó theo chiến thuật. Nên cần gạt này hoạt động tốt như **công cụ tấn công cho người chơi**, kém hơn như **điểm yếu của pháo đài người chơi**. Nó chỉ giải một nửa bài toán; nửa còn lại là chất nổ.

#### Chất nổ đối với pháo đài — trạng thái hiện tại

Hành vi **hiện đang chạy trong code**, chưa phải đề xuất:

| Nguồn | `flyOverhead` | Gây `FD_Suppressed`? | Gây `FD_ShellShock`? |
|---|---|---|---|
| Đạn thường | không | ✅ | ❌ |
| **Lựu đạn** | **không** | ✅ | ✅ **cả hai** |
| **Đạn cối** | **có** | ❌ *(prefix thoát sớm)* | ✅ |

⚠️ **Lựu đạn đang tính hai lần.** Nó không `flyOverhead` nên đi qua prefix suppression, rồi vụ nổ lại kích `Patch_Explosion`. Nạn nhân nhận **hai hediff khác thang đo**, cả hai đều cộng phạt `AimingDelayFactor` và `MoveSpeed`. Chưa quyết đây là chủ ý hay lỗi.

⚠️ **`FD_ShellShock` chưa được viết lại.** `FD_Suppressed` đã chuyển sang thang 0–9, 5 stage, suy giảm dần. `FD_ShellShock` **vẫn** ở thang 0–1, 2 stage, và **vẫn dùng `HediffComp_Disappears` không refresh** — đúng lỗi đã sửa cho suppression. Hai hệ thống song song đang trôi xa nhau.

**Đánh giá làm đối trọng:**

| | Khả thi? | Vì sao |
|---|---|---|
| **Cối** | ✅ **Mạnh nhất** | Tầm 500 ô — **pháo đài không thể áp chế thứ nó không với tới**. Nổ bỏ qua cover. Vanilla đã cho raider dùng qua siege |
| **Lựu đạn** | ⚠️ **Yếu dần theo §5.10** | Tầm ném ~13 ô → phải lọt vào vùng áp chế mới ném được. Suppression làm chậm ⇒ MG chặn được grenadier |

⚠️ **Căng thẳng thiết kế cần theo dõi:** §5.10 làm tuyến MG thắng bộ binh xung phong *và* thắng grenadier, đẩy raider về phía **siege bằng cối** — mà nhiều người chơi thấy siege tẻ nhạt. Nếu playtest cho thấy mọi raid đều biến thành siege thì cơ chế 3 (làm chậm) đang quá mạnh.

Ghi chú: `Patch_Explosion` có cổng lọc **LOS** từ tâm nổ tới nạn nhân. Đạn cối rơi **ngoài tường** sẽ không gây shell shock cho người bên trong — chỉ quả rơi lọt vào trong mới có tác dụng. Đó là hành vi hợp lý, nhưng nó nghĩa là **độ chính xác của cối quyết định giá trị của nó như đối trọng**, và cối vanilla thì rất kém chính xác.

#### Điều kiện thắng — đo được, không cảm tính

Tuyến phòng thủ **không cần thắng killbox**. Nó cần **sống được**. Đó là ngưỡng dễ đạt hơn nhiều, và thú vị hơn vì có quyết định để ra.

| Kịch bản | Kết quả bắt buộc |
|---|---|
| 6 colonist sau bao cát + lỗ châu mai, 20 raider tấn công trực diện | **Thắng: 0 chết, 0–2 bị hạ** |
| Cùng 6 colonist nhưng đứng ngoài trống | **Thua** |
| Địch vu hồi được sườn | **Tuyến vỡ** — buộc phải rút về tuyến hai |
| Cùng đội hình trong killbox | Vẫn thắng dễ hơn — **chấp nhận được** |

Dòng 2 và 3 quan trọng ngang dòng 1: nếu chơi ẩu vẫn thắng thì cơ chế không tạo ra quyết định nào.

#### Ba chỗ có thể hỏng

| Hỏng | Dấu hiệu |
|---|---|
| Quá mạnh | Tuyến thành killbox không thương vong → chán, và đẩy sang spam embrasure |
| Quá yếu | Người chơi quay về killbox — không có gì thay đổi |
| Sai trọng tâm | Suppression giảm accuracy nhiều, giảm tốc độ ít → không mua được thời gian, luận điểm sụp |

#### Phạm vi — nói rõ để không trượt

Cả ba cơ chế đều nằm ở tầng **stat / hediff**. Không chạm AI, không chạm pathfinding, không thêm tư thế, không thêm nội dung. Vẫn đúng định vị "lớp mỏng cộng thêm".

Thứ mở rộng ở đây là **tham vọng cân bằng**, không phải kiến trúc.

⚠️ **Giới hạn đã biết:** AI raider của RimWorld không biết xung phong theo đợt (tiến — nằm — tiến). Fire Discipline không thể dạy nó mà không phạm luật 5. Cảm giác "địch bị ghim" phải đến hoàn toàn từ **tốc độ di chuyển tụt xuống**, không từ hành vi. Từ ghế người chơi điều đó là đủ; nhưng đừng hứa hẹn thứ AI không làm được.

#### Trạng thái

**Chưa có gì được implement.** Cơ chế 1 chưa tồn tại ở bất kỳ đâu. Cơ chế 2 là 5.8 — ILSpy 6.8 đã trả lời, còn chờ số đo `BaseBlockChance` thật. Cơ chế 3 cần viết lại bảng stage của `FD_Suppressed` và chuyển từ offset sang nhân.

Đường 3 (kháng theo khiên) chưa tồn tại. Đường 1 và 2 đã có sẵn từ vanilla.

Hai món nợ phát hiện khi rà chất nổ, **chưa sửa**: lựu đạn tính hai lần, và `FD_ShellShock` chưa được viết lại theo chuẩn mới của `FD_Suppressed`.

---

## 6. Xác minh bằng ILSpy

RimWorld **không có API docs chính thức** — decompile `Assembly-CSharp.dll` là nguồn sự thật duy nhất.
*Xem `Vanilla Fire Modes` trước — có thể trả lời sẵn 6.1–6.4.*

| # | Cần biết | Ảnh hưởng |
|---|---|---|
| 6.1 | `Verb.ShotsPerBurst` có phải virtual property không | 5.4, 5.6 — nếu không, phải thiết kế lại |
| 6.2 | `TryCastNextBurstShot` đọc `ticksBetweenBurstShots` từ đâu | 5.4, 5.6 |
| 6.3 | `AdjustedCooldownTicks` có bị mod khác patch không | 5.4, 5.6 |
| 6.4 | `verb.burstShotsLeft` accessible từ ShotReport context không | 5.4 — cơ chế giật nòng |
| 6.5 | Vanilla đã có `factorFromPosture` chưa | Prone — nếu có thì dùng lại |
| 6.6 | `Verb.Available()` có được gọi đủ thường xuyên để chặn bắn | 5.3 — cơ chế pinned |
| 6.7 | Cách vanilla xác định "shoot through" cho embrasure | 5.7 — có thể có tiêu chí tốt hơn `fillPercent` |
| **6.8** | **Tên thật của hàm tính cover** và giá trị nó trả về: một `float` đã gộp sẵn trọng số hướng, hay phải tự tổng hợp từ danh sách ô? Khói / ánh sáng / shield belt có bị gộp vào giá trị đó không? | **5.8 — chặn toàn bộ.** Tên `CoverUtility.CalculateOverallCover` trong 5.8 **chưa được xác minh**; ứng viên khác: `CalculateOverallBlockChance`. Nếu phải tự tổng hợp từ 8 ô thì phần cache trở thành bắt buộc |

---

## 7. KIỂM CHỨNG & TEST TRƯỚC KHI HOÀN THÀNH

> Nguyên tắc: **không con số nào trong tài liệu này là giá trị chốt.** Tất cả là giả thuyết cho tới khi qua bảng dưới.

### 7.1 Debug action cần bổ sung vào `DebugHarness.cs`

| # | Action | Đo cái gì |
|---|---|---|
| A | `Print DPS Matrix` | `burstCount × damage × hitChance / (warmup + cooldown)` — 4 cự ly × 4 skill × 5 tư thế |
| B | `Print Incoming-Fire Matrix` | Pawn địch bắn **vào** pawn test ở từng tư thế — validate `factorFromTargetSize` của Prone và Pinned |
| C | `Print Graze Distribution` | Với dải `p` từ 0.05→0.95, in `grazeChance` thực tế + 1000 lần roll để kiểm phân phối |
| D | `Simulate Explosion Table` | In `shockRadius`, số pawn bị ảnh hưởng, severity min/max cho grenade / mortar / doomsday / vũ khí mod giả lập `r=25` |
| E | `Print Weapon Classification` | Với mọi `ThingDef` vũ khí ranged đang load: `d₀`, có phải shotgun-like không, `burstShotCount`, `extra shots` ở Suppression stance |
| F | `Test Pinned Cycle` | Đẩy severity lên 1.0, log thời điểm pinned bật/tắt, kháng tích luỹ, và hit chance vào pawn khi pinned |
| G | `Test Embrasure Detection` | Quét bản đồ, in mọi thing thoả tiêu chí **5.7**: `passability == Impassable && fillPercent >= 0.65 && < 1.0`, kèm mọi ô lân cận 8-way đủ điều kiện hưởng kháng |
| H | `Print Shotgun Spread Damage` | Tổng sát thương lên 1 / 2 / 3 mục tiêu ở các cự ly, so với Snap baseline |
| I | `Print Cover Values` | Quét mọi `ThingDef` có thể làm cover trong modlist đang load: `fillPercent`, `passability`, `coverPercent` thật, và `suppressionMult` tương ứng. **Các con số 30/40/55/75% trong bảng 5.8 hiện là ước lượng, chưa đọc từ Def** |

**E là action giá trị nhất** — nó kiểm chứng nguyên tắc kiến trúc số 2 trên toàn bộ modlist thật, không phải trên giả định.

### 7.2 Ma trận bắt buộc chạy

Mỗi lần tune xong, chạy đủ tổ hợp:

- **Vũ khí:** shotgun · assault rifle · SMG · sniper rifle · bolt-action · LMG *(tối thiểu; thêm 2–3 vũ khí từ mod khác để test nguyên tắc 2)*
- **Skill:** 4 · 10 · 16 · 20
- **Cự ly:** Touch 3 · Short 12 · Medium 25 · Long 40
- **Tư thế:** cả 5
- **Cover:** 0% · 50% · 75%
- **Mục tiêu:** đứng yên · di chuyển

Bolt-action một mình là ca biên — không đủ để chứng minh công thức `d₀`.

### 7.3 Chỉ tiêu pass/fail

**Regression — chạy trước mọi thứ khác:**

| Kiểm tra | Pass khi |
|---|---|
| Mod tắt hết feature | Ma trận trùng **khớp tuyệt đối** với vanilla, mọi ô |
| SnapShot | Trùng khớp tuyệt đối với vanilla |

*Nếu SnapShot lệch dù một chữ số, có patch đang chạy khi không nên chạy.*

**Stances:**

| Module | Chỉ tiêu | Ngưỡng |
|---|---|---|
| Rapid | DPS ở Touch | ≥ Snap × 1.30 |
| Rapid | DPS ở Medium | ≤ Snap × 0.70 |
| Rapid | Đường phạt theo cự ly | Xác nhận `0.93^(d−d₀)`, `d₀` đúng cho từng vũ khí |
| Sharpshot | Hit chance ở Long, skill 10 | **+8 đến +12 điểm phần trăm** so với Snap |
| Sharpshot | Hit chance ở Long, skill 16 | **≤ +8pp** — để xạ thủ giỏi không kéo dài khoảng cách |
| Sharpshot | DPS ở Touch | ≤ Snap × 0.65 |
| Prone | Xác suất **bị trúng** khi có cover | ≤ Snap × 0.55 |
| Prone | DPS khi bắn ra | ≤ Snap × 0.85 |
| Suppression | DPS tổng | ≤ Snap × 0.85 |
| Suppression | Suppression output/giây | ≥ Snap × 2.50 |
| Suppression | Số đòn vào bộ phận sống còn | **0** |

*Chỉ tiêu Sharpshot dùng mức tăng tương đối, không dùng con số tuyệt đối — vì bảng harness cho thấy skill 16 đã tự đạt 44.6% ở Long mà không cần Sharpshot.*

**Graze:**

| Kiểm tra | Pass khi |
|---|---|
| `p ≥ 0.65` | grazeChance = **0%** tuyệt đối |
| `p ≤ 0.20` | grazeChance = **100%** |
| Phân phối 1000 roll | Lệch ≤ 3% so với công thức |
| Launcher đã chết / không phải pawn | Fallback 0.5, **không throw** |
| Splash từ shotgun | Không bao giờ vào graze path |

**Shell Shock:**

| Kiểm tra | Pass khi |
|---|---|
| Bán kính | Khớp bảng 5.2 cho cả 5 nguồn nổ |
| Vũ khí mod `r = 25` | shockRadius = **20**, không hơn |
| Boomalope / firefoam / smoke / EMP | **0 hediff** được gán |
| Colonist không drafted trong siege 3 ngày | Gần như không pawn nào chạm ngưỡng 0.15 |
| Mortar rơi ngoài, pawn ngủ trong phòng kín | **0 hediff** |
| Loạt 6 quả liên tiếp | Severity **refresh**, không cộng dồn vượt trần |
| Raid 80 pawn + nổ dây chuyền | ≤ 40 pawn xử lý mỗi vụ nổ |

**Pinned (5.3):**

| Kiểm tra | Pass khi |
|---|---|
| Hit chance vào pawn pinned | ≈ Snap × 0.50 |
| Kháng tích luỹ | Pawn không thể bị pinned liên tục quá 2 chu kỳ |
| Colonist bị pinned | Vẫn di chuyển được, vẫn nhận lệnh di chuyển |
| Save/load khi đang pinned | Trạng thái khôi phục đúng, không kẹt vĩnh viễn |
| **Test cảm giác** | Chơi 3 raid liên tiếp — nếu thấy bực chứ không thấy căng, chỉ tiêu sai |

**Full-auto & Shotgun:**

| Kiểm tra | Pass khi |
|---|---|
| Full-auto tổng DPS | Trong khoảng Snap × 0.95–1.15 *(hình dạng đổi, không phải sức mạnh)* |
| Giật nòng | Phát cuối loạt có hit chance thấp hơn phát đầu, đúng `0.93^N` |
| `verbProps` | Không object Def nào bị mutate — kiểm bằng cách đọc lại stat sau 10 phút chơi |
| Shotgun 1 mục tiêu | ≤ Snap × 1.0 |
| Shotgun 3 mục tiêu cụm | ≤ Snap × 2.0 |
| Shotgun vs vũ khí mod | `d₀` phân loại đúng — kiểm bằng action E |

**Embrasure & Cover (5.7 & 5.8):**

| Kiểm tra | Pass khi |
|---|---|
| Bắn chính diện vào pawn sau bao cát | suppression ≈ ×0.83 |
| Bắn từ sườn vào cùng pawn đó | suppression ≈ ×1.00 |
| Pawn nấp sau embrasure | suppression ×0.35 (chạm sàn cứng), accuracy bắn ra ×0.85 |
| Prone + bao cát | ≥ ×0.35, không thấp hơn sàn |
| Nhận diện embrasure | Đúng trên **tối thiểu 3 mod embrasure khác nhau** |
| False positive | Không nhận nhầm cửa, hàng rào, bàn ghế, bao cát |
| Tổng chi phí cover-calc | 15 viên/loạt × 8 pawn: **Không tăng quá 5% frame time** |

### 7.4 Test tương thích

| Kiểm tra | Cách làm |
|---|---|
| Modlist đầy đủ mục 10 | Chơi tối thiểu 1 mùa, không lỗi đỏ |
| **Gỡ mod giữa save** | Save **khi có pawn đang mang `FD_Suppressed` / `FD_CombatShock` / `FD_ShellShock`**, rồi gỡ mod và load. *Test cũ chỉ chạy lúc chiến trường sạch — chưa đủ.* |
| Yayo Combat 3 bật/tắt | Ma trận không đổi bất thường khi bật Yayo |
| Suppression (Continued) bật | Module nội bộ **tắt hoàn toàn**, không double-suppress |
| Từng toggle tắt riêng | Patch tương ứng không được đăng ký — kiểm bằng Harmony debug log |
| Vũ khí từ mod ngoài | Action E trên modlist có ≥ 3 mod vũ khí |

### 7.5 Test hiệu năng

Dùng **Dubs Performance Analyzer**. Đo trước và sau mỗi module.

| Điểm nóng | Ghi chú |
|---|---|
| `Patch_ShotReport.HitReportFor` | Gọi **mỗi phát bắn**, và mỗi phát trong loạt. Full-auto + Suppression stance có thể nhân 3–4 lần số lần gọi |
| `Patch_Projectile_Impact` | Nay phục vụ 3 việc: suppression, shotgun splash, area fire |
| `Patch_Explosion` | Kiểm với mech cluster nổ dây chuyền |
| `PassiveStanceEvaluator` | Sau khi throttle 30–60 tick |
| `Patch_DamageWorker_AddInjury` | Nay gọi lại `HitReportFor` cho mỗi đòn vital |

**Kịch bản benchmark:** raid 80 pawn + 3 mortar + mech cluster, ghi TPS trước/sau. Đây là ca xấu nhất thực tế.

### 7.6 Edge case phải thử

- Pawn không vũ khí · chỉ vũ khí cận chiến · vũ khí không có `burstShotCount`
- **Turret** (turret có verb — kiểm `PassiveStanceEvaluator` và stance không áp nhầm)
- Mechanoid · động vật · pawn hoang dã
- Projectile không có launcher (bẫy, nổ môi trường)
- Launcher chết giữa lúc đạn đang bay
- Colonist bị pinned trong lúc đang mental break
- Pawn trong drop pod / caravan / world map
- Vũ khí có `burstShotCount` cực lớn (một số mod đặt 20+) — kiểm công thức extra shots ở 5.6
- Vụ nổ có `radius = 0` hoặc âm

---

## 8. Quyết định đã chốt

| # | Vấn đề | Quyết định |
|---|---|---|
| 1 | Tách trục fire mode / aim mode | ❌ **Không tách.** Quá phức tạp, không theo ý đồ. Stance = vai trò + công cụ |
| 2 | Sang tầng điều khiển RTS | ❌ Hiện tại không |
| 3 | Ngôn ngữ | Tiếng Anh |
| 4 | Bản 1.5 | ❌ Không |
| 5 | Module đổi cân bằng vũ khí | Mặc định **TẮT** |
| 6 | Suppressing Area Fire chống lạm dụng | Trần severity **~0.4** |
| 7 | Embrasure miễn nhiễm suppression | ❌ Không — kháng ×0.35 (chạm sàn cứng) thay thế |
| 8 | Overwatch Zone | ⏸ Hoãn |
| 9 | Prone tự động hay thủ công | ⏸ Hoãn — giữ thủ công |
| 10 | License / GitHub / About.xml / Preview | ⏸ Hoãn |
| 11 | Shotgun friendly fire | ✅ Ship cả hai, **toggle, mặc định BẬT** — xem 5.5a |
| 12 | Turret không bị suppress | ✅ **Kệ.** Turret là lựa chọn của người chơi; mod cung cấp công cụ cho người cần, không ép ai bỏ turret |
| 13 | Cổng gate suppression theo mod ngoài | ✅ **Bỏ cổng.** Engine luôn có mặt, người chơi tự bật/tắt; dò mod ngoài chỉ đặt mặc định lần đầu + cảnh báo |
| 14 | Giảm phương sai sát thương (hạn ngạch trúng) | ⏸ **Hoãn sau v1.0.** Đã đo: `pity` không bảo toàn kỳ vọng, `quota` phải chặn vòng roll của vanilla |

### 8.1 Bảng hằng số cân bằng XML Defs (Task A9)

> **Lịch sử sửa đổi (2026-08-08/09):**
> - Chuyển trục chính của suppression từ "phạt thời gian ngắm" sang "chặn di chuyển".
> - Giá trị cũ đã bị thay thế / xoá:
>   - Pinned settings (xoá bỏ khỏi `FireDisciplineSettings`): `enableSuppressionPinned` (false), `pinnedSeverityThreshold` (0.80).
>   - `FD_Suppressed` Stage 1 (shaken): `<AimingDelayFactor>0.10</AimingDelayFactor>` (xoá hẳn)
>   - `FD_Suppressed` Stage 2 (wavering): `<AimingDelayFactor>0.25</AimingDelayFactor>` (giảm còn 0.05), `<MoveSpeed>-0.15</MoveSpeed>` (xoá)
>   - `FD_Suppressed` Stage 3 (ducking): `<AimingDelayFactor>0.45</AimingDelayFactor>` (giảm còn 0.10), `<MoveSpeed>-0.35</MoveSpeed>` (xoá)
>   - `FD_Suppressed` Stage 4 (cowering): `<AimingDelayFactor>0.80</AimingDelayFactor>` (giảm còn 0.20), `<MoveSpeed>-0.55</MoveSpeed>` (xoá)
> - **Cân bằng lại decay (2026-08-09):** `suppressionDecayPerSecond` (0.20 -> 0.10), `suppressionDecayDelayTicks` (60 -> 120 / 2 giây).
> - **Chuyển cờ nhận diện Embrasure (Task B4, 2026-08-09):** Bỏ `embrasureMinFillPercent` (giá trị cũ 0.65f), dùng cờ Def `disableImpassableShotOverConfigError`.

Danh sách các hằng số cân bằng trong `1.6/Defs/HediffDefs/Hediffs_FireDiscipline.xml`, ý nghĩa và cơ chế nạp trong code:

| DefName | Element / Parameter | Giá trị XML | Ý nghĩa / Tác dụng | Code / Vanilla đọc |
|---|---|---|---|---|
| `FD_Suppressed` | `defaultLabelColor.r` | 0.9 | Mã màu R hiển thị Hediff label | Vanilla `HediffUIUtility` / Health Tab |
| `FD_Suppressed` | `defaultLabelColor.g` | 0.45 | Mã màu G hiển thị Hediff label | Vanilla `HediffUIUtility` / Health Tab |
| `FD_Suppressed` | `defaultLabelColor.b` | 0.2 | Mã màu B hiển thị Hediff label | Vanilla `HediffUIUtility` / Health Tab |
| `FD_Suppressed` | `minSeverity` | 0 | Sàn severity tối thiểu (phải = 0 để HediffComp_SuppressionDecay xoá hediff khi hết áp chế) | `SuppressionEngine.MinSeverity(def)` |
| `FD_Suppressed` | `maxSeverity` | 9.0 | Trần severity tối đa | `SuppressionEngine.MaxSeverity(def)` |
| `FD_Suppressed` | `severityPerSecond` | 0.10 | Tốc độ suy giảm severity mỗi giây | `HediffComp_SuppressionDecay` |
| `FD_Suppressed` | `delayTicks` | 120 | Ticks chờ trước khi bắt đầu suy giảm (2s) | `HediffComp_SuppressionDecay` |
| `FD_Suppressed` | `severityIndices.min` | 3 | Stage min hiện Effecter icon (ducking) | Vanilla `HediffComp_Effecter` |
| `FD_Suppressed` | `severityIndices.max` | 5 | Stage max hiện Effecter icon (cowering) | Vanilla `HediffComp_Effecter` |
| `FD_Suppressed` | Stage 1 (shaken) `minSeverity` | 0.5 | Ngưỡng kích hoạt stage shaken (ẩn) | Vanilla `Hediff.CurStage` |
| `FD_Suppressed` | Stage 2 (wavering) `minSeverity` | 1.0 | Ngưỡng kích hoạt stage wavering | Vanilla `Hediff.CurStage` |
| `FD_Suppressed` | Stage 2 `AimingDelayFactor` | +0.05 | Phạt thời gian ngắm (+5%) | Vanilla `StatWorker` |
| `FD_Suppressed` | Stage 3 (ducking) `minSeverity` | 2.0 | Ngưỡng kích hoạt stage ducking | Vanilla `Hediff.CurStage` |
| `FD_Suppressed` | Stage 3 `AimingDelayFactor` | +0.10 | Phạt thời gian ngắm (+10%) | Vanilla `StatWorker` |
| `FD_Suppressed` | Stage 3 `ShootingAccuracyPawn` | -0.10 | Phạt độ chính xác bắn (-0.10) | Vanilla `StatWorker` |
| `FD_Suppressed` | Stage 4 (cowering) `minSeverity` | 5.5 | Ngưỡng kích hoạt stage cowering | Vanilla `Hediff.CurStage` |
| `FD_Suppressed` | Stage 4 `AimingDelayFactor` | +0.20 | Phạt thời gian ngắm (+20%) | Vanilla `StatWorker` |
| `FD_Suppressed` | Stage 4 `ShootingAccuracyPawn` | -0.20 | Phạt độ chính xác bắn (-0.20) | Vanilla `StatWorker` |
| `FD_CombatShock` | `defaultLabelColor.r` | 0.85 | Mã màu R hiển thị Hediff label | Vanilla `HediffUIUtility` / Health Tab |
| `FD_CombatShock` | `defaultLabelColor.g` | 0.35 | Mã màu G hiển thị Hediff label | Vanilla `HediffUIUtility` / Health Tab |
| `FD_CombatShock` | `defaultLabelColor.b` | 0.35 | Mã màu B hiển thị Hediff label | Vanilla `HediffUIUtility` / Health Tab |
| `FD_CombatShock` | `initialSeverity` | 0.35 | Severity ban đầu khi tạo Hediff | Vanilla `HediffMaker.MakeHediff` |
| `FD_CombatShock` | `maxSeverity` | 1.0 | Ngưỡng severity tối đa | Vanilla `Hediff.Severity` clamp |
| `FD_CombatShock` | `disappearsAfterTicks.min` | 240 | Thời gian tồn tại tối thiểu (4 giây) | Vanilla `HediffComp_Disappears` |
| `FD_CombatShock` | `disappearsAfterTicks.max` | 420 | Thời gian tồn tại tối đa (7 giây) | Vanilla `HediffComp_Disappears` |
| `FD_CombatShock` | Stage 0 (shaken) `minSeverity` | 0.1 | Ngưỡng kích hoạt stage shaken | Vanilla `Hediff.CurStage` |
| `FD_CombatShock` | Stage 0 `AimingDelayFactor` | +0.30 | Phạt thời gian ngắm (+30%) | Vanilla `StatWorker` |
| `FD_ShellShock` | `defaultLabelColor.r` | 0.95 | Mã màu R hiển thị Hediff label | Vanilla `HediffUIUtility` / Health Tab |
| `FD_ShellShock` | `defaultLabelColor.g` | 0.3 | Mã màu G hiển thị Hediff label | Vanilla `HediffUIUtility` / Health Tab |
| `FD_ShellShock` | `defaultLabelColor.b` | 0.1 | Mã màu B hiển thị Hediff label | Vanilla `HediffUIUtility` / Health Tab |
| `FD_ShellShock` | `initialSeverity` | 0.50 | Severity ban đầu khi tạo Hediff | Vanilla `HediffMaker.MakeHediff` |
| `FD_ShellShock` | `maxSeverity` | 1.0 | Ngưỡng severity tối đa | Vanilla `Hediff.Severity` clamp |
| `FD_ShellShock` | `disappearsAfterTicks.min` | 300 | Thời gian tồn tại tối thiểu (5 giây) | Vanilla `HediffComp_Disappears` |
| `FD_ShellShock` | `disappearsAfterTicks.max` | 600 | Thời gian tồn tại tối đa (10 giây) | Vanilla `HediffComp_Disappears` |
| `FD_ShellShock` | Stage 0 (concussed) `minSeverity` | 0.1 | Ngưỡng kích hoạt stage concussed | Vanilla `Hediff.CurStage` |
| `FD_ShellShock` | Stage 0 `AimingDelayFactor` | +0.25 | Phạt thời gian ngắm (+25%) | Vanilla `StatWorker` |
| `FD_ShellShock` | Stage 0 `MoveSpeed` | -0.20 | Phạt tốc độ di chuyển (-0.20 m/s) | Vanilla `StatWorker` |
| `FD_ShellShock` | Stage 1 (shell-shocked) `minSeverity` | 0.5 | Ngưỡng kích hoạt stage shell-shocked | Vanilla `Hediff.CurStage` |
| `FD_ShellShock` | Stage 1 `AimingDelayFactor` | +0.60 | Phạt thời gian ngắm (+60%) | Vanilla `StatWorker` |
| `FD_ShellShock` | Stage 1 `MoveSpeed` | -0.45 | Phạt tốc độ di chuyển (-0.45 m/s) | Vanilla `StatWorker` |

> **Ghi chú thay đổi hằng số cũ (Luật §8):**
> - `SuppressionEngine.MinSeverity`: Giá trị CŨ là `0.01f` (hardcode trong C#, lệch 10x so với XML). Đã chuyển sang đọc từ XML def.
> - `FD_Suppressed minSeverity`: Giá trị CŨ là `0.001` (làm severity không bao giờ <= 0f nên `HediffComp_SuppressionDecay.CompShouldRemove` không bao giờ kích hoạt, làm hediff bám vĩnh viễn trên pawn). Đã hạ về `0` để hediff tự xoá khi hết áp chế.
> - `Patch_Pawn_Kill_Down clamp floor`: Giá trị CŨ là `0.1f` (hardcode trong C#). Đã chuyển sang đọc `shockDef.minSeverity` (= 0).

---

## 9. Backlog — tầng điều khiển RTS

Chi tiết ở `tactical-expansion-features.md`. Không làm ở giai đoạn này (quyết định 8.2), giữ lại phán quyết để tham chiếu:

| Tính năng | Kết luận |
|---|---|
| **Suppressing Area Fire** | ✅ Đã gộp vào Suppression stance (5.6) |
| **Overwatch Zone** | ⏸ Hoãn. Nếu làm: bỏ pre-aim (vanilla không có warmup treo), thay bằng giảm mạnh `AimingDelayFactor` khi mục tiêu vào vùng |
| **Tactical Fireteams / Volley** | ❌ Bắn đồng loạt **lãng phí sát thương** — RimWorld không có ngưỡng damage trong một tick, bắn rải rác độc lập luôn có DPS hiệu dụng cao hơn. Nếu làm, làm **focus fire** |
| **Smart Attack-Move** | ❌ Auto-maintain là **tự động hoá việc kite** — không xoá micro mà giao cho máy, người chơi mất cả quyết định. Cộng thêm xung đột `FloatMenuMakerMap` với Achtung!/Tactical Groups/RunAndGun |

---

## 10. Modlist tham chiếu & load order (1.6)

Vừa là cấu hình "đủ trải nghiệm", vừa là **test bed**.

```
Core → DLC → HugsLib
Smarter Raider AI          (hoặc CAI 5000 / SmartRaider — CHỌN ĐÚNG MỘT)
Yayo's Combat 3 (Continued)
Suppression (Continued)
Fire Discipline
Melee Animation
Simple Sidearms / Run and Gun / Achtung!
Dubs Performance Analyzer  (công cụ dev)
```

*Fire Discipline load sau Yayo và Suppression:* runtime detection không phụ thuộc thứ tự load, nhưng patch XML dùng `MayRequire` thì có.

| Tầng | Mod | 1.6 | Vai trò |
|---|---|---|---|
| Nền cơ chế | Yayo's Combat 3 (Continued) | ✓ | Ammo, armor pen, accuracy |
| | **Fire Discipline** | — | Tầng chiến thuật |
| | Suppression (Continued) | ✓ | Suppression hediff |
| AI | Smarter Raider AI | ✓ | Nhẹ nhất; avoid grid mở rộng sang pawn đã draft |
| | CAI 5000 | ⚠ kiểm tra | Nặng hơn; tactical pathfinding, đa luồng |
| | SmartRaider | ⚠ ít kiểm chứng | Khói che tiến quân, EMP vô hiệu turret |
| Cận chiến | Melee Animation | ✓ | Animation, execution, duel |
| Giảm micro | Simple Sidearms · Run and Gun · Achtung! | ✓ **đã xác nhận chạy chung ổn** | |

### Xung đột phải nhớ

- **Chỉ một mod AI.** Cả ba can thiệp avoid grid và pathfinding raid.
- **Melee Animation**: xung đột mềm với Dual Wield; không chạy cùng RimThreaded.
- **Yayo's Combat 3 – Addon (Syrus)**: có báo cáo gây lỗi ammo không hiện.
- **RimWorld of Magic**: nhiều mod combat trong danh sách xung đột.
- **RocketMan**: ❌ **không tương thích về nguyên tắc.** Stat cache khiến trạng thái hediff không cập nhật đúng lúc — đã có báo cáo phá psycast từ 1.5. Toàn bộ Fire Discipline dựa trên hediff điều khiển stat theo thời gian thực. Tác giả cũng đã bỏ modding từ đầu 1.5, và 3 chức năng lõi xung đột với chính các tối ưu của 1.6. Thay bằng Performance Fish / Performance Optimizer / FPS Stabilizer.

---

## 11. Lộ trình

```
1. Xem Vanilla Fire Modes → ILSpy mục 6
2. Bổ sung debug action mục 7.1
3. Mục 5.1 Graze              ← nhỏ, độc lập, kiểm chứng ngay
4. Mục 5.2 Shell Shock        ← nhỏ, độc lập, giảm rủi ro perf
5. Mục 5.0.6 throttle
6. Chạy đủ mục 7.3 + 7.4 + 7.5
7. ▶ PHÁT HÀNH v1.0           ← 5 module lõi
8. Mục 5.6 Suppression stance ← module riêng, mặc định tắt
9. Mục 5.5 Shotgun spread     ← module riêng, mặc định tắt
10. Mục 5.7 Embrasure
11. Mục 5.3 Pinned            ← toggle riêng, mặc định tắt, đo kỹ
12. Mục 5.4 Full-auto         ← phụ thuộc 5.3 và 6.1–6.4
```

**Vì sao phát hành ở bước 7:** năm module đã xong, chưa ai ngoài tác giả chạy thử. Phần còn lại đang được thiết kế **dựa trên phỏng đoán về thứ người chơi muốn**, trong khi một tuần phản hồi Workshop trả lời chính xác câu đó — kể cả câu hỏi shotgun friendly fire đang treo ở 5.5a.

---

## 12. Tài liệu tham khảo

| # | Mod | Nguồn | Vì sao |
|---|---|---|---|
| 1 | **Vanilla Fire Modes** | [Workshop 3662471742](https://steamcommunity.com/sharedfiles/filedetails/?id=3662471742) | Nguồn ý tưởng cho 5.6. Xem cách giải `ShotsPerBurst` — có thể trả lời sẵn 6.1–6.4 |
| 2 | **Yayo's Shooting 2** | [Workshop 2020785943](https://steamcommunity.com/sharedfiles/filedetails/?id=2020785943) | Source đi kèm, tác giả **công khai mời fork** |
| 3 | **Suppression (Continued)** | [GitHub emipa606/Suppression](https://github.com/emipa606/Suppression) | Cách hook projectile và cấu trúc hediff |
| 4 | **Combat Extended** | [GitHub](https://github.com/CombatExtended-Continued/CombatExtended) | Mô hình recoil/sway; cách suppression đổi hành vi |
| 5 | **CE Aimbot** | [Workshop 2590848610](https://steamcommunity.com/sharedfiles/filedetails/?id=2590848610) | Thuật toán chọn tư thế tự động |
| 6 | **Yayo's Combat 3 (Continued)** | [GitHub emipa606/YayosCombat3](https://github.com/emipa606/YayosCombat3) | Bài học "suy ra, đừng khai báo" |
| 7 | **Melee Animation** | [Workshop 2944488802](https://steamcommunity.com/sharedfiles/filedetails/?id=2944488802) | Cách tổ chức mod settings |

**Kỹ thuật:** ILSpy / dnSpy trên `Assembly-CSharp.dll` (nguồn sự thật duy nhất) · Harmony `harmony.pardeike.net` (patch order & priority) · Dubs Performance Analyzer · RimWorld Discord kênh mod-development

**Bản quyền:** YAYO tuyên bố công khai cho phép fork Yayo's Shooting 2 và uỷ quyền tiếp tục Yayo's Combat 3 — **lưu ảnh chụp tuyên bố**. Repo emipa606: đọc LICENSE từng repo. CE: Creative Commons, đọc kỹ attribution.

---

## 13. Tiêu chí thành công

> **Mod sống được qua ít nhất hai bản DLC mà không cần viết lại.**

Nỗi đau của cộng đồng RimWorld không phải thiếu ý tưởng combat — mà là các mod hay đều chết theo tác giả.
