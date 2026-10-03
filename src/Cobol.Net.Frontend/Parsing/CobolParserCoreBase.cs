// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;

namespace CobolNet.Frontend.Generated;

/// <summary>
/// Base class for the ANTLR-generated CobolParserCore.
/// Provides semantic predicates for paragraph detection.
/// </summary>
public abstract class CobolParserCoreBase : Parser
{
    /// <summary>
    /// The targeted edition (rearch P2.7): the SINGLE source of the dialect year the grammar's introduction
    /// gates read (ending the pre-P2 triple-sourcing across the parser base, the frontend, and the compiler).
    /// Defaults to COBOL-85 (strict) — the level the NIST CCVS corpus targets; the CLI/front-end sets it
    /// explicitly (via <see cref="DialectLevel"/>) for every real parse.
    /// </summary>
    public EditionInfo Edition { get; set; } = EditionInfo.Of(85);

    /// <summary>
    /// Dialect level (ISO year) for gating non-COBOL-85 features — a shim over <see cref="Edition"/>, the single
    /// source. Kept for the <c>parser.DialectLevel = …</c> call sites (Frontend); the setter rebuilds
    /// <see cref="Edition"/> preserving its permissive axis.
    /// </summary>
    public int DialectLevel
    {
        get => Edition.Year;
        set => Edition = EditionInfo.Of(value, Edition.Permissive);
    }

    /// <summary>
    /// The compilation group's <c>&gt;&gt;COBOL-WORDS</c> overrides (ISO §7.3.10), set by the frontend beside
    /// <see cref="DialectLevel"/>. <b>The parser needs it because several of its predicates classify a word by
    /// TEXT</b> — LOCALE, ORDER, CLASSIFICATION, ATTRIBUTE and the LC_ categories are §8.9/§8.10 words that are
    /// deliberately NOT lexer tokens, so the post-lex <c>CobolWordsRewriter</c> cannot reach them and a raw text
    /// comparison here is inert to the directive in both directions: an EQUATEd synonym would not steer the
    /// prediction (legal source rejected) and an UNDEFINE'd word would still steer it (the user's data-name
    /// silently eaten). Every such comparison goes through <see cref="Canonical"/>; kb/Work PB250.
    /// </summary>
    public CobolWordsMap CobolWords { get; set; } = CobolWordsMap.Empty;

    /// <summary>The canonical COBOL word <paramref name="written"/> denotes under this group's directives, or
    /// null when the directive de-reserved it (ISO §7.3.10.4 GR3/GR4 — it is a user-defined word now, and no
    /// syntax that requires the keyword may recognize it). Delegates to the ONE rule,
    /// <see cref="CobolWordsMap.Resolve"/>. Allocation-free and branch-cheap on the overwhelmingly common
    /// no-directive path, because these run inside ANTLR's speculative prediction.</summary>
    /// <remarks>Takes a word the caller already knows is a plain spelling (a keyword literal passed by the
    /// grammar, never token text) - token text goes through <see cref="Word"/>, which knows whether the
    /// post-lex rewriter already resolved it.</remarks>
    private string? Canonical(string? written)
        => written is null ? null
         : CobolWords.IsEmpty ? written
         : CobolWords.Resolve(written.ToUpperInvariant());

    protected bool is85()   => Edition.Has(85);
    protected bool is2002() => Edition.Has(2002);
    protected bool is2014() => Edition.Has(2014);
    protected bool is2023() => Edition.Has(2023);

    /// <summary>
    /// True when a SPECIAL-NAMES entry is the §12.3.7 <c>LOCALE locale-name-1 IS {external-locale-name-1 |
    /// literal-4}</c> clause — an <b>§A.4.9 item 10</b> optional-locale element ("SPECIAL-NAMES paragraph: LOCALE
    /// clause and LOCALE phrases in the ALPHABET clause"), which must be DIAGNOSED as documented non-support
    /// rather than fail as a parse error (fix-queue PB25).
    /// <para>⛔ WHY A TEXT PREDICATE AND NOT A LEXER TOKEN. LOCALE <i>is</i> reserved at 2002+ (§8.9;
    /// <c>reserved-words.json</c> r2002/r2014/r2023), so a token would be defensible — but the intrinsic side
    /// DEPENDS on it arriving as a bare word: <c>IntrinsicBinder.KeywordWordOf</c> detects
    /// <c>LOWER-CASE(x LOCALE …)</c> precisely because the phrase parses as ordinary space-separated arguments.
    /// Tokenizing LOCALE would silently break that already-working diagnostic, so the reservation stays modelled
    /// where the rest of §8.9 lives and this reads the word.</para>
    /// <para>⚠ EDITION-GATED DELIBERATELY. At COBOL-85 LOCALE is NOT reserved (r85 = false), so
    /// <c>SPECIAL-NAMES. LOCALE IS FOO.</c> is a legal implementor-switch entry there and must keep parsing as
    /// one. Below 2002 this returns false and the entry falls through to <c>implementorSwitchEntry</c> unchanged.
    /// </para>
    /// <para>The shape check (a following word, then IS or the value) is what keeps it off that 85 switch entry
    /// even when the predicate is reached: a bare <c>LOCALE IS FOO</c> has no locale-name and is not this clause.
    /// </para>
    /// </summary>
    /// <summary>The OBJECT-COMPUTER CHARACTER CLASSIFICATION clause is ahead (ISO §12.3.6.2; kb/Work PB78, PB695).
    /// CLASSIFICATION is not a lexer token (a plain word at COBOL-85), so the clause is recognized by the text of
    /// its one REQUIRED word — printed folio 285 underlines CLASSIFICATION and leaves CHARACTER plain, making
    /// CHARACTER an optional word (§5.2.3).
    /// <para>⛔ A LEFT-EDGE PREDICATE. It used to be asserted after the CHARACTER token had already been matched,
    /// which was sound only while CHARACTER was mandatory: a predicate reached mid-alternative does not steer
    /// prediction, it throws once the alternative has been committed to. With CHARACTER optional the rule can
    /// open on an ordinary word, so this reads BOTH spellings from the left edge — <c>CHARACTER CLASSIFICATION
    /// …</c> and a bare <c>CLASSIFICATION …</c>.</para>
    /// <para>⚠ IT DEMANDS THE CLAUSE CONTINUE. `OBJECT-COMPUTER. CLASSIFICATION.` names a computer whose name
    /// happens to be CLASSIFICATION — legal at every edition, since the word is context-sensitive and never
    /// reserved (§8.9) — so a following period (or end of input) makes this NOT the clause. That shape check is
    /// the same one <see cref="localeClauseAhead"/> uses to keep off the '85 switch entry.</para></summary>
    protected bool classificationAhead()
    {
        int i = Word(TokenStream.LT(1), "CHARACTER") ? 2 : 1;
        if (!Word(TokenStream.LT(i), "CLASSIFICATION")) return false;
        return TokenStream.LT(i + 1) is { } next && next.Type is not (CobolParserCore.DOT or Antlr4.Runtime.TokenConstants.EOF);
    }

    /// <summary>One of the OBJECT-COMPUTER paragraph's own clauses starts here — the PROGRAM COLLATING SEQUENCE
    /// clause, whose optional words leave it able to open on PROGRAM, COLLATING or SEQUENCE, the CHARACTER
    /// CLASSIFICATION clause via <see cref="classificationAhead"/> (both ISO §12.3.6.2), or one of the two
    /// X3.23-1985 clauses ISO 2002 deleted, MEMORY SIZE and SEGMENT-LIMIT (kb/Work PB830).
    /// <para>⛔ THE ONE PLACE THAT ANSWERS "IS THIS A CLAUSE OR A COMPUTER-NAME?" (kb/Work PB695). It steers
    /// whether to enter the optional <c>computer-name-1</c> slot: a clause may open on the bare WORD
    /// CLASSIFICATION, MEMORY or SEGMENT-LIMIT, none of which has a token type a `~(…)` set could exclude. The
    /// '85 clauses belong here too so that a name-less <c>OBJECT-COMPUTER. MEMORY SIZE 8 WORDS.</c> parses as the
    /// clause and draws its named edition gates, instead of binding MEMORY as the computer name and failing at
    /// SIZE.</para>
    /// </summary>
    protected bool objectComputerClauseAhead()
        => TokenStream.LT(1) is { } t
           && (t.Type is CobolParserCore.PROGRAM or CobolParserCore.COLLATING or CobolParserCore.SEQUENCE
               || classificationAhead() || memorySizeAhead() || segmentLimitAhead());

    /// <summary>X3.23-1985 <c>MEMORY SIZE integer {WORDS | CHARACTERS | MODULES}</c> — the word MEMORY followed by
    /// the SIZE token (kb/Work PB830). A left-edge predicate; the SIZE check keeps a computer named MEMORY a
    /// computer-name.</summary>
    protected bool memorySizeAhead()
        => Word(TokenStream.LT(1), "MEMORY") && TokenStream.LT(2)?.Type == CobolParserCore.SIZE;

    /// <summary>The MEMORY SIZE clause's unit word — WORDS or MODULES (CHARACTERS is a lexer token and is
    /// matched by the grammar directly).</summary>
    protected bool memoryUnitAhead()
        => Word(TokenStream.LT(1), "WORDS") || Word(TokenStream.LT(1), "MODULES");

    /// <summary>The next token spells the context word <paramref name="word"/> — a word a general format names that
    /// is no lexer token and is never reserved (§8.9), so it arrives as an IDENTIFIER and is recognized by its TEXT
    /// through the one <see cref="Word"/> funnel (>>COBOL-WORDS applied). Written as the LEFT-EDGE predicate of a
    /// one-word rule (e.g. <c>dynamicLengthPrefixedWord : {wordAhead("PREFIXED")}? IDENTIFIER</c>), which is where
    /// a predicate steers prediction rather than throwing (kb/Work PB829, the dynamic-length-structure-clause).</summary>
    protected bool wordAhead(string word) => Word(TokenStream.LT(1), word);

    /// <summary>X3.23-1985 <c>SEGMENT-LIMIT IS segment-number</c> — the word SEGMENT-LIMIT (kb/Work PB830).</summary>
    protected bool segmentLimitAhead() => Word(TokenStream.LT(1), "SEGMENT-LIMIT");

    private static readonly string[] LocaleCategories =
        ["LC_ALL", "LC_COLLATE", "LC_CTYPE", "LC_MESSAGES", "LC_MONETARY", "LC_NUMERIC", "LC_TIME", "USER-DEFAULT"];

    /// <summary>⛔ THE ONE WORD-TEXT COMPARISON IN THIS CLASS (kb/Work PB250). Every predicate that recognizes a
    /// COBOL word by its spelling calls this, so <c>&gt;&gt;COBOL-WORDS</c> is applied once, for the whole class,
    /// in both directions: an EQUATE/SUBSTITUTE synonym resolves onto the canonical word and steers the same
    /// prediction (§7.3.10.4 GR2/GR4), and an UNDEFINE'd word resolves to null and steers none (GR3). A new
    /// predicate that writes its own <c>string.Equals(token.Text, …)</c> re-opens the defect.</summary>
    private bool Word(IToken? t, string text)
        => CobolNet.Frontend.Parsing.CobolWordsRewriter.TokenIs(t, text, CobolWords);

    /// <summary>The next token spells one of <paramref name="words"/> — the LEFT-EDGE predicate of a
    /// <c>formatWord</c> brace alternative (kb/Work PB764): <c>{wordAhead("NESTED")}? formatWord | cobolWord</c>
    /// takes the general format's KEYWORD arm exactly when the word is that keyword, through the one
    /// <see cref="Word"/> comparison (so &gt;&gt;COBOL-WORDS reaches it both ways). The words are LITERAL
    /// arguments at the use site, never a rule parameter: a predicate that reads a parameter is context-dependent
    /// and cannot be evaluated while ANTLR predicts from an enclosing rule.</summary>
    protected bool wordAhead(params string[] words)
    {
        var t = TokenStream.LT(1);
        foreach (var w in words)
            if (Word(t, w)) return true;
        return false;
    }

    /// <summary>True when the token spells one of the §14.9.39 Format-11 locale-category words (LC_… /
    /// USER-DEFAULT), through the <see cref="Word"/> funnel so the directive reaches them.</summary>
    private bool IsLocaleCategory(IToken? t) => Array.Exists(LocaleCategories, c => Word(t, c));

    /// <summary>SET LOCALE {LC_… | USER-DEFAULT} TO … (ISO §14.9.39 Format 11; kb/Work PB92) — a LEFT-EDGE predicate
    /// (LT(1) is the SET token itself): true when LT(2)/LT(3) spell LOCALE and a locale category and LT(4) is TO — the
    /// format's own keywords, plain words reserved 2002+ (§8.9); below 2002 they are user words and a SET of a data
    /// item named LOCALE keeps its '85 reading.</summary>
    protected bool setLocaleAhead()
    {
        if (!Word(TokenStream.LT(2), "LOCALE")) return false;
        if (TokenStream.LT(3) is not { } cat || !IsLocaleCategory(cat)) return false;
        // The category operand is a SET (choice indicators): LT(3), LT(4), … are categories until the TO (bounded —
        // seven categories exist, so eight words is already malformed and the ordinary SET forms get it).
        int i = 4;
        while (i <= 10 && IsLocaleCategory(TokenStream.LT(i))) i++;
        if (!Word(TokenStream.LT(i), "TO")) return false;
        // ⚠ EDITION-GATED ONLY FOR THE USER-DEFAULT-FIRST SHAPE (kb/Work PB64 T1). `SET LOCALE LC_… TO x` has NO
        // COBOL-85 reading — LC_ALL … LC_TIME carry an underscore, which is not an '85 word character (§8.3.2.1;
        // Constructs.UserWordUnderscore2002) — so the shape is recognized at every edition and the ONE construct gate
        // (set-locale-2002, VisitSetLocaleStatement) answers below 2002 with the explanatory introduction diagnostic
        // instead of "'LOCALE' is not defined". `SET LOCALE USER-DEFAULT TO x` IS a legal '85 Format-1 statement (two
        // receivers named LOCALE and USER-DEFAULT), so that shape keeps its '85 reading below 2002.
        // Keys on the WRITTEN spelling deliberately: the escape is about LEXICAL shape (an underscore is not an
        // '85 word character), so a >>COBOL-WORDS synonym spelled without one correctly falls to the edition test.
        return cat.Text.StartsWith("LC_", StringComparison.OrdinalIgnoreCase) || Edition.Has(2002);
    }

    /// <summary>SET identifier-11 TO LOCALE {LC_ALL | USER-DEFAULT} (ISO §14.9.39 Format 12; kb/Work PB92) — a
    /// LEFT-EDGE predicate: scans past identifier-11 (a bounded token walk to the first TO before the statement's
    /// period) and answers true when the two words after TO spell LOCALE and LC_ALL / USER-DEFAULT.</summary>
    protected bool saveLocaleAhead()
    {
        // NOT edition-gated (kb/Work PB64 T1): `SET p TO LOCALE LC_ALL` has no '85 reading (LC_ALL's underscore) and
        // `SET p TO LOCALE USER-DEFAULT` would be a Format-1 SET followed by a stray word — a parse error either way;
        // recognizing the shape everywhere lets the set-save-locale-2002 gate answer below 2002.
        for (int i = 2; i <= 40; i++)
        {
            var t = TokenStream.LT(i);
            if (t is null || t.Type == TokenConstants.EOF || t.Type == CobolLexer.DOT) return false;
            if (Word(t, "TO"))
                return Word(TokenStream.LT(i + 1), "LOCALE")
                    && (Word(TokenStream.LT(i + 2), "LC_ALL") || Word(TokenStream.LT(i + 2), "USER-DEFAULT"));
        }
        return false;
    }

    /// <summary>PICTURE Format 2 (locale) — the word after the picture character-string spells LOCALE (ISO
    /// §13.18.40.2 `PIC IS character-string-1 LOCALE [IS locale-name-1] SIZE IS integer-1`; kb/Work PB100, live at
    /// PB64 T6).
    /// <para>⛔ NOT EDITION-GATED (the ORDER TABLE precedent — <c>orderTableClause</c>): no data description
    /// clause begins with a user-defined word, so at '85 a word LOCALE immediately after a PIC_STRING can begin
    /// nothing else — there is no legal '85 reading to protect (unlike <see cref="localeClauseAhead"/>'s
    /// implementor-switch hazard, which does not exist inside a data description entry). Recognizing the phrase at
    /// every edition is what lets the ONE construct gate (<c>picture-locale-format2-2002</c>) answer below 2002
    /// with the explanatory introduction diagnostic instead of a raw ANTLR error at SIZE.</para></summary>
    protected bool pictureLocaleAhead() => Word(TokenStream.LT(1), "LOCALE");

    /// <summary>The §12.3.8.2 class-specifier / interface-specifier EXPANDS phrase is ahead (kb/Work PB759).
    /// EXPANDS is a §8.10 context-sensitive word ("class-specifier and interface-specifier of the REPOSITORY
    /// paragraph"), never reserved, so it is read as TEXT — the LOCALE / STEP discipline. Not edition-gated: the
    /// phrase can only follow a CLASS / INTERFACE specifier, whose own introduction gate answers below 2002, and
    /// no other repository entry begins with a user-defined word, so there is no '85 reading to protect.</summary>
    protected bool expandsAhead() => Word(TokenStream.LT(1), "EXPANDS");

    /// <summary>OCCURS … STEP integer-3 — the word in the STEP position spells STEP and an integer follows (ISO
    /// §13.18.38.2 Format 3, the report-writer OCCURS: <c>OCCURS [ integer-1 TO ] integer-2 TIMES [ DEPENDING ON
    /// data-name-1 ] [ STEP integer-3 ]</c>; kb/Work PB565).
    /// <para>STEP is a §8.10 CONTEXT-SENSITIVE word ("OCCURS clause"), never reserved, so — like LOCALE and
    /// ATTRIBUTE — it is read as TEXT rather than given a lexer token: a token would have to be added to the
    /// generated <c>_dataNameTokens</c> set to keep <c>STEP (1)</c> subscripting a user item named STEP, and that
    /// set is derived from the §8.9 RESERVED word table, which STEP is not in.</para>
    /// <para>The integer lookahead is what makes the predicate safe: nothing else in an OCCURS clause is a bare
    /// user word followed by an integer literal, so a data item legitimately named STEP can still be the
    /// DEPENDING operand or an ASCENDING KEY.</para></summary>
    protected bool occursStepAhead()
        => Word(TokenStream.LT(1), "STEP") && TokenStream.LT(2) is { } n && IsIntegerToken(n);

    /// <summary>True when the token is an unsigned integer literal — the STEP-phrase lookahead's second half.</summary>
    private static bool IsIntegerToken(IToken t)
        => t.Text.Length > 0 && Array.TrueForAll(t.Text.ToCharArray(), char.IsAsciiDigit);

    // ⛔ `orderTableAhead()` WAS HERE and is DELETED (kb/Work PB704). It read the word pair (ORDER, TABLE) by text
    // only because ORDER had no lexer token — and that missing token was itself the defect: a KEYWORD slot that
    // borrows `cobolWord` is refused by the §8.9 funnel's position-blind IDENTIFIER check, which is what made
    // `SORT … WITH DUPLICATES IN ORDER` a COBOLNET0901 at every edition from 2002. ORDER is now a token with a
    // `cobol-words.json` nameSlot row (the FORMAT precedent), so `orderTableClause` matches its own two keywords
    // and the funnel's OrderTableClause slot-0 exemption is gone too. The clause stays UNGATED by edition and
    // still precedes `implementorSwitchEntry`; `Grammar/Core/CobolSpecialNames.g4` carries the reasons.

    /// <summary>SET screen-name-1 ATTRIBUTE … (ISO §14.9.39.2 Format 6 — Annex A.4.2 item 24; kb/Work PB260) —
    /// a LEFT-EDGE predicate (LT(1) is the SET token itself): true when the word ATTRIBUTE stands somewhere in
    /// the receiver position, i.e. after a bounded walk over screen-name-1 and any qualifiers.
    /// <para>ATTRIBUTE is a §8.10 CONTEXT-SENSITIVE word ("SET statement"), never reserved, so it is not a lexer
    /// token — the same reason ORDER and LOCALE are read as text here.</para>
    /// <para>⛔ NOT EDITION-GATED, the ORDER TABLE precedent. `SET x ATTRIBUTE …` has no other reading at any
    /// edition: Format 1 requires TO, Format 2 requires UP/DOWN BY, and a bare `SET x y` is not a SET at all — so
    /// there is no COBOL-85 reading to protect, and recognizing the shape everywhere is what lets the named
    /// COBOLNET1707 refusal replace a raw parse error below 2002 as well as above it.</para></summary>
    protected bool setAttributeAhead()
    {
        for (int i = 2; i <= 12; i++)
        {
            var t = TokenStream.LT(i);
            if (t is null || t.Type == TokenConstants.EOF || t.Type == CobolLexer.DOT) return false;
            if (Word(t, "ATTRIBUTE")) return i > 2;   // i == 2 would mean SET *with no receiver*
        }
        return false;
    }

    /// <summary>True when the next token opens the ACCEPT-format-3 / DISPLAY-format-2 POSITIONING PHRASE (ISO
    /// §14.9.1.2 / §14.9.11.2: <c>[AT {|[LINE NUMBER …] [{COLUMN|COL} NUMBER …]|}]</c>) — the guard that stops
    /// the DISPLAY operand loop from swallowing it.
    /// <para>⛔ WHY THIS IS SAFE RATHER THAN A HEURISTIC. AT, LINE and COLUMN are §8.9 reserved words at EVERY
    /// edition, so none of them can be a user-defined word and none can legally begin a DISPLAY operand; the
    /// plural/abbreviated spellings COL, COLS and COLUMNS are reserved only from 2002, so they are tested
    /// through <see cref="reservedHere"/> and stay ordinary user words at COBOL-85 (where <c>DISPLAY COLS</c> is
    /// legal and must keep binding as a one-operand device DISPLAY). Without this, `DISPLAY SG COLUMN 5` bound
    /// as a three-operand device DISPLAY while `DISPLAY SG COLUMN NUMBER 5` was a parse error — one construct,
    /// two non-diagnoses (kb/Work PB260).</para></summary>
    protected bool screenPositionAhead()
    {
        var t = TokenStream.LT(1);
        if (t is null) return false;
        return t.Type switch
        {
            CobolLexer.AT or CobolLexer.LINE or CobolLexer.COLUMN => true,
            CobolLexer.COL or CobolLexer.COLS or CobolLexer.COLUMNS => reservedHere(t.Text),
            _ => false,
        };
    }

    protected bool localeClauseAhead()
    {
        if (!Word(TokenStream.LT(1), "LOCALE")) return false;
        // LOCALE <locale-name> [IS] <external-locale-name | literal> — the second token is the locale-name, a WORD,
        // so the '85 implementor-switch shapes `LOCALE IS x` / `LOCALE ON|OFF STATUS …` (IS/ON/OFF are tokens) are
        // excluded. ⚠ NOT edition-gated since kb/Work PB64 T1 (it was, because "SPECIAL-NAMES. LOCALE IS FOO." is a
        // legal '85 switch entry — but that shape is excluded HERE by the word test, and `LOCALE FR IS "fr_FR"` has
        // no '85 reading at all): recognizing the clause at every edition is what lets the ONE construct gate
        // (special-names-locale-2002, VisitLocaleClause) answer below 2002 with the explanatory introduction
        // diagnostic instead of a parse error at the clause's own literal — the ORDER TABLE precedent.
        return TokenStream.LT(2) is { } second
            && second.Type != CobolLexer.IS && second.Type != CobolLexer.ON && second.Type != CobolLexer.OFF
            && second.Type != CobolLexer.DOT && second.Type != TokenConstants.EOF
            && !Word(second, "IS");
    }

    /// <summary>
    /// True when the current token spells a reserved-as-facility keyword AT THE TARGETED EDITION — i.e. it can
    /// only be the (unsupported) facility verb here, never a user-defined word. Gates the recognize-and-name
    /// statement arms for the facilities WiseOwl COBOL does not implement: MCS SEND/RECEIVE (ISO §14.9.31/§14.9.38,
    /// Annex A.3 item 4), COMMIT/ROLLBACK (A.3 items 6–7), VALIDATE (§14.9.50).
    /// <para>
    /// WHY A PREDICATE AT ALL, given these are now hard lexer tokens: their §8.9 reservation is NON-MONOTONIC —
    /// RECEIVE/SEND/END-RECEIVE are reserved at 85, USER WORDS at 2002/2014, and re-reserved at 2023;
    /// COMMIT/ROLLBACK/END-SEND are reserved at 2023 only; VALIDATE is a user word at 85; MESSAGE runs the other
    /// way (reserved 85/2002, user word 2014/2023). The tokens are admitted to the <c>cobolWord</c> nameSlot
    /// funnel so they remain legal user names wherever unreserved, and this predicate stops the STATEMENT arm
    /// from firing at those editions — so <c>01 RECEIVE PIC X.</c> at --std 2002 stays a data item.
    /// </para>
    /// <para>
    /// ⛔ The arms this gates are KEYWORD-TOKEN-LED, never IDENTIFIER-led. An IDENTIFIER-led <c>statement</c>
    /// alternative poisons ANTLR's ALL(*) boolean-factor prediction DFA and regresses
    /// <c>COMPUTE R = B-NOT A.</c> at every edition — empirically proven and reverted (DEVLOG 903). A predicate
    /// on a distinct leading token is unreachable during arithmetic/boolean prediction and cannot poison it.
    /// </para>
    /// Reads the SAME <see cref="ReservedWords"/> table the §8.9 funnel uses, so recognition and reservation
    /// can never diverge. Read-only — safe for ANTLR's repeated speculative prediction calls.
    /// </summary>
    /// <summary>True when <paramref name="keyword"/> is a §8.9 reserved word at the compile edition — the
    /// cobolWord exclusion predicate (kb/Work PB137): a word the edition reserves cannot be a user-defined
    /// word, so the alternative that would absorb it into an operand list must not match.</summary>
    /// <para>Through the &gt;&gt;COBOL-WORDS resolution (kb/Work PB250): an UNDEFINE'd word is not reserved
    /// here at any edition (§7.3.10.4 GR3), and an EQUATEd synonym is reserved exactly where its canonical
    /// word is (GR2).</para>
    protected bool reservedHere(string keyword)
        => Canonical(keyword) is { } w && (ReservedWords.Find(w)?.IsReservedAt(Edition.Year) ?? false);

    /// <summary>⛔ THE RESERVATION GATE ON THE USER-DEFINED-WORD SLOT (kb/Work PB693) — true when this compile
    /// ADMITS <paramref name="keyword"/> to <c>cobolWord</c>. The generated <c>cobolWord</c> alternatives carry
    /// <c>{userWordHere("W")}?</c> and their <c>reservedGatedWord</c> twins the exact inverse, for every name-slot
    /// word ISO §8.9 reserves at some edition (§8.3.2.1 rule 1: "Reserved words shall not be used as user-defined
    /// words or system-names").
    /// <para>It is <see cref="reservedHere"/> plus ONE thing: the MIGRATION MODE, and the whole decision lives in
    /// <see cref="ReservedWordSet.AdmitsAsUserWord"/> — the assembly that owns §8.9 — so this gate and the funnel
    /// that reports COBOLNET0901 can never disagree about which occurrences are names. <c>--permissive</c>
    /// "accepts constructs the targeted edition REMOVED, warning instead of rejecting", and a word the edition
    /// ADDED to §8.9 is precisely such a removal: the funnel computes <c>ConstructAvailability.Removed</c> for it,
    /// <see cref="EditionSeverityPolicy"/> downgrades the COBOLNET0901 to a warning, and this compile must keep
    /// PARSING the word as a user-defined word or the whole class becomes a parse error that no severity policy
    /// can downgrade.</para>
    /// <para>⛔ BUT THE EXEMPTION IS NOT A BLANKET <c>Edition.Permissive ||</c> (kb/Work PB792). A word §8.9
    /// reserves at this edition AND at every older edition this compiler targets — COLUMN and DESTINATION are
    /// reserved at all four — was never a user-defined word anywhere, so migration mode has NO pre-removal
    /// reading to restore; its verdict is <c>NotYetIntroduced</c>, an error on both axes. Admitting it here let
    /// every greedy operand list absorb it: <c>03 VALUE "DETAIL LINE " COLUMN 20 PIC X(12).</c> (NIST RW104A card
    /// 024500, legal because §13.15.3 SR2 says "All other clauses may be written in any order") parsed COLUMN and
    /// 20 as VALUE operands, dropped the §13.18.14 COLUMN clause, and printed the field in the wrong place with
    /// no diagnostic at all.</para>
    /// <para>A SEPARATE predicate from <see cref="reservedHere"/> rather than a permissive clause inside it:
    /// <see cref="facilityWord"/> and the SPECIAL-NAMES CRT/CURSOR clause guards ask the OTHER question — "is this
    /// token the reserved keyword here" — and must keep recognizing their clauses under <c>--permissive</c>.</para></summary>
    internal bool userWordHere(string keyword)
        // ReservedWordSet.Default, not a composed overlay: Canonical() has ALREADY applied this compilation
        // group's >>COBOL-WORDS directive (an UNDEFINE'd word returns null — a user word at every edition),
        // exactly as reservedHere does, so composing the overlay twice would double-count it.
        => Canonical(keyword) is not { } w || ReservedWordSet.Default.AdmitsAsUserWord(w, Edition);

    /// <summary>The reservation-gated words this parse matched in a DEFINITION slot (the generated
    /// <c>reservedGatedWord</c> rule's action, <see cref="gatedDeclaration"/>) that this compile ADMITS as user-defined
    /// words (<see cref="userWordHere"/>) — the input of the token-level §8.9 gate, <see cref="ReservationGateRewriter"/>
    /// (kb/Work PB655). Upper-case, canonical spelling. Cleared by <see cref="Reset"/>, so each parse pass reports its own.</summary>
    internal IReadOnlyCollection<string> FreeGatedDeclarations => (IReadOnlyCollection<string>?)_freeGatedDeclarations ?? [];

    private HashSet<string>? _freeGatedDeclarations;

    /// <summary>The <c>reservedGatedWord</c> action: <paramref name="word"/> was DECLARED — every use of that rule is a
    /// definition slot (VersionConformancePass.VisitReservedGatedWord relies on the same fact). Recorded only when
    /// §8.9 leaves it free here; where §8.9 reserves it the match stands and the funnel answers COBOLNET0901.
    /// Actions never run during prediction.</summary>
    protected void gatedDeclaration(IToken? word)
    {
        if (word?.Text is { Length: > 0 } t && userWordHere(t))
            (_freeGatedDeclarations ??= new(StringComparer.Ordinal)).Add(t.ToUpperInvariant());
    }

    /// <summary>The token-level gate's SECOND witness (kb/Work PB655): a syntax error whose offending token is a
    /// reservation-gated word §8.9 leaves free here. The keyword reading failed to parse, and the only other reading
    /// ISO gives the word at this edition is a user-defined word — so it is retyped exactly as a declared one is.
    /// This is what reaches a name the program introduces in a slot that does not offer <c>reservedGatedWord</c>
    /// (a SPECIAL-NAMES class-name, an index-name, …) and every reference to it. Called by the syntax-error
    /// listener of the authoritative pass.</summary>
    internal void NoteGatedOffender(IToken? offending)
    {
        if (offending is not null && CobolLexer.IsReservationGated(offending.Type)) gatedDeclaration(offending);
    }

    public override void Reset()
    {
        base.Reset();
        _freeGatedDeclarations = null;
        _declaredNames = null;
    }

    /// <summary>⛔ WHICH READING WINS WHEN A WORD COULD BE EITHER (kb/Work PB805 + PB655) — true when the lookahead
    /// word, standing where a greedy operand list could take one more <c>cobolWord</c>, can instead be read as a
    /// KEYWORD of whatever follows the list in this construct. Every non-IDENTIFIER <c>cobolWord</c> alternative
    /// carries <c>{!keywordContinuesHere()}?</c> (generated, <c>scripts/gen-cobol-words.ps1</c>), so the list ENDS
    /// there and the keyword reading wins.
    /// <para>THE RULE. §8.3.2.1 3): "Context-sensitive words may be used as user-defined words and system-names in
    /// contexts other than the language construct in which they are defined" — inside that construct the word IS
    /// its keyword (§5.2.2/§5.2.3), and a word §8.9 reserves is never a user-defined word at all (§8.3.2.1 1)).
    /// So a word that takes its keyword reading here (<see cref="IsKeywordReadingHere"/>) and that the enclosing
    /// construct can read as its next keyword is not an operand of the list before it.</para>
    /// <para>WHY DERIVED, NOT LISTED. The question used to be answered per word and per site — PROPERTY (two
    /// VALUE-list predicates), DEFAULT (INITIALIZE), five phrase words (DELETE FILE) — and for every other word by
    /// ALL(*) lookahead luck: <c>01 G VALUE N"AB" GROUP-USAGE NATIONAL.</c> compiled clean strict and drew three
    /// errors under <c>--permissive</c>, because the migration mode admits GROUP-USAGE and <c>NATIONAL</c> kept the
    /// swallowed reading viable to the period. Here the "what follows" set is COMPUTED from the ATN at the loop's
    /// exit, through the live rule-invocation stack, so every list, every construct and every future keyword is
    /// covered by construction.</para>
    /// <para>MECHANICS. The <c>cobolWord</c> predicates sit at the left edge of every loop body that begins with a
    /// name, so ANTLR hoists them into the LOOP DECISION: during that prediction <see cref="Recognizer{S,A}.State"/>
    /// is the loop's decision state and <see cref="Parser.Context"/> its rule context. Anywhere else (the
    /// alternative decision inside <c>cobolWord</c> itself, a declaration slot, an optional-phrase decision) this
    /// returns false and the ordinary admission stands. The follow walk treats <c>cobolWord</c> and
    /// <c>reservedGatedWord</c> as NAME positions, not keyword positions — otherwise the next operand of an
    /// enclosing list would count as "a keyword that follows".</para></summary>
    protected bool keywordContinuesHere()
    {
        var atn = Interpreter.atn;
        if (State < 0 || State >= atn.states.Count || atn.states[State] is not Antlr4.Runtime.Atn.DecisionState d)
            return false;
        Antlr4.Runtime.Atn.ATNState? exit = null;
        for (int i = 0; i < d.NumberOfTransitions && exit is null; i++)
            if (d.Transition(i).target is Antlr4.Runtime.Atn.LoopEndState le) exit = le;
        if (exit is null) return false;
        int la = TokenStream.LA(1);
        return IsKeywordReadingHere(la) && KeywordFollows(atn, exit, Context, la);
    }

    /// <summary>Whether the lookahead word takes its KEYWORD reading in an operand list (kb/Work PB805/PB655).
    /// True when §8.9 reserves the word at this edition (it is never a user-defined word, §8.3.2.1 1) — it reaches
    /// <c>cobolWord</c> only through the migration mode), or when it has no §8.9 row at all (a §8.10 context-
    /// sensitive word: a keyword inside its construct, §8.3.2.1 3)). A word §8.9 leaves FREE at this edition —
    /// GOBACK or END-DISPLAY at COBOL-85, ALTER at 2002 — is a user-defined word exactly when the program
    /// DECLARES it (<see cref="declareName"/>): then ISO gives the source one reading and it is one more operand.
    /// Undeclared, the user-word reading could only be an unresolvable reference, so the keyword reading wins and
    /// the union grammar's named edition gate ("END-DISPLAY requires COBOL-2002") answers instead.
    /// ⚠ DETERMINATION: GnuCOBOL (the survey model) drops such a word from its per-standard reserved list and so
    /// always takes the user-word reading; this keeps its acceptance of every declared use and differs only in
    /// the diagnostic for an undeclared one. Through the &gt;&gt;COBOL-WORDS overlay (<see cref="Canonical"/>).</summary>
    private bool IsKeywordReadingHere(int tokenType)
    {
        string? literal = Vocabulary.GetLiteralName(tokenType);
        if (literal is null || literal.Length < 3) return false;
        if (Canonical(literal[1..^1].ToUpperInvariant()) is not { } w) return false;
        var row = ReservedWords.Find(w);
        return row is null || row.IsReservedAt(Edition.Year) || !(_declaredNames?.Contains(w) ?? false);
    }

    private HashSet<string>? _declaredNames;

    /// <summary>Records a user-defined word the program DECLARES (a data-name, a file-name) — called by grammar
    /// actions, which never run during prediction. Read by <see cref="IsKeywordReadingHere"/>.</summary>
    protected void declareName(IToken? name)
    {
        if (name?.Text is { Length: > 0 } t) (_declaredNames ??= new(StringComparer.Ordinal)).Add(t.ToUpperInvariant());
    }

    /// <summary>The LL(1) follow walk behind <see cref="keywordContinuesHere"/>: can <paramref name="tokenType"/>
    /// be matched as a TOKEN (not as a name) starting at <paramref name="start"/>, following rule returns through
    /// <paramref name="outer"/> once the local invocation stack empties.</summary>
    private static bool KeywordFollows(Antlr4.Runtime.Atn.ATN atn, Antlr4.Runtime.Atn.ATNState start,
                                       RuleContext? outer, int tokenType)
    {
        var visited = new HashSet<(int, ReturnFrame?, RuleContext?)>();
        var work = new Stack<(Antlr4.Runtime.Atn.ATNState S, ReturnFrame? Local, RuleContext? Outer)>();
        work.Push((start, null, outer));
        while (work.Count > 0)
        {
            var (s, local, ctx) = work.Pop();
            if (!visited.Add((s.stateNumber, local, ctx))) continue;
            if (s is Antlr4.Runtime.Atn.RuleStopState)
            {
                if (local is not null) work.Push((local.Follow, local.Next, ctx));
                else if (ctx is { invokingState: >= 0 } && atn.states[ctx.invokingState].Transition(0)
                             is Antlr4.Runtime.Atn.RuleTransition back)
                    work.Push((back.followState, null, ctx.Parent));
                continue;
            }
            for (int i = 0; i < s.NumberOfTransitions; i++)
            {
                var t = s.Transition(i);
                switch (t)
                {
                    case Antlr4.Runtime.Atn.RuleTransition rt:
                        if (rt.target.ruleIndex is CobolParserCore.RULE_cobolWord or CobolParserCore.RULE_reservedGatedWord)
                            continue;   // a NAME position — never the keyword reading
                        // ANTLR's own LL1Analyzer guard: a rule already on the local stack is not re-entered
                        // at the same lookahead (an epsilon cycle through a rule would never terminate).
                        bool onStack = false;
                        for (var f = local; f is not null && !onStack; f = f.Next) onStack = f.RuleIndex == rt.target.ruleIndex;
                        if (!onStack) work.Push((rt.target, new ReturnFrame(rt.followState, rt.target.ruleIndex, local), ctx));
                        break;
                    case Antlr4.Runtime.Atn.WildcardTransition or Antlr4.Runtime.Atn.NotSetTransition:
                        break;         // matches "anything": no keyword is named there
                    default:
                        if (t.IsEpsilon) work.Push((t.target, local, ctx));
                        else if (t.Matches(tokenType, TokenConstants.MinUserTokenType, atn.maxTokenType)) return true;
                        break;
                }
            }
        }
        return false;
    }

    /// <summary>One frame of the follow walk's LOCAL rule-invocation stack (the rules it descended into itself,
    /// above the parser's live <see cref="RuleContext"/> chain). A reference type so the visited set keys on
    /// frame identity.</summary>
    private sealed record ReturnFrame(Antlr4.Runtime.Atn.ATNState Follow, int RuleIndex, ReturnFrame? Next);

    /// <summary>The §8.9 message for a syntax error whose offending token is a RESERVATION-GATED word, or null
    /// when the error is about something else (kb/Work PB693).
    /// <para>The gate removes such a word from <c>cobolWord</c> at the editions §8.9 reserves it, which is right —
    /// nothing may absorb it into an operand list — but it also means a REFERENCE to the word has no alternative
    /// left to match, and the bound-tree funnel never runs on a source that failed to parse. Without this the
    /// user's answer to <c>DISPLAY CONSTANT.</c> at <c>--std 2002</c> degrades from "'CONSTANT' is a reserved word
    /// in COBOL-2002" to "no viable alternative at input 'CONSTANT'" — the cause named, then unnamed.</para>
    /// <para>Narrow by construction: the token type must be in the GENERATED gate set (so an ordinary syntax error
    /// on any other reserved keyword keeps its own message), and the word must still <c>RejectsAt</c> the compile
    /// edition — the same conservative high-confidence predicate the funnel reports on, composed with this
    /// group's <c>&gt;&gt;COBOL-WORDS</c> overlay. A PERMISSIVE compile reaches here exactly where the migration
    /// mode has nothing to restore — a word §8.9 reserves at this edition and at every older one (kb/Work PB792,
    /// <see cref="ReservedWordSet.AdmitsAsUserWord"/>); for a word an edition merely ADDED the gate still stands
    /// down and this is unreachable.</para></summary>
    internal string? ReservedUserWordViolation(IToken? offendingSymbol)
    {
        if (offendingSymbol is null || !CobolLexer.IsReservationGated(offendingSymbol.Type)) return null;
        string? w = Canonical(offendingSymbol.Text);
        // ReservedWordSet.Default is the generated §8.9 table with no >>COBOL-WORDS overlay — the right set here:
        // Canonical() has ALREADY applied this group's directive (an UNDEFINE'd word returns null, an EQUATEd
        // synonym its canonical spelling), so composing the overlay twice would double-count it.
        return w is not null && ReservedWordSet.Default.RejectsAt(w, Edition.Year)
            ? ReservedWordSet.UserWordViolationMessage(w, Edition.Year)
            : null;
    }

    protected bool facilityWord(string keyword)
    {
        var t = CurrentToken;
        if (t is null) return false;
        if (!Word(t, keyword)) return false;
        return reservedHere(keyword);
    }

    protected CobolParserCoreBase(ITokenStream input) : base(input) { }
    protected CobolParserCoreBase(ITokenStream input, TextWriter output, TextWriter errorOutput)
        : base(input, output, errorOutput) { }

    /// <summary>
    /// Returns true if the current token is the first non-whitespace token on its line.
    /// Used to prevent stray identifiers (like LINES after WRITE ADVANCING)
    /// from being misinterpreted as paragraph names.
    /// </summary>
    protected bool IsAtLineStart()
    {
        var token = CurrentToken;
        if (token == null) return false;

        // Check if this token's column is 0, or if the previous token
        // is on a different line
        int tokenLine = token.Line;
        int tokenIndex = token.TokenIndex;

        if (tokenIndex <= 0) return true;

        var prevToken = TokenStream.Get(tokenIndex - 1);
        // A debugging line that is source (kb/Work PB1705) begins with its hidden DEBUG_LINE marker, which stands on the
        // line but is no program text: the token after it is the first token of the line, so a paragraph name written on a
        // debugging line (DB101A's `DDEBUG-LINE-TEST-03-A.`) begins its paragraph.
        return prevToken.Type == CobolLexer.DEBUG_LINE || prevToken.Line < tokenLine;
    }

    /// <summary>
    /// Predicate for the bare (adjective-less) INSPECT TALLYING count phrase. An ALL or
    /// LEADING adjective is transitive across the operands that follow it (ISO 1989:1985
    /// 14.9.22 GR 10), so "FOR LEADING ""S"" ""S"" ""T""" lists three operands under one
    /// counter. But an identifier followed by FOR is the NEXT tallying counter (§14.9.22.2 writes
    /// <c>identifier-2 FOR</c>), not a transitive operand. Returning false there stops the count-phrase
    /// repetition so the identifier begins a new inspectTallyingItem instead of being swallowed as a pattern.
    /// <para>⛔ THE WHOLE REFERENCE, NOT ITS FIRST TOKEN (kb/Work PB1125). The test used to be <c>LA(2) != FOR</c>, which
    /// sees the token after the first one only: a SUBSCRIPTED, QUALIFIED or REFERENCE-MODIFIED next counter
    /// (<c>C(1) FOR</c>, <c>M OF G FOR</c>) has <c>(</c> or <c>OF</c> at LA(2), so it was taken as one more
    /// operand of the previous counter and its FOR clause re-attached to THAT counter — a silent wrong count at every
    /// edition. The scan steps over the reference's own extent — each qualifier (<c>OF|IN word</c>) and each
    /// parenthesised subscript / reference-modification group, whose tokens are SUBSCRIPT-mode and close at the
    /// matching SUB_RPAREN — and asks whether FOR follows. A literal or figurative operand has no such extent, so it
    /// is still bare.</para></summary>
    protected bool IsBareInspectOperand()
    {
        // A phrase ADJECTIVE begins the NEXT phrase — `ALL "B"` after `ALL "A"` is a new ALL phrase, never the
        // figurative constant ALL "B" as one more operand (which SR3 bars from literal-1 anyway).
        if (BeginsInspectPhrase(TokenStream.LA(1))) return false;
        int t = 2;   // LA(1) is the candidate operand's first token
        while (true)
        {
            int k = TokenStream.LA(t);
            // GROUPING-PAREN-ONLY: the plain LPAREN here is a data reference's own SUBSCRIPT / reference-modification
            // group. A function-argument list (FNARG_LPAREN) belongs to a function-identifier, which is never a
            // tallying counter, so it has no extent this scan should step over.
            if (k == CobolLexer.LPAREN)
            {
                int depth = 1;
                for (t++; depth > 0 && TokenStream.LA(t) != Antlr4.Runtime.TokenConstants.EOF; t++)
                {
                    int u = TokenStream.LA(t);
                    if (u == CobolLexer.SUB_LPAREN) depth++;
                    else if (u == CobolLexer.SUB_RPAREN) depth--;
                }
                continue;
            }
            if ((k == CobolLexer.OF || k == CobolLexer.IN) && TokenStream.LA(t + 1) != Antlr4.Runtime.TokenConstants.EOF) { t += 2; continue; }
            break;
        }
        return TokenStream.LA(t) != CobolLexer.FOR;
    }

    /// <summary>The words that begin an INSPECT tallying / replacing PHRASE (§14.9.22.2): the adjectives, which are
    /// reserved words and so can never be the first token of a bare operand — except that ALL also begins the
    /// figurative constant <c>ALL literal</c>, which is what makes the test necessary.</summary>
    private static bool BeginsInspectPhrase(int token) =>
        token is CobolLexer.ALL or CobolLexer.LEADING or CobolLexer.CHARACTERS or CobolLexer.FIRST or CobolLexer.TRAILING;

    /// <summary>Predicate for a bare (adjective-less) INSPECT REPLACING pair — the operands AFTER the first under one
    /// ALL / LEADING / FIRST adjective (§14.9.22.4 GR16, transitive). A pair begins with an operand, and an
    /// adjective (or CHARACTERS) begins the NEXT item, so the pair loop ends there. Without this an
    /// <c>ALL "A" BY "Z" ALL "B" BY "Y"</c> would read the second ALL as the figurative constant <c>ALL "B"</c>
    /// starting one more pair.</summary>
    protected bool IsBareInspectReplacingPair() => !BeginsInspectPhrase(TokenStream.LA(1));

    /// <summary>Predicate for the TO-less operand of the three §13.18.60.2 pointer usages (kb/Work PB848): TO is
    /// an optional word there (§5.2.3 — not underlined on the printed folio 503), so the word after
    /// POINTER / FUNCTION-POINTER / PROGRAM-POINTER is the type-name or prototype-name — UNLESS it is a word that
    /// may legitimately FOLLOW the usage without any operand: a USAGE-clause tail phrase (HIGH-ORDER-RIGHT, the
    /// SIGN phrases — the ones a following semantic check rejects by name) or the next data-description clause.
    /// <c>cobolWord</c>'s reservation gate does not settle that on its own: it exempts the §15 function names
    /// (BIT and NATIONAL are also bare USAGE keywords) and admits a keyword wherever it is not reserved (an older
    /// edition, <c>--permissive</c>). Both failures were MEASURED — <c>USAGE POINTER BIT</c> and
    /// <c>USAGE POINTER HIGH-ORDER-RIGHT</c> each bound the word as a type-name. A plain IDENTIFIER is always
    /// the operand (the only thing it could otherwise begin is the unrecognizedClause error production).</summary>
    protected bool pointerOperandHere()
    {
        int t = TokenStream.LA(1);
        return t == CobolLexer.IDENTIFIER || !UsageFollowSet(Atn).Contains(t);
    }

    private static Antlr4.Runtime.Misc.IntervalSet? s_usageFollow;

    /// <summary>FOLLOW(<c>usageKeyword</c>) — every token that can come next after a usage keyword in ANY context
    /// that invokes it — read off the GENERATED ATN rather than listed by hand: the union, over every call site of
    /// the rule, of what the ATN can match after it, climbing to the caller's own call sites wherever the caller
    /// can end there (context-free FOLLOW). A clause added to the closed §13.16.2 format, or a phrase added to the
    /// USAGE clause, is excluded from the TO-less pointer operand the moment the grammar gains it
    /// (<see cref="pointerOperandHere"/>; pinned by PointerUsageOperandDriftTests). Computed once per process.</summary>
    public static Antlr4.Runtime.Misc.IntervalSet UsageFollowSet(Antlr4.Runtime.Atn.ATN atn)
        => s_usageFollow ??= Follow(atn, CobolParserCore.RULE_usageKeyword);

    /// <summary>The context-free FOLLOW set of <paramref name="rule"/> over <paramref name="atn"/>.</summary>
    internal static Antlr4.Runtime.Misc.IntervalSet Follow(Antlr4.Runtime.Atn.ATN atn, int rule)
    {
        var result = new Antlr4.Runtime.Misc.IntervalSet();
        var seen = new HashSet<int>();
        var work = new Stack<int>([rule]);
        while (work.Count > 0)
        {
            int r = work.Pop();
            if (!seen.Add(r)) continue;
            var start = atn.ruleToStartState[r];
            foreach (var state in atn.states)
            {
                if (state is null) continue;
                for (int i = 0; i < state.NumberOfTransitions; i++)
                {
                    if (state.Transition(i) is not Antlr4.Runtime.Atn.RuleTransition rt || rt.target != start) continue;
                    var next = atn.NextTokens(rt.followState);
                    if (next.Contains(Antlr4.Runtime.TokenConstants.EPSILON))
                        work.Push(state.ruleIndex);   // the caller can end here — its own followers follow too
                    result.AddAll(next);
                }
            }
        }
        result.Remove(Antlr4.Runtime.TokenConstants.EPSILON);
        return result;
    }

    /// <summary>
    /// The DISPLAY directive's <c>PARAMETER compilation-variable-name-1</c> operand (ISO §7.3.12.2, kb/Work PB807): the
    /// word PARAMETER followed by a word. PARAMETER is a word of the lexer (a compiler-directive word, §8.12, so never a
    /// compilation-variable-name, §7.3.11.3 SR1), told apart here by its text. Read-only over the token stream
    /// (<see cref="Antlr4.Runtime.ITokenStream.LA"/>).
    /// </summary>
    protected bool parameterPhraseAhead()
        => TokenStream.LT(1) is { Type: CobolLexer.IDENTIFIER } word
           && word.Text.Equals("PARAMETER", StringComparison.OrdinalIgnoreCase)
           && TokenStream.LA(2) == CobolLexer.IDENTIFIER;

    /// <summary>
    /// COBOL-2002 boolean-condition discriminator (ISO §8.8.4.2.2 / §8.8.4.3): true when a boolean OPERATOR
    /// (B-AND / B-OR / B-XOR / B-NOT / a B-SHIFT-*) appears in the CURRENT condition ahead of the parse position,
    /// before any condition boundary — or a parenthesized boolean LITERAL, the one operator-free boolean expression
    /// the token stream can identify (<see cref="ParenRunOpensOnBoolLiteral"/>). It is the ONE boolean-operand
    /// discriminator of every <c>{boolExprAhead()}?</c> site, the compile-time directive fragments
    /// (<c>compileTimeOperand</c> / <c>cceRelationOrBoolean</c>) included, so the runtime and compile-time lanes
    /// recognize the same boolean expressions. This gates a dedicated <c>primaryCondition</c> alternative WITHOUT touching the
    /// shared <c>comparisonExpression</c> rule (whose modification regressed subscript/ref-mod comparisons at
    /// 2002+, DEVLOG 621) — a normal comparison (no B-op ahead) returns false and falls to comparisonExpression
    /// unchanged. The scan stops at the condition's end: a period, the logical connectives (AND/OR/THEN/ELSE),
    /// a WHEN / END-* / UNTIL / VARYING, or any statement-starting keyword (so it never crosses into an IF body),
    /// and is window-capped. Read-only over the token stream — safe for ANTLR's repeated prediction calls.
    /// </summary>
    protected bool boolExprAhead()
    {
        // Fires at ALL editions (superset parse — residue migration #2): a below-2002 boolean condition must PARSE so
        // the bind-time gate can name COBOLNET0900, rather than fail as a generic parse error. A B-op-free comparison
        // still returns false and falls to comparisonExpression unchanged (the shared rule is untouched — DEVLOG 621).
        int prev = 0;   // token immediately BEFORE position i (0 = condition start — a leading binary B-op has no left operand)
        for (int i = 1; i <= 96; i++)
        {
            int t = TokenStream.LA(i);
            switch (t)
            {
                // A BINARY boolean operator is genuine ONLY with a completed operand immediately before it. A leading /
                // operand-less occurrence is a §8.9 user data-name below 2002 (IF B-AND = 5) — NOT the operator, so it
                // must fall to the normal comparison unchanged (else a plain comparison mis-gates as boolean, and RETRY
                // #4's sibling mis-fire class returns). Below 2002 B-AND/B-OR/B-XOR are legal user words; at ≥2002 the
                // §8.9 funnel already reserves them, so a leading occurrence is a name-slot error either way — never here.
                // The four boolean SHIFT operators (§8.8.2 rule 8, 2023) are NEVER legal user words (absent from
                // _dataNameTokens), so a shift token always IS the operator — detecting it here recognizes a shift-only
                // boolean expression (e.g. `A B-SHIFT-L 2`), which no binary/unary B-op precedes.
                case CobolLexer.B_AND:
                case CobolLexer.B_OR:
                case CobolLexer.B_XOR:
                case CobolLexer.B_SHIFT_L:
                case CobolLexer.B_SHIFT_R:
                case CobolLexer.B_SHIFT_LC:
                case CobolLexer.B_SHIFT_RC:
                    if (IsBoolOperandTerm(prev)) return true;
                    break;
                // UNARY prefix B-NOT is genuine when a boolean operand can immediately FOLLOW (IF B-NOT A), not when it
                // heads a comparison as a user data-name (IF B-NOT = 5).
                case CobolLexer.B_NOT:
                    if (IsBoolOperandStart(TokenStream.LA(i + 1))) return true;
                    break;
                // An OPERATOR-FREE parenthesized boolean literal — `(B"101")` — is a boolean expression with no
                // B-operator in it at all (kb/Work PB1370; see ParenRunOpensOnBoolLiteral). GROUPING-PAREN-ONLY: a
                // function argument list (FNARG_LPAREN) is an operand's own parenthesis, never a boolean group.
                case CobolLexer.LPAREN:
                    if (ParenRunOpensOnBoolLiteral(i)) return true;
                    break;
                // ── Condition boundaries: no B-operator can belong to THIS condition past here ──
                case CobolLexer.DOT:
                case CobolLexer.AND:
                case CobolLexer.OR:
                case CobolLexer.THEN:
                case CobolLexer.ELSE:
                case CobolLexer.WHEN:
                case CobolLexer.END_IF:
                case CobolLexer.END_PERFORM:
                case CobolLexer.END_EVALUATE:
                case CobolLexer.END_SEARCH:
                case CobolLexer.UNTIL:
                case CobolLexer.VARYING:
                case CobolLexer.TIMES:
                case TokenConstants.EOF:
                // Statement-starting keywords — the condition ends where the IF/WHEN body begins, so the scan
                // must never cross into a body that might itself contain a boolean COMPUTE.
                case CobolLexer.ACCEPT: case CobolLexer.ADD: case CobolLexer.ALLOCATE: case CobolLexer.CALL:
                case CobolLexer.CANCEL: case CobolLexer.CLOSE: case CobolLexer.COMPUTE: case CobolLexer.CONTINUE:
                case CobolLexer.DELETE: case CobolLexer.DISPLAY: case CobolLexer.DIVIDE: case CobolLexer.EVALUATE:
                case CobolLexer.EXIT: case CobolLexer.FREE: case CobolLexer.GOBACK: case CobolLexer.GO:
                case CobolLexer.INITIALIZE: case CobolLexer.INSPECT: case CobolLexer.INVOKE: case CobolLexer.MERGE:
                case CobolLexer.MOVE: case CobolLexer.MULTIPLY: case CobolLexer.NEXT: case CobolLexer.OPEN:
                case CobolLexer.PERFORM: case CobolLexer.RAISE: case CobolLexer.READ: case CobolLexer.RELEASE:
                case CobolLexer.RESUME: case CobolLexer.RETURN: case CobolLexer.REWRITE: case CobolLexer.SEARCH:
                case CobolLexer.SET: case CobolLexer.SORT: case CobolLexer.START: case CobolLexer.STOP:
                case CobolLexer.STRING: case CobolLexer.SUBTRACT: case CobolLexer.UNSTRING: case CobolLexer.WRITE:
                case CobolLexer.IF:
                    return false;
            }
            prev = t;
        }
        return false;
    }

    /// <summary>Are the next <paramref name="count"/> tokens ONE character-string — each starting at the character
    /// after the previous one's last (no space, no hidden-channel token between them)? The comma-decimal literals
    /// <c>123,45</c> and <c>,45</c> are assembled from tokens (§8.3.3.3.2), and a space before the comma begins a
    /// NEW character-string (§8.3.5). A left-edge predicate, so it steers prediction (kb/Work PB1446).</summary>
    protected bool tokensAreContiguous(int count) => TokensAreContiguousFrom(1, count);

    /// <summary><see cref="tokensAreContiguous"/> for the <paramref name="count"/> tokens starting at
    /// <c>LT(<paramref name="first"/>)</c>.</summary>
    private bool TokensAreContiguousFrom(int first, int count)
    {
        for (int i = first; i < first + count - 1; i++)
        {
            var a = TokenStream.LT(i);
            var b = TokenStream.LT(i + 1);
            if (a is null || b is null || a.StopIndex + 1 != b.StartIndex) return false;
        }
        return true;
    }

    /// <summary>⛔ A NUMERIC LITERAL THAT IS THE LEFT OPERAND OF AN ARITHMETIC OPERATOR IS NOT A STANDALONE ARGUMENT
    /// (kb/Work PB1135, decision R59). Every USING-argument list (CALL §14.9.4.2, INVOKE §14.9.23.2, the inline
    /// method invocation §8.4.3.4.2) prints the keyword-less operand brace
    /// <c>{ arithmetic-expression-1 | boolean-expression-1 | identifier | literal | OMITTED }</c>, and an
    /// argument list is juxtaposition, so <c>5 + 1</c> is either the ONE expression <c>5 + 1</c> or the two
    /// arguments <c>5</c> and <c>+ 1</c>. The operator tokens of the default lexer mode carry no spacing, but a
    /// sign separated from its digits is not a literal at all (§8.3.3.3.2 SR2 — it "shall appear as the leftmost
    /// character of the literal") and an arithmetic operator is "preceded by a space and followed by a space"
    /// (§8.7.1), so a <c>+ 1</c> after an operand is the binary operator, never a new argument: the expression
    /// wins. The test is on the LITERAL arm because a literal is one token (two or three for the
    /// comma-decimal forms) and the scan over it is exact; an identifier is not scanned at all — the
    /// <c>arithmeticExpression</c> arm simply precedes the <c>dataReference</c> arm's territory, which the
    /// grammar no longer names in these lists (the binder recovers the sole identifier, GR8 of §14.9.4.4).
    /// A left-edge predicate, so it steers prediction. Read-only over the token stream.</summary>
    protected bool numericLiteralIsLeftOperand()
    {
        int i = 1;
        // The literal's OWN sign is contiguous with its digits; a separated sign is the operator of an expression
        // that starts here, which no literal alternative can carry.
        if (TokenStream.LA(i) is CobolLexer.PLUS or CobolLexer.MINUS)
        {
            if (!TokensAreContiguousFrom(i, 2)) return false;
            i++;
        }
        int width = TokenStream.LA(i) switch
        {
            CobolLexer.FLOATLIT or CobolLexer.COMMA_FLOATLIT or CobolLexer.DECIMALLIT => 1,
            CobolLexer.INTEGERLIT =>
                TokenStream.LA(i + 1) == CobolLexer.COMMA && TokenStream.LA(i + 2) == CobolLexer.INTEGERLIT
                    && TokensAreContiguousFrom(i, 3) ? 3 : 1,
            CobolLexer.COMMA =>
                TokenStream.LA(i + 1) == CobolLexer.INTEGERLIT && TokensAreContiguousFrom(i, 2) ? 2 : 0,
            _ => 0,
        };
        return width != 0
            && TokenStream.LA(i + width) is CobolLexer.PLUS or CobolLexer.MINUS or CobolLexer.STAR
                or CobolLexer.SLASH or CobolLexer.POWER;
    }

    /// <summary>The ARGUMENT-scoped twin of <see cref="boolExprAhead"/> (kb/Work PB65, FMT-15.45.2): does a boolean
    /// operator belong to THIS intrinsic-function argument? §8.4.3.2.3 SR8 admits "a boolean expression" as
    /// argument-1 and §15.3 item 3 names it for a Boolean argument (INTEGER-OF-BOOLEAN(BIT-A B-AND BIT-B)). The
    /// scan stops where the ARGUMENT ends — a depth-0 comma, the argument list's ')', or a SPACE-SEPARATED
    /// boundary: two adjacent operand terms with no operator between them (kb/Work PB124, AR-15.3-3 — boolean
    /// expressions connect every term with a B-operator, so `CONCAT(WS-A B1 B-AND B2)` ends WS-A's argument at
    /// B1 and the B-AND belongs to the LATER argument; the old scan predicated EVERY argument of the list into
    /// the boolean alternative once ANY later one held a depth-0 B-operator). A '(' directly after a term is
    /// that term's subscript/ref-mod — the parser's own reading — never a new term. `COMPUTE BR = FUNCTION
    /// BOOLEAN-OF-INTEGER(5, 8) B-AND BB` still does not mistake its numeric argument for a boolean one.
    /// The 512-token cap is the implementor's scan bound: with the boundary arm the scan ends at the first
    /// adjacent-term pair, so only ONE argument written as a single >512-token boolean expression could ever
    /// reach it (it would fall to the non-boolean alternative and draw a bind diagnostic, never parse silently
    /// wrong).</summary>
    protected bool boolArgAhead()
    {
        int prev = 0, depth = 0;
        for (int i = 1; i <= 512; i++)
        {
            int t = TokenStream.LA(i);
            if (depth == 0 && IsBoolOperandTerm(prev) && IsTermStartToken(t))
                return false;   // adjacent operand terms — a space-separated argument boundary (PB124)
            switch (t)
            {
                // The operator-free parenthesized boolean literal, the same recognition boolExprAhead makes.
                case CobolLexer.LPAREN when ParenRunOpensOnBoolLiteral(i):
                    return true;
                case CobolLexer.LPAREN: case CobolLexer.FNARG_LPAREN: depth++; break;
                case CobolLexer.RPAREN: case CobolLexer.FNARG_RPAREN:
                    if (depth == 0) return false;   // the argument list's ')' — this argument is over
                    depth--; break;
                case CobolLexer.COMMA:
                    if (depth == 0) return false;   // the next argument
                    break;
                case CobolLexer.B_AND:
                case CobolLexer.B_OR:
                case CobolLexer.B_XOR:
                case CobolLexer.B_SHIFT_L:
                case CobolLexer.B_SHIFT_R:
                case CobolLexer.B_SHIFT_LC:
                case CobolLexer.B_SHIFT_RC:
                    if (IsBoolOperandTerm(prev)) return true;
                    break;
                case CobolLexer.B_NOT:
                    if (IsBoolOperandStart(TokenStream.LA(i + 1))) return true;
                    break;
                case CobolLexer.DOT:
                case TokenConstants.EOF:
                    return false;
            }
            prev = t;
        }
        return false;
    }

    /// <summary>Does the grouping-paren run starting at <c>LA(<paramref name="i"/>)</c> open on a boolean literal —
    /// <c>(B"101")</c>, <c>((B"1") B-OR …)</c>? ISO §8.8.2 lists "a boolean expression enclosed in parentheses" as a
    /// boolean expression and rule 1 lets one begin with '(' followed (Table 4, row '(') by a literal, so such an
    /// operand is boolean although it carries NO B-operator — the shape a B-operator scan alone misses, which made
    /// <c>&gt;&gt;DEFINE X AS (B"101")</c> and <c>IF (B"1") = F</c> malformed (kb/Work PB1370). The token run is
    /// decisive, not a guess: a boolean literal is never an arithmetic operand (§8.8.1.1), so a grouping '(' whose
    /// first operand is one can only open a boolean expression. A subscript / reference-modifier '(' carries SUB_*
    /// operands and a function-argument '(' is FNARG_LPAREN, so neither reaches this test with a BOOLLIT behind it.
    /// An operator-free parenthesized boolean IDENTIFIER (<c>(F)</c>) is NOT decidable from tokens — its category is
    /// a binding fact — and stays with the binder.</summary>
    private bool ParenRunOpensOnBoolLiteral(int i)
    {
        int k = i;
        // GROUPING-PAREN-ONLY: a function argument list's FNARG_LPAREN opens no parenthesized boolean expression.
        while (TokenStream.LA(k) == CobolLexer.LPAREN) k++;
        return TokenStream.LA(k) == CobolLexer.BOOLLIT;
    }

    /// <summary>An operand-ENDING token — an operand can end with an identifier, a right paren, or a literal. Used by
    /// <see cref="boolExprAhead"/> to confirm a binary B-operator has a genuine LEFT operand (so a §8.9 user data-name
    /// spelled B-AND/B-OR/B-XOR heading a comparison below 2002 is not mistaken for the operator).</summary>
    /// <remarks>FNARG_RPAREN is the function ARGUMENT-LIST ')' (ISO §8.4.3.2.3 SR6; fix-queue PB48) — the same
    /// character, a different token type. A function result IS an operand, so `FUNCTION f(x) B-AND y` ends its
    /// left operand on that token; without it here the predicate would answer false and the whole condition
    /// would take the comparison path instead. Found by sweeping every consumer of the plain paren types after
    /// the legacy binder's copy of this same omission cost 31 NIST regressions.</remarks>
    private static bool IsBoolOperandTerm(int t) => t is
        CobolLexer.IDENTIFIER or CobolLexer.RPAREN or CobolLexer.SUB_RPAREN or CobolLexer.FNARG_RPAREN
        or CobolLexer.INTEGERLIT or CobolLexer.DECIMALLIT or CobolLexer.FLOATLIT or CobolLexer.COMMA_FLOATLIT
        or CobolLexer.STRINGLIT or CobolLexer.NATLIT or CobolLexer.HEXLIT or CobolLexer.BOOLLIT
        or CobolLexer.SIGNED_INTEGERLIT or CobolLexer.SIGNED_DECIMALLIT;

    /// <summary>A token that STARTS A NEW TERM when it directly follows a completed operand term (kb/Work
    /// PB124 — <see cref="boolArgAhead"/>'s space-separated argument boundary): an identifier, any literal,
    /// figurative ZERO, a unary B-NOT, or the FUNCTION keyword opening a nested call. Deliberately NOT
    /// LPAREN — a paren straight after a term is that term's subscript/ref-mod, the parser's own reading.</summary>
    private static bool IsTermStartToken(int t) => t is
        CobolLexer.IDENTIFIER or CobolLexer.B_NOT or CobolLexer.ZERO or CobolLexer.ZEROS or CobolLexer.ZEROES
        or CobolLexer.FUNCTION
        or CobolLexer.INTEGERLIT or CobolLexer.DECIMALLIT or CobolLexer.FLOATLIT or CobolLexer.COMMA_FLOATLIT
        or CobolLexer.STRINGLIT or CobolLexer.NATLIT or CobolLexer.HEXLIT or CobolLexer.BOOLLIT
        or CobolLexer.SIGNED_INTEGERLIT or CobolLexer.SIGNED_DECIMALLIT;

    /// <summary>An operand-STARTING token — a boolean operand can start with an identifier, '(', a nested B-NOT, a
    /// boolean literal, or figurative ZERO. Used to confirm a prefix B-NOT is a genuine unary operator.</summary>
    /// <remarks>GROUPING-PAREN-ONLY (fix-queue PB48): FNARG_LPAREN is deliberately absent. An operand that
    /// STARTS with a parenthesis is a parenthesized sub-expression, and that paren is always a plain LPAREN; a
    /// function operand starts with the FUNCTION keyword or its name, never with the argument-list '('.</remarks>
    private static bool IsBoolOperandStart(int t) => t is
        CobolLexer.IDENTIFIER or CobolLexer.LPAREN or CobolLexer.B_NOT
        or CobolLexer.BOOLLIT or CobolLexer.ZERO or CobolLexer.ZEROS or CobolLexer.ZEROES;

    /// <summary>
    /// COBOL-2002 RETRY-phrase forward detector (ISO §14.7.9) for the OPEN clause, where the phrase sits inside the
    /// <c>openFileSpec+</c> file-name list and <c>RETRY</c> is a legal §8.9 user-defined word below 2002. Returns
    /// true only when the tail after RETRY is UNAMBIGUOUSLY a retry phrase — it contains a numeric count
    /// (<c>n TIMES</c> | <c>FOR? n SECONDS</c>): an integer can never be a file name, so RETRY must be the phrase
    /// keyword, AND at least one further file-name token remains before the sentence terminator (<c>openFileSpec+</c>
    /// stays satisfiable). <c>RETRY FOREVER</c> (FOREVER is a §8.10 user-legal word) and a bare <c>RETRY name</c> are
    /// genuinely ambiguous below 2002, so they return false and DEFER to <c>is2002()</c> — below 2002 they parse as
    /// file names (INV-1 continuity), never a wrong edition claim (fail-safe: a missed gate degrades to a neutral
    /// parse error). The other five RETRY sites name their file BEFORE the phrase, so they carry no ambiguity and
    /// are bind-gated directly (no forward detect needed). Read-only over the token stream — safe for ANTLR's
    /// repeated prediction calls.
    /// </summary>
    protected bool retryPhraseAhead()
    {
        if (TokenStream.LA(1) != CobolLexer.RETRY) return false;
        bool sawNumber = false;
        for (int i = 2; i <= 24; i++)
        {
            switch (TokenStream.LA(i))
            {
                // A numeric literal in the count position (n TIMES | FOR? n SECONDS) — the ambiguity-free signal.
                case CobolLexer.INTEGERLIT:
                case CobolLexer.DECIMALLIT:
                case CobolLexer.FLOATLIT:
                case CobolLexer.COMMA_FLOATLIT:
                case CobolLexer.SIGNED_INTEGERLIT:
                case CobolLexer.SIGNED_DECIMALLIT:
                    sawNumber = true;
                    break;
                // The numeric retry tail closes here — it is the phrase ONLY if a count was seen and a further
                // candidate file-name token still remains for openFileSpec+.
                case CobolLexer.TIMES:
                case CobolLexer.SECONDS:
                    if (!sawNumber) return false;
                    int next = TokenStream.LA(i + 1);
                    return next != CobolLexer.DOT && next != TokenConstants.EOF;
                // FOREVER is user-legal (ambiguous); a boundary means no numeric tail was found. Defer to is2002().
                case CobolLexer.FOREVER:
                case CobolLexer.DOT:
                case TokenConstants.EOF:
                    return false;
            }
        }
        return false;
    }

    /// <summary>
    /// The Format-3 PERFORM WHEN operand-list CONTINUATION stop-set (design §1.3 / §1.6): the context-sensitive
    /// verbs that are <c>cobolWord</c>s (so they would otherwise be annexed as a spurious exception-name /
    /// file-name) yet ALSO lead a dispatcher statement (imperative-statement-2). Only PARSE is left, carried
    /// anticipatorily (a future statement verb). RESUME/RAISE/VALIDATE/UNLOCK/SEND/RECEIVE/COMMIT/ROLLBACK/ENTER/GET
    /// left with kb/Work PB655: each is RESERVATION-GATED, so it is never a <c>cobolWord</c> alternative at all — it
    /// reaches a name slot only as the IDENTIFIER the token-level gate retypes a DECLARED free word to, and then it
    /// IS a name and annexing it is right.
    /// Pure reserved verbs (MOVE, ADD, DISPLAY, IF, PERFORM…) are NOT cobolWords, so the <c>cobolWord</c> grammar
    /// element itself stops the loop at them — no entry needed. The Format-3 phrase keywords FINALLY / OTHER /
    /// COMMON are likewise not cobolWords (FINALLY is a pure reserved keyword; OTHER/COMMON always were), so they
    /// stop the loop naturally too — no entry here. Completeness is enforced by <c>WhenOperandAheadDriftTests</c>.
    /// Single source of truth: the predicate iterates this array.
    /// </summary>
    public static readonly int[] WhenOperandStopTokens =
    {
        CobolLexer.PARSE,
    };

    /// <summary>
    /// True when LT(1) MAY continue a Format-3 WHEN operand list — i.e. it is not one of
    /// <see cref="WhenOperandStopTokens"/>. Gates only the CONTINUATION of the operand list (the first operand
    /// after WHEN / WHEN EXCEPTION is taken unconditionally), so a WHEN body's leading verb cannot be annexed as
    /// a spurious operand. (ISO §14.9.28.2 Format 3; design §1.2–§1.5.)
    /// </summary>
    protected bool whenOperandAhead()
    {
        int la1 = TokenStream.LA(1);
        foreach (int stop in WhenOperandStopTokens)
            if (la1 == stop) return false;
        return true;
    }
}
