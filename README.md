
# EmbeddedStreamingAssets

[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

unityroomなどWeb(GL) ビルドで StreamingAssets フォルダをアップロードできない環境のために、StreamingAssets フォルダの内容をProject内に保存し実行時にメモリ上に展開する Unity パッケージです。

インスパイア元は[StreamingAssetsInjector](https://github.com/KurisuJuha/StreamingAssetsInjector)です。

# Requirements
Unity 2022.3 or later

# Installation

PackageManger の Add package from git URL に以下を入力

```
https://github.com/Akeit0/EmbeddedStreamingAssets.git?path=Packages/EmbeddedStreamingAssets
```

または manifest.json の dependencies ブロックに以下を追加

```
"com.akeit0.embedded-streaming-assets": "https://github.com/Akeit0/EmbeddedStreamingAssets.git?path=Packages/EmbeddedStreamingAssets"
```
# Usage
![image](Images/BuildWindow.png)

`Tools/EmbeddedStreamingAssets`もしくは
`Window/EmbeddedStreamingAssets/Build Window`から実行したい処理を実行します。

## Embed Assets
ビルド済みのAddressablesとStreamingAssetsを埋め込む。
## Build Addressables with Embedding
Addressablesのビルドと`Embed Assets`を行う。
## Build Addressables and Player with Embedding
`Build Addressables with Embedding`をしてからアプリのビルドを行う。

---
![image](Images/PreferencesAddressable.png)

手動でビルドするので、Preferencesから`Do Not Build Addressables on Player Build`推奨

---
以上の手順で
```csharp
UnityWebRequest.Get("StreamingAssets/sample.txt");
```

やAddressables, Localizationが使えるようになります。
## License

[LICENSE](https://github.com/Akeit0/EmbeddedStreamingAssets/blob/main/LICENSE)
