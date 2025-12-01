using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class LLMJsonGridCreator : MonoBehaviour
{
    public string apiKey;
    public string model = "gpt-4o-mini";
    public UnityEvent onJsonGenerated;
    public UnityEvent JsonGenerationStarted;
    private bool isGenerating = false;
    [Header("Generation Settings")]
    [Tooltip("Number of rounds to generate in one go")]
    public int numberOfRounds = 1;
    private int currentRound = 1;

    public static LLMJsonGridCreator Instance { get; private set; }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

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
    public void Start()
    {
        //StartJsonGeneration();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void StartJsonGeneration()
    {
        if (isGenerating)
        {
            Debug.LogWarning("JSON generation is already in progress.");
            return;
        }
        JsonGenerationStarted?.Invoke();
        Debug.Log("Starting JSON generation...");
        StartCoroutine(GenerateGridMapJson());
    }

    private IEnumerator GenerateGridMapJson()
    {
        isGenerating = true;

        // Make sure the Grid folder exists
        string GridPath = Path.Combine(Application.persistentDataPath, "Rounds");
        if (!Directory.Exists(GridPath))
            Directory.CreateDirectory(GridPath);
        for (int i = 0; i < numberOfRounds; i++)
        {
            Task<string> Task = GenerateJsonFromLLM();
            yield return new WaitUntil(() => Task.IsCompleted);

            string generatedJson = Task.Result;

            if (string.IsNullOrEmpty(generatedJson))
            {
                Debug.LogError("Generated JSON was null or empty.");
                isGenerating = false;
                yield break;
            }

            // Count existing round files to determine the next number
            string[] existingFiles = Directory.GetFiles(GridPath, "Round_*.json");
            int nextRoundNumber = existingFiles.Length + 1;
            currentRound = existingFiles.Length + 1;

            // Format the filename: Round_1.json, Round_2.json, etc.
            string fileName = $"Round_{nextRoundNumber}.json";
            string filePath = Path.Combine(GridPath, fileName);

            // Save the JSON
            File.WriteAllText(filePath, generatedJson);

            Debug.Log("JSON saved to: " + filePath);
            // Small delay to avoid overwhelming the API
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("All requested map JSON files generated!");
        isGenerating = false;
        onJsonGenerated?.Invoke();

    }
    private async Task<string> GenerateJsonFromLLM()
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
                content =  "You are a JSON generator. Output ONLY valid JSON (no comments, no markdown, no explanation). " +
        "Do NOT claim to have performed searches or to have accessed external resources."

            },
            new ChatMessage
            {
                role = "user",
                content = @"Generate a JSON object containing a field called ""mapData"". 
                            Rules:
                            - current round: " + currentRound + @"
                            - mapData must be a 12x12 2D array and it will increaase with the current round number to the max of 20x20.
                            - Each row must contain min of 12 integers and max of 20 integers.
                            - Only use integers:
                              0 = empty
                              1 = wall
                              2 = Enemy
                              3 = Player
                            - Include at least Two ""2"" and it will increaase with the current round number to the max of 6.
                            - Must include 1 ""3"".
                            - ""3"" must be placed on a cell that is not adjacent (horizontally, vertically, or diagonally) to any ""2"".
                            - ""3"" must be at least 3 cell away from each ""2"".
                            - Make sure ""1"" does not block off any section of the map completely.
                            - All ""0""s must be reachable from any other ""0"" (no isolated sections).
                            - Form logical room or corridor structures with clusters of ""1""s.
                            - Output ONLY the final JSON (no comments, no markdown, no text).
                                            Example format:
                            {
                              { ""mapData"": [ [0,0,0,0,0,0,0,0,0,0,0,0], [0,0,0,0,0,0,0,0,0,0,0,0], [0,0,0,0,1,1,1,0,0,0,0,0], [0,0,0,0,1,0,0,0,0,0,0,0], [0,0,0,0,1,0,0,0,0,0,0,0], [0,0,0,0,0,0,0,0,0,0,0,0], [0,0,0,0,0,0,1,0,0,0,0,0], [0,0,0,0,0,0,1,0,0,0,0,0], [0,1,0,0,0,0,1,0,0,0,0,0], [0,1,0,0,0,0,0,0,0,0,0,0], [0,0,0,0,0,0,0,0,0,0,0,0], [0,0,2,0,0,0,1,0,0,2,0,0] ] }
                            }"
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
}
