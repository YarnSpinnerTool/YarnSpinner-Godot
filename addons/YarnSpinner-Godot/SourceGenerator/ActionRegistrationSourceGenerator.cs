/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using YarnSpinnerGodot;
using YarnAction = YarnSpinnerGodot.Action;
using System.Diagnostics;
#nullable enable

public static class GeneratorExecutionContextExtensions
{
    /// <summary>Gets the file path the source generator was called from.</summary>
    /// <param name="context">The context of the Generator's Execute method.</param>
    /// <returns>The file path the generator was called from.</returns>
    public static string? GetCallingPath(this GeneratorExecutionContext context)
    {
        return context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var result) ? result : null;
    }
}


[Generator]
public class ActionRegistrationSourceGenerator : ISourceGenerator
{
    const string YarnSpinnerCompilerAssemblyName = "YarnSpinner.Compiler";
    const string DebugLoggingPreprocessorSymbol = "YARN_SOURCE_GENERATION_DEBUG_LOGGING";
    const string DisableAllGenerationSymbol = "YARN_SOURCE_GENERATION_DISABLE_ALL";
    const string DisableYSLSGenerationSymbol = "YARN_SOURCE_GENERATION_DISABLE_YSLS";
    const string MinimumGodotVersionPreprocessorSymbol = "if GODOT4_0_OR_GREATER";

    public static string? GetProjectRoot(GeneratorExecutionContext context)
    {
        return context.GetCallingPath();
    }

    public void Execute(GeneratorExecutionContext context)
    {

        using var output = GetOutput(context);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var projectRoot = GetProjectRoot(context);
        output.WriteLine(DateTime.Now);
        if (context.ParseOptions.PreprocessorSymbolNames.Contains(DisableAllGenerationSymbol))
        {
            output.WriteLine($"All YarnSpinner source generation disabled by #define {DisableAllGenerationSymbol}.");
            return;
        }
        if (projectRoot == null)
        {
            output.WriteLine("Unable to locate caller's project directory. Can't output YSLS file.");
            return;
        }

        // we don't have plugin settings right now to disable the source generation 


        bool hasCriticalActionErrors = false;
        try
        {

            output.WriteLine("Source code generation for assembly " + context.Compilation.AssemblyName);

            if (context.AdditionalFiles.Any())
            {
                output.WriteLine($"Additional files:");
                foreach (var item in context.AdditionalFiles)
                {
                    output.WriteLine("  " + item.Path);
                }
            }

            output.WriteLine("Referenced assemblies for this compilation:");
            foreach (var referencedAssembly in context.Compilation.ReferencedAssemblyNames)
            {
                output.WriteLine(" - " + referencedAssembly.Name);
            }

            bool compilationReferencesYarnSpinner = context.Compilation.ReferencedAssemblyNames
                .Any(name => name.Name == YarnSpinnerCompilerAssemblyName);

            if (compilationReferencesYarnSpinner == false)
            {
                // This compilation doesn't reference YarnSpinner.Compiler. Any
                // code that we generate that references symbols in that
                // assembly won't work.
                output.WriteLine(
                    $"Assembly {context.Compilation.AssemblyName} doesn't reference {YarnSpinnerCompilerAssemblyName}. Not generating any code for it.");
                return;
            }

            output.WriteLine("Preprocessor Symbols: ");
            foreach (var symbol in context.ParseOptions.PreprocessorSymbolNames)
            {
                output.WriteLine("- " + symbol);
            }


            // Don't generate source code for certain Yarn Spinner provided
            // assemblies - these always manually register any actions in them.
            var prefixesToIgnore = new List<string>()
            {
                "YarnSpinnerGodot",
            };
            // note: godot - decrepit games : keeping this variable just for ease of comparison when porting
            // no assemblies to exclude right now. 
            // But DO generate source code for the Samples assembly and the Test assembly
            var prefixesToKeep = new List<string>()
            {

            };


            if (context.Compilation.AssemblyName == null)
            {
                output.WriteLine("Not generating registration code, because the provided AssemblyName is null");
                return;
            }

            if (prefixesToIgnore.Any(prefix => context.Compilation.AssemblyName.StartsWith(prefix)) &&
                !prefixesToKeep.Any(prefix => context.Compilation.AssemblyName.StartsWith(prefix)))
            {
                output.WriteLine(
                    $"Not generating registration code for {context.Compilation.AssemblyName}: we've been told to exclude it, because its name begins with one of these prefixes: {string.Join(", ", prefixesToIgnore)}");
                return;
            }

            if (!(context.Compilation is CSharpCompilation compilation))
            {
                // This is not a C# compilation, so we can't do analysis.
                output.WriteLine($"Stopping code generation because compilation is not a {nameof(CSharpCompilation)}.");
                return;
            }

            var actions = new List<YarnAction>();
            foreach (var tree in compilation.SyntaxTrees)
            {
                actions.AddRange(Analyser.GetActions(projectRoot!, compilation, tree, output));
            }

            if (actions.Count() == 0)
            {
                output.WriteLine(
                    $"Didn't find any Yarn Actions in {context.Compilation.AssemblyName}. Not generating any source code for it.");
                return;
            }

            // validating and logging all the actions
            foreach (var action in actions)
            {
                if (action == null)
                {
                    output.WriteLine($"Action is null??");
                    continue;
                }

                var diagnostics = action.Validate(compilation, output);
                foreach (var diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                    if (diagnostic.Severity == DiagnosticSeverity.Warning ||
                        diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        output.WriteLine($"Flagging '{action.Name}' ({action.MethodName}): {diagnostic}");
                        action.ContainsErrors = true;

                        if (diagnostic.Severity == DiagnosticSeverity.Error)
                        {
                            hasCriticalActionErrors = true;
                        }
                    }
                }

                // Commands are parsed as whitespace, so spaces in the command name
                // would render the command un-callable.
                if (action.Name.Any(x => Char.IsWhiteSpace(x)))
                {
                    var descriptor = new DiagnosticDescriptor(
                        "YS1002",
                        $"Yarn {action.Type} methods must have a valid name",
                        "YarnCommand and YarnFunction methods follow existing ID rules for Yarn. \"{0}\" is invalid.",
                        "Yarn Spinner",
                        DiagnosticSeverity.Warning,
                        true,
                        "[YarnCommand] and [YarnFunction] attributed methods must follow Yarn ID rules so that Yarn scripts can reference them.",
                        "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions");
                    context.ReportDiagnostic(Microsoft.CodeAnalysis.Diagnostic.Create(
                        descriptor,
                        action.Declaration?.GetLocation(),
                        action.Name
                    ));
                    action.ContainsErrors = true;
                    output.WriteLine(
                        $"Action {action.MethodIdentifierName} will be flagged due to it's name {action.Name}");
                    continue;
                }

                output.WriteLine(
                    $"Action {action.Name}: {action.SourceFileName}:{action.Declaration?.GetLocation()?.GetLineSpan().StartLinePosition.Line} ({action.Type})");
            }

            if (hasCriticalActionErrors)
            {
                stopwatch.Stop();
                output.WriteLine(
                    $"Critical issues were encountered in the actions, aborting code generation, stopping analysis after {stopwatch.Elapsed.TotalMilliseconds}ms");
                return;
            }

            output.Write($"Generating source code...");

            var source = Analyser.GenerateRegistrationFileSource(actions);

            output.WriteLine($"Done.");

            SourceText sourceText = SourceText.From(source, Encoding.UTF8);

            output.Write($"Writing generated source...");

            DumpGeneratedFile(context, source);

            output.WriteLine($"Done.");


            if (context.ParseOptions.PreprocessorSymbolNames.Contains(DisableYSLSGenerationSymbol))
            {
                output.WriteLine($"YSLS generation is disabled via {DisableYSLSGenerationSymbol}");
            }
            else
            {
                context.AddSource($"YarnActionRegistration-{compilation.AssemblyName}.Generated.cs", sourceText);

                output.Write($"Generating ysls...");
                // generating the ysls

            IEnumerable<string> commandJSON = actions.Where(a => a.Type == ActionType.Command).Select(a => a.ToJSON());
            IEnumerable<string> functionJSON =
                actions.Where(a => a.Type == ActionType.Function).Select(a => a.ToJSON());

            var ysls = "{" +
                       @"""version"":2," +
                       $@"""commands"":[{string.Join(",", commandJSON)}]," +
                       $@"""functions"":[{string.Join(",", functionJSON)}]" +
                       "}";

                output.WriteLine($"Done.");
                // todo how to find projectpath
                if (!string.IsNullOrEmpty("not empty todo"))
                {
                    output.Write($"Writing generated ysls...");
                    var fullPath = Path.Combine(projectRoot,
                        $"{context.Compilation.AssemblyName}.ysls.json");
                    try
                    {
                        System.IO.File.WriteAllText(fullPath, ysls);
                        output.WriteLine($"Done.");
                    }
                    catch (Exception e)
                    {
                        output.WriteLine($"Unable to write ysls to disk: {e.Message}");
                    }
                }
                else
                {
                    output.WriteLine("unable to identify project path, ysls will not be written to disk");
                }

            }

            stopwatch.Stop();
            output.WriteLine($"Source code generation completed in {stopwatch.Elapsed.TotalMilliseconds}ms");
            return;

        }

        catch (Exception e)
        {
            output.WriteLine($"{e}");
        }
    }

    private MethodDeclarationSyntax GenerateLoggingMethod(string methodName, string sourceExpression, string prefix)
    {
        return SyntaxFactory.MethodDeclaration(
    SyntaxFactory.PredefinedType(
        SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
    SyntaxFactory.Identifier(methodName))
    .WithModifiers(
    SyntaxFactory.TokenList(
        new[]{
            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
            SyntaxFactory.Token(SyntaxKind.StaticKeyword)}))
    .WithBody(
    SyntaxFactory.Block(
        SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("IEnumerable"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.PredefinedType(
                                SyntaxFactory.Token(SyntaxKind.StringKeyword))))))
            .WithVariables(
                SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier("source"))
                    .WithInitializer(
                        SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ParseExpression(sourceExpression)))))),
        SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.IdentifierName(
                    SyntaxFactory.Identifier(
                        SyntaxFactory.TriviaList(),
                        SyntaxKind.VarKeyword,
                        "var",
                        "var",
                        SyntaxFactory.TriviaList())))
            .WithVariables(
                SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier("prefix"))
                    .WithInitializer(
                        SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(prefix))))))),
        SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Debug"),
                    SyntaxFactory.IdentifierName("Log")
                )
            )
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                        SyntaxFactory.Argument(
                            SyntaxFactory.InterpolatedStringExpression(
                                SyntaxFactory.Token(SyntaxKind.InterpolatedVerbatimStringStartToken)
                            )
                            .WithContents(
                                SyntaxFactory.List<InterpolatedStringContentSyntax>(
                                    new InterpolatedStringContentSyntax[]{
                                        SyntaxFactory.Interpolation(
                                            SyntaxFactory.IdentifierName("prefix")
                                        ),
                                        SyntaxFactory.InterpolatedStringText()
                                        .WithTextToken(
                                            SyntaxFactory.Token(
                                                SyntaxFactory.TriviaList(),
                                                SyntaxKind.InterpolatedStringTextToken,
                                                " ",
                                                " ",
                                                SyntaxFactory.TriviaList()
                                            )
                                        ),
                                        SyntaxFactory.Interpolation(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.PredefinedType(
                                                        SyntaxFactory.Token(SyntaxKind.StringKeyword)
                                                    ),
                                                    SyntaxFactory.IdentifierName("Join")
                                                )
                                            )
                                            .WithArgumentList(
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                        new SyntaxNodeOrToken[]{
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.LiteralExpression(
                                                                    SyntaxKind.CharacterLiteralExpression,
                                                                    SyntaxFactory.Literal(';')
                                                                )
                                                            ),
                                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.IdentifierName("source")
                                                            )
                                                        }
                                                    )
                                                )
                                            )
                                        )
                                    }
                                )
                            )
                        )
                    )
                )
            )
        )
    )
    )
    .NormalizeWhitespace();
    }

    public static MethodDeclarationSyntax GenerateSingleLogMethod(string methodName, string text, string prefix)
    {
        return SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(
                SyntaxFactory.Token(SyntaxKind.VoidKeyword)
            ),
            SyntaxFactory.Identifier(methodName)
        )
        .WithModifiers(
            SyntaxFactory.TokenList(
                new[]{
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                }
            )
        )
        .WithBody(
            SyntaxFactory.Block(
                SyntaxFactory.SingletonList<StatementSyntax>(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("Debug"),
                                SyntaxFactory.IdentifierName("Log")
                            )
                        )
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.InterpolatedStringExpression(
                                            SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken)
                                        )
                                        .WithContents(
                                            SyntaxFactory.List<InterpolatedStringContentSyntax>(
                                                new InterpolatedStringContentSyntax[]{
                                                    SyntaxFactory.Interpolation(
                                                        SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.StringLiteralExpression,
                                                            SyntaxFactory.Literal(prefix)
                                                        )
                                                    ),
                                                    SyntaxFactory.InterpolatedStringText()
                                                    .WithTextToken(
                                                        SyntaxFactory.Token(
                                                            SyntaxFactory.TriviaList(),
                                                            SyntaxKind.InterpolatedStringTextToken,
                                                            " ",
                                                            " ",
                                                            SyntaxFactory.TriviaList()
                                                        )
                                                    ),
                                                    SyntaxFactory.Interpolation(
                                                        SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.StringLiteralExpression,
                                                            SyntaxFactory.Literal(text)
                                                        )
                                                    )
                                                }
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
            )
        )
        .NormalizeWhitespace();
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ClassDeclarationSyntaxReceiver());
    }

    static string GetTemporaryPath(GeneratorExecutionContext context)
    {
        string tempPath;
        var rootPath = GetProjectRoot(context);
        if (rootPath != null)
        {
            tempPath = Path.Combine(rootPath, ".godot", "dev.yarnspinner.unity");
        }
        else
        {
            tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dev.yarnspinner.logs");
        }

        // we need to make the logs folder, but this can potentially fail
        // if it does fail then we will just chuck the logs inside the tmp folder
        try
        {
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
        }
        catch
        {
            tempPath = System.IO.Path.GetTempPath();
        }
        return tempPath;
    }

    public ILogger GetOutput(GeneratorExecutionContext context)
    {
        if (GetShouldLogToFile(context))
        {
            var tempPath = ActionRegistrationSourceGenerator.GetTemporaryPath(context);

            var path = System.IO.Path.Combine(tempPath, $"{nameof(ActionRegistrationSourceGenerator)}-{context.Compilation.AssemblyName}.txt");
            var outFile = System.IO.File.Open(path, System.IO.FileMode.Create);

            return new FileLogger(new System.IO.StreamWriter(outFile));
        }
        else
        {
            return new NullLogger();
        }
    }

    private static bool GetShouldLogToFile(GeneratorExecutionContext context)
    {
        return context.ParseOptions.PreprocessorSymbolNames.Contains(DebugLoggingPreprocessorSymbol);
    }

    public void DumpGeneratedFile(GeneratorExecutionContext context, string text)
    {
        if (GetShouldLogToFile(context))
        {
            var tempPath = ActionRegistrationSourceGenerator.GetTemporaryPath(context);
            var path = System.IO.Path.Combine(tempPath, $"{nameof(ActionRegistrationSourceGenerator)}-{context.Compilation.AssemblyName}.cs.txt");
            System.IO.File.WriteAllText(path, text);
        }
    }
}

internal class ClassDeclarationSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> Classes { get; private set; } = new List<ClassDeclarationSyntax>();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        // Business logic to decide what we're interested in goes here
        if (syntaxNode is ClassDeclarationSyntax cds)
        {
            Classes.Add(cds);
        }
    }
}

