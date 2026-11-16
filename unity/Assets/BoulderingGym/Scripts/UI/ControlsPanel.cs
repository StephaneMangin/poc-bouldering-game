using System;
using UnityEngine;

namespace Project.UI
{
    /// <summary>Key bindings and bouldering rules, shown from the title screen and the pause menu.</summary>
    public static class ControlsPanel
    {
        private static readonly (string action, Func<string> keys)[] Bindings =
        {
            ("Walk / run", () => $"{KeyLabels.Movement}  +  Shift"),
            ("Aim a hold", () => "Mouse"),
            ("Left hand  /  right hand", () => $"{KeyLabels.For(KeyCode.Q)}  /  {KeyLabels.For(KeyCode.E)}"),
            ("Left foot  /  right foot", () => $"{KeyLabels.For(KeyCode.A)}  /  {KeyLabels.For(KeyCode.D)}"),
            ("Move the suggested limb", () => "Left click"),
            ("Let a limb go (no hold aimed)", () => "Its limb key"),
            ("Drop down to the mats", () => "Space"),
            ("Chalk up (one hand free)", () => KeyLabels.For(KeyCode.C)),
            ("Back to the start", () => KeyLabels.For(KeyCode.R)),
            ("Problem list", () => "Tab"),
            ("Turn / zoom the camera", () => "Right drag  /  wheel"),
            ("Pause", () => "Esc"),
        };

        private const string Rules =
            "Start with both hands on the START holds, then leave the floor. Send the problem by matching both hands on the TOP hold. " +
            "Touching the mats ends the attempt.\n" +
            "Every move costs endurance, which refills on the mats. Grip fades while you hang: chalk up with a free hand. " +
            "Arms pump with the weight they carry (steep walls, no feet): shake one arm out while the other holds. An exhausted arm slips.";

        public static void Draw(Action close)
        {
            var width = UiTheme.Px(900f);
            var height = UiTheme.Px(760f);
            var card = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            UiTheme.DrawCard(card);
            var x = card.x + UiTheme.Px(40f);
            var y = card.y + UiTheme.Px(30f);
            GUI.Label(new Rect(x, y, card.width, UiTheme.Px(40f)), "Controls", UiTheme.Heading);
            y += UiTheme.Px(56f);
            foreach (var (action, keys) in Bindings)
            {
                GUI.Label(new Rect(x, y, UiTheme.Px(440f), UiTheme.Px(30f)), action, UiTheme.Body);
                GUI.Label(new Rect(x + UiTheme.Px(460f), y, UiTheme.Px(360f), UiTheme.Px(30f)), $"<b>{keys()}</b>", UiTheme.Body);
                y += UiTheme.Px(32f);
            }

            GUI.Label(new Rect(x, y + UiTheme.Px(12f), card.width - UiTheme.Px(80f), UiTheme.Px(150f)), Rules, UiTheme.Small);
            if (UiTheme.DrawButton(new Rect(card.xMax - UiTheme.Px(200f), card.yMax - UiTheme.Px(70f), UiTheme.Px(160f), UiTheme.Px(46f)), "Back"))
            {
                close();
            }
        }
    }
}
