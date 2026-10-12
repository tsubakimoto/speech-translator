---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0025: Use framework-dependent .NET

## Context and Problem Statement

Windows App SDK Runtimeとは別に、.NET Runtimeの共有依存と同梱を選択する必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 共有.NET Runtimeの更新と小さい成果物を優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: Framework-dependent
- B: Self-contained

## Decision Outcome

選択肢: **"A: Framework-dependent"**

理由: 共有.NET Runtimeの更新と小さい成果物を優先する。

共有.NET Runtimeの更新と小さい成果物を優先し、対象PCへの対応Runtime導入が必要というTrade-offを受け入れる。Windows App SDK Runtime配置（[ADR-0024](0024-use-framework-dependent-windows-app-sdk.md)）とは独立した判断。具体的なRuntime種別・Version・ローカル導入手順は採用テンプレートと依存互換性を確認して計画へ記載する。

### Consequences

- 良い点: 成果物を小さくし共有Runtimeの更新を利用。
- 悪い点: 対象PCに対応する.NET Runtimeが必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: Framework-dependent

- 良い点: 成果物を小さくし共有Runtimeの更新を利用。
- 悪い点: 対象PCに対応する.NET Runtimeが必要。

### B: Self-contained

- 良い点: 対応する.NET Runtimeを同梱。
- 悪い点: 成果物が大きくなりRuntime更新時に再Publishが必要。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD29。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0020: Target x64](0020-target-x64.md)
- [ADR-0024: Use framework-dependent Windows App SDK](0024-use-framework-dependent-windows-app-sdk.md)
- [ADR-0029: Use .NET 10 and stable packages](0029-use-dotnet-ten-and-stable-packages.md)
