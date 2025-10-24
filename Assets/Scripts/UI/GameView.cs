using UnityEngine;

public class GameView : MonoBehaviour
{
    private void Awake()
    {
        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GridManager.Instance.ResetGame();
        //GridManager.Instance.SpawnEnemies(GridManager.Instance.enemyCount);
        //GridManager.Instance.enemyManager.initializeThreatGrid(GridManager.Instance.height, GridManager.Instance.width);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void StartGame()
    {

    }
    
}
