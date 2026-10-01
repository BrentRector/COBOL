// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using CobolNet.Frontend.Preprocessor;

namespace CobolSharp.Compiler;

/// <summary>
/// Top-level compilation facade. Orchestrates all compiler phases:
/// Source -> Preprocess -> ANTLR4 Lex -> ANTLR4 Parse -> Semantic -> Emit.
/// Each phase delegates to a focused component — this class only wires the pipeline.
/// Supports multiple programs in a single source file (batch compilation)
/// and nested/contained programs per the COBOL-85 spec.
/// </summary>
public sealed class Compilation
{
    private readonly List<string> _copySearchPaths = [];

    /// <summary>Add a directory to search for COPY copybooks.</summary>
    public void AddCopySearchPath(string path) => _copySearchPaths.Add(path);

    /// <summary>
    /// When set, enables NIST preprocessing: replaces XXXXX### placeholders
    /// with CobolSharp-appropriate values. The value is the test name (e.g., "NC101A").
    /// </summary>
    public string? NistTestName { get; set; }

    /// <summary>Compilation options controlling dialect, warnings, and feature gating.</summary>
    public Semantics.CompilationOptions Options { get; set; } = new();

    public CompilationResult Compile(string sourcePath, string? outputPath = null)
    {
        var diagnostics = new DiagnosticBag();

        // Phase 0: Preprocess
        string processedText = Preprocess(sourcePath, diagnostics);

        // Phase 1: Lex + Parse
        int dialectLevel = Options.Config.ParserLevel;
        var tree = LexAndParse(processedText, sourcePath, diagnostics, dialectLevel);
        if (tree == null)
            return new CompilationResult(false, "", diagnostics.Diagnostics);

        // Phase 2: Validate grammar invariants (debug only)
        Semantics.GrammarInvariants.ValidateSentenceAndStatementBoundaries(tree);

        // Phase 3: Collect all program contexts (top-level and nested), tracking containment.
        var programParents = new Dictionary<ParserRuleContext, ParserRuleContext?>();
        var programContexts = CollectProgramContexts(tree, programParents);
        if (programContexts.Count == 0)
            return new CompilationResult(false, "", diagnostics.Diagnostics);

        // OO: the class-names declared in this compilation group (CLASS-ID units) — valid references that live in
        // no data/procedure scope, so they must be whitelisted for the undefined-data-name check (see
        // BuildSemanticModel). Order-independent: a program may reference a class that appears later in source.
        var classNames = programContexts
            .OfType<CobolParserCore.ClassDefinitionContext>()
            .Select(ExtractProgramIdFromContext)
            .Where(n => n != null)
            .Select(n => n!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Phase 3b: Collect the names of COBOL-2002 user-defined functions (FUNCTION-ID units) in this
        // compilation group, so a `FUNCTION user-name(args)` reference in ANY unit is routed to a user-function
        // CALL (WS-2002-UDF slice 3) rather than an intrinsic — order-independent, since a caller may precede the
        // function unit in source order.
        var userFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var userFunctionSignatures =
            new Dictionary<string, (int, Runtime.PicDescriptor)>(StringComparer.OrdinalIgnoreCase);
        var userFunctionParamSignatures =
            new Dictionary<string, IReadOnlyList<(int, Runtime.PicDescriptor)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var progCtx in programContexts)
        {
            var idBody = (progCtx switch
            {
                CobolParserCore.ProgramUnitContext pu => pu.identificationDivision(),
                CobolParserCore.NestedProgramContext np => np.identificationDivision(),
                _ => null
            })?.identificationBody();
            if (idBody?.functionIdParagraph() == null || UnitName(idBody) is not { } fn) continue;
            userFunctionNames.Add(fn);

            // Build this function unit's signature (its RETURNING item's storage length + PIC) so a caller can
            // decode a `FUNCTION fn(args)` result appearing in an expression (the general inline form). This
            // pre-pass runs before any caller is bound, so it is order-independent; a throwaway DiagnosticBag
            // avoids double-reporting (the unit is built again in the main loop below).
            try
            {
                var sigModel = BuildSemanticModel(progCtx, fn, sourcePath, System.Array.Empty<string>(),
                    new DiagnosticBag(), Options);
                Semantics.StorageLayoutComputer.ComputeLayout(sigModel);
                if (sigModel.ProcedureReturningItem is { } ret
                    && sigModel.GetStorageLocation(ret) is { } loc)
                    userFunctionSignatures[fn] = (loc.Length, loc.Pic);
                var paramSigs = new List<(int, Runtime.PicDescriptor)>();
                foreach (var p in sigModel.ProcedureUsingParameters)
                    if (sigModel.GetStorageLocation(p) is { } pl) paramSigs.Add((pl.Length, pl.Pic));
                userFunctionParamSignatures[fn] = paramSigs;
            }
            catch { /* signature unavailable → the inline form falls through; whole-source form still works */ }
        }

        // Phase 4: Process each program through semantic analysis, binding, and IR generation.
        // Containing programs precede their contained programs (collection order), so each program's
        // ancestors are already built and laid out when we reach it — needed for GLOBAL inheritance.
        var compiledPrograms = new List<CompiledProgram>();
        var modelByContext = new Dictionary<ParserRuleContext, Semantics.SemanticModel>();
        var idByContext = new Dictionary<ParserRuleContext, string>();
        foreach (var progCtx in programContexts)
        {
            string programId = ExtractProgramIdFromContext(progCtx)
                ?? Path.GetFileNameWithoutExtension(sourcePath);
            bool isInitial = ExtractIsInitialFromContext(progCtx);

            // Names of IS GLOBAL items declared in containing programs are visible here (ISO §8.4.5) but
            // are not added to this program's scope until InheritGlobalItems runs below (after the model is
            // built). So feed them into the undefined-data-name check's whitelist now, computed from the
            // already-built ancestor models — otherwise a contained program's reference to an inherited
            // global (e.g. IC228A's GLO-DATA-1..4) would be falsely flagged CBL3128. (DEVLOG 310)
            var inheritedGlobalNames = CollectInheritedGlobalNames(progCtx, programParents, modelByContext);
            var semanticModel = BuildSemanticModel(progCtx, programId, sourcePath, inheritedGlobalNames, diagnostics, Options, classNames);
            semanticModel.Program.IsInitial = isInitial;
            semanticModel.UserFunctionNames = userFunctionNames;
            semanticModel.UserFunctionSignatures = userFunctionSignatures;
            semanticModel.UserFunctionParameterSignatures = userFunctionParamSignatures;

            // Validate and compute layout
            Semantics.ParagraphValidator.Validate(semanticModel, diagnostics);
            Semantics.StorageLayoutComputer.ComputeLayout(semanticModel);
            Semantics.DataItemClassifier.Validate(semanticModel, diagnostics);
            Semantics.FileStatusValidator.Validate(semanticModel, diagnostics);
            Semantics.SymbolValidator.Validate(semanticModel, diagnostics);

            // Make IS GLOBAL items declared in containing programs visible here, sharing the
            // containing program's storage at runtime (ISO §8.4.5).
            InheritGlobalItems(progCtx, semanticModel, programParents, modelByContext, idByContext);

            modelByContext[progCtx] = semanticModel;
            idByContext[progCtx] = programId;

            // GLOBAL USE AFTER ERROR declaratives declared in containing programs (ISO §14.9.49.4 GR4):
            // an I/O exception here that this program has no USE declarative for is serviced by the
            // nearest containing program's applicable GLOBAL declarative — dispatched at runtime via
            // GlobalUseDeclarativeRegistry. Gather the ancestors' GLOBAL declaratives for the lowerer.
            var inheritedGlobalUse = CollectInheritedGlobalUseDeclaratives(progCtx, programParents, modelByContext);

            // Bind -> IR
            var binder = new CodeGen.Binder(semanticModel, diagnostics, Options, inheritedGlobalUse);
            var irModule = binder.Bind(progCtx, progCtx is CobolParserCore.ClassDefinitionContext);

            // OO: tag a class unit so CIL emission produces an instance reference type (per-instance ProgramState
            // + a public instance method) instead of a static program type. (docs/OO_IMPLEMENTATION_DESIGN.md §4)
            if (progCtx is CobolParserCore.ClassDefinitionContext classCtx)
            {
                irModule.IsClass = true;
                irModule.ClassMethodName = ExtractClassMethodName(classCtx);
                irModule.BaseClassName = ExtractBaseClassName(classCtx); // OO slice 3: INHERITS FROM
                // Multi-method classes work, and a single-method class may have parameters — but a class with
                // MULTIPLE methods where any has USING/RETURNING is not yet supported (per-method LINKAGE layout +
                // offset resolution is a later OO slice; the module-level FindLinkageField would cross-wire sibling
                // methods' params → a runtime buffer crash). Reject loudly. (Adversarial review, DEVLOG 455.)
                if (irModule.ClassMethods.Count > 1
                    && irModule.ClassMethods.Any(m => m.UsingParameterNames.Count > 0))
                    diagnostics.Report(DiagnosticDescriptors.COBOL0117,
                        new SourceLocation(sourcePath, 0, 0, 0), TextSpan.Empty, programId);
                // INVOKE SUPER in a class with no base is invalid — report it cleanly (else it surfaces at emit
                // time as a misleading COBOL0600 "internal error"). (Adversarial review, DEVLOG 451.)
                if (irModule.BaseClassName == null && ClassUsesSuper(irModule))
                    diagnostics.Report(DiagnosticDescriptors.COBOL0115,
                        new SourceLocation(sourcePath, 0, 0, 0), TextSpan.Empty, programId);
                if (irModule.BaseClassName is { } baseName)
                {
                    // INHERITS FROM a base that isn't a class in this compilation group → fail loudly rather than
                    // silently degrade to a root class (the inheritance the author wrote would just not exist).
                    if (!classNames.Contains(baseName))
                        diagnostics.Report(DiagnosticDescriptors.COBOL0114,
                            new SourceLocation(sourcePath, 0, 0, 0), TextSpan.Empty, baseName);

                    // Slice 3a: a subclass inherits the base's per-instance State but does not yet emit its own —
                    // so its own OBJECT data has nowhere to live. Reject it loudly (a later slice extends State).
                    // Check the OBJECT's OWN WORKING-STORAGE parse subtree directly (its data entries) — NOT the
                    // flattened symbol list, which also carries each method's LINKAGE params and synthetic OCCURS
                    // INDEXED BY index-names (the latter are force-placed in the WS area, ISO §8.5.1.2).
                    bool hasOwnObjectData = classCtx.objectParagraph()?.dataDivision()
                        ?.workingStorageSection()?.dataDescriptionEntry() is { Length: > 0 };
                    if (hasOwnObjectData)
                        diagnostics.Report(DiagnosticDescriptors.COBOL0113,
                            new SourceLocation(sourcePath, 0, 0, 0), TextSpan.Empty, baseName);
                }
            }

            compiledPrograms.Add(new CompiledProgram(programId, irModule, semanticModel));
        }

        // Phase 5: CIL emission — all programs into a single assembly
        string mainProgramId = compiledPrograms[0].ProgramId;
        outputPath ??= Path.Combine(
            Path.GetDirectoryName(sourcePath) ?? ".",
            mainProgramId + ".dll");

        return EmitAssembly(compiledPrograms, outputPath, sourcePath, diagnostics);
    }

    /// <summary>
    /// Collect all program parse contexts from the compilation unit.
    /// Returns top-level programUnit contexts and recursively collects nested programs.
    /// Each context represents an independent COBOL program to compile.
    /// </summary>
    private static List<ParserRuleContext> CollectProgramContexts(
        CobolParserCore.CompilationUnitContext tree,
        Dictionary<ParserRuleContext, ParserRuleContext?> parents)
    {
        var result = new List<ParserRuleContext>();

        foreach (var group in tree.compilationGroup())
        {
            foreach (var programUnit in group.programUnit())
            {
                result.Add(programUnit);
                parents[programUnit] = null;
                CollectNestedPrograms(programUnit.nestedProgram(), programUnit, result, parents);
            }

            // OO (COBOL-2002, ISO §11.2): CLASS-ID units are siblings of program units in a compilation group
            // (grammar: compilationGroup : (programUnit | {is2002()}? classDefinition)+). Each becomes its own
            // compiled unit (a .NET reference type). Slice 1 classes hold methods, not nested programs.
            foreach (var classDef in group.classDefinition())
            {
                result.Add(classDef);
                parents[classDef] = null;
            }
        }

        return result;
    }

    /// <summary>
    /// Recursively collect nested (contained) program contexts, recording each one's containing
    /// program in <paramref name="parents"/> so GLOBAL inheritance can walk the containment chain.
    /// </summary>
    private static void CollectNestedPrograms(
        CobolParserCore.NestedProgramContext[] nestedPrograms,
        ParserRuleContext parent,
        List<ParserRuleContext> result,
        Dictionary<ParserRuleContext, ParserRuleContext?> parents)
    {
        foreach (var nested in nestedPrograms)
        {
            result.Add(nested);
            parents[nested] = parent;
            CollectNestedPrograms(nested.nestedProgram(), nested, result, parents);
        }
    }

    /// <summary>
    /// Make data items declared IS GLOBAL in containing programs visible in <paramref name="model"/>.
    /// ISO §8.4.5: a global name is available to the program that declares it and to every program
    /// contained within it (directly or indirectly), unless that contained program declares the same
    /// name itself (which shadows the global). The item keeps its storage in the declaring program's
    /// ProgramState; its StorageLocation is tagged with that program's id so the emitter reads the
    /// shared bytes (see <see cref="CodeGen.StorageLocation.OwnerProgramId"/>).
    /// </summary>
    private static void InheritGlobalItems(
        ParserRuleContext progCtx,
        Semantics.SemanticModel model,
        IReadOnlyDictionary<ParserRuleContext, ParserRuleContext?> parents,
        IReadOnlyDictionary<ParserRuleContext, Semantics.SemanticModel> modelByContext,
        IReadOnlyDictionary<ParserRuleContext, string> idByContext)
    {
        // Walk outward through containing programs, nearest first (nearest declaration wins).
        var ancestor = parents.TryGetValue(progCtx, out var p) ? p : null;
        while (ancestor != null)
        {
            if (modelByContext.TryGetValue(ancestor, out var ancestorModel)
                && idByContext.TryGetValue(ancestor, out var ownerId))
            {
                foreach (var item in ancestorModel.DataItemsInOrder)
                {
                    if (!item.IsGlobal) continue; // IS GLOBAL is set on the 01/77 declaring item
                    foreach (var member in EnumerateSelfAndDescendants(item))
                    {
                        if (member.IsFiller) continue; // FILLER is unreferenceable
                        if (ancestorModel.GetStorageLocation(member) is not { } loc) continue;
                        model.TryInheritGlobal(member, loc with { OwnerProgramId = ownerId });
                    }
                }

                // FD ... IS GLOBAL: the file-name, its record, and every subordinate field are referenceable
                // (OPEN/READ/CLOSE) here (ISO §8.4.6.2.2). Inherit the FileSymbol into this program's
                // GlobalScope so ResolveFile finds it, and inherit each non-FILLER record field's storage
                // (tagged with the owner program so the emitter reads the shared FILE-SECTION bytes — the
                // record area is itself shared, §8.4.6.2). The runtime FileRuntime state is name-keyed and
                // already shared, so no re-registration is needed; the contained program references the same
                // open file by name.
                foreach (var file in ancestorModel.Symbols.Program.GlobalScope
                             .GetAllSymbols<Semantics.FileSymbol>())
                {
                    if (!file.IsGlobal) continue;
                    if (!model.TryInheritGlobalFile(file)) continue; // a local file of the same name shadows it
                    if (file.Record is { } record)
                        foreach (var member in EnumerateSelfAndDescendants(record))
                        {
                            if (member.IsFiller) continue;
                            if (ancestorModel.GetStorageLocation(member) is not { } loc) continue;
                            model.TryInheritGlobal(member, loc with { OwnerProgramId = ownerId });
                        }

                    // The RELATIVE KEY is a separate working-storage item (not subordinate to the global
                    // record), so the FD's GLOBAL clause does not make it a global name — but the contained
                    // program's keyed I/O lowering must resolve it, sharing the container's storage so the
                    // shared file connector is driven by the right relative-record number (ISO §9.1.5(2)).
                    if (file.RelativeKey is { } rk
                        && ancestorModel.ResolveData(rk) is { } rkSym
                        && ancestorModel.GetStorageLocation(rkSym) is { } rkLoc)
                        model.TryInheritGlobal(rkSym, rkLoc with { OwnerProgramId = ownerId });
                }
            }
            ancestor = parents.TryGetValue(ancestor, out var pp) ? pp : null;
        }
    }

    private static IEnumerable<Semantics.DataSymbol> EnumerateSelfAndDescendants(Semantics.DataSymbol item)
    {
        yield return item;
        foreach (var child in item.Children)
            foreach (var d in EnumerateSelfAndDescendants(child))
                yield return d;
    }

    private string Preprocess(string sourcePath, DiagnosticBag diagnostics)
    {
        string rawText = File.ReadAllText(sourcePath);
        string sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? ".";

        string normalizedText = ReferenceFormatProcessor.NormalizeToFreeFormMapped(rawText,
            gates: null, sourcePath, initialFixed: null, out var referenceFormats,
            ccvsIndicators: true).Text;   // the legacy oracle keeps its CCVS column-7 reading (kb/Work PB1494)

        // Conditional compilation (>>DEFINE / >>IF / >>ELSE / >>END-IF) runs on the free-form text BEFORE COPY
        // expansion, so an >>IF may include or omit COPY statements in its branches (ISO §7.3.16 GR1). It is an
        // exact no-op for source containing no >> directive lines.
        normalizedText = ConditionalCompilationProcessor.Process(normalizedText);

        // COPY expansion runs BEFORE NIST placeholder substitution so that XXXXX###/XXXXP###/
        // XXXXD### placeholders inside copied library text are substituted too — e.g. a file's
        // ASSIGN target supplied by a COPY (K3FCB) must map to the same physical file as the
        // matching placeholder in the consumer's own source. COPY library-name qualifiers are
        // raw placeholders (XXXXX047) resolved against the copy library directory, so expanding
        // first does not disturb them.
        // Library text that cannot be located is CBL3620 on every dialect (§7.2.3.4 GR2; kb/Work PB1355).
        var copyProcessor = new CopyProcessor(_copySearchPaths, diagnostics, sourcePath);
        // Library text starts in the format in effect for its COPY statement (§7.3.24.3 3), kb/Work PB1067). This
        // legacy path numbers COPY statements by NORMALIZED line, which is the physical line wherever the format map
        // is uniform — every source without a >>SOURCE switch, the whole NIST corpus this oracle serves.
        copyProcessor.RegisterReferenceFormat(sourcePath, referenceFormats);
        string expandedText = copyProcessor.Process(normalizedText, sourceDir);

        if (NistTestName != null)
            expandedText = NistPreprocessor.Process(expandedText, NistTestName);

        return expandedText;
    }

    private static CobolParserCore.CompilationUnitContext? LexAndParse(
        string processedText,
        string sourcePath,
        DiagnosticBag diagnostics,
        int dialectLevel = 85)
    {
        var inputStream = new AntlrInputStream(processedText);
        var lexer = new CobolLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);

        // Rewrite ZERO tokens to ZERO_ARITH in arithmetic contexts.
        // Must run after lexing, before parsing — avoids grammar ambiguity
        // that causes exponential ANTLR prediction time.
        ZeroTokenRewriter.Rewrite(tokenStream);

        var parser = new CobolParserCore(tokenStream);
        parser.DialectLevel = dialectLevel;

        // ⛔ THE TOKEN-LEVEL §8.9 GATE (ISO §8.3.2.1 1); kb/Work PB655). A reservation-gated word is not a cobolWord
        // alternative, so this oracle — which parses the SAME grammar — must run the SAME gate loop as the greenfield
        // Frontend, or a word §8.9 leaves free at its edition (COL at COBOL-85) can never be read as a name.
        var tree = ReservationGateRewriter.ParseToFixpoint(parser, tokenStream, TokenRetypes.None, diagnostics,
            (bag, witness) =>
            {
                tokenStream.Seek(0);
                parser.Reset();
                parser.RemoveErrorListeners();
                parser.AddErrorListener(new CobolErrorListener(bag, sourcePath));

                // Two-stage parsing: try fast SLL mode first, fall back to full LL on error.
                // BailErrorStrategy forces SLL to throw on ambiguity instead of hanging.
                parser.Interpreter.PredictionMode = Antlr4.Runtime.Atn.PredictionMode.SLL;
                parser.ErrorHandler = new Antlr4.Runtime.BailErrorStrategy();
                try
                {
                    return parser.compilationUnit();
                }
                catch (Exception)
                {
                    // SLL failed — retry with full LL prediction and normal error handling; the gate's witness
                    // listens to this authoritative pass only.
                    tokenStream.Seek(0);
                    parser.Reset();
                    parser.Interpreter.PredictionMode = Antlr4.Runtime.Atn.PredictionMode.LL;
                    parser.ErrorHandler = new CobolErrorStrategy();
                    parser.AddErrorListener(witness);
                    return parser.compilationUnit();
                }
            });
        return parser.NumberOfSyntaxErrors > 0 ? null : tree;
    }

    /// <summary>
    /// SPECIAL-NAMES-declared names that are valid references but live outside any Scope: mnemonic-names
    /// and their ON/OFF switch conditions, symbolic characters, user CLASS names, and ALPHABET names.
    /// <see cref="Semantics.ReferenceResolver"/> whitelists these so its undefined-data-name check
    /// (CBL3128) does not flag them.
    /// </summary>
    /// <summary>
    /// Names of IS GLOBAL data items (and their non-FILLER members) declared in every containing program of
    /// <paramref name="progCtx"/> (ISO §8.4.5), gathered from the already-built ancestor models. Used to
    /// whitelist inherited globals in the undefined-data-name check (CBL3128), which runs before
    /// <see cref="InheritGlobalItems"/> adds them to this program's scope.
    /// </summary>
    private static IEnumerable<string> CollectInheritedGlobalNames(
        ParserRuleContext progCtx,
        IReadOnlyDictionary<ParserRuleContext, ParserRuleContext?> parents,
        IReadOnlyDictionary<ParserRuleContext, Semantics.SemanticModel> modelByContext)
    {
        var ancestor = parents.TryGetValue(progCtx, out var p) ? p : null;
        while (ancestor != null)
        {
            if (modelByContext.TryGetValue(ancestor, out var ancestorModel))
            {
                // An FD ... IS GLOBAL file-NAME is itself a global name (ISO §8.4.6.2.2): it is referenceable
                // (OPEN/READ/CLOSE) in every contained program. Yield it so the contained program's
                // undefined-data-name check (CBL3128) and file-reference check (CBL3121) accept it; the
                // FileSymbol itself is inherited into this program's GlobalScope by InheritGlobalItems.
                foreach (var file in ancestorModel.Symbols.Program.GlobalScope
                             .GetAllSymbols<Semantics.FileSymbol>())
                    if (file.IsGlobal)
                    {
                        yield return file.Name;
                        // The RELATIVE KEY item is inherited by InheritGlobalItems so the contained program's
                        // keyed I/O can resolve it; whitelist its name too so CBL3128 does not flag it.
                        if (file.RelativeKey is { } rk)
                            yield return rk;
                    }

                // Data items + their members, plus the INDEXED BY index-names of any OCCURS table under a
                // global item (ISO §8.4.5: such an index-name possesses the global attribute). Index-names
                // are yielded even for a FILLER table since the index itself is still referenceable.
                // A record/field is inherited if it is an IS GLOBAL data item, OR it belongs to an
                // FD ... IS GLOBAL file (ISO §8.4.6.2: a global file-name's record + all subordinate names
                // are global). Index-names of an OCCURS table under either are yielded too (§8.4.5), even
                // for a FILLER table since the index itself is still referenceable.
                foreach (var item in ancestorModel.DataItemsInOrder)
                {
                    if (!(item.IsGlobal || item.OwningFile is { IsGlobal: true })) continue;
                    foreach (var member in EnumerateSelfAndDescendants(item))
                    {
                        if (!member.IsFiller)
                            yield return member.DisplayName;
                        if (member.Occurs is { } occ)
                            foreach (var idx in occ.IndexNames)
                                yield return idx;
                    }
                }

                // ISO §8.4.5/§8.4.6.2: condition-names associated with a global name are themselves global.
                // They are ConditionSymbols (not in DataItemsInOrder/Children), so enumerate the scope and
                // yield those whose parent data item lies under an IS GLOBAL item or a GLOBAL FD record.
                foreach (var cond in ancestorModel.Symbols.Program.DataDivisionScope
                             .GetAllSymbols<Semantics.ConditionSymbol>())
                {
                    for (var d = cond.ParentDataItem; d != null; d = d.Parent)
                        if (d.IsGlobal || d.OwningFile is { IsGlobal: true }) { yield return cond.Name; break; }
                }
            }
            ancestor = parents.TryGetValue(ancestor, out var pp) ? pp : null;
        }
    }

    /// <summary>
    /// GLOBAL USE AFTER ERROR declaratives (scope + optional file name) declared in every containing
    /// program of <paramref name="progCtx"/> (ISO §14.9.49.4 GR4), nearest-first. A contained program
    /// dispatches the applicable one at runtime when an I/O exception arises that it has no USE
    /// declarative of its own for. Empty for a top-level program.
    /// </summary>
    private static IReadOnlyList<(int Scope, string? FileName)> CollectInheritedGlobalUseDeclaratives(
        ParserRuleContext progCtx,
        IReadOnlyDictionary<ParserRuleContext, ParserRuleContext?> parents,
        IReadOnlyDictionary<ParserRuleContext, Semantics.SemanticModel> modelByContext)
    {
        var result = new List<(int, string?)>();
        var ancestor = parents.TryGetValue(progCtx, out var p) ? p : null;
        while (ancestor != null)
        {
            if (modelByContext.TryGetValue(ancestor, out var ancestorModel))
                foreach (var g in ancestorModel.GlobalUseDeclaratives)
                    result.Add((g.Scope, g.FileName));
            ancestor = parents.TryGetValue(ancestor, out var pp) ? pp : null;
        }
        return result;
    }

    private static IEnumerable<string> CollectSpecialNames(Semantics.SemanticBuilder b)
    {
        foreach (var sw in b.ImplementorSwitches)
        {
            yield return sw.Name;
            if (sw.OnValueName != null) yield return sw.OnValueName;
            if (sw.OffValueName != null) yield return sw.OffValueName;
        }
        foreach (var (name, _) in b.SymbolicCharacters) yield return name;
        foreach (var c in b.ClassDefinitions) yield return c.Name;
        foreach (var a in b.AlphabetDefinitions) yield return a.Name;
        // SCREEN SECTION screen-names are referenced by DISPLAY/ACCEPT but live in no Scope (DEVLOG 310).
        foreach (var screenName in CollectScreenNames(b.ScreenItems)) yield return screenName;
    }

    private static IEnumerable<string> CollectScreenNames(IEnumerable<Semantics.Bound.BoundScreenItem> items)
    {
        foreach (var s in items)
        {
            if (s.Name != null) yield return s.Name;
            foreach (var n in CollectScreenNames(s.Children)) yield return n;
        }
    }

    private static Semantics.SemanticModel BuildSemanticModel(
        ParserRuleContext programTree,
        string programId,
        string sourcePath,
        IEnumerable<string> inheritedGlobalNames,
        DiagnosticBag diagnostics,
        Semantics.CompilationOptions options,
        IEnumerable<string>? classNames = null)
    {
        // Pass 1: Declaration collection
        var semanticBuilder = new Semantics.SemanticBuilder(programId, 1, options, sourcePath);
        semanticBuilder.Visit(programTree);
        semanticBuilder.ResolveRedefines();
        semanticBuilder.ResolveRenames();
        semanticBuilder.PropagateGroupSignClauses();

        // Pass 2: Reference resolution
        var semDiagnostics = new List<Diagnostic>(semanticBuilder.Diagnostics);
        // OO: a referenced class-name (e.g. the target of INVOKE class "NEW", or a REPOSITORY CLASS entry) is a
        // valid name that lives in no data/procedure scope; whitelist every class in the compilation group so the
        // undefined-data-name check (CBL3128) does not flag it.
        var resolver = new Semantics.ReferenceResolver(
            semanticBuilder.Symbols, semDiagnostics, sourcePath,
            CollectSpecialNames(semanticBuilder).Concat(inheritedGlobalNames)
                .Concat(classNames ?? Enumerable.Empty<string>()),
            semanticBuilder.ClassMethodScopes);   // OO §11.7: method-local PERFORM/GO TO resolution
        resolver.Visit(programTree);

        foreach (var d in semDiagnostics)
            diagnostics.Add(d);

        // Build model
        var model = new Semantics.SemanticModel(
            semanticBuilder.Symbols.Program,
            semanticBuilder.Symbols,
            diagnostics);
        model.SourceName = sourcePath;

        model.SetPicEnvironment(semanticBuilder.CurrencySign, semanticBuilder.CurrencyOutputChar, semanticBuilder.DecimalPointIsComma);

        foreach (var sw in semanticBuilder.ImplementorSwitches)
            model.RegisterImplementorSwitch(sw);

        foreach (var classDef in semanticBuilder.ClassDefinitions)
            model.RegisterClassDefinition(classDef);

        foreach (var (symName, symValue) in semanticBuilder.SymbolicCharacters)
            model.RegisterSymbolicCharacter(symName, symValue);

        foreach (var alphaDef in semanticBuilder.AlphabetDefinitions)
            model.RegisterAlphabetDefinition(alphaDef);

        // Resolve PROGRAM COLLATING SEQUENCE (needs alphabet definitions registered first).
        // An identity sequence (NATIVE / STANDARD-1 / STANDARD-2 on this ASCII host) is behaviorally
        // identical to having no program collating sequence, so normalize it to null: every consumer
        // then uses the proven native comparison path (which is trailing-space-insensitive like COBOL
        // alphanumeric comparison), instead of the weight-table path. Only a genuinely reordered
        // alphabet is stored, so it is still honored. This keeps STANDARD-* programs (e.g. the CCVS
        // boilerplate's `ALPHABET x IS STANDARD-2`) comparing exactly as native.
        if (semanticBuilder.ProgramCollatingSequenceAlphabetName is { } pcsName)
        {
            var alphaDef = model.ResolveAlphabetDefinition(pcsName);
            if (alphaDef != null && !IsIdentityCollation(alphaDef.CollatingSequence))
                model.SetProgramCollatingSequence(alphaDef.CollatingSequence);
        }

        // OPTIONS DEFAULT ROUNDED MODE (ISO §11.9.6) — the program-wide default for a bare ROUNDED phrase.
        model.SetDefaultRoundingMode(semanticBuilder.DefaultRoundingMode);

        foreach (var ext in semanticBuilder.ExtensionClauses)
            model.AddExtensionClause(ext);

        if (semanticBuilder.ScreenItems.Count > 0)
            model.RegisterScreenItems(semanticBuilder.ScreenItems);

        // Populate procedure symbols
        foreach (var sym in semanticBuilder.Symbols.Program.ProcedureDivisionScope.Symbols.Values)
        {
            if (sym is Semantics.ParagraphSymbol para)
                model.AddParagraph(para);
            else if (sym is Semantics.SectionSymbol sect)
                model.AddSection(sect);
        }

        foreach (var (sectionName, paragraphNames) in semanticBuilder.SectionParagraphs)
        {
            foreach (var paraName in paragraphNames)
                model.RegisterSectionParagraph(sectionName, paraName);
        }

        // OO (ISO §11.7): the per-method paragraph scopes (multi-method classes).
        model.ClassMethodScopes = semanticBuilder.ClassMethodScopes;

        // Populate data items
        foreach (var data in semanticBuilder.DataItemsInOrder)
        {
            if (data.LevelNumber is 1 or 77)
                model.AddDataRecord(data);
        }
        model.SetDataItemsInOrder(semanticBuilder.DataItemsInOrder);

        // Resolve PROCEDURE DIVISION USING parameters to LINKAGE SECTION DataSymbols
        var usingParams = new List<Semantics.DataSymbol>();
        foreach (var name in semanticBuilder.ProcedureUsingNames)
        {
            var sym = semanticBuilder.Symbols.Program.DataDivisionScope.Resolve<Semantics.DataSymbol>(name);
            if (sym != null)
                usingParams.Add(sym);
        }
        model.SetProcedureUsingParameters(usingParams);

        // Resolve PROCEDURE DIVISION RETURNING
        if (semanticBuilder.ProcedureReturningName != null)
        {
            var retSym = semanticBuilder.Symbols.Program.DataDivisionScope
                .Resolve<Semantics.DataSymbol>(semanticBuilder.ProcedureReturningName);
            model.SetProcedureReturningItem(retSym);
        }

        return model;
    }

    /// <summary>
    /// True when a 256-entry collating table is the identity mapping (weight[i] == i), i.e. it
    /// reorders nothing. NATIVE / STANDARD-1 / STANDARD-2 produce identity on this ASCII host, and
    /// an identity program collating sequence is indistinguishable from having none — so callers
    /// normalize it to null and use the native comparison path.
    /// </summary>
    private static bool IsIdentityCollation(byte[] sequence)
    {
        if (sequence.Length != 256) return false;
        for (int i = 0; i < 256; i++)
            if (sequence[i] != (byte)i) return false;
        return true;
    }

    private static CompilationResult EmitAssembly(
        List<CompiledProgram> programs,
        string outputPath,
        string sourcePath,
        DiagnosticBag diagnostics)
    {
        try
        {
            var mainProgram = programs[0];
            var assembly = CodeGen.CilEmitter.EmitAssembly(
                programs.Select(p => (p.IrModule, (Semantics.SemanticModel?)p.SemanticModel)).ToList(),
                mainProgram.ProgramId);

            // GetDirectoryName returns "" (not null) for a bare filename (e.g. -o prog.dll) → treat as the
            // current directory; Directory.CreateDirectory("") throws ArgumentException (empty path).
            string dir = Path.GetDirectoryName(outputPath) is { Length: > 0 } d ? d : ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            assembly.Write(outputPath);

            EmitRuntimeConfig(outputPath);
            CopyRuntimeLibrary(outputPath);

            return new CompilationResult(!diagnostics.HasErrors, outputPath, diagnostics.Diagnostics);
        }
        catch (Exception ex)
        {
            diagnostics.Report(DiagnosticDescriptors.COBOL0600,
                new SourceLocation(sourcePath, 0, 0, 0),
                TextSpan.Empty,
                programs[0].ProgramId, ex.Message);
            return new CompilationResult(false, outputPath, diagnostics.Diagnostics);
        }
    }

    private static void EmitRuntimeConfig(string outputPath)
    {
        string configPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");
        var version = Environment.Version;
        string tfm = $"net{version.Major}.{version.Minor}";
        string frameworkVersion = $"{version.Major}.{version.Minor}.{version.Build}";
        File.WriteAllText(configPath, $$"""
            {
              "runtimeOptions": {
                "tfm": "{{tfm}}",
                "framework": {
                  "name": "Microsoft.NETCore.App",
                  "version": "{{frameworkVersion}}"
                }
              }
            }
            """);
    }

    private static void CopyRuntimeLibrary(string outputPath)
    {
        string outputDir = Path.GetDirectoryName(outputPath) ?? ".";
        string compilerDir = Path.GetDirectoryName(typeof(Compilation).Assembly.Location)!;
        string candidatePath = Path.Combine(compilerDir, "CobolSharp.Runtime.dll");

        if (File.Exists(candidatePath))
        {
            string destPath = Path.Combine(outputDir, "CobolSharp.Runtime.dll");
            if (!File.Exists(destPath) ||
                new FileInfo(candidatePath).LastWriteTimeUtc > new FileInfo(destPath).LastWriteTimeUtc)
            {
                File.Copy(candidatePath, destPath, overwrite: true);
            }
        }
    }

    /// <summary>Extract the unit name from a programUnit, nestedProgram, or (OO) classDefinition context.</summary>
    private static string? ExtractProgramIdFromContext(ParserRuleContext ctx)
    {
        // OO: a class unit's name lives in its CLASS-ID paragraph (className → cobolWord), not an
        // identificationBody (which a classDefinition does not have). className() is an array — the class name is
        // first; a second entry (if INHERITS FROM is present) is the base class (see ExtractBaseClassName).
        if (ctx is CobolParserCore.ClassDefinitionContext cd)
            return cd.classIdParagraph()?.className() is { Length: > 0 } cn ? cn[0].cobolWord()?.GetText() : null;

        CobolParserCore.IdentificationDivisionContext? idDiv = ctx switch
        {
            CobolParserCore.ProgramUnitContext pu => pu.identificationDivision(),
            CobolParserCore.NestedProgramContext np => np.identificationDivision(),
            _ => null
        };

        return UnitName(idDiv?.identificationBody());
    }

    /// <summary>The first OBJECT method's <c>METHOD-ID</c> name in a class unit (slice 1 = one method), or null.
    /// <c>methodDefinition.methodName()</c> is an array (the rule names methodName at METHOD-ID and after END METHOD);
    /// the METHOD-ID name is the first.</summary>
    private static string? ExtractClassMethodName(CobolParserCore.ClassDefinitionContext cd)
        => cd.objectParagraph()?.methodDefinition() is { Length: > 0 } methods
            && methods[0].methodName() is { Length: > 0 } names
            ? names[0].cobolWord()?.GetText()
            : null;

    /// <summary>True if any method in the module's IR contains an <c>INVOKE SUPER</c> (used to reject SUPER in a
    /// root class — a class with no INHERITS FROM base — cleanly at compile time rather than at emit time.)</summary>
    private static bool ClassUsesSuper(IR.IrModule module)
        => module.Methods.Any(m => m.Blocks.Any(b => b.Instructions.Any(i => i is IR.IrInvoke { IsSuper: true })));

    /// <summary>The <c>INHERITS FROM</c> base class name of a class unit (OO slice 3), or null for a root class.
    /// <c>classIdParagraph.className()</c> is an array: [0] = this class's name, [1] = the base (present only when
    /// the <c>INHERITS FROM className</c> tail matched).</summary>
    private static string? ExtractBaseClassName(CobolParserCore.ClassDefinitionContext cd)
        => cd.classIdParagraph()?.className() is { Length: > 1 } cn ? cn[1].cobolWord()?.GetText() : null;

    /// <summary>The unit's name from either a PROGRAM-ID or a COBOL-2002 FUNCTION-ID paragraph (ISO §11.5).</summary>
    private static string? UnitName(CobolParserCore.IdentificationBodyContext? body)
        => (body?.programIdParagraph()?.programName() ?? body?.functionIdParagraph()?.programName())
            ?.cobolWord()?.GetText();

    /// <summary>Extract IS INITIAL attribute from a programUnit or nestedProgram context.</summary>
    private static bool ExtractIsInitialFromContext(ParserRuleContext ctx)
    {
        CobolParserCore.IdentificationDivisionContext? idDiv = ctx switch
        {
            CobolParserCore.ProgramUnitContext pu => pu.identificationDivision(),
            CobolParserCore.NestedProgramContext np => np.identificationDivision(),
            _ => null
        };

        var attrs = idDiv?.identificationBody()?.programIdParagraph()
            ?.programIdAttributes()?.programIdAttribute();
        if (attrs == null) return false;

        return attrs.Any(a => a.commonProgramAttribute()?.INITIAL_() != null);
    }

    /// <summary>Extract PROGRAM-ID from the first program in the tree (backward compat).</summary>
    private static string? ExtractProgramId(CobolParserCore.CompilationUnitContext tree)
    {
        var compilationGroups = tree.compilationGroup();
        if (compilationGroups.Length == 0) return null;

        var programUnit = compilationGroups[0].programUnit();
        if (programUnit.Length == 0) return null;

        var idDiv = programUnit[0].identificationDivision();
        return UnitName(idDiv?.identificationBody());
    }

    /// <summary>Compiled program tuple: PROGRAM-ID, IR module, and semantic model.</summary>
    private sealed record CompiledProgram(
        string ProgramId,
        IR.IrModule IrModule,
        Semantics.SemanticModel SemanticModel);
}
