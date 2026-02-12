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


/// <summary>
/// Production LLM controller for spell generation.
/// Handles OpenAI API communication, JSON deserialization, file I/O, and spell precompilation.
/// </summary>
public class LLMController : MonoBehaviour
{
    [Header("UI References")]
    public List<TMP_InputField> skillInputs;

    [Header("API Settings")]
    public string apiKey;
  public string model = "gpt-4o-mini";

  [Header("Prompt")]
    public string promptFileName = "LLMTrainer";

    [Header("Events")]
    public UnityEvent onJsonGenerated;
    public UnityEvent onSpellReplaced;

    // --- State ---
    private bool isGenerating = false;
  private string loadedPrompt = "";
    private string cachedScripts = "";
    private string cachedSpellRefs = "";

    public List<GeneratedSkillData> generatedSkills = new List<GeneratedSkillData>();

    private void Start()
    {
        ClearSkillsFolder();
        cachedScripts = LoadAllProjectScripts("Scripts");
        cachedSpellRefs = JsonManager.Instance != null
            ? JsonManager.Instance.MergeJsonToString("SpellReference")
     : "";
    }

    // ========================
    // API Key Setup
    // ========================

    public void SetAPIKey(string newKey)
    {
        apiKey = newKey;
        Debug.Log("[LLMController] API Key set.");

        var config = OpenAIConfig.LoadConfig();
      if (config != null)
     model = string.IsNullOrEmpty(config.model) ? "gpt-4o-mini" : config.model;

        TextAsset promptFile = Resources.Load<TextAsset>(promptFileName);
        loadedPrompt = promptFile != null ? promptFile.text : "";

        if (promptFile == null)
        Debug.LogWarning("[LLMController] Prompt file not found in Resources.");
    }

    // ========================
    // Public Generation API
    // ========================

  public void OnGenerateSkillsButton()
  {
        if (isGenerating)
        {
  Debug.LogWarning("[LLMController] Generation already in progress.");
 return;
      }
        StartCoroutine(GenerateAllSkills());
    }

    public void ShowGeneratedSpell()
    {
    for (int i = 0; i < generatedSkills.Count; i++)
        {
            UIController.Instance.getGeneratedSpellView.SetGeneratedSpellInfo(i, generatedSkills[i].spellData);
        }
    }

    // ========================
    // Batch Generation
    // ========================

    private IEnumerator GenerateAllSkills()
    {
 isGenerating = true;
        generatedSkills.Clear();

        EnsureFolder(Path.Combine(Application.persistentDataPath, "Spells"));

        for (int i = 0; i < skillInputs.Count; i++)
    {
            string prompt = skillInputs[i].text.Trim();
            if (string.IsNullOrEmpty(prompt))
  {
      Debug.LogWarning($"[LLMController] Skill input {i + 1} is empty — skipping.");
         continue;
         }
         yield return StartCoroutine(GenerateSkill(prompt, i + 1));
        }

        Debug.Log("[LLMController] All skills generated.");
 isGenerating = false;
        onJsonGenerated?.Invoke();
    }

    private IEnumerator GenerateSkill(string prompt, int skillIndex)
    {
        Debug.Log($"[LLMController] Generating skill {skillIndex}: {prompt}");

        Task<string> skillTask = GenerateSkillFromLLM(prompt);
        yield return new WaitUntil(() => skillTask.IsCompleted);

        if (skillTask.Result == null)
        {
            Debug.LogError($"[LLMController] Skill {skillIndex} generation failed.");
            yield break;
        }

        string skillJson = ExtractJsonFromResponse(skillTask.Result);
        if (string.IsNullOrEmpty(skillJson))
        {
            Debug.LogError($"[LLMController] Skill {skillIndex} JSON extraction failed.");
            yield break;
        }

        Spell spell = DeserializeSpell(skillJson);
        if (spell == null)
        {
            Debug.LogError($"[LLMController] Skill {skillIndex} deserialization failed.");
            yield break;
        }

        // Try to compile — if it fails, retry once with error feedback
        if (!TryPrecompileSpell(spell))
        {
            Debug.LogWarning($"[LLMController] Skill {skillIndex} failed compile. Retrying with error feedback...");
            yield return StartCoroutine(RetrySpellWithErrors(prompt, spell, skillIndex));
            yield break;
        }

        SaveSpellAndRegister(spell, skillJson, skillIndex);
    }

    // ========================
    // Single Spell Replacement
    // ========================

    public IEnumerator GenerateAndReplaceSpell(string prompt, int spellIndex)
    {
        Debug.Log($"[LLMController] Regenerating spell at index {spellIndex}: {prompt}");

   Task<string> skillTask = GenerateSkillFromLLM(prompt);
        yield return new WaitUntil(() => skillTask.IsCompleted);

        if (skillTask.Result == null)
        {
 Debug.LogError($"[LLMController] Failed to regenerate spell {spellIndex}.");
            yield break;
        }

        string skillJson = ExtractJsonFromResponse(skillTask.Result);
        if (string.IsNullOrEmpty(skillJson))
    {
         Debug.LogError($"[LLMController] No valid JSON returned for spell {spellIndex}.");
       yield break;
        }

        Spell newSpell = null;
        string filePath = null;
        bool needsRetry = false;

        try
        {
            newSpell = DeserializeSpell(skillJson);
            if (newSpell == null)
            {
                Debug.LogError($"[LLMController] Invalid JSON when regenerating spell {spellIndex}.");
                yield break;
            }

            string spellFolder = EnsureFolder(Path.Combine(Application.persistentDataPath, "PlayerSpells"));
            string fileName = $"{SanitizeFileName(newSpell.name)}.json";
            filePath = Path.Combine(spellFolder, fileName);

            // Delete old file if name changed
            if (spellIndex < generatedSkills.Count)
            {
                string oldPath = generatedSkills[spellIndex].filePath;
                if (File.Exists(oldPath) && !oldPath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(oldPath);
                    Debug.Log("[LLMController] Deleted old spell file: " + oldPath);
                }
            }

            File.WriteAllText(filePath, skillJson);

            var data = new GeneratedSkillData
            {
                filePath = filePath,
                jsonContent = skillJson,
                spellData = newSpell
            };

            if (spellIndex < generatedSkills.Count)
                generatedSkills[spellIndex] = data;
            else
                generatedSkills.Add(data);

            // Precompile via SpellCompiler if available
            if (!TryPrecompileSpell(newSpell))
            {
                Debug.LogWarning($"[LLMController] Replaced spell {spellIndex} failed to compile, retrying...");
                needsRetry = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LLMController] Error regenerating spell {spellIndex}: {ex.Message}");
            yield break;
        }

        if (needsRetry)
        {
            yield return StartCoroutine(RetrySpellWithErrors(prompt, newSpell, spellIndex));
            yield break;
        }

        UIController.Instance.getGeneratedSpellView.SetGeneratedSpellInfo(spellIndex, newSpell);
        Debug.Log($"[LLMController] Spell {spellIndex} regenerated: {newSpell.name}");
        onSpellReplaced?.Invoke();
    }

 // ========================
    // LLM API Call
    // ========================

    private async Task<string> GenerateSkillFromLLM(string prompt)
    {
   string endpoint = "https://api.openai.com/v1/chat/completions";

 string skillPrompt =
            $"Generate a JSON object representing a game skill. " +
 $"The skill concept is: {prompt}. " +
       $"Output only valid JSON without code blocks or explanations.";

  string systemContent =
  "You are a helpful assistant that outputs only clean JSON data for Unity games. " +
     "Do NOT claim to have performed searches or accessed external resources. " +
          "Never include code fences, markdown, or explanations — just valid JSON. " +
      cachedScripts +
       $"EXAMPLE SPELL JSON: {cachedSpellRefs}. " +
    loadedPrompt;

        ChatRequest requestData = new ChatRequest
        {
    model = model,
    messages = new List<ChatMessage>
            {
   new ChatMessage { role = "system", content = systemContent },
   new ChatMessage { role = "user", content = skillPrompt }
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

            string rawResponse = request.downloadHandler?.text ?? "";

  if (request.result != UnityWebRequest.Result.Success)
    {
                Debug.LogError($"[LLMController] Request failed: {request.error}\n{rawResponse}");
    return null;
        }

        try
            {
        OpenAIChatResponse response = JsonConvert.DeserializeObject<OpenAIChatResponse>(rawResponse);
          string jsonContent = response?.choices?[0]?.message?.content?.Trim();

       if (!string.IsNullOrEmpty(jsonContent))
        jsonContent = Regex.Replace(jsonContent, @"^```(json|csharp)?\s*|```$", "", RegexOptions.Multiline).Trim();

     return jsonContent;
       }
            catch (Exception e)
   {
    Debug.LogError($"[LLMController] Parse error: {e.Message}\n{rawResponse}");
        return null;
            }
        }
    }

  // ========================
    // Utilities
    // ========================

    private string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) return null;
        int start = response.IndexOf('{');
        int end = response.LastIndexOf('}');
   if (start >= 0 && end > start)
            return response.Substring(start, end - start + 1);
        return null;
    }

    private Spell DeserializeSpell(string json)
    {
        try
        {
      Spell spell = JsonConvert.DeserializeObject<Spell>(json);
            if (spell == null || string.IsNullOrWhiteSpace(spell.name))
         return null;
   return spell;
        }
   catch (Exception ex)
        {
            Debug.LogError($"[LLMController] Deserialization error: {ex.Message}");
 return null;
     }
    }

    private void SaveSpellAndRegister(Spell spell, string json, int index)
    {
 string spellFolder = EnsureFolder(Path.Combine(Application.persistentDataPath, "PlayerSpells"));
      string fileName = $"{SanitizeFileName(spell.name)}.json";
        string filePath = Path.Combine(spellFolder, fileName);

        try
        {
        File.WriteAllText(filePath, json);

          generatedSkills.Add(new GeneratedSkillData
   {
  filePath = filePath,
          jsonContent = json,
            spellData = spell
        });

       PrecompileSpell(spell);
            Debug.Log($"[LLMController] Skill {index} saved: {filePath}");
    }
        catch (Exception ex)
        {
   Debug.LogError($"[LLMController] Failed to save skill {index}: {ex.Message}");
        }
    }

    private void PrecompileSpell(Spell spell)
    {
        if (string.IsNullOrWhiteSpace(spell.code)) return;
        TryPrecompileSpell(spell);
    }

    /// <summary>
    /// Attempt precompilation. Returns true if successful.
    /// </summary>
    private bool TryPrecompileSpell(Spell spell)
    {
        if (string.IsNullOrWhiteSpace(spell.code)) return true;

        if (SpellCompiler.Instance != null)
            return SpellCompiler.Instance.Precompile(spell);
        else if (SpellFunction.Instance != null)
        {
            SpellFunction.Instance.PrecompileSpell(spell);
            return true; // legacy path can't report errors
        }
        return true;
    }

    /// <summary>
    /// Retry spell generation by sending compile errors back to the LLM.
    /// The LLM gets the original prompt, the broken code, and the error messages.
    /// </summary>
    private IEnumerator RetrySpellWithErrors(string originalPrompt, Spell failedSpell, int skillIndex)
    {
        string errors = SpellCompiler.Instance?.LastCompileErrors ?? "Unknown compile error";
        string failedCode = failedSpell.code ?? "";

        string retryPrompt =
            $"The previous spell code failed to compile in the SpellGlobals Roslyn sandbox.\n" +
            $"Original spell concept: {originalPrompt}\n" +
            $"Failed code:\n{failedCode}\n\n" +
            $"Compile errors:\n{errors}\n\n" +
            $"Fix the code so it compiles. Only use methods and properties available in SpellGlobals:\n" +
            $"DealDamage(int), HealTarget(int), SelfDamage(int), SelfHeal(int), " +
            $"ApplyStatus(string,int,int), ApplyStatusToSelf(string,int,int), " +
            $"RequestSummon(int hp, int dmg, int dur, int range, string ai), " +
            $"AddCurrency(int,bool), Roll(int,int), RemoveStatus(string), RemoveTargetStatus(string), " +
            $"FromHealth, ToHealth, FromMaxHealth, ToMaxHealth, FromResource, Distance, " +
            $"HasStatus(string), TargetHasStatus(string), spell, from, to.\n" +
            $"Output only valid JSON with the corrected code field.";

        Task<string> retryTask = GenerateSkillFromLLM(retryPrompt);
        yield return new WaitUntil(() => retryTask.IsCompleted);

        if (retryTask.Result == null)
        {
            Debug.LogError($"[LLMController] Retry for skill {skillIndex} returned null.");
            yield break;
        }

        string retryJson = ExtractJsonFromResponse(retryTask.Result);
        if (string.IsNullOrEmpty(retryJson))
        {
            Debug.LogError($"[LLMController] Retry JSON extraction failed for skill {skillIndex}.");
            yield break;
        }

        Spell retrySpell = DeserializeSpell(retryJson);
        if (retrySpell == null)
        {
            Debug.LogError($"[LLMController] Retry deserialization failed for skill {skillIndex}.");
            yield break;
        }

        if (!TryPrecompileSpell(retrySpell))
        {
            Debug.LogError($"[LLMController] Spell {skillIndex} still fails after retry. Giving up.");
            yield break;
        }

        Debug.Log($"[LLMController] Spell {skillIndex} compiled successfully on retry.");
        SaveSpellAndRegister(retrySpell, retryJson, skillIndex);
    }

    private string SanitizeFileName(string name)
    {
   return name.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
    }

    private string EnsureFolder(string path)
    {
 if (!Directory.Exists(path))
 Directory.CreateDirectory(path);
        return path;
  }

    public void ClearSkillsFolder()
    {
        generatedSkills.Clear();
  string spellFolder = Path.Combine(Application.persistentDataPath, "PlayerSpells");
        if (Directory.Exists(spellFolder))
 {
            try
            {
              Directory.Delete(spellFolder, true);
      }
            catch (Exception ex)
            {
     Debug.LogError($"[LLMController] Failed to clear PlayerSpells: {ex.Message}");
            }
        }
        Directory.CreateDirectory(spellFolder);
    }

    private string LoadAllProjectScripts(string folderName)
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, folderName);
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"[LLMController] Folder not found: {folderPath}");
     return "";
        }

   StringBuilder sb = new StringBuilder();
        try
        {
         string[] files = Directory.GetFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
   {
 sb.AppendLine(File.ReadAllText(file));
            }
   return sb.ToString();
     }
        catch (Exception ex)
      {
  Debug.LogError($"[LLMController] Failed to read scripts: {ex.Message}");
     return "";
    }
    }
}
