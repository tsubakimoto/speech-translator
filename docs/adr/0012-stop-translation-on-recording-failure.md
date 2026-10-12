---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0012: Stop translation on recording failure

## Context and Problem Statement

翻訳中に保存先切断・アクセス権変更・容量不足などで記録追記が失敗する可能性がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 記録不能のまま翻訳を継続しないことを優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 翻訳を停止しエラー表示
- B: 記録を無効化し翻訳を継続、目立つ警告表示
- C: 保存先を再選択して再開するまで一時停止

## Decision Outcome

選択肢: **"A: 翻訳を停止しエラー表示"**

理由: 記録不能のまま翻訳を継続しないことを優先する。

記録不能のまま処理を継続しないことを優先し、翻訳中断を受け入れる。記録失敗を契機に停止処理を実行し、エラーを表示する。停止処理自体の失敗は終了・Lifecycle設計で扱う。失敗時点の結果の完全保存は保証せず、自動再試行・保留結果の再保存を追加しない。

### Consequences

- 良い点: 記録できないまま翻訳が進むことを抑える。
- 悪い点: 翻訳も中断し、既に認識・表示された結果の完全保存は保証できない。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 翻訳を停止しエラー表示

- 良い点: 記録できないまま翻訳が進むことを抑える。
- 悪い点: 翻訳も中断し、既に認識・表示された結果の完全保存は保証できない。

### B: 記録を無効化し翻訳を継続、目立つ警告表示

- 良い点: 翻訳の可用性を優先。
- 悪い点: 以後の結果がファイルに残らない。

### C: 保存先を再選択して再開するまで一時停止

- 良い点: 同じ利用フローで復旧可能。
- 悪い点: 保留結果・再開・重複書き込みの管理が複雑化する。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD14。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0011: Block start on invalid recording folder](0011-block-start-on-invalid-recording-folder.md)
- [ADR-0022: Warn and wait on slow stop](0022-warn-and-wait-on-slow-stop.md)
- [ADR-0023: Retain error state and retry stop](0023-retain-error-state-and-retry-stop.md)
