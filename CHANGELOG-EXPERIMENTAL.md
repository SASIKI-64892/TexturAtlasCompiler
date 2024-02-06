# Changelog

v0.5.2 以降の実験的機能の変更記録です。
[Keep a Changelog](https://keepachangelog.com/en/1.0.0/)のフォーマットにある程度乗っ取りますが、そのさじ加減は適当に決められ、完全にそのフォーマットではないことをご了承ください。

## Unreleased

### Added

### Changed

### Removed

### Fixed

### Deprecated

## 0.5.3

### Added

- TTT PSD Importer の高速化 `#346`
- いくつかのレイヤーが追加されました `#346`
  - 色相・彩度・明度 の色調調整レイヤー HSVAdjustmentLayer が追加
  - バイナリ内のレイヤーイメージを指すオブジェクトを使用する RasterImportedLayer が追加
- レイヤーの追加に伴い、TTT PSD Importer が HSVAdjustmentLayer のインポート機能を追加 `#346`
- TTT PSD Importer が SolidColorLayer のインポート機能を追加 `#346`
- TextureSelector にモードが追加され、Absolute が追加 `#347`
- TextureSelector にアバター内のテクスチャだけを列挙し、選択できる DomainTexturesSelector を追加 `#347`

### Changed

- TTT PSD Importer はコンテキストメニューから、ScriptedImporter に変更 `#346`
- SolidLayer は SolidColorLayer に名称変更 `#346`
- TextureSelector にモードが追加され、以前までのデータは Relative に変更`#347`
  - 上記に伴いフィールド名を変更 `#347`

### Removed

### Fixed

- 内部挙動が大きく変更され `#346`
  - クリッピングが以前よりも再現度が高くなりました
  - LayerFolder の PassThrough が以前よりも再現度が高くなりました