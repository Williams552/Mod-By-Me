# Fire Discipline — A Tactical Layer & Combat Experience Enhancer

> **No new save required. Works standalone. Automatically integrates with Yayo's Combat 3 and Suppression (Continued).**

---

## 📖 Triết Lý Thiết Kế (Design Philosophy)

**Fire Discipline** không phải là một bản "Combat Overhaul" phức tạp làm vỡ save hay bắt người chơi phải tải lại modlist. 

Mod được thiết kế với duy nhất 2 mục tiêu:
1. **Làm mạnh phần THÚ VỊ:** Cho người chơi các công cụ chiến thuật thực sự (Tư thế bắn, Phục kích góc, Ép áp chế, Phối hợp đội hình) để mỗi trận đấu súng trở nên kịch tính và có phản công (counterplay).
2. **Giảm phần KHÓ CHỊU:** Loại bỏ các tình trạng ức chế phi lý của RimWorld vanilla (Chết ngẫu nhiên vì đạn rác vào Não, Pawn đứng ngơ người khi bị bắn, nổ pháo không có sóng xung kích).

---

## 🌟 Key Features (5 Core Tactical Modules)

### 🎯 1. Aim Modes & Tactical Stances (Module 5.2)
Drafted pawns gain access to 4 distinct tactical stances via a clean Gizmo toggle:
- **SnapShot (Default):** Standard vanilla firing baseline. Instant free switch.
- **Rapid Fire:** Reduced warmup time for close-quarters engagements. Full accuracy at close range, with progressive accuracy dropoff at distance. Inflicts +50% suppression when firing.
- **Sharpshot (Sniper):** Increased long-range precision (distance exponent $d \times 0.80$) and +40% warmup time. Vulnerable at close range (<5 cells, -30% accuracy) and takes +100% suppression (warmup resets if suppressed while aiming).
- **Prone / Cover:** Reduces target size by **35%** (making pawns significantly harder to hit) and grants +50% suppression resistance. Moving automatically exits Prone with a 45-tick transition delay.

### 🎒 2. Encumbrance & Logistics (Module 5.3)
- Dynamically injects speed penalties based on total carried equipment and inventory mass vs carrying capacity (`CarryingCapacity`).
- Pawns carrying up to 15% capacity suffer no penalty. Heavy loads gradually scale up to -35% move speed, rewarding light skirmishers.

### 🛡️ 3. Suppression & Cover Dynamics (Module 5.1)
- **Standalone Mode:** Built-in lightweight suppression engine (`FD_Suppressed`) that applies aiming delays and movement penalties when pawns are under heavy fire.
- **Interfacing Mode:** Automatically detects third-party suppression mods (e.g. `Suppression (Continued)`) and defers to them while modifying suppression build-up based on stances.

### 🩹 4. Graze System — Anti-One-Shot Mechanism (Module 5.4)
- Prevents late-game pawns with max skills and high-tech armor from being randomly killed in one hit by low-skill raiders.
- Intercepts fatal ranged shots targeting vital organs (Brain, Head, Eye, Heart, Neck, Spine, Liver).
- Converts lethal blows into **Graze Shots**, reducing damage by **65%** and rerouting injuries to non-vital outer limbs (Arms, Legs, Shoulders).

### 💥 5. Shock & Proportional Shell Shock System (Module 5.5)
- **Ally Downed Shock:** Nearby allies within 6.0 cells suffer temporary combat shock (`FD_CombatShock`) when a teammate is downed or killed.
- **Proportional Shell Shock:** Explosions dynamically scale shockwave radii ($2.0 \times \text{explosion.radius}$). Mortar shells (4.9c) generate a **9.8-cell concussive shockwave**, inflicting disorienting Shell Shock (`FD_ShellShock`) with smooth distance falloff.

---

## 🎛️ Real-Time Mod Options

All parameters can be tuned live in-game under **Options -> Mod Options -> Fire Discipline**:
- **Sharpshot:** Warmup multiplier, distance exponent, close-range penalty, suppression vulnerability.
- **Rapid Fire:** Min/max warmup clamps, inflicted suppression multiplier.
- **Prone Stance:** Target size reduction, accuracy multiplier, suppression resistance.
- **Graze System:** Base graze chance (default 25%), damage retention % (default 35%), vital organ protection.
- **Shock System:** Ally shock radius (6.0c), shell shock radius multiplier ($x2.0$).
- **Transitions:** Stance transition delay (default 45 ticks).

---

## 📦 Compatibility & Recommended Load Order

- **No new save required:** Can be added or removed mid-playthrough.
- **100% Modded Weapon / Armor Support:** Dynamically derives all stats without requiring XML patches.

### Recommended Load Order:
```text
Core -> DLCs -> HugsLib
Yayo's Combat 3 (Continued)
Suppression (Continued)
Fire Discipline                 <-- Load Fire Discipline here
Simple Sidearms / Run and Gun / Achtung!
```

---

## 📜 License & Credits

Created by **William**. Released under the MIT License.
