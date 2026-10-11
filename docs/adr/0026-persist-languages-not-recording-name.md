---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0026: Persist languages not recording name

## Context and Problem Statement

既存の初期値は英語から日本語で、記録ファイル名が空なら記録しない。WinUIでは起動間で保持する項目を決める必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 言語選択の再操作を削減し、前回ファイル名の復元による暗黙の追記を避ける。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 起動ごとに初期化
- B: 言語のみ保持
- C: 言語・ファイル名とも保持

## Decision Outcome

選択肢: **"B: 言語のみ保持"**

理由: 言語選択の再操作を削減し、前回ファイル名の復元による暗黙の追記を避ける。

話者言語・翻訳先言語を次回起動時に復元し、記録ファイル名は起動ごとに空とする。初回／未保存時は既存どおり英語→日本語。保存フォルダーは[ADR-0010](0010-persist-explicit-recording-folder.md)どおり保持する。言語選択の再操作削減を優先し、永続化項目の追加を受け入れる。前回ファイル名の復元による暗黙の追記は避ける。保存タイミング・保存失敗時の挙動は関連ADRに従う。

### Consequences

- 良い点: 言語選択を省け、記録は明示操作。
- 悪い点: 永続化する設定項目が増える。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 起動ごとに初期化

- 中立: 毎回英語→日本語・ファイル名空で開始。
- 悪い点: 言語とファイル名を再入力。

### B: 言語のみ保持

- 良い点: 言語選択を省け、記録は明示操作。
- 悪い点: 永続化する設定項目が増える。

### C: 言語・ファイル名とも保持

- 良い点: 前回の操作を再利用。
- 悪い点: 前回ファイルへの意図しない追記に注意。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD30。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0010: Persist explicit recording folder](0010-persist-explicit-recording-folder.md)
- [ADR-0027: Persist preferences on selection](0027-persist-preferences-on-selection.md)
- [ADR-0028: Rollback preferences on save failure](0028-rollback-preferences-on-save-failure.md)
