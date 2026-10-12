# ADR Workflow

このドキュメントでは、Plan で成立した Architecture / Design Decision を ADR として記録するための Agent workflow を定義する。

ADR の仕様・管理ルールは [`README.md`](README.md)、ADR の構造と記入ルールは [`adr-template.md`](adr-template.md) に従う。

## Plan

Architecture / Design に関する Plan を作成するときは、関連する既存 ADR を確認する。

1. 関連する Existing Decision がある場合は constraint として Plan に反映する。
2. Architecture / Design に関する選択が必要な場合は、選択肢と判断に必要な情報を提示する。
3. Agent が Decision を独自に確定せず、人間の意思決定者による明示的な選択または承認を得る。
4. 成立した User Decision を Plan の一部として保持する。

Agent の推奨を User Decision として扱ってはならない。

## ADR Candidate

Plan 中に成立した User Decision が長期的な Architecture / Design に影響する場合、ADR Candidate として扱う。

ADR Candidate か明確に判断できない場合は、人間の意思決定者に確認する。

1つの Plan に複数の独立した ADR Candidate が存在する場合は、Decision ごとに個別の ADR として扱う。

## Required Information

ADR Candidate について、[`adr-template.md`](adr-template.md) の Required section に必要な情報が揃っていることを確認する。

不足している場合:

1. Agent の推測で補完しない。
2. Repository から直接確認できる事実で補完できるか確認する。
3. それでも不足する場合は、人間の意思決定者に質問する。

## Create ADR

Plan 完了後、Implementation を開始する前に、ADR Candidate ごとに ADR を作成する。

User Decision が既に明示的に選択または承認され、Required information が揃っている場合、ADR 作成について追加の承認を求めない。

ADR の作成時:

1. [`README.md`](README.md) に従う。
2. [`adr-template.md`](adr-template.md) を使用する。
3. 既存 ADR から次の ADR number を決定する。
4. Plan で成立した User Decision を記録する。
5. Plan で実際に検討した Options のみ記録する。
6. Template の HTML comments を生成された ADR から削除する。
7. ADR を保存してから Implementation に進む。

Plan で選択されなかった Option のために個別の `rejected` ADR を作成しない。

## Existing Decisions

Plan および Implementation では、関連する `accepted` ADR を Architecture / Design constraint として扱う。

新しい User Decision が Existing Decision と矛盾する、または置き換える場合は、既存 ADR の Decision を直接書き換えない。

新しい ADR を作成し、[`README.md`](README.md) に定義された lifecycle に従って管理する。