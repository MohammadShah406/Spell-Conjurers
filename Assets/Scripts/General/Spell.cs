using UnityEngine;

[System.Serializable]
public class Spell
{
    public int damage;
    public int accuracy;
    public int resourceCost;
    public int range;
    public int selfDamage;
    public bool support;
    public string name;
    public string description;
    public string code;

    public float ColorR;
    public float ColorG;
    public float ColorB;
    public string status;
    public int statusDuration;
    public int statusDamagePerTurn;

}
