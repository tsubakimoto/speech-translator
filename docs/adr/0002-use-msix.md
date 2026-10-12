---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0002: Use MSIX

## Context and Problem Statement

新しいSpeechTranslatorWinUIのPackaging方式は、導入・更新・削除とデータ配置に影響する。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- インストール・更新・削除を管理できる方式を優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: MSIX
- B: Unpackaged ZIP
- C: 開発起動のみ

## Decision Outcome

選択肢: **"A: MSIX"**

理由: インストール・更新・削除を管理できる方式を優先する。

公開配布は今回の対象外で、ローカルMSIX検証を行う。インストール・更新・削除を管理可能にする方式を選択し、署名・信頼設定と配布経路の検討を受け入れる。

### Consequences

- 良い点: インストール・更新・削除の管理が可能。
- 悪い点: 署名・信頼設定、配布経路の検討が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: MSIX

- 良い点: インストール・更新・削除の管理が可能。
- 悪い点: 署名・信頼設定、配布経路の検討が必要。

### B: Unpackaged ZIP

- 良い点: 展開・起動する運用。
- 悪い点: Runtime配布・更新管理を別途設計。

### C: 開発起動のみ

- 良い点: 配布設計を後続に分離。
- 悪い点: 配布成果物は今回の受入対象外。

## More Information

元の検討はSpeechTranslatorWinUI要件・実装計画のD4。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

今回のローカル導入検証は開発用自己署名証明書を使用する。秘密鍵・PFX・パスワードをRepositoryへ保存しない。CIはBuild＋単体テストで、MSIX生成・導入はローカル検証。これらは今回の検証範囲であり、公開配布・長期署名運用の決定ではない。

- [ADR-0008: Store settings in package LocalFolder](0008-store-settings-in-package-local-folder.md)
- [ADR-0009: Use user-selected recording folder](0009-use-user-selected-recording-folder.md)
- [ADR-0024: Use framework-dependent Windows App SDK](0024-use-framework-dependent-windows-app-sdk.md)
- [ADR-0025: Use framework-dependent .NET](0025-use-framework-dependent-dotnet.md)
