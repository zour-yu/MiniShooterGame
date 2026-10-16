using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiniShooterGame.Editor
{
    public static class CleanMapProps
    {
        [MenuItem("Tools/Map/Remove Extra Auto-Generated Props", priority = 20)]
        public static void RemoveExtraProps()
        {
            GameObject stageRoot = GameObject.Find("[Arena_Stage]");
            if (stageRoot == null)
            {
                Debug.Log("[CleanMapProps] No [Arena_Stage] found in scene.");
                return;
            }

            int removedCount = 0;

            // Remove [Obstacles] folder if present
            Transform obstacles = stageRoot.transform.Find("[Obstacles]");
            if (obstacles != null)
            {
                Undo.DestroyObjectImmediate(obstacles.gameObject);
                removedCount++;
                Debug.Log("[CleanMapProps] Removed auto-generated [Obstacles] props.");
            }

            // Remove [Ground_Decals] folder if present
            Transform decals = stageRoot.transform.Find("[Ground_Decals]");
            if (decals != null)
            {
                Undo.DestroyObjectImmediate(decals.gameObject);
                removedCount++;
                Debug.Log("[CleanMapProps] Removed auto-generated [Ground_Decals].");
            }

            // Also check for any direct children like Building_Survivor_Lodge, Tree_*, Sandbag_*, Boulder_*, Pillar_*
            for (int i = stageRoot.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = stageRoot.transform.GetChild(i);
                string n = child.name;
                if (n.StartsWith("Building_") || n.StartsWith("Tree_") || n.StartsWith("Sandbag_") ||
                    n.StartsWith("Boulder_") || n.StartsWith("Pillar_") || n.StartsWith("Palm_") ||
                    n.StartsWith("Fence_") || n.StartsWith("Cape_") || n.StartsWith("Chest_"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    removedCount++;
                }
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log($"[CleanMapProps] Cleanup complete. Removed {removedCount} extra map prop objects. Restored clean integrated map!");
            EditorUtility.DisplayDialog(
                "Map Cleaned",
                $"Removed {removedCount} auto-generated map prop objects.\nYour clean integrated map has been restored!",
                "OK"
            );
        }
    }
}
