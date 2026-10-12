---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0007: Keep WPF settings independent

## Context and Problem Statement

WinUI専用設定の導入に際し、既存WPF設定をコピー・Importするか、独立設定するかを検討した。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- WPFへの依存を避け、設定の独立性と移行処理が不要な境界を優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 引き継がず独立設定
- B: 初回にWPF設定をコピー
- C: ユーザー操作でImport

## Decision Outcome

選択肢: **"A: 引き継がず独立設定"**

理由: WPFへの依存を避け、設定の独立性と移行処理が不要な境界を優先する。

自動コピー・手動Import・旧DB参照を追加しない。[ADR-0006](0006-store-settings-with-sqlite-and-dpapi.md)の変更により通常保存にはSQLiteを使用するが、移行用途には使用しない。Region／API KeyはWinUIで設定する（既存機能としての環境変数フォールバックは移植対象のまま）。設定の独立性と移行処理不要を優先し、再入力を受け入れる。

### Consequences

- 良い点: WPFへの依存なし、移行処理不要。
- 悪い点: Region／API Keyの再入力が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 引き継がず独立設定

- 良い点: WPFへの依存なし、移行処理不要。
- 悪い点: Region／API Keyの再入力が必要。

### B: 初回にWPF設定をコピー

- 良い点: 再入力不要、以後は独立。
- 悪い点: SQLite読み込みとDPAPI互換の検証が必要。

### C: ユーザー操作でImport

- 良い点: 明示的に引き継ぎを選べる。
- 悪い点: Import UI・エラー処理が追加。

## More Information

初回WPF設定コピーを一度選択した後、ユーザーが取り消し、代替比較で独立設定を明示選択した。代替比較は「A: 引き継がず独立設定」（良い点: SQLite依存・移行処理不要、悪い点: Region／API Keyの再入力）と「B: ユーザー操作でImport」（良い点: 必要時だけ引き継げる、悪い点: SQLite依存とImport UIが必要）。このSQLite依存の評価はJSON採用時点の比較であり、現在は通常保存にSQLiteを使用する[ADR-0006](0006-store-settings-with-sqlite-and-dpapi.md)が成立している。取り消されたコピー方針を有効なDecisionとして扱わない。

元の検討はSpeechTranslatorWinUI要件・実装計画のD9。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

JSON＋DPAPIと初回WPF設定コピーの過去の選択は撤回済み。本ADRは現在有効な選択のみを記録し、撤回Optionの独立したrejected ADRは作成しない。

- [ADR-0001: Isolate WinUI implementation](0001-isolate-winui-implementation.md)
- [ADR-0006: Store settings with SQLite and DPAPI](0006-store-settings-with-sqlite-and-dpapi.md)
- [ADR-0008: Store settings in package LocalFolder](0008-store-settings-in-package-local-folder.md)
