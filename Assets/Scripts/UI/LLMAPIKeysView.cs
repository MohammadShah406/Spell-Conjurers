using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class LLMAPIKeysView : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private LLMController llmController;
    [SerializeField] private GameObject invalidKeyText;   
    [SerializeField] private GameObject validatingText;    

    public string apiKey;
    private bool isAPIKeyValid = false;

    public void SaveAPIKey()
    {
        string key = inputField.text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("API key input is empty.");
            ShowInvalidKey("API key cannot be empty.");
            return;
        }

        StartCoroutine(ValidateAndSetKey(key));
    }

    private IEnumerator ValidateAndSetKey(string keyToTest)
    {
        // Show "validating..." feedback
        if (validatingText != null) validatingText.SetActive(true);
        if (invalidKeyText != null) invalidKeyText.SetActive(false);

        // Lightweight validation: GET /v1/models (no tokens consumed)
        string endpoint = "https://api.openai.com/v1/models";
        bool isValid = false;
        string errorMsg = "";

        using (UnityWebRequest request = UnityWebRequest.Get(endpoint))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + keyToTest);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                isValid = true;
            }
            else
            {
                errorMsg = request.downloadHandler?.text ?? request.error;
            }
        }

        if (validatingText != null) validatingText.SetActive(false);

        if (!isValid)
        {
            Debug.LogError("API key validation failed: " + errorMsg);
            ShowInvalidKey("Invalid API key.");
            //invalidKeyText.SetActive(true);
            CheakIsAPIKeyValid(false);
            yield break;
        }


        // Key is valid — propagate to both controllers
        Debug.Log("API key validated successfully.");
        apiKey = keyToTest;
        isAPIKeyValid = true;
        CheakIsAPIKeyValid(true);

        llmController.SetAPIKey(keyToTest);
        LLMJsonGridCreator.Instance.SetAPIKey(keyToTest);
        UIController.Instance.SwitchUI(UIIndex.MainMenu);
    }

    public void CheakIsAPIKeyValid(bool isValid)
    {
        isAPIKeyValid = isValid;
    }

    private void ShowInvalidKey(string msg)
    {
        if (invalidKeyText != null)
        {
            invalidKeyText.SetActive(true);
            var tmp = invalidKeyText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null) tmp.text = msg;
        }
    }
}
