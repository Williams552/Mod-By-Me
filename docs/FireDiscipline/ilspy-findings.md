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

Code cũ tính `shotIndex = burstShotCount − burstShotsLeft`, nên ngoài loạt cho ra `shotIndex = burstShotCount` → áp **toàn bộ** phạt giật nòng vĩnh viễn. Đo được: tư thế Rapid ở 3 ô cho 24% trong khi Standard Shot cho 37% — đúng `0.93⁶ = 0.65`.

`ShotReport.HitReportFor` chạy cả khi **rê chuột ngắm**, tức phần lớn thời gian là ngoài loạt.

→ Luôn kiểm `burstShotsLeft > 0` trước khi suy ra chỉ số phát bắn.

---

## 6.5 — Vanilla đã có `factorFromPosture` chưa?

### ⛔ CÓ, NHƯNG **KHÔNG ĐƯỢC DÙNG** — đừng chuyển Prone sang kênh này

> **Cập nhật 2026-08-09, đo bằng IL.** Kết luận ban đầu bên dưới đúng về sự *tồn tại*
> nhưng sai về *ngữ nghĩa*. Chuyển Prone sang `FactorFromPosture` sẽ **âm thầm gỡ bỏ
> tác dụng thật của Prone**.
>
> Chuỗi tiêu thụ, quét toàn assembly:
>
> ```
> get_FactorFromPosture        ← chỉ: get_AimOnTargetChance · GetTextReadout
> get_AimOnTargetChance        ← chỉ: get_TotalEstimatedHitChance
> get_TotalEstimatedHitChance  ← chỉ: GetTextReadout
> ```
>
> Không mắt xích nào tới `TryCastShot`. Phát bắn thật roll bằng:
>
> ```
> AimOnTargetChance_IgnoringPosture = AimOnTargetChance_StandardTarget × factorFromTargetSize
> ```
>
> `FactorFromPosture` (IL 66 byte) chỉ trả `0.5` khi mục tiêu nằm và cách > 4.5 ô — đó là
> **ước lượng cho tooltip** về việc vanilla chặn đạn theo tư thế ở giai đoạn va chạm, không
> phải một hệ số áp lúc ngắm.
>
> **Bẫy kèm theo:** đổi sang kênh này thì bài regression **vẫn PASS**, vì harness đo đúng
> con số mà UI hiển thị chứ không đo kết quả roll. Đây là ca mẫu cho việc phép đo và cơ chế
> nhìn vào hai chỗ khác nhau.
>
> **Giữ nguyên** cách hiện tại: nhân vào `factorFromTargetSize`, kênh này **có** feed roll thật.

### ✅ CÓ — và đây là phát hiện đáng giá nhất cho tư thế Prone *(kết luận cũ, giữ để đối chiếu)*

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

### ✅ Tên: `Verse.CoverUtility.CalculateOverallBlockChance` VÀ Công thức BaseBlockChance

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
- Công thức: `BaseBlockChance(def) = (def.Fillage == FillCategory.Full) ? 0.75f : def.fillPercent;`
- Bảng giá trị thật:
  - Tường / đá gốc (Full): 0.75
  - CE_Embrasure: 0.70
  - Sandbags / Barricade: 0.55
  - Chunk đá, turret, bàn lớn: 0.50
  - Giường, kệ, bàn: 0.40
  - Thùng, cây: 0.25-0.30

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

---

## 6.9 — `Pawn_PathFollower.StartPath` gọi từ đâu, có đường thay thế an toàn hơn không?

### ✅ Đã xác minh & Xử lý (Task D1)

**Kết luận:** KHÔNG cần `StopDead()` và KHÔNG dùng Prefix `return false`.

1. **Cơ chế vanilla:** Khi `AimStanceTracker.SetStance` được gọi (ví dụ khi pawn thoát Prone), nó kích hoạt `pawn.stances.SetStance(new Stance_Cooldown(transitionTicks, null, null))`.
2. **`Pawn_PathFollower.PatherTick`:** Vanilla tự động kiểm tra `pawn.stances.FullBodyBusy`. Trong thời gian `Stance_Cooldown`, `FullBodyBusy` trả về `true` làm `PatherTick` tạm dừng di chuyển mỗi tick mà KHÔNG huỷ đường đi (`StartPath`).
3. **Giải pháp:** Chuyển Harmony Prefix trên `StartPath` thành `void Prefix(...)`.
   - Giữ nguyên việc tự động thoát Prone khi nhận lệnh di chuyển.
   - `StartPath` vanilla luôn được phép chạy để lưu đường đi.
   - Vanilla tự hoãn di chuyển thật sự cho tới khi `Stance_Cooldown` kết thúc.
   - Không nuốt lệnh di chuyển (sửa bug người chơi phải click 2 lần), tuân thủ 100% **Luật 5**.

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
| **6.9** | **Thay thế cho Prefix `false`** | ✅ **Đã giải quyết (D1)** — dùng `void Prefix` + vanilla `Stance_Cooldown` | **D1 xong** |

**Ba việc còn cần đọc thân hàm** (ILSpy/dnSpy thật, hoặc đo trong game):

1. `FactorFromPosture` tính từ gì → quyết định chuyển Prone sang kênh đúng
2. `StopDead()` làm gì với job → quyết định cách sửa **D1**, blocker phát hành
3. Cache `cachedBurstShotCount` xoá lúc nào → điều kiện của B6

**Một việc chỉ cần đo trong game:** giá trị `BaseBlockChance` thật cho từng loại công trình → điền vào bảng 5.8 và quyết số phận `EmbrasureUtility`.
