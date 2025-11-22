using System;
using System.Collections.Generic;

namespace PigeonPea.Plugins.Profiling.Basic.Internal;

/// <summary>
/// Thread-local stack for tracking nested profiling scopes.
/// </summary>
internal class ScopeStack
{
    private readonly Stack<ScopeInfo> _stack = new();

    /// <summary>
    /// Information about an active profiling scope.
    /// </summary>
    public readonly struct ScopeInfo
    {
        public readonly string Name;
        public readonly string Category;
        public readonly long StartTicks;
        public readonly int Depth;

        public ScopeInfo(string name, string category, long startTicks, int depth)
        {
            Name = name;
            Category = category;
            StartTicks = startTicks;
            Depth = depth;
        }
    }

    /// <summary>
    /// Pushes a new scope onto the stack.
    /// </summary>
    public void Push(string name, string category, long startTicks)
    {
        _stack.Push(new ScopeInfo(name, category, startTicks, _stack.Count));
    }

    /// <summary>
    /// Pops a scope from the stack. Returns false if stack is empty.
    /// </summary>
    public bool TryPop(out ScopeInfo scope)
    {
        if (_stack.Count > 0)
        {
            scope = _stack.Pop();
            return true;
        }
        
        scope = default;
        return false;
    }

    /// <summary>
    /// Gets the current depth of the stack.
    /// </summary>
    public int Depth => _stack.Count;

    /// <summary>
    /// Clears the stack.
    /// </summary>
    public void Clear()
    {
        _stack.Clear();
    }

    /// <summary>
    /// Gets whether the stack is empty.
    /// </summary>
    public bool IsEmpty => _stack.Count == 0;
}
