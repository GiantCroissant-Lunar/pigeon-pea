using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyTests;
using Plate.SCG.General.AutoProperties;
using Plate.General.AutoProperties.Attributes;

namespace Plate.General.AutoProperties.Tests;

public static class TestHelper
{
    public static Task Verify(string source)
    {
        // Ensure attributes assembly is loaded so it appears in AppDomain assemblies
        _ = typeof(AutoPropertyAttribute);

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>();
        
        // Add basic references
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references);

        var generator = new SourceGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var verifySettings = new VerifySettings();
        verifySettings.UseDirectory("snapshots");
        
        return Verifier.Verify(driver, verifySettings);
    }
}
