using UnityEngine;
using UnityEditor;
using System.IO;

public class MaterialNormalFixer : EditorWindow
{
    [MenuItem("Tools/머테리얼 NormalMap 자동 수정")]
    public static void FixAllNormalMaps()
    {
        string materialFolder = "Assets/Prefab/Map/MATERIALS";
        string textureFolder = "Assets/Prefab/Map/Texture";

        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { materialFolder });

        foreach (string guid in matGuids)
        {
            string matPath = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) continue;

            Texture baseMap = mat.GetTexture("_BaseMap");
            Texture bumpMap = mat.GetTexture("_BumpMap");

            // 조건: BumpMap이 기본값이거나 BaseMap과 동일
            if (bumpMap == null || bumpMap == baseMap)
            {
                string baseMapPath = AssetDatabase.GetAssetPath(baseMap);
                if (string.IsNullOrEmpty(baseMapPath)) continue;

                string normalName = Path.GetFileNameWithoutExtension(baseMapPath) + "_NM.jpg";
                string normalPath = Path.Combine(textureFolder, normalName);

                // 복사 필요시
                if (!File.Exists(normalPath))
                {
                    File.Copy(baseMapPath, normalPath);
                    Debug.Log($"📦 복사된 NormalMap: {normalPath}");
                    AssetDatabase.ImportAsset(normalPath);
                }

                SetTextureAsNormalMap(normalPath);

                Texture2D newNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (newNormal != null)
                {
                    mat.SetTexture("_BumpMap", newNormal);
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetFloat("_BumpScale", 1f); // ✅ 강도 설정
                    Debug.Log($"✅ NormalMap 교체됨: {mat.name}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("🎉 모든 머테리얼의 NormalMap 자동 수정 완료!");
    }

    static void SetTextureAsNormalMap(string path)
    {
        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
        if (importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }
}
