using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Pre-processes LLM-generated spell code before Roslyn compilation.
/// Rewrites common invalid patterns into valid SpellGlobals API calls
/// so spells compile without needing a fallback or LLM retry.
/// </summary>
public static class SpellCodeSanitizer
{
    /// <summary>
    /// Sanitize spell code by rewriting forbidden patterns into valid SpellGlobals calls.
    /// Returns the cleaned code string ready for Roslyn compilation.
    /// </summary>
    public static string Sanitize(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return code;

        // Normalize line endings
        code = code.Replace("\r\n", "\n").Replace("\r", "\n");

        // ── Phase 1: Remove declarations that break Roslyn scripting context ──
        // Remove "string actionLog = ...;" declarations — actionLog is not in SpellGlobals
        code = Regex.Replace(code, @"string\s+actionLog\s*=\s*""[^""]*""\s*;", "", RegexOptions.Singleline);

        // Remove all actionLog += ... lines (log appending is not part of the API)
        code = Regex.Replace(code, @"actionLog\s*\+=\s*[^;]+;", "", RegexOptions.Singleline);

        // ── Phase 2: Rewrite direct GetComponent<Stats>() calls to helper methods ──

        // to.GetComponent<Stats>().takeDamage(X) → DealDamage(X)  (positive = damage)
        // But if the argument is negative or starts with -, it's healing → HealTarget(abs)
        code = Regex.Replace(code,
            @"to\.GetComponent<Stats>\(\)\.takeDamage\((-[^)]+)\)",
            m => $"HealTarget({m.Groups[1].Value.TrimStart('-')})",
            RegexOptions.None);
        code = Regex.Replace(code,
            @"to\.GetComponent<Stats>\(\)\.takeDamage\(([^)]+)\)",
            "DealDamage($1)");

        // from.GetComponent<Stats>().takeDamage(X) → SelfDamage(X)
        code = Regex.Replace(code,
            @"from\.GetComponent<Stats>\(\)\.takeDamage\((-[^)]+)\)",
            m => $"SelfHeal({m.Groups[1].Value.TrimStart('-')})",
            RegexOptions.None);
        code = Regex.Replace(code,
            @"from\.GetComponent<Stats>\(\)\.takeDamage\(([^)]+)\)",
            "SelfDamage($1)");

        // to.GetComponent<Stats>().StatusDamage(...) → ApplyStatus(...)
        code = Regex.Replace(code,
            @"to\.GetComponent<Stats>\(\)\.StatusDamage\(([^)]+)\)",
            "ApplyStatus($1)");

        // from.GetComponent<Stats>().StatusDamage(...) → ApplyStatusToSelf(...)
        code = Regex.Replace(code,
            @"from\.GetComponent<Stats>\(\)\.StatusDamage\(([^)]+)\)",
            "ApplyStatusToSelf($1)");

        // ── Phase 3: Remove forbidden singleton/manager access ──
        foreach (var entry in ForbiddenManagerRewrites)
        {
            code = Regex.Replace(code, entry.Key, entry.Value, RegexOptions.Singleline);
        }

        // Catch-all: remove any remaining lines with forbidden singletons
        code = Regex.Replace(code,
            @"[^\n]*(?:ActionManager|TurnManager|CombatTurnManager|EnemyManager|UIController|SummonRegistry)\s*\.\s*Instance[^\n]*\n?",
            "// removed forbidden manager access\n");

        // ── Phase 4: Remove forbidden API calls ──

        // Instantiate / Destroy (outside RequestSummon)
        code = Regex.Replace(code, @"(?:Object\.)?(?:UnityEngine\.Object\.)?(?:Instantiate|Destroy)\s*\([^)]*\)\s*;", "// removed forbidden Instantiate/Destroy");

        // Find / FindObjectOfType
        code = Regex.Replace(code, @"(?:GameObject\.)?Find(?:ObjectOfType|WithTag|GameObjectWithTag)?\s*[<(][^;]*;", "// removed forbidden Find call");

        // AddComponent (except Stats which is handled by RequestSummon internally)
        code = Regex.Replace(code, @"[^\n]*\.AddComponent\s*<[^>]+>\s*\([^)]*\)\s*;", "// removed forbidden AddComponent");

        // StartCoroutine
        code = Regex.Replace(code, @"[^\n]*StartCoroutine\s*\([^)]*\)\s*;", "// removed forbidden StartCoroutine");

        // GetComponent for anything other than Stats
        code = Regex.Replace(code,
            @"\.GetComponent<(?!Stats\b)[A-Za-z]+>\(\)",
            ".GetComponent<Stats>()");

        // ── Phase 5: Rewrite common LLM mistakes ──

        // UnityEngine.Random.Range(a, b) → Roll(a, b)
        code = Regex.Replace(code,
            @"UnityEngine\.Random\.Range\(([^)]+)\)",
            "Roll($1)");
        code = Regex.Replace(code,
            @"(?<!UnityEngine\.)Random\.Range\(([^)]+)\)",
            "Roll($1)");

        // from.name → from.name (this is actually valid on GameObject, keep it)
        // to.name → to.name (valid)

        // GameManager.Instance.currencyData.gold += X → AddCurrency(X, true)
        code = Regex.Replace(code,
            @"GameManager\.Instance\.(?:getcurrencyData|currencyData)\.gold\s*\+=\s*([^;]+);",
            "AddCurrency($1, true);");
        code = Regex.Replace(code,
            @"GameManager\.Instance\.(?:getcurrencyData|currencyData)\.manaStone\s*\+=\s*([^;]+);",
            "AddCurrency($1, false);");
        // Read-only currency access — remove assignments that go the other way
        code = Regex.Replace(code,
            @"GameManager\.Instance\.(?:getcurrencyData|currencyData)\.[a-zA-Z]+\s*[-+*/]?=\s*[^;]+;",
            "// removed direct currency mutation");

        // GameManager.Instance.ChangeCurrency(X, bool) → AddCurrency(X, bool)
        code = Regex.Replace(code,
            @"GameManager\.Instance\.ChangeCurrency\(([^)]+)\)",
            "AddCurrency($1)");

        // ── Phase 6: Clean up empty lines and comments-only residue ──
        code = Regex.Replace(code, @"^\s*//\s*removed[^\n]*\n", "", RegexOptions.Multiline);
        code = Regex.Replace(code, @"\n{3,}", "\n\n");
        code = code.Trim();

        // ── Phase 7: Safety net — if code is now empty, generate a type-appropriate default ──
        if (string.IsNullOrWhiteSpace(code))
        {
            code = "DealDamage(spell.damage);";
        }

        return code;
    }

    /// <summary>
    /// Collects all Roslyn compile error messages from a spell into a single string.
    /// Used for LLM retry prompts.
    /// </summary>
    public static string CollectCompileErrors(string spellName, Microsoft.CodeAnalysis.Diagnostic[] diagnostics)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var diag in diagnostics)
        {
            if (diag.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                sb.AppendLine($"  - {diag.GetMessage()}");
        }
        return sb.ToString();
    }

    // ── Pattern → Replacement map for singleton manager calls ──
    private static readonly Dictionary<string, string> ForbiddenManagerRewrites = new Dictionary<string, string>
    {
        // EndTurn / EndPlayerTurn calls
        { @"[^\n]*(?:ActionManager|TurnManager|CombatTurnManager)\s*\.\s*Instance\s*\.(?:EndTurn|EndPlayerTurn|WaitButtonPressed)\s*\([^)]*\)\s*;",
          "" },

        // GridManager.Instance.GetTile / grid access (not allowed in spell code)
        { @"[^\n]*GridManager\s*\.\s*Instance\s*\.\s*(?:GetTile|grid)\s*[\[(][^\n]*\n?",
          "" },
    };
}
