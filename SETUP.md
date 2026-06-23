# プロジェクト セットアップ手順（新しいPC向け）

このリポジトリは、容量節約のため **一部のプラグイン本体を Git に含めていません**
（`Assets/Plugins/` の Zenject / UniRx / UniTask / DOTween / NuGet）。
別のPCでクローンした後は、以下を実施して環境を揃えてください。

> ⚠️ **重要：Unity の GUID について**
> プラグインを再インストールする際は **必ず下記と同じバージョンを同じ入手元から** 入れてください。
> バージョンが違うと `.meta` の GUID がずれ、プレハブ・シーン・マテリアルの参照が壊れます
> （Missing script / マテリアルがピンク等）。

---

## 1. Unity エディタ

- バージョン: **2022.3.62f2**（厳密一致を推奨）

## 2. UPM パッケージ（自動復元・コミット済み・操作不要）

`Packages/manifest.json` と `Packages/packages-lock.json` はコミット済みです。
プロジェクトを開けば Package Manager が自動で復元します。手動操作は不要です。

- 主なもの: URP 14.0.12 / Input System 1.14.0 / Addressables 1.22.3 / TextMeshPro 3.0.7 / AI Navigation 1.1.6 など
- **uLoopMCP**: `io.github.hatayama.uloopmcp` 2.0.0（OpenUPM レジストリ経由、manifest.json に登録済み）

## 3. 手動インストールが必要なプラグイン（Git 未収録）

以下は `Assets/Plugins/` 配下に置かれますが、Git には含めていません。
**同じバージョン**を入れてください。

| プラグイン | バージョン | 入手元（一例） |
|---|---|---|
| Extenject / Zenject | **9.2.0** | https://github.com/Mathijs-Bakker/Extenject ／ Asset Store「Extenject Dependency Injection IOC」 |
| UniTask (Cysharp) | **2.3.3** | https://github.com/Cysharp/UniTask （tag 2.3.3） |
| UniRx (neuecc) | 要確認（インストール時にバージョン控え） | https://github.com/neuecc/UniRx ／ Asset Store「UniRx」 |
| DOTween (Demigiant) | 要確認（下記パネルで確認可） | https://dotween.demigiant.com/ ／ Asset Store「DOTween (HOTween v2)」 |

- いずれも `Assets/Plugins/<プラグイン名>/` に展開されます（既存の `.gitignore` がフォルダ本体を除外、フォルダの `*.meta` のみコミット）。
- DOTween は導入後に `Tools > Demigiant > DOTween Utility Panel > Setup DOTween...` を実行してください。
- バージョンを確定できたら、上表の「要確認」を実際の値に更新しておくと次回以降確実です。

## 4. NuGet / MCP 関連 DLL（`Assets/Plugins/NuGet`、Git 未収録）

uLoopMCP の依存 DLL（`McpPlugin` / `ReflectorNet` / SignalR 系 `Microsoft.AspNetCore.*`）です。
**ゲーム本体ではなく、Claude Code 連携（MCP）ツール用**です。

- manifest.json の OpenUPM スコープ（`com.ivanmurzak` / `org.nuget.*`）経由で、uLoopMCP 導入時に復元・配置されます。
- そのPCで Claude Code / uLoopMCP を使わないなら、無くてもゲーム自体のビルドには影響しません。
- もし `Assets/Plugins/NuGet` が無く MCP 関連のコンパイルエラーが出る場合は、uLoopMCP のセットアップ（`unity-mcp-cli` の初期セットアップ）を再実行してください。

## 5. 開いた後の確認

- Console に「Missing script」「GUID」関連のエラーが出ていないこと
- マテリアルがピンク色になっていないこと
- これらが出る場合、プラグインの **バージョン不一致** が原因の可能性が高いです。該当プラグインを正しいバージョンで入れ直してください。
