using System;
using Project.Features.Gym.Domain;
using Project.Features.Session.Domain;
using Project.Features.Session.Presentation;
using UnityEngine;

namespace Project.UI
{
    public sealed class BrowserActions
    {
        public Action<BoulderProblem> Select { get; set; }

        public Action FreeClimb { get; set; }

        public Action Close { get; set; }
    }

    /// <summary>Problem list: sectors grouped by wall on the left, the selected sector's problems on the right.</summary>
    public sealed class ProblemBrowser
    {
        private const float RowHeight = 58f;
        private Vector2 _sectorScroll;
        private Vector2 _problemScroll;
        private GymSector _selected;

        public Func<string, Color> HoldColor { get; set; }

        public void Open(GymSector sector) => _selected = sector;

        public void Draw(GymContent content, ClimbRecords records, BrowserActions actions)
        {
            var card = new Rect((Screen.width - UiTheme.Px(1240f)) * 0.5f, (Screen.height - UiTheme.Px(780f)) * 0.5f, UiTheme.Px(1240f), UiTheme.Px(780f));
            UiTheme.DrawCard(card);
            GUI.Label(new Rect(card.x + UiTheme.Px(30f), card.y + UiTheme.Px(18f), UiTheme.Px(600f), UiTheme.Px(40f)), "Problems", UiTheme.Heading);
            if (UiTheme.DrawButton(new Rect(card.xMax - UiTheme.Px(420f), card.y + UiTheme.Px(16f), UiTheme.Px(200f), UiTheme.Px(44f)), "Free climbing"))
            {
                actions.FreeClimb();
            }

            if (UiTheme.DrawButton(new Rect(card.xMax - UiTheme.Px(200f), card.y + UiTheme.Px(16f), UiTheme.Px(170f), UiTheme.Px(44f)), "Close"))
            {
                actions.Close();
            }

            var body = new Rect(card.x + UiTheme.Px(24f), card.y + UiTheme.Px(80f), card.width - UiTheme.Px(48f), card.height - UiTheme.Px(104f));
            DrawSectors(content, new Rect(body.x, body.y, UiTheme.Px(330f), body.height));
            DrawProblems(content, records, actions.Select, new Rect(body.x + UiTheme.Px(350f), body.y, body.width - UiTheme.Px(350f), body.height));
        }

        private void DrawSectors(GymContent content, Rect area)
        {
            var rowHeight = UiTheme.Px(34f);
            var view = new Rect(0f, 0f, area.width - UiTheme.Px(20f), (content.Sectors.Count + 4) * rowHeight * 1.2f);
            _sectorScroll = GUI.BeginScrollView(area, _sectorScroll, view);
            var y = 0f;
            string structure = null;
            foreach (var sector in content.Sectors)
            {
                if (sector.Sector.Structure.Name != structure)
                {
                    structure = sector.Sector.Structure.Name;
                    GUI.Label(new Rect(0f, y, view.width, rowHeight), $"<color=#EB5F38>{structure.ToUpperInvariant()}</color>", UiTheme.Small);
                    y += rowHeight;
                }

                var rect = new Rect(0f, y, view.width, rowHeight - UiTheme.Px(4f));
                if (sector.Sector == _selected)
                {
                    UiTheme.DrawRounded(rect, UiTheme.Teal);
                }

                if (GUI.Button(rect, $"  {sector.Sector.Name}   <color=#9AA4A8>{sector.Problems.Count}</color>", UiTheme.Body))
                {
                    _selected = sector.Sector;
                    _problemScroll = Vector2.zero;
                }

                y += rowHeight;
            }

            GUI.EndScrollView();
        }

        private void DrawProblems(GymContent content, ClimbRecords records, Action<BoulderProblem> select, Rect area)
        {
            var sector = Find(content, _selected) ?? content.Sectors[0];
            var rowHeight = UiTheme.Px(RowHeight);
            var view = new Rect(0f, 0f, area.width - UiTheme.Px(20f), sector.Problems.Count * rowHeight + rowHeight);
            _problemScroll = GUI.BeginScrollView(area, _problemScroll, view);
            GUI.Label(new Rect(0f, 0f, view.width, rowHeight * 0.8f), $"{sector.Sector.Name}  ·  {sector.Sector.Structure.Name}", UiTheme.Body);
            var y = rowHeight;
            foreach (var problem in sector.Problems)
            {
                DrawProblemRow(problem, records.Find(problem.Id), select, new Rect(0f, y, view.width, rowHeight - UiTheme.Px(8f)));
                y += rowHeight;
            }

            GUI.EndScrollView();
        }

        private void DrawProblemRow(BoulderProblem problem, ProblemRecord record, Action<BoulderProblem> select, Rect row)
        {
            UiTheme.DrawRounded(row, new Color(1f, 1f, 1f, 0.06f));
            var chip = new Rect(row.x + UiTheme.Px(12f), row.y + row.height * 0.2f, row.height * 0.6f, row.height * 0.6f);
            UiTheme.DrawRounded(chip, HoldColor(problem.Circuit.ColorId));
            var text = $"<b>#{problem.Number}  {problem.Grade}</b>   {problem.Circuit.DisplayName} · {problem.Circuit.Label} · {problem.Holds.Count} holds";
            GUI.Label(new Rect(chip.xMax + UiTheme.Px(16f), row.y, row.width * 0.55f, row.height), text, UiTheme.Body);
            GUI.Label(new Rect(row.x + row.width * 0.62f, row.y, row.width * 0.2f, row.height), Status(record), UiTheme.Small);
            if (UiTheme.DrawButton(new Rect(row.xMax - UiTheme.Px(130f), row.y + UiTheme.Px(7f), UiTheme.Px(118f), row.height - UiTheme.Px(14f)), "Climb"))
            {
                select(problem);
            }
        }

        private static string Status(ProblemRecord record)
        {
            if (record == null || record.attempts == 0)
            {
                return "not tried";
            }

            return record.IsTopped
                ? $"<color=#5BD08A>topped</color> · best {SessionController.FormatTime(record.bestSeconds)}"
                : $"{record.attempts} attempt{(record.attempts > 1 ? "s" : "")}";
        }

        private static SectorContent Find(GymContent content, GymSector sector)
        {
            foreach (var candidate in content.Sectors)
            {
                if (candidate.Sector == sector)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
