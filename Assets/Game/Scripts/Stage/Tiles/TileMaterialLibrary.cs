using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier.Stage
{
    static public class TileMaterialLibrary
    {
        // タイルタイプごとのプロファイル（マテリアル＋見た目/挙動の差分）
        private static Dictionary<TileType, TileProfile> _profiles;

        static public void Init()
        {
            string[] strings = new string[(int)TileType.NUM]
            {
                "None",
                "Grass",
                "Plant",
                "Wasteland",
                "Sand",
                "Water",
                "Mountain",
                "Forest"
            };

            _profiles = new Dictionary<TileType, TileProfile>();

            for (int i = 0; i < (int)TileType.NUM; i++)
            {
                TileType type = (TileType)i;

                if (i == (int)TileType.None)
                {
                    // TODO : URPに適応した場合は、"Universal Render Pipeline/Lit"に変更する必要がある
                    _profiles[type] = CreateProfile(type, new Material(Shader.Find("Standard")));
                    continue;
                }

                string materialPath = $"{Constants.TILE_MATERIALS_FOLDER_PASS}{strings[i]}";
                Material material = Resources.Load<Material>(materialPath);

                if (material == null)
                {
                    Debug.LogError($"Material not found at path: {materialPath}");
                    continue;
                }

                _profiles[type] = CreateProfile(type, material);
            }
        }

        /// <summary>
        /// タイプごとのプロファイルを生成します。
        /// 特殊仕様を持つタイルはここに分岐を1つ足すだけで定義できます。
        /// </summary>
        private static TileProfile CreateProfile(TileType type, Material material)
        {
            switch (type)
            {
                // 水：見た目を少しだけ低く沈ませ、水同士の側面はカリングしてシームレスにする
                case TileType.Water:
                    return new TileProfile(material, WATER_VISUAL_HEIGHT_OFFSET, useSideFaceCulling: true);

                // それ以外：通常の不透明タイル（沈み無し・カリング無し）
                default:
                    return new TileProfile(material, 0f, useSideFaceCulling: false);
            }
        }

        static public TileProfile GetProfile(TileType type)
        {
            return _profiles[type];
        }

        static public Material GetMaterial(TileType type)
        {
            return _profiles[type].Material;
        }

        static public Vector3 GetDefaultTileScale()
        {
            // タイルのデフォルトスケールを返す
            return new Vector3(TILE_SIZE, TILE_MIN_THICKNESS, TILE_SIZE);
        }

        static public Vector3 GetScale(TileType type)
        {
            // 高さ調整もここで一元化
            return new Vector3(1, 0.1f, 1);
        }
    }
}
