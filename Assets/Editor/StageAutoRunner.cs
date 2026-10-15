using UnityEditor;
using UnityEngine;

namespace MiniShooterGame.Editor
{
    [InitializeOnLoad]
    public static class StageAutoRunner
    {
        private const string RunFlagKey = "MiniShooterGame_IslandStageBuilt_v5";

        static StageAutoRunner()
        {
            EditorApplication.delayCall += Execute;
        }

        private static void Execute()
        {
            EditorApplication.delayCall -= Execute;

            // Only run if not already executed for this build version
            if (SessionState.GetBool(RunFlagKey, false)) return;
            SessionState.SetBool(RunFlagKey, true);

            try
            {
                Debug.Log("[StageAutoRunner] Automatically constructing Island Arena Stage...");
                ArenaSceneBuilder.BuildArenaStage(false);
                
                // Upgrade character visuals (Survivor rifleman & Zombies)
                CharacterVisualUpgrader.UpgradeVisuals();

                // Capture screenshot for AI review
                GameViewScreenshot.Capture(false);
                Debug.Log("[StageAutoRunner] Island Arena Stage constructed & screenshot captured successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[StageAutoRunner] Failed to auto-construct stage: {ex}");
            }
        }
    }
}
