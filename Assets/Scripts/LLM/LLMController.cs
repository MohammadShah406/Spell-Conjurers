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

        Spell spell;
        string fileName = $"{prompt.Replace(" ", "_")}.json";
        string savePath = Path.Combine(Application.dataPath, "Spells", fileName);

        try
        {
            spell = JsonConvert.DeserializeObject<Spell>(skillJson);
            if (spell == null || string.IsNullOrWhiteSpace(spell.name))
            {
                Debug.LogError($"Skill {skillIndex} JSON invalid or missing name.");
                yield break;
            }

            fileName = $"{spell.name.Replace(" ", "_")}.json";
            savePath = Path.Combine(Application.dataPath, "Spells", fileName);
            File.WriteAllText(savePath, skillJson);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            Debug.Log($"Skill {skillIndex} saved to {savePath}");

            // Compile and attach script
            if (!string.IsNullOrWhiteSpace(spell.script))
            {
                Debug.Log($"Compiling generated script for {spell.name}...");
                Type compiledType = RuntimeCompiler.CompileType(spell.script, spell.scriptName);

                if (compiledType != null)
                {
                    GameObject target = ResolveTarget(spell.target) ?? GameObject.FindWithTag("Player");

                    if (target != null)
                    {
                        if (target.GetComponent(compiledType) == null)
                        {
                            var component = target.AddComponent(compiledType);
                            Debug.Log($"Attached {compiledType.Name} to {target.name}");

                            // Auto-assign fields if they match available objects
                            AutoAssignFields(component);
                        }
                        else
                        {
                            Debug.Log($"{compiledType.Name} already exists on {target.name}, skipping duplicate.");
                        }
                    }
                    else
                    {
                        Debug.LogError("No valid target found for script attachment.");
                    }
                }
                else
                {
                    Debug.LogError("Script compilation failed — could not attach.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save or compile skill {skillIndex}: {ex.Message}");
        }
    }
    private void AutoAssignFields(Component component)
    {
        var fields = component.GetType()
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(GameObject))
            {
                string fieldName = field.Name;
                GameObject found = GameObject.Find(fieldName);

                if (found == null)
                    found = GameObject.FindWithTag(fieldName);

                if (found == null)
                    found = Resources.Load<GameObject>($"Prefabs/{fieldName}");

#if UNITY_EDITOR
                // Try loading from generated folder
                if (found == null)
                    found = Resources.Load<GameObject>($"GeneratedPrefabs/{fieldName}");
#endif

                // Auto-create if missing
                if (found == null)
                {
                    Debug.LogWarning($"No object or prefab found for '{fieldName}', creating one automatically.");
                    found = new GameObject(fieldName);

#if UNITY_EDITOR
                    string folderPath = "Assets/Resources/GeneratedPrefabs";
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string prefabPath = $"{folderPath}/{fieldName}.prefab";
                    UnityEditor.PrefabUtility.SaveAsPrefabAsset(found, prefabPath);
                    UnityEditor.AssetDatabase.Refresh();

                    Debug.Log($"Created new prefab: {prefabPath}");
#endif
                }

                if (found != null)
                {
                    field.SetValue(component, found);
                    Debug.Log($"Assigned GameObject '{fieldName}' to {component.GetType().Name}");
                }
            }
            else if (typeof(Component).IsAssignableFrom(field.FieldType))
            {
                var found = GameObject.FindFirstObjectByType(field.FieldType);
                if (found != null)
                {
                    field.SetValue(component, found);
                    Debug.Log($"Assigned Component '{field.FieldType.Name}' to {component.GetType().Name}");
                }
                else
                {
#if UNITY_EDITOR
                    // Create an empty GameObject with this component if not found
                    GameObject newObj = new GameObject(field.FieldType.Name);
                    var newComp = newObj.AddComponent(field.FieldType);
                    field.SetValue(component, newComp);
                    Debug.Log($"Created new component '{field.FieldType.Name}' for {component.GetType().Name}");
#endif
                }
            }
        }
    }
    private async Task<string> GenerateSkillFromLLM(string prompt)
    {
        string endpoint = "https://api.openai.com/v1/chat/completions";

        // Define what we want the model to output
        string skillPrompt =
            $"Generate a JSON object representing a game skill with the following fields: " +
            $"The skill concept is: {prompt}. " +
             "If this skill requires a new behavior script, output a JSON object in this exact format:\n" +
             "{\n" +
             "  \"target\": \"The GameObject that this script should be attached to (e.g., Player, Projectile, Enemy)\",\n" +
             "  \"scriptName\": \"The C# class name for the script\",\n" +
             "  \"script\": \"The full valid Unity C# script code\"\n" +
             "}\n" +
            $"Output only valid JSON without code blocks or explanations.";
        skillPrompt += loadedPrompt;
        string allScripts = LoadAllProjectScripts();
        Debug.Log("scripts are " + allScripts);
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

    private string LoadAllProjectScripts()
    {
        string scriptsPath = Path.Combine(Application.dataPath, "Scripts");
        if (!Directory.Exists(scriptsPath))
        {
            Debug.LogWarning($"Scripts folder not found: {scriptsPath}");
            return "";
        }

        StringBuilder sb = new StringBuilder();
        string[] files = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);

        foreach (string file in files)
        {
            try
            {
                string code = File.ReadAllText(file);
                sb.AppendLine($"// FILE: {Path.GetFileName(file)}");
                sb.AppendLine(code);
                sb.AppendLine("\n");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not read {file}: {ex.Message}");
            }
        }

    
        string allScripts = sb.ToString();
        if (allScripts.Length > 100000)//characters is 1000000 for token limit change it if we need to increase the char
        {
            allScripts = allScripts.Substring(0, 50000);
            Debug.LogWarning("Script context truncated to fit within token limits.");
        }

        return allScripts;
    }
    private GameObject ResolveTarget(string targetName)
    {
        if (string.IsNullOrEmpty(targetName))
            return null;

        // Try to find existing object by name
        GameObject obj = GameObject.Find(targetName);
        if (obj != null) return obj;

        // Try to find by tag (ignore if tag not found)
        try
        {
            obj = GameObject.FindWithTag(targetName);
            if (obj != null) return obj;
        }
        catch { }

        // Try Resources
        GameObject prefab = Resources.Load<GameObject>($"Prefabs/{targetName}");
        if (prefab == null)
            prefab = Resources.Load<GameObject>($"GeneratedPrefabs/{targetName}");
        if (prefab != null)
            return Instantiate(prefab);

#if UNITY_EDITOR
        // Auto-create prefab + tag
        return TagUtility.EnsurePrefabExists(targetName);
#else
    // Runtime fallback in builds
    GameObject go = new GameObject(targetName);
    go.name = targetName;
    return go;
#endif
    }
}
