// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// Intrinsic-function binding (ISO §15; COBOLNET_INTRINSICS_DESIGN spine 1). P7 Step 12: FUNCTION arguments
/// are REAL parse trees — <c>functionCall : FUNCTION functionName (FNARG_LPAREN functionArgList? FNARG_RPAREN)?</c>
/// (the argument-list parens carry their own token type since PB48 — ISO §8.4.3.2.3 SR6), each
/// <c>functionArgument</c> one of the §8.4.3.2 SR8 shapes (phrase word / OMITTED / non-numeric literal /
/// arithmetic expression) — and every arithmetic argument binds through the ONE
/// <see cref="ExpressionBinder.BindExpr"/> (nested <c>FUNCTION</c> calls recurse naturally through
/// <c>BindPrimary</c>). The keyword-omitted form <c>name(args)</c> (§8.4.3.2 SR2) still arrives as a
/// SUBSCRIPT-mode capture on a <c>dataReference</c> (D2 — a grammar alternative is an irreducible ambiguity);
/// <see cref="KeywordOmittedFunction"/> re-parses that captured text through the SAME <c>functionArgList</c>
/// rule (<c>FunctionArgFragment</c>), so both reference forms bind through one argument pipeline and the former
/// per-segment recursive-descent parser is DELETED. <c>table(ALL)</c> arguments expand at bind time to one
/// operand per occurrence (§15.3), detected from the argument's sole data reference whose subscript capture
/// (still SUBSCRIPT-mode — the D10/PHASE-15 deferral) holds a depth-0 ALL. ALL semantics — D8 edition gating,
/// §15.3 arity, MAX/MIN category resolution, the §15.68.3 r3 default-currency injection, the D7 LENGTH fold —
/// happen HERE; backends only render the resulting <see cref="BoundIntrinsicCall"/>.
/// P7 Step 10k: a real collaborator over <see cref="BinderContext"/>, landed TOGETHER with
/// <see cref="UdfBinder"/> (the bidirectional §12.3.8.2 GR12 pair: the user-function dispatch here PRECEDES
/// the catalog lookup and reaches <c>host.Udf</c>; UdfBinder's argument bind reaches BACK into
/// <see cref="BindArgOperand"/>). The D8 IntroducedIn/RemovedIn windows, the &lt;2002 keyword-omitted
/// routing gate, and TRIM-arg2 moved VERBATIM (Exec Step E folds the diagnostics; the &lt;2002 routing gate
/// at the top of <see cref="KeywordOmittedFunction"/> is PARSE-ROUTING behavior and stays a binder switch by
/// design — the premise audit's finding). <c>CompileClock</c> lives HERE (IntrinsicRenderer reads it).</summary>
internal sealed class IntrinsicBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>The injectable compile-time clock for WHEN-COMPILED (§15.99.3 r2 — the COMPILATION timestamp;
    /// deep-dive D6). One capture per COMPILATION (kb/Work PB120), shared by every unit of that compilation, which
    /// the backend bakes into the generated source as a string constant. Never INVOKED directly: the one read is
    /// <c>CompilationInputs.ReadCompilationTime</c> (ProgramEmitter), which records that the compilation's output
    /// depends on the clock (kb/Work PB985).</summary>
    internal static Func<DateTimeOffset> CompileClock { get; set; } = () => DateTimeOffset.Now;   // recorded at its call site: CompilationInputs.ReadCompilationTime

    /// <summary>FUNCTION call in an expression position (the <c>BindPrimary</c> hook). A trailing
    /// <c>refModPart</c> reference-modifies the RESULT (ISO §8.4.3.3.3 SR2 — fix-queue PB8).</summary>
    public BoundExpr BindIntrinsic(Core.FunctionCallContext fc)
    {
        // The KEYWORD-OMITTED alternative for a reserved intrinsic name (fix-queue PB9: RANDOM / SIGN / SUM —
        // §8.9-reserved words that are also §8.11 intrinsic function names). §8.4.3.2.3 SR2 permits the omission
        // ONLY when the REPOSITORY declares the function, which the grammar cannot know; enforce it here.
        // Rejecting is unambiguous: a reserved word cannot be a data name (§8.3.2.4.1), so there is no other
        // reading to fall back to and a plain, cited error is the whole answer.
        string? reservedName = fc.reservedIntrinsicArgFn()?.GetText() ?? fc.RANDOM()?.GetText();
        if (reservedName is { } fn)
        {
            if (!ctx.Data.IsRepositoryIntrinsic(fn))   // the ONE REPOSITORY membership (kb/Work PB1083)
            {
                ctx.Edition.Error("COBOLNET1543",
                    $"'{fn}' is written without the word FUNCTION, but the REPOSITORY paragraph does not declare "
                    + $"it. ISO §8.4.3.2.3 SR2 allows the omission only for an intrinsic named in REPOSITORY — "
                    + $"write 'FUNCTION {fn}', or add 'REPOSITORY. FUNCTION {fn} INTRINSIC.' (or FUNCTION ALL "
                    + "INTRINSIC).");
                return BoundExprError.Refused(ctx.Edition, $"FUNCTION {fn}");
            }
            // The captured group is the ARGUMENT LIST unless it holds a depth-0 colon, in which case it is a
            // reference modification of a zero-argument result — the same two shapes, decided the same way, as
            // KeywordOmittedFunction. §8.4.3.2.3 SR6 is then applied by ResultRefMod exactly as for the
            // FUNCTION-keyword form, so `RANDOM (1:4)` is rejected as an argument list on both routes.
            var sp = fc.subscriptPart();
            if (sp?.subscriptOrRefMod() is { } grp && ReferenceResolver.HasDepth0Colon(grp))
                return DefinitionPermitsArguments(fn)
                    ? Sr6ArgumentListError(fn)
                    : ResultRefMod(BindIntrinsicCore(fn, []), ctx.Refs.ReadRefMod(grp), fn);
            var args = sp is null ? [] : ReparseArgs(sp);
            return args is null
                ? BoundExprError.Refused(ctx.Edition, $"FUNCTION {fn} arguments")
                : FinishIntrinsic(fc, BindIntrinsicCore(fn, args), fn);
        }
        string name = fc.functionName().GetText();
        string display = $"FUNCTION {name}";
        // §8.4.3.2.3 SR6 is decided HERE, from the function's DEFINITION and BEFORE the arguments bind — never
        // after. `FUNCTION UPPER-CASE (1:4)` parses as a name plus a refModPart (the FNARG_LPAREN is the
        // ref-mod's, not a direct child), so the argument list is EMPTY; binding first would report the §15.3
        // arity error ("takes 1 argument(s); 0 given" — the PB61 SR-8.4.3.2.3-6 misroute) about an argument
        // list the user never wrote, and the SR6 arm inside the ref-mod applier would then never see the call.
        if (fc.FNARG_LPAREN() is null && fc.refModPart().Length != 0 && DefinitionPermitsArguments(name))
            return Sr6ArgumentListError(display);
        // §8.4.3.2.3 SR5 — "If function-pointer-name-1 is specified, the parentheses shall be specified" — decided
        // from the parse like SR6 above, before any bind: `FUNCTION FP` and `FUNCTION FP (1:4)` write no argument
        // list (kb/Work PB847).
        if (fc.FNARG_LPAREN() is null && FunctionPointerNamed(name) is not null)
        {
            ctx.Edition.Error(DiagnosticCatalog.FunctionPointerParenthesesRequired,
                $"{display}: '{name}' is a function-pointer, so the function-identifier shall write its argument "
                + "list in parentheses — '(' ')' for none (ISO §8.4.3.2.3 SR5)");
            return BoundExprError.Refused(ctx.Edition, display);
        }
        return FinishIntrinsic(fc, BindIntrinsicCore(name, ArgsOf(fc.functionArgList())), display);
    }

    /// <summary>ISO §8.4.3.2.3 SR6 — "If a function's definition permits arguments and a left parenthesis
    /// immediately follows function-prototype-name-1 or intrinsic-function-name-1, the left parenthesis is
    /// always treated as the left parenthesis of that function's arguments." The ONE answer to "does this
    /// function's DEFINITION permit arguments", read from the definition the reference names: the
    /// REPOSITORY-declared user function's prototype (its USING formals — SR6 names function-prototype-name-1
    /// too, and §12.3.8.2 GR12 gives the user function precedence over a same-named intrinsic) or the catalog
    /// signature (a >>COBOL-WORDS synonym resolves to its canonical first). Asked BEFORE any argument binds, by
    /// every route a `NAME (start:length)` can arrive on — the FUNCTION-keyword form, the reserved-name
    /// keyword-omitted form and <see cref="KeywordOmittedFunction"/> — because once the group has been read as
    /// a ref-mod the bind reports an ARITY error about an empty argument list, and no later check can undo a
    /// diagnostic already issued. False for a name that is neither: the ordinary paths report it.</summary>
    private bool DefinitionPermitsArguments(string name)
    {
        // >>COBOL-WORDS through the ONE resolution (ISO §7.3.10.4 GR2/GR3/GR4; kb/Work PB250). A name the
        // directive REMOVED (UNDEFINE literal-3 / SUBSTITUTE literal-4) is no longer a function name, so its
        // definition permits nothing — the '(' that follows is a ref-mod, not an argument list.
        if (ctx.CobolWords.Resolve(name) is not { } canonical) return false;
        name = canonical;
        if (ctx.Data.UserFunctionNames.Contains(name)
            || name.Equals(host.UdfSelfName, StringComparison.OrdinalIgnoreCase))
            return host.UserFunctions is { } fns && fns.TryGetValue(name, out var fn) && fn.Formals.Count > 0;
        return IntrinsicCatalog.TryGet(name, out var sig) && sig.MaxArgs > 0;
    }

    /// <summary>The SR6/SR8 verdict for a `(start:length)` group written directly after the NAME of a function
    /// whose definition permits arguments (<see cref="DefinitionPermitsArguments"/>): that '(' opened an
    /// ARGUMENT LIST, and <c>start:length</c> is not one of the §8.4.3.2.3 SR8 argument shapes — so it is
    /// reported as the argument-list error it is, never as an arity error, a class error about a ref-mod that
    /// was never written, or (keyword-omitted) an undefined data-name. <c>FUNCTION RANDOM (1:4)</c> is the
    /// standard's own cautionary shape (the SR6 NOTE, where the empty list <c>FUNCTION RANDOM ()</c> is how a
    /// function with only optional arguments is written argument-less); the legal way to reference-modify a
    /// result is to write the argument list first — <c>FUNCTION UPPER-CASE(x) (1:2)</c>.</summary>
    private BoundExprError Sr6ArgumentListError(string display)
    {
        ctx.Edition.Error("COBOLNET1543", $"'{display} (…)' — the '(' after the name of a function that "
            + "takes arguments is ALWAYS its argument list (ISO §8.4.3.2.3 SR6), so this is an argument list "
            + "and 'start:length' is not a valid argument (SR8). Reference-modify the RESULT by writing the "
            + $"argument list first: {display}(<arguments>) (start:length).");
        return BoundExprError.Refused(ctx.Edition, $"{display} arguments");
    }

    /// <summary>The tail both <c>functionCall</c> alternatives share: the §8.4.3.3.3 ref-mod on the RESULT
    /// (fix-queue PB8). Shared so the FUNCTION-keyword form and the reserved-name keyword-omitted form cannot
    /// drift apart on SR2/SR3/SR6 — the drift PB8 itself was.</summary>
    private BoundExpr FinishIntrinsic(Core.FunctionCallContext fc, BoundExpr call, string display)
    {
        var refMods = fc.refModPart();
        if (refMods.Length == 0) return call;
        // §8.4.3.3.3 SR3, enforced HERE rather than by the grammar's arity so that a function result and a data
        // reference report the SAME diagnostic (the data side must count anyway — dataReferenceSuffix* cannot
        // express the limit either).
        if (refMods.Length > 1)
        {
            ctx.Edition.Error(DiagnosticCatalog.RefModOfRefMod,
                $"'{display}' carries {refMods.Length} reference modifications; the result of a function cannot "
                + "be reference-modified twice (ISO §8.4.3.3.3 SR3). Compose the positions into one modifier.");
            return BoundExprError.Refused(ctx.Edition, $"reference modification of {display}");
        }
        // §8.4.3.2.3 SR6 was already decided by the caller, from the definition, before the call bound — the
        // only ref-mod that reaches here is one that FOLLOWS the argument list (or a zero-argument function).
        return ResultRefMod(call, ctx.Refs.ReadRefMod(refMods[0]), display);
    }

    /// <summary>
    /// Apply a reference modification to a FUNCTION RESULT (fix-queue PB8) — the ONE applier, shared by the
    /// FUNCTION-keyword form and both keyword-omitted shapes, so §8.4.3.3.3 SR2 is enforced once.
    /// <para><b>ISO §8.4.3.2.3 SR6</b> is NOT decided here, and deliberately so (kb/Work PB61, SR-8.4.3.2.3-6):
    /// it used to be, keyed on an <c>argListWritten</c> flag and the bound call's signature — but a
    /// <c>(1:4)</c> misread as a ref-mod leaves the argument list EMPTY, so for every function that REQUIRES an
    /// argument the bind had already reported an arity error and returned a <see cref="BoundExprError"/> before
    /// this applier could look, which is how <c>FUNCTION UPPER-CASE (1:4)</c> drew "takes 1 argument(s); 0
    /// given" while <c>FUNCTION RANDOM (1:4)</c> (MinArgs 0) drew the SR6 message. Every caller now asks
    /// <see cref="DefinitionPermitsArguments"/> BEFORE binding and returns <see cref="Sr6ArgumentListError"/>;
    /// what reaches here is a ref-mod that follows an argument list, or one on a zero-argument function.</para>
    /// <para><b>ISO §8.4.3.3.3 SR2</b> — "If identifier-1 is a function-identifier, it shall reference an
    /// alphanumeric, boolean, or national function." A numeric/integer function has no character positions for
    /// §8.4.3.3.4 GR4 to number, so it is rejected with <c>COBOLNET1629</c>.</para>
    /// <para>A USER-DEFINED function's result is already a real <see cref="Place"/> (the §8.4.3.2.4 GR1 caller
    /// temp cloned from the RETURNING item), so it reference-modifies through the SAME
    /// <see cref="RefModPlace"/> a data item does — no second slicer, and the temp's own category answers SR2.</para>
    /// </summary>
    private BoundExpr ResultRefMod(BoundExpr call, RefModSpec? spec, string display)
    {
        if (call is BoundExprError) return call;                    // already loud — do not stack a second report
        PicCategory? category = call switch
        {
            BoundIntrinsicCall c => c.ResultCategory,
            BoundNumRef { Place.Item.Pic: { } pic } => pic.Category,
            _ => null,
        };
        // §8.4.3.3.3 SR2. NumericEdited is NOT admitted: SR2 names the three FUNCTION types (§15.2), and no
        // intrinsic is numeric-edited — the category exists here only for a user-function RETURNING item.
        if (category is not (PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean))
        {
            ctx.Edition.Error(DiagnosticCatalog.RefModFunctionResultClass,
                $"'{display}' is not an alphanumeric, boolean, or national function, so its result cannot be "
                + "reference-modified (ISO §8.4.3.3.3 SR2).");
            return BoundExprError.Refused(ctx.Edition, $"reference modification of {display}");
        }
        if (spec is not { } rm) return BoundExprError.Refused(ctx.Edition, $"reference modification of {display}");
        return call switch
        {
            BoundIntrinsicCall c => c with { RefMod = rm },
            BoundNumRef r => new BoundNumRef(new RefModPlace(r.Place, rm.Start, rm.Length)
                { AllowZeroLength = rm.AllowZeroLength }),
            _ => BoundExprError.Refused(ctx.Edition, $"reference modification of {display}"),
        };
    }

    private static IReadOnlyList<Core.FunctionArgumentContext> ArgsOf(Core.FunctionArgListContext? list) =>
        list?.functionArgument() ?? [];

    /// <summary>FUNCTION call as a MOVE sending operand (the <c>BindMove</c> hook) — and the operand shape every
    /// general-operand channel shares: the bound expression wrapped as a <see cref="BoundComputedOperand"/> (a
    /// LENGTH fold surfaces as its literal; an error stays a loud named operand).</summary>
    public BoundOperand IntrinsicOperand(Core.FunctionCallContext fc) => OperandOf(BindIntrinsic(fc));

    public static BoundOperand OperandOf(BoundExpr e) => e switch
    {
        BoundNumLiteral l => new BoundNumericLiteral(l.Text),       // a folded LENGTH
        BoundNumRef r => new BoundFieldOperand(r.Place),            // a user-function result temp (M2-UDF-1)
        BoundExprError err => BoundOperandError.Carry(err.Feature, err.IsUnbuilt),
        _ => new BoundComputedOperand(e),
    };

    /// <summary>The FUNCTION-POINTER data item <paramref name="name"/> resolves to in the active scope, as a Place,
    /// or null — the ONE §8.4.3.2.3 SR4 test ("Function-pointer-name-1 shall be defined as a function-pointer data
    /// item") both function-identifier forms ask (with and without the word FUNCTION; kb/Work PB847). A name that
    /// resolves to anything else, or to more than one function-pointer, is not function-pointer-name-1 and takes
    /// the ordinary paths — whose diagnostics already name those cases. The candidates are the NEAREST declaring
    /// source element's (§8.4.6.2.1 3); <c>SymbolTable.TryResolveUnqualified</c>), so a contained program's own
    /// FUNCTION-POINTER hides a container's GLOBAL one of the same name instead of counting it as a rival.</summary>
    private Place? FunctionPointerNamed(string name)
    {
        if (!ctx.Symbols.TryResolveUnqualified(name, ctx.ActiveScope, out var candidates)) return null;
        DataItem? only = null;
        foreach (var c in candidates)
        {
            if (c.Pic?.Category is not PicCategory.FunctionPointer) continue;
            if (only is not null) return null;   // ambiguous — qualification is not part of the name form
            only = c;
        }
        return only is not null && only.SubscriptLevels().Count == 0 ? ctx.Refs.ResolveItem(only) : null;
    }

    /// <summary>The §8.4.3.2 SR2 FUNCTION-keyword-OMITTED reference form (M2-UDF-4): a data reference whose head
    /// is a REPOSITORY-declared user-function / function-prototype-name (or the containing function's own name),
    /// or — when <c>FUNCTION ALL INTRINSIC</c> / <c>FUNCTION name INTRINSIC</c> is in effect — an
    /// intrinsic-function-name, followed by a single subscript part, is a FUNCTION CALL, not a subscripted data
    /// item (SR6 :6918 — a <c>(</c> after such a name is ALWAYS its argument list). Bind-side, no grammar change
    /// (D2 — the discriminator is semantic, so a keyword-omitted <c>functionCall</c> grammar alternative would be
    /// an irreducible ambiguity with subscripted <c>dataReference</c>). Data-item-safe: a name that ALSO resolves
    /// to a declared data item stays a subscript (a data item wins — zero regression). Called at the two
    /// dataReference chokepoints (<see cref="RefExpr"/> / <see cref="FieldOperand"/>). Null = not one.</summary>
    public BoundExpr? KeywordOmittedFunction(Core.DataReferenceContext dref)
    {
        // Keyword omission via the REPOSITORY FUNCTION specifier is a COBOL-2002 introduction (§12.3.8) — below
        // 2002 the routing is inert, so the 85/NIST surface is byte-invariant (a lone name(args) stays a data
        // reference exactly as before). The FUNCTION-keyword form is unaffected at every edition.
        if (ctx.Edition.DialectLevel < 2002) return null;
        if (dref.cobolWord() is not { } cw) return null;                    // special registers (LINAGE/LINE/PAGE-COUNTER) are never functions
        var suffixes = dref.dataReferenceSuffix();
        // ⛔ ZERO SUFFIXES IS A LEGAL FORM, and requiring exactly one made every ZERO-ARGUMENT intrinsic
        // unreachable in the keyword-omitted form (fix-queue PB7). §15.21.2's general format is
        // `FUNCTION CURRENT-DATE` with no parentheses at all, so with the keyword omitted it is a BARE NAME —
        // zero suffixes, not one. `REPOSITORY. FUNCTION ALL INTRINSIC.` + `MOVE CURRENT-DATE TO X` therefore
        // fell through to a data reference, resolved to nothing, COMPILED CLEAN and threw
        // NotImplementedCobolFeatureException at RUN TIME. PI, E, WHEN-COMPILED and the rest of the family
        // failed identically; the standard writes the form itself at §D.14.3.6.
        // Admitted ONLY when the catalog says the function can take zero arguments, so a bare word can never be
        // re-routed away from a data reference on the strength of merely sharing a name with some function.
        // ── The suffix shapes a keyword-omitted function reference can wear ──────────────────────────────────
        // With FUNCTION omitted the reference lexes as a dataReference, so its parenthesised groups arrive as
        // dataReferenceSuffixes and the ARGUMENT list is indistinguishable from a subscript until this point.
        // Reference modification of the RESULT (§8.4.3.3.3 SR2, fix-queue PB8) adds two more shapes, and the
        // standard writes one of them itself at §D.14.3.6: `FUNCTION LOCALE-DATE (CURRENT-DATE (1:8))`.
        //   ()                       zero suffixes  — a bare zero-argument reference           (PB7)
        //   (args)                   subscriptPart, no depth-0 colon — the argument list
        //   (start:len)              subscriptPart WITH a depth-0 colon — a ref-mod on a ZERO-ARGUMENT result
        //   (args) (start:len)       the argument list, then a ref-mod
        // The ref-mod tail is a refModPart, not a subscriptPart: after the argument list's ')' the previous
        // token is no longer a data-name, so the lexer's SUBSCRIPT trigger does not fire and the group stays in
        // DEFAULT mode. Both carriers are read by the ONE ReferenceResolver.ReadRefMod.
        Core.SubscriptPartContext? sp = null;
        Core.SubscriptOrRefModContext? capturedRefMod = null;   // `name (start:len)` — a zero-argument result
        Core.RefModPartContext? tailRefMod = null;              // `name (args) (start:len)`
        if (suffixes.Length == 1)
        {
            if (suffixes[0].subscriptPart() is not { } only) return null;   // a qualification tail is not an argument list
            if (only.subscriptOrRefMod() is { } g && ReferenceResolver.HasDepth0Colon(g)) capturedRefMod = g;
            else sp = only;
        }
        else if (suffixes.Length == 2)
        {
            // Exactly `(args) (ref-mod)`. Anything else — two argument lists, a qualification, a second ref-mod
            // (§8.4.3.3.3 SR3) — is not a function reference and stays a data reference, which reports through
            // the ordinary unresolved-name path rather than being turned into a function-arity error here.
            if (suffixes[0].subscriptPart() is not { } argsPart
                || argsPart.subscriptOrRefMod() is { } ag && ReferenceResolver.HasDepth0Colon(ag)
                || suffixes[1].refModPart() is not { } tail) return null;
            sp = argsPart;
            tailRefMod = tail;
        }
        else if (suffixes.Length != 0)
        {
            return null;
        }
        string name = cw.GetText();
        // §8.4.3.2.3 SR2 — "if function-prototype-name-1 or function-pointer-name-1 is specified, the word FUNCTION
        // may be omitted" — and SR5 — "If function-pointer-name-1 is specified, the parentheses shall be
        // specified". So `FP (args)` over a FUNCTION-POINTER item is a function-identifier (kb/Work PB847), and a
        // bare `FP` (no parentheses) stays the data reference to the pointer itself, which is what SET and a
        // pointer comparison read. A function-pointer is not a table, so the '(' can be no subscript here: the
        // routing takes nothing from the data path. An argument list followed by a reference-modifier of the
        // RESULT is the same two-suffix shape the prototype form wears.
        if (sp is not null && FunctionPointerNamed(name) is { } fp)
        {
            var viaPointer = ReparseArgs(sp) is { } fpArgs
                ? host.Udf.UdfBindPointerCall(fp, fpArgs)
                : BoundExprError.Refused(ctx.Edition, $"FUNCTION {name} arguments");
            return tailRefMod is null ? viaPointer : ResultRefMod(viaPointer, ctx.Refs.ReadRefMod(tailRefMod), name);
        }
        // The word as an intrinsic-function-name OF THIS COMPILATION (>>COBOL-WORDS and the edition window —
        // DataBinder.TryIntrinsicOfThisCompilation), and the REPOSITORY half through DataBinder.IsRepositoryIntrinsic,
        // the one membership the declaration screen asks too (kb/Work PB1083).
        bool catalogued = ctx.Data.TryIntrinsicOfThisCompilation(name, out var sig);
        bool repositoryIntrinsic = ctx.Data.IsRepositoryIntrinsic(name);
        bool declaredFn = ctx.Data.UserFunctionNames.Contains(name)
            || name.Equals(host.UdfSelfName, StringComparison.OrdinalIgnoreCase)
            || repositoryIntrinsic;
        if (!declaredFn && !catalogued) return null;
        // A catalogued name the REPOSITORY does NOT identify may be a user-defined word (§8.3.2.1 rule 5 — a
        // table named MOD or SQRT is legal): the declared item wins, never a mis-routed subscript. A name the
        // REPOSITORY DOES identify cannot be a user-defined word in this unit (rule 5's second exception — screened
        // at every declaration, DataBinder.DeclareUserWord), so the reference IS the function
        // (§8.4.3.2.3 SR2; kb/Work PB65 FMT-15.43.2 / FMT-15.58.2 — the former unconditional "data item wins"
        // let a shadowing table answer where §15.43.4 requires +999). A user-function-prototype name keeps the
        // data-item precedence (its declaration is not screened by rule 5).
        if (!repositoryIntrinsic && ctx.Symbols.TryResolve(name, ctx.ActiveScope, out _)) return null;
        if (!declaredFn)
        {
            // The OTHER arm of the §8.4.3.2.3 SR2 discrimination (kb/Work R22): the name is a catalogued
            // intrinsic, no data item shadows it, and the REPOSITORY does not declare it — so the reference
            // is not a legal function-identifier (SR2: without the declaration "the word FUNCTION" is
            // required) and nothing else resolves it. Until now only the RESERVED names (SIGN/SUM/RANDOM,
            // the grammar alternative in BindIntrinsic) drew COBOLNET1543; every other catalogued name fell
            // through to generic unresolved-reference staging and died at RUN time. A bare name (no argument
            // list) reads as a function only when the function admits zero arguments — mirroring the declared
            // arm below — so a mere name-collision with, say, SQRT still reports as the ordinary unresolved
            // name it is.
            if (sp is null && sig.MinArgs != 0) return null;
            ctx.Edition.Error("COBOLNET1543",
                $"'{name}' is written without the word FUNCTION, but the REPOSITORY paragraph does not declare "
                + $"it. ISO §8.4.3.2.3 SR2 allows the omission only for an intrinsic named in REPOSITORY — "
                + $"write 'FUNCTION {name}', or add 'REPOSITORY. FUNCTION {name} INTRINSIC.' (or FUNCTION ALL "
                + "INTRINSIC).");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {name}");
        }
        if (sp is null)
        {
            // `NAME (start:length)` on a DECLARED name whose definition permits arguments is §8.4.3.2.3 SR6's
            // case in the keyword-omitted form (kb/Work PB61, SR-8.4.3.2.3-6): the REPOSITORY declared the name
            // a function and no data item shadows it, so nothing else can resolve it — and the '(' after it is
            // ALWAYS its argument list, of which `1:4` is not a member (SR8). Until now this fell through the
            // MinArgs guard below to the data path and died as "'UPPER-CASE(1:4)' is not defined", while the
            // reserved-word RANDOM drew the SR6 message through the grammar's own arm — one rule, two verdicts.
            // Decided from the DEFINITION and before any bind, exactly as the FUNCTION-keyword form does.
            if (capturedRefMod is not null && DefinitionPermitsArguments(name)) return Sr6ArgumentListError(name);
            // A REPOSITORY-DECLARED USER function referenced bare is §8.4.3.2.3 SR2's own case too (kb/Work
            // R35 — the two-arm-dispatch shape a SIXTH time: PB7 fixed the intrinsic arm of the bare-name
            // form and never asked the UDF arm, so `MOVE WITHOUTPAR TO X` over a declared zero-argument
            // function fell to the data path and died as "undefined"). Route it to the UDF bind, whose
            // prototype machinery owns the zero-vs-N arity verdict — a bare reference to a declared
            // 2-argument function is an ARITY error about a function, never an undefined name: the user
            // DECLARED it a function, so there is no coincidental-collision reading to protect.
            if (ctx.Data.UserFunctionNames.Contains(name)
                || name.Equals(host.UdfSelfName, StringComparison.OrdinalIgnoreCase))
            {
                var udfBare = host.Udf.UdfBindCall(name, []);
                return capturedRefMod is null
                    ? udfBare
                    : ResultRefMod(udfBare, ctx.Refs.ReadRefMod(capturedRefMod), name);
            }
            // A bare CATALOGUED name only becomes a function reference when the function genuinely admits
            // ZERO arguments (MinArgs 0 — CURRENT-DATE, PI, E, WHEN-COMPILED, and RANDOM's no-argument form,
            // whose §15.75.2 format brackets the whole parenthesised part). Anything else stays a data
            // reference: for a name that merely COLLIDES with the catalog, the ordinary unresolved-name path
            // is the honest verdict, not an arity error about a function never intended.
            if (!catalogued || sig.MinArgs != 0) return null;
            var bare = BindIntrinsicCore(name, []);
            // `CURRENT-DATE (1:8)` — the captured group carries a depth-0 colon, so it is a reference
            // modification of the RESULT, not an argument list: SR6 was answered above (a zero-argument
            // definition permits none), so the group is applied to the result exactly as the FUNCTION-keyword
            // form applies it, and the two reference forms cannot drift apart.
            return capturedRefMod is null
                ? bare
                : ResultRefMod(bare, ctx.Refs.ReadRefMod(capturedRefMod), name);
        }
        var call = ReparseArgs(sp) is { } args
            ? BindIntrinsicCore(name, args)
            : BoundExprError.Refused(ctx.Edition, $"FUNCTION {name} arguments");
        return tailRefMod is null
            ? call
            : ResultRefMod(call, ctx.Refs.ReadRefMod(tailRefMod), name);
    }


    private static string Name(DateTimeFormatKind k) => k switch
    {
        DateTimeFormatKind.Date => "DATE",
        DateTimeFormatKind.Time => "TIME",
        _ => "COMBINED date-and-time",
    };

    /// <summary>The §15.3 format kinds each format-taking function ADMITS — a SET, not one kind, because four
    /// of the seven rules name more than one.
    /// <para>⛔ THIS WAS A SINGLE VALUE AND THE CORPUS CAUGHT IT. Reading the three "sibling" functions by name
    /// analogy gave TEST-FORMATTED-DATETIME "combined", and §15.92.3 r2 actually says "either a date format, a
    /// time format, or a combined date and time format" — so an existing, legal corpus program
    /// (<c>2014/formatted_datetime</c>, <c>TEST-FORMATTED-DATETIME("YYYYMMDD" …)</c>) was rejected. Each row
    /// below is now the function's OWN rule, quoted:</para>
    /// <list type="bullet">
    ///   <item>§15.38.3 r2 FORMATTED-CURRENT-DATE — "shall be a combined date and time format"</item>
    ///   <item>§15.39.3 r2 FORMATTED-DATE — "shall be a date format"</item>
    ///   <item>§15.40.3 r2 FORMATTED-DATETIME — "shall be a combined date and time format"</item>
    ///   <item>§15.41.3 r2 FORMATTED-TIME — "shall be a time format"</item>
    ///   <item>§15.48.3 r2 INTEGER-OF-FORMATTED-DATE — "either a date format or a combined date and time format"</item>
    ///   <item>§15.79.3 r2 SECONDS-FROM-FORMATTED-TIME — "either a time format or a combined date and time format"</item>
    ///   <item>§15.92.3 r2 TEST-FORMATTED-DATETIME — "either a date format, a time format, or a combined …"</item>
    /// </list>
    /// Null for a function that takes no format argument.</summary>
    private static DateTimeFormatKind[]? FormatKindsAdmittedBy(string name) => name switch
    {
        "FORMATTED-DATE" => [DateTimeFormatKind.Date],
        "FORMATTED-TIME" => [DateTimeFormatKind.Time],
        "FORMATTED-CURRENT-DATE" or "FORMATTED-DATETIME" => [DateTimeFormatKind.Combined],
        "INTEGER-OF-FORMATTED-DATE" => [DateTimeFormatKind.Date, DateTimeFormatKind.Combined],
        "SECONDS-FROM-FORMATTED-TIME" => [DateTimeFormatKind.Time, DateTimeFormatKind.Combined],
        "TEST-FORMATTED-DATETIME" =>
            [DateTimeFormatKind.Date, DateTimeFormatKind.Time, DateTimeFormatKind.Combined],
        _ => null,
    };

    /// <summary>The ZERO-BASED position of the optional offset-from-UTC argument, for the two functions that take
    /// one, else null. FORMATTED-DATETIME's is argument-4 (§15.40.3 r5/r6) and FORMATTED-TIME's is argument-3
    /// (§15.41.3 r4/r5) — the same rule at different ordinals, which is exactly why the ordinal is a lookup and
    /// not a literal at the screen. ⚠ FORMATTED-CURRENT-DATE takes no offset argument at all (§15.38.2 gives it
    /// one operand), so it is absent here rather than mapped to a position it does not have.</summary>
    private static int? OffsetArgumentIndex(string name) => name switch
    {
        "FORMATTED-DATETIME" => 3,
        "FORMATTED-TIME" => 2,
        _ => null,
    };

    /// <summary>The clause each function's format-content rule lives in, for the diagnostic text.</summary>
    private static string FormatRuleCitation(string name) => name switch
    {
        "FORMATTED-CURRENT-DATE" => "ISO §15.38.3 r2",
        "FORMATTED-DATE" => "ISO §15.39.3 r2",
        "FORMATTED-DATETIME" => "ISO §15.40.3 r2",
        "FORMATTED-TIME" => "ISO §15.41.3 r2",
        "INTEGER-OF-FORMATTED-DATE" => "ISO §15.48.3 r2",
        "SECONDS-FROM-FORMATTED-TIME" => "ISO §15.79.3 r2",
        "TEST-FORMATTED-DATETIME" => "ISO §15.92.3 r2",
        _ => "ISO §15.3",
    };

    /// <summary>The D2 keyword-omitted argument re-parse: the SUBSCRIPT-mode captured argument text (verbatim
    /// from the source char stream, spacing intact) re-parses through the ONE <c>functionArgList</c> grammar
    /// rule via <see cref="FunctionArgFragment"/> — the same tokens, rule, and binding path as the
    /// FUNCTION-keyword form. Null (after a COBOLNET1543 diagnostic) on a malformed argument list.</summary>
    private IReadOnlyList<Core.FunctionArgumentContext>? ReparseArgs(Core.SubscriptPartContext sp)
    {
        if (sp.subscriptOrRefMod() is not { } som) return [];
        string text = som.Start.InputStream.GetText(
            new Antlr4.Runtime.Misc.Interval(som.Start.StartIndex, som.Stop.StopIndex));
        if (Frontend.Parsing.FunctionArgFragment.Parse(text, ctx.Edition.Edition, ctx.Retypes) is { } frag)
            return ArgsOf(frag.functionArgList());
        ctx.Edition.Error("COBOLNET1543", $"malformed function-argument list '({text})' — an argument is an "
            + "identifier, a literal, a boolean expression, or an arithmetic expression (ISO §8.4.3.2.3 SR8)");
        return null;
    }

    /// <summary>Bind one FUNCTION reference from its name + argument parse trees (shared by the FUNCTION-keyword
    /// entry, the keyword-omitted re-parse, and — through <c>BindExpr</c>'s <c>BindPrimary</c> — every nested
    /// FUNCTION recursion).</summary>
    private BoundExpr BindIntrinsicCore(string name, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        // >>COBOL-WORDS (ISO §7.3.10.4 GR2/GR3/GR4): an intrinsic-function-name synonym (EQUATE literal-2 /
        // SUBSTITUTE literal-5 whose canonical is an intrinsic) resolves to the canonical name; an intrinsic that
        // was UNDEFINE'd (literal-3) or SUBSTITUTE'd away (literal-4) is no longer a function. Only PURE
        // intrinsic-name synonyms reach here — reserved/context synonyms were already retyped by CobolWordsRewriter.
        // `cobolWordsRemoved` tests the ORIGINAL written name, so a SUBSTITUTE (literal-4 in DeReserved AND
        // literal-5→literal-4 in Synonyms) still resolves literal-5 to the intrinsic while literal-4 is removed.
        // ONE RULE, ONE PLACE: CobolWordsMap.Resolve (kb/Work PB250) — this used to be an inline copy.
        string? cwResolved = ctx.CobolWords.Resolve(name);
        bool cobolWordsRemoved = cwResolved is null;
        if (cwResolved is not null) name = cwResolved;

        // §12.3.8.2 GR12 (:14885): within the environment division's scope, a REPOSITORY-declared
        // function-prototype-name refers to the USER-DEFINED function "and not to an intrinsic function of
        // the same name" (the spec's own factorial-override example, :43651) — so the user-function
        // dispatch PRECEDES the catalog. §8.4.6.6 adds the CONTAINING function definition's own name with
        // no repository declaration (self-recursion; a present self-entry is ignored, §12.3.8 GR11).
        if (ctx.Data.UserFunctionNames.Contains(name)
            || name.Equals(host.UdfSelfName, StringComparison.OrdinalIgnoreCase))
            return host.Udf.UdfBindCall(name, argCtxs);

        // §8.4.3.2.2 — the function-identifier's THIRD name form, function-pointer-name-1 (SR4: "shall be defined
        // as a function-pointer data item"). It used to have no arm, so `FUNCTION FP (3)` reached the catalog and
        // was refused as "not an intrinsic function" while SET Format 8 happily filled FP (kb/Work PB847).
        if (FunctionPointerNamed(name) is { } fpItem)
            return host.Udf.UdfBindPointerCall(fpItem, argCtxs);

        if (cobolWordsRemoved || !IntrinsicCatalog.TryGet(name, out var sig))
        {
            bool definedInGroup = host.UserFunctions?.ContainsKey(name) == true;
            ctx.Edition.Error("COBOLNET1501", $"FUNCTION {name.ToUpperInvariant()} is not an intrinsic function "
                + "of ISO/IEC 1989 (§15.6 summary of functions)"
                + (definedInGroup
                    ? $"; the compilation group defines FUNCTION-ID {name.ToUpperInvariant()} — declare "
                      + $"FUNCTION {name.ToUpperInvariant()} in this unit's REPOSITORY paragraph to reference "
                      + "it as a user-defined function (ISO §12.3.8.2 GR12)"
                    : ""));
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {name}");
        }

        // D8 edition window: a function is rejected, BY NAME AND EDITION, outside [IntroducedIn, RemovedIn).
        // The 42-function 1989 Intrinsic Function Module rows carry IntroducedIn 85 — the amendment is part of
        // the CCVS-85 corpus, so --std 85 accepts them.
        // ⛔ A DOCUMENTED-NON-SUPPORT FUNCTION DOES NOT ASSERT AN INTRODUCTION EDITION (fix-queue PB27 §③).
        // COBOLNET1502 states a FACT about the standard — "was introduced by ISO/IEC 1989:{year}" — and for the
        // §A.4.9 locale family that year is HAND-ASSIGNED AND UNVERIFIABLE: the 2023 standard carries no
        // introduction record (§8.11 lists the intrinsic NAMES with no edition data; Annex E covers only
        // 2014→2023), the repo holds no 2002 or 2014 text, the reserved-word tables do not carry intrinsic
        // names, and GnuCOBOL's testsuite pins no `-std=` on these functions. Every source was checked.
        // ⚠ HISTORY: while these rows bound as documented non-support (pre-T4), COBOLNET1518 rejected the
        // reference at EVERY edition, 2023 included (measured), so the edition claim adds no actionable
        // information — no `--std` makes the program compile — while asserting something we cannot substantiate.
        // ⚠ IT SELF-LIFTS: the suppression keys on Bind, so implementing the locale module restores the gate —
        // at which point IntroducedIn would have to be verified anyway, which is the right forcing function.
        // ⚙ AND IT HAS NOW LIFTED ONCE, exactly as designed: STANDARD-COMPARE's Bind became Runtime at kb/Work
        // PB101 T7, so its 2002 window is enforced again (`standard-compare-2002` in constructs.json asserts the
        // 1502 at 85). The year was re-derived rather than inherited — ORDER is a 2002 reservation
        // (reserved-words.json r85 false / r2002 true) and the clause the function depends on cannot be written
        // below it; VERSION_CHANGE_REFERENCE records the derivation AND that Annex E cannot confirm it.
        // ⚙ AND LIFTED AGAIN AT kb/Work PB64 T4: the four LOCALE functions bind Runtime, so no catalog row is
        // Unsupported any more — the window below is enforced for every function.
        if (sig.IntroducedIn > ctx.Edition.DialectLevel)
            ctx.Edition.Error("COBOLNET1502", $"FUNCTION {sig.Name} was introduced by ISO/IEC 1989:{sig.IntroducedIn} "
                + $"(§15) — it requires --std {sig.IntroducedIn} or later (targeting COBOL-{ctx.Edition.DialectLevel})");
        else if (sig.RemovedIn is { } gone && ctx.Edition.DialectLevel >= gone)
            ctx.Edition.Error("COBOLNET1503", $"FUNCTION {sig.Name} was removed by ISO/IEC 1989:{gone} — "
                + $"it is not available when targeting COBOL-{ctx.Edition.DialectLevel}");

        // The former standard-arithmetic staging (P10 Step 12 → COBOLNET0899) is GONE (fix-queue PB56):
        // ANNUITY / PRESENT-VALUE / VARIANCE / STANDARD-DEVIATION now evaluate their §15.4.1 r1 equivalent
        // arithmetic expressions on the SDIDI carrier (CobolIntrinsics.Dec.cs, dispatched by
        // IntrinsicRenderer.RenderDec), so a standard-arithmetic program gets the standard-decimal values the
        // rule requires — the "until CobolDec evaluations land" condition the stage recorded is satisfied.

        // A.4.9 LOCALE MODULE — fully implemented (owner decision Q1, 2026-08-18; DESIGN-locale-facility §12):
        // the four LOCALE functions since T4 (BindLocaleFunction below), STANDARD-COMPARE since T7 (PB101), the
        // case functions' LOCALE phrase since T5, and NUMVAL-C / TEST-NUMVAL-C's LOCALE keyword since T6
        // (BindNumvalCFamily's r5 arm — §15.94.3 r1 imports every §15.68.3 rule). Nothing in §15 binds as
        // documented non-support any more; COBOLNET1518 and its LocaleUnsupported arm are GONE with the claim.
        // UPPER-CASE / LOWER-CASE `( argument-1 [ LOCALE locale-name-1 ] )` (§15.97.2 / §15.57.2; A.4.9 items 13 / 6)
        // — LIVE since kb/Work PB64 T5: the phrase's locale-name is a SPECIAL-NAMES LOCALE clause's name, and the
        // function maps case by that locale's LC_CTYPE (r2).
        if (sig.Name is "LOWER-CASE" or "UPPER-CASE" && HasLocalePhrase(argCtxs))
            return BindCaseFunctionWithLocale(sig, argCtxs);

        // STANDARD-COMPARE (§15.85) — argument-1 argument-2 [ordering-name-1] [argument-4]: the third position
        // is a §15.3 type-12 NAME (an ORDER TABLE ordering-name), not an operand, so the argument list is walked
        // positionally rather than bound as three operands (kb/Work PB101 T7).
        if (sig.Name == "STANDARD-COMPARE") return BindStandardCompare(sig, argCtxs);

        // LOCALE-COMPARE / LOCALE-DATE / LOCALE-TIME / LOCALE-TIME-FROM-SECONDS (§15.51–§15.54) — the optional
        // trailing position is a §15.3 type-8 LOCALE-NAME, not an operand (kb/Work PB64 T4).
        if (sig.ArgKinds.EndsWith('l')) return BindLocaleFunction(sig, argCtxs);

        // TRIM (§15.96) — the one §15 function whose argument list carries a phrase keyword (LEADING/TRAILING);
        // its bespoke shape is bound apart from the generic comma/space-split argument path.
        if (sig.Name == "TRIM") return BindTrim(sig, argCtxs);

        // FIND-STRING (§15.37) — argument-1 argument-2 [LAST] [[START AFTER] argument-3] [ANYCASE]: phrase
        // keywords interleaved with operands, bound apart from the generic argument path.
        if (sig.Name == "FIND-STRING") return BindFindString(sig, argCtxs);

        // SUBSTITUTE (§15.87) — argument-1 { [ANYCASE] [FIRST|LAST] argument-2 argument-3 } …: per-pair phrase
        // keywords ahead of each replacement pair, bound apart from the generic argument path.
        if (sig.Name == "SUBSTITUTE") return BindSubstitute(sig, argCtxs);

        // CONVERT (§15.19) — argument-1 source-format destination-format: two keyword groups after the operand,
        // bound apart from the generic argument path (the result category is computed per §15.19.1).
        if (sig.Name == "CONVERT") return BindConvert(sig, argCtxs);

        // MODULE-NAME (§15.65) — one mandatory keyword (ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL), resolved
        // from the runtime module call-name stack; bound apart from the generic argument path.
        if (sig.Name == "MODULE-NAME") return BindModuleName(sig, argCtxs);

        // FUNCTION LENGTH's optional PHYSICAL keyword (§15.50.2's general format:
        // `FUNCTION LENGTH ( argument-1 [ PHYSICAL ] )`). Bound apart from the generic argument path for the same
        // reason ANYCASE is: it is a KEYWORD, not an operand, and the generic path counts it as one — which is
        // exactly what it did, rejecting the conforming `FUNCTION LENGTH(WS-G PHYSICAL)` with
        // "COBOLNET1504: takes 1 argument(s); 2 given" (fix-queue PB24).
        if (sig.Name is "LENGTH" or "BYTE-LENGTH") return BindLengthFamily(sig, argCtxs);   // §15.50.2 / §15.14.2 — argument-1 [PHYSICAL]

        // NUMVAL-C / TEST-NUMVAL-C (§15.68.2 / §15.94.2) — the optional ANYCASE keyword (orthogonal to the
        // argument-2 currency; §15.94.3 r1 imports every §15.68.3 argument rule) + the §15.68.3 r3
        // compilation-unit currency injection; bound apart from the generic argument path. The LOCALE keyword is
        // LIVE (kb/Work PB64 T6 — BindNumvalCFamily's r5 arm); this line used to record it as the A.4.9
        // documented non-support, contradicting both the arm below and the accurate A.4.9 note above (PB250).
        if (sig.Name is "NUMVAL-C" or "TEST-NUMVAL-C") return BindNumvalCFamily(sig, argCtxs);

        // EXCEPTION-FILE / EXCEPTION-FILE-N with a file-connector-name argument (§15.28.4 r2 / §15.29.4 r2,
        // COBOL-2023 — E.3.3 items 25/26): the argument is an FD file-name (a file connector, NOT a data
        // reference — it reports the NAMED connector's I-O status); bound apart from the generic operand path.
        if (sig.Name is "EXCEPTION-FILE" or "EXCEPTION-FILE-N" && argCtxs.Count == 1)
            return BindExceptionFileArg(sig, argCtxs[0]);

        var args = BindIntrinsicArgs(sig, argCtxs);
        // Arity over the arguments as WRITTEN, with a table(ALL) argument counted as the elements it stands for
        // when its ranges are fixed and as the ONE argument §15.3 guarantees ("shall result in at least one
        // argument") when a range is a runtime value (kb/Work PB62 — the pre-expansion count used to make
        // `FUNCTION MOD(E(ALL) B)` read "takes 2 argument(s); 4 given" for a 3-element table, an incidental
        // rejection about a count the user never wrote; the §15.3 admissibility screen in TryBindAllArgument now
        // fires first, so an ALL only reaches here on a function whose MaxArgs is unbounded).
        long given = args.Sum(a => a is BoundFieldOperand { Place: TableAllPlace all } ? all.StaticCount ?? 1 : 1);
        if (given < sig.MinArgs || given > sig.MaxArgs)
        {
            ctx.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name} takes "
                + (sig.MinArgs == sig.MaxArgs ? $"{sig.MinArgs}" : sig.MaxArgs == int.MaxValue ? $"at least {sig.MinArgs}" : $"{sig.MinArgs}..{sig.MaxArgs}")
                + $" argument(s); {given} given (ISO §15.3)");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} arity");
        }

        // ISO §15.3 ARGUMENT-CLASS screen (fix-queue PB1). THE catalog-driven enforcement of every catalogued
        // function's argument rule — see IntrinsicArgumentRules for why this had to be written: sig.ArgKinds
        // declared the required class on all 79 rows and sig.ArgKind had ZERO callers, so `FUNCTION REVERSE` over
        // a numeric item and `FUNCTION ABS` over an alphanumeric one both compiled clean and produced garbage.
        // It sits HERE — after arity, before every per-function arm — so a new catalog row is screened the day it
        // is added rather than the day someone remembers to write its arm.
        CheckArgumentClasses(sig, args);

        // §15.38–15.41 / §15.48 / §15.79 / §15.92 rule 1: the date/time FORMAT (argument-1) shall be a LITERAL —
        // the format is analyzed/derived at compile time (SECONDS-FROM-FORMATTED-TIME needs the fraction scale).
        // §8.3.3.6.3 SR1 — "A figurative constant may be used whenever 'literal' appears in a format or when a
        // rule allows it": ALL "hh:mm:ss" IS a literal here (kb/Work PB58 / AR-15.79.3-1 — it was COBOLNET1517),
        // and in a length-unspecified context it takes the literal once (§8.3.3.6.4 GR3c), so it re-binds as
        // that string literal for every downstream format reader.
        bool formatFn = sig.Name is "FORMATTED-CURRENT-DATE" or "FORMATTED-DATE" or "FORMATTED-DATETIME" or "FORMATTED-TIME"
                or "INTEGER-OF-FORMATTED-DATE" or "SECONDS-FROM-FORMATTED-TIME" or "TEST-FORMATTED-DATETIME";
        if (formatFn && args.Count > 0 && args[0] is BoundAllLiteral allFmt)
            args[0] = new BoundStringLiteral(allFmt.Literal) { Category = OperandCategory(allFmt) ?? PicCategory.Alphanumeric };
        if (args.Count > 0 && args[0] is not BoundStringLiteral && formatFn)
            ctx.Edition.Error("COBOLNET1517", $"FUNCTION {sig.Name} argument-1 shall be a literal date/time format "
                + "(ISO §15 — the FORMATTED-*/INTEGER-OF-FORMATTED-DATE/SECONDS-FROM-FORMATTED-TIME/"
                + "TEST-FORMATTED-DATETIME format is a literal)");
        // §15.48.3 r3 — INTEGER-OF-FORMATTED-DATE's argument-2 "shall be a DATA ITEM of the same type as
        // argument-1": the class half is the schema's MatchArgument1; the data-item half is here (kb/Work PB58).
        if (sig.Name == "INTEGER-OF-FORMATTED-DATE" && args.Count > 1 && args[1] is not BoundFieldOperand)
            ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass, "FUNCTION INTEGER-OF-FORMATTED-DATE argument-2 "
                + "shall be a DATA ITEM of the same type as argument-1 (ISO §15.48.3 r3) — a literal or an expression is not admitted");

        // §15.38.3/§15.39.3/§15.40.3/§15.41.3/§15.48.3/§15.79.3/§15.92.3 rule 2 — the format's CONTENT
        // (fix-queue PB11). The literal screen above established only that argument-1 IS a literal; this asks
        // the question nothing asked before: is it one of the §15.3.1–§15.3.3 formats, and is it a kind THIS
        // function admits? Character-wise validation cannot answer that — every counter-example is assembled
        // from individually legal subfields.
        // §15.40.3 r6 / §15.41.3 r5 — the OFFSET ARGUMENT vs the format's ZONE (fix-queue PB11's value half).
        // "Argument-4 [argument-3] shall not be specified if the time portion of the format in argument-1 is
        // neither a UTC format nor an offset format." Decidable HERE because rule 1 above already established
        // argument-1 is a literal, so `Describe` knows the zone, and the argument's presence is syntactic.
        // ⚠ ONE-SIDED, deliberately: the CONVERSE is explicitly legal — omitting the argument for a UTC/offset
        // format "shall be evaluated as though 0 were specified" (§15.40.3 r7 / §15.41.3 r6), which the emitter
        // already does by passing hasOffset:false. Screening that too would reject conforming source.
        // Before this the argument bound cleanly and was then SILENTLY DISCARDED by a local format.
        if (args.Count > 0 && args[0] is BoundStringLiteral zoneFmt
            && OffsetArgumentIndex(sig.Name) is { } offIx && args.Count > offIx
            && DateTimeFormatGrammar.Describe(zoneFmt.Value, ctx.Data.DecimalPointIsComma) is { } zoneInfo
            && zoneInfo.Zone is DateTimeZone.Local)
            ctx.Edition.Error(DiagnosticCatalog.DateTimeOffsetArgumentNotPermitted,
                $"FUNCTION {sig.Name} is given an offset-from-UTC argument (argument-{offIx + 1}), but its format "
                + $"'{zoneFmt.Value}' has a LOCAL time portion — neither a UTC format (a trailing 'Z') nor an "
                + "offset format (an explicit '+hhmm' / '+hh:mm' subformat), ISO §15.3.3.4–§15.3.3.6. "
                + (sig.Name == "FORMATTED-DATETIME" ? "§15.40.3 r6" : "§15.41.3 r5")
                + " bars the argument there. Use a UTC or offset format, or drop the argument");

        if (args.Count > 0 && args[0] is BoundStringLiteral fmt
            && FormatKindsAdmittedBy(sig.Name) is { } admitted)
        {
            var actual = DateTimeFormatGrammar.Classify(fmt.Value, ctx.Data.DecimalPointIsComma);
            if (actual is null || Array.IndexOf(admitted, actual.Value) < 0)
                ctx.Edition.Error(DiagnosticCatalog.DateTimeFormatKindMismatch,
                    $"FUNCTION {sig.Name} argument-1 is '{fmt.Value}', which is "
                    + (actual is null
                        ? "not a date, time or combined date-and-time format at all (ISO §15.3.1.1 / §15.3.2 / "
                          + "§15.3.3.7 — note that basic and extended forms never mix)"
                        : $"a {Name(actual.Value)} format; this function admits "
                          + string.Join(" or ", admitted.Select(Name)))
                    + $" ({FormatRuleCitation(sig.Name)}).");
        }

        // FUNCTION LENGTH folds at compile time from PIC metadata (§15.50; deep-dive D7).
        if (sig.Bind == IntrinsicBind.Fold && sig.Name == "LENGTH")
            return BindLengthFold(sig, args);

        // FUNCTION BYTE-LENGTH folds at compile time from the item's declared BYTE geometry (§15.14; the D7
        // byte-vs-position twin of LENGTH).
        if (sig.Bind == IntrinsicBind.Fold && sig.Name == "BYTE-LENGTH")
            return BindByteLengthFold(sig, args);

        // SMALLEST/HIGHEST/LOWEST-ALGEBRAIC fold at compile time from the argument item's PICTURE metadata
        // (§15.83/§15.43/§15.58; the same Fold discipline as LENGTH).
        if (sig.Bind == IntrinsicBind.Fold
                && sig.Name is "SMALLEST-ALGEBRAIC" or "HIGHEST-ALGEBRAIC" or "LOWEST-ALGEBRAIC")
            return BindAlgebraicFold(sig, args);

        // ⛔ THE ONE RESULT-TYPE RESOLUTION (§15.x.1 result-type tables; fix-queue PB15). Twenty functions have a
        // type that DEPENDS ON THEIR ARGUMENTS, and this used to be two hand-written name lists here — a
        // `RuntimeMethod is "UpperCase" or "LowerCase" or "Reverse"` chain (CA25) and a `Name is "MAX" or "MIN"`
        // arm (V54) — so the other TEN functions silently kept the catalog's hardcoded Alphanumeric. The rule now
        // lives on the catalog ROW (IntrinsicSig.Result) and is read HERE and nowhere else; adding a function is
        // a column, not an edit to this method. IntrinsicResultTypeDriftTests re-derives the population from
        // specs/ISO_COBOL.md so a new §15.x.1 table cannot be missed.
        var category = IntrinsicSig.CategoryOf(IntrinsicResultType.Resolve(sig, args));

        // ⚠ SEPARATE CONCERN, DELIBERATELY NOT MERGED ABOVE: which runtime BODY compares the arguments.
        // MAX/MIN/ORD-MAX/ORD-MIN over string arguments dispatch to the string comparison body; ORD-MAX/ORD-MIN
        // still return an ordinal (§15.71.1/§15.72.1 carry NO result-type table — only MAX/MIN do), which is why
        // this is a RuntimeMethod choice and not a type one, and why the two must not be folded together.
        // ⛔ A CLASS-NEUTRAL ARGUMENT MUST NOT VOTE (fix-queue PB48). This read `args.All(IsStringOperand)`, which
        // silently counted the figurative ZERO as a NON-string argument and so forced the numeric body on a list
        // that was alphanumeric: `FUNCTION MAX(ZERO "A")` returned "0" — a WRONG ANSWER, since §15.59.4 r1 takes
        // the greatest by §8.8.4.2 relation rules and "A" (65) exceeds "0" (48) — and `FUNCTION MAX(SPACE "A")`,
        // two plainly alphanumeric arguments, drove SPACE into the numeric channel and aborted at RUN TIME.
        // §15.59.3 r2 requires all arguments to be of the SAME class, and §8.3.3.6.4 GR4 says ZERO takes its class
        // from the context — so the non-neutral arguments are the context, and they are what chooses the body.
        // With every argument neutral (`FUNCTION MAX(ZERO ZERO)`) there is no such context and the numeric reading
        // stands, which is both GR4's first-listed value and §8.8.1.1's.
        var resolved = sig;
        if (sig.ArgKinds == "p" && args.Count > 0
            && args.All(a => IsStringOperand(a) || IsClassNeutralOperand(a))
            && args.Any(IsStringOperand))
        {
            resolved = sig with
            {
                RuntimeMethod = sig.Name switch
                {
                    "MAX" => "MaxString", "MIN" => "MinString",
                    "ORD-MAX" => "OrdMaxString", _ => "OrdMinString",
                },
            };
        }

        // CHAR/ORD are PCS-relative (§15.15.4 r2 / §15.70.4 r1): flag the call when a NON-identity ALPHANUMERIC
        // program collating sequence is in effect so the backend passes its weights table — and only then
        // (hazard H5: the emitted __COLLATE field exists only under a non-identity PCS; STANDARD-1/2/NATIVE
        // normalize to identity). A NATIONAL ORD argument reads the NATIONAL program collating sequence instead
        // (§15.70.4 r2), and CHAR-NATIONAL always does (§15.16.4) — so the alphanumeric weights table must NOT
        // be passed for those (its 256-entry domain would alias national characters): they take the H5-twin
        // CollateNat flag, set only under a NON-native national sequence (an ALPHABET … FOR NATIONAL literal
        // phrase — NATIVE/UCS-4 are the D-N3 code-unit identity the parameterless runtime bodies realize).
        bool nationalArg = args.Any(a => OperandCategory(a) is PicCategory.National);
        // MAX/MIN/ORD-MAX/ORD-MIN over string arguments compare by the SAME PCS as CHAR/ORD (§15.59.4 r1 → §8.8.4.2.7
        // alphanumeric / §8.8.4.2.9 national) — flag the STRING form (resolved above) so the backend passes __COLLATE /
        // __COLLATE_NAT, else it silently uses the native ordinal and disagrees with the program's relation conditions. (CA23.)
        bool maxMinStr = resolved.RuntimeMethod is "MaxString" or "MinString" or "OrdMaxString" or "OrdMinString";
        bool collate = (sig.Name is "CHAR" or "ORD" || maxMinStr) && ctx.Data.Collating is not null && !nationalArg;
        bool collateNat = ctx.Data.NationalCollating is not null
            && (sig.Name is "CHAR-NATIONAL" || ((sig.Name is "ORD" || maxMinStr) && nationalArg));

        // CHAR takes an INTEGER argument (§15.15.3 r1) — a national operand is a category violation; the
        // national ordinal→character direction is FUNCTION CHAR-NATIONAL (§15.16, implemented — the P10
        // Step-11 EC-N wave). Rejected BY NAME at bind — never a wrong ordinal through the numeric channel.
        if (sig.Name is "CHAR" && nationalArg)
            ctx.Edition.Error("COBOLNET0844", "FUNCTION CHAR takes an integer operand (§15.15.3 rule 1) — "
                + "FUNCTION CHAR-NATIONAL (§15.16) is the national program-collating-sequence form");

        // DISPLAY-OF / NATIONAL-OF (§15.26.3 / §15.66.3) — the argument class/category rules of the sanctioned
        // national↔alphanumeric repertoire pair.
        if (sig.Name is "DISPLAY-OF" or "NATIONAL-OF") CheckRepertoireArgs(sig, args);
        // BASECONVERT (§15.12.3 r1) — the bind-time screen family (PB59 / AR-15.12.3-1 legs (b)–(f)).
        if (sig.Name is "BASECONVERT") CheckBaseConvertArgs(args);

        // A FUNCTION EXCEPTION-* reference reads the runtime last-exception register (§15.28–15.33) — flag the
        // program's EC usage so the generated source carries the Exceptions using (the group EC gate).
        if (resolved.RuntimeMethod.StartsWith("Ec", StringComparison.Ordinal)) host.Ec.EcNoteFunction();

        // §15.18.4 r3 — CONCAT returns an ALPHABETIC value when argument-1 is usage display and argument-1 and
        // all argument-2 are of class alphabetic (PB59 family 7 / RV-15.18.4-3). Class alphabetic is a PIC A
        // DATA ITEM — §8.3.3 (to which §8.5.2.1 delegates a literal's class) gives no literal that class, a ref-mod view is class alphanumeric (§8.4.3.3.4
        // GR2/GR6), and a nested CONCAT contributes through its own rider — and every PIC A item is usage
        // display, so all-alphabetic subsumes r3's usage-display precondition. Fail-soft: any shape not
        // provably alphabetic keeps r3's "otherwise" arm (alphanumeric).
        bool alphabetic = resolved.Name == "CONCAT" && args.Count > 0 && args.All(IsAlphabeticArg);

        return new BoundIntrinsicCall(resolved, args, category, collate)
            { CollateNat = collateNat, ResultIsAlphabetic = alphabetic };
    }

    /// <summary>Is this argument provably CLASS ALPHABETIC (§8.5.2.1 Table 2 — category alphabetic, the PIC A
    /// shape)? A ref-mod view never is (§8.4.3.3.4 GR2/GR6 make every view plain alphanumeric); a nested
    /// CONCAT is exactly when its own §15.18.4 r3 rider says so.</summary>
    private static bool IsAlphabeticArg(BoundOperand op) => op switch
    {
        BoundFieldOperand { Place: RefModPlace } => false,
        BoundFieldOperand f => f.Place.Item.Pic is { IsAlphabetic: true },
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultIsAlphabetic: true } } => true,
        _ => false,
    };

    /// <summary>Bind FUNCTION EXCEPTION-FILE(file-connector-name) / EXCEPTION-FILE-N(...) (§15.28.4 r2 / §15.29.4 r2,
    /// COBOL-2023, E.3.3 items 25/26): argument-1 is an FD file-name (a file connector, §15.28.3 rule 1) resolved to
    /// its <see cref="FileModel"/>; the runtime reports that connector's I-O status + SELECT-spelled name (r2b), or
    /// two spaces when it was never opened/attempted/accessed (r2a). Introduction-gated at 2023 (COBOLNET0900).</summary>
    private BoundExpr BindExceptionFileArg(IntrinsicSig sig, Core.FunctionArgumentContext argCtx)
    {
        ConstructRegistry.Check(ctx.Edition.Edition, ctx.Edition.Sink,
            sig.Name == "EXCEPTION-FILE" ? Constructs.ExceptionFileArgument2023 : Constructs.ExceptionFileNArgument2023,
            $"FUNCTION {sig.Name}(file-connector-name)");
        string name = argCtx.GetText().Trim();
        // §15.28.3 r1 / §15.29.3 r1 (word for word): "Argument-1 is optional and when specified shall be the name of
        // a file connector that is specified in an FD statement." BOTH halves (kb/Work PB63): the name must be a
        // file-name, and that file must be FD-described — an SD (a sort-merge file description) or a SELECT with no
        // description entry at all fails the second half; the resolver used to match on the bare name only, so
        // both compiled clean and answered r2a's two spaces. The clause cited is the function's OWN.
        string clause = sig.Name == "EXCEPTION-FILE" ? "§15.28.3 rule 1" : "§15.29.3 rule 1";
        // Resolve through FilesByName — the ONE file-name resolution every other site uses (kb/Work PB123:
        // this arm scanned ctx.Data.Files, the program's OWN FD list, so a contained program naming its
        // container's GLOBAL FD — visible per §13.18.30 / §13.18.27 GR1–2, merged into FilesByName by
        // BinderDriver — drew "not the name of a file connector" on legal source).
        if (!ctx.Data.FilesByName.TryGetValue(name, out var file))
        {
            ctx.Edition.Error(DiagnosticCatalog.ExceptionFileArgumentNotFile, $"FUNCTION {sig.Name} argument '{name}' "
                + $"is not the name of a file connector specified in an FD statement (ISO {clause})");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} argument");
        }
        if (file.IsSortMerge || !file.HasFd)
        {
            ctx.Edition.Error(DiagnosticCatalog.ExceptionFileArgumentNotFile, $"FUNCTION {sig.Name} argument '{name}' "
                + (file.IsSortMerge ? "names a sort-merge file (an SD entry)" : "names a SELECTed file that has no FD entry")
                + $" — argument-1 shall be the name of a file connector specified in an FD statement (ISO {clause})");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} argument");
        }
        // §13.4.5.3 SR9: a report file's name "may be referenced in the procedure division only by" USE, PERFORM's
        // WHEN phrase, CLOSE and OPEN OUTPUT / EXTEND — a function argument is none of them (kb/Work PB1171).
        if (!ctx.Validation.ScreenReportFileReference(file, $"FUNCTION {sig.Name}"))
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} argument");
        host.Ec.EcNoteFunction();
        return new BoundIntrinsicCall(sig, [], sig.ResultCategory) { FileArg = file };
    }

    /// <summary>
    /// STANDARD-COMPARE (§15.85.2) — <c>FUNCTION STANDARD-COMPARE ( argument-1 argument-2 [ ordering-name-1 ]
    /// [ argument-4 ] )</c>: two string operands, then a NAME and an integer, bound apart from the generic
    /// argument path because ordering-name-1 is a §15.3 argument type 12 — "An ordering-name defined in the
    /// SPECIAL-NAMES paragraph shall be specified" — not an operand at all. The resolved literal-9 rides
    /// <see cref="BoundIntrinsicCall.OrderingTable"/>; a call with no ordering-name leaves it null, which
    /// §15.85.3 r5 defines as the default table 'ISO 14651_2020_TABLE1'.
    /// </summary>
    /// <remarks>
    /// ⛔ THE WALK IS POSITIONAL, AND THE THREE-ARGUMENT FORM IS THE ONLY AMBIGUOUS ONE. §15.85.2 prints two
    /// bracketed optionals in a fixed order with no choice indicators, so §5.2.6 makes them positional: with FOUR
    /// arguments written, position 3 IS ordering-name-1 and position 4 IS argument-4, and a position-3 operand
    /// that is not a declared ordering-name is a form violation (COBOLNET1663). With THREE, position 3 is either
    /// one, and only §15.85.3 can decide — r5's "shall be associated with a cultural ordering table in the ORDER
    /// TABLE clause" versus r6's "positive nonzero integer" — so a bare word that names a declared ordering-name
    /// is that name and anything else is argument-4.
    /// <para>⚠ A BARE WORD IS NOT AUTOMATICALLY A NAME. §15.3 type 6 admits "an integer data item", so
    /// <c>FUNCTION STANDARD-COMPARE(A B WS-LEVEL)</c> is legal COBOL with WS-LEVEL a data-name; keying the
    /// ordering-name arm on "is a bare word" rather than on "is a DECLARED ordering-name" would reject it.</para>
    /// </remarks>
    private BoundExpr BindStandardCompare(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        BoundExpr Malformed(string why)
        {
            ctx.Edition.Error(DiagnosticCatalog.StandardCompareArgument, $"FUNCTION STANDARD-COMPARE {why}");
            return BoundExprError.Refused(ctx.Edition, "FUNCTION STANDARD-COMPARE");
        }

        if (argCtxs.Count is < 2 or > 4)
        {
            ctx.Edition.Error("COBOLNET1504", $"FUNCTION STANDARD-COMPARE takes 2..4 argument(s); "
                + $"{argCtxs.Count} given (ISO §15.85.2)");
            return BoundExprError.Refused(ctx.Edition, "FUNCTION STANDARD-COMPARE arity");
        }

        string? orderingTable = null;
        var operands = new List<BoundOperand>();
        for (int at = 0; at < argCtxs.Count; at++)
        {
            var a = argCtxs[at];
            if (at < 2) { operands.Add(BindArgOperand(a)); continue; }   // argument-1 / argument-2 (r1/r2)
            // The ordering-name slot: position 3 always when four arguments are written, position 3 in the
            // three-argument form only when the word actually names an ORDER TABLE clause's ordering-name.
            bool mustBeName = at == 2 && argCtxs.Count == 4;
            if (at == 2 && KeywordWordOf(a) is { } word && ctx.Data.OrderTables.TryGetValue(word, out string? literal9))
            {
                orderingTable = literal9;
                continue;
            }
            if (mustBeName)
                return Malformed($"argument-3 '{argCtxs[2].GetText().Trim()}' is not an ordering-name declared in an "
                    + "ORDER TABLE clause of the SPECIAL-NAMES paragraph — with four arguments written, the "
                    + "§15.85.2 general format's third position IS ordering-name-1 (ISO §15.85.3 r5; §15.3 "
                    + "argument type 12)"
                    + (ctx.Data.OrderTables.Count == 0
                        ? "; this compilation unit declares no ORDER TABLE clause"
                        : $"; declared: {string.Join(", ", ctx.Data.OrderTables.Keys)}"));
            // A SECOND ordering-name where argument-4 belongs. Reachable, and worth its own message: the operand
            // path would otherwise report it as an unresolved data-name, which is true and useless.
            if (KeywordWordOf(a) is { } second && ctx.Data.OrderTables.ContainsKey(second))
                return Malformed($"is given the ordering-name '{second}' in the argument-4 position — the "
                    + "§15.85.2 general format admits ONE [ ordering-name-1 ], and the fourth position is "
                    + "argument-4, the ordering level (ISO §15.85.3 r6)");
            var level = BindArgOperand(a);
            // §15.85.3 r6 — "Argument-4, if specified, shall be a positive nonzero integer." Decidable here only
            // for a LITERAL; a data item's value is a run-time fact, and §15.85.4 r2 owns the outcome of a level
            // the ordering table does not define (EC-ORDER-NOT-SUPPORTED).
            // Read through THE ONE integer-literal reader (kb/Work PB1579): a long.TryParse refused a 20-to-31-digit
            // POSITIVE level as "not a positive nonzero integer" — a false sentence; the level beyond the table's
            // highest is §15.85.4 r2's run-time outcome.
            if (level is BoundNumericLiteral { Text: { } text }
                && (!CobolNet.Validation.IntegerOperandRules.TryHostValue(text, out int n) || n <= 0))
                return Malformed($"argument-4 is {text}, which is not a positive nonzero integer "
                    + "(ISO §15.85.3 r6) — the ordering level is 1 (primary) through the highest level the "
                    + "ordering table defines");
            operands.Add(level);
        }
        // The §15.3 screen — see the PB12 note on the FIND-STRING arm: this binder returns before the generic
        // path reaches CheckArgumentClasses, so it screens its own operand list (§15.85.3 r1/r2/r4/r6).
        CheckArgumentClasses(sig, operands);
        // §15.85.1: "The function type is alphanumeric" — a FIXED result rule, so the row's category is the
        // answer and there is no §15.x.1 table to resolve (the PB15 hazard does not arise here).
        return new BoundIntrinsicCall(sig, operands, sig.ResultCategory) { OrderingTable = orderingTable };
    }

    /// <summary>The LOCALE functions (§15.51 LOCALE-COMPARE, §15.52 LOCALE-DATE, §15.53 LOCALE-TIME, §15.54
    /// LOCALE-TIME-FROM-SECONDS; DESIGN-locale-facility §4.7/§4.8, kb/Work PB64 T4): <c>( argument… [ locale-name-1 ] )</c>
    /// — the leading positions bind as operands (their §15.x.3 class/width rules screened by the row's Verified
    /// schema), and the optional LAST position is a LOCALE-NAME (§15.3 argument type 8; §15.51.3 r4 / §15.52.3 r3 /
    /// §15.53.3 r4 / §15.54.3 r2 — "shall be associated with a locale in the SPECIAL-NAMES paragraph"), resolved in
    /// the SPECIAL-NAMES locale table to the ONE <see cref="LocaleRef"/>. A word in that position that is not a
    /// declared locale-name is COBOLNET1664 (the one undeclared-locale-name diagnostic, citing the rule); a
    /// non-word there is a form violation (the position admits only a name). Absent, the locale current at use
    /// applies (§14.6.6 r7/r8).</summary>
    private BoundExpr BindLocaleFunction(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        int operandCount = sig.ArgKinds.Length - 1;   // the positions before the trailing 'l'
        if (argCtxs.Count < operandCount || argCtxs.Count > operandCount + 1)
        {
            ctx.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name} takes {operandCount}..{operandCount + 1} argument(s); "
                + $"{argCtxs.Count} given (ISO §{LocaleFunctionClause(sig.Name)}.2)");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} arity");
        }
        var operands = new List<BoundOperand>(operandCount);
        for (int at = 0; at < operandCount; at++) operands.Add(BindArgOperand(argCtxs[at]));
        var locale = LocaleRef.Current;
        if (argCtxs.Count > operandCount)
        {
            var a = argCtxs[operandCount];
            string rule = sig.Name switch
            {
                "LOCALE-COMPARE" => "ISO §15.51.3 r4 — locale-name-1 shall be associated with a locale in the SPECIAL-NAMES paragraph",
                "LOCALE-DATE" => "ISO §15.52.3 r3 — locale-name-1 shall be associated with a locale in the SPECIAL-NAMES paragraph",
                "LOCALE-TIME" => "ISO §15.53.3 r4 — locale-name-1 shall be associated with a locale in the SPECIAL-NAMES paragraph",
                _ => "ISO §15.54.3 r2 — locale-name-1 shall be associated with a locale in the SPECIAL-NAMES paragraph",
            };
            string site = $"FUNCTION {sig.Name}'s locale-name-1 '{a.GetText().Trim()}'";
            if (KeywordWordOf(a) is { } word)
            {
                var sym = ctx.Data.ResolveLocaleName(word, site, rule);
                if (sym is null) return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name}");
                locale = new LocaleRef(sym);
            }
            else
            {
                ctx.Edition.Error("COBOLNET1664", $"{site}: the position after the operand(s) admits only a locale-name declared by a "
                    + $"SPECIAL-NAMES LOCALE clause ({rule}; the general format is §{LocaleFunctionClause(sig.Name)}.2)");
                return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name}");
            }
        }
        // The §15.3 screen — this binder returns before the generic path reaches CheckArgumentClasses, so it
        // screens its own operand list (the row's Verified schema: classes, the 8/6-position widths).
        CheckArgumentClasses(sig, operands);
        // §15.51.1–§15.54.1: "The function type is alphanumeric" — a FIXED result rule. The LENGTH of the
        // date/time results is run-time-determined (§15.52.4 r3 / §15.53.4 r3 / §15.54.4 r3) — the returned
        // CobolString carries its own length, and the receiving context (a MOVE, a reference modification) bounds
        // it at run time, exactly as every dynamic-length result already does.
        return new BoundIntrinsicCall(sig, operands, sig.ResultCategory) { Locale = locale };
    }

    /// <summary>UPPER-CASE / LOWER-CASE with the LOCALE phrase (§15.97.2 / §15.57.2 — <c>( argument-1 [ LOCALE
    /// locale-name-1 ] )</c>; kb/Work PB64 T5): argument-1 binds as the operand (§15.97.3 r1 / §15.57.3 r1 through the
    /// row's Verified schema), the keyword LOCALE must be followed by exactly one locale-name declared by a
    /// SPECIAL-NAMES LOCALE clause (COBOLNET1664 otherwise — the ONE undeclared-locale-name diagnostic), which rides
    /// the bound node's <see cref="BoundIntrinsicCall.Locale"/>.</summary>
    private BoundExpr BindCaseFunctionWithLocale(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        string rule = sig.Name == "UPPER-CASE" ? "ISO §15.97.2 / §15.97.4 r2 — LOCALE locale-name-1, a locale-name of the SPECIAL-NAMES LOCALE clause"
            : "ISO §15.57.2 / §15.57.4 r2 — LOCALE locale-name-1, a locale-name of the SPECIAL-NAMES LOCALE clause";
        if (argCtxs.Count != 3 || KeywordWordOf(argCtxs[1]) != "LOCALE")
        {
            ctx.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name} takes argument-1 optionally followed by LOCALE locale-name-1 "
                + $"({rule}); {argCtxs.Count} argument(s) given");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name}");
        }
        // The phrase is a 2002 introduction (the function is 1985): the construct gate, like EXCEPTION-FILE's argument.
        ConstructRegistry.Check(ctx.Edition.Edition, ctx.Edition.Sink, Constructs.CaseFunctionLocalePhrase2002,
            $"the LOCALE phrase of FUNCTION {sig.Name}");
        var operand = BindArgOperand(argCtxs[0]);
        LocaleRef locale;
        if (KeywordWordOf(argCtxs[2]) is { } word)
        {
            var sym = ctx.Data.ResolveLocaleName(word, $"FUNCTION {sig.Name}'s LOCALE phrase '{word}'", rule);
            if (sym is null) return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name}");
            locale = new LocaleRef(sym);
        }
        else
        {
            ctx.Edition.Error("COBOLNET1664", $"FUNCTION {sig.Name}'s LOCALE phrase: the word after LOCALE shall be a locale-name declared by a "
                + $"SPECIAL-NAMES LOCALE clause ({rule}), not '{argCtxs[2].GetText().Trim()}'");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name}");
        }
        var operands = new List<BoundOperand> { operand };
        CheckArgumentClasses(sig, operands);
        return new BoundIntrinsicCall(sig, operands, IntrinsicSig.CategoryOf(IntrinsicResultType.Resolve(sig, operands))) { Locale = locale };   // §15.97.1 / §15.57.1 — the type follows argument-1
    }

    private static string LocaleFunctionClause(string name) => name switch
    {
        "LOCALE-COMPARE" => "15.51",
        "LOCALE-DATE" => "15.52",
        "LOCALE-TIME" => "15.53",
        _ => "15.54",
    };

    /// <summary>TRIM (§15.96) — a phrase keyword in the argument list (<c>[LEADING|TRAILING]</c>, §15.96.2),
    /// extracted here (the <see cref="KeywordWordOf"/> bare-word view) so the remaining arguments bind as
    /// ordinary operands: argument-1 (the string, required) followed by zero-or-more single-character
    /// argument-2 trim characters (§15.96.3 rule 2 — the default when none is given is a space, rule 3.a).
    /// The LEADING/TRAILING mode rides on the bound node's <see cref="BoundIntrinsicCall.TrimMode"/>
    /// (0 = both, rule 3).</summary>
    private BoundExpr BindTrim(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        int mode = 0;   // 0 = both leading+trailing (r3), 1 = LEADING (r1), 2 = TRAILING (r2)
        bool sawKeyword = false, sawAll = false;
        var operands = new List<BoundOperand>();
        BoundExpr Malformed(string why)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION TRIM takes argument-1 [LEADING|TRAILING] "
                + $"[argument-2]…, in that order (ISO §15.96.2) — {why}");
            return BoundExprError.Refused(ctx.Edition, "FUNCTION TRIM");
        }
        foreach (var a in argCtxs)
        {
            switch (KeywordWordOf(a))
            {
                // ⛔ A POSITIONAL WALK OVER THE §15.96.2 FORMAT (kb/Work PB124 — AR-15.3-7/FMT-15.96.2): the
                // [LEADING|TRAILING] group sits AFTER argument-1 and BEFORE every argument-2, at most once;
                // the old order-free switch took the keyword ANYWHERE and repeatedly.
                case ("LEADING" or "TRAILING") and var kw:
                    if (operands.Count == 0) return Malformed($"{kw} precedes argument-1");
                    if (sawKeyword) return Malformed("the [LEADING|TRAILING] group appears more than once");
                    if (operands.Count > 1 || sawAll) return Malformed($"{kw} follows an argument-2");
                    mode = kw == "LEADING" ? 1 : 2;
                    sawKeyword = true;
                    continue;
            }
            // §15.96.2's `[ argument-2 ] …` repeats argument-2, so a table(ALL) is admissible here (§15.3) — one
            // enumerating operand, exactly as on the generic path (kb/Work PB62); it used to fall to the plain
            // subscript path and throw at run time.
            if (TryBindAllArgument(sig, a, operands)) { sawAll = true; continue; }
            operands.Add(BindArgOperand(a));
        }
        if (operands.Count == 0)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION TRIM takes at least a string argument-1 (ISO §15.96.2/.3)");
            return BoundExprError.Refused(ctx.Edition, "FUNCTION TRIM");
        }
        // The argument-2 form (delete characters OTHER than space) is a 2023 enhancement — TRIM removed only
        // spaces through 2014 (Annex E.3.3 item 31; VERSION_CHANGE_REFERENCE row 74). Its introduction is gated
        // by name+edition, like the whole-function 1502 gate above. A table(ALL) argument is the SAME form
        // whatever its element count (kb/Work PB124 — FMT-15.96.2's edition-gate hole: ALL enumerates a
        // variable argument list, which only the 2023 format has; the old `> 1` gate counted the enumeration
        // as ONE operand and let `TRIM(ALL T)` through at 2014).
        if ((operands.Count > 1 || sawAll) && ctx.Edition.DialectLevel < 2023)
            ctx.Edition.Error("COBOLNET1502", "the FUNCTION TRIM argument-2 form (removing characters other than "
                + "space) was introduced by ISO/IEC 1989:2023 (§15.96; Annex E.3.3 item 31) — it requires "
                + $"--std 2023 or later (targeting COBOL-{ctx.Edition.DialectLevel}); TRIM removed only spaces through 2014");
        // The §15.3 screen — see the PB12 note on the FIND-STRING arm: this binder returns before the
        // generic path reaches CheckArgumentClasses, so it screens its own operand list.
        CheckArgumentClasses(sig, operands);
        // ⛔ THE §15.96.1 RESULT-TYPE TABLE APPLIES HERE TOO, AND THIS SITE IS WHY PB15 NEEDED FINDING TWICE.
        // TRIM has a BESPOKE bind (the LEADING/TRAILING phrase), so it builds its own node and NEVER reaches the
        // generic path's resolution — hardcoding Alphanumeric here left `MOVE FUNCTION TRIM(national) TO PIC X`
        // compiling clean even with the catalog row correct. The two-arm dispatch, in its silent form: the fix
        // present, the defect intact, every test green. Both arms now read the ONE rule.
        return new BoundIntrinsicCall(
            sig, operands, IntrinsicSig.CategoryOf(IntrinsicResultType.Resolve(sig, operands)))
            { TrimMode = mode };
    }

    /// <summary>FIND-STRING (§15.37) — <c>argument-1 argument-2 [LAST] [[START AFTER] argument-3] [ANYCASE]</c>
    /// (§15.37.2). The phrase words arrive as bare-word arguments (<see cref="KeywordWordOf"/>): LAST selects the
    /// last occurrence (rule 1); ANYCASE folds case (rule 4); START/AFTER are the optional argument-3 introducer
    /// (noise — argument-3's mere presence selects the skip form, rule 2). The operand arguments are argument-1
    /// (the haystack), argument-2 (the needle), and the optional integer argument-3 (the number of matches to
    /// ignore). The two flags ride on <see cref="BoundIntrinsicCall.FindLast"/> /
    /// <see cref="BoundIntrinsicCall.Anycase"/>; argument-3 (if given) is the third operand.</summary>
    private BoundExpr BindFindString(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        bool last = false, anycase = false;
        var operands = new List<BoundOperand>();
        BoundExpr Malformed(string why)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION FIND-STRING takes argument-1 argument-2 [LAST] "
                + $"[[START AFTER] argument-3] [ANYCASE], in that order (ISO §15.37.2) — {why}");
            return BoundExprError.Refused(ctx.Edition, "FUNCTION FIND-STRING");
        }
        // ⛔ A POSITIONAL WALK OVER THE §15.37.2 FORMAT, not an order-free switch (kb/Work R20 — ledger F28).
        // The old walk accepted the phrase words ANYWHERE, REPEATED, and — the dangerous case — a dangling
        // `START AFTER` with no argument-3, where the two written words were silently DISCARDED and the call
        // degraded to the plain two-argument form. The general format fixes order and multiplicity; each slot
        // below is one bracket of it. (BindSubstitute/BindConvert already walk positionally — this was the
        // last order-free arm.)
        //   slot 0/1 = argument-1/-2 · 2 = [LAST] open · 3 = LAST taken · 4 = START seen · 5 = AFTER seen ·
        //   6 = argument-3 taken · 7 = ANYCASE taken (nothing may follow)
        int slot = 0;
        foreach (var a in argCtxs)
        {
            switch (KeywordWordOf(a))
            {
                case "LAST":
                    if (slot < 2) return Malformed("LAST precedes the two operand arguments");
                    if (slot > 2) return Malformed(last ? "LAST is repeated" : "LAST follows a later phrase");
                    last = true; slot = 3; continue;
                case "START":
                    if (slot is not (2 or 3)) return Malformed(slot < 2
                        ? "START precedes the two operand arguments" : "START follows a later phrase");
                    slot = 4; continue;
                case "AFTER":
                    if (slot != 4) return Malformed("AFTER without an immediately preceding START");
                    slot = 5; continue;
                case "ANYCASE":
                    if (slot < 2) return Malformed("ANYCASE precedes the two operand arguments");
                    if (slot == 7) return Malformed("ANYCASE is repeated");
                    if (slot is 4 or 5) return Malformed("START AFTER is not followed by argument-3");
                    anycase = true; slot = 7; continue;
            }
            if (slot == 4) return Malformed("START is not followed by AFTER");
            if (slot == 7) return Malformed("an argument follows ANYCASE, which the format places last");
            operands.Add(BindArgOperand(a));
            slot = slot switch { 0 => 1, 1 => 2, _ => 6 };   // arg-1 → arg-2 → argument-3 (bare or after START AFTER)
        }
        if (slot is 4 or 5)
            return Malformed(slot == 4 ? "START is not followed by AFTER"
                : "START AFTER is not followed by argument-3 — the two written words would be discarded");
        if (operands.Count is < 2 or > 3)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION FIND-STRING takes argument-1 argument-2 "
                + $"[[START AFTER] argument-3] (ISO §15.37.2); {operands.Count} operand argument(s) given");
            return BoundExprError.Refused(ctx.Edition, "FUNCTION FIND-STRING");
        }
        // ⛔ THE §15.3 SCREEN IS CALLED HERE BECAUSE THIS BINDER RETURNS BEFORE THE GENERIC ONE REACHES IT
        // (fix-queue PB12). CheckArgumentClasses sits after arity on the generic path and its comment
        // claimed it ran "before every per-function arm" — FALSE for the eight bespoke binders that
        // `return` above it, so no Verified row could ever screen them. Screened here, after this
        // binder's own arity check, exactly as the generic path orders it.
        CheckArgumentClasses(sig, operands);
        return new BoundIntrinsicCall(sig, operands, PicCategory.Numeric) { FindLast = last, Anycase = anycase };
    }

    /// <summary>SUBSTITUTE (§15.87) — <c>argument-1 { [ANYCASE] [FIRST|LAST] argument-2 argument-3 } …</c>
    /// (§15.87.2). argument-1 is the source; each following group is a replacement PAIR (argument-2 matched,
    /// argument-3 substituted) preceded by its own optional phrase keywords: ANYCASE (case fold, rule 5),
    /// FIRST (first occurrence only, rule 3.a) or LAST (last only, rule 3.b) — default replaces ALL occurrences.
    /// The keyword words arrive as bare-word arguments (<see cref="KeywordWordOf"/>); each pair's mode (bits
    /// 0=FIRST/1=LAST/2=ANYCASE) rides on <see cref="BoundIntrinsicCall.SubstituteModes"/>, and
    /// <see cref="BoundIntrinsicCall.Args"/> carries [source, from₁, to₁, from₂, to₂, …].</summary>
    private BoundExpr BindSubstitute(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        var operands = new List<BoundOperand>();   // [source, from₁, to₁, from₂, to₂, …] — or [source, part₁, part₂, …] when flat
        var modes = new List<int>();               // one mode per completed pair — or one flag PER PART when flat
        int pending = 0;                           // phrase flags accumulating for the NEXT pair
        bool haveSource = false;
        int pairOperands = 0;                      // operands seen since the last completed pair
        // ⛔ A table(ALL) argument (§15.3 — admissible, SUBSTITUTE's `{ argument-2 argument-3 } …` repeats — kb/Work
        // PB81): its elements form the PAIRS at run time — a from/to interleave over a runtime count — so the call
        // switches to the FLAT form: every operand after argument-1 is a PART (a written operand, or an enumeration),
        // and the flag list carries the keywords preceding each part, which attach to the pair the part's FIRST
        // element starts (§15.3: "as if each table element … were specified", in order — the keywords precede
        // argument-2 of the pair an element opens). CobolIntrinsics.SubstituteFlat pairs the elements, takes each
        // pair's mode from its argument-2 element, and raises EC-ARGUMENT-FUNCTION for an odd count, a keyword before
        // an argument-3 element, or FIRST with LAST — the §15.87.2 malformed shapes, decided at run time because the
        // count is. Before this the ALL argument was the staged COBOLNET0899.
        bool flat = false;
        var partFlags = new List<int>();
        BoundExpr Malformed() { ctx.Edition.Error("COBOLNET1504", "FUNCTION SUBSTITUTE takes argument-1 and one "
            + "or more [ANYCASE][FIRST|LAST] argument-2 argument-3 pairs (ISO §15.87.2)"); return BoundExprError.Refused(ctx.Edition, "FUNCTION SUBSTITUTE"); }

        BoundExpr MalformedOrder(string why)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION SUBSTITUTE's phrase keywords precede their pair in the "
                + $"format's order — [ANYCASE] [FIRST|LAST] argument-2 argument-3 (ISO §15.87.2) — {why}");
            return BoundExprError.Refused(ctx.Edition, "FUNCTION SUBSTITUTE");
        }
        foreach (var a in argCtxs)
        {
            switch (KeywordWordOf(a))
            {
                // ⛔ POSITIONAL PER PAIR (kb/Work PB124 — AR-15.3-7): §15.87.2 prints `[ANYCASE] [FIRST|LAST]`
                // BEFORE each argument-2/argument-3 pair, in that written order — the old `pending |=`
                // accumulation took the keywords anywhere before the pair completed (between argument-2 and
                // argument-3 included), repeated, and in either order. `pairOperands != 0` = we are between a
                // pair's argument-2 and its argument-3 (always 0 on the flat table(ALL) form, whose pairing is
                // a run-time fact and whose keyword placement is screened by SubstituteFlat).
                case "ANYCASE":
                    if (pairOperands != 0) return MalformedOrder("ANYCASE sits between argument-2 and argument-3");
                    if ((pending & 4) != 0) return MalformedOrder("ANYCASE is repeated");
                    if ((pending & 3) != 0) return MalformedOrder("ANYCASE follows FIRST/LAST");
                    pending |= 4; continue;
                case "FIRST":
                    if (pairOperands != 0) return MalformedOrder("FIRST sits between argument-2 and argument-3");
                    if ((pending & 3) != 0) return MalformedOrder((pending & 1) != 0
                        ? "FIRST is repeated" : "FIRST and LAST are mutually exclusive");
                    pending |= 1; continue;
                case "LAST":
                    if (pairOperands != 0) return MalformedOrder("LAST sits between argument-2 and argument-3");
                    if ((pending & 3) != 0) return MalformedOrder((pending & 2) != 0
                        ? "LAST is repeated" : "FIRST and LAST are mutually exclusive");
                    pending |= 2; continue;
            }
            if (TryBindAllArgument(sig, a, operands))
            {
                if (!haveSource) return Malformed();   // argument-1 is one operand, never an enumeration (§15.87.2)
                flat = true;
                partFlags.Add(pending);
                pending = 0;
                continue;
            }
            var op = BindArgOperand(a);
            if (!haveSource) { operands.Add(op); haveSource = true; continue; }
            operands.Add(op);
            partFlags.Add(pending);   // the flags this operand carries as a PART (the flat form's per-part list)
            if (flat) { pending = 0; continue; }   // the pairing is a run-time fact once an enumeration is in play
            if (++pairOperands == 2)   // argument-2 then argument-3 complete a pair
            {
                // The pair's mode = the flags recorded on its argument-2. `pending` is always 0 here since the
                // switch rejects a keyword between argument-2 and argument-3 (kb/Work PB124); the `|` stays as
                // a belt should that screen ever move.
                int pairMode = partFlags[^2] | pending;
                if ((pairMode & 3) == 3) return Malformed();   // FIRST and LAST are mutually exclusive (§15.87.2)
                modes.Add(pairMode);
                pending = 0;
                pairOperands = 0;
            }
            else pending = 0;   // recorded on argument-2's part; anything before argument-3 accrues to the pair above
        }
        if (flat)
        {
            // The pairing is a run-time fact; only the shape rules decidable here are decided here (a source, at
            // least one part, no dangling keyword). CheckArgumentClasses screens every operand as usual.
            if (!haveSource || operands.Count < 2 || pending != 0) return Malformed();
            CheckArgumentClasses(sig, operands);
            CheckSubstituteZeroLengthArgument2(operands);
            return new BoundIntrinsicCall(
                sig, operands, IntrinsicSig.CategoryOf(IntrinsicResultType.Resolve(sig, operands)))
                { SubstituteModes = partFlags, SubstituteFlat = true };
        }
        // A well-formed call ends on a completed pair with no dangling keyword/operand (§15.87.3 rule requires
        // at least one pair; the source alone or a half pair is malformed).
        if (!haveSource || modes.Count == 0 || pairOperands != 0 || pending != 0) return Malformed();
        // §15.87.1's result-type table follows argument-1, and this bespoke bind is the second site that bypassed
        // it (see the TRIM note above — the same two-arm shape, the same silent under-rejection).
        // ⛔ THE §15.3 SCREEN IS CALLED HERE BECAUSE THIS BINDER RETURNS BEFORE THE GENERIC ONE REACHES IT
        // (fix-queue PB12). CheckArgumentClasses sits after arity on the generic path and its comment
        // claimed it ran "before every per-function arm" — FALSE for the eight bespoke binders that
        // `return` above it, so no Verified row could ever screen them. Screened here, after this
        // binder's own arity check, exactly as the generic path orders it.
        CheckArgumentClasses(sig, operands);
        CheckSubstituteZeroLengthArgument2(operands);
        return new BoundIntrinsicCall(
            sig, operands, IntrinsicSig.CategoryOf(IntrinsicResultType.Resolve(sig, operands)))
            { SubstituteModes = modes };
    }

    /// <summary>§15.87.3 r3 — "Neither argument-1 nor argument-2 shall be of zero length" — for every pair's
    /// ARGUMENT-2. Argument-1's half is the schema's <c>MinWidth(1)</c> predicate; the pairs' first members are
    /// the ODD operand positions, which one variadic tail kind cannot single out (kb/Work PB58), so they are
    /// walked here. Static widths only: a genuinely run-time width is the documented fail-open, and the runtime
    /// twin (<c>CobolIntrinsics.Substitute</c>, §15.87.4 r1) answers it.</summary>
    /// <remarks>⛔ ONE WALK, BOTH EXITS (kb/Work PB247). This sat BELOW <c>BindSubstitute</c>'s <c>if (flat)</c>
    /// early return, so the moment any operand was a table(ALL) enumeration the whole rule went unenforced and
    /// <c>FUNCTION SUBSTITUTE(X3 "" "Q" T(ALL))</c> bound clean on a STATICALLY known zero-length argument-2 —
    /// the two-arm dispatch with one arm fixed. The pair ROLE of an operand is decidable exactly as far as the
    /// element counts are static, so the walk advances a slot counter by each operand's static element count and
    /// STOPS at the first operand whose count is a run-time fact (an OCCURS DEPENDING or DYNAMIC ALL level);
    /// everything before it keeps its compile-time answer, which is strictly more than the old "all or nothing".</remarks>
    private void CheckSubstituteZeroLengthArgument2(IReadOnlyList<BoundOperand> operands)
    {
        long slot = 0;                                  // 1 = pair 1's argument-2, 2 = pair 1's argument-3, …
        for (int i = 1; i < operands.Count; i++)
        {
            long? count = operands[i] is BoundFieldOperand { Place: TableAllPlace all } ? all.StaticCount : 1;
            if (count is not { } c || c <= 0) return;   // the pairing past here is a run-time fact
            // The FIRST ODD slot this operand occupies, in arithmetic rather than by walking its elements —
            // a fixed OCCURS may be thousands wide and this runs at bind time for every SUBSTITUTE call.
            long firstArg2Slot = (slot & 1) == 0 ? slot + 1 : (c >= 2 ? slot + 2 : 0);
            slot += c;
            if (firstArg2Slot != 0 && KnownWidth(operands[i]) is 0)
                ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass, $"FUNCTION SUBSTITUTE argument-2 of pair "
                    + $"{(firstArg2Slot + 1) / 2} is of zero length, which ISO §15.87.3 r3 does not admit");
        }
    }

    /// <summary>CONVERT (§15.19) — data-representation conversion (2023). Argument-1 is followed BY POSITION by
    /// two keyword groups: source-format (ANY | ANUM | HEX | NAT) and destination-format (ANUM | NAT [HEX] |
    /// BYTE), each a bare-word argument (§15.19.2; <see cref="KeywordWordOf"/> — ANY/ALPHANUMERIC/NATIONAL are
    /// reserved words, the rest §8.10 context-sensitive words that arrive as bare names and stay legal as
    /// argument-1's own data-name). The operand binds ordinarily; the format words ride the node's
    /// <c>Convert*</c> init-properties. The argument/SR rules (§15.19.3) are enforced here (COBOLNET1514); the
    /// result category (§15.19.1) is National for a NAT destination, Alphanumeric otherwise.</summary>
    private BoundExpr BindConvert(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        // §15.19.2: ( argument-1 source-format destination-format ) — a POSITIONAL walk, not an order-free
        // harvest (fix-queue PB59 / FMT-15.19.2). Slot 0 is ALWAYS argument-1 and binds as an OPERAND: NAT /
        // ANUM / HEX / BYTE are §8.10 CONTEXT-SENSITIVE words, reserved only inside CONVERT's own format
        // ("otherwise it is treated as a user-defined word"), so a data item NAMED one of them is legal as
        // argument-1 — the old harvest swallowed it as a keyword (measured: 1504 "0 operand + 3 format
        // keyword(s) given") and, symmetrically, accepted CONVERT(ANUM WS-A ANUM HEX) with the operand
        // mid-list. ANY/ALPHANUMERIC/NATIONAL stay §8.9 reserved and can never be data-names, so only the
        // four context-sensitive words change behavior.
        var operands = new List<BoundOperand>();
        var kws = new List<string>();
        foreach (var (a, at) in argCtxs.Select((a, at) => (a, at)))
        {
            if (at == 0)
            {
                // Table 21 marks Type1 for CONVERT, but §15.19.3/§15.19.4 define NO argument rule and NO
                // returned value for a type-name (a type declaration has neither storage nor a value for any
                // source-format to read) — a standard-text inconsistency, recorded in CONFORMANCE.md
                // (kb/Work PB124 wave 4). Rejected with the reason named rather than "undefined data-name".
                if (TypeNameArgument(a) is { } td)
                {
                    ctx.Edition.Error("COBOLNET1514", $"FUNCTION CONVERT: argument-1 names the type "
                        + $"declaration '{td.CobolName}' — Table 21 lists a type-name here, but §15.19 "
                        + "defines no semantics for one (recorded as a standard-text inconsistency, "
                        + "CONFORMANCE.md); write a data item described with the type instead");
                    return BoundExprError.Refused(ctx.Edition, "FUNCTION CONVERT type argument");
                }
                operands.Add(BindArgOperand(a));
                continue;
            }
            if (KeywordWordOf(a) is { } w && IsConvertFormatWord(w)) { kws.Add(w); continue; }
            ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: the arguments after argument-1 shall be the "
                + "source-format and destination-format keywords, in that order (ISO §15.19.2)");
            return BoundExprError.Refused(ctx.Edition, "FUNCTION CONVERT format");
        }
        if (operands.Count != 1 || kws.Count is < 2 or > 3)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION CONVERT takes ( argument-1 source-format "
                + $"destination-format ) (ISO §15.19.2); {operands.Count} operand + {kws.Count} format keyword(s) given");
            return BoundExprError.Refused(ctx.Edition, "FUNCTION CONVERT arity");
        }

        int src = kws[0] switch { "ANY" => 0, "ALPHANUMERIC" or "ANUM" => 1, "HEX" => 2, "NATIONAL" or "NAT" => 3, _ => -1 };
        int i = 1, dst; bool hex = false;
        dst = kws[i] switch { "BYTE" => 4, "ALPHANUMERIC" or "ANUM" => 1, "NATIONAL" or "NAT" => 3, _ => -1 };
        if (dst >= 0) i++;
        if (dst is 1 or 3 && i < kws.Count && kws[i] == "HEX") { hex = true; i++; }

        if (src < 0 || dst < 0 || i != kws.Count)
        {
            ctx.Edition.Error("COBOLNET1514", $"FUNCTION CONVERT: '{string.Join(' ', kws)}' is not a valid "
                + "source-format destination-format pair (ISO §15.19.2 — ANY|ANUM|HEX|NAT then ANUM|NAT [HEX] | BYTE)");
            return BoundExprError.Refused(ctx.Edition, "FUNCTION CONVERT");
        }
        // The §15.3 screen — see the PB12 note on the FIND-STRING arm: this binder returns before the
        // generic path reaches CheckArgumentClasses, so it screens its own operand list.
        CheckArgumentClasses(sig, operands);
        // SR3 — source shall differ from destination (only ANUM→ANUM / NAT→NAT with no HEX collide, §15.19.3).
        if ((src == 1 && dst == 1 && !hex) || (src == 3 && dst == 3 && !hex))
            ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: the source-format and destination-format are the "
                + "same (ISO §15.19.3 SR3)");
        // SR8 — an ANY source requires an ANUM HEX or NAT HEX destination.
        if (src == 0 && !(hex && dst is 1 or 3))
            ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: a source-format of ANY requires a destination of "
                + "ANUM HEX or NAT HEX (ISO §15.19.3 SR8)");
        // SR9 — a BYTE destination requires a HEX source.
        if (dst == 4 && src != 2)
            ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: a destination-format of BYTE requires a "
                + "source-format of HEX (ISO §15.19.3 SR9)");
        // SR1 — argument-1 shall not be zero length. This arm is the COMPILE-TIME catch for the one shape
        // knowable here (an empty literal — ISO 8.5.4's shape 8); the RUNTIME twin at the top of
        // CobolIntrinsics.Convert screens the other 8.5.4 zero-length shapes (a DYNAMIC LENGTH item at
        // length 0, a zero-occurrence ODO group, …) with EC-ARGUMENT-FUNCTION (fix-queue PB59 / AR-15.19.3-1).
        if (operands[0] is BoundStringLiteral { Value.Length: 0 })
            ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: argument-1 is of zero length (ISO §15.19.3 SR1)");

        // §15.19.3 r4/r5/r6/r7 — the source-format keys what argument-1's STORAGE must hold, and the static
        // half of each rule is its REPRESENTATION (IntrinsicArgumentRules.StaticUsageOf — the keyword-dependent
        // axis the ordinal ArgSchema cannot express; DeliberatelyUnscreened["CONVERT"] records the disposition).
        // r4 names the axis outright ("a valid string of hexadecimal digits of display or national usage");
        // r5 wants "a valid string of characters from the program's alphanumeric coded character set", and its
        // NOTE ("distinct from simply requiring the string to be of class alphanumeric") cuts by representation,
        // not class — a numeric or edited DISPLAY item qualifies, a COMP or national item does not; r6 wants
        // national characters. The VALUE halves (r4's digit validity, r5/r6 membership of the coded set) are
        // the runtime screens' territory (the hex-digit screen, the Annex A.1 item-33 total correspondence), so
        // nothing further is screenable at bind; shapes with no static representation pass to runtime.
        if (IntrinsicArgumentRules.StaticUsageOf(operands[0]) is { } u)
        {
            if (src == 1 && u is not Usage.Display)
                ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: an ANUM source-format reads argument-1 as "
                    + $"a string of characters from the alphanumeric coded character set, which usage {u} does "
                    + "not hold (ISO §15.19.3 rule 5)");
            if (src == 3 && u is not Usage.National)
                ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: a NAT source-format reads argument-1 as a "
                    + $"string of national characters, which usage {u} does not hold (ISO §15.19.3 rule 6)");
            if (src == 2 && u is not (Usage.Display or Usage.National))
                ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: a HEX source-format takes argument-1 of "
                    + $"display or national usage, not {u} (ISO §15.19.3 rule 4)");
            // r7 — ANY takes any usage EXCEPT the address-holding ones; "it is not necessary for the contents
            // to be valid according to the usage", so this exclusion is the ONLY screen on an ANY source.
            // ClassOfCategory cannot express it (the pointer/program-pointer collapse), which is why the
            // predicate reads Usage. MESSAGE-TAG has no Usage member — the usage-inventory drift test forces
            // that decision when the usage lands.
            if (src == 0 && u is Usage.Index or Usage.ObjectReference or Usage.Pointer or Usage.ProgramPointer
                    or Usage.FunctionPointer)
                ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: an ANY source-format argument-1 shall not "
                    + "be of usage index, message-tag, object reference, pointer, function-pointer or "
                    + "program-pointer (ISO §15.19.3 rule 7)");
        }

        // §15.19.3 r7 — a source-format of ANY takes the operand's RAW storage bits regardless of usage. The
        // node carries the WRITTEN source; the RENDERER's storage channel (IntrinsicRenderer.StorageArg →
        // OperandText.AsStorageImage) delivers the bytes per the operand's own representation (PB59 family 5b —
        // the former bind-time ANY→NAT remap was a second mechanism for the same job and is deleted).
        var category = dst == 3 ? PicCategory.National : PicCategory.Alphanumeric;   // §15.19.1 table
        return new BoundIntrinsicCall(sig, operands, category)
            { ConvertSource = src, ConvertDest = dst, ConvertDestHex = hex };
    }

    /// <summary>The statically knowable data category of a function argument (drives the §15.3 per-function
    /// argument class/category rules): a categorized literal, a non-group field reference (a reference-modified
    /// operand keeps its item's category — a national item's ref-mod is category national, §8.4.3.3.4), a nested
    /// intrinsic's result category, or numeric for a computed arithmetic expression. Null = no fixed static
    /// class (groups, figuratives, ALL literals, error operands) — the caller skips its check.
    /// <para>⚠ THE DEFINITION LIVES IN <see cref="IntrinsicResultType"/>: the §15.x.1 RESULT-type rules need the
    /// same §8.5.2.1 answer these §15.3 ARGUMENT rules do, and two copies of one rule is how they drift apart
    /// (<c>feedback_one_rule_one_place</c>). This is a local alias, not a second implementation.</para></summary>
    private static PicCategory? OperandCategory(BoundOperand op) => IntrinsicResultType.OperandCategory(op);

    /// <summary>The statically knowable width (character positions) of a function argument — TOTAL over the
    /// static shapes (fix-queue PB59 / AR-15.26.3-2, AR-15.66.3-2): null ONLY when the length genuinely exists
    /// at run time. The previous three-arm partial returned null for a GROUP, a REF-MOD view, an ALL literal
    /// and a figurative, and its one call site's <c>is {{ }}</c> guard read every null as "skip the screen" —
    /// so a six-position group or <c>ALL "QQ"</c> as a substitution character sailed past the §15.26.3 r2 /
    /// §15.66.3 r2 one-position rule with no diagnostic, while the equivalent plain literal was rejected: a
    /// partial function where the rule needs a total one.</summary>
    /// <remarks>The CATEGORY twin (<c>OperandCategory</c>) is now TOTAL as well — landed with the family-7b
    /// measurement (2026-08-09): groups answer their §8.5.2.1 class, ALL literals their literal's category,
    /// and a ref-mod view the §8.4.3.3.4 GR6 rewrite, with MAX/MIN's type resolution and body choice aligned
    /// on the one classifier.</remarks>
    private static int? KnownWidth(BoundOperand op) => op switch
    {
        BoundStringLiteral sl => sl.Value.Length,
        // §8.4.3.3 — a ref-mod view with a LITERAL length has that static width; a computed or
        // omitted-length form is genuinely runtime.
        BoundFieldOperand { Place: RefModPlace rm } => rm.StaticLength(innerLength: null),
        BoundFieldOperand { Place.Item: { IsAnyLength: true } or { IsDynamicLength: true } } => null,
        // A group's width is static exactly when nothing beneath it varies at run time — the §15.50.4 r7
        // dynamic guards plus an ODO subordinate's varying current length (§8.5.1.8 GR7/GR8).
        BoundFieldOperand { Place.Item: { IsGroup: true } g } => HasRuntimeLength(g) ? null : g.ImageWidth,
        BoundFieldOperand { Place.Item: { } item } => item.ImageWidth,
        BoundAllLiteral a => a.Literal.Length,   // §8.3.3.6.4 GR3c — a length-unspecified context takes the literal once
        BoundFigurative => 1,                    // §8.3.3.6.4 GR3b — a bare figurative is ONE character
        BoundPredefinedNull => null,             // §8.4.3.10.1 — an address of class pointer: no character width
        _ => null,   // computed results / error operands — genuinely runtime
    };

    /// <summary>Does this group have an OCCURS DEPENDING ON table beneath it? Its CURRENT extent varies at run
    /// time (§8.5.1.8), so the group's width is not statically known. The recursive REDEFINES-excluding walk
    /// mirrors <see cref="HasDynamicLengthLeaf"/>.</summary>
    private static bool HasOdoBeneath(DataItem g) =>
        g.Children.Any(c => c.RedefinesTargetName is null
                            && (c.OccursSpec?.DependingName is not null || (c.IsGroup && HasOdoBeneath(c))));

    /// <summary>DISPLAY-OF / NATIONAL-OF argument rules — DISPLAY-OF (§15.26.3): argument-1 shall be of class
    /// national (r1); argument-2 shall be of class alphabetic or alphanumeric and one character position in
    /// length (r2 — the alphanumeric SUBSTITUTION character). NATIONAL-OF (§15.66.3): argument-1 shall be of
    /// class alphabetic or alphanumeric (r1) and shall not be a zero-length literal (r3); argument-2 shall be
    /// of category national and one character in length (r2 — the national substitution character). Class
    /// alphanumeric spans the alphanumeric, alphanumeric-edited, and numeric-edited categories (§8.4.2 table 2);
    /// the checks fire on the statically classifiable shapes and skip shapes with no fixed class.</summary>
    private void CheckRepertoireArgs(IntrinsicSig sig, List<BoundOperand> args)
    {
        bool toNat = sig.Name == "NATIONAL-OF";

        if (toNat && args[0] is BoundStringLiteral { Value.Length: 0 })
            ctx.Edition.Error("COBOLNET1546", "FUNCTION NATIONAL-OF: argument-1 shall not be a zero-length "
                + "literal (ISO §15.66.3 rule 3)");

        if (OperandCategory(args[0]) is { } c1)
        {
            if (!toNat && c1 is not PicCategory.National)
                ctx.Edition.Error("COBOLNET1546", "FUNCTION DISPLAY-OF: argument-1 shall be of class national "
                    + "(ISO §15.26.3 rule 1) — FUNCTION NATIONAL-OF is the alphanumeric→national conversion");
            else if (toNat && c1 is not (PicCategory.Alphanumeric or PicCategory.NumericEdited))
                ctx.Edition.Error("COBOLNET1546", "FUNCTION NATIONAL-OF: argument-1 shall be of class alphabetic "
                    + "or alphanumeric (ISO §15.66.3 rule 1) — FUNCTION DISPLAY-OF is the national→alphanumeric "
                    + "conversion");
        }

        if (args.Count < 2) return;
        if (OperandCategory(args[1]) is { } c2)
        {
            if (!toNat && c2 is not (PicCategory.Alphanumeric or PicCategory.NumericEdited))
                ctx.Edition.Error("COBOLNET1546", "FUNCTION DISPLAY-OF: argument-2 (the substitution character) "
                    + "shall be of class alphabetic or alphanumeric (ISO §15.26.3 rule 2)");
            else if (toNat && c2 is not PicCategory.National)
                ctx.Edition.Error("COBOLNET1546", "FUNCTION NATIONAL-OF: argument-2 (the substitution character) "
                    + "shall be of category national (ISO §15.66.3 rule 2)");
        }
        if (KnownWidth(args[1]) is { } w && w != 1)
            ctx.Edition.Error("COBOLNET1546", $"FUNCTION {sig.Name}: argument-2 (the substitution character) "
                + $"shall be one character position in length, not {w} "
                + $"(ISO {(toNat ? "§15.66.3" : "§15.26.3")} rule 2)");
    }

    /// <summary>BASECONVERT argument rules (§15.12.3 r1 — fix-queue PB59 / AR-15.12.3-1 legs (b)–(f)), the
    /// COMPILE-TIME halves; the runtime twins (the r2 digit screen, the dynamic base range and the equal-bases
    /// EC) live in <c>CobolIntrinsics.BaseConvert</c>. Argument-1 "shall be a usage display or national data
    /// item or literal" — the <see cref="IntrinsicArgumentRules.StaticUsageOf"/> axis (a COMP/PACKED item's
    /// storage is not a digit string; the same shared reader the CONVERT r4–r7 screens ride). When the base in
    /// argument-2 is a STATIC literal below 11, argument-1 "shall also be an unsigned integer data item or
    /// literal" — which rejects a SIGNED or SCALED numeric argument-1 and a numeric-edited one (not an
    /// unsigned integer data item). ⚠ An ALPHANUMERIC string under a sub-11 base is deliberately ADMITTED:
    /// "unsigned integer … literal" is readable as the literal's KIND or its CONTENT, the corpus and GnuCOBOL
    /// both accept string arguments at every base (BASECONVERT("1010", 2, 16) is a pinned-green golden), and
    /// the runtime r2 digit screen owns the content — the narrower reading would reject pinned-legal source
    /// (follow-GnuCOBOL-on-split-latitude). Argument-2/-3 "shall be positive nonzero numeric integer literals
    /// or data items with unequal values in the range 2 to 16" — every STATIC-literal violation is flagged at
    /// compile time per §4.2.2 ¶3 (a non-integer literal base, a literal base outside 2..16, two EQUAL literal
    /// bases); data-item bases stay the runtime guards' territory.</summary>
    private void CheckBaseConvertArgs(List<BoundOperand> args)
    {
        if (args.Count > 0 && IntrinsicArgumentRules.StaticUsageOf(args[0]) is { } u
            && u is not (Usage.Display or Usage.National))
            ctx.Edition.Error("COBOLNET1642", "FUNCTION BASECONVERT: argument-1 shall be a usage display or "
                + $"national data item or literal, not usage {u} (ISO §15.12.3 rule 1)");

        var s2 = StaticIntLiteral(args, 1);
        var s3 = StaticIntLiteral(args, 2);
        int? b2 = s2?.Value;
        if (s2 is { } r2 && r2.Value is < 2 or > 16)
            ctx.Edition.Error("COBOLNET1642", "FUNCTION BASECONVERT: argument-2 shall be in the range 2 to 16, "
                + $"not {r2.Text} (ISO §15.12.3 rule 1; §4.2.2 — a literal violation is flagged at compile time)");
        if (s3 is { } r3 && r3.Value is < 2 or > 16)
            ctx.Edition.Error("COBOLNET1642", "FUNCTION BASECONVERT: argument-3 shall be in the range 2 to 16, "
                + $"not {r3.Text} (ISO §15.12.3 rule 1; §4.2.2 — a literal violation is flagged at compile time)");
        // Equal VALUES, not equal saturated bounds: two different over-range literals both read as HostLimit.
        if (s2 is { Beyond: false } e2 && s3 is { Beyond: false } e3 && e2.Value == e3.Value)
            ctx.Edition.Error("COBOLNET1642", "FUNCTION BASECONVERT: argument-2 and argument-3 shall have "
                + $"unequal values — both are {e2.Value} (ISO §15.12.3 rule 1)");
        for (int i = 1; i <= 2 && i < args.Count; i++)
            if (args[i] is BoundNumericLiteral bl && (bl.Text.Contains('.') || bl.Text.Contains(',')))
                ctx.Edition.Error("COBOLNET1642", $"FUNCTION BASECONVERT: argument-{i + 1} shall be a positive "
                    + $"nonzero numeric INTEGER literal or data item, not '{bl.Text}' (ISO §15.12.3 rule 1)");

        // r1's sub-11 half. OperandCategory is the totalized §8.5.2.1 reader, so a ref-mod view (class
        // alphanumeric, GR6c) correctly skips this arm and reads as a string.
        if (b2 is >= 2 and < 11 && args.Count > 0
            && OperandCategory(args[0]) is PicCategory.Numeric or PicCategory.NumericEdited)
        {
            bool bad = args[0] switch
            {
                BoundFieldOperand f0 => f0.Place.Item.Pic is { } p0
                    && (p0.Category is PicCategory.NumericEdited || p0.Signed || p0.Scale > 0 || p0.IsFloat),
                BoundNumericLiteral nl0 => nl0.Text.Contains('.') || nl0.Text.Contains(',')
                    || nl0.Text.StartsWith('-'),
                _ => false,   // computed results — the runtime digit screen owns their content
            };
            if (bad)
                ctx.Edition.Error("COBOLNET1642", "FUNCTION BASECONVERT: when the base in argument-2 is below "
                    + "11, argument-1 shall be an unsigned integer data item or literal (ISO §15.12.3 rule 1)");
        }
    }

    /// <summary>The statically known integer value of argument <paramref name="i"/> — a plain numeric literal
    /// only (a data item or expression is runtime territory); null when absent or not an integer literal. Read
    /// through THE ONE integer-literal reader (kb/Work PB1579): a <c>long.TryParse</c> answered "not an integer
    /// literal" for a 20-to-31-digit base, and the compile-time range screen was skipped. <c>Value</c> is the host
    /// value (saturated when <c>Beyond</c>); a diagnostic quotes <c>Text</c>, the literal as written.</summary>
    private static (int Value, bool Beyond, string Text)? StaticIntLiteral(IReadOnlyList<BoundOperand> args, int i) =>
        i < args.Count && args[i] is BoundNumericLiteral nl
        && CobolNet.Validation.IntegerOperandRules.TryHostValue(nl.Text, out int v, out bool beyond)
            ? (v, beyond, nl.Text) : null;

    /// <summary>The §15.19.2 CONVERT format words (reserved within the argument list, like TRIM's LEADING/TRAILING).</summary>
    private static bool IsConvertFormatWord(string w) =>
        w.ToUpperInvariant() is "ANY" or "ALPHANUMERIC" or "ANUM" or "HEX" or "NATIONAL" or "NAT" or "BYTE";

    /// <summary>MODULE-NAME (§15.65) — one mandatory phrase keyword selecting a runtime element of the running
    /// COBOL hierarchy (§15.65.2: ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL; <c>TOP-LEVEL</c> is one hyphenated
    /// word). The selector rides on <see cref="BoundIntrinsicCall.ModuleNameKind"/>; the value resolves at
    /// runtime from the module call-name stack (<c>CobolModule</c>). NESTED requires a contained program
    /// (§15.65.3 argument rule 1) — a compile-time conformance check.</summary>
    private BoundExpr BindModuleName(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        int kind = -1;
        foreach (var a in argCtxs)
        {
            int k = KeywordWordOf(a) switch
                { "CURRENT" => 0, "ACTIVATING" => 1, "NESTED" => 2, "STACK" => 3, "TOP-LEVEL" => 4, _ => -1 };
            if (k < 0 || kind >= 0)
            {
                ctx.Edition.Error("COBOLNET1504", "FUNCTION MODULE-NAME takes exactly one keyword argument "
                    + $"(ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL) (ISO §15.65.2), not '{a.GetText()}'");
                return BoundExprError.Refused(ctx.Edition, "FUNCTION MODULE-NAME");
            }
            kind = k;
        }
        if (kind < 0)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION MODULE-NAME requires one keyword argument "
                + "(ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL) (ISO §15.65.2)");
            return BoundExprError.Refused(ctx.Edition, "FUNCTION MODULE-NAME");
        }
        // §15.65.3 argument rule 1 — NESTED only within a nested (contained) program.
        if (kind == 2 && !host.InNestedProgram)
            ctx.Edition.Error("COBOLNET1515", "FUNCTION MODULE-NAME NESTED shall be specified only within a "
                + "nested program (ISO §15.65.3 argument rule 1) — this compilation unit is not contained");
        return new BoundIntrinsicCall(sig, [], PicCategory.Alphanumeric) { ModuleNameKind = kind };
    }

    // (LocaleUnsupported — the ONE A.4.9 documented-non-support diagnostic, COBOLNET1518 — is DELETED with the
    // module's claim at PB64 T6: every A.4.9 element is implemented, so the arm had zero callers. The code
    // COBOLNET1518 is never reallocated — see the renumbering record at DataBinder.cs's 1577 note.)

    /// <summary>Detect the <c>LOCALE</c> phrase in a function's argument list — the keyword appears as a
    /// bare-word argument (via <see cref="KeywordWordOf"/>) at argument position 2 or later (never argument-1,
    /// the operand): <c>LOWER-CASE(arg-1 LOCALE locale-name-1)</c> (§15.57.2), <c>NUMVAL-C(arg-1 LOCALE
    /// [locale-name-1])</c> (§15.68.2). LOCALE is not a LEXER TOKEN here, so the phrase parses as extra space- or
    /// comma-separated arguments and is recognised by NAME.
    /// <para>⚠ It IS a reserved word from 2002 (§8.9; <c>reserved-words.json</c> r2002/r2014/r2023) — this comment
    /// used to say otherwise, which is wrong by the repo's own data. Not tokenizing it is a deliberate choice, not
    /// a consequence of the reservation: this detection depends on the word arriving as an ordinary argument, so a
    /// token would silently break the recognition. (fix-queue PB25.)</para>
    /// The argument-1 exclusion is POSITIONAL — the format puts argument-1 there, so a bare word in slot 0 is the
    /// operand whatever it spells. (It is not "a data item happening to be named LOCALE": §8.9 reserves the word,
    /// so only a &gt;&gt;COBOL-WORDS UNDEFINE can free it — and then <see cref="KeywordWordOf"/> resolves it to
    /// null and no slot sees the keyword at all. kb/Work PB250.)</summary>
    private bool HasLocalePhrase(IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        for (int i = 1; i < argCtxs.Count; i++)
            if (KeywordWordOf(argCtxs[i]) == "LOCALE") return true;
        return false;
    }

    /// <summary>NUMVAL-C / TEST-NUMVAL-C (§15.68 / §15.94) — argument-1, then EITHER the argument-2 currency
    /// string OR the <c>LOCALE [locale-name-1]</c> keyword (a bracketed stack of alternatives, §15.68.2/§15.94.2
    /// — at most one may be written), plus the orthogonal optional ANYCASE keyword (§15.68.3 r4f, or r5b.1/3
    /// under LOCALE — rides the ONE <see cref="BoundIntrinsicCall.Anycase"/> flag). With neither argument-2 nor
    /// LOCALE there is exactly ONE currency string for the compilation unit — the SPECIAL-NAMES CURRENCY string
    /// or the default sign (§15.68.3 r3) — injected HERE at bind time so the SPECIAL-NAMES config stays out of
    /// the backend (bound nodes carry complete semantics). Under LOCALE (r5 — LIVE since kb/Work PB64 T6, the
    /// A.4.9 item-12 claim) NO currency is injected (r3 reads "If neither argument-2 nor the LOCALE keyword is
    /// specified") and the bound node carries the resolved <see cref="BoundIntrinsicCall.Locale"/> plus the
    /// <see cref="BoundIntrinsicCall.LocaleWritten"/> flag — the r4 and r5 arms accept DIFFERENT languages and
    /// the renderer must tell a bare LOCALE from no phrase at all.</summary>
    private BoundExpr BindNumvalCFamily(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        string fmt = sig.Name == "NUMVAL-C" ? "15.68.2" : "15.94.2";
        // §15.68.2/§15.94.2: ( argument-1 [argument-2 | LOCALE [locale-name-1]] [ANYCASE] ) — a POSITIONAL walk,
        // not an order-free keyword sweep (fix-queue PB60 / FMT-15.68.2). Slot 0 is ALWAYS argument-1 and binds
        // as an operand — POSITIONALLY, because the format puts argument-1 there, not because a data item may be
        // named ANYCASE or LOCALE: both are §8.9 RESERVED words from 2002 (reserved-words.json r2002/r2014/r2023)
        // and NEITHER is in the §8.10 context-sensitive table, so absent a >>COBOL-WORDS UNDEFINE no data item
        // may carry either name at all. (This comment claimed §8.10 — wrong by the repo's own data; kb/Work
        // PB250, which is also why the words above now resolve through CobolWordsMap.) ANYCASE is admitted
        // ONCE, after the operands, and nothing may follow it. The old sweep
        // accepted NUMVAL-C(ANYCASE WS-A "USD") and a doubled trailing ANYCASE (both measured).
        bool anycase = false, localeWritten = false;
        var locale = LocaleRef.Current;
        var operands = new List<BoundOperand>();
        for (int at = 0; at < argCtxs.Count; at++)
        {
            var a = argCtxs[at];
            if (at > 0 && KeywordWordOf(a) == "ANYCASE")
            {
                if (anycase)
                {
                    ctx.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name}: the ANYCASE keyword is repeated "
                        + $"(ISO §{fmt} — argument-1 [argument-2 | LOCALE [locale-name-1]] [ANYCASE])");
                    return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} format");
                }
                anycase = true;
                continue;
            }
            if (anycase)
            {
                ctx.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name}: an argument follows ANYCASE, which "
                    + $"the format places last (ISO §{fmt})");
                return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} format");
            }
            // The LOCALE keyword (§15.68.3 r5a; kb/Work PB64 T6): optionally followed by a locale-name declared
            // in SPECIAL-NAMES (SR37's family rule → the ONE COBOLNET1664); absent, LC_MONETARY of the locale
            // current at use. A 2002 introduction with the locale facility — the construct gate answers below.
            if (at > 0 && !localeWritten && KeywordWordOf(a) == "LOCALE")
            {
                localeWritten = true;
                ConstructRegistry.Check(ctx.Edition.Edition, ctx.Edition.Sink, Constructs.NumvalCLocalePhrase2002,
                    $"the LOCALE keyword of FUNCTION {sig.Name}");
                if (at + 1 < argCtxs.Count && KeywordWordOf(argCtxs[at + 1]) is { } lname && lname != "ANYCASE")
                {
                    var sym = ctx.Data.ResolveLocaleName(lname, $"FUNCTION {sig.Name}'s LOCALE {lname}",
                        "ISO §15.68.3 r5a — locale-name-1 shall be associated with a locale in the SPECIAL-NAMES paragraph");
                    if (sym is null) return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name}");
                    locale = new LocaleRef(sym);
                    at++;
                }
                continue;
            }
            operands.Add(BindArgOperand(a));
        }
        if (localeWritten && operands.Count > 1)
        {
            // §15.68.2's bracketed stack: argument-2 and the LOCALE keyword are ALTERNATIVES (§5.2.6.2 — at
            // most one of a bracketed stack may be written).
            ctx.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name}: argument-2 and the LOCALE keyword are "
                + $"alternatives — at most one may be written (ISO §{fmt}; §5.2.6.2)");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} format");
        }
        if (operands.Count is < 1 or > 2)
        {
            ctx.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name} takes argument-1 [argument-2 | LOCALE "
                + $"[locale-name-1]] [ANYCASE] (ISO §{fmt}); {operands.Count} operand argument(s) given");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} arity");
        }
        // ⛔ THE §15.3 SCREEN IS CALLED HERE BECAUSE THIS BINDER RETURNS BEFORE THE GENERIC ONE REACHES IT
        // (fix-queue PB12). CheckArgumentClasses sits after arity on the generic path and its comment
        // claimed it ran "before every per-function arm" — FALSE for the eight bespoke binders that
        // `return` above it, so no Verified row could ever screen them. Screened here, after this
        // binder's own arity check, exactly as the generic path orders it.
        CheckArgumentClasses(sig, operands);
        // §15.68.3 r1 (mirrored onto TEST-NUMVAL-C by §15.94.3 r1) — argument-1 shall be of CATEGORY
        // alphanumeric or national. The §15.3 class row above admits the EDITED categories (class alphanumeric
        // spans alphanumeric-edited and numeric-edited, Table 2) that r1's CATEGORY wording excludes — the
        // finer axis screens here (fix-queue PB60 / AR-15.68.3-1). A ref-mod view is plain category
        // alphanumeric (§8.4.3.3.4 GR6) and passes; shapes with no static category pass to the runtime scan.
        // ⛔ BOTH edited CHARACTER categories, through the ONE predicate (kb/Work PB492): §8.5.2.11 makes a
        // national-edited item category NATIONAL-EDITED, which r1's "alphanumeric or national" does not name any
        // more than it names alphanumeric-edited. `OperandCategory` answers National for it (§8.5.2.1 Table 2 —
        // the CLASS), so the finer axis has to be asked here or the arm admits what r1 excludes. It was
        // unreachable while no national-edited item could be declared at all.
        if (operands.Count > 0
            && (OperandCategory(operands[0]) is PicCategory.NumericEdited
                || operands[0] is BoundFieldOperand { Place.DenotedItem: not null } f1
                   && f1.Place.Item.Pic is { IsCharacterEdited: true }))
            ctx.Edition.Error("COBOLNET1627", $"FUNCTION {sig.Name} argument-1 is of an EDITED category; "
                + "ISO §15.68.3 rule 1 admits category alphanumeric or national only");
        // §15.68.3 r2's CONTENT halves for a LITERAL argument-2 (the same-class half rides the schema row;
        // a data item's content is the runtime guard's): at least one non-space character; none of the digits
        // 0-9 or the characters * + - , . ; no CR/DB pair in any case. The characters are named OUTRIGHT —
        // the comma/period ban does not flex with DECIMAL-POINT IS COMMA.
        if (operands.Count == 2 && operands[1] is BoundStringLiteral curLit)
        {
            string cur = curLit.Value.Trim();
            if (cur.Length == 0
                || cur.Any(c => char.IsAsciiDigit(c) || c is '*' or '+' or '-' or ',' or '.')
                || cur.Contains("CR", StringComparison.OrdinalIgnoreCase)
                || cur.Contains("DB", StringComparison.OrdinalIgnoreCase))
                ctx.Edition.Error("COBOLNET1627", $"FUNCTION {sig.Name} argument-2 shall contain at least one "
                    + "non-space character and none of the digits 0-9, the characters '*' '+' '-' ',' '.', or "
                    + "the letter pair CR/DB in any case (ISO §15.68.3 rule 2)");
        }
        if (operands.Count == 1 && !localeWritten)
        {
            // §15.68.3 r3: "If NEITHER argument-2 NOR THE LOCALE KEYWORD is specified, there shall be only one
            // currency string for the compilation unit, either the default currency sign or a currency string
            // specified in the SPECIAL-NAMES paragraph" — the unit's SoleCurrencyString ("$" with no clause, the
            // one explicitly specified string, NULL past that). Two or more distinct strings make the reference
            // illegal (kb/Work PB60 / AR-15.68.3-3 — the former scalar injected whichever clause bound last, so
            // NUMVAL-C("#1,234.56") silently returned 0 in a '#'-then-'@' unit). Under LOCALE nothing is
            // injected — the currency strings are the locale's (r5b.3; PB64 T6).
            if (ctx.Data.SoleCurrencyString is { } sole)
                operands.Add(new BoundStringLiteral(sole));
            else
                ctx.Edition.Error(DiagnosticCatalog.NumvalCAmbiguousCurrency, $"FUNCTION {sig.Name} without "
                    + $"argument-2: the compilation unit specifies {ctx.Data.ExplicitCurrencyStringCount} distinct "
                    + "currency strings in its SPECIAL-NAMES paragraph; when neither argument-2 nor LOCALE is "
                    + "specified there shall be only one currency string for the compilation unit (ISO §15.68.3 "
                    + "rule 3) — name the currency string as argument-2");
        }
        return new BoundIntrinsicCall(sig, operands, PicCategory.Numeric)
        { Anycase = anycase, Locale = locale, LocaleWritten = localeWritten };
    }

    /// <summary>An operand whose comparison/result category is alphanumeric (drives MAX/MIN resolution): a string
    /// literal, an alphanumeric/edited/group item, or a nested alphanumeric-result intrinsic.</summary>
    /// <summary>
    /// Screen every argument's CLASS against the catalog row's declared <c>ArgKinds</c> (ISO §15.3 argument
    /// types; fix-queue PB1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rejects under strict conformance and warns under <c>--permissive</c>, matching DA6/<c>COBOLNET0844</c>,
    /// which settled the same question for §8.8.1.1 arithmetic operands one wave earlier — the leniency is
    /// dialect-gated, never silent.
    /// </para>
    /// <para>
    /// ⚠ Binding CONTINUES after a violation rather than returning a <c>BoundExprError</c>. The argument is
    /// well-formed, its class is merely wrong, so the rest of the call still binds and later rules still report —
    /// which is what lets one compile name every bad argument in a statement instead of the first. Under
    /// <c>--permissive</c> the existing coercion then runs unchanged, so the leniency is genuinely the old
    /// behaviour and not a second code path.
    /// </para>
    /// </remarks>
    private void CheckArgumentClasses(IntrinsicSig sig, IReadOnlyList<BoundOperand> args)
    {
        // §15.3's trailing block (kb/Work PB124 wave 4): "A variable-length group shall be referenced as an
        // argument to a function only when explicitly permitted in the function definition." Exactly TWO
        // definitions permit one — §15.50.4 r7/r8 (LENGTH) and §15.14.4 r6/r7 (BYTE-LENGTH) define the
        // variable-length-group value — so every other function rejects one, tested by the
        // §8.5.1.12.1 definition (IsVariableLengthGroup — an ODO group is FIXED-length and stays legal).
        // Placed BEFORE the Verified early-return: a
        // deliberately-unscreened function is unscreened for CLASS, not for this shape rule.
        if (sig.Name is not ("LENGTH" or "BYTE-LENGTH"))
            for (int i = 0; i < args.Count; i++)
                if (args[i] is BoundFieldOperand { Place.DenotedItem: not null, Place.Item: { IsGroup: true } g }
                    && IsVariableLengthGroup(g))
                    Report($"FUNCTION {sig.Name} argument-{i + 1} ('{g.CobolName}') is a variable-length "
                        + "group, which ISO §15.3 admits only where the function definition explicitly "
                        + "permits it — only LENGTH (§15.50.4 r7) and BYTE-LENGTH (§15.14.4 r6) do");

        // ⛔ Driven by the SPEC-VERIFIED table, not by sig.ArgKinds. The catalog's hint column is unaudited —
        // BYTE-LENGTH is declared "s" while §15.14.3 admits any class, and an empty ArgKinds defaults to 'n',
        // which would screen LENGTH as numeric-only. Screening from it rejected 12 legal corpus programs.
        // A function absent from Verified is not screened, so this can only ever ADD rejections that a cited
        // §15 rule demands. sig.ArgKind(i) still supplies the POSITION when the rule is per-argument.
        if (!IntrinsicArgumentRules.Verified.TryGetValue(sig.Name, out var schema)) return;

        // PER-POSITION (fix-queue PB12). `schema.At(i)` is null for a position the clause does not describe —
        // UNSCREENED, never screened by whatever the previous position declared, which is exactly the mistake a
        // one-kind-per-function table forced on FIND-STRING and the FORMATTED-* family.
        for (int i = 0; i < args.Count; i++)
        {
            if (schema.At(i) is not { } rule) continue;
            // "Argument-N shall not be a zero-length literal" (fix-queue PB35) — a LENGTH rule, not a class one,
            // so it is tested beside the class rule rather than through it. SIX §15 clauses state it and exactly
            // ONE was enforced: NATIONAL-OF's, hand-written in CheckRepertoireArgs, while MAX, MIN, ORD-MAX,
            // ORD-MIN and STANDARD-COMPARE accepted `FUNCTION MAX("" "AB")` silently at every edition.
            // ⚠ THE TEST IS ON THE LITERAL, NOT ON THE LENGTH. A zero-length ITEM is legal in every one of these
            // positions — the clauses each say "zero-length LITERAL" — so screening on width would reject legal
            // source, which is how PB1 turned 12 conforming corpus programs away.
            if (rule.NoZeroLengthClause is { } zlClause && args[i] is BoundStringLiteral { Value.Length: 0 })
                Report($"FUNCTION {sig.Name} argument-{i + 1} is a zero-length literal, which {zlClause} "
                    + "does not admit");
            // The position's OTHER predicates (kb/Work PB58) — width, operand shape, the strong-type exclusion —
            // each its own rule with its own clause; the class kind below is the last of them, not the only one.
            foreach (var p in rule.Predicates)
                if (IntrinsicArgumentRules.PredicateViolation(p, args[i], KnownWidth(args[i])) is { } pWhy)
                    Report($"FUNCTION {sig.Name} argument-{i + 1} {pWhy}");
            if (IntrinsicArgumentRules.Violation(rule, args[i]) is not { } why) continue;
            Report($"FUNCTION {sig.Name} argument-{i + 1} {why} ({rule.Clause})");
        }

        // CROSS-ARGUMENT (fix-queue PB31) — §15.59.3 r2 and its siblings, which no per-position check can see.
        if (IntrinsicArgumentRules.CrossViolation(schema, args) is { } cross)
            Report($"FUNCTION {sig.Name} {cross}");

        // CONCAT's USAGE halves (kb/Work PB58): §15.18.3 r2 "If any argument is usage national, all arguments
        // shall be usage national, otherwise all arguments shall be usage display", and r3 "If argument-1 or
        // argument-2 is numeric, it shall be usage display or national and shall be an unsigned integer" — the
        // StaticUsageOf axis (the same shared reader the BASECONVERT/CONVERT screens ride), which a class kind
        // structurally cannot carry. Static shapes only; a runtime-shaped operand (a group, a figurative) fails open.
        if (sig.Name == "CONCAT") CheckConcatArgs(args, Report);

        void Report(string where)
        {
            if (ctx.Edition.Permissive)
                ctx.Edition.Warning(DiagnosticCatalog.IntrinsicArgumentClass,
                    $"{where}; accepted under --permissive with the existing coercion");
            else
                ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass,
                    $"{where}. --permissive accepts it as a coercion extension");
        }
    }

    /// <summary>§15.18.3 r2/r3 for CONCAT (kb/Work PB58) — see the call site in <see cref="CheckArgumentClasses"/>.
    /// r2: one usage family for the whole list (national, else display) — a COMP/PACKED/BINARY argument is neither,
    /// and a display+national mixture is the disagreement. r3: a NUMERIC argument (class numeric — a numeric item
    /// or numeric literal) shall be usage display or national AND an unsigned integer: a signed or scaled numeric
    /// item, and a signed or fractional numeric literal, are rejected.</summary>
    private void CheckConcatArgs(IReadOnlyList<BoundOperand> args, Action<string> report)
    {
        bool anyNational = args.Any(a => IntrinsicArgumentRules.StaticUsageOf(a) is Usage.National);
        for (int i = 0; i < args.Count; i++)
        {
            var a = args[i];
            // r3 first: a numeric argument's own usage/sign/scale conditions.
            if (a is BoundFieldOperand { Place.DenotedItem: not null, Place.Item: { IsGroup: false, Pic: { Category: PicCategory.Numeric } np } })
            {
                if (np.Usage is not (Usage.Display or Usage.National))
                    report($"FUNCTION CONCAT argument-{i + 1} is a numeric item of usage {np.Usage}; ISO §15.18.3 r3 "
                        + "requires a numeric argument to be usage display or national");
                else if (np.Signed || np.Scale != 0)
                    report($"FUNCTION CONCAT argument-{i + 1} is a {(np.Signed ? "signed" : "non-integer")} numeric item; "
                        + "ISO §15.18.3 r3 requires a numeric argument to be an unsigned integer");
                continue;   // a numeric DISPLAY item is display-usage for r2 below by construction
            }
            if (a is BoundNumericLiteral nl)
            {
                if (nl.Text.StartsWith('-') || nl.Text.StartsWith('+') || nl.Text.IndexOfAny(['.', ',']) >= 0)
                    report($"FUNCTION CONCAT argument-{i + 1} is the numeric literal {nl.Text}; ISO §15.18.3 r3 "
                        + "requires a numeric argument to be an unsigned integer");
                continue;
            }
            // r2: the usage family.
            if (IntrinsicArgumentRules.StaticUsageOf(a) is not { } u) continue;   // fail open
            if (u is not (Usage.Display or Usage.National))
                report($"FUNCTION CONCAT argument-{i + 1} is of usage {u}; ISO §15.18.3 r2 requires every argument "
                    + "to be usage national or every argument to be usage display");
            else if (anyNational && u is Usage.Display)
                report($"FUNCTION CONCAT argument-{i + 1} is usage display while another argument is usage national; "
                    + "ISO §15.18.3 r2 — if any argument is usage national, all arguments shall be usage national");
        }
    }

    private static bool IsStringOperand(BoundOperand op) => op switch
    {
        BoundStringLiteral => true,
        // National/boolean operands participate through the char pipeline (MAX/MIN over national compares
        // ordinal per D-N3); a nested intrinsic with a string-class result (alphanumeric OR national —
        // NATIONAL-OF/CONVERT-to-NAT) is a string operand likewise. ⛔ The FIELD arm reads the ONE classifier
        // (PB59 family 7b) so the BODY choice and UniformArgumentType's TYPE resolution can never split on a
        // shape again: a group is category alphanumeric (§8.5.2.1), and a ref-mod view takes GR6's rewrites —
        // the pre-7b split crashed MAX(G1 G2) at run time ("no numeric render recipe").
        BoundFieldOperand => OperandCategory(op) is PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean,
        // A nested intrinsic with a STRING-class result — alphanumeric, national OR boolean (§15.2 types 1/2/4; the
        // boolean image is its '0'/'1' string, D-B1) — is a string operand (kb/Work PB68: the boolean result had no
        // arm and a numeric context over BOOLEAN-OF-INTEGER died with an unhandled NotImplemented at run time).
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } } => true,
        // A figurative whose ONLY reading is a character value — SPACE, QUOTE, HIGH-VALUE, LOW-VALUE (§8.3.3.6.4
        // GR5–GR8) — is a string operand exactly as a one-character alphanumeric literal is. ZERO is excluded by
        // the neutrality test rather than by name, so the §8.3.3.6.4 table stays written down once (PB48).
        BoundFigurative => !IsClassNeutralOperand(op),
        // ALL "literal" is an alphanumeric literal repeated (§8.3.3.6.4 GR9); never numeric (§8.3.3.6.3 SR1a).
        BoundAllLiteral => true,
        _ => false,
    };

    /// <summary>Can this operand present a NUMERIC class and a character one both — i.e. does it take its class
    /// from the context rather than bringing one (§8.3.3.6.4 GR4)? True for the figurative ZERO and nothing else
    /// the binder can build today.</summary>
    /// <remarks>Derived from <see cref="IntrinsicArgumentRules.CandidateClasses"/> rather than testing
    /// <c>Kind == 'Z'</c>, so the §8.3.3.6.4 GR1/GR4–GR8 reading lives in ONE table and a change there cannot
    /// leave this dispatch behind (<c>feedback_one_rule_one_place</c>).</remarks>
    private static bool IsClassNeutralOperand(BoundOperand op) =>
        IntrinsicArgumentRules.CandidateClasses(op) is { Length: > 1 } candidates
        && candidates.Contains(CobolClass.Numeric);

    /// <summary>FUNCTION LENGTH (§15.50.4): the argument's length in character positions. A literal or a
    /// fixed-size item folds to its compile-time size — <c>DataItem.ImageWidth</c> IS the character-position
    /// count (digits + a separate-sign position for numerics, §13.18.52; PICTURE length otherwise; the leaf-sum
    /// for groups; P positions occupy none). A nested string-function argument keeps a RUNTIME <c>.Length</c>
    /// over its rendered fixed-width image (the image length IS the count — equivalent to the legacy's
    /// length-recursion). Reference-modified and occurs-depending arguments have RUNTIME lengths (§15.50.4
    /// rules 4/7) — staged loud by name until a consumer exists (none in the NIST corpus; loud-failure §1.4).</summary>
    private BoundExpr BindLengthFold(IntrinsicSig sig, List<BoundOperand> args) => args[0] switch
    {
        // A zero-length literal has length ZERO (§8.5.4 — "a data item or a literal whose … length at runtime is
        // zero"; §15.50.4 r2/r3 count its positions): the former Math.Max(1, …) clamp answered 1 for `""` and
        // `N""` (kb/Work PB61 / RV-15.50.4-2).
        BoundStringLiteral s => new BoundNumLiteral(s.Value.Length.ToString()),
        // ⛔ A REFERENCE-MODIFIED ARGUMENT IS LEGAL AND ITS LENGTH IS THE RUNTIME CHANNEL'S ANSWER (fix-queue
        // PB24). §15.50.3 r1 admits "a data item of any class or category", and §8.4.3.3.3 SR5 makes a
        // reference-modified item a data item — so `FUNCTION LENGTH(WS-NAME(1:5))` is conforming source. It used
        // to bind to a BoundExprError, which COMPILES CLEAN and throws NotImplementedCobolFeatureException at RUN
        // TIME (the PB7/DA7 wrong-stage family): measured exit 127 on a five-line program.
        // No new machinery is needed. `IntrinsicRenderer`'s Length arm renders its argument through the ONE
        // string channel, and a ref-modified place renders as the SUBSTRING — so the runtime `.Length` over that
        // image IS §15.50.4's character-position count, for the literal `(1:5)` form and the runtime `(I:L)` form
        // alike, and the omitted-length `(I:)` form comes out right because the substring already ends where the
        // item does.
        BoundFieldOperand { Place: RefModPlace } => new BoundIntrinsicCall(sig, args, PicCategory.Numeric),
        // ⛔ A GROUP WHOSE LENGTH IS A RUNTIME VALUE — an OCCURS DEPENDING table beneath it (§15.50.4 r4b),
        // dynamic-length elementary items or dynamic-capacity tables beneath it (r7, the §8.5.1.12.1
        // variable-length group), in any combination — is ONE arm and ONE builder (kb/Work PB61: RV-15.50.4-4 /
        // RV-15.14.4-6 / RV-15.14.4-2). It used to be four arms: the ODO arm staged loud while its file-mates
        // were rewritten (RV-15.50.4-4), the r7c dynamic-capacity arm staged loud outright, and the ODO arm that
        // then landed ignored a dynamic-length leaf sitting BESIDE the table — an under-count with no
        // diagnostic. The builder derives every subordinate's place from the group's own path, so a subscripted
        // group works too, and names the ONE shape it will not sum (a runtime-length item INSIDE a table
        // element — a per-occurrence loop) instead of miscounting it.
        BoundFieldOperand g when g.Place.Item.IsGroup && HasRuntimeLength(g.Place.Item) =>
            VariableLengthGroupSum(sig, g, bytes: false),
        // An ANY LENGTH item's length exists only at RUNTIME (ISO §13.18.2 GR1 — n = the length of the
        // corresponding argument): never the compile-time fold, always the runtime .Length over the carrier
        // (the same BoundIntrinsicCall channel a nested string-function argument uses).
        BoundFieldOperand { Place.Item.IsAnyLength: true } =>
            new BoundIntrinsicCall(sig, args, PicCategory.Numeric),
        // A DYNAMIC LENGTH item's LENGTH is its CURRENT length in BYTES (ISO §15.50.4 rule 6 — unlike the
        // character-position count of rules 2/3), never a compile-time fold. For a PIC X item one byte per
        // character position, so the runtime .Length over the native string IS the answer (the ANY LENGTH
        // channel). A PIC N (national) item is 2 bytes per position (D-N1) — the byte count is 2 × positions,
        // and the plain .Length is the character count (half): staged loud by name until the doubling is wired
        // (no national dynamic-length consumer in the corpus; §1.4).
        // §15.50.4 r6 — a DYNAMIC LENGTH item's length is its current length in BYTES: a national item's is
        // 2 × its positions (D-N1), read from the storage channel (kb/Work PB61 / RV-15.50.4-6 — it staged loud
        // where the alphanumeric sibling one line below already rode the runtime channel).
        BoundFieldOperand { Place.Item: { IsDynamicLength: true, Pic.Category: PicCategory.National } } =>
            new BoundIntrinsicCall(sig, args, PicCategory.Numeric) { LengthInBytes = true },
        BoundFieldOperand { Place.Item.IsDynamicLength: true } =>
            new BoundIntrinsicCall(sig, args, PicCategory.Numeric),
        // ⛔ §15.50.4 r1 — AN ELEMENTARY BOOLEAN ITEM'S LENGTH IS IN BOOLEAN POSITIONS, NOT IN THE CHARACTER
        // POSITIONS IT OCCUPIES: "If argument-1 is a bit group item, an elementary boolean data item, a boolean
        // literal, or a type declaration for a boolean item, the returned value is an integer equal to the length
        // of argument-1 in boolean positions." The generic fold below reads ImageWidth, and for a USAGE BIT item
        // that is now ceil(n/8) — its OCCUPANCY (design D19, fix-queue PB43) — so `PIC 1(8) USAGE BIT` would fold
        // to 1 instead of 8.
        // ⚠ THIS ARM WAS NOT MISSING BEFORE; IT WAS UNNECESSARY, and that is the more useful way to say it. While
        // USAGE BIT was stored one character per bit, boolean positions and occupied positions were the same
        // number, so r1 and r3 could not be told apart and the generic arm answered both. Giving bit items their
        // real occupancy separates the two rules, and the separation is what makes r1 need its own arm.
        // Both usages come here: a DISPLAY-form boolean item (§13.18.60.3 SR13(b)) is equally "an elementary
        // boolean data item", and for it the answer is unchanged because occupancy IS the boolean-position count.
        BoundFieldOperand { Place.Item: { IsElementary: true, Pic.Category: PicCategory.Boolean } b } =>
            new BoundNumLiteral(b.Pic!.Length.ToString()),
        // The fixed fold — LengthPositions (kb/Work PB61 / RV-15.50.4-3): §15.50.4 r2 gives an elementary
        // usage-national item its NATIONAL positions; r3 gives everything else — an alphanumeric group, a
        // DISPLAY leaf, a COMP/PACKED leaf, an INDEX/POINTER/COMP-1/COMP-2 carrier — its length "in alphanumeric
        // character positions", which under the 1-byte-per-position model IS its byte width. The former
        // Math.Max(1, ImageWidth) answered 1 for every carrier (a POINTER is 8) and undercounted a group holding a
        // national or carrier child (X(3)+N(2) is 7, not 5).
        BoundFieldOperand f => new BoundNumLiteral(ItemLength.Positions(f.Place.Item).ToString()),
        // A nested string-result intrinsic (alphanumeric OR national — one UTF-16 char per national position,
        // D-N1, so .Length IS the §15.50.4 character-position count for both; and BOOLEAN — r1's boolean
        // positions ARE the '0'/'1' image's length, kb/Work PB68) keeps a runtime .Length.
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } } =>
            new BoundIntrinsicCall(sig, args, PicCategory.Numeric),   // runtime .Length over the nested result image
        // ⛔ A FIGURATIVE CONSTANT IS A LEGAL LENGTH ARGUMENT, AND THE ARM BELOW USED TO REFUSE IT ALONGSIDE THE
        // NUMERIC LITERAL IT CORRECTLY REFUSES (fix-queue PB25). One arm standing for TWO rules enforced neither:
        // §15.50.3 r1 restricts a LITERAL argument to "an alphanumeric, national, or boolean literal", which
        // excludes a NUMERIC literal — but says nothing about figurative constants, and §8.3.3.6.3 SR1 admits one
        // "whenever 'literal' appears in a format" except where the literal is restricted to NUMERIC (a) or a
        // syntax rule prohibits it (b). Neither exception applies, and §8.3.3.6.4 GR1 makes SPACE/QUOTE an
        // ALPHANUMERIC character value here — precisely what r1 permits. So `FUNCTION LENGTH(SPACE)` is legal.
        // The length is §8.3.3.6.4 GR3's, the "length not specified by the context" case that a bare argument is:
        //   (b) a figurative other than ALL literal-1 is ONE character;
        //   (c) otherwise, the length of literal-1.
        // Both are compile-time constants, so they fold exactly like the literal arm above rather than reaching
        // the runtime channel.
        BoundFigurative => new BoundNumLiteral("1"),                                     // §8.3.3.6.4 GR3b
        BoundAllLiteral a => new BoundNumLiteral(a.Literal.Length.ToString()),           // §8.3.3.6.4 GR3c
        // A COUNTER REGISTER (LINAGE-/LINE-/PAGE-COUNTER) is a DATA ITEM — "a temporary unsigned integer data item"
        // (§8.4.3.14.4 GR1 / §8.4.3.15.4 GR1) — so §15.50.3 r1's "a data item of any class or category" admits it;
        // it used to fall to the default arm below and be refused as "a numeric literal" (kb/Work PB1153). Its
        // length is §15.50.4 r3's alphanumeric character positions of its implicit description, PIC 9(d) USAGE DISPLAY
        // (docs/CONFORMANCE.md, the counter registers' declared capacity) — d, one position per digit.
        BoundComputedOperand { Expr: var reg } when AlgebraicRanges.CounterRegisterDigits(reg) is { } regDigits =>
            new BoundNumLiteral(regDigits.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        // An argument that already failed to bind (a nested call the binder rejected) is already loud — no
        // second, misattributed report ("is a numeric literal") on top of it (kb/Work PB63).
        BoundOperandError e => BoundExprError.Carry(e.Feature, e.IsUnbuilt),
        // A NUMERIC literal is still not a valid LENGTH argument — §15.50.3 r1 admits only "an alphanumeric,
        // national, or boolean literal" (a numeric *data item* is allowed as "a data item of any class or
        // category", handled by the BoundFieldOperand arm above). That half of the old arm was always correct.
        // A boolean EXPRESSION (a B-operator argument, kb/Work PB65) is neither an identifier nor a literal —
        // §15.50.3 r1 admits an item, a literal, a based entry or a type-name; name what was written, never
        // "a numeric literal" (the misattributed default that once covered a group ref-mod too, kb/Work PB70).
        BoundBoolOperand => InadmissibleArgument(sig,
            "is a boolean expression, which §15.50.3 r1 does not admit — it takes an alphanumeric, national or "
            + "boolean LITERAL, a based entry, a type-name, or a DATA ITEM of any class (write the expression's "
            + "result to a boolean item and take its LENGTH)", "§15.50.3 r1"),
        _ => InadmissibleArgument(sig,
            "is a numeric literal, which §15.50.3 r1 does not admit — it takes an alphanumeric, national or "
            + "boolean literal, a based entry, a type-name, or a DATA ITEM of any class (a numeric ITEM is fine)",
            "§15.50.3 r1"),
    };

    /// <summary>A bare-word argument that names a TYPE (§15.50.3 r1 / §15.14.3 r1: "… a based entry, a type-name,
    /// or a data item of any class or category") — a level-1 TYPEDEF lives in <c>DataBinder.TypeDecls</c>, off the
    /// data-name namespace, so <c>BindArgOperand</c> would report it undefined (kb/Work PB61 / AR-15.50.3-1 /
    /// AR-15.14.3-1). Returns the type's template item, or null when the word is not a type-name (a data-name
    /// of the same spelling cannot exist beside it — one user-word namespace).</summary>
    private DataItem? TypeNameArgument(Core.FunctionArgumentContext a) =>
        KeywordWordOf(a) is { } w && a.fnArgPhraseWord() is null && ctx.Data.TypeDecls.TryGetValue(w, out var t) ? t : null;

    /// <summary>
    /// FUNCTION LENGTH (§15.50.2) — argument-1 plus the optional PHYSICAL keyword.
    /// </summary>
    /// <remarks>
    /// <para>⛔ <b>PHYSICAL IS A KEYWORD, NOT AN ARGUMENT</b>, and the generic argument path counted it as one:
    /// `FUNCTION LENGTH(WS-G PHYSICAL)` — conforming source — was rejected with "COBOLNET1504: FUNCTION LENGTH
    /// takes 1 argument(s); 2 given" (fix-queue PB24). Consumed here exactly as ANYCASE is for the NUMVAL-C
    /// family, so no grammar change is needed: a bare word already parses as an argument context.</para>
    /// <para>⚖ <b>WHAT IT RETURNS IS AN IMPLEMENTOR DETERMINATION, AND §15.50.4 r8 IS THE ONE THAT MAKES IT
    /// SMALL.</b> r8's closing sentence: <i>"If argument-1 is physically located where it is defined, LENGTH
    /// returns the same value that would be returned had the PHYSICAL argument not been specified."</i> WiseOwl COBOL
    /// determines that a variable-length group IS physically located where it is defined — the program has no
    /// addressable out-of-line pointer to observe, and the group presents as a contiguous character image at its
    /// defined position — so PHYSICAL returns the r7 value. The alternative reading (r8's middle sentence: "the
    /// returned value includes only the length of the implementor-defined pointer") would require inventing a
    /// user-visible pointer width that nothing in this implementation exposes. Recorded in
    /// <c>docs/CONFORMANCE.md</c> per §4.2.16.</para>
    /// <para>The keyword is therefore ACCEPTED and semantically transparent — which is the whole defect: the
    /// prior behaviour was not a different answer, it was a REJECTION of legal source.</para>
    /// </remarks>
    private BoundExpr BindLengthFamily(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        bool physical = false, isByte = sig.Name == "BYTE-LENGTH";
        var operands = new List<BoundOperand>();
        DataItem? typeArg = null;
        for (int i = 0; i < argCtxs.Count; i++)
        {
            var a = argCtxs[i];
            // ⛔ POSITIONAL (kb/Work PB124 — AR-15.3-7): §15.50.2 / §15.14.2 print `argument-1 [ PHYSICAL ]`,
            // and PHYSICAL is NOT a reserved word (§8.10 has no row; reserved-words.json r2023: false), so the
            // word is the keyword ONLY in the keyword's position — after argument-1, last, once. Anywhere
            // else it is an ordinary word: `LENGTH(PHYSICAL)` measures a data item NAMED PHYSICAL (the old
            // unordered swallow consumed the user's data-name and degraded the call to zero arguments).
            if (KeywordWordOf(a) == "PHYSICAL" && operands.Count == 1 && !physical && i == argCtxs.Count - 1)
            {
                physical = true;
                continue;
            }
            // A type-name argument (§15.50.3 r1 / §15.14.3 r1 — kb/Work PB61): the length of the type
            // declaration — for a subordinate OCCURS DEPENDING, "the rules of the OCCURS clause for a receiving
            // data item" (§15.50.4 r4a / §15.14.4 r2a), i.e. the maximum, which is what the template's widths hold.
            if (TypeNameArgument(a) is { } t) { typeArg = t; operands.Add(new BoundNumericLiteral("0")); continue; }   // a placeholder for the arity count
            operands.Add(BindArgOperand(a));
        }
        if (operands.Count != 1)
        {
            ctx.Edition.Error("COBOLNET1504",
                $"FUNCTION {sig.Name} takes argument-1 [PHYSICAL] (ISO {(isByte ? "§15.14.2" : "§15.50.2")}); {operands.Count} operand argument(s) given");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} arity");
        }
        if (typeArg is not null)
        {
            // §15.3's second trailing sentence (kb/Work PB124 wave 4): "If any function permits a type
            // declaration as an argument, the type declaration shall not describe a variable-length group."
            if (IsVariableLengthGroup(typeArg))
            {
                ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass, $"FUNCTION {sig.Name}: the type "
                    + $"declaration '{typeArg.CobolName}' describes a variable-length group, which ISO §15.3 "
                    + "does not admit as a type-declaration argument");
                return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} type argument");
            }
            return new BoundNumLiteral((isByte ? typeArg.ByteWidth : ItemLength.Positions(typeArg)).ToString());
        }
        // ⛔ THE §15.3 SCREEN IS CALLED HERE BECAUSE THIS BINDER RETURNS BEFORE THE GENERIC ONE REACHES IT
        // (fix-queue PB12). CheckArgumentClasses sits after arity on the generic path and its comment
        // claimed it ran "before every per-function arm" — FALSE for the eight bespoke binders that
        // `return` above it, so no Verified row could ever screen them. Screened here, after this
        // binder's own arity check, exactly as the generic path orders it.
        CheckArgumentClasses(sig, operands);
        _ = physical;   // §15.50.4 r8 / §15.14.4 r7 — see the remarks: transparent under this implementation's determination.
        return isByte ? BindByteLengthFold(sig, operands) : BindLengthFold(sig, operands);
    }

    /// <summary>Does this group have a DYNAMIC LENGTH elementary item somewhere beneath it? (§15.50.4 r7b.)
    /// Recursive, because r7 says "all subordinate", not "all immediate children".</summary>
    private static bool HasDynamicLengthLeaf(DataItem g) =>
        g.Children.Any(c => c.RedefinesTargetName is null
                            && (c.IsDynamicLength || (c.IsGroup && HasDynamicLengthLeaf(c))));

    /// <summary>Does this group have a DYNAMIC-CAPACITY table beneath it? (§15.50.4 r7c / §15.14.4 r6c.)</summary>
    private static bool HasDynamicCapacityTable(DataItem g) =>
        g.Children.Any(c => c.RedefinesTargetName is null
                            && (c.IsDynamicTable || (c.IsGroup && HasDynamicCapacityTable(c))));

    /// <summary>Is this group's length a RUNTIME value — an OCCURS DEPENDING table (§15.50.4 r4b / §15.14.4 r2b),
    /// or a dynamic-length elementary item or dynamic-capacity table (r7 / r6 — the §8.5.1.12.1 variable-length
    /// group) anywhere beneath it? The ONE predicate behind the folds' group arm and <see cref="KnownWidth"/>.</summary>
    /// <summary>THE §8.5.1.12.1 DEFINITION, verbatim: "A variable-length group is a group item whose data
    /// description has at least one dynamic-length elementary item or dynamic-capacity table as a subordinate
    /// item. All other group items are referred to as fixed-length groups." — an OCCURS DEPENDING group is a
    /// FIXED-length group (its maximum is fixed), so the §15.3 prohibitions do NOT reach it; using
    /// <see cref="HasRuntimeLength"/> here over-rejected pb61's §15.50.4 r4a typedef leg, and the gate caught
    /// it (kb/Work PB124 wave 4).</summary>
    /// <summary>Delegates to THE one predicate (kb/Work PB132 — this was a second copy; §13.18.44.3 SR17
    /// bars a dynamic item under REDEFINES, so the old redefines-skip changed nothing on legal source).
    /// <see cref="HasDynamicLengthLeaf"/>/<see cref="HasDynamicCapacityTable"/> stay: they serve
    /// <see cref="HasRuntimeLength"/>, which answers a LAYOUT question, not the §8.5.1.12.1 classification.</summary>
    private static bool IsVariableLengthGroup(DataItem g) =>
        CobolNet.Binding.ReferenceResolver.HasVariableLengthSubordinate(g);

    private static bool HasRuntimeLength(DataItem g) =>
        HasOdoBeneath(g) || HasDynamicLengthLeaf(g) || HasDynamicCapacityTable(g);

    /// <summary>
    /// The length of a group whose length is a RUNTIME value, as ONE expression — §15.50.4 r4b + r7 (LENGTH) /
    /// §15.14.4 r2b + r6 (BYTE-LENGTH), kb/Work PB61: the fixed subordinates, plus the CURRENT extent of an OCCURS
    /// DEPENDING table (r4b — "the rules of the OCCURS clause for a sending data item", §13.18.38 GR8), plus the
    /// CURRENT length of every dynamic-length elementary item (r7b), plus every dynamic-capacity table's current
    /// capacity × its element width (r7c). Every unit is BYTES: an alphanumeric group's r3 "alphanumeric character
    /// positions" ARE its bytes (<see cref="LengthPositions"/>), and r6 makes a dynamic-length item's length a
    /// byte count.
    /// </summary>
    /// <remarks>
    /// <para>The fixed part is <see cref="DataItem.ByteWidth"/> — one width walk, the one every layout depends on —
    /// CORRECTED for what that walk cannot know: it counts an ODO table at its MAXIMUM (integer-2), a
    /// dynamic-capacity table as ONE occurrence and a dynamic-length leaf as zero, so the builder subtracts the
    /// first two and adds the runtime term for each. Two independent width walks would be two things to keep in
    /// agreement.</para>
    /// <para>Every subordinate's place is DERIVED from the group's own access path (a member segment per level),
    /// so a group that is itself a table element or a nested member sums correctly — nothing is re-resolved by
    /// name from the root. The ODO term reads data-name-1 through the <see cref="OdoGroupPlace"/> the resolver
    /// already wrapped the operand in.</para>
    /// <para>⚠ THE ONE SHAPE NOT SUMMED, NAMED: a runtime-length item INSIDE a table element (a dynamic-length
    /// leaf or a dynamic-capacity table under a fixed, ODO or dynamic OCCURS) — its total is a per-occurrence
    /// loop over the table, and the standard's own phrase for r7c ("based on their current capacity") defines only
    /// the fixed-element case. Reported as a named loud stage (§1.4), never as an under-count. A bit-bearing group
    /// (its ByteWidth is the §8.5.1.6.3 layout extent, not a sum) is likewise staged rather than corrected by
    /// subtraction.</para>
    /// </remarks>
    private BoundExpr VariableLengthGroupSum(IntrinsicSig sig, BoundFieldOperand op, bool bytes)
    {
        string rules = bytes ? "§15.14.4 r2b/r6" : "§15.50.4 r4b/r7";
        OdoGroupPlace? odo = op.Place as OdoGroupPlace;
        // The STORAGE place, every decoration peeled (kb/Work PB393) — the switch two lines down is over
        // storage FORMS, and `odo?.Inner` peeled exactly one known layer, so any other (or nested) decoration
        // fell into its `_ => null` and staged the whole intrinsic loud.
        Place inner = op.Place.Undecorated;
        DataItem group = inner.Item;
        AccessPath? basePath = inner switch { MemberPlace m => m.Path, DynTablePlace d => d.Path, _ => null };
        BoundExprError Stage(string what) => BoundExprError.Unbuilt(ctx.Edition, $"FUNCTION {sig.Name} of '{group.CobolName ?? group.CsName}': {what} (ISO {rules})");

        if (group.HasBitDescendant)
            return Stage("a group holding USAGE BIT items and a runtime-length subordinate — its fixed extent is a "
                         + "§8.5.1.6.3 layout, not a sum");
        long fixedPart = group.ByteWidth;
        BoundExpr? runtime = null;
        void Add(BoundExpr e) => runtime = runtime is null ? e : new BoundBinary(runtime, '+', e);
        BoundExprError? failure = null;

        void Walk(DataItem g, AccessPath? path)
        {
            foreach (var c in g.Children.Where(x => x.RedefinesTargetName is null))
            {
                if (failure is not null) return;
                var cPath = path?.Add(new MemberSegment(c.CsName));
                if (c.IsDynamicTable)
                {
                    // r7c / r6c — the current capacity × the element width. ByteWidth counted ONE occurrence.
                    if (HasRuntimeLength(c) || (c.IsElementary && c.IsDynamicLength))
                    { failure = Stage($"the dynamic-capacity table '{c.CobolName ?? c.CsName}' has a runtime-length element — a per-occurrence sum"); return; }
                    if (cPath is null || c.OccursSpec?.CapacityRegister is not { } reg)
                    { failure = Stage($"the dynamic-capacity table '{c.CobolName ?? c.CsName}' could not be addressed"); return; }
                    fixedPart -= c.ByteWidth;
                    Add(new BoundBinary(new BoundNumRef(new CapacityRegisterPlace(cPath, reg)), '*',
                                        new BoundNumLiteral(c.ByteWidth.ToString())));
                }
                else if (c.OccursSpec is { DependingName: not null } spec)
                {
                    // r4b / r2b — the ODO table's CURRENT extent: data-name-1's clamped count × the element
                    // width (EC-BOUND-ODO outside integer-1..integer-2, §13.18.38.4 GR7). ByteWidth counted the
                    // MAXIMUM. data-name-1's place is the one the resolver wrapped the operand with (SR22 makes
                    // the table the record's trailing part, so the operand IS the OdoGroupPlace).
                    if (HasRuntimeLength(c))
                    { failure = Stage($"the OCCURS DEPENDING table '{c.CobolName ?? c.CsName}' has a runtime-length element — a per-occurrence sum"); return; }
                    if (odo is null)   // the resolver wraps every ODO group operand — struct member or class-tier window (kb/Work PB80); reaching here is a shape it could not resolve
                    { failure = Stage($"the OCCURS DEPENDING table '{c.CobolName ?? c.CsName}' has no resolvable data-name-1 place"); return; }
                    int elem = c.ByteWidth, max = c.Occurs ?? 1;
                    fixedPart -= (long)elem * max;
                    // r4a / r2a — a BASED entry not associated with actual data answers the maximum (GR8b): the
                    // entry's data-address field guards the ODO term (kb/Work PB80).
                    Add(new BoundOdoExtent(odo.Depending, spec.Min, max, 0, elem)
                        { BasedAddress = group.Class?.BasedPointerField });   // null for a non-BASED class
                }
                else if (c.Occurs is not null)
                {
                    // A fixed table: wholly inside ByteWidth — unless an occurrence carries a runtime length.
                    if (HasRuntimeLength(c) || c.IsDynamicLength)
                    { failure = Stage($"the table '{c.CobolName ?? c.CsName}' has a runtime-length element — a per-occurrence sum"); return; }
                }
                else if (c.IsDynamicLength)
                {
                    // r7b / r6b — the CURRENT length of the leaf, in bytes: LENGTH reads a national leaf's storage
                    // channel (LengthInBytes — r6's byte count); BYTE-LENGTH is the storage image already.
                    // A CELL-BACKED group's dynamic-length subordinate (an EXTERNAL record, an ADDRESS-OF-taken
                    // record — kb/Work PB1026) has no member path: its content rides the cell's dynamic slot, and
                    // its place is built by THE ONE window builder over the same cell (Place.DynSlotWindow).
                    Place? cPlace = cPath is not null ? new MemberPlace(cPath, c)
                        : inner is RedefViewPlace { Coding: VarGroupWindow vg } rv
                            ? RedefViewPlace.For(rv.Backing, c, rv.OffsetExpr, cell: vg.Cell)
                            : null;
                    if (cPlace is null) { failure = Stage($"the dynamic-length subordinate '{c.CobolName ?? c.CsName}' could not be addressed"); return; }
                    Add(new BoundIntrinsicCall(sig, [new BoundFieldOperand(cPlace)], PicCategory.Numeric)
                            { LengthInBytes = !bytes && c.Pic is { Category: PicCategory.National } });
                }
                else if (c.IsGroup) Walk(c, cPath);
            }
        }
        Walk(group, basePath);
        if (failure is not null) return failure;
        if (fixedPart < 0) return Stage("its fixed extent is negative (an internal width disagreement)");
        BoundExpr sum = new BoundNumLiteral(fixedPart.ToString());
        return runtime is null ? sum : new BoundBinary(sum, '+', runtime);
    }

    /// <summary>FUNCTION BYTE-LENGTH (§15.14.4 r1): the argument's length in BYTES — the compile-time twin of the
    /// LENGTH fold, counting bytes instead of character positions (D7). §15.14.3 r1 restricts a LITERAL argument
    /// to an ALPHANUMERIC or NATIONAL literal (unlike LENGTH, a boolean/numeric literal is NOT valid): an
    /// alphanumeric literal is 1 byte/char, a national literal 2 bytes/char (D-N1). A fixed data item folds to
    /// <see cref="DataItem.ByteWidth"/> (the pinned per-usage widths). Runtime-length shapes — a reference-modified
    /// view, a variable-length (OCCURS DEPENDING) group, or an ANY LENGTH item (§15.14.4 r2/r5) — have a byte
    /// length known only at runtime; with no runtime BYTE-LENGTH body (the §15.14 CONSTANT-entry path aside) they
    /// stage LOUD by name, the LENGTH discipline (§1.4).</summary>
    /// <summary>An argument the §15 rules make INADMISSIBLE, reported where it is decidable — at BIND (fix-queue
    /// PB52 cause 3). Returns a <see cref="BoundExprError"/> so the caller's expression shape is unchanged.</summary>
    /// <remarks>
    /// ⛔ THE DISTINCTION THIS DRAWS IS THE POINT, because the LENGTH/BYTE-LENGTH folds return
    /// <c>BoundExprError</c> for TWO different reasons and only one of them is a defect. A ref-modified, ODO,
    /// ANY LENGTH or DYNAMIC LENGTH argument has a RUNTIME length these compile-time folds genuinely cannot
    /// produce — a staged gap, correctly loud where it is discovered. **An inadmissible LITERAL is not that**:
    /// §15.14.3 r1 and §15.50.3 r1 decide it from the source text alone, so reporting it at run time is the
    /// wrong-STAGE family ([[PB47]]'s shape), not a missing capability.
    /// <para>
    /// ⚠ IT IS A HARD ERROR AND DOES NOT TAKE THE <c>--permissive</c> DOWNGRADE the class screen's
    /// <c>Report</c> helper applies. That helper's warning says "accepted … with the existing coercion", and
    /// here there is no coercion to accept: the fold has no value it could produce for a numeric literal, so a
    /// warning would be followed by the identical run-time abort. Permissive is the migration mode for
    /// constructs an EDITION removed, and this is illegal in every edition.
    /// </para>
    /// </remarks>
    private BoundExpr InadmissibleArgument(IntrinsicSig sig, string why, string clause)
    {
        ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass,
            $"FUNCTION {sig.Name} argument-1 {why} (ISO {clause})");
        return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} argument ({why}, ISO {clause})");
    }

    private BoundExpr BindByteLengthFold(IntrinsicSig sig, List<BoundOperand> args) => args[0] switch
    {
        // A zero-length literal is ZERO bytes (§8.5.4; §15.14.4 r1) — the Max(1, …) clamp is gone (kb/Work PB61 / RV-15.14.4-1).
        BoundStringLiteral { Category: PicCategory.National } s => new BoundNumLiteral((2 * s.Value.Length).ToString()),
        BoundStringLiteral { Category: PicCategory.Alphanumeric } s => new BoundNumLiteral(s.Value.Length.ToString()),
        // A literal of any OTHER class — in practice a BOOLEAN literal, since the alphanumeric and national
        // arms matched above. §15.14.3 r1 admits "an alphanumeric or national literal" and stops there.
        // ⚠ THE SIBLING CLAUSE DIFFERS AND THE TWO MUST NOT BE UNIFIED: §15.50.3 r1 admits "an alphanumeric,
        // national, or BOOLEAN literal", so `FUNCTION LENGTH(B"101")` is legal where BYTE-LENGTH's is not.
        BoundStringLiteral => InadmissibleArgument(sig,
            "is a literal of a class §15.14.3 r1 does not admit — it takes an alphanumeric or national literal "
            + "(a boolean literal is admissible to FUNCTION LENGTH, §15.50.3 r1, but not here)", "§15.14.3 r1"),
        // ⛔ THE RUNTIME SHAPES ROUTE TO THE RUNTIME BODY, EXACTLY AS LENGTH's DO (kb/Work PB61 — every one of
        // these was a BoundExprError that COMPILED CLEAN and threw NotImplementedCobolFeatureException at run time,
        // the wrong-stage family, while the LENGTH twin one method up had been given its runtime channel). The
        // body is the STORAGE image's length: a reference-modified view (§8.4.3.3.4 GR6 — a national slice keeps
        // category national and its bytes are UTF-16BE), an ANY LENGTH or DYNAMIC LENGTH item's current bytes
        // (§15.14.4 r5); an OCCURS DEPENDING group's current extent in bytes (§15.14.4 r2b) is the group arm
        // below — VariableLengthGroupSum builds it from data-name-1's clamped value and the byte widths.
        BoundFieldOperand { Place: RefModPlace } => new BoundIntrinsicCall(sig, args, PicCategory.Numeric),
        // §15.14.4 r2b + r6 — a group whose length is a runtime value (an OCCURS DEPENDING table, dynamic-length
        // items, dynamic-capacity tables beneath it): the ONE builder LENGTH uses, in bytes (kb/Work PB61).
        BoundFieldOperand g when g.Place.Item.IsGroup && HasRuntimeLength(g.Place.Item) =>
            VariableLengthGroupSum(sig, g, bytes: true),
        BoundFieldOperand { Place.Item.IsAnyLength: true } => new BoundIntrinsicCall(sig, args, PicCategory.Numeric),
        BoundFieldOperand { Place.Item.IsDynamicLength: true } => new BoundIntrinsicCall(sig, args, PicCategory.Numeric),
        BoundFieldOperand f => new BoundNumLiteral(f.Place.Item.ByteWidth.ToString()),
        // A COUNTER REGISTER is a data item (§8.4.3.14.4 GR1 / §8.4.3.15.4 GR1), admitted by §15.14.3 r1's
        // "a data item of any class or category"; its implicit PIC 9(d) USAGE DISPLAY occupies d bytes (the
        // LENGTH twin above — kb/Work PB1153).
        BoundComputedOperand { Expr: var reg } when AlgebraicRanges.CounterRegisterDigits(reg) is { } regDigits =>
            new BoundNumLiteral(regDigits.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        BoundOperandError e => BoundExprError.Carry(e.Feature, e.IsUnbuilt),   // already loud (kb/Work PB63)
        // ⛔ THE FIGURATIVE HALF OF THE ARM BELOW WAS FALSE, AND IT IS PB25's OWN DEFECT IN THE ADJACENT METHOD
        // (fix-queue PB48 sweep). PB25 gave BindLengthFold its §8.3.3.6.4 GR3 arms and cited the reasoning in
        // full; BYTE-LENGTH — the neighbouring fold, with the same rule shape — kept a default arm that named
        // "a numeric/FIGURATIVE literal" as invalid on the authority of §15.14.3. §15.14.3 r1 says the opposite:
        // "Argument-1 shall be an alphanumeric or national literal, a based entry, a type-name, or a data item of
        // any class or category", and §8.3.3.6.4 GR1 makes a figurative in a character context an ALPHANUMERIC
        // character value — exactly what r1 admits. §8.3.3.6.3 SR1 bars a figurative only where the literal is
        // restricted to NUMERIC (a) or a syntax rule prohibits it (b); neither applies. So
        // `FUNCTION BYTE-LENGTH(SPACE)` is legal source that aborted at RUN TIME, and it did so before PB48 as
        // well — ZERO merely joined it once the figurative stopped being rewritten (feedback_scan_all_similar).
        // The byte count follows §8.3.3.6.4 GR3: (b) one character for a figurative other than ALL literal-1,
        // (c) the length of literal-1 otherwise; both are alphanumeric here, so one byte per character (D-N1).
        BoundFigurative => new BoundNumLiteral("1"),                                        // §8.3.3.6.4 GR3b
        BoundAllLiteral a => new BoundNumLiteral(a.Literal.Length.ToString()),              // §8.3.3.6.4 GR3c
        // A NUMERIC literal remains invalid — §15.14.3 r1 admits only "an alphanumeric or national literal"
        // (a numeric DATA ITEM is "a data item of any class or category" and folds on the arm above).
        BoundBoolOperand => InadmissibleArgument(sig,
            "is a boolean expression, which §15.14.3 r1 does not admit — it takes an alphanumeric or national "
            + "LITERAL, a based entry, a type-name, or a DATA ITEM of any class", "§15.14.3 r1"),   // kb/Work PB65
        _ => InadmissibleArgument(sig,
            "is a numeric literal, which §15.14.3 r1 does not admit — it takes an alphanumeric or national "
            + "literal, a based entry, a type-name, or a DATA ITEM of any class (a numeric ITEM is fine)",
            "§15.14.3 r1"),
    };

    /// <summary>SMALLEST/HIGHEST/LOWEST-ALGEBRAIC (§15.83 / §15.43 / §15.58) — a compile-time PICTURE fold, like
    /// LENGTH. Argument-1 must be a category-numeric DATA ITEM (SMALLEST §15.83.3 r1) or numeric/numeric-edited
    /// DATA ITEM (HIGHEST/LOWEST §15.43.3/§15.58.3 r1) — never a literal, expression, group, ref-mod, or function.
    /// SMALLEST = 10^(−scale) (the smallest positive increment, r2). HIGHEST/LOWEST = the greatest/lowest value the
    /// PICTURE represents: all-nines (10^Digits−1) for a DigitCount item, the two's-complement container range for a
    /// BinaryCapacity item (COMP-5 / BINARY-CHAR family), the mask capacity for a numeric-edited item; LOWEST is the
    /// negated magnitude for a sign-representable item, else 0 (Annex D.32). The value returns as a numeric literal
    /// at the correct scale (BoundNumLiteral — the LENGTH-fold precedent).</summary>
    private BoundExpr BindAlgebraicFold(IntrinsicSig sig, List<BoundOperand> args)
    {
        // §8.5.2.12 items 3/4/5 make the LINAGE-/LINE-/PAGE-COUNTER registers category-numeric DATA ITEMS, so
        // §15.43.3/§15.58.3/§15.83.3 r1 ADMITS them (kb/Work R26 — they used to draw the r1 rejection). Their
        // extremes are AlgebraicRanges.OfCounterRegister's — the ONE statement of the registers' capacity, which
        // the §8.8.4.4.4 class conditions read too (kb/Work PB1401).
        if (args[0] is BoundComputedOperand { Expr: var register } && AlgebraicRanges.OfCounterRegister(register) is { } counter)
            return CounterRegisterFold(sig, counter);
        if (args[0] is not BoundFieldOperand f || f.Place is RefModPlace || f.Place.Item.IsGroup
            || f.Place.Item.Pic is not { } pic)
            return AlgebraicArgError(sig);
        // r1 admits "a data item … and shall not be an integer function or numeric function" — an exclusion
        // that must be WRITTEN because §8.5.2.12 items 6/7 make functions category numeric too. A USER-DEFINED
        // function's result binds to a synthesized caller temp that wears BoundFieldOperand exactly like
        // declared data, so `FUNCTION HIGHEST-ALGEBRAIC(FUNCTION MY-FN)` folded a constant from the TEMP's
        // PICTURE (kb/Work R26, ledger F76). The temp flag is the positive is-a-declared-item test — placed
        // HERE, not in OperandOf: every other verb legitimately needs the temp→operand mapping.
        if (f.Place.Item.IsCompilerTemp)
        {
            string fsec = sig.Name == "SMALLEST-ALGEBRAIC" ? "15.83.3"
                : sig.Name == "HIGHEST-ALGEBRAIC" ? "15.43.3" : "15.58.3";
            ctx.Edition.Error("COBOLNET1516", $"FUNCTION {sig.Name}: argument-1 is a FUNCTION RESULT, and "
                + $"ISO §{fsec} rule 1 admits a data item and \"shall not be an integer function or numeric "
                + "function\" — name the data item whose description defines the range instead");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} function argument");
        }
        // §15.83.3 r1: SMALLEST admits ONLY category numeric; HIGHEST/LOWEST also admit numeric-edited.
        bool edited = pic.Category is PicCategory.NumericEdited;
        if (pic.Category is not PicCategory.Numeric && !(edited && sig.Name != "SMALLEST-ALGEBRAIC"))
            return AlgebraicArgError(sig);
        if (pic.Usage is Usage.Index)   // class index — not category numeric (§13.18.60)
            return AlgebraicArgError(sig);

        // Float usage — kb/Work R10 (Phase-B F72) + PB122: ALL THREE functions share one mode-aware path —
        // the r2/r3 usage bars, the §15.x.4 r1 IN-ARITHMETIC-RANGE screen, then the per-function fold. Under
        // NATIVE arithmetic nothing bars any float (§8.5.2.12 item 2 makes every float usage category
        // numeric); §15.43.4 r2 / §15.58.4 r2 define HIGHEST/LOWEST as the greatest finite magnitude
        // representable in argument-1 (negated for LOWEST — every float usage is signed), §15.83.4 r2
        // SMALLEST as the smallest. R10's guard history: the original rejected ALL floats for ALL THREE and
        // claimed COMP-1/COMP-2 are barred by rule 2 under STANDARD-DECIMAL — false, rule 2 bars only the
        // §3.166 STANDARD BINARY usages; R10 fixed HIGHEST/LOWEST but left SMALLEST refusing every float on
        // the r4 native latitude, which cannot license the refusal under the STANDARD modes (PB122 —
        // A.1 item 180's documented restriction is now "none beyond rules 2 and 3", CONFORMANCE.md).
        if (pic.IsFloat)
        {
            string fsec3 = sig.Name == "SMALLEST-ALGEBRAIC" ? "15.83.3"
                : sig.Name == "HIGHEST-ALGEBRAIC" ? "15.43.3" : "15.58.3";
            // The two families through the ONE place they are written down (UsageFamilies, PicInfo.cs) — §3.166
            // and §3.167 name them as terms and several rules key on those exact sets, so a second spelling here
            // is a set that can silently disagree when a member is added (kb/Work PB174).
            bool stdBinaryUsage = UsageFamilies.IsStandardBinaryFloat(pic.Usage);
            bool stdDecimalUsage = UsageFamilies.IsStandardDecimalFloat(pic.Usage);
            if (ctx.Data.Options.Arithmetic == ArithmeticMode.StandardDecimal && stdBinaryUsage)
            {
                ctx.Edition.Error("COBOLNET1516", $"FUNCTION {sig.Name}: under STANDARD-DECIMAL arithmetic "
                    + "argument-1 shall not specify a standard binary floating-point usage "
                    + $"(ISO §{fsec3} rule 2)");
                return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} float argument");
            }
            if (ctx.Data.Options.Arithmetic == ArithmeticMode.StandardBinary && stdDecimalUsage)
            {
                ctx.Edition.Error("COBOLNET1516", $"FUNCTION {sig.Name}: under STANDARD-BINARY arithmetic "
                    + "argument-1 shall not specify a standard decimal floating-point usage "
                    + $"(ISO §{fsec3} rule 3)");
                return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} float argument");
            }
            // §15.83.4 r1 (kb/Work PB122; the float-ITEM twin of the float-EDITED screen below): the entry
            // shall be such that every value it permits passes an IN-ARITHMETIC-RANGE test — the carrier's
            // extremes against the MODE's intermediate range (§8.8.4.4.4 GR3 l), which is
            // ArithmeticModes.IntermediateExponentRange and nowhere else (kb/Work PB194). Every carrier
            // declarable today (binary32 ±38/−45, binary64 ±308/−324, decimal ±28) passes under every reachable
            // mode, so this arm is LATENT: it fires only if a wider carrier (a true binary128, say) ever lands,
            // and then automatically. Its sibling below — the float-EDITED arm, whose PICTURE can name any
            // exponent width — is the one the mode-set drift was observable through.
            {
                var (farthestExp, closestExp) = pic.Usage switch
                {
                    Usage.Float or Usage.FloatShort or Usage.FloatBinary32 => (38, -45),
                    Usage.FloatDecimal16 or Usage.FloatDecimal34 => (28, -28),
                    _ => (308, -324),
                };
                // ⛔ THE MODE'S OWN BOUNDS, FROM THE ONE TABLE (kb/Work PB194) — this site used to test
                // `== StandardDecimal` alone, so ARITHMETIC IS STANDARD screened against binary64 while its
                // engine was the SDIDI. ArithmeticModes.IntermediateExponentRange is the single home.
                var (modeFar, modeNear) = ArithmeticModes.IntermediateExponentRange(ctx.Data.Options.Arithmetic);
                if (farthestExp > modeFar || closestExp < modeNear)
                {
                    ctx.Edition.Error(DiagnosticCatalog.AlgebraicFloatEditedRange, $"FUNCTION {sig.Name}: "
                        + $"argument-1's usage describes values as far from zero as 1E+{farthestExp} and as near "
                        + $"as 1E{closestExp}, outside the {ArithmeticModes.IntermediateName(ctx.Data.Options.Arithmetic)} "
                        + "intermediate's range — the entry shall be such that its values pass an "
                        + $"IN-ARITHMETIC-RANGE test (ISO §{fsec3[..^2]}.4 rule 1; §8.8.4.4.4 GR3 l)");
                    return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} float argument");
                }
            }
            // SMALLEST — §15.83.1/§15.83.4 r2: the smallest algebraic difference between two values the item
            // can represent = the CARRIER's smallest positive (subnormal) value — a property of the FORMAT,
            // like 10^(−scale) for fixed-point (kb/Work PB122: the old blanket refusal claimed the increment
            // was "exponent-dependent, not a PICTURE property" — that confuses ULP-of-a-value with the format
            // minimum, and r4's implementor latitude it leaned on covers only NATIVE arithmetic anyway).
            // The constants themselves are AlgebraicRanges' (kb/Work PB452) — SET format 15 needs the same
            // three values and a second table would be the same rule written twice.
            return FoldFrom(sig, pic, AlgebraicRanges.Of(pic, ctx.Data.DecimalPointIsComma));
        }

        // (SMALLEST needs no early return of its own any more: §15.83.3 r1 admits category numeric ONLY, so the
        // float-edited screen below cannot fire for it, and its 10^(−scale) increment is one of the three
        // extremes AlgebraicRanges computes.)
        // A FLOATING-POINT numeric-edited argument-1 (D21/PB66; §15.43.4 r1-r2 / §15.58.4 r1-r2): the extreme is the
        // all-nines significand at the maximum exponent (LOWEST = 0 for an unsigned mask, as the standard's own
        // fixed-point NOTE table shows for `$**,**9.99`); r1 is a WELL-FORMEDNESS condition on the entry — its
        // extreme shall pass an IN-ARITHMETIC-RANGE test, i.e. lie within the intermediate data item's range for the
        // arithmetic mode in effect (§8.8.4.4.4 GR3 l): binary64 under NATIVE, decimal128 under STANDARD-DECIMAL.
        if (edited && pic.IsFloatEdited)
        {
            var fm = CobolNet.Runtime.CobolEdit.FloatMask.Parse(pic.EditMask!, ctx.Data.DecimalPointIsComma);
            int intDigits = fm.SigDigits - fm.SigScale;
            // farthest = (10^d − 1) × 10^(−f) × 10^maxExp  ≈ 10^(intDigits + maxExp); closest nonzero = 10^(intDigits − 1 − maxExp)
            int farthestExp = intDigits + fm.MaxExp;             // the decimal exponent of the extreme's magnitude
            int closestExp = intDigits - 1 - fm.MaxExp;
            // ⛔ THE SAME ONE TABLE AS THE USAGE ARM ABOVE (kb/Work PB194 — this is the second of the two copies
            // that named StandardDecimal alone; the mode set now lives in exactly one place).
            var (modeFarthest, modeClosest) = ArithmeticModes.IntermediateExponentRange(ctx.Data.Options.Arithmetic);
            if (farthestExp > modeFarthest || closestExp < modeClosest)
            {
                ctx.Edition.Error(DiagnosticCatalog.AlgebraicFloatEditedRange, $"FUNCTION {sig.Name}: argument-1's picture "
                    + $"{pic.EditMask} describes values as far from zero as 1E+{farthestExp} and as near as 1E{closestExp}, "
                    + $"outside the {ArithmeticModes.IntermediateName(ctx.Data.Options.Arithmetic)} intermediate's range — the entry "
                    + "shall be such that its extreme passes an IN-ARITHMETIC-RANGE test (ISO §15.43.4 r1 / §15.58.4 r1; §8.8.4.4.4 GR3 l)");
                return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} floating-point edited argument");
            }
        }
        return FoldFrom(sig, pic, AlgebraicRanges.Of(pic, ctx.Data.DecimalPointIsComma));
    }

    /// <summary>Pick the §15.43.4 / §15.58.4 / §15.83.4 r2 value this FUNCTION names out of the description's
    /// extremes, which <see cref="AlgebraicRanges"/> computes for BOTH surfaces the standard gives them (this and
    /// §14.9.39 SET format 15 — Annex D.32 states the equivalence; kb/Work PB452). The three r2s differ only in
    /// WHICH extreme they name, and that difference is this method.</summary>
    private BoundExpr FoldFrom(IntrinsicSig sig, PicInfo pic, AlgebraicRange? range)
    {
        if (range is not { } r) return AlgebraicArgError(sig);   // no numeric capacity — screened above, belt and braces
        // A BINARY float usage's extreme is a carrier value the text only images (BoundNumLiteral.Carrier —
        // kb/Work PB1196): §15.43.4 r2's "represented in argument-1" is that exact binary32/binary64 value.
        BinaryFloatCarrier? carrier = pic.IsFloat && pic.ClrType is "float" or "double"
            ? (pic.IsSingle ? BinaryFloatCarrier.Binary32 : BinaryFloatCarrier.Binary64)
            : null;
        switch (sig.Name)
        {
            case "HIGHEST-ALGEBRAIC":
                return new BoundNumLiteral(r.Farthest, carrier);
            case "SMALLEST-ALGEBRAIC":
                // §15.83.3 r1 admits category numeric ONLY, so the edited descriptions whose Nearest is
                // deliberately unmodelled are already refused above; this arm never sees one.
                return r.Nearest is { } n ? new BoundNumLiteral(n, carrier) : AlgebraicArgError(sig);
            default:
                // LOWEST: sign-representable → −magnitude; else zero AT THE ITEM'S SCALE (§15.58.4 / Annex D.32).
                // Routed through the range's own Zero rather than a bare "0" literal so the folded text carries
                // the scale (a 9V99 item's lowest value is "0.00", not "0"), matching the runtime rule and
                // keeping the literal's precision for any arithmetic it feeds.
                return new BoundNumLiteral(r.FarthestNegative ?? r.Zero, carrier);
        }
    }

    /// <summary>The HIGHEST/LOWEST/SMALLEST fold for a counter register (kb/Work R26), read off the register's
    /// <see cref="AlgebraicRange"/>: the registers are UNSIGNED integers (§8.4.3.14.4 GR1 / §8.4.3.15.4 GR1), so
    /// LOWEST is its zero (0) and SMALLEST its nearest nonzero value (1, scale 0); HIGHEST is its capacity.</summary>
    private static BoundExpr CounterRegisterFold(IntrinsicSig sig, AlgebraicRange counter) => sig.Name switch
    {
        "SMALLEST-ALGEBRAIC" => new BoundNumLiteral(counter.Nearest!),
        "LOWEST-ALGEBRAIC" => new BoundNumLiteral(counter.Zero),
        _ => new BoundNumLiteral(counter.Farthest),
    };

    private BoundExpr AlgebraicArgError(IntrinsicSig sig)
    {
        string cat = sig.Name == "SMALLEST-ALGEBRAIC" ? "category numeric" : "category numeric or numeric-edited";
        string sec = sig.Name == "SMALLEST-ALGEBRAIC" ? "15.83.3" : sig.Name == "HIGHEST-ALGEBRAIC" ? "15.43.3" : "15.58.3";
        ctx.Edition.Error("COBOLNET1516", $"FUNCTION {sig.Name} argument-1 shall be a {cat} DATA ITEM — not a "
            + "literal, an arithmetic expression, a group item, a reference-modified item, an index, or another "
            + $"function (ISO §{sec} rule 1)");
        return BoundExprError.Refused(ctx.Edition, $"FUNCTION {sig.Name} argument");
    }

    // ── Argument-list binding: split → ALL expansion → per-segment parse ───────────────────────────────────

    /// <summary>The generic (non-phrase-keyword) argument bind: a table(ALL) argument becomes ONE enumerating operand
    /// (§15.3 — admissible only for a function whose definition repeats an argument, kb/Work PB62), every other
    /// argument one typed operand through <see cref="BindArgOperand"/>.</summary>
    private List<BoundOperand> BindIntrinsicArgs(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        var args = new List<BoundOperand>();
        foreach (var a in argCtxs)
        {
            if (TryBindAllArgument(sig, a, args)) continue;
            args.Add(BindArgOperand(a));
        }
        return args;
    }

    /// <summary>Bind one function argument to a typed operand (the §8.4.3.2 SR8 shapes): a non-numeric literal
    /// stays a categorized literal operand; a sole data reference stays a field operand through the ONE
    /// <c>BindExpr</c>→<c>RefExpr</c> mapping (its category decides string-vs-numeric rendering; a bare
    /// index-name reads its occurrence number, §13.18.38); anything arithmetic binds through <c>BindExpr</c>
    /// and wraps as a computed operand (<see cref="OperandOf"/>). OMITTED is barred for an intrinsic argument
    /// (§8.4.3.2 SR7); an unconsumed phrase word is a loud named operand — never a silent skip (§1.4).
    /// <para><paramref name="nullAdmitting"/> is true ONLY for a function-prototype activation (UdfBinder): §8.4.3.10.3
    /// SR1 a) admits the predefined NULL "as an argument in … a function-prototype format function activation", and an
    /// intrinsic-function argument is no such context (§8.4.3.2.2 distinguishes intrinsic-function-name-1 from
    /// function-prototype-name-1), so every intrinsic keeps the ONE SR1 refusal, COBOLNET2576 (kb/Work PB1427).</para></summary>
    internal BoundOperand BindArgOperand(Core.FunctionArgumentContext a, bool nullAdmitting = false)
    {
        if (a.OMITTED() is not null)
        {
            ctx.Edition.Error("COBOLNET1544", "OMITTED shall not be specified as an intrinsic-function argument "
                + "(ISO §8.4.3.2.3 SR7 — OMITTED applies to user-defined function parameters declared OPTIONAL)");
            return BoundOperandError.Refused(ctx.Edition, "OMITTED intrinsic argument");
        }
        if (a.fnArgPhraseWord() is { } kw)
        {
            // A phrase word this function does not take — REPORTED, not silently staged (kb/Work R19): the
            // OMITTED arm above reports and this arm did not, so `FUNCTION EXP10(LEADING)` compiled with zero
            // diagnostics and threw at run time (§4.2.2 ¶3 obliges the indication).
            ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentNotAValue,
                $"the reserved phrase word '{kw.GetText()}' is written as an intrinsic-function argument, but "
                + "this function's §15.x.2 general format takes no phrase — a phrase word is not an identifier, "
                + "literal, or expression (ISO §15.3)");
            return BoundOperandError.Refused(ctx.Edition, $"intrinsic argument '{kw.GetText()}'");
        }
        if (a.nonNumericLiteral() is { } nn)
            // Asked of the parse node first, so an ordinary literal is bound ONCE (by NonNumericOperand) and its
            // screens report once.
            return nullAdmitting && nn.predefinedNull() is not null
                ? host.Expr.NullAdmittingOperand(nn)!
                : NonNumericOperand(nn);
        // §8.4.3.2.3 SR8 — "a boolean expression" as an argument (kb/Work PB65, FMT-15.45.2): bound through the ONE
        // boolean-expression binder; the class-boolean operand every §15.3 item-3 rule (INTEGER-OF-BOOLEAN,
        // BOOLEAN-OF-INTEGER's siblings) admits and the renderer images as its '0'/'1' string.
        if (a.booleanExpression() is { } be) return new BoundBoolOperand(host.Cond.BindBoolExpr(be));
        // ⛔ A CONSTANT-NAME SUBSTITUTES ITS LITERAL, OF ITS OWN CLASS — HERE, BEFORE THE NUMERIC PATH BELOW
        // (fix-queue R01). §13.10.4 GR1: "the effect of specifying constant-name-1 in other than this entry is as
        // if literal-1 … were written where constant-name-1 is written", and §13.10.3 SR2 admits it "anywhere
        // that a format specifies a literal of the class and category of constant-name-1". An intrinsic argument
        // is such a position for whatever class the function's own §15.x argument rule admits.
        // Without this arm the reference fell to `BindFunctionArgumentExpr` — the §8.8.1.1 NUMERIC-expression
        // bind — and an alphanumeric or national constant was rejected as "not a numeric operand", in EVERY
        // intrinsic argument position. `FUNCTION UPPER-CASE(K-TEXT)` did not compile while
        // `FUNCTION UPPER-CASE("abcdef")` did, for source §13.10.4 GR1 makes identical.
        // ⚠ A NUMERIC constant deliberately falls through: the expression path already substitutes it correctly
        // (ExpressionBinder.RefExpr → BoundNumLiteral) and it must keep participating in arithmetic — an argument
        // like `FUNCTION MAX(K-NUM + 1)` is an expression, not a bare literal, and only that path can bind it.
        if (SoleDataReference(a) is { } cref
            && ctx.Data.ConstantOf(cref) is { Category: not PicCategory.Numeric } k)
            return new BoundStringLiteral(k.Text) { Category = k.Category };
        // An argument is NOT an §8.8.1.1 arithmetic expression: its legality comes from this function's own §15.x
        // ARGUMENT RULE, and the string functions admit alphanumeric data. The named entry says so at the call
        // site — TRIM / SUBSTITUTE / FIND-STRING / CONVERT over a PIC X item are legal (DA6).
        return OperandOf(host.Expr.BindFunctionArgumentExpr(a.arithmeticExpression()));
    }

    /// <summary>A non-numeric-literal argument as a categorized operand (§8.3.3.4/.5/.6.4 — the same decode +
    /// introduction-gate helpers every literal channel uses). HEXLIT decodes as the alphanumeric literal it is
    /// (§8.3.3.2 Format 2) — DA3.</summary>
    private BoundOperand NonNumericOperand(Core.NonNumericLiteralContext nn)
    {
        // Through the ONE literal mapping (ExpressionBinder.NonNumericLiteralOperand) — this used to be a second
        // hand-maintained copy of the same chain, which is how the hexadecimal form came to be supported in some
        // literal positions and not others (DA3). §8.8.3.3 GR3 concatenation folding and the §8.3.3.4/.5/.6.4
        // decode + introduction gates all live there now.
        if (host.Expr.NonNumericLiteralOperand(nn) is { } op) return op;
        // The F18 sibling one shape over (kb/Work R19): the null fallback was a silent runtime stage too.
        ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentNotAValue,
            $"the literal '{nn.GetText()}' is not a form this intrinsic-argument position admits (ISO §15.3)");
        return BoundOperandError.Refused(ctx.Edition, $"literal argument '{nn.GetText()}'");
    }

    /// <summary>The bare-word view of an argument for the §15 phrase-keyword functions: a reserved phrase word
    /// (<c>fnArgPhraseWord</c>) or a bare unqualified, unsubscripted name (the IDENTIFIER-shaped phrase words —
    /// ANYCASE, HEX, NAT, ANUM, BYTE, CURRENT, ACTIVATING, NESTED, STACK, TOP-LEVEL). Uppercase; null when the
    /// argument is not a bare word. A word that matches no phrase keyword falls back to the ordinary operand
    /// bind, so a data-name argument is never swallowed.
    /// <para>⛔ THE ONE >>COBOL-WORDS FUNNEL FOR §15 PHRASE WORDS (ISO §7.3.10.4 GR2/GR3/GR4; kb/Work PB250).
    /// Every phrase-keyword function reads its words through here, so the directive is applied ONCE, for the
    /// whole class, in both directions — <see cref="CobolNet.Editions.CobolWordsMap.Resolve"/> maps an EQUATE/
    /// SUBSTITUTE synonym onto the canonical keyword (GR2/GR4) and returns null for an UNDEFINE'd/SUBSTITUTE'd-away
    /// word (GR3/GR4), which drops the argument into the ordinary operand bind as the user-defined word it now is.
    /// This is NOT redundant with the post-lex token retype: it is the ONLY mechanism that reaches a phrase word
    /// the lexer does not make a keyword token (ANYCASE and LOCALE are §8.9 RESERVED words yet arrive as plain
    /// IDENTIFIERs — <c>CobolExpressions.g4</c> functionArgument), and it is also what closes the de-reserved arm
    /// for the words that ARE tokens: the retype turns a de-reserved LEADING into an IDENTIFIER, which would
    /// otherwise reach the <c>cobolWord</c> branch below and be re-read as the keyword by its raw text.</para></summary>
    private string? KeywordWordOf(Core.FunctionArgumentContext a)
    {
        var tok = a.fnArgPhraseWord() is { } kw ? kw.Start
            : SoleDataReference(a) is { } d && d.dataReferenceSuffix().Length == 0
                && d.cobolWord() is { } cw ? cw.Start : null;
        return CobolNet.Frontend.Parsing.CobolWordsRewriter.CanonicalWordOf(tok, ctx.CobolWords);
    }

    /// <summary>The argument's sole data reference — non-null when the arithmetic-expression alternative is
    /// exactly one <c>dataReference</c> primary with no operators, unary signs, or parentheses around it.
    /// <para>⛔ THROUGH THE ONE DESCENT (kb/Work PB224). This was a tier-by-tier list-pattern re-implementation
    /// of <see cref="CobolNet.Frontend.Expressions.SoleOperand.Primary"/> that agreed with it by coincidence
    /// rather than by construction — the fifth copy of a boundary that decides §8.8.4.7.3 SR2, §8.8.1.1's
    /// sole-vs-compound split and the §7.3.11 directive operand at once.</para></summary>
    private static Core.DataReferenceContext? SoleDataReference(Core.FunctionArgumentContext a) =>
        CobolNet.Frontend.Expressions.SoleOperand.DataRef(a.arithmeticExpression());

    /// <summary>
    /// A <c>table(… ALL …)</c> argument (ISO §15.3; kb/Work PB62): detected from an argument that is a sole data
    /// reference whose one subscript capture (SUBSCRIPT-mode tokens — the D10/PHASE-15 deferral) holds a depth-0
    /// ALL, and bound as ONE <see cref="TableAllPlace"/> operand the backend ENUMERATES — never a bind-time
    /// expansion into N operands. Returns true when the argument IS such a reference (consuming it — including a
    /// loud error operand for an unresolvable shape); false hands the argument to the ordinary operand bind.
    /// <para><b>§15.3 admissibility, decided FIRST and from the DEFINITION:</b> "When the definition of a function
    /// permits an argument to be repeated a variable number of times, a table may be referenced by … ALL" — so on a
    /// function whose format repeats no argument (MOD, ANNUITY, LOG, INTEGER-OF-DAY …) the ALL is rejected as such
    /// (COBOLNET1645), at ANY table cardinality; the former bind-time expansion accepted `MOD(E(ALL) B)` over a
    /// one-occurrence table outright and rejected the three-occurrence twin only through the arity count. The
    /// property is <see cref="IntrinsicSig.RepeatsAnArgument"/>, pinned to the §15.x.2 formats by drift test.</para>
    /// <para><b>The ranges are the standard's three</b>, one <see cref="AllCount"/> per ALL level, outermost first:
    /// a fixed OCCURS count; an OCCURS DEPENDING table's data-name-1 value ("the range of values is determined by
    /// the object of the OCCURS DEPENDING ON clause" — the runtime quantity the bind-time expansion staged loud, the
    /// FMT-15.60.2 family); a dynamic-capacity table's current capacity ("from 1 to the current capacity of the
    /// table" — a level the old walk never built, because a dynamic table has no <c>Occurs</c>). A nested dynamic
    /// table's capacity path carries the OUTER index variables, so each occurrence's own capacity is read.</para>
    /// </summary>
    private bool TryBindAllArgument(IntrinsicSig sig, Core.FunctionArgumentContext a, List<BoundOperand> args)
    {
        if (SoleDataReference(a) is not { } dref || dref.cobolWord() is not { } cw) return false;
        string name = cw.GetText();

        // Collect OF/IN qualifiers and the ONE trailing subscript group — which the grammar hangs off the LAST
        // qualification when qualifiers are present (qualification : (OF|IN) cobolWord (subscriptPart|refModPart)*).
        var quals = new List<string>();
        Core.SubscriptPartContext? sp = null;
        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (sp is not null) return false;                    // anything after the subscript group — not the shape
            if (suffix.subscriptPart() is { } direct) { sp = direct; continue; }
            if (suffix.qualification() is not { } q || q.cobolWord() is not { } qw) return false;   // ref-mod tail
            quals.Add(qw.GetText());
            if (q.refModPart().Length > 0 || q.subscriptPart().Length > 1) return false;
            if (q.subscriptPart() is [{ } trailing]) sp = trailing;
        }
        if (sp?.subscriptOrRefMod() is not { } som) return false;

        var inner = new List<IToken>();
        ReferenceResolver.CollectLeafTokens(som, inner);
        // THE SAME declaration-informed '(' predicate the two reference-resolution callers use (kb/Work PB877):
        // this was the one caller that passed none, so the splitter's "unknown → no split" fallback stood here
        // and a §8.4.2.3.2 NOTE-2 parenthesized-expression subscript (`T (CTR (- OFF) ALL)`) joined itself to the
        // preceding name. Three callers, one §8.4.2.3.3 SR2 answer.
        var innerSegs = ReferenceResolver.SplitSubscriptTokens(inner, ctx.Refs.CannotBeSubscripted);
        if (!innerSegs.Any(IsAllSegment)) return false;

        if (!sig.RepeatsAnArgument)
        {
            ctx.Edition.Error(DiagnosticCatalog.AllSubscriptNotRepeatable,
                $"FUNCTION {sig.Name} argument '{name}(… ALL …)': the ALL subscript is permitted only when the "
                + "function's definition permits an argument to be repeated a variable number of times (ISO §15.3), and "
                + $"FUNCTION {sig.Name}'s general format repeats none — write the occurrence you mean, or use a function "
                + "whose format is `{ argument } …` (MAX, MIN, SUM, MEAN, …).");
            args.Add(BoundOperandError.Refused(ctx.Edition, $"FUNCTION {sig.Name} table(ALL) argument"));
            return true;
        }
        if (ctx.Refs.FindItem(name, quals) is not { } item)
        {
            // The resolver's ONE §8.4.2.1 / §8.4.2.2 report (kb/Work PB1029 — this arm refused silently).
            ctx.Refs.ReportUnidentified(dref, name, quals);
            args.Add(BoundOperandError.Refused(ctx.Edition, $"table(ALL) reference '{name}'"));
            return true;
        }
        // The table levels on the item's ancestor chain, outermost first — the AccessPath subscript order, from
        // THE one §8.4.2.3.3 SR3 walk (DataItem.SubscriptLevels, kb/Work PB877). A dynamic-capacity table IS a
        // level (IsTable), which the former `Occurs is not null` walk missed.
        var levels = item.SubscriptLevels();
        if (levels.Count != innerSegs.Count)
        {
            // The resolver's ONE §8.4.2.3.3 SR2/SR3 report (kb/Work PB1029 — this arm refused silently).
            ctx.Refs.ScreenSubscriptArity(dref, item, innerSegs.Count);
            args.Add(BoundOperandError.Refused(ctx.Edition, $"table(ALL) subscript count for '{name}'"));
            return true;
        }

        string indexVar = $"__all{_allSerial++}";
        var exprs = new string[innerSegs.Count];
        var counts = new List<AllCount>();
        var outerExprs = new List<string>();   // the rendered index of every level ABOVE the current one
        for (int i = 0; i < innerSegs.Count; i++)
        {
            DataItem level = levels[i];
            if (IsAllSegment(innerSegs[i]))
            {
                exprs[i] = $"{indexVar}[{counts.Count}]";
                // The level's range is the ONE current-occurrence-count model (fixed / ODO / dynamic capacity).
                if (ctx.Refs.CurrentOccurrenceCount(level, outerExprs) is not { } count)
                {
                    args.Add(BoundOperandError.Unbuilt(ctx.Edition, level.OccursSpec is { Depending: { } dep }
                        ? $"table(ALL) over OCCURS DEPENDING table '{name}': data-name-1 '{dep.CobolName}' could not be addressed"
                        : $"table(ALL) over dynamic-capacity table '{name}': the table could not be addressed"));
                    return true;
                }
                counts.Add(count);
            }
            else if (ctx.Refs.RenderIndexSegment(innerSegs[i]) is { } rendered)
                exprs[i] = rendered;
            else
            {
                args.Add(BoundOperandError.Unbuilt(ctx.Edition, $"table(ALL) subscript of '{name}'"));
                return true;
            }
            outerExprs.Add(exprs[i]);
        }
        if (ctx.Refs.ResolveByName(name, quals, exprs) is not { } element)
        {
            args.Add(BoundOperandError.Unbuilt(ctx.Edition, $"table(ALL) occurrence of '{name}'"));
            return true;
        }
        args.Add(new BoundFieldOperand(new TableAllPlace(element, indexVar, counts)));
        return true;
    }

    /// <summary>One index variable per table(ALL) operand in the unit (<c>__all0</c>, <c>__all1</c>, …): a nested
    /// function inside a fixed subscript of an ALL argument renders its own enumeration lambda INSIDE the outer
    /// one, and C# forbids a lambda parameter that shadows an enclosing one.</summary>
    private int _allSerial;

    private static bool IsAllSegment(List<IToken> seg) =>
        seg.Count(t => t.Type != Core.SUB_WS) == 1 && seg.Any(t => t.Type == Core.SUB_ALL);

}
