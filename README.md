# TsunTsunShader
ほっぺたとかをつんつんするシェーダーです。
VRChat の Material Contacts を使い、指で触れた部分のメッシュがぷにっと凹みます。
liltoonのカスタムシェーダー機能を用いて作成されています。

## 導入
1. VCCを開き、**Settings → Packages → Add Repository**と選択し、`https://sigmal00.github.io/TsunTsunShaderVPM/index.json`と入力してAddを選択
2. 続いてVCC上でシェーダーを追加したいプロジェクトの"Manage Project"を選択
3. パッケージ一覧から"TsunTsunShader"を探してプロジェクトに追加する
4. プロジェクトを開き、マテリアルのシェーダー選択欄から"Sigmal00/TsunTsunShader/..."以下のシェーダーが選択できるようになっていることを確認します

## 必要な環境
* VRChat SDK (Base) 3.10.6 以降（Material Contacts を使用しています）
* liltoon 2.4.1 以降（それ以前のバージョンでの動作は確認していません）
