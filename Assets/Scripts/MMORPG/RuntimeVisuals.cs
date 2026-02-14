using UnityEngine;

namespace MiniMMORPG
{
    public static class RuntimeVisuals
    {
        public static void ApplyColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            var material = new Material(FindSupportedShader());
            material.color = color;
            renderer.sharedMaterial = material;
        }

        private static Shader FindSupportedShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                   ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                   ?? Shader.Find("Standard")
                   ?? Shader.Find("Unlit/Color")
                   ?? Shader.Find("Sprites/Default");
        }
    }
}
