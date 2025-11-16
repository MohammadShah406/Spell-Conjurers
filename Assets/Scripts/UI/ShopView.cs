using UnityEngine;

public class ShopView : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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

