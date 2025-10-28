using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EditSingleSpellView : MonoBehaviour
{
    public TMP_InputField skillInputs;
    public int spellIndex;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnEditSpellButtonClicked()
    {
        UIController uiController = UIController.Instance;

        if (spellIndex >= uiController.getUnitEditCurrentSpellView.getSpells.Length || uiController.getUnitEditCurrentSpellView.getSpells[spellIndex] == null)
        {
            Debug.LogWarning($"No spell found at slot {spellIndex}");
            return;
        }

        Debug.Log($"Editing spell: {uiController.getUnitEditCurrentSpellView.getSpells[spellIndex].name}");
        // Send the spell concept (name/description) to the LLMController for regeneration
        string newPrompt = skillInputs.text.Trim();
        LLMController llm = GameManager.Instance.LLMController;

        if (llm != null)
        {
            llm.StartCoroutine(llm.GenerateAndReplaceSpell(newPrompt, spellIndex));
        }
        else
        {
            Debug.LogError("LLMController not found in the scene!");
        }
    }
    public void GoToUnitEditCurrentSpellView()
    {
        UIController.Instance.SwitchUI(UIIndex.UnitEditCurrentSpell);
    }
}
