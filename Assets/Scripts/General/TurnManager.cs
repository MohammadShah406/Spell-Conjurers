using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public EnemyManager enemyManager;

    void Start()
    {
        if(enemyManager == null)
        {
            Debug.Log("TurnManager: enemyManager not found. Attempting to find");
            enemyManager = GameObject.FindAnyObjectByType<EnemyManager>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Starting enemy turns...");
            enemyManager.StartEnemyTurns();
        }
    }
}
