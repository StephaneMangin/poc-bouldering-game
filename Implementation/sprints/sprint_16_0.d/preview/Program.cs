using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Project.Features.Climbing.Domain;
using Project.Features.Gym.Domain;
using UnityEngine;

/// <summary>
/// Offline companion of the game, using the same domain code:
///   bake   layout.json problems.json            generates the problems and volumes the game loads
///   export layout.json problems.json dir pass   writes the gym as OBJ (pass: whitebox, greybox, art)
///   wire   layout.json out.json                 writes the gym edges (polylines) for photo overlays
///   route  layout.json problems.json id out.json [scale]  writes a playtest scenario climbing problem id
///          with the climber scale the game plays (logged at start as "played as")
/// </summary>
public static class Program
{
    private static readonly Color WhiteboxWall = new(0.86f, 0.86f, 0.84f);
    private static readonly Color GreyboxWall = new(0.62f, 0.63f, 0.62f);
    private static readonly Color TrimGrey = new(0.48f, 0.49f, 0.50f);
    private static readonly Color PlainFloor = new(0.55f, 0.55f, 0.53f);
    private static readonly Color PlainShell = new(0.40f, 0.41f, 0.42f);
    private static readonly Color SteelYellow = new(0.95f, 0.74f, 0.08f);
    private static readonly Color DuctWhite = new(0.93f, 0.93f, 0.91f);
    private static readonly Color SkylightWhite = new(0.95f, 0.97f, 1f);
    private static readonly Color LampWarm = new(1f, 0.93f, 0.82f);
    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true };

    public static int Main(string[] args)
    {
        var layout = GymLayoutFactory.Build(JsonSerializer.Deserialize<LayoutDefinition>(File.ReadAllText(args[1]), Json));
        var circuits = Circuit.CreateAll(CircuitParameters.Defaults());
        if (args[0] == "wire")
        {
            File.WriteAllText(args[2], JsonSerializer.Serialize(Wireframe(layout)));
            return 0;
        }

        if (args[0] == "bake")
        {
            var generated = GymContent.Plan(layout, circuits, ClimberAnatomy.Default.Scaled(0.94f), ClimberAnatomy.Default.Scaled(1.04f));
            File.WriteAllText(args[2], JsonSerializer.Serialize(ContentBaking.ToDefinition(generated), Json));
            Console.WriteLine($"baked {generated.ProblemCount} problems, {generated.HoldCount} holds -> {args[2]}");
            return 0;
        }

        var content = ContentBaking.FromDefinition(layout, JsonSerializer.Deserialize<ContentDefinition>(File.ReadAllText(args[2]), Json), circuits);
        if (args[0] == "route")
        {
            var scale = args.Length > 5 ? float.Parse(args[5], CultureInfo.InvariantCulture) : 1f;
            return WriteRouteScenario(content.ProblemById(args[3]), args[4], ClimberAnatomy.Default.Scaled(scale));
        }

        var pass = Enum.Parse<VisualPass>(args[4], true);
        var scene = new ObjScene(args[3]);
        AddStructures(scene, layout, content, pass);
        AddHolds(scene, layout, content, pass);
        AddHall(scene, layout, pass);
        scene.Write("gym");
        WriteSceneInfo(Path.Combine(args[3], "scene_info.json"), layout, content);
        Console.WriteLine($"{pass}: {content.ProblemCount} problems, {content.HoldCount} holds -> {args[3]}");
        return 0;
    }

    private static void AddStructures(ObjScene scene, GymLayout layout, GymContent content, VisualPass pass)
    {
        scene.Material("trim", TrimGrey);
        scene.Material("wall_whitebox", WhiteboxWall);
        scene.Material("wall_greybox", GreyboxWall);
        foreach (var structure in layout.Structures)
        {
            var outline = structure.Outline;
            for (var i = 0; i < outline.Edges.Count; i++)
            {
                var face = outline.Edges[i].Face;
                if (face != null)
                {
                    var mesh = StructureMeshBuilder.BuildFace(outline, i);
                    scene.Add($"face_{face.Id}", WallMaterial(scene, face, mesh, (layout, content), pass), mesh);
                }
            }

            scene.Add($"trim_{structure.Id}", "trim", StructureMeshBuilder.BuildTrim(outline));
        }
    }

    /// <summary>Face UVs are meters; the game maps them with the material scale, the export bakes it in the UVs.</summary>
    private static string WallMaterial(ObjScene scene, WallFace face, MeshData mesh, (GymLayout layout, GymContent content) gym, VisualPass pass)
    {
        if (pass != VisualPass.Art)
        {
            return pass == VisualPass.Whitebox ? "wall_whitebox" : "wall_greybox";
        }

        var canvas = PanelPainter.Paint(face, gym.layout, 96, 2048, gym.content.HandHoldSpots(face));
        for (var i = 0; i < mesh.Uvs.Count; i++)
        {
            var uv = mesh.Uvs[i];
            mesh.Uvs[i] = new Vector2((uv.x - canvas.OriginU) / canvas.MetersWide, uv.y / canvas.MetersHigh);
        }

        var file = $"panel_{face.Id}.png";
        scene.Texture(file, canvas);
        scene.Material($"panel_{face.Id}", Color.white, 0.85f, file);
        return $"panel_{face.Id}";
    }

    private static void AddHolds(ObjScene scene, GymLayout layout, GymContent content, VisualPass pass)
    {
        foreach (var structure in layout.Structures)
        {
            foreach (var batch in HoldBatches.Build(structure, content, layout, pass))
            {
                var name = $"hold_{ColorUtility.ToHtmlStringRGB(batch.Key)}";
                scene.Material(name, batch.Key, 0.45f);
                scene.Add($"{name}_{structure.Id}", name, batch.Value);
            }
        }
    }

    private static void AddHall(ObjScene scene, GymLayout layout, VisualPass pass)
    {
        var parts = EnvironmentDressing.Build(layout);
        if (pass != VisualPass.Art)
        {
            scene.Material("floor_plain", PlainFloor);
            scene.Material("shell_plain", PlainShell);
            scene.Add("floor", "floor_plain", parts[DressingMaterial.Floor]);
            scene.Add("cladding", "shell_plain", parts[DressingMaterial.Cladding]);
            scene.Add("ceiling", "shell_plain", parts[DressingMaterial.Ceiling]);
            scene.Add("mezzanine", "shell_plain", parts[DressingMaterial.Mezzanine]);
            scene.Add("railing", "shell_plain", parts[DressingMaterial.Railing]);
            return;
        }

        var size = layout.EnvelopeMax - layout.EnvelopeMin;
        var floor = parts[DressingMaterial.Floor];
        for (var i = 0; i < floor.Uvs.Count; i++)
        {
            floor.Uvs[i] = new Vector2(floor.Uvs[i].x / size.x, floor.Uvs[i].y / size.y);
        }

        scene.Texture("floor.png", HallPainter.Floor(size));
        scene.Texture("cladding.png", HallPainter.CorrugatedCladding());
        scene.Texture("roof.png", HallPainter.RoofSheeting());
        scene.Material("floor", Color.white, 0.9f, "floor.png");
        scene.Material("cladding", Color.white, 0.6f, "cladding.png");
        scene.Material("roof", Color.white, 0.6f, "roof.png");
        scene.Material("steel", SteelYellow, 0.5f);
        scene.Material("duct", DuctWhite, 0.95f);
        scene.Material("skylight", SkylightWhite, 1f, null, 6f);
        scene.Material("lamp", LampWarm, 1f, null, 4f);
        scene.Add("floor", "floor", parts[DressingMaterial.Floor]);
        scene.Add("cladding", "cladding", parts[DressingMaterial.Cladding]);
        scene.Add("ceiling", "roof", parts[DressingMaterial.Ceiling]);
        scene.Add("steel", "steel", parts[DressingMaterial.Steel]);
        scene.Add("ducts", "duct", parts[DressingMaterial.Duct]);
        scene.Add("skylights", "skylight", parts[DressingMaterial.Skylight]);
        scene.Add("lamps", "lamp", parts[DressingMaterial.Lamp]);
        scene.Material("strip", SkylightWhite, 1f, null, 5f);
        scene.Material("wood", new Color(0.62f, 0.45f, 0.28f), 0.7f);
        scene.Material("buoy_red", new Color(0.85f, 0.10f, 0.10f), 0.5f);
        scene.Material("buoy_white", DuctWhite, 0.5f);
        scene.Material("flag", new Color(0.06f, 0.06f, 0.07f), 0.9f);
        scene.Add("strips", "strip", parts[DressingMaterial.LightStrip]);
        scene.Add("mast_and_bar", "wood", parts[DressingMaterial.Wood]);
        scene.Add("buoy", "buoy_red", parts[DressingMaterial.BuoyRed]);
        scene.Add("buoy_bands", "buoy_white", parts[DressingMaterial.BuoyWhite]);
        scene.Add("flag", "flag", parts[DressingMaterial.Flag]);
        scene.Material("deck", new Color(0.32f, 0.32f, 0.34f), 0.8f);
        scene.Material("railing", new Color(0.08f, 0.08f, 0.09f), 0.6f);
        scene.Material("table", new Color(0.95f, 0.42f, 0.12f), 0.6f);
        scene.Add("mezzanine", "deck", parts[DressingMaterial.Mezzanine]);
        scene.Add("railing", "railing", parts[DressingMaterial.Railing]);
        scene.Add("tables", "table", parts[DressingMaterial.Furniture]);
    }

    /// <summary>
    /// UAT harness scenario replaying the validator's way up the problem: each move aims the hold by
    /// its collider name and presses the limb key. The game must be started with -problem id.
    /// </summary>
    private static int WriteRouteScenario(BoulderProblem problem, string path, ClimberAnatomy anatomy)
    {
        const float firstMove = 2f;
        const float moveInterval = 1.4f;
        var route = ClimbabilityValidator.FindRoute(problem.Holds, anatomy);
        if (route == null)
        {
            Console.WriteLine($"no route for {problem.Id}");
            return 1;
        }

        var keys = new[] { "Q", "E", "A", "D" };
        var steps = route.Select((move, i) => new
        {
            atSeconds = firstMove + i * moveInterval,
            durationSeconds = 0.2f,
            key = keys[(int)move.limb],
            mouseButton = -1,
            mouseScreenX = -1f,
            mouseScreenY = -1f,
            aimAtHold = move.hold.Name,
        }).ToList();
        var scenario = new
        {
            name = $"climb_{problem.Id}",
            scene = "BlockOutRennes",
            durationSeconds = firstMove + route.Count * moveInterval + 4f,
            screenshotEveryNFrames = 8,
            mouseScreenX = 0.5f,
            mouseScreenY = 0.5f,
            steps,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(scenario, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{problem.Id} ({problem.Circuit.ColorId} {problem.Grade}): {route.Count} moves -> {path}");
        return 0;
    }

    /// <summary>
    /// Edges of the gym in game coordinates, grouped by structure: the outline ring at every profile level
    /// and the corner lines from the floor to the top; plus the hall box and the mast.
    /// </summary>
    private static object Wireframe(GymLayout layout)
    {
        var structures = layout.Structures.Select(structure =>
        {
            var outline = structure.Outline;
            var rings = Enumerable.Range(0, outline.Levels.Count).Select(k => outline.Corners
                .SelectMany(c => c.IsSplit ? new[] { c.Incoming[k], c.Outgoing[k] } : new[] { c.Incoming[k] })
                .Select(Point).ToList()).ToList();
            var corners = outline.Corners.SelectMany(c => c.IsSplit ? new[] { c.Incoming, c.Outgoing } : new[] { c.Incoming })
                .Select(column => column.Select(Point).ToList()).ToList();
            return new { id = structure.Id, rings, corners };
        }).ToList();
        var min = layout.EnvelopeMin;
        var max = layout.EnvelopeMax;
        var hall = new[] { new Vector3(min.x, 0f, min.y), new Vector3(max.x, 0f, min.y), new Vector3(max.x, 0f, max.y), new Vector3(min.x, 0f, max.y) };
        return new
        {
            structures,
            hallFloor = hall.Select(Point).ToList(),
            hallCeiling = hall.Select(p => Point(p + Vector3.up * layout.CeilingHeight)).ToList(),
            mast = layout.MastFoot.HasValue ? new[] { Point(layout.MastFoot.Value), Point(new Vector3(layout.MastFoot.Value.x, layout.CeilingHeight, layout.MastFoot.Value.z)) } : null,
        };
    }

    /// <summary>Hall bounds and the start spot of each problem, in game (Unity) coordinates.</summary>
    private static void WriteSceneInfo(string path, GymLayout layout, GymContent content)
    {
        var problems = content.Problems.Select(problem => new
        {
            id = problem.Id,
            circuit = problem.Circuit.ColorId,
            grade = problem.Grade,
            start = Point(problem.Face.FloorPointInFront(problem.StartS, 0.9f)),
            facing = Point(-problem.Face.Outward),
        });
        var info = new
        {
            envelopeMin = new[] { layout.EnvelopeMin.x, layout.EnvelopeMin.y },
            envelopeMax = new[] { layout.EnvelopeMax.x, layout.EnvelopeMax.y },
            ceiling = layout.CeilingHeight,
            spawn = Point(layout.SpawnPosition),
            problems,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static float[] Point(Vector3 v) => new[] { v.x, v.y, v.z };
}
