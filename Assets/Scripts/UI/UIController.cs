using System.Collections.Generic;
using UnityEngine;

public enum UIIndex
{
    Game = 0,
    GameOver = 1,
    MainMenu = 2,
    PlayerUnits = 3,
    PresetCharacterSelection = 4,
    Setting = 5,
    Shop = 6,
    GeneratedSpell = 7,
}


public class UIController : MonoBehaviour
{
    public static UIController Instance;
    [SerializeField] GameView gameView; // to be implemented need to use gameScene
    [SerializeField] GameOverView gameOverView;
    [SerializeField] MainMenuView mainMenuView;
    [SerializeField] PlayerUnitsView playerUnitsView;
    [SerializeField] PresetCharacterSelectionView presetCharacterSelectionView;
    [SerializeField] SettingView settingView;
    [SerializeField] ShopView shopView;
    public GeneratedSpellView generatedSpellView;

    [SerializeField] List<GameObject> uiList = new List<GameObject>();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    void Start()
    {
        SwitchUI(UIIndex.MainMenu);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwitchUI(UIIndex index)
    {
        foreach(GameObject UI in uiList)
        {
            UI.SetActive(false);
        }
        uiList[(int)index].SetActive(true);
    }
}
