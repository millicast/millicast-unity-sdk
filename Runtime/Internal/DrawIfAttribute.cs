using UnityEngine;
using System;
 
/// <summary>
/// Draws the field/property ONLY if the compared property compared by the comparison type with the value of comparedValue returns true.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class DrawIfAttribute : PropertyAttribute
{
    #region Fields
 
    public string comparedPropertyName { get; private set; }
    public object comparedValue { get; private set; }
    public bool invert { get; private set; }
 
    #endregion
 
    /// <summary>
    /// Only draws the field only if a condition is met. Supports enum and bools.
    /// </summary>
    /// <param name="comparedPropertyName">The name of the property that is being compared (case sensitive).</param>
    /// <param name="comparedValue">The value the property is being compared to.</param>
    /// <param name="reverse">applies reverse of the condition result i.e hides if the condition is true / shows if the condition is false.</param>
    public DrawIfAttribute(string comparedPropertyName, object comparedValue, bool reverse = false)
    {
        this.comparedPropertyName = comparedPropertyName;
        this.comparedValue = comparedValue;
        invert = (bool) reverse;
    }
}