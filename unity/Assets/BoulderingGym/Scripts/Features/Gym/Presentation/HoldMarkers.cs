using System.Collections.Generic;
using Project.Features.Gym.Domain;
using UnityEngine;

namespace Project.Features.Gym.Presentation
{
    /// <summary>Glowing rings around holds: the aimed hold, and the start and top holds of the selected problem.</summary>
    public sealed class HoldMarkers
    {
        private const int RingSegments = 28;
        private const float RingInner = 0.82f;
        private const float RingLift = 0.015f;
        private const float RingPadding = 1.35f;
        private const float TagCharacterSize = 0.025f;
        private const int TagFontSize = 64;
        private static readonly Color StartColor = new(1f, 1f, 1f);
        private static readonly Color TopColor = new(1f, 0.85f, 0.1f);

        private readonly Transform _root;
        private readonly MaterialLibrary _materials;
        private readonly Mesh _ring;
        private readonly List<GameObject> _problemMarkers = new();
        private readonly GameObject _target;

        public HoldMarkers(Transform parent, MaterialLibrary materials)
        {
            _root = new GameObject("Markers").transform;
            _root.SetParent(parent, false);
            _materials = materials;
            _ring = MeshFactory.ToMesh(RingMesh(), "Marker ring");
            _target = MeshFactory.CreateObject("Target marker", _root, _ring, materials.Tinted(materials.Templates.marker, Color.white));
            _target.SetActive(false);
        }

        public void ShowTarget(ProblemHold hold, Color color)
        {
            Place(_target.transform, hold);
            _target.GetComponent<MeshRenderer>().sharedMaterial = _materials.Tinted(_materials.Templates.marker, color);
            _target.SetActive(true);
        }

        public void HideTarget() => _target.SetActive(false);

        public void ShowProblem(BoulderProblem problem)
        {
            HideProblem();
            foreach (var hold in problem.Holds)
            {
                if (hold.Role != HoldRole.Intermediate)
                {
                    AddProblemMarker(hold, hold.Role == HoldRole.Top ? "TOP" : "START", hold.Role == HoldRole.Top ? TopColor : StartColor);
                }
            }
        }

        public void HideProblem()
        {
            foreach (var marker in _problemMarkers)
            {
                Object.Destroy(marker);
            }

            _problemMarkers.Clear();
        }

        private void AddProblemMarker(ProblemHold hold, string text, Color color)
        {
            var ring = MeshFactory.CreateObject($"{text} ring", _root, _ring, _materials.Tinted(_materials.Templates.marker, color));
            Place(ring.transform, hold);
            _problemMarkers.Add(ring);

            var tag = new GameObject($"{text} tag");
            tag.transform.SetParent(_root, false);
            tag.transform.SetPositionAndRotation(
                hold.SurfacePoint - hold.Up * (Mathf.Max(hold.Size.x, hold.Size.y) * 0.8f) + hold.Normal * 0.02f,
                Quaternion.LookRotation(-hold.Normal, hold.Up));
            var label = tag.AddComponent<TextMesh>();
            label.text = text;
            label.font = _materials.Templates.labelFont;
            label.fontSize = TagFontSize;
            label.characterSize = TagCharacterSize;
            label.anchor = TextAnchor.UpperCenter;
            label.color = color;
            tag.GetComponent<MeshRenderer>().sharedMaterial = _materials.Templates.label;
            _problemMarkers.Add(tag);
        }

        private static void Place(Transform marker, ProblemHold hold)
        {
            var radius = Mathf.Max(hold.Size.x, hold.Size.y) * 0.5f * RingPadding;
            marker.SetPositionAndRotation(hold.SurfacePoint + hold.Normal * RingLift, Quaternion.LookRotation(hold.Normal, hold.Up));
            marker.localScale = Vector3.one * radius;
        }

        /// <summary>Flat annulus of radius 1 in the XY plane facing +Z.</summary>
        private static MeshData RingMesh()
        {
            var mesh = new MeshData();
            for (var i = 0; i < RingSegments; i++)
            {
                var a = Direction(i);
                var b = Direction(i + 1);
                var uvs = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                mesh.AddQuad(a * RingInner, b * RingInner, b, a, uvs);
            }

            return mesh;
        }

        private static Vector3 Direction(int segment)
        {
            var angle = segment * Mathf.PI * 2f / RingSegments;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }
    }
}
