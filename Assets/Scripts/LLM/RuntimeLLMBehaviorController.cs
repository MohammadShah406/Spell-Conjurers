using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class RuntimeLLMBehaviorController : MonoBehaviour
{
    public string apiKey;
    public string model = "gpt-4o-mini";

    private ICustomBehavior currentBehavior;
    private Player player;

    private string userPrompt = "";
    private bool isGenerating = false;

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


        Debug.Log("OpenAI configuration loaded successfully.");
    }
    private void Start()
    {
        player = GameManager.Instance.player;
    }

    private void Update()
    {
        currentBehavior?.Update(player);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 200, 25), "Enter Prompt:");
        userPrompt = GUI.TextField(new Rect(20, 50, 400, 25), userPrompt);

        if (GUI.Button(new Rect(430, 50, 80, 25), "Run") && !isGenerating)
        {
            if (!string.IsNullOrWhiteSpace(userPrompt))
                StartCoroutine(GenerateAndCompileBehavior(userPrompt));
        }
    }

    private IEnumerator GenerateAndCompileBehavior(string prompt)
    {
        isGenerating = true;
        Debug.Log($"Sending prompt: {prompt}");

        Task<string> task = GenerateCodeFromLLM(prompt);
        yield return new WaitUntil(() => task.IsCompleted);

        string code = task.Result;
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("LLM returned empty code.");
            isGenerating = false;
            yield break;
        }

        code = ExtractCodeFromResponse(code);
        Debug.Log($"Generated Code:\n{code}");

        var behaviorTask = RuntimeCompiler.CompileBehavior(code);
        yield return new WaitUntil(() => behaviorTask.IsCompleted);

        currentBehavior = behaviorTask.Result;

        if (currentBehavior != null)
        {
            Debug.Log("Behavior compiled and applied!");
            currentBehavior.Start(player);
        }
        else
        {
            Debug.LogError("Failed to compile behavior.");
        }

        isGenerating = false;
    }

    private async Task<string> GenerateCodeFromLLM(string prompt)
    {
        string endpoint = "https://api.openai.com/v1/chat/completions";

        ChatRequest requestData = new ChatRequest
        {
            model = model,
            messages = new List<ChatMessage>
            {
                new ChatMessage
                {
                    role = "system",
                    content = "You are a Unity C# scripting assistant. Generate only valid C# code for the body of an Update(Player player) method. The Player class has public fields like projectilePrefab (GameObject)," +
                    " firePoint (Transform), and moveSpeed (float). Always access them as player.projectilePrefab etc. Use Object.Instantiate() for spawning.  No comments or explanations. Raw code Only. You are a Unity C# coding assistant. Only return code inside Update(player)." +
                    "Generate only the contents of the Update(Player player) method.\r\nDo NOT include `void Update` or the class.\r\nDo NOT include any other methods.\r\nUse `player.transform` to access the player."
                },
                new ChatMessage
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0.7f
        };

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
                OpenAIChatResponse response = JsonConvert.DeserializeObject<OpenAIChatResponse>(request.downloadHandler.text);
                return response?.choices?[0]?.message?.content?.Trim();
            }
            catch (Exception e)
            {
                Debug.LogError($"JSON parse error: {e.Message}\nResponse: {request.downloadHandler.text}");
                return null;
            }
        }
    }

    private string ExtractCodeFromResponse(string response)
    {
        var match = Regex.Match(response, "```(?:csharp)?\\s*([\\s\\S]*?)```");
        if (match.Success)
            return match.Groups[1].Value.Trim();

        return response.Trim();
    }
}
