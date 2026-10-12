---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0014: Commit service settings explicitly

## Context and Problem Statement

独立した設定画面ではRegion／API Keyの編集状態と保存済み状態、画面離脱時の扱いを区別する必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 保存成功時のみ設定を確定し、未保存入力の誤破棄を防ぐ。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 保存＋キャンセル、未保存の画面離脱時に確認
- B: 保存＋キャンセル、画面離脱時は未保存の編集を破棄
- C: 入力変更時に自動保存

## Decision Outcome

選択肢: **"A: 保存＋キャンセル、未保存の画面離脱時に確認"**

理由: 保存成功時のみ設定を確定し、未保存入力の誤破棄を防ぐ。

Region／API Keyは編集用状態で保持し、保存成功時のみ確定する。キャンセルは保存済み値へ復元。未保存の画面離脱時は保存・破棄・離脱中止を確認する。保存失敗時は編集内容を保持し、保存済み値を更新しない。誤破棄防止を優先し、編集状態・離脱確認の管理を受け入れる。

### Consequences

- 良い点: 保存成功時のみ確定し、誤破棄を防げる。
- 悪い点: 編集用状態と離脱確認が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 保存＋キャンセル、未保存の画面離脱時に確認

- 良い点: 保存成功時のみ確定し、誤破棄を防げる。
- 悪い点: 編集用状態と離脱確認が必要。

### B: 保存＋キャンセル、画面離脱時は未保存の編集を破棄

- 良い点: 操作と状態管理が単純。
- 悪い点: 未保存の入力が確認なしに失われる。

### C: 入力変更時に自動保存

- 良い点: 保存操作不要。
- 悪い点: 不完全な入力の扱い、保存頻度・失敗通知が複雑。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD16。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0003: Separate translation and settings](0003-separate-translation-and-settings.md)
- [ADR-0006: Store settings with SQLite and DPAPI](0006-store-settings-with-sqlite-and-dpapi.md)
- [ADR-0015: Stop and release before closing](0015-stop-and-release-before-closing.md)
- [ADR-0021: Recover settings load failures in app](0021-recover-settings-load-failures-in-app.md)
- [ADR-0027: Persist preferences on selection](0027-persist-preferences-on-selection.md)
