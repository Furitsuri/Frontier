using Frontier.Entities;
using Frontier.Registries;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Frontier.Loaders;

public class CharacterFactory
{
    [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
    [Inject] private PrefabRegistry _prefabReg      = null;

    static private List<GameObject>[] CharacterPrefabs  = null;

    /// <summary>
    /// 引数で指定された内容でキャラクターを作成します
    /// ※レベルを指定してテーブルからパラメータを引用するようにしていますが、
    /// 　今回はレベルの概念を用いないため、すべて1としています
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="prefabIndex"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public Character CreateCharacter( CHARACTER_TAG tag, int prefabIndex, int level = 1 )
    {
        CharacterPrefabs = new List<GameObject>[]
        {
            new List<GameObject>(_prefabReg.PlayerPrefabs),
            new List<GameObject>(_prefabReg.EnemyPrefabs),
            new List<GameObject>(_prefabReg.OtherPrefabs),
        };

        Character chara = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Character>( CharacterPrefabs[( int )tag][prefabIndex], true, false, typeof( Character ).Name );
        chara.Setup();

        return chara;
    }

    /// <summary>
    /// ステータスを指定してキャラクターを作成します
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="prefabIndex"></param>
    /// <param name="deployData"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public Character CreateCharacter( CHARACTER_TAG tag, int prefabIndex, BattleFileLoader.CharacterDeployData deployData, int level = 1 )
    {
        Character chara = CreateCharacter( tag, prefabIndex, level );
        if( null == chara ) {
            Debug.LogError( $"キャラクターの生成に失敗しました タグ:{tag} プレハブインデックス:{prefabIndex}" );
            return null;
        }

        chara.Init();
        chara.Apply( deployData ); // ステータスデータを適応

        if( tag != CHARACTER_TAG.PLAYER )
        {
            var npc = chara as Npc;
            npc.ThinkingType = ( ( ThinkingType ) deployData.ThinkType );
        }

        return chara;
    }

    /// <summary>
    /// Statusを指定してキャラクターを作成します。
    /// UserDomain.Members(List&lt;Status&gt;)からキャラクターを再構築する用途を想定しています。
    /// プレハブは Status.PrefabIndex を使用します。
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="status"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public Character CreateCharacter( CHARACTER_TAG tag, Status status, int level = 1 )
    {
        Character chara = CreateCharacter( tag, status.PrefabIndex, level );
        if( null == chara )
        {
            Debug.LogError( $"キャラクターの生成に失敗しました タグ:{tag} プレハブインデックス:{status.PrefabIndex}" );
            return null;
        }

        chara.Init();
        chara.GetStatusRef = status;

        return chara;
    }
}