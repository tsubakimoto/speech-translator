---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0006: Store settings with SQLite and DPAPI

## Context and Problem Statement

既存WPFはSQLiteへRegionとDPAPIで保護したAPI Keyを保存する。WinUI側に独立した設定Storeが必要となる。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- SQLiteのトランザクションによる保存整合性とDPAPIによるAPI Key保護を優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: SQLite＋DPAPIを継承
- B: JSON＋DPAPI
- C: Windowsの資格情報ストア＋別設定保存

## Decision Outcome

選択肢: **"A: SQLite＋DPAPIを継承"**

理由: SQLiteのトランザクションによる保存整合性とDPAPIによるAPI Key保護を優先する。

ユーザーの「SQLiteに変更」により、先に選択したJSON＋DPAPIを取り消し、通常の設定保存をSQLiteへ変更する。API KeyのDPAPI保護は維持する。トランザクションによる整合性管理を利用できる代わりにSQLite依存を受け入れる。[ADR-0001](0001-isolate-winui-implementation.md)に従いWinUI側に独立実装し、[ADR-0007](0007-keep-wpf-settings-independent.md)に従いWPF設定の共有・コピー・Importは行わない。WinUI専用DBとし、旧WPF DBを参照・更新しない。Package versionは未確定。

### Consequences

- 良い点: 既存と同じ方式で検証できる。
- 悪い点: SQLite依存、Windowsユーザーに紐づく暗号化。

## Validation

Not specified.

## Pros and Cons of the Options

### A: SQLite＋DPAPIを継承

- 良い点: 既存と同じ方式で検証できる。
- 悪い点: SQLite依存、Windowsユーザーに紐づく暗号化。

### B: JSON＋DPAPI

- 良い点: 単一設定には簡潔でSQLite不要。
- 悪い点: ファイルの整合性・原子的保存を自前管理。

### C: Windowsの資格情報ストア＋別設定保存

- 良い点: Secretを専用ストアへ分離。
- 悪い点: 保存先が分かれ、API・移行検証が増える。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD8。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

JSON＋DPAPIと初回WPF設定コピーの過去の選択は撤回済み。本ADRは現在有効な選択のみを記録し、撤回Optionの独立したrejected ADRは作成しない。

- [ADR-0001: Isolate WinUI implementation](0001-isolate-winui-implementation.md)
- [ADR-0007: Keep WPF settings independent](0007-keep-wpf-settings-independent.md)
- [ADR-0008: Store settings in package LocalFolder](0008-store-settings-in-package-local-folder.md)
- [ADR-0021: Recover settings load failures in app](0021-recover-settings-load-failures-in-app.md)
