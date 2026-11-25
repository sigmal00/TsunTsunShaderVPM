using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Sigmal00.TsunTsunShader.Editor
{
    /// <summary>
    /// TsunTsunReceiver のインスペクター。ShapeType に関係しない形状の項目は表示しない。
    /// </summary>
    [CustomEditor(typeof(TsunTsunReceiver))]
    [CanEditMultipleObjects]
    public class TsunTsunReceiverEditor : UnityEditor.Editor
    {
        private SerializedProperty _rootTransform;
        private SerializedProperty _shapeType;
        private SerializedProperty _radius;
        private SerializedProperty _height;
        private SerializedProperty _size;
        private SerializedProperty _position;
        private SerializedProperty _rotation;
        private SerializedProperty _allowSelf;
        private SerializedProperty _allowOthers;
        private SerializedProperty _localOnly;
        private SerializedProperty _targetRenderers;
        private SerializedProperty _maskOutputFolder;
        private SerializedProperty _maskResolution;
        private SerializedProperty _maskFadeWidth;
        private SerializedProperty _maskMaxValue;
        private SerializedProperty _maskExcludeHands;

        private const string ReadmeUrl = "https://github.com/sigmal00/TsunTsunShaderVPM#readme";
        private const string TsunTsunShaderName = "Sigmal00/TsunTsunShader";
        private const int MaxTargetRenderers = 4;
        private const string MaskFoldoutKey = "Sigmal00.TsunTsunShader.TsunTsunReceiverEditor.MaskFoldout";

        private static readonly GUIContent MaskFoldoutLabel = new GUIContent("つんつんマスク生成", "Receiverが覆っている範囲をもとにマスクを生成してマテリアルに割り当てます。");
        private static readonly GUIContent MaskOutputFolderLabel = new GUIContent("保存先", "生成したマスクの保存先フォルダ（Assets/ 以下）。空ならマテリアルと同じフォルダに保存します。");
        private static readonly GUIContent MaskResolutionLabel = new GUIContent("解像度", "生成するマスクの解像度");
        private static readonly GUIContent MaskFadeWidthLabel = new GUIContent("フェード幅 (m)", "範囲の端でマスクを白から黒へフェードさせる幅");
        private static readonly GUIContent MaskMaxValueLabel = new GUIContent("マスク最大値", "範囲の内側でのマスクの強さ（0 で凹まない、1 で最大まで凹む）");
        private static readonly GUIContent MaskExcludeHandsLabel = new GUIContent("手を除外", "手・指のボーンにウェイトが乗っている部分をマスクから除外します");

        private void OnEnable()
        {
            _rootTransform   = serializedObject.FindProperty(nameof(TsunTsunReceiver.rootTransform));
            _shapeType       = serializedObject.FindProperty(nameof(TsunTsunReceiver.shapeType));
            _radius          = serializedObject.FindProperty(nameof(TsunTsunReceiver.radius));
            _height          = serializedObject.FindProperty(nameof(TsunTsunReceiver.height));
            _size            = serializedObject.FindProperty(nameof(TsunTsunReceiver.size));
            _position        = serializedObject.FindProperty(nameof(TsunTsunReceiver.position));
            _rotation        = serializedObject.FindProperty(nameof(TsunTsunReceiver.rotation));
            _allowSelf       = serializedObject.FindProperty(nameof(TsunTsunReceiver.allowSelf));
            _allowOthers     = serializedObject.FindProperty(nameof(TsunTsunReceiver.allowOthers));
            _localOnly       = serializedObject.FindProperty(nameof(TsunTsunReceiver.localOnly));
            _targetRenderers = serializedObject.FindProperty(nameof(TsunTsunReceiver.targetRenderers));
            _maskOutputFolder = serializedObject.FindProperty(nameof(TsunTsunReceiver.maskOutputFolder));
            _maskResolution   = serializedObject.FindProperty(nameof(TsunTsunReceiver.maskResolution));
            _maskFadeWidth    = serializedObject.FindProperty(nameof(TsunTsunReceiver.maskFadeWidth));
            _maskMaxValue     = serializedObject.FindProperty(nameof(TsunTsunReceiver.maskMaxValue));
            _maskExcludeHands = serializedObject.FindProperty(nameof(TsunTsunReceiver.maskExcludeHands));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // TsunTsunShader のマテリアルとセットで動くことの案内とマニュアルへのリンク
            EditorGUILayout.HelpBox("このコンポーネントはTsunTsunShaderが設定されたマテリアルとセットで動作します。セットアップ手順はマニュアルを参照してください。", MessageType.Info);
            if (GUILayout.Button("マニュアルを開く"))
            {
                Application.OpenURL(ReadmeUrl);
            }
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_rootTransform);

            // Shape（[Header("Shape")] は shapeType に付いているのでここで表示される）
            EditorGUILayout.PropertyField(_shapeType);

            // 複数選択で ShapeType が混在しているときは全部表示する
            var mixed = _shapeType.hasMultipleDifferentValues;
            var shape = (ContactBase.ShapeType)_shapeType.intValue;

            if (mixed || shape == ContactBase.ShapeType.Sphere || shape == ContactBase.ShapeType.Capsule)
            {
                EditorGUILayout.PropertyField(_radius);
            }
            if (mixed || shape == ContactBase.ShapeType.Capsule)
            {
                EditorGUILayout.PropertyField(_height);
            }
            if (mixed || shape == ContactBase.ShapeType.Box)
            {
                EditorGUILayout.PropertyField(_size);
            }
            EditorGUILayout.PropertyField(_position);
            EditorGUILayout.PropertyField(_rotation);

            // Filtering
            EditorGUILayout.PropertyField(_allowSelf);
            EditorGUILayout.PropertyField(_allowOthers);
            EditorGUILayout.PropertyField(_localOnly);

            // Write to Materials
            EditorGUILayout.PropertyField(_targetRenderers);

            serializedObject.ApplyModifiedProperties();

            // Write to Materials の設定ミスを警告する
            if (!serializedObject.isEditingMultipleObjects)
            {
                DrawRendererValidation((TsunTsunReceiver)target);
            }

            EditorGUILayout.Space();
            DrawMaskGeneration();
        }

        /// <summary>
        /// マスク生成のオプションと「マスクを生成」ボタン。
        /// </summary>
        private void DrawMaskGeneration()
        {
            var foldout = SessionState.GetBool(MaskFoldoutKey, false);
            var newFoldout = EditorGUILayout.Foldout(foldout, MaskFoldoutLabel, true);
            if (newFoldout != foldout)
            {
                SessionState.SetBool(MaskFoldoutKey, newFoldout);
            }
            if (!newFoldout)
            {
                return;
            }

            serializedObject.Update();
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.HelpBox("Receiverが覆っている範囲をもとにマスクテクスチャを生成してマテリアルに割り当てます。", MessageType.Info);
                EditorGUILayout.Space();

                DrawFolderField(_maskOutputFolder, MaskOutputFolderLabel);
                EditorGUILayout.IntPopup(_maskResolution, ResolutionLabels, TsunTsunMaskGenerator.Resolutions, MaskResolutionLabel);
                EditorGUILayout.PropertyField(_maskFadeWidth, MaskFadeWidthLabel);
                if (_maskFadeWidth.floatValue < 0.0f)
                {
                    _maskFadeWidth.floatValue = 0.0f;
                }
                EditorGUILayout.PropertyField(_maskMaxValue, MaskMaxValueLabel);
                EditorGUILayout.PropertyField(_maskExcludeHands, MaskExcludeHandsLabel);

                // マスク生成で使われる TsunTsunMask の数
                if (!serializedObject.isEditingMultipleObjects)
                {
                    var masks = TsunTsunMaskGenerator.CollectMasks((TsunTsunReceiver)target);
                    EditorGUILayout.LabelField("TsunTsun Mask", masks.Length > 0 ? $"{masks.Length} 個（アバター内）" : "なし");
                }
            }
            serializedObject.ApplyModifiedProperties();

            // 生成は 1 つずつ（複数選択中は押せない）
            using (new EditorGUI.DisabledScope(serializedObject.isEditingMultipleObjects))
            {
                if (GUILayout.Button("マスクを生成してマテリアルに設定する"))
                {
                    TsunTsunMaskGenerator.Generate((TsunTsunReceiver)target);
                }
            }
        }

        private static readonly GUIContent[] ResolutionLabels = System.Array.ConvertAll(
            TsunTsunMaskGenerator.Resolutions, r => new GUIContent($"{r} x {r}"));

        /// <summary>
        /// Assets/ 以下のフォルダパスを入力する欄（テキスト ＋ 選択ボタン）。
        /// </summary>
        private static void DrawFolderField(SerializedProperty property, GUIContent label)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(property, label);
                if (GUILayout.Button("...", GUILayout.Width(28)))
                {
                    var current = string.IsNullOrEmpty(property.stringValue) ? "Assets" : property.stringValue;
                    var selected = EditorUtility.OpenFolderPanel("マスクの保存先", current, "");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        var relative = ToAssetPath(selected);
                        if (relative != null)
                        {
                            property.stringValue = relative;
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("TsunTsunShader", "プロジェクトの Assets フォルダ以下を選んでください。", "OK");
                        }
                    }
                    // OpenFolderPanel の後はレイアウトが崩れるのでここで抜ける
                    property.serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
            }

            var path = property.stringValue;
            if (!string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path))
            {
                EditorGUILayout.HelpBox($"保存先フォルダ {path} が見つかりません。", MessageType.Warning);
            }
        }

        /// <summary>
        /// 絶対パスを Assets/ から始まるパスに変換する。プロジェクトの Assets 以下でなければ null。
        /// </summary>
        private static string ToAssetPath(string absolutePath)
        {
            var dataPath = Application.dataPath.Replace('\\', '/');
            var path = absolutePath.Replace('\\', '/');
            if (path == dataPath)
            {
                return "Assets";
            }
            if (path.StartsWith(dataPath + "/"))
            {
                return "Assets" + path.Substring(dataPath.Length);
            }
            return null;
        }

        /// <summary>
        /// Write to Materials の設定ミス（未設定・数の超過・TsunTsunShader のマテリアルが無い）を警告する。
        /// </summary>
        private static void DrawRendererValidation(TsunTsunReceiver receiver)
        {
            var renderers = receiver.targetRenderers;
            if (renderers == null || renderers.Length == 0)
            {
                EditorGUILayout.HelpBox("Write to Materials にレンダラーが設定されていません。TsunTsunShaderを適用したマテリアルがあるレンダラーを設定してください。", MessageType.Warning);
                return;
            }

            if (renderers.Length > MaxTargetRenderers)
            {
                EditorGUILayout.HelpBox($"Write to Materials に設定できるレンダラーは {MaxTargetRenderers} つまでです。5 つ目以降は無視されます。", MessageType.Warning);
            }

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    EditorGUILayout.HelpBox("Write to Materials に空の欄があります。", MessageType.Warning);
                    continue;
                }

                if (!HasTsunTsunMaterial(renderer))
                {
                    EditorGUILayout.HelpBox(
                        $"{renderer.name} に TsunTsunShader のマテリアルがありません。\n" +
                        "マテリアルを右クリック → TsunTsunShader → Convert material to TsunTsunShader で変換してください。",
                        MessageType.Warning);
                }
            }
        }

        private static bool HasTsunTsunMaterial(Renderer renderer)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                if (material != null && material.shader != null && material.shader.name.Contains(TsunTsunShaderName))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
