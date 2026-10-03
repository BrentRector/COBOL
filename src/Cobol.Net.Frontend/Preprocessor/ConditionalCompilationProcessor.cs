// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using CobolNet.Runtime;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// COBOL conditional compilation (ISO/IEC 1989:2023 §7.3.11 DEFINE, §7.3.16 IF, §7.3.13 EVALUATE) — a
/// text-manipulation stage (§7.2) that selectively includes source lines based on compilation variables. It runs on
/// free-form normalized text, before COPY expansion, so an <c>&gt;&gt;IF</c> may gate COPY statements in its
/// branches.
///
/// Two cleanly separated jobs (DESIGN-compile-time-expressions.md §2): (1) LINE SELECTION — walking the
/// <c>&gt;&gt;IF/&gt;&gt;ELSE/&gt;&gt;END-IF/&gt;&gt;EVALUATE/&gt;&gt;WHEN</c> nesting to decide which physical lines
/// survive; this stays a small line-inclusion state machine here because "text-1/text-2" may be any source lines,
/// including un-expanded COPY and (in omitted branches) non-COBOL, so it MUST precede the main parse. (2) EXPRESSION /
/// CONDITION EVALUATION — every <c>&gt;&gt;DEFINE</c> operand, <c>&gt;&gt;IF</c> cce, and <c>&gt;&gt;EVALUATE</c>/
/// <c>&gt;&gt;WHEN</c> operand is fragment-parsed by ANTLR (<see cref="DirectiveExpressionFragment"/>) and evaluated
/// by the ONE shared <see cref="CompileTimeExpressionEvaluator"/> — there is no hand-rolled tokenizer or condition
/// parser; the ANTLR grammar is the single source of truth for directive-expression syntax (§7.3.6 arithmetic,
/// §7.3.7 boolean, §7.3.8 constant-conditional-expression). A formation violation is a loud <b>COBOLNET1619</b>,
/// never a silently mis-bound value.
///
/// A THIRD job it owns for the whole §7.3 family, because this is the one place that sees every directive line:
/// (3) THE EDITION GATE. §7.3.2 gives ONE general format for every compiler directive —
/// <c>&gt;&gt;compiler-instruction</c> — and §7.3.3 SR6 opens compiler-instruction with a compiler-directive word
/// (§8.12), so "may this word head a <c>&gt;&gt;</c> line at the targeted edition" is one question asked once,
/// here, against <see cref="CobolNet.Editions.CompilerDirectiveCatalog"/> (the <c>directiveWords</c> column of
/// <c>constructs.json</c>, inverted). It answers for the consumed directives, the ones a downstream stage owns,
/// and the conditional-compilation directives alike. kb/Work PB725: the roster used to be a flat name set with no
/// edition column, so eleven directives compiled clean at <c>--std 85</c>, an edition with no compiler directives
/// at all, while the five with their own per-stage gate were correctly rejected. <c>&gt;&gt;SOURCE FORMAT</c> is
/// the sole exception and gates in <see cref="ReferenceFormatProcessor"/>, which consumes its line before this
/// driver runs — same row, same COBOLNET0900 producer, one stage earlier.
///
/// A FOURTH job, the one thing a directive does here that is not a change to the text or to a table: (4) THE DISPLAY
/// DIRECTIVE (§7.3.12, kb/Work PB807 / PB1538 — <c>ConditionalCompilationProcessor.Display.cs</c>). Its operands are read
/// by the one directive-expression grammar and evaluated by the shared evaluator like every other directive's, and what it
/// transfers is the compilation's compile-time output (<see cref="DiagnosticBag.CompileOutput"/>), not a diagnostic.
///
/// Blast radius is essentially nil: a source with no <c>&gt;&gt;</c> lines is reproduced byte-for-byte.
/// </summary>
public static partial class ConditionalCompilationProcessor
{
    /// <param name="text">The free-form-normalized source text.</param>
    /// <param name="leaveDirectives">The ISO §7.3 directive keywords whose emitting-branch lines are LEFT IN the
    /// text for a downstream dedicated stage (the WiseOwl COBOL pipeline: TURN, PROPAGATE, REF-MOD-ZERO-LENGTH,
    /// FLAG-02/FLAG-14, COBOL-WORDS, LEAP-SECOND — <c>Frontend.LeftDirectives</c>); an omitted-branch line still
    /// drops with its branch. Null/empty (the legacy caller) consumes every recognized directive here. ONE set,
    /// not one bool per directive (kb/Work PB65 — the sixth flag was the shape's own reproach).</param>
    public static string Process(string text, IReadOnlySet<string>? leaveDirectives = null,
        DiagnosticBag? diagnostics = null, string? sourcePath = null, int dialectLevel = 2023,
        bool permissive = false)
        => new Run(leaveDirectives, diagnostics, sourcePath, copy: null,
                dialectLevel, permissive, inputs: null, implicitOps: [])
            .Render(text);

    /// <summary>
    /// The MERGED text-manipulation driver (ISO §7.2.1) — conditional compilation INTERLEAVED with COPY expansion
    /// so directives INSIDE copybooks are processed. On each emitting-branch region the driver expands its COPY
    /// statements (via <paramref name="copyProcessor"/>) and feeds each incorporated copybook back through the SAME
    /// driver (shared DEFINE / IF-EVALUATE / FlagScan state across the copybook boundary); an omitted-branch COPY is
    /// never expanded (so a false-path missing copybook raises no error). REPLACE (Step 3) is applied over the fully
    /// expanded text. Greenfield-only — the legacy pipeline keeps the separate <see cref="Process"/> + COPY calls,
    /// byte-identical. Design SSOT: <c>docs/rearchitecture/DESIGN-cc-in-copy.md</c>.
    /// </summary>
    public static string ProcessWithCopy(string text, CopyProcessor copyProcessor,
        IReadOnlySet<string>? leaveDirectives, DiagnosticBag? diagnostics, string? sourcePath, int dialectLevel,
        bool permissive = false)
        => ProcessWithCopyMapped(MappedText.Identity(text, sourcePath ?? "<source>"), copyProcessor,
            leaveDirectives, diagnostics, sourcePath, dialectLevel, permissive).Text;

    /// <summary>The MAPPED driver (kb/Work PB82): the same interleaved CC + COPY + REPLACE manipulation over a text
    /// that carries its per-line origins, returning the resultant text with ITS origins — main-source lines keep
    /// their physical line, copied lines carry the copybook's path and line, so every downstream position (the
    /// parser's, the binder's, EXCEPTION-LOCATION's) can name what the user edits.</summary>
    public static MappedText ProcessWithCopyMapped(MappedText text, CopyProcessor copyProcessor,
        IReadOnlySet<string>? leaveDirectives, DiagnosticBag? diagnostics, string? sourcePath, int dialectLevel,
        bool permissive = false, CompilationInputs? inputs = null)
        => Manipulate(text, copyProcessor, leaveDirectives, diagnostics, sourcePath, dialectLevel,
            permissive, inputs, implicitOps: []).Text;

    /// <summary>The mapped driver with its DIRECTIVE ENCOUNTERS (kb/Work PB1066): the resultant text, plus — for
    /// every <c>&gt;&gt;</c> line the driver met, in encounter order — the resultant line it ended on and whether it
    /// changed the state this driver holds. <paramref name="implicitOps"/> is the §14.9.28.4 GR14 implicit PUSH ALL /
    /// POP ALL program a PREVIOUS run's parse placed (<see cref="ConditionalCompilationResult.KeyImplicitOps"/>),
    /// each applied immediately before the directive encounter it is keyed to; empty on the first run.</summary>
    public static ConditionalCompilationResult Manipulate(MappedText text, CopyProcessor copyProcessor,
        IReadOnlySet<string>? leaveDirectives, DiagnosticBag? diagnostics, string? sourcePath, int dialectLevel,
        bool permissive, CompilationInputs? inputs, IReadOnlyList<KeyedDirectiveOp> implicitOps)
    {
        var run = new Run(leaveDirectives, diagnostics, sourcePath, copyProcessor,
            dialectLevel, permissive, inputs, implicitOps);
        var expanded = run.Render(text);
        var resultant = CopyProcessor.ApplyReplaceStatements(expanded, diagnostics, EditionInfo.Of(dialectLevel, permissive));   // Step 3 — REPLACE over the expanded compilation group
        if (run.Encounters.Count == 0) return new ConditionalCompilationResult(resultant, [], []);
        // Each encounter's line in the driver's OUTPUT frame, carried through REPLACE (which may drop and join lines)
        // to the RESULTANT frame the parser's tokens use — and each compilation-variable event's, which is an encounter's.
        int[] resultantLine = CopyProcessor.ReplaceLineMap(expanded.Text);
        var encounters = run.Encounters
            .Select(e => new DirectiveEncounter(resultantLine[e.Line] + 1, e.ChangesState)).ToArray();
        var compilationVariables = run.CompilationVariables
            .Select(e => e with { Line = resultantLine[e.Line] + 1 }).ToArray();
        return new ConditionalCompilationResult(resultant, encounters, compilationVariables);
    }

    /// <summary>
    /// ONE run of the conditional-compilation state machine (ISO §7.3.11/§7.3.16 line selection), optionally
    /// INTERLEAVED with COPY expansion (§7.2.1). All directive state — the <c>defines</c> map, the
    /// <c>&gt;&gt;IF</c>/<c>&gt;&gt;EVALUATE</c> frame stack, the <see cref="FlagScanState"/>, the shared evaluator —
    /// lives here so it is threaded through the recursive COPY expansion (a copybook <c>&gt;&gt;DEFINE</c> is visible
    /// to following source, per Step-2 encounter order). <see cref="Render"/> uses a LOCAL output/block buffer per
    /// call so recursion into a copybook does not clobber the caller's output while sharing this directive state.
    /// </summary>
    private sealed class Run
    {
        private readonly IReadOnlySet<string> _leave;
        private readonly Dictionary<string, CtValue> _defines = new(StringComparer.OrdinalIgnoreCase);
        private readonly FlagScanState _flagScan = new();
        // §7.3.20 / §7.3.22 (kb/Work PB941): THIS stage's share of the directive state a PUSH saves and a POP
        // restores — the compilation-variable table (every instance at once, §7.3.22.4 GR3) and the running FLAG
        // options — carried by the ONE DirectiveStateStack mechanism, fed in encounter order so a copybook's
        // PUSH/POP act where they are met. The unsuccessful-POP warning is not issued here: the post-COPY
        // DirectiveSiteProcessor sees every PUSH/POP of the final text and issues it once.
        private readonly DirectiveStateStack _directiveState = new();
        private readonly DirectiveDiag _diag;
        private readonly CompileTimeExpressionEvaluator _evaluator;
        private readonly Stack<Frame> _stack = new();
        // kb/Work PB1363 — the library-text identity of the text Render is now walking (1 = the source text, then one
        // fresh id per incorporated copybook), which every frame records when it opens.
        private int _textCounter;
        private int _currentText;
        // COPY interleave context (null = pure CC, the legacy shape): the copybook engine + the per-group include
        // set + the current nesting depth (threaded through the recursion for the SR1 circular / depth-20 guards).
        private readonly CopyProcessor? _copy;
        private readonly HashSet<string> _alreadyIncluded = new(StringComparer.OrdinalIgnoreCase);
        private int _depth;
        // kb/Work PB1066 — the §14.9.28.4 GR14 implicit-op program, keyed to directive encounters, and the
        // encounters themselves (Line = 0-based line in THIS run's output frame). _renderBase is the output-frame line
        // at which the Render now running begins (0 for the source; a copybook's is its splice point), and
        // _flushBase the output-frame line at which the block now being COPY-expanded begins.
        private readonly IReadOnlyList<KeyedDirectiveOp> _implicitOps;
        private int _nextImplicitOp;
        private readonly List<DirectiveEncounter> _encounters = [];
        // kb/Work PB1368 — every change a DEFINE made to `_defines`, in encounter order (Line = 0-based line in THIS
        // run's output frame, like an encounter's): the compilation-variable TIMELINE the later stages read.
        private readonly List<CompilationVariableEvent> _compilationVariables = [];
        private int _renderBase;
        private int _flushBase;

        /// <summary>Every <c>&gt;&gt;</c> line met, in encounter order; <see cref="DirectiveEncounter.Line"/> is the
        /// 0-based line of this run's OUTPUT frame.</summary>
        public IReadOnlyList<DirectiveEncounter> Encounters => _encounters;

        /// <summary>Every change a DEFINE made to the compilation-variable table, in encounter order;
        /// <see cref="CompilationVariableEvent.Line"/> is the 0-based line of this run's OUTPUT frame.</summary>
        public IReadOnlyList<CompilationVariableEvent> CompilationVariables => _compilationVariables;

        // The targeted edition. TWO rules read it: the §8.3.2.1 word-length ceiling for >>DEFINE names (a
        // compilation-variable-name never reaches the tree-walk funnel, so this stage enforces it itself —
        // CobolWordRule, kb/Work R05's sweep) and THE compiler-directive introduction/removal gate below
        // (kb/Work PB725). The severity axis rides along because a REMOVED directive is an error strict /
        // a warning permissive (EditionSeverityPolicy), while an unintroduced one is an error on both.
        private readonly int _dialectLevel;
        private readonly EditionInfo _edition;
        private readonly DiagnosticBag? _bag;
        // The compilation's ambient-input gateway (kb/Work PB985) — a >>DEFINE … PARAMETER value is read from the
        // environment THROUGH it, so the variable and its value are part of the compilation's recorded inputs.
        private readonly CompilationInputs _inputs;

        public Run(IReadOnlySet<string>? leaveDirectives,
            DiagnosticBag? diagnostics, string? sourcePath, CopyProcessor? copy,
            int dialectLevel, bool permissive, CompilationInputs? inputs, IReadOnlyList<KeyedDirectiveOp> implicitOps)
        {
            _implicitOps = implicitOps;
            _inputs = inputs ?? new CompilationInputs();
            _leave = leaveDirectives ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _dialectLevel = dialectLevel;
            _edition = EditionInfo.Of(dialectLevel, permissive);
            _bag = diagnostics;
            _diag = new DirectiveDiag(diagnostics, sourcePath, _flagScan, _edition);
            // The ONE shared compile-time expression evaluator (ledger C2). Name resolution reads the CURRENT
            // `_defines` (a directive may reference a variable an earlier directive — or a copybook — set); the
            // frontend routes every formation diagnostic to COBOLNET1619; a directive operand is dot-decimal (§5.3).
            _evaluator = new CompileTimeExpressionEvaluator(
                edition: _edition,
                resolveName: w => _defines.TryGetValue(w, out var v) ? v : null,
                diag: _diag,
                vocab: new CtOperandVocabulary("previously defined numeric compilation variables", "ISO §7.3.6.2 SR1b"),
                decimalPointIsComma: false);
            _copy = copy;
            _directiveState
                .Carry(Constructs.DefineDirective2002, new DirectiveValueCarrier<Dictionary<string, CtValue>>(
                    () => new Dictionary<string, CtValue>(_defines, StringComparer.OrdinalIgnoreCase),
                    saved => { _defines.Clear(); foreach (var (k, v) in saved) _defines[k] = v; }))
                .Carry(Constructs.Flag02Directive2014, _flagScan.CarrierFor(FlagDirective.Flag02))
                .Carry(Constructs.Flag14Directive2023, _flagScan.CarrierFor(FlagDirective.Flag14));
        }

        /// <summary>Process <paramref name="text"/> (source or copybook) into the manipulated text, sharing this
        /// run's directive state. Consecutive emitting non-directive lines are accumulated into a block and flushed
        /// (COPY-expanded when interleaving) at each directive/omitted-line boundary — a COPY statement always lies
        /// wholly within one emitting block, so multi-line COPY REPLACING and mid-line COPY are handled by the
        /// copybook engine's char scan.</summary>
        public string Render(string text) => Render(MappedText.Identity(text, _diag.SourcePath ?? "<source>")).Text;

        /// <summary>The MAPPED render (kb/Work PB82): every output line carries the origin of the input line it came
        /// from — an omitted or directive line its own, a block its lines', a copybook expansion the copybook's.</summary>
        public MappedText Render(MappedText input)
        {
            // §7.3.16.3 SR7 / §7.3.13.3 SR9 (kb/Work PB1363): every text this driver renders — the source text and
            // each incorporated copybook — is ONE library text, and the directives OPENED in it shall be closed in it.
            int textId = ++_textCounter;
            int outerText = _currentText;
            _currentText = textId;
            var lines = input.Text.Split('\n');
            var output = new List<string>(lines.Length);
            var outputOrigins = new List<SourceOrigin>(lines.Length);
            var block = new List<string>();
            var blockOrigins = new List<SourceOrigin>();
            string openStatementText = "";   // the text of a COPY / REPLACE statement still open at a directive line (§7.3.3 SR8 b)

            void Flush()
            {
                if (block.Count == 0) return;
                var blockText = new MappedText(string.Join('\n', block), blockOrigins.ToArray());
                block.Clear();
                blockOrigins.Clear();
                _flushBase = _renderBase + output.Count;
                MappedText expanded = _copy is null
                    ? blockText
                    : _copy.ExpandCopiesOneLevel(blockText, _alreadyIncluded, _depth, RenderCopybook);
                output.AddRange(expanded.Text.Split('\n'));
                outputOrigins.AddRange(expanded.Lines);
            }

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                SourceOrigin origin = input.Lines[i];
                string trimmed = line.TrimSpacesStart();
                _diag.At = origin;

                if (!trimmed.StartsWith(">>", StringComparison.Ordinal))
                {
                    // §7.3.13.2: `>> EVALUATE` is followed directly by its first `>> WHEN` — text-1 belongs to a WHEN phrase,
                    // so program text between the two belongs to no phrase of the format (a blank line is no text).
                    if (trimmed.Length > 0 && _stack.Count > 0 && _stack.Peek() is { Phase: FramePhase.EvaluateBeforeWhen } before
                        && !before.TextBeforeWhenReported)
                    {
                        before.TextBeforeWhenReported = true;
                        _diag.Structure($"text follows the >>EVALUATE opened at {DescribeStart(before)} before its first >>WHEN — "
                            + "text-1 belongs to a >>WHEN phrase and the general format writes no text between "
                            + ">>EVALUATE and its first >>WHEN (ISO §7.3.13.2)");
                    }
                    if (_stack.Count == 0 || _stack.Peek().Emitting) { block.Add(line); blockOrigins.Add(origin); }   // accumulate; COPY expands at Flush
                    else { Flush(); output.Add(""); outputOrigins.Add(origin); }                                     // omitted ordinary line
                    continue;
                }

                // §7.3.3 SR8 b) (kb/Work PB1384): a compiler directive is not specified "within a source text manipulation
                // statement". The block flushed below ends at this line, so a COPY or REPLACE statement still open at the
                // end of it (no separator period yet) has this directive inside it. The question is asked of the text
                // since the statement began — carried across directive lines, so a statement split by several is
                // reported at each — and the directive then takes effect like any other (superset-continue).
                openStatementText = string.Concat(openStatementText, openStatementText.Length > 0 ? "\n" : "", string.Join('\n', block));
                if (CopyProcessor.OpenStatementAt(openStatementText) is { } open)
                {
                    _diag.WithinStatement(open.Keyword);
                    openStatementText = openStatementText[open.Start..];
                }
                else openStatementText = "";

                // A directive ends the current emitting block (a COPY can never span a directive line). Flush FIRST
                // so the block's copybooks' own directives (a copybook >>DEFINE / >>IF) have already run — the
                // following directive's condition + emitting state see them (ISO §7.2.1 Step-2 encounter order).
                Flush();
                _diag.At = origin;   // Flush may have re-anchored _diag.At inside a copybook; restore for this line
                // §14.9.28.4 GR14 (kb/Work PB1066): an implicit PUSH ALL / POP ALL a previous parse placed before THIS
                // directive acts on the state here first, exactly as a written >>PUSH ALL / >>POP ALL would.
                int encounter = _encounters.Count;
                while (_nextImplicitOp < _implicitOps.Count && _implicitOps[_nextImplicitOp].BeforeDirective <= encounter)
                    _directiveState.Apply(_implicitOps[_nextImplicitOp++].Op);
                bool changesState = false;
                bool emitting = _stack.Count == 0 || _stack.Peek().Emitting;
                var (keyword, rest) = SplitDirective(trimmed);
                // ── THE edition gate for EVERY compiler directive (kb/Work PB725) ────────────────────────
                // ISO §7.3.2 gives ONE general format for all of them — `>>compiler-instruction` — and
                // §7.3.3 SR6 opens compiler-instruction with a compiler-directive word (§8.12). So "may this
                // word head a >> line at the targeted edition" is one question with one answer per directive,
                // asked HERE, once, for the whole family: the recognized-and-consumed directives, the ones a
                // downstream stage owns (_leave), and the conditional-compilation directives handled by the
                // switch below alike. It routes through the ONE ConstructRegistry funnel, so the introduction
                // edge is COBOLNET0900, a removal COBOLNET0902 and an obsolete use COBOLNET0903, with the
                // §4.2 severity decided by the ONE EditionSeverityPolicy — never a local `if (dialect < N)`.
                // A directive in an OMITTED branch is not compiled, so it is not gated (it drops with its
                // branch, like every other omitted line).
                if (emitting && _bag is not null)
                {
                    var sink = new BagSink(_bag, _diag.At.ToLocation());
                    CompilerDirectiveCatalog.Check(keyword, _edition, sink);
                    // ── AND THE OPERAND, from the same row (kb/Work PB794) ───────────────────────────────
                    // §7.3.3 SR6 composes compiler-instruction "as specified in the syntax of each directive",
                    // so "may this word head a >> line" and "may these words follow it" are two questions with
                    // one answer each per directive, asked at the same point. Before PB794 the second was asked
                    // by six stages in six spellings with six codes — and not at all for the seven directives
                    // this stage consumes, so >>SOURCE FORMAT UNKNOWN, >>LISTING GARBAGE and >>PUSH GARBAGE
                    // compiled clean. The closed-word-set rows answer here, through the ONE COBOLNET1911
                    // producer; a row whose operand a downstream stage parses is a declared no-op.
                    CompilerDirectiveCatalog.CheckOperand(keyword, rest, _edition, sink);
                }
                else if (_bag is not null && keyword is "ELSE" or "END-IF" or "END-EVALUATE")
                {
                    // The phrase directives of an IF / EVALUATE in an omitted branch are not COMPILED, but they are still
                    // the frame stack's structure, and their format writes no operand (kb/Work PB806; §7.3.3 SR3/SR4):
                    // `>>ELSE JUNK` is as malformed inside an omitted branch as outside one.
                    CompilerDirectiveCatalog.CheckOperand(keyword, rest, _edition, new BagSink(_bag, _diag.At.ToLocation()));
                }
                string emit = "";   // directives are consumed by default (output blank line)
                switch (keyword)
                {
                    case "IF":
                    {
                        bool parentActive = _stack.Count == 0 || _stack.Peek().Emitting;
                        bool cond = parentActive && EvaluateCceText(rest, _evaluator, _diag, ">>IF");
                        _stack.Push(new Frame { Kind = FrameKind.If, Phase = FramePhase.IfThen, TextId = _currentText,
                            ParentActive = parentActive, Emitting = cond, BranchTaken = cond, Start = origin });
                        break;
                    }
                    case "ELSE":
                        if (PhraseFrame(FrameKind.If, "ELSE", origin) is { } ifFrame)
                        {
                            if (ifFrame.Phase == FramePhase.IfElse)
                                _diag.Structure($">>ELSE: the >>IF opened at {DescribeStart(ifFrame)} already has its >>ELSE — "
                                    + "the general format writes at most one (ISO §7.3.16.2)");
                            else
                            {
                                ifFrame.Phase = FramePhase.IfElse;
                                ifFrame.Emitting = ifFrame.ParentActive && !ifFrame.BranchTaken;   // the ELSE body emits only if no prior branch did
                            }
                        }
                        break;
                    case "END-IF":
                        if (PhraseFrame(FrameKind.If, "END-IF", origin) is not null) _stack.Pop();
                        break;
                    case "EVALUATE":
                    {
                        // Format 1: >>EVALUATE selection-subject   Format 2: >>EVALUATE TRUE
                        bool parentActive = _stack.Count == 0 || _stack.Peek().Emitting;
                        var f = new Frame { Kind = FrameKind.Evaluate, Phase = FramePhase.EvaluateBeforeWhen, TextId = _currentText,
                            ParentActive = parentActive, Emitting = false, BranchTaken = false,
                            Start = origin, EvaluateFlagOn = _flagScan.IsOn(FlagOption.Flag14Evaluate) };   // c anchor (§7.3.15.4 GR4 c)
                        string subj = rest.TrimSpaces();
                        if (subj.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) f.TruthForm = true;
                        else if (parentActive) f.Subject = EvaluateOperandText(subj, _evaluator, _diag, ">>EVALUATE");
                        _stack.Push(f);
                        break;
                    }
                    case "WHEN":
                        if (PhraseFrame(FrameKind.Evaluate, "WHEN", origin) is { } evalFrame)
                        {
                            string obj = rest.TrimSpaces();
                            // §7.3.13.3 SR5/SR6: `>>WHEN OTHER` is specified entirely on its line — OTHER and nothing else.
                            bool isOther = StartsWithWord(obj, "OTHER");
                            if (isOther)
                            {
                                if (obj.Length > "OTHER".Length)
                                    _diag.Structure($">>WHEN OTHER: '{obj["OTHER".Length..].TrimSpaces()}' follows OTHER on the directive line — "
                                        + ">>WHEN OTHER shall be specified entirely on its line and text-2 shall begin on a new line "
                                        + "(ISO §7.3.13.3 SR5, SR6)");
                                if (evalFrame.Phase == FramePhase.EvaluateOther)
                                    _diag.Structure($">>WHEN OTHER: the >>EVALUATE opened at {DescribeStart(evalFrame)} already has its "
                                        + ">>WHEN OTHER — the general format writes at most one, after every >>WHEN (ISO §7.3.13.2)");
                            }
                            else if (evalFrame.Phase == FramePhase.EvaluateOther)
                                _diag.Structure($">>WHEN: follows the >>WHEN OTHER of the >>EVALUATE opened at {DescribeStart(evalFrame)} — "
                                    + ">>WHEN OTHER is the last phrase before >>END-EVALUATE (ISO §7.3.13.2)");
                            // c EVALUATE: record the syntactic presence of a >>WHEN / >>WHEN OTHER (independent of which
                            // branch emits) — GR4 c flags a directive containing BOTH.
                            if (isOther) evalFrame.SawWhenOther = true; else evalFrame.SawWhen = true;
                            if (isOther)
                            {
                                evalFrame.Phase = FramePhase.EvaluateOther;
                                evalFrame.Emitting = evalFrame.ParentActive && !evalFrame.BranchTaken;     // OTHER fires only if nothing matched
                                evalFrame.BranchTaken |= evalFrame.Emitting;                               // a mis-ordered later WHEN cannot re-select
                            }
                            else
                            {
                                if (evalFrame.Phase == FramePhase.EvaluateBeforeWhen) evalFrame.Phase = FramePhase.EvaluateWhen;
                                // §7.3.13.3 SR3/SR11/SR12/SR14-16 are properties of EVERY >>WHEN of the directive, not of the
                                // branch §7.3.13.4 GR4 selects (kb/Work PB1364): the operand is parsed and category-checked
                                // whenever the directive itself is being compiled; only the EMIT decision is gated on an earlier match.
                                bool match = evalFrame.ParentActive
                                    && (evalFrame.TruthForm
                                        ? EvaluateCceText(obj, _evaluator, _diag, ">>WHEN")               // Format 2: constant-conditional-expression
                                        : MatchWhen(evalFrame.Subject, obj, _evaluator, _diag));          // Format 1: subject = object [THRU object3]
                                evalFrame.Emitting = match && !evalFrame.BranchTaken;
                                if (evalFrame.Emitting) evalFrame.BranchTaken = true;
                            }
                        }
                        break;
                    case "END-EVALUATE":
                        if (PhraseFrame(FrameKind.Evaluate, "END-EVALUATE", origin) is { } endFrame)
                        {
                            if (endFrame.Phase == FramePhase.EvaluateBeforeWhen)
                                _diag.Structure($">>END-EVALUATE: the >>EVALUATE opened at {DescribeStart(endFrame)} has no >>WHEN — "
                                    + "the general format writes one or more (ISO §7.3.13.2)");
                            // c EVALUATE (§7.3.15.4 GR4 c; E.2 item 8) — flag the directive when it carried both a >>WHEN
                            // and a >>WHEN OTHER and FLAG-14 EVALUATE was ON at the >>EVALUATE line.
                            if (endFrame.SawWhen && endFrame.SawWhenOther && endFrame.EvaluateFlagOn)
                                _diag.FlagWarn(FlagOption.Flag14Evaluate, endFrame.Start);
                            _stack.Pop();
                        }
                        break;
                    case "DEFINE":
                        // A DEFINE in an omitted branch has no effect. One that changed the table is an EVENT of the
                        // compilation-variable timeline, recorded at this directive's output-frame line (the encounter's,
                        // below) so the CONSTANT FROM and directive-literal uses can read the table as of their own line
                        // (§7.3.11.4 GR1, kb/Work PB1368).
                        if (emitting && ApplyDefine(rest, _defines, _evaluator, _diag, _dialectLevel, _inputs) is var (defined, written))
                            _compilationVariables.Add(new CompilationVariableEvent(_renderBase + output.Count, defined,
                                _defines.GetValueOrDefault(defined), written));
                        changesState = emitting;
                        break;
                    case "DISPLAY":
                        // §7.3.12.4 GR1: the operands are transferred when the directive is processed — in an emitting branch
                        // only, like every directive (an omitted branch is not compiled). The line itself is consumed.
                        if (emitting) ApplyDisplay(rest, _evaluator, _diag, _inputs, _bag);
                        break;
                    default:
                        // A >> directive other than the conditional-compilation set handled above. Its edition
                        // gate already fired above; what is left here is the DISPOSITION of the line.
                        // A PUSH/POP acts on this stage's share of the directive state where it is met — in an
                        // emitting branch only, like every other directive (kb/Work PB941) — and its line then
                        // takes the ordinary disposition below (left for DirectiveSiteProcessor).
                        if (emitting && DirectiveStackOp.TryParse(line, 0, out var stackOp))
                        {
                            _directiveState.Apply(stackOp);
                            changesState = true;
                        }
                        if (!emitting) emit = "";
                        else if (_leave.Contains(keyword))
                        {
                            // A directive a downstream dedicated stage owns: the line SURVIVES for it. FLAG-02/FLAG-14
                            // additionally feed the running FLAG state for the frontend-inline options here (the
                            // post-COPY FlagDirectiveProcessor builds the bound-option FlagState from the same line).
                            if (keyword is "FLAG-02" or "FLAG-14")
                            {
                                var which = keyword == "FLAG-02" ? FlagDirective.Flag02 : FlagDirective.Flag14;
                                if (FlagDirectiveLine.TryParse(which, rest, out var flagOpts, out bool flagOn, out _))
                                {
                                    _flagScan.Apply(which, flagOpts, flagOn);
                                    changesState = true;
                                }
                            }
                            emit = line;
                        }
                        // A recognized ISO §7.3 directive with no downstream stage: CONSUME it (the program
                        // compiles with the default behaviour) — the roster is the constructs.json rows, never a
                        // hand-kept name set (kb/Work PB725). An UNRECOGNIZED >> word is left in place when
                        // emitting so it surfaces downstream (catching typos like >>IFF).
                        else emit = CompilerDirectiveCatalog.IsDirective(keyword) ? "" : line;
                        break;
                }
                _encounters.Add(new DirectiveEncounter(_renderBase + output.Count, changesState));
                output.Add(emit);
                outputOrigins.Add(origin);
            }

            Flush();
            // A directive OPENED in this text and still open at its end is unclosed: the source text is a compilation
            // group's end, a copybook's end is the end of its library text (§7.3.16.3 SR7, §7.3.13.3 SR9). Pop it, so
            // the text that follows the COPY is judged against the frames it opened itself.
            while (_stack.Count > 0 && _stack.Peek().TextId == textId)
            {
                var open = _stack.Pop();
                _diag.At = open.Start;
                _diag.Structure($"the >>{(open.Kind == FrameKind.If ? "IF" : "EVALUATE")} opened at {DescribeStart(open)} is not closed "
                    + $"by >>{(open.Kind == FrameKind.If ? "END-IF" : "END-EVALUATE")} in the same "
                    + (textId == 1 ? "source text" : "library text")
                    + (open.Kind == FrameKind.If ? " (ISO §7.3.16.2, §7.3.16.3 SR7)" : " (ISO §7.3.13.2, §7.3.13.3 SR9)"));
            }
            _currentText = outerText;
            return new MappedText(string.Join('\n', output), outputOrigins.ToArray());
        }

        /// <summary>The frame a phrase directive (<c>&gt;&gt;ELSE</c>, <c>&gt;&gt;END-IF</c>, <c>&gt;&gt;WHEN</c>,
        /// <c>&gt;&gt;END-EVALUATE</c>) belongs to — the top frame when it is of <paramref name="kind"/> — or null after
        /// reporting that there is none (§7.3.16.2 / §7.3.13.2: a phrase follows its own opening directive). A phrase
        /// written in a different library text from its opening directive is reported too, but the frame is still
        /// returned so the stack keeps the nesting the source wrote (§7.3.16.3 SR7, §7.3.13.3 SR9).</summary>
        private Frame? PhraseFrame(FrameKind kind, string word, SourceOrigin at)
        {
            string opener = kind == FrameKind.If ? "IF" : "EVALUATE";
            if (_stack.Count == 0 || _stack.Peek().Kind != kind)
            {
                _diag.Structure($">>{word} has no open >>{opener} directive to belong to"
                    + (_stack.Count > 0 ? $" (the innermost open directive is the >>{(_stack.Peek().Kind == FrameKind.If ? "IF" : "EVALUATE")} "
                        + $"opened at {DescribeStart(_stack.Peek())})" : "")
                    + (kind == FrameKind.If ? " (ISO §7.3.16.2)" : " (ISO §7.3.13.2)"));
                return null;
            }
            var f = _stack.Peek();
            if (f.TextId != _currentText)
                _diag.Structure($">>{word} is written in a different library text from the >>{opener} opened at {DescribeStart(f)} — "
                    + "the phrases of one directive shall all be in the same library text or all in source text"
                    + (kind == FrameKind.If ? " (ISO §7.3.16.3 SR7)" : " (ISO §7.3.13.3 SR9)"));
            return f;
        }

        private static string DescribeStart(Frame f) => $"{f.Start.File} line {f.Start.Line}";

        /// <summary>Expand one incorporated copybook through the SAME driver at <paramref name="depth"/> — its own
        /// directives + nested COPY are processed with this run's shared state; the depth is restored on return so
        /// the SR1/depth guards stay accurate across sibling copybooks. The copybook's text carries ITS origins
        /// (its path and physical lines — kb/Work PB82). <paramref name="lineOffset"/> is the line of the block's
        /// expansion at which the copybook is spliced, so a directive inside it is recorded at its output-frame
        /// line (kb/Work PB1066).</summary>
        private MappedText RenderCopybook(MappedText copybookText, int depth, int lineOffset)
        {
            (int savedDepth, int savedRenderBase, int savedFlushBase) = (_depth, _renderBase, _flushBase);
            _depth = depth;
            _renderBase = _flushBase + lineOffset;   // the copybook's first line in the output frame (kb/Work PB1066)
            var result = Render(copybookText);
            (_depth, _renderBase, _flushBase) = (savedDepth, savedRenderBase, savedFlushBase);
            return result;
        }
    }

    private enum FrameKind { If, Evaluate }

    /// <summary>Where a frame is in its general format (kb/Work PB1363) — §7.3.16.2: <c>IF [ELSE] END-IF</c>;
    /// §7.3.13.2: <c>EVALUATE WHEN… [WHEN OTHER] END-EVALUATE</c>. Every phrase directive is judged against the
    /// phase of the frame it arrives at, so a second <c>&gt;&gt;ELSE</c> or a <c>&gt;&gt;WHEN</c> after
    /// <c>&gt;&gt;WHEN OTHER</c> is a phase that has no arrow out, not a special case.</summary>
    private enum FramePhase { IfThen, IfElse, EvaluateBeforeWhen, EvaluateWhen, EvaluateOther }

    /// <summary>One <c>&gt;&gt;IF…&gt;&gt;END-IF</c> or <c>&gt;&gt;EVALUATE…&gt;&gt;END-EVALUATE</c> nesting level.</summary>
    private sealed class Frame
    {
        public FrameKind Kind;
        public FramePhase Phase;
        public int TextId;          // the library text (source text or one copybook incorporation) the directive was OPENED in — §7.3.16.3 SR7 / §7.3.13.3 SR9
        public bool TextBeforeWhenReported;   // the one "text between EVALUATE and its first WHEN" complaint per directive
        public bool ParentActive;   // is the enclosing context emitting? (a nested directive inside an omitted branch stays omitted)
        public bool Emitting;       // is THIS branch's text currently being included?
        public bool BranchTaken;    // IF: the IF arm was taken (drives ELSE); EVALUATE: some WHEN already matched (drives later WHEN/OTHER)
        public bool TruthForm;      // EVALUATE: true for Format 2 (>>EVALUATE TRUE), where each WHEN carries a constant-conditional-expression
        public CtValue? Subject;    // EVALUATE Format 1: the selection-subject value
        // FLAG-14 c EVALUATE (§7.3.15.4 GR4 c): flag a >>EVALUATE directive that carries BOTH a >>WHEN and a
        // >>WHEN OTHER — syntactic presence (independent of which branch emits). Captured at the >>EVALUATE line.
        public bool SawWhen;
        public bool SawWhenOther;
        public SourceOrigin Start;   // where the >>EVALUATE directive is (its source file and physical line)
        public bool EvaluateFlagOn;   // FLAG-14 EVALUATE was ON at the >>EVALUATE directive's line (the GR2 anchor)
    }

    /// <summary>The running per-option <c>&gt;&gt;FLAG-02</c>/<c>&gt;&gt;FLAG-14</c> ON/OFF state, updated as the CC
    /// stage scans directive lines in order — the frontend-inline options (b COMPILE-TIME-ARITHMETIC-EXPRESSIONS,
    /// c EVALUATE) query it at their construct's line. Mirrors the compile-time <see cref="Binding.FlagState"/> fold
    /// ("last toggle strictly before the site wins, default OFF") but built incrementally in source order, because
    /// these constructs are consumed at THIS stage and never reach the bound tree.</summary>
    private sealed class FlagScanState
    {
        private readonly Dictionary<FlagOption, bool> _on = [];
        public void Apply(FlagDirective which, IReadOnlyList<FlagOption> options, bool on)
        {
            foreach (var opt in options.Count == 0 ? FlagOptions.OptionsOf(which) : options) _on[opt] = on;
        }
        public bool IsOn(FlagOption opt) => _on.TryGetValue(opt, out bool v) && v;

        /// <summary>The PUSH/POP carrier of one directive's options (§7.3.22.4 GR1 / §7.3.20.4 GR1; kb/Work
        /// PB941) — FLAG-02 and FLAG-14 are two directives, saved and restored independently.</summary>
        public IDirectiveStateCarrier CarrierFor(FlagDirective which) =>
            new DirectiveValueCarrier<Dictionary<FlagOption, bool>>(
                () => FlagOptions.OptionsOf(which).Where(_on.ContainsKey).ToDictionary(o => o, o => _on[o]),
                saved =>
                {
                    foreach (var opt in FlagOptions.OptionsOf(which)) _on.Remove(opt);
                    foreach (var (opt, on) in saved) _on[opt] = on;
                });
    }

    // ── DEFINE (§7.3.11) ──────────────────────────────────────────────────────────────────────────────────────

    private enum DefineKind { Value, Off, Parameter }

    /// <summary>Split a <c>&gt;&gt;DEFINE compilation-variable-name [AS] { operand | PARAMETER | OFF } [OVERRIDE]</c>
    /// directive at the DIRECTIVE-SYNTAX level (name / AS / OFF / PARAMETER / OVERRIDE are directive keywords, not
    /// expression syntax); the OPERAND text is handed to the ANTLR fragment parse. §7.3.11.2 makes AS optional and
    /// OVERRIDE a trailing phrase; OFF and PARAMETER are the two operand-less alternatives.</summary>
    private static bool TrySplitDefine(string rest, out (string Name, DefineKind Kind, string Operand, bool Override) define,
        out string complaint)
    {
        define = default;
        string s = rest.TrimSpaces();
        int sp = 0;
        while (sp < s.Length && !CobolSpace.IsSeparator(s[sp])) sp++;
        string name = s[..sp];
        string body = sp < s.Length ? s[sp..].TrimSpaces() : "";
        // §7.3.11.2: compilation-variable-name-1 is required, and is ONE word — not a literal, not an expression.
        if (name.Length == 0) { complaint = "no compilation-variable-name-1 is written"; return false; }
        if (!IsCompilationVariableNameShape(name))
        {
            complaint = $"'{name}' is not a COBOL word, so it cannot be compilation-variable-name-1";
            return false;
        }
        if (StartsWithWord(body, "AS")) body = body["AS".Length..].TrimSpacesStart();   // AS is not underlined: an optional word (§5.2.3)

        // { value-group [OVERRIDE] | OFF }: OFF is an alternative of the OUTER brace, so neither OVERRIDE nor any other
        // word may follow it (kb/Work PB1367 — the old trailing-OVERRIDE strip ran before the OFF test and took
        // `OFF OVERRIDE` for OFF).
        if (StartsWithWord(body, "OFF"))
        {
            string after = body["OFF".Length..].TrimSpaces();
            if (after.Length == 0) { define = (name, DefineKind.Off, "", false); complaint = ""; return true; }
            complaint = StartsWithWord(after, "OVERRIDE") && after.Length == "OVERRIDE".Length
                ? "the OVERRIDE phrase belongs to the value alternative — OFF is an alternative of its own and is not followed by OVERRIDE"
                : $"'{after}' follows OFF, which is an operand by itself";
            return false;
        }

        bool over = EndsWithWord(body, "OVERRIDE");
        if (over) body = body[..^"OVERRIDE".Length].TrimSpacesEnd();
        body = body.TrimSpaces();
        if (body.Length == 0)
        {
            complaint = over ? "OVERRIDE follows no operand" : "no operand follows the compilation-variable-name";
            return false;
        }
        define = body.Equals("PARAMETER", StringComparison.OrdinalIgnoreCase)
            ? (name, DefineKind.Parameter, "", over)
            : (name, DefineKind.Value, body, over);
        complaint = "";
        return true;
    }

    /// <summary>A compilation-variable-name is a COBOL user-defined word (§8.3.2.1): basic letters, digits, hyphen and
    /// underscore, neither beginning nor ending with a hyphen.</summary>
    private static bool IsCompilationVariableNameShape(string w)
    {
        foreach (char c in w) if (!char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')) return false;
        return w[0] != '-' && w[^1] != '-';
    }

    /// <summary>Apply one emitting-branch <c>&gt;&gt;DEFINE</c> to <paramref name="defines"/>. Returns the name whose
    /// entry the directive CHANGED and the operand as written — the event the compilation-variable timeline records
    /// (kb/Work PB1368) — or null when the directive changed nothing (malformed, or an operand that did not evaluate;
    /// each already reported).</summary>
    private static (string Name, string Written)? ApplyDefine(string rest, Dictionary<string, CtValue> defines,
        CompileTimeExpressionEvaluator evaluator, DirectiveDiag diag, int dialectLevel, CompilationInputs inputs)
    {
        // The directive's own general format first (kb/Work PB1367): every violation names the rule and the directive
        // is not applied, so one malformed line cannot cascade into misleading "undefined variable" errors.
        if (!TrySplitDefine(rest, out var define, out string complaint))
        {
            diag.DefineMalformed(complaint);
            return null;
        }
        var (name, kind, operand, over) = define;
        // §7.3.11.3 SR1 (with §7.3.3 SR7, SR9): the name shall not be a compiler-directive word — the ONE §8.12
        // representation, asked of the same screen the defined condition uses (kb/Work PB1366).
        if (CompilerDirectiveWords.IsReserved(name))
        {
            diag.DirectiveWordAsName(name, "a DEFINE directive", "ISO §7.3.11.3 SR1");
            return null;
        }
        // §8.3.2.1 applies to the compilation-variable-name — a word the tree-walk funnel never sees. Checked at
        // the DEFINITION site (the root: an over-long word can never become defined, so a reference-site spelling
        // is already diagnosed as an unknown variable). Report and continue, matching the funnel's posture.
        if (CobolWordRule.LengthViolation(name, dialectLevel) is { } violation) diag.WordLength(violation);
        switch (kind)
        {
            case DefineKind.Off:
                defines.Remove(name);   // GR2 — undefine
                return (name, "");
            case DefineKind.Parameter:
            {
                // GR4 — the value is obtained from the operating environment by an implementor-defined method
                // (DOC-A.1-49, docs/CONFORMANCE.md): the environment variable named by the compilation-variable-name in
                // its canonical upper-case spelling — a COBOL word is case-insensitive (§8.3.1), so two spellings of
                // one name read ONE variable (kb/Work PB1533). A value that parses as a fixed-point numeric literal is
                // numeric, else alphanumeric. Unavailable ⇒ NOT defined.
                string? env = ParameterText(name, inputs);
                CtValue? pv = env is null ? null
                    : decimal.TryParse(env, NumberStyles.Number, CultureInfo.InvariantCulture, out var num)
                        ? CtValue.Numeric(CtNumeric.FromDecimal(num), env) : CtValue.Alphanumeric(env);
                // SR2 applies to the PARAMETER alternative like every other (kb/Work PB1367): without OVERRIDE, a name
                // already defined may be redefined only to the SAME value, and "no value" is not the same value.
                if (pv is null && !over && defines.ContainsKey(name)) diag.Report1618(name);
                if (pv is null) { defines.Remove(name); return (name, ""); }
                AssignDefine(name, pv, over, defines, diag);
                return (name, "");
            }
            default:
                if (EvaluateOperandText(operand, evaluator, diag, $">>DEFINE {name}") is not { } v) return null;
                AssignDefine(name, v, over, defines, diag);
                return (name, operand);
        }
    }

    /// <summary>Bind <paramref name="name"/> to <paramref name="newVal"/>, enforcing §7.3.11.3 SR2: without the
    /// OVERRIDE phrase a compilation variable already defined (and not OFF'd) may be redefined only to the SAME
    /// value (category-aware value equality — <c>AS 1</c> / <c>AS 01</c> / <c>AS 1.0</c> are the same). A differing
    /// no-OVERRIDE redefinition is COBOLNET1618 (superset-continue: the new value still binds).</summary>
    private static void AssignDefine(string name, CtValue newVal, bool over, Dictionary<string, CtValue> defines,
        DirectiveDiag diag)
    {
        if (!over && defines.TryGetValue(name, out var existing) && !existing.Equals(newVal))
            diag.Report1618(name);
        defines[name] = newVal;
    }

    // ── operand / cce evaluation via the ANTLR fragment parse + the shared evaluator ──────────────────────────

    /// <summary>Fragment-parse and evaluate one compile-time operand to a <see cref="CtValue"/>, or null (a
    /// syntax error is reported as COBOLNET1619; a formation error is reported by the evaluator).</summary>
    private static CtValue? EvaluateOperandText(string text, CompileTimeExpressionEvaluator evaluator,
        DirectiveDiag diag, string where)
    {
        if (DirectiveExpressionFragment.ParseOperand(text) is not { } frag) { diag.Malformed(where, text); return null; }
        var operand = frag.compileTimeOperand();
        diag.FlagArithmetic(operand);   // b COMPILE-TIME-ARITHMETIC-EXPRESSIONS (§7.3.15.4 GR4 b) — evaluated context
        diag.GateBooleanOperators(operand);
        return evaluator.EvaluateOperand(operand, where);
    }

    /// <summary>Fragment-parse and evaluate a constant-conditional-expression; a malformed cce / formation error
    /// yields false for line selection (and is reported).</summary>
    private static bool EvaluateCceText(string text, CompileTimeExpressionEvaluator evaluator,
        DirectiveDiag diag, string where)
    {
        if (DirectiveExpressionFragment.ParseCce(text) is not { } frag) { diag.Malformed(where, text); return false; }
        var cce = frag.constantConditionalExpression();
        diag.FlagArithmetic(cce);   // b COMPILE-TIME-ARITHMETIC-EXPRESSIONS (§7.3.15.4 GR4 b) — evaluated context
        diag.GateBooleanOperators(cce);
        diag.GateLogicalOperators(cce);
        return evaluator.EvaluateCce(cce, where) ?? false;
    }

    /// <summary>The first descendant of type <typeparamref name="T"/> in <paramref name="node"/>'s subtree — used
    /// to detect an arithmetic OPERATOR (<c>addOp</c>/<c>mulOp</c>) inside a directive operand for FLAG-14 b.</summary>
    private static bool HasDescendant<T>(Antlr4.Runtime.Tree.IParseTree node) where T : class
    {
        for (int k = 0; k < node.ChildCount; k++)
        {
            var child = node.GetChild(k);
            if (child is T || HasDescendant<T>(child)) return true;
        }
        return false;
    }

    /// <summary>Format-1 WHEN match (§7.3.13.4 GR4): subject == object, or (with THROUGH/THRU) the subject in the
    /// inclusive NUMERIC range [object, object3] (SR12 — a range requires numeric operands). Non-numeric equality
    /// is category-aware and length-sensitive (GR7).</summary>
    private static bool MatchWhen(CtValue? subject, string whenText, CompileTimeExpressionEvaluator evaluator,
        DirectiveDiag diag)
    {
        if (subject is null) return false;
        var (loText, hiText) = SplitRange(whenText);
        if (EvaluateOperandText(loText, evaluator, diag, ">>WHEN") is not { } lo) return false;
        // §7.3.13.3 SR11 — all selection subjects and objects shall be of the same category.
        if (subject.Category != lo.Category)
        {
            diag.Report(CtDiagCode.DirectiveRule,
                ">>WHEN: a selection object shall be of the same category as the selection subject (ISO §7.3.13.3 SR11)");
            return false;
        }
        if (hiText is null) return subject.RelationalEquals(lo);   // GR4a — subject == object (boolean right-extends, §8.8.4.2.8)
        if (EvaluateOperandText(hiText, evaluator, diag, ">>WHEN") is not { } hi) return false;
        // GR4b / SR12 — an inclusive NUMERIC range.
        if (subject.Category != CtCategory.Numeric || lo.Category != CtCategory.Numeric || hi.Category != CtCategory.Numeric)
        {
            diag.Report(CtDiagCode.DirectiveRule, ">>WHEN: a THROUGH range requires numeric operands (ISO §7.3.13.3 SR12)");
            return false;
        }
        return CobolDec.Compare(subject.Number, lo.Number) >= 0 && CobolDec.Compare(subject.Number, hi.Number) <= 0;
    }

    /// <summary>Split a WHEN object at a top-level <c>THROUGH</c>/<c>THRU</c> word (the §7.3.13 range separator),
    /// ignoring any occurrence inside a string literal. Returns (object, null) when no range is present.</summary>
    private static (string Lo, string? Hi) SplitRange(string text)
    {
        int idx = FindRangeWord(text);
        if (idx < 0) return (text.TrimSpaces(), null);
        int end = idx;
        while (end < text.Length && !CobolSpace.IsSeparator(text[end])) end++;
        return (text[..idx].TrimSpaces(), text[end..].TrimSpaces());
    }

    private static int FindRangeWord(string text)
    {
        bool inStr = false;
        char q = '\0';
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inStr) { if (c == q) inStr = false; continue; }
            if (c is '"' or '\'') { inStr = true; q = c; continue; }
            bool wordStart = i == 0 || CobolSpace.IsSeparator(text[i - 1]);
            if (wordStart && (MatchesWordAt(text, i, "THROUGH") || MatchesWordAt(text, i, "THRU"))) return i;
        }
        return -1;
    }

    private static bool MatchesWordAt(string text, int i, string word)
    {
        if (i + word.Length > text.Length) return false;
        if (string.Compare(text, i, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) != 0) return false;
        int after = i + word.Length;
        return after == text.Length || CobolSpace.IsSeparator(text[after]);
    }

    // ── small directive-syntax helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>The upper-cased directive keyword and its operand, through the ONE compiler-directive line parse
    /// (<see cref="CompilerDirectiveLine"/>, kb/Work PB794) — which is also what removes a §7.3.3 SR3/SR4 trailing
    /// inline comment, so <c>&gt;&gt;IF X *&gt; why</c> reaches the evaluator as <c>X</c>. A line that is not a
    /// directive (a bare <c>&gt;&gt;</c>) yields an empty keyword and falls through to the unrecognized arm.</summary>
    private static (string keyword, string rest) SplitDirective(string trimmed) =>
        CompilerDirectiveLine.TryParse(trimmed, out var d) ? (d.Word, d.Operand) : ("", "");

    private static bool StartsWithWord(string s, string word) =>
        s.StartsWith(word, StringComparison.OrdinalIgnoreCase)
        && (s.Length == word.Length || CobolSpace.IsSeparator(s[word.Length]));

    private static bool EndsWithWord(string s, string word) =>
        s.EndsWith(word, StringComparison.OrdinalIgnoreCase)
        && (s.Length == word.Length || CobolSpace.IsSeparator(s[s.Length - word.Length - 1]));

    /// <summary>The frontend diagnostic gateway: the shared evaluator's code-preserving reports (any
    /// <see cref="CtDiagCode"/>) and a fragment syntax error both route to COBOLNET1619 (the directive-expression
    /// violation); the §7.3.11.3 SR2 redefinition is COBOLNET1618. <see cref="At"/> is set before each directive
    /// (kb/Work PB82: the SOURCE origin of the directive line — the copybook's own file and line when the directive
    /// is inside copied text — never an index into the text being rendered).</summary>
    private sealed class DirectiveDiag(DiagnosticBag? bag, string? sourcePath, FlagScanState flagScan, EditionInfo edition)
        : ICtDiagnostics
    {
        /// <summary>The source file the directives are read from (kb/Work PB82 — the identity origin of unmapped text).</summary>
        public string? SourcePath => sourcePath;
        private readonly FlagScanState _flagScan = flagScan;
        /// <summary>The origin (file, physical line) of the directive being processed.</summary>
        public SourceOrigin At = new(sourcePath ?? "", 1);

        /// <summary>FLAG-14 b (§7.3.15.4 GR4 b; E.2 item 6) — flag a compile-time arithmetic EXPRESSION (one with a
        /// real <c>addOp</c>/<c>mulOp</c>, not a bare literal) when COMPILE-TIME-ARITHMETIC-EXPRESSIONS is ON at the
        /// current directive line. Called on the already-parsed operand/cce fragment, in the evaluated context.</summary>
        public void FlagArithmetic(Antlr4.Runtime.Tree.IParseTree tree)
        {
            if (_flagScan.IsOn(FlagOption.Flag14CompileTimeArithmeticExpressions)
                && (HasDescendant<CobolParserCore.AddOpContext>(tree) || HasDescendant<CobolParserCore.MulOpContext>(tree)))
                FlagWarn(FlagOption.Flag14CompileTimeArithmeticExpressions, At);
        }

        /// <summary>The boolean-operator introduction gate over an evaluated directive fragment (kb/Work PB1370): a
        /// compile-time boolean expression is "formed in accordance with 8.8.2" (ISO §7.3.7.2 SR1) OF THE TARGETED
        /// EDITION, so a B-SHIFT-* below COBOL-2023 is the same COBOLNET0900 its runtime twin draws — asked through
        /// the ONE body the compilation-unit walk uses (<see cref="BooleanOperatorGate"/>), once per fragment.</summary>
        public void GateBooleanOperators(Antlr4.Runtime.Tree.IParseTree fragment)
        {
            if (bag is not null) BooleanOperatorGate.Check(edition, new BagSink(bag, At.ToLocation()), fragment);
        }

        /// <summary>The logical-operator introduction gate over an evaluated constant-conditional-expression
        /// fragment (kb/Work PB1371): a >>IF operand is "a complex condition as specified in 8.8.4.9" (ISO §7.3.8.2
        /// SR1 d)) of the targeted edition, so an XOR / EXCLUSIVE-OR connective below COBOL-2023 is the same
        /// COBOLNET0900 its runtime twin draws — asked through the ONE body the compilation-unit walk uses
        /// (<see cref="LogicalOperatorGate"/>), once per fragment.</summary>
        public void GateLogicalOperators(Antlr4.Runtime.Tree.IParseTree fragment)
        {
            if (bag is not null) LogicalOperatorGate.Check(edition, new BagSink(bag, At.ToLocation()), fragment);
        }

        public void Report(CtDiagCode code, string message) => Emit(
            code == CtDiagCode.DirectiveWordAsName
                ? Editions.Diagnostics.DiagnosticCatalog.DirectiveWordAsName.Code : "COBOLNET1619", message);

        /// <summary>COBOLNET2650 — a compilation-variable-name that is a §8.12 compiler-directive word (kb/Work PB1366).</summary>
        public void DirectiveWordAsName(string name, string where, string rule) =>
            Emit(Editions.Diagnostics.DiagnosticCatalog.DirectiveWordAsName.Code,
                $"'{name}' is a compiler-directive word (ISO §8.12) and shall not be used as a compilation-variable-name in {where} ({rule})");

        /// <summary>COBOLNET2651 — a DEFINE directive that does not match its general format (kb/Work PB1367).</summary>
        public void DefineMalformed(string complaint) =>
            Emit(Editions.Diagnostics.DiagnosticCatalog.DefineDirectiveMalformed.Code,
                $">>DEFINE is malformed: {complaint} — the general format is "
                + ">>DEFINE compilation-variable-name-1 [AS] { { arithmetic-expression | boolean-expression | literal | PARAMETER } [OVERRIDE] | OFF } (ISO §7.3.11.2)");

        /// <summary>COBOLNET2698 — a DISPLAY directive's UPON phrase names no available compile-time device, or breaks the
        /// choice-indicator discipline of its format (kb/Work PB807).</summary>
        public void DisplayUpon(string complaint) =>
            Emit(Editions.Diagnostics.DiagnosticCatalog.DisplayDirectiveUpon.Code,
                $">>DISPLAY UPON: {complaint} (ISO §7.3.12.2, §7.3.12.4 GR5)");

        /// <summary>COBOLNET2697 — the directive at <see cref="At"/> stands within an unfinished COPY or REPLACE
        /// statement (kb/Work PB1384).</summary>
        public void WithinStatement(string statementKeyword) =>
            Emit(Editions.Diagnostics.DiagnosticCatalog.DirectiveWithinTextManipulationStatement.Code,
                $"a compiler directive is specified within a {statementKeyword} statement — the statement has no separator "
                + "period before this line (ISO §7.3.3 SR8 b)");

        public void Report1618(string name) => Emit("COBOLNET1618",
            $">>DEFINE: compilation variable '{name}' is redefined to a different value without the OVERRIDE "
            + "phrase (ISO §7.3.11.3 SR2)");

        /// <summary>COBOLNET2649 — a conditional-compilation directive breaks the structure of its general format
        /// (kb/Work PB1363).</summary>
        public void Structure(string message) =>
            Emit(Editions.Diagnostics.DiagnosticCatalog.DirectiveStructureViolation.Code, message);

        public void Malformed(string where, string text) => Emit("COBOLNET1619",
            $"{where}: malformed compile-time expression '{text}' (ISO §7.3.6 / §7.3.7 / §7.3.8)");

        /// <summary>Emit a migration-flag WARNING for a frontend-inline FLAG option (b / c) at
        /// <paramref name="at"/> — the same code/message shape as <c>FlagConformancePass</c> for the bound
        /// options, so the two collection sites are indistinguishable to the user.</summary>
        public void FlagWarn(FlagOption option, SourceOrigin at)
        {
            var info = FlagOptions.Info(option);
            string code = info.Directive == FlagDirective.Flag14
                ? Editions.Diagnostics.DiagnosticCatalog.Flag14Warning.Code
                : Editions.Diagnostics.DiagnosticCatalog.Flag02Warning.Code;
            bag?.ReportWarning(code,
                $"{info.Change} — flagged by >>{FlagDirectiveLine.DirectiveWord(info.Directive)} {info.Word}",
                at.ToLocation(), default);
        }

        /// <summary>COBOLNET1567 — the §8.3.2.1 word-length ceiling on a directive-carried word, the SAME code
        /// and text the tree-walk funnel emits (CobolWordRule owns the message).</summary>
        public void WordLength(string violation) =>
            Emit(Editions.Diagnostics.DiagnosticCatalog.WordLengthExceeded.Code, violation);

        private void Emit(string code, string message) =>
            bag?.ReportError(code, message, At.ToLocation(), default);
    }
}
