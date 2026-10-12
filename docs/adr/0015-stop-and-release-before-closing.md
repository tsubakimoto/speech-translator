---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0015: Stop and release before closing

## Context and Problem Statement

翻訳中や未保存設定の存在中にウィンドウを閉じる場合、入力保護とSpeechリソースの終了処理が必要となる。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 未保存入力の保護と翻訳停止・リソース解放を優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: 未保存設定を確認し、翻訳中なら停止・リソース解放後に終了
- B: 未保存設定を破棄し、翻訳中なら停止・リソース解放後に終了
- C: 翻訳中は閉じる操作を拒否し、利用者に停止を促す（未保存設定は確認）

## Decision Outcome

選択肢: **"A: 未保存設定を確認し、翻訳中なら停止・リソース解放後に終了"**

理由: 未保存入力の保護と翻訳停止・リソース解放を優先する。

未保存設定があれば保存・破棄・終了中止を確認し、保存失敗なら終了せず編集内容を保持する。終了を続行する場合は翻訳を停止してリソースを解放し、その後にウィンドウを閉じる。入力保護とリソース解放を優先し、終了延期・非同期処理の管理を受け入れる。停止失敗・期限超過時は関連ADRに従う。

### Consequences

- 良い点: [ADR-0014](0014-commit-service-settings-explicitly.md)の誤破棄防止とセッションの終了処理を両立。
- 悪い点: 終了の延期と非同期処理の管理が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: 未保存設定を確認し、翻訳中なら停止・リソース解放後に終了

- 良い点: [ADR-0014](0014-commit-service-settings-explicitly.md)の誤破棄防止とセッションの終了処理を両立。
- 悪い点: 終了の延期と非同期処理の管理が必要。

### B: 未保存設定を破棄し、翻訳中なら停止・リソース解放後に終了

- 良い点: 終了操作が少ない。
- 悪い点: 画面離脱時の確認と異なり、閉じる操作で入力を失う。

### C: 翻訳中は閉じる操作を拒否し、利用者に停止を促す（未保存設定は確認）

- 良い点: 終了時の自動停止を避けられる。
- 悪い点: 終了前に手動停止操作が必要。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD17。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0005: Compose dependencies manually](0005-compose-dependencies-manually.md)
- [ADR-0014: Commit service settings explicitly](0014-commit-service-settings-explicitly.md)
- [ADR-0016: Confirm exit after stop failure](0016-confirm-exit-after-stop-failure.md)
