using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerUnitsView : MonoBehaviour
{
    [SerializeField] private List<Image> targetImage;
    [SerializeField] private List<Sprite> characterSprites;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private List<GameObject> createCharButton;
    [SerializeField] private List<GameObject> editCharButton;

    public int targetSlot;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void GoToShop()
    {
        UIController.Instance.SwitchUI(UIIndex.Shop);
    }
    public void GoToCharPreset()
    {
        UIController.Instance.SwitchUI(UIIndex.PresetCharacterSelection);
    }

    public void GoToMainMenu()
    {
        UIController.Instance.SwitchUI(UIIndex.MainMenu);
    }
    public void ChangeImage(Image targetImage, Sprite newSprite)
    {
        if (targetImage == null)
        {
            Debug.LogWarning("UIImageChanger: Target Image is null!");
            return;
        }

        if (newSprite == null)
        {
            Debug.LogWarning("UIImageChanger: Sprite is null!");
            return;
        }

        targetImage.sprite = newSprite;
    }

    public void ChangeCharacterImage()
    {
        Debug.Log("target index Slot is " + targetSlot);
        ChangeImage(targetImage[targetSlot], characterSprites[targetSlot]);
    }

    public void ChangeCharacterBackImage()
    {
        Debug.Log("target index Slot is " + targetSlot);
        ChangeImage(targetImage[targetSlot], defaultSprite);
    }

    public void ChangeButton()
    {
        createCharButton[targetSlot].SetActive(false);
        editCharButton[targetSlot].SetActive(true);
    }
    public void TargetSlot(int index)
    {
        targetSlot = index;
    }
    public void GoToUnitEditCurrentSpellView()
    {
        UIController.Instance.SwitchUI(UIIndex.UnitEditCurrentSpell);
    }

}
