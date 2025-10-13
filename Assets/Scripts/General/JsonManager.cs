using System;
using UnityEngine;
using System.IO;

public class JsonManager : MonoBehaviour
{
    public static JsonManager Instance { get; private set; }

    [Header("Directory Settings")]
    public string location = "Assets/Spells";
    private string FolderPath => Path.Combine(Application.dataPath, location.Replace("Assets/", ""));


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
            name = "Ice Lance",
            damage = 1,
            accuracy = 90,
            resourceCost = 10,
            range = 5,
            selfDamage = 0,
            support = false,
            description = "An icy projectile with surprising accuracy."
        };
        CreateJsonFile(current);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        

        
        DontDestroyOnLoad(gameObject); // Optional: keeps it across scenes

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
