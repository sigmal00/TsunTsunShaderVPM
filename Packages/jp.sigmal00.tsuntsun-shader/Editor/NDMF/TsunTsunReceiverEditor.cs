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

        private const string ReadmeUrl = "https://github.com/sigmal00/TsunTsunShaderVPM#readme";
        private const string TsunTsunShaderName = "Sigmal00/TsunTsunShader";
        private const int MaxTargetRenderers = 4;

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
