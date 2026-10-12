---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0005: Compose dependencies manually

## Context and Problem Statement

WinUI側には複数画面とServicesの依存構成・Lifetime管理が必要となる。MVVM Packageの採用とDI containerの採用は別の判断である。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- Lifetimeの明確さと追加Packageを避ける構成を優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: Appで手動構築＋constructor injection
- B: Microsoft.Extensions.DependencyInjection
- C: Generic Host

## Decision Outcome

選択肢: **"A: Appで手動構築＋constructor injection"**

理由: Lifetimeの明確さと追加Packageを避ける構成を優先する。

Lifetimeを明確にし追加Packageを避ける代わりに、依存追加時の配線修正を受け入れる。破棄・終了処理の具体的な実装は未確定。終了ポリシーは[ADR-0015](0015-stop-and-release-before-closing.md)・[ADR-0016](0016-confirm-exit-after-stop-failure.md)に従う。CommunityToolkit.Mvvm採用はDI container採用を意味しない。DI container／Generic Hostは追加しない。

### Consequences

- 良い点: Lifetimeが明確、追加Package不要。
- 悪い点: 依存追加時に配線修正が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: Appで手動構築＋constructor injection

- 良い点: Lifetimeが明確、追加Package不要。
- 悪い点: 依存追加時に配線修正が必要。

### B: Microsoft.Extensions.DependencyInjection

- 良い点: 登録集約と複数画面での再利用。
- 悪い点: Lifetime・破棄管理の設計が必要。

### C: Generic Host

- 良い点: DI・設定・Logging・Lifecycleを統合。
- 悪い点: WinUI Lifecycleとの連携が増える。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD7。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0001: Isolate WinUI implementation](0001-isolate-winui-implementation.md)
- [ADR-0004: Use CommunityToolkit MVVM](0004-use-community-toolkit-mvvm.md)
- [ADR-0015: Stop and release before closing](0015-stop-and-release-before-closing.md)
