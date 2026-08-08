# Fire Discipline — Knowledge Base Index (Mod Tham Chiếu)

Thư mục này lưu trữ phân tích kiến trúc, danh sách API, lỗi cấu trúc (Structural Bugs) cần tránh và chiến lược tích hợp của các mod tham chiếu đối me đối với **Fire Discipline** (`william.firediscipline`).

---

## 📚 Danh Mục Tài Liệu Knowledge Base

| # | Tài liệu | Mod Tham Chiếu | Mục Đích Phân Tích & Kết Luận |
|---|---|---|---|
| 1 | [yayos_shooting_2.md](file:///d:/Games/Rimworld/Mod%20By%20Me/docs/knowledge_base/yayos_shooting_2.md) | **Yayo's Shooting 2** (Workshop 2020785943) | **Nền tảng cho Module 5.2 (Aim Mode & Stance)**. Phân tích nguyên nhân lỗi gãy cấu trúc (do thay `verbClass` & thêm `VerbProperties` trực tiếp vào `ThingDef`). Khẳng định giải pháp Postfix của Fire Discipline. |
| 2 | [suppression_continued.md](file:///d:/Games/Rimworld/Mod%20By%20Me/docs/knowledge_base/suppression_continued.md) | **Suppression (Continued)** (Mlie - Workshop 2559826227) | **Nền tảng cho Module 5.1 (Suppression Integration)**. Phân tích cấu trúc Hediff áp chế, cách hook đạn bay và cơ chế nhường quyền khi phát hiện `Mlie.Suppression`. |
| 3 | [yayos_combat_3.md](file:///d:/Games/Rimworld/Mod%20By%20Me/docs/knowledge_base/yayos_combat_3.md) | **Yayo's Combat 3 (Continued)** (Mlie - Workshop 2854006492) | **Bài học "Suy ra, đừng khai báo"**. Phân tích cách tự tính toán stat phụ thuộc từ vũ khí/giáp gốc của mod khác mà không cần viết file patch riêng. |
| 4 | [smarter_raider_ai.md](file:///d:/Games/Rimworld/Mod%20By%20Me/docs/knowledge_base/smarter_raider_ai.md) | **Smarter Raider AI** (Workshop 2945497357) | **Ranh giới tầng AI**. Xác định các hàm pathfinding / `AvoidGrid` mà Fire Discipline **tuyệt đối không chạm vào** để né xung đột lag FPS. |

---

## 🎯 Nguyên Tắc Đóng Góp Kiến Thức (KI Principles)
1. **Kiểm chứng trên Mã Nguồn Thực tế:** Mọi phân tích dựa trên source code thực tế đã kiểm tra tại `d:\Games\Rimworld\Mod By Me\`.
2. **Không phỏng đoán:** Trích dẫn tên class, tên method và file nguồn cụ thể.
3. **Độ bền vững:** Ghi chú rõ điểm yếu của từng mod upstream để Fire Discipline không đi lại vết xe đổ.
