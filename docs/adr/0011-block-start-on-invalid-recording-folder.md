---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0011: Block start on invalid recording folder

## Context and Problem Statement

ファイル名が入力され記録を要求している場合でも、保存先が未選択・存在しない・書き込み不可となり得る。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 記録要求を黙って無視せず、開始前に利用者へ修正を促す。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 翻訳開始を阻止して保存先の選び直しを促す
- B: 警告し、ユーザーの確認後に記録なしで翻訳開始
- C: 警告して自動的に記録なしで翻訳開始

## Decision Outcome

選択肢: **"A: 翻訳開始を阻止して保存先の選び直しを促す"**

理由: 記録要求を黙って無視せず、開始前に利用者へ修正を促す。

記録を要求している場合、開始時に保存先が未選択・存在しない・書き込み不可と判明すれば翻訳を開始しない。記録なしで翻訳する場合はファイル名を空にする。記録要求を黙って無視しないことを優先し、修正操作が必要というTrade-offを受け入れる。開始時の確認は翻訳中の書き込み成功を保証しない。

### Consequences

- 良い点: 記録要求を黙って無視しない。
- 悪い点: 記録なしで翻訳したい場合はファイル名を空にする操作が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 翻訳開始を阻止して保存先の選び直しを促す

- 良い点: 記録要求を黙って無視しない。
- 悪い点: 記録なしで翻訳したい場合はファイル名を空にする操作が必要。

### B: 警告し、ユーザーの確認後に記録なしで翻訳開始

- 良い点: 翻訳を優先しつつ明示同意を得る。
- 悪い点: 確認操作・分岐が増える。

### C: 警告して自動的に記録なしで翻訳開始

- 良い点: 操作を中断しない。
- 悪い点: 記録要求に反して記録が残らない。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD13。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0009: Use user-selected recording folder](0009-use-user-selected-recording-folder.md)
- [ADR-0010: Persist explicit recording folder](0010-persist-explicit-recording-folder.md)
- [ADR-0012: Stop translation on recording failure](0012-stop-translation-on-recording-failure.md)
