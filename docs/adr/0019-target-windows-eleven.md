---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0019: Target Windows 11

## Context and Problem Statement

新しいWinUIアプリの製品対応OS範囲を決める必要がある。Frameworkの技術的最小OSと製品の対応範囲は別である。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- OS別の検証・保守範囲をWindows 11へ限定する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: Windows 11のみ
- B: Windows 10 22H2＋Windows 11

## Decision Outcome

選択肢: **"A: Windows 11のみ"**

理由: OS別の検証・保守範囲をWindows 11へ限定する。

Windows 10は製品の対応対象外とする。OS別の検証・保守範囲を絞る代わりにWindows 10では利用できない制約を受け入れる。Windows 11の最小Release／buildは未確定。Frameworkの技術的最小OSと製品の対応範囲は区別する。

### Consequences

- 良い点: 現行OSへ検証範囲を絞れる。
- 悪い点: Windows 10では利用不可。

## Validation

Not specified.

## Pros and Cons of the Options

### A: Windows 11のみ

- 良い点: 現行OSへ検証範囲を絞れる。
- 悪い点: Windows 10では利用不可。

### B: Windows 10 22H2＋Windows 11

- 良い点: Windows 10利用環境も対象にできる。
- 悪い点: OS別検証が増え、Windows 10の通常サポート終了とESU等の利用条件を運用側で考慮する必要。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD21。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0020: Target x64](0020-target-x64.md)
- [ADR-0029: Use .NET 10 and stable packages](0029-use-dotnet-ten-and-stable-packages.md)
