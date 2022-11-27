using System;
using System.Collections.Generic;
using Godot;
using Yarn.Compiler;
namespace Yarn.GodotIntegration
{
    [Serializable]
    public class SerializedDeclaration
    {
        public static List<IType> BuiltInTypesList = new List<IType>
        {
            BuiltinTypes.String,
            BuiltinTypes.Boolean,
            BuiltinTypes.Number,
        };

        public string name = "$variable";


        public string typeName = BuiltinTypes.String.Name;

        public bool defaultValueBool;
        public float defaultValueNumber;
        public string defaultValueString;

        public string description;

        public bool isImplicit;

        public Resource sourceYarnAsset;

        public SerializedDeclaration(Declaration decl)
        {
            name = decl.Name;
            typeName = decl.Type.Name;
            description = decl.Description;
            isImplicit = decl.IsImplicit;

            sourceYarnAsset = ResourceLoader.Load<Resource>(decl.SourceFileName);

            if (typeName == BuiltinTypes.String.Name)
            {
                defaultValueString = Convert.ToString(decl.DefaultValue);
            }
            else if (typeName == BuiltinTypes.Boolean.Name)
            {
                defaultValueBool = Convert.ToBoolean(decl.DefaultValue);
            }
            else if (typeName == BuiltinTypes.Number.Name)
            {
                defaultValueNumber = Convert.ToSingle(decl.DefaultValue);
            }
            else
            {
                throw new InvalidOperationException($"Invalid declaration type {decl.Type.Name}");
            }
        }
    }

}