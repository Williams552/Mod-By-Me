# Fire Discipline — Acceptance Criteria & Test Suite

Tài liệu quy định tiêu chuẩn chấp nhận (Acceptance Criteria) và danh sách các kịch bản kiểm thử (Test Cases) cho Mod **Fire Discipline** (`packageId`: `william.firediscipline`).

---

## 🎯 1. Tiêu Chuẩn Chấp Nhận (Acceptance Criteria / Definition of Done)

Mod được coi là **hoàn thành và đạt chất lượng** khi thỏa mãn tất cả 5 điều kiện cứng sau:

1. **An toàn cho File Save:** Tháo mod ra khỏi game bất kỳ lúc nào **không làm gãy file Save** và không để lại class lỗi trong Save Data.
2. **Không ghi đè Def gốc / Không thay Class:** Tuyệt đối không sửa `verbClass`, `thingClass`, `projectile.thingClass` hay `DamageWorker`. Chỉ sử dụng Harmony Postfix và Dynamic StatPart.
3. **Hiệu năng FPS:** Không làm sụt giảm FPS trong các trận đánh đông quân (không chạm Pathfinding, ThinkTree hay JobGiver).
4. **Tự động thích ứng hệ sinh thái (Runtime Detection):** Tự nhường quyền cho mod *Suppression (Continued)* khi phát hiện mod đó đang active mà không cần patch phụ.
5. **Biên dịch Sạch:** Mã nguồn C# biên dịch thành công `0 Errors` trên RimWorld 1.6.

---

## 📋 2. Các Bước Thực Hiện Kiểm Thử (Testing Workflow)

1. **Chuẩn bị môi trường:**
   - Đảm bảo thư mục [FireDiscipline](../../FireDiscipline/) đã có file `1.6/Assemblies/FireDiscipline.dll`.
   - Mở game RimWorld, bật **Development Mode** trong Options để theo dõi Log Console.
2. **Cấu hình Load Order:**
   ```
   Core -> DLCs -> HugsLib
   Yayo's Combat 3 (Continued)
   Suppression (Continued)
   Fire Discipline             <-- Mod của mình
   Melee Animation
   Simple Sidearms / Run and Gun / Achtung!
   ```
3. **Thực hiện các Test Case bên dưới.**

---

## 🧪 3. Chi Tiết Các Kịch Bản Kiểm Thử (Test Cases)

### Test Case 1: Kiểm thử Module Encumbrance (Tải trọng Trang bị)
- **Mục tiêu:** Xác minh chỉ số `MoveSpeed` bị suy giảm chính xác theo trọng lượng trang bị.
- **Thao tác:**
  1. Vào game, chọn 1 Pawn không mặc gì (tải trọng 0kg). Kiểm tra `MoveSpeed` (mặc định ~4.61 c/s).
  2. Cho Pawn mặc giáp nặng (Recon/Cataphract Armor) và cầm súng nặng (Minigun/Sniper Rifle).
  3. Mở bảng `Stats` của Pawn, nhấp vào chỉ số `MoveSpeed`.
- **Kỳ vọng:**
  - Chỉ số `MoveSpeed` bị giảm tương ứng với tỷ lệ vượt ngưỡng 40% sức mang.
  - Bảng giải thích chi tiết hiện dòng: `Fire Discipline Encumbrance (X kg / Y kg): -Z%`.

---

### Test Case 2: Kiểm thử Module Aim Mode & Stance (Tư thế Combat)
- **Mục tiêu:** Xác minh nút Gizmo xuất hiện khi Drafted và nhân thời gian ngắm (Warmup time).
- **Thao tác:**
  1. Chọn 1 Pawn cầm súng, bấm **Draft (R)**.
  2. Kiểm tra thanh Gizmo bên dưới màn hình.
  3. Bấm nhấp vào nút `Stance: Standard Shot` để chuyển sang `Stance: Careful Aim`.
  4. Ra lệnh cho Pawn bắn vào 1 mục tiêu bất kỳ.
- **Kỳ vọng:**
  - Nút Gizmo đổi label thành `Stance: Careful Aim`.
  - Thanh thời gian chuẩn bị ngắm (Warmup bar màu xám) chạy chậm gấp đôi (x2.0 warmup time).

---

### Test Case 3: Kiểm thử Tích hợp Runtime Detection (Suppression Integration)
- **Mục tiêu:** Đảm bảo mod nhường quyền cho *Suppression (Continued)* khi mod này bật.
- **Thao tác A (Không có Suppression Continued):**
  - Tắt mod *Suppression (Continued)* trong danh sách mod, load vào game.
  - Kiểm tra DevLog Console.
- **Kỳ vọng A:** Log hiện: `[Fire Discipline] External Suppression mod not detected. Running internal lightweight suppression engine.`

- **Thao tác B (Bật Suppression Continued):**
  - Bật mod *Suppression (Continued)*, load vào game.
- **Kỳ vọng B:** Log hiện: `[Fire Discipline] Detected external mod 'Mlie.Suppression'. Deferring to upstream & enabling Supplementary Mode.`

---

### Test Case 4: Kiểm thử Tháo Mod An Toàn (Unload Safety)
- **Mục tiêu:** Đảm bảo gỡ mod không gây hỏng Save.
- **Thao tác:**
  1. Trong game, cho Pawn mặc trang bị đầy đủ và chọn tư thế `Careful Aim`.
  2. Bấm **Save Game**.
  3. Thoát ra Menu chính, tắt mod **Fire Discipline** trong danh sách Mod.
  4. Load lại file Save vừa tạo.
- **Kỳ vọng:**
  - Game load thành công 100%, Pawn hoạt động bình thường, không có lỗi đỏ `Missing Class` hay `NullReferenceException` trong DevLog Console.

