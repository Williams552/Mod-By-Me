# Fire Discipline — Tài liệu

Mod RimWorld 1.6 thêm một **lớp chiến thuật** lên combat vanilla: tư thế tác chiến,
suppression, cover, encumbrance, graze, shell shock. Không viết lại combat, không
yêu cầu save mới, không patch riêng cho từng mod vũ khí.

---

## Tài liệu nóng — đọc khi phát triển tiếp

| File | Dùng khi |
|---|---|
| [`architecture.md`](architecture.md) | **Đọc đầu tiên.** 10 luật bất di bất dịch, 6 module, trục suppression, bất biến quét được bằng lệnh, định nghĩa "xong" |
| [`1.1-roadmap.md`](1.1-roadmap.md) | Việc còn lại của 1.0, Đợt B, quyết định đang treo, nợ kỹ thuật, ngưỡng tune |
| [`ilspy-findings.md`](ilspy-findings.md) | Câu hỏi về engine RimWorld — cái nào đã trả lời, cái nào còn mở cho B1/B6 |

Ba file này mô tả **trạng thái hiện tại của code**. Nếu chúng lệch với code, code đúng
và tài liệu sai — sửa tài liệu, đừng sửa code cho khớp.

---

## Lưu trữ 1.0 — đọc khi cần truy vấn sâu

[`1.0/`](1.0/) chứa toàn bộ quá trình xây dựng 1.0. Xem [`1.0/README.md`](1.0/README.md).

Vào đó khi cần biết **vì sao** một quyết định được đưa ra, hoặc khi định làm lại
một thứ có vẻ hiển nhiên — nhiều thứ hiển nhiên trong đó đã được thử và bỏ.

Điểm vào đáng giá nhất: [`1.0/lessons-and-wrong-turns.md`](1.0/lessons-and-wrong-turns.md).

---

## Quy ước

- **Tài liệu tiếng Việt. Code, comment, commit, chuỗi UI tiếng Anh.**
- Số liệu trong tài liệu phải là **đo được**, không phải ước lượng. Chỗ nào là ước
  lượng thì ghi rõ.
- Đổi hằng số cân bằng thì **ghi lại giá trị cũ** — bảng ở `1.0/master-design.md` §8.1.
