// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ⛔ A MNEMONIC-NAME OPERAND IS A WORD — THE ONE STATEMENT OF THAT RULE'S DIAGNOSTIC (kb/Work PB2499).
///
/// <para>Four procedure statements print a mnemonic-name-1 operand: ACCEPT Format 1 (<c>FROM mnemonic-name-1</c>,
/// ISO §14.9.1.2), DISPLAY Format 1 (<c>UPON mnemonic-name-1</c>, §14.9.11.2), SET Format 3
/// (<c>{ mnemonic-name-1 } … TO { ON | OFF }</c>, §14.9.39.2) and WRITE Format 1 (<c>ADVANCING mnemonic-name-1</c>,
/// §14.9.51.2).
/// ISO §8.3.2.2.16: a mnemonic-name "identifies an implementor-defined device-name, feature-name, or switch-name" —
/// it is a word with no data description, so it has nothing to subscript or reference-modify, and
/// no format of §8.4.2.2.2 qualifies one. The first three slots are spelled <c>cobolWord</c> in the grammar, so a
/// suffix there is a syntax error that <see cref="CobolErrorStrategy"/> names with this text; WRITE's slot shares
/// identifier-2's <c>dataReference</c> (only the SPECIAL-NAMES registry tells the two apart), so its binder
/// refuses the suffix with the same text. Before kb/Work PB2499 the ACCEPT and SET slots were
/// <c>dataReference</c>, their binders read only the word, and <c>ACCEPT X FROM MYIN (1)</c> /
/// <c>SET MYSW (1) TO ON</c> compiled clean with the suffix discarded.</para>
/// </summary>
public static class MnemonicNameSlot
{
    /// <summary>The COBOLNET2269 (statement-format-shape) message for a subscript, reference modification or
    /// qualifier written on the word <paramref name="name"/> in the mnemonic-name-1 slot of <paramref name="statement"/>
    /// (for example <c>ACCEPT … FROM</c>), whose general format is <paramref name="format"/> (for example
    /// <c>§14.9.1.2 Format 1</c>). Worded by POSITION ("stands in the mnemonic-name-1 position"), not by the
    /// word's declaration, because the parse layer cannot know what the word was declared as — only that a
    /// mnemonic-name is what the slot takes.</summary>
    public static string SuffixMessage(string statement, string name, string format)
        => $"'{name}' stands in the mnemonic-name-1 position of {statement}, and a mnemonic-name \"identifies an "
           + "implementor-defined device-name, feature-name, or switch-name\" (ISO §8.3.2.2.16) — a word with no data "
           + "description, so it takes no subscript or reference modification, and no qualified format of §8.4.2.2.2 "
           + "names one. "
           + $"ISO {format} writes mnemonic-name-1 alone; write '{name}'.";
}
