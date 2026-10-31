// Claude-owned integration (owner report 2026-10-31; E-18).
using System;
using System.Collections.Generic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// Ward signs drawn with depth testing (WorldTextDepth), so walls hide them. Seat B's labels and their text stay as built;
    /// only the material they render with changes at runtime.
    /// </summary>
    public static class WorldTextStep
    {
        public const string ShaderName = "PatientZero/World Text";

        [SceneBuildStep(BuildScene.Ward, 970)]
        public static void Build(Transform root)
        {
            var shader = Shader.Find(ShaderName);
            if (!shader) throw new InvalidOperationException("Shader " + ShaderName + " is missing (Integration/Shaders/WorldText.shader).");
            var texts = new List<TextMesh>();
            foreach (GameObject sceneRoot in root.gameObject.scene.GetRootGameObjects())
                texts.AddRange(sceneRoot.GetComponentsInChildren<TextMesh>(true));
            var depth = root.gameObject.AddComponent<WorldTextDepth>();
            depth.shader = shader;
            depth.texts = texts.ToArray();
        }
    }
}
