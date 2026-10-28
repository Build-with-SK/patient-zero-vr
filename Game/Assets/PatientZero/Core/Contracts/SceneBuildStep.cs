using System;

namespace PatientZero.Core
{
    public enum BuildScene
    {
        /// <summary>Loaded once, never reloaded: XR rig, GameFlow, settings, audio cue player, fader, UI.</summary>
        Bootstrap = 0,
        /// <summary>Loaded additively and reloaded on every restart: geometry, lights, puzzles, zombies, director.</summary>
        Ward = 1,
    }

    /// <summary>
    /// Marks an editor method that adds one team's content to a generated scene.
    /// Scenes are never edited by hand: the Integration scene composer (Claude) creates each
    /// scene empty and calls every step in Order, so two teams never touch the same .unity file.
    ///
    /// Signature: public static void Build(UnityEngine.Transform root)
    ///   root is a GameObject named after the step, created by the composer in the target scene.
    ///   Place content with WardLayout ids, never literal coordinates.
    ///
    /// Reserved order bands:
    ///   100-199 Environment (B): greybox geometry, colliders, NavMesh bake
    ///   200-299 Atmosphere (B): lights, emissives, fog, director, beat markers
    ///   300-399 Interaction (A): planks, valve, cable, socket, lever, teleport anchors
    ///   400-499 Player, Flow, UI (A): rig, GameFlow, settings, fader, menus   (Bootstrap)
    ///   500-599 AI (B): zombies and patrol routes
    ///   600-699 Audio (B): cue player (Bootstrap), static emitters (Ward)
    ///   900-999 Integration (Claude): validation only
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SceneBuildStepAttribute : Attribute
    {
        public SceneBuildStepAttribute(BuildScene scene, int order)
        {
            Scene = scene;
            Order = order;
        }

        public BuildScene Scene { get; }
        public int Order { get; }
    }
}
