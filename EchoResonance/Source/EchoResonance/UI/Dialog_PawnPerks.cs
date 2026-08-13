using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using EchoResonance.Perks;
using EchoResonance.Core;
using EchoResonance.Buildings;

namespace EchoResonance.UI
{
    public class Dialog_PawnPerks : Window
    {
        private Pawn pawn;
        private CompPawnPerks compPerks;
        private PerkBranch selectedBranch = PerkBranch.Flesh;
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(920f, 680f);

        public Dialog_PawnPerks(Pawn pawn, CompPawnPerks compPerks)
        {
            this.pawn = pawn;
            this.compPerks = compPerks;
            this.doCloseButton = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 1. Draw Header
            Rect headerRect = new Rect(0, 0, inRect.width, 60f);
            DrawHeader(headerRect);

            // 2. Draw Branch Tabs
            Rect tabRect = new Rect(0, 65f, inRect.width, 35f);
            DrawBranchTabs(tabRect);

            // 3. Draw Skill Tree Canvas
            Rect canvasRect = new Rect(0, 105f, inRect.width, inRect.height - 155f);
            DrawSkillTreeCanvas(canvasRect);
        }

        private void DrawHeader(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.14f, 0.16f, 0.9f));
            Widgets.DrawBox(rect, 1);

            // Pawn Portrait / Icon
            Rect iconRect = new Rect(rect.x + 8f, rect.y + 6f, 48f, 48f);
            Widgets.ThingIcon(iconRect, pawn);

            // Pawn Info Text
            Text.Font = GameFont.Medium;
            float currentEcho = EchoWorldComponent.Instance?.StoredEcho ?? 0f;
            float multiplier = EchoWorldComponent.Instance?.CachedMultiplier ?? 1.0f;
            Widgets.Label(new Rect(rect.x + 65f, rect.y + 6f, 350f, 30f), $"{pawn.LabelCap}");

            Text.Font = GameFont.Small;
            int activeCount = compPerks.ActivePerks.Count;
            float currentExpMultiplier = Mathf.Pow(EchoResonanceMod.Settings?.costMultiplierExponent ?? EchoTuning.EscalatingExponent, activeCount);
            Widgets.Label(new Rect(rect.x + 65f, rect.y + 32f, 400f, 24f), $"Active Perks: {activeCount} | Cost Escalation: x{currentExpMultiplier:F2}");

            // Colony Echo Pool & Pylon Status (Right-aligned)
            Rect poolRect = new Rect(rect.width - 320f, rect.y + 8f, 310f, 44f);
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = new Color(0.4f, 0.85f, 1.0f);
            Widgets.Label(new Rect(poolRect.x, poolRect.y, poolRect.width, 22f), $"Echo Pool: {currentEcho:F1} Echo");
            GUI.color = Color.yellow;
            Widgets.Label(new Rect(poolRect.x, poolRect.y + 22f, poolRect.width, 22f), $"Resonance Multiplier: x{multiplier:F1}");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawBranchTabs(Rect rect)
        {
            float tabWidth = rect.width / 4f;

            DrawSingleTab(new Rect(rect.x, rect.y, tabWidth, rect.height), "🫀 Flesh (Nhục Thân)", PerkBranch.Flesh);
            DrawSingleTab(new Rect(rect.x + tabWidth, rect.y, tabWidth, rect.height), "🧠 Mind (Tâm Trí)", PerkBranch.Mind);
            DrawSingleTab(new Rect(rect.x + tabWidth * 2, rect.y, tabWidth, rect.height), "🔨 Livelihood (Sinh Kế)", PerkBranch.Livelihood);
            DrawSingleTab(new Rect(rect.x + tabWidth * 3, rect.y, tabWidth, rect.height), "⚔️ Combat (Chiến Trận)", PerkBranch.Combat);
        }

        private void DrawSingleTab(Rect rect, string label, PerkBranch branch)
        {
            bool isSelected = (selectedBranch == branch);
            Color bgColor = isSelected ? new Color(0.2f, 0.35f, 0.5f, 0.9f) : new Color(0.15f, 0.18f, 0.22f, 0.7f);

            Widgets.DrawBoxSolid(rect, bgColor);
            Widgets.DrawBox(rect, isSelected ? 2 : 1);

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            GUI.color = isSelected ? Color.cyan : Color.white;
            if (Widgets.ButtonInvisible(rect))
            {
                selectedBranch = branch;
            }
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawSkillTreeCanvas(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.09f, 0.11f, 0.95f));
            Widgets.DrawBox(rect, 1);

            // Tier Column Headers
            float colWidth = (rect.width - 40f) / 3f;
            DrawColumnHeader(new Rect(rect.x + 15f, rect.y + 10f, colWidth, 25f), "TIER 1 (Base: 20 Echo)", Color.white);
            DrawColumnHeader(new Rect(rect.x + 20f + colWidth, rect.y + 10f, colWidth, 25f), "TIER 2 (Base: 60 Echo + 1 Focus)", Color.cyan);
            DrawColumnHeader(new Rect(rect.x + 25f + colWidth * 2, rect.y + 10f, colWidth, 25f), "TIER 3 (Base: 150 Echo)", Color.yellow);

            // Get perks for selected branch
            var allPerksInBranch = DefDatabase<PerkDef>.AllDefsListForReading
                .Where(p => p.branch == selectedBranch)
                .OrderBy(p => p.tier)
                .ToList();

            var tier1Perks = allPerksInBranch.Where(p => p.tier == 1).ToList();
            var tier2Perks = allPerksInBranch.Where(p => p.tier == 2).ToList();
            var tier3Perks = allPerksInBranch.Where(p => p.tier == 3).ToList();

            // Draw Cards for each Tier
            float cardHeight = 85f;
            float startY = rect.y + 45f;
            float spacingY = 95f;

            // Tier 1 Cards
            for (int i = 0; i < tier1Perks.Count; i++)
            {
                Rect cardRect = new Rect(rect.x + 15f, startY + (i * spacingY), colWidth, cardHeight);
                DrawPerkNodeCard(cardRect, tier1Perks[i]);
            }

            // Tier 2 Cards
            for (int i = 0; i < tier2Perks.Count; i++)
            {
                Rect cardRect = new Rect(rect.x + 20f + colWidth, startY + (i * spacingY), colWidth, cardHeight);
                DrawPerkNodeCard(cardRect, tier2Perks[i]);

                // Draw Connecting Line from Tier 1
                if (tier1Perks.Count > 0)
                {
                    Vector2 lineStart = new Vector2(rect.x + 15f + colWidth, startY + 42f);
                    Vector2 lineEnd = new Vector2(rect.x + 20f + colWidth, startY + (i * spacingY) + 42f);
                    bool lineActive = compPerks.HasPerk(tier2Perks[i]);
                    Widgets.DrawLine(lineStart, lineEnd, lineActive ? Color.cyan : new Color(0.4f, 0.4f, 0.4f, 0.5f), 2f);
                }
            }

            // Tier 3 Cards
            for (int i = 0; i < tier3Perks.Count; i++)
            {
                Rect cardRect = new Rect(rect.x + 25f + colWidth * 2, startY + (i * spacingY), colWidth, cardHeight);
                DrawPerkNodeCard(cardRect, tier3Perks[i]);

                // Draw Connecting Line from Tier 2
                if (tier2Perks.Count > 0)
                {
                    Vector2 lineStart = new Vector2(rect.x + 20f + colWidth * 2, startY + 42f);
                    Vector2 lineEnd = new Vector2(rect.x + 25f + colWidth * 2, startY + (i * spacingY) + 42f);
                    bool lineActive = compPerks.HasPerk(tier3Perks[i]);
                    Widgets.DrawLine(lineStart, lineEnd, lineActive ? Color.yellow : new Color(0.4f, 0.4f, 0.4f, 0.5f), 2f);
                }
            }
        }

        private void DrawColumnHeader(Rect rect, string text, Color color)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.18f, 0.22f, 0.8f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = color;
            Widgets.Label(rect, text);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawPerkNodeCard(Rect rect, PerkDef perk)
        {
            bool hasPerk = compPerks.HasPerk(perk);
            float cost = compPerks.CalculatePerkCost(perk);
            float currentEcho = EchoWorldComponent.Instance?.StoredEcho ?? 0f;
            bool canAfford = currentEcho >= cost;

            // Background & Border colors based on state
            Color bgColor = hasPerk ? new Color(0.1f, 0.3f, 0.35f, 0.85f) : (canAfford ? new Color(0.15f, 0.22f, 0.18f, 0.85f) : new Color(0.18f, 0.18f, 0.18f, 0.85f));
            Color borderColor = hasPerk ? Color.cyan : (perk.isTradeOff ? Color.red : (canAfford ? Color.green : Color.gray));

            Widgets.DrawBoxSolid(rect, bgColor);
            Widgets.DrawBox(rect, hasPerk ? 2 : 1);

            // Icon Placeholder
            Rect iconRect = new Rect(rect.x + 6f, rect.y + 10f, 32f, 32f);
            Widgets.ThingIcon(iconRect, ThingDefOf.Apparel_ShieldBelt ?? ThingDefOf.PsychicAmplifier);

            // Label & Trade-Off Badge
            Text.Font = GameFont.Small;
            GUI.color = hasPerk ? Color.cyan : Color.white;
            Widgets.Label(new Rect(rect.x + 44f, rect.y + 6f, rect.width - 50f, 22f), perk.label);
            GUI.color = Color.white;

            if (perk.isTradeOff)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = Color.red;
                Widgets.Label(new Rect(rect.x + 44f, rect.y + 26f, 100f, 18f), "🟥 [TRADE-OFF]");
                GUI.color = Color.white;
            }

            // Action / Status Button
            Rect btnRect = new Rect(rect.x + 8f, rect.y + rect.height - 30f, rect.width - 16f, 24f);
            if (hasPerk)
            {
                GUI.color = Color.cyan;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                Widgets.Label(btnRect, "✓ UNLOCKED");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else
            {
                string btnText = canAfford ? $"BUY ({cost:F0} Echo)" : $"NEED {cost:F0} Echo";
                if (Widgets.ButtonText(btnRect, btnText, true, true, canAfford))
                {
                    compPerks.TryUnlockPerk(perk);
                }
            }

            // Tooltip on Hover
            TooltipHandler.TipRegion(rect, GetPerkTooltipText(perk, cost));
        }

        private string GetPerkTooltipText(PerkDef perk, float finalCost)
        {
            string text = $"<b>{perk.label}</b> (Tier {perk.tier} - {perk.branch})\n";
            text += $"{perk.description}\n\n";

            if (perk.isTradeOff && !perk.tradeOffDescription.NullOrEmpty())
            {
                text += $"<color=red><b>Trade-off Penalty:</b> {perk.tradeOffDescription}</color>\n\n";
            }

            if (compPerks.HasPerk(perk))
            {
                text += "<color=cyan><b>Status:</b> Already Unlocked</color>";
            }
            else
            {
                text += "<b>Price Breakdown:</b>\n";
                text += $"• Base Price: {perk.baseCost} Echo\n";
                int n = compPerks.ActivePerks.Count + 1;
                float exponent = EchoResonanceMod.Settings?.costMultiplierExponent ?? EchoTuning.EscalatingExponent;
                text += $"• Pawn Multiplier (Perk #{n}): x{Mathf.Pow(exponent, n - 1):F2}\n";

                if (compPerks.ActivePerks.Any(p => p.branch == perk.branch))
                {
                    text += "• Specialization Discount: -25% (Same Branch)\n";
                }
                if (perk.isTradeOff)
                {
                    text += "• Trade-off Discount: -40%\n";
                }
                text += $"<b>Final Price: {finalCost:F0} Echo</b>";
            }

            return text;
        }
    }
}
