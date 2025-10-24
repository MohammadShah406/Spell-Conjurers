using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerUnitsView : MonoBehaviour
{
    [SerializeField] private List<Image> targetImage;
    [SerializeField] private List<Sprite> characterSprites;
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

    public void ChangeCharacterSelectionImage()
    {

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
    public void TargetSlot(int index)
    {
        targetSlot = index;
    }


}
