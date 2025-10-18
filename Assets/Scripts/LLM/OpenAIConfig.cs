using System.IO;
using System;
using UnityEngine;

[Serializable]
public class OpenAIConfig
{
    public string apiKey;
    public string model;

    public static OpenAIConfig LoadConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "openai_config.json");

        if (!File.Exists(path))
        {
            Debug.LogError($"Missing config file: {path}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            OpenAIConfig config = JsonUtility.FromJson<OpenAIConfig>(json);
            if (string.IsNullOrEmpty(config.apiKey))
                Debug.LogWarning("API key is empty in openai_config.json");
            return config;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load OpenAI config: {ex.Message}");
            return null;
        }
    }
}
