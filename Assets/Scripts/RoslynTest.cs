using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using UnityEngine;

public class RoslynTest : MonoBehaviour
{
    async void Start()
    {
        var result = await CSharpScript.EvaluateAsync<int>(
            "1 + 2",
            ScriptOptions.Default
                .WithReferences(typeof(GameObject).Assembly)
                .WithImports("System", "UnityEngine")
        );

        Debug.Log("Roslyn Result = " + result); // should log "3"
    }
}
