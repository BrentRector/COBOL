// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// The token types that ARE a literal of class alphanumeric, boolean or national: the four DEFAULT-mode literal tokens
/// and their SUBSCRIPT-mode twins. ONE set, read by both rules that are asked of every literal token — the literal's
/// own §8.3.3 syntax rules (<c>LiteralScreenPass</c>) and its §8.3.5 5) delimiter separation
/// (<see cref="SeparatorRule"/>). Derived-and-pinned: <c>LiteralScreenDriftTests</c> reads the lexer grammar and
/// requires this set to equal the tokens defined over a literal body fragment, so a new literal token joins both
/// rules or fails there.
/// </summary>
public static class LiteralTokens
{
    /// <summary>Every literal token type the lexer emits for a delimited (quoted) literal.</summary>
    public static IReadOnlySet<int> Types { get; } = new HashSet<int>
    {
        CobolLexer.STRINGLIT, CobolLexer.HEXLIT, CobolLexer.NATLIT, CobolLexer.BOOLLIT,
    };
}
