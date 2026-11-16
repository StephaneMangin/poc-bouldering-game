using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Project.Features.Gym.Domain;
using UnityEngine;

/// <summary>
/// Collects named meshes and materials and writes them as OBJ + MTL + PNG. Unity is left handed:
/// x is mirrored and triangles reversed so the scene keeps its real orientation in right-handed tools.
/// </summary>
public sealed class ObjScene
{
    private readonly string _directory;
    private readonly List<(string name, string material, MeshData mesh)> _objects = new();
    private readonly Dictionary<string, string> _materials = new();

    public ObjScene(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
    }

    public void Add(string name, string material, MeshData mesh)
    {
        if (!mesh.IsEmpty)
        {
            _objects.Add((name, material, mesh));
        }
    }

    public void Material(string name, Color color, float roughness = 0.8f, string texture = null, float emission = 0f)
    {
        var linear = new Color(ToLinear(color.r), ToLinear(color.g), ToLinear(color.b));
        var builder = new StringBuilder();
        builder.AppendLine($"newmtl {name}");
        builder.AppendLine(F($"Kd {linear.r} {linear.g} {linear.b}"));
        builder.AppendLine(F($"Ns {(1f - roughness) * 900f}"));
        builder.AppendLine(F($"Pr {roughness}"));
        if (texture != null)
        {
            builder.AppendLine($"map_Kd {texture}");
        }

        if (emission > 0f)
        {
            builder.AppendLine(F($"Ke {linear.r * emission} {linear.g * emission} {linear.b * emission}"));
        }

        _materials[name] = builder.ToString();
    }

    public void Texture(string fileName, PanelCanvas canvas)
    {
        PngWriter.Write(Path.Combine(_directory, fileName), canvas.Width, canvas.Height, canvas.Pixels);
    }

    public void Write(string baseName)
    {
        File.WriteAllText(Path.Combine(_directory, baseName + ".mtl"), string.Concat(_materials.Values));
        using var writer = new StreamWriter(Path.Combine(_directory, baseName + ".obj"));
        writer.WriteLine($"mtllib {baseName}.mtl");
        var offset = 1;
        foreach (var (name, material, mesh) in _objects)
        {
            writer.WriteLine($"o {name}");
            foreach (var v in mesh.Vertices)
            {
                writer.WriteLine(F($"v {-v.x} {v.y} {v.z}"));
            }

            foreach (var uv in mesh.Uvs)
            {
                writer.WriteLine(F($"vt {uv.x} {uv.y}"));
            }

            foreach (var n in mesh.Normals)
            {
                writer.WriteLine(F($"vn {-n.x} {n.y} {n.z}"));
            }

            writer.WriteLine($"usemtl {material}");
            for (var t = 0; t < mesh.Triangles.Count; t += 3)
            {
                var a = mesh.Triangles[t] + offset;
                var b = mesh.Triangles[t + 2] + offset;
                var c = mesh.Triangles[t + 1] + offset;
                writer.WriteLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
            }

            offset += mesh.Vertices.Count;
        }
    }

    private static string F(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    /// <summary>sRGB to linear (Color.linear calls native engine code).</summary>
    private static float ToLinear(float c) => c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
}
