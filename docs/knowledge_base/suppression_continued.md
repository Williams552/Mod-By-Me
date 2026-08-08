# Knowledge Base — Suppression (Continued) Integration Analysis

> Mod tham chiếu: **Suppression (Continued)** (Tác giả: Mlie, Steam Workshop ID: `2559826227`)
> `packageId`: `Mlie.Suppression`
> Thư mục nguồn tại chỗ: `Suppression (Continued)/`

---

## 1. Phân Tích Cơ Chế Suppression (Continued)

Mod *Suppression (Continued)* hoạt động dựa trên nguyên lý:
1. **Hook đạn bay:** Sử dụng Harmony Patch lên `Projectile.Tick` hoặc kiểm tra vị trí đạn bay qua các ô tile xung quanh Pawn.
2. **Áp chế bằng Hediff (`Hediff_Suppression`):**
   - Khi đạn bay sát qua Pawn, mức độ áp chế (Suppression level) tăng lên.
   - Khi mức áp chế đạt ngưỡng, Pawn nhận các `HediffStage` giảm tốc độ di chuyển (`MoveSpeed`), giảm độ chính xác bắn (`ShootingAccuracyPawn`) và tăng thời gian chuẩn bị ngắm (`WarmupTime`).

---

## 2. Chiến Lược Tích Hợp Của Fire Discipline (`william.firediscipline`)

Để tuân thủ Nguyên tắc kiến trúc #7 ("Không hard dependency, tự phát hiện và phối hợp khi phát hiện mod khác"), **Module 5.1 (Suppression Integration)** của Fire Discipline vận hành theo 2 chế độ:

```
                      [Khởi chạy Mod]
                             │
            Kiểm tra: ModsConfig.IsActive("Mlie.Suppression")
                             │
             ┌───────────────┴───────────────┐
           [CÓ]                             [KHÔNG]
             │                                 │
     (Supplementary Mode)              (Internal Lightweight Engine)
  - Tắt module áp chế nội bộ       - Bật engine áp chế thưa tick (15-30 ticks)
  - Cho Stance & Cover tương tác   - Thêm Hediff áp chế nhẹ nội bộ
    trực tiếp với Hediff của Mlie  - Không ảnh hưởng FPS
```

---

## 3. Mã Nguồn Tương Tác Giữa Hai Mod

Lớp [SuppressionIntegrationModule.cs](file:///d:/Games/Rimworld/Mod%20By%20Me/FireDiscipline/Source/FireDiscipline/Suppression/SuppressionIntegrationModule.cs) thực hiện việc này tại thời điểm startup:

```csharp
public const string ExternalSuppressionPackageId = "Mlie.Suppression";

public bool ShouldEnable()
{
    // Đọc từ Mod Settings
    return FireDisciplineMod.Settings?.IsModuleEnabled(this) ?? DefaultEnabled;
}

public void OnStartup()
{
    bool externalActive = ModsConfig.IsActive(ExternalSuppressionPackageId);
    if (externalActive)
    {
        Log.Message("[Fire Discipline] Detected external mod 'Mlie.Suppression'. Deferring to upstream & enabling Supplementary Mode.");
    }
}
```

---

## 4. Kết Luận & Quy Tắc An Toàn
- **Tránh trùng lặp Hediff:** Việc check `ModsConfig.IsActive("Mlie.Suppression")` ngăn chặn tình trạng Pawn bị áp chế 2 lần từ 2 mod khác nhau.
- **Tháo mod an toàn:** Nếu gỡ *Suppression (Continued)*, Fire Discipline tự động chuyển về engine áp chế nội bộ mà không báo lỗi `NullReferenceException`.
