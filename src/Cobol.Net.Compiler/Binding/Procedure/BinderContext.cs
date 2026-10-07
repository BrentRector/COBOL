// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Binding.Validation;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// The binder collaborators' shared spine (P7 Step 10c — the phase doc's §Step 10 AS-BUILT PLAN; the
/// <c>EmitContext</c> precedent: core refs + derived accessors, ONE instance per bound unit / class roster,
/// constructed and held by <c>StatementBinder</c> — the transitional host until the 10t final wiring). The
/// as-built shape supersedes the design sketch's names: <see cref="Edition"/> (the ONE diagnostic sink AND
/// edition surface — there is no separate <c>IDiagnosticSink</c>/<c>EditionInfo</c> split), <see cref="Symbols"/>
/// (the ONE per-binder scope-aware <c>SymbolTable</c> from Exec Step B — the sketch's <c>SymbolTableBuilder</c>
/// was superseded), and <see cref="ActiveScope"/> (recomputed per READ — callers must never capture it at
/// construction; the scoped <c>EnterMethodScope</c> model lands with the OO conversion, batch 10s).
/// Collaborator handles register here as their batches land — exactly ONE instance each, because per-verb
/// counters/memos are per-unit lifetime.
/// </summary>
internal sealed class BinderContext(DataBinder data, ReferenceResolver refs)
{
    /// <summary>The unit's bound data model (files/reports/alphabets/options/collected facts).</summary>
    public DataBinder Data => data;

    /// <summary>The unit's reference resolver (dataReference → <see cref="Place"/>).</summary>
    public ReferenceResolver Refs => refs;

    /// <summary>The ONE diagnostic sink AND edition surface (Error/Removed/Warning · DialectLevel ·
    /// CheckDigitCapacity) — every verbatim-moved gate, lifted check, and diagnostic flows through this
    /// single instance, preserving emission order.</summary>
    public EditionContext Edition => data.Edition;

    /// <summary>The USER's source line of a parse context's first token (kb/Work PB82) — the line in the file that
    /// physically holds it, through the preprocessing chain's origin map. This is what a bound node's
    /// <c>SourceLine</c> (the X3.23-1985 DEBUG-LINE) is built from; the raw <c>ctx.Start.Line</c> is the RESULTANT
    /// line and stays the anchor for the TURN / FLAG / REF-MOD-ZERO-LENGTH directive folds only.</summary>
    public int SourceLine(Antlr4.Runtime.ParserRuleContext c) => Edition.SourceLineOf(c.Start.Line);

    /// <summary>The ONE scope-aware name resolver (per binder — P6 Exec Step B).</summary>
    public SymbolTable Symbols => data.Symbols;

    /// <summary>The CURRENT resolution scope — recomputed per read (the method overlay changes as the OO
    /// roster binds); never capture this at construction.</summary>
    public Scope ActiveScope => data.ActiveScope;

    /// <summary>Compilation options (arithmetic mode for the composite cap, DEFAULT ROUNDED mode).</summary>
    public OptionsModel Options => data.Options;

    /// <summary>The group's <c>&gt;&gt;COBOL-WORDS</c> override (ISO §7.3.10) — the intrinsic binder resolves a
    /// function-name synonym / removal through it. <c>Empty</c> when there is no directive.</summary>
    public CobolNet.Editions.CobolWordsMap CobolWords => data.CobolWords;

    /// <summary>The post-lex token decisions every fragment re-parse applies (kb/Work PB655).</summary>
    public CobolNet.Frontend.Parsing.TokenRetypes Retypes => data.Retypes;

    /// <summary>The edition-invariant SR check catalog (P7 Step 10 — pure checks only: each reports to
    /// <see cref="Edition"/> and returns the verdict; the verb binder owns all error+placeholder control
    /// flow).</summary>
    public StatementValidation Validation { get; } = new(data);

    /// <summary>The per-unit SPECIAL-NAMES mnemonic registry (10h — ACCEPT-FROM and the WRITE SR13 /
    /// ADVANCING zero-advance legs share the ONE lazily built map).</summary>
    public MnemonicRegistry Mnemonics { get; } = new();

    /// <summary>The EC bind state (10r — the TurnState + PD-RAISING sets + USE-F3 cross-USE pairs + the seven
    /// EcFeatures accumulator bits), shared by <c>EcBinder</c> and the Declaratives half.</summary>
    public EcBindState EcState { get; } = new();

    /// <summary>The out-of-line half of ISO §14.9.28.3 SR8 (kb/Work PB434): the UNTIL EXIT statements and the
    /// VARYING / TEST PERFORM ranges recorded while the unit binds, answered once its procedures are bound.</summary>
    public UntilExitPlacement UntilExit { get; } = new();

    /// <summary>The pc whose sentences are being bound (RESUME SR1/SR2 declarative context + the §15.30.3 r2
    /// location anchoring; 10r — the host's <c>_currentBindPc</c> relocated). −1 outside the bind loop.</summary>
    public int BindCursor { get; set; } = -1;

    /// <summary>The section whose paragraph is being bound (ISO §8.4.2.2 — unqualified procedure names
    /// resolve in-section first; 10s — the host's <c>_currentSection</c> relocated). Set through
    /// <see cref="EnterMethodScope"/> during the bind loops; the SetAlter prepass saves/restores it.</summary>
    public SectionInfo? CurrentSection { get; set; }

    /// <summary>The OWNING method's paragraph/data scope while its statements bind (ISO §11.7 — method-local
    /// procedure names; 10s — the host's <c>_currentMethodScope</c> relocated). Also the COLLECTION cursor
    /// while a method's paragraphs register. Null outside a method.</summary>
    public OoMethodScope? CurrentMethodScope { get; set; }

    /// <summary>The per-unit procedure table (paragraphs ∥ sections ∥ method scopes ∥ declaratives — ONE
    /// pc space) and its §8.4.2.2 resolver (10t — table ownership relocated per the AS-BUILT plan).</summary>
    private ProcedureTableBuilder? _table;
    public ProcedureTableBuilder Table => _table ??= new(this);

    // ── The placement-rule context (kb/Work PB403): ONE "where am I bound?" probe, read by every syntax rule
    //    that says a statement "may be specified only in …". See EnclosingContext for the rule inventory. ──

    /// <summary>The kind of source element whose procedure division this binder binds (ISO §14.2.2 SR10) — set
    /// once per unit by <c>BinderDriver.BindUnitProcedure</c> from the unit's own facts, beside the
    /// <c>UdfSelfName</c> it is derived from, so the two cannot disagree. A METHOD body overrides it through
    /// <see cref="CurrentMethodScope"/> (see <see cref="SourceElement"/>), because a method's statements bind on
    /// a CLASS unit's binder.</summary>
    public SourceElementKind UnitKind { get; set; } = SourceElementKind.Program;

    /// <summary>The source element kind in force AT THE BIND CURSOR: a method body wherever a method scope is
    /// entered, and otherwise the unit's own kind.</summary>
    public SourceElementKind SourceElement =>
        CurrentMethodScope is not null ? SourceElementKind.MethodDefinition : UnitKind;

    /// <summary>The lexical enclosing-construct stack (innermost last) — pushed by <see cref="EnterConstruct"/>.
    /// A field rather than a local because the constructs nest across binder collaborators (an inline PERFORM in
    /// <c>ControlFlowBinder</c>, a WHEN phrase in <c>EcBinder</c>) and every verb must see the same stack.</summary>
    private readonly List<EnclosingFrame> _constructs = [];

    /// <summary>⛔ THE PLACEMENT-RULE PROBE. Recomputed per read from the live bind position, never cached.</summary>
    public EnclosingContext Enclosing => new(SourceElement, _constructs, DeclarativeAtCursor());

    /// <summary>The declarative section containing <see cref="BindCursor"/>, or null — ONE lookup, so a second
    /// placement rule cannot acquire a second, differently-guarded copy of it (it had exactly one asker, RESUME,
    /// and three rules that needed it and did not ask). Each declarative section is a contiguous pc run
    /// (<see cref="BoundDeclarative.Contains"/>), so the scan needs no "below EntryPc" bound — which a CLASS unit
    /// could not supply, since each METHOD's declaratives sit below that method's own entry (kb/Work PB1010).</summary>
    private BoundDeclarative? DeclarativeAtCursor()
    {
        if (_table is null || BindCursor < 0) return null;
        var declaratives = _table.Declaratives;
        for (int i = 0; i < declaratives.Count; i++)
            if (declaratives[i].Contains(BindCursor)) return declaratives[i];
        return null;
    }

    /// <summary>Push one lexical construct onto <see cref="Enclosing"/>'s stack for the extent of the returned
    /// token — <c>using var _ = ctx.EnterConstruct(EnclosingConstruct.InlinePerform);</c> around the bind of the
    /// construct's statement block.</summary>
    public ConstructScope EnterConstruct(EnclosingConstruct construct, bool specifiesVaryingOrTest = false)
    {
        _constructs.Add(new EnclosingFrame(construct, specifiesVaryingOrTest));
        return new ConstructScope(this);
    }

    /// <summary>The restore token of <see cref="EnterConstruct"/> — disposing pops exactly the frame it
    /// pushed.</summary>
    public readonly struct ConstructScope(BinderContext ctx) : IDisposable
    {
        public void Dispose() => ctx._constructs.RemoveAt(ctx._constructs.Count - 1);
    }

    /// <summary>Enter a per-pc bind position as ONE scoped operation (10s — replaces the ambient ordered
    /// quadruple mutation): section → method scope → the §11.7 GR5 data shadowing
    /// (<c>Data.ActiveMethodScope</c>) → cursor, set coherently and restored together on dispose.</summary>
    public BindPositionScope EnterMethodScope(SectionInfo? section, OoMethodScope? scope, int pc)
    {
        var token = new BindPositionScope(this, CurrentSection, CurrentMethodScope, Data.ActiveMethodScope, BindCursor);
        CurrentSection = section;
        CurrentMethodScope = scope;
        Data.ActiveMethodScope = scope?.Data;
        BindCursor = pc;
        return token;
    }

    /// <summary>The restore token of <see cref="EnterMethodScope"/> — disposing re-establishes the PRIOR
    /// quadruple (so sequential per-iteration scopes leave the pre-loop state at loop exit).</summary>
    public readonly struct BindPositionScope(
        BinderContext ctx, SectionInfo? section, OoMethodScope? scope, OoMethodDataScope? dataScope, int pc) : IDisposable
    {
        public void Dispose()
        {
            ctx.CurrentSection = section;
            ctx.CurrentMethodScope = scope;
            ctx.Data.ActiveMethodScope = dataScope;
            ctx.BindCursor = pc;
        }
    }
}
