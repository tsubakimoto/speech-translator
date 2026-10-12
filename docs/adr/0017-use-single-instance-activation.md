---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0017: Use single-instance activation

## Context and Problem Statement

WinUIアプリの複数起動は同一マイク・設定DB・同名記録ファイルの競合につながる。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 競合を抑制し、二回目の起動から既存ウィンドウへ誘導する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 単一起動、二回目の起動は既存ウィンドウへ誘導
- B: 複数起動を許可
- C: 単一起動、二回目は通知して終了

## Decision Outcome

選択肢: **"A: 単一起動、二回目の起動は既存ウィンドウへ誘導"**

理由: 競合を抑制し、二回目の起動から既存ウィンドウへ誘導する。

WinUIアプリ自身の多重起動を抑止し、起動情報を既存インスタンスへ転送してウィンドウを表示・アクティブ化する。マイク・設定DB・記録先の競合抑制を優先し、起動判定・Activation転送の実装を受け入れる。WPFとの相互排他は含めない。

### Consequences

- 良い点: マイク・設定DB・記録先の競合を避けやすい。
- 悪い点: 起動判定とActivation転送の実装が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 単一起動、二回目の起動は既存ウィンドウへ誘導

- 良い点: マイク・設定DB・記録先の競合を避けやすい。
- 悪い点: 起動判定とActivation転送の実装が必要。

### B: 複数起動を許可

- 良い点: 別セッションを同時利用可能。
- 悪い点: 同一マイク、設定更新、同名記録ファイルへの競合対策が必要。

### C: 単一起動、二回目は通知して終了

- 良い点: 競合を避け、Activation転送が不要。
- 悪い点: 既存ウィンドウへ自動誘導できない。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD19。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0006: Store settings with SQLite and DPAPI](0006-store-settings-with-sqlite-and-dpapi.md)
- [ADR-0009: Use user-selected recording folder](0009-use-user-selected-recording-folder.md)
- [ADR-0015: Stop and release before closing](0015-stop-and-release-before-closing.md)
