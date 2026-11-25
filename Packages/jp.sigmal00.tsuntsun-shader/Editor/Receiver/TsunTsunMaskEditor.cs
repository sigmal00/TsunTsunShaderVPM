using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Sigmal00.TsunTsunShader.Editor
{
    /// <summary>
    /// TsunTsunMask のインスペクター。ShapeType に関係しない形状の項目は表示しない。
    /// </summary>
    [CustomEditor(typeof(TsunTsunMask))]
    [CanEditMultipleObjects]
    public class TsunTsunMaskEditor : UnityEditor.Editor
    {
        private SerializedProperty _rootTransform;
        private SerializedProperty _shapeType;
        private SerializedProperty _radius;
        private SerializedProperty _height;
        private SerializedProperty _size;
        private SerializedProperty _position;
        private SerializedProperty _rotation;
        private SerializedProperty _fadeWidth;
        private SerializedProperty _maxValue;

        private static readonly GUIContent FadeWidthLabel = new GUIContent("フェード幅 (m)", "範囲の端で、外側の値からこのマスクの値へ切り替わる幅");
        private static readonly GUIContent MaxValueLabel = new GUIContent("マスク最大値", "範囲の内側でのマスクの強さ（0 で凹まない、1 で最大まで凹む）");

        private void OnEnable()
        {
            _rootTransform = serializedObject.FindProperty(nameof(TsunTsunMask.rootTransform));
            _shapeType     = serializedObject.FindProperty(nameof(TsunTsunMask.shapeType));
            _radius        = serializedObject.FindProperty(nameof(TsunTsunMask.radius));
            _height        = serializedObject.FindProperty(nameof(TsunTsunMask.height));
            _size          = serializedObject.FindProperty(nameof(TsunTsunMask.size));
            _position      = serializedObject.FindProperty(nameof(TsunTsunMask.position));
            _rotation      = serializedObject.FindProperty(nameof(TsunTsunMask.rotation));
            _fadeWidth     = serializedObject.FindProperty(nameof(TsunTsunMask.fadeWidth));
            _maxValue      = serializedObject.FindProperty(nameof(TsunTsunMask.maxValue));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "この範囲の内側では、TsunTsun Receiver のマスク設定を上書きします。\n" +
                "マスクは TsunTsun Receiver の「マスクを生成」で作り直したときに反映されます。\n" +
                "Receiver の範囲の外側にはみ出した部分は無視されます。",
                MessageType.Info);
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

            // Mask（[Header("Mask")] は fadeWidth に付いているのでここで表示される）
            EditorGUILayout.PropertyField(_fadeWidth, FadeWidthLabel);
            EditorGUILayout.PropertyField(_maxValue, MaxValueLabel);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
