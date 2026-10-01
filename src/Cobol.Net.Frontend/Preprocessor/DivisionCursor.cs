// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// WHICH DIVISION a position of logical text is in, as far as the §6.5 logical conversion of LIBRARY TEXT needs to know
/// (kb/Work PB1494): the obsolete COBOL-85 comment-entry paragraphs (AUTHOR … SECURITY, REMARKS) exist only in the
/// IDENTIFICATION DIVISION, so a copybook is converted knowing whether the COPY statement that brings it in stands in
/// one — otherwise a PROCEDURE DIVISION paragraph named REMARKS, copied from a library, is read as a comment-entry and
/// its lines are lost.
/// <para>The state is the one <see cref="ReferenceFormatProcessor"/>'s fixed-form classifier keeps for the text it
/// converts: an <c>IDENTIFICATION DIVISION</c> / <c>ID DIVISION</c> header or a paragraph named <c>…-ID</c> (the header
/// is optional) enters it, any other <c>… DIVISION</c> header leaves it. It is read off the text-words of the
/// logical text, so a division header inside a literal or a comment is never one. Positions asked must not decrease:
/// the cursor advances through the text once, however many COPY statements ask.</para>
/// </summary>
internal sealed class DivisionCursor(string text, bool startsInIdentificationDivision)
{
    private int _scanned;
    private string _previousWord = "";
    private bool _inIdentificationDivision = startsInIdentificationDivision;

    /// <summary>The text this cursor reads — a cursor is reused only for the very same text.</summary>
    public string Text { get; } = text;

    /// <summary>Whether the text is in an IDENTIFICATION DIVISION at <paramref name="position"/> (non-decreasing across
    /// calls).</summary>
    public bool InIdentificationDivisionAt(int position)
    {
        while (true)
        {
            int next = _scanned;   // a word that reaches past the position is left for a later, greater one
            if (!TextWordScanner.TryNext(Text, ref next, out var word) || word.End > position) break;
            _scanned = next;
            if (word.Kind == TextWordKind.DirectiveLine) continue;
            string spelling = Text.Substring(word.Start, word.End - word.Start).TrimEnd('.');
            if (spelling.Equals("DIVISION", StringComparison.OrdinalIgnoreCase))
                _inIdentificationDivision = _previousWord.ToUpperInvariant() is "IDENTIFICATION" or "ID";
            else if (spelling.EndsWith("-ID", StringComparison.OrdinalIgnoreCase))
                _inIdentificationDivision = true;           // PROGRAM-ID / CLASS-ID / ... (the header is optional)
            _previousWord = spelling;
        }
        return _inIdentificationDivision;
    }
}
