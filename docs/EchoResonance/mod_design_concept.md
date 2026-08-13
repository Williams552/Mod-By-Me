# 📄 RimWorld Mod Concept: Echo Resonance — Đài Cộng Hưởng & Hệ Perk Pawn

> **Tài liệu Thiết kế Mod RimWorld**
> *Trạng thái:* Bản chốt ý tưởng (v2) — thay thế hoàn toàn bản draft "Colonial Merit".

---

## 📌 1. Mục Tiêu Thiết Kế (Design Intent)

**Đối tượng:** Người chơi đã suffer đủ nhiều với RimWorld và muốn một save **chill, mạnh, có mục tiêu dài hạn**.

**Nguyên tắc nền:**

1. **Mod chấp nhận OP.** Perk được phép mạnh tới mức phá cân bằng — đó là phần thưởng cho hàng chục giờ tích lũy, không phải lỗi thiết kế.
2. **Đắt bằng thời gian, không đắt bằng tài nguyên.** Không có cách nào đổi bạc/steel/điện lấy điểm. Không thể "giàu là mạnh".
3. **Không có slot cứng.** Muốn nhồi 10 perk vào một pawn thì cứ nhồi — chỉ là chi phí lũy tiến theo cấp số nhân.
4. **Rủi ro nằm ở một điểm duy nhất:** công trình lõi. Không có Strain, không có mental break bắt buộc, không có pawn hóa quái vật.
5. **Perk ưu tiên xóa bỏ phiền toái**, không chỉ tăng chỉ số. Đây là mod để thở, không phải mod để tính toán.

---

## 🏛️ 2. Tiền Tệ: **Dư Ảnh** (Echo)

Một bể điểm chung của thuộc địa. **Không phải item** — không stack trong kho, không trade, không cướp được, không đổi từ tài nguyên.

Dư Ảnh chỉ sinh ra khi **Đài Cộng Hưởng đang đứng và có điện**. Không có Đài = không tích được điểm (điểm cũ vẫn giữ, trừ trường hợp Đài bị phá — xem §3).

### 2.1 Nguồn sinh điểm — dựa trên hoạt động của Pawn

Điểm không nhỏ giọt vô hồn theo thời gian. Nó phản chiếu **những gì thuộc địa thực sự làm được**. Colony càng sống động, càng tiến bộ thì càng tích nhanh.

| Sự kiện | Dư Ảnh | Ghi chú |
|---|---|---|
| Nền (passive trickle) | **0.25 / ngày** | Sàn tối thiểu, để colony đang gặp hạn vẫn nhích |
| Pawn lên level skill vanilla (1–10) | **0.5** | Mỗi lần lên level |
| Pawn lên level skill vanilla (11–15) | **1.5** | |
| Pawn lên level skill vanilla (16–20) | **4.0** | Level 20 đầu tiên của colony: thưởng thêm 10 |
| Đẩy lui một đợt Raid | **2–6** | Scale theo raid points, cap 6 |
| Hoàn thành một research project | **1–3** | Theo tech level của project |
| Chế ra đồ Masterwork | **1** | |
| Chế ra đồ Legendary | **3** | |
| Sống qua mỗi Quadrum (15 ngày) | **3** | Cột mốc ổn định |
| Hoàn thành Ritual (Ideology) | **1** | Chỉ tính ritual thành công |
| Thuần hóa thú có wildness ≥ 75% | **1** | |
| Pawn mới gia nhập vĩnh viễn | **2** | Recruit / born / rescue-join |

**Tốc độ thực tế ước tính:** early game ~1 Echo/ngày, mid game colony 8–12 pawn hoạt động tốt ~2.5–4 Echo/ngày, trước khi nhân hệ số Pylon.

### 2.2 Điều bị cấm có chủ đích

- ❌ Không có công trình "nạp tài nguyên → ra điểm"
- ❌ Không có bill, không có pawn ngồi cày điểm
- ❌ Không mua điểm từ trader
- ❌ Không có cách nào tăng tốc ngoài Pylon (trần cứng ×3)

---

## 🗼 3. Công Trình Lõi: **Đài Cộng Hưởng** (Archotech Resonator)

**Giới hạn tuyệt đối: 1 cái / map.**

| Thuộc tính | Giá trị |
|---|---|
| Research | `ResonanceCore` — chi phí ngang **Multi-Analyzer**, prereq: Microelectronics Basics |
| Chi phí xây | 150 Steel, 8 Component, 1 Advanced Component |
| Kích thước | 3×3 |
| Điện | 200W |
| HP | 500 (cao, nhưng không bất tử) |
| Work to build | 4000 |

**Cố ý làm rẻ.** Cái đắt của mod này là 50–200 giờ sau khi xây, không phải viên gạch đầu tiên. Người chơi nên có Đài vào khoảng giữa game và sống với nó suốt phần còn lại của save.

### 3.1 Cơ chế rủi ro — Trái tim của mod

- Mất điện → **ngưng tích điểm**, không mất điểm đã có. Bật lại là chạy tiếp.
- **Bị phá hủy → mất 100% Dư Ảnh chưa tiêu.** Không hoàn, không grace period.
- **Perk đã mua giữ nguyên vĩnh viễn.** Đây là ranh giới quan trọng: bạn mất tiến độ, không bao giờ mất thành quả.
- Xây lại được ngay lập tức với chi phí gốc, bể điểm bắt đầu lại từ 0.

Đây là **toàn bộ** áp lực mà mod đặt lên người chơi. Hệ quả thiết kế: người chơi được khuyến khích chôn Đài sâu trong núi, bọc tường đá, đặt turret quanh — một mục tiêu phòng thủ tự nhiên, không cần script ép buộc.

### 3.2 UI

- Gizmo trên Đài: hiện số Dư Ảnh hiện tại + tốc độ/ngày + số Pylon đang hoạt động.
- Cảnh báo đỏ (letter) khi Đài xuống dưới 40% HP: *"Đài Cộng Hưởng đang bị phá hoại — X Dư Ảnh sắp tan biến."*

---

## 📡 4. Công Trình Tăng Tốc: **Trụ Khuếch Tán** (Attunement Pylon)

| Thuộc tính | Giá trị |
|---|---|
| Research | Cùng project `ResonanceCore` |
| Chi phí xây | 60 Steel, 2 Component, 20 Gold |
| Kích thước | 1×1 |
| Điện | 100W mỗi trụ |
| HP | 180 |

**Ba tầng khóa chống spam:**

1. **Hard cap 4 trụ / map** — chặn ở `PlaceWorker`, không cho đặt blueprint thứ 5.
2. **Phải nằm trong bán kính 12 ô tính từ Đài** — buộc mọi thứ tập trung một chỗ, dễ bị đánh cùng lúc.
3. **Mỗi trụ cách trụ khác tối thiểu 8 ô** — không dồn cục, chiếm diện tích thật trong base.

**Hiệu ứng:** mỗi trụ hoạt động (có điện, đúng vị trí) cộng **+50%** tốc độ tích lũy.

| Số Pylon | Hệ số |
|---|---|
| 0 | ×1.0 |
| 1 | ×1.5 |
| 2 | ×2.0 |
| 3 | ×2.5 |
| 4 | ×3.0 (trần cứng) |

Pylon **không** nhận tài nguyên, **không** có bill, **không** cần pawn. Nó chỉ là hệ số nhân thuần túy.

Pylon bị phá → mất hệ số, **không** mất điểm.

---

## 🔒 5. Ba Tầng Tech Gate — Khi Nào Được Thả Cửa Godpawn

Đây là cơ chế điều tiết chính. Trước khi mở khóa hết, người chơi **buộc phải cân nhắc pawn này xứng đáng nhận perk nào** — vì chỉ có Tier 1 để chọn.

### Tier 1 — Mở ngay khi có Đài
Chỉ cần Dư Ảnh. Không cần gì thêm.
→ *Giai đoạn "chọn lựa": mỗi perk là một cam kết, vì bạn chưa với tới thứ mạnh hơn.*

### Tier 2 — Cần research `EchoAttunement` **+ Item xúc tác mỗi lần mua**

**Research:** `EchoAttunement`, prereq Advanced Fabrication.

**Item xúc tác: Tinh Thể Cộng Hưởng (Resonance Focus)**
- Chế tại Fabrication Bench: **30 Plasteel + 15 Gold + 2 Advanced Component**, work 6000, cần Crafting ≥ 8
- **Tiêu thụ 1 viên cho mỗi perk Tier 2 được mua**, bỏ trực tiếp vào Đài

Lưu ý thiết kế: Tinh Thể **không** sinh ra Dư Ảnh và **không** rút ngắn thời gian chờ. Nó chỉ là cánh cửa — bạn vẫn phải chờ đủ điểm. Nó gate bằng *năng lực sản xuất*, không phải bằng *độ giàu*.

### Tier 3 — Cần research `ArchotechResonance`, **không cần xúc tác**

**Research:** `ArchotechResonance`, prereq `EchoAttunement` + Ship Basics-tier, chi phí research rất cao.

Từ điểm này trở đi **thả cửa hoàn toàn**: chỉ còn Dư Ảnh và thời gian đứng giữa bạn và godpawn. Đây là phần thưởng cho việc đi tới cuối tech tree.

---

## 💰 6. Giá & Chi Phí Lũy Tiến

### Giá gốc

| Tier | Dư Ảnh | Thời gian ước tính (tốc độ ×3 mid-game) |
|---|---|---|
| **Tier 1** | 20 | ~7 ngày |
| **Tier 2** | 60 | ~20 ngày |
| **Tier 3** | 150 | ~50 ngày (gần 1 năm in-game) |

### Hệ số lũy tiến theo pawn

Perk thứ **N** mua cho **cùng một pawn**:

```
Giá thực = Giá gốc × 1.6^(N-1)
```

| Perk thứ | Hệ số | T1 | T2 | T3 |
|---|---|---|---|---|
| 1 | ×1.00 | 20 | 60 | 150 |
| 2 | ×1.60 | 32 | 96 | 240 |
| 3 | ×2.56 | 51 | 154 | 384 |
| 4 | ×4.10 | 82 | 246 | 614 |
| 5 | ×6.55 | 131 | 393 | 983 |
| 6 | ×10.49 | 210 | 629 | 1573 |

**Không có trần.** Perk thứ 8 trên một pawn là hoàn toàn hợp lệ — nó chỉ là mục tiêu của cả một save. Đây chính là "đánh lũy tiến thay vì slot cứng".

Hệ quả tự nhiên: đầu tư dàn trải cho 4 pawn rẻ hơn nhiều so với dồn hết vào 1 — nhưng nếu người chơi *muốn* một godpawn duy nhất, mod không cấm, chỉ tính phí.

### Giảm giá chuyên môn (Specialization Discount)

Nếu perk đang mua **cùng Nhánh** với ít nhất một perk pawn đó đã sở hữu:

```
Giá cuối = Giá gốc × 1.6^(N-1) × 0.75
```

| Ví dụ: pawn đã có 2 perk Nhục Thân, mua perk thứ 3 | Giá |
|---|---|
| Perk T1 **cùng nhánh** Nhục Thân | 20 × 2.56 × 0.75 = **38** |
| Perk T1 **khác nhánh** (Chiến Trận) | 20 × 2.56 = **51** |
| Perk T3 **cùng nhánh** | 150 × 2.56 × 0.75 = **288** |
| Perk T3 **khác nhánh** | 150 × 2.56 = **384** |

Mục đích: đẩy người chơi về phía **archetype** — pawn này là cỗ máy lao động, pawn kia là chiến thần — mà không cấm đoán gì. Muốn pawn đa năng toàn diện thì vẫn được, chỉ đắt hơn ~33%.

Giảm giá **không cộng dồn**: có 1 hay 5 perk cùng nhánh cũng chỉ ×0.75 một lần.

### Perk nâng cấp không làm tăng N

Khi một perk **thay thế** perk cũ (xem §7.1), perk cũ bị **tiêu thụ** — pawn không giữ cả hai. Do đó:

- **N (số perk đang sở hữu) không tăng** khi nâng cấp trong cùng chuỗi.
- Không hoàn lại Dư Ảnh đã tiêu cho perk cũ. Trả đủ giá perk mới.

Ví dụ pawn đang có 3 perk, trong đó có *Giấc Ngủ Nông* (T1):

| Hành động | N sau đó | Giá |
|---|---|---|
| Mua *Không Biết Mệt* (thay thế Giấc Ngủ Nông) | vẫn 3 | 60 × 1.6² × 0.75 = **115** |
| Mua một perk T2 mới hoàn toàn | 4 | 60 × 1.6³ × 0.75 = **184** |

Hệ quả có chủ đích: **đi sâu một chuỗi rẻ hơn hẳn so với sưu tầm dàn trải.** Cộng với giảm giá cùng nhánh, mod đẩy mạnh về phía archetype chuyên biệt — nhưng vẫn không cấm gì cả.

---

## ✨ 7. Danh Sách Perk — Lưới 4 Nhánh × 3 Tier

Triết lý: **perk phải xóa bỏ một phiền toái, không chỉ cộng phần trăm.** Người chơi mục tiêu đã chán việc micro-manage.

Perk được phân loại theo **hai trục độc lập**:
- **Tier** (1/2/3) = chi phí và tech gate
- **Nhánh** (4 nhánh) = chủ đề, dùng để tính giảm giá chuyên môn ×0.75

Nhánh Trade-off **không phải nhánh thứ 5** — nó là một **cờ đánh dấu** gắn lên perk bất kỳ trong 4 nhánh, giảm giá **-40%** sau mọi hệ số khác.

---

## 7.1 Ba Loại Quan Hệ Giữa Các Perk

Perk không phải một danh sách phẳng. Ba quan hệ định hình cây perk:

### 🔗 `requires` — Điều kiện tiên quyết (perk cũ **ở lại**)

Phải sở hữu perk A mới mua được perk B. A **vẫn hoạt động** song song với B.

Dùng khi hai perk khác tác dụng nhưng có quan hệ nhân quả: *Siêu Cận Chiến* cần *Cơ Bắp Phát Triển* — cơ bắp là nền tảng thể chất, kỹ thuật cận chiến là thứ xây trên nó. Cả hai cùng có hiệu lực.

**Chi phí:** N tăng bình thường (pawn giữ cả hai perk).

### ⬆️ `replaces` — Nâng cấp (perk cũ **bị tiêu thụ**)

Phải sở hữu perk A; khi mua B thì A **bị gỡ bỏ**, hediff của A biến mất.

Dùng khi hai perk **cùng loại tác dụng ở cấp độ khác nhau**: *Không Biết Mệt* (-70% rest) thay thế *Giấc Ngủ Nông* (-40% rest). Không bao giờ cộng dồn, không bao giờ có perk cấp thấp thành rác.

**Chi phí:** N **không tăng** (xem §6). Đây là lý do đi sâu chuỗi rẻ hơn sưu tầm.

`replaces` bao hàm luôn `requires` — không cần khai báo cả hai.

### ⛔ `exclusionTags` — Loại trừ lẫn nhau

Hai perk cùng mang một tag loại trừ thì không thể cùng tồn tại trên một pawn. Perk bị khóa hiện màu xám trong UI kèm dòng giải thích *"Xung đột với: [tên perk]"*.

Dùng để buộc người chơi **chọn một con đường**, chủ yếu ở các nhánh trade-off. Không gỡ được sau khi đã mua — đây là cam kết thật.

---

## 7.2 Lưới Perk

Ký hiệu: `⬆️ X` = thay thế X · `🔗 X` = cần X (giữ lại) · `⛔ tag` = nhóm loại trừ · 🟥 = trade-off (-40% giá)

---

### 🫀 Nhánh **Nhục Thân** — Thể chất, sinh tồn, thương tật

| Tier | Perk | Quan hệ | Hiệu ứng |
|---|---|---|---|
| T1 | **Giấc Ngủ Nông** | — | Rest tụt chậm 40% |
| T1 | **Thịt Lành** | — | Tốc độ hồi phục vết thương ×2; nguy cơ nhiễm trùng giảm nửa |
| T1 | **Da Dày** | — | Miễn nhiễm mood penalty về nhiệt độ; dải nhiệt an toàn mở rộng ±25°C |
| T1 | **Chân Nhẹ** | — | Move speed +35%; không bị chậm do địa hình (bùn, tuyết, đầm lầy) |
| T1 | **Dạ Dày Sắt** | — | Miễn nhiễm food poisoning; không mood penalty vì đồ ăn thô/nguyên liệu |
| T1 | **Cơ Bắp Phát Triển** | — | Melee damage +30%; carry capacity +60%; carrying speed +25% |
| T2 | **Không Biết Mệt** | ⬆️ Giấc Ngủ Nông | Rest tụt chậm 70%; ngủ 3 giờ là đầy |
| T2 | **Máu Nhanh** | ⬆️ Thịt Lành | Hồi phục vết thương ×5; miễn nhiễm nhiễm trùng hoàn toàn |
| T2 | **Phổi Sạch** | 🔗 Da Dày | Miễn nhiễm toxic fallout, khí độc, bụi phổi, mọi bệnh hô hấp mãn tính |
| T3 | **Máu Archotech** | ⬆️ Máu Nhanh | Chi thể mất tự mọc lại sau 15 ngày; xóa mọi bệnh mãn tính; lão hóa ngừng |
| T3 | **Bất Tử Kém Chất Lượng** | 🔗 Máu Archotech | Không thể chết. Khi lẽ ra chết → downed, tự hồi hoàn toàn sau 3 ngày |

---

### 🧠 Nhánh **Tâm Trí** — Tinh thần, xã hội, trí tuệ

| Tier | Perk | Quan hệ | Hiệu ứng |
|---|---|---|---|
| T1 | **Tâm Vững** | — | Ngưỡng mental break giảm 50% |
| T1 | **Vô Ưu** | — | Miễn nhiễm mood penalty từ xác chết, máu, phòng xấu, bóng tối, chật chội |
| T1 | **Trí Nhớ Sáng** | — | Learning rate mọi skill +60%; skill không bao giờ decay |
| T1 | **Miệng Lưỡi** | — | Social impact +40%; negotiation +25% |
| T2 | **Lặng Tâm** | ⬆️ Tâm Vững | Ngưỡng mental break giảm 80%; miễn nhiễm break do một sự kiện đơn lẻ |
| T2 | **Tư Duy Sắc** | ⬆️ Trí Nhớ Sáng | Research speed ×2.2; mỗi project hoàn thành cho thêm +1 Dư Ảnh · ⛔ `Tâm Trí Nguyên Vẹn` |
| T2 | **Giọng Nói Ấm** | ⬆️ Miệng Lưỡi | Social impact ×2; recruit difficulty -50%; giá trade +30% · ⛔ `Tâm Trí Nguyên Vẹn` |
| T2 | **Kẻ Dẫn Đường** | 🔗 Giọng Nói Ấm | Đồng đội trong bán kính 10 ô được **+8 mood** và work speed +15% |
| T3 | **Tâm Bất Động** | ⬆️ Lặng Tâm | **Không bao giờ mental break.** Mood luôn được ép ≥ 35% |
| T3 | **Ý Chí Lan Tỏa** | 🔗 Kẻ Dẫn Đường | **Toàn colony** giảm 40% ngưỡng mental break; miễn nhiễm mood penalty do người thân qua đời |

---

### 🔨 Nhánh **Sinh Kế** — Lao động, chế tạo, nông nghiệp, chăn nuôi

| Tier | Perk | Quan hệ | Hiệu ứng |
|---|---|---|---|
| T1 | **Tay Vững** | — | Không bao giờ tạo ra đồ Awful/Poor; crafting speed +20% |
| T1 | **Ngón Xanh** | — | Plant work speed +60%; ruộng do pawn này trồng miễn nhiễm blight |
| T1 | **Bạn Của Thú** | — | Tame chance +80%; huấn luyện nhanh gấp đôi |
| T1 | **Bàn Tay Ấm** | — | Tỷ lệ phẫu thuật thất bại giảm 80%; tend quality +25% |
| T1 | 🟥 **Nông Dân Đại Địa** | ⬆️ Ngón Xanh · ⛔ `Bàn Tay Súng` | *Buff:* Plant work +150%, sản lượng +50% · *Debuff:* Shooting accuracy -50% |
| T2 | **Bàn Tay Tổ** | ⬆️ Tay Vững | Mọi đồ chế ra tối thiểu **Excellent**; +25% cơ hội Legendary |
| T2 | **Lời Của Đất** | ⬆️ Ngón Xanh | Cây trong bán kính 15 ô lớn nhanh +50%; thu hoạch +40%; đất cằn vẫn trồng được |
| T2 | **Người Thì Thầm** | ⬆️ Bạn Của Thú | Tame chance ×3; thú không bao giờ hoang trở lại; huấn luyện tức thì; thú hoang không tấn công pawn này |
| T3 | **Nhịp Chậm** | 🔗 *bất kỳ perk T2 nhánh Sinh Kế* | Toàn bộ work speed ×2.5 |
| T3 | **Bậc Thầy Vạn Nghề** | 🔗 Bàn Tay Tổ | Mọi skill được tính như **level 20** cho tốc độ & phẩm chất, bất kể level thật |

---

### ⚔️ Nhánh **Chiến Trận** — Đánh nhau, phòng thủ, sống sót trong raid

| Tier | Perk | Quan hệ | Hiệu ứng |
|---|---|---|---|
| T1 | **Phản Xạ Nhanh** | — | Aiming time -25%; melee dodge +20%; reload speed +50% |
| T1 | **Gan Lì** | — | Miễn nhiễm suppression; pain shock threshold +30% |
| T1 | **Tay Súng Bền** | ⛔ `Bàn Tay Súng` | Friendly fire = 0; shooting accuracy +15% |
| T2 | **Mắt Diều Hâu** | ⬆️ Phản Xạ Nhanh · ⛔ `Bàn Tay Súng` | Shooting accuracy tối đa ở **mọi** khoảng cách; aiming time -40% |
| T2 | **Thân Thép** | ⬆️ Gan Lì · ⛔ `Thép hay Thủy Tinh` | Blunt & Sharp armor +60%; không bị stagger |
| T2 | **Siêu Cận Chiến** | 🔗 Cơ Bắp Phát Triển · ⛔ `Đường Cận Chiến` | Melee DPS +80%; melee dodge +40%; đánh trúng gây stagger |
| T2 | 🟥 **Sát Thủ Thủy Tinh** | 🔗 Phản Xạ Nhanh · ⛔ `Thép hay Thủy Tinh` | *Buff:* Sát thương tầm xa +60%, aiming -30% · *Debuff:* Nhận sát thương +100% |
| T2 | 🟥 **Cuồng Thể** | 🔗 Cơ Bắp Phát Triển · ⛔ `Đường Cận Chiến`, `Tâm Trí Nguyên Vẹn` | *Buff:* Melee DPS +120%, armor +40% · *Debuff:* Incapable of Intellectual & Social |
| T3 | **Bước Nhòe** | 🔗 Chân Nhẹ *(khác nhánh)* | Teleport tới bất kỳ ô đã thấy trên map. Cooldown 1 ngày |
| T3 | **Ánh Nhìn Đè Nén** | 🔗 Thân Thép | Kẻ địch trong bán kính 12 ô bị suppression liên tục và -30% accuracy |

---

### 7.3 Bảng Nhóm Loại Trừ

| Tag | Perk xung đột | Ý nghĩa |
|---|---|---|
| `Bàn Tay Súng` | Nông Dân Đại Địa ⟷ Tay Súng Bền ⟷ Mắt Diều Hâu | Tay quen cày cuốc thì không quen cò súng |
| `Thép hay Thủy Tinh` | Thân Thép ⟷ Sát Thủ Thủy Tinh | Chọn: chịu đòn hay ra đòn |
| `Đường Cận Chiến` | Siêu Cận Chiến ⟷ Cuồng Thể | Kỹ thuật hay bản năng |
| `Tâm Trí Nguyên Vẹn` | Cuồng Thể ⟷ Tư Duy Sắc, Giọng Nói Ấm | Cuồng Thể xóa Intellectual & Social — không thể vừa cuồng vừa khôn |

---

### 7.4 Tổng Quan Lưới

| Nhánh | T1 | T2 | T3 | Tổng |
|---|---|---|---|---|
| 🫀 Nhục Thân | 6 | 3 | 2 | 11 |
| 🧠 Tâm Trí | 4 | 4 | 2 | 10 |
| 🔨 Sinh Kế | 5 (1 trade-off) | 3 | 2 | 10 |
| ⚔️ Chiến Trận | 3 | 5 (2 trade-off) | 2 | 12 |
| **Tổng** | **18** | **15** | **8** | **41** |

Tier 3 cố tình mỏng (2 perk/nhánh): đây là đích đến, không phải danh mục mua sắm. Mỗi cái phải đáng nhớ.

**Ghi chú thiết kế:** Nhục Thân T1 phình to (6 perk) vì nó là **tầng nền của cả cây** — *Cơ Bắp Phát Triển* nuôi nhánh cận chiến bên Chiến Trận, *Chân Nhẹ* nuôi *Bước Nhòe*. Điều này tạo áp lực mềm: pawn chiến đấu vẫn phải đầu tư vào Nhục Thân trước, và giảm giá cùng nhánh khiến việc đó không hề miễn phí. Đúng tinh thần lũy tiến.

---

## 🛠️ 8. Kiến Trúc Kỹ Thuật (C#)

```
Source/EchoResonance/
├── Core/
│   ├── EchoWorldComponent.cs        // Bể điểm, save/load, tick tích lũy
│   ├── EchoAccrualTracker.cs        // Lắng nghe sự kiện pawn → cộng điểm
│   └── EchoTuning.cs                // Toàn bộ hằng số cân bằng, một chỗ
├── Buildings/
│   ├── Building_Resonator.cs        // Đài lõi: gizmo, Destroy() → wipe pool
│   ├── Building_AttunementPylon.cs  // Trụ: đóng góp hệ số
│   ├── PlaceWorker_SingleResonator.cs
│   └── PlaceWorker_PylonPlacement.cs // cap 4 + radius 12 + spacing 8
├── Perks/
│   ├── PerkDef.cs                   // tier, branch, baseCost, catalystDef, hediffDef
│   │                                // + requires / replaces / requiresAnyOfBranchTier
│   │                                // + exclusionTags
│   ├── PerkGraph.cs                 // Validate cây perk lúc load; resolve khả dụng
│   ├── CompPawnPerks.cs             // ThingComp: List<PerkDef>, tính giá lũy tiến
│   └── PerkApplier.cs               // Áp dụng qua Hediff (không sửa trực tiếp stat)
└── UI/
    └── Dialog_PawnPerks.cs          // Mở từ gizmo trên pawn
```

**Quyết định kỹ thuật quan trọng:**

1. **Perk triển khai bằng `Hediff` ẩn**, không phải Trait. Lý do: Hediff hỗ trợ `statOffsets`/`statFactors`/`capMods` đầy đủ, không đụng giới hạn 3-trait của vanilla, và save/load an toàn khi mod bị gỡ.
2. **`EchoWorldComponent` là nguồn sự thật duy nhất** cho bể điểm. `Building_Resonator.Destroy()` gọi thẳng `EchoWorldComponent.WipePool()`.
3. **Tốc độ tích lũy tính lại (cache) mỗi 250 tick**, không mỗi tick — quét Pylon là thao tác đắt.
4. **`EchoAccrualTracker` dùng Harmony postfix** trên `SkillRecord.Learn`, `Thing.SetQuality` (crafting), và `IncidentWorker_Raid` resolution. Tránh patch những thứ chạy mỗi tick.
5. **Toàn bộ số cân bằng nằm trong `EchoTuning.cs`** + expose qua Mod Settings, để người chơi tự chỉnh tốc độ theo khẩu vị chill của mình.
6. **`PerkGraph` chạy validate một lần lúc `StaticConstructorOnStartup`**: phát hiện chu trình (A replaces B replaces A), perk mồ côi, tag loại trừ chỉ có một thành viên, và perk tier thấp yêu cầu perk tier cao. Lỗi ghi ra log chứ không crash game — mod khác có thể thêm perk vào cây này.
7. **`replaces` thực thi bằng `RemoveHediff` trước rồi `AddHediff`** trong cùng một tick, để không có frame nào pawn mang cả hai hediff.

### Ví dụ XML PerkDef

```xml
<EchoResonance.PerkDef>
  <defName>ER_Tireless</defName>
  <label>Không Biết Mệt</label>
  <tier>2</tier>
  <branch>Body</branch>
  <baseCost>60</baseCost>
  <hediffDef>ER_Hediff_Tireless</hediffDef>
  <replaces>ER_LightSleeper</replaces>
</EchoResonance.PerkDef>

<EchoResonance.PerkDef>
  <defName>ER_SuperMelee</defName>
  <label>Siêu Cận Chiến</label>
  <tier>2</tier>
  <branch>War</branch>
  <baseCost>60</baseCost>
  <hediffDef>ER_Hediff_SuperMelee</hediffDef>
  <requires>
    <li>ER_MuscleGrowth</li>
  </requires>
  <exclusionTags>
    <li>MeleePath</li>
  </exclusionTags>
</EchoResonance.PerkDef>
```

---

## ✅ 9. Tóm Tắt Vòng Lặp Người Chơi

1. **Early game** — chơi bình thường, không có gì thay đổi.
2. **~Giữa game** — nghiên cứu `ResonanceCore`, xây Đài. Bắt đầu thấy Dư Ảnh nhích lên từ mọi việc colony làm.
3. **Chọn lựa** — chỉ có Tier 1. Mỗi perk là một quyết định thật: pawn nào xứng đáng?
4. **Mở rộng** — xây dần 4 Pylon, tốc độ lên ×3. Bắt đầu bọc tường quanh khu Đài.
5. **`EchoAttunement`** — mở Tier 2, dựng dây chuyền sản xuất Tinh Thể Cộng Hưởng.
6. **Khủng hoảng** — một đợt raid xuyên thủng, Đài nổ, 200 Dư Ảnh bay sạch. Perk vẫn còn. Xây lại, cày lại, lần này thủ kỹ hơn.
7. **`ArchotechResonance`** — thả cửa. Từ đây chỉ còn thời gian đứng giữa bạn và một colony toàn godpawn.
8. **End state** — chill hoàn toàn. Bạn đã trả giá bằng hàng trăm giờ; giờ là lúc hưởng.

---

## 🖥️ 10. UI Specification — `Dialog_PawnPerks`

### 10.1 Nguyên tắc nền

1. **Cây perk là bảng giá, không phải cây quyết định.** Ngoài `exclusionTags`, **không ngã rẽ nào khóa ngã rẽ nào**. Việc "phải chọn" đến từ chỗ người chơi không đủ Dư Ảnh, không đến từ chỗ game cấm. Rẽ nhánh → khóa nhánh còn lại là **phản thiết kế** ở mod này: nó dựng lại hệ class cứng, buộc người chơi tra wiki trước khi bấm nút đầu tiên.
2. **Layout sinh tự động từ `PerkGraph`.** Không hard-code toạ độ node. Mod khác phải chèn được perk vào cây mà không cần sửa UI.
3. **Cây rời rạc là đúng.** Dữ liệu có hình dạng "nhiều chuỗi ngắn", không phải mạng lưới liền mạch kiểu Path of Exile. Perk lẻ chiếm một hàng chỉ có một ô — chấp nhận, không cố nối.
4. **Zero texture riêng.** Toàn bộ vẽ bằng `Widgets.DrawMenuSection`, `DrawBoxSolid`, `DrawLineHorizontal` + `GUI.color`. Icon perk: khối màu theo nhánh, hoặc mượn `ContentFinder<Texture2D>` từ vanilla.

### 10.2 Kích thước & Layout

| Thành phần | Kích thước |
|---|---|
| Window | 1000 × 700, `absorbInputAroundWindow = true` |
| Header (portrait + số liệu) | cao 72 |
| Dải "Đã có" | cao 34 |
| Thanh tab nhánh | cao 32 |
| Vùng cây (scroll cả 2 chiều) | phần còn lại |
| Node | 150 × 56 |
| Gutter ngang (giữa 2 tier) | 40 |
| Gutter dọc (giữa 2 hàng) | 24 |
| Footer chú thích | cao 28 |

**Thuật toán layout:**

1. Lọc perk theo nhánh của tab đang mở.
2. Gom thành *weakly connected components* qua cạnh `requires` + `replaces` → mỗi component là một **hàng**.
3. Trong hàng, cột `x = tier - 1`. Nếu hai perk cùng component trùng tier thì tách thành hàng con.
4. Sắp xếp hàng: component dài nhất lên trên, perk lẻ xuống dưới.
5. Prereq nằm ở **nhánh khác** → chèn **ghost node** ở cột `tier - 1` của hàng đó.

### 10.3 Sơ đồ

```
+---------------------------------------------------------------------------------------+
| [Portrait]  Alex  ·  Perk: 2  ·  Hệ số kế tiếp ×2.56                                  |
|             Dư Ảnh: 125.4   (+2.5/ngày · Trụ ×2.5)          Tinh Thể Cộng Hưởng: 3    |
+---------------------------------------------------------------------------------------+
| Đã có:  🫀 Chân Nhẹ   🫀 Thịt Lành                        ← luôn hiện, mọi tab         |
+---------------------------------------------------------------------------------------+
| [ 🫀 Nhục Thân 2 ] [ 🧠 Tâm Trí ] [ 🔨 Sinh Kế ] [ ⚔️ Chiến Trận ]   ✓ Giảm giá 🫀    |
+---------------------------------------------------------------------------------------+
|        TIER 1  ·  20            TIER 2  ·  60 + ◈           TIER 3  ·  150            |
|                                                                                       |
|  ┌──────────────┐         ┌──────────────┐                                            |
|  │ Giấc Ngủ Nông│  ══⬆️══> │ Không Biết Mệt│         (chuỗi dừng ở T2)                 |
|  │   38 Echo    │         │  KHOÁ ⬆️      │                                            |
|  └──────────────┘         └──────────────┘                                            |
|                                                                                       |
|  ┌──────────────┐         ┌──────────────┐         ┌──────────────┐   ┌─────────────┐ |
|  │ ✓ Thịt Lành  │  ══⬆️══> │  Máu Nhanh   │  ══⬆️══> │ Máu Archotech│──🔗─>│ Bất Tử Kém │ |
|  │   ĐÃ CÓ      │         │  115 Echo ◈  │         │  THIẾU TECH  │   │ THIẾU TECH  │ |
|  └──────────────┘         └──────────────┘         └──────────────┘   └─────────────┘ |
|                                                                                       |
|  ┌──────────────┐                                                                     |
|  │ ✓ Chân Nhẹ   │      ← perk lẻ, chiếm hàng riêng, không nối đi đâu                  |
|  └──────────────┘                                                                     |
|                                                                                       |
|  ┌──────────────┐         ┌──────────────┐                                            |
|  │ ✓ Da Dày     │  ──🔗──> │  Phổi Sạch   │     🔗 = tiền đề, perk cũ Ở LẠI            |
|  └──────────────┘         │  115 Echo ◈  │     ⬆️ = nâng cấp, perk cũ BỊ NUỐT         |
|                           └──────────────┘                                            |
+---------------------------------------------------------------------------------------+
| ◈ = cần Tinh Thể Cộng Hưởng          Hover để xem công thức giá & hiệu ứng đầy đủ     |
+---------------------------------------------------------------------------------------+
```

Trong tab ⚔️ Chiến Trận — ghost node và xung đột:

```
   ┌ ─ ─ ─ ─ ─ ─ ┐         ┌──────────────┐
   ┊ Chân Nhẹ  ✓ ┊ ──🔗──> │  Bước Nhòe   │      ghost node: viền đứt, mờ 50%
   ┊ 🫀 (nhấn →) ┊         │  384 Echo    │      click → nhảy sang tab Nhục Thân
   └ ─ ─ ─ ─ ─ ─ ┘         └──────────────┘

   ┌──────────────┐   ⛔    ┌──────────────┐
   │ ✓ Thân Thép  │ ×××××× │Sát Thủ T.Tinh│      viền ĐỎ + gạch chéo
   └──────────────┘        │ XUNG ĐỘT     │      (KHÔNG phải "mờ")
                           └──────────────┘
```

### 10.4 Sáu Trạng Thái Node

| Trạng thái | Viền | Nền | Ghi chú |
|---|---|---|---|
| **Đã sở hữu** | Vàng đậm | Tối | Dấu ✓ |
| **Mua được ngay** | Màu nhánh, sáng | Hơi sáng | Hiện giá; highlight khi hover |
| **Đủ điều kiện, thiếu Echo** | Xám | Tối | Giá màu **đỏ** |
| **Khóa tiền đề** | — | Mờ 45% | Icon 🔒 · tooltip nêu rõ cần perk nào |
| **Khóa tech** | — | Mờ 45% | Icon 🔬 · tooltip nêu rõ cần research nào |
| **Xung đột** | **Đỏ** | Gạch chéo | Tooltip nêu rõ xung đột với perk nào |

Ba nhóm "khóa" **bắt buộc phải phân biệt được bằng màu**, vì cách xử lý hoàn toàn khác nhau: thiếu tiền thì chờ, thiếu tiền đề thì mua perk khác trước, xung đột thì **không bao giờ** mở được.

**Màu nhánh:** 🫀 Nhục Thân `#B4524A` · 🧠 Tâm Trí `#7A5FA8` · 🔨 Sinh Kế `#C89A3C` · ⚔️ Chiến Trận `#5A7FA0`

### 10.5 Đường Nối

| Loại | Vẽ |
|---|---|
| `replaces` ⬆️ | Đường **đôi, dày 3px**, màu nhánh, mũi tên đặc |
| `requires` 🔗 | Đường **đơn, dày 1px**, xám, mũi tên rỗng |
| `exclusionTags` ⛔ | Đường **đứt đoạn đỏ**, không mũi tên, có ⛔ ở giữa |

Phân biệt thị giác giữa ⬆️ và 🔗 là bắt buộc — người chơi phải nhìn ra ngay perk cũ sẽ bị nuốt hay được giữ, vì nó ảnh hưởng trực tiếp tới giá perk kế tiếp.

### 10.6 Tooltip — Phải Hiện Công Thức Giá

```
Máu Nhanh                                    🫀 Nhục Thân · Tier 2
──────────────────────────────────────────────────────────────────
Hồi phục vết thương ×5
Miễn nhiễm nhiễm trùng hoàn toàn

⬆️ Thay thế: Thịt Lành  (sẽ bị gỡ bỏ)
◈ Tiêu thụ: 1 Tinh Thể Cộng Hưởng

Giá:  60 gốc  ×1.60 (perk thứ 2)  ×0.75 (cùng nhánh 🫀)  =  72 Echo
      Perk này thay thế → không làm tăng bậc lũy tiến
──────────────────────────────────────────────────────────────────
Bạn có: 125.4 Echo · 3 Tinh Thể                    [ MUA ]
```

Hiện **từng thừa số**, không chỉ con số cuối. Người chơi thấy `×0.75 (cùng nhánh)` là tự hiểu cơ chế chuyên môn hóa mà không cần đọc tài liệu — đây là chỗ dạy luật chơi hiệu quả nhất trong toàn mod.

### 10.7 Quy Tắc Rõ Ràng

- **Không có nút `Reset Điểm`.** Dư Ảnh đã tiêu là mất. Nếu về sau bật tùy chọn gỡ perk, nó là nút nhỏ **trên từng node đã sở hữu** (`Gỡ · 30 Echo`, không hoàn điểm), không phải nút toàn cục ở footer.
- **Dải "Đã có" luôn hiện ở mọi tab** — vì hệ số lũy tiến và giảm giá chuyên môn đều là đại lượng xuyên nhánh, giấu đi là ép người chơi phải nhớ.
- **Mở dialog từ gizmo trên pawn**, không phải từ Đài. Đài chỉ hiện tổng quan bể điểm.
- **Xác nhận trước khi mua perk có `replaces` hoặc `exclusionTags`** — hai hành động này không thể hoàn tác.

---
*Tài liệu chốt ý tưởng cho dự án Echo Resonance.*
