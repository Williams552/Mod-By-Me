# Lone Survivor - RimWorld Mod

**Lone Survivor** là một mod RimWorld hỗ trợ và cân bằng lối chơi cho các thuộc địa ít người hoặc solo (như *Naked Brutality*, *Rich Explorer*). Mod tự động cấp một Hediff buff linh hoạt cho colonist, với sức mạnh tối đa khi chỉ có 1 pawn và tự động giảm dần về 0 khi thuộc địa đông người lên.

---

## 🌟 Tính Năng Chính

1. **Dynamic Scaling Buff (Tự Động Cân Bằng Theo Dân Số):**
   - **1 Colonist (Solo):** Nhận **100%** hiệu lực buff.
   - **2 Colonists:** Nhận **75%** hiệu lực buff.
   - **3 Colonists:** Nhận **50%** hiệu lực buff.
   - **4 Colonists:** Nhận **25%** hiệu lực buff.
   - **$\ge 5$ Colonists (hoặc ngưỡng $N$ tùy chỉnh):** Buff về **0%** và tự động gỡ khỏi pawn.

2. **Chỉ Số Mặc Định (Ở trạng thái Solo 1 Pawn):**
   - **Global Work Speed:** **+200%** (Tương đương 300% tốc độ làm việc bình thường — làm việc nhanh gấp 3 lần).
   - **Global Learning Factor:** **+100%** (Tăng gấp 2 lần tốc độ cày exp kĩ năng để đa năng hóa sớm).
   - **Rest Fall Rate:** **-50%** (Tốc độ mệt mỏi giảm 50% — chỉ cần ngủ một nửa thời gian so với bình thường).

3. **Giao Diện Mod Settings Chi Tiết (Options -> Mod Options -> Lone Survivor):**
   - **Population Threshold ($N$):** Tùy chỉnh ngưỡng kết thúc buff (2 đến 15 pawn).
   - **Solo Work Speed Bonus:** Slider từ 0% đến 500%.
   - **Solo Learning Factor Bonus:** Slider từ 0% đến 300%.
   - **Solo Rest Fall Rate Reduction:** Slider từ 0% đến 90%.
   - **Solo Movement Speed Bonus (Tùy chọn):** Slider từ 0 đến +2.0 c/s.
   - **Solo Immunity Gain Bonus (Tùy chọn):** Slider từ 0% đến +100%.
   - **Chế Độ Đếm Colonist:** Lựa chọn đếm theo toàn bộ thuộc địa (mặc định) hoặc chỉ đếm trên từng map riêng biệt.
   - **Tần Suất Cập Nhật:** Tùy chỉnh chu kỳ kiểm tra (mặc định mỗi 2000 ticks ~ 33.3 giây).
   - **Nút "Reset to Recommended Defaults"**: Khôi phục nhanh về cấu hình chuẩn.

---

## 📁 Cấu Trúc Dự Án

```
LoneSurvivor/
├── About/
│   └── About.xml
├── 1.6/
│   ├── Assemblies/
│   │   └── LoneSurvivor.dll
│   └── Defs/
│       └── HediffDefs/
│           └── Hediffs_LoneSurvivor.xml
├── README.md
└── Source/
    └── LoneSurvivor/
        ├── LoneSurvivor.csproj
        ├── LoneSurvivorMod.cs
        ├── LoneSurvivorSettings.cs
        ├── LoneSurvivorUtility.cs
        ├── Hediff_LoneSurvivor.cs
        └── GameComponent_LoneSurvivor.cs
```
