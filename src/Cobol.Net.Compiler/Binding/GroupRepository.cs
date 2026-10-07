// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Runtime;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE ONE SEARCH OF THE COMPILATION GROUP THAT A REPOSITORY SPECIFIER RESOLVES THROUGH (kb/Work PB989; ISO
/// §12.3.8.4 GR10 for a program-prototype-name, GR11 for a function-prototype-name — one sentence written twice,
/// which is why it is one function here, <see cref="Find"/>; DESIGN-external-repository §8.2, §8.3, slice 6).
/// <para>The rule, in full (<c>cite.py --check 12.3.8.4 "specified previously in the same compilation group"</c>):</para>
/// <list type="number">
/// <item>a) the externalized name of the prototype is the externalized name of a DEFINITION "specified PREVIOUSLY in
/// the same compilation group" — the details come from that definition;</item>
/// <item>b) otherwise, it is the externalized name of a PROTOTYPE definition "specified in the same compilation
/// group" (NOT "previously": §10.6.2 SR1 puts every prototype ahead of every other unit anyway) — the details come
/// from the prototype definition;</item>
/// <item>c) otherwise the details come from the external repository, which this implementation does not hold yet
/// (DESIGN-external-repository slice 7; kb/Work PB1086). A program then carries no compile-time signature and is
/// located in the run unit at activation; a function cannot be bound at all (COBOLNET1505), because its RETURNING
/// description is what a reference's result takes.</item>
/// </list>
/// <para><b>"Previously" is a POSITION, not a membership.</b> Every definition in the group used to be visible to every
/// source element, so a REPOSITORY entry naming a definition that FOLLOWS the element took a)'s details from it
/// (the order-blind tables this class replaced). The search now takes the position of the referencing element
/// (<see cref="BoundUnit.SourcePosition"/>, or a class's own start) and reads only the definitions that start strictly
/// before it. A prototype and its definition must have the same signature (§10.6.2 SR2/SR3, COBOLNET1513), so a) and
/// b) never disagree on the details; the observable difference is a definition WITHOUT a prototype that follows its
/// caller, which is c).</para>
/// <para><b>Matched by EXTERNALIZED name, never by word</b> (GR10 a) / GR11 a) say "the externalized name" three
/// times). The user-function table used to merge a prototype with a definition of the same WORD, which dropped the
/// prototype's <c>AS literal-1</c> (§11.5.4 GR2: literal-1 "is the name of the function prototype that is
/// externalized to the operating environment") whenever a definition happened to share its word. The WORD still has
/// one job, for functions only: when a), b) and c) all miss, the function the user-function-name NAMES (§8.4.6.7) —
/// determination D-R3, <see cref="ResolveFunction"/>.</para>
/// </summary>
internal sealed class GroupRepository
{
    private readonly List<BoundUnit> _functions = [];
    private readonly List<BoundUnit> _programs = [];

    private static readonly IReadOnlyDictionary<string, UserFunctionSignature> NoFunctions =
        new Dictionary<string, UserFunctionSignature>(CobolNames.Comparer);

    /// <summary>The group's FUNCTION-ID units (definitions and prototypes — §9.4 puts them in the function namespace)
    /// and its OUTERMOST PROGRAM-ID units (a contained program is part of its container's definition and is reachable
    /// only through §14.9.4.3 SR15's AS NESTED, which has its own table), in source order.</summary>
    public GroupRepository(IReadOnlyList<BoundUnit> units)
    {
        foreach (var u in units)
            if (u.IsFunction) _functions.Add(u);
            else if (u.Parent is null) _programs.Add(u);
    }

    /// <summary>⛔ GR10 / GR11 a) then b): the unit that supplies the details of a prototype, as seen by a source element
    /// that starts at <paramref name="position"/> — the first definition <paramref name="matches"/> that starts strictly
    /// before it, else the first prototype definition <paramref name="matches"/>, else null (c). The predicate is the
    /// externalized-name test for GR10 / GR11 and the user-function-name test for D-R3 step 2; the order is the
    /// rule's own.</summary>
    private static BoundUnit? Find(List<BoundUnit> units, Func<BoundUnit, bool> matches, int position)
    {
        BoundUnit? prototype = null;
        foreach (var u in units)
        {
            if (!matches(u)) continue;
            if (u.IsPrototype) prototype ??= u;
            else if (u.SourcePosition < position) return u;
        }

        return prototype;
    }

    private static Func<BoundUnit, bool> Externalized(string externalized) =>
        u => ExternalizedNames.Same(u.ExternalizedName, externalized);

    private static Func<BoundUnit, bool> Word(string word) => u => CobolNames.Same(u.Name, word);

    /// <summary>GR10's answer for a program-prototype whose externalized name is <paramref name="externalized"/>: the
    /// calling details a source element at <paramref name="position"/> takes from the group, or null for c) — the
    /// external repository, i.e. this implementation's run-unit program registry, resolved at activation.</summary>
    public CalleeSignature? ProgramDetails(string externalized, int position) =>
        Find(_programs, Externalized(externalized), position) is { } u
            ? new CalleeSignature(u.Data.LinkageFormals, u.Data.LinkageReturning)
            : null;

    /// <summary>⛔ GR11 with determination D-R3 (DESIGN-external-repository §8.3; owner decision 2026-10-05) for a
    /// REPOSITORY entry <c>FUNCTION <paramref name="word"/> [AS literal-5]</c> — <paramref name="externalized"/> is
    /// literal-5, or the word itself when no AS is written (GR11 NOTE 2) — as seen by a source element at
    /// <paramref name="position"/>:
    /// <list type="number">
    /// <item>GR11 a) / b) keyed by externalized name, <see cref="Find"/> — whichever hits names the function
    /// (GnuCOBOL 3.2.0's answer and the standard's text);</item>
    /// <item>when it misses, the function the WORD names (§8.4.6.7; SR10's "the name of a function definition specified
    /// previously" and "the name of a function prototype specified in this compilation group"): the definition
    /// specified before the element whose user-function-name is the word, else the prototype of that name — its
    /// externalized name is what is activated;</item>
    /// <item>when step 1 hit and a DIFFERENT function — another externalized name — carries the word and is visible to
    /// the element, <see cref="FunctionResolution.Shadowed"/> names it so the caller can warn.</item>
    /// </list>
    /// Null: nothing in the group answers (c). The signature carries the entry's own word, which is how the element
    /// refers to the function, and the activated function's externalized name.</summary>
    public FunctionResolution? ResolveFunction(string word, string externalized, int position)
    {
        var byName = Find(_functions, Externalized(externalized), position);
        var details = byName ?? Find(_functions, Word(word), position);
        if (details is null) return null;
        // Step 3: another function — another externalized name — that the element can see under the same word; "can
        // see" is the one rule Find states, so the warning never names a definition the search itself ignores.
        var shadowed = byName is null
            ? null
            : Find(_functions, u => CobolNames.Same(u.Name, word)
                                    && !ExternalizedNames.Same(u.ExternalizedName, details.ExternalizedName), position);
        return new FunctionResolution(SignatureOf(word, details), shadowed);
    }

    /// <summary>The user-defined functions a source element at <paramref name="position"/> can see by WORD, without a
    /// REPOSITORY entry resolving them: each word's definition specified before the element, else its prototype (D-R3
    /// step 2's search, unspecified). The entries of the element's own specifiers replace these
    /// (<c>BinderDriver.UserFunctionsOf</c>); the rest is what lets a reference to a function the element did not
    /// declare say which function it could have meant.</summary>
    public IReadOnlyDictionary<string, UserFunctionSignature> FunctionsAt(int position)
    {
        if (_functions.Count == 0) return NoFunctions;
        var table = new Dictionary<string, UserFunctionSignature>(CobolNames.Comparer);
        foreach (var u in _functions)
            if (!table.ContainsKey(u.Name) && Find(_functions, Word(u.Name), position) is { } details)
                table[u.Name] = SignatureOf(u.Name, details);
        return table;
    }

    private static UserFunctionSignature SignatureOf(string name, BoundUnit details) =>
        new(name, details.ExternalizedName, details.Data.LinkageReturning, details.Data.LinkageFormals);
}

/// <summary>What <see cref="GroupRepository.ResolveFunction"/> found: the <paramref name="Signature"/> the entry's word
/// resolves to, and — D-R3 step 3 — the <paramref name="Shadowed"/> function that carries the same word under another
/// externalized name, or null.</summary>
internal sealed record FunctionResolution(UserFunctionSignature Signature, BoundUnit? Shadowed);
