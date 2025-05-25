using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class FBX_EmbeddedMaterialFixer : EditorWindow
{
    [MenuItem("Tools/FBX → 머티리얼 추출 및 정리")]
    public static void FixEmbeddedMaterials()
    {
        string fbxFolder = "Assets/Prefab/Map";
        string texFolder = "Assets/Prefab/Map/Texture";
        string matFolder = "Assets/Prefab/Map/MATERIALS";

        EnsureFolderExists(matFolder);

        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { fbxFolder });
        foreach (string guid in guids)
        {
            string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null) continue;

            Renderer[] renderers = fbx.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rend in renderers)
            {
                Material sourceMat = rend.sharedMaterial;
                if (sourceMat == null || sourceMat.shader.name.Contains("Default")) continue;

                string matName = sourceMat.name;
                string matPath = $"{matFolder}/{matName}.mat";

                // 이미 생성된 경우 불러오기
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.name = matName;
                    string upperName = matName.ToUpper();

                    if (upperName.Contains("GLASS") || upperName.Contains("WINDOW"))
                    {
                        mat.SetFloat("_Surface", 1); // Transparent
                        mat.SetOverrideTag("RenderType", "Transparent");
                        mat.SetInt("_ZWrite", 0);
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                        Color baseColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
                        baseColor.a = 0.3f;
                        mat.SetColor("_BaseColor", baseColor);

                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                    }

                    // 복사된 텍스처가 필요할 경우
                    Texture baseMap = sourceMat.GetTexture("_BaseMap");
                    Texture bumpMap = sourceMat.GetTexture("_BumpMap");

                    if (baseMap != null)
                        mat.SetTexture("_BaseMap", baseMap);

                    if (bumpMap != null)
                    {
                        if (baseMap == bumpMap && baseMap is Texture2D tex2D)
                        {
                            string originPath = AssetDatabase.GetAssetPath(tex2D);
                            string normalPath = CreateNormalMapCopy(originPath, texFolder);
                            Texture2D newBump = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                            SetTextureAsNormalMap(normalPath);
                            mat.SetTexture("_BumpMap", newBump);
                        }
                        else
                        {
                            mat.SetTexture("_BumpMap", bumpMap);
                        }

                        mat.EnableKeyword("_NORMALMAP");
                    }

                    AssetDatabase.CreateAsset(mat, matPath);
                    Debug.Log($"✅ 생성된 머티리얼: {mat.name}");
                }

                // 실제 적용
                rend.sharedMaterial = mat;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("🎉 FBX 내장 머티리얼 → .mat 자동 변환 및 연결 완료!");
    }

    static string CreateNormalMapCopy(string sourcePath, string targetFolder)
    {
        string filename = Path.GetFileNameWithoutExtension(sourcePath);
        string newPath = $"{targetFolder}/{filename}_Normal.jpg";
        if (!File.Exists(newPath))
        {
            File.Copy(sourcePath, newPath);
            AssetDatabase.ImportAsset(newPath);
        }
        return newPath;
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

    static void EnsureFolderExists(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
