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
    private bool isGenerating = false;
    public string promptFileName = "LLMTrainer";
    private string loadedPrompt;

    public List<GeneratedSkillData> generatedSkills = new List<GeneratedSkillData>();

    public UnityEvent onSpellReplaced;

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
    private void Start()
    {
        ClearSkillsFolder();
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
        generatedSkills.Clear();
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

        // Deserialize the JSON
        Spell spell = JsonConvert.DeserializeObject<Spell>(skillJson);
        if (spell == null || string.IsNullOrWhiteSpace(spell.name))
        {
            Debug.LogError($"Skill {skillIndex} JSON was invalid or missing skillName.");
            yield break;
        }

        // Create the folder in persistentDataPath
        string spellFolder = Path.Combine(Application.persistentDataPath, "PlayerSpells");
        if (!Directory.Exists(spellFolder))
            Directory.CreateDirectory(spellFolder);

        // Clean filename
        string fileName = $"{spell.name.Replace(" ", "_")}.json";
        string filePath = Path.Combine(spellFolder, fileName);

        try
        {
            // Save the JSON file
            File.WriteAllText(filePath, skillJson);

            // Add to generated skills list
            generatedSkills.Add(new GeneratedSkillData
            {
                filePath = filePath,
                jsonContent = skillJson,
                spellData = spell
            });

            Debug.Log($"Skill {skillIndex} saved to runtime folder:\n{filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save skill {skillIndex}: {ex.Message}");
        }
    }
    public void ClearSkillsFolder()
    {
        generatedSkills.Clear();
        string spellFolder = Path.Combine(Application.persistentDataPath, "PlayerSpells");
        if (Directory.Exists(spellFolder))
        {
            try
            {
                Directory.Delete(spellFolder, true); // true = delete all files & subfolders
                Debug.Log($"Cleared PlayerSpells folder at: {spellFolder}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to clear PlayerSpells folder: {ex.Message}");
            }
        }

        // Recreate the folder so you can save files later
        Directory.CreateDirectory(spellFolder);
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
        string allScripts = LoadAllProjectScripts("Scripts");
        string spellsRef = LoadAllProjectScripts("SpellReference");
        Debug.Log("scripts are " + allScripts);
        Debug.Log("spellsRef are " + spellsRef);
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
                          allScripts +
                          "Never include code fences, markdown, or explanations — just valid JSON."+
                          $"Balance the spell with this values: {spellsRef}. " +
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

    private string LoadAllProjectScripts(string folderName)
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, folderName);

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"Folder not found: {folderPath}");
            return "";
        }

        StringBuilder allScripts = new StringBuilder();

        try
        {
            // Get all .txt files in the folder
            string[] files = Directory.GetFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                string content = File.ReadAllText(file);
                allScripts.AppendLine(content);
            }

            return allScripts.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to read scripts from {folderPath}: {ex.Message}");
            return "";
        }
    }


    public void ShowGeneratedSpell()
    {
        for(int i = 0; i < generatedSkills.Count; i++)
        {
            UIController.Instance.getGeneratedSpellView.SetGeneratedSpellInfo(i, generatedSkills[i].spellData);
            Debug.Log("Showing Skill");
        }
        
    }
    public IEnumerator GenerateAndReplaceSpell(string prompt, int spellIndex)
    {
        Debug.Log($"Regenerating spell at index {spellIndex} with prompt: {prompt}");

        // --- RUN LLM REQUEST ---
        Task<string> skillTask = GenerateSkillFromLLM(prompt);
        yield return new WaitUntil(() => skillTask.IsCompleted);

        if (skillTask.Result == null)
        {
            Debug.LogError($"Failed to regenerate spell {spellIndex}.");
            yield break;
        }

        string skillJson = ExtractJsonFromResponse(skillTask.Result);
        if (string.IsNullOrEmpty(skillJson))
        {
            Debug.LogError($"No valid JSON returned for spell {spellIndex}.");
            yield break;
        }

        try
        {
            // --- DESERIALIZE NEW SPELL ---
            Spell newSpell = JsonConvert.DeserializeObject<Spell>(skillJson);
            if (newSpell == null || string.IsNullOrWhiteSpace(newSpell.name))
            {
                Debug.LogError($"Invalid JSON when regenerating spell {spellIndex}.");
                yield break;
            }

            // --- CREATE RUNTIME FOLDER ---
            string spellFolder = Path.Combine(Application.persistentDataPath, "PlayerSpells");
            if (!Directory.Exists(spellFolder))
                Directory.CreateDirectory(spellFolder);

            // --- FILENAME ---
            string fileName = $"{newSpell.name.Replace(" ", "_")}.json";
            string filePath = Path.Combine(spellFolder, fileName);

            // --- DELETE OLD FILE IF NAME CHANGED ---
            if (spellIndex < generatedSkills.Count)
            {
                string oldPath = generatedSkills[spellIndex].filePath;
                if (File.Exists(oldPath) && !oldPath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(oldPath);
                    Debug.Log("Deleted old spell file: " + oldPath);
                }
            }

            // --- SAVE NEW SPELL ---
            File.WriteAllText(filePath, skillJson);

            // --- UPDATE GENERATED SPELL LIST ---
            GeneratedSkillData data = new GeneratedSkillData
            {
                filePath = filePath,
                jsonContent = skillJson,
                spellData = newSpell
            };

            if (spellIndex < generatedSkills.Count)
                generatedSkills[spellIndex] = data;
            else
                generatedSkills.Add(data);

            // --- UPDATE UI ---
            UIController.Instance.getGeneratedSpellView.SetGeneratedSpellInfo(spellIndex, newSpell);

            Debug.Log($"Spell {spellIndex} regenerated and saved at runtime:\n{filePath}");
            onSpellReplaced?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error regenerating spell {spellIndex}: {ex.Message}");
        }
    }


}
