// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// ⛔ THE ONE RULE FOR "WHICH DIVISION IS THIS TEXT IN" in the §6.5 logical conversion (kb/Work PB1494, PB2739): the
/// obsolete COBOL-85 comment-entry paragraphs (AUTHOR … SECURITY, REMARKS) exist only in the IDENTIFICATION DIVISION, so
/// the converter must know whether it stands in one — otherwise a PROCEDURE DIVISION paragraph named REMARKS is read as a
/// comment-entry and its lines are lost.
/// <para>The rule is read off TEXT-WORDS, wherever they begin on a line: §6.3.1 "The program-text area begins in
/// character position 8 and terminates with the character position immediately to the left of margin R" draws no
/// Area A / Area B line around a division header. A <c>DIVISION</c> word preceded by <c>IDENTIFICATION</c> or <c>ID</c>
/// enters the IDENTIFICATION DIVISION, any other <c>… DIVISION</c> leaves it, and — the header being optional (§11.2.1
/// prints <c>[ IDENTIFICATION DIVISION. ]</c>) — the first paragraph of an identification division
/// (<see cref="IdentificationParagraphNames"/>) enters it. A word merely ENDING in <c>-ID</c> (<c>CUSTOMER-ID</c>) is a
/// user's name, never a paragraph header.</para>
/// <para>Two readers feed the one rule through <see cref="Feed"/>: the fixed-form converter line by line
/// (<see cref="Read"/>) over source and library text alike, and the COPY driver over the text-words of a block of logical
/// text (<see cref="InIdentificationDivisionAt"/>), so the library text of a COPY starts in the division the COPY statement
/// stands in. The cursor is per TEXT, not per block: the merged driver hands the COPY engine one block per directive
/// boundary, and the cursor carries across them (<see cref="Finish"/>). A division header inside a literal or a comment
/// is never one.</para>
/// </summary>
internal sealed class DivisionCursor(bool startsInIdentificationDivision)
{
    /// <summary>The paragraphs that begin an identification division (§11.2.1 General format: program-id-paragraph,
    /// function-id-paragraph, class-id-paragraph, method-id-paragraph, interface-id-paragraph):
    /// <see cref="CompilationUnitStart.UnitParagraphNames"/> and METHOD-ID. FACTORY and OBJECT begin definitions nested in
    /// a class and hold no comment-entry.</summary>
    private static readonly HashSet<string> IdentificationParagraphNames
        = new([.. CompilationUnitStart.UnitParagraphNames, "METHOD-ID"], CobolNames.Comparer);

    private string _previousWord = "";
    private string? _text;      // the block of logical text the scan position belongs to
    private int _scanned;

    /// <summary>Whether the text read so far ends in an IDENTIFICATION DIVISION.</summary>
    public bool InIdentificationDivision { get; private set; } = startsInIdentificationDivision;

    /// <summary>One text-word, spelled as written (a glued separator period is not part of it) — THE rule.</summary>
    private void Feed(string spelling)
    {
        if (CobolNames.Same(spelling, "DIVISION"))
            InIdentificationDivision = CobolNames.Same(_previousWord, "IDENTIFICATION") || CobolNames.Same(_previousWord, "ID");
        else if (IdentificationParagraphNames.Contains(spelling))
            InIdentificationDivision = true;
        _previousWord = spelling;
    }

    /// <summary>Read every text-word of <paramref name="text"/> — one physical line's program-text area.</summary>
    public void Read(string text)
    {
        for (int next = 0; TextWordScanner.TryNext(text, ref next, out var word);)
            if (word.Kind != TextWordKind.DirectiveLine)
                Feed(text.Substring(word.Start, word.End - word.Start).TrimEnd('.'));
    }

    /// <summary>Whether the logical <paramref name="text"/> is in an IDENTIFICATION DIVISION at
    /// <paramref name="position"/>. Positions asked of one text must not decrease; a different text (the next block of
    /// the same library text) is read from its start, in the state the previous one left.</summary>
    public bool InIdentificationDivisionAt(string text, int position)
    {
        Begin(text);
        while (true)
        {
            int next = _scanned;   // a word that reaches past the position is left for a later, greater one
            if (!TextWordScanner.TryNext(text, ref next, out var word) || word.End > position) break;
            _scanned = next;
            if (word.Kind != TextWordKind.DirectiveLine)
                Feed(text.Substring(word.Start, word.End - word.Start).TrimEnd('.'));
        }
        return InIdentificationDivision;
    }

    /// <summary>Read the rest of <paramref name="text"/> — the block is done and the next one continues from here.</summary>
    public void Finish(string text) => InIdentificationDivisionAt(text, text.Length);

    /// <summary>Pass over the COPY statement that ends at <paramref name="position"/> without reading it: its operands
    /// (<c>REPLACING ==PROCEDURE DIVISION== BY …</c>) are not text that stands in a division, and the library text it
    /// brings in is read by its own cursor (<see cref="Adopt"/>).</summary>
    public void Skip(string text, int position)
    {
        Begin(text);
        _scanned = Math.Max(_scanned, position);
        _previousWord = "";
    }

    /// <summary>Take the division a spliced library text left the text in.</summary>
    public void Adopt(DivisionCursor libraryText) => InIdentificationDivision = libraryText.InIdentificationDivision;

    private void Begin(string text)
    {
        if (ReferenceEquals(_text, text)) return;
        _text = text;
        _scanned = 0;
    }
}
