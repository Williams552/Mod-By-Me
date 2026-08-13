# Lưu trữ 1.0

Tài liệu của quá trình xây dựng phiên bản 1.0. **Truy vấn sâu, không phải tham chiếu nhanh.**

Trạng thái hiện tại của code nằm ở [`../architecture.md`](../architecture.md), không phải ở đây.

---

## Đọc theo mục đích

| Bạn cần biết | Đọc |
|---|---|
| Một thứ có vẻ hiển nhiên — đã ai thử chưa? | [`lessons-and-wrong-turns.md`](lessons-and-wrong-turns.md) |
| Vì sao một tính năng được thiết kế như vậy | [`master-design.md`](master-design.md) |
| Giá trị cân bằng hiện tại và **giá trị cũ** đã đổi | [`master-design.md`](master-design.md) §8.1 |
| Chỉ tiêu pass/fail phải chạy trước phát hành | [`master-design.md`](master-design.md) §7.3 |
| Code từng lệch tài liệu ra sao | [`reality-report-2026-08-05.md`](reality-report-2026-08-05.md) |
| Mod tham chiếu làm gì và ta học được gì | [`knowledge_base/`](knowledge_base/) |

---

## Danh mục

| File | Là gì | Còn đúng không |
|---|---|---|
| [`lessons-and-wrong-turns.md`](lessons-and-wrong-turns.md) | **17 bug production, 6 hướng đi sai, 9 lần tài liệu nói dối, số liệu đã đo** | ✅ Viết sau khi 1.0 xong, đối chiếu với code cuối |
| [`master-design.md`](master-design.md) | Tài liệu thiết kế đầy đủ | ⚠️ Là **ý định thiết kế**, không phải trạng thái code. Chỗ lệch đã đánh dấu tại chỗ |
| [`reality-report-2026-08-05.md`](reality-report-2026-08-05.md) | Bản kiểm toán đối chiếu tài liệu với code, ngày 2026-08-05 | ⚠️ **Ảnh chụp lịch sử.** Mọi vấn đề nó nêu đã được xử lý. Giữ vì nó là bằng chứng cho việc tài liệu trôi khỏi code nhanh thế nào |
| [`v3-execution-spec.md`](v3-execution-spec.md) | Đặc tả thực thi v3 | ⚠️ Lịch sử. Đã thực thi hoặc đã bị quyết định sau ghi đè |
| [`original-definition.md`](original-definition.md) | Bản định nghĩa mod đầu tiên | ⚠️ Lịch sử. Đã được `master-design.md` §1–3 thay thế |
| [`test-suite.md`](test-suite.md) | Kịch bản test ban đầu | ⚠️ Test case 3 mô tả cơ chế dò mod ngoài **đã bị bỏ**. Bộ test thật là `master-design.md` §7.3 |
| [`rejected-rts-command-layer.md`](rejected-rts-command-layer.md) | Overwatch, Attack-Move, Fireteams, Suppressing Area Fire | ❌ **Đã chốt KHÔNG làm.** Giữ để không ai đề xuất lại từ đầu |
| [`knowledge_base/`](knowledge_base/) | Phân tích 4 mod tham chiếu | ⚠️ Xem cảnh báo trong từng file |
| `Player.log` | Log game ngày 2026-08-05 | Bằng chứng đi kèm reality report |

---

## Vì sao giữ tài liệu đã lỗi thời

Ba file được đánh dấu ⚠️ *"lịch sử"* vẫn nằm đây thay vì bị xoá, vì chúng ghi lại
**cái đã được cân nhắc và bỏ**. Xoá đi thì lần sau sẽ có người đề xuất lại đúng thứ
đó, và không ai nhớ vì sao nó bị bỏ.

Thứ bị xoá hẳn là tài liệu vừa **lỗi thời vừa trùng lặp** — `project_handoff.md` và
`CLAUDE-CODE-HANDOFF.md`. Cả hai mô tả module đã bị xoá khỏi code và trạng thái đã sai
hoàn toàn, mà không mang thông tin nào không có ở nơi khác. Chúng vẫn nằm trong git
history nếu cần tra.
