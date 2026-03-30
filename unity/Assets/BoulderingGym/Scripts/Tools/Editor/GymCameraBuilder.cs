using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Project.Tools.Editor
{
    /// <summary>
    /// One-click: creates gym_generator, populates database, generates cameras,
    /// auto-assigns photo textures to each gym_overlay.
    /// Menu: Project/Gym/Build Reference Cameras
    /// </summary>
    public static class GymCameraBuilder
    {
        private const string PhotosAssetFolder = "Assets/BoulderingGym/Art/RefPhotos";

        [MenuItem("Project/Gym/Build Reference Cameras")]
        public static void Build()
        {
            // ensure the photos folder exists in Assets
            if (!AssetDatabase.IsValidFolder(PhotosAssetFolder))
            {
                CreateFolderRecursive(PhotosAssetFolder);
                Debug.LogWarning(
                    $"Created {PhotosAssetFolder} — import your gym photos there and re-run."
                );
                return;
            }

            // remove previous generation if any
            var old = GameObject.Find("GymCameras");
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old);
            }

            // 1. create root GameObject
            var root = new GameObject("GymCameras");
            Undo.RegisterCreatedObjectUndo(root, "Build Reference Cameras");

            // 2. add gym_generator and populate database via Reset
            var generator = root.AddComponent<gym_generator>();
            // Reset() is called automatically by AddComponent, but force it
            // in case it wasn't (editor quirk with Undo)
            generator.SendMessage("Reset", SendMessageOptions.DontRequireReceiver);

            if (generator.database.Count == 0)
            {
                Debug.LogError("gym_generator database is empty after Reset. Aborting.");
                Undo.DestroyObjectImmediate(root);
                return;
            }

            // 3. generate cameras
            generator.generate();

            // 4. auto-assign textures
            int assigned = 0;
            foreach (Transform child in root.transform)
            {
                var overlay = child.GetComponent<gym_overlay>();
                if (overlay == null) continue;

                // extract image filename from camera name: "ref_cam_image_001.jpg" → "image_001.jpg"
                string cam_name = child.name;
                string image_name = cam_name.StartsWith("ref_cam_")
                    ? cam_name.Substring("ref_cam_".Length)
                    : cam_name;

                // try to find the texture in the photos folder
                Texture2D tex = FindTexture(image_name);
                if (tex != null)
                {
                    overlay.background_texture = tex;
                    assigned++;
                }
            }

            int total = root.transform.childCount;
            Debug.Log(
                $"GymCameraBuilder: {total} cameras created, " +
                $"{assigned}/{total} textures assigned from {PhotosAssetFolder}"
            );

            if (assigned < total)
            {
                Debug.LogWarning(
                    $"{total - assigned} cameras have no texture. " +
                    $"Import missing photos to {PhotosAssetFolder} and re-run."
                );
            }

            EditorUtility.SetDirty(root);
        }

        private static Texture2D FindTexture(string filename)
        {
            // strip extension for search since Unity may import as .jpg, .png, etc.
            string stem = Path.GetFileNameWithoutExtension(filename);

            // search for assets matching this stem in the photos folder
            string[] guids = AssetDatabase.FindAssets(stem, new[] { PhotosAssetFolder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string found_stem = Path.GetFileNameWithoutExtension(path);

                // exact stem match to avoid image_01 matching image_010
                if (found_stem != stem) continue;

                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null) return tex;
            }

            return null;
        }

        private static void CreateFolderRecursive(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
