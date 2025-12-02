using UnityEngine;

public class GameView : MonoBehaviour
{
    public GameObject playerSpellPanel;
    public GameObject spellTextHolder;
    public GameObject playerStatsHolder;
    public GameObject ui_SelectedEnemy;

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
        GridManager.Instance.ResetGame(dmgMultiplier);

    }

}
