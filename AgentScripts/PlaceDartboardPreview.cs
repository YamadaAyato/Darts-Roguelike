using System;
using System.Collections.Generic;
using System.Reflection;
using DartsRoguelike.Core.Model;
using DartsRoguelike.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
///     開発用シーンに3D盤面の表示確認用オブジェクトを配置する
/// </summary>
public static class PlaceDartboardPreview
{
    private const string SCENE_PATH = "Assets/Level/Scenes/Development/DartThrowSandbox.unity";
    private const string ASSET_FOLDER = "Assets/Art/Materials/Test/DartThrowSandbox";
    private static readonly int[] NUMBERS =
        { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };

    /// <summary>
    ///     既存シーンを確認して盤面と表示参照を追加する
    /// </summary>
    public static string Build()
    {
        UnityEngine.SceneManagement.Scene scene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH || scene.isDirty || Application.isPlaying)
        {
            throw new InvalidOperationException("保存済みのDartThrowSandboxを編集モードで開いてください");
        }

        if (GameObject.Find("DartboardPreview") != null)
        {
            throw new InvalidOperationException("検証用盤面はすでに配置されています");
        }

        EnsureFolder("Assets/Art/Materials/Test");
        EnsureFolder(ASSET_FOLDER);
        Material dark = CreateMaterial("Dark", new Color(0.055f, 0.07f, 0.09f));
        Material cream = CreateMaterial("Cream", new Color(0.86f, 0.82f, 0.68f));
        Material red = CreateMaterial("Red", new Color(0.72f, 0.08f, 0.13f));
        Material green = CreateMaterial("Green", new Color(0.05f, 0.43f, 0.32f));
        Material cyan = CreateMaterial("Aim", new Color(0.15f, 0.95f, 1f));
        Material yellow = CreateMaterial("Landing", new Color(1f, 0.7f, 0.12f));

        var root = new GameObject("DartboardPreview");
        Undo.RegisterCreatedObjectUndo(root, "Place 3D dartboard preview");
        root.transform.position = new Vector3(0f, 1.25f, 0f);
        root.transform.rotation = Quaternion.Euler(0f, -12f, 0f);
        DartboardSurface surface = root.AddComponent<DartboardSurface>();
        SetFloat(surface, "_radius", 1f);

        GameObject backing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        backing.name = "BoardBacking";
        backing.transform.SetParent(root.transform, false);
        backing.transform.localPosition = new Vector3(0f, 0f, 0.06f);
        backing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        backing.transform.localScale = new Vector3(2.42f, 0.05f, 2.42f);
        backing.GetComponent<Renderer>().sharedMaterial = dark;
        UnityEngine.Object.DestroyImmediate(backing.GetComponent<Collider>());

        var visual = new GameObject("BoardVisual", typeof(MeshFilter), typeof(MeshRenderer));
        visual.transform.SetParent(root.transform, false);
        Mesh mesh = CreateBoardMesh();
        AssetDatabase.CreateAsset(mesh, ASSET_FOLDER + "/StandardBoard.asset");
        visual.GetComponent<MeshFilter>().sharedMesh = mesh;
        visual.GetComponent<MeshRenderer>().sharedMaterials =
            new[] { dark, cream, red, green };
        CreateNumbers(root.transform);

        var circleObject = new GameObject("AimCircle", typeof(LineRenderer));
        circleObject.transform.SetParent(root.transform, false);
        LineRenderer circle = circleObject.GetComponent<LineRenderer>();
        circle.useWorldSpace = true;
        circle.loop = true;
        circle.widthMultiplier = 0.012f;
        circle.numCornerVertices = 3;
        circle.alignment = LineAlignment.View;
        circle.sharedMaterial = cyan;
        circle.startColor = Color.white;
        circle.endColor = Color.white;
        circle.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        circle.receiveShadows = false;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "LandingMarker";
        marker.transform.SetParent(root.transform, false);
        marker.transform.localScale = Vector3.one * 0.045f;
        marker.GetComponent<Renderer>().sharedMaterial = yellow;
        UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());

        DartAimView view = root.AddComponent<DartAimView>();
        var serializedView = new SerializedObject(view);
        serializedView.FindProperty("_surface").objectReferenceValue = surface;
        serializedView.FindProperty("_aimCircle").objectReferenceValue = circle;
        serializedView.FindProperty("_landingMarker").objectReferenceValue = marker.transform;
        serializedView.ApplyModifiedPropertiesWithoutUndo();

        Camera camera = Camera.main;
        if (camera == null)
        {
            throw new InvalidOperationException("Main Cameraが見つかりません");
        }

        Undo.RecordObject(camera.transform, "Frame dartboard preview");
        Undo.RecordObject(camera, "Frame dartboard preview");
        camera.transform.position = new Vector3(0f, 1.25f, -4.2f);
        camera.transform.rotation = Quaternion.identity;
        camera.orthographic = false;
        camera.fieldOfView = 40f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.018f, 0.025f, 0.045f);

        // 編集モードでも配置結果を見られるように、一度だけサンプル表示する
        typeof(DartAimView).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(view, null);
        ShowSample(view);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;
        return "Placed 3D board, aim circle, landing marker and references in " + scene.path;
    }

    /// <summary>
    ///     再生中のViewと座標変換を確認し、サンプルを表示する
    /// </summary>
    public static string Verify()
    {
        GameObject root = GameObject.Find("DartboardPreview");
        DartboardSurface surface = root.GetComponent<DartboardSurface>();
        DartAimView view = root.GetComponent<DartAimView>();
        Vector2 sample = new Vector2(0.18f, 0.38f);
        Vector2 roundTrip = surface.WorldToBoard(surface.BoardToWorld(sample));
        if (Vector2.Distance(roundTrip, sample) > 0.0001f)
        {
            throw new InvalidOperationException("座標の往復変換が一致しません");
        }

        Ray ray = new Ray(Camera.main.transform.position,
            surface.BoardToWorld(sample) - Camera.main.transform.position);
        if (!surface.TryRaytoBoard(ray, out Vector2 hit) ||
            Vector2.Distance(hit, sample) > 0.0001f)
        {
            throw new InvalidOperationException("Rayからの座標変換が一致しません");
        }

        ShowSample(view);
        LineRenderer circle = root.transform.Find("AimCircle").GetComponent<LineRenderer>();
        Transform marker = root.transform.Find("LandingMarker");
        Vector3 offset = -surface.transform.forward * 0.01f;
        for (int i = 0; i < circle.positionCount; i++)
        {
            Vector2 point = surface.WorldToBoard(circle.GetPosition(i) - offset);
            if (Mathf.Abs(Vector2.Distance(point, sample) - 0.32f) > 0.0001f)
            {
                throw new InvalidOperationException("照準円の表示位置が一致しません");
            }
        }

        if (!marker.gameObject.activeSelf ||
            Vector3.Distance(marker.position, surface.BoardToWorld(sample) + offset) > 0.0001f)
        {
            throw new InvalidOperationException("着弾マーカーの表示位置が一致しません");
        }

        return "Verified ray intersection, coordinate round trip, 64 circle points and landing marker";
    }

    /// <summary>
    ///     同じ座標へ照準円と着弾マーカーを表示する
    /// </summary>
    private static void ShowSample(DartAimView view)
    {
        Vector2 sample = new Vector2(0.18f, 0.38f);
        view.ShowAim(sample, 0.32f);
        view.ShowThrowResult(new DartThrowResult(sample,
            StandardDartboardHitCalculator.Calculate(sample)));
    }

    /// <summary>
    ///     標準盤面のリング境界に対応する描画用メッシュを生成する
    /// </summary>
    private static Mesh CreateBoardMesh()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>[]
            { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
        float[] radii = { 0f,
            StandardDartboardHitCalculator.INNER_BULL_RADIUS,
            StandardDartboardHitCalculator.OUTER_BULL_RADIUS,
            StandardDartboardHitCalculator.INNER_SINGLE_RADIUS,
            StandardDartboardHitCalculator.TRIPLE_RADIUS,
            StandardDartboardHitCalculator.OUTER_SINGLE_RADIUS, 1f };
        for (int ring = 0; ring < radii.Length - 1; ring++)
        {
            for (int sector = 0; sector < 20; sector++)
            {
                int material = ring == 0 ? 2 : ring == 1 ? 3 :
                    ring == 3 || ring == 5 ? 2 + sector % 2 : sector % 2;
                for (int step = 0; step < 8; step++)
                {
                    float a = (sector * 18f - 9f + step * 18f / 8f) * Mathf.Deg2Rad;
                    float b = (sector * 18f - 9f + (step + 1) * 18f / 8f) * Mathf.Deg2Rad;
                    int start = vertices.Count;
                    vertices.Add(new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * radii[ring]);
                    vertices.Add(new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * radii[ring + 1]);
                    vertices.Add(new Vector3(Mathf.Sin(b), Mathf.Cos(b), 0f) * radii[ring + 1]);
                    vertices.Add(new Vector3(Mathf.Sin(b), Mathf.Cos(b), 0f) * radii[ring]);
                    triangles[material].AddRange(new[]
                        { start, start + 1, start + 2, start, start + 2, start + 3 });
                }
            }
        }

        var mesh = new Mesh { name = "StandardDartboardPreview", subMeshCount = 4 };
        mesh.SetVertices(vertices);
        for (int i = 0; i < triangles.Length; i++)
        {
            mesh.SetTriangles(triangles[i], i);
        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    ///     盤面の外周に標準の番号を配置する
    /// </summary>
    private static void CreateNumbers(Transform parent)
    {
        TMPro.TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
            "Assets/Mock/TextResources/Resources/Fonts & Materials/LiberationSans SDF.asset");
        for (int i = 0; i < NUMBERS.Length; i++)
        {
            var label = new GameObject("Number" + NUMBERS[i], typeof(TMPro.TextMeshPro));
            label.transform.SetParent(parent, false);
            float angle = i * 18f * Mathf.Deg2Rad;
            label.transform.localPosition =
                new Vector3(Mathf.Sin(angle) * 1.1f, Mathf.Cos(angle) * 1.1f, -0.012f);
            TMPro.TextMeshPro text = label.GetComponent<TMPro.TextMeshPro>();
            text.font = font;
            text.fontSize = 0.85f;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(0.2f, 0.18f);
            text.color = new Color(0.85f, 0.9f, 0.96f);
            text.text = NUMBERS[i].ToString();
        }
    }

    /// <summary>
    ///     検証用の色を持つUnlitマテリアルを保存する
    /// </summary>
    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            throw new InvalidOperationException("URP Unlit Shaderが見つかりません");
        }

        var material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, ASSET_FOLDER + "/" + name + ".mat");
        return material;
    }

    /// <summary>
    ///     保存するコンポーネントの設定値を指定する
    /// </summary>
    private static void SetFloat(UnityEngine.Object target, string field, float value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    ///     未作成のアセットフォルダを作る
    /// </summary>
    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            int separator = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
        }
    }
}
