# Fire Discipline — Tactical Expansion & Command Layer Features

> File thiết kế tính năng mở rộng tầng điều khiển chiến thuật (Command & RTS Tactical Layer) cho mod **Fire Discipline**.

---

## 1. Tổng Quan Mục Tiêu

Bên cạnh 5 module chỉ số lõi đã hoàn thành (**Suppression, Aim Stances, Encumbrance, Graze, Shock & Shell Shock**), phần mở rộng này bổ sung các tính năng điều khiển trực quan giúp giảm bớt công việc micromanagement rườm rà, đồng thời nâng tầm trải nghiệm combat của RimWorld lên cấp độ **Chiến Thuật RTS / XCOM**.

---

## 2. Danh Sách Các Tính Năng Đề Xuất

### 🎯 2.1 Overwatch Zone — Chế Độ Phục Kích Vùng (Angle Holding)

- **Bài toán Vanilla:** Pawn khi drafted nhận diện kẻ địch rất chậm (mất 1.5–2.0 giây sau khi địch bước ra khỏi cửa), hoặc tự động quay sang bắn mục tiêu không quan trọng ở xa.
- **Thiết kế:**
  - **Gizmo Command:** Nút `Set Overwatch Zone` (Icon ống dòm / chiếc nón tầm nhìn) trên thanh điều khiển Pawn.
  - **Thao tác:** Chọn Pawn $\rightarrow$ Click nút Overwatch $\rightarrow$ Kéo chọn một vùng/hành lang/cánh cửa trên bản đồ.
  - **Hành vi Pawn:**
    - Pawn chĩa súng sẵn về hướng chỉ định và bỏ qua kẻ địch nằm ngoài vùng phục kích.
    - Ngay khi có bất kỳ kẻ địch nào **bước chân vào vùng Overwatch**, Pawn sẽ **khóa mục tiêu và nổ súng NGAY LẬP TỨC (độ trễ ~0.1s)**.
    - Kết hợp hoàn hảo với tư thế **Sharpshot (Sniper)** để tiêu diệt ngay lập tức đối thủ thò đầu ra khỏi góc tường.
  - **Hình ảnh UI:** Hiển thị mảng lưới mầu xanh lá nhẹ chỉ vùng canh phục kích khi chọn Pawn.

---

### 🚀 2.2 Smart Attack-Move — Tự Động Di Chuyển Vào Tầm Bắn (Auto-Advance)

- **Bài toán Vanilla:** Click chuột phải vào kẻ địch ở ngoài tầm súng $\rightarrow$ Game báo "Out of range" hoặc Pawn đứng trố mắt, bắt người chơi phải click đất cho Pawn chạy lại gần rồi mới ra lệnh bắn lại.
- **Thiết kế:**
  - Khi Pawn đang ở trạng thái **Drafted**: Right-click trực tiếp vào kẻ địch ở xa.
  - Pawn sẽ **tự động di chuyển thông minh** hướng về phía kẻ địch cho đến khi vừa chạm **ranh giới tầm bắn tối ưu (90% Max Range)**.
  - Ngay khi vừa vào tầm súng, Pawn tự động **dừng lại, vào tư thế tác chiến và khai hỏa**.
  - Nếu kẻ địch lùi lại, Pawn tự động bước tới để duy trì khoảng cách bắn.

---

### 🔫 2.3 Tactical Fireteams & Synchronized Volley — Đội Chiến Thuật & Bắn Đồng Loạt

- **Bài toán Vanilla:** Các Pawn nổ súng rải rác không đồng bộ, khó dồn hỏa lực tiêu diệt nhanh mục tiêu nguy hiểm (như Centipede hoặc Sát thủ cận chiến).
- **Thiết kế:**
  - **Tạo Đội Tác Chiến:** Cho phép gán các Pawn đang draft thành **Squad Alpha, Bravo, Charlie** bằng Gizmo phím tắt.
  - **Lệnh Bắn Đồng Loạt (Synchronized Volley):**
    - Khi bật lệnh này, tất cả Pawn trong Đội sẽ giơ súng ngắm sẵn vào mục tiêu được chỉ định nhưng **giữ đạn (Hold Fire)**.
    - Ngay khi **toàn bộ thành viên trong đội đều đã sẵn sàng/đúng tư thế**, toàn đội sẽ **nổ súng ĐỒNG LOẠT trong cùng 1 tích tắc**.
    - Tạo hiệu ứng phục kích hủy diệt cực kỳ sướng mắt và hiệu quả.

---

### 💥 2.4 Suppressing Area Fire — Bắn Áp Chế Vùng (Bắn Mù Bờ Tường)

- **Bài toán Vanilla:** Không thể bắn vào vị trí mà Pawn không có Line of Sight (đường đạn trực tiếp) tới kẻ địch.
- **Thiết kế:**
  - Right-click vào một mảng bờ tường / bụi rậm / bao cát $\rightarrow$ Chọn **Suppressing Area Fire**.
  - Pawn sẽ liên tục xả đạn vào vị trí đó dù không nhìn thấy kẻ địch bên trong.
  - Kẻ địch nấp sau mảng tường đó sẽ bị dính tích lũy **Suppression (Áp chế)** liên tục, bị khóa chặt không thể thò đầu ra bắn trả.

---

## 3. Lộ Trình Triển Khai Ưu Tiên

1. **Giai đoạn 1:** `Overwatch Zone` (Chế độ Phục Kích Vùng) & `Smart Attack-Move` (Tự di chuyển vào tầm súng).
2. **Giai đoạn 2:** `Tactical Fireteams` (Đội tác chiến & Bắn đồng loạt).
3. **Giai đoạn 3:** `Suppressing Area Fire` (Bắn áp chế vùng).
