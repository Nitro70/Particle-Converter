**日本語** / [English](https://github.com/kemo14331/Particle-Converter/blob/main/README_EN.md)
# Particle Converter 
![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/kemo14331/Particle-Converter)  [![GitHub license](https://img.shields.io/github/license/kemo14331/Particle-Converter)](https://github.com/kemo14331/Particle-Converter/blob/main/LICENSE)  
画像ファイルをmcfunctionに変換するツール

## ScreenShot
 ![screenshot0](https://imgur.com/SEKM371.jpg,"screenshot")
 <details>
 <summary>and more</summary><div>  
 <img src="https://imgur.com/Ld544Cx.jpg", "screenshot1">
 <img src="https://imgur.com/hdSbSkc.jpg" alt="screenshot2" />
 </div></details>  

> **このフォークはMinecraft 26.2に対応しています。** 本家は1.16が最後の対応で、現在は出力がそのままでは動きません
> (1.20.5でパーティクルオプションがSNBTに変わり、1.21でデータパックの`functions`ディレクトリが`function`に改名されました)。
> 詳細は[README_EN.md](README_EN.md#whats-different-in-this-fork)を参照してください。

## Feature
* 画像ファイル(.jpg|.png)をMinecraftで表示可能なparticleコマンドに変換し、mcfunction形式で出力
* ワールド相対座標(\~)とローカル相対座標(\^)に対応
* パラメータの変更をリアルタイムでプレビュー可能
* 表示サイズをブロック単位で指定可能
* 解像度の変更をサポート
* dustの色指定に対応
* dust以外のパーティクルに対応
* アプリの多言語対応
* **データパックとして出力** - `pack.mcmeta`と`data/<名前空間>/function/<名前>.mcfunction`を生成し、実行する`/function`コマンドを表示
* **1.16.5から26.2まで**の任意のバージョンを選択でき、コマンド構文・ディレクトリ名・pack formatを自動で切り替え
* パーティクルサイズの上限を`1.00`から、バニラの実際の上限である`4.00`に修正

## Downloads
 [Particle-Converter/Release](https://github.com/kemo14331/Particle-Converter/releases/latest)

## Requirement

 * .NET 10 デスクトップ ランタイム
 
## Library
 * [Material Design In Xaml](http://materialdesigninxaml.net/)
 * [OpenCVSharp4](https://github.com/shimat/opencvsharp)
 * [HelixToolkit.SharpDX.Core.Wpf](https://github.com/helix-toolkit/helix-toolkit) 

## Author

* Kemo431  
* Twitter: [@newkemo431](https://twitter.com/newkemo431)
 
## License
This app is under the [MIT license](https://en.wikipedia.org/wiki/MIT_License).
