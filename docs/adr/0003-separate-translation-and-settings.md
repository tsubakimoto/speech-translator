---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0003: Separate translation and settings

## Context and Problem Statement

既存WPFは翻訳操作・ログとRegion／API Key設定を単一画面で提供する。WinUI側の画面責務を決める必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 翻訳操作とログに集中できるよう、翻訳と設定の画面責務を分離する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 既存同様の単一画面
- B: 翻訳画面＋設定画面
- C: 翻訳画面＋設定Dialog

## Decision Outcome

選択肢: **"B: 翻訳画面＋設定画面"**

理由: 翻訳操作とログに集中できるよう、翻訳と設定の画面責務を分離する。

翻訳操作とログに集中できる構成とし、Navigationと画面間状態管理を受け入れる。

### Consequences

- 良い点: 翻訳操作とログに集中可能。
- 悪い点: Navigationと画面間状態管理が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 既存同様の単一画面

- 良い点: 操作の互換性と機能比較が容易。
- 悪い点: 設定・ログで画面が密になる。

### B: 翻訳画面＋設定画面

- 良い点: 翻訳操作とログに集中可能。
- 悪い点: Navigationと画面間状態管理が必要。

### C: 翻訳画面＋設定Dialog

- 良い点: メイン画面を簡潔に保てる。
- 悪い点: Dialogの保存・キャンセル仕様が必要。

## More Information

関連する承認済みNavigation要件（元Plan D33）は「B: 設定ボタン＋戻るボタン」。2画面に限定した簡潔さ・省スペースを優先し、画面切替・戻る操作を独自に管理する。「A: NavigationView」（WinUI標準で現在画面を明示できる一方、メニュー領域を使用）も比較したが採用しない。未保存の離脱確認と、翻訳中の閲覧許可・編集禁止は関連ADRに従う。

元の検討はSpeechTranslatorWinUI要件・実装計画のD5。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0004: Use CommunityToolkit MVVM](0004-use-community-toolkit-mvvm.md)
- [ADR-0013: Freeze settings during translation](0013-freeze-settings-during-translation.md)
- [ADR-0014: Commit service settings explicitly](0014-commit-service-settings-explicitly.md)
