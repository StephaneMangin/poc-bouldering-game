using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>Raised deck of the hall: its area seen from above (x, z) and the height of its floor.</summary>
    public sealed class MezzanineArea
    {
        public MezzanineArea(Vector2 min, Vector2 max, float height)
        {
            Min = min;
            Max = max;
            Height = height;
        }

        public Vector2 Min { get; }

        public Vector2 Max { get; }

        public float Height { get; }
    }
}
