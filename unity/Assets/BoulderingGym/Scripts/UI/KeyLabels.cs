using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.UI
{
    /// <summary>
    /// Label printed on the key at a physical position for the player's keyboard layout
    /// (the game reads physical keys: the left hand key shows "A" on AZERTY and "Q" on QWERTY).
    /// </summary>
    public static class KeyLabels
    {
        public static string For(KeyCode code)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && System.Enum.TryParse<Key>(code.ToString(), out var key))
            {
                var label = keyboard[key].displayName;
                if (!string.IsNullOrEmpty(label))
                {
                    return label.ToUpperInvariant();
                }
            }

            return code.ToString().ToUpperInvariant();
        }

        public static string Movement => $"{For(KeyCode.W)} {For(KeyCode.A)} {For(KeyCode.S)} {For(KeyCode.D)}";
    }
}
