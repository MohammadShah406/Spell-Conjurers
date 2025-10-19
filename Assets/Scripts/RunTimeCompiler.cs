using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil.Cil;
using UnityEngine;


public static class RuntimeCompiler
{

    public static Type CompileType(string code, string className = "DynamicBehavior")
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            Debug.LogError("Cannot compile empty code.");
            return null;
        }

        code = SanitizeLLMScript(code);

        bool isAlreadyMonoBehaviour =
            code.Contains("class ") && code.Contains(":") && code.Contains("MonoBehaviour");

        // Wrap if it's just a snippet, otherwise compile raw
        string finalCode = isAlreadyMonoBehaviour
            ? code
            : $@"
using UnityEngine;
using System.Collections;

public class {className} : MonoBehaviour
{{
    void Start() {{ }}
    void Update() {{ Run(); }}

    void Run()
    {{
{IndentLines(code, 8)}
    }}
}}";

        var syntaxTree = CSharpSyntaxTree.ParseText(finalCode);

        // Collect all loaded assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        // Ensure Unity + System assemblies are present
        var requiredAssemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(UnityEngine.MonoBehaviour).Assembly,
            typeof(UnityEngine.GameObject).Assembly,
            typeof(System.Collections.IEnumerable).Assembly
        };

        foreach (var asm in requiredAssemblies)
        {
            var reference = MetadataReference.CreateFromFile(asm.Location);
            if (!assemblies.Any(a => a.Display == reference.Display))
                assemblies.Add(reference);
        }

        var compilation = CSharpCompilation.Create(
            "RuntimeGeneratedAssembly",
            new[] { syntaxTree },
            assemblies,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var diag in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                Debug.LogError("[Roslyn Error] " + diag.ToString());
            Debug.LogError("Compilation failed. Source:\n" + finalCode);
            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());

        // Try to find the compiled MonoBehaviour
        var type = assembly.GetType(className)
            ?? assembly.GetTypes().FirstOrDefault(t => typeof(MonoBehaviour).IsAssignableFrom(t));

        if (type == null)
            Debug.LogError("No MonoBehaviour type found in compiled code.");

        return type;
    }

    /// <summary>
    /// Creates a new GameObject and attaches the compiled MonoBehaviour.
    /// </summary>
    public static GameObject InstantiateRuntimeBehavior(string code, string className = "DynamicBehavior")
    {
        Type compiledType = CompileType(code, className);
        if (compiledType == null)
        {
            Debug.LogError("Failed to compile runtime script.");
            return null;
        }

        if (!typeof(MonoBehaviour).IsAssignableFrom(compiledType))
        {
            Debug.LogError($"Compiled type {compiledType.Name} is not a MonoBehaviour.");
            return null;
        }

        GameObject go = new GameObject(className);
        go.AddComponent(compiledType);
        Debug.Log($"Created GameObject '{className}' with attached '{compiledType.Name}' script.");
        return go;
    }

    // ---------- Utility helpers ----------
    private static string SanitizeLLMScript(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "";

        code = code.Replace("“", "\"").Replace("”", "\"");
        code = new string(code.Where(c => !char.IsControl(c) || c == '\n' || c == '\r').ToArray());

        if (!code.EndsWith("\n"))
            code += "\n";

        return code;
    }

    private static string IndentLines(string text, int spaces)
    {
        string indent = new string(' ', spaces);
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            lines[i] = indent + lines[i];
        return string.Join("\n", lines);
    }
}
