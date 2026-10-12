---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0004: Use CommunityToolkit MVVM

## Context and Problem Statement

既存WPFは手動MVVM／Commandを使用する。WinUI独立実装のMVVM基盤を決める必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 定型コードの削減とCommand実装の標準化を優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: CommunityToolkit.Mvvm
- B: 手動MVVM／Command
- C: code-behind中心

## Decision Outcome

選択肢: **"A: CommunityToolkit.Mvvm"**

理由: 定型コードの削減とCommand実装の標準化を優先する。

定型コードを削減しCommand実装を標準化する代わりにPackage・source generator依存を受け入れる。Version・具体的な属性利用は未確定。

### Consequences

- 良い点: 定型コード削減、Command実装の標準化。
- 悪い点: Package・source generatorに依存。

## Validation

Not specified.

## Pros and Cons of the Options

### A: CommunityToolkit.Mvvm

- 良い点: 定型コード削減、Command実装の標準化。
- 悪い点: Package・source generatorに依存。

### B: 手動MVVM／Command

- 良い点: 既存構造に近く生成コード不要。
- 悪い点: 非同期・再入制御を自前で保守。

### C: code-behind中心

- 良い点: 小規模画面の初期実装が単純。
- 悪い点: UIと処理が結合し単体テストが難しい。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD6。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0001: Isolate WinUI implementation](0001-isolate-winui-implementation.md)
- [ADR-0005: Compose dependencies manually](0005-compose-dependencies-manually.md)
- [ADR-0029: Use .NET 10 and stable packages](0029-use-dotnet-ten-and-stable-packages.md)
