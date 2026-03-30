using UnityEngine;
using System.Collections.Generic;

namespace Project.Tools
{
    public class gym_generator : MonoBehaviour
    {
        [System.Serializable]
        public struct photo_entry
        {
            public string file_name;
            public float fov;
            public float y_pos;
            public float pitch_x;
            public float ratio;
            public float focal;
        }

        public List<photo_entry> database = new List<photo_entry>();

        void Reset()
        {
            database.Clear();

            // batch 001 - 018
            add("image_001.jpg", 60, 1.65f, 0, 1.33f, 28);
            add("image_005.jpg", 85, 0.80f, 15, 1.33f, 18);
            add("image_006.jpg", 75, 1.20f, 5, 1.33f, 22);
            add("image_008.jpg", 95, 0.50f, 25, 1.33f, 14);
            add("image_009.jpg", 85, 1.50f, 0, 1.33f, 18);
            add("image_010.jpg", 85, 1.20f, 10, 1.33f, 18);
            add("image_011.jpg", 80, 1.40f, 5, 1.33f, 20);
            add("image_013.jpg", 85, 1.50f, 0, 1.33f, 18);
            add("image_014.jpg", 75, 1.60f, 0, 1.33f, 22);
            add("image_015.jpg", 80, 1.40f, 5, 1.33f, 20);
            add("image_016.jpg", 85, 1.50f, 5, 1.33f, 18);
            add("image_017.jpg", 80, 1.60f, 0, 1.33f, 20);
            add("image_018.jpg", 85, 1.50f, 0, 1.33f, 18);

            // batch 020 - 038
            add("image_020.jpg", 95, 0.60f, 25, 1.33f, 14);
            add("image_021.jpg", 70, 1.60f, 0, 1.33f, 24);
            add("image_022.jpg", 90, 1.00f, 10, 1.33f, 16);
            add("image_023.jpg", 70, 1.60f, 5, 1.33f, 24);
            add("image_024.jpg", 75, 1.60f, 5, 1.33f, 22);
            add("image_025.jpg", 90, 0.80f, 20, 1.33f, 16);
            add("image_028.jpg", 65, 1.65f, 0, 1.33f, 26);
            add("image_029.jpg", 85, 1.50f, 0, 1.33f, 18);
            add("image_030.jpg", 90, 0.80f, 15, 1.33f, 16);
            add("image_034.jpg", 100, 0.50f, 30, 1.33f, 12);
            add("image_035.jpg", 75, 1.50f, 5, 1.33f, 22);
            add("image_036.jpg", 80, 1.20f, 10, 1.33f, 20);
            add("image_037.jpg", 85, 1.40f, 5, 1.33f, 18);
            add("image_038.jpg", 90, 1.20f, 45, 1.33f, 16);

            // batch 041 - 060
            add("image_041.jpg", 80, 1.60f, 0, 1.33f, 20);
            add("image_042.jpg", 80, 1.60f, 0, 1.33f, 20);
            add("image_043.webp", 90, 1.60f, -5, 1.77f, 16);
            add("image_046.jpg", 85, 1.10f, 15, 1.33f, 18);
            add("image_047.jpg", 85, 1.30f, 10, 1.33f, 18);
            add("image_048.jpg", 90, 0.60f, 20, 1.33f, 16);
            add("image_049.jpg", 90, 1.20f, 5, 1.33f, 16);
            add("image_050.jpg", 100, 0.40f, 35, 1.33f, 12);
            add("image_051.jpg", 85, 1.20f, 10, 1.33f, 18);
            add("image_052.jpg", 80, 1.50f, 0, 1.33f, 20);
            add("image_053.jpg", 80, 3.50f, -25, 1.33f, 20);
            add("image_054.jpg", 85, 1.40f, 0, 1.33f, 18);
            add("image_055.jpg", 80, 1.60f, 0, 1.33f, 20);
            add("image_056.jpg", 70, 1.60f, 0, 1.33f, 24);
            add("image_060.jpg", 90, 3.50f, -30, 1.33f, 16);

            // batch 061 - 087
            add("image_061.jpg", 80, 1.50f, 5, 1.33f, 20);
            add("image_062.jpg", 85, 3.80f, -20, 1.33f, 18);
            add("image_064.jpg", 75, 1.50f, 0, 1.33f, 22);
            add("image_065.jpg", 80, 1.60f, 0, 1.33f, 20);
            add("image_067.jpg", 85, 1.50f, 0, 1.33f, 18);
            add("image_069.webp", 85, 1.60f, 0, 1.77f, 18);
            add("image_070.jpg", 90, 3.20f, -15, 1.33f, 16);
            add("image_071.jpg", 80, 1.50f, 5, 1.33f, 20);
            add("image_072.jpg", 75, 1.60f, 0, 1.33f, 22);
            add("image_073.jpg", 85, 1.40f, 10, 1.33f, 18);
            add("image_074.jpg", 80, 1.20f, 15, 1.33f, 20);
            add("image_087.jpg", 70, 1.60f, 0, 1.33f, 24);
        }

        void add(string f, float fov, float y, float p, float r, float mm)
        {
            database.Add(new photo_entry { file_name = f, fov = fov, y_pos = y, pitch_x = p, ratio = r, focal = mm });
        }

        [ContextMenu("generate cameras")]
        public void generate()
        {
            foreach (var data in database)
            {
                GameObject cam_go = new GameObject("ref_cam_" + data.file_name);
                cam_go.transform.SetParent(this.transform);

                Camera unity_cam = cam_go.AddComponent<Camera>();
                unity_cam.usePhysicalProperties = true;
                unity_cam.fieldOfView = data.fov;
                unity_cam.focalLength = data.focal;
                unity_cam.nearClipPlane = 0.01f;
                unity_cam.enabled = false;

                cam_go.transform.localPosition = new Vector3(0, data.y_pos, 0);
                cam_go.transform.localRotation = Quaternion.Euler(data.pitch_x, 0, 0);

                cam_go.AddComponent<gym_overlay>();
            }
            Debug.Log("generated " + database.Count + " reference cameras.");
        }
    }
}
