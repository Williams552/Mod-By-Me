# 00 — Vision & Ranh giới dự án

**Rimward Exiles** — `william.rimwardexiles` — def prefix `RWX_`

> Tài liệu này là **trọng tài**. Khi phân vân có nên thêm một tính năng, đọc lại đây trước.
> Mọi thứ mâu thuẫn với phần "Ràng buộc cứng" đều bị loại, dù ý tưởng có hay đến đâu.

---

## 1. Mod này là gì

Một hệ thống nhân vật cho RimWorld: chuỗi custom quest chiêu mộ các **hero pawn** có
cốt truyện riêng, kèm một hệ **lòng trung thành (loyalty)** mô phỏng niềm tin cá nhân,
trí nhớ, và mâu thuẫn giữa họ với nhau.

Câu một dòng: *người chơi luôn chiêu mộ được hero — thử thách là giữ họ ở lại.*

## 2. Mod này KHÔNG phải là gì

- **Không phải quest pack.** Quest chỉ là cửa vào; nội dung chính nằm ở giai đoạn sau khi hero gia nhập.
- **Không phải overhaul.** Không thay đổi combat, economy, pathfinding, hay AI.
- **Không phải content mod cho người khác.** Dùng riêng, khoá vào modlist cá nhân.
- **Không phải framework.** Không có docs cho modder, không đóng băng schema, không có bộ content vanilla.
- **Không phải RPG hoá RimWorld.** Không có level, EXP, class tree cho pawn thường.
- **Không phải hệ thống thay thế Ideology.** Ideology vẫn lo phần cộng đồng; mod lo phần cá nhân.

---

## 3. Ràng buộc cứng

Vi phạm bất kỳ dòng nào dưới đây = thiết kế sai, làm lại.

| # | Ràng buộc | Lý do |
|---|---|---|
| R1 | Không đụng AI (JobGiver, ThinkTree, Lord) | Nguồn xung đột mod lớn nhất |
| R2 | Không patch hot path (pathfinding, tick pawn, job scan) | Hiệu năng + tương thích |
| R3 | Không đại tu hệ thống vanilla — chỉ đọc và bổ sung | Triết lý "không đập đi xây lại" |
| R4 | Mọi Harmony patch phải là postfix hoặc prefix không huỷ, trừ khi có lý do ghi rõ | Dễ gỡ, dễ debug |
| R5 | Gỡ mod giữa save không được làm vỡ save | Đã áp dụng ở Fire Discipline, giữ nguyên chuẩn |
| R6 | Khoá modlist — chấp nhận phụ thuộc cứng vào def của mod khác | Đánh đổi đã chọn khi lấy hướng snapshot-based |
| R7 | Không tạo texture/asset mới; tái dùng texture vanilla, chỉ đổi màu | Giảm phạm vi, không có năng lực art |
| R8 | `Core` không được `using` `Odyssey`; không hardcode tên hero/trục trong `Core` | Kỷ luật K1–K2, xem mục 4b |

---

## 4. Bốn nguyên tắc thiết kế

### N1 — Hero luôn lấy được

Không có quest chiêu mộ nào thất bại vì xui. Người chơi **luôn** có hero nếu chịu trả giá.
Kịch tính nằm ở **cái giá**, không ở **xác suất**.

Hệ quả: mọi quest chiêu mộ phải kết thúc bằng một lựa chọn "mất gì", không phải một roll.

### N2 — Thử thách nằm ở việc giữ

Giai đoạn sau khi gia nhập mới là nội dung chính. Hero có thể bỏ đi, và việc giữ tất cả
phải **khả thi nhưng đắt** — đắt về tài nguyên, về quan hệ faction, về sự chú ý của người chơi.

Hệ quả: mọi `HeroDecisionDef` phải có ít nhất một option "giữ tất cả" với chi phí thật.

### N3 — Random khuếch đại lỗi, không quyết định sống chết

Không có roll đơn lẻ nào giết hero hoặc kết thúc một nhánh truyện.
Random được phép quyết định **khi nào** và **cái gì** xảy ra; hậu quả phải là hàm của
trạng thái người chơi đã tạo ra trước đó.

Ba dạng random được phép:
- Random về thời điểm, tất định về nội dung
- Random chọn trong tập hữu hạn mà người chơi biết trước
- Random kích hoạt thứ đã tích tụ (weight tăng khi loyalty đã thấp)

Cấm: roll sống/chết, roll quyết định kết cục cuối chuỗi.

### N4 — Minh bạch tuyệt đối

Mọi thay đổi loyalty phải **hiện được lý do** cho người chơi. Không có hộp đen.
Nếu một factor không viết được thành một dòng label dễ hiểu, factor đó không được tồn tại.

Hệ quả: ITab hiển thị factor list nằm trong v1, không phải "làm sau".

---

## 4b. Ba kỷ luật kỹ thuật

Mod là một tác phẩm cá nhân, không phải framework. Nhưng ba kỷ luật sau vẫn áp dụng,
vì lợi ích rơi vào chính tác giả:

| # | Kỷ luật | Lợi ích trực tiếp |
|---|---|---|
| K1 | Namespace tách đôi: `Core` không được `using` `Odyssey` | Thêm hero thứ 4 không phải sửa code lõi |
| K2 | `Core` không chứa tên hero hay tên trục | Thêm trục mới chỉ cần thêm XML |
| K3 | Reaction map và hiệu ứng trục là Def | Tinh chỉnh cân bằng không cần recompile |

Chi phí: khoảng 15% thời gian phát triển.

## 4c. Kể chuyện & devlog

Dự án có một sản phẩm phụ có chủ đích: một **story RimWorld pha devlog**.

- **Ngôi kể:** một main character tự tạo đầu game làm **người quan sát**. Nhân vật này
  phải **yếu** — người bình thường, có lý do để ghi chép. Điểm nhìn của kẻ bất lực
  nhìn những kẻ mạnh cãi nhau.
- **Rủi ro:** người quan sát **có thể chết**. Hero thì không (N1). Đây là thứ duy nhất
  thật sự có rủi ro trong truyện.
- **Nhịp giải thích hệ thống:** chỉ giải thích một hệ thống **ngay sau khi nó vừa gây đau**.
  Không giải thích loyalty ở chương 1; đợi tới lúc một hero bỏ đi.
- **Tỉ lệ devlog:** 15–20%. Dưới 10% thì mất điểm khác biệt; trên 30% thì độc giả story rơi.
- **Đơn vị đăng:** mỗi hero một phần, mỗi phần hé lộ một hệ thống. Không viết một bài dài.

**Yêu cầu vận hành: `journal.md` mở sẵn khi chơi.** Ghi mỗi sự kiện một dòng kèm ngày in-game.
Chuyện hay nhất sẽ là thứ không lường trước — chơi xong 40 giờ rồi mới nhớ lại thì chỉ còn
lại cái khung, mà cái khung đã viết sẵn trong `04-content.md`.

Hệ quả sang thiết kế: **người quan sát là một nhân vật có định nghĩa**, không phải colonist
ngẫu nhiên. Cần một mục riêng trong `03-heroes.md` (dù anh ta không phải hero và
không có loyalty state).

## 5. Phạm vi v1

### Có trong v1

- Hệ loyalty đầy đủ: creed, memory, tensions, ngưỡng rời đi, phanh an toàn
- `HeroValueDef` + `HeroCreedDef` (bộ trục ở tài liệu 01)
- `HeroDecisionDef` + worker + incident queue
- Phản ứng với incident vanilla theo trục
- Phân loại `ModPath` (Steel / Flesh / Purity)
- Snapshot pawn loader + validate
- 3 hero đầu tiên, mỗi hero một chuỗi 3 tầng
- Ma trận quan hệ giữa 3 hero đó
- ITab hiển thị loyalty + factor list
- Debug actions

### Để sau v1

- Path psycast/aura riêng cho Purity hero (thiết kế xong nhưng implement sau)
- Hero thứ 4+
- Hero rời đi rồi quay lại trong raid
- Hero thay đổi creed vĩnh viễn qua sự kiện
- World object làm điểm vào bền vững (v1 dùng letter nhắc lại là đủ)

### Không bao giờ làm

- Cutscene, hội thoại cây
- Hero có stat vượt trội đến mức thay thế cả colony
- Nội dung phụ thuộc mod ngoài modlist cá nhân
- Framework công khai kèm docs cho modder (xem 4b)
- Bộ content "vanilla" mà tác giả không chơi
- **Tích hợp Anomaly.** DLC này đổi thái độ nhận thức của game (sci-fi có giải thích → horror
  cố tình không giải thích) và đổi vai trò người chơi (kẻ quản lý → kẻ chịu đựng). Monolith
  còn là một cốt truyện tuyến tính tranh chỗ với chuỗi hero. Không dùng, kể cả ở mức
  "bật anomaly không bật monolith".

---

## 6. Tiêu chí "xong"

v1 được coi là hoàn thành khi:

1. Chơi 60 ngày in-game với 3 hero, không có red error
2. Ít nhất một hero bỏ đi trong một lần test, và người chơi hiểu được tại sao chỉ bằng ITab
3. Ít nhất một decision buộc phải chọn phe, và option "giữ tất cả" thật sự đắt
4. Gỡ mod giữa save → load lại được, colony không vỡ
5. Đổi một mod trong modlist → tool validate báo rõ def nào hỏng, không crash im lặng

---

## 7. Nhật ký quyết định

Ghi lại các quyết định đã chốt và lý do, để không phải tranh luận lại với chính mình.

| Ngày | Quyết định | Lý do |
|---|---|---|
| — | Snapshot-based qua Scribe | Mod dùng riêng, không cần portable; giữ pawn giống hệt 100% |
| — | Không coi implant EvolvedOrgansRedux là "tự nhiên" | Purist ghê tởm việc con người bị sửa đổi, không phải ghê tởm kim loại |
| — | Tách `HeroCreed` khỏi Ideology | Ideology là hệ cộng đồng, không có trọng số liên tục, trí nhớ cá nhân, hay mâu thuẫn nội tâm |
| — | Storyteller chỉ làm gia vị, không quyết định cốt truyện | Cốt truyện phải tất định; storyteller lo nhịp nền |
| — | Mô hình 3 trục thân thể (Steel/Flesh/Purity) | Steel và Flesh cũng xung đột nhau → tam giác giàu hơn |
| — | Tác phẩm cá nhân, giữ 3 kỷ luật kỹ thuật | Framework tác giả không chơi thì chết vì thiếu phản hồi thật; kỷ luật vẫn có lợi cho chính mình |
| — | Story RimWorld pha devlog, ngôi kể là người quan sát yếu | Điểm nhìn cố định giải bài toán chuyển ngôi của devlog; người quan sát là thứ duy nhất có rủi ro thật |
| — | Loyalty trôi dần, hệ số 0.08 | Tạo quán tính, cho người chơi thời gian phản ứng và thấy được xu hướng |
| — | Loyalty không buff stat | Tránh vòng lặp thắng-thắng biến mọi thất bại thành xoáy ốc tử thần |
| — | Psycast/aura cho Purity đẩy sang v2 | Phạm vi v1 đã đủ lớn; Purity hero vẫn chơi được không cần nó |
| — | Thêm trục thứ tư `RWX_BloodPath` cho Biotech | Gene không tháo được, áp đặt lên người khác được, và di truyền — ba tính chất đạo đức mà `FleshPath` không mô tả nổi |
| — | Không tích hợp Anomaly | Lệch tông với phần còn lại của game; monolith tranh chỗ cốt truyện với chuỗi hero |
