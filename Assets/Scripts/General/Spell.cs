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
    public string code; // for function

    public string spellVisualType;
    public string status;
    public int statusDuration;
    public int statusDamagePerTurn;

    public string target;       
    public string scriptName;   
    public string script;       // full C# MonoBehaviour code
}
