using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public List<Enemy> enemies = new List<Enemy>();
    private bool isTakingTurn;
    public Transform player;

    public void AddEnemy(Enemy enemy)
    {
        enemies.Add(enemy);
    }

    public void StartEnemyTurns()
    {
        if (!isTakingTurn)
            StartCoroutine(EnemyTurnRoutine());
    }

    public IEnumerator StartEnemyTurnsCoroutine()
    {
        if (isTakingTurn)
            yield break;

        isTakingTurn = true;
        yield return StartCoroutine(EnemyTurnRoutine());
        isTakingTurn = false;
    }
    private IEnumerator EnemyTurnRoutine()
    {
        isTakingTurn = true;

        foreach (var enemy in enemies)
        {
            enemy.GetComponent<Stats>().TakeStatusDamage();
            yield return enemy.TakeTurn(() => { });
            yield return new WaitForSeconds(0.25f);
        }

        isTakingTurn = false;
        Debug.Log("All enemies finished their turns!");
    }
}
