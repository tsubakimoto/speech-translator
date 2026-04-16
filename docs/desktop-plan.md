# デスクトップ化計画 — SpeechTranslatorConsole -> Desktop App

## 概要
既存のコンソールアプリ（SpeechTranslatorConsole）は、Azure Speech SDK を使ったリアルタイム音声翻訳を行います。主な機能は:
- マイク入力による連続認識・翻訳（Speech SDK の TranslationRecognizer を利用）
- 翻訳結果のコンソール表示
- オプションで翻訳／認識結果を recordings/<file>.txt に保存
- 設定（Region, SubscriptionKey）は appsettings.json で管理

目的: 上記機能をデスクトップ GUI（Windows）に移植し、使いやすい UI と設定管理・記録機能を提供する。

## 現状の把握（重要ポイント）
- Translator クラス（src/Shared/Translator.cs）が Speech SDK の設定と継続認識を担う。
- TranslationRecognizerWorkerBase と TranslationRecognizerWorker がイベントハンドリングとファイル保存/出力を担当。
- コンソールで言語選択（en-US / ja-JP）、ファイル名入力、recordings フォルダ管理を行っている。

## 推奨プラットフォーム
選択肢:
- WPF (.NET 10): 安定していて既存 .NET コードとの統合が容易。Windows 向け短期導入に推奨。
- WinUI 3: モダン UI を重視する場合。テンプレートや依存の準備が必要。
- .NET MAUI / Avalonia: クロスプラットフォーム対応（将来的な macOS/Linux 対応が必要な場合）。

初期は WPF (.NET 10) を推奨（素早く動くデスクトップ版を提供でき、既存ロジックを流用しやすいため）。

## ゴール
- ユーザーが起動後に言語を選び、Start/Stop でリアルタイム翻訳を行える GUI
- 翻訳テキストを画面上に時系列で表示（原文＋翻訳）
- ファイル保存（ユーザー指定ファイル名）と保存先の選択
- 設定 UI（Region, SubscriptionKey）の提供と安全な取り扱いガイド
- Windows 向けインストーラ／配布方法の案内

## 実施手順（高レベル）
1. ソリューションに新しい WPF プロジェクトを追加 (SpeechTranslator.Desktop)。
   - ターゲット: .NET 10
   - Shared プロジェクト（Translator/WorkerBase）への参照を追加

2. アーキテクチャ: MVVM を採用
   - ViewModel 層に TranslatorService を実装して Translator をラップ
   - UI 用の Worker 実装（TranslationRecognizerWorkerBase を継承）を作成し、Dispatcher 経由で UI 更新

3. UI 設計（MainWindow）
   - 言語選択 ComboBox（source / target）
   - 録音ファイル名入力 TextBox と保存先選択ボタン
   - Start / Stop ボタン
   - 翻訳結果表示領域（ListView または ObservableCollection バインド）
   - ログ／状態表示（ステータスバー）
   - 設定画面（Azure Region / SubscriptionKey、マイクデバイス選択）

4. 設定とシークレット管理
   - サブスクリプションキーは平文保存を避け、起動時に入力させるか Windows Credential Manager 等の利用を検討
   - ユーザー設定はユーザーローカル（%LOCALAPPDATA%）へ保存

5. ファイル保存
   - recordings フォルダは既定でユーザーのドキュメント内に作成
   - 既存のテキスト出力ロジックを再利用（スレッド同期に注意）

6. UI と非同期処理の同期
   - Speech SDK のイベントは別スレッドから来るため、Dispatcher/ SynchronizationContext を用いて UI 更新

7. エラーハンドリングと通知
   - エラーはダイアログで通知。キャンセル・停止時の状態管理を実装

8. テスト
   - 手動テスト項目: Start/Stop、言語切替、保存、ネットワーク障害時の振る舞い

9. パッケージング
   - MSIX / 単一実行可能ファイル（self-contained publish）等を検討

## タスクリスト（TODO）
- [ ] (T1) WPF プロジェクト追加と Shared 参照設定（受け入れ基準: ビルドが通る）
- [ ] (T2) UI ワイヤーフレーム作成（MainWindow）
- [ ] (T3) TranslatorService 実装（Translator をラップ）
- [ ] (T4) UI Worker 実装（TranslationRecognizerWorkerBase 継承）とイベントバインド
- [ ] (T5) 設定画面実装（Region / SubscriptionKey / 保存設定）
- [ ] (T6) ファイル保存ロジックと保存先 UI 実装
- [ ] (T7) エラーハンドリング、ログ、ステータス表示
- [ ] (T8) パッケージングと配布手順の文書化

## 注意事項・リスク
- SubscriptionKey をアプリ内に平文で置かない（セキュリティ）。配布時の扱いに注意。
- Speech SDK のネイティブ依存やバージョン要件（ランタイムの追加が必要な場合あり）。
- マイクデバイス選択/権限は OS に依存。UWP/WinUI と WPF で挙動が異なる点に留意。

## 次のアクション（ユーザーへの質問）
1. ターゲット UI フレームワークは WPF でよいですか？（代替: WinUI / MAUI / Avalonia）
2. サブスクリプションキーの保存方法（ユーザーに毎回入力 / Windows Credential Manager / 環境変数）の希望はありますか？

## Microsoft Learn 参考
- https://learn.microsoft.com/en-us/dotnet/core/porting/
- https://learn.microsoft.com/en-us/dotnet/desktop/wpf/
- https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk?tabs=net

----
作業を開始する場合は「開始して下さい」と指示ください。指示があれば、最初の実装タスク（T1: プロジェクト追加）を実行します。
