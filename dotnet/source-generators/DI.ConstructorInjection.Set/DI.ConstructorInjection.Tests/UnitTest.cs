// Copyright (c) GiantCroissant. All rights reserved.

namespace Yokan.SCG.DI.ConstructorInjection.Tests;

public class UnitTest : VerifyBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UnitTest(ITestOutputHelper testOutputHelper) : base()
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public Task CheckConstructorInjection()
    {
        // The source code to test
        const string code =
"""
using System;
using System.Collections.Generic;

namespace Yokan.Game.Fake;

using Yokan.SCG.DI.ConstructorInjection.Attributes;

[ConstructorInjection]
public partial class SomeType01
{
    [ResolveInConstructor]
    private readonly string _name;

    [ResolveInConstructor]
    private readonly int _age;

    [ResolveInConstructor]
    private readonly List<int> _values;
}
""";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(
            code,
            _testOutputHelper);
    }
}
