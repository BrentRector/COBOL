// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ANTLR4 error listener that feeds syntax errors into a <see cref="DiagnosticBag"/>.
/// Extracts [COBOLxxxx] diagnostic codes from messages produced by CobolErrorStrategy
/// and caps total errors at <see cref="MaxErrors"/> to prevent cascading noise.
/// </summary>
public sealed class CobolErrorListener(DiagnosticBag diagnostics, string sourcePath, SourceLineMap? lineMap = null)
    : BaseErrorListener
{
    /// <summary>
    /// Maximum number of parse errors to report per file.
    /// After this limit, additional errors are silently dropped.
    /// </summary>
    public const int MaxErrors = 20;

    private int _errorCount;

    /// <summary>Start offsets of the offending tokens already re-coded to COBOLNET0901 (kb/Work PB693).
    /// ANTLR raises TWO syntax errors on one offending token — the prediction failure and
    /// <c>CobolErrorStrategy</c>'s recovery message — which used to render as two DIFFERENT sentences. The
    /// §8.9 re-code below rewrites BOTH to the same one, so without this the user is told twice that the
    /// word is reserved. ISO §8.3.2.1 rule 1 is violated ONCE by one occurrence of the word, and that is how
    /// often it is reported; bounded by <see cref="MaxErrors"/>.</summary>
    private readonly HashSet<int> _reservedWordReported = [];

    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        if (_errorCount >= MaxErrors)
            return;

        // ⛔ THE §8.9 ARM (kb/Work PB693). A syntax error ON a reservation-gated word IS the §8.9 violation: the
        // gate is precisely why no user-defined-word alternative could match, and a REFERENCE to the word never
        // reaches the bound-tree funnel because the source did not parse. Report the targeted COBOLNET0901 so the
        // cause is NAMED, rather than "no viable alternative at input 'X'". The parser owns the test (it holds the
        // edition, the >>COBOL-WORDS overlay and the generated gate set); this stage only re-codes the
        // diagnostic, and the message has ONE definition in ReservedWordSet. Computed BEFORE the error is
        // counted so a suppressed duplicate does not eat a slot of the MaxErrors budget.
        string? reservedWordMessage = recognizer is CobolParserCoreBase parser
            ? parser.ReservedUserWordViolation(offendingSymbol)
            : null;
        if (reservedWordMessage is not null && !_reservedWordReported.Add(offendingSymbol.StartIndex))
            return;

        _errorCount++;

        // Extract [COBOLxxxx] code prefix from message if present
        string code = Diagnostics.DiagnosticDescriptors.COBOL0001.Code;
        string message = msg;
        if (msg.StartsWith('['))
        {
            int closeBracket = msg.IndexOf(']');
            if (closeBracket > 1)
            {
                code = msg[1..closeBracket];
                message = msg[(closeBracket + 1)..].TrimStart();
            }
        }

        if (reservedWordMessage is not null)
        {
            code = CobolNet.Editions.EditionCodes.ReservedWord;
            message = reservedWordMessage;
        }

        // ANTLR's `line` and `charPositionInLine` count the RESULTANT text (after the reference-format conversion,
        // COPY, REPLACE and continuation joins). The map names the file, physical line AND column the user edits (a
        // copybook's own path and line for an error inside copied text — kb/Work PB82; the column of the physical
        // line, not of the program-text area the fixed-form conversion kept — kb/Work PB1688, PB1801).
        var location = lineMap?.Locate(line, charPositionInLine, sourcePath)
                       ?? new SourceOrigin(sourcePath, line).ToLocation(charPositionInLine);
        var span = new TextSpan(offendingSymbol?.StartIndex ?? 0,
            offendingSymbol?.StopIndex ?? 0);
        diagnostics.ReportError(code, message, location, span);
    }
}
