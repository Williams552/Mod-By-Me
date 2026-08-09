# 🎯 Fire Discipline — RimWorld Tactical Combat Overhaul

[![RimWorld 1.6](https://img.shields.io/badge/RimWorld-1.6-brightgreen.svg)](https://rimworldgame.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Compatibility](https://img.shields.io/badge/Save%20Compatibility-Mid--Save%20Safe-blue.svg)](#-compatibility--load-order--t%C6%B0%C6%A1ng-th%C3%ADch--th%E1%BB%A9-t%E1%BB%B1-load)
[![Download Latest Release](https://img.shields.io/github/v/release/Williams552/Mod-By-Me?label=Download%20Latest%20Release&color=brightgreen&logo=github)](https://github.com/Williams552/Mod-By-Me/releases/latest)

> **[English](#english)** | **[Tiếng Việt](#tiếng-việt)**

---

## English

### 📖 Design Philosophy & Core Intent

**Fire Discipline** adds a tactical combat layer to RimWorld 1.6. It is designed to work alongside vanilla combat mechanics without requiring a new save file or custom XML patches for modded weapons.

The mod is engineered around two primary design goals:
1. **Enhance Tactical Depth:** Give players real squad-level combat choices (Tactical Aim Stances, Movement Suppression, Cover Resistance, Logistics & Encumbrance) so gunfights involve maneuvering, flanking, and counterplay rather than stat-checking.
2. **Eliminate Unfair Frustration:** Remove immersion-breaking vanilla RimWorld RNG moments—such as high-level armored pawns dying instantly from a random stray bullet to the brain, or pawns standing idly under heavy mortar bombardment.

#### ⚙️ Universal Compatibility Rule
Everything in Fire Discipline is **derived dynamically** from vanilla stats and Def attributes (e.g., mass, weapon warmup, cover block chance, Def flags). **No hardcoded weapon lists, no string matching, and no per-mod XML patches.** Weapons and armor from any mod work automatically.

---

### 🌟 Key Features (6 Core Tactical Modules)

Each module can be toggled **ON** or **OFF** independently at any time in **Mod Options**:

#### 🎯 1. Aim Stances & Tactical Postures (`AimStanceModule`)
Drafted pawns gain access to 4 distinct tactical postures via a clean action gizmo on the command bar:
- **Standard (Baseline):** Standard vanilla firing speed and accuracy. Instant free stance switch.
- **Rapid Fire:** Fast close-range hipfire. Reduces weapon warmup time, inflicts **+50% suppression** on targets, but suffers accuracy penalties at long range.
- **Sharpshot (Precision):** Long-range sniper stance ($d \times 0.80$ distance exponent). Suffers **+40% warmup time**, close-range accuracy penalty (<5 cells, -30%), and **+100% suppression vulnerability** (warmup resets if suppressed while aiming).
- **Prone (Dug-In):** Trench posture. Reduces pawn target size by **35%** ($x0.65$), grants **+50% suppression resistance**, but disables movement. Moving automatically exits Prone with a 45-tick transition delay.

#### 🎒 2. Encumbrance & Logistics (`EncumbranceModule`)
- Dynamically applies a `MoveSpeed` penalty based on carried equipment and inventory mass vs pawn `CarryingCapacity` (worn armor is excluded).
- Light skirmishers carrying up to 15% capacity suffer 0 penalty. Heavy loadouts scale up to **-35% MoveSpeed**, rewarding light equipment loadouts.

#### 🛡️ 3. Suppression & Cover Dynamics (`SuppressionCoreModule`)
- **Movement-Focused Suppression:** Built-in lightweight suppression engine (`FD_Suppressed`) that punishes **movement speed** rather than aim locking. Defenders holding cover don't need to run, while attackers are slowed, allowing smaller defending forces to hold chokepoints.
- **Mechanics:** Accumulates +0.25 severity per near-miss shot (scaled by shooter stance, target stance, and cover). Decays at -0.10 severity/sec after a 120-tick (2s) grace period.
- **Stages:** Shaken (0.5), Wavering (1.0), Ducking (2.0), Cowering (5.5) on a 0–9 severity scale. MoveSpeed multipliers: $\times 0.95 \rightarrow \times 0.80 \rightarrow \times 0.50 \rightarrow \times 0.15$ (absolute floor 0.70 cells/sec).
- **Cover Integration:** All cover (sandbags, walls, embrasures) reduces incoming suppression by $\text{clamp}(1 - \text{blockChance} \times 0.85, 0.25, 1.0)$. Embrasures provide cover suppression resistance automatically like any wall. An optional setting adds an accuracy penalty when firing through narrow embrasure slits.
- **Smart Interfacing:** Automatically detects third-party suppression mods (e.g., *Suppression (Continued)*) or *Combat Extended* on first install and defaults to OFF if detected.

#### 🩹 4. Graze System — Anti-One-Shot Protection (`GrazeModule`)
- Protects veteran pawns from instant RNG deaths caused by low-skill raiders.
- Intercepts fatal ranged hits targeting vital organs (*Brain, Head, Eye, Heart, Neck, Spine, Liver*).
- Downgrades lethal blows into a **Graze Shot** (reducing damage by **65%** and rerouting injuries to non-vital outer limbs). Graze probability is derived dynamically from the shot's actual hit chance, making inherently inaccurate shots more likely to graze rather than using a flat base chance.

#### 💥 5. Combat Shock & Shell Shock (`ShockModule`)
- **Ally Downed Shock (`FD_CombatShock`):** Nearby allies within 6.0 cells suffer temporary combat shock when a teammate is downed or killed.
- **Proportional Shell Shock (`FD_ShellShock`):** Explosions generate non-linear concussive shockwaves ($r + 2\sqrt{r}$, capped at 20 cells). Mortar shells (4.9 radius) generate a **9.3-cell concussive wave**, inflicting disorienting Shell Shock with smooth distance falloff.

#### 🔫 6. Shotgun Cone Spread & Danger Zone (`ShotgunAoEModule` — OFF by default)
- Simulates realistic pellet spread, creating a cone of splash damage from muzzle to max range (70% primary damage base, density scaled).
- Includes an on-screen tactical danger zone overlay highlighting friendly pawns in red to prevent accidental teamkills.

---

### 🎛️ Real-Time Mod Options

All parameters can be tuned live in-game under **Options -> Mod Options -> Fire Discipline**:
- **Sharpshot:** Warmup multiplier, distance exponent, close-range penalty, suppression vulnerability.
- **Rapid Fire:** Min/max warmup clamps, inflicted suppression multiplier.
- **Prone Stance:** Target size reduction, accuracy multiplier, suppression resistance.
- **Graze System:** Hit chance ceiling (default 65%), chance span (default 45%), damage multiplier (default 35%).
- **Shock System:** Ally shock radius (6.0c), shell shock cap (20) and coefficient (2.0).
- **Transitions:** Stance transition delay (default 45 ticks).

---

### 📦 Compatibility & Load Order

- **Save Compatibility:** Safe to add or remove mid-playthrough.
- **Modded Weapons/Armor:** 100% compatible out of the box.
- **Recommended Load Order:**
  ```text
  Core -> DLCs -> HugsLib
  Yayo's Combat 3 (Continued)
  Suppression (Continued)
  Fire Discipline                  <-- Load Fire Discipline here
  Simple Sidearms / Run and Gun / Achtung!
  ```

---

## Tiếng Việt

### 📖 Triết Lý Thiết Kế & Mục Tiêu

**Fire Discipline** bổ sung một lớp chiến thuật vào hệ thống chiến đấu của RimWorld 1.6. Mod được thiết kế để hoạt động song song với cơ chế combat vanilla, không yêu cầu tạo save mới và không cần patch XML riêng cho các mod vũ khí khác.

Mod được thiết kế xoay quanh 2 mục tiêu cốt lõi:
1. **Tăng Tính Chiến Thuật:** Cung cấp cho người chơi các công cụ quản lý tiểu đội thực sự (Tư thế ngắm bắn, Áp chế di chuyển, Kháng áp chế từ vật cản, Tải trọng trang bị) để mỗi cuộc chạm súng đòi hỏi di chuyển, bọc lót và phản công chứ không chỉ so chỉ số.
2. **Giảm Ức Chế Phi Lý:** Loại bỏ các tình huống chết ngẫu nhiên phi lý của RimWorld Vanilla—như Pawn mặc giáp xịn bị đạn rác bắn trúng não chết ngay lập tức, hoặc Pawn đứng ngơ người khi bị pháo kích.

#### ⚙️ Nguyên Tắc Tương Thích Tự Động
Toàn bộ thông số trong Fire Discipline được **suy ra tự động** từ stat vanilla và các cờ Def (khối lượng, thời gian ngắm, tỷ lệ cản của vật cản,...). **Không hardcode danh sách vũ khí, không khớp chuỗi tên mod, không cần file patch XML riêng.** Vũ khí và giáp từ mọi mod khác đều tự động tương thích.

---

### 🌟 6 Module Chiến Thuật Cốt Lõi

Mỗi module có thể bật/tắt độc lập và tức thì trong **Options -> Mod Options -> Fire Discipline**:

#### 🎯 1. Tư Thế Tác Chiến (`AimStanceModule`)
Pawn ở trạng thái Draft sở hữu 4 tư thế chiến thuật chuyển đổi linh hoạt qua nút bấm UI:
- **Standard (Mặc định):** Tốc độ và độ chính xác Vanilla tiêu chuẩn. Chuyển đổi miễn phí tức thì.
- **Rapid Fire (Bắn nhanh):** Giảm thời gian ngắm ở cự ly gần, gây **+50% áp chế** lên mục tiêu, nhưng giảm độ chính xác ở khoảng cách xa.
- **Sharpshot (Bắn tỉa):** Tăng độ chính xác tầm xa (hệ số khoảng cách $d \times 0.80$). Tăng **+40% thời gian ngắm**, giảm độ chính xác cự ly gần (<5 ô, -30%), và **dễ bị áp chế +100%** (nếu bị áp chế khi đang ngắm sẽ bị reset thanh ngắm).
- **Prone (Nằm sấp/Bunker):** Giảm kích thước mục tiêu đi **35%** ($x0.65$), tăng **+50% kháng áp chế**, nhưng không thể di chuyển. Di chuyển sẽ tự động thoát Prone với độ trễ chuyển đổi 45 tick.

#### 🎒 2. Tải Trọng Trang Bị (`EncumbranceModule`)
- Phạt tốc độ di chuyển (`MoveSpeed`) dựa trên tổng khối lượng vũ khí + đồ trong túi hành trang so với sức chở (`CarryingCapacity`) của Pawn (không tính giáp đang mặc).
- Mang đồ nhẹ dưới 15% sức chở không bị phạt. Mang nặng tăng dần lên đến **-35% MoveSpeed**, khuyến khích trang bị gọn nhẹ cho lính cơ động.

#### 🛡️ 3. Áp Chế & Vật Cản (`SuppressionCoreModule`)
- **Áp Chế Trừng Phạt Di Chuyển:** Hệ thống áp chế nhẹ (`FD_Suppressed`) tập trung phạt **tốc độ di chuyển** thay vì khóa ngắm. Bên phòng thủ đứng trong cover không cần di chuyển, trong khi bên tấn công bị làm chậm, giúp lực lượng nhỏ giữ chokepoint hiệu quả.
- **Cơ chế:** Tích lũy +0.25 độ nghiêm trọng mỗi phát đạn qua gần (nhân với tư thế bắn, tư thế nhận và vật cản). Giảm -0.10/giây sau 120 tick (2 giây) ân hạn.
- **Các Stage:** Shaken (0.5), Wavering (1.0), Ducking (2.0), Cowering (5.5) trên thang 0–9. Hệ số MoveSpeed: $\times 0.95 \rightarrow \times 0.80 \rightarrow \times 0.50 \rightarrow \times 0.15$ (sàn tuyệt đối 0.70 ô/s).
- **Tương tác Vật Cản (Cover):** Mọi vật cản (bao cát, tường, lỗ châu mai) giảm áp chế theo $\text{clamp}(1 - \text{blockChance} \times 0.85, 0.25, 1.0)$. Lỗ châu mai (Embrasure) tự động kháng áp chế như tường. Tùy chọn nâng cao cho phép thêm phạt độ chính xác khi bắn qua khe hẹp.
- **Tự Động Nhận Diện:** Tự động phát hiện mod áp chế khác (như *Suppression (Continued)*) hoặc *Combat Extended* ở lần cài đầu và tự đặt TẮT để tránh xung đột.

#### 🩹 4. Cơ Chế Graze — Chống Chết Chóc Ngẫu Nhiên (`GrazeModule`)
- Bảo vệ lính kỳ cựu khỏi những đòn chí mạng ngẫu nhiên từ quân địch skill thấp.
- Chặn các phát đạn nguy hiểm nhắm vào nội tạng quan trọng (*Não, Đầu, Mắt, Tim, Cổ, Cột sống, Gan*).
- **Cơ Chế:** Khả năng Graze được suy ra tự động dựa trên tỷ lệ trúng thực tế của viên đạn (đạn càng khó trúng càng dễ sượt). Giảm sát thương đi **65%** và chuyển hướng vết thương ra các chi bên ngoài (*Tay, Chân, Vai*).

#### 💥 5. Shock Đồng Đội & Sóng Xung Kích (`ShockModule`)
- **Ally Downed Shock (`FD_CombatShock`):** Đồng đội trong phạm vi 6.0 ô bị sốc tinh thần tạm thời khi có lính cùng phe bị gục hoặc chết.
- **Proportional Shell Shock (`FD_ShellShock`):** Vụ nổ tạo ra sóng xung kích phi tuyến tính ($r + 2\sqrt{r}$, trần tối đa 20 ô). Đạn pháo (4.9 ô) tạo sóng xung kích **9.3 ô**, gây Shell Shock choáng váng giảm dần theo khoảng cách.

#### 🔫 6. Shotgun Spread & Vùng Nguy Hiểm (`ShotgunAoEModule` — Mặc định TẮT)
- Giả lập độ tỏa đạn shotgun theo hình nêm từ nòng súng ra tầm xa tối đa (70% sát thương gốc, giảm dần theo mật độ).
- Hiển thị vùng nguy hiểm trên màn hình, đánh dấu đỏ đồng đội nằm trong tầm bắn để tránh bắn nhầm.

---

### 🎛️ Tùy Chỉnh Thời Gian Thực

Tất cả thông số có thể điều chỉnh ngay trong game tại **Options -> Mod Options -> Fire Discipline**:
- **Sharpshot:** Hệ số ngắm, hệ số khoảng cách, phạt cự ly gần, điểm yếu áp chế.
- **Rapid Fire:** Giới hạn thời gian ngắm (min/max), hệ số gây áp chế.
- **Prone Stance:** Tỷ lệ kích thước mục tiêu, kháng áp chế.
- **Cơ Chế Graze:** Trần hit-chance (65%), khoảng biên (45%), hệ số giữ sát thương (35%).
- **Shock Đồng Đội:** Bán kính shock (6.0), giới hạn số ô Shell Shock (20), hệ số Shell Shock (2.0).
- **Độ Trễ:** Số tick để chuyển đổi tư thế (mặc định 45 tick).

---

### 📦 Tương Thích & Thứ Tự Load

- **Tương thích Save:** Thêm hoặc gỡ khỏi Save mid-game an toàn tuyệt đối.
- **Vũ khí / Giáp Mod:** Tương thích 100% tự động.
- **Thứ Tự Load Khuyên Dùng:**
  ```text
  Core -> DLCs -> HugsLib
  Yayo's Combat 3 (Continued)
  Suppression (Continued)
  Fire Discipline                  <-- Load Fire Discipline ở đây
  Simple Sidearms / Run and Gun / Achtung!
  ```

---

## 📜 License & Credits

- Created by **William**.
- Source code available under the **MIT License**.
