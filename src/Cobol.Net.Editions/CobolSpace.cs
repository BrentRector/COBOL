// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>
/// ⛔ THE ONE DEFINITION OF "A SPACE" IN SOURCE TEXT (kb/Work PB1543, PB1660). ISO §8.3.5 1): "The COBOL character space
/// is a separator" — and it is the ONLY character that is (<c>cite.py --check 8.3.5 "The COBOL character space is a
/// separator"</c> → OK 1)). Here the separator characters are the space and the LINE END: exactly the lexer's skipped
/// <c>WS</c> rule (<c>CobolLexer.g4</c>: <c>[ \n]</c>), so a word the text stages end is a word the lexer ends. No tab
/// and no CR LF reaches a stage after the line-entry stage (<c>PhysicalLines</c> has expanded the one — DOC-A.1-157 —
/// and taken the other to its LF — DOC-A.1-156). A blank line (§6.3.6: "A blank line is one that contains only space
/// characters between margin C and margin R") is the same question.
/// <para>⚠ NEVER .NET's Unicode <c>White_Space</c> — <c>char.IsWhiteSpace</c>, a parameterless <c>Trim()</c> /
/// <c>TrimStart()</c> / <c>TrimEnd()</c>, <c>Split()</c> and <c>string.IsNullOrWhiteSpace</c> all use it. U+00A0,
/// U+2000–U+200A, U+3000 and the rest only LOOK like a space: they are ordinary characters of whatever word they stand
/// in (§7.2.2.5 3)), so a stage that splits on them reads two operands where the programmer wrote one, and blanks a line
/// that holds program text. A text stage asks THIS class; <c>CobolSpaceDriftTests</c> fails the build when a stage under
/// <c>Preprocessor/</c>, the directive-line parse or the directive operand catalog spells the other set.</para>
/// </summary>
public static class CobolSpace
{
    /// <summary>The separator characters, for the <c>Trim(char[])</c> / <c>Split(char[])</c> overloads.</summary>
    public static readonly char[] Separators = [' ', '\n'];

    /// <summary>Whether <paramref name="c"/> is a separator space (§8.3.5 1)).</summary>
    public static bool IsSeparator(char c) => c is ' ' or '\n';

    /// <summary>Whether <paramref name="text"/> holds no character but separator spaces — a blank line (§6.3.6).</summary>
    public static bool IsBlank(ReadOnlySpan<char> text) => text.IndexOfAnyExcept(Separators) < 0;

    /// <summary><paramref name="text"/> without its leading and trailing separator spaces.</summary>
    public static string TrimSpaces(this string text) => text.Trim(Separators);

    /// <summary><paramref name="text"/> without its leading separator spaces.</summary>
    public static string TrimSpacesStart(this string text) => text.TrimStart(Separators);

    /// <summary><paramref name="text"/> without its trailing separator spaces.</summary>
    public static string TrimSpacesEnd(this string text) => text.TrimEnd(Separators);

    /// <summary>The span form of <see cref="TrimSpacesStart(string)"/>.</summary>
    public static ReadOnlySpan<char> TrimSpacesStart(this ReadOnlySpan<char> text) => text.TrimStart(Separators);

    /// <summary>The words of <paramref name="text"/> — its runs of non-separator characters.</summary>
    public static string[] SplitSpaces(this string text) => text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
}
