# Fire Discipline — ILSpy / Reflection Findings

> Trả lời cho mục 6 của `CLAUDE-CODE-HANDOFF.md`.
> Ngày: **2026-08-07** · RimWorld **1.6** · nguồn: `RimWorldWin64_Data/Managed/Assembly-CSharp.dll`

## Phương pháp

**Không dùng ILSpy.** Tất cả kết quả dưới đây lấy bằng **reflection trên `Assembly-CSharp.dll`** qua PowerShell: nạp assembly với một `AssemblyResolve` handler trỏ vào thư mục `Managed`, rồi liệt kê type / field / property / method.

Ưu điểm: chạy được ngay, lặp lại được, và **bám đúng bản game đang cài** thay vì bám vào trí nhớ hay tài liệu.

Giới hạn — cần nói rõ:

| Reflection trả lời được | Reflection KHÔNG trả lời được |
|---|---|
| Type/member có tồn tại không, tên chính xác, chữ ký, `virtual` hay không | **Thân hàm làm gì** |
| Cấu trúc dữ liệu (field nào nằm trên struct nào) | Giá trị thực tế lúc chạy |
| — | Mod khác có patch hàm đó không (cần kiểm lúc chạy) |

Những câu cần đọc thân hàm được đánh dấu **⚠ CHƯA XÁC MINH**, không suy đoán.

⚠️ **Một sai lầm đã mắc, ghi lại để không lặp:** lần probe đầu tra đúng một chuỗi `"RimWorld.CoverUtility"` và báo `NOT FOUND`. Nó nằm ở namespace `Verse`. **Một kết quả null từ tra tên đầy đủ không phân biệt được "không tồn tại" với "sai namespace".** Luôn quét theo tên ngắn trên mọi assembly đã nạp.

---

## 6.1 — `Verb.ShotsPerBurst` có phải virtual property không?

### ✅ CÓ

```
Verse.Verb
    prop    Int32 ShotsPerBurst    VIRTUAL
    prop    Int32 BurstShotCount
    prop    Int32 TicksBetweenBurstShots
    prop    Boolean Bursting
    field   Int32 burstShotsLeft
    field   Int32 ticksToNextBurstShot
    field   Nullable`1 cachedTicksBetweenBurstShots
    field   Nullable`1 cachedBurstShotCount
```

`ShotsPerBurst` là property **virtual** → postfix được, không cần transpiler.

**Chặn:** B1, B6 — cả hai đều mở khoá về mặt điểm móc.

### ⚠ Cạm bẫy phải biết trước khi làm B6

`Verb` có **hai field cache**: `cachedTicksBetweenBurstShots` và `cachedBurstShotCount`, đều `Nullable<int>`.

Nghĩa là `TicksBetweenBurstShots` và `BurstShotCount` **tính một lần rồi nhớ**. Nếu B6 đổi số phát trong loạt theo tư thế, giá trị đã cache có thể **cũ** khi pawn đổi tư thế giữa chừng.

Chưa xác minh cache được xoá lúc nào — **phải trả lời trước khi code B6**, nếu không sẽ có bug "đổi tư thế nhưng loạt bắn vẫn theo tư thế cũ" rất khó truy.

---

## 6.2 — `TryCastNextBurstShot` đọc `ticksBetweenBurstShots` từ đâu?

### ✅ Qua property `Verb.TicksBetweenBurstShots`, có cache

```
Verse.Verb
    method  Int32 get_TicksBetweenBurstShots()      (KHÔNG virtual)
    method  Void TryCastNextBurstShot()
    field   Nullable`1 cachedTicksBetweenBurstShots

Verse.VerbProperties
    field   Int32 ticksBetweenBurstShots
```

Chuỗi dữ liệu: `VerbProperties.ticksBetweenBurstShots` (field cấp Def) → `Verb.TicksBetweenBurstShots` (property, có cache) → `TryCastNextBurstShot`.

⚠️ Property này **không virtual**. Muốn đổi nhịp trong loạt thì postfix chính property getter, **không được ghi vào `VerbProperties.ticksBetweenBurstShots`** — đó là object cấp Def, dùng chung toàn cục (luật 8).

**⚠ CHƯA XÁC MINH:** `TryCastNextBurstShot` đọc property đó mỗi phát hay chỉ đọc một lần đầu loạt. Cần đọc thân hàm.

---

## 6.3 — `AdjustedCooldownTicks` có bị mod khác patch không?

### ✅ Chữ ký xác nhận · ❌ Câu hỏi "mod khác có patch không" **không trả lời được bằng reflection**

```
Verse.VerbProperties
    method  Int32   AdjustedCooldownTicks(Verb, Pawn)
    method  Single  AdjustedCooldown(Verb, Pawn)
    method  Single  AdjustedCooldown(Tool, Pawn, Thing)
    method  Single  AdjustedCooldown(Tool, Pawn, ThingDef, ThingDef)
```

Hook đúng như thiết kế 5.4 giả định: **nhận `Pawn attacker`**, nên chỉnh được theo từng pawn và từng tư thế mà không mutate Def.

Việc mod khác có patch hay không **phụ thuộc modlist**, chỉ kiểm được lúc chạy qua `Harmony.GetPatchInfo`. Cần một debug action liệt kê chủ sở hữu patch trên hàm này — **chưa viết**.

⚠️ Thực tế đã biết: **Yayo's Combat 3 gần như chắc chắn có đụng vào vùng cooldown.** Đây là mod nằm trong modlist khuyến nghị, nên câu hỏi này là thật, không phải giả định.

---

## 6.4 — `verb.burstShotsLeft` có accessible từ ngữ cảnh ShotReport không?

### ✅ CÓ — đã xác minh bằng code sản xuất đang chạy

```
Verse.Verb
    field   Int32 burstShotsLeft
```

`Patch_ShotReport.cs` truy cập nó bằng `AccessTools.FieldRefAccess<Verb, int>("burstShotsLeft")` và hoạt động trong game.

### ⚠️ Nhưng có một bẫy đắt giá, đã trả giá rồi

**`burstShotsLeft` chỉ có nghĩa khi đang trong loạt bắn. Ngoài loạt nó bằng 0.**

Code cũ tính `shotIndex = burstShotCount − burstShotsLeft`, nên ngoài loạt cho ra `shotIndex = burstShotCount` → áp **toàn bộ** phạt giật nòng vĩnh viễn. Đo được: tư thế Rapid ở 3 ô cho 24% trong khi Snap Shot cho 37% — đúng `0.93⁶ = 0.65`.

`ShotReport.HitReportFor` chạy cả khi **rê chuột ngắm**, tức phần lớn thời gian là ngoài loạt.

→ Luôn kiểm `burstShotsLeft > 0` trước khi suy ra chỉ số phát bắn.

---

## 6.5 — Vanilla đã có `factorFromPosture` chưa?

### ✅ CÓ — và đây là phát hiện đáng giá nhất cho tư thế Prone

```
Verse.ShotReport
    prop    Single FactorFromPosture
    prop    Single AimOnTargetChance_IgnoringPosture
    prop    Single AimOnTargetChance_StandardTarget
    prop    Single AimOnTargetChance
```

Vanilla **đã có kênh riêng cho tư thế**, và còn có sẵn một property tính hit chance **bỏ qua** tư thế — dấu hiệu rõ rằng đây là một trục độc lập trong mô hình của game.

### Hệ quả cho Fire Discipline

Hiện Prone được cài bằng cách **nhân vào `factorFromTargetSize`**. Đó là mượn một kênh không thuộc về nó. Kênh đúng đã tồn tại.

⚠ **CHƯA XÁC MINH:** `FactorFromPosture` là property **get-only tính toán** — chưa biết nó tính từ gì (nhiều khả năng từ trạng thái nằm/downed của mục tiêu) và có postfix an toàn không. **Phải đọc thân hàm trước khi chuyển Prone sang kênh này.**

Chưa đổi gì. Ghi lại như một cải tiến kiến trúc ứng viên.

---

## 6.6 — `Verb.Available()` có được gọi đủ thường xuyên để chặn bắn không?

### ✅ Là virtual · ⚠ Tần suất **chưa đo**

```
Verse.Verb
    method  Boolean Available()    VIRTUAL
```

Virtual nên postfix được — `Patch_Verb_Available` đang làm đúng vậy.

**Tần suất gọi phải đo bằng Dubs Performance Analyzer, không đoán được.** Đây là điều kiện của B5 (Pinned): nếu gọi quá thưa thì pawn vẫn bắn được vài phát sau khi đáng lẽ đã bị ghim.

Hiện Pinned mặc định TẮT nên chưa cấp bách.

---

## 6.7 — Vanilla xác định "shoot through" cho embrasure thế nào?

### ✅ Bằng cơ chế **lean** (nhoài người ra bắn), không phải bằng thuộc tính của công trình

```
Verse.ShootLeanUtility
    method  Void    LeanShootingSourcesFromTo(IntVec3, IntVec3, Map, List`1)
    method  Void    CalcShootableCellsOf(List`1, Thing, IntVec3)
    method  Boolean CellCanSeeCell(IntVec3, IntVec3, Map)

Verse.ShotReport
    field   ShootLine shootLine
```

Vanilla không hỏi "công trình này có bắn xuyên được không". Nó tìm **ô nguồn bắn thay thế** quanh người bắn — pawn nhoài người sang ô bên cạnh để có đường ngắm.

### Hệ quả

Khái niệm "embrasure" **không tồn tại trong vanilla**. Không có cờ, không có thuộc tính. Nó chỉ là một công trình mà hình học lean tình cờ cho phép bắn qua.

→ Cách nhận diện của A8 (`Impassable && fillPercent ∈ [0.65, 1.0)`) **là suy luận thay thế hợp lý duy nhất**. Không có API nào tốt hơn để chuyển sang.

⚠ **CHƯA XÁC MINH:** điều kiện chính xác để `LeanShootingSourcesFromTo` chấp nhận một ô. Nếu đọc được, có thể thay heuristic fillPercent bằng câu hỏi đúng: *"vanilla có cho bắn qua ô này không"*.

---

## 6.8 — Tên thật của hàm tính cover, và giá trị trả về *(chặn B3 — GIỜ ĐÃ MỞ)*

### ✅ Tên: `Verse.CoverUtility.CalculateOverallBlockChance`

```
Verse.CoverUtility                                  ← namespace Verse, KHÔNG phải RimWorld
    Single  CalculateOverallBlockChance(LocalTargetInfo target, IntVec3 shooterLoc, Map map)
    List`1  CalculateCoverGiverSet(LocalTargetInfo target, IntVec3 shooterLoc, Map map)
    Single  BaseBlockChance(ThingDef def)
    Single  BaseBlockChance(Thing thing)
    Single  TotalSurroundingCoverScore(IntVec3 c, Map map)
    Boolean ThingCovered(Thing thing, Map map)
```

- Tài liệu thiết kế đoán `CoverUtility.CalculateOverallCover` → **sai tên hàm**
- Reality Report đoán `CalculateOverallBlockChance` → **đúng**

### ✅ Đã gộp trọng số hướng — KHÔNG phải tự tổng hợp từ 8 ô

Hàm **nhận `IntVec3 shooterLoc`**. Giá trị trả về là một `float` đã tính theo hướng người bắn.

Hệ quả thiết kế: **vu hồi tự động làm giảm cover của mục tiêu**, không cần code thêm gì. Đó là nền cơ học cho vòng lặp chiến thuật ở thiết kế 5.10.

### ✅ Khói / ánh sáng KHÔNG bị gộp vào — chúng là field riêng

```
Verse.ShotReport
    field   List`1  covers
    field   Single  coversOverallBlockChance     ← CHỈ cover
    field   Single  factorFromCoveringGas        ← khói, TÁCH RIÊNG
    field   Single  offsetFromDarkness           ← ánh sáng, TÁCH RIÊNG
    field   Single  factorFromWeather
    field   Single  factorFromShooterAndDist
    field   Single  factorFromEquipment
    field   Single  factorFromTargetSize
    field   Single  forcedMissRadius
```

Bố cục field trả lời dứt điểm: **cover, khói và ánh sáng là ba trục độc lập.**

→ Giả định ⚠ số 3 của tài liệu thiết kế (mục 5.8) **được xác nhận đúng**. B3 dùng `coversOverallBlockChance` sẽ chỉ phản ánh cover vật lý, không lẫn khói hay bóng tối.

### 🔑 Và một phát hiện quan trọng hơn: **không cần gọi lại `CoverUtility`**

`ShotReport` **đã mang sẵn** `coversOverallBlockChance` và cả `List covers`.

Fire Discipline **đã** postfix `ShotReport.HitReportFor`. Nghĩa là giá trị cover có thể đọc thẳng từ struct đã tính, thay vì gọi lại `CalculateOverallBlockChance` cho mỗi nạn nhân mỗi viên đạn.

Đây là khác biệt lớn về hiệu năng cho B3 — nhưng lưu ý ngữ cảnh: suppression áp lúc **đạn chạm**, còn `ShotReport` tính lúc **bắn**. Muốn dùng lại thì phải truyền giá trị qua, hoặc chấp nhận gọi lại. Cần bàn khi làm B3.

### ⚠ CHƯA XÁC MINH
- Shield belt có được tính vào `coversOverallBlockChance` không. Không thấy field riêng cho shield trên `ShotReport` — nhiều khả năng shield chặn ở lúc đạn chạm, không ở lúc tính hit chance. **Chưa xác nhận.**
- Giá trị thật của `BaseBlockChance` cho từng loại công trình. Bảng 30/40/55/75% ở thiết kế 5.8 **vẫn là ước lượng** cho tới khi có debug action đo.

---

## 6.9 — `Pawn_PathFollower.StartPath` gọi từ đâu, có đường thay thế an toàn hơn không?

### ⚠ Trả lời một phần

```
Verse.AI.Pawn_PathFollower
    method  Void StartPath(LocalTargetInfo, PathEndMode)
    method  Void StopDead()
```

**Có `StopDead()`** — một API công khai để dừng di chuyển. Đây là ứng viên rõ ràng thay cho việc Prefix trả `false` trên `StartPath` (vấn đề **D1**).

Khác biệt về bản chất:

| | Prefix `false` trên `StartPath` | Gọi `StopDead()` |
|---|---|---|
| Job phía trên | **Vẫn tồn tại**, tưởng pawn đang đi | Được thông báo qua đường vanilla |
| Kiểu can thiệp | Nuốt một lời gọi của vanilla | Dùng API vanilla cung cấp |
| Luật 5 | **Vi phạm** | Nhiều khả năng hợp lệ |

⚠ **CHƯA XÁC MINH:** `StopDead()` làm gì với job đang chạy, và gọi nó từ ngoài có an toàn không. **Phải đọc thân hàm trước khi dùng nó sửa D1.**

Câu "gọi từ đâu" cần phân tích call-site — reflection không làm được. Cần ILSpy thật hoặc dnSpy.

---

## Tổng kết trạng thái

| # | Câu hỏi | Trạng thái | Mở khoá |
|---|---|---|---|
| 6.1 | `ShotsPerBurst` virtual | ✅ Có | B1, B6 |
| 6.2 | Nguồn `ticksBetweenBurstShots` | ✅ Có, kèm cảnh báo cache | B1, B6 |
| 6.3 | Mod khác patch `AdjustedCooldownTicks` | ⚠ Chỉ kiểm được lúc chạy | B6 |
| 6.4 | `burstShotsLeft` accessible | ✅ Có, kèm bẫy đã trả giá | B6 |
| 6.5 | Vanilla có `factorFromPosture` | ✅ **Có** — Prone nên chuyển sang | Cải tiến Prone |
| 6.6 | `Verb.Available()` đủ thường xuyên | ⚠ Virtual ✅, tần suất chưa đo | B5 |
| 6.7 | Vanilla xử "shoot through" | ✅ Bằng cơ chế lean, không có khái niệm embrasure | B4, A8 |
| **6.8** | **Tên hàm cover + ngữ nghĩa** | ✅ **Đã trả lời** — tên, tính hướng, khói/sáng tách riêng | **B3 mở khoá** |
| 6.9 | Thay thế cho Prefix `false` | ⚠ Tìm được `StopDead()`, chưa xác minh | D1 |

**Ba việc còn cần đọc thân hàm** (ILSpy/dnSpy thật, hoặc đo trong game):

1. `FactorFromPosture` tính từ gì → quyết định chuyển Prone sang kênh đúng
2. `StopDead()` làm gì với job → quyết định cách sửa **D1**, blocker phát hành
3. Cache `cachedBurstShotCount` xoá lúc nào → điều kiện của B6

**Một việc chỉ cần đo trong game:** giá trị `BaseBlockChance` thật cho từng loại công trình → điền vào bảng 5.8 và quyết số phận `EmbrasureUtility`.
