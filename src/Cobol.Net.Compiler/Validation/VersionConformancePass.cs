// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;        // Place subtypes (RefModPlace, …), PicCategory, Usage
using CobolNet.Common;         // CobolLiteral — the ONE literal decoder / §8.3.3 hex-grouping rule (R03)
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;  // BoundUnit / OoClassUnit — the bound model (P6)
using CobolNet.Binding.Passes; // GroupBindContext — this pass is the manifest's NAMED terminal pass (P6 Step 4)
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;   // DiagnosticCatalog / EditionDiagnostic / EditionCodes / EditionSeverity(Policy) — the §8.9 funnel
using CobolNet.Frontend.Common;        // CobolWordRule — the ONE §8.3.2.1 word-length ceiling (shared with the directive stages)
using CobolNet.Frontend.Expressions;    // BooleanOperatorGate — the ONE boolean-operator introduction gate (both lanes)
using CobolNet.Frontend.Generated;     // CobolParserCore / CobolLexer / CobolParserCoreBaseVisitor — the parse-tree arm
using CobolNet.Frontend.Parsing;       // CobolKeywordTokens — the reverse vocab map (>>COBOL-WORDS SR3/SR4 category)

using CobolNet.Compiler.Oo;

namespace CobolNet.Validation;

/// <summary>
/// THE single post-bind edition-conformance pass (rearch PHASE-03 Step 14;
/// <c>docs/rearchitecture/DESIGN-version-conformance-pipeline.md</c> §2.4). It runs over the bound run unit AFTER
/// bind and BEFORE emit and is the sole owner of edition gating, routing every version-gated construct through the
/// ONE <see cref="ConstructRegistry.Check"/> funnel so the binder is edition-AGNOSTIC (zero <c>Check</c> calls of
/// its own).
/// </summary>
/// <remarks>
/// TWO-ARM by design (§1/§2.4 of the pipeline design):
/// <list type="bullet">
/// <item><b>Parse-tree arm</b> (<see cref="ParseArm"/>) — walks the raw <c>CompilationUnitContext</c> and fires
/// every SYNTACTIC introduction/removal/phrase gate + the §8.9 reserved-word funnel on the construct's
/// RECOGNITION (it absorbs the former <c>EditionValidator</c>). Recognition-based because an introduction gate
/// must name its edition even when the below-edition construct ALSO has a semantic error: the bound node it would
/// have produced is dropped (<c>BoundUnsupported</c>/<c>BoundNop</c>) on that path, but its PARSE node is always
/// present (the load-bearing finding, DEVLOG 724 — a bound-arm intro gate silently dropped the 0900).</item>
/// <item><b>Bound-tree arm</b> (<see cref="GateStatement"/>/<see cref="GateMove"/>/<see cref="GateData"/>) — the
/// END-STATE home of only the genuinely-SEMANTIC gates whose identity is a RESOLVED bound fact, never mere
/// presence: MOVE figurative-category (source × each receiver's picture), the SET forms one grammar rule cannot
/// tell apart (object-reference vs pointer arithmetic vs dynamic-length SIZE, all spelled <c>SET x TO y</c>),
/// the PicInfo USAGE/PICTURE-category gates and the prototype gate. A parse-arm
/// home would over- or under-fire exactly these. No bound node carries a raw parse context — the
/// <c>BoundTree.cs</c> invariant stands.
/// <para>⛔ THE TEST FOR THIS ARM IS "WHICH RESOLVED FACT?", AND IT IS ENFORCED, NOT REMEMBERED.
/// <c>EditionGateArmDriftTests</c> derives both arms' construct ids from this file and requires every bound-arm
/// id to carry an adjudicated reason naming that fact — because for four gates the classification was simply
/// wrong (READ PREVIOUS / READ ADVANCING ON LOCK / START FIRST-LAST / START WITH LENGTH, each keyed on a bound
/// member assigned from a parse fact alone), and the cost was silent: every bail-out path that returned
/// <c>BoundUnsupported</c>/<c>BoundNop</c> before the node existed compiled a post-85 construct at
/// <c>--std 85</c> without naming the edition (kb/Work PB353, Step 14h.5).</para></item>
/// </list>
/// The two arms are disjoint: a <c>Check</c> for any one construct fires from EXACTLY one arm, the losing site
/// deleted as its twin lands. <b>Migration state:</b> Step 14h (DONE) moved every SYNTACTIC statement/expression/
/// literal gate + the §8.9 funnel to the parse-arm (14h.1 foundation → 14h.4b), so the binder holds ZERO
/// statement-level edition Checks. Step 14g is relocating the DATA/PICTURE/OO gates: <b>14g.1 (DONE)</b> moved the 8
/// PicInfo USAGE / PICTURE-category gates to the bound-arm <see cref="GateData"/> enumerator over
/// <c>DataBinder.ConformanceForest()</c>; <b>14g.2 (DONE)</b> moved all four data-description-clause gates
/// (BASED/TYPE/PROPERTY/TYPEDEF) to the parse-arm on RECOGNITION — none needs a resolved fact, and a bound-arm home
/// drops the 0900 on a declaration-error path (the review correction, DEVLOG 734: even init-only <c>IsTypedef</c>
/// fails, because the typedef ITEM is discarded from the forest when RegisterTypeDecl rejects it or it binds into
/// method scope). <b>14g.3–14g.5 (DONE)</b> completed the DATA/PIC/OO migration: OO class/interface + OCCURS DYNAMIC
/// (14g.3, parse-arm); file SHARING/LOCK-MODE + SPECIAL-NAMES FOR + PD RETURNING/RAISING (14g.4, parse-arm — the recon's
/// bound-arm SHARING/LOCK-MODE reclassified for the same drop-proof reason); FUNCTION- and PROGRAM-PROTOTYPE (parse-arm
/// over the shared <c>prototypePhrase</c> since kb/Work PB894; 14g.5 had it bound-arm) + REPOSITORY CLASS/INTERFACE/PROPERTY (14g.5, parse-arm) + the external-float /
/// national-edited PICTURE forms (14g.5, bound-arm via <see cref="PictureConstructId"/> — each keyed on the
/// analyzed <c>PicInfo</c>'s own shape: <c>IsFloatEdited</c>, and a category-national picture's <c>EditMask</c>).
/// The one principled exception is the
/// UDF-invocation gate (an intrinsic FUNCTION and a user-function call are
/// syntactically identical — only the repository-resolved name set separates them), which stays BIND-TIME
/// (<c>StatementBinder.Udf.cs</c>) where it already fires on recognition before operand binding.
/// </remarks>
internal sealed class VersionConformancePass
{
    private readonly EditionInfo _edition;
    private readonly IDiagnosticSink _sink;

    private VersionConformancePass(EditionInfo edition, IDiagnosticSink sink)
    {
        _edition = edition;
        _sink = sink;
    }

    /// <summary>Gate every version-gated construct in the bound <paramref name="group"/>, reporting to
    /// <paramref name="sink"/>. The manifest's NAMED TERMINAL pass (P6 Step 4 — <c>BindPipeline.GroupTail</c>,
    /// Requires <c>StorageComputed</c>, Produces <c>EditionConformanceChecked</c>): it runs INSIDE
    /// <c>BinderDriver.Bind</c> after every other pass, so the Bind result already carries every edition
    /// diagnostic and the driver HALTs before emit if the sink then carries errors (rearch exit criterion 9 — no
    /// codegen on an errored tree; a <c>CheckOnly</c> verdict is complete without emit).</summary>
    public static void Run(GroupBindContext group, EditionInfo edition, IDiagnosticSink sink)
    {
        var pass = new VersionConformancePass(edition, sink);
        // The per-compilation-group EFFECTIVE reserved-word set (ISO §7.3.10 D9 seam): the generated §8.9 table
        // composed with the >>COBOL-WORDS overlay (RESERVE adds, UNDEFINE/SUBSTITUTE remove). Empty map ⇒ Default.
        var reservedWords = ReservedWordSet.Compose(group.Session.CobolWords);
        // >>COBOL-WORDS SR3/SR4 category validation (§7.3.10.3) — needs all three registries (reserved via the §8.9
        // table + the lexer vocab, intrinsic via IntrinsicCatalog); the frontend validated SR1/SR2/SR5 already.
        ValidateCobolWords(group.Session.CobolWords, edition, sink);
        // ── PARSE-tree arm (Step 14h): ONE walk of the raw compilation unit, firing every SYNTACTIC
        //    introduction/removal/phrase gate + the §8.9 reserved-word funnel on the construct's RECOGNITION
        //    (absorbs the former EditionValidator). Recognition-based so a below-edition construct that ALSO
        //    has a semantic error still names its edition — the bound node it would have produced may be
        //    dropped (BoundUnsupported/BoundNop), but its parse node is always present (DEVLOG 724). ──
        new ParseArm(pass, reservedWords, group.Session.CobolWords).VisitPositioned(group.Tree);
        // ── BOUND-tree arm: the genuinely-SEMANTIC gates (MOVE figurative-category; the file-org / USAGE /
        //    pointer-category conditioned STATEMENT gates) + the DATA-attribute gates (Step 14g — every
        //    source-declared DataItem's resolved USAGE / PICTURE category), which need a resolved bound fact. ──
        foreach (var unit in group.Units)
        {
            pass.WalkProgram(unit.Bound);
            pass.GateData(unit.Data);
        }
        foreach (var cls in group.Classes)
        {
            // A class body's OBJECT and FACTORY halves are two bound programs over one class; both carry statements
            // (each METHOD-ID is a pc slice of the ONE dispatch space, so walking Paragraphs covers every method).
            pass.WalkProgram(cls.Bound);
            pass.WalkProgram(cls.FactoryBound);
            pass.GateData(cls.Data);
            pass.GateData(cls.FactoryData);
        }
    }

    /// <summary>&gt;&gt;COBOL-WORDS SR3/SR4 category validation (ISO §7.3.10.3): the EXISTING word (literal-1/3/4)
    /// must be a reserved word, context-sensitive word, or intrinsic-function name (SR3); the NEW word
    /// (literal-2/5/6) must be none of those (SR4 — the §8.3.2.2 well-formedness is the frontend half). Reserved /
    /// context membership comes from the §8.9 table + the lexer vocabulary (<see cref="CobolKeywordTokens"/>);
    /// intrinsic from <see cref="IntrinsicCatalog"/>. Both checks err AWAY from rejecting legal source (the
    /// no-false-reject principle): SR3 accepts on ANY membership signal, SR4 rejects only on a CERTAIN one.</summary>
    private static void ValidateCobolWords(CobolWordsMap map, EditionInfo edition, IDiagnosticSink sink)
    {
        if (map.IsEmpty) return;
        foreach (var op in map.Ops)
        {
            if (op.Existing is { } e && !IsExistingWordCategory(e))
                ReportCobolWordsInvalid(sink, $"the existing word '{e}' is not a reserved word, "
                    + "context-sensitive word, or intrinsic-function name (ISO §7.3.10.3 SR3)");
            if (op.New is { } n && IsReservedCategory(n, edition))
                ReportCobolWordsInvalid(sink, $"the new word '{n}' is a reserved / context-sensitive / "
                    + "intrinsic-function word and cannot be a user-defined word (ISO §7.3.10.3 SR4)");
        }
    }

    /// <summary>SR3 membership — broad (accept on any signal): a lexer keyword/context token, an intrinsic-function
    /// name, ANY §8.9 table entry, or any §8.10 context-sensitive word.
    /// <para>⛔ THE §8.10 ARM IS WHAT THE LEXER VOCABULARY CANNOT SUPPLY (kb/Work PB250).
    /// <see cref="CobolKeywordTokens"/> knows only the context words this compiler TOKENIZES; the other 31 —
    /// HEX, NAT, ANUM, BYTE, CURRENT, ACTIVATING, STACK, TOP-LEVEL, the LC_ categories, UCS-4/UTF-8/UTF-16 and
    /// the rest — arrive as bare IDENTIFIERs, so SR3 rejected the perfectly legal
    /// <c>&gt;&gt;COBOL-WORDS EQUATE "HEX" WITH …</c> with COBOLNET1623 and no directive could name them at all.
    /// §8.10's own NOTE is the counter-evidence: "Words can be added or deleted from this list for a specific
    /// compilation group by use of the COBOL-WORDS directive."</para></summary>
    private static bool IsExistingWordCategory(string w) =>
        CobolKeywordTokens.IsKeyword(w) || IntrinsicCatalog.TryGet(w, out _) || ReservedWords.Find(w) is not null
        || ContextSensitiveWords.Contains(w);

    /// <summary>SR4 membership — narrow (reject only when certain): a lexer keyword/context token, an
    /// intrinsic-function name, a §8.10 context-sensitive word, or a HIGH-CONFIDENCE reserved-at-edition table
    /// entry. The §8.10 arm is CERTAIN despite carrying no per-edition flags: the directive is a COBOL-2023
    /// introduction (<c>cobol-words-directive-2023</c>; §7.3.10, Annex E.3.3 item 12), so 2023 is the only
    /// edition at which this question is ever asked and the table transcribes exactly that edition.</summary>
    private static bool IsReservedCategory(string w, EditionInfo edition) =>
        CobolKeywordTokens.IsKeyword(w) || IntrinsicCatalog.TryGet(w, out _) || ContextSensitiveWords.Contains(w)
        || (ReservedWords.Find(w) is { Confidence: "high" } r && r.IsReservedAt(edition.Year));

    private static void ReportCobolWordsInvalid(IDiagnosticSink sink, string message) =>
        sink.Report(new EditionDiagnostic(DiagnosticCatalog.CobolWordsDirectiveInvalid.Code, EditionSeverity.Error,
            "cobol-words-directive-invalid", message, "", "ISO §7.3.10.3"));

    private void WalkProgram(BoundProgram? prog)
    {
        if (prog is null) return;
        foreach (var para in prog.Paragraphs)
            foreach (var stmt in para.Statements)
                WalkStatement(stmt);
        GateMergeInSortMergeProc(prog);
    }

    /// <summary>VCR 27 (ISO §14.9.24; Annex E.2 item 20): at COBOL-2023 a MERGE statement is PROHIBITED in the
    /// output procedure of another MERGE or the input/output procedure of a file-format SORT (the prior standard
    /// allowed it with conflicting rules; SORT already disallowed it). A bind-time cross-pass over the paragraph-pc
    /// ranges — a paragraph's pc IS its index in <see cref="BoundProgram.Paragraphs"/>, the same pc space as the
    /// SORT/MERGE procedure ranges (<c>SortRange</c> → the ProcedureTable). Below 2023 the runtime
    /// EC-SORT-MERGE-ACTIVE raise (CobolSort.Init, when checking is enabled — kb/Work PB1036) is the dynamic net,
    /// so this fires only at ≥2023.</summary>
    private void GateMergeInSortMergeProc(BoundProgram prog)
    {
        if (_edition.Year < 2023) return;

        // Pass A — the prohibited paragraph-pc ranges: every file-format SORT's input/output procedure + every
        // MERGE's output procedure (a SORT/MERGE with only USING/GIVING files contributes no procedure range).
        var prohibited = new List<PcRange>();
        void Collect(BoundStatement s)
        {
            if (s is BoundSort { InputProcedure: { } ip }) prohibited.Add(ip);
            if (s is BoundSort { OutputProcedure: { } op }) prohibited.Add(op);
            if (s is BoundMerge { OutputProcedure: { } mop }) prohibited.Add(mop);
            foreach (var c in s.StatementChildren()) Collect(c);
        }
        foreach (var para in prog.Paragraphs)
            foreach (var stmt in para.Statements) Collect(stmt);
        if (prohibited.Count == 0) return;

        // Pass B — flag every MERGE whose ENCLOSING paragraph pc falls within a prohibited range (a MERGE nested in
        // an IF/inline-PERFORM is still in that paragraph). The owning MERGE's own paragraph is never in its own
        // output-proc range (a distinct named procedure), so a MERGE never false-flags itself.
        // The 2023 prohibition is a REMOVAL of a prior-edition capability, so its severity follows the removal
        // policy — an Error under strict, downgraded to a Warning (compile succeeds) under --permissive migration
        // mode (EditionSeverityPolicy), matching the version matrix's RemovedConstruct_CompilesPermissive contract.
        var severity = EditionSeverityPolicy.For(ConstructAvailability.Removed, _edition);
        void Flag(BoundStatement s, int paraPc)
        {
            if (s is BoundMerge m && prohibited.Any(r => paraPc >= r.Start && paraPc <= r.End))
                _sink.Report(new EditionDiagnostic("COBOLNET1572", severity, "merge-in-sort-merge-proc",
                    $"MERGE '{m.File.CobolName}' is prohibited in the output procedure of another MERGE or the input "
                    + "or output procedure of a file SORT (ISO §14.9.24; COBOL-2023, Annex E.2 item 20)",
                    $"MERGE '{m.File.CobolName}'", "ISO §14.9.24; Annex E.2 item 20"));
            // kb/Work PB137 — the batch-8 finding verbatim: this pass implemented exactly the SR2 ban with a
            // MERGE-only predicate; COMMIT (§14.9.7.3 SR2) and ROLLBACK (§14.9.36.3 SR2) are its siblings,
            // reachable only now that the bind produces an identity-bearing node.
            if (s is BoundCommitRollback cr && prohibited.Any(r => paraPc >= r.Start && paraPc <= r.End))
                _sink.Report(new EditionDiagnostic(DiagnosticCatalog.CommitRollbackContext.Code,
                    EditionSeverity.Error, "commit-rollback-context",
                    $"{(cr.IsCommit ? "COMMIT" : "ROLLBACK")} shall not be specified in the input or output "
                    + $"procedure of a MERGE or file SORT statement (ISO {(cr.IsCommit ? "§14.9.7.3" : "§14.9.36.3")} SR2)",
                    cr.IsCommit ? "COMMIT" : "ROLLBACK", "ISO §14.9.7.3 SR2 / §14.9.36.3 SR2"));
            foreach (var c in s.StatementChildren()) Flag(c, paraPc);
        }
        for (int i = 0; i < prog.Paragraphs.Count; i++)
            foreach (var stmt in prog.Paragraphs[i].Statements) Flag(stmt, i);
    }

    private void WalkStatement(BoundStatement s)
    {
        GateStatement(s);
        Recurse(s);
    }

    private void Check(string constructId, string where)
        => ConstructRegistry.Check(_edition, _sink, constructId, where);

    // ── The bound-tree arm — statement-level edition gates ──────────────────────────────────────────────────
    // The constructs whose IDENTITY is the bound-node TYPE or a resolved node ATTRIBUTE. Step 14h migrates the
    // SYNTACTIC introduction/removal/phrase gates from here to the ParseArm (fire-on-recognition, DEVLOG 724);
    // the genuinely-SEMANTIC gates (MOVE-category, file-org/USAGE/pointer-category conditioned) stay. During the
    // 14h.2–14h.4 migration a Check fires from EXACTLY one arm — the losing site is deleted as its parse-arm
    // twin lands. Each Check keeps the exact constructId + where-string of its former binder site (byte-identical).
    private void GateStatement(BoundStatement s)
    {
        switch (s)
        {
            // (UNLOCK / FREE / ALTER / DELETE FILE / SET ADDRESS / ALLOCATE — self-identifying statement gates —
            // migrated to the ParseArm in Step 14h.2: they fire on RECOGNITION, so a below-edition occurrence
            // names its edition even when the statement also fails to bind, DEVLOG 724.)
            case BoundSetObjectRef:
                Check(Constructs.SetObjectReference2002, "the SET … TO object-reference statement (Format 5)"); break;
            case BoundSetPointerUpDown:
                Check(Constructs.PointerArithmetic2002, "SET pointer UP/DOWN BY (ISO §14.9.39 Format 10)"); break;
            // SET Format 8 (function-pointer-assignment, §14.9.39.2) — a 2014 introduction (kb/Work PB452).
            // BOUND arm for the SetObjectReference2002 reason: the PLAIN form has no grammar rule of its own.
            // `SET fp1 TO fp2` parses as setToValueStatement and `SET fp TO NULL` as setObjectReferenceStatement,
            // and only the operands' RESOLVED PicCategory.FunctionPointer re-routes either one here — so a
            // recognition gate would have to gate every SET of an index. The ADDRESS OF FUNCTION sender's own
            // rule binds to the second node, and both take the SAME id from this one arm.
            case BoundSetFunctionPointer or BoundSetFunctionAddress:
                Check(Constructs.SetFunctionPointer2014, "the SET … TO function-pointer statement (Format 8)"); break;
            // SET Format 9 (program-pointer-assignment, §14.9.39.2) — a 2002 introduction (kb/Work PB549), the
            // Format-8 arm's twin and gated for the same reason: the PLAIN form has no grammar rule of its own
            // (`SET pp1 TO pp2` parses as setToValueStatement, `SET pp TO NULL` as setObjectReferenceStatement)
            // and only the operands' resolved PicCategory.ProgramPointer re-routes either one here.
            // BoundSetEntry joins the arm because it IS the bound form of the sender phrase — both the ISO
            // §8.4.3.13 ADDRESS OF PROGRAM rule and the Micro Focus / IBM TO ENTRY extension bind to it, and
            // the receiving operand is category program-pointer in both (§14.9.39.3 SR21).
            case BoundSetProgramPointer or BoundSetEntry:
                Check(Constructs.SetProgramPointer2002, "the SET … TO program-pointer statement (Format 9)"); break;
            case BoundSetSize:
                // SET [SIZE OF] dynamic-length-item TO n (§14.9.39 Format 16) — a 2023 introduction. Semantic (the
                // target must be dynamic-length), so it stays a bound-tree gate — one arm covers the explicit SIZE
                // OF form and the bare re-routed form (both bind to BoundSetSize).
                Check(Constructs.SetDynLengthSize2023, "the SET [SIZE OF] … TO length statement (dynamic-length item, Format 16)"); break;

            // (OPEN-SHARING / GOBACK-RETURNING / CALL-BY-VALUE / CALL-ON-OVERFLOW / STOP-RUN-STATUS / END-ACCEPT —
            // phrase gates whose presence is purely syntactic — migrated to the ParseArm in Step 14h.3.)
            // (INVOKE — migrated to the ParseArm in Step 14h.5 with the four I/O phrase gates below. `case
            // BoundInvoke` was BOTH under- and over-inclusive: under, because an INVOKE whose target does not
            // resolve returns before the node exists (measured — `INVOKE NOSUCHO "M1"` at --std 85 named
            // COBOLNET0823 and no edition); over, because BoundInvoke is ALSO the bound form of a PROPERTY
            // get/set re-write and of NEW / SELF-NEW (OoBinder), none of which is "the INVOKE statement".)
            case BoundMove mv:
                GateMove(mv); break;
            // INITIALIZE REPLACING's hypothetical MOVE (§14.9.20.3 SR4 → §14.9.25.3 SR5) — kb/Work PB879.
            case BoundInitialize ini:
                GateInitialize(ini); break;
            // (READ PREVIOUS / READ ADVANCING ON LOCK / START FIRST-LAST / START WITH LENGTH — four PHRASE gates
            // this arm used to own on the shape of BoundKeyedRead / BoundRead / BoundKeyedStart. They are
            // recognition gates, migrated to the ParseArm in Step 14h.5: kb/Work PB353 measured that each is
            // decided by the PRESENCE of a printed phrase and by nothing the bind resolves, so a bound-tree home
            // bought nothing and cost the diagnostic on every path that bails out before the node is built.)
        }
    }

    // ── MOVE figurative-constant category gates (ISO §14.9.25.3 SR5) ─────────────────────────────────────────
    // Genuinely SEMANTIC: which of the three edition rows applies depends on the source figurative × each
    // receiver's RESOLVED picture — re-derived here from the bound MOVE (Group B). Mirrors the binder's former
    // MoveBinder classification EXACTLY (same figText, same per-target exemptions, same
    // integer/QUOTE/digit-only split, same where-string); the binder keeps only the SR1 class-index error (0809,
    // version-invariant) and the pre-removal StoreAsImage marking.
    // ⛔ IT READS m.Source — THE WRITTEN SENDING OPERAND — NEVER m.Stores[i].Sender. SR5 is a SYNTAX rule over a
    // closed list of figurative constants SPELLED IN THE SOURCE ("SPACE, QUOTE, HIGH-VALUE, LOW-VALUE, ALL
    // "literal", or ALL symbolic-character"); §14.9.25.4 GR2/GR3 substitute the figurative SPACE / ZERO for a
    // zero-length literal as a VALUE, and a zero-length literal is not on SR5's list. Gating the substituted
    // sender would make `MOVE "" TO PIC 9(3)` — permitted at every edition — a COBOLNET0902 rejection, i.e. a
    // wrong answer traded for a false one (kb/Work PB425).
    private void GateMove(BoundMove m)
    {
        if (Sr5FigurativeText(m.Source) is not { } figText) return;   // not on SR5's list — SR5 does not reach it
        foreach (var t in m.Targets)
        {
            // Exemptions (§14.9.25.3 SR5): a ref-mod receiver (unique elementary alphanumeric), a group receiver
            // (a conversion-free character copy), a non-numeric receiver, or class index (SR1-errored in the binder).
            if (t is RefModPlace || t.Item.IsGroup || t.Item.Pic is not { } pic) continue;
            if (pic.Category is not (PicCategory.Numeric or PicCategory.NumericEdited)) continue;
            if (pic.Usage is Usage.Index) continue;
            // The IMPLICIT move of a FROM / INTO phrase reaches this gate too (kb/Work PB348 — it is now a
            // BOUND child of the I-O node, so the StatementChildren walk finds it), and the edition report has
            // to name the statement the programmer wrote: `RELEASE R FROM QUOTE` is the removal §14.9.32.4 GR4
            // makes it, not a MOVE anywhere in their source.
            string where = m.ImplicitOf is { } phrase
                ? $"MOVE {figText} TO {t.Item.CobolName}, {phrase.Where(t.Item.CobolName)}"
                : $"MOVE {figText} TO {t.Item.CobolName}";
            GateSr5(m.Source, pic.Category,
                    integerReceiver: pic is { Category: PicCategory.Numeric, IsFloat: false, Scale: <= 0 }, where);
        }
    }

    /// <summary>The source spelling §14.9.25.3 SR5's closed list names — "SPACE, QUOTE, HIGH-VALUE, LOW-VALUE, ALL
    /// "literal", or ALL symbolic-character" — or null when the operand is not on it.</summary>
    private static string? Sr5FigurativeText(BoundOperand source) => source switch
    {
        BoundFigurative { Kind: 'S' } => "SPACE",
        BoundFigurative { Kind: 'Q' } => "QUOTE",
        BoundFigurative { Kind: 'H' } => "HIGH-VALUE",
        BoundFigurative { Kind: 'L' } => "LOW-VALUE",
        BoundAllLiteral a => $"ALL \"{a.Literal}\"",
        _ => null,
    };

    /// <summary>⭐ §14.9.25.3 SR5's THREE EDITION ROWS, in ONE place, for one (SR5-listed sender, numeric or
    /// numeric-edited receiver) pair — asked by the written MOVE (<see cref="GateMove"/>, per receiving item) and by
    /// INITIALIZE REPLACING's hypothetical MOVE (<see cref="GateInitialize"/>, per category). Before kb/Work PB879
    /// the rows lived inline in <see cref="GateMove"/>, which only a bound <c>BoundMove</c> node reaches.
    /// <para>⛔ §8.3.3.6.3 SR3 IS ASKED FIRST, AND IT IS NOT A §14.9.25.3 SR5 ROW (kb/Work PB422): a multi-character
    /// <c>ALL literal</c> associated with a numeric or numeric-edited item is SR3's, whose edition edge (removed
    /// 2002) is not §14.9.25.3 SR5's (removed 2023) — §14.9.25.3 SR5's digit-only exception has no length qualifier, so <c>ALL "57"</c>
    /// was never SR5's to refuse. The predicate and its row live in <see cref="FigurativeAssociation"/>, shared
    /// with the relation checkpoint; when SR3 decides the pair no SR5 row is asked of it.</para></summary>
    private void GateSr5(BoundOperand source, PicCategory receiverCategory, bool integerReceiver, string where)
    {
        if (FigurativeAssociation.GateSr3(_edition, _sink, source, receiverCategory, where)) return;
        if (source is BoundAllLiteral { IsDigitOnly: true } && integerReceiver)
            // SR5's surviving exception — valid everywhere, obsolete at 2023 (0903; SR5 NOTE / Annex F.2).
            Check(Constructs.MoveAllDigitIntegerObsolete2023, where);
        else if (source is BoundFigurative { Kind: 'Q' })
            // QUOTE→numeric — obsolete 2014 (Annex E.2 item 21) then removed 2023 (dual-window row).
            Check(Constructs.MoveQuoteNumericObsolete2014, where);
        else
            // Every other shape — REMOVED by ISO 2023 (Annex E.2 item 1 bullet 1; 0902 — VCR row 1).
            Check(Constructs.MoveAlphanumericFigurativeRemoved2023, where);
    }

    /// <summary>
    /// ISO §14.9.20.3 SR4 — "For each of the other categories specified in the REPLACING phrase, a MOVE statement
    /// with identifier-2 or literal-1 as the sending item and an item of the specified category as the receiving
    /// operand shall be valid" — for the one MOVE rule SR4 reaches that is EDITION-dependent: §14.9.25.3 SR5
    /// (kb/Work PB879). <c>INITIALIZE G REPLACING NUMERIC DATA BY SPACE</c> is valid exactly when
    /// <c>MOVE SPACE TO &lt;a numeric item&gt;</c> is, so it takes the same three rows at the same edition — before
    /// this arm it compiled clean at 2023 while the byte-identical explicit MOVE was COBOLNET0902.
    /// <para>⛔ ASKED OF THE PHRASE, PER CATEGORY, NOT PER RECEIVER: SR4's receiving operand is "an item of the
    /// specified category", so the pair is valid or invalid before any receiver is walked (the INITIALIZE binder
    /// asks the edition-blind rules the same way). The implicit moves themselves are marked
    /// <see cref="ImplicitMovePhrase.ValidityAskedByStatement"/> and are not re-gated.</para>
    /// <para>⚠ DETERMINATION — the digit-only-ALL exception ("an ALL "literal" figurative constant (containing only
    /// digits) … to an integer numeric item") is applied to the NUMERIC category as if its item were an integer:
    /// the category names integer and non-integer items alike, and the reading that never rejects a program
    /// whose actual receivers are all integers is chosen over one that would. NUMERIC-EDITED is never an integer
    /// numeric item, so it takes the removal row.</para>
    /// </summary>
    private void GateInitialize(BoundInitialize ini)
    {
        foreach (var item in ini.Replacing)
        {
            if (Sr5FigurativeText(item.Sender) is not { } figText) continue;
            foreach (var cat in (ReadOnlySpan<InitializeCategory>)[InitializeCategory.Numeric, InitializeCategory.NumericEdited])
                if (item.Categories.Contains(cat))
                    GateSr5(item.Sender,
                        cat is InitializeCategory.Numeric ? PicCategory.Numeric : PicCategory.NumericEdited,
                        integerReceiver: cat is InitializeCategory.Numeric,
                        $"INITIALIZE REPLACING {InitializeCategories.Spelling(cat)} … BY {figText} (the MOVE "
                        + "ISO §14.9.20.3 SR4 requires to be valid)");
        }
    }

    // ── The complete nested-statement traversal (PHASE-07 Step 6h) ────────────────────────────────────────────
    // Descends EVERY container via the generated BoundStatementTree.StatementChildren — the ONE drift-proof source
    // of the statement-tree shape — so a gate nested inside IF/EVALUATE/PERFORM/SEARCH/an ON-phrase escapes nothing,
    // and a NEW container node is covered automatically (the former hand-listed switch was synced-by-prose against
    // the binder's own traversals). Leaves yield no children.
    private void Recurse(BoundStatement s)
    {
        foreach (var child in s.StatementChildren())
            WalkStatement(child);
    }

    // ── Step 14g: the DATA-attribute (USAGE / PICTURE-category) edition gates ────────────────────────────────
    // Genuinely SEMANTIC — identity is a RESOLVED DataItem attribute (its USAGE / PICTURE category), never parse
    // presence, so a bound-arm walk is the correct home. Fires ONCE per SOURCE declaration: DataBinder's
    // ConformanceForest excludes the post-bind TYPE-clones + compiler temps (which the binder never re-analyzed
    // and so never gated), reproducing the former per-entry PictureAnalyzer.ParseUsage/Analyze gates byte-for-byte.
    private void GateData(DataBinder data)
    {
        foreach (var item in data.ConformanceForest())
            GateDataItem(item, $"data item '{item.CobolName ?? "FILLER"}'");
        // Report printable items are SYNTHETIC DataItems in the RD model (off the forest) — their national/boolean
        // PICTURE gates fired in Analyze with the report where-string (DataBinder.Reports.cs); reproduce that here.
        foreach (var report in data.Reports)
        {
            foreach (var grp in report.Groups)
                foreach (var line in grp.Lines)
                    foreach (var field in line.Fields)
                        GateDataItem(field.PrintItem,
                            $"RD '{report.Name}' printable item '{field.PrintItem.CobolName ?? "FILLER"}'");
            // SUM counters (§13.18.54) analyze their PICTURE for the counter scale (GR1) in a DISTINCT Analyze call off
            // both the forest AND the printable-item walk (DataBinder.Reports.cs:BindSumClause), so an external-float /
            // national-edited SUM-counter picture carries its 0900 here on ReportSumModel.SkeletonGate (DEVLOG 740 —
            // the 14g.5 review found the former inline gate was dropped). Fires once per SUM counter — so a NON-printable
            // SUM gets its one 0900, and a PRINTABLE SUM (also a print item, gated above) gets both, exactly as the
            // former two Analyze sites did.
            foreach (var sum in report.Sums)
                if (sum.SkeletonGate is { } sumSkeletonId) Check(sumSkeletonId, sum.SkeletonWhere);
        }
    }

    /// <summary>Gate one resolved DataItem's USAGE / PICTURE-category edition attribute. ⛔ At most ONE gate
    /// fires, and where both answer the PICTURE SHAPE wins because it is the FINER identity: a national-edited
    /// picture is construct <c>national-edited-2002</c> (§8.5.2.11, its own registry row), and its item is ALSO
    /// class and category national (§8.5.2.1 Table 2), so the usage arm would otherwise report a second,
    /// redundant COBOLNET0900 naming <c>national-data-2002</c> on the same entry (kb/Work PB492).</summary>
    private void GateDataItem(DataItem item, string where)
    {
        string? id = (item.Pic is { } pic ? PictureConstructId(pic) : null) ?? UsageConstructId(item);
        if (id is { } gate) Check(gate, where);
    }

    /// <summary>The 2002-introduction gate a PICTURE's SHAPE carries (or null when version-invariant) — the ONE
    /// function for every Analyze site: the forest / report printable items (<see cref="GateDataItem"/>) and the
    /// report SUM-counter scale Analyze (<c>ReportSumModel.SkeletonGate</c>, DataBinder.Reports.cs — a distinct call
    /// whose PicInfo is otherwise discarded, DEVLOG 740). The floating-point numeric-edited form (symbol E — LIVE,
    /// data-model design D21 / kb/Work PB66) keys on <see cref="PicInfo.IsFloatEdited"/>; NATIONAL-EDITED (§8.5.2.11
    /// / §13.18.40.4 GR10 — LIVE since kb/Work PB492) on the category-national picture's EDIT MASK, which is what
    /// the category IS in this model (the same <c>{ Category, EditMask: not null }</c> pair alphanumeric-edited
    /// uses). It used to ride a <c>PicInfo.SkeletonGate</c> string because the analyzer RECOVERED the category to
    /// Alphanumeric and no category key could see it; the category is real now and the carrier is gone.</summary>
    internal static string? PictureConstructId(PicInfo pic) =>
        pic.IsFloatEdited ? Constructs.PicExternalFloat2002
        : pic is { Category: PicCategory.National, EditMask: not null } ? Constructs.NationalEdited2002
        : null;

    /// <summary>The 2002-introduction USAGE / PICTURE-category of a resolved item, or null when version-invariant.
    /// Keyed on the resolved <c>(OwnUsage, Pic.Category, Pic.Usage)</c>: <see cref="DataItem.OwnUsage"/> is mandatory
    /// because a group-header USAGE sheds <c>Pic</c> to null (<c>DataBinder.ResolveIndexItems</c>), leaving only the
    /// own keyword; the <c>Pic.Usage</c> member (never <c>IsFloat</c>/<c>ClrType</c>) carries the identity for the
    /// picture-less usages — <c>FloatLong</c>/<c>FloatExtended</c> share a <c>double</c> ClrType with COMP-2.</summary>
    private static string? UsageConstructId(DataItem item)
    {
        var cat = item.Pic?.Category;
        var pu = item.Pic?.Usage;
        var ou = item.OwnUsage;
        return
            // ⛔ The RESOLVED usage is read beside the written keyword, exactly as every arm below does: a
            // national-form numeric / numeric-edited / boolean item (§13.18.60.3 SR12, LIVE since kb/Work
            // PB646) is category-neutral, so only Pic.Usage names it — and the §13.18.60.4 GR1 INHERITED
            // spelling (`01 G USAGE NATIONAL. 05 L PIC 9(3).`) leaves the leaf no OwnUsage at all.
            cat is PicCategory.National || pu is Usage.National || ou is Usage.National ? Constructs.NationalData2002
            : cat is PicCategory.Boolean || ou is Usage.Bit ? Constructs.BooleanData2002
            : pu is Usage.Pointer || ou is Usage.Pointer ? Constructs.UsagePointer2002
            : cat is PicCategory.ProgramPointer || pu is Usage.ProgramPointer || ou is Usage.ProgramPointer
                ? Constructs.UsageProgramPointer2002
            : cat is PicCategory.FunctionPointer || pu is Usage.FunctionPointer || ou is Usage.FunctionPointer
                ? Constructs.UsageFunctionPointer2014   // LIVE (kb/Work PB452/PB817) — the 0900 introduction gate below 2014, the ProgramPointer2002 shape
            : cat is PicCategory.ObjectReference || pu is Usage.ObjectReference || ou is Usage.ObjectReference
                ? Constructs.UsageObjectReference2002
            : pu is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
              || ou is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
                ? Constructs.UsageBinaryCharFamily2002
            : pu is Usage.FloatShort || ou is Usage.FloatShort ? Constructs.UsageFloatShort2002
            : pu is Usage.FloatLong || ou is Usage.FloatLong ? Constructs.UsageFloatLong2002
            : pu is Usage.FloatExtended || ou is Usage.FloatExtended ? Constructs.UsageFloatExtended2002
            // The 2014 IEEE interchange floats binary32/64 (LIVE) — the 0900 introduction gate below 2014. The
            // FLOAT-BINARY-128 / FLOAT-DECIMAL-16/34 non-support forms have no construct row: their operative
            // diagnostic is COBOLNET1564 (processor-dependent non-support, Annex A.3), fired by ParseUsage at every
            // edition, so a redundant 0900 introduction gate below 2014 would only add noise.
            : pu is Usage.FloatBinary32 || ou is Usage.FloatBinary32 ? Constructs.UsageFloatBinary322014
            : pu is Usage.FloatBinary64 || ou is Usage.FloatBinary64 ? Constructs.UsageFloatBinary642014
            : null;
    }

    /// <summary>
    /// Whether this <c>cobolWord</c> occurrence sits in a grammar position that is UNAMBIGUOUSLY a
    /// user-defined-word use (ISO §8.3.2.2), so a context-keyword token there is provably the program NAMING
    /// something with that word — the §8.3.2.1 rule-1 violation ("reserved words shall not be used as
    /// user-defined words"; restated §8.3.2.4.1) the 0901 band enforces. CONSERVATIVE by design (P2.8 W2 —
    /// the RW104A adversarial review): a slot qualifies only when NO clause/statement keyword admitted by
    /// <c>cobolWord</c> can legally occupy it, proven against the grammar rule by rule below. Mis-parse-prone
    /// OPTIONAL entry-name slots stay UNCHECKED — the report-group entry-name (<c>dataName?</c> under <c>reportGroupEntry</c>
    /// swallows the keyword of RW102A/103A/104A's report COLUMN clause, §13.18.14) and the screen entry-name
    /// (<c>screenName?</c> would swallow a screen attribute keyword) — as does every REFERENCE position
    /// (dataReference, qualification, procedure-name refs, DISPLAY UPON, SPECIAL-NAMES operands, …), keeping
    /// the funnel's no-false-reject guarantee (VCR scope-limit rule). Consumed by the <see cref="ParseArm"/>
    /// §8.9 funnel and by <c>ReservedWordPositionTests</c> (kept <c>internal static</c> for that witness).
    /// </summary>
    internal static bool IsProvableUserWordPosition(CobolParserCore.CobolWordContext ctx) => ctx.Parent switch
    {
        // The data-description / linkage-parameter ENTRY-NAME slot (§13.16 level-number data-name-1): the
        // slot is optional, and a cobolWord token that lands in it is always the entry's NAME.
        // ⚠ THE GROUND FOR THAT CHANGED and is re-derived here rather than inherited. It used to read "NO
        // dataDescriptionClause alternative begins with a cobolWord-admitted token", which the declined-A.4.14
        // validationClause arm (Grammar/Core/CobolDeclined.g4) made FALSE: DEFAULT, DESTINATION, PRESENT,
        // VAL-STATUS and VALIDATE-STATUS all lead an alternative AND all ride cobolWord (they must, so
        // `01 DESTINATION PIC X.` keeps drawing the named 0901 rather than a parse error). The CONCLUSION
        // survives on a stronger footing: ANTLR's prediction here is FULL-CONTEXT, so the token lands in a
        // DataNameContext only when the whole entry parses with it as the name — and a program whose entry
        // parses that way IS naming something with the word, which is the §8.3.2.1 violation the 0901 band
        // reports. The mis-parse worry that keeps the report-group and screen entry-name slots UNCHECKED (a
        // greedy `dataName?` under `reportGroupEntry` swallowing a COLUMN keyword) does not arise: each validation-clause
        // alternative diverges from the name reading within one or two tokens. The witness that it stays true
        // is `conformance:negative/declined-validate-entry-name-still-0901`.
        CobolParserCore.DataNameContext
        {
            Parent: CobolParserCore.DataDescriptionEntryContext
                or CobolParserCore.LinkageProcedureParameterContext
                // A report-section constant entry (§13.8.2): the only token that may follow the name slot is
                // CONSTANT, so a cobolWord there is always the constant's NAME (kb/Work PB1226).
                or CobolParserCore.ConstantEntryContext
        } => true,
        // A paragraph/section DEFINITION (§14.4.2/§14.4.3: section-name SECTION. / paragraph-name.): the name
        // stands at a procedure-unit boundary followed by [SECTION] DOT — no statement in cobolWord's token
        // set can begin a sentence there, so the word is the procedure's name. REFERENCES (GO TO / PERFORM /
        // RESUME AT targets) route through procedureName under OTHER parents and stay unchecked.
        CobolParserCore.ProcedureNameContext
        {
            Parent: CobolParserCore.ParagraphNameContext or CobolParserCore.SectionNameContext
        } => true,
        // The SELECT clause file-name (§12.4.5.1 general formats: SELECT [OPTIONAL] file-name-1): the name is
        // mandatory and directly follows SELECT [OPTIONAL], before any file-control clause keyword — the slot
        // is always the file being DEFINED. FD/statement file-name REFERENCES stay unchecked.
        CobolParserCore.FileNameContext { Parent: CobolParserCore.FileControlClauseGroupContext } => true,
        // The PROGRAM-ID / FUNCTION-ID / END-marker program-name (§11.10.2 / §11.5 / §10.6.1): program-names
        // are user-defined words (§8.3.2.2) and every programName site names the source unit itself, directly
        // after a dedicated header token — no keyword can occupy the slot.
        CobolParserCore.ProgramNameContext => true,
        _ => false,
    };

    /// <summary>True when <paramref name="ctx"/> is the SOLE word of a bare (unsuffixed, operator-free)
    /// function argument — the shape a §15 phrase word occupies. The walk ascends the sole-child
    /// expression spine (primary → unary → power → multiplicative → additive → arithmeticExpression);
    /// any operator, suffix, or extra operand makes the word a provable operand.
    /// <para>Shared with <c>FormatWordDriftTests</c> (kb/Work PB764): it is one of the two NON-positional reasons
    /// the §8.9 funnel lets a reserved-word <c>cobolWord</c> pass, so the drift test asks the same question.</para></summary>
    internal static bool IsBareFunctionArgumentWord(CobolParserCore.CobolWordContext ctx)
    {
        if (ctx.Parent is not CobolParserCore.DataReferenceContext dref
            || dref.dataReferenceSuffix().Length != 0)
            return false;
        Antlr4.Runtime.RuleContext? n = dref.Parent;
        while (n is CobolParserCore.PrimaryExpressionContext or CobolParserCore.UnaryExpressionContext
            or CobolParserCore.PowerExpressionContext or CobolParserCore.MultiplicativeExpressionContext
            or CobolParserCore.AdditiveExpressionContext or CobolParserCore.ArithmeticExpressionContext)
        {
            if (n.ChildCount != 1) return false;   // an operator / second operand — provably an operand
            n = n.Parent;
        }
        return n is CobolParserCore.FunctionArgumentContext;
    }

    /// <summary>
    /// The parse-tree arm of the conformance pass (rearch PHASE-03 Step 14h): the ABSORBED
    /// <c>EditionValidator</c>. It walks the raw compilation unit (ANTLR <c>-no-listener -visitor</c>, so
    /// every override returns <c>base.VisitChildren</c>/<c>base.VisitXxx</c> to keep descending) and routes
    /// every SYNTACTIC edition gate through the enclosing pass's <see cref="Check"/> — one policy, several
    /// emit sites. Recognition-based (fires on the construct's OWN tokens being present), so it names an
    /// edition even for a below-edition construct that ALSO fails to bind. Runs AFTER bind (the driver
    /// dropped the pre-bind fail-fast), so a below-edition construct surfaces BOTH its edition diagnostic and
    /// its bind diagnostics — intended (both are true; the tests are contains-based).
    /// </summary>
    private sealed class ParseArm(VersionConformancePass p, ReservedWordSet reservedWords, CobolWordsMap cobolWords)
        : CursorFollowingVisitor(p._sink)   // the cursor follows the walk (kb/Work PB82)
    {
        private readonly VersionConformancePass _p = p;
        // The effective reserved-word set for THIS compilation group (P2.4/D9 seam): the generated §8.9 table
        // composed with the 2023 >>COBOL-WORDS overlay (RESERVE/UNDEFINE/SUBSTITUTE); ReservedWordSet.Default when
        // the group has no directive (byte-identical).
        private readonly ReservedWordSet _reservedWords = reservedWords;
        // The group's >>COBOL-WORDS overrides. The reserved-word SET above answers "is this word reserved";
        // this answers "which keyword does this written word denote" - the question every TEXT-recognized
        // §8.9/§8.10 word below asks, and the one a raw string comparison gets wrong (kb/Work PB250).
        private readonly CobolWordsMap _cobolWords = cobolWords;
        // One COBOLNET0901 per distinct word per compilation (P2.4) — not one per occurrence.
        private HashSet<string>? _flaggedWords;
        // One COBOLNET1567 per distinct over-long word per compilation (the §8.3.2.1 length ceiling).
        private HashSet<string>? _overlongWords;
        // Whether the CURRENT source unit declares SOURCE-COMPUTER … WITH DEBUGGING MODE (the X3.23-1985
        // compile-time debug switch): decides the USE FOR DEBUGGING posture (VCR Table 7 row 7.17) — without it
        // a debugging section is comment-treated (never walked); with it, DEBUG-* register references diagnose
        // 0899 (the deferred facility), not 0901. Reset per top-level program unit; nestedProgram subtrees are
        // walked within the outer unit, so contained programs INHERIT the switch (the '85 rule).
        private bool _debuggingModeDeclared;

        /// <summary>Reset the per-source-unit state at each TOP-LEVEL unit (nested programs keep the outer's).</summary>
        public override object? VisitProgramUnit(CobolParserCore.ProgramUnitContext ctx)
        {
            _debuggingModeDeclared = false;
            return base.VisitChildren(ctx);
        }

        // ── P2.6 removal gates: every override routes through the pass's ONE Check (one policy) ────────────

        /// <summary>LABEL RECORDS (FD) — obsolete '85 element DELETED by ISO/IEC 1989:2002; the 2023 FD clause
        /// set (§13.18) has no LABEL clause.</summary>
        public override object? VisitLabelRecordsClause(CobolParserCore.LabelRecordsClauseContext ctx)
        {
            _p.Check(Constructs.LabelRecordsRemoved2002, "the FD LABEL RECORDS clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>VALUE OF (FD) — obsolete '85 label-field clause, deleted 2002 (P2.6).</summary>
        public override object? VisitValueOfClause(CobolParserCore.ValueOfClauseContext ctx)
        {
            _p.Check(Constructs.ValueOfRemoved2002, "the FD VALUE OF clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>DATA RECORDS (FD **and** SD — one grammar rule, so ONE enforcement site). Keeps its pinned
        /// 0873.</summary>
        public override object? VisitDataRecordsClause(CobolParserCore.DataRecordsClauseContext ctx)
        {
            _p.Check(Constructs.DataRecordsRemoved2002, "the FD/SD DATA RECORDS clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The RECORD clause's BYTES spelling (ISO §13.18.43.2, all three general formats; §13.18.43.3
        /// SR2 <i>"The words BYTES and CHARACTERS are synonymous and may be used interchangeably"</i>) — a
        /// COBOL-2023 ADDITION, gated here so `--std 85/2002/2014` does not accept a construct the targeted
        /// standard does not contain.
        /// <para>⛔ THE EDITION FACT IS DERIVED, NOT ASSUMED (kb/Work PB721). Annex E.3.3 item 13 lists BYTES
        /// among the words that "have either been added to the list of context-sensitive words or the context in
        /// which they are reserved has been expanded", and that sentence is an OR — but §8.10 gives BYTES exactly
        /// ONE construct, the RECORD clause, so there is no earlier context for 2023 to have EXPANDED and the
        /// first arm is the one that applies. CONTINUE AFTER … SECONDS is the precedent: SECONDS is in the same
        /// item-13 list and its phrase is gated the same way.</para>
        /// <para>⚖ ONLY THE WORD IS GATED, on BOTH counts. CHARACTERS is 1985-continuous and ungated, so a
        /// program that spells the clause the old way is untouched at every edition; and BYTES stays a
        /// <c>nameSlot</c> user word in <c>cobol-words.json</c>, so `01 BYTES PIC X.` remains legal COBOL-85
        /// source — the gate is on the CLAUSE, never on the word (§8.10: a context-sensitive word used where the
        /// format does not permit it "is treated as a user-defined word").</para>
        /// <para>ONE ARM COVERS FD AND SD, because <c>recordClause</c> is one grammar rule shared by the file
        /// description entry and the sort-merge file description entry — the DATA RECORDS precedent directly
        /// above.</para></summary>
        public override object? VisitRecordClause(CobolParserCore.RecordClauseContext ctx)
        {
            if (ctx.BYTES() is not null)
                _p.Check(Constructs.RecordClauseBytes2023, "the RECORD clause's BYTES phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>MULTIPLE FILE [TAPE] (I-O-CONTROL) — reel-sharing description, deleted 2002 (P2.6).</summary>
        public override object? VisitMultipleFileClause(CobolParserCore.MultipleFileClauseContext ctx)
        {
            _p.Check(Constructs.MultipleFileTapeRemoved2002, "the I-O-CONTROL MULTIPLE FILE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>PADDING CHARACTER (file control entry) — the ANSI X3.23-1985 Sequential I-O block-fill
        /// character, DELETED from the standard: the 2023 file control entry's clauses are §12.4.5.4–§12.4.5.15
        /// and none is PADDING, and the word appears nowhere in the 2023 text (§8.9's list runs PACKED-DECIMAL →
        /// PAGE). Parsed-and-ignored at 85/2002 (a compiler with no blocking model has nothing to pad — the
        /// MULTIPLE FILE / RERUN posture, and NIST SQ216A/SQ217A both write the clause), gated here from 2014.
        /// <para>⛔ THE ABSENCE OF THIS ARM WAS THE DEFECT (kb/Work PB300): the clause parsed at EVERY edition
        /// with nothing reading the rule, so `--std 2023` accepted a clause the targeted standard does not
        /// contain and said nothing — the one leniency in the compiler that was on neither dialect axis.</para></summary>
        public override object? VisitPaddingCharacterClause(CobolParserCore.PaddingCharacterClauseContext ctx)
        {
            _p.Check(Constructs.PaddingCharacterRemoved2014, "the SELECT PADDING CHARACTER clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The three X3.23-1985 SOURCE-/OBJECT-COMPUTER clauses ISO 2002 deleted — MEMORY SIZE, SEGMENT-LIMIT and
        /// WITH DEBUGGING MODE, each its own registry row / VCR item (7.7, 7.8, 7.9). ⛔ They are MODELLED clauses now
        /// (kb/Work PB830): the gate reads the parsed NODE. They used to be a token-TEXT scan over the
        /// <c>computerAttributes</c> sink, which ran to the period and so also admitted any other words at every
        /// edition in silence.</summary>
        public override object? VisitMemorySizeClause(CobolParserCore.MemorySizeClauseContext ctx)
        {
            _p.Check(Constructs.MemorySizeRemoved2002, "the OBJECT-COMPUTER MEMORY SIZE clause");
            return base.VisitChildren(ctx);
        }

        public override object? VisitSegmentLimitClause(CobolParserCore.SegmentLimitClauseContext ctx)
        {
            _p.Check(Constructs.SegmentLimitRemoved2002, "the OBJECT-COMPUTER SEGMENT-LIMIT clause");
            return base.VisitChildren(ctx);
        }

        public override object? VisitDebuggingModeClause(CobolParserCore.DebuggingModeClauseContext ctx)
        {
            _p.Check(Constructs.DebuggingModeRemoved2014, "the SOURCE-COMPUTER WITH DEBUGGING MODE clause");
            // The switch also drives the USE FOR DEBUGGING posture (row 7.17): the configuration section precedes
            // the procedure division in the walk, so the flag is set before any declarative section is visited.
            _debuggingModeDeclared = true;
            return base.VisitChildren(ctx);
        }

        /// <summary>The five identification comment paragraphs — obsolete '85 elements deleted 2002 (P2.6; one
        /// registry row, the paragraph named per site).</summary>
        public override object? VisitAuthorParagraph(CobolParserCore.AuthorParagraphContext ctx)
        { _p.Check(Constructs.IdentificationCommentsRemoved2002, "the AUTHOR paragraph"); return base.VisitChildren(ctx); }
        public override object? VisitInstallationParagraph(CobolParserCore.InstallationParagraphContext ctx)
        { _p.Check(Constructs.IdentificationCommentsRemoved2002, "the INSTALLATION paragraph"); return base.VisitChildren(ctx); }
        public override object? VisitDateWrittenParagraph(CobolParserCore.DateWrittenParagraphContext ctx)
        { _p.Check(Constructs.IdentificationCommentsRemoved2002, "the DATE-WRITTEN paragraph"); return base.VisitChildren(ctx); }
        public override object? VisitDateCompiledParagraph(CobolParserCore.DateCompiledParagraphContext ctx)
        { _p.Check(Constructs.IdentificationCommentsRemoved2002, "the DATE-COMPILED paragraph"); return base.VisitChildren(ctx); }
        public override object? VisitSecurityParagraph(CobolParserCore.SecurityParagraphContext ctx)
        { _p.Check(Constructs.IdentificationCommentsRemoved2002, "the SECURITY paragraph"); return base.VisitChildren(ctx); }

        /// <summary>REMARKS — a '74 carryover the grammar accepts for CCVS; flagged ≥2002 ONLY (never at 85 —
        /// CCVS-85 programs write it; the 85 FIPS flagger is future strictness work, P2.6).</summary>
        public override object? VisitRemarksParagraph(CobolParserCore.RemarksParagraphContext ctx)
        { _p.Check(Constructs.RemarksRemoved2002, "the REMARKS paragraph"); return base.VisitChildren(ctx); }

        /// <summary>STOP literal (Format 2) — obsolete '85 element deleted 2002 (P2.6). The STOP RUN … WITH
        /// STATUS phrase (Format 1, a 2002 INTRODUCTION) is the disjoint alternative, gated in the same
        /// override (14h.3).</summary>
        public override object? VisitStopStatement(CobolParserCore.StopStatementContext ctx)
        {
            if (ctx.literal() is not null)
                _p.Check(Constructs.StopLiteralRemoved2002, "the STOP literal statement");
            // STOP RUN … WITH NORMAL/ERROR STATUS (Format 1) — a COBOL-2002 INTRODUCTION (14h.3), a disjoint
            // alternative from the STOP literal (Format 2) removal above; both fire from this one override.
            if (ctx.statusPhrase() is not null)
                _p.Check(Constructs.StopRunStatus2002, "the STOP RUN … WITH NORMAL/ERROR STATUS phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>USAGE … WITH NO SIGN (ISO §13.18.60.4 GR11) — a COBOL-2023 addition. Recognition-based on the
        /// parsed noSignPhrase (NO SIGN is a modifier on Usage.Packed, which exists at every edition, so it cannot
        /// be keyed on the resolved Usage enum). The binder separately rejects NO SIGN on a non-Packed usage (1565)
        /// and an 'S' picture (SR31, 1566); this arm owns only the below-2023 introduction gate.
        /// <para>The USAGE clause's float FORMAT phrases (endianness-phrase / encoding-phrase, ISO §13.18.60.2
        /// general format) are a COBOL-2014 addition and gate from the SAME override — a disjoint alternative, the
        /// STOP-statement precedent. Recognition-based for the same reason: the phrase is a MODIFIER, and the
        /// grammar tolerates it after any usageKeyword (the binder narrows — COBOLNET1716/1706/1707), so the
        /// resolved Usage enum cannot key it. A below-2014 program that writes it on a standard float usage draws
        /// this gate AND the usage keyword's own (usage-float-binary32-2014 etc.): two distinct 2014 language
        /// elements were written, and Annex A.3 item 18 names the phrase separately from item 17's usages.</para></summary>
        public override object? VisitUsageClause(CobolParserCore.UsageClauseContext ctx)
        {
            if (ctx.noSignPhrase() is not null)
                _p.Check(Constructs.UsagePackedNoSign2023, "USAGE PACKED-DECIMAL WITH NO SIGN");
            if (ctx.floatFormatPhrase().Length > 0)
                _p.Check(Constructs.UsageFloatFormatPhrase2014,
                    "the USAGE clause's endianness-phrase / encoding-phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>CONTINUE AFTER arithmetic-expression SECONDS (ISO §14.9.9) — the timed-pause phrase is a
        /// COBOL-2023 addition; plain CONTINUE is 1985-continuous. Recognition-based on the phrase's presence (the
        /// arithmeticExpression child ≡ the AFTER … SECONDS phrase); parse-arm so a below-edition occurrence names
        /// its edition even though the phrase would otherwise bind to a no-op (DEVLOG 724).</summary>
        public override object? VisitContinueStatement(CobolParserCore.ContinueStatementContext ctx)
        {
            if (ctx.arithmeticExpression() is not null)
                _p.Check(Constructs.ContinueAfter2023, "the CONTINUE AFTER … SECONDS phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>WRITE … BEFORE AFTER ADVANCING … (ISO Annex §E.3.3 item 2 — <i>"Both BEFORE and AFTER are
        /// allowed together in WRITE ADVANCING"</i>) — specifying BOTH words on one WRITE is a COBOL-2023 addition.
        /// Gate on the CO-OCCURRENCE OF THE TWO WORDS, not on ADVANCING itself — a single BEFORE or AFTER is
        /// edition-invariant. ⛔ This used to read <c>writeAdvancePhrase().Length == 2</c>, gating on a SECOND
        /// ADVANCING PHRASE that §14.9.51.2 Format 1 never prints (kb/Work PB712): the phrase's choice indicators
        /// enclose the two WORDS and there is exactly one <c>ADVANCING</c> operand. Recognition-based (DEVLOG 724).</summary>
        public override object? VisitWriteBeforeAfter(CobolParserCore.WriteBeforeAfterContext ctx)
        {
            if (ctx.BEFORE() is not null && ctx.AFTER() is not null)
                _p.Check(Constructs.WriteBeforeAndAfterAdvancing2023, "the combined WRITE BEFORE AFTER ADVANCING phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>PERFORM … UNTIL EXIT (ISO §14.9.28.4 GR11) — the infinite-loop phrase is a COBOL-2023 addition
        /// (plain UNTIL condition is edition-invariant). Recognition-based on the EXIT alternative of performUntil;
        /// parse-arm so a below-2023 occurrence names its edition even though it drops to a bound control node.</summary>
        public override object? VisitPerformUntil(CobolParserCore.PerformUntilContext ctx)
        {
            if (ctx.EXIT() is not null)
                _p.Check(Constructs.PerformUntilExit2023, "the PERFORM UNTIL EXIT phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>The Format-3 (exception-checking) PERFORM (ISO §14.9.28.2 Format 3) — a COBOL-2023 addition.
        /// Recognition-based on any WHEN / WHEN OTHER / WHEN COMMON / FINALLY phrase or a [WITH] LOCATION head (the
        /// same discriminator the binder uses); disjoint from the UNTIL EXIT gate above. Parse-arm so a below-2023
        /// occurrence names its edition even though it binds to a BoundExceptionPerform.</summary>
        public override object? VisitPerformStatement(CobolParserCore.PerformStatementContext ctx)
        {
            // The ONE Format-3 discriminator, shared with the binder (PerformFormat.IsFormat3), so the
            // COBOLNET0900 gate here and the COBOLNET0899 staged-reject there cannot drift apart.
            if (PerformFormat.IsFormat3(ctx))
                _p.Check(Constructs.PerformExceptionChecking2023, "the Format-3 (exception-checking) PERFORM");
            return base.VisitChildren(ctx);
        }

        /// <summary>OPEN … REVERSED — obsolete '85 tape phrase deleted 2002 (P2.6; NO REWIND stays — it survives
        /// into 2023 §14.9.26).</summary>
        public override object? VisitOpenFileSpec(CobolParserCore.OpenFileSpecContext ctx)
        {
            if (ctx.REVERSED() is not null)
                _p.Check(Constructs.OpenReversedRemoved2002, "the OPEN REVERSED phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>CLOSE … WITH LOCK — REMOVED 2014→2023 (Annex E deletion; VCR row 7; P2.6).</summary>
        public override object? VisitCloseOption(CobolParserCore.CloseOptionContext ctx)
        {
            if (ctx.LOCK() is not null)
                _p.Check(Constructs.CloseWithLockRemoved2023, "the CLOSE WITH LOCK phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>EXIT METHOD / EXIT FUNCTION — introduced 2002 (0900 below), REMOVED 2023 (Annex E; VCR rows
        /// 5/6) — the dual-obligation window rows; EXIT PROGRAM — ARCHAIC in 2023 (0903 warning; VCR 89).</summary>
        public override object? VisitExitStatement(CobolParserCore.ExitStatementContext ctx)
        {
            if (ctx.METHOD() is not null)
                _p.Check(Constructs.ExitMethodWindow, "the EXIT METHOD statement");
            else if (ctx.FUNCTION() is not null)
                _p.Check(Constructs.ExitFunctionWindow, "the EXIT FUNCTION statement");
            else if (ctx.PROGRAM() is not null)
                _p.Check(Constructs.ExitProgramArchaic2023, "the EXIT PROGRAM statement");
            else if (ctx.SECTION() is not null)   // §14.9.14 Format 4 — a COBOL-2002 structured-exit introduction
                _p.Check(Constructs.ExitSection2002, "the EXIT SECTION statement");
            else if (ctx.PARAGRAPH() is not null)   // §14.9.14.2 Format 4 — the COBOL-2002 EXIT PARAGRAPH twin of EXIT SECTION
                _p.Check(Constructs.ExitParagraph2002, "the EXIT PARAGRAPH statement");
            else if (ctx.PERFORM() is not null)     // §14.9.14.2 Format 3 — EXIT PERFORM [CYCLE], a COBOL-2002 structured exit
                _p.Check(Constructs.ExitPerform2002, "the EXIT PERFORM statement");
            return base.VisitChildren(ctx);
        }

        /// <summary>NEXT SENTENCE — ARCHAIC in 2023 (0903 warning; VCR 90; P2.6).</summary>
        public override object? VisitNextSentenceStatement(CobolParserCore.NextSentenceStatementContext ctx)
        {
            _p.Check(Constructs.NextSentenceArchaic2023, "the NEXT SENTENCE phrase");
            return base.VisitChildren(ctx);
        }

        // ── The W3 notInGrammar 85-acceptance gates (VCR Table 7 rows 7.15–7.18; DEVLOG 599): four obsolete '85
        //    elements DELETED by ISO 2002 that formerly had no grammar at all. Each now parses unconditionally,
        //    binds inert at 85, and gates here. ──────────────────────────────────────────────────────────────

        /// <summary>RERUN (I-O-CONTROL) — the '85 checkpoint hint, deleted 2002. Parsed-and-ignored at 85.</summary>
        public override object? VisitRerunClause(CobolParserCore.RerunClauseContext ctx)
        {
            _p.Check(Constructs.RerunRemoved2002, "the I-O-CONTROL RERUN clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>ENTER — the '85 other-language entry statement, deleted 2002. Comment-equivalent (BoundNop)
        /// at 85 — the conforming COBOL-only posture.</summary>
        public override object? VisitEnterStatement(CobolParserCore.EnterStatementContext ctx)
        {
            _p.Check(Constructs.EnterRemoved2002, "the ENTER statement");
            return base.VisitChildren(ctx);
        }

        /// <summary>USE FOR DEBUGGING — the '85 debug facility's declarative, deleted 2002 with the whole
        /// facility. Sub-shape gate on the one useStatement rule (the STOP-literal pattern).</summary>
        public override object? VisitUseStatement(CobolParserCore.UseStatementContext ctx)
        {
            if (ctx.DEBUGGING() is not null)
                _p.Check(Constructs.UseForDebuggingRemoved2002, "the USE FOR DEBUGGING declarative");
            // The EC-model declarative formats (Exec Step E — folded from the ProcedureTableBuilder inline
            // gates): F4 = USE AFTER {EXCEPTION OBJECT | EO} (§14.9.49.2, the EC-OO selector); F3 = USE AFTER
            // {EXCEPTION CONDITION | EC} (§14.9.49.2). Both are 2002 introductions, recognized from the
            // alternative's own tokens (DEVLOG 724 — recognition-based, never a bound-arm drop).
            if (ctx.OBJECT() is not null || ctx.EO() is not null)
                _p.Check(Constructs.UseAfterExceptionObject2002, "USE AFTER EXCEPTION OBJECT (Format 4)");
            else if (ctx.CONDITION() is not null || ctx.EC() is not null)
                _p.Check(Constructs.UseAfterExceptionCondition2002, "USE AFTER EXCEPTION CONDITION (Format 3)");
            return base.VisitChildren(ctx);
        }

        /// <summary>A section-header segment-number — the '85 Segmentation module's priority number, deleted
        /// 2002. Parsed and ignored at 85 (all segments resident — a conforming posture).</summary>
        public override object? VisitSectionDefinition(CobolParserCore.SectionDefinitionContext ctx)
        {
            if (ctx.integerLiteral() is not null)
                _p.Check(Constructs.SegmentNumbersRemoved2002,
                    $"the segment-number on section '{ctx.sectionName().GetText()}'");
            return base.VisitChildren(ctx);
        }

        /// <summary>The declarative-section twin of <see cref="VisitSectionDefinition"/> (one registry row, the
        /// site named per header). ALSO the '85 comment-treatment seam (row 7.17): without WITH DEBUGGING MODE,
        /// X3.23-1985 compiles a USE FOR DEBUGGING section as if it were comment lines — so the walk visits ONLY
        /// the USE statement (its ≥2002 0902 gate must still fire) and never the section body, keeping the §8.9
        /// funnel off the unimplemented DEBUG-* register references inside (DB103M is the corpus witness).</summary>
        public override object? VisitDeclarativeSection(CobolParserCore.DeclarativeSectionContext ctx)
        {
            if (ctx.integerLiteral() is not null)
                _p.Check(Constructs.SegmentNumbersRemoved2002,
                    $"the segment-number on declarative section '{ctx.sectionName().GetText()}'");
            if (!_debuggingModeDeclared
                && ctx.sentence() is { Length: > 0 } sentences
                && sentences[0].statement() is { Length: 1 } first
                && first[0].useStatement() is { } use && use.DEBUGGING() is not null)
            {
                Visit(use);
                return null;
            }
            return base.VisitChildren(ctx);
        }

        // ── Step 14g.2: the data-description-clause introduction gates (BASED / TYPE / PROPERTY / TYPEDEF) ─────
        // COBOL-2002 introductions on a data-description entry, ALL parse-arm (recognition). None needs a resolved
        // fact — each is a single dataDescriptionClause alternative, so the parse node IS the identity — and a
        // bound-arm home would DROP the 0900 on a declaration-error path (the DEVLOG-724 flaw): DataItem.IsBased is
        // reset to false for a LINKAGE item (DataBinder.Linkage.cs); DataItem.TypeRefName is nulled by ExpandTypes
        // the moment the clone is materialized; the PROPERTY identity is consumed entirely by the OO property binder;
        // and — the 14g.2-review correction (DEVLOG 734) — although DataItem.IsTypedef is init-only, the typedef
        // ITEM is discarded from ConformanceForest whenever RegisterTypeDecl rejects it (an unnamed/FILLER typedef, a
        // duplicate type-name) or it binds into a method's LocalRoots/LinkageRoots (off the forest), so a bound-arm
        // TYPEDEF gate silently dropped the 0900 on exactly those paths. Recognition fixes all four uniformly: the
        // parse node is always present, so each fires once per written clause with the former BindEntry site's exact
        // constructId + where-string (byte-identical). The InGatedDataEntry guard reproduces the binder's reach — it
        // fires only inside a non-66/88 dataDescriptionEntry (BindEntries intercepts level-66/88 BEFORE the clause
        // loop, and these four clauses never appear in a report/screen entry), so a clause mis-attached to a
        // condition/renames entry, where the 0900 would be spurious, is skipped.

        /// <summary>The BASED clause (ISO §13.18.5) — a COBOL-2002 introduction (a storage template with an implicit
        /// data-address pointer). The level-01/77 placement SR stays in the binder; this arm only names the edition.</summary>
        public override object? VisitBasedClause(CobolParserCore.BasedClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.BasedClause2002, "the BASED clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The ALIGNED clause (ISO §13.18.1) — a COBOL-2002 introduction (§8.9 reserves ALIGNED from 2002;
        /// it aligns a bit group item or elementary bit data item on the first bit of the first available byte,
        /// §13.18.1.4 GR1). Parse-arm (recognition) like the BASED / ANY LENGTH gates: <c>DataItem.IsAligned</c> is
        /// CLEARED by <c>CheckAlignedClauses</c> on a §13.18.1.3 SR1 violation, so a bound-arm home would drop the
        /// 0900 on exactly the declaration-error paths. The SR1 subject rule stays bind-time (COBOLNET1942) and the
        /// GR1/GR2 layout effect lives at the ONE bit-layout site (kb/Work PB487).</summary>
        public override object? VisitAlignedClause(CobolParserCore.AlignedClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.AlignedClause2002, "the ALIGNED clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The GROUP-USAGE clause (ISO §13.18.29) — a COBOL-2002 introduction (a bit group / national group,
        /// data-model design D20; kb/Work PB79). SR1–SR3 stay in the binder (COBOLNET1653 + the shared §13.18.60.4 GR1
        /// leaf conformance); this arm only names the edition.</summary>
        public override object? VisitGroupUsageClause(CobolParserCore.GroupUsageClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.GroupUsageClause2002, "the GROUP-USAGE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The ANY LENGTH clause (ISO §13.18.2) — a COBOL-2002 introduction (a LINKAGE item whose length
        /// tracks the corresponding argument at runtime, GR1). Parse-arm (recognition) like the BASED gate:
        /// <c>DataItem.IsAnyLength</c> is cleared by the binder on every SR1/SR2/SR3/SR4 shape violation, so a
        /// bound-arm home would drop the 0900 on exactly the declaration-error paths. The §13.18.2 placement SRs
        /// stay bind-time (DataBinder.BindEntry + the unit/method sweeps).</summary>
        public override object? VisitAnyLengthClause(CobolParserCore.AnyLengthClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.AnyLengthClause2002, "the ANY LENGTH clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The DYNAMIC LENGTH clause (ISO §8.5.1.10 / §13.18.19) — a COBOL-2014 introduction (a
        /// variable-length, minimum-length-zero PIC X/N elementary string). Parse-arm (recognition) like the ANY
        /// LENGTH gate: <c>DataItem.IsDynamicLength</c> is cleared by the binder on every SR1/SR18 shape violation,
        /// so a bound-arm home would drop the 0900 on exactly the declaration-error paths. The §13.18.19.3 /
        /// §13.16.3 SR18 shape SRs stay bind-time (DataBinder.BindEntry, COBOLNET1561/1563/2258).</summary>
        public override object? VisitDynamicLengthClause(CobolParserCore.DynamicLengthClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.DynamicLengthItem2014, "the DYNAMIC LENGTH clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The PICTURE EDITING phrase (ISO §13.18.40.2 Format 1) — a COBOL-2023 introduction (user-defined
        /// picture editing via the new reserved word EDITING; Annex E.3.3 item 19). Recognition-based on the presence
        /// of an <c>editingPhrase</c> under the picture clause: the phrase is a purely syntactic 2023 marker wherever
        /// it appears, and <c>PictureAnalyzer</c> RECOVERS the item on the render-staged (sign-control / multi-char /
        /// floating) forms, so a bound-arm gate would drop the 0900 on those paths. NOT <c>InGatedDataEntry</c>-guarded —
        /// unlike BASED/ANY LENGTH, a PICTURE clause legally carries EDITING in the report section too. Fires once per
        /// editing-bearing PICTURE clause; the §13.18.40.3 SR8–SR25 shape rules stay bind-time (PictureAnalyzer,
        /// COBOLNET1591–1602).</summary>
        public override object? VisitPictureClause(CobolParserCore.PictureClauseContext ctx)
        {
            if (ctx.editingPhrase().Length > 0) _p.Check(Constructs.PictureEditing2023, "the PICTURE EDITING phrase");
            // PICTURE format 2 (the LOCALE phrase, §13.18.40.2) — a COBOL-2002 introduction with the locale
            // facility (A.4.9 item 8; kb/Work PB64 T6). Recognition-based here in the PARSE arm because the
            // binder recovers/clears on every SR32–SR37 violation, so a bound-arm gate would drop the 0900 on
            // exactly those paths. NOT also in PictureConstructId — the two arms are disjoint by the class
            // contract and a double gate doubles the diagnostic.
            if (ctx.pictureLocalePhrase() is not null) _p.Check(Constructs.PictureLocaleFormat22002, "the PICTURE LOCALE phrase (format 2)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The Format 2 (table) VALUE clause (ISO §13.18.40 → §13.18.63.2) — a COBOL-2002 introduction
        /// (literals keyed to OCCURS occurrences by a mandatory FROM (subscript) phrase). Recognition-based on the
        /// presence of a <c>valueClauseTablePhrase</c>: the binder DROPS the table spec on every §13.18.63.3 SR
        /// violation (no OCCURS, bad subscript, …), so a bound-arm gate would lose the 0900 on those paths — the
        /// TYPEDEF/ANY LENGTH drop-proof lesson. Fires once per written table VALUE; the SR16–SR23 shape rules stay
        /// bind-time (DataBinder, COBOLNET1585–1590).</summary>
        public override object? VisitValueClause(CobolParserCore.ValueClauseContext ctx)
        {
            if (ctx.valueClauseTablePhrase().Length > 0) _p.Check(Constructs.ValueTableFormat2002, "the Format 2 (table) VALUE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The Format 3 <c>[ WHEN SET TO FALSE IS literal-4 ]</c> phrase (ISO §13.18.63.2 format 3) — a
        /// COBOL-2002 introduction, with the <c>SET … TO FALSE</c> arm it exists to feed (§14.9.39.4 GR7 /
        /// §13.18.63.4 GR20). Recognition-based on the dedicated <c>valueClauseFalsePhrase</c> node for the same
        /// drop-proof reason as the table format above: the binder abandons literal-4 on a §13.18.63.3 SR2/SR4/SR5
        /// category violation and on a non-literal operand, so a bound-arm gate would lose the 0900 on exactly
        /// the paths that already went wrong. kb/Work PB555.
        /// <para>⚠ The edge is DERIVED, not quoted, and the derivation is one line so it can be overturned in
        /// one: the repo holds no 2002 or 2014 text, the 2023 Annex E carries no 85→2002 VALUE row (the A.1
        /// authority gap, D18), and the reserved-word evidence cannot date it because FALSE is already reserved
        /// at COBOL-85 for the EVALUATE statement.</para></summary>
        public override object? VisitValueClauseFalsePhrase(CobolParserCore.ValueClauseFalsePhraseContext ctx)
        {
            _p.Check(Constructs.ValueFalsePhrase2002, "the VALUE clause's WHEN SET TO FALSE phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary><c>SET condition-name+ TO FALSE</c> (ISO §14.9.39 Format 4's FALSE arm) — a COBOL-2002
        /// introduction on the same derived authority as its VALUE-clause half; COBOL-85's Format 4 carries the
        /// TRUE arm only, which is why the gate keys on the FALSE_ token and not on the statement. Recognition-
        /// based so it survives the §14.9.39.3 SR7 refusal (COBOLNET2049) the binder may raise on the same
        /// statement.</summary>
        public override object? VisitSetBooleanStatement(CobolParserCore.SetBooleanStatementContext ctx)
        {
            // ⛔ THE TOKEN IS ON THE PHRASE, AND A STATEMENT MAY CARRY SEVERAL (kb/Work PB450): §14.9.39.2
            // Format 4's outer `…` repeats the whole `{ condition-name-1 } … TO { TRUE | FALSE }` unit, so
            // `SET A TO TRUE B TO FALSE` writes ONE FALSE arm inside the second group. Asking the statement
            // node for FALSE_ after the phrase rule landed would have found none and let the COBOL-2002
            // introduction through unmeasured at --std 85.
            if (ctx.setConditionPhrase().Any(p => p.FALSE_() is not null))
                _p.Check(Constructs.SetConditionFalse2002, "SET condition-name TO FALSE");
            return base.VisitChildren(ctx);
        }

        /// <summary>The TYPE [TO] type-name clause (ISO §13.18.57 Format 1; the TYPEDEF family, §13.18.58; D17) — a
        /// COBOL-2002 introduction. Fires once per written <c>TYPE</c> occurrence: the ExpandTypes clones are DataItem objects, not parse
        /// nodes, so a TYPEDEF referenced N times yields exactly N typeClause nodes (matching the former per-entry
        /// binder Check). The §13.18.57.3 placement SRs stay bind-time.</summary>
        public override object? VisitTypeClause(CobolParserCore.TypeClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.TypeClause2002, "the TYPE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The PROPERTY clause (ISO §13.18.42, OO) — a COBOL-2002 introduction. The OO property SEMANTICS
        /// are bound independently in <c>DataBinder.Oo.OoBindPropertyClauses</c> (which reads the propertyClause node
        /// directly), so the storage-clause loop no longer touches it; this arm only gates the edition.</summary>
        public override object? VisitPropertyClause(CobolParserCore.PropertyClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.PropertyClause2002, "the PROPERTY clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>A constant entry (ISO §13.10 — <c>01 name CONSTANT …</c>) — a COBOL-2002 introduction.
        /// Recognition-based on the dedicated <c>constantEntryBody</c> alternative: the binder folds the entry
        /// into the compile-time constant table and produces NO DataItem (a bound-arm gate would have no node
        /// to key on), and a §13.10 SR violation abandons the fold — so recognition is the drop-proof home
        /// (the TYPEDEF lesson). Fires once per written entry; the §13.10 SRs stay bind-time
        /// (DataBinder.Constants.cs, COBOLNET1547).</summary>
        public override object? VisitConstantEntryBody(CobolParserCore.ConstantEntryBodyContext ctx)
        {
            // The body has two hosts: a data description entry (through its body rule) and a report-section
            // constant entry (§13.8.2, kb/Work PB1226) — the same entry, spelled under its own rule.
            CobolParserCore.DataNameContext? nameCtx = ctx.Parent switch
            {
                CobolParserCore.DataDescriptionBodyContext { Parent: CobolParserCore.DataDescriptionEntryContext e } => e.dataName(),
                CobolParserCore.ConstantEntryContext c => c.dataName(),
                _ => null,
            };
            string name = nameCtx?.GetText() ?? "?";
            _p.Check(Constructs.ConstantEntry2002, $"the constant entry '{name}' (01 … CONSTANT)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The CONSTANT RECORD clause (ISO §13.18.15) — a COBOL-2002 introduction (a structured
        /// constant). Recognition-based like the BASED/ANY LENGTH gates: <c>DataItem.IsConstantRecord</c> is
        /// cleared by the binder on every §13.16.3 SR3/SR6/SR13 shape violation, so a bound-arm home would drop
        /// the 0900 on exactly the declaration-error paths. The placement SRs stay bind-time (COBOLNET1549).</summary>
        public override object? VisitConstantRecordClause(CobolParserCore.ConstantRecordClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.ConstantRecord2002, "the CONSTANT RECORD clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The SAME AS clause (ISO §13.18.49; P10 Step 16) — a COBOL-2002 introduction (the
        /// TYPEDEF-family data-description edge). Recognition-based (the typedefClause pattern): the subject's
        /// SameAsName is nulled by ExpandSameAs during bind — and cleared entirely on a §13.16.3 SR12
        /// composition violation — so a bound-arm gate would drop the 0900 on exactly those paths. One Check
        /// per written SAME AS clause.</summary>
        public override object? VisitSameAsClause(CobolParserCore.SameAsClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.SameAsClause2002, "the SAME AS clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The TYPEDEF [STRONG] clause (ISO §13.18.58; D17) — a COBOL-2002 introduction (a type DECLARATION).
        /// Recognition-based (the 14g.2-review correction, DEVLOG 734), NOT bound-arm: the typedef ITEM is dropped
        /// from ConformanceForest whenever RegisterTypeDecl rejects it (unnamed/FILLER, duplicate type-name) or it
        /// binds into method LocalRoots/LinkageRoots, so a bound-arm gate lost the 0900 on those declaration-error
        /// paths. The parse node is always present — one Check per written TYPEDEF, matching the former binder site.</summary>
        public override object? VisitTypedefClause(CobolParserCore.TypedefClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.TypedefDef2002, "the TYPEDEF clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>A strongly-typed EXTERNAL type declaration (ISO §13.18.22.3 SR1/SR5; §8.5.3; Annex E.3.3 item 20) —
        /// a COBOL-2023 introduction: E.3.3 item 20 ("External data items may now be strongly typed"), corroborated by
        /// E.2 item 10 ("previously external items could not be strongly typed"), so
        /// it is specifically the <c>STRONG</c>+<c>EXTERNAL</c> combination on a level-1 TYPEDEF that is new. A WEAK
        /// <c>TYPEDEF IS EXTERNAL</c> (no STRONG) was already valid in COBOL-2002 (§13.18.58.3 SR3), so it is NOT gated
        /// here; plain EXTERNAL is 1985-continuous and plain TYPEDEF is the separate COBOL-2002 gate above. Gate on the
        /// CO-OCCURRENCE of the EXTERNAL clause with a STRONG TYPEDEF clause on ONE dataDescriptionEntry. SR5 forces a
        /// strongly-typed external record's type to itself be a strong external type declaration, so gating the
        /// DECLARATION covers both faces (the declaration AND the "strongly-typed external item" that must reference
        /// it). Recognition-based, keyed on the externalClause node (the TYPEDEF/DEVLOG-734 drop-proof lesson: a
        /// bound-arm gate would lose the 0900 whenever RegisterTypeDecl rejects the typedef). Fires once per written
        /// strong-external-typedef entry.</summary>
        public override object? VisitExternalClause(CobolParserCore.ExternalClauseContext ctx)
        {
            if (InGatedDataEntry(ctx) && EntryHasStrongTypedef(ctx))
                _p.Check(Constructs.ExternalTypeDeclaration2023, "a strongly-typed EXTERNAL type declaration");
            return base.VisitChildren(ctx);
        }

        /// <summary>Whether the dataDescriptionEntry enclosing <paramref name="ctx"/> also carries a <c>TYPEDEF STRONG</c>
        /// clause — i.e. <paramref name="ctx"/> (an externalClause) sits on a STRONGLY-TYPED type DECLARATION (the 2023
        /// combination, E.3 item 10), not a plain external data item nor a weak (COBOL-2002) external type declaration.
        /// externalClause and typedefClause are both <c>dataDescriptionClause</c> alternatives under the entry's
        /// <c>dataDescriptionClauses</c> list, so the sibling scan is over that list; <c>typedefClause.STRONG()</c> is
        /// the STRONG phrase (the same test <c>DataBinder</c> uses at its typedef bind, §13.18.58.2).</summary>
        private static bool EntryHasStrongTypedef(Antlr4.Runtime.RuleContext ctx)
        {
            for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.DataDescriptionEntryContext e)
                    return e.dataDescriptionBody()?.dataDescriptionClauses()?.dataDescriptionClause()
                        ?.Any(c => c.typedefClause()?.STRONG() is not null) ?? false;
            return false;
        }

        /// <summary>The report-group PRESENT WHEN clause (ISO §13.18.41 Format 1; P10 Step 13) — a COBOL-2002
        /// introduction (the 2002 RW modernization; PRESENT itself is §8.9-reserved "added 2002").
        /// Recognition-based: the rule is report-section-exclusive (never shared with data/screen entries), and
        /// the binder drops the condition on its own staging paths, so recognition is the drop-proof home.</summary>
        public override object? VisitReportPresentWhenClause(CobolParserCore.ReportPresentWhenClauseContext ctx)
        {
            _p.Check(Constructs.ReportPresentWhen2002, "the PRESENT WHEN clause (report group description)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The report-group VARYING clause (ISO §13.18.64; P10 Step 13) — a COBOL-2002 introduction
        /// (the repetition-counter half of the 2002 RW modernization). Recognition-based; report-section-exclusive
        /// rule. The §13.18.64.3 SRs stay bind-time (COBOLNET1559).</summary>
        public override object? VisitReportVaryingClause(CobolParserCore.ReportVaryingClauseContext ctx)
        {
            _p.Check(Constructs.ReportVarying2002, "the VARYING clause (report group description)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The 2002 COLUMN-clause forms (ISO §13.18.14 Format 1; P10 Step 13): more than one operand
        /// (the SR10 "multiple COLUMN clause"), a relative PLUS operand, or the COL/COLS/COLUMNS/NUMBERS/ARE
        /// spellings — the COBOL-85 form was exactly <c>COLUMN NUMBER IS integer-1</c>. Fires at most once per
        /// written clause; report-section-exclusive rule.</summary>
        public override object? VisitReportColumnClause(CobolParserCore.ReportColumnClauseContext ctx)
        {
            if (ctx.COL() is not null || ctx.COLS() is not null || ctx.COLUMNS() is not null
                || ctx.NUMBERS() is not null || ctx.ARE() is not null
                || ctx.reportColumnOperand().Length > 1
                || ctx.reportColumnOperand().Any(o => o.reportRelativeSign() is not null))
                _p.Check(Constructs.ReportMultiColumn2002, "the multiple/relative COLUMN clause forms (report group description)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The 2002 SOURCE-clause forms (ISO §13.18.53.2; kb/Work PB506): more than one operand (the SR6
        /// multi-operand form, distributed over the entry's repetitions by §13.18.53.4 GR4) or the SOURCES/ARE
        /// spellings — the COBOL-85 form was exactly <c>SOURCE IS identifier-1</c>, matching the single-operand
        /// LINE and COLUMN clauses it repeats with. Fires at most once per written clause; report-section-exclusive
        /// rule, so recognition is the drop-proof home (the VisitReportColumnClause precedent).</summary>
        public override object? VisitReportSourceClause(CobolParserCore.ReportSourceClauseContext ctx)
        {
            if (ctx.SOURCES() is not null || ctx.ARE() is not null || ctx.reportValueOperand().Length > 1)
                _p.Check(Constructs.ReportMultiSource2002, "the multiple SOURCE operand / SOURCES clause forms (report group description)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The 2002 LINE-clause forms (ISO §13.18.35 Format 1; P10 Step 13): more than one operand
        /// (the SR10 "multiple LINE clause") or the LINES/NUMBERS/ARE spellings — the COBOL-85 form was
        /// <c>LINE NUMBER IS</c> with ONE operand. The repetition itself also stages LOUD at bind
        /// (COBOLNET0899 report-multiple-line). Report-section-exclusive rule.</summary>
        public override object? VisitReportLineClause(CobolParserCore.ReportLineClauseContext ctx)
        {
            if (ctx.LINES() is not null || ctx.NUMBERS() is not null || ctx.ARE() is not null
                || ctx.reportLineOperand().Length > 1)
                _p.Check(Constructs.ReportMultiLine2002, "the multiple LINE clause form (report group description)");
            return base.VisitChildren(ctx);
        }

        /// <summary>Whether <paramref name="ctx"/> (a data-description clause) sits inside a real
        /// <c>dataDescriptionEntry</c> whose level is NEITHER 66 nor 88 — i.e. exactly the entries the binder routes
        /// through <c>DataBinder.BindEntry</c>'s storage-clause loop (the former gate site). It excludes two families:
        /// (1) a level-66 RENAMES / level-88 condition-name entry, which <c>BindEntries</c> intercepts BEFORE the
        /// clause loop (<c>lvl is 66 or 88</c> → continue), so a clause mis-attached to one was never gated; and
        /// (2) — the 14g.3-review correction (DEVLOG 736) — a report-group or screen-section entry, which reuse the
        /// SHARED <c>occursClause</c> rule (CobolReportWriter.g4 / CobolScreen.g4) but are bound by neither BindEntry
        /// nor <c>OdoBindOccursSpec</c> (report groups → COBOLNET0899; screen sections are unbound), so a bare tree
        /// walk over-fired OCCURS DYNAMIC there. A POSITIVE "inside a gated data entry" test reproduces the binder's
        /// exact reach; the four data-only clauses (BASED/TYPE/PROPERTY/TYPEDEF, never shared with report/screen)
        /// always satisfy it, so their behavior is unchanged.</summary>
        private static bool InGatedDataEntry(Antlr4.Runtime.RuleContext ctx)
        {
            for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.DataDescriptionEntryContext e)
                    return e.levelNumber()?.GetText() is not ("66" or "88");
            return false;   // no dataDescriptionEntry ancestor — a report-group / screen-section clause the binder never gated
        }

        // ── Step 14g.3: the OO class/interface definition + OCCURS DYNAMIC gates ───────────────────────────────
        // Two OO-2002 introductions (a CLASS-ID / INTERFACE-ID compilation unit) + one 2014 introduction (OCCURS
        // DYNAMIC). All parse-arm: a class/interface gate would UNDER-count from a bound-arm home — OoClassTable
        // drops a duplicate/colliding definition with a `continue` BEFORE it enters the built table, yet the former
        // gate fired for EVERY parse node (before that continue) — and the OCCURS DYNAMIC gate would OVER-count a
        // per-DataItem bound-arm walk (a TYPEDEF template's dynamic table is cloned into each TYPE reference, but the
        // former gate fired once per source occursClause). Recognition fires once per parse node — byte-identical to
        // the former OoClassTable.Build / OdoBindOccursSpec Checks (same constructId + where-string, incl. the
        // name-embedding class/interface where-strings). Class/interface definitions are top-level compilation units
        // (not data-description entries), so they need no level-66/88 guard; OCCURS DYNAMIC does (the binder skipped
        // those levels before OdoBindOccursSpec ran).

        /// <summary>A class definition (CLASS-ID compilation unit, ISO §11.2/§11.3) — a COBOL-2002 introduction. The
        /// where-string embeds the class-name exactly as the former OoClassTable.Build site (<c>className(0)</c> — the
        /// class's own name, not an INHERITS base).</summary>
        public override object? VisitClassDefinition(CobolParserCore.ClassDefinitionContext ctx)
        {
            _p.Check(Constructs.ClassDefinition2002,
                $"class definition '{ctx.classIdParagraph().className(0).GetText()}' (CLASS-ID compilation unit)");
            return base.VisitChildren(ctx);
        }

        /// <summary>An interface definition (INTERFACE-ID compilation unit, ISO §11.5/§11.6) — a COBOL-2002
        /// introduction. The where-string embeds <c>interfaceName(0)</c> (the interface's own name), matching the
        /// former OoClassTable.Build site.</summary>
        public override object? VisitInterfaceDefinition(CobolParserCore.InterfaceDefinitionContext ctx)
        {
            _p.Check(Constructs.InterfaceDefinition2002,
                $"interface definition '{ctx.interfaceName(0).GetText()}' (INTERFACE-ID compilation unit)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The OCCURS DYNAMIC clause (a dynamic-capacity table, ISO §13.18.38 Format 4; data-model D9) — a
        /// COBOL-2014 introduction. Gated on the DYNAMIC alternative of the shared <c>occursClause</c> rule (matching
        /// <c>OdoBindOccursSpec</c>'s <c>occ.DYNAMIC()</c> test), once per source clause; a fixed / Format-2 OCCURS is
        /// untouched. The <see cref="InGatedDataEntry"/> guard restricts firing to a DATA-description entry — the sole
        /// reach of the former <c>OdoBindOccursSpec</c> (called only from <c>BindEntry</c>): the same <c>occursClause</c>
        /// rule ALSO appears in report-group and screen-section entries, which the binder never gated, so a bare tree
        /// walk over-fired there (the 14g.3-review correction, DEVLOG 736).</summary>
        public override object? VisitOccursClause(CobolParserCore.OccursClauseContext ctx)
        {
            if (ctx.DYNAMIC() is not null && InGatedDataEntry(ctx))
                _p.Check(Constructs.OccursDynamic2014, "the OCCURS DYNAMIC clause");
            // The report-writer format (ISO §13.18.38 format 3) is a COBOL-2002 introduction — the COBOL-85 report
            // group description entry had NO OCCURS clause, which is why its whole repeating-entry family
            // (multiple LINE/COLUMN, VARYING, PRESENT WHEN) gates at 2002 beside it. The shared `occursClause`
            // rule serves data, report-group and screen entries alike, so the arm keys on the ANCESTOR (the 14g.3
            // shared-rule lesson) rather than on any clause token.
            if (InReportGroupEntry(ctx))
                _p.Check(Constructs.ReportOccurs2002, "the OCCURS clause (report group description)");
            return base.VisitChildren(ctx);
        }

        /// <summary>True when <paramref name="ctx"/> sits inside a REPORT GROUP description entry (ISO §13.15) —
        /// the ancestor test the shared <c>occursClause</c> rule needs to tell a repeating report entry from a
        /// data-division or screen-section table.</summary>
        private static bool InReportGroupEntry(Antlr4.Runtime.RuleContext ctx)
        {
            for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.ReportGroupEntryContext) return true;
            return false;
        }

        // ── Step 14g.4: the file-control / SPECIAL-NAMES / PROCEDURE-DIVISION-header clause gates ───────────────
        // Seven COBOL-2002 introductions on config/file-control/PD-header clauses, ALL parse-arm (recognition). None
        // needs a resolved fact — each is a dedicated clause node with a constant where-string. SHARING/LOCK-MODE were
        // recon-classified bound-arm ("key on FileModel.Sharing/LockMode"), but that is the SAME inverted rationale the
        // 14g.2 review corrected for TYPEDEF: a bound-arm gate keyed on the resolved FileModel would DROP the 0900 if
        // the file is discarded on a SELECT error, whereas the binder fired it on the clause's PRESENCE in the
        // file-control loop — so recognition is byte-exact and drop-proof. The scope match holds because every program
        // unit (top-level AND nested — CallCollectUnits recurses nestedProgram), every class, and every factory gets
        // its OWN DataBinder whose BindDeclarations runs SwitchBindSpecialNames + BindFileControl + CallBindLinkage; the
        // parse-arm's single whole-tree walk equals the UNION of those per-scope binder walks. The one scope asymmetry
        // is PROCEDURE DIVISION RETURNING/RAISING: the procedureDivision rule is SHARED by program and method PDs, but
        // CallBindLinkage gates PROGRAM units only (methods bind via OoBindMethodData) — so those two gates carry the
        // InMethodDefinition guard (the 14g.3 shared-rule lesson).

        /// <summary>The SELECT … SHARING clause (ISO §12.4.5.15) — a COBOL-2002 introduction. Its OPEN-statement twin
        /// (same constructId, the "OPEN SHARING phrase" where-string) already gates from <see cref="VisitOpenStatement"/>;
        /// both fire parse-arm.</summary>
        public override object? VisitSharingClause(CobolParserCore.SharingClauseContext ctx)
        { _p.Check(Constructs.FileSharingClause2002, "the SHARING clause"); return base.VisitChildren(ctx); }

        /// <summary>The SELECT … LOCK MODE clause (ISO §12.4.5.9) — a COBOL-2002 introduction. The §12.4.5.9 SR2
        /// (WITH LOCK ON MULTIPLE RECORDS vs sequential) validation stays in the binder; this arm only names the edition.</summary>
        public override object? VisitLockModeClause(CobolParserCore.LockModeClauseContext ctx)
        { _p.Check(Constructs.LockModeClause2002, "the LOCK MODE clause"); return base.VisitChildren(ctx); }

        /// <summary>The file-control COLLATING SEQUENCE clause (ISO §12.4.5.7 — programmable INDEXED record-key
        /// collating) — a COBOL-2002 introduction. Parses at all editions (superset); this arm names the edition
        /// below 2002. Recognition-fire on the clause's presence, drop-proof on a SELECT error (DEVLOG 724).</summary>
        public override object? VisitFileCollatingSequenceClause(CobolParserCore.FileCollatingSequenceClauseContext ctx)
        { _p.Check(Constructs.FileCollatingClause2002, "the file COLLATING SEQUENCE clause"); return base.VisitChildren(ctx); }

        /// <summary>The LINE SEQUENTIAL phrase of the ORGANIZATION clause (ISO §12.4.5.10.3 GR2: "The LINE
        /// SEQUENTIAL phrase specifies that the file organization is line sequential") — a COBOL-2023 introduction.
        /// <para>THE EDITION IS DERIVED, NOT INHERITED (kb/Work PB688). The Foreword's list of the main changes
        /// this third edition makes over ISO/IEC 1989:2014 names "Line Sequential file organization" outright, so
        /// — unlike its 85↔2002 neighbours in this block — the introducing edition needs no older standard; Annex E
        /// happens to carry no item for it. §9.1.7.2 is why it is a PHRASE and not a fourth organization: §9.1.6
        /// still names exactly three organizations and §9.1.7.2 splits the SEQUENTIAL one into its record- and
        /// line- delimited types, which is the {LINE|RECORD} inner choice of the §12.4.5.10.2 general format.
        /// The clause parses at every edition (the grammar is the 2023 superset); this arm names the edition below
        /// 2023. Recognition-fire on the clause's presence, so a SELECT that ALSO fails to bind still names its
        /// edition instead of dropping the 0900 with the discarded FileModel (DEVLOG 724).</para>
        /// <para>The OTHER half of that 2023 inner choice gates HERE too, from the same token test (kb/Work PB706):
        /// the written-out `RECORD` of `RECORD SEQUENTIAL`. RECORD is an OPTIONAL word (rendered §12.4.5.10.2 —
        /// not underlined; §5.2.3), so the grammar writes the arm `RECORD? SEQUENTIAL` and only the RECORD token's
        /// presence is the 2023 spelling; bare `SEQUENTIAL` is the same alternative and stays legal at every edition
        /// (§12.4.5.10.3 GR6 — the absent clause implies it).</para></summary>
        public override object? VisitOrganizationClause(CobolParserCore.OrganizationClauseContext ctx)
        {
            var type = ctx.organizationType();
            if (type?.LINE() is not null)
                _p.Check(Constructs.FileOrganizationLineSequential2023,
                    "the LINE SEQUENTIAL phrase of the ORGANIZATION clause");
            else if (type?.RECORD() is not null)
                _p.Check(Constructs.FileOrganizationRecordSequential2023,
                    "the RECORD SEQUENTIAL phrase of the ORGANIZATION clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The SUPPRESS WHEN phrase of the ALTERNATE RECORD KEY clause (ISO §12.4.5.6.2) — a COBOL-2023
        /// addition (Introduction p.27 / Annex E.3.3 item 42). Parses at all editions (superset); this arm names
        /// the edition below 2023. Recognition-fire on the dedicated phrase rule (DEVLOG-736-safe).</summary>
        public override object? VisitAlternateKeySuppressWhen(CobolParserCore.AlternateKeySuppressWhenContext ctx)
        { _p.Check(Constructs.AlternateKeySuppressWhen2023, "the SUPPRESS WHEN phrase of the ALTERNATE RECORD KEY clause"); return base.VisitChildren(ctx); }

        /// <summary>ALPHABET … FOR ALPHANUMERIC/NATIONAL (ISO §12.3.7) — a COBOL-2002 introduction; the base ALPHABET
        /// clause is version-invariant. One of the three SPECIAL-NAMES FOR-phrase sites (all one constructId +
        /// where-string), gated once per clause on the FOR phrase's presence at its ISO position between the name
        /// and IS (a trailing FOR phrase is refused at every edition by ClosedFormatPass — kb/Work PB977).
        /// The UCS-4/UTF-8/UTF-16 coded-set phrases (§12.3.7.2, the FOR NATIONAL branch) are §8.9
        /// CONTEXT-SENSITIVE words arriving as plain cobolWord entries — recognized here BY TEXT (never lexer
        /// keywords) and gated as their own 2002 introduction (alphabet-national-2002).</summary>
        public override object? VisitAlphabetClause(CobolParserCore.AlphabetClauseContext ctx)
        {
            if (ctx.specialNamesForPhrase() is not null)
                _p.Check(Constructs.SpecialNamesForNational2002, "the FOR ALPHANUMERIC/NATIONAL phrase");
            if (ctx.alphabetDefinition() is { } def && def.alphabetEntry() is [{ ChildCount: 1 } entry]
                && entry.GetChild(0) is CobolParserCore.CobolWordContext w
                && _cobolWords.Resolve(w.GetText().ToUpperInvariant()) is "UCS-4" or "UTF-8" or "UTF-16")
                _p.Check(Constructs.AlphabetNational2002, $"the ALPHABET {w.GetText().ToUpperInvariant()} phrase");
            // `IS LOCALE [locale-name-2]` (§12.3.7.2, either branch) — the locale facility's collating sequence, a 2002
            // introduction (Annex A.4.9 item 10; kb/Work PB101). The grammar recognizes the phrase at every edition as
            // its own rule (alphabetLocalePhrase — LOCALE is its formatWord keyword, kb/Work PB764), which the binder reads too.
            if (ctx.alphabetDefinition()?.alphabetLocalePhrase() is not null)
                _p.Check(Constructs.AlphabetLocale2002, "the ALPHABET LOCALE phrase");
            return base.VisitChildren(ctx);
        }

        public override object? VisitDynamicLengthStructureClause(CobolParserCore.DynamicLengthStructureClauseContext ctx)
        {
            // The SPECIAL-NAMES DYNAMIC LENGTH STRUCTURE clause (ISO §12.3.7.2; kb/Work PB829) — a COBOL-2014
            // introduction with the DYNAMIC LENGTH clause it serves. Gated on RECOGNITION: DYNAMIC is reserved at
            // every edition, so the clause parses at all four and the sub-2014 answer is this explanatory
            // introduction diagnostic, not a parse error.
            _p.Check(Constructs.DynamicLengthStructure2014, "the DYNAMIC LENGTH STRUCTURE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The SPECIAL-NAMES ORDER TABLE clause (ISO §12.3.7.2 — the cultural ordering table
        /// STANDARD-COMPARE names, §12.3.7.4 GR17): a 2002 introduction, gated on recognition (kb/Work PB101).
        /// <para>⚠ The grammar recognizes the clause at EVERY edition (unlike the LOCALE clause's edition-gated
        /// predicate — TABLE is reserved at all four, so ORDER + TABLE has no competing '85 reading), which is
        /// precisely what makes this gate reachable below 2002: the answer there is this explanatory
        /// introduction diagnostic rather than a parse error at the word TABLE.</para></summary>
        public override object? VisitOrderTableClause(CobolParserCore.OrderTableClauseContext ctx)
        {
            _p.Check(Constructs.OrderTable2002, "the ORDER TABLE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The OBJECT-COMPUTER CHARACTER CLASSIFICATION clause (ISO §12.3.6.2 — Annex A.4.9 item 7; kb/Work PB64
        /// T5): a 2002 introduction, gated on recognition (the clause parses at every edition — CLASSIFICATION is a
        /// context-sensitive word matched by text).</summary>
        public override object? VisitCharacterClassificationClause(CobolParserCore.CharacterClassificationClauseContext ctx)
        {
            _p.Check(Constructs.CharacterClassification2002, "the OBJECT-COMPUTER CHARACTER CLASSIFICATION clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The SPECIAL-NAMES LOCALE clause (ISO §12.3.7.2 — the locale facility's declaration, Annex A.4.9 item
        /// 10; kb/Work PB64 T1): a 2002 introduction, gated on recognition. The grammar recognizes the clause by SHAPE at
        /// every edition (the '85 switch shapes are excluded by its predicate), so below 2002 the answer is this
        /// explanatory introduction diagnostic rather than a parse error at the clause's own literal.</summary>
        public override object? VisitLocaleClause(CobolParserCore.LocaleClauseContext ctx)
        {
            _p.Check(Constructs.SpecialNamesLocale2002, "the SPECIAL-NAMES LOCALE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>SET LOCALE … (ISO §14.9.39 Format 11) / SET … TO LOCALE … (Format 12) — the locale facility's statements
        /// (Annex A.4.9 item 9; kb/Work PB64 T1), 2002 introductions gated on recognition. The two formats share one parse
        /// rule; the words' POSITION tells them apart (format 12's LOCALE follows the identifier and TO).</summary>
        public override object? VisitSetLocaleStatement(CobolParserCore.SetLocaleStatementContext ctx)
        {
            bool save = ctx.setLocaleSource() is null;   // F12 has no Format-11 TO operand
            _p.Check(save ? Constructs.SetSaveLocale2002 : Constructs.SetLocale2002,
                save ? "SET … TO LOCALE (save-locale, §14.9.39 Format 12)" : "SET LOCALE (set-locale, §14.9.39 Format 11)");
            return base.VisitChildren(ctx);
        }

        /// <summary>PROGRAM COLLATING SEQUENCE's national surface (ISO §12.3.6.2) — alphabet-name-2 of the
        /// two-name IS form and the FOR ALPHANUMERIC/FOR NATIONAL forms are 2002 introductions (the 85 format is
        /// the single-name IS form); one gate per clause on recognition.</summary>
        public override object? VisitProgramCollatingSequenceClause(CobolParserCore.ProgramCollatingSequenceClauseContext ctx)
        {
            if (ctx.collatingForPhrase().Length > 0 || ctx.cobolWord().Length > 1)
                _p.Check(Constructs.ProgramCollatingNational2002,
                    "the PROGRAM COLLATING SEQUENCE alphabet-name-2 / FOR ALPHANUMERIC/NATIONAL forms");
            return base.VisitChildren(ctx);
        }

        /// <summary>A program or function source unit WITHOUT the IDENTIFICATION DIVISION header (ISO §11.2.1 prints
        /// `[ IDENTIFICATION DIVISION. ]` in brackets): a 2002 relaxation — X3.23-1985 required the header (kb/Work
        /// PB829). The class, factory, object, interface and method units reach the same optional header through
        /// their own rules (Core/CobolOO.g4), whose units are themselves 2002 introductions gated elsewhere.</summary>
        public override object? VisitIdentificationDivision(CobolParserCore.IdentificationDivisionContext ctx)
        {
            if (ctx.IDENTIFICATION() is null)
                _p.Check(Constructs.IdentificationHeaderOptional2002, "a source unit without the IDENTIFICATION DIVISION header");
            return base.VisitChildren(ctx);
        }

        /// <summary>OBJECT-COMPUTER clauses WITHOUT computer-name-1 (ISO §12.3.6.2 — the name is optional and the
        /// clauses may follow the period directly): a 2002 relaxation — X3.23-1985 hung MEMORY SIZE / PROGRAM COLLATING
        /// SEQUENCE / SEGMENT-LIMIT off a REQUIRED computer-name (kb/Work PB78). The empty paragraph is legal at every
        /// edition; only a clause without a name is gated. CHARACTER CLASSIFICATION itself BINDS since kb/Work PB64 T5
        /// (the A.4.9 locale module is implemented; the COBOLNET1518 non-support era is over).</summary>
        public override object? VisitObjectComputerParagraph(CobolParserCore.ObjectComputerParagraphContext ctx)
        {
            if (ctx.computerName() is null && ctx.objectComputerClause().Length > 0)
                _p.Check(Constructs.ComputerNameOptional2002, "an OBJECT-COMPUTER clause without computer-name-1");
            return base.VisitChildren(ctx);
        }

        /// <summary>CLASS … FOR ALPHANUMERIC/NATIONAL (ISO §12.3.7) — the second SPECIAL-NAMES FOR-phrase site.
        /// <para>⛔ THE GATE TESTS THE SUBRULE, NEVER THE WORD (kb/Work PB695). It used to read <c>ctx.FOR()</c>,
        /// and FOR is an OPTIONAL WORD in this format (folio 290 rules ALPHANUMERIC and NATIONAL and nothing
        /// else): the moment the grammar let the user omit it, a word-keyed gate would have stopped firing and
        /// `CLASS X IS "0" NATIONAL` would have compiled silently at COBOL-85. All three sites now share
        /// <c>specialNamesForPhrase</c> and all three ask the same question of it.</para></summary>
        public override object? VisitClassDefinitionClause(CobolParserCore.ClassDefinitionClauseContext ctx)
        {
            if (ctx.specialNamesForPhrase() is not null)
                _p.Check(Constructs.SpecialNamesForNational2002, "the FOR ALPHANUMERIC/NATIONAL phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>SYMBOLIC CHARACTERS … FOR ALPHANUMERIC/NATIONAL (ISO §12.3.7) — the third SPECIAL-NAMES FOR-phrase
        /// site, keyed on the shared subrule for the reason spelled out on <see cref="VisitClassDefinitionClause"/>.
        /// The base SYMBOLIC CHARACTERS clause stays accepted-inert.</summary>
        public override object? VisitSymbolicCharactersClause(CobolParserCore.SymbolicCharactersClauseContext ctx)
        {
            if (ctx.specialNamesForPhrase() is not null)
                _p.Check(Constructs.SpecialNamesForNational2002, "the FOR ALPHANUMERIC/NATIONAL phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>The FD CODE-SET clause's NATIONAL half (ISO §13.18.13.2; kb/Work PB110) — alphabet-name-2 and the
        /// FOR phrases entered at 2002 with the national repertoire; the '85 format was the single-name IS form.</summary>
        public override object? VisitCodeSetClause(CobolParserCore.CodeSetClauseContext ctx)
        {
            if (ctx.codeSetForPhrase().Length > 0 || ctx.cobolWord().Length > 1)
                _p.Check(Constructs.CodeSetNational2002, "the CODE-SET national phrases (alphabet-name-2 / FOR)");
            return base.VisitChildren(ctx);
        }

        /// <summary>PROCEDURE DIVISION … RETURNING (ISO §14.2) — a COBOL-2002 introduction. Gated only OUTSIDE a method
        /// (the shared procedureDivision rule serves both, but CallBindLinkage — the former gate site — runs for program
        /// units only; a method's RETURNING binds through OoBindMethodData, ungated). §14.2.3 GR6/SR1 stay bind-time.</summary>
        public override object? VisitReturningClause(CobolParserCore.ReturningClauseContext ctx)
        {
            if (!InMethodDefinition(ctx))
                _p.Check(Constructs.ProcedureReturning2002, "the PROCEDURE DIVISION RETURNING phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>PROCEDURE DIVISION … RAISING (ISO §14.2.2) — a COBOL-2002 introduction. Same program-only scope as
        /// RETURNING; the RAISING clause's EC semantics stay in StatementBinder.Exceptions.</summary>
        public override object? VisitRaisingClause(CobolParserCore.RaisingClauseContext ctx)
        {
            if (!InMethodDefinition(ctx))
                _p.Check(Constructs.ProcedureRaising2002, "the PROCEDURE DIVISION RAISING phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>PROCEDURE DIVISION USING … BY VALUE (ISO §14.2.2 using-phrase :23641) — a COBOL-2002
        /// introduction, the header-side twin of <see cref="Constructs.CallByValue2002"/> (one-construct-one-gate:
        /// §14.2 header vs §14.9.4 statement). Fires on RECOGNITION, once per BY VALUE parameter phrase, in
        /// programs, functions, AND methods alike (a method-header occurrence below 2002 is already inside the
        /// gated class construct — the extra where-string only sharpens the report). The §14.2.2 SR2 class
        /// restriction and the §14.2.3 GR10 value-copy semantics stay bind-time (DataBinder.CallBindLinkage).</summary>
        public override object? VisitUsingByValue(CobolParserCore.UsingByValueContext ctx)
        {
            _p.Check(Constructs.PdHeaderByValue2002, "the PROCEDURE DIVISION USING BY VALUE phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>The LOCAL-STORAGE SECTION (ISO §13.6 — automatic data) — a COBOL-2002 introduction. Fires on
        /// RECOGNITION wherever the shared <c>dataDivision</c> rule carries it (programs, functions, AND methods
        /// alike — a method occurrence below 2002 is already inside the gated class construct; the extra 0900 only
        /// sharpens the report, the VisitUsingByValue posture). The §13.6.4 GR1 activation-state semantics
        /// (automatic data — initial state on every activation, §14.6.2.3.2) stay bind/emit-time
        /// (<c>DataBinder.LocalStorageRoots</c> + the P10 RECURSIVE-WS activation-state model).</summary>
        public override object? VisitLocalStorageSection(CobolParserCore.LocalStorageSectionContext ctx)
        {
            _p.Check(Constructs.LocalStorageSection2002, "the LOCAL-STORAGE SECTION");
            return base.VisitChildren(ctx);
        }

        // ── Step 14g.5: the REPOSITORY specifier gates ────────────────────────────────────────────────────────
        /// <summary>A REPOSITORY CLASS / INTERFACE / PROPERTY / PROGRAM specifier (ISO §12.3.8) — each a COBOL-2002
        /// introduction. Parse-arm (recognition): the entry is one <c>repositoryEntry</c> node, mirroring the binder's
        /// PROPERTY→PROGRAM→INTERFACE→CLASS <c>else if</c> order and its name-embedding where-strings. The
        /// FUNCTION-intrinsic alternatives are version-invariant (ungated). ⚠ Like the SPECIAL-NAMES / file-control
        /// gates, REPOSITORY is in the CONFIGURATION SECTION, so for a CLASS the former per-scope binder gated it
        /// 0/1/2× (the flagged OO-env double/zero-bind, DEVLOG 738); the parse-arm fires the spec-correct ONCE. The
        /// property and program-prototype NAMES still register for reference resolution in <c>DataBinder</c>.</summary>
        public override object? VisitRepositoryEntry(CobolParserCore.RepositoryEntryContext ctx)
        {
            if (ctx.PROPERTY() is not null && ctx.propertyName() is { } pn)
                _p.Check(Constructs.RepositoryProperty2002, $"REPOSITORY PROPERTY '{pn.GetText()}'");
            // §12.3.8.2's program-specifier (kb/Work PB237) — the ONE surface that declares a program-prototype-name.
            // The whole program-prototype facility is 2002 (the '85 CALL general format has Format 1 only), so the
            // declaration gate and the Format-2 CALL / prototype CANCEL gates all key on the same edition.
            else if (ctx.PROGRAM() is not null && ctx.programPrototypeName() is { } ppn)
                _p.Check(Constructs.RepositoryProgram2002, $"REPOSITORY PROGRAM '{ppn.GetText()}'");
            else if (ctx.INTERFACE() is not null && ctx.interfaceName() is { } ifn)
                _p.Check(Constructs.RepositoryInterface2002, $"REPOSITORY INTERFACE '{ifn.GetText()}'");
            else if (ctx.CLASS() is not null && ctx.className() is { } cn)
                _p.Check(Constructs.RepositoryClass2002, $"REPOSITORY CLASS '{cn.GetText()}'");
            return base.VisitChildren(ctx);
        }

        // ── Step 14h.2: the SELF-IDENTIFYING statement gates ─────────────────────────────────────────────────
        // Each construct is ONE dedicated grammar rule → the parse node IS the identity; the gate fires once per
        // statement on RECOGNITION (never per operand — a multi-operand FREE / a compound ALTER is ONE node), so a
        // below-edition occurrence names its edition even when the statement ALSO fails to bind (DEVLOG 724 — the
        // bound-arm dropped ALLOCATE's + DELETE FILE's 0900 on the BoundNop/BoundUnsupported error paths). Each
        // keeps its former bound-arm/bind-time where-string verbatim.

        /// <summary>UNLOCK (ISO §14.9.47) — a COBOL-2002 introduction.</summary>
        public override object? VisitUnlockStatement(CobolParserCore.UnlockStatementContext ctx)
        { _p.Check(Constructs.UnlockStatement2002, "the UNLOCK statement"); return base.VisitChildren(ctx); }

        /// <summary>FREE (ISO §14.9.15) — a COBOL-2002 introduction (one Check for the whole pointer list).</summary>
        public override object? VisitFreeStatement(CobolParserCore.FreeStatementContext ctx)
        { _p.Check(Constructs.Free2002, "the FREE statement"); return base.VisitChildren(ctx); }

        /// <summary>ALTER (ISO §14.9.2) — obsolete '85, REMOVED by 2002 (one Check for a compound ALTER).</summary>
        public override object? VisitAlterStatement(CobolParserCore.AlterStatementContext ctx)
        { _p.Check(Constructs.AlterRemoved2002, "the ALTER statement"); return base.VisitChildren(ctx); }

        /// <summary>DELETE FILE (ISO 2023 §14.9.10 Format 2) — a COBOL-2023 introduction. Recognition-based so a
        /// below-2023 DELETE FILE names its edition even when the file is undeclared (the binder returns a
        /// BoundUnsupported before a BoundKeyedDeleteFile — the bound-arm dropped it, DEVLOG 724).</summary>
        public override object? VisitDeleteFileStatement(CobolParserCore.DeleteFileStatementContext ctx)
        { _p.Check(Constructs.DeleteFile2023, "the DELETE FILE statement"); return base.VisitChildren(ctx); }

        /// <summary>SET ADDRESS OF (ISO §14.9.39 Format 7) — a COBOL-2002 introduction. The ONE
        /// <c>setAddressStatement</c> rule carries the WHOLE printed format — a receiving LIST whose operands
        /// are independently <c>ADDRESS OF data-name-1</c> or <c>identifier-5</c>, over one identifier-6 sender
        /// that may itself be a §8.4.3.11 data-address-identifier — so one parse override is one Check per
        /// statement however the operands are spelled (kb/Work PB450).</summary>
        public override object? VisitSetAddressStatement(CobolParserCore.SetAddressStatementContext ctx)
        { _p.Check(Constructs.SetAddress2002, "SET ADDRESS OF (ISO §14.9.39 Format 7)"); return base.VisitChildren(ctx); }

        /// <summary>An ADDRESS-IDENTIFIER operand (ISO §8.4.3.1.2 identifier Format 9 — the §8.4.3.11 data arm or
        /// the §8.4.3.13 program arm) — a COBOL-2002 introduction with the pointer classes (kb/Work PB239). The
        /// <c>addressIdentifier</c> rule is the operand's GENERAL spelling; the SET Formats 7 and 9 name their
        /// senders' arms directly and carry their own statement gates, so this override fires exactly for the
        /// surfaces that take the identifier as an ordinary operand — today the CALL argument (§14.9.4.3 SR3/SR4)
        /// — and for every future surface that spells it through the same rule.</summary>
        public override object? VisitAddressIdentifier(CobolParserCore.AddressIdentifierContext ctx)
        { _p.Check(Constructs.AddressIdentifier2002, "an address-identifier operand (ISO §8.4.3.1.2 Format 9)"); return base.VisitChildren(ctx); }

        /// <summary>ALLOCATE (ISO §14.9.3) — a COBOL-2002 introduction. Recognition-based so a below-2002 ALLOCATE
        /// names its edition even when its RETURNING fails to resolve (SR3/0869; the bind-time gate that formerly
        /// carried this — StatementBinder.Ptr.cs — is removed this commit, DEVLOG 724).</summary>
        public override object? VisitAllocateStatement(CobolParserCore.AllocateStatementContext ctx)
        { _p.Check(Constructs.Allocate2002, "the ALLOCATE statement"); return base.VisitChildren(ctx); }

        // ── Step 14h.3: the PHRASE statement gates ───────────────────────────────────────────────────────────
        // A phrase's presence within a statement rule is purely syntactic — detectable from the parse tree without
        // any resolved fact. Each fires ONCE per statement (an Any-over-the-child-list where a statement can carry
        // the phrase on several clauses, matching the binder which collapsed them to one bound attribute). The
        // STOP RUN … STATUS gate rides the existing VisitStopStatement above.

        /// <summary>OPEN … SHARING (ISO §14.9.27) — a COBOL-2002 introduction. Fires ONCE per OPEN even when several
        /// openClauses carry SHARING: the gate answers "does this statement use a post-85 construct", one edition
        /// diagnostic per statement, so the Any-over-the-groups is deliberate. It is NOT a claim that the phrase is
        /// statement-scoped — since kb/Work PB316 each group's phrase binds to its own <c>BoundOpenFile</c>.</summary>
        public override object? VisitOpenStatement(CobolParserCore.OpenStatementContext ctx)
        {
            if (ctx.openClause().Any(c => c.sharingPhrase() is not null))
                _p.Check(Constructs.FileSharingClause2002, "the OPEN SHARING phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>GOBACK … RETURNING/GIVING (ISO §14.9.18) — a COBOL-2002 introduction. The RETURNING operand is
        /// the ONLY direct <c>dataReference</c> child of <c>gobackStatement</c> (the RAISING tail nests its own), so
        /// its presence ≡ the phrase. EXCLUDE a method context: inside a METHOD, GOBACK is a method-return
        /// (<c>BoundMethodReturn</c>, ungated) — CallBindGoback short-circuits to OoBindMethodGoback when InMethod,
        /// so the former bound-arm gate (keyed on BoundGoback.ReturningSource) never saw it. A UDF GOBACK is NOT
        /// excluded (it binds through CallBindGoback like a program). Recognition-based so an undeclared RETURNING
        /// operand (a BoundUnsupported before BoundGoback) still names its edition (DEVLOG 724).</summary>
        public override object? VisitGobackStatement(CobolParserCore.GobackStatementContext ctx)
        {
            if (ctx.dataReference() is not null && !InMethodDefinition(ctx))
                _p.Check(Constructs.GobackReturning2002, "GOBACK … RETURNING");
            // GOBACK itself is a 2002 introduction (§14.9.18; §14.9.16 in 2002 — Exec Step E, folded from the
            // CallBinder gate). Fires only for a BARE GOBACK: when RETURNING is present its more-specific 0900
            // (above) subsumes the 0880, and a METHOD GOBACK is a method return, never an activation return
            // (§14.9.18.4 GR4 — the same InMethodDefinition exclusion the binder's InMethod short-circuit gave).
            else if (ctx.dataReference() is null && !InMethodDefinition(ctx))
                _p.Check(Constructs.GobackBare2002, "the GOBACK statement");
            // GOBACK … WITH NORMAL/ERROR STATUS (§14.9.18.2) — a COBOL-2023 introduction (annex item 32: GOBACK
            // "now allows the same status phrase as the STOP statement"). DISTINCT edition from the STOP-status
            // gate (StopRunStatus2002 = 2002); the shared statusPhrase rule is 2002-gated on STOP, 2023 on GOBACK.
            // ⛔ NO InMethodDefinition EXCLUSION, unlike the two gates above, and the asymmetry is the rule's
            // (kb/Work PB411). Those two exclude a method because §14.9.18.4 GR4 makes a method's GOBACK a
            // different STATEMENT MEANING — a method return, not an activation return — so the phrase they gate
            // is not the phrase that was written. This gate asks a different question: was this SYNTAX available
            // in the targeted edition? §14.9.18.2 is the ONE general format a method's GOBACK is written in, the
            // status phrase was added to it at 2023, and §14.9.18.3 SR6/SR7/SR8 carry no context qualifier at
            // all. Measured before the fix: `GOBACK WITH ERROR STATUS 5.` inside a method compiled clean at
            // --std 2002 and --std 2014 while the byte-identical statement in a program drew COBOLNET0900.
            // ⛔ `.Length > 0`, NOT `is not null`: §14.9.18.2's tail is a CHOICE-INDICATOR group, so
            // gobackStatement carries `(raisingPhrase | statusPhrase)*` and ANTLR's accessor returns an ARRAY —
            // which is never null, so the null test fired this gate on EVERY GOBACK (kb/Work PB407; measured:
            // a bare `GOBACK.` at --std 2002 drew COBOLNET0900 naming a phrase it did not write). STOP's own
            // check above stays a null test because `stopStatement` still writes `(statusPhrase)?`.
            if (ctx.statusPhrase().Length > 0)
                _p.Check(Constructs.GobackStatus2023, "the GOBACK … WITH NORMAL/ERROR STATUS phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>CALL phrases: BY VALUE (§14.9.4, a 2002 introduction) then ON OVERFLOW (the COBOL-74 synonym for
        /// ON EXCEPTION, REMOVED at 2023) — both from the one CALL statement, in the binder's order (arguments bind
        /// before the exception phrases). BY VALUE fires ONCE per statement (Any over callArgument — the binder
        /// collapsed the per-argument checks); OVERFLOW fires on either exception phrase using the spelling
        /// (matching BoundCallProgram.UsedOverflowSpelling).</summary>
        public override object? VisitCallStatement(CobolParserCore.CallStatementContext ctx)
        {
            if (ctx.callUsingPhrase()?.callArgument().Any(a => a.callByValue() is not null) == true)
                _p.Check(Constructs.CallByValue2002, "the CALL … BY VALUE phrase");
            // Both phrases hang off the ONE callExceptionPhrases container (either order — ISO 5.2.6.4).
            var callExc = ctx.callExceptionPhrases();
            if (callExc?.callOnExceptionPhrase()?.OVERFLOW() is not null
                || callExc?.callNotOnExceptionPhrase()?.OVERFLOW() is not null)
                _p.Check(Constructs.CallOnOverflowRemoved2023, "the CALL statement");
            // CALL … RETURNING (§14.9.4) — a 2002 introduction (Exec Step E — folded from the CallBinder gate).
            if (ctx.callReturningPhrase() is not null)
                _p.Check(Constructs.CallReturning2002, "CALL … RETURNING");
            // CALL … AS {NESTED | program-prototype-name-1} (§14.9.4.2 Format 2) — a 2002 introduction with the
            // program prototypes (§8.9 reserves NESTED from 2002). The FMT-14.9.4.2 row carried this as residue
            // and kb/Work PB239 re-measured it: an 85 compile accepted `CALL "X" AS NESTED` silently.
            if (ctx.callAsPhrase() is not null)
                _p.Check(Constructs.CallAsPhrase2002, "the CALL … AS phrase (ISO §14.9.4.2 Format 2)");
            return base.VisitChildren(ctx);
        }

        /// <summary>END-ACCEPT (ISO §14.9.1, a 2002 introduction). Mirrors the binder's <c>AcceptHasTerminator</c>
        /// token scan EXACTLY. NOTE — a grammar gap: <c>acceptStatement</c> has no <c>END_ACCEPT</c> alternative, so
        /// the scan finds nothing and this gate fires ZERO times today (as the bound-arm's
        /// BoundAccept.HasEndTerminator did); kept in the correct home so it lights up if the grammar gains the
        /// terminator.</summary>
        /// <summary>END-DISPLAY (§14.9.11.2, a COBOL-2002 addition like every explicit scope terminator of the
        /// 2002 wave) — the registered end-accept twin had a gate and this had NONE, so `DISPLAY X END-DISPLAY`
        /// was silently accepted at 85 (kb/Work PB134).</summary>
        public override object? VisitDisplayStatement(CobolParserCore.DisplayStatementContext ctx)
        {
            for (int i = 0; i < ctx.ChildCount; i++)
                if (ctx.GetChild(i) is Antlr4.Runtime.Tree.ITerminalNode { Symbol.Type: CobolLexer.END_DISPLAY })
                {
                    _p.Check(Constructs.EndDisplay2002, "the DISPLAY statement");
                    break;
                }
            return base.VisitChildren(ctx);
        }

        public override object? VisitAcceptStatement(CobolParserCore.AcceptStatementContext ctx)
        {
            for (int i = 0; i < ctx.ChildCount; i++)
                if (ctx.GetChild(i) is Antlr4.Runtime.Tree.ITerminalNode { Symbol.Type: CobolLexer.END_ACCEPT })
                {
                    _p.Check(Constructs.EndAccept2002, "the ACCEPT statement");
                    break;
                }
            // The four-digit-year temporal phrases (§14.9.1 — the 1985 formats list only bare DATE / DAY /
            // DAY-OF-WEEK / TIME): a 2002 introduction (Exec Step E — folded from the AcceptDisplayBinder gate).
            if (ctx.acceptSource() is { } src && (src.YYYYMMDD() ?? src.YYYYDDD()) is not null)
                _p.Check(Constructs.AcceptFourDigitYear2002,
                    src.DATE() is not null ? "ACCEPT FROM DATE YYYYMMDD" : "ACCEPT FROM DAY YYYYDDD");
            return base.VisitChildren(ctx);
        }

        // ── Exec Step E: the folded binder-inline gates (plan §4.1 task #13) ────────────────────────────────
        // Each was an inline `DialectLevel` gate that moved VERBATIM with its verb at P7 Step 10; folded here so
        // the two-arm pass is the ONE gating funnel and every gate enters the version matrix (its registry row
        // carries the negative witness). Recognition-based (DEVLOG 724): a below-edition construct names its
        // edition even when it ALSO fails to bind. The catalog-driven per-NAME windows (the D8 intrinsic windows,
        // the ExceptionCatalog EC-name windows, the PictureAnalyzer symbol rows, the digit caps) STAY binder-side
        // by design — their version facts live in their own tables, not in constructs.json — as do the two
        // sanctioned BEHAVIORAL edition reads (the <2002 keyword-omitted FUNCTION routing, the ≥2002 MOVE CORR
        // pair-selection window) and the SYNCHRONIZED-on-group site (sync-on-group-2023 — bind-side because
        // "has subordinates" is not a parse-tree fact, but through the canonical Check funnel since CA14, so it
        // is no longer a severity exception: NO site makes an introduction lenient).

        /// <summary>INSPECT BACKWARD (ISO §14.9.22.2; VCR row 77) — a COBOL-2023 introduction.</summary>
        public override object? VisitInspectStatement(CobolParserCore.InspectStatementContext ctx)
        {
            if (ctx.BACKWARD() is not null)
                _p.Check(Constructs.InspectBackward2023, "INSPECT BACKWARD");
            return base.VisitChildren(ctx);
        }

        /// <summary>The INITIALIZE post-85 surface (ISO §14.9.20) — WITH FILLER / TO VALUE / TO DEFAULT / the
        /// THEN connective, each a 2002 introduction with its own pinned code (REPLACING itself is COBOL-85).</summary>
        public override object? VisitInitializeStatement(CobolParserCore.InitializeStatementContext ctx)
        {
            if (ctx.FILLER() is not null)
                _p.Check(Constructs.InitializeFiller2002, "INITIALIZE … WITH FILLER");
            if (ctx.initializeCategoryToValue() is not null)
                _p.Check(Constructs.InitializeToValue2002, "INITIALIZE … TO VALUE");
            if (ctx.initializeDefaultPhrase() is not null)
                _p.Check(Constructs.InitializeToDefault2002, "INITIALIZE … TO DEFAULT");
            if (ctx.initializeReplacingPhrase()?.THEN() is not null)
                _p.Check(Constructs.InitializeThenReplacing2002, "INITIALIZE … THEN REPLACING");
            return base.VisitChildren(ctx);
        }

        /// <summary>⛔ THE CATEGORY-NAME WORDS THAT POST-DATE COBOL-85 (ISO §14.9.20.2 "where category-name is:";
        /// kb/Work PB415). Eight of the printed thirteen entered with the editions that made their DATA CATEGORY
        /// possible, and §8.9 reserves each word from exactly that edition — BOOLEAN, DATA-POINTER, NATIONAL,
        /// NATIONAL-EDITED, OBJECT-REFERENCE and PROGRAM-POINTER from 2002, FUNCTION-POINTER from 2014,
        /// MESSAGE-TAG from 2023 (tests/version-matrix/reserved-words.json, and
        /// <c>InitializeLaneDriftTests</c> pins this mapping against it so a new word cannot be mis-banded).
        /// The five COBOL-85 words are ungated.
        /// <para>The gate is HERE and not in the grammar on purpose: a category-name slot can hold nothing but a
        /// category-name, so no user-defined word competes for it and there is nothing for a
        /// <c>userWordHere</c> predicate to disambiguate — a named COBOLNET0900 that says which edition
        /// introduced the word beats the "no viable alternative" a grammar predicate would produce.</para></summary>
        public override object? VisitInitializeCategoryName(CobolParserCore.InitializeCategoryNameContext ctx)
        {
            string? id =
                ctx.BOOLEAN() is not null || ctx.DATA_POINTER() is not null || ctx.NATIONAL() is not null
                    || ctx.NATIONAL_EDITED() is not null || ctx.OBJECT_REFERENCE() is not null
                    || ctx.PROGRAM_POINTER() is not null ? Constructs.InitializeCategory2002
                : ctx.FUNCTION_POINTER() is not null ? Constructs.InitializeCategory2014
                : ctx.MESSAGE_TAG() is not null ? Constructs.InitializeCategory2023
                : null;                                   // the COBOL-85 five: ALPHABETIC/ALPHANUMERIC[-EDITED]/NUMERIC[-EDITED]
            if (id is not null) _p.Check(id, $"INITIALIZE category-name {ctx.GetText()}");
            return base.VisitChildren(ctx);
        }

        /// <summary>RELEASE … FROM literal-1 (ISO §14.9.32.2) — X3.23-1985 allows only an identifier as the
        /// FROM operand; the literal form is 2002+.</summary>
        public override object? VisitReleaseFrom(CobolParserCore.ReleaseFromContext ctx)
        {
            if (ctx.literal() is not null)
                _p.Check(Constructs.ReleaseFromLiteral2002, "RELEASE … FROM literal-1");
            return base.VisitChildren(ctx);
        }

        /// <summary>The Format-2 in-place table SORT (ISO §14.9.40) — a 2002 introduction. The F2 shape is
        /// syntactic: a SORT with neither USING/GIVING nor INPUT/OUTPUT procedures (F1 requires an input AND an
        /// output leg, §14.9.40.2), so recognition never needs the operand's resolved name class.</summary>
        public override object? VisitSortStatement(CobolParserCore.SortStatementContext ctx)
        {
            if (ctx.sortUsingPhrase() is null && ctx.sortInputProcedurePhrase() is null
                && ctx.sortGivingPhrase() is null && ctx.sortOutputProcedurePhrase() is null)
                _p.Check(Constructs.TableSort2002, "SORT of a table (Format 2, ISO §14.9.40)");
            return base.VisitChildren(ctx);
        }

        /// <summary>SORT/MERGE COLLATING SEQUENCE's national surface (ISO §14.9.40.2 / §14.9.24.2 —
        /// §14.9.40.3 SR2 national collating): alphabet-name-2 of the IS form and the FOR ALPHANUMERIC/FOR
        /// NATIONAL forms are 2002 introductions (the national class); one gate per phrase on recognition.</summary>
        public override object? VisitSortCollatingPhrase(CobolParserCore.SortCollatingPhraseContext ctx)
        {
            if (ctx.cobolWord().Length > 1 || ctx.collatingForPhrase().Length > 0)
                _p.Check(Constructs.SortCollatingNational2002, "COLLATING SEQUENCE alphabet-name-2 / FOR forms");
            return base.VisitChildren(ctx);
        }

        /// <summary>RAISE (ISO §14.9.29) — the 2002+ exception-condition statement.</summary>
        public override object? VisitRaiseStatement(CobolParserCore.RaiseStatementContext ctx)
        { _p.Check(Constructs.RaiseStatement2002, "the RAISE statement"); return base.VisitChildren(ctx); }

        /// <summary>RESUME (ISO §14.9.33) — the 2002+ exception-recovery statement.</summary>
        public override object? VisitResumeStatement(CobolParserCore.ResumeStatementContext ctx)
        { _p.Check(Constructs.ResumeStatement2002, "the RESUME statement"); return base.VisitChildren(ctx); }

        /// <summary>SET LAST EXCEPTION TO OFF (ISO §14.9.39 Format 13) — the 2002+ saved-exception form.</summary>
        public override object? VisitSetLastExceptionStatement(CobolParserCore.SetLastExceptionStatementContext ctx)
        { _p.Check(Constructs.SetLastException2002, "SET LAST EXCEPTION TO OFF"); return base.VisitChildren(ctx); }

        /// <summary>SET CONTENT OF … TO … (ISO §14.9.39.2 Format 15, numeric-content) — a COBOL-2014 introduction.
        /// A RECOGNITION gate, not a semantic one: the statement is self-identifying from its own grammar rule, so a
        /// below-edition occurrence names its edition even when the statement would also fail SR31/SR32.</summary>
        public override object? VisitSetContentStatement(CobolParserCore.SetContentStatementContext ctx)
        { _p.Check(Constructs.SetNumericContent2014, "the SET CONTENT OF … TO … statement (numeric-content, Format 15)"); return base.VisitChildren(ctx); }

        /// <summary>The statement-level RAISING phrase (ISO §14.9.18.2 / §14.9.14.2 F2 — the ONE rule GOBACK and
        /// the EXIT forms share) — 2002+ exception propagation. Distinct from the PROCEDURE DIVISION header
        /// RAISING clause (<c>raisingClause</c> → procedure-raising-2002).</summary>
        public override object? VisitRaisingPhrase(CobolParserCore.RaisingPhraseContext ctx)
        { _p.Check(Constructs.StatementRaising2002, "the GOBACK / EXIT … RAISING phrase"); return base.VisitChildren(ctx); }

        /// <summary>The AS externalized-name phrase (ISO §8.3.2.2 2)) wherever an identification-division
        /// paragraph prints it — PROGRAM-ID §11.10.2, FUNCTION-ID §11.5.2, CLASS-ID §11.3.2, INTERFACE-ID
        /// §11.6.2, METHOD-ID §11.7.2 — and on §12.3.8.2's program-specifier, which shares the grammar rule.
        /// A 2002 introduction: X3.23-1985's PROGRAM-ID paragraph has no AS phrase and AS is user-definable
        /// there (the user-word-as-2002 twin). ONE arm for one rule, so a sixth AS site is gated by writing
        /// `externalizedNamePhrase?` and nothing else (kb/Work PB303). The specifier's OWN
        /// repository-program-2002 gate stands beside this one exactly as options-paragraph-2002 stands beside
        /// arithmetic-standard-2002 — two true statements about one line of source.</summary>
        /// <summary>The IS PROTOTYPE tail — FUNCTION-ID's prototype format and §11.10.2 Format 2 (PROGRAM-ID), both
        /// COBOL-2002 introductions. ONE parse node, <c>prototypePhrase</c>, identifies both, so the gate is
        /// recognition-fired here (kb/Work PB894). It used to be a BOUND-arm check over <c>BoundUnit.IsPrototype</c>,
        /// adjudicated there only because the FUNCTION-ID tail had no rule of its own; once the shared rule existed
        /// a parse rule identified the construct, and PB353's placement rule puts the gate on the parse arm.</summary>
        public override object? VisitPrototypePhrase(CobolParserCore.PrototypePhraseContext ctx)
        {
            if (ctx.Parent is CobolParserCore.FunctionIdParagraphContext)
                _p.Check(Constructs.FunctionPrototype2002, "a FUNCTION-ID … IS PROTOTYPE (function prototype)");
            else
                _p.Check(Constructs.ProgramPrototype2002, "a PROGRAM-ID … IS PROTOTYPE (program prototype)");
            return base.VisitChildren(ctx);
        }

        public override object? VisitExternalizedNamePhrase(CobolParserCore.ExternalizedNamePhraseContext ctx)
        {
            _p.Check(Constructs.ExternalizedNameAs2002, "the AS externalized-name phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>PROGRAM-ID … RECURSIVE (ISO §11.10) — a 2002 introduction. Recognized by TOKEN TYPE within
        /// the attribute list (the END-ACCEPT scan idiom), never by word text — the AS phrase is a sibling
        /// of the attribute list, not a member of it, so it cannot collide.</summary>
        public override object? VisitProgramIdAttributes(CobolParserCore.ProgramIdAttributesContext ctx)
        {
            if (FindsToken(ctx, CobolLexer.RECURSIVE))
                _p.Check(Constructs.ProgramIdRecursive2002, "PROGRAM-ID … RECURSIVE");
            return base.VisitChildren(ctx);
        }

        private static bool FindsToken(Antlr4.Runtime.Tree.IParseTree node, int tokenType)
        {
            if (node is Antlr4.Runtime.Tree.ITerminalNode t) return t.Symbol.Type == tokenType;
            for (int i = 0; i < node.ChildCount; i++)
                if (FindsToken(node.GetChild(i), tokenType)) return true;
            return false;
        }

        /// <summary>The OPTIONS paragraph (ISO §11.9) — a 2002 introduction (the 2002 standard added it with the
        /// ARITHMETIC clause; Annex E.2 item 21 back-derives the container from obsolete-in-2014 Standard
        /// Arithmetic). The binder still returns <c>OptionsModel.Default</c> below 2002 (silent routing — this
        /// arm owns the diagnostic). The 2014-only clauses carry their own arms below (P10 Step 12).</summary>
        public override object? VisitOptionsParagraph(CobolParserCore.OptionsParagraphContext ctx)
        { _p.Check(Constructs.OptionsParagraph2002, "the OPTIONS paragraph"); return base.VisitChildren(ctx); }

        /// <summary>The ARITHMETIC clause's mode keywords (ISO §11.9.5; P10 Step 12). STANDARD is the dual-window
        /// 2002 mode: introduced 2002, obsolete 2014, DROPPED by 2023 (Annex E.2 item 21 — use STANDARD-DECIMAL);
        /// the Removed verdict is error-strict / warning-permissive (a permissive 2023 compile continues under
        /// standard arithmetic). STANDARD-BINARY / STANDARD-DECIMAL are 2014 keywords of the 2002 clause — their
        /// introduction edges live HERE now that the paragraph parses at 2002 (NATIVE needs no arm: the paragraph
        /// row IS its introduction).</summary>
        public override object? VisitArithmeticMethod(CobolParserCore.ArithmeticMethodContext ctx)
        {
            if (ctx.STANDARD() is not null)
                _p.Check(Constructs.ArithmeticStandard2002, "ARITHMETIC IS STANDARD");
            if (ctx.STANDARD_DECIMAL() is not null)
                _p.Check(Constructs.ArithmeticStandardDecimal2014, "ARITHMETIC IS STANDARD-DECIMAL");
            if (ctx.STANDARD_BINARY() is not null)
                _p.Check(Constructs.ArithmeticStandardBinary2014, "ARITHMETIC IS STANDARD-BINARY");
            return base.VisitChildren(ctx);
        }

        // ── The 2014-only OPTIONS clauses (ISO §11.9.6–§11.9.11; P10 Step 12): with the paragraph gate at
        // 2002, each 2014 clause fires its own introduction row on RECOGNITION. ENTRY-CONVENTION is gated 2014
        // conservatively (the in-repo 85/2002 evidence chain establishes only the ARITHMETIC clause at 2002 —
        // the constructs.json row records the ambiguity). ──────────────────────────────────────────────────

        /// <summary>DEFAULT ROUNDED (ISO §11.9.6) — a 2014 clause of the 2002 OPTIONS paragraph.</summary>
        public override object? VisitDefaultRoundedClause(CobolParserCore.DefaultRoundedClauseContext ctx)
        { _p.Check(Constructs.OptionsDefaultRounded2014, "the DEFAULT ROUNDED clause"); return base.VisitChildren(ctx); }

        /// <summary>INTERMEDIATE ROUNDING (ISO §11.9.11) — a 2014 clause of the 2002 OPTIONS paragraph.</summary>
        public override object? VisitIntermediateRoundingClause(CobolParserCore.IntermediateRoundingClauseContext ctx)
        { _p.Check(Constructs.OptionsIntermediateRounding2014, "the INTERMEDIATE ROUNDING clause"); return base.VisitChildren(ctx); }

        /// <summary>ENTRY-CONVENTION (ISO §11.9.7) — gated 2014 conservatively (see the block note).</summary>
        public override object? VisitEntryConventionClause(CobolParserCore.EntryConventionClauseContext ctx)
        { _p.Check(Constructs.OptionsEntryConvention2014, "the ENTRY-CONVENTION clause"); return base.VisitChildren(ctx); }

        /// <summary>FLOAT-BINARY (ISO §11.9.8) — a 2014 clause of the 2002 OPTIONS paragraph.</summary>
        public override object? VisitFloatBinaryClause(CobolParserCore.FloatBinaryClauseContext ctx)
        { _p.Check(Constructs.OptionsFloatBinary2014, "the FLOAT-BINARY clause"); return base.VisitChildren(ctx); }

        /// <summary>FLOAT-DECIMAL (ISO §11.9.9) — a 2014 clause of the 2002 OPTIONS paragraph.</summary>
        public override object? VisitFloatDecimalClause(CobolParserCore.FloatDecimalClauseContext ctx)
        { _p.Check(Constructs.OptionsFloatDecimal2014, "the FLOAT-DECIMAL clause"); return base.VisitChildren(ctx); }

        /// <summary>OPTIONS INITIALIZE (ISO §11.9.10) — a 2023 clause of the 2002 OPTIONS paragraph (Annex E §E.3.3
        /// item 33: a NEW 2023 clause using already-reserved words, not a 2014 clause).</summary>
        public override object? VisitOptionsInitializeClause(CobolParserCore.OptionsInitializeClauseContext ctx)
        { _p.Check(Constructs.OptionsInitialize2023, "the OPTIONS INITIALIZE clause"); return base.VisitChildren(ctx); }

        /// <summary>CURRENCY SIGN … WITH PICTURE SYMBOL (ISO §12.3.7) — the 1985 clause had only the
        /// single-character literal form; the PICTURE SYMBOL form is 2002+.</summary>
        public override object? VisitCurrencySignClause(CobolParserCore.CurrencySignClauseContext ctx)
        {
            if (ctx.PIC() is not null)
                _p.Check(Constructs.CurrencyPictureSymbol2002, "CURRENCY SIGN … WITH PICTURE SYMBOL");
            return base.VisitChildren(ctx);
        }

        /// <summary>Whether <paramref name="ctx"/> is nested inside a METHOD definition (the parse-tree analogue of
        /// the binder's <c>InMethod</c> flag) — the GOBACK-RETURNING exclusion.</summary>
        private static bool InMethodDefinition(Antlr4.Runtime.RuleContext ctx)
        {
            for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.MethodDefinitionContext) return true;
            return false;
        }

        // ── Step 14h.4a: the clean expression/phrase gates (one unambiguous detection point each) ─────────────

        /// <summary>The logical XOR / EXCLUSIVE-OR operator (ISO §8.8.4.9) — a COBOL-2023 introduction, recognized as
        /// the ONE <c>xorOperator</c> node every tier spells it through (the condition's leading and succeeding tiers
        /// and the EVALUATE partial-expression spine alike). The gate used to hang off <c>logicalXorExpression</c>
        /// alone, so an XOR inside a partial expression (<c>WHEN &gt; 5 XOR &lt; 3</c>) compiled clean below 2023
        /// (kb/Work PB1390). The body is <see cref="LogicalOperatorGate"/>, shared with the directive stage.</summary>
        public override object? VisitXorOperator(CobolParserCore.XorOperatorContext ctx)
        {
            LogicalOperatorGate.Check(_p._edition, _p._sink, ctx);
            return base.VisitChildren(ctx);
        }

        /// <summary>The target-less <c>GO TO.</c> (ISO §14.9.17; the ANSI-85 alterable GO TO, REMOVED by 2002) —
        /// the grammar's third <c>goToStatement</c> alternative, asked of the ONE format classifier rather than
        /// re-derived from optional children (kb/Work PB412). The condition used to be spelled
        /// <c>procedureName().Length == 0 &amp;&amp; dataReference() is null</c> here AND, in its own words, at
        /// <c>BindGoTo</c> and <c>AlterBindBareGoTo</c> — three copies of one format rule, and a
        /// <c>GO TO DEPENDING</c> with no names slipped between them. No shape can now reach this visitor that
        /// is not one of the two printed formats or this gated legacy arm.</summary>
        public override object? VisitGoToStatement(CobolParserCore.GoToStatementContext ctx)
        {
            if (GoToFormats.Of(ctx) is GoToFormat.AnsiAlterable)
                _p.Check(Constructs.BareGotoRemoved2002, "the GO TO statement");
            return base.VisitChildren(ctx);
        }

        /// <summary>ROUNDED MODE IS (ISO §14.7.4) — a COBOL-2014 introduction (the explicit MODE phrase + the 8-mode
        /// set); a bare ROUNDED is version-invariant. One <c>roundedPhrase</c> per receiver → one node here,
        /// matching the per-receiver RoundingOf gate.</summary>
        public override object? VisitRoundedPhrase(CobolParserCore.RoundedPhraseContext ctx)
        {
            if (ctx.roundingModeName() is not null)
                _p.Check(Constructs.RoundedModeIs2014, "the ROUNDED MODE IS phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>The RETRY phrase (ISO §14.7.9) on OPEN/READ/WRITE/REWRITE/DELETE/DELETE-FILE — a COBOL-2002
        /// introduction. ONE grammar rule at six sites → one override; the phrase's very EXISTENCE is already
        /// governed by the grammar (the OPEN site's <c>{is2002()||retryPhraseAhead()}?</c> forward-detect only
        /// enters the phrase on an unambiguous numeric tail), so presence IS the gate — matching the former
        /// GateRetryIntro (once per phrase).</summary>
        public override object? VisitRetryPhrase(CobolParserCore.RetryPhraseContext ctx)
        {
            _p.Check(Constructs.RetryPhrase2002, "the RETRY phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>The verb lock-RETENTION phrase (WITH LOCK / WITH NO LOCK) on READ/WRITE/REWRITE
        /// (ISO §14.9.30/.51/.35) — a COBOL-2002 introduction. The where-string names the verb from the parent
        /// statement type (matching CheckRecordLockPhrase's <c>verb</c> argument). DISTINCT from the READ …
        /// ADVANCING ON LOCK occurrence (same constructId, a different where-string —
        /// <see cref="VisitReadAdvancingOnLock"/>, moved here from the bound arm by kb/Work PB353) and from
        /// IGNORING LOCK: §14.9.30.2 prints all three as separate syntax elements, all three now gate on
        /// recognition, and a READ writing more than one reports the introduction once per phrase.</summary>
        public override object? VisitRecordLockPhrase(CobolParserCore.RecordLockPhraseContext ctx)
        {
            string verb = ctx.Parent switch
            {
                CobolParserCore.WriteStatementContext => "WRITE",
                CobolParserCore.RewriteStatementContext => "REWRITE",
                _ => "READ",   // readStatement — the sequential (StatementBinder) and keyed (KeyedIo) READ both route here
            };
            _p.Check(Constructs.RecordLockPhrase2002, $"a record-lock phrase on {verb}");
            return base.VisitChildren(ctx);
        }

        /// <summary>READ … IGNORING LOCK (ISO §14.9.30) — the SAME COBOL-2002 introduction as the retention
        /// phrase above, and it needs its own override only because §14.9.30.2 prints the two in DIFFERENT
        /// brackets, so kb/Work PB331 had to give them different grammar rules. ⛔ WITHOUT THIS THE SPLIT WOULD
        /// HAVE SHIPPED `READ … IGNORING LOCK` UNGATED AT --std 85 (feedback_edition_gate_sweep): the phrase's
        /// only edition gate was its membership of <c>recordLockPhrase</c>. Same where-string as the READ arm
        /// above, so a program writing both phrases reports the introduction once per phrase, as it always did.</summary>
        public override object? VisitReadIgnoringLock(CobolParserCore.ReadIgnoringLockContext ctx)
        {
            _p.Check(Constructs.RecordLockPhrase2002, "a record-lock phrase on READ");
            return base.VisitChildren(ctx);
        }

        // ── Step 14h.5: the four I/O PHRASE gates that stayed on the bound arm (kb/Work PB353) ────────────────
        // ⛔ THEY WERE CLASSIFIED "SEMANTIC" AND THEY ARE NOT. The bound arm keyed them on
        // `BoundKeyedStart.Mode ∈ {First,Last}`, `BoundKeyedStart.Length != null`, `IBoundRead.Kind == Previous`
        // and `IBoundRead.AdvancingOnLock` — and every one of those members is assigned from a PARSE fact alone
        // (`st.FIRST()/st.LAST()`, `kp.startWithLength()`, `readDirection PREVIOUS`, `readAdvancingOnLock`), with
        // no file organization, access mode, USAGE or category anywhere in the decision. So the bound-tree home
        // bought no precision and cost the diagnostic on every path that bails out BEFORE the node is built.
        // MEASURED at --std 85 before this move: `START NOSUCHF FIRST` → COBOLNET1639 alone; `START SDF FIRST`
        // on an SD → COBOLNET1692 alone; `START IXF KEY = NOSUCHK WITH LENGTH 3` → COBOLNET1639 + the PB236
        // deferral alone; `READ NOSUCHF PREVIOUS ADVANCING ON LOCK` → COBOLNET1639 alone. Four statements
        // naming a post-85 construct, four compilations that never named the edition.
        // The where-strings are byte-identical to the bound-arm sites they replace.

        /// <summary>The INVOKE statement (ISO §14.9.23) — a COBOL-2002 introduction. <c>invokeStatement</c> is a
        /// dedicated grammar rule reached from exactly one place (<c>StatementBinder</c> → <c>OoBindInvoke</c>),
        /// so presence IS the gate — and recognition fixes both halves of the bound arm's <c>case BoundInvoke</c>:
        /// an INVOKE whose target resolves to neither a data item nor a class returns COBOLNET0823 before any
        /// node exists (it named no edition at --std 85), and <c>BoundInvoke</c> is equally the bound form of a
        /// PROPERTY get/set re-write and of NEW / SELF-NEW, which the where-string called "the INVOKE
        /// statement" (kb/Work PB353).</summary>
        public override object? VisitInvokeStatement(CobolParserCore.InvokeStatementContext ctx)
        { _p.Check(Constructs.Invoke2002, "the INVOKE statement"); return base.VisitChildren(ctx); }

        /// <summary>The INLINE METHOD INVOCATION (ISO §8.4.3.4 — the §8.4.3.1.2 Format 4 identifier), gated
        /// with the rest of the object-orientation facility at 2002 (kb/Work PB428).
        /// <para>⛔ IT IS GATED ON RECOGNITION AND IT NOW CAN BE, which is the whole reason the row changed.
        /// The registry row used to read <c>inline-method-invocation-2023</c> and carried its own reason for
        /// staying PENDING: "the identifier(args) form has NO distinctive token (ambiguous with subscripting
        /// and function-reference), so below 2023 it does not yield a distinct COBOLNET0900". That was true of
        /// the misnamed shape the grammar had; the REAL construct is led by the §8.7.4 invocation operator
        /// <c>::</c>, which is unambiguous with everything — so a below-2002 inline invocation names the
        /// edition instead of failing as punctuation.</para>
        /// <para>⚠ THE EDITION EDGE IS DERIVED, NOT QUOTED (the VERSION_CHANGE_REFERENCE discipline — the repo
        /// holds no 2002 or 2014 text). Three legs: (a) §A.4.10 lists exactly three OPTIONAL
        /// object-orientation elements — CLASS-ID INHERITS repetition, INTERFACE-ID INHERITS repetition and
        /// parametric polymorphism — and inline method invocation is NOT among them, so it is mandatory
        /// surface of the object-orientation facility, whose every other element this compiler gates at
        /// 2002 (CLASS-ID, INVOKE, USAGE OBJECT REFERENCE …);
        /// (b) Annex E itemizes the 2014→2023 delta and lists no inline invocation, so it predates 2023;
        /// (c) §8.4.3.4.4 GR1 defines it in terms of the INVOKE statement, a 2002 introduction — a construct
        /// cannot be defined by one that does not exist yet.</para></summary>
        public override object? VisitInlineMethodInvocation(CobolParserCore.InlineMethodInvocationContext ctx)
        {
            _p.Check(Constructs.InlineMethodInvocation2002, "an inline method invocation (the '::' operator)");
            return base.VisitChildren(ctx);
        }

        /// <summary>START … FIRST / … LAST (ISO §14.9.41.2 general format — <c>cite.py --check 14.9.41.2 "FIRST"</c>
        /// OK, the printed alternative stack <c>FIRST | LAST | KEY …</c>) — a COBOL-2002 introduction. FIRST and
        /// LAST are ONE construct gated once, and §14.9.41.3 SR2 ("If the organization of the file referenced by
        /// file-name-1 is sequential, either the FIRST or the LAST phrase shall be specified" —
        /// <c>cite.py --check 14.9.41.3</c> OK) makes the phrase MANDATORY on a sequential file rather than
        /// different there, so the gate cannot depend on the organization and must not be reachable only through
        /// the arm that resolves one.</summary>
        public override object? VisitStartStatement(CobolParserCore.StartStatementContext ctx)
        {
            if (ctx.FIRST() is not null || ctx.LAST() is not null)
                _p.Check(Constructs.StartFirstLast2002, $"START {(ctx.LAST() is not null ? "LAST" : "FIRST")}");
            return base.VisitChildren(ctx);
        }

        /// <summary>START … KEY … WITH LENGTH (ISO §14.9.41.2 — the printed <c>[ WITH LENGTH
        /// arithmetic-expression-1 ]</c> bracket; <c>cite.py --check 14.9.41.2 "arithmetic-expression-1"</c> OK
        /// lands on that very line — quote the operand, not <c>"WITH LENGTH"</c>, which the underline markup
        /// splits) — a COBOL-2002 introduction, one Check per phrase. Its own
        /// override rather than a branch of the START one because <c>startWithLength</c> is its own grammar rule
        /// with exactly one use site (<c>startKeyPhrase</c>), so presence IS the gate. §14.9.41.3 SR8 ("If the
        /// LENGTH phrase is specified, file-name-1 shall reference a file with indexed organization" —
        /// <c>cite.py --check 14.9.41.3 "If the LENGTH phrase is specified, file-name-1 shall reference a file
        /// with indexed organization"</c> OK) restricts the phrase to indexed organization; that is a SYNTAX RULE
        /// the binder reports separately and NOT this gate's business — an ill-organized START must still name the
        /// edition of the phrase it wrote.</summary>
        public override object? VisitStartWithLength(CobolParserCore.StartWithLengthContext ctx)
        {
            _p.Check(Constructs.StartWithLength2002, "the START … WITH LENGTH phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>READ … PREVIOUS (ISO §14.9.30.2 Format 1 braces) — a COBOL-2002 introduction. The gate is on
        /// the PREVIOUS alternative of <c>readDirection</c> only: NEXT is the non-underlined default alternative
        /// (§14.9.30.3 SR8 — <c>cite.py --check 14.9.30.3 "the PREVIOUS phrase"</c> OK: "If neither the NEXT
        /// phrase nor the PREVIOUS phrase is specified and ACCESS MODE SEQUENTIAL is specified … the NEXT phrase
        /// is implied"), so an implied direction is not a written phrase and does not gate. ONE override serves
        /// the sequential and the keyed READ alike — <c>readDirection</c> has a single use site — which is the
        /// organization-independence kb/Work PB334 won for the bound arm, now unconditional.</summary>
        public override object? VisitReadDirection(CobolParserCore.ReadDirectionContext ctx)
        {
            if (ctx.PREVIOUS() is not null)
                _p.Check(Constructs.ReadPrevious2002, "READ … PREVIOUS");
            return base.VisitChildren(ctx);
        }

        /// <summary>READ … ADVANCING ON LOCK (ISO §14.9.30.2 bracket 1) — the SAME COBOL-2002 record-lock
        /// introduction as <see cref="VisitRecordLockPhrase"/> and <see cref="VisitReadIgnoringLock"/>, and the
        /// last of the three printed lock spellings to reach the parse arm. ⛔ Until kb/Work PB353 this ONE
        /// construct id fired from BOTH arms — two of its three spellings on recognition, this one on
        /// <c>IBoundRead.AdvancingOnLock</c> — which is precisely the "a Check for any one construct fires from
        /// EXACTLY one arm" invariant this pass documents, broken. §14.9.30.3 SR6 ("None of the phrases ADVANCING,
        /// AT END, NEXT, NOT AT END, or PREVIOUS shall be specified if ACCESS MODE RANDOM is specified in the file
        /// control entry for file-name-1" — <c>cite.py --check 14.9.30.3 "ADVANCING"</c> OK) restricts WHERE the
        /// phrase may be written; <c>Validation.CheckReadRandomAccessPhrases</c> reports that from the binder, and
        /// the EDITION of the phrase is not conditional on the access mode the binder resolves.
        /// The where-string is the bound arm's, not the retention phrase's — the two are distinct occurrences and
        /// a READ writing both still reports the introduction once per phrase.</summary>
        public override object? VisitReadAdvancingOnLock(CobolParserCore.ReadAdvancingOnLockContext ctx)
        {
            _p.Check(Constructs.RecordLockPhrase2002, "the READ … ADVANCING ON LOCK phrase");
            return base.VisitChildren(ctx);
        }

        // ── Step 14h.4b: the boolean-operator + national/boolean LITERAL gates (the delicate cases) ────────────
        // (1) The boolean OPERATORS gate is detected at the primaryCondition / computeStatement ALTITUDE with a
        //     whole-subtree operator scan (BooleanOperatorGate) — never per booleanExpression node: the tiers nest via parentheses /
        //     the relation form, so a per-node gate would over-count. (2) The national/boolean LITERAL gates fire
        //     for a PROCEDURE-DIVISION statement operand only (a StatementContext ancestor); a data-division VALUE
        //     literal is left to the data/PIC gate (its item's national/boolean USAGE, Step 14g) — firing here too
        //     would double the below-2002 diagnostic.

        /// <summary>⭐ THE boolean-operator introduction gate — ONE rule, called once per GRAMMAR SITE that hosts
        /// a top-level <c>booleanExpression</c>. B-AND/B-OR/B-XOR/B-NOT are a COBOL-2002 introduction (ISO
        /// §8.7.2) and the four boolean SHIFT operators a COBOL-2023 one (§8.8.2 rule 8).
        /// <para>⚠ THE GATE IS PER-SITE, NOT PER-NODE, AND THAT IS DELIBERATE: the <c>booleanExpression</c>
        /// tiers nest through parentheses and through the relation form, so a gate hanging off the tier rule
        /// itself would fire once per nesting level and multiply the diagnostic. Passing a site's operand(s)
        /// here fires exactly once for that site, matching the binder's own altitude
        /// (<c>HasBoolOp</c> in ConditionBinder.BindPrimaryBoolean).</para>
        /// <para>⛔ IT WAS COPIED PER SITE UNTIL PB46 ADDED A THIRD ONE. Two identical four-line bodies is the
        /// point at which a new grammar site silently ships UNGATED — a 2023 shift operator accepted under
        /// <c>--std 2002</c>. The hosting sites are enumerated by <c>BooleanExpressionGateSiteDriftTests</c>,
        /// which reads the .g4 files, so a fourth one fails a test instead of passing silently.</para>
        /// <para>⛔ THE BODY IS <see cref="BooleanOperatorGate"/>, SHARED WITH THE DIRECTIVE STAGE (kb/Work PB1370): a
        /// compile-time boolean expression is a site too, and it is parsed from a directive fragment this walk never
        /// reaches, so the conditional-compilation stage asks the same body there.</para></summary>
        private void GateBooleanOperators(params CobolParserCore.BooleanExpressionContext?[] operands) =>
            BooleanOperatorGate.Check(_p._edition, _p._sink, operands);

        /// <summary>Site — a bare boolean-expression CALL argument (§14.9.4.2 Format 2's keyword-less
        /// boolean-expression-1; kb/Work PB130 — the callArgument alternation gained the guarded arm and
        /// the BooleanExpressionGateSiteDrift net demanded this gate the same day).</summary>
        public override object? VisitCallArgument(CobolParserCore.CallArgumentContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
            if (ctx.OMITTED() is not null)
                _p.Check(Constructs.OmittedArguments2002, "the OMITTED argument");
            return base.VisitChildren(ctx);
        }

        /// <summary>The omitted-argument facility's other three surfaces (kb/Work PB133 wave C) — all
        /// COBOL-2002 introductions on one construct row: the BY REFERENCE OMITTED spelling, the OPTIONAL
        /// formal-parameter phrase (§14.2.2), and the §8.8.4.8 omitted-argument condition.</summary>
        public override object? VisitCallByReference(CobolParserCore.CallByReferenceContext ctx)
        {
            if (ctx.OMITTED() is not null)
                _p.Check(Constructs.OmittedArguments2002, "the OMITTED argument");
            return base.VisitChildren(ctx);
        }

        public override object? VisitUsingParameter(CobolParserCore.UsingParameterContext ctx)
        {
            if (ctx.OPTIONAL() is not null)
                _p.Check(Constructs.OmittedArguments2002, "the OPTIONAL formal parameter");
            return base.VisitChildren(ctx);
        }

        public override object? VisitUsingByReference(CobolParserCore.UsingByReferenceContext ctx)
        {
            if (ctx.OPTIONAL() is not null)
                _p.Check(Constructs.OmittedArguments2002, "the OPTIONAL formal parameter");
            return base.VisitChildren(ctx);
        }

        public override object? VisitComparisonExpression(CobolParserCore.ComparisonExpressionContext ctx)
        {
            if (ctx.OMITTED() is not null)
                _p.Check(Constructs.OmittedArguments2002, "the omitted-argument condition");
            return base.VisitChildren(ctx);
        }

        /// <summary>Site 1 — a boolean expression in a CONDITION (§8.8.4.2.2 relation / §8.8.4.3 simple
        /// condition). A B-op-free comparison uses the untouched shared comparison rule and never enters here.</summary>
        public override object? VisitPrimaryCondition(CobolParserCore.PrimaryConditionContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
            return base.VisitChildren(ctx);
        }

        /// <summary>Site 2 — the COMPUTE Format 2 RHS (ISO §14.9.8). The F1 arithmetic alternative has no
        /// <c>booleanExpression</c>.</summary>
        public override object? VisitComputeStatement(CobolParserCore.ComputeStatementContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
            return base.VisitChildren(ctx);
        }

        /// <summary>Site 4 — <c>CALL … USING BY CONTENT boolean-expression-1</c> (ISO §14.9.4.2 Format 2;
        /// fix-queue PB46 CALL half). ⚙ THIS OVERRIDE EXISTS BECAUSE THE DRIFT TEST DEMANDED IT: widening
        /// <c>callByContent</c> added a fourth grammar site and
        /// <c>BooleanExpressionGateSiteDriftTests</c> — written one item earlier for exactly this — failed with
        /// "grammar rule(s) admit a booleanExpression with no introduction gate: callByContent". The list is
        /// derived from the <c>.g4</c> files, so it saw the new site the moment it appeared.</summary>
        public override object? VisitCallByContent(CobolParserCore.CallByContentContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
            return base.VisitChildren(ctx);
        }

        /// <summary>Site 3 — <c>INVOKE … USING BY CONTENT boolean-expression-1</c> (ISO §14.9.23.2; fix-queue
        /// PB46). The BooleanOperators2002 half is unreachable in practice — INVOKE is itself a COBOL-2002
        /// introduction, so no edition admits the statement but not the operators — but the SHIFT half is
        /// live: <c>BY CONTENT B1 B-SHIFT-L 2</c> is a 2023 construct inside a 2002 statement, and without
        /// this call it would compile clean under <c>--std 2002</c> and <c>--std 2014</c>.</summary>
        public override object? VisitInvokeArgument(CobolParserCore.InvokeArgumentContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
            // §14.9.23.2's OMITTED operand (kb/Work PB757) rides the same construct row as CALL's twin.
            if (ctx.OMITTED() is not null)
                _p.Check(Constructs.OmittedArguments2002, "the OMITTED argument");
            return base.VisitChildren(ctx);
        }

        /// <summary>The INLINE METHOD INVOCATION's argument — <c>boolean-expression-1</c> of §8.4.3.4.2's
        /// printed brace (kb/Work PB428), and the exact twin of the INVOKE site above: §8.4.3.4.4 GR1 a) makes
        /// these operands the arguments of the equivalent <c>INVOKE … USING</c>, so they owe the same boolean
        /// gates. The BooleanOperators2002 half is again unreachable (the construct is itself 2002), the SHIFT
        /// half is live — <c>O :: "M" (B1 B-SHIFT-L 2)</c> is a 2023 operator inside a 2002 construct and
        /// would otherwise compile clean under <c>--std 2002</c> / <c>--std 2014</c>.
        /// <para>⚠ The gate is owed because the ARGUMENT RULE GAINED THE ALTERNATIVE: `argument` carried no
        /// `booleanExpression` arm at all until PB428 wrote the general format's five printed operand forms
        /// into it, and `BooleanExpressionGateSiteDriftTests` is what said so out loud.</para></summary>
        public override object? VisitArgument(CobolParserCore.ArgumentContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
            return base.VisitChildren(ctx);
        }

        /// <summary>An intrinsic-function ARGUMENT that is a boolean expression (§8.4.3.2.3 SR8; kb/Work PB65 —
        /// `INTEGER-OF-BOOLEAN(BIT-A B-AND BIT-B)`): the same boolean-operator introduction gates as every other
        /// boolean-expression site (the 2002 data / the 2023 shift operators).</summary>
        public override object? VisitFunctionArgument(CobolParserCore.FunctionArgumentContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
            return base.VisitChildren(ctx);
        }

        /// <summary>National (<c>N"…"</c>) and boolean (<c>B"…"</c>) literals as a PROCEDURE-DIVISION statement
        /// operand — COBOL-2002 introductions (ISO §8.3.3.5 / §8.3.3.4). Scoped to a StatementContext ancestor so a
        /// DATA-division VALUE literal is left to the data/PIC gate (Step 14g) — firing here too would double the
        /// below-2002 diagnostic. One Check per literal occurrence, matching the binder's NationalLiteralOperand /
        /// BooleanLiteralOperand / boolean-expression-operand paths.</summary>
        public override object? VisitNonNumericLiteral(CobolParserCore.NonNumericLiteralContext ctx)
        {
            bool nat = ctx.NATLIT() is not null;
            if ((nat || ctx.BOOLLIT() is not null) && InStatement(ctx))
                _p.Check(nat ? Constructs.NationalData2002 : Constructs.BooleanData2002,
                    nat ? "national literal N\"…\"" : "boolean literal B\"…\"");
            // The literal's OWN syntax rules — the §8.3.3 length and hexadecimal-grouping rules — are LiteralScreenPass's,
            // asked of every literal TOKEN wherever it is written (kb/Work PB1393). The grouping rule used to be asked
            // here and in the ALL arm below, and a concatenation operand and a keyword-omitted intrinsic argument
            // slipped past both.
            return base.VisitChildren(ctx);
        }

        /// <summary>The <c>ALL literal</c> figurative's literal-1 (ISO §8.3.3.6.3 SR2 — kb/Work PB71): a national or
        /// boolean literal-1 is the same COBOL-2002 introduction as the bare literal (statement-scoped, as above —
        /// a VALUE clause's is the data/PIC gate's). The tokens are direct children of figurativeConstant, not of
        /// nonNumericLiteral, so the override above never saw them — `ALL B"1"` at --std 85 was ungated. Only the
        /// EDITION gates live here. A CONCATENATED literal-1 is THE §8.8.3 concatenationExpression node (kb/Work
        /// PB1627), so the &amp; gate is <see cref="VisitConcatenationExpression"/>'s on recognition, and §8.3.3.6.3
        /// SR2 and the §8.8.3.2 rules are the binder's, through the one Format 6 reader
        /// <c>ConcatFolder.FoldAllLiteral</c> — they need the program's constant-names, which a parse walk has not
        /// got (the rules used to be asked here, over a quoted-only operand list).</summary>
        public override object? VisitFigurativeConstant(CobolParserCore.FigurativeConstantContext ctx)
        {
            if (ctx.allLiteral() is not { } al || !InStatement(ctx)) return base.VisitChildren(ctx);
            // The literal-1 tokens: the one quoted literal, or a concatenation's quoted operands.
            var ops = al.concatenationExpression()?.concatOperand();
            bool nat = ops?.Any(o => o.NATLIT() is not null) ?? al.NATLIT() is not null;
            bool bl = ops?.Any(o => o.BOOLLIT() is not null) ?? al.BOOLLIT() is not null;
            if (nat || bl)
                _p.Check(nat ? Constructs.NationalData2002 : Constructs.BooleanData2002,
                    nat ? "the figurative ALL N\"…\"" : "the figurative ALL B\"…\"");
            return base.VisitChildren(ctx);
        }

        /// <summary>A concatenation expression — the <c>&amp;</c> operator joining literals (ISO §8.8.3) — is a
        /// COBOL-2002 introduction (concat-operator-2002; roadmap D6). POSITION-BLIND, unlike the national/
        /// boolean literal gate above: §8.8.3.3 GR3 lets a concat stand anywhere a literal may (VALUE clauses,
        /// SPECIAL-NAMES operands, statement operands, FUNCTION arguments), and no data-side gate covers the
        /// construct — recognition of the parse node IS the one funnel. One Check per concatenation
        /// expression; the &amp; token appears in no other rule, so recognition is exact. The §8.8.3.2 SR
        /// checks (same class / no ALL figurative / 8,191 cap) are the binder's ConcatFolder — edition-
        /// invariant semantics, not gates.</summary>
        public override object? VisitConcatenationExpression(CobolParserCore.ConcatenationExpressionContext ctx)
        {
            _p.Check(Constructs.ConcatOperator2002, "a concatenation expression (the & operator)");
            return base.VisitChildren(ctx);
        }

        /// <summary>Whether <paramref name="ctx"/> sits inside a procedure-division statement (a StatementContext
        /// ancestor) — the scope that reaches the binder's literal-operand gates, excluding data-division VALUE.</summary>
        private static bool InStatement(Antlr4.Runtime.RuleContext ctx)
        {
            for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.StatementContext) return true;
            return false;
        }


        // Which cobolWord token TYPES the funnel checks POSITION-BLIND (P2.4, refined — DEVLOG 585): IDENTIFIER
        // occurrences are ALWAYS genuine words (the lexer didn't tokenize them), and they carry the whole
        // newly-reserved payload (the Annex-E 2023 additions lex as IDENTIFIER). The six EC-band tokens are ALSO
        // checked everywhere — §8.9-reserved since 2002 (except the context-sensitive STATEMENT, inert in the
        // table) and their keyword uses parse through dedicated statement rules, never a name slot. The REMAINING
        // allowlisted tokens (the screen/report/OPTIONS bands: COL, COLUMN, AUTO, …) are checked POSITION-AWARE
        // instead (the P2.8 W2 adversarial review of the former blanket exclusion): the permissive grammar can
        // bind their KEYWORD occurrences into optional entry-name slots (RW104A binds the report COLUMN clause's
        // keyword into the report-group entry-name slot), so a position-blind check false-rejects conforming
        // CCVS-85 programs. They reject only in a position provably a user-word use — IsProvableUserWordPosition.
        private static readonly HashSet<int> CheckedTokenTypes =
        [
            CobolLexer.IDENTIFIER,
            CobolLexer.RAISE, CobolLexer.RAISING, CobolLexer.RESUME,
            CobolLexer.CONDITION, CobolLexer.EC, CobolLexer.STATEMENT,
            // The 2023 logical-operator words (the W3 XOR regating, VCR rows 32/41): admitted to cobolWord as
            // user words below 2023; their keyword occurrences parse through the operator alternative, never a
            // name slot — so, like the EC band, they are position-safe to check everywhere.
            CobolLexer.XOR, CobolLexer.EXCLUSIVE_OR,
            // The 2002 boolean operators (ISO §8.7.2): user words at 85, funnel-0901'd at ≥2002. Their keyword
            // occurrences parse only through the booleanExpression tiers, never a name slot — position-safe.
            CobolLexer.B_AND, CobolLexer.B_OR, CobolLexer.B_XOR, CobolLexer.B_NOT,
            // The 2002 GROUP-USAGE clause word (ISO §13.18.29; D20/PB79): a user word at 85, funnel-0901'd at ≥2002.
            // Its keyword occurrence parses only through groupUsageClause, never a name slot — position-safe.
            CobolLexer.GROUP_USAGE,
            // The 2002 file-sharing §8.9-reserved words (SHARING/RETRY/UNLOCK): user words at 85, 0901'd ≥2002.
            // Keyword occurrences parse only through the gated sharing/lock rules, never a name slot.
            CobolLexer.SHARING, CobolLexer.RETRY, CobolLexer.UNLOCK,
            // The X3.23-1985 notInGrammar 85-acceptance words (VCR Table 7 rows 7.15–7.18): '85-reserved, user
            // words at later editions per the §8.9 table (RERUN/ENTER free ≥2002, DEBUGGING ≥2014, the rest
            // ≥2023). Their keyword occurrences parse through dedicated rules, never a name slot.
            CobolLexer.FACTORY,   // §11.4 (2002+): keyword slots are factoryParagraph/END FACTORY/FACTORY OF only — position-blind safe (the EC-band argument)
            CobolLexer.OVERRIDE,  // §11.7 (2002+): the METHOD-ID attribute slot is a direct token — position-blind safe
            CobolLexer.GET, CobolLexer.PROPERTY, CobolLexer.INTERFACE,   // §11.6/§11.7/§13.18.42 (2002+): keyword slots are direct tokens — position-blind safe. IMPLEMENTS is §8.10 context-sensitive: NEVER here.
            CobolLexer.PROTOTYPE, // §11.5 (2002+): the keyword occurs only in the functionIdParagraph `IS PROTOTYPE` tail — a direct token, never a name slot — position-blind safe (the UDF-3 wave)
            CobolLexer.CONSTANT,  // §13.10/§13.18.15 (2002+): keyword slots are the constantEntryBody head and `CONSTANT RECORD` — direct tokens, never a name slot — position-blind safe (P10 Step 15; the PROTOTYPE precedent)
            CobolLexer.AS,        // §13.10 (2002+): the sole keyword slot is the constantEntryBody `AS` — a direct token, never a name slot — position-blind safe (the CONSTANT twin)
            CobolLexer.PROGRAM_POINTER,  // §13.18.60 (2002+): the sole keyword slot is the programPointerUsage head — a direct token — position-blind safe (P10 Step 7)
            CobolLexer.FUNCTION_POINTER, // §13.18.60 (2014+): the sole keyword slot is the functionPointerUsage head — a direct token — position-blind safe (P10 Step 7)
            CobolLexer.RERUN, CobolLexer.ENTER, CobolLexer.EVERY, CobolLexer.CLOCK_UNITS,
            CobolLexer.DEBUGGING, CobolLexer.REFERENCES, CobolLexer.PROCEDURES,
        ];

        /// <summary>
        /// The §8.9 reserved-word funnel (P2.4): every user-defined word reaches the tree through the
        /// <c>cobolWord</c> rule — IDENTIFIER plus the allowlisted context-keyword tokens — so ONE text-based
        /// check here covers 2023-new words that lex as IDENTIFIER (COMMIT, FINALLY, …) AND the EC-band tokens
        /// the 2023 edition reserves (RAISE/RAISING/RESUME/CONDITION/EC). The grammar stays a permissive superset
        /// ("legal user word at every edition"); this arm enforces per edition — position-blind for
        /// <see cref="CheckedTokenTypes"/>, position-AWARE for every other allowlisted context keyword
        /// (<see cref="IsProvableUserWordPosition"/> — the P2.8 W2 subset). Only high-confidence rows reject
        /// (<see cref="ReservedWordSet.RejectsAt"/> — the conservative policy); severity routes through
        /// <see cref="EditionSeverityPolicy"/> (error strict / warning permissive, the 0901 band row).
        /// </summary>

        public override object? VisitCobolWord(CobolParserCore.CobolWordContext ctx)
        {
            // §8.3.2.1 word-length ceiling — a COBOL word is "not more than 63 characters" at COBOL-2023; the
            // limit was 31 in 2002/2014 and 30 in 1985 (E.3.3 item 11: a 2023 RELAXATION — the OPPOSITE direction
            // from a new-feature introduction gate, so it is NOT a 0900 construct). Fire below 2023 for 32–63-char
            // words, and at EVERY edition for >63 (a hard cap). Checked for every COBOL word regardless of role
            // (reserved words are all short — only user-defined words reach the limit); deduped per distinct word.
            string raw = ctx.Start.Text;
            int max = CobolWordRule.MaxLength(_p._edition.Year);   // ONE ceiling — the directive stages share it
            // §8.3.2.1's OTHER half — the word CHARACTER SET (fix-queue R02). The same sentence that caps the
            // length names the admissible characters: "…the basic special characters HYPHEN AND UNDERSCORE. The
            // hyphen or underscore shall not appear as the first or last character in such words." The
            // underscore is a COBOL-2002 introduction ('85's set is hyphen-only), and the lexer admits it at
            // every edition (superset-parse), so this is where a below-2002 use is named.
            // ⚠ Deduped per distinct word through the SAME set the length check uses: one diagnostic per word
            // per compilation, matching the §8.9 funnel's posture rather than one per occurrence.
            if (raw.Contains('_') && (_overlongWords ??= []).Add("_" + raw.ToUpperInvariant()))
                _p.Check(Constructs.UserWordUnderscore2002, $"the underscore in the COBOL word '{raw}'");
            if (raw.Length > max && (_overlongWords ??= []).Add(raw.ToUpperInvariant()))
                _p._sink.Report(new EditionDiagnostic(DiagnosticCatalog.WordLengthExceeded.Code,
                    EditionSeverity.Error, DiagnosticCatalog.WordLengthExceeded.Id,
                    CobolWordRule.LengthViolation(raw, _p._edition.Year)!, "", "ISO §8.3.2.1"));
            if (!CheckedTokenTypes.Contains(ctx.Start.Type) && !IsProvableUserWordPosition(ctx))
                return base.VisitChildren(ctx);
            string word = ctx.Start.Text.ToUpperInvariant();
            // A BARE word argument of a function reference may be a §15 PHRASE WORD (FIND-STRING ANYCASE,
            // CONVERT HEX/NAT/ANUM/BYTE, MODULE-NAME CURRENT/ACTIVATING/NESTED/STACK/TOP-LEVEL, …) — a use OF
            // the reserved word, not a user-defined-word use (§8.4.3.2 SR8 + the per-function §15 argument
            // rules; the EXCEPTION-OBJECT precedent below). The position is NOT provable: only the sole-word
            // shape skips — a subscripted/qualified/compound argument is provably an operand and stays
            // funneled, and a genuine data-item collision is still caught at its DECLARATION slot. (P7
            // Step 12: arguments parse as real trees, so phrase words reach cobolWord — the former SUB-token
            // capture never surfaced them here.)
            if (IsBareFunctionArgumentWord(ctx))
                return base.VisitChildren(ctx);
            // ⛔ NO KEYWORD SLOT IS EXEMPTED HERE, AND THE ABSENCE IS THE DESIGN (kb/Work PB764). This method used to
            // carry a LADDER — six `ctx.Parent is …Context` arms (ENTRY-CONVENTION, CALL AS NESTED, the SPECIAL-NAMES
            // LOCALE clause, ALPHABET IS LOCALE, PICTURE LOCALE, CHARACTER CLASSIFICATION) and an ancestor walk over
            // SET LOCALE — each saying "this cobolWord is the general format's own keyword, not a name". A keyword
            // standing where its format prints it is a use OF the word (ISO §5.2.2 / §5.2.3), outside §8.3.2.1 rule 1
            // entirely; but a list of the places that happens is exactly what forgot `SORT … WITH DUPLICATES IN ORDER`
            // for two trains (kb/Work PB704). Those keywords now parse as `formatWord` — IDENTIFIER, never cobolWord —
            // so this funnel does not meet them BY CONSTRUCTION, and the next general format that spells a keyword by
            // text needs no C# here (`FormatWordDriftTests` fails if it borrows cobolWord instead).
            // ⚠ The ladder was also WRONG in two slots: it exempted ENTRY-CONVENTION's entry-convention-name-1 and the
            // LOCALE clause's external-locale-name-1, which are SYSTEM-NAMES (§8.3.2.3.1) — and §8.3.2.1 rule 1 forbids
            // a reserved word as a system-name exactly as it does as a user-defined word. They reach the funnel now.
            // What remains below is not position-keyed: a DECLINED construct is refused whole, by the pass that owns it.
            if (DeclinedFacilityPass.EnclosingDeclinedConstruct(ctx) is not null)
                return base.VisitChildren(ctx);
            // EXCEPTION-OBJECT ANYWHERE INSIDE A STATEMENT is a reference to the PREDEFINED OBJECT REFERENCE
            // (§8.4.3.6), never a user-defined word: §8.4.3.6.3 SR2 declares the name ("implicitly described as
            // class object and category object reference, as an external data item, and as a universal object
            // reference") and the §8.9 reservation is exactly what makes the reference unambiguous — a program
            // CANNOT have defined a word with this spelling, so there is no user-defined word to complain about.
            // ⛔ THIS USED TO BE A POSITION WALK (an `objectReference` / `SetToValueStatement` ancestor), i.e. a
            // THIRD opinion about what the name is, narrowed to the one statement that could bind it — so
            // `MOVE U TO EXCEPTION-OBJECT` and `IF EXCEPTION-OBJECT = NULL` each drew "'EXCEPTION-OBJECT' is a
            // reserved word and cannot be used as a user-defined word" beside "'EXCEPTION-OBJECT' is not
            // defined", a pair of contradictory falsehoods. The RESOLVER now knows the register
            // (ReferenceResolver → ExceptionObjectPlace) and the receiving chokepoint screens §8.4.3.6.3 SR1, so
            // every statement position is bound by rule and the funnel has nothing to add (kb/Work PB922).
            // A DECLARATION still keeps the funnel: the walk stops at the enclosing statement, and a data
            // description entry has none — which is what must reject `01 EXCEPTION-OBJECT PIC X.`
            if (Binding.Procedure.OoBinder.OoIsExceptionObject(word))
                for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                    if (a is CobolParserCore.StatementContext)
                        return base.VisitChildren(ctx);
            // Under WITH DEBUGGING MODE a DEBUG-* occurrence is the X3.23-1985 REGISTER (DEBUG-ITEM family), not a
            // user-defined word — a legal '85 reference to the now-modeled debug facility (VCR Table 7 row 7.17;
            // the switch-ABSENT case never gets here — comment treatment skips the section body). The binder
            // resolves it to a DebugRegisterPlace, so ACCEPT it here (no §8.9 violation, no not-implemented note).
            if (_debuggingModeDeclared && word.StartsWith("DEBUG-", StringComparison.Ordinal))
                return base.VisitChildren(ctx);
            FlagReservedUserWord(word);
            return base.VisitChildren(ctx);
        }

        /// <summary>⛔ THE ONE §8.9 reserved-word report — both walks that can reach a user-defined-word
        /// position call it, so the message, the code, the once-per-word suppression and the severity have a
        /// single definition (<c>feedback_one_rule_one_place</c>; it was written out twice, and the second copy
        /// is exactly where a fix would have been forgotten).
        /// <para>The severity routes through the ONE policy and the VERDICT is COMPUTED from the word's own
        /// reservation interval, never asserted here. Hard-coding <c>Removed</c> made <c>--permissive</c> accept
        /// a RE-RESERVED word (RECEIVE, END-RECEIVE) as a user-defined word at COBOL-85, where the '85
        /// communication module reserves it and no conforming '85 program could contain one — a
        /// not-yet-introduced construct getting the migration mode's leniency (CA14's policy; these two were
        /// found by the introduction-axis theory the fix added).</para></summary>
        private void FlagReservedUserWord(string word)
        {
            if (!_reservedWords.RejectsAt(word, _p._edition.Year) || !(_flaggedWords ??= []).Add(word)) return;
            _p._sink.Report(new EditionDiagnostic(EditionCodes.ReservedWord,
                EditionSeverityPolicy.For(_reservedWords.UserWordVerdictAt(word, _p._edition.Year), _p._edition),
                "edition-reserved-word",
                ReservedWordSet.UserWordViolationMessage(word, _p._edition.Year), "", "ISO §8.9"));
        }

        /// <summary>kb/Work PB137/PB300/PB693: the generated <c>reservedGatedWord</c> rule keeps a DECLARATION of
        /// a reservation-gated word parseable at the editions where §8.9 reserves it, precisely so this funnel can
        /// NAME the word — it bypasses cobolWord entirely, so the §8.9 check must meet it here.
        /// <para>⛔ SLOT-LIST-FREE AND WORD-LIST-FREE BY CONSTRUCTION. The arm hangs on the SHARED RULE, not on
        /// the slots that use it: every use of <c>reservedGatedWord</c> is a definition slot by construction (the
        /// rule exists only to re-admit a word §8.9 forbids as a user-defined word, so its presence in the tree IS
        /// the §8.3.2.1 rule-1 violation), which is why <c>dataName</c> and <c>programName</c> both get their 0901
        /// with no second override — and why the NEXT definition slot needs no C# at all (CLAUDE.md rule 5). It
        /// was <c>VisitDataName</c>, reading <c>ctx.reservedGatedWord()</c>, and before that a hand-written
        /// <c>ctx.COMMIT() is not null ? "COMMIT" : …</c> word list that had already gone stale (CRT/CURSOR).
        /// Every alternative of the rule is a SINGLE token, so the subrule's own text IS the word.</para>
        /// <para>PROCEDURE and FILLER are deliberately NOT reached: they are separate <c>dataName</c>
        /// alternatives — PROCEDURE is §8.9-reserved at every edition and NC205A legally names a data item with
        /// it, and FILLER is not a user-defined word at all.</para></summary>
        public override object? VisitReservedGatedWord(CobolParserCore.ReservedGatedWordContext ctx)
        {
            FlagReservedUserWord(ctx.GetText().ToUpperInvariant());
            return base.VisitChildren(ctx);
        }
    }
}
