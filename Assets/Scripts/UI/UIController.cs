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
    UnitEditCurrentSpell = 8,
    editSingleSpell = 9,
    DeathPanel = 10,
    RoundFinished = 11,
    Loading = 12,
    llmAPIKeys = 13,
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
    [SerializeField] GeneratedSpellView generatedSpellView;
    [SerializeField] UnitEditCurrentSpellView unitEditCurrentSpellView;
    [SerializeField] EditSingleSpellView editSingleSpellView;
    [SerializeField] DeathPanelView deathPanelView;
    [SerializeField] RoundFinishedView roundFinishedView;
    [SerializeField] LoadingView loadingView;
    [SerializeField] LLMAPIKeysView llmAPIKeysView;

    public GameView getGameView { get; private set; }
    public GameOverView getGameOverView { get; private set; }
    public MainMenuView getMainMenuView { get; private set; }
    public PlayerUnitsView getPlayerUnitsView { get; private set; }
    public PresetCharacterSelectionView getPresetCharacterSelectionView { get; private set; }
    public SettingView getSettingView { get; private set; }
    public ShopView getShopView { get; private set; }
    public GeneratedSpellView getGeneratedSpellView { get; private set; }
    public UnitEditCurrentSpellView getUnitEditCurrentSpellView { get; private set; }
    public EditSingleSpellView getEditSingleSpellView { get; private set; }

    public DeathPanelView GetDeathPanelView { get; private set; }
    public RoundFinishedView GetRoundFinishedView { get; private set; }

    public LoadingView GetLoadingView { get; private set; }

    public LLMAPIKeysView GetLLMAPIKeysView { get; private set; }

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
        getGameView = gameView;
        getGameOverView = gameOverView;
        getMainMenuView = mainMenuView;
        getPlayerUnitsView = playerUnitsView;
        getPresetCharacterSelectionView = presetCharacterSelectionView;
        getSettingView = settingView;
        getShopView = shopView;
        getGeneratedSpellView = generatedSpellView;
        getUnitEditCurrentSpellView = unitEditCurrentSpellView;
        getEditSingleSpellView = editSingleSpellView;
        GetDeathPanelView = deathPanelView;
        GetRoundFinishedView = roundFinishedView;
        GetLoadingView = loadingView;
        GetLLMAPIKeysView = llmAPIKeysView;
    }
    void Start()
    {
        SwitchUI(UIIndex.llmAPIKeys);
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
