using System;
using System.Collections.Generic;
using Godot;
using Yarn.Compiler;
namespace Yarn.GodotIntegration
{
    /// <summary>
    /// A declaration of a variable that is written to a yarn project
    /// </summary>
    [Serializable]
    public class SerializedDeclaration: Resource
    {
        public static List<IType> BuiltInTypesList = new List<IType>
        {
            BuiltinTypes.String,
            BuiltinTypes.Boolean,
            BuiltinTypes.Number
        };

        [Export] public string name = "$variable";

        [Export] public string typeName = BuiltinTypes.String.Name;

        [Export] public bool defaultValueBool;
        [Export] public float defaultValueNumber;
        [Export] public string defaultValueString;

        [Export] public string description;

        [Export] public bool isImplicit;

        [Export] public string sourceYarnAssetPath;

        /// <summary>
        /// Set all of the serialized properties from a <see cref="Declaration"/> instance.
        /// </summary>
        /// <param name="decl"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void SetDeclaration(Declaration decl)
        {
            name = decl.Name;
            typeName = decl.Type.Name;
            description = decl.Description;
            isImplicit = decl.IsImplicit;
            sourceYarnAssetPath = decl.SourceFileName;

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