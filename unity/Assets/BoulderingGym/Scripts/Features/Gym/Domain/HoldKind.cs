using Project.Features.Climbing.Domain;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    public enum HoldKind
    {
        Jug,
        Edge,
        Crimp,
        Sloper,
        Pinch,
        Pocket,
        Foothold,

        /// <summary>Big rounded hold of the easy circuits, as large as a small volume.</summary>
        Macro,
    }

    /// <summary>Shape size (local x across, y up the wall, z out of the wall) and grip of each hold kind.</summary>
    public static class HoldKindInfo
    {
        public static Vector3 Size(HoldKind kind)
        {
            return kind switch
            {
                HoldKind.Jug => new Vector3(0.20f, 0.13f, 0.10f),
                HoldKind.Edge => new Vector3(0.24f, 0.06f, 0.06f),
                HoldKind.Crimp => new Vector3(0.15f, 0.045f, 0.035f),
                HoldKind.Sloper => new Vector3(0.26f, 0.22f, 0.08f),
                HoldKind.Pinch => new Vector3(0.07f, 0.18f, 0.09f),
                HoldKind.Pocket => new Vector3(0.13f, 0.12f, 0.07f),
                HoldKind.Macro => new Vector3(0.40f, 0.30f, 0.16f),
                _ => new Vector3(0.07f, 0.05f, 0.035f),
            };
        }

        public static GripType Grip(HoldKind kind)
        {
            return kind switch
            {
                HoldKind.Jug => GripType.Jug,
                HoldKind.Edge => GripType.Open,
                HoldKind.Crimp => GripType.Crimp,
                HoldKind.Sloper => GripType.Sloper,
                HoldKind.Pinch => GripType.Pinch,
                HoldKind.Pocket => GripType.Open,
                HoldKind.Macro => GripType.Open,
                _ => GripType.Crimp,
            };
        }

        public static bool AcceptsHands(HoldKind kind) => kind != HoldKind.Foothold;
    }
}
