using UnityEngine;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Scripting;

/// <summary>
/// Legacy compatibility shim. Delegates all functionality to SpellCompiler.
/// Maintained so existing scene references and serialized UnityEvents continue to work.
/// New code should use SpellCompiler directly.
/// </summary>
public class SpellFunction : MonoBehaviour
{
    public static SpellFunction Instance { get; private set; }

    public Spell spell;
    public GameObject from;
    public GameObject to;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetVariables(Spell spell, GameObject from, GameObject to)
    {
        this.spell = spell;
        this.from = from;
        this.to = to;
    }

    public void CustomSpellFunction()
    {
        if (spell == null) return;
        SpellCompiler.Instance?.Execute(spell, from, to);
    }

    public void PrecompileSpell(Spell spell)
    {
        if (spell == null || string.IsNullOrWhiteSpace(spell.code)) return;
        SpellCompiler.Instance?.Precompile(spell);
    }

    public void UsePrecompiledSpell(Spell spell, GameObject from, GameObject to)
    {
        SetVariables(spell, from, to);
        CustomSpellFunction();
    }
}
