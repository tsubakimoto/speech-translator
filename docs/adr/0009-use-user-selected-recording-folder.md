---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0009: Use user-selected recording folder

## Context and Problem Statement

記録は音声録音ではなくUTF-8テキスト追記である。既存WPFの実行ファイル配下への保存を、MSIXのWinUIへそのまま持ち込まない。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 記録を設定DBから分離し、利用者が保存・同期先を制御できることを優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: Documents配下のSpeechTranslatorWinUI\\recordingsへ固定
- B: ユーザーが保存フォルダーを選択
- C: Package専用LocalFolder内のrecordings

## Decision Outcome

選択肢: **"B: ユーザーが保存フォルダーを選択"**

理由: 記録を設定DBから分離し、利用者が保存・同期先を制御できることを優先する。

記録テキストを設定DBから分離し、利用者が保存・同期先を制御できる方式とする。Folder Picker、選択状態の保持、アクセス不可時の対応が必要というTrade-offを受け入れる。既存のテキスト追記形式・ファイル名検証を維持する。初期値・保持は関連ADRに従い、旧記録の自動移行は今回の対象外。

### Consequences

- 良い点: 保存・同期先を利用者が制御。
- 悪い点: Folder Picker、選択状態の保持、アクセス不可時の対応が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: Documents配下のSpeechTranslatorWinUI\\recordingsへ固定

- 良い点: 保存先が予測可能で、Package削除と独立して記録を保持。
- 悪い点: 保存先を選べず、DocumentsのOneDrive等へのリダイレクトがある場合はその保存先・同期方針に従う。

### B: ユーザーが保存フォルダーを選択

- 良い点: 保存・同期先を利用者が制御。
- 悪い点: Folder Picker、選択状態の保持、アクセス不可時の対応が必要。

### C: Package専用LocalFolder内のrecordings

- 良い点: 保存実装と管理が単純。
- 悪い点: アンインストールで記録が失われ、ユーザーデータの保存先として非推奨。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD11。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0002: Use MSIX](0002-use-msix.md)
- [ADR-0008: Store settings in package LocalFolder](0008-store-settings-in-package-local-folder.md)
- [ADR-0010: Persist explicit recording folder](0010-persist-explicit-recording-folder.md)
- [ADR-0011: Block start on invalid recording folder](0011-block-start-on-invalid-recording-folder.md)
- [ADR-0012: Stop translation on recording failure](0012-stop-translation-on-recording-failure.md)
