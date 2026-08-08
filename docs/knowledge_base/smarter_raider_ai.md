# Knowledge Base — Smarter Raider AI Boundary Analysis

> Mod tham chiếu: **Smarter Raider AI** (Steam Workshop ID: `2945497357`)
> Thư mục nguồn tại chỗ: `Smarter Raider AI/`

---

## 1. Phân Tích Phạm Vi Can Thiệp Của Smarter Raider AI

Smarter Raider AI tập trung cải thiện hành vi đột kích (Raid AI) của kẻ địch:
- Mở rộng bản đồ né tránh (`AvoidGrid`) của RimWorld vanilla sang cho cả các Pawn đã Drafted.
- Tự động tìm đường né các ô bị bao phủ bởi tầm bắn của Turret/Pawn phòng thủ mà không làm nặng máy.

---

## 2. Ranh Giới Kiến Trúc Ràng Buộc Cứng (Principle #6)

Tài liệu thiết kế [rimworld-combat-mod-definition.md](file:///d:/Games/Rimworld/Mod%20By%20Me/docs/rimworld-combat-mod-definition.md) quy định rõ:
> **"Không chạm Pathfinding / ThinkTree / JobGiver."**

### Lý do:
- Các mod như CAI 5000 hay mod AI khác bị phàn nàn nhiều nhất về việc tụt giảm FPS nghiêm trọng khi trận đánh lớn diễn ra (do tính toán pathfinding đa luồng/raytracing quá nặng).
- Việc can thiệp vào `ThinkTree` hoặc `JobGiver` rất dễ gây xung đột làm AI đứng yên ("freeze") khi chơi chung với Smarter Raider AI hoặc các mod AI khác.

---

## 3. Tương Tác Giữa Fire Discipline và Mod AI

Fire Discipline can thiệp combat dưới dạng **Hiệu ứng Stat & Tư thế (Stat & Stance Layer)**:
- **Tư thế Prone/Cover:** Thêm chỉ số Cover modifier và giảm tốc độ qua `StatPart` / Harmony Postfix.
- **Suppression:** Thêm Hediff giảm accuracy / move speed.
- **Kết quả:** AI của Smarter Raider AI tự động coi các Pawn bị áp chế / bị phạt move speed là mục tiêu di chuyển chậm mà **không cần Fire Discipline phải viết một dòng code pathfinding nào**.
