// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;

namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// ONE runtime element's <c>&gt;&gt;TURN</c> checking state AT ONE STATEMENT, in a form that can be asked about a
/// name not known until run time (ISO/IEC 1989:2023 §7.3.25.4 GR6/GR8 — a directive enables/disables checking
/// "for the procedure division statements … that follow in the compilation group", so the state is a property of
/// a (source element, line), and §14.6.13.1.1 — "if checking for an exception that occurs is not enabled, no
/// exception condition is raised").
///
/// <para><b>Why a runtime object at all, when <see cref="ExceptionState"/>'s other gates are compile-time.</b>
/// Every ordinary raise site knows its exception-name at COMPILE time, so the binder folds the TURN state there
/// and the emitter simply does not write the guard when checking is off. §14.9.18.4 GR1 b) asks the same question
/// about a name that is NOT known at compile time: a callee's <c>GOBACK … RAISING LAST EXCEPTION</c> decides the
/// name at run time, and the rule localises the raise in the ACTIVATING runtime element — "an exception condition
/// is raised in the activating runtime element if checking for that exception condition is enabled in the
/// activating runtime element". So the ACTIVATING statement has to carry its own per-condition checking state
/// into the run, and this is that carrier. (Before kb/Work PB408 the compiler asked the CALLEE's TURN state at
/// bind time instead, which is a different element's answer.)</para>
///
/// <para><b>The encoding is the directive stream, not the answer set.</b> A profile is the ordered
/// <c>(exception-name, on)</c> prefix of the element's TURN directives that governs the statement — exactly what
/// <c>TurnState.Fold</c> walks — so <see cref="Enabled"/> reproduces the compile-time fold for ANY name: last
/// matching event wins, with §7.3.25.4 GR2/GR3/GR4's hierarchy expansion supplied by
/// <see cref="ExceptionCatalog.DirectiveCovers"/>, the ONE place that rule is written. Encoding the ANSWER set
/// instead would mean naming every level-3 name the catalog holds at every activating statement.</para>
///
/// <para>FILE-SCOPED directives (<c>&gt;&gt;TURN EC-I-O … FILE F1</c>) are deliberately absent: GR6 scopes those
/// to statements referencing that file, and an activating statement references none — the compile-time query is
/// <c>Enabled(name, file: null, line)</c>, whose fold skips them, so the profile that feeds this must too.</para>
/// </summary>
public sealed class EcCheckingProfile
{
    /// <summary>No directive governs the site — every query is disabled (the §7.3.25.4 GR1 default).</summary>
    public static readonly EcCheckingProfile None = new("", []);

    private static readonly ConcurrentDictionary<string, EcCheckingProfile> Cache = new(StringComparer.Ordinal);

    private readonly (string Ec, bool On)[] _events;

    /// <summary>The wire form — see <see cref="Of"/>. Rendered as a C# string literal at the activating site,
    /// so a profile costs one interned literal and no allocation on the call path.</summary>
    public string Encoded { get; }

    private EcCheckingProfile(string encoded, (string Ec, bool On)[] events)
    {
        Encoded = encoded;
        _events = events;
    }

    /// <summary>Build a profile from its ordered events (the compiler side — <c>TurnState.ProfileAt</c>).</summary>
    public static EcCheckingProfile FromEvents(IEnumerable<(string Ec, bool On)> events)
    {
        var list = events.Select(e => (Ec: CobolNames.UpperFold(e.Ec), e.On)).ToArray();
        if (list.Length == 0) return None;
        return new EcCheckingProfile(
            string.Join(";", list.Select(e => (e.On ? "+" : "-") + e.Ec)), list);
    }

    /// <summary>Parse (once per distinct literal) the wire form emitted at an activating statement:
    /// <c>"+EC-USER;-EC-SIZE-OVERFLOW"</c> — source order, <c>+</c> = CHECKING ON, <c>-</c> = CHECKING OFF, the
    /// empty string = <see cref="None"/>. Cached because the literal is interned and the same site is reached
    /// many times.</summary>
    public static EcCheckingProfile Of(string? encoded)
    {
        if (string.IsNullOrEmpty(encoded)) return None;
        return Cache.GetOrAdd(encoded, static s =>
        {
            var parts = s.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var evs = new (string, bool)[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                evs[i] = (CobolNames.UpperFold(parts[i][1..]), parts[i][0] == '+');
            return new EcCheckingProfile(s, evs);
        });
    }

    /// <summary>Is checking for level-3 <paramref name="level3"/> enabled at the statement this profile
    /// describes? The LAST directive covering the name wins (§7.3.25.4 GR6/GR8 — enabled/disabled "until" the
    /// next toggle); no covering directive is the GR1 default, disabled.</summary>
    public bool Enabled(string? level3)
    {
        if (string.IsNullOrEmpty(level3)) return false;
        bool on = false;
        foreach (var (ec, evOn) in _events)
            if (ExceptionCatalog.DirectiveCovers(ec, level3)) on = evOn;
        return on;
    }

    /// <summary>True when no directive governs the site at all — the emitter's zero-scaffolding gate (an
    /// activating statement in an element with no TURN emits the same text it always did).</summary>
    public bool IsEmpty => _events.Length == 0;
}
