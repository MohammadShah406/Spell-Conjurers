using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;

public class LLMController : MonoBehaviour
{
    [Header("UI References")]
    public List<TMP_InputField> skillInputs;
    public string apiKey;
    public string model = "gpt-4o-mini";
    [Header("Events")]
    public UnityEvent onJsonGenerated;
    //private ICustomBehavior currentBehavior;
    //private Player player;
    //private string userPrompt = "";
    private bool isGenerating = false;
    public string promptFileName = "LLMTrainer";
    private string loadedPrompt;
    //private const string apiUrl = "https://api.openai.com/v1/chat/completions";

    private void Awake()
    {
        var config = OpenAIConfig.LoadConfig();
        if (config == null)
        {
            Debug.LogError("Could not load OpenAI configuration file.");
            return;
        }

        apiKey = config.apiKey;
        model = string.IsNullOrEmpty(config.model) ? "gpt-4o-mini" : config.model;
        TextAsset promptFile = Resources.Load<TextAsset>(promptFileName);
        if (promptFile != null)
        {
            loadedPrompt = promptFile.text;
        }
        else
        {
            Debug.LogError("Prompt file not found in Resources!");
            loadedPrompt = "";
        }

        Debug.Log("OpenAI configuration loaded successfully.");
    }
    public void OnGenerateSkillsButton()
    {
        if (isGenerating)
        {
            Debug.LogWarning("Skill generation already in progress...");
            return;
        }
        Debug.Log("generating skill from LLM");
        StartCoroutine(GenerateAllSkills());
    }

    private IEnumerator GenerateAllSkills()
    {
        isGenerating = true;

        // Make sure the Skills folder exists
        string skillsPath = Path.Combine(Application.dataPath, "Spells");
        if (!Directory.Exists(skillsPath))
            Directory.CreateDirectory(skillsPath);

        for (int i = 0; i < skillInputs.Count; i++)
        {
            string prompt = skillInputs[i].text.Trim();

            if (string.IsNullOrEmpty(prompt))
            {
                Debug.LogWarning($"Skill input {i + 1} is empty — skipping.");
                continue;
            }

            yield return StartCoroutine(GenerateSkill(prompt, i + 1));
        }

        Debug.Log("All skill JSON files generated!");
        isGenerating = false;
        onJsonGenerated?.Invoke();
    }
    private IEnumerator GenerateSkill(string prompt, int skillIndex)
    {
        Debug.Log($"Generating skill {skillIndex}: {prompt}");

        // Run the async skill generation as a task
        Task<string> skillTask = GenerateSkillFromLLM(prompt);
        yield return new WaitUntil(() => skillTask.IsCompleted);

        if (skillTask.Result == null)
        {
            Debug.LogError($"Skill {skillIndex} generation failed.");
            yield break;
        }

        string skillJson = ExtractJsonFromResponse(skillTask.Result);
        if (string.IsNullOrEmpty(skillJson))
        {
            Debug.LogError($"Skill {skillIndex} JSON extraction failed.");
            yield break;
        }

        // Save the JSON file
        Spell spell = new Spell();
        string fileName = $"{prompt.Replace(" ", "_")}.json";
        string savePath = Path.Combine(Application.dataPath, "Spells", fileName);

        try
        {
            spell = JsonConvert.DeserializeObject<Spell>(skillJson);
            if (spell == null || string.IsNullOrWhiteSpace(spell.name))
            {
                Debug.LogError($"Skill {skillIndex} JSON was invalid or missing skillName.");
                yield break;
            }
            fileName = $"{spell.name.Replace(" ", "_")}.json";
            savePath = Path.Combine(Application.dataPath, "Spells", fileName);
            File.WriteAllText(savePath, skillJson);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"Skill {skillIndex} saved to {savePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save skill {skillIndex}: {ex.Message}");
        }
    }
    private async Task<string> GenerateSkillFromLLM(string prompt)
    {
        string endpoint = "https://api.openai.com/v1/chat/completions";

        // Define what we want the model to output
        string skillPrompt =
            $"Generate a JSON object representing a game skill with the following fields: " +
            $"The skill concept is: {prompt}. " +
            $"Output only valid JSON without code blocks or explanations.";
        skillPrompt += loadedPrompt;
        // Build the OpenAI chat request
        ChatRequest requestData = new ChatRequest
        {
            model = model,
            messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                role = "system",
                content = "You are a helpful assistant that outputs only clean JSON data for Unity games. " +
                          "Never include code fences, markdown, or explanations — just valid JSON."+
                          loadedPrompt
            },
            new ChatMessage
            {
                role = "user",
                content = skillPrompt
            }
        },
            temperature = 0.7f
        };

        // Serialize the request body
        string jsonBody = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Request failed: {request.error}\n{request.downloadHandler.text}");
                return null;
            }

            try
            {
                // Deserialize the response
                OpenAIChatResponse response = JsonConvert.DeserializeObject<OpenAIChatResponse>(request.downloadHandler.text);

                // Extract the JSON content (the actual skill)
                string jsonContent = response?.choices?[0]?.message?.content?.Trim();

                // Optional: remove markdown code fences if model includes them accidentally
                if (!string.IsNullOrEmpty(jsonContent))
                {
                    jsonContent = Regex.Replace(jsonContent, @"^```(json)?|```$", "", RegexOptions.Multiline).Trim();
                }

                return jsonContent;
            }
            catch (Exception e)
            {
                Debug.LogError($"JSON parse error: {e.Message}\nResponse: {request.downloadHandler.text}");
                return null;
            }
        }
    }
    private string ExtractJsonFromResponse(string response)
    {
        // Simple JSON extraction from the LLM reply
        int start = response.IndexOf('{');
        int end = response.LastIndexOf('}');
        if (start >= 0 && end > start)
            return response.Substring(start, end - start + 1);
        return null;
    }
}
