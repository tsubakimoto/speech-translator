---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0001: Isolate WinUI implementation

## Context and Problem Statement

既存WPFのViewModel・ServicesはDesktop内にあり、SharedはSpeechセッションとRecognizerイベント連携を提供する。WinUIへの再利用境界を決める必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 今回はWPFを変更しない制約の下で、WinUI側の独立性を優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: WinUI側に独立実装
- B: 共通ライブラリへ抽出
- C: 既存Desktopソースをリンクして共有

## Decision Outcome

選択肢: **"A: WinUI側に独立実装"**

理由: 今回はWPFを変更しない制約の下で、WinUI側の独立性を優先する。

WinUI側にViewModel・Servicesを独立実装し、既存Sharedのみ再利用する。Desktopアセンブリ参照・ソースリンク・共通ライブラリ抽出は行わない。既存Desktopは仕様・テスト観点の参照元とする。

### Consequences

- 良い点: WPF無変更、WinUIに適した設計が可能。
- 悪い点: ロジックの重複・乖離を受け入れる。

## Validation

Not specified.

## Pros and Cons of the Options

### A: WinUI側に独立実装

- 良い点: WPF無変更、WinUIに適した設計が可能。
- 悪い点: ロジックの重複・乖離を受け入れる。

### B: 共通ライブラリへ抽出

- 良い点: 重複抑制、テスト共有。
- 悪い点: WPFの参照・配線変更が必要であり、今回はWPFを変更しない制約への例外許可が必要。

### C: 既存Desktopソースをリンクして共有

- 良い点: WPFソースを変更せず再利用。
- 悪い点: Desktop側の配置・変更に依存。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD3。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0002: Use MSIX](0002-use-msix.md)
- [ADR-0005: Compose dependencies manually](0005-compose-dependencies-manually.md)
- [ADR-0006: Store settings with SQLite and DPAPI](0006-store-settings-with-sqlite-and-dpapi.md)
- [ADR-0007: Keep WPF settings independent](0007-keep-wpf-settings-independent.md)
