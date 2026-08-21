# 🪐 RimWorld Mod Collection Hub

[![RimWorld Versions](https://img.shields.io/badge/RimWorld-1.5%20%7C%201.6-brightgreen.svg)](https://rimworldgame.com/)
[![Target Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/)
[![Harmony](https://img.shields.io/badge/Lib.Harmony-2.2.2-orange.svg)](https://github.com/pardeike/Harmony)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Chào mừng bạn đến với kho lưu trữ Mono-repo tổng hợp các bản mod RimWorld được phát triển bởi **William**. Kho lưu trữ này quản lý mã nguồn, tài liệu thiết kế và công cụ build/deploy cho nhiều dự án mod độc lập.

> **[Tiếng Việt](#-tiếng-việt)** | **[English](#-english)**

---

## 🇻🇳 Tiếng Việt

### 📦 Danh Mục Các Bản Mod

| Mod | Tên Gói (`packageId`) | Trạng Thái | Phiên Bản RimWorld | Yêu Cầu DLC | Tài Liệu & Chi Tiết | Steam Workshop |
| :--- | :--- | :---: | :---: | :---: | :--- | :---: |
| **🎯 [Fire Discipline](FireDiscipline/README.md)** | `william.firediscipline` | ![Stable](https://img.shields.io/badge/Status-Stable%20(v1.0.0)-success.svg) | **1.6** | Không | [`README`](FireDiscipline/README.md) · [`Docs`](docs/FireDiscipline/) | *Sắp ra mắt* |
| **🗼 [Echo Resonance](EchoResonance/README.md)** | `william.echoresonance` | ![Dev](https://img.shields.io/badge/Status-In%20Dev%20(v0.1)-blue.svg) | **1.6** | Không | [`README`](EchoResonance/README.md) · [`Docs`](docs/EchoResonance/) | *Sắp ra mắt* |
| **🌲 [Lone Survivor](LoneSurvivor/README.md)** | `william.lonesurvivor` | ![Ready](https://img.shields.io/badge/Status-Ready%20(v0.1)-brightgreen.svg) | **1.5, 1.6** | Không | [`README`](LoneSurvivor/README.md) · [`Docs`](docs/LoneSurvivor/overview.md) | *Sắp ra mắt* |
| **🧬 [Matrilineal Gene](MatrilinealGene/README.md)** | `william.matrilinealgene` | ![Ready](https://img.shields.io/badge/Status-Ready%20(v0.1)-brightgreen.svg) | **1.5, 1.6** | **Biotech** | [`README`](MatrilinealGene/README.md) · [`Docs`](docs/MatrilinealGene/overview.md) | *Sắp ra mắt* |
| **👑 [Rimward Exiles](RimwardExiles/README.md)** | `william.rimwardexiles` | ![Design](https://img.shields.io/badge/Status-Design%20Phase-orange.svg) | **1.6** | Không | [`README`](RimwardExiles/README.md) · [`Docs`](docs/RimwardExiles/) | *Dự kiến* |

---

### 🌟 Tóm Tắt Tính Năng Từng Mod

#### 1. 🎯 [Fire Discipline](FireDiscipline/README.md) — Đại tu Chiến thuật Bắn súng & Phòng thủ
- **Tư thế chiến thuật (Aim Stances):** Standard (+20% accuracy gốc, phẳng hóa tầm xa), Rapid Fire (càn quét tầm gần), Sharpshot (tỉa tầm xa, xuyên 50% vật nấp).
- **Cố thủ nằm sấp (Dug-In / Prone):** Đứng yên $\ge 1.5$s giảm 60% giật nòng cho súng máy hạng nặng (LMG/HMG/Minigun), thu nhỏ profile mục tiêu 35%.
- **Hệ thống Áp chế (Suppression Engine):** Đạn bay sát gây nao núng, chạy chậm và khóa bắn (Pinned) khi áp chế $\ge 7.0$. Súng máy gây gấp đôi áp chế. Vật nấp giúp giảm áp chế nhận vào.
- **Bảo vệ chống chết sốc (Graze System):** Chuyển đòn chí mạng bắn vào nội tạng hiểm thành vết thương sượt ngoài chi.
- **Tải trọng & Sốc chiến đấu:** Phạt tốc độ khi mang vác nặng; choáng khi đồng đội tử trận hoặc gần vụ nổ pháo kích (giáp khiên năng lượng giúp hấp thụ 85% sóng xung kích).
- **Tự động tương thích:** Phân loại vũ khí bằng thuật toán Stat Curve, tương thích 100% mọi mod súng mà không cần XML patch.

#### 2. 🗼 [Echo Resonance](EchoResonance/README.md) — Kiến trúc Archotech & Cường hoá Nhân vật
- **Điểm Echo hữu cơ:** Tích luỹ Echo khi căn cứ phát triển (luyện skill, đẩy lùi raid, chế tác đồ Masterwork, hoàn thành nghiên cứu/nghi lễ).
- **Archotech Resonator & Pylons:** Công trình lõi duy nhất tạo và giữ điểm Echo (bị phá hủy sẽ mất điểm chưa dùng). Xây tối đa 4 Attunement Pylons để khuếch đại tốc độ sinh Echo lên tới $\times 3.0$.
- **Hệ thống Perks đa tầng:** 4 nhánh bổ trợ (Flesh, Mind, Livelihood, Combat). Không giới hạn ô perk cố định; chi phí tăng lũy tiến ($1.6^{N-1}$) cho mỗi pawn nhưng giảm 25% nếu chọn perk cùng nhánh chuyên môn.

#### 3. 🌲 [Lone Survivor](LoneSurvivor/README.md) — Cân bằng Thuộc địa Ít người & Solo
- **Buff thích ứng giảm dần (Dynamic Scaling):** Cung cấp buff tối đa cho thuộc địa 1 người (+200% Work Speed, +100% Learning, -50% Rest Fall) và suy giảm mượt mà về 0% khi số lượng dân số tăng lên ngưỡng tuỳ chỉnh (mặc định $\ge 5$ người).
- **Giao diện Mod Settings hoàn chỉnh:** Cho phép tùy chỉnh chỉ số, ngưỡng dân số kết thúc buff, và chế độ đếm theo map hoặc toàn thuộc địa.

#### 4. 🧬 [Matrilineal Gene](MatrilinealGene/README.md) — Gene Sinh Sản Thuần Mẫu Hệ (Biotech)
- **100% Sinh con gái:** Mọi ca sinh nở tự nhiên hoặc nuôi cấy phôi trong bể (Growth Vat) khi mang gene mẫu hệ đều chắc chắn sinh ra bé gái.
- **Kế thừa Xenotype & Endogene thuần mẫu hệ:** Con gái kế thừa trọn vẹn toàn bộ hệ gen bẩm sinh, định nghĩa XenotypeDef (Dirtmole, Highmate, Sanguophage...) và cả biểu tượng/tên Custom Xenotype của người mẹ, ngăn ngừa việc bị lai tạp ngẫu nhiên (hybrid).

#### 5. 👑 [Rimward Exiles](RimwardExiles/README.md) — Hero Pawns & Hệ thống Lòng Trung Thành
- **Chuỗi Quest Hero:** Tuyển mộ các nhân vật anh hùng có cốt truyện và chiều sâu tính cách độc đáo.
- **Dynamic Loyalty System:** Mô phỏng lòng trung thành, mâu thuẫn lý tưởng và ký ức cá nhân. Thử thách của người chơi là duy trì sự gắn kết và ngăn ngừa đào tẩu.

---

### 🏷️ Quy Ước Git Tag & Release Độc Lập

Do repository quản lý nhiều mod độc lập, quy ước Git Tag sử dụng tiền tố tên mod theo chuẩn Semantic Versioning:

- **Fire Discipline:** `firediscipline-v1.1`, `firediscipline-v1.2`, ...
- **Echo Resonance:** `echoresonance-v0.1`, `echoresonance-v0.2`, ...
- **Lone Survivor:** `lonesurvivor-v0.1`, `lonesurvivor-v0.2`, ...
- **Matrilineal Gene:** `matrilinealgene-v0.1`, `matrilinealgene-v0.2`, ...
- **Rimward Exiles:** `rimwardexiles-v0.1`, ...
- *(Lưu ý: Tag lịch sử `v1.0.0` được bảo lưu nguyên vẹn đại diện cho bản phát hành Fire Discipline v1.0.0 đầu tiên).*

---

### 🛠️ Hướng Dẫn Build & Deploy

Repository cung cấp bộ PowerShell scripts chuẩn hoá đặt tại thư mục [`scripts/`](file:///d:/Games/Rimworld/Mod%20By%20Me/scripts/):

```powershell
# 1. Đóng gói file phân phối Release (.zip) cho một mod cụ thể
.\scripts\build-release.ps1 -ModName FireDiscipline
.\scripts\build-release.ps1 -ModName EchoResonance
.\scripts\build-release.ps1 -ModName LoneSurvivor
.\scripts\build-release.ps1 -ModName MatrilinealGene

# Hoặc đóng gói tất cả các mod cùng lúc
.\scripts\build-release.ps1 -All

# 2. Deploy trực tiếp vào thư mục game RimWorld Mods
.\scripts\deploy.ps1 -ModName FireDiscipline
.\scripts\deploy.ps1 -ModName MatrilinealGene

# Deploy tất cả mod kèm kiểm tra an toàn (-WhatIf)
.\scripts\deploy.ps1 -All -WhatIf
```

Hoặc build trực tiếp bằng .NET CLI:
```bash
dotnet build FireDiscipline/Source/FireDiscipline/FireDiscipline.csproj -c Release
dotnet build EchoResonance/Source/EchoResonance/EchoResonance.csproj -c Release
dotnet build LoneSurvivor/Source/LoneSurvivor/LoneSurvivor.csproj -c Release
dotnet build MatrilinealGene/Source/MatrilinealGene/MatrilinealGene.csproj -c Release
```

---

## 🌐 English

### 📖 Overview

This repository is a **Multi-Mod Mono-repo Hub** housing RimWorld modifications developed by **William**. Each mod is self-contained with its own source code, Defs, version folders, and documentation, while sharing automated build & deployment infrastructure.

### 📁 Repository Structure

```
Mod-By-Me/
├── .gitignore              # Unified ignore rules for assemblies, binaries, and build caches
├── README.md               # Main hub navigation & build guide
├── FireDiscipline/         # Tactical Combat Overhaul mod
│   ├── About/
│   ├── 1.6/
│   ├── README.md
│   └── Source/
├── EchoResonance/          # Archotech Resonator & Pawn Perks mod
│   ├── About/
│   ├── 1.6/
│   ├── README.md
│   └── Source/
├── LoneSurvivor/           # Small Colony & Solo Dynamic Buffs mod
│   ├── About/
│   ├── 1.5/ & 1.6/
│   ├── README.md
│   └── Source/
├── MatrilinealGene/        # Pure Matrilineal Reproduction & Inheritance mod (Biotech)
│   ├── About/
│   ├── 1.5/ & 1.6/
│   ├── README.md
│   └── Source/
├── RimwardExiles/          # Hero Pawns & Loyalty System mod (Design phase)
│   ├── README.md
│   └── (Source & Defs coming soon)
├── docs/                   # Centralized design & technical documentation
│   ├── FireDiscipline/
│   ├── EchoResonance/
│   ├── LoneSurvivor/
│   ├── MatrilinealGene/
│   └── RimwardExiles/
└── scripts/                # Shared build, packaging, and deployment scripts
    ├── build-release.ps1
    └── deploy.ps1
```

---

## 📜 License & Credits

- All original C# code and XML configurations are licensed under the [MIT License](LICENSE).
- Powered by [RimWorld](https://rimworldgame.com/) by Ludeon Studios and [Lib.Harmony](https://github.com/pardeike/Harmony) by Andreas Pardeike.
