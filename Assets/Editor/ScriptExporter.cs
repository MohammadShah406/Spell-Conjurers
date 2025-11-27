using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Text;

public class ScriptExporter : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        ExportAllScriptsToSingleFile();
    }

    private void ExportAllScriptsToSingleFile()
    {
        string sourcePath = Application.dataPath; // Assets/
        string targetFolder = Path.Combine(Application.streamingAssetsPath, "Scripts");

        if (!Directory.Exists(targetFolder))
            Directory.CreateDirectory(targetFolder);

        string combinedFilePath = Path.Combine(targetFolder, "AllScripts.txt");

        StringBuilder sb = new StringBuilder();

        // Find all .cs files in the project
        string[] files = Directory.GetFiles(sourcePath, "*.cs", SearchOption.AllDirectories);

        int exportedCount = 0;

        foreach (string file in files)
        {
            string relativePath = file.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar);

            // Skip Editor scripts (optional)
            if (relativePath.StartsWith("Editor"))
                continue;

            try
            {
                string code = File.ReadAllText(file);
                sb.AppendLine($"// FILE: {relativePath}");
                sb.AppendLine(code);
                sb.AppendLine("\n"); // Add spacing between files
                exportedCount++;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not read {file}: {ex.Message}");
            }
        }

        File.WriteAllText(combinedFilePath, sb.ToString());
        Debug.Log($"[ScriptExporter] Exported {exportedCount} .cs scripts into {combinedFilePath}");
    }
}
