using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine.Events;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class ChatRequest
{
    public string model;
    public List<ChatMessage> messages;
    public float temperature;
}

[System.Serializable]
public class ChatMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class OpenAIChatResponse
{
    public Choice[] choices;
}

[System.Serializable]
public class Choice
{
    public ChatMessage message;
}

public class OpenAIScript : MonoBehaviour
{
    public string apiKey;
    public string model = "gpt-4o-mini";

    public bool useBaseJsonAsExample = false;
    public UnityEvent onJsonGenerated;

    private ICustomBehavior currentBehavior;
    private Player player;
}
