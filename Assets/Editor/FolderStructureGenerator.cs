using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniShooterGame.Editor
{
    /// <summary>
    /// Editor utility for generating and maintaining the standard project folder architecture.
    /// Accessible via menu: Tools > Architecture > Generate Folder Structure
    /// </summary>
    public static class FolderStructureGenerator
    {
        private const string RootFolder = "Assets/_Game";

        private static readonly string[] FolderStructure = new[]
        {
            "Animations",
            "Audio/BGM",
            "Audio/SFX",
            "Prefabs/Characters",
            "Prefabs/Enemies",
            "Prefabs/Combat",
            "Prefabs/UI",
            "Scenes",
            "Scripts/Combat",
            "Scripts/Core",
            "Scripts/Enemy",
            "Scripts/Player",
            "Scripts/UI",
            "Sprites/Environment",
            "Sprites/Characters",
            "Sprites/UI"
        };

        [MenuItem("Tools/Architecture/Generate Folder Structure", priority = 100)]
        public static void GenerateFolders()
        {
            int createdFolders = 0;
            int gitkeepsAdded = 0;

            string projectPath = Application.dataPath; // Points to <ProjectRoot>/Assets

            foreach (string relativePath in FolderStructure)
            {
                string fullFolderPath = Path.Combine(projectPath, "_Game", relativePath);

                if (!Directory.Exists(fullFolderPath))
                {
                    Directory.CreateDirectory(fullFolderPath);
                    createdFolders++;
                }

                // Place .gitkeep if the folder is empty or contains no non-meta files
                string gitkeepPath = Path.Combine(fullFolderPath, ".gitkeep");
                string[] files = Directory.GetFiles(fullFolderPath);

                if (files.Length == 0)
                {
                    File.WriteAllText(gitkeepPath, string.Empty);
                    gitkeepsAdded++;
                }
            }

            AssetDatabase.Refresh();

            Debug.Log($"[FolderStructureGenerator] Completed folder setup under '{RootFolder}'. Created {createdFolders} new folder(s) and placed {gitkeepsAdded} .gitkeep file(s).");
            EditorUtility.DisplayDialog(
                "Folder Structure Generator",
                $"Project folder structure setup complete!\n\n• Folders created: {createdFolders}\n• .gitkeep files placed: {gitkeepsAdded}",
                "OK"
            );
        }
    }
}
