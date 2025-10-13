using JetBrains.Annotations;
using UnityEditor.Experimental.GraphView;
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

    public void StatusDamage(string name, int duration, int dot)
    {
        Debug.Log("Status name: " + name + "\n Status duration: " + duration + "\n dot: " + dot);
        Debug.Log(name + " applied");


        //apply status dmg like burn here. It will take damage every turn
    }


}
