---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0028: Rollback preferences on save failure

## Context and Problem Statement

言語・保存フォルダーの選択変更時の自動保存は失敗し得る。現在値と次回起動時の保存値の整合性を決める必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 画面の確定選択と保存済み値の一致を優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 変更前の選択へ戻してエラー表示
- B: 今回だけ変更を適用し、未保存を警告

## Decision Outcome

選択肢: **"A: 変更前の選択へ戻してエラー表示"**

理由: 画面の確定選択と保存済み値の一致を優先する。

保存成功後に選択を確定し、失敗時は変更前の言語・保存フォルダーへ戻す。画面と保存済み値の一致を優先し、選び直しが必要というTrade-offを受け入れる。初回フォルダー保存が失敗した場合は未選択のままとする。Region／API Keyは[ADR-0014](0014-commit-service-settings-explicitly.md)どおり編集内容を保持する。非同期保存中は再入・翻訳開始による未確定値の使用を防ぎ、古い保存完了が新しい選択を上書きしない。

### Consequences

- 良い点: 画面と保存済み値が一致。
- 悪い点: 選び直しが必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 変更前の選択へ戻してエラー表示

- 良い点: 画面と保存済み値が一致。
- 悪い点: 選び直しが必要。

### B: 今回だけ変更を適用し、未保存を警告

- 良い点: 現在の操作を継続できる。
- 悪い点: 現在値と次回起動時の値が異なる。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD32。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0010: Persist explicit recording folder](0010-persist-explicit-recording-folder.md)
- [ADR-0014: Commit service settings explicitly](0014-commit-service-settings-explicitly.md)
- [ADR-0026: Persist languages not recording name](0026-persist-languages-not-recording-name.md)
- [ADR-0027: Persist preferences on selection](0027-persist-preferences-on-selection.md)
