using System;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class HoldLaneLayoutController : MonoBehaviour
    {
        [SerializeField] private Transform holdsRoot;
        [SerializeField] private bool applyOnAwake = true;
        [SerializeField] private int holdsPerLane = 4;

        [Header("Lane X Positions")]
        [SerializeField] private float laneLeftX = -2.8f;
        [SerializeField] private float laneCenterX = -0.2f;
        [SerializeField] private float laneRightX = 2.4f;

        [Header("Lane Vertical Layout")]
        [SerializeField] private float startY = 1.2f;
        [SerializeField] private float verticalSpacing = 0.9f;
        [SerializeField] private float horizontalJitter = 0.16f;

        [Header("Depth")]
        [SerializeField] private float baseZ = 1.7f;
        [SerializeField] private float laneZStep = 0.08f;

        private void Awake()
        {
            if (applyOnAwake)
            {
                ApplyLayout();
            }
        }

        [ContextMenu("Apply Lane Layout")]
        public void ApplyLayout()
        {
            var root = holdsRoot != null ? holdsRoot : transform;
            var holds = root.GetComponentsInChildren<HoldComponent>(includeInactive: true);
            if (holds.Length == 0)
            {
                return;
            }

            Array.Sort(holds, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));

            var safeHoldsPerLane = Mathf.Max(1, holdsPerLane);
            for (var index = 0; index < holds.Length; index++)
            {
                var hold = holds[index];
                var lane = Mathf.Min(index / safeHoldsPerLane, 2);
                var row = index % safeHoldsPerLane;

                var x = GetLaneX(lane) + (row % 2 == 0 ? -horizontalJitter : horizontalJitter);
                var y = startY + row * verticalSpacing + lane * 0.1f;
                var z = baseZ + lane * laneZStep;

                hold.transform.position = new Vector3(x, y, z);
                if (hold.gripPoint != null)
                {
                    hold.gripPoint.localPosition = new Vector3(0f, 0f, -0.18f);
                }
            }

            var registry = FindFirstObjectByType<HoldRegistry>();
            if (registry != null)
            {
                registry.Refresh();
            }
        }

        private float GetLaneX(int lane)
        {
            return lane switch
            {
                0 => laneLeftX,
                1 => laneCenterX,
                _ => laneRightX,
            };
        }
    }
}
