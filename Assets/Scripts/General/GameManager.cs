using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int roundNo;
    public static GameManager Instance;
    public List<GameObject> players = new List<GameObject>();
    public List<GameObject> enemies = new List<GameObject>();
    [SerializeField] private LLMController llmController;
    [SerializeField] private CurrencyData currencyData;

    public LLMController LLMController { get; private set; }
    public CurrencyData getcurrencyData { get; private set; }

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
        LLMController = llmController;
        getcurrencyData = currencyData;
    }

    public void CheckGameState()
    {
        int playerDeaths = 0;
        int enemyDeaths = 0;
        foreach (GameObject player in players)
        {
            if(player.GetComponent<Stats>().isDead)
            {
                playerDeaths++;
            }
        }
        foreach (GameObject enemy in enemies)
        {
            if (enemy.GetComponent<Stats>().isDead)
            {
                enemyDeaths++;
            }
        }

        if (playerDeaths >= players.Count)
        {
            OnPlayersLost();
        }
        if (enemyDeaths >= enemies.Count)
        {
            OnPlayersWon();
        }
        
    }

    private void OnPlayersLost()
    {
        Debug.Log("Players Lost");
        players[0].SetActive(false);
        Destroy(players[0]);
        players.Clear();

        UIController.Instance.SwitchUI(UIIndex.DeathPanel);
    }

    private void OnPlayersWon()
    {
        Debug.Log("Players Won");
        ChangeCurrency(100 + 100 * roundNo, true);
        players[0].SetActive(false);

        UIController.Instance.SwitchUI(UIIndex.RoundFinished);
    }

    public void ChangeCurrency(int amount, bool gold)
    {
        if (gold)
            getcurrencyData.gold = getcurrencyData.gold + amount;
        else
            getcurrencyData.manaStone = getcurrencyData.manaStone + amount;
    }

}
