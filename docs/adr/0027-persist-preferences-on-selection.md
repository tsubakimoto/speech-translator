---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0027: Persist preferences on selection

## Context and Problem Statement

言語と保存フォルダーの起動間保持が選択されたため、永続化タイミングを決める必要がある。Region／API Keyは明示保存する別の編集境界である。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 追加の保存操作をなくし、終了前にも選択を保存できることを優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 選択変更時に保存
- B: 明示的な保存ボタン
- C: 終了時に保存

## Decision Outcome

選択肢: **"A: 選択変更時に保存"**

理由: 追加の保存操作をなくし、終了前にも選択を保存できることを優先する。

言語選択・保存フォルダーは変更時に永続化し、追加の保存ボタンを設けない。終了前にも保存できる操作性を優先し、保存失敗の通知・状態管理を受け入れる。Region／API Keyは[ADR-0014](0014-commit-service-settings-explicitly.md)の保存・キャンセルを維持し、自動保存に変更しない。Pickerキャンセルは変更扱いにしない。保存失敗時の現在値の扱いは関連ADRに従う。

### Consequences

- 良い点: 追加操作不要、終了前にも保存できる。
- 悪い点: 保存失敗の通知・状態管理が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 選択変更時に保存

- 良い点: 追加操作不要、終了前にも保存できる。
- 悪い点: 保存失敗の通知・状態管理が必要。

### B: 明示的な保存ボタン

- 良い点: 保存の意図が明確。
- 悪い点: 操作と未保存管理が増える。

### C: 終了時に保存

- 良い点: 操作中の書き込みを減らせる。
- 悪い点: 異常終了時に選択が失われる。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD31。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0010: Persist explicit recording folder](0010-persist-explicit-recording-folder.md)
- [ADR-0014: Commit service settings explicitly](0014-commit-service-settings-explicitly.md)
- [ADR-0026: Persist languages not recording name](0026-persist-languages-not-recording-name.md)
- [ADR-0028: Rollback preferences on save failure](0028-rollback-preferences-on-save-failure.md)
