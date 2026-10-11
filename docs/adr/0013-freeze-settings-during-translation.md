---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0013: Freeze settings during translation

## Context and Problem Statement

Region／API Key、言語、記録ファイル名・保存先の変更は実行中セッションの設定境界に影響する。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- セッション設定と記録先の一貫性、状態管理の単純さを優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 翻訳中は変更不可、停止後に変更
- B: 翻訳中も編集・保存可能、次回開始から適用
- C: 変更時に停止・再開始して即時適用

## Decision Outcome

選択肢: **"A: 翻訳中は変更不可、停止後に変更"**

理由: セッション設定と記録先の一貫性、状態管理の単純さを優先する。

Region／API Key、話者・翻訳先言語、記録ファイル名・保存フォルダーは翻訳中に変更できない。設定画面の閲覧は可能。セッション設定の一貫性と状態管理の単純さを優先し、変更には停止が必要というTrade-offを受け入れる。開始・停止の遷移中も変更を抑止し、停止完了後に編集可能とする。

### Consequences

- 良い点: セッションと記録先が一貫し、状態管理が単純。
- 悪い点: 変更には停止が必要。
- 中立: 設定画面への閲覧Navigationは可能。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 翻訳中は変更不可、停止後に変更

- 良い点: セッションと記録先が一貫し、状態管理が単純。
- 悪い点: 変更には停止が必要。
- 中立: 設定画面への閲覧Navigationは可能。

### B: 翻訳中も編集・保存可能、次回開始から適用

- 良い点: 次回用設定を準備可能。
- 悪い点: 現在値と次回値の区別・表示が必要。

### C: 変更時に停止・再開始して即時適用

- 良い点: すぐ変更を反映。
- 悪い点: 認識の空白時間、再開始失敗、記録の境界処理が増える。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD15。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0003: Separate translation and settings](0003-separate-translation-and-settings.md)
- [ADR-0014: Commit service settings explicitly](0014-commit-service-settings-explicitly.md)
- [ADR-0022: Warn and wait on slow stop](0022-warn-and-wait-on-slow-stop.md)
- [ADR-0023: Retain error state and retry stop](0023-retain-error-state-and-retry-stop.md)
