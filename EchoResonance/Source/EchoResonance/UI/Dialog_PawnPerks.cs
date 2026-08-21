using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using EchoResonance.Perks;
using EchoResonance.Core;

namespace EchoResonance.UI
{
    public class Dialog_PawnPerks : Window
    {
        private Pawn pawn;
        private CompPawnPerks compPerks;
        private PerkBranch selectedBranch = PerkBranch.Flesh;
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(1000f, 700f);

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
            // Section 10.2: Layout Heights
            // Header (72px) -> Owned Strip (34px) -> Tab Bar (32px) -> Canvas (remaining) -> Footer (28px)

            // 1. Header (72px)
            Rect headerRect = new Rect(0, 0, inRect.width, 72f);
            DrawHeader(headerRect);

            // 2. Always-visible Owned Perks Strip (34px)
            Rect ownedStripRect = new Rect(0, 76f, inRect.width, 34f);
            DrawOwnedPerksStrip(ownedStripRect);

            // 3. Branch Tabs (32px)
            Rect tabRect = new Rect(0, 114f, inRect.width, 32f);
            DrawBranchTabs(tabRect);

            // 4. Footer (28px) at bottom
            Rect footerRect = new Rect(0, inRect.height - 58f, inRect.width, 28f);
            DrawFooter(footerRect);

            // 5. Main Canvas (Scrollable area)
            Rect canvasRect = new Rect(0, 150f, inRect.width, inRect.height - 212f);
            DrawSkillTreeCanvas(canvasRect);
        }

        private void DrawHeader(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.14f, 0.16f, 0.95f));
            Widgets.DrawBox(rect, 1);

            // Pawn Portrait / Icon
            Rect iconRect = new Rect(rect.x + 8f, rect.y + 8f, 56f, 56f);
            Widgets.ThingIcon(iconRect, pawn);

            // Pawn Info Text
            Text.Font = GameFont.Medium;
            float currentEcho = EchoWorldComponent.Instance?.StoredEcho ?? 0f;
            float multiplier = EchoWorldComponent.Instance?.CachedMultiplier ?? 1.0f;
            Widgets.Label(new Rect(rect.x + 72f, rect.y + 8f, 380f, 30f), $"{pawn.LabelCap}");

            Text.Font = GameFont.Small;
            int activeCount = compPerks.ActivePerks.Count;
            float currentExpMultiplier = Mathf.Pow(EchoResonanceMod.Settings?.costMultiplierExponent ?? EchoTuning.EscalatingExponent, Mathf.Max(0, activeCount - 1));
            Widgets.Label(new Rect(rect.x + 72f, rect.y + 38f, 420f, 24f), $"Owned Perks: {activeCount} · Next Multiplier: x{currentExpMultiplier:F2}");

            // Colony Echo Pool & Catalyst Item Count (Right-aligned)
            Rect poolRect = new Rect(rect.width - 340f, rect.y + 8f, 330f, 56f);
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = new Color(0.4f, 0.85f, 1.0f);
            Widgets.Label(new Rect(poolRect.x, poolRect.y, poolRect.width, 24f), $"Echo Pool: {currentEcho:F1} Echo");

            GUI.color = Color.yellow;
            int focusCount = GetResonanceFocusCount();
            Widgets.Label(new Rect(poolRect.x, poolRect.y + 26f, poolRect.width, 24f), $"Resonance Multiplier: x{multiplier:F1} | ◈ Focus Crystals: {focusCount}");

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private int GetResonanceFocusCount()
        {
            Map map = pawn?.MapHeld ?? Find.CurrentMap;
            if (map == null) return 0;

            var focusDef = DefDatabase<ThingDef>.GetNamedSilentFail("ER_ResonanceFocus");
            return focusDef != null ? map.resourceCounter.GetCount(focusDef) : 0;
        }

        private void DrawOwnedPerksStrip(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.09f, 0.11f, 0.13f, 0.9f));
            Widgets.DrawBox(rect, 1);

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 8f, 80f, 20f), "Owned Perks:");
            GUI.color = Color.white;

            float curX = rect.x + 90f;
            if (compPerks.ActivePerks.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(curX, rect.y + 8f, 200f, 20f), "(None)");
                GUI.color = Color.white;
            }
            else
            {
                foreach (var perk in compPerks.ActivePerks)
                {
                    Color branchColor = GetBranchColor(perk.branch);
                    GUI.color = branchColor;
                    string badge = $"{GetBranchIcon(perk.branch)} {perk.label}";
                    Vector2 size = Text.CalcSize(badge);
                    Rect badgeRect = new Rect(curX, rect.y + 5f, size.x + 12f, 22f);
                    Widgets.DrawBoxSolid(badgeRect, new Color(branchColor.r * 0.2f, branchColor.g * 0.2f, branchColor.b * 0.2f, 0.8f));
                    Widgets.DrawBox(badgeRect, 1);

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(badgeRect, badge);
                    Text.Anchor = TextAnchor.UpperLeft;

                    TooltipHandler.TipRegion(badgeRect, $"{perk.label} (Tier {perk.tier} - {perk.branch})\n{perk.description}");

                    curX += size.x + 18f;
                }
            }
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
            bool hasSameBranchPerk = compPerks.ActivePerks.Any(p => p.branch == branch);
            Color branchColor = GetBranchColor(branch);

            Color bgColor = isSelected ? new Color(branchColor.r * 0.4f, branchColor.g * 0.4f, branchColor.b * 0.4f, 0.9f) : new Color(0.14f, 0.16f, 0.19f, 0.7f);

            Widgets.DrawBoxSolid(rect, bgColor);
            Widgets.DrawBox(rect, isSelected ? 2 : 1);

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            GUI.color = isSelected ? Color.cyan : (hasSameBranchPerk ? Color.yellow : Color.white);

            string title = label + (hasSameBranchPerk ? " ✓ -25%" : "");
            if (Widgets.ButtonInvisible(rect))
            {
                selectedBranch = branch;
            }
            Widgets.Label(rect, title);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawSkillTreeCanvas(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.07f, 0.08f, 0.10f, 0.95f));
            Widgets.DrawBox(rect, 1);

            // Column Headers (Tier 1, Tier 2, Tier 3)
            float colWidth = (rect.width - 60f) / 3f;
            DrawColumnHeader(new Rect(rect.x + 20f, rect.y + 10f, colWidth, 24f), "TIER 1  ·  Base: 20 Echo", Color.white);
            DrawColumnHeader(new Rect(rect.x + 30f + colWidth, rect.y + 10f, colWidth, 24f), "TIER 2  ·  Base: 60 Echo + ◈ Focus", Color.cyan);
            DrawColumnHeader(new Rect(rect.x + 40f + colWidth * 2, rect.y + 10f, colWidth, 24f), "TIER 3  ·  Base: 150 Echo", Color.yellow);

            var allPerksInBranch = DefDatabase<PerkDef>.AllDefsListForReading
                .Where(p => p.branch == selectedBranch)
                .OrderBy(p => p.tier)
                .ToList();

            float cardWidth = colWidth;
            float cardHeight = 56f;
            float startY = rect.y + 44f;
            float spacingY = 72f;

            // Render Nodes by Tier
            var tier1Perks = allPerksInBranch.Where(p => p.tier == 1).ToList();
            var tier2Perks = allPerksInBranch.Where(p => p.tier == 2).ToList();
            var tier3Perks = allPerksInBranch.Where(p => p.tier == 3).ToList();

            // Cache node positions
            Dictionary<PerkDef, Rect> nodeRects = new Dictionary<PerkDef, Rect>();

            for (int i = 0; i < tier1Perks.Count; i++)
            {
                Rect nodeRect = new Rect(rect.x + 20f, startY + (i * spacingY), cardWidth, cardHeight);
                nodeRects[tier1Perks[i]] = nodeRect;
            }

            for (int i = 0; i < tier2Perks.Count; i++)
            {
                Rect nodeRect = new Rect(rect.x + 30f + colWidth, startY + (i * spacingY), cardWidth, cardHeight);
                nodeRects[tier2Perks[i]] = nodeRect;
            }

            for (int i = 0; i < tier3Perks.Count; i++)
            {
                Rect nodeRect = new Rect(rect.x + 40f + colWidth * 2, startY + (i * spacingY), cardWidth, cardHeight);
                nodeRects[tier3Perks[i]] = nodeRect;
            }

            // Draw Connecting Lines for replaces ⬆️ only (Section 10.5)
            foreach (var perk in allPerksInBranch)
            {
                if (nodeRects.TryGetValue(perk, out Rect childRect))
                {
                    var replacedList = perk.GetAllReplacedPerks();
                    foreach (var rep in replacedList)
                    {
                        if (nodeRects.TryGetValue(rep, out Rect parentRect))
                        {
                            Vector2 p1 = new Vector2(parentRect.xMax, parentRect.center.y);
                            Vector2 p2 = new Vector2(childRect.x, childRect.center.y);
                            bool isUnlocked = compPerks.HasPerk(perk);
                            Color lineCol = isUnlocked ? GetBranchColor(selectedBranch) : new Color(0.4f, 0.4f, 0.4f, 0.6f);

                            // Draw double line (offset by 2px)
                            Widgets.DrawLine(new Vector2(p1.x, p1.y - 2f), new Vector2(p2.x, p2.y - 2f), lineCol, 2f);
                            Widgets.DrawLine(new Vector2(p1.x, p1.y + 2f), new Vector2(p2.x, p2.y + 2f), lineCol, 2f);
                        }
                    }
                }
            }

            // Draw Node Cards (Section 10.4 - Six Node States)
            foreach (var perk in allPerksInBranch)
            {
                if (nodeRects.TryGetValue(perk, out Rect nodeRect))
                {
                    DrawSixStateNodeCard(nodeRect, perk);
                }
            }
        }

        private void DrawSixStateNodeCard(Rect rect, PerkDef perk)
        {
            bool hasPerk = compPerks.HasPerk(perk);
            bool hasConflict = compPerks.HasConflict(perk, out var conflictPerk);
            bool hasPrereqs = compPerks.HasPrerequisites(perk, out var missingReqs);
            bool isTechUnlocked = compPerks.IsTechUnlocked(perk, out var techReason);
            bool hasCatalyst = compPerks.HasCatalystItem(perk);
            float cost = compPerks.CalculatePerkCost(perk);
            float currentEcho = EchoWorldComponent.Instance?.StoredEcho ?? 0f;
            bool canAffordEcho = currentEcho >= cost;

            Color branchColor = GetBranchColor(perk.branch);

            // Determine State
            Color bgColor = new Color(0.12f, 0.14f, 0.16f, 0.9f);
            Color borderColor = Color.gray;

            if (hasPerk)
            {
                // State 1: Owned
                bgColor = new Color(0.12f, 0.28f, 0.32f, 0.95f);
                borderColor = Color.yellow;
            }
            else if (hasConflict)
            {
                // State 6: Conflict / Excluded
                bgColor = new Color(0.3f, 0.1f, 0.1f, 0.95f);
                borderColor = Color.red;
            }
            else if (!hasPrereqs || !isTechUnlocked)
            {
                // State 4 & 5: Locked by Prereq or Tech
                bgColor = new Color(0.1f, 0.1f, 0.12f, 0.6f);
                borderColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);
            }
            else if (canAffordEcho && hasCatalyst)
            {
                // State 2: Available
                bgColor = new Color(0.14f, 0.24f, 0.18f, 0.95f);
                borderColor = branchColor;
            }
            else
            {
                // State 3: Insufficient Echo / Catalyst
                bgColor = new Color(0.15f, 0.15f, 0.17f, 0.85f);
                borderColor = Color.gray;
            }

            Widgets.DrawBoxSolid(rect, bgColor);
            Widgets.DrawBox(rect, hasPerk ? 2 : 1);

            // Card Text & State Display
            Text.Font = GameFont.Small;
            GUI.color = hasPerk ? Color.yellow : (hasConflict ? Color.red : Color.white);
            
            string relationPrefix = (perk.replaces != null || !perk.replacesList.NullOrEmpty()) ? "⬆️ " : (!perk.requires.NullOrEmpty() ? "🔗 " : "");
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 4f, rect.width - 16f, 20f), $"{relationPrefix}{perk.label}");

            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;

            if (perk.isTradeOff)
            {
                GUI.color = Color.red;
                Widgets.Label(new Rect(rect.x + 8f, rect.y + 24f, 90f, 16f), "🟥 TRADE-OFF");
                GUI.color = Color.white;
            }

            // Action / Status Text
            Rect btnRect = new Rect(rect.x + rect.width - 95f, rect.y + rect.height - 24f, 90f, 20f);

            if (hasPerk)
            {
                GUI.color = Color.yellow;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(btnRect, "✓ ĐÃ CÓ");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else if (hasConflict)
            {
                GUI.color = Color.red;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(btnRect, "⛔ XUNG ĐỘT");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else if (!hasPrereqs)
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(btnRect, "🔒 KHÓA");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else if (!isTechUnlocked)
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(btnRect, "🔬 TECH");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else
            {
                bool canBuy = canAffordEcho && hasCatalyst;
                string btnLabel = canBuy ? $"{cost:F0} Echo" : $"{cost:F0} Echo";
                
                if (Widgets.ButtonText(btnRect, btnLabel, true, true, canBuy))
                {
                    // Confirmation dialog for replaces or trade-off/exclusion
                    var replacedList = perk.GetAllReplacedPerks();
                    if (replacedList.Count > 0 || !perk.exclusionTags.NullOrEmpty())
                    {
                        string confirmMsg = $"Unlock '{perk.label}'?";
                        if (replacedList.Count > 0)
                        {
                            string repNames = string.Join(", ", replacedList.Select(r => r.label));
                            confirmMsg += $"\nThis will ABSORB and REMOVE: {repNames}!";
                        }
                        if (perk.isTradeOff) confirmMsg += $"\nWarning: Has trade-off penalty!";

                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(confirmMsg, () =>
                        {
                            compPerks.TryUnlockPerk(perk);
                        }));
                    }
                    else
                    {
                        compPerks.TryUnlockPerk(perk);
                    }
                }
            }

            // Rich Tooltip with Formula Breakdown (Section 10.6)
            TooltipHandler.TipRegion(rect, GetPerkTooltipText(perk, cost, hasConflict, conflictPerk, hasPrereqs, missingReqs, isTechUnlocked, techReason));
        }

        private string GetPerkTooltipText(PerkDef perk, float finalCost, bool hasConflict, PerkDef conflictPerk, bool hasPrereqs, List<PerkDef> missingReqs, bool isTechUnlocked, string techReason)
        {
            string text = $"<b>{perk.label}</b> ({GetBranchIcon(perk.branch)} {perk.branch} · Tier {perk.tier})\n";
            text += "──────────────────────────────────────────────────\n";
            text += $"{perk.description}\n\n";

            var replacedList = perk.GetAllReplacedPerks();
            if (replacedList.Count > 0)
            {
                string repNames = string.Join(", ", replacedList.Select(r => r.label));
                text += $"<color=yellow>⬆️ Thay thế / Nuốt: {repNames} (sẽ bị gỡ bỏ, bậc N được tối ưu)</color>\n";
            }

            if (!perk.requires.NullOrEmpty())
            {
                string reqNames = string.Join(", ", perk.requires.Select(r => r.label));
                text += $"<color=cyan>🔗 Tiền đề: {reqNames} (perk cũ vẫn ở lại)</color>\n";
            }

            if (perk.tier == 2)
            {
                text += "◈ Tiêu thụ: 1 Tinh Thể Cộng Hưởng (Resonance Focus)\n";
            }

            if (perk.isTradeOff && !perk.tradeOffDescription.NullOrEmpty())
            {
                text += $"<color=red><b>Trade-off Penalty:</b> {perk.tradeOffDescription}</color>\n";
            }

            text += "──────────────────────────────────────────────────\n";

            if (compPerks.HasPerk(perk))
            {
                text += "<color=yellow><b>Trạng thái:</b> Đã sở hữu</color>";
            }
            else if (hasConflict)
            {
                text += $"<color=red><b>XUNG ĐỘT:</b> Không thể mua do đã có perk '{conflictPerk?.label}'</color>";
            }
            else if (!hasPrereqs)
            {
                string missing = string.Join(", ", missingReqs.Select(m => m.label));
                text += $"<color=orange><b>KHÓA TIỀN ĐỀ:</b> Cần mở khóa perk [{missing}] trước</color>";
            }
            else if (!isTechUnlocked)
            {
                text += $"<color=orange><b>KHÓA CÔNG NGHỆ:</b> {techReason}</color>";
            }
            else
            {
                int replacedCount = replacedList.Count(p => compPerks.HasPerk(p));
                int n = Mathf.Max(1, compPerks.ActivePerks.Count - replacedCount + 1);
                float exponent = EchoResonanceMod.Settings?.costMultiplierExponent ?? EchoTuning.EscalatingExponent;

                text += "<b>Giá:</b> ";
                text += $"{perk.baseCost} gốc  ×{Mathf.Pow(exponent, n - 1):F2} (perk #{n})  ";

                if (compPerks.ActivePerks.Any(p => p.branch == perk.branch))
                {
                    text += "×0.75 (cùng nhánh)  ";
                }
                if (perk.isTradeOff)
                {
                    text += "×0.60 (trade-off)  ";
                }
                text += $"= <b>{finalCost:F0} Echo</b>\n";

                if (replacedCount > 0)
                {
                    text += $"<color=cyan><i>*Nuốt {replacedCount} perk cũ → bậc lũy tiến N giảm còn #{n}</i></color>";
                }
            }

            return text;
        }

        private void DrawColumnHeader(Rect rect, string text, Color color)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.14f, 0.16f, 0.19f, 0.85f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = color;
            Widgets.Label(rect, text);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawFooter(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.09f, 0.10f, 0.12f, 0.9f));
            Widgets.DrawBox(rect, 1);

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 6f, rect.width - 20f, 20f), "◈ = Cần 1 Tinh Thể Cộng Hưởng  |  ⬆️ = Nâng cấp (perk cũ bị thay thế, N không tăng)  |  🔗 = Tiền đề (perk cũ ở lại)  |  Hover để xem công thức giá chi tiết");
            GUI.color = Color.white;
        }

        private Color GetBranchColor(PerkBranch branch)
        {
            switch (branch)
            {
                case PerkBranch.Flesh: return new Color(0.71f, 0.32f, 0.29f);     // #B4524A
                case PerkBranch.Mind: return new Color(0.48f, 0.37f, 0.66f);      // #7A5FA8
                case PerkBranch.Livelihood: return new Color(0.78f, 0.60f, 0.24f);// #C89A3C
                case PerkBranch.Combat: return new Color(0.35f, 0.50f, 0.63f);    // #5A7FA0
                default: return Color.white;
            }
        }

        private string GetBranchIcon(PerkBranch branch)
        {
            switch (branch)
            {
                case PerkBranch.Flesh: return "🫀";
                case PerkBranch.Mind: return "🧠";
                case PerkBranch.Livelihood: return "🔨";
                case PerkBranch.Combat: return "⚔️";
                default: return "✨";
            }
        }
    }
}
