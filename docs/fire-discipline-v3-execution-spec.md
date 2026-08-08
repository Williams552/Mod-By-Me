# Fire Discipline — v3 Execution Spec

> Tổng hợp các thay đổi thiết kế. Chỉ gồm: **làm gì / vì sao / kiểm tra gì**.
> Áp dụng lên codebase hiện tại (5 module đã hoàn thành).

---

## 0. Bắt buộc làm trước mọi thứ

| # | Việc | Vì sao |
|---|---|---|
| 0.1 | Cache `FieldInfo` trong `Patch_ShotReport.cs` thành `static readonly`, hoặc chuyển sang `AccessTools.FieldRefAccess` | Reflection + boxing chạy **mỗi phát bắn**. Full-auto ở mục 4 sẽ nhân đôi số lần gọi. Không sửa trước = tự tạo bug hiệu năng |
| 0.2 | Sửa cột Skill 20 bị thiếu trong `DebugHarness` | Skill 20 là biên trên — nơi lỗi cân bằng lộ rõ nhất |
| 0.3 | Thêm ma trận DPS vào harness: `burstCount × damage × hitChance / (warmup + cooldown)` | Chỉ tiêu của Rapid là DPS, không phải hit chance. Hiện chưa đo được |
| 0.4 | Thêm ma trận "bị bắn vào" (pawn địch bắn **vào** pawn test) | `factorFromTargetSize ×0.65` của Prone **chưa từng được kiểm chứng** — đó là lý do Prone tồn tại |
| 0.5 | Test với 4 vũ khí: shotgun, assault rifle, SMG, sniper | Bolt-action là ca biên. Công thức `d₀` chỉ chứng minh được khi có đủ dải |
| 0.6 | Throttle `PassiveStanceEvaluator` xuống 30–60 tick | Đánh giá cự ly + vũ khí cho mọi raider mỗi tick là chi phí thật |

---

## 1. Graze — đổi điều kiện kích hoạt

### Chỉ lệnh

Bỏ ý tưởng "miễn trừ theo stance" và "hit roll vượt ngưỡng". Thay bằng:

```
p = TotalEstimatedHitChance tại thời điểm bắn
grazeChance = clamp(0, 1, (0.65 − p) / 0.45)
```

- `p ≥ 0.65` → không bao giờ graze
- `p ≤ 0.20` → luôn graze
- Fallback `0.5` nếu launcher đã chết hoặc không phải pawn

**Cài đặt:** trong `Patch_DamageWorker_AddInjury`, khi phát hiện đòn vào bộ phận sống còn, **gọi lại** `ShotReport.HitReportFor` từ `projectile.Launcher` + `intendedTarget` + khoảng cách đã đi. **Không chuyền state qua projectile.**

### Vì sao

- **"Hit roll vượt ngưỡng" không cài được sạch.** RimWorld roll nhị phân trong `Verb_LaunchProjectile.TryCastShot` và không lưu biên độ. Muốn có biên độ phải chuyền `Rand.Value` xuyên 3 lớp patch — nợ kỹ thuật không đáng.
- **Stance exemption chỉ cho 1 cần gạt. `p` cho tất cả** — vì `p` đã chứa tác động của tư thế, cự ly, cover, ánh sáng, skill, suppression, shell shock. Người chơi không "bật chế độ sát thương", họ **tạo điều kiện** để phát bắn xứng đáng gây chết người.
- Phát biểu đúng mục đích gốc của Graze: viên may mắn ở 8% không được xoá sổ pawn skill 20; phát bắn dàn dựng ở 85% thì phải giết được.
- **Đối xứng** — `p` tính cho cả raider, không power creep.

### Kết quả mong đợi (kiểm chứng bằng harness)

| Tình huống | `p` | grazeChance |
|---|---|---|
| Touch 3, skill 4, Snap | 77.9% | 0% |
| Long 40, skill 16, Sharpshot | 52.4% | 28% |
| Long 40, skill 16, Snap | 44.6% | 45% |
| Long 40, skill 10, Snap | 19.5% | 100% |

→ Rapid kiếm được sát thương chí mạng ở cự ly gần, Sharpshot ở cự ly xa, Prone và bắn hoảng loạn thì không — **mà không cần đặc cách cho tư thế nào**.

---

## 2. Shell Shock — giới hạn

### Chỉ lệnh

```
shockRadius  = min(20, r + 2 × sqrt(r))
powerFactor  = clamp(0.4, 1.0, damAmount / 50)
```

Kèm 5 cổng lọc:

| # | Quy tắc | Vì sao |
|---|---|---|
| 2.1 | Sàn cắt: bỏ qua pawn có severity < **0.15** | Cứu TPS. Không có sàn, một quả mortar tạo hàng chục hediff vô nghĩa; siege thì nhân lên liên tục |
| 2.2 | Bỏ qua nếu `damAmount < 10` hoặc damType không phải sát thương vật lý | `StartExplosion` bắn cho **mọi** vụ nổ: firefoam, smoke, EMP, extinguish. Không lọc thì boomalope chết gần bếp gây shell shock cho đầu bếp |
| 2.3 | Pawn **không drafted**: severity ×0.3 | Siege kéo dài nhiều ngày in-game. Không có luật này, cả colony bị debuff liên tục suốt siege — biến sự kiện căng thẳng thành sự kiện bực bội |
| 2.4 | Theo LOS như vanilla rải sát thương nổ | Nếu không, mortar rơi ngoài sân shock cả người đang ngủ trong phòng kín |
| 2.5 | **Refresh** severity, không cộng dồn; trần 40 pawn mỗi vụ nổ | Loạt mortar 6 quả sẽ đẩy severity lên trần và giữ pawn vô dụng rất lâu. Trần pawn chặn mech cluster nổ dây chuyền giữa raid 80 pawn |

### Vì sao đổi công thức

Vấn đề của `radius × 2.0` không phải hệ số mà là **quan hệ tuyến tính**. Sóng xung kích suy giảm theo luỹ thừa khoảng cách → vùng ảnh hưởng không được lớn tỉ lệ thuận với bán kính nổ.

| Nguồn | `r` | Cũ (×2) | Mới |
|---|---|---|---|
| Grenade | 2.9 | 5.8 | 6.3 |
| Mortar HE | 4.9 | 9.8 | **9.3** ← giữ được giá trị đang thích |
| Rocket lớn | 9.0 | 18.0 | 15.0 |
| Doomsday | 13.9 | 27.8 | 20 (cap) |
| Vũ khí mod | 20–30 | **40–60** ⚠ | 20 (cap) |

**Trần 20 ô có lý do gameplay:** bằng khoảng nửa tầm bắn tối đa → **luôn tồn tại vị trí ngoài vùng shock**. Nếu shock phủ toàn bộ khu giao tranh thì nó không còn là yếu tố vị trí, chỉ là thuế đánh đều.

`powerFactor` cần thiết vì hiện grenade và doomsday cùng cho 0.85 ở tâm.

---

## 3. Suppression → tầng hành vi *(điều kiện tiên quyết cho mục 4)*

### Chỉ lệnh

Thêm ngưỡng **pinned**: severity > 0.8 → pawn không bắn được.

**Cài đặt:** chặn qua `Verb.Available()`. **Vẫn cho di chuyển** — không chạm JobGiver, không chạm ThinkTree.

### Vì sao — đây là mấu chốt

Trong CE, suppression **đổi hành vi**: pawn chạy tìm cover kể cả khi bị ra lệnh ngược lại; chạm ngưỡng tới hạn thì nằm rạp mất hoàn toàn khả năng phản ứng. Đó là lý do suppressive fire trong CE đáng dùng.

Trong Fire Discipline hiện tại, suppression chỉ là **hiệu ứng chỉ số** — địch bắn kém hơn nhưng vẫn làm đúng việc nó định làm.

**Hệ quả:** nếu làm full-auto theo mô hình CE (hy sinh DPS đổi lấy suppression) thì người chơi **hy sinh thật nhưng nhận về ít hơn nhiều** → full-auto thành lựa chọn tệ hơn, không ai dùng.

Ngưỡng pinned biến suppression từ debuff thành **điều kiện thắng thua**, và là mảnh còn thiếu duy nhất để 5 module hiện có khoá vào nhau.

⚠ **Đây là thay đổi cân bằng lớn nhất của v3.** Pinned áp lên colonist đau đúng bằng lúc áp lên raider. Đo kỹ trước khi bật mặc định.

---

## 4. Full-auto — Rapid + vũ khí burst

### Chỉ lệnh

```
Rapid + súng có burstShotCount ≥ 3:
  burstShotCount      ×2.0    (3 → 6)
  cooldown            ×1.6
  ticksBetweenBurst   ×0.6

Giật nòng: phát thứ N trong loạt → accuracy ×Pow(0.93, N)
```

**Điểm móc:**
- Cooldown → postfix `VerbProperties.AdjustedCooldownTicks(verb, pawn)` — method **có sẵn pawn**, chỉnh được theo từng pawn theo từng tư thế mà không đụng Def
- Giật nòng → `Patch_ShotReport.cs` **đã ở đúng chỗ**; đọc `verb.burstShotsLeft` để biết phát thứ mấy

⚠ **TUYỆT ĐỐI KHÔNG** mutate `verbProps` trực tiếp — object cấp Def, sửa một lần là mọi khẩu cùng loại đổi theo (kể cả của raider) và rò rỉ qua save.

### Vì sao

- **Không biến assault rifle thành full-auto vĩnh viễn.** Full-auto là *thứ Rapid làm với súng có khả năng đó* — giữ nguyên mô hình "tư thế quyết định, không phải vũ khí quyết định".
- **Tổng DPS không tăng, hình dạng đổi:** dồn hoả lực 1 giây rồi phơi mình 1.5 giây. Đó là quyết định, không phải buff.
- **Giật nòng tự cân bằng loạt dài:** đuôi loạt gần như vô dụng ở tầm xa nhưng vẫn hiệu quả ở tầm gần — đúng bản sắc Rapid. CE có hiệu ứng này miễn phí (đạn đạo thật + recoil tích luỹ); ta phải khai báo tay, nhưng mua được kết quả mà không mua chi phí kiến trúc.
- **Ý đồ CE cần nhớ:** full-auto là **công cụ áp chế**, không phải công cụ sát thương — một pawn hy sinh sát thương để tạo điều kiện cho pawn khác. Giá trị nằm ở người khác. Chỉ đúng nếu mục 3 đã làm.

---

## 5. Shotgun spread AoE

### Chỉ lệnh

```
R = 2.5 ô (cố định theo vũ khí, KHÔNG theo skill)
e = lerp(0.15, 0.55, shootingSkill / 20)
dmgFactor(d) = lerp(1.0, e, d / R)
primaryDamage ×0.70
```

**Nhận diện shotgun:** dùng lại `d₀` của Rapid — `AccuracyTouch ≥ AccuracyMedium` (tức `d₀ = 12`). Loại trừ `Projectile_Explosive` và vũ khí có `range > 25`.

**Cài đặt:** `Patch_Projectile_Impact.cs` **đã làm đúng dạng tính toán này** (bán kính + suy giảm theo khoảng cách từ tâm). Lần thứ ba dùng cùng khuôn. Không cần projectile class mới, không cần verb mới.

### Vì sao

- **Skill điều khiển viền, không điều khiển bán kính:** nếu cả hai cùng scale theo skill thì thành bậc hai — xạ thủ 20 mạnh gấp bội khó kiểm soát. `R` cố định thành con số người chơi **học thuộc và tính vị trí theo đó**.
- **Cùng tiêu chí `d₀` cho hai module** → nhất quán, và mọi súng của mọi mod tự phân loại đúng.
- **`×0.70` bắt buộc:** không có nó, shotgun được AoE miễn phí → power creep.

### Bốn quyết định kèm theo

| # | Quyết định | Vì sao |
|---|---|---|
| 5.1 | **Không miễn trừ phe** — splash trúng cả đồng đội | 2.5 ô đủ nhỏ để thành quyết định vị trí chứ không thành hình phạt ngẫu nhiên; cho shotgun một chi phí thật |
| 5.2 | **Bắt buộc có cảnh báo UI** — tô vùng nguy hiểm khi rê chuột nhắm | Không có cảnh báo, người chơi sẽ nghĩ mod bị lỗi |
| 5.3 | Splash **không bao giờ graze, cũng không bao giờ trúng bộ phận sống còn** — chọn bộ phận ngoại vi ngay từ đầu | Nạn nhân splash không có hit roll → không có `p` để tính graze. Vừa nhất quán vừa rẻ hơn |
| 5.4 | Splash gây suppression ở mức ×0.4 | Không giảm thì shotgun thành cỗ máy áp chế mạnh nhất game ở cự ly gần |

---

## 6. Quyết định kiến trúc: tách trục fire mode / aim mode

### Vấn đề

CE tách **fire mode** (bao nhiêu viên: single / burst / auto) khỏi **aim mode** (ngắm kỹ đến đâu: aimed / snapshot / suppressive). Hai hệ thống độc lập, tổ hợp thành ma trận.

Fire Discipline gộp cả hai vào một trục 4 tư thế → **mất khả năng diễn đạt "bắn nhiều đạn nhưng vẫn ngắm kỹ"**.

### Quyết định cần đưa ra

Chưa cần làm ngay, nhưng phải quyết trước khi mở rộng thêm tư thế:

- **Giữ 1 trục:** đơn giản hơn, ít micro hơn, nhưng ma trận biểu đạt hẹp
- **Tách 2 trục:** biểu đạt đầy đủ như CE, nhưng gánh nặng micro tăng bội

⚠ Bằng chứng thực nghiệm: sự tồn tại của mod **CE Aimbot** (tự chọn fire mode và aim mode theo cự ly + loại súng) chứng minh ma trận của CE tạo gánh nặng micro **đủ lớn để cần một mod riêng dọn dẹp**. → Nếu tách 2 trục, `PassiveStanceEvaluator` / tư thế mặc định tự động **bắt buộc phải có sẵn trong mod**, không phải làm sau.

---

## 7. Phải xác minh bằng ILSpy trước khi code

| # | Cần biết | Ảnh hưởng đến |
|---|---|---|
| 7.1 | `Verb.ShotsPerBurst` có phải virtual property không | Mục 4 — nếu không, phải tìm đường khác |
| 7.2 | `TryCastNextBurstShot` đọc `ticksBetweenBurstShots` từ đâu | Mục 4 |
| 7.3 | `AdjustedCooldownTicks` có bị mod khác patch không | Mục 4 — điểm xung đột tiềm tàng |
| 7.4 | `verb.burstShotsLeft` có accessible từ ShotReport context không | Mục 4 — cơ chế giật nòng |
| 7.5 | Vanilla đã có `factorFromPosture` chưa | Prone — nếu có thì dùng lại thay vì tự thêm |
| 7.6 | `Verb.Available()` có được gọi đủ thường xuyên để chặn bắn không | Mục 3 — cơ chế pinned |

---

## 8. Tham chiếu bắt buộc xem trước

| Mod | Xem để làm gì |
|---|---|
| **Vanilla Fire Modes** *(Workshop 3662471742)* | Gần như **chính xác thứ đang định làm** — fire mode chọn được, không đại tu core. Xem cách họ giải `ShotsPerBurst`. Có thể tiết kiệm cả buổi ILSpy, hoặc cho thấy chỗ không giải được |
| **CE Aimbot** *(Workshop 2590848610)* | Bằng chứng về gánh nặng micro của ma trận mode. Tham chiếu cho thuật toán chọn tư thế tự động |
| **Combat Extended** *(GitHub)* | Mô hình recoil / sway, và cách suppression đổi hành vi |

---

## 9. Thứ tự thực thi

```
1. Mục 0 (toàn bộ)          ← hạ tầng đo lường + hiệu năng
2. Mục 1 Graze              ← nhỏ, độc lập, kiểm chứng được ngay bằng harness
3. Mục 2 Shell Shock        ← nhỏ, độc lập, giảm rủi ro perf
4. Mục 5 Shotgun spread     ← tái dùng Patch_Projectile_Impact, rủi ro thấp
5. Mục 3 Suppression pinned ← thay đổi cân bằng lớn, cần đo kỹ
6. Mục 4 Full-auto          ← phụ thuộc mục 3 và mục 7.1–7.4
7. Mục 6                    ← chỉ quyết định, chưa cần code
```

**Ghi chú định vị:** Mục 4 và 5 **đổi cân bằng vũ khí**, khác về bản chất với 5 module đầu vốn chỉ *cộng thêm một tầng*. Đó là lãnh địa Yayo Combat 3 đang chiếm. → Đóng gói thành module riêng, **mặc định TẮT**, nói rõ trong mô tả, để người chỉ muốn tầng chiến thuật vẫn cài được mà không bị đổi cân bằng vũ khí ngoài ý muốn.
