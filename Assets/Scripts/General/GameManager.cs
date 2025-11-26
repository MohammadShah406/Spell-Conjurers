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
    public List<PlayerFunctionality> playerFunctionality;
    public LLMController LLMController { get; private set; }
    public CurrencyData getcurrencyData { get; private set; }
    public bool lostGame = false;


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

    public void CallSetSpellFrom(string tragetName)
    {
        for (int i = 0; i < playerFunctionality.Count; i++)
        {
            Debug.Log("Setting spell from " + tragetName + " for player " + i + "player is :" + playerFunctionality[i]);
            playerFunctionality[i].setSpellFrom(tragetName);
        }

    }

    public void CallSetSelectedSpell(int index)
    {
        for (int i = 0; i < playerFunctionality.Count; i++)
        {
            Debug.Log("selecting spell from index " + index + " for player " + i + "player is :" + playerFunctionality[i]);
            playerFunctionality[i].SetSelectedSpell(index);
        }
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
        if(lostGame)
            return; 
        Debug.Log("Players Lost");
        if(players!= null)
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
        Debug.Log("Players Won");
        ChangeCurrency(100 + 100 * roundNo, true);

        UIController.Instance.SwitchUI(UIIndex.RoundFinished);
    }

    public void ChangeCurrency(int amount, bool gold)
    {
        if (gold)
            getcurrencyData.gold = getcurrencyData.gold + amount;
        else
            getcurrencyData.manaStone = getcurrencyData.manaStone + amount;
    }

    public void CleanUpPlayer()
    {
        foreach (GameObject player in players)
        {
            Destroy(player);
        }
        players.Clear();
    }

}
