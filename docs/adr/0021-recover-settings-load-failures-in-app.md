---
status: accepted
date: 2026-10-11
deciders: tsubakimoto
---

# ADR-0021: Recover settings load failures in app

## Context and Problem Statement

起動時に設定DBの読込・API Key復号が失敗する場合がある。設定未登録と破損・アクセス不可・復号失敗を区別する必要がある。

本ADRはSpeechTranslatorWinUIに適用する。既存WPFの廃止・恒久併存は決定せず、今回は既存WPFを変更しない。

## Decision Drivers

- 読込失敗を隠さず、アプリを起動して利用者が設定画面で復旧できることを優先する。
- 選択したOptionの以下のtrade-offを受け入れる。

## Considered Options

- A: エラー表示＋設定画面で復旧
- B: エラー表示して終了
- C: 自動初期化して起動

## Decision Outcome

選択肢: **"A: エラー表示＋設定画面で復旧"**

理由: 読込失敗を隠さず、アプリを起動して利用者が設定画面で復旧できることを優先する。

起動を継続し、読込失敗を未設定扱いで隠さず設定画面へ案内する。自動削除・初期化・上書きは行わない。再設定・復旧操作と上書き確認を設けるTrade-offを受け入れる。DB未作成／設定未登録と破損／アクセス不可／復号失敗を区別する。復旧成功前に不明な保存値を有効な設定として使用しない。具体的な復旧方法（再保存／退避再作成等）は未確定。後続の承認済み起動要件により、DB読込／復号失敗時は環境変数フォールバックによる通常画面表示より、本ADRのエラー・復旧フローを優先する。アクセス不可は再設定だけでは解消しないため保存失敗時は[ADR-0014](0014-commit-service-settings-explicitly.md)どおり入力を保持する。

### Consequences

- 良い点: アプリを起動し再設定できる。
- 悪い点: 復旧操作と上書き確認が必要。

## Validation

Not specified.

## Pros and Cons of the Options

### A: エラー表示＋設定画面で復旧

- 良い点: アプリを起動し再設定できる。
- 悪い点: 復旧操作と上書き確認が必要。

### B: エラー表示して終了

- 良い点: 不整合状態で利用しない。
- 悪い点: アプリ内で復旧できない。

### C: 自動初期化して起動

- 良い点: 操作なしで利用を再開。
- 悪い点: 保存設定が意図せず失われる。

## More Information

関連する承認済み起動要件（元Plan D34）では、DB未作成／設定未登録時でもSPEECH_REGION／SPEECH_KEYのフォールバックで必要値が揃えば翻訳画面、揃わなければ設定画面へ案内する。「有効」は必要値と形式を指し、起動時のAzure接続・認証成功を保証しない。読込／復号失敗はこの通常起動分岐とは区別し、本ADRの復旧フローを優先する。自動翻訳開始は含まない。

元の検討はSpeechTranslatorWinUI要件・実装計画のD25。Planはセッション内成果物であり、Repositoryから恒久参照できないため、必要な背景・Options・選択・trade-offを本ADRへ転記した。

dateは本ADRの判断理由をユーザーが承認した2026-10-11を記録する。過去の個別Option選択日は未記録。decidersとこの日付の記録方法はユーザーが明示承認した。

- [ADR-0006: Store settings with SQLite and DPAPI](0006-store-settings-with-sqlite-and-dpapi.md)
- [ADR-0014: Commit service settings explicitly](0014-commit-service-settings-explicitly.md)
