using UnityEngine;

namespace Project.Tools
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class gym_overlay : MonoBehaviour
    {
        public Texture2D background_texture;
        [Range(0, 1)] public float opacity = 0.5f;
        public bool is_visible = true;

        private Camera cam;

        void OnEnable()
        {
            cam = GetComponent<Camera>();
        }

        void OnGUI()
        {
            if (!is_visible || background_texture == null || cam == null) return;

            // only show overlay when this object is selected in the inspector
            #if UNITY_EDITOR
            if (UnityEditor.Selection.activeGameObject != gameObject) return;
            #endif

            GUI.color = new Color(1, 1, 1, opacity);

            float screen_ratio = (float)Screen.width / (float)Screen.height;
            float tex_ratio = (float)background_texture.width / (float)background_texture.height;

            Rect area;
            if (screen_ratio > tex_ratio)
            {
                float w = Screen.height * tex_ratio;
                area = new Rect((Screen.width - w) / 2, 0, w, Screen.height);
            }
            else
            {
                float h = Screen.width / tex_ratio;
                area = new Rect(0, (Screen.height - h) / 2, Screen.width, h);
            }

            GUI.DrawTexture(area, background_texture, ScaleMode.ScaleToFit);
        }
    }
}
