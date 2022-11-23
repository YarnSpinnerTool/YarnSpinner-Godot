using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

public partial class StringInfo : Godot.Resource
{
    [Export]
    public string text;

    [Export]
    public string nodeName;

    [Export]
    public int lineNumber;

    [Export]
    public string fileName;

    [Export]
    public bool isImplicitTag;

    [Export]
    public string[] metadata;

    public StringInfo()
    {
        this.text = "";
        this.nodeName = "";
        this.lineNumber = -1;
        this.fileName = "";
        this.isImplicitTag = false;
        this.metadata = new string[] { };
    }

    public StringInfo(string text, string nodeName, int lineNumber, string fileName, bool isImplicitTag, string[] metadata)
    {
        this.text = text;
        this.nodeName = nodeName;
        this.lineNumber = lineNumber;
        this.fileName = fileName;
        this.isImplicitTag = isImplicitTag;
        this.metadata = metadata;
    }

    public static StringInfo fromStringInfo(Yarn.Compiler.StringInfo value)
    {
        return new StringInfo(
            value.text,
            value.nodeName,
            value.lineNumber,
            value.fileName,
            value.isImplicitTag,
            value.metadata
        );
    }
}