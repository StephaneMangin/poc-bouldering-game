using Project.Features.Gym.Domain;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.Features.Gym.Presentation
{
    /// <summary>Uploads engine-agnostic mesh data to Unity meshes and scene objects.</summary>
    public static class MeshFactory
    {
        public static Mesh ToMesh(MeshData data, string name)
        {
            var mesh = new Mesh { name = name };
            if (data.Vertices.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(data.Vertices);
            mesh.SetNormals(data.Normals);
            mesh.SetUVs(0, data.Uvs);
            mesh.SetTriangles(data.Triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        public static GameObject CreateObject(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.isStatic = true;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        public static GameObject CreateSolid(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = CreateObject(name, parent, mesh, material);
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }
    }
}
