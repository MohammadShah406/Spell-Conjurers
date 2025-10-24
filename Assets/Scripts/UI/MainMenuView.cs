using UnityEngine;

public class MainMenuView : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoToGameStart()
    {
        UIController.Instance.SwitchUI(UIIndex.Game);
    }

    public void GoToSetting()
    {
        UIController.Instance.SwitchUI(UIIndex.Setting);
    }

    public void GoToPlayerUnits()
    {
        UIController.Instance.SwitchUI(UIIndex.PlayerUnits);
    }
}
