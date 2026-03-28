using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Registries;
using System;
using System.IO;
using UnityEngine;
using Zenject;

namespace Frontier.Loaders
{
    public class GeneralFileLoader
    {
        [Serializable]
        public struct FileSkillData
        {
            public string Name;
            public int Cost;
            public int SituationType;
            public int ActionType;
            public int Flags;
            public int Duration;
            public int RangeShape;
            public int RangeValue;
            public int TargetingMode;
            public int TargetingValue;
            public float AddAtkMag;
            public float AddDefMag;
            public int AddAtkNum;
            public float Param1;
            public float Param2;
            public float Param3;
            public float Param4;
            public string ExplainTextKey;
        }

        [Serializable]
        public class SkillDataContainer
        {
            public FileSkillData[] SkillsData;
        }

        [Inject] private FilePathRegistry _filePathReg = null;

        /// <summary>
        /// 各スキルのデータをロードします
        /// </summary>
        public void LoadSkillsData()
        {
            string json = File.ReadAllText( _filePathReg.SkillDataFilePath );
            var dataContainer = JsonUtility.FromJson<SkillDataContainer>( json );
            if( dataContainer == null ) return;
            for( int i = 0; i < ( int ) SkillID.NUM; ++i )
            {
                ApplySkillsData( ref SkillsData.data[i], dataContainer.SkillsData[i] );
            }
        }

        /// <summary>
        /// スキルデータを適応させます
        /// </summary>
        /// <param name="data">適応先のスキルデータ</param>
        /// <param name="fdata">適応元のファイルから読み取ったスキルデータ</param>
        private void ApplySkillsData( ref SkillsData.Data data, in FileSkillData fdata )
        {
            data.Name           = fdata.Name;
            data.Cost           = fdata.Cost;
            data.SituationType  = ( SituationType ) fdata.SituationType;
            data.ActionType     = ( ActionType ) fdata.ActionType;
            data.Flags          = ( SkillBitFlag ) fdata.Flags;
            data.Duration       = fdata.Duration;
            data.RangeShape     = ( RangeShape ) fdata.RangeShape;
            data.RangeValue     = fdata.RangeValue;
            data.TargetingMode  = ( TargetingMode ) fdata.TargetingMode;
            data.TargetingValue = fdata.TargetingValue;
            data.AddAtkMag      = fdata.AddAtkMag;
            data.AddDefMag      = fdata.AddDefMag;
            data.AddAtkNum      = fdata.AddAtkNum;
            data.Param1         = fdata.Param1;
            data.Param2         = fdata.Param2;
            data.Param3         = fdata.Param3;
            data.Param4         = fdata.Param4;
            data.ExplainTextKey = fdata.ExplainTextKey;
            // TODO : nullアクセス防止の暫定対応。後で消すこと
            if( null == data.ExplainTextKey ) { data.ExplainTextKey = ""; }
        }
    }
}