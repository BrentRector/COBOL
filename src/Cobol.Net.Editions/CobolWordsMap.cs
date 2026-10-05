// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>The four mutually-exclusive options of a <c>&gt;&gt;COBOL-WORDS</c> directive (ISO §7.3.10.2).</summary>
public enum CobolWordsAction
{
    /// <summary>EQUATE literal-1 WITH literal-2 — literal-2 becomes a synonym for literal-1 (GR2).</summary>
    Equate,
    /// <summary>UNDEFINE literal-3 — literal-3 is no longer reserved/restricted (GR3).</summary>
    Undefine,
    /// <summary>SUBSTITUTE literal-4 BY literal-5 — literal-5 takes over literal-4's role; literal-4 becomes a
    /// user word (GR4).</summary>
    Substitute,
    /// <summary>RESERVE literal-6 — literal-6 shall not be used as a user-defined word (GR5).</summary>
    Reserve,
}

/// <summary>
/// One parsed <c>&gt;&gt;COBOL-WORDS</c> operation (ISO §7.3.10). Both words are stored UPPER-CASE (SR2:
/// case-insensitive) with the enclosing literal quotes stripped. <see cref="Existing"/> is the
/// reserved/context-sensitive/intrinsic word SR3 governs (present for EQUATE/UNDEFINE/SUBSTITUTE);
/// <see cref="New"/> is the fresh user-defined word SR4 governs (present for EQUATE/SUBSTITUTE/RESERVE).
/// </summary>
/// <param name="Action">Which option.</param>
/// <param name="Existing">literal-1 (EQUATE) / literal-3 (UNDEFINE) / literal-4 (SUBSTITUTE); null for RESERVE.</param>
/// <param name="New">literal-2 (EQUATE) / literal-5 (SUBSTITUTE) / literal-6 (RESERVE); null for UNDEFINE.</param>
/// <param name="Line">The directive's 0-based line index in the final preprocessed text (diagnostics anchor).</param>
public sealed record CobolWordsOp(CobolWordsAction Action, string? Existing, string? New, int Line);

/// <summary>
/// The per-compilation-group <c>&gt;&gt;COBOL-WORDS</c> override layer (ISO §7.3.10; Annex D.12; Annex E.3.3
/// item 12) — the ONE runtime carrier the owner's recorded direction names: built in the Frontend
/// (<c>CobolWordsDirectiveProcessor</c>) from the directive text, then consulted by the lexer-applied
/// <c>CobolWordsRewriter</c> (token retyping), the map-aware lexer data-name gate, the composed
/// <see cref="ReservedWordSet"/> (RESERVE/UNDEFINE/SUBSTITUTE), and the binder's intrinsic-function-name
/// resolution. A pure-string data carrier so it crosses the frontend→compiler boundary like
/// <c>FlagState</c>/<c>RefModZeroLengthState</c>. <see cref="Empty"/> ⇒ every consumer is a no-op (the
/// zero-overhead invariant). Design SSOT: <c>docs/rearchitecture/DESIGN-cobol-words-directive.md</c>.
/// </summary>
public sealed class CobolWordsMap
{
    /// <summary>The no-directive map — every consumer short-circuits on <see cref="IsEmpty"/>.</summary>
    public static readonly CobolWordsMap Empty = new([]);

    /// <summary>The edition whose word populations §7.3.10.3 speaks of: the one that INTRODUCED the directive
    /// (<c>cobol-words-directive-2023</c>; §7.3.10, Annex E.3.3 item 12), the only edition at which any rule of the
    /// directive is ever asked. SR3/SR4 (§8.9 reserved ∪ §8.10 context-sensitive ∪ §8.11 intrinsic) and SR4's
    /// §8.3.2.1 word limits are both read at it, so no caller restates the number.</summary>
    public static readonly int DirectiveEdition = ConstructRegistry.Find(Constructs.CobolWordsDirective2023)!.IntroducedIn;

    private readonly Dictionary<string, string> _synonyms;
    private readonly HashSet<string> _deReserved;
    private readonly HashSet<string> _reserved;

    public CobolWordsMap(IReadOnlyList<CobolWordsOp> ops)
    {
        Ops = ops;
        _synonyms = new(StringComparer.OrdinalIgnoreCase);
        _deReserved = new(StringComparer.OrdinalIgnoreCase);
        _reserved = new(StringComparer.OrdinalIgnoreCase);
        foreach (var op in ops)
        {
            switch (op.Action)
            {
                case CobolWordsAction.Equate when op is { Existing: { } e, New: { } n }:
                    _synonyms[n] = e;   // the new word acts as the existing word
                    break;
                case CobolWordsAction.Substitute when op is { Existing: { } e, New: { } n }:
                    _synonyms[n] = e;   // the new word takes over the existing word's role
                    _deReserved.Add(e); // and the existing word becomes a user word
                    break;
                case CobolWordsAction.Undefine when op.Existing is { } e:
                    _deReserved.Add(e);
                    break;
                case CobolWordsAction.Reserve when op.New is { } n:
                    _reserved.Add(n);
                    break;
            }
        }
    }

    /// <summary>The parsed operations, in source order (SR3/SR4 validation + the token rewriter walk these).</summary>
    public IReadOnlyList<CobolWordsOp> Ops { get; }

    /// <summary>True when no <c>&gt;&gt;COBOL-WORDS</c> directive was present — every consumer short-circuits.</summary>
    public bool IsEmpty => Ops.Count == 0;

    /// <summary>A fresh user word (EQUATE literal-2 / SUBSTITUTE literal-5) → the existing word it stands in for
    /// (literal-1 / literal-4). The identifier→keyword rewriter and the intrinsic-synonym binder read this.</summary>
    public IReadOnlyDictionary<string, string> Synonyms => _synonyms;

    /// <summary>Words that LOSE their reserved/context/intrinsic status for this group (UNDEFINE literal-3 +
    /// SUBSTITUTE literal-4): the keyword→identifier rewriter, the <see cref="ReservedWordSet"/> suppress set,
    /// and the map-aware lexer data-name gate read this.</summary>
    public IReadOnlySet<string> DeReserved => _deReserved;

    /// <summary>Words newly reserved for this group (RESERVE literal-6): the <see cref="ReservedWordSet"/> reserve
    /// overlay reads this so a use as a user-defined word is COBOLNET0901-rejected.</summary>
    public IReadOnlySet<string> Reserved => _reserved;

    /// <summary>
    /// The canonical COBOL word that a word WRITTEN in the source denotes under this compilation group's
    /// directives (ISO §7.3.10.4 GR2/GR3/GR4) — <b>the ONE resolution</b> every consumer that classifies a word
    /// BY NAME calls. The lexer's <c>CobolWordsRewriter.Plan</c> retype can only reach words the lexer makes a keyword
    /// TOKEN; a word the binder classifies from its TEXT (the §15 phrase words ANYCASE/LOCALE/HEX/NAT/…, the
    /// SET-statement LC_ categories, the ALPHABET coded-set names) is reached ONLY here, so a site that compares
    /// raw text without calling this is inert to the directive in both directions (kb/Work PB250).
    /// <list type="bullet">
    /// <item><see langword="null"/> when <paramref name="written"/> was DE-RESERVED — UNDEFINE literal-3 (GR3:
    /// the word "shall no longer be reserved or restricted in any way … and any syntax requiring the use of the
    /// COBOL word that is the content of literal-3 shall not be available for use in this compilation group") or
    /// SUBSTITUTE literal-4 (GR4: it "shall no longer be a reserved word, a context-sensitive word, nor an
    /// intrinsic function name within this compilation group"). The caller must then treat the word as the
    /// user-defined word it now is — never as the keyword it spells.</item>
    /// <item>the CANONICAL word when <paramref name="written"/> is a synonym — EQUATE literal-2 (GR2: "may be
    /// used in any syntax requiring the use of the reserved word, context-sensitive word, or intrinsic function
    /// name that is the content of literal-1") or SUBSTITUTE literal-5 (GR4: "shall be used in any syntax where
    /// the COBOL word that is the content of literal-4 is documented as required or optional").</item>
    /// <item><paramref name="written"/> itself otherwise — including every word when <see cref="IsEmpty"/>, the
    /// zero-overhead no-directive path.</item>
    /// </list>
    /// SR3 (§7.3.10.3) restricts literal-1/3/4 to a reserved word, a context-sensitive word or an
    /// intrinsic-function name, and SR5 forbids the same COBOL word in more than one directive of a compilation
    /// group, so the de-reserved set and the synonym keys are disjoint: the order of the two tests below is not a
    /// precedence choice. It matches the order <c>IntrinsicBinder.BindIntrinsicCore</c> has always used
    /// (removal tested against the ORIGINAL written name, then the synonym applied).
    /// </summary>
    /// <param name="written">The word as written in the source, UPPER-CASE (SR2/GR1 — case-insensitive).</param>
    public string? Resolve(string written)
    {
        if (Ops.Count == 0) return written;
        if (_deReserved.Contains(written)) return null;
        return _synonyms.TryGetValue(written, out string? canonical) ? canonical : written;
    }

    /// <summary>
    /// True when the word WRITTEN in the source denotes the keyword <paramref name="keyword"/> under this
    /// group's directives — <b>the comparison every by-name classifier makes</b>, so <see cref="Resolve"/>'s
    /// GR2/GR3/GR4 reading is applied once and no caller re-implements it. Used by the parser's text predicates
    /// (<c>CobolParserCoreBase.Word</c>) and by every binder site that recognizes a §8.9/§8.10 word the lexer
    /// does not tokenize — the SET-statement locale categories, the ALPHABET coded-set names, CALL … AS NESTED.
    /// <para>Allocation-free on the no-directive path: the uppercase normalization <see cref="Resolve"/> needs
    /// happens only when a directive is present, and these run inside ANTLR's speculative prediction.</para>
    /// </summary>
    /// <remarks>⛔ Give this a word AS WRITTEN, and only for a word the LEXER does not tokenize (the
    /// §8.9/§8.10 words that arrive as bare IDENTIFIERs). For a word that may arrive as a keyword TOKEN use
    /// <c>CobolWordsRewriter.TokenIs</c> instead: the lexer's retype already resolved those, and resolving
    /// them again loses the synonym the user wrote.</remarks>
    public bool Is(string? written, string keyword)
        => written is not null
           && (Ops.Count == 0
               ? string.Equals(written, keyword, StringComparison.OrdinalIgnoreCase)
               : Resolve(written.ToUpperInvariant()) is { } w
                 && string.Equals(w, keyword, StringComparison.OrdinalIgnoreCase));
}
