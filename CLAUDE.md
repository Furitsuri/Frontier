# CLAUDE.md

このファイルは、本リポジトリのコードを扱う際に Claude Code (claude.ai/code) へ提供するガイダンスです。

## プロジェクト概要

FRONTIER は Unity 3D で作られたグリッドベースの戦闘システムを持つタクティカル RPG です。C# で記述されており、Unity 2022 LTS を使用しています。

## コーディング規約

### ファイルフォーマット

- **文字コード**: UTF-8 BOM あり
- **改行コード**: CRLF

スクリプトファイルを新規作成・編集する際は必ずこの形式を維持すること。PowerShell で変換する場合は以下を使用する:

```powershell
$utf8Bom = New-Object System.Text.UTF8Encoding $true
$content = [System.IO.File]::ReadAllText($file)
$converted = $content -replace "(?<!\r)\n", "`r`n"
[System.IO.File]::WriteAllText($file, $converted, $utf8Bom)
```

## ビルド・テスト

Unity プロジェクトのため、CLI によるビルドコマンドはありません。Unity Editor (2022 LTS) でプロジェクトを開いてください。テストは Unity 組み込みの Test Runner (Window > General > Test Runner) から実行します。

このセッションでは uLoopMCP ツールを使って Unity Editor と直接やり取りできます（コンパイル、テスト実行、スクリーンショット、動的 C# コード実行など）。

CLI セッションからコンパイルしてエラーを確認する場合:
- `mcp__uLoopMCP__compile` — 開いている Editor でコンパイルをトリガーする
- `mcp__uLoopMCP__get-logs` — Console の出力を読み取る
- `mcp__uLoopMCP__run-tests` — Unity テストを実行する

## アーキテクチャ

### 依存性注入 (DI)

プロジェクトは **Zenject** を DI に使用しています。ルートインストーラは `Assets/Game/Scripts/System/DIInstaller.cs` で、全シングルトンサービス (`InputFacade`、`StageController`、`SequenceFacade`、`UserDomain` 等) をコンテナにバインドします。

**`HierarchyBuilderBase`** (`Assets/Game/Scripts/Utility/HierarchyBuilderBase.cs`) は、DI 注入が必要なすべてのオブジェクトを生成し、Unity ヒエラルキー上に配置し、必要に応じてコンテナへバインドする中央ファクトリです。非trivialな型の `new` 生成はすべてこのクラスの `InstantiateWithDiContainer<T>()` や `CreateComponentAndOrganizeWithDiContainer<T>()` を経由します。

**`LazyInject`** は初回アクセス時にフィールドを初期化する小さなヘルパーです:
```csharp
LazyInject.GetOrCreate(ref _myField, () => _hierarchyBld.InstantiateWithDiContainer<MyClass>(false));
```

### ゲームフロー

```
GameMain (MonoBehaviour エントリポイント)
  └─ GameRoutineController (FocusRoutineBase)
       ├─ FormTroopRoutineController  (雇用・編成フェーズ)
       └─ BattleRoutineController     (戦闘フェーズ)
            ├─ DeploymentPhaseHandler  (配置フェーズ)
            ├─ PlayerPhaseHandler      (プレイヤーフェーズ)
            ├─ EnemyPhaseHandler       (エネミーフェーズ)
            └─ OtherPhaseHandler       (その他フェーズ)
```

`GameRoutineController` は `FormTroopRoutineController`（雇用・編成）と `BattleRoutineController`（戦闘本体）を交互に切り替えます。各フェーズハンドラは `PhaseStateBase` サブクラスのステートマシンを駆動します。

### ステートマシン

ステートは `PhaseStateBase → StateBase → IState` を継承します。入力はクラス名の安定ハッシュをキーとして `RegisterInputCodes()` でステートごとに登録されます。基底クラスは毎フレームの `LateUpdate` で `TryShowTutorial()` を呼び出します。

### キャラクターシステム

- **`Character`**（基底 MonoBehaviour）— `Status`（シリアライズ済みステータス）、`BattleParameters`（戦闘一時データ）、`CameraParameter`、`TransformHandler`、`AnimationController`、`BattleLogicBase` を保持します。
- 具体型: `Player`、`Enemy`、`Npc`、`Other`。
- **`BattleLogicBase`**（実行時にコンポーネントとして追加）— 戦闘中のロジックをすべて担います。サブクラス: `PlayerBattleLogic`、`EnemyBattleLogic`、`OtherBattleLogic`。`ActionRangeController`、`SkillNotifierBase[]`、対戦相手参照を保持します。
- `BattleLogicBase` はシーンロード後に追加されるため `[SerializeField]` が使えません。そのため戦闘一時パラメータ (`BattleParameters`) は `Character` 自身にシリアライズされています。

### ステージ・グリッド

- **`StageController`** — グリッド操作全般のファサード。`GridCursorController`、`TileDataHandler`、`StageFileLoader`、`StageDirectionConverter` をラップします。経路探索は `Dijkstra` (`Assets/Game/Scripts/Utility/Dijkstra.cs`) を使用します。
- **`GridCursorController`** — 現在選択中のタイルと攻撃ターゲットを管理します。移動時は `Character` にバインドされます。内部的に UniRx を使用しています。
- タイルは 1×1 単位 (`TILE_SIZE = 1.0f`) で、グリッドは最大 25×25 です。
- 移動範囲・行動範囲は **`ActionRangeController`** が管理し、描画を `ActionableRangeRenderer`、経路追跡を `MovePathHandler` に委譲します。

### スキル

- スキル定義は静的クラス **`SkillsData`** (`Assets/Game/Scripts/Combat/Skills/SkillDatas.cs`) にあります。全スキルデータは `SkillID` enum でインデックスされた `SkillsData.data[]` に格納されています。
- 各スキルは `SituationType`、`ActionType`、`RangeShape`、`TargetingMode`、コスト・パラメータフィールドを持ちます。
- `SkillNotifierBase` がスキルごとの通知を担い、ファクトリは `SkillsData.BuildSkillNotifierFactory()` で一度だけ構築されます。
- スキルアクションは `SkillActionBase` を継承します。

### シーケンス

**`SequenceFacade`** は `ISequence` オブジェクト（攻撃、自己バフ）を `SequenceHandler` にキューイングします。`CharacterAttackSequence` と `SelfBuffSequence` が主なシーケンス型です。シーケンスハンドラは `FocusRoutineBase` と同一オブジェクトであり、`SequenceFacade.Setup()` に渡されます。

### 入力システム

`InputFacade` が `InputContext` を管理します。ステートは安定ハッシュ (`Hash.GetStableHash(GetType().Name)`) をキーとして入力ハンドラを登録します。`PhaseStateBase` は各ボタン種別 (`Confirm`、`Cancel`、`Direction`、`Tool`、`Info`、`Sub1–4`、`Camera`) に対応する仮想 `CanAccept*` / `Accept*` メソッドを提供します。

### レジストリ

- **`PrefabRegistry`** — ヒエラルキーから注入される全プレハブ参照を保持します。
- **`FilePathRegistry`** — セーブ・ロード用のファイルパス定数を保持します。

### UI

`IUiSystem` / `UISystem` はあらゆる場所に注入される最上位の UI ファサードです。バトル UI、ステータスウィンドウ、チュートリアル UI はすべてここを経由してアクセスします。

### 主要定数

すべてのマジックナンバーは `Constants` 静的クラス (`Assets/Game/Scripts/Utility/Define.cs`) に集約されています。タイルサイズ、キャラクター上限数、ゲージ最大値、移動速度、タイミング定数などが含まれます。

## 名前空間

| 名前空間 | 主な担当 |
|---|---|
| `Frontier` | ルート・フロー (GameRoutineController, DiInstaller) |
| `Frontier.Entities` | キャラクター、AI、ActionRange、ステータス効果 |
| `Frontier.Battle` | BattleRoutineController、フェーズハンドラ |
| `Frontier.Combat` | スキル、パリィ、SkillsData |
| `Frontier.Stage` | タイル、GridCursorController、StageController |
| `Frontier.StateMachine` | PhaseStateBase、各ステートクラス |
| `Frontier.Sequences` | 攻撃・バフシーケンス |
| `Frontier.UI` | UISystem、プレゼンター |
| `Frontier.Input` | InputFacade、InputContext、InputCode |
| `Frontier.FormTroop` | 雇用・編成フェーズ |
| `Frontier.Tutorial` | TutorialFacade、TutorialPresenter |
| `Frontier.Registries` | PrefabRegistry、FilePathRegistry |
| `Frontier.Loaders` | BattleFileLoader、GeneralFileLoader |
