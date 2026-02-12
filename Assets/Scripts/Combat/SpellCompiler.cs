using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Production Roslyn spell compiler and executor.
/// Unified entry point for all spell code compilation and execution.
/// Features:
/// - Compilation caching by code hash (avoids recompiling identical code)
/// - Sandboxed API surface via SpellGlobals
/// - Error diagnostics surfaced to console
/// - Deterministic execution within turns
/// </summary>
public class SpellCompiler : MonoBehaviour
{
    public static SpellCompiler Instance { get; private set; }

    private Dictionary<int, ScriptRunner<object>> compilationCache
        = new Dictionary<int, ScriptRunner<object>>();

    private ScriptOptions sandboxedOptions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildScriptOptions();
    }

    private void BuildScriptOptions()
    {
        sandboxedOptions = ScriptOptions.Default
            .WithReferences(
                Assembly.GetAssembly(typeof(GameObject)),         // UnityEngine
                Assembly.GetAssembly(typeof(UnityEngine.Object)), // UnityEngine.CoreModule
                typeof(System.Linq.Enumerable).Assembly,          // System.Linq
                Assembly.Load("Assembly-CSharp")                  // Game types
            )
            .WithImports(
                "System",
                "UnityEngine",
                "System.Linq",
                "System.Collections.Generic"
            );
    }

    /// <summary>
    /// Precompile a spell and cache the result.
    /// Runs SpellCodeSanitizer first to fix common LLM mistakes.
    /// Returns true if compilation succeeded.
    /// </summary>
    public bool Precompile(Spell spell)
    {
        if (spell == null || string.IsNullOrWhiteSpace(spell.code))
        {
            Debug.LogWarning("[SpellCompiler] Cannot precompile null/empty spell code.");
            return false;
        }

        // Sanitize code before compilation
        spell.code = SpellCodeSanitizer.Sanitize(spell.code);

        int codeHash = spell.code.GetHashCode();
        if (compilationCache.ContainsKey(codeHash))
            return true;

        try
        {
            var script = CSharpScript.Create(spell.code, sandboxedOptions, typeof(SpellGlobals));
            var diagnostics = script.Compile();

            bool hasErrors = false;
            var errorList = new List<Diagnostic>();
            foreach (var diag in diagnostics)
            {
                if (diag.Severity == DiagnosticSeverity.Error)
                {
                    hasErrors = true;
                    errorList.Add(diag);
                    Debug.LogError($"[SpellCompiler] Compile error in '{spell.name}': {diag.GetMessage()}");
                }
                else if (diag.Severity == DiagnosticSeverity.Warning)
                {
                    Debug.LogWarning($"[SpellCompiler] Warning in '{spell.name}': {diag.GetMessage()}");
                }
            }

            if (hasErrors)
            {
                // Store errors for LLM retry
                LastCompileErrors = SpellCodeSanitizer.CollectCompileErrors(spell.name, errorList.ToArray());
                Debug.LogError($"[SpellCompiler] Spell '{spell.name}' failed to compile after sanitizing.");
                return false;
            }

            compilationCache[codeHash] = script.CreateDelegate();
            LastCompileErrors = null;
            Debug.Log($"[SpellCompiler] Spell '{spell.name}' compiled and cached.");
            return true;
        }
        catch (Exception ex)
        {
            LastCompileErrors = ex.Message;
            Debug.LogError($"[SpellCompiler] Exception compiling '{spell.name}': {ex.Message}");
            return false;
        }
    }

    /// <summary>Last compile error string, available for LLM retry prompts.</summary>
    public string LastCompileErrors { get; private set; }

    /// <summary>
    /// Execute a spell's compiled code. Precompiles if not cached.
    /// </summary>
    public bool Execute(Spell spell, GameObject from, GameObject to)
    {
        if (spell == null || string.IsNullOrWhiteSpace(spell.code))
        {
            Debug.LogWarning("[SpellCompiler] No code to execute.");
            return false;
        }

        int codeHash = spell.code.GetHashCode();

        if (!compilationCache.ContainsKey(codeHash))
        {
            if (!Precompile(spell))
                return false;
        }

        if (!compilationCache.TryGetValue(codeHash, out var runner))
        {
            Debug.LogError($"[SpellCompiler] Cache miss after compile for '{spell.name}'.");
            return false;
        }

        try
        {
            var globals = new SpellGlobals(spell, from, to);
            runner(globals).Wait();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SpellCompiler] Runtime error in '{spell.name}': {ex.Message}");
            return false;
        }
    }

    /// <summary>Clear the compilation cache (e.g. on scene reload).</summary>
    public void ClearCache()
    {
        compilationCache.Clear();
        Debug.Log("[SpellCompiler] Cache cleared.");
    }

    /// <summary>Number of cached compilations.</summary>
    public int CachedCount => compilationCache.Count;
}

/// <summary>
/// Globals exposed to Roslyn spell scripts.
/// This is the sandboxed API surface — spell code can only access these members.
/// Provides safe helper methods so spell scripts don't need direct component access.
/// </summary>
public class SpellGlobals
{
    public Spell spell;
    public GameObject from;
    public GameObject to;

    public SpellGlobals() { }

    public SpellGlobals(Spell spell, GameObject from, GameObject to)
    {
        this.spell = spell;
        this.from = from;
        this.to = to;
    }

    // ── Safe API for spell scripts ──

    /// <summary>Get the caster's Stats.</summary>
    public Stats FromStats => from != null ? from.GetComponent<Stats>() : null;

    /// <summary>Get the target's Stats.</summary>
    public Stats ToStats => to != null ? to.GetComponent<Stats>() : null;

    /// <summary>Deal damage to the target (respects armor).</summary>
    public void DealDamage(int amount) => ToStats?.takeDamage(amount);

    /// <summary>Heal the target (positive amount).</summary>
    public void HealTarget(int amount) => ToStats?.takeDamage(-amount);

    /// <summary>Apply a status effect to the target.</summary>
    public void ApplyStatus(string name, int duration, int dotDamage)
        => ToStats?.StatusDamage(name, duration, dotDamage);

    /// <summary>Deal damage to the caster.</summary>
    public void SelfDamage(int amount) => FromStats?.takeDamage(amount);

    /// <summary>Heal the caster.</summary>
    public void SelfHeal(int amount) => FromStats?.takeDamage(-amount);

    /// <summary>Apply a status effect to the caster.</summary>
    public void ApplyStatusToSelf(string name, int duration, int dotDamage)
        => FromStats?.StatusDamage(name, duration, dotDamage);

    /// <summary>Check if the caster has a specific status.</summary>
    public bool HasStatus(string name) => FromStats != null && FromStats.HasStatus(name);

    /// <summary>Check if the target has a specific status.</summary>
    public bool TargetHasStatus(string name) => ToStats != null && ToStats.HasStatus(name);

    /// <summary>Remove a status from the caster.</summary>
    public void RemoveStatus(string name) => FromStats?.RemoveStatus(name);

    /// <summary>Remove a status from the target.</summary>
    public void RemoveTargetStatus(string name) => ToStats?.RemoveStatus(name);

    /// <summary>Caster current health.</summary>
    public int FromHealth => FromStats?.health ?? 0;

    /// <summary>Target current health.</summary>
    public int ToHealth => ToStats?.health ?? 0;

    /// <summary>Caster max health.</summary>
    public int FromMaxHealth => FromStats?.maxHealth ?? 100;

    /// <summary>Target max health.</summary>
    public int ToMaxHealth => ToStats?.maxHealth ?? 100;

    /// <summary>Caster current resource.</summary>
    public int FromResource => FromStats?.resource ?? 0;

    /// <summary>Caster world position.</summary>
    public Vector3 CasterPosition => from != null ? from.transform.position : Vector3.zero;

    /// <summary>Target world position.</summary>
    public Vector3 TargetPosition => to != null ? to.transform.position : Vector3.zero;

    /// <summary>Random integer in [min, max).</summary>
    public int Roll(int min, int max) => UnityEngine.Random.Range(min, max);

    /// <summary>Grant currency to the player.</summary>
    public void AddCurrency(int amount, bool isGold)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeCurrency(amount, isGold);
    }

    /// <summary>
    /// Spawn a summon entity near the caster on the grid.
    /// AI types: "Aggressive", "Defensive", "Stationary", "Support"
    /// </summary>
    public void RequestSummon(int health = 30, int damage = 10, int duration = 3, int range = 1, string ai = "Aggressive")
    {
        if (from == null) return;
        var grid = GridManager.Instance;
        if (grid == null) { Debug.LogWarning("[SpellGlobals] No GridManager — cannot summon."); return; }

        Vector2Int casterPos = GetGridPosition(from);
        Vector2Int spawnPos = FindAdjacentEmpty(casterPos, grid);
        if (spawnPos.x < 0) { Debug.LogWarning("[SpellGlobals] No empty adjacent tile for summon."); return; }

        // Create summon GameObject with visual
        var go = new GameObject($"{from.name}_Summon");
        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.transform.SetParent(go.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.8f;
        var rend = visual.GetComponent<Renderer>();
        if (rend != null)
        {
            float r = spell != null ? spell.ColorR / 255f : 0f;
            float g = spell != null ? spell.ColorG / 255f : 1f;
            float b = spell != null ? spell.ColorB / 255f : 1f;
            rend.material.color = new Color(r, g, b);
        }
        // Remove capsule collider on visual to avoid physics conflicts
        var capsuleCol = visual.GetComponent<Collider>();
        if (capsuleCol != null) UnityEngine.Object.Destroy(capsuleCol);


        // SummonedEntity has [RequireComponent(typeof(Stats))] so Stats is auto-added
        var summon = go.AddComponent<SummonedEntity>();

        // Configure Stats
        var stats = go.GetComponent<Stats>();
        stats.maxHealth = health;
        stats.health = health;
        stats.maxResource = 50;
        stats.resource = 50;

        // Configure summon
        summon.maxLifetimeTurns = duration;
        summon.attackRange = range;

        // Parse AI type
        SummonAIType aiType = SummonAIType.Aggressive;
        System.Enum.TryParse(ai, true, out aiType);
        summon.aiType = aiType;

        // Create attack/support spell for the summon
        bool isSupport = aiType == SummonAIType.Support;
        var summonSpell = new Spell
        {
            name = isSupport ? "Summon Heal" : "Summon Strike",
            damage = isSupport ? -damage : damage,
            accuracy = 85,
            range = range,
            resourceCost = 0,
            support = isSupport,
            spellType = isSupport ? SpellType.Support : SpellType.Damage,
            code = isSupport ? "HealTarget(-spell.damage);" : "DealDamage(spell.damage);",
            ColorR = spell?.ColorR ?? 0,
            ColorG = spell?.ColorG ?? 255,
            ColorB = spell?.ColorB ?? 255,
            status = "None",
            statusDuration = 0,
            statusDamagePerTurn = 0
        };
        summon.summonSpells = new Spell[] { summonSpell };

        // Precompile summon spell
        if (SpellCompiler.Instance != null)
            SpellCompiler.Instance.Precompile(summonSpell);

        // Get owner ICombatEntity
        ICombatEntity owner = from.GetComponent<PlayerFunctionality>() as ICombatEntity;
        if (owner == null) owner = from.GetComponent<Enemy>() as ICombatEntity;
        if (owner == null) owner = from.GetComponent<SummonedEntity>() as ICombatEntity;

        if (owner != null)
        {
            summon.InitializeSummon(owner, spawnPos, grid);
            Debug.Log($"[SpellGlobals] Summoned '{go.name}' at {spawnPos} (HP={health}, DMG={damage}, AI={ai}, Dur={duration})");
        }
        else
        {
            Debug.LogWarning("[SpellGlobals] Caster has no ICombatEntity — summon created without owner.");
            UnityEngine.Object.Destroy(go);
        }
    }

    private Vector2Int FindAdjacentEmpty(Vector2Int center, GridManager grid)
    {
        Vector2Int[] offsets = {
            Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down,
            new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };
        foreach (var off in offsets)
        {
            Vector2Int pos = center + off;
            Tile tile = grid.GetTile(pos);
            if (tile != null && tile.walkable && tile.occupant == null)
                return pos;
        }
        return new Vector2Int(-1, -1);
    }

    /// <summary>Manhattan distance between caster and target.</summary>
    public int Distance
    {
        get
        {
            var fromPos = GetGridPosition(from);
            var toPos = GetGridPosition(to);
            return Pathfinding.ManhattanDistance(fromPos, toPos);
        }
    }

    private Vector2Int GetGridPosition(GameObject obj)
    {
        if (obj == null) return Vector2Int.zero;
        var pf = obj.GetComponent<PlayerFunctionality>();
        if (pf != null) return pf.gridPosition;
        var enemy = obj.GetComponent<Enemy>();
        if (enemy != null) return enemy.gridPosition;
        var summon = obj.GetComponent<SummonedEntity>();
        if (summon != null) return summon.GridPosition;
        return new Vector2Int(
            Mathf.RoundToInt(obj.transform.position.x),
            Mathf.RoundToInt(obj.transform.position.z));
    }
}
