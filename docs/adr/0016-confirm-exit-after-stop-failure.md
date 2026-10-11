---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0016: Confirm exit after stop failure

## Context and Problem Statement

終了時の停止処理は失敗または長時間未完了となり得る。正常停止を待つことと終了応答を両立する必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- UIの無期限待機を避け、停止失敗・期限超過時の終了判断を利用者に委ねる。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 期限付き待機、失敗・期限超過時に再試行／終了中止／そのまま終了を確認
- B: 期限付き待機、失敗・期限超過時は警告して終了中止（再試行のみ）
- C: 停止完了まで期限なしで待機

## Decision Outcome

選択肢: **"A: 期限付き待機、失敗・期限超過時に再試行／終了中止／そのまま終了を確認"**

理由: UIの無期限待機を避け、停止失敗・期限超過時の終了判断を利用者に委ねる。

停止失敗時または待機期限超過時に利用者へ確認する。UIの無期限待機を避け、終了判断を利用者に委ねる代わりに、エラー状態・確認UI・遅延完了を管理する。そのまま終了した場合は未保存記録や完全なリソース解放を保証しない。停止処理が未完了の場合、再試行で重複した停止処理を起動しない。待機期限は10秒の実装定数とし、ユーザー向け変更設定を追加しない。期限超過は停止Taskのキャンセル・完了を意味しない。10秒は正常停止時間の保証ではない。

### Consequences

- 良い点: UIの無期限待機を避け、終了判断を利用者に委ねる。
- 悪い点: エラー状態・確認UI・遅延完了の管理が必要。そのまま終了した場合は未保存記録や完全なリソース解放を保証しない。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 期限付き待機、失敗・期限超過時に再試行／終了中止／そのまま終了を確認

- 良い点: UIの無期限待機を避け、終了判断を利用者に委ねる。
- 悪い点: エラー状態・確認UI・遅延完了の管理が必要。そのまま終了した場合は未保存記録や完全なリソース解放を保証しない。

### B: 期限付き待機、失敗・期限超過時は警告して終了中止（再試行のみ）

- 良い点: 不完全な停止での終了を避ける。
- 悪い点: 復旧できない場合アプリから終了できない。

### C: 停止完了まで期限なしで待機

- 良い点: 正常停止を優先。
- 悪い点: SDKやネットワークの問題で終了が無期限に遅れる可能性。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD18。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

待機期限として実際に比較したOptions: 5秒（応答を優先するが一時的遅延でも確認が出やすい）、10秒（終了応答と正常停止待機を両立する初期候補）、30秒（遅延への余裕があるが終了待ちが長い）。ユーザーは10秒を選択した。

- [ADR-0015: Stop and release before closing](0015-stop-and-release-before-closing.md)
- [ADR-0022: Warn and wait on slow stop](0022-warn-and-wait-on-slow-stop.md)
- [ADR-0023: Retain error state and retry stop](0023-retain-error-state-and-retry-stop.md)
