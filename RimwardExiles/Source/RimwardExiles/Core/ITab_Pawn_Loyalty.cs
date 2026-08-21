using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimwardExiles.Core
{
    public class ITab_Pawn_Loyalty : ITab
    {
        private Vector2 scrollPosition = Vector2.zero;
        private static readonly Color BarFilledColor = new Color(0.2f, 0.75f, 0.35f);
        private static readonly Color BarDiscontentColor = new Color(0.9f, 0.6f, 0.1f);
        private static readonly Color BarCriticalColor = new Color(0.9f, 0.2f, 0.2f);
        private static readonly Color BarBgColor = new Color(0.15f, 0.15f, 0.15f);

        public ITab_Pawn_Loyalty()
        {
            size = new Vector2(480f, 560f);
            labelKey = "RWX_TabLoyalty";
        }

        public override bool IsVisible
        {
            get
            {
                Pawn pawn = SelPawnForGear;
                if (pawn == null) return false;
                var comp = GameComponent_Exiles.Instance;
                return comp != null && comp.IsHero(pawn);
            }
        }

        private Pawn SelPawnForGear => SelPawn;

        protected override void FillTab()
        {
            Pawn pawn = SelPawn;
            if (pawn == null) return;

            var hero = GameComponent_Exiles.Instance?.GetHeroState(pawn);
            if (hero == null) return;

            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(14f);
            GUI.BeginGroup(rect);

            float curY = 0f;

            // 1. Tiêu đề: Tên Hero & Trạng thái
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, curY, rect.width, 32f), $"{pawn.LabelCap}");
            curY += 34f;

            // 2. Thanh Loyalty Bar
            float loyalty = hero.loyalty;
            Color barColor = BarFilledColor;
            string statusLabel = "Tận tuỵ";

            if (hero.CurrentTier == LoyaltyTier.Devoted)
            {
                barColor = BarFilledColor;
                statusLabel = "Tận tuỵ (Rất gắn bó)";
            }
            else if (hero.CurrentTier == LoyaltyTier.Normal)
            {
                barColor = new Color(0.4f, 0.7f, 0.9f);
                statusLabel = "Bình thường";
            }
            else if (hero.CurrentTier == LoyaltyTier.Discontent)
            {
                barColor = BarDiscontentColor;
                statusLabel = "Bất mãn (Có dấu hiệu lung lay)";
            }
            else if (hero.CurrentTier == LoyaltyTier.Critical)
            {
                barColor = BarCriticalColor;
                statusLabel = "Nguy cấp (Chuẩn bị rời bỏ thuộc địa)";
            }
            else if (hero.CurrentTier == LoyaltyTier.Leaving)
            {
                barColor = Color.gray;
                statusLabel = "Đã rời đi";
            }

            string trend = "→";
            if (hero.lastDriftDelta > 0.05f) trend = "↑";
            else if (hero.lastDriftDelta < -0.05f) trend = "↓";

            Rect barRect = new Rect(0f, curY, rect.width - 90f, 24f);
            Widgets.DrawBoxSolid(barRect, BarBgColor);
            Rect fillRect = new Rect(0f, curY, barRect.width * (loyalty / 100f), 24f);
            Widgets.DrawBoxSolid(fillRect, barColor);
            Widgets.DrawHighlightIfMouseover(barRect);

            Rect valueRect = new Rect(rect.width - 85f, curY, 85f, 24f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(valueRect, $"{loyalty:F0} / 100 {trend}");
            Text.Anchor = TextAnchor.UpperLeft;

            curY += 28f;

            // Dòng trạng thái
            GUI.color = barColor;
            Widgets.Label(new Rect(0f, curY, rect.width, 22f), statusLabel);
            GUI.color = Color.white;
            curY += 24f;

            Widgets.DrawLineHorizontal(0f, curY, rect.width);
            curY += 8f;

            // Khung cuộn nội dung chi tiết
            Rect outRect = new Rect(0f, curY, rect.width, rect.height - curY);
            float viewHeight = CalculateContentHeight(hero);
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float subY = 0f;

            // === Phần 1: Vì sao (Factor List) ===
            DrawSectionHeader("── Vì sao (Tác động hiện tại) ──", ref subY, viewRect.width);

            var factors = LoyaltyCalculator.GatherFactors(hero);
            if (factors.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(8f, subY, viewRect.width - 16f, 22f), "Không có biến động đáng kể (Điểm neo: 50).");
                GUI.color = Color.white;
                subY += 22f;
            }
            else
            {
                for (int i = 0; i < factors.Count; i++)
                {
                    var f = factors[i];
                    Rect rowRect = new Rect(8f, subY, viewRect.width - 16f, 22f);
                    if (i % 2 == 1) Widgets.DrawAltRect(rowRect);

                    Widgets.Label(new Rect(8f, subY, rowRect.width - 70f, 22f), f.label);

                    Color fColor = f.delta >= 0 ? Color.green : new Color(1f, 0.4f, 0.4f);
                    GUI.color = fColor;
                    Text.Anchor = TextAnchor.MiddleRight;
                    string sign = f.delta >= 0 ? "+" : "";
                    Widgets.Label(new Rect(rowRect.width - 65f, subY, 65f, 22f), $"{sign}{f.delta:F0}");
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;

                    subY += 22f;
                }
            }

            subY += 8f;

            // === Phần 2: Ấn tượng (Disposition) ===
            if (hero.disposition != null)
            {
                DrawSectionHeader("── Ấn tượng ──", ref subY, viewRect.width);
                GUI.color = new Color(1f, 0.85f, 0.5f);
                Widgets.Label(new Rect(8f, subY, viewRect.width - 16f, 22f), $"\"{hero.disposition.reason}\"");
                subY += 22f;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(8f, subY, viewRect.width - 16f, 20f),
                    $"Tác động: Thăng tiến ×{hero.disposition.gainMultiplier:F2} | Thất thoát ×{hero.disposition.lossMultiplier:F2}");
                GUI.color = Color.white;
                subY += 26f;
            }

            // === Phần 3: Ký ức (HeroMemories) ===
            if (hero.memories != null && hero.memories.Count > 0)
            {
                DrawSectionHeader("── Ký ức ──", ref subY, viewRect.width);
                int now = Find.TickManager.TicksGame;

                for (int i = 0; i < hero.memories.Count; i++)
                {
                    var mem = hero.memories[i];
                    float w = mem.GetCurrentWeight(now);
                    if (Mathf.Abs(w) < 0.2f) continue;

                    Rect rowRect = new Rect(8f, subY, viewRect.width - 16f, 22f);
                    if (i % 2 == 1) Widgets.DrawAltRect(rowRect);

                    Widgets.Label(new Rect(8f, subY, rowRect.width - 90f, 22f), mem.label);

                    Color mColor = w >= 0 ? Color.green : new Color(1f, 0.4f, 0.4f);
                    GUI.color = mColor;
                    Text.Anchor = TextAnchor.MiddleRight;
                    string sign = w >= 0 ? "+" : "";
                    string decayStr = mem.decayable ? "" : "*";
                    Widgets.Label(new Rect(rowRect.width - 85f, subY, 85f, 22f), $"{sign}{w:F1}{decayStr}");
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;

                    subY += 22f;
                }
                subY += 8f;
            }

            // === Phần 4: Niềm tin (HeroCreed) ===
            if (hero.creed != null && hero.creed.values != null)
            {
                DrawSectionHeader($"── Niềm tin ({hero.creed.LabelCap}) ──", ref subY, viewRect.width);
                for (int i = 0; i < hero.creed.values.Count; i++)
                {
                    var valEntry = hero.creed.values[i];
                    if (valEntry.value == null) continue;

                    Rect rowRect = new Rect(8f, subY, viewRect.width - 16f, 22f);
                    if (i % 2 == 1) Widgets.DrawAltRect(rowRect);

                    Widgets.Label(new Rect(8f, subY, 140f, 22f), valEntry.value.LabelCap);

                    // Mini bar biểu thị trọng số weight (-1.0 .. +1.0)
                    Rect miniBarRect = new Rect(155f, subY + 5f, 150f, 12f);
                    Widgets.DrawBoxSolid(miniBarRect, BarBgColor);

                    float midX = miniBarRect.x + (miniBarRect.width / 2f);
                    float fillWidth = (valEntry.weight / 2f) * miniBarRect.width;

                    if (valEntry.weight >= 0)
                    {
                        Rect posFill = new Rect(midX, miniBarRect.y, fillWidth, miniBarRect.height);
                        Widgets.DrawBoxSolid(posFill, new Color(0.3f, 0.7f, 1f));
                    }
                    else
                    {
                        Rect negFill = new Rect(midX + fillWidth, miniBarRect.y, -fillWidth, miniBarRect.height);
                        Widgets.DrawBoxSolid(negFill, new Color(1f, 0.4f, 0.4f));
                    }

                    Text.Anchor = TextAnchor.MiddleRight;
                    string wSign = valEntry.weight >= 0 ? "+" : "";
                    Widgets.Label(new Rect(rowRect.width - 65f, subY, 65f, 22f), $"{wSign}{valEntry.weight:F2}");
                    Text.Anchor = TextAnchor.UpperLeft;

                    subY += 22f;
                }
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawSectionHeader(string title, ref float curY, float width)
        {
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(4f, curY, width, 18f), title);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            curY += 20f;
        }

        private float CalculateContentHeight(HeroState hero)
        {
            float h = 60f;
            var factors = LoyaltyCalculator.GatherFactors(hero);
            h += factors.Count * 24f;

            if (hero.disposition != null) h += 55f;
            if (hero.memories != null) h += hero.memories.Count * 24f + 30f;
            if (hero.creed?.values != null) h += hero.creed.values.Count * 24f + 30f;

            return Mathf.Max(h, 450f);
        }
    }
}
