---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0024: Use framework-dependent Windows App SDK

## Context and Problem Statement

MSIX採用とは独立して、Windows App SDK Runtimeを共有依存とするかアプリに同梱するかを決める必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 共有Windows App SDK Runtimeの標準serviceabilityを優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: Framework-dependent
- B: Self-contained

## Decision Outcome

選択肢: **"A: Framework-dependent"**

理由: 共有Windows App SDK Runtimeの標準serviceabilityを優先する。

Windows App SDK Runtimeを共有依存として利用し、標準のserviceabilityを優先する。ローカルMSIX導入時に対象architectureのRuntime／Framework package依存の準備・解決が必要というTrade-offを受け入れる。.NET Runtimeの配置は独立判断であり、本選択で確定しない。

### Consequences

- 良い点: SDK標準方式で、共有Runtimeの更新を利用。
- 悪い点: ローカル導入時にRuntime依存の準備が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: Framework-dependent

- 良い点: SDK標準方式で、共有Runtimeの更新を利用。
- 悪い点: ローカル導入時にRuntime依存の準備が必要。

### B: Self-contained

- 良い点: アプリ側でSDK Runtimeを同梱・管理。
- 悪い点: 同梱Runtimeの更新にはアプリ再Buildが必要。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD28。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0002: Use MSIX](0002-use-msix.md)
- [ADR-0020: Target x64](0020-target-x64.md)
- [ADR-0025: Use framework-dependent .NET](0025-use-framework-dependent-dotnet.md)
- [ADR-0029: Use .NET 10 and stable packages](0029-use-dotnet-ten-and-stable-packages.md)
