using System;
using UnityEngine;
using System.IO;

public class JsonManager : MonoBehaviour
{
    public static JsonManager Instance { get; private set; }

    [Header("Directory Settings")]
    public string location = "Assets/Spells";
    public string playerLocation = "Assets/PlayerPool";
    private string FolderPath => Path.Combine(Application.dataPath, location.Replace("Assets/", ""));
    private string playerFolderPath => Path.Combine(Application.dataPath, location.Replace("Assets/", ""));


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

        //Making New spell
        current = new Spell
        {
            damage = 1,
            accuracy = 90,
            resourceCost = 10,
            range = 10,
            selfDamage = 0,
            support = false,
            name = "Ice Lance",
            description = "Hurls an ice lance at surprising accuracy",
            code = "string actionLog = \"\";\r\n        \r\n            to.GetComponent<Stats>().takeDamage(spell.damage);\r\n            actionLog += from.name + \" hurled a Fireball at \" + to.name + \", dealing \" + spell.damage + \" damage. \";\r\n            to.GetComponent<Stats>().StatusDamage(spell.status, spell.statusDuration, spell.statusDamagePerTurn);\r\n        \r\n        ",
            spellVisualType = "Sphere",
            status = "None",
            statusDuration = 0,
            statusDamagePerTurn = 0

        };
        CreateJsonFile(current);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        

        
        DontDestroyOnLoad(gameObject); // Keep it across scenes

        // Ensure the folder exists inside project
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
            Debug.Log($"Created folder at {FolderPath}");
        }
        

        

        //Reading random spell
        //current = ReturnRandomJson();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Spell ReturnRandomSpell()
    {
        string[] files = Directory.GetFiles(FolderPath, "*.json");

        if (files.Length == 0)
        {
            Debug.LogWarning("No spell files found in " + FolderPath);
            return null;
        }

        // Pick a random file
        string randomFile = files[UnityEngine.Random.Range(0, files.Length)];
        string json = File.ReadAllText(randomFile);
        Spell spell = JsonUtility.FromJson<Spell>(json);

        Debug.Log($"Loaded Spell: {spell.name}");
        return spell;
    }

    public Spell ReturnPlayerSpell(int index)
    {
        string[] files = Directory.GetFiles(playerFolderPath, "*.json");

        if (files.Length == 0)
        {
            Debug.LogWarning("No spell files found in " + playerFolderPath);
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
        return spell;
    }

    public void CreateJsonFile(Spell spell)
    {
        if (string.IsNullOrEmpty(spell.name))
        {
            Debug.LogError("Spell name cannot be empty!");
            return;
        }

        string filePath = Path.Combine(FolderPath, spell.name + ".json");
        string json = JsonUtility.ToJson(spell, true);
        File.WriteAllText(filePath, json);

        Debug.Log($"Saved spell '{spell.name}' to {filePath}");
    }
}
