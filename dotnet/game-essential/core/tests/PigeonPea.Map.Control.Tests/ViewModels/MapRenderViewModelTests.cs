using Xunit;
using PigeonPea.Map.Control.ViewModels;
using PigeonPea.Map.Rendering;

namespace PigeonPea.Map.Control.Tests.ViewModels;

public class MapRenderViewModelTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var vm = new MapRenderViewModel();

        Assert.Equal(0, vm.CenterX);
        Assert.Equal(0, vm.CenterY);
        Assert.Equal(1.0, vm.Zoom);
        Assert.Equal(80, vm.ViewportCols);
        Assert.Equal(24, vm.ViewportRows);
        Assert.Equal(ColorScheme.Original, vm.ColorScheme);
    }

    [Fact]
    public void ColorScheme_CanBeChanged()
    {
        var vm = new MapRenderViewModel();
        vm.ColorScheme = ColorScheme.Fantasy;
        Assert.Equal(ColorScheme.Fantasy, vm.ColorScheme);
    }

    [Fact]
    public void ColorScheme_RaisesPropertyChanged()
    {
        var vm = new MapRenderViewModel();
        bool raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MapRenderViewModel.ColorScheme))
                raised = true;
        };

        vm.ColorScheme = ColorScheme.Realistic;

        Assert.True(raised, "PropertyChanged should be raised for ColorScheme");
    }

    [Fact]
    public void AvailableColorSchemes_ContainsAllSchemes()
    {
        var vm = new MapRenderViewModel();
        var schemes = vm.AvailableColorSchemes.ToList();

        Assert.Equal(6, schemes.Count);
        Assert.Contains(ColorScheme.Original, schemes);
        Assert.Contains(ColorScheme.Realistic, schemes);
        Assert.Contains(ColorScheme.Fantasy, schemes);
        Assert.Contains(ColorScheme.HighContrast, schemes);
        Assert.Contains(ColorScheme.Monochrome, schemes);
        Assert.Contains(ColorScheme.Parchment, schemes);
    }

    [Theory]
    [InlineData(ColorScheme.Original)]
    [InlineData(ColorScheme.Realistic)]
    [InlineData(ColorScheme.Fantasy)]
    [InlineData(ColorScheme.HighContrast)]
    [InlineData(ColorScheme.Monochrome)]
    [InlineData(ColorScheme.Parchment)]
    public void ColorScheme_CanBeSetToAnyValidValue(ColorScheme scheme)
    {
        var vm = new MapRenderViewModel();
        vm.ColorScheme = scheme;
        Assert.Equal(scheme, vm.ColorScheme);
    }

    [Fact]
    public void CenterX_RaisesPropertyChanged()
    {
        var vm = new MapRenderViewModel();
        bool raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MapRenderViewModel.CenterX))
                raised = true;
        };

        vm.CenterX = 100.0;

        Assert.True(raised, "PropertyChanged should be raised for CenterX");
        Assert.Equal(100.0, vm.CenterX);
    }

    [Fact]
    public void CenterY_RaisesPropertyChanged()
    {
        var vm = new MapRenderViewModel();
        bool raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MapRenderViewModel.CenterY))
                raised = true;
        };

        vm.CenterY = 200.0;

        Assert.True(raised, "PropertyChanged should be raised for CenterY");
        Assert.Equal(200.0, vm.CenterY);
    }

    [Fact]
    public void Zoom_RaisesPropertyChanged()
    {
        var vm = new MapRenderViewModel();
        bool raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MapRenderViewModel.Zoom))
                raised = true;
        };

        vm.Zoom = 2.5;

        Assert.True(raised, "PropertyChanged should be raised for Zoom");
        Assert.Equal(2.5, vm.Zoom);
    }

    [Fact]
    public void ViewportCols_RaisesPropertyChanged()
    {
        var vm = new MapRenderViewModel();
        bool raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MapRenderViewModel.ViewportCols))
                raised = true;
        };

        vm.ViewportCols = 120;

        Assert.True(raised, "PropertyChanged should be raised for ViewportCols");
        Assert.Equal(120, vm.ViewportCols);
    }

    [Fact]
    public void ViewportRows_RaisesPropertyChanged()
    {
        var vm = new MapRenderViewModel();
        bool raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MapRenderViewModel.ViewportRows))
                raised = true;
        };

        vm.ViewportRows = 40;

        Assert.True(raised, "PropertyChanged should be raised for ViewportRows");
        Assert.Equal(40, vm.ViewportRows);
    }
}
