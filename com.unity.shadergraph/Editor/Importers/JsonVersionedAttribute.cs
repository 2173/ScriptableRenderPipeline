using System;

namespace UnityEditor.Importers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    class JsonVersionedAttribute : Attribute
    {
        public JsonVersionedAttribute(Type previousVersionType, string fieldName = "version")
        {

        }
    }

    interface IUpgradableTo<TTo>
    {
        TTo Upgrade();
    }

    interface IUpgradableFrom<TFrom>
    {
    }
}
