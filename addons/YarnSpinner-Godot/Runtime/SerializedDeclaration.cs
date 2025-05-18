#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Yarn;
using Yarn.Compiler;

namespace YarnSpinnerGodot;

/// <summary>
/// A declaration of a variable that is written to a yarn project
/// </summary>
[Tool]
public class SerializedDeclaration
{
    public static List<IType> TypesList = new()
    {
        Types.String,
        Types.Boolean,
        Types.Number
    };

    public string name;

    public string typeName = Types.String.Name;

    public bool defaultValueBool;
    public float defaultValueNumber;
    public string defaultValueString;

    public string description;

    public bool isImplicit;

    public string sourceYarnAssetPath;

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
        sourceYarnAssetPath = ProjectSettings.LocalizePath(decl.SourceFileName);

        if (typeName == Types.String.Name)
        {
            defaultValueString = Convert.ToString(decl.DefaultValue, CultureInfo.InvariantCulture);
        }
        else if (typeName == Types.Boolean.Name)
        {
            defaultValueBool = Convert.ToBoolean(decl.DefaultValue);
        }
        else if (typeName == Types.Number.Name)
        {
            defaultValueNumber = Convert.ToSingle(decl.DefaultValue);
        }
        else if (decl.Type is EnumType enumDecl)
        {
            typeName = $"{enumDecl.Name} Enum ({enumDecl.RawType.Name})";

            var defaultValue = decl.DefaultValue;
            if (enumDecl.RawType.Name == Types.String.Name)
            {
                defaultValueString = Convert.ToString(defaultValue, CultureInfo.InvariantCulture);
            }
            else if (enumDecl.RawType.Name == Types.Boolean.Name)
            {
                defaultValueBool = Convert.ToBoolean(defaultValue);
            }
            else if (enumDecl.RawType.Name == Types.Number.Name)
            {
                defaultValueNumber = Convert.ToSingle(defaultValue);
            }
        }
        else

        {
            throw new InvalidOperationException($"Invalid declaration type {decl.Type.Name}");
        }
    }
}