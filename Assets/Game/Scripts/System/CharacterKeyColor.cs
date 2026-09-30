using Frontier.Entities;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// キャラクターの種類(CHARACTER_TAG)とインデックスから、キャラクター毎に一意の表示色を決定します。
    /// 移動経路の矢印や、移動前の位置を示すタイル外枠など、どのキャラクターに属する表示かを色で判別させたい箇所で共通して使用してください。
    /// </summary>
    public static class CharacterKeyColor
    {
        /// <summary>
        /// CHARACTER_TAG ごとに色相帯を割り当て、CharacterIndex で帯内の色相をずらした色を返します。
        /// </summary>
        public static Color Resolve( in CharacterKey charaKey )
        {
            float baseHue = charaKey.CharacterTag switch
            {
                CHARACTER_TAG.PLAYER => 210f,
                CHARACTER_TAG.ENEMY  =>   0f,
                CHARACTER_TAG.OTHER  => 120f,
                _                    =>  60f,
            };

            float hue = ( ( baseHue + charaKey.CharacterIndex * 10f ) % 360f ) / 360f;
            return Color.HSVToRGB( hue, 0.85f, 1.0f );
        }
    }
}
