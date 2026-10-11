---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0022: Warn and wait on slow stop

## Context and Problem Statement

通常の停止操作でも停止Taskが長時間未完了となり得る。期限超過は停止成功やTaskのキャンセルを意味しない。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 停止の応答状況を可視化し、未完了中に再開始や設定変更を許可しない。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 10秒後に警告し、停止待機を継続
- B: 期限なしで待機

## Decision Outcome

選択肢: **"A: 10秒後に警告し、停止待機を継続"**

理由: 停止の応答状況を可視化し、未完了中に再開始や設定変更を許可しない。

通常の停止操作で10秒を超えたら警告し、停止Taskの完了を引き続き待つ。停止成功として扱わず、再開始・設定変更を許可しない。UIスレッドをブロックせず、ウィンドウ終了操作には[ADR-0015](0015-stop-and-release-before-closing.md)〜[ADR-0016](0016-confirm-exit-after-stop-failure.md)の確認フローを適用する。停止Taskを重複実行しない。期限は実装定数でユーザー設定なし。応答状況の可視化を優先し停止未完了中の操作制約を受け入れる。停止Taskが例外終了した場合は関連ADRの再試行方針に従う。

### Consequences

- 良い点: 応答状況を把握できる。
- 悪い点: 停止完了まで再開始・設定変更不可。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 10秒後に警告し、停止待機を継続

- 良い点: 応答状況を把握できる。
- 悪い点: 停止完了まで再開始・設定変更不可。

### B: 期限なしで待機

- 良い点: 状態管理が単純。
- 悪い点: 停止が長引くと操作できない状態が続く。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD26。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0013: Freeze settings during translation](0013-freeze-settings-during-translation.md)
- [ADR-0016: Confirm exit after stop failure](0016-confirm-exit-after-stop-failure.md)
- [ADR-0023: Retain error state and retry stop](0023-retain-error-state-and-retry-stop.md)
