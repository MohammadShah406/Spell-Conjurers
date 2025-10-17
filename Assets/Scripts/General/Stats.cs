using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour
{
    public int health = 10;
    public int armor = 0;

    public List<Status> statuses = new List<Status>();

    public GameObject floatingTextPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void takeDamage(int damage)
    {
        health -= damage;
        if (damage >= 0)
        {
            ShowFloatingText(damage.ToString(), Color.red);
        }
        else
        {
            ShowFloatingText(damage.ToString(), Color.green);
        }

        if (health <= 0)
        {
            Debug.Log("Gameobject " + gameObject.name + " died");
        }

    }

    public void takeDamage(int damage, Color color)
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
                takeDamage(s.dotDamage, Color.yellow);
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
