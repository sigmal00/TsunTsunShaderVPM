using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;

namespace Sigmal00.TsunTsunShader.Editor
{
    /// <summary>
    /// TsunTsunReceiver の範囲と、アバター内の TsunTsunMask からつんつんマスク（_TsunTsunMask）を生成して、PNG として保存・マテリアルに割り当てる。
    /// ビルド時ではなくエディタ上でボタンから実行し、結果はユーザーのアセットとして残す。
    ///
    /// 処理の流れ
    ///  1. targetRenderers の TsunTsunShader マテリアルを集める（同じマテリアルは 1 枚のマスクにまとめる）
    ///  2. 各レンダラーのメッシュを今のポーズでワールド座標にする
    ///  3. 頂点ごとにマスク値を決める（MaskEvaluator 参照）
    ///     - Receiver の範囲の完全に外側は 0
    ///     - 内側は Receiver の maskMaxValue（端は maskFadeWidth でフェード）
    ///     - TsunTsunMask の内側はその maxValue で上書き（端は fadeWidth で混ぜる。重なったところは大きいほう）
    ///     - 最後に maskExcludeHands なら手・指ボーンのウェイトぶん暗くする（TsunTsunMask より優先）
    ///  4. UV0 上に三角形をラスタライズし、UV 島の縁を膨張させる
    ///  5. PNG で保存し、_TsunTsunMask に割り当てる
    /// </summary>
    public static class TsunTsunMaskGenerator
    {
        /// <summary>選べる解像度</summary>
        public static readonly int[] Resolutions = { 256, 512, 1024, 2048 };

        private const string MaskPropertyName = "_TsunTsunMask";
        private const string TsunTsunShaderName = "Sigmal00/TsunTsunShader";
        private const string DialogTitle = "TsunTsunShader";
        private const string FileSuffix = "_TsunTsunMask.png";

        /// <summary>1 つのマテリアルに対応するマスク</summary>
        private class MaskCanvas
        {
            public readonly Material Material;
            public readonly int Resolution;
            public readonly float[] Pixels;
            public readonly bool[] Covered;

            public MaskCanvas(Material material, int resolution)
            {
                Material = material;
                Resolution = resolution;
                Pixels = new float[resolution * resolution];
                Covered = new bool[resolution * resolution];
            }
        }

        /// <summary>
        /// receiver の設定（maskOutputFolder / maskResolution / maskFadeWidth / maskMaxValue / maskExcludeHands）と
        /// アバター内の TsunTsunMask でマスクを生成する。
        /// </summary>
        public static void Generate(TsunTsunReceiver receiver)
        {
            var warnings = new List<string>();

            var renderers = receiver.targetRenderers?.Where(r => r != null).Distinct().ToArray() ?? new Renderer[0];
            if (renderers.Length == 0)
            {
                ShowDialog("Write to Materials にレンダラーが設定されていません。");
                return;
            }

            var resolution = Resolutions.Contains(receiver.maskResolution) ? receiver.maskResolution : 1024;
            var outputFolder = receiver.maskOutputFolder;
            if (!string.IsNullOrEmpty(outputFolder) && !AssetDatabase.IsValidFolder(outputFolder))
            {
                ShowDialog($"保存先フォルダ {outputFolder} が見つかりません。");
                return;
            }

            // 1. マテリアルを集める
            var canvases = new Dictionary<Material, MaskCanvas>();
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (!IsTsunTsunMaterial(material) || canvases.ContainsKey(material))
                    {
                        continue;
                    }
                    var path = AssetDatabase.GetAssetPath(material);
                    if (string.IsNullOrEmpty(path) || !path.EndsWith(".mat"))
                    {
                        warnings.Add($"{material.name}: マテリアルが .mat アセットではないため（FBX 内のマテリアルなど）スキップしました。");
                        continue;
                    }
                    canvases.Add(material, new MaskCanvas(material, resolution));
                }
            }
            if (canvases.Count == 0)
            {
                ShowDialog("Write to Materials のレンダラーに TsunTsunShader のマテリアルが見つかりません。" + FormatWarnings(warnings));
                return;
            }

            // すでにマスクが設定されているマテリアルは差し替えてよいか確認する
            var hasMask = canvases.Keys.Where(m => m.GetTexture(MaskPropertyName) != null).ToArray();
            if (hasMask.Length > 0)
            {
                var names = string.Join("\n", hasMask.Select(m => "・" + m.name));
                if (!EditorUtility.DisplayDialog(DialogTitle,
                        "次のマテリアルにはすでにマスクが設定されています。生成したマスクに差し替えますか？\n（元のマスク画像は削除しません）\n\n" + names,
                        "差し替える", "キャンセル"))
                {
                    return;
                }
            }

            var masks = CollectMasks(receiver);
            var evaluator = new MaskEvaluator(receiver, masks);
            var handBones = receiver.maskExcludeHands ? CollectHandBones(receiver, warnings) : null;

            var generated = new List<string>();
            try
            {
                // 2〜4. レンダラーごとにラスタライズ
                for (var i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    EditorUtility.DisplayProgressBar(DialogTitle, $"マスクを描いています: {renderer.name}", (float)i / renderers.Length * 0.8f);
                    RasterizeRenderer(renderer, canvases, evaluator, handBones, warnings);
                }

                // 膨張して保存・割り当て
                var index = 0;
                foreach (var canvas in canvases.Values)
                {
                    EditorUtility.DisplayProgressBar(DialogTitle, $"保存しています: {canvas.Material.name}", 0.8f + 0.2f * index / canvases.Count);
                    index++;

                    Dilate(canvas, Mathf.Max(2, canvas.Resolution / 256));
                    var assetPath = SaveAndAssign(canvas, outputFolder);
                    if (assetPath != null)
                    {
                        generated.Add(assetPath);
                    }
                }
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            foreach (var warning in warnings)
            {
                Debug.LogWarning("[TsunTsunShader] " + warning, receiver);
            }

            var message = generated.Count > 0
                ? $"マスクを生成して割り当てました（TsunTsun Mask: {masks.Length} 個）。\n\n" + string.Join("\n", generated)
                : "マスクを生成できませんでした。";
            ShowDialog(message + FormatWarnings(warnings));
        }

        // ------------------------------------------------------------------------------------------------
        // ラスタライズ

        private static void RasterizeRenderer(Renderer renderer, Dictionary<Material, MaskCanvas> canvases,
            MaskEvaluator evaluator, HashSet<Transform> handBones, List<string> warnings)
        {
            if (!TryGetWorldMesh(renderer, out var mesh, out var worldVertices))
            {
                warnings.Add($"{renderer.name}: メッシュを読み込めないためスキップしました（Read/Write が無効なメッシュなど）。");
                return;
            }

            var uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);
            if (uvs.Count != mesh.vertexCount)
            {
                warnings.Add($"{renderer.name}: UV0 が無いためスキップしました。");
                return;
            }

            // 頂点ごとのマスク値
            var values = new float[worldVertices.Length];
            for (var v = 0; v < worldVertices.Length; v++)
            {
                values[v] = evaluator.Evaluate(worldVertices[v]);
            }

            if (handBones != null)
            {
                var handWeights = CalcHandWeights(renderer, mesh, handBones);
                for (var v = 0; v < values.Length; v++)
                {
                    values[v] *= 1.0f - handWeights[v];
                }
            }

            var materials = renderer.sharedMaterials;
            var subMeshCount = Mathf.Min(materials.Length, mesh.subMeshCount);
            for (var s = 0; s < subMeshCount; s++)
            {
                var material = materials[s];
                if (material == null || !canvases.TryGetValue(material, out var canvas))
                {
                    continue;
                }
                if (mesh.GetTopology(s) != MeshTopology.Triangles)
                {
                    warnings.Add($"{renderer.name}: サブメッシュ {s} が三角形ではないためスキップしました。");
                    continue;
                }

                var triangles = mesh.GetTriangles(s);
                for (var t = 0; t + 2 < triangles.Length; t += 3)
                {
                    int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                    RasterizeTriangle(canvas, uvs[a], uvs[b], uvs[c], values[a], values[b], values[c]);
                }
            }
        }

        /// <summary>
        /// 今のポーズ（ブレンドシェイプ込み）でのワールド座標の頂点を得る。mesh は UV・ウェイト・三角形を読むための元メッシュ。
        /// </summary>
        private static bool TryGetWorldMesh(Renderer renderer, out Mesh mesh, out Vector3[] worldVertices)
        {
            worldVertices = null;
            mesh = null;

            if (renderer is SkinnedMeshRenderer skinned)
            {
                mesh = skinned.sharedMesh;
                if (mesh == null)
                {
                    return false;
                }

                var baked = new Mesh();
                try
                {
                    // useScale = true のときは Transform のスケール込みのローカル座標になるので、位置と回転だけ掛ける
                    skinned.BakeMesh(baked, true);
                    var vertices = baked.vertices;
                    if (vertices.Length != mesh.vertexCount)
                    {
                        return false;
                    }
                    var matrix = Matrix4x4.TRS(skinned.transform.position, skinned.transform.rotation, Vector3.one);
                    for (var i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
                    }
                    worldVertices = vertices;
                    return true;
                }
                finally
                {
                    Object.DestroyImmediate(baked);
                }
            }

            var filter = renderer.GetComponent<MeshFilter>();
            mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
            {
                return false;
            }
            var localVertices = mesh.vertices;
            if (localVertices.Length != mesh.vertexCount)
            {
                return false;
            }
            var localToWorld = renderer.transform.localToWorldMatrix;
            for (var i = 0; i < localVertices.Length; i++)
            {
                localVertices[i] = localToWorld.MultiplyPoint3x4(localVertices[i]);
            }
            worldVertices = localVertices;
            return true;
        }

        /// <summary>
        /// UV 空間で三角形を塗る。値は頂点の値を補間し、重なったところは大きいほうを採用する。
        /// </summary>
        private static void RasterizeTriangle(MaskCanvas canvas, Vector2 uvA, Vector2 uvB, Vector2 uvC, float va, float vb, float vc)
        {
            var res = canvas.Resolution;
            var pa = uvA * res;
            var pb = uvB * res;
            var pc = uvC * res;

            // 小さい三角形がピクセル中心を 1 つも含まないと消えるので、頂点の位置は必ず塗る
            Plot(canvas, Mathf.FloorToInt(pa.x), Mathf.FloorToInt(pa.y), va);
            Plot(canvas, Mathf.FloorToInt(pb.x), Mathf.FloorToInt(pb.y), vb);
            Plot(canvas, Mathf.FloorToInt(pc.x), Mathf.FloorToInt(pc.y), vc);

            var area = Edge(pa, pb, pc);
            if (Mathf.Abs(area) < 1e-8f)
            {
                return;
            }

            // UV が 0〜1 から大きくはみ出していても固まらないように範囲を制限する
            var minX = Mathf.FloorToInt(Mathf.Min(pa.x, Mathf.Min(pb.x, pc.x)));
            var maxX = Mathf.Min(Mathf.CeilToInt(Mathf.Max(pa.x, Mathf.Max(pb.x, pc.x))), minX + res * 2);
            var minY = Mathf.FloorToInt(Mathf.Min(pa.y, Mathf.Min(pb.y, pc.y)));
            var maxY = Mathf.Min(Mathf.CeilToInt(Mathf.Max(pa.y, Mathf.Max(pb.y, pc.y))), minY + res * 2);

            const float eps = -1e-5f;
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var w0 = Edge(pb, pc, p) / area;
                    var w1 = Edge(pc, pa, p) / area;
                    var w2 = 1.0f - w0 - w1;
                    if (w0 < eps || w1 < eps || w2 < eps)
                    {
                        continue;
                    }
                    Plot(canvas, x, y, w0 * va + w1 * vb + w2 * vc);
                }
            }
        }

        private static float Edge(Vector2 a, Vector2 b, Vector2 p)
        {
            return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        }

        /// <summary>UV のリピートに合わせて折り返して書き込む</summary>
        private static void Plot(MaskCanvas canvas, int x, int y, float value)
        {
            var res = canvas.Resolution;
            x = ((x % res) + res) % res;
            y = ((y % res) + res) % res;
            var i = y * res + x;
            if (!canvas.Covered[i] || canvas.Pixels[i] < value)
            {
                canvas.Pixels[i] = value;
            }
            canvas.Covered[i] = true;
        }

        /// <summary>
        /// UV 島の外側に値を広げる（バイリニア補間で縁が黒くにじむのを防ぐ）。
        /// </summary>
        private static void Dilate(MaskCanvas canvas, int iterations)
        {
            var res = canvas.Resolution;
            var pixels = canvas.Pixels;
            var covered = canvas.Covered;

            for (var it = 0; it < iterations; it++)
            {
                var nextPixels = (float[])pixels.Clone();
                var nextCovered = (bool[])covered.Clone();
                var changed = false;

                for (var y = 0; y < res; y++)
                {
                    for (var x = 0; x < res; x++)
                    {
                        var i = y * res + x;
                        if (covered[i])
                        {
                            continue;
                        }

                        var found = false;
                        var best = 0.0f;
                        for (var dy = -1; dy <= 1; dy++)
                        {
                            var ny = y + dy;
                            if (ny < 0 || ny >= res) continue;
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                var nx = x + dx;
                                if (nx < 0 || nx >= res) continue;
                                var n = ny * res + nx;
                                if (covered[n] && (!found || pixels[n] > best))
                                {
                                    best = pixels[n];
                                    found = true;
                                }
                            }
                        }

                        if (found)
                        {
                            nextPixels[i] = best;
                            nextCovered[i] = true;
                            changed = true;
                        }
                    }
                }

                System.Array.Copy(nextPixels, pixels, pixels.Length);
                System.Array.Copy(nextCovered, covered, covered.Length);
                if (!changed)
                {
                    break;
                }
            }
        }

        // ------------------------------------------------------------------------------------------------
        // 手の除外

        /// <summary>
        /// Humanoid の左右の Hand ボーンとその子孫（指・指先・手に付けたものなど）を集める。
        /// </summary>
        private static HashSet<Transform> CollectHandBones(TsunTsunReceiver receiver, List<string> warnings)
        {
            var animator = receiver.GetComponentInParent<Animator>(true);
            if (animator == null || !animator.isHuman)
            {
                warnings.Add("Humanoid の Animator が見つからないため、手の除外は行いませんでした。");
                return null;
            }

            var bones = new HashSet<Transform>();
            foreach (var hand in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
            {
                var handTransform = animator.GetBoneTransform(hand);
                if (handTransform == null)
                {
                    continue;
                }
                foreach (var t in handTransform.GetComponentsInChildren<Transform>(true))
                {
                    bones.Add(t);
                }
            }

            if (bones.Count == 0)
            {
                warnings.Add("Hand ボーンが見つからないため、手の除外は行いませんでした。");
                return null;
            }
            return bones;
        }

        /// <summary>頂点ごとの手・指ボーンのウェイトの合計（0〜1）</summary>
        private static float[] CalcHandWeights(Renderer renderer, Mesh mesh, HashSet<Transform> handBones)
        {
            var result = new float[mesh.vertexCount];

            // ウェイトの無いメッシュは Transform（スキンなら rootBone）ごと手に付いているかで決める
            var skinned = renderer as SkinnedMeshRenderer;
            var bonesPerVertex = skinned != null ? mesh.GetBonesPerVertex() : default;
            if (skinned == null || bonesPerVertex.Length == 0)
            {
                var anchor = skinned != null && skinned.rootBone != null ? skinned.rootBone : renderer.transform;
                if (handBones.Contains(anchor))
                {
                    for (var v = 0; v < result.Length; v++) result[v] = 1.0f;
                }
                return result;
            }

            var bones = skinned.bones;
            var isHandBone = new bool[bones.Length];
            for (var b = 0; b < bones.Length; b++)
            {
                isHandBone[b] = bones[b] != null && handBones.Contains(bones[b]);
            }

            var weights = mesh.GetAllBoneWeights();
            var w = 0;
            for (var v = 0; v < bonesPerVertex.Length && v < result.Length; v++)
            {
                var sum = 0.0f;
                for (var k = 0; k < bonesPerVertex[v]; k++, w++)
                {
                    var bw = weights[w];
                    if (bw.boneIndex < isHandBone.Length && isHandBone[bw.boneIndex])
                    {
                        sum += bw.weight;
                    }
                }
                result[v] = Mathf.Clamp01(sum);
            }
            return result;
        }

        // ------------------------------------------------------------------------------------------------
        // 保存

        /// <summary>PNG で保存してマテリアルに割り当てる。保存したアセットパスを返す。</summary>
        private static string SaveAndAssign(MaskCanvas canvas, string outputFolder)
        {
            var material = canvas.Material;
            var folder = string.IsNullOrEmpty(outputFolder)
                ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(material))?.Replace('\\', '/')
                : outputFolder;
            if (string.IsNullOrEmpty(folder))
            {
                return null;
            }

            // 前回このマテリアル用に生成したファイルなら上書きし、それ以外の同名ファイルがあるときは別名にする
            var assetPath = folder + "/" + SanitizeFileName(material.name) + FileSuffix;
            var currentMask = material.GetTexture(MaskPropertyName);
            var currentPath = currentMask != null ? AssetDatabase.GetAssetPath(currentMask) : null;
            if (currentPath != assetPath && File.Exists(Path.GetFullPath(assetPath)))
            {
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            }

            var res = canvas.Resolution;
            var colors = new Color32[res * res];
            for (var i = 0; i < colors.Length; i++)
            {
                var v = (byte)Mathf.RoundToInt(Mathf.Clamp01(canvas.Pixels[i]) * 255.0f);
                colors[i] = new Color32(v, v, v, 255);
            }

            var texture = new Texture2D(res, res, TextureFormat.RGBA32, false, true);
            try
            {
                texture.SetPixels32(colors);
                texture.Apply();
                File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                // R だけ使うので Single Channel（R8 / BC4）にする。頂点シェーダーでも LOD 0 で読むのでミップマップは不要
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.textureType = TextureImporterType.SingleChannel;
                settings.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
                settings.sRGBTexture = false;
                settings.mipmapEnabled = false;
                settings.alphaSource = TextureImporterAlphaSource.None;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (mask == null)
            {
                return null;
            }
            Undo.RecordObject(material, "Generate TsunTsun Mask");
            material.SetTexture(MaskPropertyName, mask);
            EditorUtility.SetDirty(material);
            return assetPath;
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
            {
                sb.Append(invalid.Contains(c) ? '_' : c);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------------------------------------
        // その他

        private static bool IsTsunTsunMaterial(Material material)
        {
            return material != null && material.shader != null && material.shader.name.Contains(TsunTsunShaderName);
        }

        private static void ShowDialog(string message)
        {
            EditorUtility.DisplayDialog(DialogTitle, message, "OK");
        }

        private static string FormatWarnings(List<string> warnings)
        {
            return warnings.Count == 0 ? "" : "\n\n注意:\n" + string.Join("\n", warnings.Select(w => "・" + w));
        }

        // ------------------------------------------------------------------------------------------------
        // TsunTsunMask の収集と合成

        /// <summary>
        /// receiver が属するアバター（VRCAvatarDescriptor、無ければ最上位の Transform）以下の TsunTsunMask を集める。
        /// </summary>
        public static TsunTsunMask[] CollectMasks(TsunTsunReceiver receiver)
        {
            var descriptor = receiver.GetComponentInParent<VRCAvatarDescriptor>(true);
            var root = descriptor != null ? descriptor.transform : receiver.transform.root;
            return root.GetComponentsInChildren<TsunTsunMask>(true);
        }

        /// <summary>
        /// ワールド座標の点のマスク値（手の除外の前）を求める。
        /// </summary>
        private class MaskEvaluator
        {
            private readonly Shape _receiverShape;
            private readonly float _receiverFade;
            private readonly float _receiverMax;
            private readonly Shape[] _maskShapes;
            private readonly float[] _maskFades;
            private readonly float[] _maskMaxValues;

            public MaskEvaluator(TsunTsunReceiver receiver, TsunTsunMask[] masks)
            {
                _receiverShape = new Shape(receiver.rootTransform != null ? receiver.rootTransform : receiver.transform,
                    receiver.shapeType, receiver.radius, receiver.height, receiver.size, receiver.position, receiver.rotation);
                _receiverFade = Mathf.Max(receiver.maskFadeWidth, 0.0f);
                _receiverMax = Mathf.Clamp01(receiver.maskMaxValue);

                _maskShapes = new Shape[masks.Length];
                _maskFades = new float[masks.Length];
                _maskMaxValues = new float[masks.Length];
                for (var i = 0; i < masks.Length; i++)
                {
                    var m = masks[i];
                    _maskShapes[i] = new Shape(m.rootTransform != null ? m.rootTransform : m.transform,
                        m.shapeType, m.radius, m.height, m.size, m.position, m.rotation);
                    _maskFades[i] = Mathf.Max(m.fadeWidth, 0.0f);
                    _maskMaxValues[i] = Mathf.Clamp01(m.maxValue);
                }
            }

            public float Evaluate(Vector3 p)
            {
                // Receiver の範囲の完全に外側は TsunTsunMask があっても黒
                var receiverDistance = _receiverShape.SignedDistance(p);
                if (receiverDistance > 0.0f)
                {
                    return 0.0f;
                }
                var baseValue = Insideness(receiverDistance, _receiverFade) * _receiverMax;

                // TsunTsunMask の合成
                //   coverage : どれだけ TsunTsunMask の内側にいるか（各マスクの内側度の最大）
                //   maskValue: 重なったところは maxValue の大きいほうが勝つように、max(maxValue × 内側度) / coverage
                //              （フェードの途中で別のマスクに入っても値が飛ばない）
                // 最終値は lerp(Receiver の値, maskValue, coverage)
                var coverage = 0.0f;
                var weighted = 0.0f;
                for (var i = 0; i < _maskShapes.Length; i++)
                {
                    var inside = Insideness(_maskShapes[i].SignedDistance(p), _maskFades[i]);
                    if (inside <= 0.0f)
                    {
                        continue;
                    }
                    coverage = Mathf.Max(coverage, inside);
                    weighted = Mathf.Max(weighted, _maskMaxValues[i] * inside);
                }

                if (coverage <= 0.0f)
                {
                    return baseValue;
                }
                return Mathf.Lerp(baseValue, weighted / coverage, coverage);
            }

            /// <summary>範囲の内側度。境界で 0、境界からフェード幅以上内側で 1。フェード幅 0 なら内外で 0/1</summary>
            private static float Insideness(float signedDistance, float fadeWidth)
            {
                return fadeWidth > 0.0f
                    ? Mathf.Clamp01(-signedDistance / fadeWidth)
                    : (signedDistance <= 0.0f ? 1.0f : 0.0f);
            }
        }

        /// <summary>
        /// Receiver / TsunTsunMask の範囲（ギズモと同じ解釈）までの符号付き距離。内側が負。
        /// </summary>
        private readonly struct Shape
        {
            private readonly Matrix4x4 _worldToShape;
            private readonly ContactBase.ShapeType _type;
            private readonly float _radius;
            private readonly float _halfLine;
            private readonly Vector3 _halfSize;

            public Shape(Transform root, ContactBase.ShapeType shapeType, float radius, float height,
                Vector3 size, Vector3 position, Vector3 rotation)
            {
                var center = root.TransformPoint(position);
                var rot = root.rotation * Quaternion.Euler(rotation);
                var lossy = root.lossyScale;
                var absLossy = new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z));
                var scale = Mathf.Max(absLossy.x, absLossy.y, absLossy.z);

                _worldToShape = Matrix4x4.TRS(center, rot, Vector3.one).inverse;
                _type = shapeType;
                _radius = radius * scale;
                _halfLine = Mathf.Max(height * scale * 0.5f - _radius, 0.0f);
                _halfSize = Vector3.Scale(size, absLossy) * 0.5f;
            }

            public float SignedDistance(Vector3 worldPosition)
            {
                var p = _worldToShape.MultiplyPoint3x4(worldPosition);
                switch (_type)
                {
                    case ContactBase.ShapeType.Capsule:
                    {
                        var q = new Vector3(p.x, p.y - Mathf.Clamp(p.y, -_halfLine, _halfLine), p.z);
                        return q.magnitude - _radius;
                    }
                    case ContactBase.ShapeType.Box:
                    {
                        var d = new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z)) - _halfSize;
                        var outside = Vector3.Max(d, Vector3.zero).magnitude;
                        var inside = Mathf.Min(Mathf.Max(d.x, Mathf.Max(d.y, d.z)), 0.0f);
                        return outside + inside;
                    }
                    default:
                        return p.magnitude - _radius;
                }
            }
        }
    }
}
