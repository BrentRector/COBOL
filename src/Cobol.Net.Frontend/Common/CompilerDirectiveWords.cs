// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Frontend.Common;

/// <summary>
/// ISO/IEC 1989:2023 §8.12 <b>Compiler-directive words</b> — the ONE representation of the words "reserved in
/// compiler directives" (kb/Work PB1366). §7.3.3 SR7 reserves each of them within the context of a directive that
/// specifies it, and §7.3.11.3 SR1 / §7.3.8.4.3 SR1 forbid any of them as a compilation-variable-name; before
/// PB1366 no stage consulted the list, because the list had no representation in <c>src</c> at all, so
/// <c>&gt;&gt;DEFINE OFF AS 1</c> and <c>&gt;&gt;IF LISTING IS DEFINED</c> were accepted.
///
/// <para>The words are data, transcribed once from the printed table (PDF page 215, cross-checked against the
/// Markdown transcription) and held equal to it by <c>CompilerDirectiveWordsDriftTests</c>; that test also asserts
/// every word of the directive catalog (<see cref="CobolNet.Editions.CompilerDirectiveCatalog"/>) is on the list, so
/// a new directive cannot be added without its word being reserved. "In addition to the above list, all of the
/// exception-names specified in 14.6.13.1, Exception conditions, are reserved in the context of compiler
/// directives" — those are the <see cref="ExceptionCatalog"/>'s, asked through it, never copied.</para>
/// </summary>
public static class CompilerDirectiveWords
{
    private static readonly string[] Table =
    [
        "ALL", "AND", "AS",
        "B-AND", "B-NOT", "B-OR", "B-SHIFT-L", "B-SHIFT-R", "B-SHIFT-LC", "B-SHIFT-RC", "B-XOR",
        "CALL-CONVENTION", "CHECKING", "COBOL", "COBOL-WORDS", "COMPILE-TIME-ARITHMETIC-EXPRESSIONS", "CORRESPONDING",
        "DE-EDITING", "DEFINE", "DEFINED", "DISPLAY", "DIVIDE",
        "ELSE", "END-EVALUATE", "END-IF", "EQUAL", "EQUATE", "EVALUATE", "EXCLUSIVE-OR", "EXTERNAL-FILE-FILE-STATUS",
        "FIXED", "FLAG-02", "FLAG-14", "FORMAT", "FREE", "FUNCTION-ARGUMENT",
        "GREATER",
        "IF", "IMP", "IS", "I-O-DECLARATIVE", "I-O-STATUS-04", "I-O-STATUS-07",
        "LEAP-SECOND", "LESS", "LISTING", "LOCATION",
        "MOVE", "MOVE-TO-SAME-NAME",
        "NOT", "NUMVAL",
        "OFF", "ON", "OR", "OTHER", "OVERRIDE",
        "PAGE", "PARAMETER", "POP", "PROPAGATE", "PUSH",
        "READ-PREVIOUS", "REF-MOD-ZERO-LENGTH", "RESERVE",
        "SET", "SIZE", "SOURCE", "STANDARD-1", "STANDARD-2", "SUBSTITUTE",
        "THAN", "THROUGH", "THRU", "TO", "TRUE", "TURN",
        "UNDEFINE", "UPON",
        "VALUE-EDITING", "VALUE-FIG-CON-LENGTH", "VALUE-ZERO",
        "WHEN", "WITH", "WRITE-END-OF-PAGE",
        "XOR",
        "ZERO-LENGTH",
        "+", "-", "*", "/", "<=", ">=", "<", ">", "<>", "=", "(", ")",
    ];

    private static readonly HashSet<string> Set = new(Table, StringComparer.OrdinalIgnoreCase);

    /// <summary>The §8.12 table, in the order the standard prints its columns.</summary>
    public static IReadOnlyList<string> All => Table;

    /// <summary>True when <paramref name="word"/> is reserved in compiler directives: a §8.12 table word, or an
    /// exception-name of §14.6.13.1 (the open <c>EC-USER-</c> / <c>EC-IMP-</c> families included). Case-insensitive,
    /// like every COBOL word (§8.3.1).</summary>
    public static bool IsReserved(string word) => Set.Contains(word) || ExceptionCatalog.TryGet(word, out _);
}
