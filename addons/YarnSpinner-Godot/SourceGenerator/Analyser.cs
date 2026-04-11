/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#nullable enable

namespace YarnSpinnerGodot
{
    static class EnumerableExtensions
    {
        private struct Comparer<TItem, TKey> : IEqualityComparer<TItem>
        {
            Func<TItem, TKey> KeyFunc;
            public Comparer(Func<TItem, TKey> keyFunc) => this.KeyFunc = keyFunc;

            public readonly bool Equals(TItem x, TItem y)
            {
                var xKey = KeyFunc(x);
                var yKey = KeyFunc(y);
                return (xKey == null && yKey == null) || (xKey != null && xKey.Equals(yKey));
            }

            public readonly int GetHashCode(TItem obj) => KeyFunc(obj)?.GetHashCode() ?? 0;
        }
        public static IEnumerable<TItem> DistinctBy<TItem, TKey>(this IEnumerable<TItem> enumerable, Func<TItem, TKey> key) => Enumerable.Distinct(enumerable, new Comparer<TItem, TKey>(key));
    }

    public class Analyser
    {
        const string toolName = "YarnActionAnalyzer";
        const string toolVersion = "1.0.0.0";
        const string registrationMethodName = "RegisterActions";
        const string targetParameterName = "target";
        const string registrationTypeParameterName = "registrationType";
        const string initialisationMethodName = "AddRegisterFunction";

        /// <summary>
        /// The name of a scripting define symbol that, if set, indicates that
        /// Yarn actions specific to unit tests should be generated.
        /// </summary>
        public const string GenerateTestActionRegistrationSymbol = "YARN_GENERATE_TEST_ACTION_REGISTRATIONS";

        public Analyser(string sourcePath)
        {
            this.SourcePath = sourcePath;
        }

        public IEnumerable<string> SourceFiles => GetSourceFiles(SourcePath);
        public string SourcePath { get; set; }

        public IEnumerable<Action> GetActions(string projectRoot, IEnumerable<string>? assemblyPaths = null, ILogger? logger = null)
        {
            var trees = SourceFiles
                .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path))
                .ToList();

            var systemAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            if (string.IsNullOrEmpty(systemAssemblyPath))
            {
                throw new AnalyserException("Unable to find an assembly that defines System.Object");
            }

            static string GetLocationOfAssemblyWithType(string typeName)
            {
                return GetTypeByName(typeName)?.Assembly.Location ?? throw new AnalyserException($"Failed to find an assembly for type " + typeName);
            }
            
            var netstandard = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "netstandard");
            var systemCore = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "System.Core");

            var references = new List<MetadataReference> {
                MetadataReference.CreateFromFile(netstandard.Location),
                MetadataReference.CreateFromFile(systemCore.Location),
                MetadataReference.CreateFromFile(
                    typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(
                    GetLocationOfAssemblyWithType("YarnSpinnerGodot.DialogueRunner")),
                MetadataReference.CreateFromFile(
                    GetLocationOfAssemblyWithType("YarnSpinnerGodot.YarnCommandAttribute")),
                MetadataReference.CreateFromFile(
                    GetLocationOfAssemblyWithType("Godot.Node")),
            };

            if (assemblyPaths != null)
            {
                references.AddRange(assemblyPaths.Select(p => MetadataReference.CreateFromFile(p)));
            }

            var compilation = CSharpCompilation.Create("YarnActionAnalysis")
                .AddReferences(references)
                .AddSyntaxTrees(trees);

            var output = new List<Action>();

            var diagnostics = compilation.GetDiagnostics();

            try
            {
                foreach (var tree in trees)
                {
                    output.AddRange(GetActions(projectRoot!, compilation, tree, logger));
                }
            }
            catch (Exception e)
            {
                throw new AnalyserException(e.Message, e, diagnostics);
            }

            foreach (var action in output)
            {
                if (action.Validate(compilation, logger).Any(d => d.Severity == DiagnosticSeverity.Warning || d.Severity == DiagnosticSeverity.Error))
                {
                    action.ContainsErrors = true;
                }
            }

            return output;
        }

        public static string GenerateRegistrationFileSource(IEnumerable<Action> actions, string @namespace = "YarnSpinnerGodot.Generated", string className = "ActionRegistration")
        {
            var namespaceDecl = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(@namespace));

            var classDeclaration = SyntaxFactory.ClassDeclaration(className);
            classDeclaration = classDeclaration.WithModifiers(
                TokenList(
                    new[]{
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword)}
                ));
            classDeclaration = classDeclaration.AddAttributeLists(GeneratedCodeAttributeList);

            MethodDeclarationSyntax registrationMethod = GenerateRegistrationMethod(actions);
            ConstructorDeclarationSyntax initializationMethod = GenerateInitialisationMethod();

            classDeclaration = classDeclaration.AddMembers(
                initializationMethod,
                registrationMethod
            );


            namespaceDecl = namespaceDecl.AddMembers(classDeclaration);

            return namespaceDecl.NormalizeWhitespace().ToFullString();
        }

        private static ConstructorDeclarationSyntax GenerateInitialisationMethod()
        {
            return ConstructorDeclaration(
                    Identifier("ActionRegistration"))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.StaticKeyword)))
                .WithBody(
                    Block(
                        SingletonList<StatementSyntax>(
                            ExpressionStatement(
                                InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                AliasQualifiedName(
                                                    IdentifierName(
                                                        Token(SyntaxKind.GlobalKeyword)),
                                                    IdentifierName("YarnSpinnerGodot")),
                                                IdentifierName("Actions")),
                                            IdentifierName("AddRegistrationMethod")))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList<ArgumentSyntax>(
                                                Argument(
                                                    IdentifierName(registrationMethodName)))))))))
                .NormalizeWhitespace();
        }

        public static MethodDeclarationSyntax GenerateRegistrationMethod(IEnumerable<Action> actions)
        {
            var actionGroups = actions.GroupBy(a => a.SourceFileName);

            // Create registrations for all attribute registrations
            // ([YarnCommand], [YarnFunction]). These registration are always
            // invoked. Separately, create registrations for all runtime
            // registrations (AddCommandHandler() invocations). These are only
            // invoked when the registration methods 'registrationType'
            // parameter equals YarnSpinnerGodot.RegistrationType, which is true when
            // Yarn Spinner needs to list all commands and functions from
            // everywhere.
            //
            // We should only register runtime-registered commands when
            // explicitly requested, because otherwise if we register them here
            // AND during gameplay, they'll overlap and cause problems

            var attributeRegistrationStatements = actionGroups.SelectMany(group =>
            {
                var attributeRegistrations = group
                    .Where(a => a.DeclarationType != DeclarationType.DirectRegistration);
                return GetRegistrationStatements(attributeRegistrations);
            });

            var runtimeRegistrationStatements = actionGroups.SelectMany(group =>
            {
                var runtimeRegistrations = group
                    .Where(a => a.DeclarationType == DeclarationType.DirectRegistration);
                return GetRegistrationStatements(runtimeRegistrations);
            });

            var functionDeclarations = actionGroups.SelectMany(group =>
            {
                var functions = group
                    .Where(a => a.Type == ActionType.Function);
                return GetFunctionDeclarationStatements(functions);
            });

            // Create the method body that combines the attribute-registered and
            // (possibly) runtime-registered action registrations.
            var registrationMethodBody = SyntaxFactory.Block().WithStatements(SyntaxFactory.List<StatementSyntax>(
                attributeRegistrationStatements.Concat(functionDeclarations)
                ));

            // Create the list of attributes to attach to this method.
            var attributes = SyntaxFactory.List(new[] { GeneratedCodeAttributeList });

            var methodSyntax = SyntaxFactory.MethodDeclaration(
                attributes, // attribute list
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                ), // modifiers
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), // return type
                null, // explicit interface identifier
                SyntaxFactory.Identifier(registrationMethodName), // method name
                null, // type parameter list
                SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
                    new[] {
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(targetParameterName)).WithType(SyntaxFactory.ParseTypeName("global::YarnSpinnerGodot.IActionRegistration")),
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(registrationTypeParameterName)).WithType(SyntaxFactory.ParseTypeName("YarnSpinnerGodot.RegistrationType")),
                    }
                )), // parameters
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), // type parameter constraints
                registrationMethodBody, // body
                null, // arrow expression clause
                SyntaxFactory.Token(SyntaxKind.None) // semicolon token
                );

            return methodSyntax.NormalizeWhitespace();

            IEnumerable<StatementSyntax> GetRegistrationStatements(IEnumerable<Action> registerableCommands)
            {
                return registerableCommands
                    .Where(a => a.MethodSymbol?.MethodKind != MethodKind.AnonymousFunction)
                    .Select((a, i) =>
                    {
                        var registrationStatement = a.GetRegistrationSyntax(targetParameterName);
                        if (i == 0)
                        {
                            // Add a comment above the first registration that indicates where these actions came from
                            return registrationStatement.WithLeadingTrivia(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Comment($"// Actions from file:"),
                                    SyntaxFactory.Comment($"// {a.SourceFileName}")
                            ));
                        }
                        else
                        {
                            return registrationStatement;
                        }
                    }
                );
            }

            IEnumerable<StatementSyntax> GetFunctionDeclarationStatements(IEnumerable<Action> functions)
            {
                return functions
                    .Select((a, i) =>
                    {
                        var declarationStatement = a.GetFunctionDeclarationSyntax(targetParameterName);
                        if (i == 0)
                        {
                            // Add a comment above the first registration that indicates where these actions came from
                            return declarationStatement.WithLeadingTrivia(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Comment($"// Function declarations from file:"),
                                    SyntaxFactory.Comment($"// {a.SourceFileName}")
                            ));
                        }
                        else
                        {
                            return declarationStatement;
                        }
                    }
                );

            }
        }

        private static AttributeListSyntax GeneratedCodeAttributeList
        {
            get
            {
                // [System.CodeDom.Compiler.GeneratedCode(<toolName>, <toolVersion>)]

                var toolNameArgument = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(toolName)
                    )
                );

                var toolVersionArgument = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(toolVersion)
                    )
                );

                return SyntaxFactory.AttributeList(
                    SyntaxFactory.SeparatedList(
                        new[] {
                            SyntaxFactory.Attribute(
                                SyntaxFactory.ParseName("System.CodeDom.Compiler.GeneratedCode"),
                                SyntaxFactory.AttributeArgumentList(
                                    SyntaxFactory.SeparatedList(
                                        new[] {
                                            toolNameArgument,
                                            toolVersionArgument,
                                        }
                                    )
                                )
                            )
                        }
                    )
                );
            }
        }

        public static IEnumerable<Action> GetActions(string projectRoot, CSharpCompilation compilation, SyntaxTree tree, ILogger? yLogger = null)
        {
            var logger = yLogger;
            if (logger == null)
            {
                logger = new NullLogger();
            }

            var root = tree.GetCompilationUnitRoot();

            SemanticModel? model = null;

            if (compilation != null)
            {
                model = compilation.GetSemanticModel(tree, true);
            }

            if (model == null)
            {
                return Array.Empty<Action>();
            }

            return GetAttributeActions(projectRoot, root, model, logger).Concat(GetRuntimeDefinedActions(projectRoot, root, model, logger));
        }

        private static IEnumerable<Action> GetRuntimeDefinedActions(string projectRoot, CompilationUnitSyntax root, SemanticModel model, ILogger? logger)
        {

            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            classes = classes.Where(c =>
            {
                var classAttributes = GetAttributes(c, model);

                bool hasGeneratedCodeAttribute = classAttributes.Any(attr =>
                {
                    var syntax = attr.Item1;
                    var data = attr.Item2;

                    // Check to see if this attribute is the [GeneratedCode]
                    // attribute
                    return (data?.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)) == "global::System.CodeDom.Compiler.GeneratedCodeAttribute";
                });

                // Do not visit this class if it is generated code
                if (hasGeneratedCodeAttribute)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            });

            var methodInvocations = classes
                .SelectMany(classDecl => classDecl.DescendantNodes())
                .OfType<InvocationExpressionSyntax>()
                .Select(i =>
                {
                    // Get the symbol represening the method that's being called
                    var symbolInfo = model.GetSymbolInfo(i.Expression);

                    ISymbol? symbol = symbolInfo.Symbol;

                    if (symbol == null && symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
                    {
                        // We weren't able to determine what specific method
                        // this was. Pick the first one - we don't actually need
                        // to know about what specific overload was used, since
                        // they all have a similar signature.
                        symbol = symbolInfo.CandidateSymbols.First();
                    }
                    var methodSymbol = symbol as IMethodSymbol;
                    return (Syntax: i, Symbol: methodSymbol);
                })
                .Where(i => i.Symbol != null)
                .DistinctBy(i => i.Syntax)
                .ToList();

            var dialogueRunnerCalls = methodInvocations
                .Where(info => info.Symbol?.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::YarnSpinnerGodot.DialogueRunner").ToList();

            var addCommandCalls = methodInvocations.Where(
                info => info.Symbol?.Name == "AddCommandHandler"
            ).Select(c => (Call: c, Type: ActionType.Command));

            var addFunctionCalls = methodInvocations.Where(
                info => info.Symbol?.Name == "AddFunction"
            ).Select(c => (Call: c, Type: ActionType.Function));

            var methodCalls = addCommandCalls.Concat(addFunctionCalls);

            foreach (var methodCall in methodCalls)
            {
                var (Syntax, Symbol) = methodCall.Call;

                var methodNameSyntax = Syntax.ArgumentList.Arguments.ElementAtOrDefault(0);
                var targetSyntax = Syntax.ArgumentList.Arguments.ElementAtOrDefault(1);
                if (methodNameSyntax == null || targetSyntax == null)
                {
                    continue;
                }

                if (!(model.GetConstantValue(methodNameSyntax.Expression).Value is string name))
                {
                    // TODO: handle case of 'we couldn't figure out the constant value here'
                    continue;
                }

                SymbolInfo targetSymbolInfo = model.GetSymbolInfo(targetSyntax.Expression);
                IMethodSymbol? targetSymbol = targetSymbolInfo.Symbol as IMethodSymbol;
                if (targetSymbol == null && targetSymbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
                {
                    // We couldn't figure out exactly which of the targets to
                    // use. Choose one.
                    targetSymbol = targetSymbolInfo.CandidateSymbols.FirstOrDefault() as IMethodSymbol;
                }
                if (targetSymbol == null)
                {
                    // TODO: handle case of 'we couldn't figure out target method's
                    // symbol'
                    continue;
                }

                var declaringSyntax = targetSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

                string? ReturnDescription = null;
                if (TryGetDocumentation(targetSymbol, logger, out XElement? documentationXML, out string? summary))
                {
                    var returnNode = documentationXML?.Element("returns");
                    if (returnNode != null)
                    {
                        ReturnDescription = string.Join("", returnNode.DescendantNodes().OfType<XText>().Select(n => n.ToString())).Trim();
                        logger?.WriteLine($"\tFound a return: {ReturnDescription}");
                    }
                }

                string sourceFileName = root.SyntaxTree.FilePath;
                if (sourceFileName.StartsWith(projectRoot))
                {                  
                    logger?.WriteLine($"Adjusting {sourceFileName} to remove {projectRoot}");
                    sourceFileName = sourceFileName.Substring(projectRoot.Length);
                }
                else
                {
                    logger?.WriteLine(
                        $"{sourceFileName} does not start with {projectRoot}. The path may end up being absolute in the output.");
                }
                yield return new Action(name, methodCall.Type, targetSymbol)
                {
                    SemanticModel = model,
                    MethodName = targetSymbol.Name,
                    MethodDeclarationSyntax = declaringSyntax,
                    Declaration = declaringSyntax,
                    Description = summary,
                    Parameters = GetParams(targetSymbol, documentationXML, logger),
                    SourceFileName = sourceFileName,
                    DeclarationType = DeclarationType.DirectRegistration,
                    ReturnDescription = ReturnDescription,
                };
            }
        }

        private static bool TryGetDocumentation(IMethodSymbol targetSymbol, ILogger? logger, out XElement? documentationXML, out string? summary)
        {
            documentationXML = null;
            summary = null;

            var documentationComments = targetSymbol.GetDocumentationCommentXml();
            if (string.IsNullOrEmpty(documentationComments))
            {
                documentationComments = null;
                logger?.WriteLine($"Unable to find any xml documentation for {targetSymbol.Name}, attempting to load it syntactically instead.");

                foreach (var reference in targetSymbol.DeclaringSyntaxReferences)
                {
                    var method = reference.GetSyntax() as MethodDeclarationSyntax;
                    if (method != null)
                    {
                        var comment = GetActionTrivia(method, logger);
                        if (!string.IsNullOrEmpty(comment))
                        {
                            documentationComments = comment;
                            break;
                        }
                    }
                }
            }

            // at this point we still don't have a doc string
            // going to have to just give up
            if (documentationComments == null || string.IsNullOrWhiteSpace(documentationComments))
            {
                logger?.WriteLine($"Unable to find any xml documentation for {targetSymbol.Name}, syntactically either.");
                return false;
            }
            logger?.WriteLine($"Found a potential documentation candidate:\"{documentationComments}\"");

            // there are three different situations:
            // 1. This is a correctly structured docs string that has come from GetDocumentationCommentXml
            if (TryGetXMLFromDocumentString(documentationComments, out documentationXML, logger))
            {
                var summaryNode = documentationXML?.Element("summary");
                if (summaryNode != null)
                {
                    summary = string.Join("", summaryNode.DescendantNodes().OfType<XText>().Select(n => n.ToString())).Trim();
                    logger?.WriteLine("Found the GetDocumentationCommentXml comments and parsed it successfully");

                    return true;
                }
            }

            // 2. This is a syntactically determined string that happens to also be XML, but it will be missing the synthesised member root
            // so we add the missing root node on and try again
            if (TryGetXMLFromDocumentString($"<member name=\"M:{targetSymbol.ToString()}\">{documentationComments}</member>", out documentationXML, logger))
            {
                // so we wrap this node and try again
                var summaryNode = documentationXML?.Element("summary");
                if (summaryNode != null)
                {
                    summary = string.Join("", summaryNode.DescendantNodes().OfType<XText>().Select(n => n.ToString())).Trim();
                    logger?.WriteLine("Found the unrooted XML comments and parsed it successfully");

                    return true;
                }
            }

            // 3. This is not doc XML and just happens to be a comment above a command/function
            summary = documentationComments;
            documentationXML = null;
            logger?.WriteLine("Unable to determine XML, returning the comment as is");
            return true;
        }

        private static bool TryGetXMLFromDocumentString(string comment, out XElement? element, ILogger? logger)
        {
            try
            {
                element = XElement.Parse(comment);
                return true;
            }
            catch (System.Xml.XmlException ex)
            {
                logger?.WriteLine("Failed to parse comments as XML");
                logger?.WriteException(ex);
                element = null;
                return false;
            }
        }

        private static IEnumerable<Action> GetAttributeActions(string projectRoot, CompilationUnitSyntax root, SemanticModel model, ILogger logger)
        {
            var methodInfos = root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(decl => decl.Parent is ClassDeclarationSyntax);

            var methodsAndSymbols = methodInfos
                .Select(decl =>
                {
                    return (MethodDeclaration: decl, Symbol: model.GetDeclaredSymbol(decl));
                })
                .Where(pair => pair.Symbol != null);

            var actionMethods = methodsAndSymbols
                .Select(pair =>
                {
                    var actionType = GetActionType(model, pair.MethodDeclaration, out var attr);

                    return (pair.MethodDeclaration, pair.Symbol, ActionType: actionType, ActionAttribute: attr);
                })
                .Where(info => info.ActionType != ActionType.NotAnAction);

            foreach (var methodInfo in actionMethods)
            {
                var attr = methodInfo.ActionAttribute;

                if (attr == null)
                {
                    // Not a valid action; skip
                    continue;
                }

                // working on an assumption that most people just use the method name
                string actionName = methodInfo.MethodDeclaration.Identifier.ToString();

                // handling the situation where they have provided arguments
                // if we have an argument list
                if (attr.ArgumentList != null)
                {
                    // we resolve the value of first item in that list
                    // and if it's a string we use that as the action name
                    var constantValue = model.GetConstantValue(attr.ArgumentList.Arguments.First().Expression);
                    if (constantValue.HasValue)
                    {
                        if (constantValue.Value is string constantString)
                        {
                            logger?.WriteLine($"resolved constant expression value for the action name: {constantValue.Value.ToString()}");
                            actionName = constantString;
                        }
                        else
                        {
                            // Otherwise just logging the incorrect type and moving on with our life
                            logger?.WriteLine($"resolved constant expression value for the action name, but it is not a string, skipping: {constantValue!.Value}");
                        }
                    }
                }

                var position = methodInfo.MethodDeclaration.GetLocation();
                var lineIndex = position.GetLineSpan().StartLinePosition.Line + 1;

                var methodSymbol = methodInfo.Symbol;
                if (methodSymbol == null)
                {
                    logger?.WriteLine($"Failed to get a symbol for " + methodInfo.MethodDeclaration.Identifier);
                    continue;
                }

                if (!(methodSymbol.ContainingSymbol is ITypeSymbol container))
                {
                    logger?.WriteLine($"Failed to get a containing symbol for " + methodInfo.MethodDeclaration.Identifier);
                    continue;
                }

                string? ReturnDescription = null;
                if (TryGetDocumentation(methodSymbol, logger, out XElement? documentationXML, out string? summary))
                {
                    var returnNode = documentationXML?.Element("returns");
                    if (returnNode != null)
                    {
                        ReturnDescription = string.Join("", returnNode.DescendantNodes().OfType<XText>().Select(n => n.ToString())).Trim();
                        logger?.WriteLine($"\tFound a return: {ReturnDescription}");
                    }
                }

                var containerName = container?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "<unknown>" ;
                string sourceFileName = root.SyntaxTree.FilePath;
                if (projectRoot != null && sourceFileName.StartsWith(projectRoot!))
                {                  
                    logger?.WriteLine($"Adjusting {sourceFileName} to remove {projectRoot!}");
                    sourceFileName = sourceFileName.Substring(projectRoot!.Length);
                }
                else
                {
                    logger?.WriteLine(
                        $"{sourceFileName} does not start with {projectRoot}. The path may end up being absolute in the output.");
                }
                yield return new Action(actionName, methodInfo.ActionType, methodSymbol)
                {
                    Name = actionName,
                    Type = methodInfo.ActionType,
                    MethodName = $"{containerName}.{methodInfo.MethodDeclaration.Identifier}",
                    MethodIdentifierName = methodInfo.MethodDeclaration.Identifier.ToString(),
                    MethodSymbol = methodSymbol,
                    MethodDeclarationSyntax = methodInfo.MethodDeclaration,
                    IsStatic = methodSymbol.IsStatic,
                    Declaration = methodInfo.MethodDeclaration,
                    Parameters = GetParams(methodSymbol, documentationXML, logger),
                    AsyncType = GetAsyncType(methodSymbol),
                    SemanticModel = model,
                    Description = summary,
                    SourceFileName = sourceFileName,
                    DeclarationType = DeclarationType.Attribute,
                    ReturnDescription = ReturnDescription,
                };
            }
        }

        /// <summary>
        /// Returns a value indicating the async type for this action.
        /// </summary>
        /// <param name="symbol">The method symbol to test.</param>
        /// <returns></returns>
        private static AsyncType GetAsyncType(IMethodSymbol symbol)
        {
            var returnType = symbol.ReturnType;

            if (returnType.SpecialType == SpecialType.System_Void)
            {
                return AsyncType.Sync;
            }

            // If the method returns IEnumerator, it is a coroutine, and therefore async.
            if (returnType.SpecialType == SpecialType.System_Collections_IEnumerator)
            {
                return AsyncType.AsyncCoroutine;
            }
            

            // now checking for the various different awaiter types
            // later on it might be worth seeing if there is a good way to check if the return type is something that can be awaited
            // but we only have four types so it's probably fine this way
            switch (returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            {
                case "global::YarnSpinnerGodot.YarnTask":
                case "global::System.Threading.Tasks.Task":
                    return AsyncType.AsyncTask;
                default:
                    return default;
            };
        }

        private static List<Parameter> GetParams(IMethodSymbol symbol, XElement? documentationXML, ILogger? logger)
        {
            List<Parameter> parameters = new List<Parameter>();

            // if this is an instance command registered via the yarn attribute we need to do an extra step
            // we will need to create and add in a new parameter to the front of the parameters list
            // to represent the game object that we will do a lookup for
            var isAttributeRegistered = symbol.GetAttributes().Where(a => a.AttributeClass?.Name == "YarnCommandAttribute").Count() > 0;
            if (isAttributeRegistered && !symbol.IsStatic)
            {
                logger?.WriteLine("Command has been registered via the attribute, will be adding a target parameter.");
                var p = new Parameter
                {
                    Name = "target",
                    IsOptional = false,
                    Type = symbol.ContainingType,
                    Description = "The name of the Game Object the runner will search for to run this command upon. This will be done through a normal Godot.Node.FindChild call.",
                    IsParamsArray = false,
                };
                parameters.Insert(0, p);
            }

            if (symbol.Parameters.Count() > 0)
            {    
                logger?.WriteLine($"Processing {symbol.Name} parameters");
            }
            else
            {
                logger?.WriteLine($"{symbol.Name} has no parameters");
                return parameters;
            }

            var parameterDocumentation = new Dictionary<string, string>();

            if (documentationXML != null)
            {
                var parameterNodes = documentationXML.Elements("param");
                foreach (var parameterNode in parameterNodes)
                {
                    var name = parameterNode.Attribute("name");
                    if (name == null) { continue; }
                    var text = string.Join(
                        "",
                        parameterNode.DescendantNodes().OfType<XText>().Select(v => v.Value)
                    ).Trim();

                    if (!parameterDocumentation.ContainsKey(name.Value))
                    {
                        parameterDocumentation.Add(name.Value, text);
                    }
                }
            }

            foreach (var param in symbol.Parameters)
            {
                logger?.WriteLine($"\t{param.Name} is a {param.Type.ToDisplayString()}");

                List<AttributeData> attributes = new List<AttributeData>();
                foreach (var attribute in param.GetAttributes())
                {
                    if (attribute.AttributeClass?.BaseType?.Name == "YarnParameterAttribute")
                    {
                        logger?.WriteLine($"\t\tattribute: {attribute.AttributeClass?.Name}");
                        attributes.Add(attribute);
                    }
                }

                // ok here need to make some changes
                // if p is variadic it will be array<T> and I need to get just the T
                ITypeSymbol parameterType = param.Type;
                if (param.IsParams)
                {
                    if (param.Type is IArrayTypeSymbol arrayTypeSymbol)
                    {
                        parameterType = arrayTypeSymbol.ElementType;
                    }
                    else
                    {
                        logger?.WriteLine($"\t{param.Name} is a variadic parameter but isn't an array");
                    }
                }

                parameterDocumentation.TryGetValue(param.Name, out var paramDoc);
                var p = new Parameter
                {
                    Name = param.Name,
                    IsOptional = param.IsOptional,
                    Type = parameterType,
                    Description = paramDoc,
                    IsParamsArray = param.IsParams,
                    Attributes = attributes.Count() == 0 ? null : attributes.ToArray(),
                    DefaultValueString = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue?.ToString() : null,
                };
                parameters.Add(p);
            }

            return parameters;
        }

        internal static bool IsAttributeYarnCommand(AttributeData attribute)
        {
            return GetActionType(attribute) != ActionType.NotAnAction;
        }

        internal static ActionType GetActionType(SemanticModel model, MethodDeclarationSyntax decl, out AttributeSyntax? actionAttribute)
        {
            var attributes = GetAttributes(decl, model);

            var actionTypes = attributes
                .Select(attr => (Syntax: attr.Item1, Data: attr.Item2, Type: GetActionType(attr.Item2)))
                .Where(info => info.Type != ActionType.NotAnAction);

            if (actionTypes.Count() != 1)
            {
                // Not an action, because you can only have one YarnCommand or
                // YarnFunction attribute
                actionAttribute = null;
                return ActionType.Invalid;
            }

            var actionType = actionTypes.Single();
            actionAttribute = actionType.Syntax;
            return actionType.Type;
        }

        internal static ActionType GetActionType(AttributeData attr)
        {
            INamedTypeSymbol? attributeClass = attr.AttributeClass;

            switch (attributeClass?.Name)
            {
                case "YarnCommandAttribute": return ActionType.Command;
                case "YarnFunctionAttribute": return ActionType.Function;
                default: return ActionType.NotAnAction;
            }
        }

        public static IEnumerable<(AttributeSyntax, AttributeData)> GetAttributes(ClassDeclarationSyntax classDecl, SemanticModel model)
        {
            INamedTypeSymbol? classSymbol = model.GetDeclaredSymbol(classDecl);

            var methodAttributes = classSymbol?.GetAttributes();

            if (methodAttributes == null)
            {
                yield break;
            }

            foreach (var attribute in methodAttributes)
            {
                if (attribute.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax syntax)
                {
                    yield return (syntax, attribute);
                }
            }
        }

        public static IEnumerable<(AttributeSyntax, AttributeData)> GetAttributes(MethodDeclarationSyntax method, SemanticModel model)
        {
            IMethodSymbol? methodSymbol = model.GetDeclaredSymbol(method);

            var methodAttributes = methodSymbol?.GetAttributes();

            if (methodAttributes == null)
            {
                yield break;
            }

            foreach (var attribute in methodAttributes)
            {
                if (attribute.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax syntax)
                {
                    yield return (syntax, attribute);
                }
            }
        }

        internal static IEnumerable<string> GetSourceFiles(string sourcePath)
        {
            if (Directory.Exists(sourcePath))
            {
                return System.IO.Directory.EnumerateFiles(sourcePath, "*.cs", SearchOption.AllDirectories);
            }
            if (File.Exists(sourcePath))
            {
                return new[] { sourcePath };
            }
            throw new FileNotFoundException($"No file or directory at {sourcePath} was found.", sourcePath);
        }

        public static Type? GetTypeByName(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var tt = assembly.GetType(name);
                if (tt != null)
                {
                    return tt;
                }
            }

            return null;
        }

        // these are basically just ripped straight from the LSP
        // should maybe look at making these more accessible, for now the code dupe is fine IMO
        public static string? GetActionTrivia(MethodDeclarationSyntax method, ILogger? logger)
        {
            // The main string to use as the function's documentation.
            if (method.HasLeadingTrivia)
            {
                var trivias = method.GetLeadingTrivia();
                var structuredTrivia = trivias.LastOrDefault(t => t.HasStructure);
                if (structuredTrivia.Kind() != SyntaxKind.None)
                {
                    // The method contains structured trivia. Extract the
                    // documentation for it.
                    logger?.WriteLine($"trivia for {method.Identifier} is structured");
                    return GetDocumentationFromStructuredTrivia(structuredTrivia);
                }
                else
                {
                    // There isn't any structured trivia, but perhaps there's a
                    // comment above the method, which we can use as our
                    // documentation.
                    logger?.WriteLine($"trivia for {method.Identifier} is unstructured");
                    return GetDocumentationFromUnstructuredTrivia(trivias, logger);
                }
            }
            else
            {
                logger?.WriteLine($"{method.Identifier} has no trivia");
                return null;
            }
        }
        private static string GetDocumentationFromUnstructuredTrivia(SyntaxTriviaList trivias, ILogger? logger)
        {
            string documentation;
            bool emptyLineFlag = false;
            var documentationParts = Enumerable.Empty<string>();

            // loop in reverse order until hit something that doesn't look like it's related
            foreach (var trivia in trivias.Reverse())
            {
                var doneWithTrivia = false;
                switch (trivia.Kind())
                {
                    case SyntaxKind.EndOfLineTrivia:
                        // if we hit two lines in a row without a comment/attribute inbetween, we're done collecting trivia
                        if (emptyLineFlag == true)
                        {
                            logger?.WriteLine("have hit two empty lines in a row, done collecting unstructured trivia");
                            doneWithTrivia = true;
                        }
                        emptyLineFlag = true;
                        break;
                    case SyntaxKind.WhitespaceTrivia:
                        break;
                    case SyntaxKind.Attribute:
                        emptyLineFlag = false;
                        break;
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                        documentationParts = documentationParts.Prepend(trivia.ToString().Trim('/', ' '));
                        emptyLineFlag = false;
                        break;
                    default:
                        doneWithTrivia = true;
                        break;
                }

                if (doneWithTrivia)
                {
                    break;
                }
            }

            documentation = string.Join(" ", documentationParts);
            return documentation;
        }
        private static string? GetDocumentationFromStructuredTrivia(SyntaxTrivia structuredTrivia)
        {
            string documentation;
            var triviaStructure = structuredTrivia.GetStructure();
            if (triviaStructure == null)
            {
                return null;
            }

            string? ExtractStructuredTrivia(string tagName)
            {
                // Find the tag that matches the requested name.
                var triviaMatch = triviaStructure
                    .ChildNodes()
                    .OfType<XmlElementSyntax>()
                    .FirstOrDefault(x =>
                        x.StartTag.Name.ToString() == tagName
                    );

                if (triviaMatch != null
                    && triviaMatch.Kind() != SyntaxKind.None
                    && triviaMatch.Content.Any())
                {
                    // Get all content from this element that isn't a newline, and
                    // join it up into a single string.
                    var v = triviaMatch
                        .Content[0]
                        .ChildTokens()
                        .Where(ct => ct.Kind() != SyntaxKind.XmlTextLiteralNewLineToken)
                        .Select(ct => ct.ValueText.Trim());

                    return string.Join(" ", v).Trim();
                }

                return null;
            }

            var summary = ExtractStructuredTrivia("summary");
            var remarks = ExtractStructuredTrivia("remarks");

            documentation = summary ?? triviaStructure.ToString();

            if (remarks != null)
            {
                documentation += "\n\n" + remarks;
            }

            return documentation;
        }
    }
}
