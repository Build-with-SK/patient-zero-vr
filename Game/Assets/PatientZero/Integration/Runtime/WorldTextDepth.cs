using UnityEngine;

namespace PatientZero.Integration
{
    /// <summary>
    /// Integration E-18: gives the ward's signs (TextMesh) a depth-tested material, so a wall hides the sign behind it. The
    /// font atlas is dynamic, so the material follows the font's texture whenever Unity rebuilds it.
    /// </summary>
    public sealed class WorldTextDepth : MonoBehaviour
    {
        public Shader shader;
        public TextMesh[] texts;
        public Color tint = new Color(.82f, .84f, .8f, 1);
        private Material material;
        private Font font;

        private void Awake()
        {
            if (!shader || !shader.isSupported || texts == null || texts.Length == 0) { enabled = false; return; }
            material = new Material(shader) { name = "World text (depth tested)" };
            material.SetColor("_Color", tint);
            foreach (TextMesh text in texts)
            {
                if (!text) continue;
                if (!font) font = text.font;
                var meshRenderer = text.GetComponent<MeshRenderer>();
                if (meshRenderer) meshRenderer.sharedMaterial = material;
            }

            Refresh(font);
        }

        private void OnEnable() => Font.textureRebuilt += Refresh;
        private void OnDisable() => Font.textureRebuilt -= Refresh;

        private void Refresh(Font changed)
        {
            if (material && font && changed == font) material.mainTexture = font.material.mainTexture;
        }

        private void OnDestroy()
        {
            if (material) Destroy(material);
        }
    }
}
