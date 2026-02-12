using UnityEngine;

/// <summary>
/// Deprecated. Functionality merged into TurnManager.
/// This class is maintained for backward compatibility with existing scene references.
/// </summary>
public class CombatTurnManager : MonoBehaviour
{
    public static CombatTurnManager Instance { get; private set; }

    [Header("References")]
    public EnemyManager enemyManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Delegates to TurnManager.</summary>
    public bool IsPlayerTurn => TurnManager.Instance != null && TurnManager.Instance.IsPlayerTurn;

    /// <summary>Delegates to TurnManager.</summary>
    public void EndPlayerTurn()
    {
        TurnManager.Instance?.EndPlayerTurn();
    }
}
