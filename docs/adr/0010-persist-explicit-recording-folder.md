---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0010: Persist explicit recording folder

## Context and Problem Statement

利用者が選択する記録フォルダーについて、初期値と起動間の保持方針が必要となる。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 暗黙の保存先を避け、二回目以降の選択操作を削減する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 初期値なし、選択したフォルダーを次回起動でも再利用
- B: Documents配下を初期値にして選択で変更、次回起動でも再利用
- C: 初期値なし、起動ごとに選択

## Decision Outcome

選択肢: **"A: 初期値なし、選択したフォルダーを次回起動でも再利用"**

理由: 暗黙の保存先を避け、二回目以降の選択操作を削減する。

初回の記録には保存先の明示選択が必要。選択先を永続化し次回起動時に復元する。暗黙の保存を避け、二回目以降の操作を削減する代わりに、保存先の移動・削除・アクセス権変更への対応が必要となる。ファイル名が空なら保存先未選択でも記録なしで翻訳可能。永続化の具体的方法は実装計画で整理する。

### Consequences

- 良い点: 暗黙の保存先を避け、二回目以降の操作を削減。
- 悪い点: 初回の記録には選択が必要で、保存先が無効になった場合の対応も必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 初期値なし、選択したフォルダーを次回起動でも再利用

- 良い点: 暗黙の保存先を避け、二回目以降の操作を削減。
- 悪い点: 初回の記録には選択が必要で、保存先が無効になった場合の対応も必要。

### B: Documents配下を初期値にして選択で変更、次回起動でも再利用

- 良い点: 初回から記録可能。
- 悪い点: 明示選択前に既定先へ書き込み、Documentsの同期設定に従う。

### C: 初期値なし、起動ごとに選択

- 良い点: 古い選択を持ち越さない。
- 悪い点: 記録利用のたびに操作が増える。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD12。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0009: Use user-selected recording folder](0009-use-user-selected-recording-folder.md)
- [ADR-0011: Block start on invalid recording folder](0011-block-start-on-invalid-recording-folder.md)
- [ADR-0027: Persist preferences on selection](0027-persist-preferences-on-selection.md)
- [ADR-0028: Rollback preferences on save failure](0028-rollback-preferences-on-save-failure.md)
