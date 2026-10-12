## Entry 109 — 2026-03-18: Full-Scale Codebase Modernization — .NET 9, C# 13, Architectural Overhaul

A 10-phase modernization of the entire compiler codebase, driven by a comprehensive anti-pattern
catalog and staged migration plan. Every phase was gated by the full guard script (unit tests,
integration tests, 10 NIST golden-file regressions = 964 test cases). Zero regressions throughout.

**Phase 1 — Build modernization:**
- net8.0 → net9.0, C# 12 → C# 13, global.json (SDK 9.0.312), central package management
  (Directory.Packages.props). Compilation.EmitRuntimeConfig: hardcoded "net8.0" → Environment.Version.
- First regression caught immediately: 153 integration tests failed because compiled COBOL programs
  referenced System.Runtime 8.0 while the test host ran on 9.0. Root cause was the hardcoded
  runtimeconfig — a [MagicValues] anti-pattern that had been invisible on net8.0.

**Phase 2 — Lexer, tokenization, preprocessor:**
- TextSpan → record struct, Diagnostic → sealed record, CompilationResult → sealed record.
- Extracted CobolErrorListener from Compilation.cs to Parsing/CobolErrorListener.cs (primary constructor).
- ReferenceFormatProcessor: magic column numbers 6/7/65/60 → 5 named constants.
- CopyProcessor: primary constructor, MaxCopyDepth constant, FindKeywordAtLineStart consolidation.
- FrozenSet for ValidateParagraphs suspicious names. List.Exists over LINQ .Any().

**Phase 3 — Parser pipeline and type decomposition:**
- Compilation.cs split from 425 lines to 195 (−54%): StorageLayoutComputer, ParagraphValidator,
  FieldSizeCalculator extracted. Compilation.Compile now reads as a 6-step pipeline.
- [Duplication] eliminated: ComputeFieldSize (Compilation) and ComputeStorageSize (RecordLayoutBuilder)
  consolidated into FieldSizeCalculator.ComputeElementSize — single source of truth.
- StorageLocation → record struct. RecordLayout → record. PicLayout → sealed record.
  DataTypeSymbol → sealed record implementing ITypeSymbol.

**Phase 4 — Semantic model:**
- [LayerViolation] StorageAreaKind moved from CodeGen to Semantics — semantic layer no longer
  imports CodeGen. DataSymbol.FigurativeInit: int? with comment → FigurativeKind? enum.
- CategoryCompatibility: HashSet → FrozenSet. LoweringTable.Get(): per-call reflection → FrozenDictionary
  cached at static init. CategoryCompatibility arithmetic checks simplified to direct enum comparisons.

**Phase 5 — IR and lowering:**
- IrField, IrGlobal, IrParameter, IrLocal, IrTemp → sealed records. IrValue → record struct.
- IrMoveFigurative.FigurativeKind and BoundFigurativeExpression.FigurativeKind: int → FigurativeKind
  enum, traced end-to-end through BoundTreeBuilder → Binder → IR → CilEmitter (cast to int only
  at the CIL emission boundary). Primary constructors on IrType, IrRecordType, IrModule, IrMethod,
  IrBasicBlock.

**Phase 6 — Code generation and runtime:**
- PicEnvironment → sealed record. RecordLayoutBuilder.GetOccursCount trivial wrapper inlined.
- Identified CobolProgram/CobolField as legacy dead code (compiler never references them; only unit
  tests do). Flagged for future cleanup.

**Phase 7 — Numeric, PIC, and editing subsystems:**
- Dead MoveStatus struct removed (defined but never referenced). ArithmeticStatus.SizeError: public
  field → property, requiring CilEmitter update from Ldloc+Ldfld to Ldloca+Call (correct CIL for
  struct property access). 5 Substring calls → range slicing.

**Phase 8 — Diagnostics, logging, and tooling:**
- AcceptSourceKind enum moved from Compiler.Semantics.Bound to Runtime — shared between compiler
  and runtime. AcceptRuntime.Accept: int sourceKind → AcceptSourceKind enum. Magic 0x20 → (byte)' '.
  CLI: collection expression for CopyProcessor.

**Phase 9 — Final consolidation:**
- BasicBlock → primary constructor + collection expressions. ControlFlowGraph → sealed record.
  ParagraphReachabilityAnalyzer, PerformRangeChecker → primary constructors. BoundTreeBuilder:
  3 Substring → range slicing, StartsWith(string) → StartsWith(char).

**Phase 10 — Documentation:**
- Comprehensive XML doc comments across 27 source files. ~70 enum members documented with COBOL
  semantics and ISO references. ~80 public properties/methods documented. ~20 record parameters
  with <param> tags. Inline comments explain WHY (COBOL spec rationale), never WHAT.

**Cumulative anti-pattern scorecard:**
- 4 [GodObject] extractions (Compilation.cs → 5 focused components)
- 3 [LayerViolation] fixes (StorageAreaKind, AcceptSourceKind, DataSymbol→CodeGen dependency)
- 12+ [PrimitiveObsession] fixes (manual types → records/record structs, int → enums)
- 3 [Duplication] eliminations (FieldSizeCalculator, helper consolidation)
- 4 [HotAlloc] optimizations (FrozenSet, FrozenDictionary, List.Exists, simplified predicates)
- 3 [MagicValues] fixes (column constants, runtime version, magic bytes)
- 2 [DeadCode] removals (MoveStatus, GetOccursCount wrapper)
- 2 typed enum pipelines (FigurativeKind, AcceptSourceKind traced end-to-end)

**AI performance this session:**
- Executed all 10 phases in a single session with zero regressions.
- Learned mid-session to run guard.sh (NIST golden-file tests) after every phase, not just dotnet test.
- Caught ArithmeticStatus field→property CIL breakage immediately (Ldfld → Ldloca+Call).
- Caught namespace resolution issue (Runtime.AcceptSourceKind inside `using CobolSharp.Runtime`
  resolves to CobolSharp.Runtime.Runtime.AcceptSourceKind — fixed to unqualified AcceptSourceKind).

---

