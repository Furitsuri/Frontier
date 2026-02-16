using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomPropertyDrawer( typeof( ShowIfAttribute ) )]
public class ShowIfDrawer : PropertyDrawer
{
    public override float GetPropertyHeight( SerializedProperty property, GUIContent label )
    {
        ShowIfAttribute showIf = ( ShowIfAttribute ) attribute;

        if( ShouldShow( property, showIf ) )
        {
            return EditorGUI.GetPropertyHeight( property, label, true );
        }

        return 0f; // 非表示なら高さ0
    }

    public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
    {
        ShowIfAttribute showIf = ( ShowIfAttribute ) attribute;

        if( ShouldShow( property, showIf ) )
        {
            EditorGUI.PropertyField( position, property, label, true );
        }
    }

    private bool ShouldShow( SerializedProperty property, ShowIfAttribute showIf )
    {
        object target = property.serializedObject.targetObject;

        FieldInfo conditionField = target.GetType()
            .GetField( showIf.conditionName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );

        if( conditionField == null )
        {
            Debug.LogWarning( $"ShowIf: 条件フィールド '{showIf.conditionName}' が見つかりません" );
            return true;
        }

        if( conditionField.FieldType == typeof( bool ) )
        {
            return ( bool ) conditionField.GetValue( target );
        }

        Debug.LogWarning( "ShowIf: bool型のみ対応しています" );
        return true;
    }
}