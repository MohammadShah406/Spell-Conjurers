using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UnityEngine;


public static class RuntimeCompiler
{

    public static async Task<ICustomBehavior> CompileBehavior(string code)
    {
        string wrappedCode = $@"
using UnityEngine;

public class DynamicBehavior : ICustomBehavior
{{
    public void Start(Player player) {{ }}
    public void Update(Player player)
    {{
        {code}
    }}
}}";

        var syntaxTree = CSharpSyntaxTree.ParseText(wrappedCode);

        // References: only Unity & basic assemblies
        var refs = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Player).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Vector3).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(UnityEngine.Input).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(UnityEngine.Time).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(UnityEngine.MonoBehaviour).Assembly.Location)
        };

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
    .Select(a => MetadataReference.CreateFromFile(a.Location))
    .ToList();

        var compilation = CSharpCompilation.Create(
    "DynamicAssembly",
    new[] { syntaxTree },
    assemblies,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var d in result.Diagnostics)
                Debug.LogError(d.ToString());
            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var type = assembly.GetType("DynamicBehavior");
        return Activator.CreateInstance(type) as ICustomBehavior;
    }
}
