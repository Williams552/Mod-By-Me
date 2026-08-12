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
1. **Enhance Tactical Depth ("Strong Defense, Weak Offense"):** Give players real squad-level combat choices (Tactical Aim Stances, Stationary Dug-In Heavy Firepower, Movement Suppression, Cover Resistance, Logistics & Encumbrance) so gunfights reward prepared defensive positions and tactical maneuvering over aggressive rushing.
2. **Eliminate Unfair Frustration:** Remove immersion-breaking vanilla RimWorld RNG moments—such as high-level armored pawns dying instantly from a random stray bullet to the brain, or pawns standing idly under heavy mortar bombardment.

#### ⚙️ Universal Compatibility Rule & Hybrid Weapon Classification
Everything in Fire Discipline is **derived dynamically** from vanilla stats and Def attributes (e.g., mass, weapon warmup, cover block chance, Def flags). **No hardcoded weapon lists, no string matching, and no per-mod XML patches.** Weapons from any mod work automatically using a 2-layer hybrid classification system (Layer 1: Keyword Safety Net for Scatterguns/Shotguns; Layer 2: 5-gate Stat Curve Analysis).

---

### 🌟 Key Features (7 Core Tactical Modules)

Each module can be toggled **ON** or **OFF** independently at any time in **Mod Options**:

#### 🎯 1. Aim Stances & Dug-In Heavy Firepower (`AimStanceModule`)
Drafted pawns cycle between 3 active tactical stances via a clean UI Gizmo, plus 1 automatic passive condition:
- **Standard (Baseline):** Standard vanilla firing speed and accuracy. Instant free stance switch. Includes a **+20% base accuracy boost** (`globalAccuracyMultiplier = 1.20`) and category-based distance decay flattening (Sniper: 0.65 exponent, Rifle/LMG: 0.85 exponent, Shotgun/SMG: 1.00 unchanged to preserve close-quarters roles).
- **Rapid Fire:** Fast close-range hipfire. Reduces weapon warmup time (30%–75%), inflicts **+50% suppression** on targets, but suffers accuracy penalties at long range. Multi-shot shotguns (>1 burst, e.g., Chain Shotgun) suffer a **2.5x higher recoil penalty** ($0.93^{2.5 \times \text{shotIndex}}$) to prevent point-blank burst damage abuse.
- **Sharpshot (Precision):** Long-range sniper stance ($d \times 0.80$ distance exponent). Suffers **+40% warmup time**, close-range accuracy penalty (<5 cells, -30%), and **+100% suppression vulnerability** (receives double suppression severity from incoming fire). Bypasses **50% of target cover block chance**. Burst/automatic weapons suffer **2x double recoil penalty** to prevent LMG sniper abuse.
- **Dug-In / Prone (`FD_DugIn` — "Strong Defense, Weak Offense"):** **Automatic passive condition** granted when standing still in combat for at least **1.5 seconds (90 ticks entry delay)**. 
  - **+20% Firing Speed Boost:** Reduces aiming delay by 20% (`proneWarmupMultiplier = 0.80`).
  - **-60% Recoil Penalty for Heavy Automatics:** LMGs/HMGs/Miniguns ($\text{burst} \ge 5$) gain a 60% recoil reduction when Dug-In (`dugInHeavyRecoilMultiplier = 0.40`), firing laser-tight defensive bursts! Running and gunning on the move retains full recoil penalties.
  - **Defense:** Reduces pawn target size by **35%** ($x0.65$), grants **+50% suppression resistance**. Moving instantly exits Dug-In.
- **AI Enemy Stance Evaluation:** Enemy pawns automatically evaluate target distance and adopt Rapid Fire ($\le 6$c) or Sharpshot ($\ge 30$c) to ensure tactical parity.

#### 🎒 2. Encumbrance & Logistics (`EncumbranceModule`)
- Dynamically applies a `MoveSpeed` penalty based on carried equipment and inventory mass vs pawn `CarryingCapacity` (worn armor is excluded).
- Light skirmishers carrying up to 15% capacity suffer 0 penalty. Heavy loadouts scale up to **-35% MoveSpeed**, rewarding light equipment loadouts.

#### 🛡️ 3. Suppression & Cover Dynamics (`SuppressionCoreModule`)
- **Movement & Firing Suppression:** Built-in lightweight suppression engine (`FD_Suppressed`) that punishes movement speed, aiming delay, and shooting accuracy.
- **Heavy Weapon Suppression Bonus (+100%):** Heavy automatic weapons ($\text{burst} \ge 5$, LMG/HMG/Minigun) inflict **double suppression severity** (`heavyWeaponSuppressionMultiplier = 2.00`), instantly pinning down charging tribal hordes before they close into range!
- **Mechanics:** Accumulates +0.25 severity per near-miss shot (3.5-cell radius). Decays at -0.10 severity/sec after a 120-tick (2s) grace period.
- **Stages:** Shaken (0.5), Wavering (1.0), Ducking (2.0), Cowering (5.5) on a 0–9 severity scale. MoveSpeed multipliers: $\times 0.95 \rightarrow \times 0.80 \rightarrow \times 0.50 \rightarrow \times 0.15$ (absolute floor 0.70 cells/sec).
- 🔴 **Pinned State (Severity $\ge$ 7.0):** Pawns under heavy suppression are **completely blocked from firing ranged weapons** (`Verb.Available = false`) until suppression decays.
- **Cover Integration:** All cover (sandbags, walls, embrasures) reduces incoming suppression proportionally (`coverSuppressionFactor` default 1.00, floor 0.25).
- **Smart Interfacing:** Automatically detects third-party suppression mods (e.g., *Suppression (Continued)*) or *Combat Extended* on first install and defaults to OFF if detected.

#### 🩹 4. Graze System — Anti-One-Shot Protection (`GrazeModule`)
- Protects veteran pawns from instant RNG deaths caused by low-skill raiders.
- Intercepts fatal ranged hits targeting vital organs (*Brain, Head, Eye, Heart, Neck, Spine, Liver*).
- Downgrades lethal blows into a **Graze Shot** (reducing damage by **65%** and rerouting injuries to non-vital outer limbs). Shots with hit chance $\ge 65\%$ never graze.

#### 💥 5. Combat Shock & Shell Shock (`ShockModule`)
- **Ally Downed Shock (`FD_CombatShock`):** Nearby allies within 6.0 cells suffer temporary combat shock (+30% aim delay) when a teammate is downed or killed.
- **Proportional Shell Shock (`FD_ShellShock`):** Explosions generate non-linear concussive shockwaves ($r_{\text{eff}} = \min(20, r + 2\sqrt{r})$). Mortar shells (4.9 radius) generate a **9.3-cell concussive wave**, inflicting disorienting Shell Shock.
- 🛡️ **Energy Shield Absorption:** Active energy shields (`CompShield`) absorb up to **85% of shell shock severity** based on remaining shield energy fraction.

#### 1. 6. Shotgun Cone Spread & Danger Zone (`ShotgunAoEModule` — OFF by default)
- Simulates realistic pellet spread, creating a cone of splash damage (70% primary damage base).
- **Outer Limb Protection:** Splash damage strictly targets outer limbs (Arm/Leg/Shoulder), never penetrating vital organs.
- Includes an on-screen tactical danger zone overlay highlighting friendly pawns in red.

#### 🎲 7. Hit Variance Mitigation & Quota Engine (`VarianceModule` — OFF by default)
- Eliminates frustrating RNG miss streaks by enforcing exact theoretical DPS expectations across all ranged weapons.
- **Unified Quota-Carry Model:** Accumulates true shot hit probability $p$ into a per-pawn carry tracker. Whenever `carry >= 1.0`, the next shot is **guaranteed to hit (100% Force Hit)** and subtracts 1.0 from the accumulator.
- Automatically bypasses forced-miss weapons (mortars, grenades).

---

### 🎛️ Real-Time Mod Options

All parameters can be tuned live in-game under **Options -> Mod Options -> Fire Discipline**:
- **Accuracy & Distance Decay:** Global Base Accuracy Multiplier (x1.20), Sniper Distance Decay Flattener (0.65), Rifle/LMG Distance Decay Flattener (0.85).
- **Dug-In & Heavy Weapons:** Entry Delay (90 ticks / 1.5s), Firing Speed Boost (x0.80), Heavy Recoil Reduction (x0.40 / -60%), Heavy Weapon Suppression Bonus (x2.00 / +100%).
- **Sharpshot:** Warmup multiplier, distance exponent, close-range penalty, suppression vulnerability, cover bypass factor.
- **Rapid Fire:** Min/max warmup clamps, inflicted suppression multiplier, multi-shot shotgun recoil multiplier ($x2.50$).
- **Graze System:** Hit chance ceiling (default 65%), chance span (default 45%), damage multiplier (default 35%).
- **Suppression & Cover:** Cover suppression factor (0.00–3.00, default 1.00), pinned threshold (7.0).

---

## Tiếng Việt

### 📖 Triết Lý Thiết Kế & Mục Tiêu

**Fire Discipline** bổ sung một lớp chiến thuật vào hệ thống chiến đấu của RimWorld 1.6. Mod được thiết kế để hoạt động song song với cơ chế combat vanilla, không yêu cầu tạo save mới và không cần patch XML riêng cho các mod vũ khí khác.

Mod được thiết kế xoay quanh 2 mục tiêu cốt lõi:
1. **Tăng Tính Chiến Thuật ("Thủ Mạnh, Công Yếu"):** Cung cấp cho người chơi các công cụ quản lý tiểu đội thực sự (Tư thế ngắm bắn, Hỏa lực súng nặng khi cố thủ, Áp chế di chuyển, Kháng áp chế từ vật cản, Tải trọng trang bị) để thưởng cho các vị trí phòng thủ chuẩn bị trước và di chuyển bọc lót thay vì càn quét vô não.
2. **Giảm Ức Chế Phi Lý:** Loại bỏ các tình huống chết ngẫu nhiên phi lý của RimWorld Vanilla—như Pawn mặc giáp xịn bị đạn rác bắn trúng não chết ngay lập tức, hoặc Pawn đứng ngơ người khi bị pháo kích.

#### ⚙️ Nguyên Tắc Tương Thích Tự Động & Phân Loại Vũ Khí Phức Hợp
Toàn bộ thông số trong Fire Discipline được **suy ra tự động** từ stat vanilla và các cờ Def (khối lượng, thời gian ngắm, tỷ lệ cản của vật cản,...). **Không hardcode danh sách vũ khí, không khớp chuỗi tên mod, không cần file patch XML riêng.** Vũ khí từ mọi mod khác đều tự động tương thích nhờ hệ thống phân loại lai 2 tầng (Tầng 1: Lưới an toàn từ khóa Scattergun/Shotgun; Tầng 2: 5 cổng lọc độ phẳng đường cong Stat Curve).

---

### 🌟 7 Module Chiến Thuật Cốt Lõi

Mỗi module có thể bật/tắt độc lập và tức thì trong **Options -> Mod Options -> Fire Discipline**:

#### 🎯 1. Tư Thế Tác Chiến & Hỏa Lực Cố Thủ HMG (`AimStanceModule`)
Pawn ở trạng thái Draft có thể chuyển đổi giữa 3 tư thế chiến thuật qua nút bấm Gizmo UI, cộng 1 trạng thái thụ động tự động:
- **Standard (Mặc định):** Tốc độ và độ chính xác tiêu chuẩn. Tự động **tăng 20% độ chính xác gốc (`globalAccuracyMultiplier = 1.20`)** và phân cấp làm phẳng dốc tầm xa (Sniper: lũy thừa 0.65, Rifle/LMG: lũy thừa 0.85, Shotgun/SMG: 1.00 giữ nguyên bản chất cận chiến).
- **Rapid Fire (Bắn nhanh):** Giảm thời gian ngắm ở cự ly gần (30%–75%), gây **+50% áp chế** lên mục tiêu, nhưng giảm độ chính xác ở khoảng cách xa. Shotgun bắn loạt (>1 viên/loạt, như Chain Shotgun) chịu **phạt giật nòng gấp 2.5 lần LMG** ($0.93^{2.5 \times \text{shotIndex}}$) để tránh lạm dụng dồn sát thương tầm gần.
- **Sharpshot (Bắn tỉa):** Tăng độ chính xác tầm xa (hệ số khoảng cách $d \times 0.80$). Tăng **+40% thời gian ngắm**, giảm độ chính xác cự ly gần (<5 ô, -30%), **dễ bị áp chế +100%** (nhận gấp đôi độ nghiêm trọng áp chế từ đạn sượt qua). Bỏ qua **50% tỷ lệ nấp (`Cover Block Chance`)** của mục tiêu. Vũ khí bắn loạt bị **phạt giật nòng gấp 2 lần** để chống lạm dụng LMG bắn tỉa.
- **Dug-In / Prone (`FD_DugIn` — Triết lý "Thủ Mạnh, Công Yếu"):** **Trạng thái thụ động tự động** khi đứng yên cố thủ trong combat tối thiểu **1.5 giây (90 ticks entry delay)**.
  - **Tăng 20% Tốc độ bắn:** Giảm thời gian ngắm Aiming Delay còn 80% (`proneWarmupMultiplier = 0.80`).
  - **Giảm 60% Phạt Giật Nòng Súng Hạng Nặng:** LMG/HMG/Minigun ($\text{burst} \ge 5$) khi cố thủ Dug-In được triệt tiêu 60% giật nòng (`dugInHeavyRecoilMultiplier = 0.40`), xả loạt đạn dồn thẳng vào 1 điểm. Khi di chuyển/chạy bắn vẫn bị phạt giật nòng $0.93^{\text{shotIndex}}$ như gốc.
  - **Phòng thủ:** Giảm kích thước mục tiêu đi **35%** ($x0.65$), tăng **+50% kháng áp chế**. Di chuyển sẽ tự động thoát Dug-In.
- **Tự Động Chọn Tư Thế Cho AI Kẻ Địch:** NPC tự động chọn Rapid Fire ($\le 6$c) hoặc Sharpshot ($\ge 30$c) dựa trên khoảng cách tới mục tiêu để đảm bảo cân bằng.

#### 🎒 2. Tải Trọng Trang Bị (`EncumbranceModule`)
- Phạt tốc độ di chuyển (`MoveSpeed`) dựa trên tổng khối lượng vũ khí + đồ trong túi hành trang so với sức chở (`CarryingCapacity`) của Pawn (không tính giáp đang mặc).
- Mang đồ nhẹ dưới 15% sức chở không bị phạt. Mang nặng tăng dần lên đến **-35% MoveSpeed**, khuyến khích trang bị gọn nhẹ cho lính cơ động.

#### 🛡️ 3. Áp Chế & Vật Cản (`SuppressionCoreModule`)
- **Áp Chế Trừng Phạt Di Chuyển & Khóa Bắn:** Hệ thống áp chế nhẹ (`FD_Suppressed`) tập trung phạt tốc độ di chuyển, thời gian ngắm và độ chính xác ngắm bắn.
- **Thưởng +100% Áp Chế Súng Hạng Nặng:** Súng tự động hạng nặng ($\text{burst} \ge 5$, HMG/LMG/Minigun) gây **gấp đôi độ nghiêm trọng áp chế** (`heavyWeaponSuppressionMultiplier = 2.00`), ép đợt càn bầy người vào trạng thái Ducking/Cowering từ cự ly 25–30 ô!
- **Cơ chế:** Tích lũy +0.25 độ nghiêm trọng mỗi phát đạn qua gần (bán kính 3.5 ô). Giảm -0.10/giây sau 120 tick (2 giây) ân hạn.
- **Các Stage:** Shaken (0.5), Wavering (1.0), Ducking (2.0), Cowering (5.5) trên thang 0–9. Hệ số MoveSpeed: $\times 0.95 \rightarrow \times 0.80 \rightarrow \times 0.50 \rightarrow \times 0.15$ (sàn tuyệt đối 0.70 ô/s).
- 🔴 **Trạng thái Bị Ghim (Pinned State - Severity $\ge$ 7.0):** Pawn bị áp chế nặng sẽ **KHÓA HOÀN TOÀN KHẢ NĂNG BẮN TRẢ** (`Verb.Available = false`) cho đến khi mức áp chế giảm xuống.
- **Tương tác Vật Cản (Cover):** Mọi vật cản (bao cát, tường, lỗ châu mai) giảm áp chế theo tỷ lệ nấp của vật đó (`coverSuppressionFactor` mặc định 1.00, sàn 0.25).
- **Tự Động Nhận Diện:** Tự động phát hiện mod áp chế khác (như *Suppression (Continued)*) hoặc *Combat Extended* ở lần cài đầu và tự đặt TẮT để tránh xung đột.

#### 🩹 4. Cơ Chế Graze — Chống Chết Chóc Ngẫu Nhiên (`GrazeModule`)
- Bảo vệ lính kỳ cựu khỏi những đòn chí mạng ngẫu nhiên từ quân địch skill thấp.
- Chặn các phát đạn nguy hiểm nhắm vào nội tạng quan trọng (*Não, Đầu, Mắt, Tim, Cổ, Cột sống, Gan*).
- **Cơ Chế:** Khả năng Graze được suy ra tự động dựa trên tỷ lệ trúng thực tế của viên đạn. Giảm sát thương đi **65%** và chuyển hướng vết thương ra các chi bên ngoài (*Tay, Chân, Vai*). Phát bắn có tỷ lệ trúng $\ge 65\%$ không bao giờ Graze.

#### 💥 5. Shock Đồng Đội & Sóng Xung Kích (`ShockModule`)
- **Ally Downed Shock (`FD_CombatShock`):** Đồng đội trong phạm vi 6.0 ô bị sốc tinh thần tạm thời (+30% thời gian ngắm) khi có lính cùng phe bị gục hoặc chết.
- **Proportional Shell Shock (`FD_ShellShock`):** Vụ nổ tạo ra sóng xung kích phi tuyến tính ($r_{\text{eff}} = \min(20, r + 2\sqrt{r})$). Đạn pháo (4.9 ô) tạo sóng xung kích **9.3 ô**, gây Shell Shock choáng váng giảm dần theo khoảng cách.
- 🛡️ **Khiên Năng Lượng Hấp Thụ Choáng:** Khiên cá nhân (`CompShield`) hấp thụ tới **85% độ sốc xung kích** từ vụ nổ dựa trên tỷ lệ năng lượng khiên còn lại.

#### 🔫 6. Shotgun Spread & Vùng Nguy Hiểm (`ShotgunAoEModule` — Mặc định TẮT)
- Giả lập độ tỏa đạn shotgun theo hình nêm từ nòng súng ra tầm xa tối đa (70% sát thương gốc).
- **Bảo Vệ Nội Tạng:** Sát thương lan của Shotgun chỉ gây thương tích ở các chi ngoài (Tay/Chân/Vai), không bao giờ chọc thủng nội tạng.
- Hiển thị vùng nguy hiểm trên màn hình, đánh dấu đỏ đồng đội nằm trong tầm bắn để tránh bắn nhầm.

#### 🎲 7. Khống Chế RNG & Tích Lũy Quota Trúng Đạn (`VarianceModule` — Mặc định TẮT)
- Loại bỏ chuỗi đạn trượt ngẫu nhiên phi lý (RNG miss streaks), bảo đảm tổng DPS thực tế trùng khớp 100% với DPS lý thuyết.
- **Mô Hình Quota-Carry Đồng Nhất:** Tích lũy xác suất trúng đạn thực tế $p$ vào bộ đếm `carry` của từng Pawn cho đến khi `carry >= 1.0`, phát bắn tiếp theo **ÉP TRÚNG 100% (Force Hit)** và trừ bớt 1.0.
- Tự động bỏ qua vũ khí có bán kính trượt bắt buộc (súng cối, lựu đạn).

---

### 🎛️ Tùy Chỉnh Thời Gian Thực

Tất cả thông số có thể điều chỉnh ngay trong game tại **Options -> Mod Options -> Fire Discipline**:
- **Độ Chính Xác & Cự Ly:** Hệ số tăng Acc gốc (x1.20), Hệ số làm phẳng tầm xa Sniper/DMR (0.65), Hệ số làm phẳng tầm xa Rifle/LMG/AR (0.85).
- **Cố Thủ Dug-In & Súng Hạng Nặng:** Đô trễ cố thủ (90 ticks / 1.5s), Thưởng tốc độ bắn Dug-In (x0.80), Giảm giật nòng súng nặng khi Dug-In (x0.40 / -60%), Thưởng áp chế súng hạng nặng (x2.00 / +100%).
- **Sharpshot:** Hệ số ngắm, hệ số khoảng cách, phạt cự ly gần, điểm yếu áp chế, tỷ lệ xuyên nấp.
- **Rapid Fire:** Giới hạn thời gian ngắm (min/max), hệ số gây áp chế, hệ số phạt giật nòng Shotgun bắn loạt ($x2.50$).
- **Graze System:** Trần hit-chance (65%), khoảng biên (45%), hệ số giữ sát thương (35%).
- **Áp Chế & Vật Cản:** Hệ số giảm áp chế vật nấp (0.00–3.00, mặc định 1.00), ngưỡng bị ghim (7.0).

---

## 📜 License & Credits

- Created by **William**.
- Source code available under the **MIT License**.
