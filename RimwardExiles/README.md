# 👑 Rimward Exiles — Hero Pawns & Dynamic Loyalty System

[![RimWorld 1.6](https://img.shields.io/badge/RimWorld-1.6-brightgreen.svg)](https://rimworldgame.com/)
[![Status: Planning](https://img.shields.io/badge/Status-Design%20Phase-orange.svg)]()

**Rimward Exiles** (`william.rimwardexiles`) là dự án mod bổ sung chuỗi Quest tuyển mộ các **Hero Pawn** độc đáo với cốt truyện và tính cách riêng, kết hợp hệ thống **Lòng Trung Thành (Dynamic Loyalty System)** mô phỏng niềm tin cá nhân, ký ức và xung đột giữa các thành viên.

> *"Người chơi luôn có thể chiêu mộ được Hero — nhưng thử thách thực sự là giữ họ ở lại."*

---

## 📖 Tài Liệu Thiết Kế Chi Tiết

Toàn bộ tài liệu phân tích kỹ thuật và hệ thống game design nằm trong thư mục [`docs/RimwardExiles/`](file:///d:/Games/Rimworld/Mod%20By%20Me/docs/RimwardExiles/):

- [`00-vision.md`](file:///d:/Games/Rimworld/Mod%20By%20Me/docs/RimwardExiles/00-vision.md): Tầm nhìn, ranh giới dự án và 6 ràng buộc kiến trúc cứng.
- [`01-value-axes.md`](file:///d:/Games/Rimworld/Mod%20By%20Me/docs/RimwardExiles/01-value-axes.md): Hệ trục giá trị nhân vật và xung đột tư tưởng.
- [`02-loyalty-system.md`](file:///d:/Games/Rimworld/Mod%20By%20Me/docs/RimwardExiles/02-loyalty-system.md): Cơ chế lòng trung thành, các tầng trạng thái và nguy cơ đào tẩu/nổi loạn.
- [`05-technical.md`](file:///d:/Games/Rimworld/Mod%20By%20Me/docs/RimwardExiles/05-technical.md): Kiến trúc mã nguồn C#, lưu trữ snapshot và cơ chế Harmony patching an toàn.

---

## 🛠️ Cấu Trúc Dự Kiến

```
RimwardExiles/
├── About/
│   └── About.xml
├── 1.6/
│   ├── Assemblies/
│   ├── Defs/
│   ├── Languages/
│   └── Textures/
├── README.md
└── Source/
    └── RimwardExiles/
```
