using UnityEngine;

namespace SymmetryBreakStudio.TastyGrassShader
{
    /// <summary>
    /// NOTE: Sometimes doesn't work if the order of attributes is not correct. This attribute should not be the last one.
    /// </summary>
    public class DisableGroupAttribute : PropertyAttribute
    {
        public ValueType valueType;
        public int compareValue;
        public bool invertCondition;
        public string valueName;

        public DisableGroupAttribute(string valueName, ValueType valueType = ValueType.BoolAsInt, int compareValue = 0,
            bool invertCondition = false)
        {
            this.valueName = valueName;
            this.valueType = valueType;
            this.compareValue = compareValue;
            this.invertCondition = invertCondition;
        }
    }

    public enum ValueType
    {
        Int,
        BoolAsInt, // False = 0, True = 1.
        FloatAsInt, // Use float casted to int.
        EnumAsInt // Use raw enum int value.
    }
}