using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;

[assembly: ExportsPlugin(typeof(Sigmal00.TsunTsunShader.Editor.TsunTsunReceiverPlugin))]

namespace Sigmal00.TsunTsunShader.Editor
{
    /// <summary>
    /// TsunTsunReceiver を、TsunTsunShader 用に設定済みの VRCContactReceiver に置き換える NDMF プラグイン。
    /// </summary>
    public class TsunTsunReceiverPlugin : Plugin<TsunTsunReceiverPlugin>
    {
        public override string QualifiedName => "jp.sigmal00.tsuntsun-shader.receiver";
        public override string DisplayName => "TsunTsun Receiver";

        /// <summary>シェーダーの VRC_CONTACT_DECLARE(Sigmal00_TsunTsun) と一致させるパラメータ名</summary>
        public const string ParameterName = "Sigmal00_TsunTsun";

        /// <summary>
        /// シェーダーはタグのビット位置で指を判別する（0: 人差し指, 1: 中指, 2: 薬指, 3: 小指）。
        /// GetFingerLength() と同じ並び順にすること。
        /// </summary>
        public static readonly string[] CollisionTags = { "FingerIndex", "FingerMiddle", "FingerRing", "FingerLittle" };

        /// <summary>Write to Materials で書き込めるレンダラーの上限（ContactReceiver.SHADER_MAX_UPDATED_RENDERERS）</summary>
        private const int MaxTargetRenderers = 4;

        protected override void Configure()
        {
            InPhase(BuildPhase.Generating).Run("Generate TsunTsun ContactReceivers", ctx =>
            {
                var sources = ctx.AvatarRootObject.GetComponentsInChildren<TsunTsunReceiver>(true);
                foreach (var source in sources)
                {
                    GenerateReceiver(source);
                    UnityEngine.Object.DestroyImmediate(source);
                }
            });
        }

        private static void GenerateReceiver(TsunTsunReceiver source)
        {
            var renderers = source.targetRenderers?.Where(r => r != null).Distinct().ToArray() ?? new Renderer[0];
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[TsunTsunShader] {source.name}: Target Renderers が設定されていないため、つんつんがマテリアルに反映されません。", source);
            }
            else if (renderers.Length > MaxTargetRenderers)
            {
                Debug.LogWarning($"[TsunTsunShader] {source.name}: Target Renderers は {MaxTargetRenderers} つまでです。5 つ目以降は無視されます。", source);
                renderers = renderers.Take(MaxTargetRenderers).ToArray();
            }

            var receiver = source.gameObject.AddComponent<VRCContactReceiver>();

            // 利用者が設定する項目
            receiver.rootTransform = source.rootTransform;
            receiver.shapeType = source.shapeType;
            receiver.radius = source.radius;
            receiver.height = source.height;
            receiver.size = source.size;
            receiver.position = source.position;
            receiver.rotation = Quaternion.Euler(source.rotation);
            receiver.allowSelf = source.allowSelf;
            receiver.allowOthers = source.allowOthers;
            receiver.localOnly = source.localOnly;
            receiver.shaderTargetRenderers = renderers;

            // シェーダーと合わせる必要があるので固定する項目
            receiver.collisionTags = new List<string>(CollisionTags);
            receiver.receiverType = ContactReceiver.ReceiverType.Constant;
            receiver.parameter = ParameterName;
            receiver.writeToMaterials = true;
            receiver.writeToMaterialsContactTags = true;
        }
    }
}
