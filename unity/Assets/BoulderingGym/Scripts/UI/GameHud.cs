using System;
using Project.Features.Climbing.Presentation;
using Project.Features.Session.Domain;
using Project.Features.Session.Presentation;
using UnityEngine;

namespace Project.UI
{
    /// <summary>What the HUD reads every frame.</summary>
    public sealed class HudSources
    {
        public ClimberController Climber { get; set; }

        public SessionController Session { get; set; }

        public AimFeedback Aim { get; set; }

        public Func<string> SectorName { get; set; }

        public Func<string, Color> HoldColor { get; set; }
    }

    /// <summary>
    /// In-game overlay: the problem being climbed with its rules state, the attempt timer, the four
    /// effort gauges, the aimed-hold hint next to the cursor, announcements and a reminder of the keys.
    /// </summary>
    public static class GameHud
    {
        private static readonly Color EnduranceColor = new(0.36f, 0.82f, 0.54f);
        private static readonly Color GripColor = new(0.42f, 0.70f, 0.98f);
        private static readonly Color ArmColor = new(0.98f, 0.62f, 0.25f);

        public static void Draw(HudSources sources)
        {
            DrawProblemCard(sources);
            DrawSector(sources.SectorName());
            DrawTimer(sources.Session.Session);
            DrawGauges(sources.Climber);
            DrawAnnouncements(sources);
            DrawCursorHint(sources.Aim.Hint);
            DrawKeyReminder();
        }

        private static void DrawProblemCard(HudSources sources)
        {
            var card = new Rect(UiTheme.Px(24f), UiTheme.Px(24f), UiTheme.Px(520f), UiTheme.Px(118f));
            UiTheme.DrawCard(card);
            var session = sources.Session.Session;
            var inner = new Rect(card.x + UiTheme.Px(20f), card.y + UiTheme.Px(12f), card.width - UiTheme.Px(40f), UiTheme.Px(34f));
            if (session.Problem == null)
            {
                GUI.Label(inner, "Free climbing", UiTheme.Heading);
                GUI.Label(new Rect(inner.x, inner.y + UiTheme.Px(44f), inner.width, UiTheme.Px(52f)), "Every hold is yours. Press Tab to pick a problem.", UiTheme.Small);
                return;
            }

            var problem = session.Problem;
            UiTheme.DrawRounded(new Rect(inner.x, inner.y + UiTheme.Px(4f), UiTheme.Px(26f), UiTheme.Px(26f)), sources.HoldColor(problem.Circuit.ColorId));
            GUI.Label(new Rect(inner.x + UiTheme.Px(38f), inner.y, inner.width, inner.height), $"{problem.Grade}  {problem.DisplayName}", UiTheme.Heading);
            var record = sources.Session.Records.Find(problem.Id);
            var best = record != null && record.IsTopped ? $"best {SessionController.FormatTime(record.bestSeconds)}" : "not topped yet";
            GUI.Label(new Rect(inner.x, inner.y + UiTheme.Px(40f), inner.width, UiTheme.Px(28f)), PhaseText(session), UiTheme.Body);
            GUI.Label(new Rect(inner.x, inner.y + UiTheme.Px(70f), inner.width, UiTheme.Px(24f)), $"attempt {session.Attempts}  ·  {best}  ·  {KeyLabels.For(KeyCode.R)}: back to the start", UiTheme.Small);
        }

        private static string PhaseText(ProblemSession session)
        {
            return session.Phase switch
            {
                AttemptPhase.Ready => "Both hands on the <b>START</b> holds, then leave the floor",
                AttemptPhase.Climbing => $"Climbing  ·  {session.Moves} moves  ·  match the <b>TOP</b> hold",
                AttemptPhase.Topped => "<color=#5BD08A><b>Topped!</b></color>  Drop down to try again",
                _ => "<color=#EB5F38>Attempt over</color>  Back on the mats",
            };
        }

        private static void DrawSector(string sectorName)
        {
            if (string.IsNullOrEmpty(sectorName))
            {
                return;
            }

            var rect = new Rect(Screen.width - UiTheme.Px(344f), UiTheme.Px(24f), UiTheme.Px(320f), UiTheme.Px(48f));
            UiTheme.DrawCard(rect);
            GUI.Label(rect, $"<color=#9AA4A8>Sector</color>  {sectorName}", MenuScreens.Centered(UiTheme.Body));
        }

        private static void DrawTimer(ProblemSession session)
        {
            if (session.Phase != AttemptPhase.Climbing && session.Phase != AttemptPhase.Topped)
            {
                return;
            }

            var rect = new Rect((Screen.width - UiTheme.Px(220f)) * 0.5f, UiTheme.Px(24f), UiTheme.Px(220f), UiTheme.Px(60f));
            UiTheme.DrawCard(rect);
            GUI.Label(rect, SessionController.FormatTime(session.AttemptSeconds), MenuScreens.Centered(UiTheme.Heading));
        }

        private static void DrawGauges(ClimberController climber)
        {
            var resources = climber.Resources;
            var width = UiTheme.Px(760f);
            var card = new Rect((Screen.width - width) * 0.5f, Screen.height - UiTheme.Px(104f), width, UiTheme.Px(80f));
            UiTheme.DrawCard(card);
            var gauge = (card.width - UiTheme.Px(100f)) / 4f;
            var x = card.x + UiTheme.Px(20f);
            var y = card.y + UiTheme.Px(12f);
            var chalk = resources.Grip.IsChalkReady ? "Grip" : $"Grip  (chalk {resources.Grip.ChalkCooldownRemaining:0}s)";
            UiTheme.Gauge(new Rect(x, y, gauge, UiTheme.Px(54f)), "Endurance", resources.Endurance.Ratio, EnduranceColor);
            UiTheme.Gauge(new Rect(x + (gauge + UiTheme.Px(20f)), y, gauge, UiTheme.Px(54f)), chalk, resources.Grip.Ratio, GripColor);
            UiTheme.Gauge(new Rect(x + (gauge + UiTheme.Px(20f)) * 2f, y, gauge, UiTheme.Px(54f)), "Left arm", 1f - resources.Fatigue.LeftRatio, ArmColor);
            UiTheme.Gauge(new Rect(x + (gauge + UiTheme.Px(20f)) * 3f, y, gauge, UiTheme.Px(54f)), "Right arm", 1f - resources.Fatigue.RightRatio, ArmColor);
        }

        private static void DrawAnnouncements(HudSources sources)
        {
            var banner = sources.Session.Banner;
            if (banner != null)
            {
                GUI.Label(new Rect(0f, Screen.height * 0.28f, Screen.width, UiTheme.Px(90f)), banner, UiTheme.Banner);
            }

            var message = sources.Climber.Message;
            if (message == null)
            {
                return;
            }

            var rect = new Rect((Screen.width - UiTheme.Px(620f)) * 0.5f, Screen.height - UiTheme.Px(170f), UiTheme.Px(620f), UiTheme.Px(48f));
            UiTheme.DrawCard(rect);
            GUI.Label(rect, message, MenuScreens.Centered(UiTheme.Body));
        }

        private static void DrawCursorHint(string hint)
        {
            if (hint == null)
            {
                return;
            }

            var mouse = Event.current.mousePosition;
            var rect = new Rect(mouse.x + UiTheme.Px(18f), mouse.y + UiTheme.Px(12f), UiTheme.Px(220f), UiTheme.Px(34f));
            UiTheme.DrawRounded(rect, UiTheme.Card);
            GUI.Label(rect, hint, MenuScreens.Centered(UiTheme.Small));
        }

        private static void DrawKeyReminder()
        {
            var keys = $"{KeyLabels.For(KeyCode.Q)} {KeyLabels.For(KeyCode.E)} hands  ·  {KeyLabels.For(KeyCode.A)} {KeyLabels.For(KeyCode.D)} feet  ·  click: auto  ·  Space: drop  ·  {KeyLabels.For(KeyCode.C)}: chalk  ·  Tab: problems  ·  Esc: menu";
            GUI.Label(new Rect(UiTheme.Px(24f), Screen.height - UiTheme.Px(34f), Screen.width, UiTheme.Px(26f)), keys, UiTheme.Small);
        }
    }
}
