using System;
using System.Collections.Generic;
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

    public int          Money;
    public int          StageLevel;
    public List<Status> Members = new List<Status>();

    public FieldProgress FieldProgress;
}
