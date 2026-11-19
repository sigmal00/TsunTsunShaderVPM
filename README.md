# TsunTsunShader
ほっぺたとかをつんつんするシェーダーです。
VRChat の Material Contacts を使い、指で触れた部分のメッシュがぷにっと凹みます。
liltoonのカスタムシェーダー機能を用いて作成されています。

## 導入
1. VCCを開き、**Settings → Packages → Add Repository**と選択し、`https://sigmal00.github.io/TsunTsunShaderVPM/index.json`と入力してAddを選択
2. 続いてVCC上でシェーダーを追加したいプロジェクトの"Manage Project"を選択
3. パッケージ一覧から"TsunTsunShader"を探してプロジェクトに追加する
4. プロジェクトを開き、マテリアルのシェーダー選択欄から"Sigmal00/TsunTsunShader/..."以下のシェーダーが選択できるようになっていることを確認します

## 使用方法
[ミルティナちゃん](https://booth.pm/ja/items/6538026)を例に導入の手順を説明していきます。

### 1.liltoonマテリアルにつんつんシェーダーを適用する
Projectウィンドウからつんつんシェーダーを適用したいマテリアルを選択し、**マテリアルのシェーダー選択欄から"Sigmal00/TsunTsunShader/..."を選択**します。

既にliltoonシェーダーのパラメータを設定済みのマテリアルがある場合は、**マテリアルを右クリックして"TsunTsunShader/Convert material to TsunTsunShader"を選択**すればマテリアルをつんつんシェーダーに対応したものに変換できます。

![image](https://github.com/user-attachments/assets/a7fa4eeb-67b0-4aed-adb5-6db3956262a0)

### 2.アバターにつんつんシェーダー用の当たり判定を追加する
Hierachy上のアバターを右クリックしCreateEmptyを選択して**アバターの直下にGameObjectを作成**します。名前は**TsunTsunReceiver**とします。

![image](https://github.com/user-attachments/assets/24ddbb2c-1d6f-4abe-99de-e9382b385114)

![image](https://github.com/user-attachments/assets/4ac35de0-9205-4b4f-bbcb-14a9fecdc4de)


作成したTsunTsunReceiverを選択し、InspectorウィンドウからAdd Componentを選択、**TsunTsun Receiverというコンポーネントを探して追加**します。

![image](https://github.com/user-attachments/assets/93a08a6f-2375-464b-9836-f1e5553a917d)


追加するとSceneに当たり判定のプレビューが表示されます。つんつんシェーダーはこの判定の中に入ってきたユーザーの指に反応します。

![image](https://github.com/user-attachments/assets/b0a0c05a-c111-465f-89a4-c52b5e3ad54a)

### 3.つんつんシェーダー用の当たり判定を設定する
2の手順で追加したTsunTsun Receiverを設定していきます。

画像はほっぺたなど顔をつんつんしたい場合の設定例です。

![image](https://github.com/user-attachments/assets/22f1661d-ad5c-4267-8922-0497d2626bf3)

#### RootTransform
TsunTsun Receiverを追従させたいTransformを設定します。頭に追従させたい場合はHead、胸に追従させたい場合はChestなどを設定します。
設定しない場合はGameObjectが配置されている階層に追従します。

#### Shape
TsunTsun Receiverの形状を設定できます。つんつんしたい場所を覆うような形状に調整してください。

#### Filterling
誰の指に反応するかを設定できます。

Allow Selfにチェックを入れると自分自身の指に、Allow Othersにチェックを入れると他のユーザーの指に反応します。

Local Onlyにチェックを入れると反応しているのが自分にしか見えなくなります。


#### Write to Materials
つんつんシェーダーを適用したマテリアルが設定されているメッシュレンダラーをここに設定します。**設定しないと動きません。**


## Q&A
### つんつんした場所が綺麗にへこまない
ポリゴンの分割数が足りない可能性があります。
マテリアルのテッセレーションを有効にすると指を近づけた場所のポリゴンの分割数が増えるようになるので試してみてください。

![image](https://github.com/user-attachments/assets/ccd39277-cb6d-435e-8804-a02f896d1d1b)


## 必要な環境
* VRChat SDK (Base) 3.10.6 以降（Material Contacts を使用しています）
* liltoon 2.4.1 以降（それ以前のバージョンでの動作は確認していません）
