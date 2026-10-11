---
status: {proposed | accepted | rejected | deprecated | superseded}
date: {YYYY-MM-DD when the decision was made, using the decision maker's local date}
deciders: {human decision makers, comma-separated}
---

# ADR-{NNNN}: {Short English title of the decision}

<!--
ADR の本文は日本語で記述する。
セクション見出し、ADR title、status、定型ラベル、および Plan で使用された Option 名は、このテンプレートで定義された表記を維持する。
-->

## Context and Problem Statement

<!-- Required.
Planで確認された背景・課題、およびRepositoryから直接確認できる事実を日本語で記載する。
Agentの推測で補完しない。
-->

{このDecisionが必要になった背景と解決すべき問題を日本語で記載する}

## Decision Drivers

<!-- Required.
何を重視して判断したかを日本語で記載する。
Planで確認された情報、またはRepositoryから直接確認できる事実のみ使用し、Agentの推測で判断基準を追加しない。
-->

- {decision driver 1}
- {decision driver 2}

## Considered Options

<!-- Required.
Planで実際にユーザーへ提示・検討された選択肢のみ記載する。
ADR作成時に新しい選択肢を追加しない。Option名はPlanで使用された名称を維持する。
-->

- {option 1}
- {option 2}

## Decision Outcome

<!-- Required.
ユーザーがPlanで明示的に選択または承認したOptionのみDecisionとして記載する。
選択理由は日本語で記載し、Planで確認された情報、またはRepositoryから直接確認できる事実に基づく。
Agentの推測による理由付けを行わない。
-->

選択肢: **"{selected option}"**

理由: {選択理由を日本語で記載する}

### Consequences

<!-- Required.
選択したOptionを採用することで受け入れる影響やtrade-offを日本語で記載する。
Pros and Consとは異なり、Decision採用後の影響を扱う。
-->

- 良い点: {positive consequence}
- 悪い点: {negative consequence or trade-off}

## Validation

<!-- Optional.
実装がこのADRのDecisionに従っていることを確認する方法を日本語で記載する。
情報がない場合は `Not specified.` とする。
-->

{Decisionへの準拠を確認する方法を日本語で記載する}

## Pros and Cons of the Options

<!-- Required.
Decision前に各Optionを比較した評価を日本語で記載する。
Planで確認された情報、またはRepositoryから直接確認できる事実のみ使用し、Agentの推測でPros/Consを追加しない。
-->

### {option 1}

- 良い点: {argument}
- 中立: {argument}
- 悪い点: {argument}

### {option 2}

- 良い点: {argumentを日本語で記載する}
- 中立: {argumentを日本語で記載する}
- 悪い点: {argumentを日本語で記載する}

## More Information

<!-- Optional.
関連するPlan、Issue、PR、ADR、Decisionの再検討条件などを日本語で記載する。
元Planを永続的に参照できる場合は必ず記載する。
Supersededの場合は、新旧ADRをここで相互参照する。
情報がない場合は `Not specified.` とする。
-->

{関連情報を日本語で記載する}