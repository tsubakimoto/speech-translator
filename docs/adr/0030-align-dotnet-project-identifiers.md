---
status: accepted
date: 2026-10-12
deciders: tsubakimoto
---

# ADR-0030: Align .NET Project Namespaces and Assembly Names

## Context and Problem Statement

Issue #59「名前空間の整備」は、source/test projectのディレクトリ名とnamespaceを `Tsubakimoto.SpeechTranslator.*` 系へ変更するよう指定している。Planでは、Issue記載の範囲だけを変更する案、projectファイル名も統一してAssemblyNameを維持する案、ディレクトリ・`.csproj`名・namespace・AssemblyNameをすべて新名称へ統一する案を比較した。ユーザーは第三の案を明示的に選択した。

この選択はnamespaceおよびAssemblyNameの公開識別子に影響する。既存ADRを確認したが、この命名規約と衝突するaccepted decisionは見つからなかった。

## Decision Drivers

- Issue #59で指定された新しいsource/test project名とnamespaceに追従する。
- Planではディレクトリ・projectファイル・namespace・AssemblyNameのすべてを揃えるOptionが明示的に選択された。
- この選択に関する追加の動機や外部互換性要件は記録されていないため、ここでは推測で補わない。

## Considered Options

- Issue記載範囲に限定
- projectファイル名まで統一、AssemblyName維持
- ディレクトリ・`.csproj`名・namespace・AssemblyNameを全て新名称に統一（選択）

## Decision Outcome

選択肢: **"ディレクトリ・`.csproj`名・namespace・AssemblyNameを全て新名称に統一（選択）"**

source/test projectのディレクトリ名、projectファイル名、RootNamespaceおよびAssemblyNameをIssue #59の新名称に揃える。source namespaceはproject名に対応する `Tsubakimoto.SpeechTranslator.*` とし、test namespace/assembly名は対応する `Tsubakimoto.SpeechTranslator.*.Tests` とする。ユーザーがPlanでこのOptionを明示的に選択したことを記録理由とする。選択に至った追加の動機は未確認であり、本ADRでは補完しない。

### Consequences

- 良い点: source/testのproject名、namespaceおよびassembly識別子が一貫する。
- 悪い点: AssemblyName変更により旧assembly名を参照する利用側の更新が必要になる場合がある。また、solution、project references、CIなどのproject pathsも更新する必要がある。

## Validation

実装時に全source/test projectのRootNamespaceおよびAssemblyNameが新名称と一致すること、solution・project references・CIの参照先に旧project pathsが残らないことを確認し、solutionのbuild/testを実行する。

## Pros and Cons of the Options

### Issue記載範囲に限定

- 良い点: 変更範囲をIssueに明記されたディレクトリ名とnamespaceに限定できる。
- 悪い点: `.csproj`名とAssemblyNameが旧名のまま残り、新名称との不整合が続く。

### projectファイル名まで統一、AssemblyName維持

- 良い点: project directory、project file、namespaceを新名称に揃えられる。
- 中立: AssemblyNameの互換性を維持できる。
- 悪い点: AssemblyNameだけが旧名のまま残り、projectの名称とassembly識別子が不一致になる。

### ディレクトリ・`.csproj`名・namespace・AssemblyNameを全て新名称に統一（選択）

- 良い点: ディレクトリ、project file、namespace、assembly識別子を一貫させられる。
- 悪い点: solutionやCI等の参照更新が必要で、旧AssemblyNameに依存する利用側との互換性を破る。

## More Information

- Issue: [#59 名前空間の整備](https://github.com/tsubakimoto/speech-translator/issues/59)
- 元Planはsession artifact `plan.md` にあり、Repositoryから恒久参照できないため、選択肢、選択結果、影響を本ADRに記録した。
- Planでは選択理由の追加説明を得られなかった。将来、decision driverや互換性要件が明確になった場合は、本ADRのDecision自体を変えずに説明を追記する。
