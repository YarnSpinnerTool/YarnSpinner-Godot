using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using Godot.Collections;
using Google.Protobuf;
using Yarn;
using Yarn.Compiler;
using Array = Godot.Collections.Array;
using Directory = System.IO.Directory;
using Path = System.IO.Path;
[Tool]
public class YarnFileImporter : EditorImportPlugin
{

    public override string GetImporterName()
    {
        return "yarnproject";
    }

    public override string GetVisibleName()
    {
        return "Yarn Project";
    }
    public override Array GetRecognizedExtensions() =>
        new Array(new[] { "yarn" });

    public override string GetSaveExtension() => "yarn";
    public override string GetResourceType()
    {
        return "Resource";
    }
    public override int GetPresetCount()
    {
        return 0;
    }

    public override float GetPriority()
    {
        return 1.0f;
    }
    public override int GetImportOrder()
    {
        return 0;
    }

    public override Array GetImportOptions(int preset) 
    {
        return new Array();
    }

    private static string Base64Encode(string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    public override int Import(string sourceFile, string savePath, Dictionary options,
        Array platformVariants, Array genFiles)
    {
        GD.Print(String.Format("importing yarn {0} to {1}", sourceFile, savePath));

        Array<string> files = new Array<string>();
        string path = Path.GetDirectoryName(ProjectSettings.GlobalizePath(sourceFile));
        foreach (var file in Directory.GetFiles(path))
        {
            if (file.Contains("yarnproject"))
                continue;
            GD.Print(String.Format($"Importing yarn story: {file}"));
            files.Add(Path.Combine(path, file));
        }

        Error error = _Import_Files_Direct(Path.GetFileNameWithoutExtension(sourceFile), files, savePath, options);
        if (error != Error.Ok) {
            GD.PrintErr("could not compile files!");
            return (int)Error.CompilationFailed;
        }
        return (int)Error.Ok;
    }

    private Error _Import_Files_Direct(string name, Array<string> files, string savePath, Dictionary options)
    {
        var fileInfo = new List<FileInfo>();
        foreach (var fileName in files)
        {
            fileInfo.Add(new FileInfo(fileName));

        }
        CompilationResult compiledResults = CompileProgram(fileInfo.ToArray());
        foreach (var diagnostic in compiledResults.Diagnostics)
        {
            LogDiagnostic(diagnostic);
        }

        if (compiledResults.Diagnostics.Any(d => d.Severity == Diagnostic.DiagnosticSeverity.Error))
        {
            GD.PrintErr("Not compiling files because errors were encountered.");
            return Error.CompilationFailed;
        }

        string yarnC = null;
        using (var outStream = new MemoryStream())
        using (var codedStream = new CodedOutputStream(outStream))
        {
            compiledResults.Program.WriteTo(codedStream);
            codedStream.Flush();
            yarnC = Convert.ToBase64String(outStream.ToArray());
        }

        var stringTable = new Godot.Collections.Dictionary<string, StringInfo>();
        foreach (var item in compiledResults.StringTable)
        {
            stringTable.Add(item.Key, StringInfo.fromStringInfo(item.Value));
        }

        var resource = new CompiledYarnProject(
            yarnC,
            stringTable
        );

        ResourceSaver.SaverFlags flags = 0;
        if (options.Contains("compress") && options["compress"].ToString() == true.ToString())
        {
            flags = ResourceSaver.SaverFlags.Compress;
        }
        return ResourceSaver.Save($"{savePath}.{GetSaveExtension()}",resource, flags);
    }

    private static void LogDiagnostic(Diagnostic diagnostic)
    {
        var messagePrefix = string.IsNullOrEmpty(diagnostic.FileName) ? string.Empty : $"{diagnostic.FileName}: {diagnostic.Range.Start}:{diagnostic.Range.Start.Character} ";

        var message = messagePrefix + diagnostic.Message;

        switch (diagnostic.Severity)
        {
            case Diagnostic.DiagnosticSeverity.Error:
                GD.PrintErr(message);
                break;
            case Diagnostic.DiagnosticSeverity.Warning:
                GD.Print(message);
                break;
            case Diagnostic.DiagnosticSeverity.Info:
                GD.Print(message);
                break;
        }
    }

    public static CompilationResult CompileProgram(FileInfo[] inputs)
    {
        // The list of all files and their associated compiled results
        var results = new List<(FileInfo file, Program program, IDictionary<string, StringInfo> stringTable)>();

        var compilationJob = CompilationJob.CreateFromFiles(inputs.Select(fileInfo => fileInfo.FullName));

        CompilationResult compilationResult;

        try
        {
            compilationResult = Compiler.Compile(compilationJob);
        }
        catch (Exception e)
        {
            var errorBuilder = new StringBuilder();

            errorBuilder.AppendLine("Failed to compile because of the following error:");
            errorBuilder.AppendLine(e.ToString());

            GD.PrintErr(errorBuilder.ToString());
            throw new Exception();
        }

        return compilationResult;
    }

/*
    private Error _Import_Files(string name, Array<string> files, string savePath, Dictionary options)
    {
        string ysc = _Fetch_Ysc();
        if (ysc == null)
        {
            GD.PrintErr("could not find ysc executable, check the project settings");
            return Error.CompilationFailed;
        }

        Random random = new Random();
        string output_folder = String.Format("{0}/{1}/", OS.GetUserDataDir(), random.Next(0, 100000));
        DirAccess.MakeDirAbsolute(output_folder);

        string[] arguments = new string[] {
            "compile",
            "--output-directory",
            output_folder,
            "--output-name",
            name,  
            String.Join(' ', files)
        };

        Error _error = Error.Ok;
        Godot.Collections.Array _output = new Godot.Collections.Array();
        switch(OS.GetName()) {
            case "OSX": case "Windows": case "X11":
                _error = (Error)OS.Execute(
                    ysc,
                    arguments,
                    _output, 
                    true
                );
                break;
            default:
                return Error.CompilationFailed;
        }
	    if (_error != Error.Ok) {
            GD.PrintErr(_output[0]);
            return Error.CompilationFailed;
        }

        _Import_Compiled(output_folder, name, savePath, options);

        GD.Print("removing: " + output_folder);
        Error error = EmptyAndRemoveFolder(output_folder);
        if (error != Error.Ok) 
        {
            GD.PrintErr(String.Format("could not remove output folder: {0}", output_folder));
            return Error.CompilationFailed;
        }   
        return Error.Ok;
    }*/

    private Error EmptyAndRemoveFolder(string path)
    {
        Error error = Error.Ok; 
        foreach (var file in Directory.GetFiles(path))
        {
            var filename = Path.Combine(path, file);
            Directory.Delete(filename);
        }
        Directory.Delete(path, true);
        return error;  
    }
/*
    private Error _Import_Compiled(string folder, string name, string savePath, Dictionary options)
    {
        string yarnc = _Get_File_Content(Path.Join(folder, String.Format("{0}.{1}", name, "yarnc")));

        Dictionary<string, Dictionary<string, string>> stringTable = _ParseStringTable(folder, name);

        string metadata = _Get_File_Content(Path.Join(folder, String.Format("{0}-Metadata.{1}", name, "csv")));

        var resource = new CompiledYarnProject(
            yarnc,
            metadata,
            stringTable
        );
        ResourceSaver.SaverFlags flags = ResourceSaver.SaverFlags.None;
        if (options.ContainsKey("compress") && options["compress"].AsBool() == true)
        {
            flags = ResourceSaver.SaverFlags.Compress;
        }
        return ResourceSaver.Save(resource, String.Format("{0}.{1}", savePath, _GetSaveExtension()), flags);
    }


    private static Dictionary<string, Dictionary<string, string>> _ParseStringTable(string folder, string name)
    {
        Dictionary<string, Dictionary<string, string>> stringTable = new Dictionary<string, Dictionary<string, string>>();
        using (TextFieldParser parser = new TextFieldParser(Path.Join(folder, String.Format("{0}-Lines.{1}", name, "csv"))))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            var header = false;
            while (!parser.EndOfData)
            {
                string[] fields = parser.ReadFields();
                if (!header)
                {
                    header = true;
                    continue;
                }
                stringTable[fields[0]] = new Godot.Collections.Dictionary<string, string>()
                {
                    ["text"] = fields[1],
                    ["file"] = fields[2],
                    ["node"] = fields[3],
                    ["lineNumber"] = fields[4]
                };
            }
        }

        return stringTable;
    }

    private string _Get_File_Content(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return Convert.ToBase64String(bytes);
        
    }

    private string _Fetch_Ysc()
    {
        string ysc_setting = "yarn/ysc_path";
        string ysc = null;

        if (ProjectSettings.HasSetting(ysc_setting)) {
            ysc = ProjectSettings.GlobalizePath(ProjectSettings.GetSetting(ysc_setting).AsString());
        } else {
            GD.PrintErr("ysc setting not found!");
        }

        return ysc;
    }*/
}
