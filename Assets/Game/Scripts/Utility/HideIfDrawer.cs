using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomPropertyDrawer( typeof( HideIfAttribute ) )]
public class HideIfDrawer : PropertyDrawer
{
    public override float GetPropertyHeight( SerializedProperty property, GUIContent label )
    {
        if( ShouldHide( property ) )
            return 0f;

        return EditorGUI.GetPropertyHeight( property, label, true );
    }

    public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
    {
        if( !ShouldHide( property ) )
        {
            EditorGUI.PropertyField( position, property, label, true );
        }
    }

    private bool ShouldHide( SerializedProperty property )
    {
        HideIfAttribute hideIf = ( HideIfAttribute ) attribute;

        object target = property.serializedObject.targetObject;

        FieldInfo field = target.GetType().GetField(
            hideIf.conditionName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );

        if( field == null )
            return false;

        if( field.FieldType == typeof( bool ) )
        {
            return ( bool ) field.GetValue( target );
        }

        return false;
    }
}