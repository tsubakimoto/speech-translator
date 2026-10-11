---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0008: Store settings in package LocalFolder

## Context and Problem Statement

MSIXを採用したWinUI専用設定DBについて、保存先とアンインストール後のLifecycleを決める必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- Package identity・ユーザー単位の分離と通常更新時の保持を優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: Package専用LocalFolder
- B: Package外のユーザー領域

## Decision Outcome

選択肢: **"A: Package専用LocalFolder"**

理由: Package identity・ユーザー単位の分離と通常更新時の保持を優先する。

WinUI専用SQLite DBをPackageのLocalFolderに保存する。Package identity単位・ユーザー単位の分離と通常更新時の保持を優先し、アンインストール後の再設定を受け入れる。物理パスを固定せずApplicationDataのLocalFolderから取得する。記録テキストの保存先は[ADR-0009](0009-use-user-selected-recording-folder.md)に従う。

### Consequences

- 良い点: Package identity単位・ユーザー単位で分離し、通常の更新で保持される。
- 悪い点: アンインストールで削除され、再インストール時に再設定が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: Package専用LocalFolder

- 良い点: Package identity単位・ユーザー単位で分離し、通常の更新で保持される。
- 悪い点: アンインストールで削除され、再インストール時に再設定が必要。

### B: Package外のユーザー領域

- 良い点: 再インストール後も保持する方針にできる。
- 悪い点: 残存Secretの管理とMSIX仮想化対象外への保存設計・検証が必要。単純なLocalAppDataパス指定だけで非仮想化・保持を保証しない。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD10。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0002: Use MSIX](0002-use-msix.md)
- [ADR-0006: Store settings with SQLite and DPAPI](0006-store-settings-with-sqlite-and-dpapi.md)
- [ADR-0009: Use user-selected recording folder](0009-use-user-selected-recording-folder.md)
