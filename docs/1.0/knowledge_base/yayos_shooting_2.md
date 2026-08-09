# Knowledge Base — Yayo's Shooting 2 Analysis

> Mod tham chiếu: **Yayo's Shooting 2** (Steam Workshop ID: `2020785943`)
> Tác giả gốc: YAYO (công khai mời fork và nâng cấp)
> File mã nguồn phân tích: `Yayo's Shooting 2/source/VerbChanger.cs`

---

## 1. Phân Tích Kiến Trúc Yayo's Shooting 2

Trong `VerbChanger.cs`, mod thực hiện việc bổ sung các chế độ bắn (Aimed Fire / Suppressive Fire) như sau:

```csharp
// Đọc tất cả Ranged Weapons trong DefDatabase lúc game startup
foreach (ThingDef t in from thing in DefDatabase<ThingDef>.AllDefs where thing.Verbs != null select thing)
{
    // Thêm trực tiếp VerbProperties mới vào t.Verbs
    t.Verbs.Add(propAimShot);
    t.Verbs.Add(propBurstShot);
    
    // Đổi verbClass của Verb đầu tiên thành Verb_Shoot_Selected
    t.Verbs[0].verbClass = typeof(Verb_Shoot_Selected);
}
```

---

## 2. Điểm Yếu Kiến Trúc & Bug Cấu Trúc (Structural Bugs)

Tác giả YAYO tự thừa nhận mod có bug ở tầng cấu trúc. Việc phân tích code khẳng định các nguyên nhân gãy sau:

1. **Vi phạm nguyên tắc "Không thay Class & không sửa Def gốc":**
   - Việc chèn thêm `VerbProperties` vào danh sách `t.Verbs` của `ThingDef` làm biến đổi cấu trúc dữ liệu của weapon Def trong bộ nhớ.
   - Khi có mod vũ khí khác can thiệp (như Combat Extended, Dual Wield, Simple Sidearms), các patch XML hoặc Harmony patch của họ kỳ vọng `t.Verbs[0]` giữ nguyên class `Verb_Shoot` hoặc `Verb_LaunchProjectile`, gây crash `NullReferenceException` hoặc gãy chọn vũ khí.
2. **Không tương thích với Save/Unload:**
   - Việc ghi đè `verbClass = typeof(Verb_Shoot_Selected)` lưu thông tin class của mod vào file Save. Khi gỡ mod, game không tìm thấy `Verb_Shoot_Selected` dẫn đến gãy Save hoàn toàn.

---

## 3. Giải Pháp Khắc Phục Trong Fire Discipline (`william.firediscipline`)

Fire Discipline áp dụng thiết kế **mới 100%** cho **Module 5.2 (Aim Mode & Stance)**:

| Tiêu chí | Yayo's Shooting 2 | Fire Discipline |
|---|---|---|
| **Can thiệp Def** | Thêm item vào `ThingDef.Verbs` | **Không đụng `ThingDef`** |
| **Đổi verbClass** | Đổi thành `Verb_Shoot_Selected` | **Giữ nguyên `verbClass` vanilla** |
| **Cách chọn Stance** | Chọn từ menu Verb của súng | **Nút Gizmo trên Pawn đã Drafted** |
| **Tính toán Warmup** | Sửa biến `warmupTime` trên Def | **Harmony Postfix** trên `VerbProperties.WarmupTime` |
| **Tháo Mod** | Gãy Save | **An toàn 100% khi gỡ mod** |

---

## 4. Trích Xuất Code Tham Khảo Cho Fire Discipline

- **Công thức tính điều chỉnh độ chính xác (Accuracy adjustment):**
  ```csharp
  // Nhắm kỹ (Careful Aim): Warmup x2.0, Accuracy x1.5
  // Bắn nhanh (Snap Shot): Mặc định
  ```
- Fire Discipline áp dụng các hệ số này trực tiếp thông qua lớp [AimStanceTracker.cs](../../../FireDiscipline/Source/FireDiscipline/AimStance/AimStanceTracker.cs) và StatPart tiêm vào `AimingDelayFactor`.

> ⚠ Bản trước còn dẫn tới `Patch_Verb_WarmupTicks.cs`. **File đó đã bị xoá ở A5** vì nó
> nhân đôi hiệu ứng mà StatPart đã áp — cùng một khoản phạt warmup tính hai lần.

