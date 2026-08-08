# Mod Definition — RimWorld Combat Tactical Layer

> Tên: **Fire Discipline** *(đã chốt)*
> `packageId`: chốt ngay từ commit đầu (vd. `<tacgia>.firediscipline`). Đổi tên hiển thị lúc nào cũng được; **đổi packageId là gãy mọi save và mọi modlist đã có.**
> Trạng thái: bản định nghĩa v1, chưa viết code

---

## 1. Một câu định nghĩa

> Một **lớp chiến thuật** bổ sung cho combat RimWorld, tạo ra lý do để pawn di chuyển và cho người chơi quyền quyết định trong từng pha bắn — **không viết lại** hệ thống combat, không yêu cầu save mới, không đòi patch riêng cho từng mod vũ khí.

Mô tả bán hàng (dùng cho Steam Workshop):

> A tactical layer for RimWorld combat. No new save required. Works standalone, integrates automatically with Yayo's Combat 3 and Suppression.

Ba mệnh đề này trả lời đúng ba câu hỏi đầu tiên của mọi người đọc: *có phải CE không / có phải chơi save mới không / có phá modlist của tôi không.*

---

## 2. Vấn đề đang giải quyết

Combat vanilla có ba khiếm khuyết thiết kế, xếp theo mức độ nghiêm trọng:

| # | Vấn đề | Hệ quả |
|---|--------|--------|
| 1 | Cover chỉ là modifier %, không có suppression | Không có lý do để cơ động; đấu súng = đua DPS thuần |
| 2 | Người chơi không có hành động nào ảnh hưởng một pha bắn | Chỉ có xác suất, không có counterplay → save-scum |
| 3 | Không có hậu cần/sức mang | Vũ khí chỉ khác nhau ở DPS/tầm → luôn có "vũ khí tối ưu duy nhất" |

**Không nằm trong phạm vi:** AI ngu, killbox meta, tính swingy của việc pawn gục vì một viên may mắn (chỉ giảm nhẹ, không xử lý triệt để).

---

## 3. Nguyên tắc kiến trúc (ràng buộc cứng)

Đây là phần quan trọng hơn mọi tính năng. Mỗi dòng dưới đây là một quy tắc **không được vi phạm**:

1. **Không thay class.** Không set `verbClass`, `thingClass`, `projectile.thingClass`, không thay `DamageWorker`. Chỉ Harmony postfix lên hàm tính toán.
2. **Suy ra, đừng khai báo.** Mọi giá trị cho vũ khí/giáp của mod khác phải được **tính từ stat vanilla đã có** (Mass, techLevel, damage, range, armor rating). Nếu mod cần một file patch cho mỗi mod vũ khí ngoài kia → thiết kế sai.
3. **Cộng thêm bằng Hediff / Comp / StatPart**, không sửa Def gốc. Gỡ mod không được hỏng save.
4. **Đăng ký Harmony thủ công, KHÔNG dùng `PatchAll()`.** Feature tắt thì patch không tồn tại. Tắt hết feature → mod gần như trong suốt với game.
5. **Transpiler là nợ kỹ thuật.** Ưu tiên postfix ngay cả khi thiết kế kém "sạch" hơn. Mỗi transpiler là một lời hứa rằng IL của Ludeon không đổi.
6. **Không chạm pathfinding / ThinkTree / JobGiver.** (CAI 5000 chết ở 1.6 vì đúng lý do này.)
7. **Không hard dependency.** Chạy độc lập, tự phối hợp khi phát hiện mod khác.

---

## 4. Kiến trúc: một mod, nhiều module

**Quyết định: một mod duy nhất, một assembly, không mod phụ.**

Lý do không làm mod patch thuần:
- Thừa kế cái chết của upstream (upstream không lên 1.7 → mod chết theo trong cùng tuần)
- Khoảng trống lớn nhất (aim mode) hiện không có upstream để patch
- Hard dependency là một khoản thuế lên tỉ lệ cài đặt
- 3 mod upstream = 7 tổ hợp phải hỗ trợ; gộp một assembly thì chỉ còn các nhánh điều kiện

Mỗi feature là một class có `ShouldEnable()`, đọc từ mod settings + `ModsConfig.IsActive`.

Chỉ tách mod riêng khi thoả ít nhất một trong ba: có khán giả riêng biệt / nặng về Def & asset / phá save khi gỡ.

---

## 5. Các module

### 5.1 Suppression — *ưu tiên cao nhất, rủi ro thấp nhất*

Giải quyết vấn đề #1. Thiết kế như **hiệu ứng stat**, không phải hành vi → không cần đụng AI.

- **Hook:** postfix `Projectile.Tick` hoặc `CheckForFreeIntercept` — đạn bay qua gần ô nào thì cộng suppression cho pawn ở đó
- **Hiệu ứng:** qua `HediffStage` — tăng warmup, giảm ShootingAccuracyPawn, giảm move speed
- **Điểm mấu chốt:** cover làm **giảm tốc độ tích suppression**, không chỉ giảm % trúng
- **Vì sao tương thích:** bám vào *projectile*, không bám vào *weapon* → mọi súng của mọi mod tự động hoạt động

**Lưu ý quan trọng:** mod *Suppression (Continued)* (Mlie, đã lên 1.6) đã làm gần đúng kiến trúc này. → **Mặc định tắt module nội bộ nếu phát hiện mod đó**, chuyển sang chế độ *bổ sung*: cho cover và stance tương tác với hediff của họ. Không có mod đó thì bật bản nội bộ.

### 5.2 Aim mode / Stance

Giải quyết vấn đề #2. Gizmo toggle trên pawn đã draft:

| Mode | Hiệu ứng |
|------|----------|
| Ngắm kỹ | warmup ×2, accuracy ×1.5 |
| Bắn nhanh | mặc định |
| Nằm nấp | +cover, −accuracy, −move |

- **Hook:** patch chỗ tính warmup ticks và hit chance factor. **Không thay Verb class.**
- **Tham chiếu:** *Yayo's Shooting 2* đã làm phần này (Aimed Fire +30% accuracy/range/warmup, Suppressive Fire ×3 shots −80% accuracy). Tác giả **công khai mời fork**, có source code, và **tự thừa nhận có bug ở tầng cấu trúc** → biết trước cần viết lại chỗ nào. Chưa có bản 1.6 nào còn sống → khoảng trống rõ nhất.
- **Việc cần làm:** kiểm tra license trước khi fork.

### 5.3 Encumbrance

Giải quyết vấn đề #3 **không cần hệ thống inventory mới**.

- Vanilla đã có `MassCarried` (dùng cho caravan) nhưng không ảnh hưởng gì trong combat
- **Hook:** inject một `StatPart` vào MoveSpeed theo khối lượng mang
- Ghép chồng rất tốt lên hệ ammo của Yayo's Combat 3
- Chưa tìm thấy mod nào còn được duy trì ở 1.6 → mảnh sạch nhất

### 5.4 Graze / trúng sượt

Chuyển một phần "trúng đủ" thành "trúng sượt vào chi, sát thương giảm". Postfix ở tầng armor/damage. Giảm tính swingy mà không viết lại damage. **Cẩn thận cân bằng.**

### 5.5 Shock

Đồng đội gục gần đó → hediff giảm accuracy tạm thời. Rẻ, hợp chất "story generator" của RimWorld.

---

## 6. Tích hợp với hệ sinh thái

| Mod | Trạng thái | Cách xử lý |
|-----|-----------|------------|
| Yayo's Combat 3 (Continued) — Mlie | Sống, 1.6 | Để nguyên mảng ammo/armor/accuracy cho họ. **Không ôm ammo** — phá save khi gỡ |
| Suppression (Continued) — Mlie | Sống, 1.6 | Tắt module nội bộ, chuyển sang chế độ bổ sung |
| Yayo's Shooting 2 | Chết, tác giả mời fork | Nguồn tham chiếu cho module 5.2 |
| Yayo's Combat 3 – Addon (Syrus) | — | Kiểm tra xung đột |
| Melee Animation, Run and Gun, Simple Sidearms | Sống | Không chồng lấn, chỉ cần test |

XML dùng `MayRequire` cho các patch có điều kiện, **không tách thành mod phụ**.

---

## 7. Rủi ro & cách né

| Rủi ro | Xử lý |
|--------|-------|
| **Hiệu năng** — suppression tính theo từng viên đạn từng tick là chỗ dễ giết FPS nhất (CAI 5000 bị chê nặng về đúng khoản này) | Cache theo vùng, tick thưa (15–30 tick), bỏ qua pawn ngoài tầm |
| **Xung đột hediff kép** với Suppression (Continued) | Runtime detection, mặc định nhường upstream |
| **Vỡ khi lên DLC mới** | Ít transpiler; không chạm pathfinding |
| **Bị hiểu nhầm là CE thứ hai** | Tên + mô tả nhấn mạnh "no new save required" |

---

## 8. Lộ trình đề xuất

1. **Encumbrance** — nhỏ, độc lập, dễ kiểm thử, không đụng ai. Dùng để dựng khung mod settings + hệ đăng ký Harmony thủ công.
2. **Aim mode / Stance** — giá trị người dùng cao nhất, có codebase tham chiếu, khoảng trống 1.6 rõ ràng.
3. **Suppression integration** — lớp mỏng cho cover + stance tương tác với suppression. Cân nhắc contribute thẳng vào repo của Mlie thay vì tự làm.
4. **Graze / Shock** — chỉ làm sau khi 1–3 đã ổn định qua một chu kỳ phản hồi.

---

## 9. Modlist tham chiếu & load order (1.6)

Đây vừa là cấu hình "đủ trải nghiệm" để chơi, vừa là **test bed** của mod. Nếu Fire Discipline chạy sạch ở đây thì đã vượt qua phần lớn kịch bản thực tế. **Nên dựng modlist này trước khi viết dòng code đầu tiên** để có baseline so sánh.

### Load order

```
Core → DLC → HugsLib
Smarter Raider AI          (hoặc CAI 5000 / SmartRaider — CHỌN ĐÚNG MỘT)
Yayo's Combat 3 (Continued)
Suppression (Continued)
Fire Discipline            ← mod của mình
Melee Animation
Simple Sidearms / Run and Gun / Achtung!
```

**Vì sao Fire Discipline load sau Yayo và Suppression:** runtime detection bằng `ModsConfig.IsActive` không phụ thuộc thứ tự load, nhưng các patch XML dùng `MayRequire` thì có. Load sau là an toàn cho cả hai cơ chế.

### Các tầng

| Tầng | Mod | Trạng thái 1.6 | Vai trò |
|---|---|---|---|
| 1. Nền cơ chế bắn | Yayo's Combat 3 (Continued) — Mlie | ✓ xác minh | Ammo, armor pen, accuracy |
| | **Fire Discipline** | — | Aim mode, stance, encumbrance, graze |
| | Suppression (Continued) — Mlie | ✓ xác minh | Suppression hediff |
| 2. AI | Smarter Raider AI | ✓ 1.4–1.6 | Nhẹ nhất; mở rộng avoid grid vanilla sang cả pawn đã draft |
| | CAI 5000 | ⚠ cần kiểm tra | Nặng hơn; tactical pathfinding, raytracing tầm nhìn, đa luồng |
| | SmartRaider | ⚠ mới, ít kiểm chứng | Thêm khói che tiến quân, EMP vô hiệu turret |
| 3. Cận chiến | Melee Animation | ✓ 1.4–1.6 | Animation, execution, duel, lasso |
| 4. Giảm micro | Simple Sidearms · Run and Gun · Achtung! / Tactical Groups | ⚠ chưa xác minh | Đổi vũ khí theo cự ly, bắn khi di chuyển, điều khiển đội hình |

### Xung đột phải nhớ

- **Chỉ một mod AI.** Cả ba đều can thiệp avoid grid và pathfinding raid. Đây là lỗi ghép modlist phổ biến nhất.
- **Melee Animation**: xung đột mềm với Dual Wield (chỉ hiện một vũ khí); không chạy cùng RimThreaded vì đã có đa luồng sẵn.
- **Yayo's Combat 3 – Addon (Syrus)**: có báo cáo gần đây gây lỗi ammo không hiện. Bỏ ra nếu gặp.
- **RimWorld of Magic**: nhiều mod combat nằm trong danh sách xung đột của nó.
- **Fire Discipline**: tự tắt module suppression nội bộ khi phát hiện Suppression (Continued) — xem mục 5.1.

> Các trạng thái ⚠ cần tự kiểm tra lại trên Steam Workshop. Trang mirror thường lệch phiên bản.

---

## 10. Tiêu chí thành công

Không phải số lượt subscribe. Là:

> **Mod sống được qua ít nhất hai bản DLC mà không cần viết lại.**

Nỗi đau của cộng đồng RimWorld không phải là thiếu ý tưởng combat — mà là các mod hay đều chết theo tác giả.

---

## 11. Tài liệu tham khảo

### A. Mod nên tải về đọc source

Xếp theo thứ tự ưu tiên đọc:

| # | Mod | Nguồn | Vì sao đọc |
|---|-----|-------|-----------|
| 1 | **Yayo's Shooting 2** | [Workshop 2020785943](https://steamcommunity.com/sharedfiles/filedetails/?id=2020785943) | Nền trực tiếp cho module 5.2. Source đi kèm, tác giả **công khai mời fork**. Đọc cả phần "bug cấu trúc" mà tác giả tự thừa nhận |
| 2 | **Suppression (Continued)** | [GitHub emipa606/Suppression](https://github.com/emipa606/Suppression) · [Workshop 2559826227](https://steamcommunity.com/sharedfiles/filedetails/?id=2559826227) | Tham chiếu trực tiếp cho 5.1. Xem cách họ hook projectile và cấu trúc hediff |
| 3 | **Yayo's Combat 3 (Continued)** | [GitHub emipa606/YayosCombat3](https://github.com/emipa606/YayosCombat3) · [Workshop 2854006492](https://steamcommunity.com/sharedfiles/filedetails/?id=2854006492) | Bài học "suy ra, đừng khai báo" — cách sinh ammo/armor cho vũ khí mod khác mà không cần patch riêng |
| 4 | **Combat Extended** | [GitHub CombatExtended-Continued](https://github.com/CombatExtended-Continued/CombatExtended) · [Workshop 2890901044](https://steamcommunity.com/workshop/filedetails/?id=2890901044) | Đọc để biết cái gì **không** nên làm. Đồng thời là tham chiếu tốt nhất về mô hình ballistics và deflection-based armor |
| 5 | **Smarter Raider AI** | [Workshop 2945497357](https://steamcommunity.com/sharedfiles/filedetails/?id=2945497357) | Ranh giới tầng AI — biết họ chạm gì để mình chắc chắn không chạm |
| 6 | **SmartRaider** | [Workshop 3662126834](https://steamcommunity.com/sharedfiles/filedetails/?id=3662126834) | Cách tiếp cận AI mới hơn, so sánh với mục trên |
| 7 | **Melee Animation** | [Workshop 2944488802](https://steamcommunity.com/sharedfiles/filedetails/?id=2944488802) | Ví dụ mod lớn nhưng tương thích tốt; tham khảo cách tổ chức mod settings ("mọi thứ chỉnh được") |

Bản gốc để đối chiếu lịch sử: [Yayo's Combat 3 gốc](https://steamcommunity.com/sharedfiles/filedetails/?id=2038409475) · [Suppression gốc của YAYO](https://steamcommunity.com/sharedfiles/filedetails/?id=2016866580) · [Suppression nguyên thuỷ của D A](https://steamcommunity.com/sharedfiles/filedetails/?id=1421919369)

### B. Tài liệu kỹ thuật

- **RimWorld Wiki — Modding Tutorials** (`rimworldwiki.com`): điểm khởi đầu cho cấu trúc mod, XML patching, `MayRequire`
- **Harmony** (`harmony.pardeike.net`): đọc kỹ phần **patch order & priority** và sự khác biệt prefix / postfix / transpiler — đây là trọng tâm của nguyên tắc kiến trúc #1 và #5
- **ILSpy** hoặc **dnSpy**: decompile `Assembly-CSharp.dll` trong thư mục game. RimWorld **không có API docs chính thức** — decompiler là nguồn sự thật duy nhất
- **RimWorld Official Discord**, kênh mod-development: nơi hỏi nhanh nhất, và cũng là nơi tác giả Yayo's Shooting 2 mời liên hệ
- **Ludeon Forums — Mods (Help)**: lưu trữ nhiều thảo luận kỹ thuật cũ nhưng vẫn đúng

### C. Thiết kế & bối cảnh

- **Tynan Sylvester — *Designing Games***: giải thích triết lý "storyteller, not skill test". Đọc để hiểu vì sao combat vanilla được thiết kế như vậy, trước khi quyết định đi ngược lại nó
- **Các thread CE compatibility trên Workshop**: dữ liệu thực tế về việc mod gãy như thế nào và vì sao — nguồn tốt nhất để rút ra danh sách ràng buộc ở mục 3

### D. Bản quyền — kiểm tra trước khi copy code

- **Yayo's Shooting 2**: tác giả tuyên bố công khai cho phép lấy về sửa và làm biến thể. Vẫn nên lưu lại ảnh chụp tuyên bố đó
- **Yayo's Combat 3**: tác giả cho phép upload bản mới và uỷ quyền tiếp tục phát triển
- **Repo của emipa606**: đọc file LICENSE trong từng repo, không suy đoán
- **Combat Extended**: phát hành dưới giấy phép Creative Commons — đọc kỹ điều khoản attribution trước khi tham khảo code

