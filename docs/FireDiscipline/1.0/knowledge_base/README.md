# Knowledge Base — Mod tham chiếu

Phân tích kiến trúc, API, lỗi cấu trúc cần tránh, và điều Fire Discipline học được từ
bốn mod tham chiếu. Nguồn tại chỗ: `Reference Mods/`.

---

| Tài liệu | Mod | Học được gì |
|---|---|---|
| [`yayos_shooting_2.md`](yayos_shooting_2.md) | **Yayo's Shooting 2** (`2020785943`) | Vì sao **không** thay `verbClass` và không thêm `VerbProperties` thẳng vào `ThingDef`. Đây là nguồn gốc của luật 1 và luật 8 |
| [`suppression_continued.md`](suppression_continued.md) | **Suppression (Continued)** (Mlie, `2559826227`) | Cấu trúc Hediff áp chế, thang 0–9, và **hằng số thật đọc từ DLL**. Họ đã thử hạ `MoveSpeed` rồi phải thêm sàn — Fire Discipline lấy lại sàn đó |
| [`yayos_combat_3.md`](yayos_combat_3.md) | **Yayo's Combat 3 (Continued)** (Mlie, `2854006492`) | Bài học **"suy ra, đừng khai báo"** — tính stat phụ thuộc từ vũ khí/giáp gốc của mod khác mà không cần patch riêng. Nguồn gốc của luật 2 |
| [`smarter_raider_ai.md`](smarter_raider_ai.md) | **Smarter Raider AI** (`2945497357`) | Ranh giới tầng AI: các hàm pathfinding / `AvoidGrid` **tuyệt đối không chạm**. Nguồn gốc của luật 5 |

---

## Nguyên tắc

1. **Kiểm chứng trên mã nguồn thật**, không phải trên mô tả Workshop. Vài kết luận trong
   thư mục này đến từ đọc IL của DLL, không phải từ tài liệu của tác giả.
2. **Không phỏng đoán.** Trích tên class, tên method, tên file cụ thể.
3. **Ghi rõ điểm yếu của mod upstream** để không đi lại vết xe đổ — và ghi cả chỗ họ
   thử rồi bỏ, vì đó là dữ liệu đắt nhất.

> ⚠ `suppression_continued.md` từng mô tả một cơ chế tích hợp hai chế độ đã bị bỏ.
> Đã sửa. Xem [`../lessons-and-wrong-turns.md`](../lessons-and-wrong-turns.md) §2.4.
