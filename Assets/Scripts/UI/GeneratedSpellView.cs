using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GeneratedSpellView : MonoBehaviour
{
    [SerializeField] private List<TextMeshProUGUI> spellNameText;
    [SerializeField] private List<TextMeshProUGUI> spellDescriptionText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetGeneratedSpellInfo(int index, Spell spell)
    {
        spellNameText[index].text = spell.name;
        spellDescriptionText[index].text = spell.description;
        Debug.Log("Skill are now set");
    }

}
