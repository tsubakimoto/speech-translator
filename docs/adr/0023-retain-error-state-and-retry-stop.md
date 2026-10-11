---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0023: Retain error state and retry stop

## Context and Problem Statement

通常停止が例外終了してもSpeechセッションが実際に停止済みとは断定できない。失敗後の復旧方針が必要となる。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- エラー状態を保持し、アプリ内で停止の復旧を試せることを優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: エラー状態を保持し、停止再試行を許可
- B: エラー表示し、アプリ再起動で復旧

## Decision Outcome

選択肢: **"A: エラー状態を保持し、停止再試行を許可"**

理由: エラー状態を保持し、アプリ内で停止の復旧を試せることを優先する。

停止失敗を停止済みと断定せず、再開始・設定変更は復旧まで禁止する。前の停止Taskが例外終了した後のみ再試行を許可し、未完了Taskとの重複実行を避ける。アプリ内での復旧を優先し、エラー状態と再試行の管理を受け入れる。終了操作には[ADR-0015](0015-stop-and-release-before-closing.md)〜[ADR-0016](0016-confirm-exit-after-stop-failure.md)を適用する。記録失敗を契機とした停止にも同じ失敗ポリシーを適用する。Sharedの停止失敗後の再試行可能性は技術検証が必要であり、成功を保証しない。

### Consequences

- 良い点: アプリ内で復旧を試せる。
- 悪い点: 復旧まで再開始・設定変更不可。

## Validation

実装計画の技術検証項目として、Sharedの停止失敗後に再試行できるかを確認する。未検証の再試行成功を保証しない。

## Pros and Cons of the Options

### A: エラー状態を保持し、停止再試行を許可

- 良い点: アプリ内で復旧を試せる。
- 悪い点: 復旧まで再開始・設定変更不可。

### B: エラー表示し、アプリ再起動で復旧

- 良い点: 復旧経路が単純。
- 悪い点: 利用を中断し未保存内容の確認が必要。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD27。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0012: Stop translation on recording failure](0012-stop-translation-on-recording-failure.md)
- [ADR-0013: Freeze settings during translation](0013-freeze-settings-during-translation.md)
- [ADR-0016: Confirm exit after stop failure](0016-confirm-exit-after-stop-failure.md)
- [ADR-0022: Warn and wait on slow stop](0022-warn-and-wait-on-slow-stop.md)
