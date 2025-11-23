using System.Collections.Generic;
using FluentAssertions;
using PigeonPea.Goap.Actions;
using PigeonPea.Goap.Goals;
using PigeonPea.Goap.Planning;
using PigeonPea.Goap.WorldState;
using Xunit;

namespace PigeonPea.Goap.Core.Tests.Planning;

public class PlannerTests
{
    [Fact]
    public void CreatePlan_WithSimpleGoal_FindsPlan()
    {
        var currentState = new WorldState()
            .Set("HasWeapon", false)
            .Set("PlayerVisible", true);

        var goal = new GoapGoal
        {
            Name = "KillPlayer",
            DesiredState = new WorldState()
                .Set("PlayerDead", true)
        };

        var pickupWeapon = new GoapAction
        {
            Name = "PickupWeapon",
            Cost = 1f,
            Preconditions = new List<Precondition>(),
            Effects = new List<Effect>
            {
                new("HasWeapon", true)
            }
        };

        var attackPlayer = new GoapAction
        {
            Name = "AttackPlayer",
            Cost = 1f,
            Preconditions = new List<Precondition>
            {
                new("HasWeapon", true),
                new("PlayerVisible", true)
            },
            Effects = new List<Effect>
            {
                new("PlayerDead", true)
            }
        };

        var actions = new List<GoapAction> { pickupWeapon, attackPlayer };
        var planner = new Planner();

        var result = planner.CreatePlan(currentState, goal, actions);

        result.Success.Should().BeTrue();
        result.Plan.Should().NotBeNull();
        result.Plan!.Actions.Should().HaveCount(2);
        result.Plan.Actions[0].Name.Should().Be("PickupWeapon");
        result.Plan.Actions[1].Name.Should().Be("AttackPlayer");
    }

    [Fact]
    public void CreatePlan_WithAlreadySatisfiedGoal_ReturnsEmptyPlan()
    {
        var currentState = new WorldState()
            .Set("PlayerDead", true);

        var goal = new GoapGoal
        {
            Name = "KillPlayer",
            DesiredState = new WorldState()
                .Set("PlayerDead", true)
        };

        var planner = new Planner();

        var result = planner.CreatePlan(currentState, goal, new List<GoapAction>());

        result.Success.Should().BeTrue();
        result.Plan.Should().NotBeNull();
        result.Plan!.IsEmpty.Should().BeTrue();
        result.Plan.TotalCost.Should().Be(0);
    }

    [Fact]
    public void CreatePlan_WithImpossibleGoal_Fails()
    {
        var currentState = new WorldState()
            .Set("HasWeapon", false);

        var goal = new GoapGoal
        {
            Name = "KillPlayer",
            DesiredState = new WorldState()
                .Set("PlayerDead", true)
        };

        var planner = new Planner();

        var result = planner.CreatePlan(currentState, goal, new List<GoapAction>());

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreatePlan_ChoosesCheaperPath()
    {
        var currentState = new WorldState()
            .Set("HasWeapon", false);

        var goal = new GoapGoal
        {
            Name = "KillPlayer",
            DesiredState = new WorldState()
                .Set("PlayerDead", true)
        };

        var pickupSword = new GoapAction
        {
            Name = "PickupSword",
            Cost = 1f,
            Effects = new List<Effect> { new("HasWeapon", true) }
        };

        var pickupBow = new GoapAction
        {
            Name = "PickupBow",
            Cost = 5f,
            Effects = new List<Effect> { new("HasWeapon", true) }
        };

        var attack = new GoapAction
        {
            Name = "Attack",
            Cost = 1f,
            Preconditions = new List<Precondition> { new("HasWeapon", true) },
            Effects = new List<Effect> { new("PlayerDead", true) }
        };

        var actions = new List<GoapAction> { pickupSword, pickupBow, attack };
        var planner = new Planner();

        var result = planner.CreatePlan(currentState, goal, actions);

        result.Success.Should().BeTrue();
        result.Plan.Should().NotBeNull();
        result.Plan!.Actions[0].Name.Should().Be("PickupSword");
        result.Plan.TotalCost.Should().Be(2f);
    }
}
