using System.Collections.Generic;
using System.Text.Json.Serialization;
using Yarn.Compiler;

namespace YarnSpinnerGodot;

[JsonSerializable(typeof(Dictionary<string, StringTableEntry>))]
[JsonSerializable(typeof(Project))]
[JsonSerializable(typeof(DialogueRunner.SaveData))]
[JsonSerializable(typeof(FunctionInfo[]))]
[JsonSerializable(typeof(SerializedDeclaration[]))]
[JsonSerializable(typeof(LineMetadata))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(float))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default,
    IncludeFields = true
)]
public partial class YarnJSONContext : JsonSerializerContext;