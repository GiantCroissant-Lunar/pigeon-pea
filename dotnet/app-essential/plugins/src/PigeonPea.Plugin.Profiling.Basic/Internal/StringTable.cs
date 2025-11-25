using System.Collections.Concurrent;

namespace PigeonPea.Plugin.Profiling.Basic.Internal;

/// <summary>
/// Thread-safe string interning table to avoid string allocations.
/// </summary>
internal class StringTable
{
    private readonly ConcurrentDictionary<string, int> _stringToIndex = new();
    private readonly ConcurrentDictionary<int, string> _indexToString = new();
    private volatile int _nextIndex = 0;

    /// <summary>
    /// Gets the index for a string, creating a new entry if necessary.
    /// </summary>
    public int GetOrCreateIndex(string value)
    {
        return _stringToIndex.GetOrAdd(value, _ =>
        {
            var index = Interlocked.Increment(ref _nextIndex) - 1;
            _indexToString[index] = value;
            return index;
        });
    }

    /// <summary>
    /// Gets the string for an index, returns empty string if not found.
    /// </summary>
    public string GetString(int index)
    {
        return _indexToString.TryGetValue(index, out var value) ? value : string.Empty;
    }

    /// <summary>
    /// Gets all strings in the table, ordered by index.
    /// </summary>
    public string[] GetAllStrings()
    {
        var strings = new string[_nextIndex];
        for (int i = 0; i < _nextIndex; i++)
        {
            strings[i] = GetString(i);
        }
        return strings;
    }

    /// <summary>
    /// Clears the string table.
    /// </summary>
    public void Clear()
    {
        _stringToIndex.Clear();
        _indexToString.Clear();
        Interlocked.Exchange(ref _nextIndex, 0);
    }
}
