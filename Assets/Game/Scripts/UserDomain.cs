using Frontier.Combat;
using Frontier.Entities;
using Frontier.Field;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserDomain
{
    // 敵撃破報酬・雇用・スキル購入・キャラクターのレベルアップ等、あらゆる用途で共有される単一の触媒。
    // 元は所持金(Money)とキャラクター個別のレベルアップに割り振れるポイント(Exp)に分かれていたが、
    // ゲーム内では両者を区別する必要が無いため統合した。
    [SerializeField] public int Anima { get; private set; } = 0;
    [SerializeField] public int StageLevel { get; private set; } = 0;   // 0オリジンのため、0はステージレベル1を表します
    [SerializeField] public List<Status> Members { get; private set; } = new List<Status>();
    // 所持しているスキルとその個数(ステージ攻略報酬・店購入等で増える想定)。アクティブ/パッシブで別々に保持する。
    // 同じスキルを複数所持することで、複数のキャラクターに同時に装備させられる。
    [SerializeField] public List<ActiveSkillInventoryEntry> ActiveSkillInventory { get; private set; } = new List<ActiveSkillInventoryEntry>();
    [SerializeField] public List<PassiveSkillInventoryEntry> PassiveSkillInventory { get; private set; } = new List<PassiveSkillInventoryEntry>();
    // ショップの品揃え抽選等に使う、セーブファイル(周回)ごとに一度だけ決まる乱数シード。
    // ステージ/ノード固有IDと組み合わせて使うことで、同一セーブ内では常に同じ抽選結果を再現しつつ、
    // 別のセーブファイル(周回)では異なる結果になる。コンストラクタで新規生成し、ロード時はセーブ値で上書きする。
    [SerializeField] public int WorldSeed { get; private set; }

    public UserDomain()
    {
        // GameSessionのフィールド初期化子経由でUnityのシリアライズ処理中に構築されることがあるため、
        // その最中の呼び出しが禁止されているUnityEngine.Randomではなく、System.Randomを使う。
        WorldSeed = new System.Random().Next( int.MinValue, int.MaxValue );
    }

    public void AddAnima( int amount )
    {
        Anima += amount;
    }

    public void IncreaseStageLevel()
    {
        StageLevel++;
    }

    /// <summary>
    /// 味方を編成に加えます。
    /// シーンをまたいで永続化するデータのため、Character(Player)ではなく値型のStatusとして保持します。
    /// </summary>
    public void RecruitMember( Status status )
    {
        status.characterIndex = Members.Count; // characterIndexは0オリジンのため、カウント数がそのままIndexになります

        Members.Add( status );
    }

    /// <summary>
    /// 指定インデックスの味方を編成から除名します。
    /// characterIndexは「Members内の位置」を表す規約(RecruitMember参照)のため、
    /// 除名後は残ったメンバー全員のcharacterIndexを詰め直します。
    /// </summary>
    public void DismissMember( int index )
    {
        Members.RemoveAt( index );

        for( int i = 0; i < Members.Count; ++i )
        {
            var status = Members[i];
            status.characterIndex = i;
            Members[i] = status;
        }
    }

    /// <summary>
    /// 指定スキルの所持数を返します(未所持の場合は0)。
    /// </summary>
    public int GetSkillCount( ActiveSkillID skillID )
    {
        for ( int i = 0; i < ActiveSkillInventory.Count; ++i )
        {
            if ( ActiveSkillInventory[i].SkillID == skillID ) { return ActiveSkillInventory[i].Count; }
        }

        return 0;
    }

    /// <summary>
    /// 指定スキルの所持数を増減させます(ステージ攻略報酬・店購入・売却等)。
    /// 負の値を渡すことで減らすことも出来ますが、所持数は0未満になりません。
    /// </summary>
    public void AddSkill( ActiveSkillID skillID, int amount )
    {
        for ( int i = 0; i < ActiveSkillInventory.Count; ++i )
        {
            if ( ActiveSkillInventory[i].SkillID != skillID ) { continue; }

            var entry = ActiveSkillInventory[i];
            entry.Count = Mathf.Max( 0, entry.Count + amount );
            ActiveSkillInventory[i] = entry;

            return;
        }

        if ( 0 < amount )
        {
            ActiveSkillInventory.Add( new ActiveSkillInventoryEntry { SkillID = skillID, Count = amount } );
        }
    }

    /// <summary>
    /// 指定パッシブスキルの所持数を返します(未所持の場合は0)。
    /// </summary>
    public int GetSkillCount( PassiveSkillID skillID )
    {
        for ( int i = 0; i < PassiveSkillInventory.Count; ++i )
        {
            if ( PassiveSkillInventory[i].SkillID == skillID ) { return PassiveSkillInventory[i].Count; }
        }

        return 0;
    }

    /// <summary>
    /// 指定パッシブスキルの所持数を増減させます。所持数は0未満になりません。
    /// </summary>
    public void AddSkill( PassiveSkillID skillID, int amount )
    {
        for ( int i = 0; i < PassiveSkillInventory.Count; ++i )
        {
            if ( PassiveSkillInventory[i].SkillID != skillID ) { continue; }

            var entry = PassiveSkillInventory[i];
            entry.Count = Mathf.Max( 0, entry.Count + amount );
            PassiveSkillInventory[i] = entry;

            return;
        }

        if ( 0 < amount )
        {
            PassiveSkillInventory.Add( new PassiveSkillInventoryEntry { SkillID = skillID, Count = amount } );
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void Debug_SetAnima( int value )        => Anima = value;
    public void Debug_SetWorldSeed( int value )   => WorldSeed = value;
    public void Debug_SetStageLevel( int value )  => StageLevel = value;
    public void Debug_ClearMembers()              => Members.Clear();
    public void Debug_ClearSkillInventory()       { ActiveSkillInventory.Clear(); PassiveSkillInventory.Clear(); }
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

    /// <summary>
    /// 現在の状態をセーブデータ(DTO)へ変換します。
    /// FieldProgressはUserDomainが保持していないため、呼び出し側(GameSession等)から渡します。
    /// </summary>
    public UserSaveData ToSaveData( FieldProgress fieldProgress )
    {
        return new UserSaveData
        {
            SavedAt        = DateTime.Now.ToString( "yyyy/MM/dd HH:mm" ),
            Anima          = Anima,
            StageLevel     = StageLevel,
            WorldSeed      = WorldSeed,
            Members        = new List<Status>( Members ),
            SkillInventory        = new List<ActiveSkillInventoryEntry>( ActiveSkillInventory ),
            PassiveSkillInventory = new List<PassiveSkillInventoryEntry>( PassiveSkillInventory ),
            FieldProgress  = fieldProgress,
        };
    }

    /// <summary>
    /// セーブデータ(DTO)の内容を反映します。FieldProgressの反映は呼び出し側の責務とします。
    /// </summary>
    public void ApplySaveData( UserSaveData data )
    {
        Anima      = data.Anima;
        StageLevel = data.StageLevel;
        WorldSeed  = data.WorldSeed;
        Members.Clear();
        Members.AddRange( data.Members );

        // パッシブスキル枠追加以前のセーブデータ等、装備スキル枠の数が現行と異なるデータを補完する
        for ( int i = 0; i < Members.Count; ++i )
        {
            var status = Members[i];
            status.EnsureSkillSlots();
            Members[i] = status;
        }
        ActiveSkillInventory.Clear();
        if ( data.SkillInventory != null ) { ActiveSkillInventory.AddRange( data.SkillInventory ); }
        PassiveSkillInventory.Clear();
        if ( data.PassiveSkillInventory != null ) { PassiveSkillInventory.AddRange( data.PassiveSkillInventory ); }
    }
}