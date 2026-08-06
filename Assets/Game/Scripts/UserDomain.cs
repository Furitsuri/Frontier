using Frontier.Combat;
using Frontier.Entities;
using Frontier.Field;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserDomain
{
    [SerializeField] public int Money { get; private set; } = 0;
    // 部隊で共有し、キャラクター個別のレベルアップに割り振れるポイント。
    // 名称は仮のもの(「経験値」という呼び方は個人が貯めるものという印象を与えるため、後日変更予定)。
    [SerializeField] public int Exp { get; private set; } = 0;
    [SerializeField] public int StageLevel { get; private set; } = 0;   // 0オリジンのため、0はステージレベル1を表します
    [SerializeField] public List<Status> Members { get; private set; } = new List<Status>();
    // 所持しているスキルとその個数(ステージ攻略報酬・店購入等で増える想定)。
    // 同じスキルを複数所持することで、複数のキャラクターに同時に装備させられる。
    [SerializeField] public List<SkillInventoryEntry> SkillInventory { get; private set; } = new List<SkillInventoryEntry>();

    public void AddMoney( int amount )
    {
        Money += amount;
    }

    public void AddExp( int amount )
    {
        Exp += amount;
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
    /// 指定スキルの所持数を返します(未所持の場合は0)。
    /// </summary>
    public int GetSkillCount( SkillID skillID )
    {
        for ( int i = 0; i < SkillInventory.Count; ++i )
        {
            if ( SkillInventory[i].SkillID == skillID ) { return SkillInventory[i].Count; }
        }

        return 0;
    }

    /// <summary>
    /// 指定スキルの所持数を増減させます(ステージ攻略報酬・店購入・売却等)。
    /// 負の値を渡すことで減らすことも出来ますが、所持数は0未満になりません。
    /// </summary>
    public void AddSkill( SkillID skillID, int amount )
    {
        for ( int i = 0; i < SkillInventory.Count; ++i )
        {
            if ( SkillInventory[i].SkillID != skillID ) { continue; }

            var entry = SkillInventory[i];
            entry.Count = Mathf.Max( 0, entry.Count + amount );
            SkillInventory[i] = entry;

            return;
        }

        if ( 0 < amount )
        {
            SkillInventory.Add( new SkillInventoryEntry { SkillID = skillID, Count = amount } );
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void Debug_SetMoney( int value )       => Money = value;
    public void Debug_SetExp( int value )         => Exp = value;
    public void Debug_SetStageLevel( int value )  => StageLevel = value;
    public void Debug_ClearMembers()              => Members.Clear();
    public void Debug_ClearSkillInventory()       => SkillInventory.Clear();
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
            Money          = Money,
            Exp            = Exp,
            StageLevel     = StageLevel,
            Members        = new List<Status>( Members ),
            SkillInventory = new List<SkillInventoryEntry>( SkillInventory ),
            FieldProgress  = fieldProgress,
        };
    }

    /// <summary>
    /// セーブデータ(DTO)の内容を反映します。FieldProgressの反映は呼び出し側の責務とします。
    /// </summary>
    public void ApplySaveData( UserSaveData data )
    {
        Money      = data.Money;
        Exp        = data.Exp;
        StageLevel = data.StageLevel;
        Members.Clear();
        Members.AddRange( data.Members );
        SkillInventory.Clear();
        if ( data.SkillInventory != null ) { SkillInventory.AddRange( data.SkillInventory ); }
    }
}