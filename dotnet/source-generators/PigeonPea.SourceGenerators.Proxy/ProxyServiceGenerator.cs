using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PigeonPea.SourceGenerators.Proxy;

/// <summary>
/// Incremental source generator that creates proxy service implementations
/// for partial classes marked with [RealizeService] attribute.
/// </summary>
[Generator]
public class ProxyServiceGenerator : IIncrementalGenerator
{
    private const string RealizeServiceAttributeName = "PigeonPea.Contracts.Plugin.Attributes.RealizeServiceAttribute";
    private const string SelectionStrategyAttributeName = "PigeonPea.Contracts.Plugin.Attributes.SelectionStrategyAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateClass(node),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private static bool IsCandidateClass(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
            return false;

        if (classDeclaration.AttributeLists.Count == 0)
            return false;

        if (!classDeclaration.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))
            return false;

        return true;
    }

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;
                if (symbol is IMethodSymbol attributeSymbol)
                {
                    var attributeClass = attributeSymbol.ContainingType;
                    var fullName = attributeClass.ToDisplayString();

                    if (fullName == RealizeServiceAttributeName)
                    {
                        return classDeclaration;
                    }
                }
            }
        }

        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "PROXY003",
                    "No proxy classes found",
                    "ProxyServiceGenerator did not find any classes marked with [RealizeService]",
                    "ProxyGenerator",
                    DiagnosticSeverity.Info,
                    isEnabledByDefault: true),
                Location.None));
            return;
        }

        var processedSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var classDeclaration in classes)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken) as INamedTypeSymbol;

            if (classSymbol is null)
                continue;

            if (!processedSymbols.Add(classSymbol))
                continue;

            ProcessClass(context, classSymbol, classDeclaration, compilation);
        }
    }

    private static void ProcessClass(
        SourceProductionContext context,
        INamedTypeSymbol classSymbol,
        ClassDeclarationSyntax classDeclaration,
        Compilation compilation)
    {
        var serviceType = GetServiceTypeFromAttribute(classSymbol);
        if (serviceType is null)
        {
            return;
        }

        if (serviceType.TypeKind != TypeKind.Interface)
        {
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "PROXY001",
                    "Service type must be an interface",
                    "The type '{0}' specified in [RealizeService] must be an interface",
                    "ProxyGenerator",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                classDeclaration.GetLocation(),
                serviceType.Name);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        var hasRegistryField = classSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(f => f.Name == "_registry" && f.Type.Name == "IRegistry");

        if (!hasRegistryField)
        {
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "PROXY002",
                    "Missing IRegistry field",
                    "The class '{0}' must have a field 'private readonly IRegistry _registry;'",
                    "ProxyGenerator",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                classDeclaration.GetLocation(),
                classSymbol.Name);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        var selectionMode = GetSelectionStrategy(classSymbol);

        var source = GenerateProxySource(classSymbol, serviceType, selectionMode);

        var hintName = $"{classSymbol.ContainingNamespace.ToDisplayString()}.{classSymbol.Name}.g.cs";
        context.AddSource(hintName, source);
    }

    private static ITypeSymbol? GetServiceTypeFromAttribute(INamedTypeSymbol classSymbol)
    {
        var realizeServiceAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == RealizeServiceAttributeName);

        if (realizeServiceAttribute is null)
            return null;

        if (realizeServiceAttribute.ConstructorArguments.Length > 0)
        {
            var typeArg = realizeServiceAttribute.ConstructorArguments[0];
            if (typeArg.Kind == TypedConstantKind.Type)
            {
                return typeArg.Value as ITypeSymbol;
            }
        }

        return null;
    }

    private static string? GetSelectionStrategy(INamedTypeSymbol classSymbol)
    {
        var selectionStrategyAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == SelectionStrategyAttributeName);

        if (selectionStrategyAttribute is null)
            return null;

        if (selectionStrategyAttribute.ConstructorArguments.Length > 0)
        {
            var modeArg = selectionStrategyAttribute.ConstructorArguments[0];
            if (modeArg.Kind == TypedConstantKind.Enum && modeArg.Type is INamedTypeSymbol enumType)
            {
                var enumValue = modeArg.Value;
                if (enumValue != null)
                {
                    foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
                    {
                        if (member.HasConstantValue && Equals(member.ConstantValue, enumValue))
                        {
                            return member.Name;
                        }
                    }
                }
            }
        }

        return null;
    }

    private static string GenerateProxySource(
        INamedTypeSymbol classSymbol,
        ITypeSymbol serviceType,
        string? selectionMode)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine($"// Generated by ProxyServiceGenerator at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");

        var alreadyImplementsInterface = classSymbol.Interfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, serviceType));
        
        if (alreadyImplementsInterface)
        {
            sb.AppendLine($"    partial class {classSymbol.Name}");
        }
        else
        {
            sb.AppendLine($"    partial class {classSymbol.Name} : {serviceType.ToDisplayString()}");
        }
        sb.AppendLine("    {");

        var methods = GetAllInterfaceMembers(serviceType)
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .Distinct(MethodSymbolComparer.Instance);

        foreach (var method in methods)
        {
            GenerateMethod(sb, method, serviceType, selectionMode);
        }

        var properties = GetAllInterfaceMembers(serviceType)
            .OfType<IPropertySymbol>()
            .Distinct(PropertySymbolComparer.Instance);

        foreach (var property in properties)
        {
            GenerateProperty(sb, property, serviceType, selectionMode);
        }

        var events = GetAllInterfaceMembers(serviceType)
            .OfType<IEventSymbol>()
            .Distinct(EventSymbolComparer.Instance);

        foreach (var evt in events)
        {
            GenerateEvent(sb, evt, serviceType, selectionMode);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static IEnumerable<ISymbol> GetAllInterfaceMembers(ITypeSymbol interfaceType)
    {
        var members = new List<ISymbol>(interfaceType.GetMembers());

        foreach (var baseInterface in interfaceType.AllInterfaces)
        {
            members.AddRange(baseInterface.GetMembers());
        }

        return members;
    }

    private static void GenerateMethod(
        StringBuilder sb,
        IMethodSymbol method,
        ITypeSymbol serviceType,
        string? selectionMode)
    {
        sb.AppendLine();

        var xmlDoc = method.GetDocumentationCommentXml();
        if (!string.IsNullOrWhiteSpace(xmlDoc))
        {
            GenerateXmlDocumentation(sb, xmlDoc, "        ");
        }

        sb.Append("        ");
        sb.Append("public ");

        sb.Append(method.ReturnType.ToDisplayString());
        sb.Append(" ");

        sb.Append(method.Name);

        if (method.IsGenericMethod)
        {
            sb.Append("<");
            var typeParams = method.TypeParameters;
            for (int i = 0; i < typeParams.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(typeParams[i].Name);
            }
            sb.Append(">");
        }

        sb.Append("(");
        var parameters = method.Parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
                sb.Append(", ");

            var param = parameters[i];

            if (param.RefKind == RefKind.Ref)
                sb.Append("ref ");
            else if (param.RefKind == RefKind.Out)
                sb.Append("out ");
            else if (param.RefKind == RefKind.In)
                sb.Append("in ");

            if (param.IsParams)
                sb.Append("params ");

            sb.Append(param.Type.ToDisplayString());
            sb.Append(" ");
            sb.Append(param.Name);

            if (param.HasExplicitDefaultValue)
            {
                sb.Append(" = ");
                if (param.ExplicitDefaultValue == null)
                {
                    if (param.Type.IsValueType)
                        sb.Append("default");
                    else
                        sb.Append("null");
                }
                else if (param.ExplicitDefaultValue is string strValue)
                {
                    sb.Append($"\"{strValue}\"");
                }
                else if (param.ExplicitDefaultValue is bool boolValue)
                {
                    sb.Append(boolValue ? "true" : "false");
                }
                else
                {
                    sb.Append(param.ExplicitDefaultValue.ToString());
                }
            }
        }
        sb.Append(")");

        if (method.IsGenericMethod)
        {
            foreach (var typeParam in method.TypeParameters)
            {
                var constraints = new List<string>();

                if (typeParam.HasReferenceTypeConstraint)
                    constraints.Add("class");
                if (typeParam.HasValueTypeConstraint)
                    constraints.Add("struct");
                if (typeParam.HasUnmanagedTypeConstraint)
                    constraints.Add("unmanaged");
                if (typeParam.HasNotNullConstraint)
                    constraints.Add("notnull");

                foreach (var constraintType in typeParam.ConstraintTypes)
                {
                    constraints.Add(constraintType.ToDisplayString());
                }

                if (typeParam.HasConstructorConstraint)
                    constraints.Add("new()");

                if (constraints.Count > 0)
                {
                    sb.AppendLine();
                    sb.Append($"            where {typeParam.Name} : {string.Join(", ", constraints)}");
                }
            }
        }

        sb.AppendLine();

        sb.AppendLine("        {");

        var serviceCall = selectionMode switch
        {
            "One" => $"_registry.Get<{serviceType.ToDisplayString()}>(PigeonPea.Contracts.Plugin.SelectionMode.One)",
            "HighestPriority" => $"_registry.Get<{serviceType.ToDisplayString()}>(PigeonPea.Contracts.Plugin.SelectionMode.HighestPriority)",
            "All" => $"_registry.GetAll<{serviceType.ToDisplayString()}>().First()",
            _ => $"_registry.Get<{serviceType.ToDisplayString()}>()"
        };

        var argsList = new List<string>();
        foreach (var param in parameters)
        {
            var argPrefix = param.RefKind switch
            {
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                _ => string.Empty
            };
            argsList.Add($"{argPrefix}{param.Name}");
        }
        var args = string.Join(", ", argsList);

        var methodCall = method.Name;
        if (method.IsGenericMethod)
        {
            methodCall += "<" + string.Join(", ", method.TypeParameters.Select(tp => tp.Name)) + ">";
        }

        if (method.ReturnsVoid)
        {
            sb.AppendLine($"            {serviceCall}.{methodCall}({args});");
        }
        else
        {
            sb.AppendLine($"            return {serviceCall}.{methodCall}({args});");
        }

        sb.AppendLine("        }");
    }

    private static void GenerateProperty(
        StringBuilder sb,
        IPropertySymbol property,
        ITypeSymbol serviceType,
        string? selectionMode)
    {
        sb.AppendLine();

        var xmlDoc = property.GetDocumentationCommentXml();
        if (!string.IsNullOrWhiteSpace(xmlDoc))
        {
            GenerateXmlDocumentation(sb, xmlDoc, "        ");
        }

        sb.Append("        ");
        sb.Append("public ");

        sb.Append(property.Type.ToDisplayString());
        sb.Append(" ");

        sb.AppendLine(property.Name);
        sb.AppendLine("        {");

        var serviceCall = selectionMode switch
        {
            "One" => $"_registry.Get<{serviceType.ToDisplayString()}>(PigeonPea.Contracts.Plugin.SelectionMode.One)",
            "HighestPriority" => $"_registry.Get<{serviceType.ToDisplayString()}>(PigeonPea.Contracts.Plugin.SelectionMode.HighestPriority)",
            "All" => $"_registry.GetAll<{serviceType.ToDisplayString()}>()",
            _ => $"_registry.Get<{serviceType.ToDisplayString()}>()"
        };

        if (property.GetMethod is not null)
        {
            sb.AppendLine($"            get => {serviceCall}.{property.Name};");
        }

        if (property.SetMethod is not null)
        {
            sb.AppendLine($"            set => {serviceCall}.{property.Name} = value;");
        }

        sb.AppendLine("        }");
    }

    private static void GenerateEvent(
        StringBuilder sb,
        IEventSymbol evt,
        ITypeSymbol serviceType,
        string? selectionMode)
    {
        sb.AppendLine();

        var xmlDoc = evt.GetDocumentationCommentXml();
        if (!string.IsNullOrWhiteSpace(xmlDoc))
        {
            GenerateXmlDocumentation(sb, xmlDoc, "        ");
        }

        sb.Append("        ");
        sb.Append("public event ");

        sb.Append(evt.Type.ToDisplayString());
        sb.Append(" ");

        sb.Append(evt.Name);
        sb.AppendLine();
        sb.AppendLine("        {");

        var serviceCall = selectionMode switch
        {
            "One" => $"_registry.Get<{serviceType.ToDisplayString()}>(PigeonPea.Contracts.Plugin.SelectionMode.One)",
            "HighestPriority" => $"_registry.Get<{serviceType.ToDisplayString()}>(PigeonPea.Contracts.Plugin.SelectionMode.HighestPriority)",
            "All" => $"_registry.GetAll<{serviceType.ToDisplayString()}>()",
            _ => $"_registry.Get<{serviceType.ToDisplayString()}>()"
        };

        sb.AppendLine($"            add => {serviceCall}.{evt.Name} += value;");
        sb.AppendLine($"            remove => {serviceCall}.{evt.Name} -= value;");

        sb.AppendLine("        }");
    }

    private static void GenerateXmlDocumentation(StringBuilder sb, string? xmlDoc, string indent)
    {
        if (string.IsNullOrWhiteSpace(xmlDoc))
            return;

        var lines = xmlDoc!.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.StartsWith("<?xml") || trimmed.StartsWith("<member"))
                continue;

            sb.Append(indent);
            sb.Append("/// ");
            sb.AppendLine(trimmed);
        }
    }

    private sealed class MethodSymbolComparer : IEqualityComparer<IMethodSymbol>
    {
        public static readonly MethodSymbolComparer Instance = new();

        public bool Equals(IMethodSymbol? x, IMethodSymbol? y) =>
            SymbolEqualityComparer.Default.Equals(x, y);

        public int GetHashCode(IMethodSymbol obj) =>
            SymbolEqualityComparer.Default.GetHashCode(obj);
    }

    private sealed class PropertySymbolComparer : IEqualityComparer<IPropertySymbol>
    {
        public static readonly PropertySymbolComparer Instance = new();

        public bool Equals(IPropertySymbol? x, IPropertySymbol? y) =>
            SymbolEqualityComparer.Default.Equals(x, y);

        public int GetHashCode(IPropertySymbol obj) =>
            SymbolEqualityComparer.Default.GetHashCode(obj);
    }

    private sealed class EventSymbolComparer : IEqualityComparer<IEventSymbol>
    {
        public static readonly EventSymbolComparer Instance = new();

        public bool Equals(IEventSymbol? x, IEventSymbol? y) =>
            SymbolEqualityComparer.Default.Equals(x, y);

        public int GetHashCode(IEventSymbol obj) =>
            SymbolEqualityComparer.Default.GetHashCode(obj);
    }
}
