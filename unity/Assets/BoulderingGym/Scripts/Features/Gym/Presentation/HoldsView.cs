using Project.Features.Gym.Domain;
using UnityEngine;

namespace Project.Features.Gym.Presentation
{
    /// <summary>Collider handle of a hold, found by the mouse targeting raycast.</summary>
    public sealed class HoldTarget : MonoBehaviour
    {
        public ProblemHold Hold { get; set; }
    }

    /// <summary>
    /// Builds holds and volumes: one merged render mesh per structure and color, plus one sphere
    /// collider per hold on the Holds layer for targeting, named after its problem for scripted tests.
    /// </summary>
    public sealed class HoldsView
    {
        public const string HoldsLayerName = "Holds";

        private const float ColliderPadding = 0.03f;

        private readonly MaterialLibrary _materials;

        public HoldsView(MaterialLibrary materials)
        {
            _materials = materials;
        }

        public Transform Build(GymContent content, GymLayout layout, VisualPass pass, Transform parent)
        {
            var root = new GameObject("Holds and volumes").transform;
            root.SetParent(parent, false);
            foreach (var structure in layout.Structures)
            {
                foreach (var batch in HoldBatches.Build(structure, content, layout, pass))
                {
                    var name = $"{structure.Id} {ColorUtility.ToHtmlStringRGB(batch.Key)}";
                    MeshFactory.CreateObject(name, root, MeshFactory.ToMesh(batch.Value, name), _materials.Tinted(_materials.Templates.hold, batch.Key));
                }
            }

            AddColliders(content, root, LayerMask.NameToLayer(HoldsLayerName));
            return root;
        }

        private static void AddColliders(GymContent content, Transform parent, int layer)
        {
            foreach (var problem in content.Problems)
            {
                foreach (var hold in problem.Holds)
                {
                    var go = new GameObject(hold.Name) { layer = layer };
                    go.transform.SetParent(parent, false);
                    go.transform.position = hold.SurfacePoint + hold.Normal * (hold.Size.z * 0.5f);
                    var collider = go.AddComponent<SphereCollider>();
                    collider.radius = Mathf.Max(hold.Size.x, hold.Size.y) * 0.5f + ColliderPadding;
                    go.AddComponent<HoldTarget>().Hold = hold;
                }
            }
        }
    }
}
