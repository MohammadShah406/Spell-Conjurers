using UnityEngine;

public class GameView : MonoBehaviour
{


    public GameObject playerSpellPanel;
    public GameObject spellTextHolder;
    public GameObject playerStatsHolder;
    public GameObject ui_SelectedEnemy;


    private void Awake()
    {
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        //GridManager.Instance.SpawnEnemies(GridManager.Instance.enemyCount);
        //GridManager.Instance.enemyManager.initializeThreatGrid(GridManager.Instance.height, GridManager.Instance.width);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GameManager.Instance.GenerateNextRound();
        }
        // Force switch turn
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (GameManager.Instance.players.Count > 0)
            {
                TurnManager.Instance.WaitButtonPressed();
            }
        }
    }
    public void StartGame()
    {

    }

    private void OnEnable()
    {
        ResetGame();
    }

    public void ResetGame()
    {
        GameManager.Instance.GameStarted = true;

        if (GameManager.Instance == null || GridManager.Instance == null)
            return;

        int round = Mathf.Max(1, GameManager.Instance.roundNo);

        // Base enemy count for round 1
        int baseEnemies = 2;

        // Increase enemy count by 1 for every even round
        int extraFromEvenRounds = round / 2;
        int enemyCount = baseEnemies + extraFromEvenRounds;

        // Damage multiplier increases by 0.5 every round
        float dmgMultiplier = 0.5f * round;

        Debug.Log($"[GameView] Round {round}: ResetGame(enemyCount={enemyCount}, dmgMultiplier={dmgMultiplier:F2})");
        GridManager.Instance.ResetGame(enemyCount, dmgMultiplier);

    }

}
