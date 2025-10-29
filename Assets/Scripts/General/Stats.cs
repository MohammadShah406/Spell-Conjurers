using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using TMPro;

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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;
        resource = maxResource;
        if (gameObject.tag == "Player")
        {
            UpdateStatsHolder();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void takeSelfDamage(int damage)
    {
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

        if (health <= 0)
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

    public void takeDamage(int damage)
    {
        //finalDamage = baseDamage * (100f / (100f + defense)); 
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

        if (health <= 0)
        {
            Debug.Log("Gameobject " + gameObject.name + " died");
            isDead = true;
            OnDead();
        }

        if (gameObject.tag == "Player")
        {
            UpdateStatsHolder();
        }


    }

    public void takeTrueDamage(int damage, Color color)
    {
        health -= damage;
        ShowFloatingText(damage.ToString(), Color.yellow);

        if (health <= 0)
        {
            Debug.Log("Gameobject " + gameObject.name + " died");
        }

    }



    public void StatusDamage(string name, int duration, int dot)
    {
        if (dot <=0)
        {
            return;
        }
        Debug.Log($"Applying status {name} ({duration} turns, {dot} dmg per turn) to {gameObject.name}");

        // Check if the same status already exists → refresh it
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
                takeTrueDamage(s.dotDamage, Color.yellow);
                s.turnsLeft--;
                Debug.Log($"{gameObject.name} takes {s.dotDamage} {s.name} damage ({health} HP left)");

                if (s.turnsLeft <= 0)
                {
                    expired.Add(s);
                }
            }
        }

        // Remove expired statuses
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
        // Prevent multiple calls if already dead
        if (!isDead)
            return;

        Debug.Log($"[Stats] {gameObject.name} died. Removing from grid and updating managers.");

        // --- 1. Remove from GridManager ---
        GridManager gridManager = GridManager.Instance;
        if (gridManager != null)
        {
            // Clear this unit's tile occupant if found
            foreach (Tile tile in gridManager.grid)
            {
                if (tile != null && tile.occupant == gameObject)
                {
                    tile.occupant = null;
                    break;
                }
            }
        }

        // --- 2. Remove from GameManager lists ---
        if (GameManager.Instance != null)
        {
            if (gameObject.CompareTag("Enemy"))
            {
                gameObject.SetActive(false);
                //for adding gold//
                GameManager gameManager = GameManager.Instance;
                gameManager.ChangeCurrency(50 + Random.Range(50 , 50 + 20 * gameManager.roundNo), true);
                if (Random.Range(0,300) == 0) gameManager.ChangeCurrency(1,false);
                Debug.Log("Added Currency ");

            }
            else if (gameObject.CompareTag("Player"))
            {
                Debug.Log("Player dead lul");
            }

            // Check game win/loss state
            GameManager.Instance.CheckGameState();
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
