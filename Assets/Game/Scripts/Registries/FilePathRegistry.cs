using System.IO;
using UnityEngine;

namespace Frontier.Registries
{
    public class FilePathRegistry : MonoBehaviour
    {
        [Header( "各ステージデータの参照先" )]
        [SerializeField] private string[] _stageNames;

        [Header( "各味方キャラクターのパラメータ参照先" )]
        [SerializeField] private string[] _playerParamFilePath;

        [Header( "各スキルデータのパラメータ参照先" )]
        [SerializeField] private string _skillDataFilePath;

        [Header( "フィールドデータのID一覧" )]
        [SerializeField] private string[] _fieldIds;

        [Header( "近接攻撃時のカメラパラメータの参照先" )]
        [SerializeField] private string _closeAtkCameraParamFilePath;

        [Header( "遠隔攻撃時のカメラパラメータの参照先" )]
        [SerializeField] private string _rangedAtkCameraParamFilePath;

        public string[] StageNames  => _stageNames;
        public string[] FieldIds    => _fieldIds;
        public string[] PlayerParamFilePath => _playerParamFilePath;
        public string SkillDataFilePath => _skillDataFilePath;
        public string CloseAtkCameraParamFilePath => _closeAtkCameraParamFilePath;
        public string RangedAtkCameraParamFilePath => _rangedAtkCameraParamFilePath;

        /// <summary>
        /// 指定ステージの敵キャラクターパラメータファイルのパスを、StageNamesから導出して取得します
        /// </summary>
        public string GetEnemyParamFilePath( int stageIndex ) => BuildCharacterDataPath( stageIndex, "Enemy" );

        /// <summary>
        /// 指定ステージの第三軍勢キャラクターパラメータファイルのパスを、StageNamesから導出して取得します
        /// </summary>
        public string GetOtherParamFilePath( int stageIndex ) => BuildCharacterDataPath( stageIndex, "Other" );

        // _stageNames[stageIndex] を冠にしたディレクトリ・ファイル名を組み立てる。
        // _enemyParamFilePath/_otherParamFilePath を個別配列として持つと、_stageNamesとのインデックス対応が崩れるため、
        // ステージ名から一意に導出する方式に統一している。
        private string BuildCharacterDataPath( int stageIndex, string characterTagName )
        {
            string stageName = _stageNames[stageIndex];
            return Path.Combine(
                "Assets", "Resources", "CharactersData", stageName,
                $"Frontier_{stageName}_CharacterData_{characterTagName}.json" );
        }
    }
}