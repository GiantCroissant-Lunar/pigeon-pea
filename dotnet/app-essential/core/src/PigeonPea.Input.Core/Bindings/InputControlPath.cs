using System.Text.RegularExpressions;

namespace PigeonPea.Input.Core.Bindings;

/// <summary>
/// Represents a path to a physical control (e.g., "<Keyboard>/w", "<Mouse>/leftButton").
/// Format: "<DeviceType>/controlName"
/// </summary>
public readonly struct InputControlPath : IEquatable<InputControlPath>
{
    private static readonly Regex PathRegex = new(
            @"^<(?<device>[A-Za-z0-9]+)>/(?<control>[A-Za-z0-9_\-]+)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Path { get; }
    public string DeviceType { get; }
    public string ControlName { get; }

    public InputControlPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Control path cannot be empty", nameof(path));

        Path = path;

        var match = PathRegex.Match(path);
        if (!match.Success)
        {
            throw new ArgumentException(
                    $"Invalid control path format: '{path}'. Expected format: '<DeviceType>/controlName' " +
                    $"where DeviceType contains only letters/numbers and controlName contains letters/numbers/underscore/hyphen.",
                    nameof(path));
        }

        DeviceType = match.Groups["device"].Value;
        ControlName = match.Groups["control"].Value;
    }

    public override string ToString() => Path;
    public override int GetHashCode() => Path.GetHashCode();
    public override bool Equals(object? obj) => obj is InputControlPath other && Equals(other);
    public bool Equals(InputControlPath other) => Path == other.Path;

    public static bool operator ==(InputControlPath left, InputControlPath right) => left.Equals(right);
    public static bool operator !=(InputControlPath left, InputControlPath right) => !left.Equals(right);

    public static implicit operator string(InputControlPath path) => path.Path;
    public static implicit operator InputControlPath(string path) => new(path);
}
