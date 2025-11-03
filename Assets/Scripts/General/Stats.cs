using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using System;
using UnityEngine;

public class Stats : MonoBehaviour
{
    public int maxHealth = 10;
    public int maxResource = 100;
    public bool isDead = false;

    public int health = 10;
    public int armor = 0;
    public int resource = 100;

    public List<Status> statuses = new List<Status>();

    public GameObject floatingTextPrefab;

    public GameObject uiStatsHolder;

    public enum Type
    {
        Attack,
        Support
    }
    public Type type = Type.Attack;

    void Start()
    {
        health = maxHealth;
        resource = maxResource;
        if (gameObject.tag == "Player")
        {
            UpdateStatsHolder();
        }
    }

    void Update()
    {
        
    }

    public void takeDamage(int damage)
    {
        damage = Mathf.FloorToInt(damage * (100f / (100f + armor)));
        Debug.Log("Damage after armor calculation: " + damage);
        health -= damage;
        if (damage >= 0)
        {
            ShowFloatingText(damage.ToString(), Color.red);
        }
        else
        {
            ShowFloatingText((damage * -1).ToString(), Color.green);
        }

        if (health <= 0 && !isDead)
        {
            Debug.Log("Gameobject " + gameObject.name + " died");
            isDead = true;
            OnDead();
        }

        if(gameObject.tag == "Player")
        {
            UpdateStatsHolder();
        }
    }

    public void takeDamage(int damage, Color color)
    {
        health -= damage;
        ShowFloatingText(damage.ToString(), Color.yellow);

        if (health <= 0 && !isDead)
        {
            Debug.Log("Gameobject " + gameObject.name + " died");
            isDead = true;
            OnDead();
        }
    }

    public void StatusDamage(string name, int duration, int dot)
    {
        if (dot <=0)
            return;

        Debug.Log($"Applying status {name} ({duration} turns, {dot} dmg per turn) to {gameObject.name}");

        Status existing = statuses.Find(s => s.name == name);
        if (existing != null)
        {
            existing.turnsLeft += duration;
            existing.dotDamage = Mathf.Max(existing.dotDamage, dot);
        }
        else
        {
            statuses.Add(new Status(name, duration, dot));
        }
    }

    public void TakeStatusDamage()
    {
        if (statuses.Count == 0) return;
        List<Status> expired = new List<Status>();

        foreach (Status s in statuses)
        {
            if (s.turnsLeft > 0)
            {
                takeDamage(s.dotDamage, Color.yellow);
                s.turnsLeft--;
                Debug.Log($"{gameObject.name} takes {s.dotDamage} {s.name} damage ({health} HP left)");

                if (s.turnsLeft <= 0)
                {
                    expired.Add(s);
                }
            }
        }

        foreach (Status s in expired)
        {
            Debug.Log($"{s.name} expired on {gameObject.name}");
            statuses.Remove(s);
        }
    }

    private void ShowFloatingText(string text, Color color)
    {
        if (floatingTextPrefab == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * 2f;
        GameObject go = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
        go.GetComponent<FloatingDamageNumber>().Initialize(text, color);
    }

    public void UpdateStatsHolder()
    {
        if(uiStatsHolder != null)
        {
            uiStatsHolder.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Health : " + health + "/" + maxHealth;
            uiStatsHolder.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Resource : " + resource + "/" + maxResource;
        }
    }

    public void OnDead()
    {
        // prevent repeated execution
        isDead = true;

        // If this is an enemy, remove it from EnemyManager and GameManager lists,
        // clear its tile occupant and destroy the GameObject.
        Enemy enemyComp = GetComponent<Enemy>();
        if (enemyComp != null)
        {
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.enemies.Remove(enemyComp);
            }

            if (GameManager.Instance != null && GameManager.Instance.enemies.Contains(gameObject))
            {
                GameManager.Instance.enemies.Remove(gameObject);
            }

            GridManager gm = GridManager.Instance;
            if (gm != null)
            {
                Tile t = gm.GetTile(enemyComp.gridPosition);
                if (t != null && t.occupant == gameObject)
                    t.occupant = null;
            }

            GameManager.Instance?.CheckGameState();

            Destroy(gameObject);
            return;
        }

        // If this is a player, move their spells to the main spells folder,
        // disable player functionality and update UI/state.
        if (gameObject.CompareTag("Player"))
        {
            // Move player spells into the main Spells folder (overwrite if necessary)
            try
            {
                JsonManager.Instance?.MovePlayerSpellsToSpells(true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to move player spells on death: {ex.Message}");
            }

            // disable player control component to avoid further input
            PlayerFunctionality pf = GetComponent<PlayerFunctionality>();
            if (pf != null)
            {
                pf.enabled = false;
            }

            UpdateStatsHolder();
            GameManager.Instance?.CheckGameState();
        }
    }
}

[System.Serializable]
public class Status
{
    public string name;
    public int turnsLeft;
    public int dotDamage;

    public Status(string name, int duration, int dot)
    {
        this.name = name;
        this.turnsLeft = duration;
        this.dotDamage = dot;
    }
}
