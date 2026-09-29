// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE ONE SPELLING OF ISO/IEC 1989:2023 §8.4.6.3 2)'s EXCEPTION for a COMMON program (kb/Work PB1460). The rule:
/// a program possessing the common attribute and directly contained in another program may be referenced by any
/// program directly or indirectly contained in that containing program, "except that the program possessing the
/// common attribute and any programs contained within it may reference the program-name only if the program
/// possesses the recursive attribute".
/// <para>Two scope implementations ask it and must agree: the compiler's AS NESTED callee table
/// (<c>BinderDriver.NestedCallablesOf</c>, which decides §14.9.4.3 SR15 at bind time) and the run-time resolver
/// (<see cref="ProgramTable"/>'s <c>ResolveVisible</c>). The bind-time table used to admit every COMMON child of every
/// ancestor unconditionally, so <c>CALL "SNQ" AS NESTED</c> from inside SNQ's own subtree compiled clean and then died
/// on EC-PROGRAM-NOT-FOUND, because only the resolver applied the exception. The containment walk is a parameter —
/// each side has its own tree — and the rule is written here once.</para>
/// </summary>
public static class ProgramNameScope
{
    /// <summary>May <paramref name="caller"/> reference the COMMON program <paramref name="common"/> — which the
    /// caller's containment chain already reaches through a container of <paramref name="common"/> — under §8.4.6.3
    /// 2)? True unless the caller IS the common program or is contained (directly or indirectly) within it, in which
    /// case only a RECURSIVE common program is referable.</summary>
    /// <param name="containerOf">The caller side's own containment edge: a program's directly containing program,
    /// or null for an outermost program.</param>
    public static bool CommonProgramReferable<T>(T common, bool commonIsRecursive, T caller, Func<T, T?> containerOf)
        where T : class
    {
        if (commonIsRecursive) return true;
        for (T? p = caller; p is not null; p = containerOf(p))
            if (ReferenceEquals(p, common)) return false;   // the common program itself, or a program within it
        return true;
    }
}
