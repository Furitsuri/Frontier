using UnityEngine;

namespace Frontier.Registries
{
    public class FilePathRegistry : MonoBehaviour
    {
        [Header( "各味方キャラクターのパラメータ参照先" )]
        [SerializeField] private string[] _playerParamFilePath;

        [Header( "各敵キャラクターのパラメータ参照先" )]
        [SerializeField] private string[] _enemyParamFilePath;

        [Header( "各第三軍勢キャラクターのパラメータ参照先" )]
        [SerializeField] private string[] _otherParamFilePath;

        [Header( "各スキルデータのパラメータ参照先" )]
        [SerializeField] private string _skillDataFilePath;

        [Header( "近接攻撃時のカメラパラメータの参照先" )]
        [SerializeField] private string _closeAtkCameraParamFilePath;

        [Header( "遠隔攻撃時のカメラパラメータの参照先" )]
        [SerializeField] private string _rangedAtkCameraParamFilePath;

        public string[] PlayerParamFilePath => _playerParamFilePath;
        public string[] EnemyParamFilePath => _enemyParamFilePath;
        public string[] OtherParamFilePath => _otherParamFilePath;
        public string SkillDataFilePath => _skillDataFilePath;
        public string CloseAtkCameraParamFilePath => _closeAtkCameraParamFilePath;
        public string RangedAtkCameraParamFilePath => _rangedAtkCameraParamFilePath;
    }
}