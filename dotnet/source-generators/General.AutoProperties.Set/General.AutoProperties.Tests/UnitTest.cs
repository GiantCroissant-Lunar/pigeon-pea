using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyXunit;
using Plate.SCG.General.AutoProperties;
using Xunit;

namespace Plate.General.AutoProperties.Tests;

[UsesVerify]
public class UnitTest
{
    [Fact]
    public Task CheckBasicPropertyGeneration()
    {
        var source = @"
using Plate.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty]
public partial class TestClass
{
    [GenerateProperty]
    private string _name;
    
    [GenerateProperty]
    private int _age;
}";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckCustomPropertyNames()
    {
        var source = @"
using Plate.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty]
public partial class TestClass
{
    [GenerateProperty(PropertyName = \"FullName\")]
    private string _name;
    
    [GenerateProperty(PropertyName = \"YearsOld\")]
    private int _age;
}";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckDifferentPropertyKinds()
    {
        var source = @"
using Plate.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty]
public partial class TestClass
{
    [GenerateProperty(PropertyKind = PropertyKind.GetterSetter)]
    private string _readWrite;
    
    [GenerateProperty(PropertyKind = PropertyKind.GetterOnly)]
    private string _readOnly;
    
    [GenerateProperty(PropertyKind = PropertyKind.GetterPrivateSetter)]
    private string _privateSetter;
}";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckDifferentAccessibilityLevels()
    {
        var source = @"
using Plate.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty]
public partial class TestClass
{
    [GenerateProperty(Accessibility = PropertyAccessibility.Public)]
    private string _publicField;
    
    [GenerateProperty(Accessibility = PropertyAccessibility.Internal)]
    private string _internalField;
    
    [GenerateProperty(Accessibility = PropertyAccessibility.Protected)]
    private string _protectedField;
}";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckGenerateForAllFields()
    {
        var source = @"
using Plate.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty(GenerateForAllFields = true)]
public partial class TestClass
{
    private string _name;
    private int _age;
    
    [SkipProperty(Reason = \"Internal use only\")]
    private string _internal;
}";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckCustomFieldPrefix()
    {
        var source = @"
using Plate.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty(FieldPrefix = \"m_\")]
public partial class TestClass
{
    [GenerateProperty]
    private string m_name;
    
    [GenerateProperty]
    private int m_age;
}";

        return TestHelper.Verify(source);
    }
}
