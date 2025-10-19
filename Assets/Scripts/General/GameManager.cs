using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public List<GameObject> players = new List<GameObject>();
    public List<GameObject> enemies = new List<GameObject>();




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
            Debug.Log("Players Lost");
        }
        if (enemyDeaths >= enemies.Count)
        {
            Debug.Log("Player Won");
        }
        
    }

}
