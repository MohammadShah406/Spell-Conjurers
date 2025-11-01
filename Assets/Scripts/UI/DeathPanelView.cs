using UnityEngine;

public class DeathPanelView : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoToMainMenu()
    {
        UIController.Instance.SwitchUI(UIIndex.MainMenu);
    }
}
