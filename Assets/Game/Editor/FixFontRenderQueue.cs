// Assets/Editor/FixFontRenderQueue.cs
using UnityEditor;
using UnityEngine;

public class FixFontRenderQueue
{
    [MenuItem("Tools/Fix IBM Plex Mono Render Queue")]
    static void Fix()
	{
		var guids = AssetDatabase.FindAssets("IBM Plex Mono t:Material");
		int count = 0;

		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (mat == null) continue;

			if (mat.shader.name.Contains("Overlay"))
			{
				var newShader = Shader.Find("TextMeshPro/Distance Field");
				if (newShader != null)
				{
					mat.shader = newShader;
					EditorUtility.SetDirty(mat);
					Debug.Log($"Fixed: {mat.name}");
					count++;
				}
			}
		} 

		AssetDatabase.SaveAssets();
		Debug.Log($"Done. Fixed {count} materials.");
	}
}   