using Frontier.Combat.Skill;
using Frontier.DebugTools.StageEditor;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Stage
{
    public class StageFileLoader : MonoBehaviour
    {
        [SerializeField]
        private List<string> _stageNames;

        [Inject] private IStageDataProvider _stageDataProvider  = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;

        private GameObject[] _tilePrefabs;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tilePregabs"></param>
        public void Init( GameObject[] tilePregabs )
        {
            NullCheck.AssertNotNull( _stageDataProvider , nameof( _stageDataProvider ) );
            NullCheck.AssertNotNull( _hierarchyBld , nameof( _hierarchyBld ) );

            _tilePrefabs = tilePregabs;
        }

        /// <summary>
        /// �X�e�[�W�f�[�^���t�@�C�������w�肷�邱�Ƃœǂݍ��݂܂�
        /// </summary>
        /// <param name="fileName">�w�肷��t�@�C����</param>
        /// <returns>�Ǎ��̐���</returns>
        public bool Load( string fileName )
        {
            var data = StageDataSerializer.Load(fileName);
            if ( data == null ) { return false; }

            // �����̃X�e�[�W�f�[�^�����݂���ꍇ�͔j��
            if ( null != _stageDataProvider.CurrentData )
            {
                _stageDataProvider.CurrentData.Dispose();
            }

            var row = data.GridRowNum;
            var col = data.GridColumnNum;
            _stageDataProvider.CurrentData.Init( row, col ); // �V�����X�e�[�W�f�[�^��������

            for ( int x = 0; x < col; x++ )
            {
                for ( int y = 0; y < row; y++ )
                {
                    var srcTile = data.GetTile(x, y);
                    _stageDataProvider.CurrentData.SetTile( x, y, _hierarchyBld.InstantiateWithDiContainer<StageTileData>( false ) );
                    _stageDataProvider.CurrentData.GetTile( x, y ).Init( x, y, srcTile.Height, srcTile.Type, _tilePrefabs );
                }
            }

            return true;
        }

        /// <summary>
        /// �X�e�[�W�f�[�^���t�@�C�����z��ɃC���f�b�N�X���w�肷��`�œǍ��݂܂�
        /// </summary>
        /// <param name="stageNameIdx">�X�e�[�W���z��ւ̃C���f�b�N�X�l</param>
        /// <returns>�Ǎ��̐���</returns>
        public bool Load( int stageNameIdx )
        {
            return Load( _stageNames[stageNameIdx] );
        }
    }
}