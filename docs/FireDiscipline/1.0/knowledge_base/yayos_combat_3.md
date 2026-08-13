# Knowledge Base — Yayo's Combat 3 Integration Analysis

> Mod tham chiếu: **Yayo's Combat 3 (Continued)** (Tác giả: Mlie, Steam Workshop ID: `2854006492`)
> `packageId`: `Mlie.YayosCombat3`
> Thư mục nguồn tại chỗ: `Yayo's Combat 3 (Continued)/`

---

## 1. Phân Tích Phạm Vi Hoạt Động Của Yayo's Combat 3

Yayo's Combat 3 giải quyết 3 mảng chính trong combat RimWorld:
1. **Hệ thống Đạn dược (Ammo System):** Yêu cầu đạn cho từng loại vũ khí.
2. **Khả năng xuyên giáp (Armor Penetration):** Tính toán lại độ nảy đạn/xuyên giáp dựa trên Tech Level và chỉ số giáp.
3. **Hoạt ảnh combat (Combat Animations):** Nạp đạn và vung vũ khí.

---

## 2. Bài Học Thiết Kế "Suy Ra, Đừng Khai Báo" (Section 3)

Một điểm sáng trong kiến trúc của Yayo's Combat 3 là:
- **Không hard-code patch riêng cho từng mod vũ khí:** Yayo's Combat 3 tự động tính toán loại đạn, sức xuyên giáp của mọi loại súng từ bất kỳ mod vũ khí nào dựa trên các chỉ số Stat có sẵn (`TechLevel`, `Mass`, `Damage`, `Range`).
- **Ứng dụng vào Fire Discipline:**
  - Lớp [StatPart_Encumbrance.cs](../../../FireDiscipline/Source/FireDiscipline/Encumbrance/StatPart_Encumbrance.cs) học hỏi triết lý này: Tự động tính toán khối lượng trang bị từ `apparel.WornApparel` và `equipment.Primary` của mọi mod giáp/súng ngoài kia mà không cần viết file patch riêng cho từng mod.

---

## 3. Phân Chia Ranh Giới (Scope Boundaries)

Fire Discipline tuân thủ **không ôm mảng Ammo**:
- **Không làm Ammo:** Việc làm Ammo riêng sẽ khiến mod tạo ra `ThingDef` đạn mới trong file Save. Khi gỡ mod ra, toàn bộ item đạn trong kho người chơi biến mất -> gãy Save.
- **Phân công:** Để nguyên mảng Ammo/Armor Pen cho Yayo's Combat 3. Fire Discipline chỉ đóng vai trò lớp chiến thuật (Encumbrance, Aim Mode, Stance, Graze).

---

## 4. Load Order An Toàn (Section 9)

Trong file `About/About.xml` của Fire Discipline, chúng ta đặt:

```xml
<loadAfter>
  <li>Mlie.YayosCombat3</li>
  <li>Mlie.Suppression</li>
</loadAfter>
```

Đảm bảo Fire Discipline luôn tải sau Yayo's Combat 3 để các patch XML dùng `MayRequire` chạy sạch sẽ và chính xác.

