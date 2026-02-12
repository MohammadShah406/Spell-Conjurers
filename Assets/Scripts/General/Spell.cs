using UnityEngine;

/// <summary>
/// Data-driven spell definition. Loaded from JSON, supports Roslyn code execution.
/// All fields are serializable for JSON and Unity Inspector compatibility.
/// </summary>
[System.Serializable]
public class Spell
{
    [Header("Identity")]
    public string name;
    public string description;

    [Header("Combat Stats")]
    public int damage;
    public int accuracy;
    public int resourceCost;
    public int range;
    public int selfDamage;

    [Header("Classification")]
    public bool support;
    public SpellType spellType;

    [Header("Roslyn Code")]
    public string code;

    [Header("Visuals")]
    public float ColorR;
    public float ColorG;
    public float ColorB;

    [Header("Status Effect")]
    public string status;
    public int statusDuration;
    public int statusDamagePerTurn;

    [Header("Cooldown")]
    public int cooldown;

    [Header("Summoning")]
    public string summonPrefabId;
    public int summonDuration;

    /// <summary>Constructed color from RGB fields.</summary>
    public Color SpellColor => new Color(ColorR, ColorG, ColorB);
}

/// <summary>
/// Classification of spell behavior for AI and UI purposes.
/// </summary>
public enum SpellType
{
    Damage,
    Support,
    Summon,
    Utility
}
