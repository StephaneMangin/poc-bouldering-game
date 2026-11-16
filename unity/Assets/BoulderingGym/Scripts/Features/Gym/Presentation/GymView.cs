using Project.Features.Gym.Domain;
using UnityEngine;

namespace Project.Features.Gym.Presentation
{
    /// <summary>Builds the static gym: climbing structures, floor, building shell and sector signs.</summary>
    public sealed class GymView
    {
        private const float FloorThickness = 0.5f;
        private const float ShellThickness = 0.5f;
        private const float SignLift = 0.45f;
        private const float SignCharacterSize = 0.035f;
        private const int SignFontSize = 64;

        private static readonly Color WhiteboxWall = new(0.86f, 0.86f, 0.84f);
        private static readonly Color GreyboxWall = new(0.62f, 0.63f, 0.62f);
        private static readonly Color TrimGrey = new(0.48f, 0.49f, 0.50f);
        private static readonly Color FloorGrey = new(0.55f, 0.55f, 0.53f);
        private static readonly Color ShellGrey = new(0.40f, 0.41f, 0.42f);
        private static readonly Color SteelYellow = new(0.95f, 0.74f, 0.08f);
        private static readonly Color DuctWhite = new(0.93f, 0.93f, 0.91f);
        private static readonly Color SkylightWhite = new(0.95f, 0.97f, 1f);
        private static readonly Color LampWarm = new(1f, 0.93f, 0.82f);
        private static readonly Color MastWood = new(0.62f, 0.45f, 0.28f);
        private static readonly Color BuoyRed = new(0.85f, 0.10f, 0.10f);
        private static readonly Color FlagBlack = new(0.06f, 0.06f, 0.07f);
        private static readonly Color DeckGrey = new(0.32f, 0.32f, 0.34f);
        private static readonly Color RailBlack = new(0.08f, 0.08f, 0.09f);
        private static readonly Color TableOrange = new(0.95f, 0.42f, 0.12f);

        private readonly MaterialLibrary _materials;
        private readonly PanelTextureFactory _panels;

        public GymView(MaterialLibrary materials, PanelTextureFactory panels)
        {
            _materials = materials;
            _panels = panels;
        }

        public Transform Build(GymLayout layout, GymContent content, VisualPass pass, Transform parent)
        {
            var root = new GameObject("Gym").transform;
            root.SetParent(parent, false);
            foreach (var structure in layout.Structures)
            {
                BuildStructure(structure, pass, (layout, content), root);
            }

            BuildHall(layout, pass, root);
            AddColliders(layout, root);
            return root;
        }

        private void BuildStructure(GymStructure structure, VisualPass pass, (GymLayout layout, GymContent content) gym, Transform parent)
        {
            var root = new GameObject(structure.Name).transform;
            root.SetParent(parent, false);
            var outline = structure.Outline;
            for (var i = 0; i < outline.Edges.Count; i++)
            {
                var face = outline.Edges[i].Face;
                if (face != null)
                {
                    var mesh = MeshFactory.ToMesh(StructureMeshBuilder.BuildFace(outline, i), face.Id);
                    MeshFactory.CreateSolid(face.Id, root, mesh, FaceMaterial(face, pass, gym));
                }
            }

            var trim = MeshFactory.ToMesh(StructureMeshBuilder.BuildTrim(outline), $"{structure.Id}-trim");
            MeshFactory.CreateSolid($"{structure.Id} trim", root, trim, _materials.Tinted(_materials.Templates.plain, TrimGrey));
            foreach (var sector in structure.Sectors)
            {
                BuildSign(sector, root);
            }
        }

        private Material FaceMaterial(WallFace face, VisualPass pass, (GymLayout layout, GymContent content) gym)
        {
            return pass switch
            {
                VisualPass.Whitebox => _materials.Tinted(_materials.Templates.plain, WhiteboxWall),
                VisualPass.Greybox => _materials.Tinted(_materials.Templates.plain, GreyboxWall),
                _ => _panels.CreateMaterial(face, gym.layout, gym.content),
            };
        }

        /// <summary>Mats, cladding, roof and mezzanine; the art pass adds the steel frame, ducts, skylights, lamps and furniture.</summary>
        private void BuildHall(GymLayout layout, VisualPass pass, Transform parent)
        {
            var parts = EnvironmentDressing.Build(layout);
            var plain = _materials.Templates.plain;
            if (pass != VisualPass.Art)
            {
                Create(parts[DressingMaterial.Floor], "Floor", _materials.Tinted(plain, FloorGrey), parent);
                Create(parts[DressingMaterial.Cladding], "Cladding", _materials.Tinted(plain, ShellGrey), parent);
                Create(parts[DressingMaterial.Ceiling], "Ceiling", _materials.Tinted(plain, ShellGrey), parent);
                Create(parts[DressingMaterial.Mezzanine], "Mezzanine", _materials.Tinted(plain, ShellGrey), parent);
                Create(parts[DressingMaterial.Railing], "Mezzanine posts and railing", _materials.Tinted(plain, ShellGrey), parent);
                return;
            }

            var size = layout.EnvelopeMax - layout.EnvelopeMin;
            Create(parts[DressingMaterial.Floor], "Floor", _panels.CreateHallMaterial(HallPainter.Floor(size), TextureWrapMode.Clamp), parent);
            Create(parts[DressingMaterial.Cladding], "Cladding", _panels.CreateHallMaterial(HallPainter.CorrugatedCladding(), TextureWrapMode.Repeat), parent);
            Create(parts[DressingMaterial.Ceiling], "Ceiling", _panels.CreateHallMaterial(HallPainter.RoofSheeting(), TextureWrapMode.Repeat), parent);
            Create(parts[DressingMaterial.Steel], "Steel frame", _materials.Tinted(plain, SteelYellow), parent);
            Create(parts[DressingMaterial.Duct], "Ducts", _materials.Tinted(plain, DuctWhite), parent);
            Create(parts[DressingMaterial.Skylight], "Skylights", _materials.Tinted(_materials.Templates.marker, SkylightWhite), parent);
            Create(parts[DressingMaterial.Lamp], "Lamps", _materials.Tinted(_materials.Templates.marker, LampWarm), parent);
            Create(parts[DressingMaterial.LightStrip], "Light strips", _materials.Tinted(_materials.Templates.marker, SkylightWhite), parent);
            Create(parts[DressingMaterial.Wood], "Mast and bar", _materials.Tinted(plain, MastWood), parent);
            Create(parts[DressingMaterial.BuoyRed], "Lifebuoy", _materials.Tinted(plain, BuoyRed), parent);
            Create(parts[DressingMaterial.BuoyWhite], "Lifebuoy bands", _materials.Tinted(plain, DuctWhite), parent);
            Create(parts[DressingMaterial.Flag], "Pirate flag", _materials.Tinted(plain, FlagBlack), parent);
            Create(parts[DressingMaterial.Mezzanine], "Mezzanine", _materials.Tinted(plain, DeckGrey), parent);
            Create(parts[DressingMaterial.Railing], "Mezzanine posts and railing", _materials.Tinted(plain, RailBlack), parent);
            Create(parts[DressingMaterial.Furniture], "Restaurant tables", _materials.Tinted(plain, TableOrange), parent);
        }

        private static void Create(MeshData data, string name, Material material, Transform parent)
        {
            MeshFactory.CreateObject(name, parent, MeshFactory.ToMesh(data, name), material);
        }

        /// <summary>Floor slab and building walls: the climber walks on the first and stays inside the second.</summary>
        private static void AddColliders(GymLayout layout, Transform parent)
        {
            var min = new Vector3(layout.EnvelopeMin.x, 0f, layout.EnvelopeMin.y);
            var max = new Vector3(layout.EnvelopeMax.x, 0f, layout.EnvelopeMax.y);
            var floor = new GameObject("Floor collider").AddComponent<BoxCollider>();
            floor.transform.SetParent(parent, false);
            floor.center = (min + max) * 0.5f + Vector3.down * (FloorThickness * 0.5f);
            floor.size = new Vector3(max.x - min.x, FloorThickness, max.z - min.z);
            var corners = new[] { min, new Vector3(max.x, 0f, min.z), max, new Vector3(min.x, 0f, max.z) };
            for (var i = 0; i < corners.Length; i++)
            {
                AddWallCollider(parent, corners[i], corners[(i + 1) % corners.Length], layout.CeilingHeight);
            }

            if (layout.Mezzanine != null)
            {
                AddMezzanineCollider(layout.Mezzanine, parent);
            }
        }

        /// <summary>The bar under the mezzanine is outside the climbing floor: its whole volume is solid.</summary>
        private static void AddMezzanineCollider(MezzanineArea area, Transform parent)
        {
            var collider = new GameObject("Mezzanine collider").AddComponent<BoxCollider>();
            collider.transform.SetParent(parent, false);
            collider.center = new Vector3((area.Min.x + area.Max.x) * 0.5f, area.Height * 0.5f, (area.Min.y + area.Max.y) * 0.5f);
            collider.size = new Vector3(area.Max.x - area.Min.x, area.Height, area.Max.y - area.Min.y);
        }

        private static void AddWallCollider(Transform parent, Vector3 a, Vector3 b, float height)
        {
            var along = (b - a).normalized;
            var outward = new Vector3(along.z, 0f, -along.x);
            var go = new GameObject("Building wall collider");
            go.transform.SetParent(parent, false);
            var collider = go.AddComponent<BoxCollider>();
            collider.center = (a + b) * 0.5f + Vector3.up * (height * 0.5f) + outward * (ShellThickness * 0.5f);
            collider.size = new Vector3(Mathf.Abs(b.x - a.x) + ShellThickness, height, Mathf.Abs(b.z - a.z) + ShellThickness);
        }

        private void BuildSign(GymSector sector, Transform parent)
        {
            var face = WidestFace(sector);
            if (face == null)
            {
                return;
            }

            var sign = new GameObject($"{sector.Name} sign");
            sign.transform.SetParent(parent, false);
            sign.transform.position = face.SurfacePoint(face.Width * 0.5f, face.Height) + Vector3.up * SignLift + face.NormalAt(face.Height) * 0.1f;
            sign.transform.rotation = Quaternion.LookRotation(-face.Outward, Vector3.up);
            var text = sign.AddComponent<TextMesh>();
            text.text = sector.Name;
            text.font = _materials.Templates.labelFont;
            text.fontSize = SignFontSize;
            text.characterSize = SignCharacterSize;
            text.anchor = TextAnchor.LowerCenter;
            text.color = Color.white;
            sign.GetComponent<MeshRenderer>().sharedMaterial = _materials.Templates.label;
        }

        private static WallFace WidestFace(GymSector sector)
        {
            WallFace widest = null;
            foreach (var face in sector.Faces)
            {
                if (widest == null || face.Width > widest.Width)
                {
                    widest = face;
                }
            }

            return widest;
        }
    }
}
