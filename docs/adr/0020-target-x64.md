---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0020: Target x64

## Context and Problem Statement

WinUIとSpeech SDKのnative依存を含むBuild・MSIX・実機検証について、製品対応CPU architectureを決める必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- Build・配布・実機検証の範囲をx64へ限定する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: x64のみ
- B: x64＋ARM64
- C: ARM64のみ

## Decision Outcome

選択肢: **"A: x64のみ"**

理由: Build・配布・実機検証の範囲をx64へ限定する。

Build・配布・実機検証の範囲を限定し、ARM64ネイティブ版は提供しない。ARM64上のエミュレーション動作は今回の保証対象外。

### Consequences

- 良い点: Build・配布・実機検証を絞れる。
- 悪い点: ARM64ネイティブ版は提供しない。ARM64上のエミュレーション動作は今回の保証対象外。

## Validation

Not specified.

## Pros and Cons of the Options

### A: x64のみ

- 良い点: Build・配布・実機検証を絞れる。
- 悪い点: ARM64ネイティブ版は提供しない。ARM64上のエミュレーション動作は今回の保証対象外。

### B: x64＋ARM64

- 良い点: 両architectureへネイティブ版を提供。
- 悪い点: architecture別のBuild・MSIX成果物・Speech SDKを含むnative依存と実機検証が必要。

### C: ARM64のみ

- 良い点: ARM環境に集中できる。
- 悪い点: x64 Windows環境では利用不可。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD22。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0019: Target Windows 11](0019-target-windows-eleven.md)
- [ADR-0024: Use framework-dependent Windows App SDK](0024-use-framework-dependent-windows-app-sdk.md)
- [ADR-0025: Use framework-dependent .NET](0025-use-framework-dependent-dotnet.md)
