using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitEditCurrentSpellView : MonoBehaviour
{
    [SerializeField] private Spell[] spells = new Spell[4];
    [SerializeField] private List<TextMeshProUGUI> spellNameText;
    [SerializeField] private List<TextMeshProUGUI> spellDescriptionText;
    public Spell[] getSpells { get; private set; }
    //[SerializeField] private List<Button> editSpelButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        getSpells = spells;
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetPlayerSpell()
    {
        for (int i = 0; i < spells.Length; i++)
        {
            spells[i] = GameManager.Instance.LLMController.generatedSkills[i].spellData;
            Debug.Log("spell is : " + spells[i].name);
            SetGeneratedSpellInfo(i, spells[i]);
        }
    }

    private void OnEnable()
    {
        GetPlayerSpell();
    }

    public void SetGeneratedSpellInfo(int index, Spell spell)
    {
        if (index < spellNameText.Count && spell != null)
        {
            spellNameText[index].text = spell.name;
            spellDescriptionText[index].text = spell.description;
        }
    }

    public void GoToEditSingleSpell(int spellIndex)
    {
        UIController.Instance.getEditSingleSpellView.spellIndex = spellIndex;
        UIController.Instance.SwitchUI(UIIndex.editSingleSpell);
    }

    public void GoToUnitEditCurrentSpellView()
    {
        UIController.Instance.SwitchUI(UIIndex.UnitEditCurrentSpell);
    }

    public void GoToPlayerUnitView()
    {
        UIController.Instance.SwitchUI(UIIndex.PlayerUnits);
    }
}


