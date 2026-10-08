// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The static facade over the run unit's <see cref="ModuleStack"/> (the emitted surface for FUNCTION
/// MODULE-NAME; kept name-stable pre-G8). Every member forwards to <c>RunUnit.Current.Modules</c>.
/// </summary>
public static class CobolModule
{
    /// <summary>The run unit's stack, resolved ONCE. <see cref="RunUnit.Current"/> is an
    /// <c>AsyncLocal</c> lookup, so a caller that pushes and pops (every method activation) pays the ambient
    /// resolution TWICE unless it holds the stack across the pair — measured at ~12 ns per activation, which is
    /// the ambient reads, not the list bookkeeping (fix-queue PB36).</summary>
    public static ModuleStack Stack => RunUnit.Current.Modules;

    /// <inheritdoc cref="ModuleStack.Push"/>
    public static void Push(string name, string outermost, bool isNested)
        => RunUnit.Current.Modules.Push(name, outermost, isNested);

    /// <inheritdoc cref="ModuleStack.Pop"/>
    public static void Pop() => RunUnit.Current.Modules.Pop();

    /// <inheritdoc cref="ModuleStack.Name"/>
    public static string Name(int kind) => RunUnit.Current.Modules.Name(kind);
}
