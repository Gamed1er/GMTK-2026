using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 一鍵將 Prefab 與當前 Scene 中所有 TextMesh 換成 TextMeshPro（世界空間）
/// 選單：Tools → Replace TextMesh with TMP
/// </summary>
public class TextMeshReplacer : EditorWindow
{
    private TMP_FontAsset targetFont;
    private Color textColor = Color.white;

    [MenuItem("Tools/Replace TextMesh with TMP")]
    public static void ShowWindow()
    {
        GetWindow<TextMeshReplacer>("TextMesh → TMP");
    }

    private void OnGUI()
    {
        GUILayout.Label("將 TextMesh 換成 TextMeshPro（保留原始 Font Size）", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "TMP Font Asset", targetFont, typeof(TMP_FontAsset), false);
        textColor = EditorGUILayout.ColorField("預設文字顏色（原本為黑色時套用）", textColor);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "同時掃描：\n" +
            "① 當前開啟的 Scene 所有 GameObject\n" +
            "② Assets 內所有 Prefab\n\n" +
            "Font Size 保留原始 TextMesh 設定。\n" +
            "操作不可復原，請先 commit！",
            MessageType.Warning);

        EditorGUILayout.Space();

        if (GUILayout.Button("開始替換", GUILayout.Height(36)))
        {
            if (targetFont == null)
            {
                EditorUtility.DisplayDialog("缺少字體", "請先指定 TMP Font Asset！", "OK");
                return;
            }
            int sceneCount = ReplaceInCurrentScene();
            int prefabCount = ReplaceInPrefabs(out int prefabsModified);
            EditorUtility.DisplayDialog(
                "完成",
                $"Scene 替換：{sceneCount} 個\n" +
                $"Prefab 替換：{prefabCount} 個（共 {prefabsModified} 個 Prefab）",
                "OK");
        }
    }

    // ── Scene ──────────────────────────────────────────────

    private int ReplaceInCurrentScene()
    {
        int count = 0;
        Scene scene = SceneManager.GetActiveScene();

        // GetRootGameObjects 取得所有頂層物件，再往下找
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            TextMesh[] meshes = root.GetComponentsInChildren<TextMesh>(true);
            foreach (TextMesh tm in meshes)
            {
                ReplaceComponent(tm);
                count++;
            }
        }

        if (count > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"[TextMeshReplacer] Scene 替換完成：{count} 個");
        }
        return count;
    }

    // ── Prefabs ────────────────────────────────────────────

    private int ReplaceInPrefabs(out int prefabsModified)
    {
        prefabsModified = 0;
        int count = 0;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            bool modified = false;

            TextMesh[] meshes = prefab.GetComponentsInChildren<TextMesh>(true);
            foreach (TextMesh tm in meshes)
            {
                ReplaceComponent(tm);
                count++;
                modified = true;
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                prefabsModified++;
                Debug.Log($"[TextMeshReplacer] Prefab 替換：{path}");
            }

            PrefabUtility.UnloadPrefabContents(prefab);
        }

        AssetDatabase.Refresh();
        return count;
    }

    // ── Core ───────────────────────────────────────────────

    private void ReplaceComponent(TextMesh tm)
    {
        // 備份原始資料
        string savedText  = tm.text;
        float  savedSize  = tm.fontSize > 0 ? tm.fontSize : 24f; // fontSize 0 = 預設
        Color  savedColor = tm.color;
        GameObject go = tm.gameObject;

        DestroyImmediate(tm);

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text      = savedText;
        tmp.font      = targetFont;
        tmp.fontSize  = savedSize;   // 保留原始字號
        tmp.color     = savedColor;
        tmp.alignment = TextAlignmentOptions.Center;
    }
}
