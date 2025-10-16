using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

public class SpellFunction : MonoBehaviour
{
    private Dictionary<Spell, ScriptRunner<object>> compiledSpells = new Dictionary<Spell, ScriptRunner<object>>();

    public static SpellFunction Instance { get; private set; }

    public Spell spell;
    public GameObject from;
    public GameObject to;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetVariables(Spell spell, GameObject from, GameObject to)
    {
        this.spell = spell;
        this.from = from;
        this.to = to;
    }

    public void CustomSpellFunction()
    {
        if (spell == null)
        {
            Debug.LogWarning("No spell selected!");
            return;
        }

        // Check if spell is precompiled
        if (compiledSpells.TryGetValue(spell, out var runner))
        {
            // Already compiled, just run it
            var globals = new SpellGlobals { spell = spell, from = from, to = to };
            runner(globals).Wait();
        }
        else
        {
            // Not compiled yet, precompile and run
            PrecompileSpell(spell);

            if (compiledSpells.TryGetValue(spell, out runner))
            {
                var globals = new SpellGlobals { spell = spell, from = from, to = to };
                runner(globals).Wait();
            }
            else
            {
                Debug.LogError($"Failed to compile spell '{spell.name}'");
            }
        }
    }

    /// <summary>
    /// Precompiles a spell and stores its runner for instant execution later.
    /// </summary>
    public void PrecompileSpell(Spell spell)
    {
        if (spell == null)
        {
            Debug.LogWarning("Cannot precompile a null spell.");
            return;
        }

        // Skip if already compiled
        if (compiledSpells.ContainsKey(spell))
            return;

        try
        {
            var options = ScriptOptions.Default
                .WithReferences(
                    Assembly.GetAssembly(typeof(GameObject)),          // UnityEngine.GameObject
                    Assembly.GetAssembly(typeof(UnityEngine.Object)), // UnityEngine.Object
                    typeof(System.Linq.Enumerable).Assembly,         // System.Linq
                    Assembly.Load("Assembly-CSharp")                  // Your game assembly (Stats, Player, etc.)
                )
                .WithImports(
                    "System",
                    "UnityEngine",
                    "System.Linq",
                    "System.Collections.Generic"
                );

            // Create the script
            var script = CSharpScript.Create(spell.code, options, typeof(SpellGlobals));

            // Compile only to check for errors
            var compilationDiagnostics = script.Compile();

            // Check for errors
            bool hasErrors = false;
            foreach (var diag in compilationDiagnostics)
            {
                if (diag.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                {
                    hasErrors = true;
                    Debug.LogError($"[Spell Compilation Error] Spell '{spell.name}': {diag.GetMessage()}");
                }
            }

            if (hasErrors)
            {
                Debug.LogWarning($"Spell '{spell.name}' was not compiled due to errors.");
                return; // Stop here, don't store an invalid runner
            }

            // Create and store delegate
            var runner = script.CreateDelegate();
            compiledSpells[spell] = runner;

            Debug.Log($"Spell '{spell.name}' successfully precompiled!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error while precompiling spell '{spell.name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a precompiled spell.
    /// </summary>
    public void UsePrecompiledSpell(Spell spell, GameObject from, GameObject to)
    {
        if (!compiledSpells.TryGetValue(spell, out var runner))
        {
            Debug.LogWarning($"Spell '{spell.name}' not precompiled, compiling now...");
            PrecompileSpell(spell);
            runner = compiledSpells[spell];
        }

        var globals = new SpellGlobals { spell = spell, from = from, to = to };
        runner(globals).Wait();
    }
}

public class SpellGlobals
{
    public Spell spell;
    public GameObject from;
    public GameObject to;
}
