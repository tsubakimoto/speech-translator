# Architectural Decision Records

このディレクトリでは、Plan で人間の意思決定者が明示的に選択・承認した、長期的な Architecture / Design Decision を Architectural Decision Record (ADR) として管理する。

ADR は Decision そのものだけでなく、その時点の Context、判断基準、検討した選択肢、trade-off を Repository に永続化する。これにより、後続の設計・実装で過去の Decision とその背景を参照できる状態を維持する。

## Decision Types

ADR では Decision の provenance を次のように区別する。

| Type | Definition |
| --- | --- |
| **User Decision** | 人間の意思決定者が Plan 中に明示的に選択または承認した Decision |
| **Agent Decision** | GitHub Copilot、Codex などの Agent が自律的に行った Decision |
| **Existing Decision** | `accepted` な ADR によって既に確立されている Decision |

- `accepted` な ADR として記録できるのは **User Decision** のみとする。Agent Decision を User Decision として記録してはならない。
- `accepted` な Existing Decision は、後続の Architecture、Design、Implementation に対する constraint として扱う。

## ADR Candidates

- 長期的な Architecture / Design への影響を持つ User Decision を **ADR Candidate** とする。
- ADR Candidate とならない一時的または局所的な Decision は、ADR として記録しない。
- ADR を作成する場合は [`adr-template.md`](adr-template.md) を使用し、テンプレートに定義された構造と記入ルールに従う。
- ADR の情報を Agent の推測によって補完してはならない。

## Naming and Numbering

ADR のファイル名は次の形式とする。

```text
NNNN-short-title.md
```

| Element | Rule |
| --- | --- |
| `NNNN` | zero-padded 4-digit sequential number |
| `short-title` | Decision を表す English kebab-case |
| ADR title | `ADR-NNNN: Short English title` |

例:

```text
0001-use-managed-identity.md
0002-use-postgresql.md
0003-use-azure-container-apps.md
```

新しい ADR を作成するときは、既存 ADR の最大番号に 1 を加えた番号を使用する。

一度使用した ADR 番号は、`rejected`、`deprecated`、`superseded` になった場合も再利用しない。

## Status Lifecycle

ADR の `status` は次のいずれかとする。

| Status | Meaning |
| --- | --- |
| `proposed` | Decision が提案されているが、まだ承認されていない |
| `accepted` | 人間の意思決定者によって承認され、現在有効 |
| `rejected` | ADR として提案された Decision 自体が却下された |
| `deprecated` | Decision は現在適用・推奨されておらず、直接置き換える ADR は存在しない |
| `superseded` | Decision が新しい ADR によって明示的に置き換えられた |

`rejected` は `Considered Options` の中で選択されなかった Option を意味しない。非採用の Option は、採用された Decision の ADR 内に記録する。

現在有効な Architecture / Design constraint として扱うのは `accepted` な ADR のみとする。`deprecated` および `superseded` な ADR は constraint として扱わない。

### Accepted ADR

`accepted` な ADR は、その Decision の historical record として扱う。

- 誤字、リンク、説明の明確化など、Decision を変更しない修正は許可する。
- Decision 自体を変更するために既存 ADR を書き換えてはならない。
- Decision を変更する場合は、新しい ADR を作成する。

新しい ADR が既存 ADR の Decision を置き換える場合:

1. 新しい ADR を作成する。
2. 既存 ADR の `status` を `superseded` に変更する。
3. 両 ADR の `More Information` で相互に参照する。

例:

```markdown
## More Information

Supersedes [ADR-0003](0003-use-app-service.md).
```

旧 ADR:

```markdown
## More Information

Superseded by [ADR-0012](0012-use-azure-container-apps.md).
```

## Decision Makers

`deciders` には、Decision を選択または承認した**人間の意思決定者のみ**を記録する。

GitHub Copilot、Codex などの Agent は、選択肢の提示、分析、推奨を行った場合でも `deciders` には含めない。

## Template Origin

https://github.com/microsoft/agent-framework/blob/main/docs/decisions/adr-template.md