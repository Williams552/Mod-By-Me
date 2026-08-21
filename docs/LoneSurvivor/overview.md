# 🌲 Lone Survivor — Tài Liệu Thiết Kế & Kỹ Thuật

**Lone Survivor** (`william.lonesurvivor`) là mod hỗ trợ và cân bằng trải nghiệm sinh tồn cho các thuộc địa solo hoặc số lượng dân số rất ít (1–4 colonists).

---

## 🎯 1. Triết Lý Thiết Kế

- **Khởi đầu khó khăn:** Trong RimWorld, kịch bản 1 người sống sót (*Naked Brutality*, *Rich Explorer*) cực kỳ dễ bị nghẽn (bottleneck) vì một colonist phải gánh vác toàn bộ công việc: xây dựng, nghiên cứu, nấu ăn, chữa bệnh, trồng trọt và tự vệ.
- **Tự động cân bằng giảm dần (Diminishing Return):** Thay vì cho các chỉ số cố định vĩnh viễn (gây mất cân bằng khi đông người), mod áp dụng công thức suy giảm mượt mà theo quy mô dân số và tự động gỡ bỏ khi đạt ngưỡng thiết lập.
- **Không xâm lấn (Non-intrusive):** Sử dụng hệ thống Hediff và GameComponent định kỳ, không can thiệp vào AI JobGiver hay ThinkTree, tương thích an toàn khi thêm/bỏ giữa chừng save game.

---

## ⚙️ 2. Cơ Chế Hoạt Động Kỹ Thuật

### 2.1. Công thức Dynamic Scaling

Hệ số hiệu lực buff $S$ được tính toán như sau:
$$\text{Buff Factor} = \max\left(0, 1 - \frac{\text{Colonist Count} - 1}{N - 1}\right)$$
*(với $N$ là ngưỡng dân số tối đa được cấu hình trong Mod Settings, mặc định $N = 5$)*.

| Số Lượng Colonist | Tỷ Lệ Hiệu Lực ($N=5$) | Work Speed (+200%) | Learning (+100%) | Rest Fall (-50%) |
| :---: | :---: | :---: | :---: | :---: |
| **1 (Solo)** | **100%** | **+200%** | **+100%** | **-50%** |
| **2** | **75%** | **+150%** | **+75%** | **-37.5%** |
| **3** | **50%** | **+100%** | **+50%** | **-25%** |
| **4** | **25%** | **+50%** | **+25%** | **-12.5%** |
| **$\ge 5$** | **0% (Gỡ buff)** | 0% | 0% | 0% |

### 2.2. Các Thành Phần Mã Nguồn C#

- [`LoneSurvivorMod.cs`](file:///d:/Games/Rimworld/Mod%20By%20Me/LoneSurvivor/Source/LoneSurvivor/LoneSurvivorMod.cs): Khởi tạo mod và giao diện Mod Settings.
- [`LoneSurvivorSettings.cs`](file:///d:/Games/Rimworld/Mod%20By%20Me/LoneSurvivor/Source/LoneSurvivor/LoneSurvivorSettings.cs): Lưu trữ cấu hình tuỳ biến (ngưỡng dân số, hệ số buff, chế độ đếm theo map hay toàn cầu).
- [`LoneSurvivorUtility.cs`](file:///d:/Games/Rimworld/Mod%20By%20Me/LoneSurvivor/Source/LoneSurvivor/LoneSurvivorUtility.cs): Logic tính toán số lượng colonist hợp lệ và công thức scaling.
- [`Hediff_LoneSurvivor.cs`](file:///d:/Games/Rimworld/Mod%20By%20Me/LoneSurvivor/Source/LoneSurvivor/Hediff_LoneSurvivor.cs): Quản lý mức severity và hiển thị tooltip chi tiết.
- [`GameComponent_LoneSurvivor.cs`](file:///d:/Games/Rimworld/Mod%20By%20Me/LoneSurvivor/Source/LoneSurvivor/GameComponent_LoneSurvivor.cs): Vòng lặp kiểm tra định kỳ (mỗi 2,000 ticks) để thêm/cập nhật/gỡ Hediff trên các pawn thuộc địa.
