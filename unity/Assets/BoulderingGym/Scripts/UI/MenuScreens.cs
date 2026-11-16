using System;
using UnityEngine;

namespace Project.UI
{
    /// <summary>Title screen actions; the game flow fills them.</summary>
    public sealed class TitleActions
    {
        public Action Climb { get; set; }

        public Action SwitchClimber { get; set; }

        public Action ShowControls { get; set; }

        public Action Quit { get; set; }

        public Func<bool> IsReady { get; set; }

        public Func<string> ClimberName { get; set; }

        public Func<string> Subtitle { get; set; }
    }

    /// <summary>Pause menu actions; problem actions are hidden when no problem is selected.</summary>
    public sealed class PauseActions
    {
        public Action Resume { get; set; }

        public Action Restart { get; set; }

        public Action LeaveProblem { get; set; }

        public Action OpenProblems { get; set; }

        public Action ShowControls { get; set; }

        public Action CyclePass { get; set; }

        public Action Quit { get; set; }

        public Func<bool> HasProblem { get; set; }

        public Func<string> PassName { get; set; }
    }

    public static class MenuScreens
    {
        private const float ButtonWidth = 360f;
        private const float ButtonHeight = 52f;
        private const float ButtonGap = 14f;

        public static void DrawTitle(TitleActions actions)
        {
            UiTheme.DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.35f));
            var top = Screen.height * 0.18f;
            GUI.Label(new Rect(0f, top, Screen.width, UiTheme.Px(80f)), "BLOCK'OUT RENNES", UiTheme.Title);
            GUI.Label(new Rect(0f, top + UiTheme.Px(84f), Screen.width, UiTheme.Px(36f)), actions.Subtitle(), Centered(UiTheme.Body));
            var y = top + UiTheme.Px(190f);
            var ready = actions.IsReady();
            if (Button(ref y, ready ? "Climb" : "Setting the gym...", ready))
            {
                actions.Climb();
            }

            if (Button(ref y, $"Climber: {actions.ClimberName()}", ready))
            {
                actions.SwitchClimber();
            }

            if (Button(ref y, "Controls"))
            {
                actions.ShowControls();
            }

            if (Button(ref y, "Quit"))
            {
                actions.Quit();
            }
        }

        public static void DrawPause(PauseActions actions)
        {
            var hasProblem = actions.HasProblem();
            var buttons = hasProblem ? 7 : 5;
            var height = UiTheme.Px(110f + buttons * (ButtonHeight + ButtonGap));
            var card = new Rect((Screen.width - UiTheme.Px(460f)) * 0.5f, (Screen.height - height) * 0.5f, UiTheme.Px(460f), height);
            UiTheme.DrawCard(card);
            GUI.Label(new Rect(card.x, card.y + UiTheme.Px(20f), card.width, UiTheme.Px(40f)), "Paused", Centered(UiTheme.Heading));
            var y = card.y + UiTheme.Px(80f);
            if (Button(ref y, "Resume"))
            {
                actions.Resume();
            }

            if (hasProblem && Button(ref y, "Back to the start"))
            {
                actions.Restart();
            }

            if (hasProblem && Button(ref y, "Leave the problem"))
            {
                actions.LeaveProblem();
            }

            if (Button(ref y, "Problem list"))
            {
                actions.OpenProblems();
            }

            if (Button(ref y, "Controls"))
            {
                actions.ShowControls();
            }

            if (Button(ref y, $"View: {actions.PassName()}"))
            {
                actions.CyclePass();
            }

            if (Button(ref y, "Quit"))
            {
                actions.Quit();
            }
        }

        public static GUIStyle Centered(GUIStyle style) => new(style) { alignment = TextAnchor.MiddleCenter };

        private static bool Button(ref float y, string label, bool enabled = true)
        {
            var rect = new Rect((Screen.width - UiTheme.Px(ButtonWidth)) * 0.5f, y, UiTheme.Px(ButtonWidth), UiTheme.Px(ButtonHeight));
            y += UiTheme.Px(ButtonHeight + ButtonGap);
            return UiTheme.DrawButton(rect, label, enabled);
        }
    }
}
