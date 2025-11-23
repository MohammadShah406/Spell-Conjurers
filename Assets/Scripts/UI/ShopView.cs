using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopView : MonoBehaviour
{
    public List<TMP_InputField> skillInputs;
    public Button sendButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var input in skillInputs)
        {
            input.onValueChanged.AddListener(delegate { ValidateInputs(); });
        }

        ValidateInputs();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ValidateInputs()
    {
        sendButton.interactable = AllInputsFilled();
    }

    private bool AllInputsFilled()
    {
        foreach (var input in skillInputs)
        {
            if (string.IsNullOrWhiteSpace(input.text))
                return false;
        }
        return true;
    }

    public void GoToGeneratedSpell()
    {
        UIController.Instance.SwitchUI(UIIndex.GeneratedSpell);
    }

    public void GoToMainMenu()
    {
        UIController.Instance.SwitchUI(UIIndex.MainMenu);
    }
    public void GoToPlayerUnits()
    {
        UIController.Instance.SwitchUI(UIIndex.PlayerUnits);
    }

    public void GotoLoading()
    {
        UIController.Instance.SwitchUI(UIIndex.Loading);
    }
}

