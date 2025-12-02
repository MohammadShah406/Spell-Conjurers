using System;
using UnityEngine;
using System.IO;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Reflection;

public class JsonManager : MonoBehaviour
{
    public static JsonManager Instance { get; private set; }

    private string spellsFolderPath => Path.Combine(Application.persistentDataPath, "Spells");
    private string playerSpellsFolderPath => Path.Combine(Application.persistentDataPath, "PlayerSpells");

    private string premadeSpellsFolderPath => Path.Combine(Application.streamingAssetsPath, "SpellPool");

    private string roundFilesFolderPath => Path.Combine(Application.persistentDataPath, "Rounds");


    // Dictionary to store precompiled spell runners
    public Dictionary<Spell, ScriptRunner<object>> compiledSpells = new Dictionary<Spell, ScriptRunner<object>>();

    [Header("Latest Spell")]
    public Spell current;

    private void Awake()
    {
        Instance = this;
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Ensure folders exist
        if (!Directory.Exists(spellsFolderPath))
            Directory.CreateDirectory(spellsFolderPath);

        if (!Directory.Exists(playerSpellsFolderPath))
            Directory.CreateDirectory(playerSpellsFolderPath);

        if (!Directory.Exists(roundFilesFolderPath))
            Directory.CreateDirectory(roundFilesFolderPath);

        PrecompileAllSpells();
    }

    private void PrecompileAllSpells()
    {
        string[] files = Directory.GetFiles(spellsFolderPath, "*.json");
        foreach (string file in files)
        {
            string json = File.ReadAllText(file);
            Spell spell = JsonUtility.FromJson<Spell>(json);
            PrecompileSpell(spell);
        }

        files = Directory.GetFiles(playerSpellsFolderPath, "*.json");
        foreach (string file in files)
        {
            string json = File.ReadAllText(file);
            Spell spell = JsonUtility.FromJson<Spell>(json);
            PrecompileSpell(spell);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ClearJsonInFolder();
        DontDestroyOnLoad(gameObject); // Keep it across scenes

        // Ensure the folder exists inside project
        if (!Directory.Exists(spellsFolderPath))
        {
            Directory.CreateDirectory(spellsFolderPath);
            Debug.Log($"Created folder at {spellsFolderPath}");
        }
        if (!Directory.Exists(playerSpellsFolderPath))
        {
            Directory.CreateDirectory(playerSpellsFolderPath);
            Debug.Log($"Created folder at {playerSpellsFolderPath}");
        }
        if (!Directory.Exists(premadeSpellsFolderPath))
        {
            Directory.CreateDirectory(premadeSpellsFolderPath);
            Debug.Log($"Created folder at {premadeSpellsFolderPath}");
        }

        if (Directory.Exists(premadeSpellsFolderPath))
        {
            CopySpellsFromFolders(Path.Combine(Application.streamingAssetsPath, "SpellPool"), Path.Combine(Application.persistentDataPath,"Spells"), overwrite: true);
        }
    }

    public Spell ReturnRandomSpell()
    {
        string[] files = Directory.GetFiles(spellsFolderPath, "*.json");

        if (files.Length == 0)
        {
            Debug.LogWarning("No spell files found in " + spellsFolderPath);
            return null;
        }

        // Pick a random file
        string randomFile = files[UnityEngine.Random.Range(0, files.Length)];
        string json = File.ReadAllText(randomFile);
        Spell spell = JsonUtility.FromJson<Spell>(json);

        Debug.Log($"Loaded Spell: {spell.name}");
        PrecompileSpell(spell);
        return spell;
    }

    public Spell ReturnPlayerSpell(int index)
    {
        string[] files = Directory.GetFiles(playerSpellsFolderPath, "*.json");

        if (files.Length == 0)
        {
            Debug.LogWarning("No spell files found in " + playerSpellsFolderPath);
            return null;
        }

        if (index < 0 || index >= files.Length)
        {
            Debug.LogWarning($"Invalid spell index {index}. Only {files.Length} spell files found.");
            return null;
        }

        string json = File.ReadAllText(files[index]);
        Spell spell = JsonUtility.FromJson<Spell>(json);

        Debug.Log($"Loaded Spell: {spell.name}");
        PrecompileSpell(spell);
        return spell;
    }
    public void OnApplicationQuit()
    {
        ClearJsonInFolder();
    }
    public void CreateJsonFile(Spell spell)
    {
        if (string.IsNullOrEmpty(spell.name))
        {
            Debug.LogError("Spell name cannot be empty!");
            return;
        }

        string filePath = Path.Combine(spellsFolderPath, spell.name + ".json");
        string json = JsonUtility.ToJson(spell, true);
        File.WriteAllText(filePath, json);

        Debug.Log($"Saved spell '{spell.name}' to {filePath}");
    }
    public void ClearJsonInFolder()
    {
        try
        {
            string[] files = Directory.GetFiles(playerSpellsFolderPath, "*.json");
            foreach (string file in files)
            {
                File.Delete(file);
                Debug.Log($"Deleted spell file: {file}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error clearing spell files: {e.Message}");
        }
    }

    private void PrecompileSpell(Spell spell)
    {
        SpellFunction.Instance.PrecompileSpell(spell);
    }

    // Moves all files from a source folder to a destination folder.
    // If overwrite is true existing files in the destination will be replaced.
    public void MoveSpellsFromFolders(string sourceDir, string targetDir, bool overwrite = true)
    {

        try
        {
            // Determine source directory (configured or fallback to Assets/PlayerSpells)


            if (!Directory.Exists(sourceDir))
            {
                string fallback = Path.Combine(Application.dataPath, "PlayerSpells");
                if (Directory.Exists(fallback))
                {
                    sourceDir = fallback;
                    Debug.Log($"Player spells folder not found at configured path; using fallback: {fallback}");
                }
                else
                {
                    Debug.LogWarning($"No player spells folder found at '{playerSpellsFolderPath}' or fallback '{fallback}'. Nothing to move.");
                    return;
                }
            }

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string[] files = Directory.GetFiles(sourceDir, "*.json");
            if (files.Length == 0)
            {
                Debug.Log("No player spell JSON files to move.");
                return;
            }

            foreach (string srcFile in files)
            {
                string fileName = Path.GetFileName(srcFile);
                string destFile = Path.Combine(targetDir, fileName);

                if (File.Exists(destFile))
                {
                    if (overwrite)
                    {
                        File.Delete(destFile);
                    }
                    else
                    {
                        Debug.Log($"Skipping move for '{fileName}' — destination already exists.");
                        continue;
                    }
                }

                File.Move(srcFile, destFile);
                Debug.Log($"Moved player spell '{fileName}' to main spells folder.");

                // Precompile the newly moved spell
                try
                {
                    string json = File.ReadAllText(destFile);
                    Spell spell = JsonUtility.FromJson<Spell>(json);
                    if (spell != null)
                        PrecompileSpell(spell);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to precompile moved spell '{fileName}': {ex.Message}");
                }
            }

#if UNITY_EDITOR
            // Refresh AssetDatabase so Editor sees moved files
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Error moving player spells: {e.Message}");
        }
    }

    // Copy all files from a source folder to a destination folder.
    // If overwrite is true existing files in the destination will be replaced.
    public void CopySpellsFromFolders(string sourceDir, string targetDir, bool overwrite = true)
    {

        try
        {
            // Determine source directory (configured or fallback to Assets/PlayerSpells)


            if (!Directory.Exists(sourceDir))
            {
                string fallback = Path.Combine(Application.dataPath, "PlayerSpells");
                if (Directory.Exists(fallback))
                {
                    sourceDir = fallback;
                    Debug.Log($"Player spells folder not found at configured path; using fallback: {fallback}");
                }
                else
                {
                    Debug.LogWarning($"No player spells folder found at '{playerSpellsFolderPath}' or fallback '{fallback}'. Nothing to move.");
                    return;
                }
            }

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string[] files = Directory.GetFiles(sourceDir, "*.json");
            if (files.Length == 0)
            {
                Debug.Log("No player spell JSON files to move.");
                return;
            }

            foreach (string srcFile in files)
            {
                string fileName = Path.GetFileName(srcFile);
                string destFile = Path.Combine(targetDir, fileName);

                if (File.Exists(destFile))
                {
                    if (overwrite)
                    {
                        File.Delete(destFile);
                    }
                    else
                    {
                        Debug.Log($"Skipping move for '{fileName}' — destination already exists.");
                        continue;
                    }
                }

                File.Copy(srcFile, destFile);
                Debug.Log($"Moved player spell '{fileName}' to main spells folder.");

                // Precompile the newly moved spell
                try
                {
                    string json = File.ReadAllText(destFile);
                    Spell spell = JsonUtility.FromJson<Spell>(json);
                    if (spell != null)
                        PrecompileSpell(spell);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to precompile moved spell '{fileName}': {ex.Message}");
                }
            }

#if UNITY_EDITOR
            // Refresh AssetDatabase so Editor sees moved files
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Error moving player spells: {e.Message}");
        }
    }

    public bool CanGenerate(int roundsNo)
    {
        int roundsTotal = CheckRoundFiles();
        if (roundsNo >= roundsTotal - 2)
            return true;
        else
            return false;
    }

    public int CheckRoundFiles()
    {
        try
        {
            if (!Directory.Exists(roundFilesFolderPath))
            {
                // If the folder doesn't exist, there are no files.
                return 0;
            }

            string[] files = Directory.GetFiles(roundFilesFolderPath, "*.json");
            return files.Length;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error checking round files: {e.Message}");
            return 0;
        }
    }
    
}
