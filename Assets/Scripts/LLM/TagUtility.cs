#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.IO;
using System.Linq;
using System.Threading;

public static class TagUtility
{
#if UNITY_EDITOR
    /// <summary>
    /// Ensures that the specified tag exists in the TagManager.
    /// </summary>
    public static void EnsureTagExists(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
        );
        var tagsProp = tagManager.FindProperty("tags");

        bool exists = Enumerable.Range(0, tagsProp.arraySize)
            .Any(i => tagsProp.GetArrayElementAtIndex(i).stringValue == tag);

        if (!exists)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            tagsProp.GetArrayElementAtIndex(0).stringValue = tag;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created new tag: {tag}");
        }
    }

    /// <summary>
    /// Creates a prefab with the given name and tag (if possible). 
    /// Returns an instantiated object.
    /// </summary>
    public static GameObject EnsurePrefabExists(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        // Step 1: Make sure tag exists (Editor only)
        EnsureTagExists(name);

        string folderPath = "Assets/Resources/GeneratedPrefabs";
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string prefabPath = $"{folderPath}/{name}.prefab";

        // Step 2: Try to load prefab if already exists
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            Debug.Log($"Prefab already exists: {prefabPath}");
            return Object.Instantiate(prefab);
        }

        // Step 3: Create temporary GameObject
        GameObject temp = new GameObject(name);

        // Give it a visible cube shape (optional)
        var filter = temp.AddComponent<MeshFilter>();
        var renderer = temp.AddComponent<MeshRenderer>();
        filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        renderer.sharedMaterial = new Material(Shader.Find("Standard"));

        // Step 4: Try to assign tag safely
        bool tagAssigned = false;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                temp.tag = name;
                tagAssigned = true;
                break;
            }
            catch
            {
                Thread.Sleep(200); // wait and retry
                AssetDatabase.Refresh();
            }
        }

        if (!tagAssigned)
        {
            Debug.LogWarning($"Could not assign tag '{name}'. Prefab will use 'Untagged'.");
            temp.tag = "Untagged";
        }

        // Step 5: Save prefab asset
        PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        Object.DestroyImmediate(temp);

        AssetDatabase.Refresh();
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        Debug.Log($"Created new prefab: {prefabPath}");
        return Object.Instantiate(prefab);
    }
#else
    // RUNTIME fallback (non-editor build)
    public static GameObject EnsurePrefabExists(string name)
    {
        // Try to load from Resources if it exists
        var prefab = Resources.Load<GameObject>($"GeneratedPrefabs/{name}");
        if (prefab != null)
            return Object.Instantiate(prefab);

        // Otherwise, create a default cube GameObject at runtime
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Debug.LogWarning($"Runtime fallback: Created default cube for '{name}'");
        return go;
    }
#endif
}
