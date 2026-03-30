using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.Tools.Editor
{
    // builds solid block-out meshes from gym_blockout.json.
    // json sectors contain N bottom verts (y=0), N top verts (y=5), 1 apex (y=2).
    // the json indices create a broken fan through the apex — we ignore them
    // and re-triangulate as proper extrusions: wall quads + floor/ceiling caps.
    public static class BlockOutBuilder
    {
        [Serializable]
        private class GymData
        {
            public SectorData[] sectors;
        }

        [Serializable]
        private class SectorData
        {
            public string id;
            public string label;
            public string zone;
            public string color;
            public VertexData[] vertices;
            public int[] indices;
        }

        [Serializable]
        private class VertexData
        {
            public float x;
            public float y;
            public float z;
        }

        [MenuItem("Project/Block_Out/Build Complete Gym")]
        public static void BuildGym()
        {
            string path = EditorUtility.OpenFilePanel("Select gym_blockout.json", "", "json");
            if (string.IsNullOrEmpty(path)) return;

            string json_content = File.ReadAllText(path);
            GymData gym = JsonUtility.FromJson<GymData>(json_content);

            if (gym == null || gym.sectors == null)
            {
                Debug.LogError("Parsing Error: could not parse JSON or 'sectors' field is missing.");
                return;
            }

            // compute global bounding box to shift origin from center_island to bottom-left (min X, min Z)
            Vector3 global_min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 global_max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var s in gym.sectors)
            {
                if (s.vertices == null) continue;
                foreach (var v in s.vertices)
                {
                    if (v.x < global_min.x) global_min.x = v.x;
                    if (v.y < global_min.y) global_min.y = v.y;
                    if (v.z < global_min.z) global_min.z = v.z;
                    if (v.x > global_max.x) global_max.x = v.x;
                    if (v.y > global_max.y) global_max.y = v.y;
                    if (v.z > global_max.z) global_max.z = v.z;
                }
            }

            // offset so that the south-west ground corner becomes (0, 0, 0)
            Vector3 origin_offset = new Vector3(global_min.x, global_min.y, global_min.z);
            Vector3 gym_size = global_max - global_min;

            GameObject root = new GameObject("gym_reconstruction");

            // ground plane covering the entire gym footprint
            build_ground_plane(gym_size.x, gym_size.z, root.transform);

            // group sectors by zone, weld vertices at shared borders
            var zones = new Dictionary<string, List<SectorData>>();
            foreach (var s in gym.sectors)
            {
                string z = string.IsNullOrEmpty(s.zone) ? "unknown_zone" : s.zone;
                if (!zones.ContainsKey(z)) zones[z] = new List<SectorData>();
                zones[z].Add(s);
            }

            foreach (var kvp in zones)
            {
                build_zone_mesh(kvp.Key, kvp.Value, root.transform, origin_offset);
            }

            Debug.Log($"Gym build complete: {gym.sectors.Length} sectors in {zones.Count} zones. Floor: {gym_size.x:F1}m x {gym_size.z:F1}m.");
        }

        private static void build_ground_plane(float width, float depth, Transform parent)
        {
            GameObject floor = new GameObject("floor");
            floor.transform.SetParent(parent);

            MeshFilter mf = floor.AddComponent<MeshFilter>();
            MeshRenderer mr = floor.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh { name = "gym_floor" };
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(width, 0f, 0f),
                new Vector3(width, 0f, depth),
                new Vector3(0f, 0f, depth)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.75f, 0.75f, 0.72f);
            mr.sharedMaterial = mat;

            floor.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void build_zone_mesh(string zone_name, List<SectorData> sectors, Transform parent, Vector3 origin_offset)
        {
            GameObject zone_go = new GameObject(zone_name);
            zone_go.transform.SetParent(parent);

            MeshFilter mf = zone_go.AddComponent<MeshFilter>();
            MeshRenderer mr = zone_go.AddComponent<MeshRenderer>();

            var all_verts = new List<Vector3>();
            var all_tris = new List<int>();
            var welder = new Dictionary<Vector3Int, int>();

            const float weld_precision = 1000f;

            foreach (var sector in sectors)
            {
                if (sector.vertices == null || sector.vertices.Length < 3) continue;

                // classify vertices into bottom ring, top ring, and cyrano (apex)
                float min_y = float.MaxValue, max_y = float.MinValue;
                foreach (var v in sector.vertices)
                {
                    if (v.y < min_y) min_y = v.y;
                    if (v.y > max_y) max_y = v.y;
                }

                var bottom = new List<int>();
                var top = new List<int>();
                int cyrano_idx = -1;

                for (int i = 0; i < sector.vertices.Length; i++)
                {
                    var v = sector.vertices[i];
                    Vector3 pos = new Vector3(v.x, v.y, v.z) - origin_offset;
                    Vector3Int key = new Vector3Int(
                        Mathf.RoundToInt(pos.x * weld_precision),
                        Mathf.RoundToInt(pos.y * weld_precision),
                        Mathf.RoundToInt(pos.z * weld_precision)
                    );

                    if (!welder.TryGetValue(key, out int global_idx))
                    {
                        global_idx = all_verts.Count;
                        all_verts.Add(pos);
                        welder[key] = global_idx;
                    }

                    if (Mathf.Approximately(v.y, min_y)) bottom.Add(global_idx);
                    else if (Mathf.Approximately(v.y, max_y)) top.Add(global_idx);
                    else cyrano_idx = global_idx;
                }

                int n = bottom.Count;
                if (n < 3 || top.Count != n) continue;

                // determine polygon winding: compute signed area in XZ plane
                float signed_area = 0f;
                for (int i = 0; i < n; i++)
                {
                    Vector3 a = all_verts[bottom[i]];
                    Vector3 b = all_verts[bottom[(i + 1) % n]];
                    signed_area += (b.x - a.x) * (b.z + a.z);
                }
                // if signed_area > 0 the ring is CW in XZ — flip to get outward-facing normals
                bool flip = signed_area <= 0f;

                if (cyrano_idx >= 0)
                {
                    // wall faces split at the cyrano: bottom→cyrano + cyrano→top
                    for (int i = 0; i < n; i++)
                    {
                        int next = (i + 1) % n;
                        int b0 = bottom[i], b1 = bottom[next];
                        int t0 = top[i], t1 = top[next];
                        int cy = cyrano_idx;

                        if (flip)
                        {
                            // lower half: b0→cy, b1→cy quad
                            all_tris.Add(b0); all_tris.Add(cy); all_tris.Add(b1);
                            // upper half: cy→t0, t1 quad
                            all_tris.Add(cy); all_tris.Add(t0); all_tris.Add(t1);
                            // side triangles connecting lower and upper halves
                            all_tris.Add(b0); all_tris.Add(t0); all_tris.Add(cy);
                            all_tris.Add(b1); all_tris.Add(cy); all_tris.Add(t1);
                        }
                        else
                        {
                            all_tris.Add(b0); all_tris.Add(b1); all_tris.Add(cy);
                            all_tris.Add(cy); all_tris.Add(t1); all_tris.Add(t0);
                            all_tris.Add(b0); all_tris.Add(cy); all_tris.Add(t0);
                            all_tris.Add(b1); all_tris.Add(t1); all_tris.Add(cy);
                        }
                    }
                }
                else
                {
                    // no cyrano: flat wall quads
                    for (int i = 0; i < n; i++)
                    {
                        int next = (i + 1) % n;
                        int b0 = bottom[i], b1 = bottom[next];
                        int t0 = top[i], t1 = top[next];

                        if (flip)
                        {
                            all_tris.Add(b0); all_tris.Add(t0); all_tris.Add(t1);
                            all_tris.Add(b0); all_tris.Add(t1); all_tris.Add(b1);
                        }
                        else
                        {
                            all_tris.Add(b0); all_tris.Add(b1); all_tris.Add(t1);
                            all_tris.Add(b0); all_tris.Add(t1); all_tris.Add(t0);
                        }
                    }
                }

                // floor cap (fan from first bottom vertex, facing down)
                for (int i = 1; i < n - 1; i++)
                {
                    if (flip)
                    {
                        all_tris.Add(bottom[0]); all_tris.Add(bottom[i]); all_tris.Add(bottom[i + 1]);
                    }
                    else
                    {
                        all_tris.Add(bottom[0]); all_tris.Add(bottom[i + 1]); all_tris.Add(bottom[i]);
                    }
                }

                // ceiling cap (fan from first top vertex, facing up)
                for (int i = 1; i < n - 1; i++)
                {
                    if (flip)
                    {
                        all_tris.Add(top[0]); all_tris.Add(top[i + 1]); all_tris.Add(top[i]);
                    }
                    else
                    {
                        all_tris.Add(top[0]); all_tris.Add(top[i]); all_tris.Add(top[i + 1]);
                    }
                }
            }

            Mesh mesh = new Mesh { name = zone_name };
            if (all_verts.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(all_verts);
            mesh.SetTriangles(all_tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();
            mf.sharedMesh = mesh;

            // double-sided material to avoid any remaining winding issues
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            if (sectors.Count > 0 && !string.IsNullOrEmpty(sectors[0].color)
                && ColorUtility.TryParseHtmlString(sectors[0].color, out Color col))
            {
                mat.color = col;
            }
            else
            {
                mat.color = new Color(0.6f, 0.6f, 0.6f);
            }
            mr.sharedMaterial = mat;

            zone_go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
    }
}