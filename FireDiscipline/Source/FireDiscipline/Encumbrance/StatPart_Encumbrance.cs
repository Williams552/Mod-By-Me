using FireDiscipline.AimStance;
using FireDiscipline.Core;
using RimWorld;
using Verse;

namespace FireDiscipline.Encumbrance
{
    /// <summary>
    /// StatPart can thiệp trực tiếp vào stat Tốc độ di chuyển (StatDefOf.MoveSpeed).
    /// 
    /// [TÍNH NĂNG / FEATURE]: Module Tải trọng & Hành trang (EncumbranceModule).
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Khuyến khích người chơi trang bị linh hoạt (lính cơ động mang đồ nhẹ vs lính hạng nặng); 
    ///     phạt di chuyển hợp lý dựa trên trọng lượng vũ khí + đồ trong túi mà không phạt trùng lặp lên bộ giáp đang mặc (do giáp đã có stat penalty riêng của Vanilla).
    /// [ĐIỀU CHỈNH MẶC ĐỊNH / DEFAULTS]: Ngưỡng sức chở = 0.15 (15% capacity không phạt). Mức phạt tối đa = 0.35 (-35% MoveSpeed).
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Đọc tổng khối lượng đồ cầm tay + túi hành trang (`CarriedMass`), so sánh với `CarryingCapacity` của Pawn, 
    ///     và tính toán hệ số trừ MoveSpeed hiển thị minh bạch trên bảng thông tin nhân vật (Stat Sheet).
    /// </summary>
    public class StatPart_Encumbrance : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing || !(req.Thing is Pawn pawn) || pawn.Dead)
                return;

            // This StatPart carries two effects owned by two different modules, so each is gated
            // independently. Once injected a StatPart cannot be removed from the StatDef, so these
            // guards are what make switching a module OFF mid-session take effect immediately.
            if (PatchRegistry.IsModuleEnabled(EncumbranceModule.Id))
            {
                float penaltyMultiplier = GetEncumbranceMultiplier(pawn);
                val *= penaltyMultiplier;
            }

            if (PatchRegistry.IsModuleEnabled(AimStanceModule.Id)
                && AimStanceTracker.IsDugIn(pawn))
            {
                float proneMult = FireDisciplineMod.Settings?.proneMoveSpeedMultiplier ?? 0.60f;
                val *= proneMult;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Pawn pawn && !pawn.Dead)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                if (PatchRegistry.IsModuleEnabled(AimStanceModule.Id)
                    && AimStanceTracker.IsDugIn(pawn))
                {
                    float proneMult = FireDisciplineMod.Settings?.proneMoveSpeedMultiplier ?? 0.60f;
                    sb.AppendLine($"Fire Discipline Passive (Dug-In / Prone): x{(int)(proneMult * 100f)}%");
                }

                float multiplier = PatchRegistry.IsModuleEnabled(EncumbranceModule.Id)
                    ? GetEncumbranceMultiplier(pawn)
                    : 1.0f;
                if (multiplier < 0.999f)
                {
                    float totalMass = CarriedMass(pawn);
                    float capacity = MassUtility.Capacity(pawn);
                    float pct = (1f - multiplier) * 100f;
                    // Says "carried" out loud: the player can see worn armour is not counted here,
                    // and that vanilla's own apparel lines above are the whole cost of wearing it.
                    sb.AppendLine($"Fire Discipline Encumbrance (carried {totalMass:F1}kg / {capacity:F1}kg): -{pct:F1}%");
                }

                return sb.Length > 0 ? sb.ToString().TrimEnd() : null;
            }
            return null;
        }

        /// <summary>
        /// Load is measured against MassUtility.Capacity - the SAME number the Gear tab shows the
        /// player as "Mass carried: X / Y".
        ///
        /// It used to read StatDefOf.CarryingCapacity, which is a different stat: roughly 72 kg for
        /// an adult human against the 35 kg the Gear tab displays. Two consequences, both bad. The
        /// player had no way to read the ratio the mod was actually using, so they could not tune a
        /// loadout against it. And because the curve only reaches its full penalty at 100% load, a
        /// pawn would have needed 72 kg of gear to feel the designed -35% - which never happens, so
        /// the module lived permanently in the shallow end of its own curve.
        ///
        /// The two changes landed together and pull in opposite directions, so the net effect on a
        /// typical combat pawn is smaller than either one alone: switching the denominator made the
        /// penalty harsher, and dropping apparel from the numerator made it much lighter. An LMG
        /// gunner went 6.8% -> 20% -> 3.8% across the two changes.
        /// </summary>
        public static float GetEncumbranceMultiplier(Pawn pawn)
        {
            float capacity = MassUtility.Capacity(pawn);
            if (capacity <= 0f) return 1.0f;

            return MultiplierForLoadRatio(CarriedMass(pawn) / capacity);
        }

        /// <summary>
        /// Weapons and inventory. Worn apparel is deliberately excluded.
        ///
        /// Vanilla already charges movement for armour - flak vest, pants and jacket are -0.12 c/s
        /// each, and the pawn's stat tooltip lists them by name. Counting their mass here as well
        /// billed the player twice for the same decision, and the second bill was the larger one:
        /// measured on a teenager in full flak, vanilla took 7.8% and this module took another 23%.
        ///
        /// Worse than the size was the shape. Armour mass dominates a combat pawn's load - 83% of
        /// it in that measurement - so every armoured colonist paid roughly the same toll no matter
        /// what they were holding. A flat tax nobody can avoid is not a decision, it is just a
        /// slower game.
        ///
        /// Counting only what a pawn CARRIES restores the choice: a sniper rifle costs nothing, an
        /// LMG a few percent, an autocannon or a stack of sidearms a great deal. Vanilla owns the
        /// cost of what you wear; this module owns the cost of what you haul.
        /// </summary>
        public static float CarriedMass(Pawn pawn)
        {
            float mass = MassUtility.InventoryMass(pawn);

            if (pawn.equipment != null)
            {
                foreach (ThingWithComps equipment in pawn.equipment.AllEquipmentListForReading)
                {
                    mass += equipment.GetStatValue(StatDefOf.Mass) * equipment.stackCount;
                }
            }

            return mass;
        }

        /// <summary>
        /// The encumbrance curve itself, separated from where the mass came from so the debug
        /// harness can ask "what would this weapon alone cost a standard pawn" without inventing a
        /// second copy of the formula.
        ///
        /// Up to the threshold there is no penalty; above it the penalty scales linearly to
        /// encumbranceMaxPenalty at full carrying capacity.
        /// </summary>
        public static float MultiplierForLoadRatio(float ratio)
        {
            float threshold = FireDisciplineMod.Settings?.encumbranceThreshold ?? 0.15f;
            if (ratio <= threshold) return 1.0f;

            float excess = (ratio - threshold) / (1f - threshold);
            float maxPenalty = FireDisciplineMod.Settings?.encumbranceMaxPenalty ?? 0.35f;

            return UnityEngine.Mathf.Clamp(1.0f - excess * maxPenalty, 0.65f, 1.0f);
        }
    }
}
