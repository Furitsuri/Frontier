using System;
using System.Collections.Generic;
using Frontier.Combat;
using Frontier.Entities;
using Frontier.Field;

/// <summary>
/// ユーザーのプレイ進行状況を表すセーブデータ本体(ファイルI/O用のDTO)。
/// UserDomain/GameSession.FieldProgress とは独立したスキーマとして保持し、
/// 実行時のドメインオブジェクトの都合(プロパティ構成等)に左右されないようにする。
/// Versionはセーブデータのスキーマ変更に備えたバージョン番号。
/// </summary>
[Serializable]
public class UserSaveData
{
    public int    Version = 1;
    public string SavedAt = "";

    /// <summary>
    /// 保存時にアクティブだったシーン名。ロード確定時、このシーンへ遷移する。
    /// 現状セーブはFieldSceneからのみ行われるため常に"FieldScene"になるが、
    /// 将来他のシーンからの保存に対応した際にそのまま分岐できるよう、実際のシーン名を保持する。
    /// </summary>
    public string SceneName = "";

    public int          Money;
    public int          Exp;
    public int          StageLevel;
    public List<Status> Members = new List<Status>();
    public List<SkillInventoryEntry> SkillInventory = new List<SkillInventoryEntry>();

    public FieldProgress FieldProgress;
}
