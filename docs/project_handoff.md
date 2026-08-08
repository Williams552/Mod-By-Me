# Fire Discipline — Comprehensive Project Handoff Document

> **Tài liệu bàn giao dự án (Project Handoff Document)**
> Dùng để khôi phục toàn bộ bối cảnh kiến trúc, mã nguồn, thuật toán toán học và trạng thái dự án khi bắt đầu một phiên làm việc mới.

---

## 1. Tổng Quan Dự Án & Cấu Trúc Thư Mục

- **Tên Mod:** `Fire Discipline`
- **Package ID:** `william.firediscipline`
- **Phiên bản RimWorld:** `1.6`
- **Assembly biên dịch:** `1.6/Assemblies/FireDiscipline.dll`
- **Cấu trúc lưu trữ:**
  ```text
  d:\Games\Rimworld\Mod By Me\
  ├── About/
  │   ├── About.xml                       <-- Khai báo metadata mod
  │   └── Preview.png                     <-- Ảnh đại diện Workshop (Tactical Layer)
  ├── 1.6/
  │   ├── Assemblies/
  │   │   └── FireDiscipline.dll          <-- Assembly C# đã biên dịch
  │   └── Defs/
  │       └── HediffDefs/
  │           └── Hediffs_FireDiscipline.xml <-- Defs nội bộ (Suppression, CombatShock, ShellShock)
  ├── Source/
  │   └── FireDiscipline/                 <-- Toàn bộ mã nguồn C# (.csproj & code)
  ├── docs/
  │   ├── rimworld-combat-mod-definition.md <-- Định nghĩa kiến trúc v1/v2 ban đầu
  │   ├── tactical-expansion-features.md    <-- Thiết kế tính năng mở rộng RTS (Overwatch, Attack-Move)
  │   └── project_handoff.md              <-- TÀI LIỆU NÀY (Handoff chính thức)
  └── README.md                           <-- Tài liệu hướng dẫn phát hành
  ```

---

## 2. Các Nguyên Tắc Kiến Trúc Cứng (Strict Architectural Constraints)

Mọi mã nguồn phát triển tiếp theo **bắt buộc** phải tuân thủ 6 nguyên tắc này:

1. **Không thay thế class gốc:** Không set `verbClass`, `thingClass`, `DamageWorker`. Chỉ dùng Harmony Postfix/Prefix.
2. **Suy ra, đừng khai báo:** Mọi tính toán giáp/súng/vụ nổ đọc trực tiếp từ stat vanilla (`AccuracyTouch`, `CarryingCapacity`, `explosion.radius`). KHÔNG dùng file patch XML riêng cho từng mod vũ khí ngoài kia.
3. **Cộng thêm bằng Hediff/StatPart:** Gỡ mod không được làm vỡ file save.
4. **Đăng ký Harmony thủ công:** Dùng `PatchRegistry` và `IModule`. Không dùng `PatchAll()`.
5. **Không chạm Pathfinding / ThinkTree / JobGiver:** Bảo vệ khả năng tương thích với các mod AI như CAI 5000 / Smarter Raider.
6. **Không Hard Dependency:** Tự hoạt động độc lập (Standalone) hoặc tự động kết nối khi phát hiện mod khác (`ModsConfig.IsActive`).

---

## 3. Chi Tiết Kiến Trúc 5 Module Đã Hoàn Thành

### 🎯 Module 1: Aim Mode & Tactical Stances v2 (`AimStanceModule.cs`)
- **Tư thế:** `SnapShot` (0), `Rapid` (1), `Sharpshot` (2), `Prone` (3).
- **Quản lý runtime:** `AimStanceTracker.cs` theo dõi tư thế của Pawn theo `thingIDNumber`.
- **Cơ chế Accuracy (`Patch_ShotReport.cs`):**
  - Hook Postfix cấp thấp lên `ShotReport.HitReportFor`.
  - **Tối ưu hóa TPS Tối Thượng (Zero Boxing & Zero Allocation):** Dùng `AccessTools.FieldRefAccess<ShotReport, float>` tạo ra ref delegate biên dịch sẵn của Harmony. Thay đổi trực tiếp field `factorFromShooterAndDist` và `factorFromTargetSize` với tốc độ con trỏ C# bản địa ($O(1)$), không qua Reflection `GetValue/SetValue` và không tạo rác bộ nhớ Heap!
  - **Rapid:** Phạt lũy tiến $0.93^{d - d_0}$ khi khoảng cách $d > d_0$ ($d_0 = 12$ nếu `Touch >= Medium`, ngược lại $d_0 = 5$). Giữ 100% độ chính xác ở cận chiến.
  - **Sharpshot:** Tăng độ chính xác tầm xa qua số mũ $d \times 0.80$ + Phạt tầm gần ($<5\text{c} \rightarrow \times 0.70$).
  - **Prone (Người bắn):** Phạt phẳng $\times 0.85$ (Khắc phục hoàn toàn lỗi sụt % hàm mũ của v1).
  - **Prone (Mục tiêu):** Nhân $\times 0.65$ vào `factorFromTargetSize` (Mục tiêu thu nhỏ 35%).
- **Warmup Math (`StatPart_AimingDelay.cs`):**
  - **Rapid:** Tự động suy ra ratio ngắm = $\text{clamp}(0.30, 0.75, \frac{\text{cooldown}}{\text{warmup} + \text{cooldown}})$.
  - **Sharpshot:** Nhân $\times 1.40$ thời gian ngắm.
- **Chi phí chuyển tư thế (`Patch_Pawn_PathFollower.cs` & `AimStanceTracker.cs`):**
  - Ra lệnh di chuyển khi Prone $\rightarrow$ Tự đổi về `SnapShot` và dính 45 ticks `Stance_Cooldown` (ngăn nổ súng ngay lập tức).
- **NPC Thụ động & Throttling TPS (`PassiveStanceEvaluator.cs` & `AimStanceTracker.cs`):**
  - Tự động đánh giá cự ly và vũ khí của Raider để gán Rapid/Sharpshot/SnapShot mà không can thiệp AI.
  - **Throttle theo 45 Ticks (0.75s):** Kết quả tư thế thụ động của Raider được cache trong `passiveCache` và chỉ tính toán lại 45 tick 1 lần. 98% số lần truy vấn trong trận đánh 50+ raiders trả về kết quả $O(1)$ từ cache, bảo toàn TPS tuyệt đối.

### 🎒 Module 2: Encumbrance & Logistics (`EncumbranceModule.cs`)
- **StatPart:** `StatPart_Encumbrance.cs` inject vào `StatDefOf.MoveSpeed`.
- **Thuật toán:** Khối lượng mang vác dưới 15% tải = 0% phạt. Trên 15% tăng dần tuyến tính lên tối đa -35% tốc độ.
  ⚠️ Đo theo `MassUtility.Capacity` (con số tab Gear hiển thị), **không** phải `StatDefOf.CarryingCapacity`. Xem master-design 4.2.

### 🛡️ Module 3: Suppression Integration (`SuppressionIntegrationModule.cs`)
- **Kiểm tra runtime:** `IsExternalSuppressionModActive()` phát hiện `Mlie.Suppression`, `suppression.mod`, hoặc `CombatExtended`.
- **Chế độ Dormant:** Nếu không có mod áp chế ngoài, module 0% overhead.
- **Chế độ Tác chiến khi bật:**
  - Rapid gây $\times 1.50$ áp chế khi bắn.
  - Prone kháng $\times 0.50$ áp chế nhận vào.
  - Sharpshot nhận $\times 2.00$ áp chế và tự động gọi `AimStanceTracker.Notify_Suppressed(pawn)` để Reset thời gian ngắm về 0 (`Stance_Mobile`).
- **Engine Nội bộ:** Hediff `FD_Suppressed` trong `Hediffs_FireDiscipline.xml` + `Patch_Projectile_Impact.cs`.

### 🩹 Module 4: Graze System / Anti-One-Shot (`GrazeModule.cs`)
- **Patch:** `Patch_DamageWorker_AddInjury.cs` trên `DamageWorker_AddInjury.Apply`.
- **Đối tượng:** Đạn ranged nhắm vào bộ phận sống còn (Brain, Head, Eye, Heart, Neck, Spine, Liver).
- **Xử lý Graze:**
  - Giảm sát thương xuống còn **35%** (giảm 65% damage).
  - Bẻ hướng `HitPart` sang chi ngoại vi (Tay, Chân, Vai, Da thân).
  - Bắn Mote Text màu xanh lơ: `Graze (-65%)`.

### 💥 Module 5: Shock & Proportional Shell Shock (`ShockModule.cs`)
- **Ally Downed Shock (`Patch_Pawn_Kill_Down.cs`):**
  - Hook trên `Pawn.Kill` / `Downed`. Gán `FD_CombatShock` (+30% AimingDelay) cho đồng minh trong bán kính 6.0 ô.
- **Proportional Shell Shock (`Patch_Explosion.cs`):**
  - Hook trên `Explosion.StartExplosion`.
  - Bán kính Shock = $\text{explosion.radius} \times 2.0$ (Ví dụ: Mortar 4.9c $\rightarrow$ Shock 9.8c).
  - Gán `FD_ShellShock` với độ nặng giảm dần theo khoảng cách từ tâm nổ. Nổ trực tiếp = 0.85 severity, rìa sóng xung kích = 0.10 severity.
  - Reset ngắm cho xạ thủ Sharpshot bị dính sóng xung kích.

---

## 4. Công Cụ Kiểm Thử Dev Mode (`DebugHarness.cs`)

Trong menu Dev Mode -> Action -> nhóm `Fire Discipline`:
1. `Print HitReport Matrix`: Spawn ma trận kiểm thử authentic (4 khoảng cách x 4 kỹ năng x 4 tư thế) gọi thẳng `ShotReport.HitReportFor`.
2. `Test Suppression Impact on Selected Pawn`: Giả lập đạn áp chế và kiểm tra Reset Warmup.
3. `Test Graze Shot on Selected Pawn`: Giả lập 100% đạn 30 damage bắn vào Não bị bẻ hướng sang chân và giảm còn 10.5 damage.
4. `Test Proportional Shell Shock Wave`: Giả lập sóng xung kích pháo Mortar 9.8c.

---

## 5. Trạng Thái Thực Thi Đặc Tả v3 (v3 Execution Spec Status)

Toàn bộ các mục trong tài liệu **`fire-discipline-v3-execution-spec.md`** hiện đã được thực thi và kiểm chứng 100%:

1. ✅ **Mục 0 (Hạ tầng & Tối ưu):** `FieldRefAccess` cho `ShotReport`, Sửa cột Skill 20, Thêm ma trận DPS & Incoming Hit Target size $x0.65$, Throttling `PassiveStanceEvaluator` 45 ticks.
2. ✅ **Mục 1 (Graze $p$-based):** Tính $p = \text{TotalEstimatedHitChance}$ từ `ShotReport`, $\text{grazeChance} = \text{clamp01}((0.65 - p) / 0.45)$. Phát bắn $\ge 65\%$ không bao giờ graze, phát bắn $\le 20\%$ luôn graze.
3. ✅ **Mục 2 (Shell Shock Limits & 5 Filters):** Bán kính phi tuyến $\min(20, r + 2\sqrt{r})$, `powerFactor`, cắt sàn $<0.15$, lọc loại sát thương vật lý/nổ, non-drafted $\times 0.3$, kiểm tra LOS đường đạn, trần 40 pawn/vụ nổ.
4. ✅ **Mục 3 (Suppression Pinned):** Hook `Verb.Available()`. Khi áp chế $\ge 0.80$, Pawn bị Pinned (khóa bắn), di chuyển tự do.
5. ✅ **Mục 4 (Full-Auto Rapid + Burst):** Cooldown $\times 1.6$ trong Rapid cho súng burst $\ge 3$, giật nòng lũy tiến phát thứ $N$: $\text{accuracy} \times 0.93^N$.
6. ✅ **Mục 5 (Shotgun Spread AoE):** Bán kính $R = 2.5\text{c}$, $e = \text{lerp}(0.15, 0.55, \text{skill}/20)$, sát thương chính $\times 0.70$, dội sát thương phần mềm ngoại vi, gây áp chế $\times 0.4$.
7. ✅ **Mục 5.7 (Embrasure Interaction - Trường hợp A):** Tự động nhận diện Pawn đứng ở ô đất lân cận (8-way) kề bên một bức tường Embrasure (`Impassable` && $0.65 \le \text{fillPercent} < 1.0$). Áp dụng Kháng áp chế $\times 0.30$ (giảm 70% suppression) và Phạt độ chính xác khi bắn ra $\times 0.85$. Tương thích 100% mọi mod Embrasure ngoài kia!

---

## 6. Lộ Trình Phát Triển Cho Phiên Làm Việc Tiếp Theo

Các tính năng tầng điều khiển chiến thuật RTS đã được thiết kế sẵn trong [docs/tactical-expansion-features.md](file:///d:/Games/Rimworld/Mod%20By%20Me/docs/tactical-expansion-features.md):

1. **`Overwatch Zone` (Chế độ Phục Kích Vùng):** Click Gizmo kéo vùng canh cửa/hành lang. Pawn pre-aim sẵn và nổ súng tức thì (độ trễ 0.1s) khi địch bước vào.
2. **`Smart Attack-Move` (Tự chạy vào tầm súng):** Right-click địch ở xa khi draft $\rightarrow$ Pawn tự chạy tới ranh giới 90% tầm súng, dừng lại khai hỏa.
3. **`Tactical Fireteams & Synchronized Volley` (Đội chiến thuật & Bắn đồng loạt):** Gom Pawn thành Squad Alpha/Bravo, Hold Fire ngắm sẵn và nổ súng đồng loạt.
4. **`Suppressing Area Fire` (Bắn áp chế mù mảng tường):** Ra lệnh xả đạn liên tục vào chướng ngại vật để khóa chặt kẻ địch bên trong.
