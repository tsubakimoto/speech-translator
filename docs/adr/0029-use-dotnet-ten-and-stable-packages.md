---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0029: Use .NET 10 and stable packages

## Context and Problem Statement

既存Shared・WPF・CIは.NET 10を使用する。WinUI追加時の.NET世代と安定版／Preview利用方針を決める必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 既存Shared・CIと.NET世代を揃え、Previewを避ける。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: .NET 10＋安定版Packageのみ
- B: Previewも許容して検討

## Decision Outcome

選択肢: **"A: .NET 10＋安定版Packageのみ"**

理由: 既存Shared・CIと.NET世代を揃え、Previewを避ける。

既存Shared・CIと.NET世代を揃え、Previewを避ける。Windows App SDK・関連Packageとの互換性確認が必要というTrade-offを受け入れる。具体的なVersionは公式情報・Package互換性の調査後に提示し、ユーザー承認前には確定しない。既存WPFのTarget Framework・依存Versionは変更しない。

### Consequences

- 良い点: 既存Shared・CIと揃えPreviewを避ける。
- 悪い点: Windows App SDK等との互換性確認が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: .NET 10＋安定版Packageのみ

- 良い点: 既存Shared・CIと揃えPreviewを避ける。
- 悪い点: Windows App SDK等との互換性確認が必要。

### B: Previewも許容して検討

- 良い点: 新機能・修正を先行利用。
- 悪い点: 仕様変更・回帰・更新対応の負担。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD37。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0001: Isolate WinUI implementation](0001-isolate-winui-implementation.md)
- [ADR-0004: Use CommunityToolkit MVVM](0004-use-community-toolkit-mvvm.md)
- [ADR-0019: Target Windows 11](0019-target-windows-eleven.md)
- [ADR-0024: Use framework-dependent Windows App SDK](0024-use-framework-dependent-windows-app-sdk.md)
- [ADR-0025: Use framework-dependent .NET](0025-use-framework-dependent-dotnet.md)
