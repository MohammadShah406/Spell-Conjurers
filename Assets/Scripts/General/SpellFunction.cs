using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System;
using System.Linq;
using System.Reflection;

public class SpellFunction : MonoBehaviour
{
    public static SpellFunction Instance { get; private set; }

    public Spell spell;
    public GameObject from;
    public GameObject to;

    private void Awake()
    {
        Instance = this;
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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
        string code = spell.code;

        try
        {
            // Get the main Unity game assembly
            var gameAssembly = Assembly.Load("Assembly-CSharp");

            var options = ScriptOptions.Default
                .WithReferences(
                    Assembly.GetAssembly(typeof(UnityEngine.GameObject)),  // Unity
                    Assembly.GetAssembly(typeof(UnityEngine.Object)),      // UnityEngine types
                    typeof(System.Linq.Enumerable).Assembly,               // System.Linq
                    gameAssembly                                           // (Stats, Player, etc.)
                )
                .WithImports(
                    "System",
                    "UnityEngine",
                    "System.Linq",
                    "System.Collections.Generic"
                );

            var globals = new SpellGlobals
            {
                spell = this.spell,
                from = this.from,
                to = this.to
            };


            var script = CSharpScript.Create(code, options, typeof(SpellGlobals));
            Debug.Log("Attempting to compile: \n" + script);
            script.Compile();
            var result = script.RunAsync(globals).Result;
        }
        catch (CompilationErrorException compilationException)
        {
            foreach (var diagnostic in compilationException.Diagnostics)
                Debug.LogError($"Roslyn error: {diagnostic.GetMessage()}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error running script: {ex.Message}");
        }
    }
}

public class SpellGlobals
{
    public Spell spell;
    public GameObject from;
    public GameObject to;
}
