using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(DrawIfAttribute))]
public class DrawIfPropertyDrawer : PropertyDrawer
{
    #region Fields
 
    // Reference to the attribute on the property.
    DrawIfAttribute drawIf;
 
    // Field that is being compared.
    SerializedProperty comparedField;
 
    #endregion
 
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!ShowMe(property))
        {
           return 0;
        }
           
        float totalHeight = EditorGUI.GetPropertyHeight(property, label) ;
        float height = 0;
        while (property.NextVisible(true) && property.hasVisibleChildren && property.isExpanded)
        {
            if (!ShowMe(property))
                break;
            height += EditorGUI.GetPropertyHeight(property, label, true) ;
        }
        return Mathf.Max(totalHeight, height);
    }
 
    /// <summary>
    /// Errors default to showing the property.
    /// </summary>
    private bool ShowMe(SerializedProperty property)
    {
        drawIf = attribute as DrawIfAttribute;
        // Replace propertyname to the value from the parameter
        string path = property.propertyPath.Contains(".") ? System.IO.Path.ChangeExtension(property.propertyPath, drawIf.comparedPropertyName) : drawIf.comparedPropertyName;
 
        comparedField = property.serializedObject.FindProperty(path);
 
        if (comparedField == null)
        {
            return false;
        }
        bool inverse = drawIf.invert;

        // get the value & compare based on types
        switch (comparedField.type)
        { // Possible extend cases to support your own type
            case "bool":
                return inverse ? !comparedField.boolValue.Equals(drawIf.comparedValue) : comparedField.boolValue.Equals(drawIf.comparedValue);
            case "Enum":
                return inverse ? !comparedField.enumValueIndex.Equals((int)drawIf.comparedValue) : comparedField.enumValueIndex.Equals((int)drawIf.comparedValue);
            default:
                return inverse ? false : true;
        }
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShowMe(property))
        {
            EditorGUI.PropertyField(position, property, label, true);
        }  
    }
 }
#endif