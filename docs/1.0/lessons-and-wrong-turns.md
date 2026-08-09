# Fire Discipline 1.0 — Lỗi đã gặp và hướng đi sai

> Hồ sơ lưu trữ. Mục đích **không** phải kể lại lịch sử, mà để lần sau không đi lại
> đúng con đường đó. Mỗi mục ghi: triệu chứng, nguyên nhân gốc, và **cách nó bị phát hiện**.
>
> Cột cuối là cột quan trọng nhất. Phần lớn lỗi ở đây **không lộ ra khi đọc code**.

---

## 1. Bug production

### 1.1 Đợt đầu — engine suppression chưa từng chạy thật

| # | Bug | Hệ quả thật | Phát hiện bằng |
|---|---|---|---|
| 1 | `Patch_Projectile_Impact` là **Postfix** trên `Projectile.Impact` | `Impact` huỷ viên đạn trước khi trả về → `__instance.Map` là `null` → hàm return ngay dòng đầu, **mỗi viên đạn**. Engine "chạy" nhưng đóng góp bằng 0 | Debug action in log từng phát bắn |
| 2 | Recoil Rapid đọc `burstShotsLeft` ngoài loạt bắn | Ngoài loạt `burstShotsLeft = 0` → `shotIndex = burstCount` → phạt **×0.93⁶ = ×0.65 vĩnh viễn**. Đo được: Rapid @3ô = 24% trong khi SnapShot = 37% | Ma trận hit chance |
| 3 | Cooldown đọc từ `verbProps.defaultCooldownTime` | Field đó **bằng 0 trên hầu hết vũ khí**; RimWorld lấy từ stat `RangedWeapon_Cooldown`. Sai ở **3 chỗ** | Bảng DPS |
| 4 | Hệ quả của #3 trong `CalculateRapidWarmupRatio` | `cooldown = 0` → `rawRatio = 0` → **clamp về đáy 0.30 cho mọi vũ khí**. Cả dải `0.30–0.75` chưa bao giờ được dùng | như trên |
| 5 | `HediffComp_Disappears` không refresh khi bị bắn tiếp | Suppression tan sau 3–5 giây kể từ viên **đầu**. Đo được: 219→214→…→186 tick rồi hết hạn giữa loạt | Log từng phát bắn |

**Ba trong năm bug đọc sai nguồn dữ liệu** — `Map` sau khi despawn, `burstShotsLeft` ngoài ngữ cảnh, `defaultCooldownTime` thay vì stat. Không cái nào lộ khi đọc code.

### 1.2 Đợt sau — hai nguồn sự thật và thang số lệch nhau

| # | Bug | Hệ quả thật | Phát hiện bằng |
|---|---|---|---|
| 6 | `FD_Suppressed` **không bao giờ tự gỡ** | `CompShouldRemove` kiểm `Severity <= 0f`, nhưng `Hediff.set_Severity` của vanilla clamp về `def.minSeverity = 0.001` → không bao giờ chạm 0. Hediff bám vĩnh viễn lên mọi pawn từng bị bắn về phía | Đọc IL `Verse.Hediff.set_Severity` |
| 7 | `minSeverity` tồn tại hai bản lệch 10× | XML `0.001` vs `SuppressionEngine.MinSeverity = 0.01f` | Đọc code khi làm A9 |
| 8 | `initialSeverity 0.25` trong XML là code chết | Engine gán thẳng `hediff.Severity` khi tạo → giá trị XML không ai đọc. Khối comment 15 dòng phía trên lại giải thích thang dựa vào nó | Đọc code khi làm A9 |
| 9 | Ngưỡng Pinned `0.80` thuộc thang cũ | Thang đổi từ 0–1 sang **0–9** mà ngưỡng bê nguyên. `0.80` rơi vào stage `shaken` — stage **cố ý giấu** khỏi health tab. Pawn bị **cấm bắn hoàn toàn sau ~4 phát đạn**, không có chỉ dấu nào trên màn hình | Đối chiếu ngưỡng với bảng stage |
| 10 | `MoveSpeed` statOffsets là **cộng thẳng**, không nhân | Base người ≈ 4.6 ô/s, nên `-0.15/-0.35/-0.55` = `-3%/-8%/-12%`. Suppression **tuyên bố** là cơ chế ghim chân nhưng **giao** ra cơ chế phạt ngắm | Tính tay từ giá trị base |
| 11 | `FindOuterLimb` trả về part không thể trúng | `Waist` có `coverageAbs == 0`; vanilla `Hediff_Injury.PostAdd` bắn `Log.Error` đỏ mỗi phát splash | Chơi game, thấy log đỏ |
| 12 | Nêm shotgun **xuyên tường** | `Contains` là hình học thuần, không có tham số `Map`. Cả sát thương lẫn overlay **sai giống hệt nhau**, nên đối chiếu overlay với sát thương luôn khớp | Nhìn màn hình |
| 13 | `.ToList()` mỗi viên đạn chạm | `AllPawnsSpawned` là `IReadOnlyList` → sao chép thật, 100–200 phần tử, trong prefix của `Projectile.Impact` | Đọc code khi review |
| 14 | Suppression decay `0.20/s` quá cao | **2/3 số vũ khí không bao giờ áp chế được ai**; `0 of 148` giữ nổi mục tiêu. Cộng cover B3 vào thì cả LMG cũng không ghim nổi pawn sau bao cát | `Print Suppression Output Matrix` |
| 15 | Splash shotgun áp chế **không giảm theo cự ly** | Sát thương có `densityFactor`, suppression thì không → pawn cách 15 ô bị ghim y hệt pawn cách 2 ô, dù chỉ ăn 9% sát thương | Đọc code khi review |
| 16 | `AimStanceTracker` rò 3 Dictionary | Khoá theo `thingIDNumber`, không nơi nào gọi `ClearCache()` | grep ra đúng 1 kết quả: chính định nghĩa hàm |
| 17 | `lastShockTicks` **tái tạo lại đúng bug 16** | Task sau thêm một Dictionary tĩnh mới không dọn, ngay khi task trước vừa vá bug cùng loại ở file bên cạnh | Review |

---

## 2. Hướng đi sai về thiết kế

### 2.1 Nhận diện embrasure — sai **ba lần** trước khi đúng

| Lần | Cách làm | Vì sao sai |
|---|---|---|
| 1 | Khớp chuỗi `defName`/`label` chứa `"embrasure"` | Vi phạm luật 2. **Gãy trên mọi client không phải tiếng Anh** vì `label` đã dịch. Kèm `!isStuffableAirtight` — cờ đó `false` mặc định trên hầu hết công trình, nên **hàng trăm def khớp**: tường thường, đá granite, mọi khoáng sản, cửa |
| 2 | Dải `fillPercent ∈ [0.65, 1.0)` + `Impassable` | Vẫn ôm `FleshmassHeart` (0.75), `CerebrexStabilizer` (0.70). Và **loại nhầm** embrasure có `fill = 1.0` — trên modlist thật đó là **2 trong 4** khẩu |
| 3 | Siết theo `Fillage` | **Không siết được gì.** `ThingDef.get_Fillage` chỉ 32 byte IL: `< 0.01 → None`, `> 0.99 → Full`, còn lại `Partial`. Suy hoàn toàn từ `fillPercent`, **không mang thêm một bit thông tin nào** |
| ✅ | Cờ Def `disableImpassableShotOverConfigError` | Vanilla cảnh báo khi vật `Impassable` cho bắn xuyên; mod embrasure phải bật cờ để tắt cảnh báo. **4/563 def, 0 false positive**, trên modlist có ba mod embrasure độc lập |

**Bài học:** lần 3 suýt được cài đặt vì nghe hợp lý. Chỉ đọc IL mới thấy nó vô nghĩa.

### 2.2 Phân loại shotgun — một vị từ trả lời hai câu hỏi ngược nhau

Ban đầu: `(AccuracyTouch >= AccuracyMedium) && range <= 25`.

Sai **~73%** số vũ khí nó gắn nhãn shotgun, và đẩy **64%** toàn bộ vũ khí tầm xa vào nhánh `d0` rộng.

Nguyên nhân gốc: shotgun có đường cong **phẳng**, còn `d0` rộng thuộc về vũ khí có đường cong **dốc**. Một vị từ không thể trả lời cả hai. Đã tách thành hai phép tính riêng, `HasShotgunProfile` 5 gate + `CalculateD0` liên tục.

Kết quả sau khi sửa: **163 vũ khí / 6 nguồn → 12 shotgun, 0 false positive**, 1 false negative đã chấp nhận (`Gun_Scattergun`, range 19.9 bị ngưỡng 17 cắt).

### 2.3 Tầm nêm shotgun — sửa nửa vời còn tệ hơn không sửa

Trần cũ `Mathf.Min(8f, khoảng cách tới mục tiêu)` không co giãn theo vũ khí — đúng là phải bỏ.

Nhưng **bỏ trần mà không thêm suy giảm theo cự ly** biến shotgun thành **cây thương 16 ô, sát thương đầy đủ suốt chiều dài**. Comment trong code lúc đó từ chối suy giảm theo cự ly vì *"sẽ khiến ô cạnh chính xạ thủ là chỗ nguy hiểm nhất bản đồ"* — lập luận đúng khi `length ≤ 8`, sập hoàn toàn khi `length = 16`.

Sửa đúng: nón loe theo **ô tuyệt đối** thay vì theo tỉ lệ chiều dài, và suy giảm theo **mật độ pellet** suy ra từ chính hình học — không thêm hằng số nào.

### 2.4 Cổng gate suppression đảo ngược

`SuppressionIntegrationModule` bật module **khi CÓ** mod suppression ngoài — ngược hoàn toàn với thiết kế. Người chơi standalone mất sạch tính năng.

Đã bỏ hẳn khái niệm "cổng": engine luôn có mặt, người chơi tự bật/tắt, dò mod ngoài chỉ dùng để đặt mặc định lần chạy đầu và cảnh báo hai chiều.

### 2.5 Giảm phương sai sát thương — đo rồi bỏ

Bốn mô hình, 20 000 cửa sổ mỗi ô:

| Mô hình | Bảo toàn kỳ vọng | CV (LMG) | CV (phát một) |
|---|---|---|---|
| `independent` *(giữ)* | — | 0.31 | 0.77 |
| `quota-carry` | ✅ chính xác | 0.07 | 0.31 |
| `pity-oneway` | ❌ 37%→44% | 0.22 | 0.64 |
| `pity-symmetric` | ❌ 37%→45%, **3%→26%** | 0.13 | 0.62 |

`pity` bị loại **bằng bằng chứng**: nâng accuracy sau mỗi phát trượt không tách được khỏi việc buff accuracy. Ở `p` thấp, bonus nằm lì ở trần → buff lớn nhất đúng nơi tỉ lệ trúng tệ nhất.

`quota-carry` hoạt động nhưng phải chặn `Verb_LaunchProjectile.TryCastShot` — tức **viết lại giải quyết chiến đấu**, đúng thứ định vị mod từ chối. Hoãn sau 1.0.

### 2.6 Hai đề xuất của planner bị bác bỏ đúng

| Đề xuất | Vì sao sai |
|---|---|
| Dùng `Stance_Cooldown` thay cho Pinned patch | `Stance_Cooldown` chặn **cả di chuyển**, trong khi B5 cố ý cho pawn bò đi. Sai mục tiêu thiết kế |
| Gỡ bỏ hoàn toàn B4 embrasure | Viện dẫn "vi phạm luật 2" — **sai**, luật 2 cấm hardcode `defName`, còn hằng số trong settings chính là hình thức luật 9 yêu cầu. Viện dẫn "8 ô mỗi phát bắn" — **sai**, `&&` ngắn mạch nên chi phí bằng 0 khi tắt. Và modlist ba mod embrasure chồng nhau là **bàn thử tương thích**, không phải cấu hình người chơi thật |

---

## 3. Tài liệu và UI đã nói dối

Loại lỗi này lặp lại nhiều nhất trong dự án, và **luôn theo cùng một cơ chế: một câu đúng lúc viết, không ai cập nhật khi thứ nó mô tả thay đổi.**

| Nơi | Nói gì | Thực tế |
|---|---|---|
| `Print Cover Values` | in `UNVERIFIED` và *"B3/B4 stay blocked until Q6.8 is answered"* | **Chính output của nó** đã liệt kê `BaseBlockChance(ThingDef) -> Single`. Nó tự khoá mình bằng một cảnh báo lỗi thời qua nhiều phiên |
| `DebugHarness` | *"cột `sustain` là cột quan trọng nhất… vũ khí bắn lại kịp trong cửa sổ decay sẽ ghim vô hạn"* | Phép đo cho `0 of 148` vũ khí có chu kỳ đủ ngắn. Cột đó **không bao giờ có thể in YES** |
| `About.xml` | *"the Pinned state"* (×2) | Pinned đã bị xoá cùng `Patch_Verb_Available` |
| `About.xml` | *"shotgun spread has no danger-zone indicator"* | Overlay có thật, còn tô đỏ ô có đồng đội |
| `About.xml` | không nhắc cover | Cover kháng suppression **bật mặc định**, là trụ cột giúp bên ít quân đánh được bên đông quân |
| Slider UI | *"the direct hit is reduced to pay for the splash"* | Prefix không ghi ngược vào `dinfo`; phát trúng thẳng ăn **100%** sát thương vanilla |
| `FireDisciplineSettings` | *"blocked on the unverified cover API"* | Q6.8 đã trả lời, đường cover đã chạy trong cùng commit |
| `EmbrasureUtility` | *"over-inclusive là phía an toàn của sai số"* | Đúng khi hiệu ứng là **phạt**. **Đảo chiều** khi cùng phép khớp đó cấp **lợi ích** ×0.30 kháng suppression |
| Tooltip embrasure | *"detection is not yet verified"* | Đã đo hai lần, 0 false positive. Nói dối **theo hướng ngược lại**: khiến người chơi ngờ vực đúng cái đáng tin |

---

## 4. Bài học về phương pháp đo

**Metric sai không bác bỏ được gì.** Đo phương sai **mỗi loạt bắn** làm `quota` trông vô dụng với súng phát một (CV `1.34 → 1.33`). Đổi sang **cửa sổ 10 giây** — thứ người chơi thật sự cảm nhận — mới lộ ra `0.77 → 0.31`.

**Khởi tạo sai gây lệch hệ thống.** Quota khởi tạo `carry = 0` mỗi cửa sổ gây lệch xuống (33% so với 37%) và ở `p` thấp cho **0% tuyệt đối**. Phải khởi tạo **phase ngẫu nhiên**.

**Cột kiểm chứng bắt buộc.** Không có cột `hit%` so với `baseP` thì `pity` đã lọt qua.

**Hai đường sai giống nhau thì đối chiếu vô dụng.** Overlay shotgun và sát thương dùng chung một bản cài đặt — nguyên tắc đúng, nhưng khi bản đó thiếu kiểm tra vật cản thì **cả hai cùng sai**, và phép đối chiếu luôn báo khớp. Chỉ nhìn màn hình mới thấy.

**Đọc IL thắng đoán, nhiều lần.** `CoverUtility.BaseBlockChance` (22 byte) trả lời câu hỏi chặn B3 suốt nhiều phiên. `ThingDef.get_Fillage` (32 byte) bác bỏ một hướng siết lọc. `Pawn_PathFollower.PatherTick` chứng minh không cần `StopDead()`. `Hediff_Injury.PostAdd` chỉ đúng field gây `Log.Error`.

**"0 false positive" trên một modlist không phải là đúng.** Kết quả embrasure lần đầu đẹp một phần vì modlist đó chỉ có **một** mod embrasure. Đo lại trên modlist khác mới lộ ra bộ lọc cũ bỏ sót một nửa.

---

## 5. Số liệu tham chiếu đã đo được

Giữ lại vì tốn công đo và sẽ cần khi tune 1.1.

**Cover vanilla** — `BaseBlockChance(def) = (Fillage == Full) ? 0.75f : fillPercent`

| Vật | cover |
|---|---|
| Tường, đá gốc (`Full`) | 0.75 |
| Embrasure (fill 70%) | 0.70 |
| Sandbag, Barricade | 0.55 |
| Chunk đá, bàn lớn, turret | 0.50 |
| Giường, kệ, bàn | 0.40 |
| Thùng, cây | 0.25–0.30 |

Bảng ước lượng 30/40/55/75% trong thiết kế 5.8 **đúng từ đầu** — nó bị đánh dấu "chưa xác minh" suốt nhiều phiên mà không ai sai cả.

**Mod tham chiếu Suppression (Continued)** — đọc từ `SuppressionMod.dll`

```
movespeedFactorByHediffStage   = [1, 1, 1, 0.80, 0.65]
accuracyFactorByHediffStage    = [1, 1, 1, 0.80, 0.40]
aimingDelayFactorByHediffStage = [1, 1, 1, 1.50, 3.00]
coverAdvantageFactorByHediffStage = [1, 1, 1, 0.85, 0.70]
suppressedMovespeedMin = 0.7      severityReductionPerSecond = 0.1
severityDelayTicks = 60           maxDistanceToImpact = 3   minDistanceFromLauncher = 5
```

Ba stage đầu của họ **không có tác dụng gì**. Và comment của chính tác giả trong XML: *"Can't really use 'moving' stat as it just tends to knock them over"* — họ đã thử hạ MoveSpeed và phải thêm sàn `0.7`.

**Phương sai sát thương** — CV 0.31 (LMG) / 0.77 (phát một) trong cửa sổ 10 giây.
