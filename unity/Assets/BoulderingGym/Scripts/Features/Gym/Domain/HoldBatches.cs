using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Gym.Domain
{
    /// <summary>
    /// Holds and volumes of a structure merged into one mesh per color, so the renderer draws a
    /// whole wall's holds of one circuit in a single call. Shared by the game and the preview export.
    /// </summary>
    public static class HoldBatches
    {
        private const int ShapeVariants = 6;
        private const float TagSize = 0.09f;
        private const float TagThickness = 0.008f;
        private const float TagGap = 0.08f;
        private static readonly Color GreyboxVolume = new(0.55f, 0.56f, 0.55f);
        private static readonly Color TagBlack = new(0.04f, 0.04f, 0.05f);

        public static Dictionary<Color, MeshData> Build(GymStructure structure, GymContent content, GymLayout layout, VisualPass pass)
        {
            var batches = new Dictionary<Color, MeshData>();
            var shapes = new Dictionary<(HoldKind, int), MeshData>();
            foreach (var sector in content.Sectors)
            {
                if (sector.Sector.Structure != structure)
                {
                    continue;
                }

                AddHolds(sector, layout, pass, batches, shapes);
                if (pass != VisualPass.Whitebox)
                {
                    AddVolumes(sector, layout, pass, batches);
                    AddStartTags(sector, batches);
                }
            }

            return batches;
        }

        /// <summary>Rotates an in-plane direction around the wall normal.</summary>
        public static Vector3 Roll(Vector3 up, Vector3 normal, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var side = Vector3.Cross(up, normal);
            return up * Mathf.Cos(radians) + side * Mathf.Sin(radians);
        }

        private static void AddHolds(SectorContent sector, GymLayout layout, VisualPass pass, Dictionary<Color, MeshData> batches, Dictionary<(HoldKind, int), MeshData> shapes)
        {
            foreach (var problem in sector.Problems)
            {
                var color = layout.ColorOf("hold_" + problem.Circuit.ColorId);
                foreach (var hold in problem.Holds)
                {
                    var shape = pass == VisualPass.Whitebox ? HoldMeshFactory.Placeholder(hold.Kind) : Shape(shapes, hold);
                    var up = Roll(hold.Up, hold.Normal, hold.RollDeg);
                    Batch(batches, color).AppendTransformed(shape, hold.SurfacePoint, Vector3.Cross(up, hold.Normal), up, hold.Normal, hold.Scale);
                }
            }
        }

        private static void AddVolumes(SectorContent sector, GymLayout layout, VisualPass pass, Dictionary<Color, MeshData> batches)
        {
            foreach (var volume in sector.Volumes)
            {
                var color = pass == VisualPass.Art ? layout.ColorOf(volume.ColorId) : GreyboxVolume;
                var normal = volume.Face.NormalAt(volume.Y);
                var up = Roll(volume.Face.UpTangentAt(volume.Y), normal, volume.RollDeg);
                var origin = volume.Face.SurfacePoint(volume.S, volume.Y);
                var mesh = VolumeMeshFactory.Build(volume.Shape, volume.Size, volume.Depth);
                Batch(batches, color).AppendTransformed(mesh, origin, Vector3.Cross(up, normal), up, normal, 1f);
            }
        }

        /// <summary>Black round tag beside each start hold, as gyms mark their problems.</summary>
        private static void AddStartTags(SectorContent sector, Dictionary<Color, MeshData> batches)
        {
            var disc = VolumeMeshFactory.Build(VolumeShape.Dome, TagSize, TagThickness);
            foreach (var problem in sector.Problems)
            {
                foreach (var hold in problem.StartHolds)
                {
                    var side = Vector3.Cross(hold.Up, hold.Normal);
                    var position = hold.SurfacePoint + side * (hold.Size.x * 0.5f + TagGap);
                    Batch(batches, TagBlack).AppendTransformed(disc, position, side, hold.Up, hold.Normal, 1f);
                }
            }
        }

        private static MeshData Shape(Dictionary<(HoldKind, int), MeshData> shapes, ProblemHold hold)
        {
            var key = (hold.Kind, hold.Id % ShapeVariants);
            if (!shapes.TryGetValue(key, out var shape))
            {
                shape = HoldMeshFactory.Build(hold.Kind, key.Item2);
                shapes[key] = shape;
            }

            return shape;
        }

        private static MeshData Batch(Dictionary<Color, MeshData> batches, Color color)
        {
            if (!batches.TryGetValue(color, out var batch))
            {
                batch = new MeshData();
                batches[color] = batch;
            }

            return batch;
        }
    }
}
