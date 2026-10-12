---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0018: Retain unbounded screen logs

## Context and Problem Statement

既存WPFの翻訳ログ・状態ログには件数上限がない。WinUIでは保持メモリとUI負荷を踏まえた方針が必要となる。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 既存同様に全画面ログを保持し、件数を理由に古い項目を削除しないことを優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 件数上限を設け、古い項目から削除
- B: 既存同様、上限なし
- C: 仮想化表示とファイルからの履歴読み込み

## Decision Outcome

選択肢: **"B: 既存同様、上限なし"**

理由: 既存同様に全画面ログを保持し、件数を理由に古い項目を削除しないことを優先する。

翻訳ログ・状態ログの保持件数を制限せず、古い項目を件数理由で削除しない。全画面ログを保持する代わりに、長時間利用時のメモリ増加・UI負荷を受け入れる。表示Controlの仮想化を検討しても保持データ自体のメモリ増加は解消しない。履歴読み込み機能は追加しない。

### Consequences

- 良い点: セッション内の全画面ログを保持。
- 悪い点: 長時間利用でメモリ増加・UI負荷の可能性。

## Validation

長時間利用時のメモリ増加・UI負荷を受入検証で確認する。検証結果を理由に、ユーザーの承認なく件数上限を追加しない。

## Pros and Cons of the Options

### A: 件数上限を設け、古い項目から削除

- 良い点: 長時間利用時のメモリ使用を抑える。
- 悪い点: 古い画面ログを参照できなくなる。
- 中立: 記録テキストの保存内容は削除しない。

### B: 既存同様、上限なし

- 良い点: セッション内の全画面ログを保持。
- 悪い点: 長時間利用でメモリ増加・UI負荷の可能性。

### C: 仮想化表示とファイルからの履歴読み込み

- 良い点: 大量の履歴を扱える。
- 悪い点: 履歴保存・検索・読み込みが追加機能となり実装範囲が増える。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD20。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0003: Separate translation and settings](0003-separate-translation-and-settings.md)
