using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

/// <summary>
/// Core combat statistics for any entity.
/// Handles health, resource, armor, damage calculation, status effects, and death.
/// Death cleanup is event-driven: entities subscribe to OnDeath for entity-specific logic.
/// </summary>
public class Stats : MonoBehaviour
{
    [Header("Base Stats")]
    public int maxHealth = 100;
    public int maxResource = 100;
    public int health = 100;
    public int armor = 0;
    public int resource = 100;
    public bool isDead = false;

    [Header("Entity Type")]
    public Type type = Type.Attack;
    public EntityRole role = EntityRole.Damage;

    [Header("Prefabs")]
    public GameObject floatingTextPrefab;
    public GameObject manaCrystalPrefab;

    [Header("UI")]
    public GameObject uiStatsHolder;

    [Header("Status Effects")]
    public List<StatusEffect> statuses = new List<StatusEffect>();

    // --- Events ---

    /// <summary>Raised when this entity dies. Subscribers handle entity-specific cleanup.</summary>
    public event Action<Stats> OnDeath;

    /// <summary>Raised when health changes. Args: (currentHealth, maxHealth)</summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary>Raised when resource changes. Args: (currentResource, maxResource)</summary>
    public event Action<int, int> OnResourceChanged;

    // --- Backward compatibility enum ---
    public enum Type { Attack, Support }

    private void OnEnable()
    {
        if (UIController.Instance != null && UIController.Instance.getGameView != null)
            uiStatsHolder = UIController.Instance.getGameView.playerStatsHolder;
    }

    private void Start()
    {
        health = maxHealth;
        resource = maxResource;
        if (gameObject.CompareTag("Player"))
            UpdateStatsHolder();
    }

    // ──────────────────────────────────────────────
    // Damage & Healing
    // ──────────────────────────────────────────────

    /// <summary>
    /// Apply damage with armor mitigation. Negative values heal.
    /// </summary>
    public void takeDamage(int damage)
    {
        if (isDead) return;

        if (damage > 0)
            damage = Mathf.FloorToInt(damage * (100f / (100f + armor)));

        health -= damage;

        if (damage >= 0)
            ShowFloatingText(damage.ToString(), Color.red);
        else
            ShowFloatingText((-damage).ToString(), Color.green);

        OnHealthChanged?.Invoke(health, maxHealth);

        if (health <= 0 && !isDead)
        {
            isDead = true;
            HandleDeath();
        }

        if (gameObject.CompareTag("Player"))
            UpdateStatsHolder();
    }

    /// <summary>Damage with custom floating text color.</summary>
    public void takeDamage(int damage, Color color)
    {
        if (isDead) return;

        health -= damage;
        ShowFloatingText(damage.ToString(), color);
        OnHealthChanged?.Invoke(health, maxHealth);

        if (health <= 0 && !isDead)
        {
            isDead = true;
            HandleDeath();
        }
    }

    /// <summary>Restore resource by percentage of max, clamped.</summary>
    public void RegenerateResource(float percentage)
    {
        resource = Mathf.Clamp(
            resource + Mathf.FloorToInt(maxResource * percentage), 0, maxResource);
        OnResourceChanged?.Invoke(resource, maxResource);
    }

    public void ClampHealth()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateStatsHolder();
    }

    // ──────────────────────────────────────────────
    // Status Effects
    // ──────────────────────────────────────────────

    /// <summary>Apply or stack a damage-over-time status.</summary>
    public void StatusDamage(string name, int duration, int dot)
    {
        if (dot <= 0) return;

        Debug.Log($"Applying status {name} ({duration} turns, {dot} dmg/turn) to {gameObject.name}");

        StatusEffect existing = statuses.Find(s => s.statusName == name);
        if (existing != null)
        {
            existing.remainingDuration += duration;
            existing.damagePerTurn = Mathf.Max(existing.damagePerTurn, dot);
        }
        else
        {
            statuses.Add(new StatusEffect(name, duration, dot));
        }
    }

    /// <summary>Process all active status effects at turn start.</summary>
    public void TakeStatusDamage()
    {
        if (statuses.Count == 0) return;

        var expired = new List<StatusEffect>();
        foreach (var effect in statuses)
        {
            if (effect.remainingDuration > 0)
            {
                takeDamage(effect.damagePerTurn, Color.yellow);
                effect.remainingDuration--;
                Debug.Log($"{gameObject.name} takes {effect.damagePerTurn} {effect.statusName} damage ({health} HP left)");

                if (effect.remainingDuration <= 0)
                    expired.Add(effect);
            }
        }

        foreach (var effect in expired)
        {
            Debug.Log($"{effect.statusName} expired on {gameObject.name}");
            statuses.Remove(effect);
        }
    }

    /// <summary>Check if entity has a specific active status.</summary>
    public bool HasStatus(string statusName)
    {
        return statuses.Exists(s => s.statusName == statusName && s.remainingDuration > 0);
    }

    /// <summary>Remove all instances of a named status.</summary>
    public void RemoveStatus(string statusName)
    {
        statuses.RemoveAll(s => s.statusName == statusName);
    }

    /// <summary>Clear all status effects.</summary>
    public void ClearAllStatuses()
    {
        statuses.Clear();
    }

    // ──────────────────────────────────────────────
    // UI & Visual Feedback
    // ──────────────────────────────────────────────

    public void ShowFloatingText(string text, Color color)
    {
        if (floatingTextPrefab == null) return;
        Vector3 spawnPos = transform.position + Vector3.up * 2f;
        var go = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
        go.GetComponent<FloatingDamageNumber>().Initialize(text, color);
    }

    public void UpdateStatsHolder()
    {
        if (uiStatsHolder == null) return;
        uiStatsHolder.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text =
            $"Health : {health}/{maxHealth}";
        uiStatsHolder.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text =
            $"Resource : {resource}/{maxResource}";
    }

    // ──────────────────────────────────────────────
    // Death Handling
    // ──────────────────────────────────────────────

    private void HandleDeath()
    {
        Debug.Log($"[Stats] {gameObject.name} died");

        if (OnDeath != null)
        {
            try { OnDeath.Invoke(this); }
            catch (Exception ex) { Debug.LogError($"[Stats] Death handler error: {ex.Message}"); }
        }
        else
        {
            // Legacy fallback for entities that haven't migrated to event-driven cleanup
            LegacyDeathHandling();
        }
    }

    /// <summary>
    /// Legacy death handling preserved for backward compatibility.
    /// New entities should subscribe to OnDeath event instead.
    /// </summary>
    private void LegacyDeathHandling()
    {
        Enemy enemyComp = GetComponent<Enemy>();
        if (enemyComp != null)
        {
            // Mana crystal drop
            int rand = UnityEngine.Random.Range(1, 101);
            if (rand <= 25 && manaCrystalPrefab != null)
            {
                Instantiate(manaCrystalPrefab, transform.position, Quaternion.identity);
                GameManager.Instance?.ChangeCurrency(1, false);
            }

            EnemyManager.Instance?.enemies.Remove(enemyComp);

            if (GameManager.Instance != null && GameManager.Instance.enemies.Contains(gameObject))
                GameManager.Instance.enemies.Remove(gameObject);

            if (GridManager.Instance != null)
            {
                Tile tile = GridManager.Instance.GetTile(enemyComp.gridPosition);
                if (tile != null && tile.occupant == gameObject)
                    tile.occupant = null;
            }

            GameManager.Instance?.CheckGameState();
            Destroy(gameObject);
            return;
        }

        if (gameObject.CompareTag("Player"))
        {
            try
            {
                JsonManager.Instance?.MoveSpellsFromFolders(
                    Path.Combine(Application.persistentDataPath, "PlayerSpells"),
                    Path.Combine(Application.persistentDataPath, "Spells"), true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to move player spells on death: {ex.Message}");
            }

            var pf = GetComponent<PlayerFunctionality>();
            if (pf != null) pf.enabled = false;

            UpdateStatsHolder();
            GameManager.Instance?.CheckGameState();
        }
    }

    public void OnDead()
    {
        if (!isDead)
        {
            isDead = true;
            HandleDeath();
        }
    }
}

/// <summary>
/// Role classification for AI targeting priority.
/// </summary>
public enum EntityRole
{
    Damage,
    Support,
    Tank,
    Summoner
}

/// <summary>
/// An active status effect on an entity.
/// Unified type used by both Stats and StatusEffectController.
/// </summary>
[System.Serializable]
public class StatusEffect
{
    public string statusName;
    public int remainingDuration;
    public int damagePerTurn;

    public StatusEffect(string name, int duration, int damagePerTurn)
    {
        this.statusName = name;
        this.remainingDuration = duration;
        this.damagePerTurn = damagePerTurn;
    }

    public bool IsExpired => remainingDuration <= 0;

    /// <summary>Tick one turn: reduce duration, return damage dealt.</summary>
    public int Tick()
    {
        if (IsExpired) return 0;
        remainingDuration--;
        return damagePerTurn;
    }
}
