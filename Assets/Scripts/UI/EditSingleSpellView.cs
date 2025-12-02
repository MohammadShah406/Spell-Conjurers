using System.Collections.Generic;
using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EditSingleSpellView : MonoBehaviour
{
    public TMP_InputField skillInputs;
    public int spellIndex;
    public GameObject insufficientText;
    [SerializeField] private TMP_Text currencyText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnEnable()
    {
        currencyText.text = GameManager.Instance.getcurrencyData.manaStone.ToString();
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
            if(GameManager.Instance.getcurrencyData.manaStone >= 1)
            {
                llm.StartCoroutine(llm.GenerateAndReplaceSpell(newPrompt, spellIndex));
                GameManager.Instance.getcurrencyData.manaStone--;
                UIController.Instance.SwitchUI(UIIndex.Loading);
            }
            else
            {
                StartCoroutine(ShowInsufficientText());
            }
            
        }
        else
        {
            Debug.LogError("LLMController not found in the scene!");
        }
        currencyText.text = GameManager.Instance.getcurrencyData.manaStone.ToString();
    }
    public void GoToUnitEditCurrentSpellView()
    {
        UIController.Instance.SwitchUI(UIIndex.UnitEditCurrentSpell);
    }

    public IEnumerator ShowInsufficientText()
    {
        insufficientText.SetActive(true);
        yield return new WaitForSeconds(1);
        insufficientText.SetActive(false);
    }
}
