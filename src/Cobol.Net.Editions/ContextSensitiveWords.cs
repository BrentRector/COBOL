// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Editions;

/// <summary>One ISO §8.10 context-sensitive word: the word and the language construct or context that reserves
/// it. "If a context-sensitive word is used where the context-sensitive word is permitted in the general format,
/// the word is treated as a keyword; otherwise it is treated as a user-defined word" (§8.10) — so unlike a §8.9
/// reserved word this carries no per-edition reservation flags and never bars a user-defined name on its
/// own.</summary>
/// <param name="Word">The word, uppercase.</param>
/// <param name="Context">The §8.10 table's second column — the construct the word is a keyword in.</param>
public sealed record ContextSensitiveWordEntry(string Word, string Context);

/// <summary>
/// The generated ISO §8.10 context-sensitive word table (the partial half is
/// <c>ContextSensitiveWords.Table.cs</c>, emitted by <c>scripts/gen-reserved-words.ps1</c> straight from the
/// spec section; <c>tests/version-matrix/context-sensitive-words.json</c> is the same data and
/// <c>ContextSensitiveWordsDriftTests</c> asserts the three agree).
/// <para><b>WHY THE COMPILER NEEDS THIS POPULATION AND NOT JUST THE LEXER'S</b> (kb/Work PB250). §7.3.10.3 SR3
/// admits "a reserved word, a context-sensitive word, or an intrinsic function name" as the EXISTING word of a
/// <c>&gt;&gt;COBOL-WORDS</c> directive, and SR4 bars all three as the NEW word. The category test used to answer
/// "context-sensitive?" from the lexer vocabulary, which knows only the context words this compiler happens to
/// TOKENIZE — so a legal <c>&gt;&gt;COBOL-WORDS EQUATE "HEX" WITH …</c> was rejected outright (COBOLNET1623), and
/// with it every §15 phrase word, SET-statement locale category and ALPHABET coded-set name that arrives as a
/// bare IDENTIFIER. §8.10's own NOTE points the other way: "Words can be added or deleted from this list for a
/// specific compilation group by use of the COBOL-WORDS directive."</para>
/// <para>No per-edition flags: the directive is a COBOL-2023 introduction
/// (<c>cobol-words-directive-2023</c>; §7.3.10, Annex E.3.3 item 12), so the only edition at which SR3/SR4 are
/// ever asked is the one this table transcribes.</para>
/// </summary>
public static partial class ContextSensitiveWords
{
    private static Dictionary<string, ContextSensitiveWordEntry>? _byWord;

    private static Dictionary<string, ContextSensitiveWordEntry> ByWord =>
        _byWord ??= Entries.ToDictionary(e => e.Word, CobolNames.Comparer);

    /// <summary>The §8.10 row for <paramref name="word"/> (case-insensitive), or null when the word is not a
    /// context-sensitive word.</summary>
    public static ContextSensitiveWordEntry? Find(string word) => ByWord.GetValueOrDefault(word);

    /// <summary>True when <paramref name="word"/> is an ISO §8.10 context-sensitive word.</summary>
    public static bool Contains(string word) => ByWord.ContainsKey(word);

    /// <summary>The number of rows — the drift test's population check (never the words themselves: the
    /// content-filter rule keeps word lists out of conversation streams).</summary>
    public static int Count => Entries.Length;
}
