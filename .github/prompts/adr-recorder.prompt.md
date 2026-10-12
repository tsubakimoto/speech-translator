---
agent: agent
description: Records Architectural Decision Records (ADRs)
---

これまでのPlanで、私が明示的に選択・承認した重要な設計判断をADRとして記録すること。

ルール:
- 対象は長期的に影響するアーキテクチャ上のDecisionのみ。
- 1つのADRには原則1つのDecisionを記録する。
- 私が選択していないOptionを確定しない。
- Context、Decision Drivers、Considered Options、Decision Outcome、Consequencesを記録する。
- 判断理由や制約が不明な場合は質問する。推測で補完しない。
- 既存のADRがあれば、番号・内容・制約との整合性を確認する。
- ADRの本文は日本語とする。

既存の `docs/adr/README.md` と `docs/adr/adr-template.md` があれば、その規約に従うこと。
ADRは `docs/adr/` 以下に保存すること。