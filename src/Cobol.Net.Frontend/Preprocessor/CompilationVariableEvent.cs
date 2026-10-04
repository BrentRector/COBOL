// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Expressions;
using CobolNet.Runtime;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// ONE change to the compilation-variable table that a <c>&gt;&gt;DEFINE</c> directive made (ISO §7.3.11; kb/Work
/// PB1368): on resultant line <paramref name="Line"/>, <paramref name="Name"/> came to reference
/// <paramref name="Value"/> — or, when <paramref name="Value"/> is null, ceased to be defined (§7.3.11.4 GR2, the OFF
/// phrase; GR4, a PARAMETER the operating environment supplies no value for).
/// </summary>
/// <param name="Line">The 1-based RESULTANT line of the DEFINE directive — the parser tokens' frame.</param>
/// <param name="Name">compilation-variable-name-1 as written.</param>
/// <param name="Value">The value the name references after the directive, or null when it is not defined.</param>
/// <param name="Written">The DEFINE operand as written (empty for OFF and PARAMETER) — the "text represented by"
/// the name, which a directive literal slot screens with its own literal rules (§7.3.10.3 SR2's "shall not be
/// specified in hexadecimal-alphanumeric format" is a rule about the WRITTEN literal, not its value).</param>
public sealed record CompilationVariableEvent(int Line, string Name, CtValue? Value, string Written);

/// <summary>
/// THE question every non-conditional-compilation use of a compilation variable asks of the table's
/// <see cref="DirectiveTimeline{T}"/> (ISO §7.3.11.4 GR1: "In text that follows a DEFINE directive specifying
/// compilation-variable-name-1 without the OFF phrase, compilation-variable-name-1 may be used … in any compiler
/// directive where a literal of the category associated with the name is permitted, … or in a constant entry where
/// the FROM phrase is specified"): which definition, if any, is in force at a given line. The table is a TIMELINE,
/// never a final map, because GR1 scopes a definition to the text that FOLLOWS it — a constant entry written between
/// <c>&gt;&gt;DEFINE X AS 1</c> and <c>&gt;&gt;DEFINE X AS 2 OVERRIDE</c> reads 1.
/// </summary>
public static class CompilationVariableTimeline
{
    /// <summary>The definition of <paramref name="name"/> in force at a construct on <paramref name="siteLine"/>: the
    /// most recent DEFINE of that name written before the site and not revoked by a <c>&gt;&gt;POP</c> (§7.3.20.4
    /// GR1/GR3 restore the whole table, §7.3.22.4 GR3), or null when that DEFINE specified OFF, supplied no PARAMETER
    /// value, or no DEFINE of the name precedes the site — the name's defined condition is then false (§7.3.11.4
    /// GR2). The match ignores case: "COBOL basic letters appearing elsewhere within the compilation group are treated
    /// in a case-insensitive manner" (§8.1.3.2 3) a)).</summary>
    public static CompilationVariableEvent? DefinitionAt(
        this DirectiveTimeline<CompilationVariableEvent> timeline, string name, int siteLine) =>
        timeline.TryLastInEffectBefore(siteLine, ev => ev.Line, out var last,
            ev => CobolNames.Same(ev.Name, name))
        && last.Value is not null ? last : null;
}
