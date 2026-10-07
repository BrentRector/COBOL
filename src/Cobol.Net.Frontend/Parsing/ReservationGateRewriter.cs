// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ⛔ THE TOKEN-LEVEL §8.9 RESERVATION GATE (kb/Work PB655). ISO §8.3.2.1 1): "Reserved words shall not be used as
/// user-defined words or system-names" — and §8.9 reserves a word per EDITION, so one lexer token (GOBACK, NULL,
/// ALTER, CONSTANT, …) is a keyword at some editions and a legal user-defined word at the others.
/// <para><b>Why it is not a predicate.</b> It was one: every such token was a <c>cobolWord</c> alternative behind
/// <c>{userWordHere("W")}?</c>. ANTLR evaluates a semantic predicate only at the LEFT EDGE of the decision it is
/// predicting, and <c>cobolWord</c> sits deep inside every reference — so for every ENCLOSING decision the gate was
/// invisible. At COBOL-2002, where NULL is reserved, <c>SET P TO NULL</c> chose the SET-to-identifier format on
/// the strength of a NULL "name" the gate then refused, and legal source failed to parse (measured: 115
/// Conformance reds when the PB655 words were admitted that way; <c>feedback_left_edge_predicates</c>).</para>
/// <para><b>What it is instead.</b> A gated word is never a <c>cobolWord</c> alternative; it stays its keyword
/// token, and its only grammar home is the generated <c>reservedGatedWord</c> rule that every DEFINITION slot
/// offers. A parse that matches it there at an edition where §8.9 leaves the word FREE has found a DECLARATION
/// (<see cref="CobolParserCoreBase.FreeGatedDeclarations"/>); this rewriter then retypes EVERY occurrence of that
/// word to <c>IDENTIFIER</c> and the source is parsed again, so the authoritative parse — every prediction in it —
/// sees a plain user-defined word. The SECOND witness is a syntax error whose offending token is such a free word
/// (<see cref="CobolParserCoreBase.NoteGatedOffender"/>): its keyword reading failed to parse, and the only other
/// reading §8.9 leaves it is a user-defined word — which is how a name introduced in a slot that does not offer
/// <c>reservedGatedWord</c> (an index-name, a SPECIAL-NAMES class-name) is reached. Where §8.9 RESERVES the word
/// nothing is retyped: no name slot can take the
/// keyword token, a reference fails with the targeted COBOLNET0901 (<see cref="CobolParserCoreBase.ReservedUserWordViolation"/>)
/// and a declaration draws it from the funnel.</para>
/// <para><b>Why DECLARED, not merely free.</b> The union grammar still spells the word's constructs at editions
/// that lack them, so the named edition gate can answer (<c>GOBACK.</c> at COBOL-85 → "requires COBOL-2002"), and
/// the migration mode (<c>--permissive</c>) admits a word the edition added while it is ALSO that edition's
/// keyword. A free word the program never declares can only be that keyword or an unresolvable reference; one it
/// declares is, per §8.3.2.1, a user-defined word — the same DETERMINATION PB805 recorded for operand lists
/// (<see cref="CobolParserCoreBase"/>'s <c>IsKeywordReadingHere</c>), now carried by the token itself.</para>
/// <para>The same decision must reach every re-parse of this group's text (the D18 subscript and D2 argument
/// fragments), which is why it travels as <see cref="TokenRetypes"/>. Design SSOT:
/// <c>docs/rearchitecture/DESIGN-frontend-grammar.md</c> ("the token-level reservation gate").</para>
/// </summary>
public static class ReservationGateRewriter
{
    /// <summary>⛔ THE GATE LOOP — the ONE entry every WHOLE-GROUP parse goes through (kb/Work PB655). <c>Frontend.LexAndParse</c>
    /// calls it. A gated word is no longer a <c>cobolWord</c> alternative, so a parse that
    /// skips this loop can never read it as a name, and every program naming a table
    /// <c>COL</c> (free at COBOL-85, reserved by §8.9 from 2002) fails with "no viable alternative". The loop
    /// therefore lives HERE, beside the retype it drives.
    /// <para>A pass that DECLARES a reservation-gated word §8.9 leaves free at this edition retypes every
    /// occurrence of it to IDENTIFIER, and the source is parsed again so every prediction sees a user-defined word.
    /// Each round frees at least one more word and a retyped token can never be declared again, so the loop ends; a
    /// program that declares no such word — nearly every program — parses exactly once. Only the LAST pass's
    /// diagnostics describe the source; they are copied into <paramref name="diagnostics"/>, and the decisions reach
    /// the tree as <c>TokenRetypes</c> so every fragment re-parse reads each word exactly as the tree does.</para>
    /// <para><paramref name="parsePass"/> parses the whole stream once from token 0 into the bag it is handed, and
    /// attaches the listener it is handed to its AUTHORITATIVE pass only (never a speculative SLL pass, which can
    /// fail where full LL succeeds) — that listener is the gate's second witness,
    /// <see cref="CobolParserCoreBase.NoteGatedOffender"/>.</para></summary>
    public static CobolParserCore.CompilationUnitContext ParseToFixpoint(CobolParserCore parser,
        CommonTokenStream tokens, TokenRetypes retypes, DiagnosticBag diagnostics,
        Func<DiagnosticBag, IAntlrErrorListener<IToken>, CobolParserCore.CompilationUnitContext> parsePass)
    {
        // Seeded with the words an earlier attempt already found declared (the caller's retypes; kb/Work PB1669), so a
        // later round's decisions ADD to them instead of replacing them.
        var freed = new HashSet<string>(retypes.FreedReservedWords, StringComparer.Ordinal);
        var passDiagnostics = new DiagnosticBag();
        var tree = parsePass(passDiagnostics, GatedOffenderWitness.Instance);
        while (parser.FreeGatedDeclarations.Where(freed.Add).ToList() is { Count: > 0 })
        {
            retypes = retypes with { FreedReservedWords = new HashSet<string>(freed, StringComparer.Ordinal) };
            Retype(tokens, retypes.FreedReservedWords);
            passDiagnostics = new DiagnosticBag();
            tree = parsePass(passDiagnostics, GatedOffenderWitness.Instance);
        }
        foreach (var d in passDiagnostics.Diagnostics) diagnostics.Add(d);
        tree.TokenRetypes = retypes;
        return tree;
    }

    /// <summary>Feeds each syntax error's offending token to the gate's second witness
    /// (<see cref="CobolParserCoreBase.NoteGatedOffender"/>). Stateless — the parser holds the set.</summary>
    private sealed class GatedOffenderWitness : BaseErrorListener
    {
        public static readonly GatedOffenderWitness Instance = new();

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
            int charPositionInLine, string msg, RecognitionException e)
            => (recognizer as CobolParserCoreBase)?.NoteGatedOffender(offendingSymbol);
    }

    /// <summary>Retype every default-channel reservation-gated keyword token whose word is in
    /// <paramref name="freed"/> (upper-case) to <c>IDENTIFIER</c>, keeping its source spelling. Returns how many
    /// tokens changed; the stream is rewound.</summary>
    public static int Retype(CommonTokenStream tokens, IReadOnlySet<string> freed)
    {
        if (freed.Count == 0) return 0;
        tokens.Fill();
        var list = tokens.GetTokens();
        int idType = CobolKeywordTokens.IdentifierType, changed = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var tok = list[i];
            if (tok.Channel != Lexer.DefaultTokenChannel || !CobolLexer.IsReservationGated(tok.Type)) continue;
            // By WORD, not by type alone: the decision is §8.9's, and §8.9 is keyed by spelling (the PB250 text rule).
            if (!freed.Contains(tok.Text.ToUpperInvariant())) continue;
            list[i] = new CommonToken(tok) { Type = idType };
            changed++;
        }
        tokens.Seek(0);
        return changed;
    }
}
