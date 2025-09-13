using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier.Stage
{
    static public class TileMaterialLibrary
    {
        private static Dictionary<TileType, Material> _materials;

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

            _materials = new Dictionary<TileType, Material>();

            for (int i = 0; i < (int)TileType.NUM; i++)
            {
                if (i == (int)TileType.None)
                {
                    _materials[TileType.None] = new Material(Shader.Find("Standard"));  // TODO : URPに適応した場合は、"Universal Render Pipeline/Lit"に変更する必要がある
                    continue;
                }

                TileType type = (TileType)i;
                string materialPath = $"{Constants.TILE_MATERIALS_FOLDER_PASS}{strings[i]}";
                Material material = Resources.Load<Material>(materialPath);

                if (material == null)
                {
                    Debug.LogError($"Material not found at path: {materialPath}");
                    continue;
                }

                _materials[type] = material;
            }
        }

        static public Material GetMaterial(TileType type)
        {
            return _materials[type];
        }

        static public Vector3 GetDefaultTileScale()
        {
            // タイルのデフォルトスケールを返す
            return new Vector3(TILE_SIZE, TILE_THICKNESS_MIN, TILE_SIZE);
        }

        static public Vector3 GetScale(TileType type)
        {
            // 高さ調整もここで一元化
            return new Vector3(1, 0.1f, 1);
        }
    }
}