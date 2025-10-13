using UnityEngine;

public class Stats : MonoBehaviour
{
    public int health = 10;
    public int armor = 0;

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
        if(health <= 0)
        {
            Debug.Log("Gameobject " + gameObject.name + " died");
        }
    }
}
