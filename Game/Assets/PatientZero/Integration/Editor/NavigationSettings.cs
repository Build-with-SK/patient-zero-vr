// UNVERIFIED IN EDITOR: written 2026-10-28 before a Unity 6.3 Editor existed on any seat. First compiled at integration.
using PatientZero.Integration.Logic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// Project-wide NavMesh agent settings (Claude's area: ProjectSettings). Environment's NavMeshSurface bakes for
    /// agent type 0, so the radius set here is the one the ward is baked with (INT-B-1).
    /// </summary>
    public static class NavigationSettings
    {
        public const string AssetPath = "ProjectSettings/NavMeshAreas.asset";

        public static bool EnsureZombieAgentRadius(EditorReport report)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetPath);
            if (assets == null || assets.Length == 0 || assets[0] == null)
            {
                report.Error("navmesh: " + AssetPath + " could not be loaded.");
                return false;
            }

            var settings = new SerializedObject(assets[0]);
            SerializedProperty list = settings.FindProperty("m_Settings");
            if (list == null || !list.isArray)
            {
                report.Error("navmesh: " + AssetPath + " has no m_Settings array; the serialized layout changed.");
                return false;
            }

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty agent = list.GetArrayElementAtIndex(i);
                SerializedProperty id = agent.FindPropertyRelative("agentTypeID");
                if (id == null || id.intValue != QuestBuildConfig.ZombieAgentTypeId) continue;

                SerializedProperty radius = agent.FindPropertyRelative("agentRadius");
                if (radius == null)
                {
                    report.Error("navmesh: agent type " + QuestBuildConfig.ZombieAgentTypeId + " has no agentRadius field.");
                    return false;
                }

                float before = radius.floatValue;
                if (!Mathf.Approximately(before, QuestBuildConfig.ZombieAgentRadius))
                {
                    radius.floatValue = QuestBuildConfig.ZombieAgentRadius;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.SaveAssets();
                }

                // Read it back through the runtime API the bake uses, so a write that did not stick is caught here.
                float now = NavMesh.GetSettingsByID(QuestBuildConfig.ZombieAgentTypeId).agentRadius;
                if (!Mathf.Approximately(now, QuestBuildConfig.ZombieAgentRadius))
                {
                    report.Error("navmesh: agent type " + QuestBuildConfig.ZombieAgentTypeId + " radius reads " + now + " m after setting " +
                                 QuestBuildConfig.ZombieAgentRadius + " m.");
                    return false;
                }

                report.Pass("navmesh: agent type " + QuestBuildConfig.ZombieAgentTypeId + " radius " + before + " -> " + now + " m (INT-B-1)");
                return true;
            }

            report.Error("navmesh: no agent type " + QuestBuildConfig.ZombieAgentTypeId + " in " + AssetPath + ".");
            return false;
        }
    }
}
