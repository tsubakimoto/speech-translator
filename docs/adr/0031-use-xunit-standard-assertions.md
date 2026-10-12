---
status: accepted
date: 2026-10-12
deciders: tsubakimoto
---

# ADR-0031: Use xUnit standard assertions

## Context and Problem Statement

Shared.TestsとDesktopApp.TestsはFluentAssertionsに依存している。一方、WinUIApp.Testsを含む全テストプロジェクトにxUnitが導入済みである。Issue #61は対象2プロジェクトのFluentAssertions廃止とxUnitへの置換を要求している。今回の移行に限定するか、Repository全体の長期的なAssertion方針とするか、その適用方法を決める必要がある。

## Decision Drivers

- Repository全体で将来のAssertion方針を統一する。
- 追加Toolを導入せず、方針と理由を保持する。

## Considered Options

方針の範囲:

- A: 今回の2プロジェクトのみ
- B: Repository全体でxUnit標準Assertionを原則とする

方針の適用:

- A: ADR＋AGENTS.mdに明記
- B: A＋CIでFluentAssertions依存を検査

## Decision Outcome

選択肢: **"B: Repository全体でxUnit標準Assertionを原則とする"** および **"A: ADR＋AGENTS.mdに明記"**

理由: Repository全体で将来の方針を統一し、追加Toolを導入せず方針と理由を保持するため。

Repository全体のテストでxUnit標準Assertionを原則とする。方針は本ADRとAGENTS.mdに明記し、CIによる禁止依存検査は追加しない。他Assertion libraryの採用が必要になった場合は人間による設計再検討を行う。

### Consequences

- 良い点: 将来のAssertion選定が統一され、追加Toolを導入せず方針と背景を保持できる。
- 悪い点: 他Libraryの採用には再検討が必要となる。自動強制はなく、準拠確認はレビューに依存する。

## Validation

Issue #61の移行では、対象2プロジェクトのFluentAssertions利用と直接・推移依存が除去され、既存テストがxUnit標準Assertionで成功することを確認する。以後の変更は本方針への準拠をレビューで確認する。

## Pros and Cons of the Options

### A: 今回の2プロジェクトのみ

- 良い点: 変更・制約が局所的。
- 悪い点: 将来のAssertion library選定は別判断になる。

### B: Repository全体でxUnit標準Assertionを原則とする

- 良い点: 将来も方針が統一される。
- 悪い点: 他Libraryの採用には再検討が必要。

### A: ADR＋AGENTS.mdに明記

- 良い点: 追加Tool不要で、方針と理由を保持できる。
- 悪い点: 違反検出はレビューに依存する。

### B: A＋CIでFluentAssertions依存を検査

- 良い点: FluentAssertionsの再導入を自動検出できる。
- 悪い点: CI保守が増える。他Libraryまで網羅するには追加ルールが必要。

## More Information

- [Issue #61: FluentAssertionsの廃止](https://github.com/tsubakimoto/speech-translator/issues/61)
- 本DecisionはIssue #61のPlanでユーザーが明示的に選択した。decidersと判断日2026-10-12の記録もユーザーが承認した。
- 既存Assertionの検証意味の保持とVersionPrefix更新は今回の局所的な移行要件であり、本ADRの長期方針とは分けて扱う。
