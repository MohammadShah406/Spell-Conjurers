using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central game state manager.
/// Tracks players, enemies, currency, round progression, and win/loss conditions.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Round")]
    public int roundNo;

    [Header("Entity Tracking")]
    public List<GameObject> players = new List<GameObject>();
    public List<GameObject> enemies = new List<GameObject>();
    public List<PlayerFunctionality> playerFunctionality = new List<PlayerFunctionality>();

    [Header("References")]
    [SerializeField] private LLMController llmController;
    public CurrencyData currencyData;

    [Header("State")]
    public bool lostGame = false;
    public bool GameStarted = false;
    public bool GameEnded = false;
    public bool debugMode = false;

    // Public accessors (backward compat with UI code)
    public LLMController LLMController { get; private set; }
    public CurrencyData getcurrencyData { get; private set; }

    private bool shouldGenerate = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LLMController = llmController;
        getcurrencyData = currencyData;
    }

    // ──────────────────────────────────────────────
    // Spell Selection Delegates
    // ──────────────────────────────────────────────

    public void CallSetSpellFrom(string targetName)
    {
        for (int i = 0; i < playerFunctionality.Count; i++)
        {
            Debug.Log($"Setting spell from {targetName} for player {i}: {playerFunctionality[i]}");
            playerFunctionality[i].setSpellFrom(targetName);
        }
    }

    public void CallSetSelectedSpell(int index)
    {
        for (int i = 0; i < playerFunctionality.Count; i++)
        {
            Debug.Log($"Selecting spell index {index} for player {i}: {playerFunctionality[i]}");
            playerFunctionality[i].SetSelectedSpell(index);
        }
    }

    // ──────────────────────────────────────────────
    // Game State
    // ──────────────────────────────────────────────

    public void CheckGameState()
    {
        int playerDeaths = 0;
        int enemyDeaths = 0;

        foreach (GameObject player in players)
        {
            if (player != null && player.GetComponent<Stats>().isDead)
                playerDeaths++;
        }

        foreach (GameObject enemy in enemies)
        {
            if (enemy != null && enemy.GetComponent<Stats>().isDead)
                enemyDeaths++;
        }

        if (playerDeaths >= players.Count)
            OnPlayersLost();

        if (enemyDeaths >= enemies.Count && !GameEnded)
        {
            GameEnded = true;
            OnPlayersWon();
        }
    }

    private void OnPlayersLost()
    {
        if (lostGame) return;

        Debug.Log("[GameManager] Players Lost");
        if (players != null && players.Count > 0)
        {
            players[0].SetActive(false);
            Destroy(players[0]);
            players.Clear();
        }

        lostGame = true;
        UIController.Instance.SwitchUI(UIIndex.DeathPanel);
    }

    private void OnPlayersWon()
    {
        Debug.Log("[GameManager] Players Won");
        ChangeCurrency(100 + 100 * roundNo, true);
        UIController.Instance.SwitchUI(UIIndex.RoundFinished);
    }

    // ──────────────────────────────────────────────
    // Currency
    // ──────────────────────────────────────────────

    public void ChangeCurrency(int amount, bool gold)
    {
        Debug.Log($"Currency change: {amount} (gold={gold})");
        if (gold)
            getcurrencyData.gold += amount;
        else
            getcurrencyData.manaStone += amount;
    }

    public void ResetCurrency()
    {
        getcurrencyData.gold = 0;
        getcurrencyData.manaStone = 0;
    }

    // ──────────────────────────────────────────────
    // Round Management
    // ──────────────────────────────────────────────

    public void CleanUpPlayer()
    {
        foreach (GameObject player in players)
            Destroy(player);
        players.Clear();
    }

    public void GenerateNextRound()
    {
        roundNo += 1;
        UIController.Instance.getGameView.ResetGame();
        tryGeneratingRounds();
    }

    public void tryGeneratingRounds()
    {
        shouldGenerate = JsonManager.Instance.CanGenerate(roundNo);

        if (shouldGenerate)
        {
            LLMJsonGridCreator.Instance.StartJsonGeneration();
            shouldGenerate = false;
        }
    }
}
