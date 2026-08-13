# Knowledge Base — Suppression (Continued)

> Mod tham chiếu: **Suppression (Continued)** (Mlie, Workshop `2559826227`)
> `packageId`: `Mlie.Suppression` · nguồn tại chỗ: `Reference Mods/Suppression (Continued)/`

---

## 1. Cách nó hoạt động

1. **Hook đạn bay:** Harmony patch `Bullet.Impact`, tính severity theo khoảng cách tới điểm chạm.
2. **Áp chế bằng Hediff** `Suppressed`, thang **0–9**, 5 stage: `unsettled` · `shaken` · `wavering` · `ducking` · `cowering`.
3. **Hiệu ứng KHÔNG nằm trong XML.** File `Hediffs_Global_Suppression.xml` chỉ có nhãn stage, không một `statOffsets` nào. Toàn bộ đi qua patch `StatWorker_GetValueUnfinalized`.

---

## 2. Hằng số thật — đọc từ `1.6/Assemblies/SuppressionMod.dll`

```
movespeedFactorByHediffStage      = [1, 1, 1, 0.80, 0.65]
accuracyFactorByHediffStage       = [1, 1, 1, 0.80, 0.40]
aimingDelayFactorByHediffStage    = [1, 1, 1, 1.50, 3.00]
coverAdvantageFactorByHediffStage = [1, 1, 1, 0.85, 0.70]

suppressedMovespeedMin     = 0.7    (sàn tuyệt đối, ô/giây)
severityReductionPerSecond = 0.1
severityDelayTicks         = 60
maxDistanceToImpact        = 3      minDistanceFromLauncher = 5
duckingHediffStage = 3              proneHediffStage = 4
```

**Ba stage đầu hoàn toàn không có tác dụng gì.** Hiệu ứng chỉ bắt đầu từ `ducking`.

---

## 3. Bốn điều Fire Discipline học được

**a) Dồn sức vào `AimingDelayFactor`, không phải `MoveSpeed`.** Họ dùng `×1.5` / `×3.0` cho ngắm, trong khi `MoveSpeed` chỉ xuống `×0.65`. Fire Discipline đi **ngược lại** — có chủ đích, vì trục di chuyển giúp bên phòng thủ ít quân giữ đất.

**b) Họ đã thử hạ `MoveSpeed` và gặp vấn đề.** Comment của chính tác giả trong XML:

> *"Can't really use 'moving' stat as it just tends to knock them over"*

và ở stage `cowering`:

> *"Prone. Can't fire back or move (Disabled above, doesn't seem to add anything interesting to concept)"*

Họ thử **cả hai** thứ Fire Discipline định làm — hạ MoveSpeed mạnh và khoá bắn — rồi bỏ cả hai, và phải thêm sàn `suppressedMovespeedMin = 0.7`.

Fire Discipline vẫn đi hướng đó nhưng **lấy sàn 0.7 của họ**. Đối chiếu: `4.6 × 0.15 = 0.69` — cực trị thang của ta rơi đúng chỗ họ đặt sàn.

**c) Ba stage đầu để trống là có lý.** Fire Discipline theo hình dạng đó: stage `shaken` không mang hiệu ứng nào.

**d) `coverAdvantageFactor` — cover **giảm** tác dụng khi bị áp chế.** Chiều **ngược** với B3 của Fire Discipline (cover *giảm* suppression nhận vào). Chưa cân nhắc, ghi lại vì đáng xem ở 1.1.

---

## 4. Chồng chéo — quy tắc an toàn

Nếu cả hai mod cùng bật, **một pawn nhận suppression từ cả hai** — severity tích nhanh gần gấp đôi, mỗi mod áp debuff riêng.

Fire Discipline **không tự tắt** khi phát hiện `Mlie.Suppression`. Đây là toggle của người
chơi: lần chạy đầu tự đặt TẮT nếu dò thấy mod suppression khác hoặc CE, sau đó người chơi
sở hữu công tắc và việc dò không bao giờ ghi đè nữa. Settings window cảnh báo hai chiều.

> ⚠ Bản trước của file này mô tả một cơ chế "Supplementary Mode / Internal Engine" tự
> chuyển theo `ModsConfig.IsActive`, cài đặt trong `SuppressionIntegrationModule.cs`.
> **Class đó đã bị xoá và cơ chế đó đã bị bỏ** — cổng gate của nó đảo ngược, bật module
> khi *có* mod ngoài. Xem [`../lessons-and-wrong-turns.md`](../lessons-and-wrong-turns.md) §2.4.
