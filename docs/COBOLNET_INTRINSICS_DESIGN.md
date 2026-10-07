# WiseOwl COBOL — Intrinsics, Special Registers & Misc (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the WiseOwl COBOL rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §12; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.
>
> **SPINE 1 — IMPLEMENTED (every ISO §15 function LIVE — PHASE-11 drove the `Deferred` backlog to ZERO).**
> `IntrinsicCatalog` (the complete §15 2023 table with D8 windows; every row now binds `Runtime` [a runtime
> body] or `Fold` [a compile-time fold — LENGTH/BYTE-LENGTH/the ALGEBRAIC family/WHEN-COMPILED] — the former
> `Unsupported` case (the A.4.9 non-support era) is DELETED with the module's claim at PB64 T6; the `Deferred` enum case remains
> only as the renderer's never-hit backstop), `BoundIntrinsicCall`,
> `Binding/Procedure/Verbs/IntrinsicBinder.cs` (P7 Step 12: FUNCTION arguments are REAL parse trees —
> `functionCall : FUNCTION functionName (LPAREN functionArgList? RPAREN)?`, each argument a §8.4.3.2 SR8 shape
> bound through the ONE `ExpressionBinder.BindExpr`; the keyword-omitted form binds the `functionArgument` items its
> reference's parsed `subscriptPart` already holds (`IntrinsicBinder.ArgumentsOf`, kb/Work PB2113); table(ALL) expansion, MAX/MIN resolution, LENGTH
> fold, the §15.68.3 r3 currency injection), `IntrinsicRenderer` (ONE instance channel — numeric + string; the
> static twin is deleted; arguments render through the ONE `NumericRenderer`, `ReceiverContext.None` in the
> string channel), runtime `CobolIntrinsics`/`CobolDate`. The 1989 Intrinsic Function Module (42
> functions) is fully implemented — all 42 NIST IF programs byte-match. **Two design points worth flagging:**
> (1) `BoundIntrinsicCall.Args` is `IReadOnlyList<BoundOperand>`, NOT `BoundExpr` — the string-argument
> functions (NUMVAL, ORD, LOWER-CASE …) take alphanumeric operands the numeric expression tree cannot represent;
> numeric argument expressions wrap as `BoundComputedOperand`. (2) The node carries a binder-set `Collate` flag
> for CHAR/ORD under a NON-identity PROGRAM COLLATING SEQUENCE (§15.15.4 r2/§15.70.4) so the backend passes its
> `__COLLATE` weights only when the field exists (hazard H5) — backend-neutral semantics, not a rendered
> fragment. WHEN-COMPILED is realized as the spec-correct COMPILE-TIME constant (§15.99.3 r2) baked into the
> generated source via the injectable `IntrinsicBinder.CompileClock` (D6).

## Summary

DESIGN FOR THE WiseOwl COBOL (greenfield COBOL→idiomatic-C#/Roslyn) target. The subsystem has TWO graded spines plus several smaller surfaces; everything is designed to fit the backend-neutral bound-tree pipeline (SSOT §1.1: ANTLR parse tree → binder → ONE bound tree → `ICodeGenBackend`, `--backend roslyn|cil`; Roslyn C#-source is the primary backend, Cecil/CIL is future-additive with its own private lowering — NO shared lowered IR). The binder resolves every FUNCTION call to a structured `BoundIntrinsicCall` (the resolved `IntrinsicSig` + typed bound arguments — never a pre-rendered C# fragment); backends only RENDER it, routing through the singular `CobolNum.Store(value,scale,profile)` and `CobolString.Store(value,width)` paths and the native substrate (fixed-point = `long` holding the UNSCALED value; float = `double`; alphanumeric = UTF-16 `string`; bool = `bool`; no byte[], no software `decimal`/`BigInteger` by default).

SPINE 1 — INTRINSIC CATALOG (the graded deliverable). Reject porting the legacy `decimal`-typed signatures. Instead build ONE declarative table `IntrinsicCatalog`: name → {ISO §15.2 function-type, result category, arity model (fixed N / optional trailing / variadic), per-arg category, binding = compile-time-fold | runtime-method}. §15.2 gives exactly six types — alphanumeric / boolean / national / numeric / INTEGER / index — and THAT classification IS the return-type column, mapped to the native substrate: integer-function→`long` (`Int128` past 18 digits, e.g. FACTORIAL); floating-point math (SQRT, SIN/COS/TAN/ASIN/ACOS/ATAN, LOG/LOG10/EXP/EXP10, PI, STANDARD-DEVIATION, VARIANCE, ANNUITY, PRESENT-VALUE, RANDOM)→`double`; exact numeric (SUM, MEAN, MEDIAN, MIDRANGE, RANGE, MAX/MIN-numeric, MOD, REM, INTEGER, INTEGER-PART, FRACTION-PART, ABS, SIGN, NUMVAL, NUMVAL-C, NUMVAL-F)→`NumX` (unscaled `long`+scale) so it flows straight into `CobolNum.Store` with the receiver's ROUNDED; alphanumeric/national (UPPER-CASE, LOWER-CASE, REVERSE, TRIM, CONCAT [§15.18, 2023 — CONCATENATE is NOT an ISO function at any edition and is intentionally absent, PHASE-11 Step 7], SUBSTITUTE, CHAR, CHAR-NATIONAL, NATIONAL-OF, DISPLAY-OF, the date-string functions)→`string`; boolean→`bool` (BOOLEAN-OF-INTEGER on the D-B1 '0'/'1' substrate, PHASE-11 Step 2). Runtime home = `CobolNet.Runtime.CobolIntrinsics` (numeric/string/financial/conversion families) + `CobolNet.Runtime.CobolDate` (date/time). Mine the legacy `IntrinsicFunctions.cs` for BEHAVIOR only (NaN/out-of-domain→EC-ARGUMENT-FUNCTION default result; ORD-MAX/ORD-MIN tie = first; date algorithms; NUMVAL parsing), not for types. The catalog is consulted in the BINDER at the function-call operand cell (`IntrinsicBinder`, the `BindPrimary` hook) and at the DISPLAY/MOVE-source/condition-operand binding paths — it is the single source of result-category truth, replacing the legacy ad-hoc `AlphanumericFunctions` HashSet + special-cases. MAX/MIN are category-polymorphic (resolve by arg category at the call site); `table(ALL)` expansion and variadic `params` port from legacy; FUNCTION LENGTH folds at compile time from PIC metadata.

SPINE 2 — SPECIAL-REGISTER REGISTRY (the other graded deliverable). Model every special register as a SYNTHESIZED `DataItem` (with a NumProfile-or-string type, a storage class, read-only/compile-time-fold flags, and a D8 edition-availability window from `docs/VERSION_CHANGE_REFERENCE.md`) registered in `DataBinder.ByName`, so a register read/store reuses the EXACT SAME `CobolNum`/`CobolString` paths as any data item — zero special-case in the verb emitters. RETURN-CODE/TALLY/SORT-RETURN→static `long`; WHEN-COMPILED→compile-time constant string (timestamp captured at compile, injectable for determinism); LENGTH OF/BYTE-LENGTH→folded `long` byte-size from PIC+USAGE; ADDRESS OF→`ManagedPointer`; LINAGE-COUNTER/LINE-COUNTER/PAGE-COUNTER and XML-*/JSON-* register NAMES are reserved by the registry but attach to their (scope-flagged) subsystems.

SMALLER SURFACES (one approach each, dependency named): figurative constants modeled as a context-materialized sentinel resolved at each use site against the receiver category+width per §8.3.3.6 (ZERO→`0L` or `'0'`-fill; SPACE→space-fill; QUOTE→`'"'`; ALL lit + symbolic-char→repeat-to-width); HIGH/LOW-VALUE on a UTF-16 string = U+00FF/U+0000 (alphanumeric) and U+FFFF/U+0000 (national), which preserves ordinal ordering through `CobolString.Compare` for the corpus (full ALPHABET/CODE-SET fidelity sits on the G6 char↔byte boundary — G6-core is implemented; the custom-ALPHABET/CODE-SET collating tail is still open). INITIALIZE = compile-time tree-walk to per-elementary typed stores (default/VALUE/REPLACING; FILLER skipped). SET = dispatch by target kind (index→long, pointer→ManagedPointer/NULL, switch→bool, cond-name TO TRUE→store the 88's first VALUE) — the 88-level binding (`Condition88`/`BoundSetConditions`) and OCCURS INDEXED BY index fields are bound in DataBinder; only level-66 and the pointer/switch arms remain open. ACCEPT/DISPLAY system sources = a `CobolSystem` runtime with an INJECTABLE clock (DATE/DAY/TIME/DAY-OF-WEEK/YYYYMMDD/YYYYDDD) + console UPON SYSOUT/SYSERR/mnemonic. ALPHABET/CLASS/CURRENCY/DECIMAL-POINT IS COMMA = a SPECIAL-NAMES config object threaded into emit (mostly compile-time). SCREEN SECTION, REPORT WRITER, JSON/XML GENERATE/PARSE are scope-flagged big subsystems — designed only to the seam (reserve their register names, one-paragraph deferral each).

## Decisions

### D1. Do NOT port the legacy decimal-typed intrinsic signatures; type every intrinsic by its ISO §15.2 declared function-type mapped to the native substrate (integer→long/Int128, floating-point math→double, exact numeric→NumX, alphanumeric/national→string, boolean→bool).

**Rationale.** The owner-locked model bans the software decimal substrate; §15.2 is the spec-authoritative classification and it maps cleanly onto the four native types CobolNet already speaks. Exact-numeric→NumX lets intrinsic results flow through the SINGLE CobolNum.Store(value,scale,profile) path with the receiver's ROUNDED, matching the singular-pattern doctrine.

**Rejected alternatives.** (a) Reuse legacy decimal bodies as-is — reintroduces banned software-decimal, less spec-faithful, and forces a decimal↔long boundary at every call site. (b) Make every intrinsic return double — loses base-10 exactness for SUM/MOD/INTEGER-PART and breaks ROUNDED determinism.

### D2. One declarative IntrinsicCatalog table (name → §15.2 type, RESULT-TYPE RULE, arity, arg categories, runtime-method-or-fold) is the single source of result-category truth, consulted during emission.

**Rationale.** Replaces the legacy ad-hoc AlphanumericFunctions HashSet + scattered special-cases with one table that the NumPrimary, DISPLAY, MOVE-source, and condition-operand paths all consult. Decades-sustainable: adding a function is one row. Fits the bound-tree pipeline (the binder consults the catalog once and produces a structured bound node; no lowered IR; backends only render — the dual-backend discipline, SSOT §1.1/§18 #23).

**⛔ THE TYPE COLUMN IS A RULE, NOT A SCALAR — and this decision was silently false for a year (fix-queue PB15).** Twenty §15 functions do not have a constant type: their own §15.x.1 clause carries a table whose left column is an ARGUMENT property and whose right column is the function type ("The type of this function depends on the type of argument-1 as follows"). A scalar column cannot express that, so each such function needed a hand-written exception in the binder — and only five ever got one, in **two separate name lists** (CA25's `RuntimeMethod is "UpperCase" or "LowerCase" or "Reverse"`, V54's `Name is "MAX" or "MIN"`). The other ten stayed mislabelled, every one a silent under-rejection: a national result labelled alphanumeric passes the §14.9.25.3 Table-16 MOVE guard instead of being rejected by it. **Three mechanisms decided one property, so "the single source of result-category truth" named a file that was not one.**

The row now carries `IntrinsicResultRule` — seven spec-derived shapes covering all twenty functions (`FollowsArgument1` alone covers ten) — and `IntrinsicResultType.Resolve` is the ONE reader, called from the generic bind path **and from every bespoke one**. That last clause is load-bearing: TRIM and SUBSTITUTE parse phrase keywords and build their own bound node, so a correct catalog row left them wrong until their construction sites read the rule too — the two-arm dispatch in its silent form, with the fix present and every test green.

**The resolved type is also the function's §14.9.25.3 Table-16 ROW as a MOVE sender (kb/Work PB73, adjudicated 2026-08-18).** §15.2 item 5 gives an INTEGER function "no digits to the right of the decimal point" and item 4 gives a NUMERIC function only "an operational sign"; §8.4.3.2.3 SR11 says a numeric function is never an integer operand "even though a particular reference … might yield an integer value". So `MoveBinder` builds the intrinsic sender's `Table16Operand` with `IsNonInteger = ResultCategory is Numeric && !IntrinsicResultType.IsIntegerOperand(call)` — the ONE integer classifier, resolved per call, so `MAX(3 -14 8)` is the Integer row and `MAX(1.5 2)` the Noninteger one — and Table 16 refuses a NUMERIC function into an alphanumeric / national / boolean / alphabetic receiver (COBOLNET0819) exactly as it refuses a noninteger literal or item. `--permissive` keeps the earlier admission with a warning (the CONFORMANCE.md item-92 text form). Before this the computed-sender arm carried `IsNonInteger: false` for every function, an under-rejection.

**Resolve returns the §15.2 TYPE, not the category.** `ResultCategory` folds INTEGER, NUMERIC and INDEX into `PicCategory.Numeric` — correct for every consumer that exists today, and precisely why resolving to a category would have made the four integer-following rules unrepresentable, i.e. dead members reading as coverage. Resolving the type keeps D1's §15.2 classification intact and leaves the answer already correct for the first consumer of integer-ness that arrives.

**`IntrinsicResultTypeDriftTests` re-derives the population from `specs/ISO_COBOL.md` itself** — the structural key is a markdown table in §15.x.1 whose header names the "Function type" column — so a function whose clause grows a table, or a new function that has one, fails the build until its row declares a rule. Without that, this decision decays exactly the way it decayed the first time: silently, because a name list cannot say what it omits.

**Rejected alternatives.** Per-call-site if/else chains (the legacy ad-hoc approach) — violates the refactor-first/canonical-dispatch doctrine and drifts out of sync; PB15 is the measured proof, since that is what the two name lists were. A class-per-function hierarchy — over-engineered for a static catalog. Keying the drift test on the clauses' PROSE rather than on the table — the wordings differ ("depends on the argument type" §15.38.1, "depends on the type of argument-1" §15.39.1, "depends upon the argument types" §15.18.1) and a prose key found ten of twenty, a dead guard inside the guard against a dead lookup.

### D3. Model every special register as a synthesized DataItem registered in DataBinder.ByName (with type, storage class, read-only/fold flags).

**Rationale.** A register read/store then reuses the EXACT CobolNum/CobolString paths every data item uses — zero special-case in the verb emitters. RETURN-CODE/TALLY are static long fields; WHEN-COMPILED/LENGTH OF fold to constants; ADDRESS OF→ManagedPointer.

**Rejected alternatives.** A parallel special-register subsystem with its own read/emit logic — duplicates the data path (two mechanisms for one job, the banned anti-pattern).

### D4. Figurative constants are context-materialized sentinels resolved at the use site against the receiver's category+width (§8.3.3.6), never fixed strings/values of their own.

**Rationale.** A figurative has no intrinsic length/type; ZERO is 0L to a numeric receiver but '0'-fill to alphanumeric; ALL lit + symbolic-char repeat-to-width. Materializing at the site is the only spec-correct model.

**Rejected alternatives.** Pre-expanding a figurative to a fixed-width string at parse time — wrong, since width is only known at the receiver, and numeric vs alphanumeric meaning diverges.

### D5. HIGH-VALUE/LOW-VALUE on the UTF-16 string substrate = U+FFFF/U+0000 for BOTH classes (owner decision kb/Work R52; kb/Work PB1093, PB1759).

**Rationale.** §8.3.3.6.4 GR6/GR7 define them as the characters with the highest/lowest ordinal position in the collating sequence, and the native alphanumeric and national sequences are both the 65,536 UTF-16 code units in code-unit order (DOC-A.1-31/-188), so `FUNCTION ORD(HIGH-VALUE)` is 65536 and no character compares above HIGH-VALUE. One member answers it everywhere: `CobolNet.Runtime.NativeCollatingSequence`. On a one-byte medium, and in a byte-form leaf's image, HIGH-VALUE is the byte 0xFF by the storage-byte law (`StorageByte`, design COBOLNET_FILES D29).

**Rejected alternatives.** U+00FF for alphanumeric HIGH-VALUE (the former pin) — left U+0100..U+FFFF ordered above "the highest character" (PB1093). Storing a byte sentinel — reintroduces the byte substrate.

### D6. Date/time/WHEN-COMPILED nondeterminism handled by an injectable clock (CobolSystem.Clock) in the runtime.

**Rationale.** CURRENT-DATE, ACCEPT FROM DATE/TIME, and WHEN-COMPILED are nondeterministic; an injectable clock makes conformance .out comparison reproducible (the legacy already needs this) and keeps the generated C# testable.

**Rejected alternatives.** Direct DateTime.Now calls — untestable, breaks golden-output conformance.

### D7. Compute LENGTH OF / FUNCTION BYTE-LENGTH from compile-time PIC+USAGE byte-size metadata kept in PicInfo; keep FUNCTION LENGTH (character positions, §15.50) separate from LENGTH OF/BYTE-LENGTH (byte size).

**Rationale.** The byteless data model stores no byte count, but byte-size is a pure function of PIC+USAGE known at compile time, so it folds to a constant. FUNCTION LENGTH counts character positions (already folded in legacy BindLength); LENGTH OF/BYTE-LENGTH count bytes — for COMP/COMP-3 these differ and must not be conflated.

**Rejected alternatives.** Treating LENGTH OF == FUNCTION LENGTH — wrong for COMP/COMP-3/national where byte size != char positions. Storing a runtime byte length — reintroduces byte bookkeeping.

### D8. Per-edition gating (G1): availability is a catalog/registry COLUMN (IntroducedIn/RemovedIn from `docs/VERSION_CHANGE_REFERENCE.md`), enforced by the binder against `--std` with a specific diagnostic — the co-equal obligation to implementing the behavior.

**Rationale.** The intrinsic library is one of the most edition-varying surfaces in the standard: the FUNCTION facility postdates base COBOL-85, and 2002/2014/2023 each add (and occasionally remove or alter) functions, special registers, and figuratives (e.g. NULL). The mission is four compilers in one executable (ISO COBOL 1985 / 2002 / 2014 / 2023). Every edition-varying construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed) — "FUNCTION X requires COBOL-yyyy; current --std is yyyy". Tests (NIST etc.) only VERIFY; they never SCOPE. Window data comes from `docs/VERSION_CHANGE_REFERENCE.md`, the 130-row edition-change checklist (2002→2023 deltas ONLY — it has NO 85→2002 rows; derive 85↔2002 gating from the 2002 standard / the ISO2023_CONFORMANCE_PLAN M2 catalog). The version test matrix (`docs/VERSION_TEST_MATRIX_DESIGN.md`, the (construct × edition) matrix; Phase 0 done) gains a (function × edition) row per catalog entry: compiles+behaves inside the window, rejected-with-diagnostic outside it.

**Rejected alternatives.** Per-edition grammar gating — availability is a binding-time property of an identical token stream, and the diagnostic must NAME the function and editions. A separate per-edition allowlist file — duplicates the catalog (two mechanisms for one job, the banned anti-pattern).

## C# mapping (the RoslynBackend rendering; the future CilBackend renders the same bound nodes via the same runtime entry points)

RETURN-TYPE DISCIPLINE (§15.2) — the spine, with examples:

  COBOL: COMPUTE R = FUNCTION SQRT(N)            [N is PIC 9(4), R is PIC 9(4)V99]
  C#:    R = CobolNum.Store((long)(System.Math.Sqrt((double)N) * 100m_pow), 2, _P_R);
         // SQRT is a floating-point function → double; result re-scaled into R's NumX at scale 2, stored via the
         // singular CobolNum.Store path. (The emitter wraps the double result back to NumX at the receiver scale.)

  COBOL: COMPUTE T = FUNCTION SUM(A B C)         [exact numeric function → NumX]
  C#:    T = CobolNum.Store(CobolIntrinsics.SumScaled(new[]{(A,0),(B,0),(C,0)}), 0, _P_T);
         // SUM is an EXACT numeric function: arguments flow as (unscaledLong, scale) pairs; result is NumX → Store.
         // NOT decimal. Scale alignment uses the same Align/Combine machinery the arithmetic verbs already use.

  COBOL: MOVE FUNCTION UPPER-CASE(NM) TO OUT     [alphanumeric function → string]
  C#:    OUT = CobolString.Store(CobolIntrinsics.UpperCase(NM), 10);

  COBOL: COMPUTE F = FUNCTION FACTORIAL(20)      [integer function, overflows long]
  C#:    F = CobolNum.Store((long)CobolIntrinsics.FactorialI128(20), 0, _P_F);
         // integer functions → long; FACTORIAL/large products compute in Int128, size-error past Int128 range.

THE CATALOG (declarative, in CobolNet.Binding):

  enum IntrinsicType { Alphanumeric, Boolean, National, Numeric, Integer, Index }  // ISO §15.2
  enum ArgArity     { Fixed, OptionalTrailing, Variadic }                          // §15.3
  enum IntrinsicBind { Fold, Runtime }                                             // compile-time vs runtime call
  readonly record struct IntrinsicSig(string Name, IntrinsicType Type, PicCategory ResultCategory,
      ArgArity Arity, int MinArgs, int? MaxArgs, string RuntimeMethod, IntrinsicBind Bind,
      int IntroducedIn, int? RemovedIn);  // edition window (85/2002/2014/2023) from docs/VERSION_CHANGE_REFERENCE.md;
      // the binder rejects a FUNCTION outside its window for Options.DialectLevel with a per-edition diagnostic (D8)
  static class IntrinsicCatalog {
      static readonly Dictionary<string,IntrinsicSig> _t = new(StringComparer.OrdinalIgnoreCase){ ... ~95 rows ... };
      public static bool TryGet(string name, out IntrinsicSig sig) => _t.TryGetValue(name, out sig);
      // category-polymorphic resolution for MAX/MIN at the call site:
      public static IntrinsicSig Resolve(string name, IReadOnlyList<PicCategory> argCats) { ... }
  }

BINDER HOOK — the function-call operand cell binds to a structured node the
backends render (ALL semantics — availability per D8, arity/category checks, table(ALL) expansion, MAX/MIN resolution —
happen in the binder; the sketch below is the RoslynBackend RENDERING; a future CilBackend renders the SAME node as a
direct call to the same `CobolNet.Runtime` method):

  // Args are BoundOperand (not BoundExpr — string-argument functions take alphanumeric operands, see the status
  // banner) and the binder sets Collate for CHAR/ORD under a non-identity PCS:
  sealed record BoundIntrinsicCall(IntrinsicSig Sig, IReadOnlyList<BoundOperand> Args, PicCategory ResultCategory, bool Collate = false) : BoundExpr;
  // RoslynBackend, numeric result — the function-call operand cell returns:
  if (pe.functionCall() is { } fc) return EmitIntrinsicNum(fc);   // → NumX (numeric/integer/exact)
  // in ReadAsString / SendAsString / OperandAsString: if functionCall && result is alphanumeric → EmitIntrinsicStr(fc)

  private NumX EmitIntrinsicNum(Core.FunctionCallContext fc){
     string name = fc.functionName().GetText();
     if (name.Equals("LENGTH",IC)) return new NumX($"{FoldLength(arg0)}L", 0);     // §15.50 compile-time fold
     var sig = IntrinsicCatalog.Get(name);
     var args = BindIntrinsicArgs(fc, sig);                                         // table(ALL) expanded, typed per sig
     return sig.Type switch {
        IntrinsicType.Integer  => new NumX($"CobolIntrinsics.{sig.RuntimeMethod}({IntArgs(args)})", 0),
        IntrinsicType.Numeric when IsFloatFn(sig) =>
            new NumX($"(long)(CobolIntrinsics.{sig.RuntimeMethod}({DblArgs(args)}) * {Pow10(_targetScale)})", _targetScale),
        IntrinsicType.Numeric  => /* exact: emit a CobolIntrinsics.*Scaled returning unscaled long at a known scale */,
        _ => throw ... };
  }

SPECIAL REGISTERS — synthesized DataItems (in DataBinder bootstrap), reusing the normal read/store paths:

  // RETURN-CODE: a real static long field + a synthesized numeric DataItem in ByName, NumProfile S9(4).
  private static long RETURN_CODE = 0L;        // also written by GOBACK/STOP RUN and read as $? exit code (cross-dep: control-flow/CALL)
  // WHEN-COMPILED: compile-time-folded constant string item (§15.99.3 r1 — the 21-char YYYYMMDDHHMMSShh±hhmm
  // form, same structure as CURRENT-DATE, rendered by the ONE CobolDate.Format21 formatter).
  private const string WHEN_COMPILED = "2026060812000000+0000";  // captured at compile (injectable for determinism)
  // LENGTH OF X / FUNCTION BYTE-LENGTH(X): folded long from PicByteSize(item):
  long _len_of_X = 18;   // PIC byte-size computed from PIC+USAGE metadata (NOT stored anywhere at runtime)
  // ADDRESS OF X → ManagedPointer; LINAGE-COUNTER/LINE-COUNTER/PAGE-COUNTER → owned by file/RW subsystem.

FIGURATIVE CONSTANTS — context-materialized at the use site (CobolNet.Binding.Figurative):

  enum Fig { None, Zero, Space, HighValue, LowValue, Quote, Null, AllLiteral }
  // MOVE ZERO TO X → CobolString.Store(new string('0', width), width)  [alphanumeric receiver]
  // MOVE ZERO TO N → CobolNum.Store(0L, 0, _P_N)                        [numeric receiver]
  // MOVE SPACES TO X → CobolString.Store(new string(' ', width), width)
  // MOVE HIGH-VALUES TO X → CobolString.Store(new string('ÿ', width), width)   [alphanumeric]
  // MOVE LOW-VALUES  TO X → CobolString.Store(new string('\0', width), width)
  // MOVE ALL "AB" TO X(5) → CobolString.Store("ABABA", 5)              [§8.3.3.6 repeat-to-width-then-trim]

INITIALIZE — compile-time walk → typed stores (CobolNet.CodeGen):
  // INITIALIZE REC → for each elementary leaf: numeric→Store(0L,...); alphanumeric→Store(spaces,...)
  // INITIALIZE REC REPLACING NUMERIC BY 1 / TO VALUE → per-leaf override; OCCURS → emitted as a for-loop over the array.

SET — dispatch by target kind (CobolNet.CodeGen.EmitSet):
  // SET IDX TO 3            → IDX = 3;                       (index = long)
  // SET P TO ADDRESS OF X   → P = ManagedPointer.Of(ref X);
  // SET SW TO ON/OFF        → SW = true/false;              (mnemonic switch = bool)
  // SET FLAG-IS-OK TO TRUE  → FLAG = <the 88's first VALUE>; (cond-name; depends on G2-cont 88-level support)

ACCEPT/DISPLAY SYSTEM SOURCES — CobolNet.Runtime.CobolSystem with an injectable clock:
  static class CobolSystem { public static IClock Clock = SystemClock.Instance;
     public static string DateYYMMDD()  => Clock.Now.ToString("yyMMdd");
     public static string TimeHHMMSScc()=> Clock.Now.ToString("HHmmss") + Cs(Clock.Now);
     public static long DayOfWeek()     => ((long)Clock.Now.DayOfWeek + 6) % 7 + 1;  // 1=Mon..7=Sun
     /* DAY (yyDDD), DateYYYYMMDD, DayYYYYDDD likewise */ }
  // ACCEPT X FROM DATE  → X = CobolString.Store(CobolSystem.DateYYMMDD(), width)  (or numeric Store if X is numeric)
  // DISPLAY A B UPON SYSERR → System.Console.Error.WriteLine(imageA + imageB)

## Hard problems

### Intrinsic return type vs the native scaled-long substrate: SUM/INTEGER-PART must stay exact base-10, SQRT/trig are inherently floating, FACTORIAL overflows long.

Three-way split by §15.2 type: exact-numeric intrinsics return NumX (unscaled long + scale) and route through CobolNum.Store with the receiver's scale+ROUNDED; floating-point-math intrinsics compute in double then the emitter rescales the double result back into the receiver's NumX scale; integer intrinsics return long, escalating to Int128 for FACTORIAL/large products with EC-ARGUMENT-FUNCTION/size-error past Int128 range.

### HIGH-VALUE/LOW-VALUE/custom ALPHABET on a UTF-16 string substrate — the spec defines them by collating-sequence ordinal, but there is no byte image.

Map HIGH/LOW-VALUE to U+FFFF/U+0000 in both classes (D5; owner decision kb/Work R52), the extremes of the 65,536-character native sequence. Custom ALPHABET/collating, CODE-SET round-trip, and REDEFINES-over-bytes are explicitly the char↔byte boundary deferred to G6; SPECIAL-NAMES ALPHABET config is threaded into a collating table that feeds Compare/CHAR/ORD/figuratives when G6 lands.

### LENGTH OF / BYTE-LENGTH want a byte count that the byteless model never stores.

Keep a compile-time PicByteSize derived from PIC+USAGE in PicInfo (DISPLAY=char count; COMP=1/2/4/8 by digits; COMP-3=digits/2+1; national=2×chars) and fold LENGTH OF/BYTE-LENGTH to that constant. Keep it strictly separate from FUNCTION LENGTH (§15.50 character positions).

### RETURN-CODE has a triple role: a readable/writable special register, the process exit code, and the conduit for CALL RETURNING / GOBACK.

Model RETURN-CODE as a synthesized static long DataItem (normal read/store), have GOBACK/STOP RUN read it for the process exit code, and document the cross-dependency with the control-flow/CALL subsystem so they wire writes into the same field (one canonical location, no second mechanism).

### SET cond-name TO TRUE, SET index, and class/condition-name conditions depend on data-division features (88-level binding + OCCURS INDEXED BY are bound in DataBinder; level 66 is still skipped).

The SET dispatch slots (index→long, cond-name→store the 88's first VALUE into its parent, switch→bool, pointer→ManagedPointer) rely on the 88-level binding and INDEXED BY dependencies (`Condition88`, DataBinder index-name registration), so the cond-name/index arms are implemented directly; only the level-66 and switch-mnemonic dependencies remain stubbed — the catalog/registry spines do not depend on this.

### TWENTY functions are category-polymorphic — the result TYPE follows the arguments, per the §15.x.1 result-type table each one carries.

⚠ **This section used to name MAX/MIN only, and that framing is what let the defect spread** (fix-queue PB15): treating it as a two-function quirk produced a two-function fix, twice. It is a §15-wide rule shape. The catalog row carries `IntrinsicResultRule` and `IntrinsicResultType.Resolve` reads it at bind time; **`IntrinsicResultTypeDriftTests` derives the population from the spec**, so this list is documentation, never the register.

The seven shapes, and the functions each covers:

| Rule | Functions | The table it implements |
|---|---|---|
| `FollowsArgument1` | BASECONVERT §15.12 · FORMATTED-CURRENT-DATE §15.38 · FORMATTED-DATE §15.39 · FORMATTED-DATETIME §15.40 · FORMATTED-TIME §15.41 · LOWER-CASE §15.57 · REVERSE §15.78 · SUBSTITUTE §15.87 · TRIM §15.96 · UPPER-CASE §15.97 | Alphabetic→Alphanumeric · Alphanumeric→Alphanumeric · National→National |
| `IntegerFollowsArgument1` | ABS §15.7 · HIGHEST-ALGEBRAIC §15.43 · LOWEST-ALGEBRAIC §15.58 · SMALLEST-ALGEBRAIC §15.83 | Integer→Integer, every other admitted row→Numeric |
| `IntegerFollowsAllArguments` | RANGE §15.76 · SUM §15.88 | All arguments integer→Integer · otherwise Numeric |
| `FollowsUniformArguments` | MAX §15.59 · MIN §15.63 | the six-row table below |
| `FollowsConcatArguments` | CONCAT §15.18 | keyed on class AND usage |
| `FollowsDestinationFormat` | CONVERT §15.19 | keyed on the destination KEYWORDS, not an argument |
| `Fixed` | everything else | §15.x.1 states one type, or §15.2/§15.6 does |

**MAX/MIN in full** (§15.59.1/§15.63.1, over the uniform §15.59.3/§15.63.3 r2 argument list): alphabetic or alphanumeric → an ALPHANUMERIC result (the selected string); NATIONAL → a NATIONAL result (the selected national string — NOT alphanumeric); INDEX → an INDEX result; all-integer → an INTEGER result; otherwise → NUMERIC. The size of an alphanumeric or national result is the size of the selected argument-1 (§15.59.4/§15.63.4 r3). ⚠ The INDEX row is **not** a `PicCategory` — an index item is PICTURE-less and this model carries it as `PicInfo.IndexItem`, category Numeric with usage Index, so a category-keyed test would route it to the numeric arm and, since its scale is 0, answer INTEGER: a plausible wrong type rather than a visible failure.

**ORD-MAX §15.71 / ORD-MIN §15.72 are `Fixed` and share none of this.** They always return an integer ordinal — their §15.71.1/§15.72.1 clauses carry no result-type table at all — but they DO dispatch their comparison to the string body by the same argument category. That `RuntimeMethod` choice is deliberately kept separate from the result-type resolution in the binder: folding the two together is what made the old code read as if MAX's type and MAX's comparison body were one decision.

**The FORMATTED-\* family is the row most easily read backwards.** Their type follows argument-1 — the FORMAT literal (§15.38.3 r1 and siblings admit "a national or alphanumeric literal") — so `FUNCTION FORMATTED-DATE(N"YYYYMMDD" D)` is a NATIONAL function even though what it renders is an integer date.

### FUNCTION argument shapes: table(ALL) expansion, optional trailing args (e.g. RANDOM seed), and variadic statistical functions — REAL argument parse trees (P7 Step 12).

FUNCTION arguments parse through the grammar (`functionArgList` — the lexer suppresses the SUBSCRIPT push after
`FUNCTION functionName`, keeps the §8.3.5 comma/semicolon separator as `FNARG_SEPARATOR`, and lexes
sign-adjacent-after-separator literals as `SIGNED_*` per §8.7.1/§8.3.3.3.2, so `MAX(A -4)` is two arguments and
`MAX(A - 4)` one). Each argument binds via `BindArgOperand` — non-numeric literals stay categorized literal
operands, a sole data reference stays a `BoundFieldOperand`, anything arithmetic goes through the ONE
`ExpressionBinder.BindExpr` and wraps as `BoundComputedOperand` — with a `table(… ALL …)` argument bound by
`TryBindAllArgument` as ONE enumerating operand (below), driven by the catalog's arity model
(Fixed/OptionalTrailing/Variadic). The five phrase-keyword functions
(TRIM/FIND-STRING/SUBSTITUTE/CONVERT/MODULE-NAME) classify bare-word arguments by name (`KeywordWordOf`) and each
walks its §15.x.2 general format BY POSITION, never as an order-free keyword harvest — CONVERT's slot 0 is ALWAYS
argument-1 and binds as an operand, so a data item named one of the §8.10 context-sensitive words (NAT/ANUM/HEX/
BYTE, reserved only where CONVERT's format permits them) stays a legal argument-1, and an operand after the format
words is rejected (PB59 / FMT-15.19.2); the §8.9
reserved-word funnel skips a BARE argument word (a §15 phrase-word position is not a provable user-word slot).
The keyword-omitted reference form (D2 — no grammar alternative of its own) parses its arguments as its reference's
`subscriptPart`, whose items are the SAME `functionArgument` rule (`IntrinsicBinder.ArgumentsOf`; kb/Work PB2113 retired
the `FunctionArgFragment` text re-parse and the lexer's `PrimeFunctionArgs`), and
`UdfBinder.UdfBindCall` binds its arguments through the same `BindArgOperand` — ONE argument pipeline; the former
hand-rolled per-segment recursive-descent parser is deleted. That pipeline is also where §8.4.3.2.4 GR2's LEFT-TO-RIGHT
argument order is settled for every function (kb/Work PB1423): `BindArgOperand` records each bound operand into the
`ArgumentOrder` window `BindIntrinsicCore` opened, and `Settle` stores an earlier argument into an intermediate result
item when a later argument's user-function activation would otherwise be evaluated first (see
COBOLNET_INTERPROGRAM_DESIGN.md).

**The §15.3 argument-rule SCREEN (`IntrinsicArgumentRules`, driven from `IntrinsicBinder.CheckArgumentClasses` after
arity and before every per-function arm) is a per-position SCHEMA, not a class column** (PB1 → PB12 → PB31 →
PB35 → PB58): each catalogued function has an `ArgSchema` — per-position `ArgRule`s plus a variadic tail — whose
class KIND (`'n'` numeric, `'i'` §15.3 type-6 integer, `'s'` the string family — class alphabetic, alphanumeric or
national, `'a'` class alphanumeric or national (alphabetic out), `'b'` boolean, `'c'` CONCAT's
all-but-index/object/pointer, `'p'` the MAX/MIN-family negative list, `' '` no arguments) is one predicate among
several: the position may also carry `ArgPredicate`s — `MinWidth`/`ExactWidth` in character positions (§15.57.3 /
§15.97.3 / §15.78.3 r1's "at least one character position", §15.70.3 r1's and §15.96.3 r2's "one character",
decided only on a STATIC width), `DataItemOrLiteralOnly` (§15.37.3 r3's "an integer data item or integer literal",
narrower than type 6 — an expression or nested function is barred outright), `NotStrongGroup` (§15.59.3 /
§15.63.3 / §15.71.3 / §15.72.3 r1's "nor shall it be a strongly-typed group item"), `CharacterCategory` (the
CATEGORY-worded rows — NUMVAL §15.67.3, NUMVAL-F §15.69.3, NUMVAL-C §15.68.3 and TEST-NUMVAL-C §15.94.3 r1, ORD
§15.70.3 r1 — bar the three EDITED categories, which Table 2 folds into the class the kind admits; the CLASS-worded
TEST-NUMVAL, TEST-NUMVAL-F, LOCALE-DATE and LOCALE-TIME carry none — `IntrinsicArgumentClassDriftTests` reads each
clause's wording from the specification), and the zero-length-LITERAL
clause — and the schema a `CrossArgRule` (`AllSameClass` for MAX/MIN/ORD-MAX/ORD-MIN r2/r3, `MatchArgument1` for
FIND-STRING, NUMVAL-C/TEST-NUMVAL-C, INTEGER-OF-FORMATTED-DATE, SECONDS-FROM-FORMATTED-TIME, TEST-FORMATTED-DATETIME,
TRIM, SUBSTITUTE).

**The class lattice is FINER than §8.5.2.1 Table 2's class column, and the difference is projected in exactly one
place** (kb/Work PB305). `CobolClass` carries one member finer than the class it belongs to —
`NumericEditedDeEditing`, category numeric-edited, whose CLASS is alphanumeric — kept apart so a CATEGORY-worded
rule can see the category Table 2 folds away. §15.68.3 is why both readings are needed of one operand: r1 is
category-worded ("Argument-1 shall be of category alphanumeric or national") and r2 is class-worded ("shall be of
the same class as argument-1"), and §8.5.2.1's closing sentence ("refers to the category unless class is
specifically indicated") is what keeps them apart. `IntrinsicArgumentRules.TableTwoClass` is the ONE reader of
"what class is this operand, for a rule that says CLASS"; `ByClass(...)` derives every CLASS-worded `Admissible`
arm from it (`'n'`, `'i'`, `'b'`, `'s'`, `'a'`, `'c'` — so a refined member is admitted by its class, never by
being remembered in the list). There is NO category-worded kind (kb/Work PB658): the hand-listed category set `'t'`
admitted the edited items its own clauses exclude while a bespoke binder arm rejected them for NUMVAL-C alone, so a
category-worded position is the derived class set plus the `CharacterCategory` predicate, screened in one place.
**Integer positions are derived from §15.6 Table 21** (kb/Work PB1420): `IntrinsicArgumentClassDriftTests` reads
every operand position the table types `Int` and nothing else and demands kind `'i'`, which is what makes §8.4.3.2.3
SR11 complete (it found RANDOM's seed and COMBINED-DATETIME's argument-1). **SR12** — an integer function other than
the integer form of ABS shall not stand where an unsigned integer is required — is
`IntrinsicArgumentRules.UnsignedIntegerViolation`, asked at the two intrinsic positions that say "unsigned integer":
CONCAT §15.18.3 r3 and BASECONVERT §15.12.3 r1 below base 11; the legal ABS form renders through the numeric-
admitting string-argument visitor as its digits. **A strongly-typed group is an alphanumeric argument** (§15.3 item
2: "treated as though they were of class and category alphanumeric, unless they are prohibited") — `ClassOfItem`
answers Alphanumeric for it on purpose — and its pointer/object leaves read as the D-SLOT placeholder image through
`OperandText.AsTransferString` (the one-way reader DISPLAY and a MOVE sender also use; kb/Work PB1901). `CrossBlock` composes the projection with the cross rules'
own alphabetic|alphanumeric merge — **projecting FIRST and merging SECOND**, because the projection is a class
fact and the merge is a rule-level exception §15.59.3 r2 states outright ("with the exception that mixing of
arguments of alphabetic and alphanumeric classes is allowed"). ⚠ EVERY cross clause in the catalogue is
class-worded, which is why `CrossViolation` needs no per-rule axis flag. The order was the defect: a one-case fold
(Alphabetic → Alphanumeric) applied to the UNPROJECTED member left numeric-edited in a block of its own, and all
twelve cross-rule functions rejected `FUNCTION MAX(<PIC ZZ9 item> "A")` and its siblings — legal source, on rules
eleven of whose inventory rows read CONFORMS. `IntrinsicArgumentClassDriftTests` holds both halves: no cross screen
may SPLIT a Table-2 class, and every class-worded kind's admissible set stays closed under its class.
`ClassOfCategory` maps category numeric-edited to the refined member unconditionally; §8.4.3.3.4 GR6 c)'s national
half is unreachable while `PIC ZZ9 USAGE NATIONAL` is refused at COBOLNET0899 (kb/Work PB646), whose landing owns
the usage-keyed arm in `ClassOfItem` beside `Usage.Index`'s.

`IntegerViolation` decides §15.3 type 6 — "an arithmetic expression that will always result in an
integer value **or** an integer data item" — for four operand shapes: a DATA ITEM, a nested numeric function
(§8.4.3.2.3 SR11), a numeric LITERAL, and the three provably-not-always-integral EXPRESSION shapes (a bare-item/literal
quotient at the root; a non-integral item appearing purely as additive leaves with an uncancelled net
coefficient; and — kb/Work PB617 — an additive spine whose literal-only terms fold EXACTLY to a non-integer constant
while every other term is provably integral, so `CHAR(1.5 + 1)` and `CHAR(W-I + 0.5)` are refused on VALUE and
`CHAR(1.5 + 0.5)` is admitted; the fold is the algebraic value, never an arithmetic mode's rounded intermediate);
every other expression fails open, because "always results in an integer value" is not generally
decidable (§4.2.2 discretion). The item test is `PicInfo.IsIntegerDescription` — **the ONE primitive**, and both of
type 6's disqualifying shapes live in it: a declared SCALE > 0, and a FLOATING-POINT usage (kb/Work PB248 — a float
item is PICTURE-less, so it carries scale 0 and a scale-only test admitted it; §14.6.8.3 sets its content to "the
algebraic value of the sending operand", so its declared value set contains non-integers and no reference to it is
provably integral). `IntrinsicResultType.IsIntegerOperand`, the §15.2 type-5 classifier that also answers every
statement rule saying "shall be an integer" (§14.9.28.3 SR2), reads the SAME primitive as its complement over the
scale, so neither screen can grow an arm the other lacks. The literal test is on the literal's VALUE, through
`NumericLiteral.TryParseExact` — the one exact parser — so the fixed-point form (§8.3.3.3.2) and the floating form
(§8.3.3.3.3, whose GR5 value is the significand times ten to the exponent) are judged alike: `1.0` and `1.0E2` are
admitted, `1.5` and `2.02402299E7` are not. ⚠ A floating-point operand at an integer position is REJECTED under
strict and accepted with a COBOLNET1627 WARNING under `--permissive` (the documented coercion extension,
`FloatIntegerArgumentPermissiveTests`). Rules a class kind cannot
carry at all live beside the schema on the same axis (`StaticUsageOf` — CONCAT's §15.18.3 r2/r3 usage halves in
`CheckConcatArgs`, BASECONVERT's and CONVERT's usage screens) or in the function's own binder (SUBSTITUTE's per-pair
§15.87.3 r3 widths — ONE method, `CheckSubstituteZeroLengthArgument2`, called from BOTH of `BindSubstitute`'s
exits and advancing a pair-SLOT counter by each operand's STATIC element count, so an enumeration in the list
stops the walk only where the run-time count genuinely makes the pair role unknowable rather than switching the
whole rule off (kb/Work PB247) —, INTEGER-OF-FORMATTED-DATE's "shall be a DATA ITEM" half of §15.48.3 r3, the date/time FORMAT
literal arm — which admits `ALL "…"` per §8.3.3.6.3 SR1). Every catalogued function has a row in `Verified` or a
cited reason in `DeliberatelyUnscreened` (`IntrinsicArgumentClassDriftTests.EveryCataloguedFunction_HasARow`), so a
function can no longer be silently unscreened; the screen is fail-open by construction (an undecidable operand is
never rejected) and its leniency is `--permissive`'s. Pinned by `pb58_argument_predicates_legal` (the shapes that
must NOT be rejected) and the `pb58-*` negatives (one per rule instance).

### FUNCTION LENGTH (§15.50) and BYTE-LENGTH (§15.14): one fold per function, its arms enumerated from the RULE BRANCHES, and ONE builder for a group whose length is a runtime value (PB24 → PB43 → PB61).

`IntrinsicBinder.BindLengthFamily` serves BOTH functions (`argument-1 [PHYSICAL]` — §15.50.2 / §15.14.2 — the
keyword consumed by name; a bare word that names a level-1 TYPEDEF is a type-name argument, §15.50.3 r1 /
§15.14.3 r1, resolved from `DataBinder.TypeDecls` because a typedef lives off the data-name namespace) and hands the
operand to `BindLengthFold` / `BindByteLengthFold`, whose arms are the returned-value rules in order, never a list of
shapes that happened to be met: a LITERAL folds to its own length (a zero-length literal is ZERO — §8.5.4 — no
`Math.Max(1, …)`; national is 2 bytes/position under BYTE-LENGTH; the literal-class rules DIFFER — §15.50.3 r1
admits a boolean literal, §15.14.3 r1 does not); a REF-MOD view rides the runtime string/storage channel (§8.4.3.3.4
GR6 makes it a data item); a group whose length is a RUNTIME value — `HasRuntimeLength`: an OCCURS DEPENDING table
(§15.50.4 r4b / §15.14.4 r2b), a dynamic-length elementary item or dynamic-capacity table (r7 / r6, the §8.5.1.12.1
variable-length group) anywhere beneath it, in ANY combination — goes to the ONE `VariableLengthGroupSum`; an ANY
LENGTH or DYNAMIC LENGTH elementary item is its current length (r6 makes it a BYTE count, so a national
dynamic-length item reads the storage channel — `BoundIntrinsicCall.LengthInBytes`); an elementary BOOLEAN item is
its boolean positions (r1 — a USAGE BIT item OCCUPIES ceil(n/8) bytes, D19); and every FIXED item is
`ItemLength.Positions` — the ONE fixed-item fold, which §13.10.4 GR6 `CONSTANT AS LENGTH OF` also reads (kb/Work PB1213) — (r2 national positions for an elementary usage-national item; r3 "alphanumeric character
positions" for everything else — an alphanumeric group, a DISPLAY/COMP/PACKED leaf, an INDEX/POINTER/COMP-1/COMP-2
carrier — which under the 1-byte-per-position model IS `DataItem.ByteWidth`, the BYTE-LENGTH authority; the pinned
per-usage widths are on `ByteWidth`'s doc and CONFORMANCE.md). A type-name argument with an ODO subordinate takes
"the rules of the OCCURS clause for a receiving data item" (r4a → §13.18.38.4 GR8b, the MAXIMUM — the width the
template already holds); a bit group / national group (GROUP-USAGE, §13.18.29) is not yet modelled — kb/Work PB79 —
so every group is r3's alphanumeric group here.

**A FUNCTION-IDENTIFIER argument is ONE arm for both folds, keyed on the function's §15.2 type (`FunctionResultLength`,
kb/Work PB1398).** §8.4.3.2.1 — "A function-identifier references the unique data item that results from the
evaluation of a function" — makes every function result "a data item of any class or category" (§15.50.3 r1 / §15.14.3
r1), so the arm is the item the TYPE describes, never a list of the three string types the LENGTH twin once matched
(and BYTE-LENGTH did not match at all, so every function argument was refused as "a numeric literal"): an alphanumeric,
national or boolean result keeps a RUNTIME length over its string image (BYTE-LENGTH: 1 byte per alphanumeric position,
2 per national position — D-N1 — and ceil(positions / 8) per boolean, §15.14.4 r4, the renderer's `ByteLength` arm over
`ByteLengthNational` / `ByteLengthBoolean`); an integer or numeric result is the §15.4 temporary
(`SendingValueTemp.NumericReturnedValuePic` — the ONE description the statement hoist stores the value in, so the item a
value is stored in and the item its length is asked of cannot differ), S9(30) DISPLAY, 30 positions and 30 bytes
(DOC-A.1-92); an index result is the index carrier, 8. A result the binder FOLDED to a literal (`FUNCTION
LENGTH(FUNCTION LENGTH(X))`) is not a literal the source wrote: `BoundNumLiteral.FunctionValue` /
`BoundNumericLiteral.FunctionValue` carry that origin from the ONE point every bound function result passes
(`BindIntrinsicCore`) to every syntax rule that asks "is this operand a literal" (kb/Work PB1662). What the catch-all
refuses is named for what was WRITTEN — `WrittenForm`: a numeric literal, or an arithmetic expression.

**`VariableLengthGroupSum` is one expression, in BYTES, from ONE width walk corrected for what it cannot know:**
`ByteWidth` counts an ODO table at its MAXIMUM, a dynamic-capacity table as ONE occurrence and a dynamic-length leaf
as zero, so the builder subtracts the first two and adds the runtime term for each — the ODO table's current extent
as a `BoundOdoExtent` (data-name-1's value clamped to [integer-1, integer-2] with EC-BOUND-ODO outside, §13.18.38.4
GR7 — the SAME `CobolTable.OdoExtent` the group's sending image slices with, so the two extents cannot disagree),
each dynamic-length leaf's current byte length, and each dynamic-capacity table's `Capacity × element bytes` read
through a `CapacityRegisterPlace` (the register is now minted for EVERY dynamic table and NAMED only under
`CAPACITY IN` — `DataBinder.DynamicResolve`). Every subordinate's place is DERIVED from the group's own access path
(a `MemberSegment` per level), so a subscripted or nested group sums correctly. The one shape it will not sum is
NAMED, never miscounted: a runtime-length item INSIDE a table element (a per-occurrence loop; §15.50.4 r7c's "based
on their current capacity" defines only the fixed-element case), and a bit-bearing group (its `ByteWidth` is the
§8.5.1.6.3 layout, not a sum) — both a loud stage with the shape in the message. An ODO group inside a BASED entry
or a REDEFINES-class record is never wrapped as an `OdoGroupPlace` by the resolver, so its r4 extent (and its GR8
sending slice) is not applied — kb/Work PB80. PHYSICAL (§15.50.4 r8 / §15.14.4 r7) is accepted on both functions
and transparent under WiseOwl COBOL's determination that a group is physically located where it is defined
(CONFORMANCE.md). Pinned by `pb61_length_byte_length_rule_branches` (one probe per rule branch, values derived in
its header), `pb24_length_*`, `v59_length_agrees`, and the `pb61-*` negatives.

**§8.4.3.2.3 SR6 is decided ONCE, from the function's DEFINITION, and BEFORE any argument binds** (`IntrinsicBinder.
DefinitionPermitsArguments` / `Sr6ArgumentListError`, PB61 row SR-8.4.3.2.3-6): "if a function's definition permits
arguments and a left parenthesis immediately follows … the left parenthesis is always treated as the left
parenthesis of that function's arguments" — so `NAME (start:length)` on an argument-permitting intrinsic OR a
REPOSITORY user function with USING formals (SR6 names function-prototype-name-1 too; §12.3.8.2 GR12 gives the user
function precedence) is an ARGUMENT LIST holding a non-argument (SR8), reported COBOLNET1543 on every route: the
FUNCTION-keyword form (`FUNCTION UPPER-CASE (1:4)` — the FNARG_LPAREN belongs to the refModPart, so the argument list
is EMPTY and binding first reported the §15.3 arity error), the reserved-name keyword-omitted form (RANDOM/SIGN/SUM),
and `KeywordOmittedFunction` (`UPPER-CASE (1:4)` under FUNCTION ALL INTRINSIC — it fell through the MinArgs guard to
the data path and died "not defined"). `ResultRefMod` no longer carries an SR6 arm — a ref-mod that reaches it
FOLLOWS an argument list or sits on a zero-argument function (`CURRENT-DATE (1:8)`, the standard's own D.14.3.6
shape), and it applies §8.4.3.3.3 SR2 (class) only. `FunctionRefModSr6SweepTests` sweeps the CATALOG: every
argument-permitting row in both forms draws 1543 (never 1504/1639), every zero-argument row does not.

### The class of a function's RESULT is asked of ONE classifier — `IntrinsicResultType.OperandCategory` (PB59 → PB68).

§15.2 gives every function a type (alphanumeric / boolean / integer / national / numeric), and a function-identifier
references a temporary data item of that class and category (§8.4.3.2.4 GR1) — so a `BoundComputedOperand` wrapping
a `BoundIntrinsicCall` carries the call's `ResultCategory`, and `IntrinsicResultType.OperandCategory` is TOTAL over
the operand kinds (literals, fields, ref-mod views per §8.4.3.3.4 GR6, groups, ALL literals, boolean expressions,
computed operands). **Every checkpoint that needs an operand's class asks it; none re-derives** — the lesson of PB68,
where four sites each kept a local class switch and none had learned the computed boolean operand: the relation
checkpoint (`StatementValidation.CheckRelationalOperands` — `IF FUNCTION BOOLEAN-OF-INTEGER(544, 6) = B"100000"`
was rejected as a class mix while the illegal alphanumeric mirror was accepted), the boolean-expression operand
(`ConditionBinder.BindBoolOperandValue` — §8.8.2's "an identifier referencing a boolean data item" now binds a
`BoundBoolCall`, rendered through the string channel), the LENGTH fold and `IsStringOperand` (a boolean function's
'0'/'1' image is a string operand), the condition renderer's `StringCategoryOf`/`BoolRead` (two boolean function
results compared each other take §8.8.4.2.8's right-zero-extension), and §8.8.1.1's arithmetic screen, which now
fires at BIND for a string-class function operand (`ExpressionBinder.BindIdentifierOperand` — it compiled clean and
threw at run time; `--permissive` decodes the digits exactly as for a data item, the DA6 gate). That entry is the ONE
path for both non-data-name identifier formats of §8.4.3.1.2 — the function-identifier (Format 1) and
its Format-4 twin, the inline method invocation — from BOTH the expression spine (`BindPrimary`) and the operand-wrapper walk (`BindOperandExprCore`), so every arithmetic
verb's written operand — not only COMPUTE's — is screened (kb/Work PB1142); its screening half
(`ScreenIdentifierOperand`) also serves the keyword-omitted spelling (§8.4.3.2.3 SR2), which parses as a data
reference and reaches it from `RefExpr`. A SOLE function or invocation stays a
legal relation / EVALUATE comparand through the comparand binders' short-circuits. Pinned by
`pb68_boolean_function_operand_contexts`, the `pb68-*` negatives, `pb1142-multiply-alphanumeric-function` and
`ArithmeticIdentifierOperandTests`.

### The ALL subscript in an argument (§15.3): ONE enumerating operand, three ranges, admissible only where the format repeats an argument (PB62).

§15.3: "When the definition of a function permits an argument to be repeated a variable number of times, a table
may be referenced by … subscripting where one or more of the subscripts is the word ALL … the effect is as if
each table element associated with that subscript position were specified", left to right, the rightmost ALL
varying fastest — and the range of an ALL is a fixed OCCURS count, "the object of the OCCURS DEPENDING ON clause",
or "from 1 to the current capacity of the table" for a dynamic-capacity table. Two of the three ranges are RUNTIME
values, so the former bind-time expansion (N `BoundFieldOperand`s by the OCCURS count; an ODO table staged loud, a
dynamic table not even a level) was the wrong model. **`IntrinsicBinder.TryBindAllArgument` binds the argument as
one `BoundFieldOperand` whose place is a `TableAllPlace(Element, IndexVar, Counts)`** — a `PlaceDecorator` over
the ELEMENT place (its ALL subscripts written as `__allN[k]`, its fixed subscripts rendered) carrying one
`AllCount` per ALL level (`Fixed(occurs)` / `Odo(depending, min, max)` / `Capacity(register)` — a nested dynamic
table's register path carries the outer index variables, so each outer occurrence's own capacity is read). Being a
decorator, every static classifier (class, category, usage, width, the §15.3 screen, MAX/MIN's type resolution)
sees the element as it would a single subscripted reference; only the intrinsic ARGUMENT-LIST renderers expand it
— `IntrinsicRenderer.ArgArray` turns any list holding an ALL into ONE `T[]` expression (runs of written operands as
array literals, each ALL a `CobolTable.AllArgs<T>(counts, __allN => element)` enumeration, joined by
`CobolTable.ArgConcat`), which every `params T[]` body binds to; `AlignedArgsEx`/`RawArgPairs`/`DecArgList`/
`StrArgList` and `RenderFloat`'s list ride it, PRESENT-VALUE's leading rate goes through `LeadThenTail` (a leading
argument with its OWN intake — PRESENT-VALUE's screened discount base — is rendered, when that argument is itself an
ALL, by `AllArgs`'s optional `lead` lambda for the FIRST element the walk enumerates, which is argument-1 by §15.3
rule 14's left-to-right order and which only the walk can name, since an inner ALL level may range over nothing
for the first outer occurrence; kb/Work PB1000 — it used to screen the enumerated binary64 `[0]`), and MEAN's
divisor becomes the enumerated list's `.Length` under a `CobolTable.With` binding when a range is a runtime value.
A single-value read of a `TableAllPlace` (`PlaceRenderer.Read`) is an internal error by construction. **The
capacity register is minted for every dynamic table** (named only under `CAPACITY IN`) so an unnamed table's
range is readable (PB61 first needed this for LENGTH r7c). **Admissibility is decided FIRST, from the
DEFINITION:** `IntrinsicSig.RepeatsAnArgument` (`MaxArgs` unbounded — `IntrinsicRepeatedArgumentDriftTests` pins
it to the ellipsis in every §15.x.2 general format, both ways; CONVERT/FIND-STRING are Variadic through phrase
words and repeat nothing) — on any other function the ALL is COBOLNET1645 at any cardinality (`MOD(E(ALL) B)`
bound over a one-occurrence table before), and the arity gate counts the arguments as WRITTEN, an ALL as its
elements when its ranges are fixed and as the one argument §15.3 guarantees otherwise. "The evaluation of an ALL
subscript shall result in at least one argument, otherwise the result … is undefined" — WiseOwl COBOL defines the
undefined case as EC-ARGUMENT-FUNCTION (set when checking is on) and terminates the reference with that name
either way (`CobolTable.AllArgs`), never handing an empty list to a body. ⛔ **AND EVERY VARIADIC NUMERIC BODY
ASSERTS THAT INVARIANT AT ITS OWN ENTRY, through the one `CobolIntrinsics.RequireArguments` (kb/Work PB257),
delegating bodies included.** Depending on `AllArgs` alone is what let the three carriers DISAGREE about the
case — `CobolIntrinsics.Variance` answered a silent `0` where the standard defines no value, `VarianceDec` had
no guard and indexed `xs[0]`, and the Int128 exact carrier's `MaxScaled`/`OrdMax`/`MedianScaled` did the same —
for the same ten statistical functions, three times over, while the arity gate that was cited as making it
unreachable does NOT hold it (`TableAllPlace.StaticCount` is null for an ODO or DYNAMIC ALL level, so
`FUNCTION VARIANCE(TBL(ALL))` passes arity with `given == 1` whatever the run-time count is). The rule is
UNIFORM with no exemption list — a forwarding exemption is a list, and the assertion is one integer compare —
and `EmptyArgumentListDriftTests` holds all three halves: the banned SHAPE (a body testing its argument array
for emptiness and answering anyway), the watchdog biting on the verbatim pre-PB257 lines, and the positive
sweep over every `params double[]` / `CobolDec[]` / `Int128[]` declaration in the folder, asserting its own
population. A variadic STRING list is deliberately out of scope: §15.18 CONCAT and §15.96 TRIM have
returned-value rules defining their own empty cases, and the string extremes reach the invariant through their
one selection helper `ExtremeIndex`. TRIM's repeated argument-2 binds through
its own per-function binder (not `BindIntrinsicArgs`), so an ALL there is today the ordinary subscript path's
verdict — recorded, not hidden. **TRIM's argument-2 also carries a SCHEMA TAIL as of kb/Work PB249:** §15.96.2
repeats argument-2 and §15.96.3 r2 is one sentence about "argument-2", so the `Verified` row declares
`tail: 's'` with the same `ExactWidth(1)` the declared position has, and the cross-argument agreement reaches
the repeats through `ArgSchema.MatchedPositions`' tail arm. Where the width is a run-time fact (an ANY LENGTH
or DYNAMIC LENGTH item, a computed-length ref-mod) the predicate fails open by design and r2 becomes a VALUE
constraint answered at the body under §15.3 rule 14 — which is also where r3's *syntactic* antecedent ("when no
argument-2 is specified") is decided, on the emptiness of the emitted `chars` array and on nothing else.
**SUBSTITUTE's repeated PAIRS take the ALL as of kb/Work PB81 (2026-08-18):** an
enumeration among the pairs makes the pairing a RUN-TIME fact, so `BindSubstitute` switches to the FLAT form —
`Args` = [argument-1, part₁, part₂, …] where a part is a written operand or a `TableAllPlace`, `SubstituteModes`
one flag PER PART (the keywords that preceded it, which attach to the pair the part's FIRST element opens — the
keywords precede argument-2), `SubstituteFlat = true` — and `IntrinsicRenderer.RenderSubstitute` emits
`CobolIntrinsics.SubstituteFlat(source, string[][] parts, int[] partFlags)`, each part its singleton or its
`AllArgs` enumeration. The runtime pairs the elements in order, takes each pair's mode from its argument-2
element, and decides the §15.87.2 shapes only a count can decide (an odd element count, a keyword on an
argument-3 element, FIRST with LAST) as EC-ARGUMENT-FUNCTION with the zero-length default, then runs the ONE
`Substitute` kernel. Written-only calls keep the bind-time pairing byte-for-byte (`SubstituteModes` per pair).
The staged `substitute-all-subscript-argument` descriptor is retired. Golden `pb81_substitute_table_all`. ### Under a STANDARD arithmetic mode, EVERY CROSS-ALIGNING ARM evaluates on the SDIDI — and that set is DERIVED (PB62, PB252).

§15.4.1 r1 leaves no latitude under STANDARD-DECIMAL / STANDARD-BINARY ("the returned value shall equal the value of the equivalent arithmetic expression") and §8.8.1.5.2 r1 converts **each operand** to the SDIDI individually, so a standard mode never forms a common scale at all. The NATIVE arms align first, on the Int128 carrier, and **alignment multiplies**: a 31-digit integer beside a scale-18 operand needs 49 digits, so `NumericRenderer.Align`'s per-argument escape raises EC-SIZE-OVERFLOW where the SDIDI holds the answer exactly (`MEAN(10³⁰, 2.0)` = 500000000000000000000000000001; `REM(10³¹−2, 1.5)` = 0.5). **"Does this arm cross-align?" is therefore the exact test for "must it be on the Dec lane under a standard mode"** — the routing is a property of the switch, not a taste.

⛔ It was nevertheless built one arm at a time, and each pass believed itself complete: PB62 moved the summing family (SUM/RANGE/MEAN/MEDIAN/MIDRANGE) for MEAN's witness; PB252 found **MOD and REM still on the Int128 lane** — the same clause, the same mechanism, the arm nobody swept. The set is now two NAMED collections instead of an inline `is … or …` chain: `IntrinsicRenderer.CrossAlignedNativeArms` (MOD · REM · SUM · RANGE · MEAN · MEDIAN · MIDRANGE — the structural half) and `StandardValueFixedByRule` (SQRT · FACTORIAL · the four inexact-EAE financial/statistical functions · the NUMVAL family — each for its own cited reason). `ExactCarrierBoundaryDriftTests.EveryCrossAligningNativeArm_IsRoutedToTheSdidiUnderAStandardMode` reads the native switch, collects the case labels of every arm calling `AlignedArgs`/`AlignedArgsEx` or `NumericRenderer.Align(x, s)`, and fails if one is missing — so a new aligning arm is routed automatically or the build goes red. MAX/MIN/ORD-MAX/ORD-MIN stay OUT by the complementary test: `RawArgPairs` rescales each argument to its OWN scale, so they form no common scale (PB65).

Pinned by `pb62_all_subscript_runtime_ranges` (every range and shape, values derived in its header),
`pb62_standard_decimal_summing_family`, `pb252_mod_rem_standard_decimal` (REM's exact 0.5 across the 49-digit alignment, plus the §15.64.4 NOTE sign table on the SDIDI carrier), and the `pb62-all-subscript-*` negatives.

### A SELECTION function delivers the selected argument's CONTENT from its own carrier (PB65, RV-15.59.4-1 D2).

MAX / MIN / ORD-MAX / ORD-MIN — and MEDIAN over an odd count (§15.61.4 r1's equivalent expression is
`(argument-a)`, the middle argument itself) — return "the content of the argument" compared "according to the rules
for simple conditions" (§15.59.4 r1 / §15.63.4 r1 / §15.61.4 r3); no equivalent ARITHMETIC expression is involved,
so §15.4.1's native latitude over the representation of the returned value never reaches the VALUE. A MIXED
argument list — a float beside a fixed-point item, conforming under §8.5.2.1 Table 2 (both class numeric) — used to
route the WHOLE call through binary64 (`AnyRealArgument` → `RenderFloat` → `MaxReal(params double[])` →
`FromDouble`): `MAX(F1 N1)` with N1 = 999999999999999999 returned **13** (1e18 → FromDouble at scale 9 → the
modular store), `MAX(D9 F1)` corrupted the last digit of a 9(9)V9(9), `MEDIAN(F1 N1 N2)` returned 0. **The rule
now:** a mixed selection list evaluates on the SDIDI carrier under NATIVE arithmetic too (`RenderNum` routes it to
the `RenderDec` bodies `MaxDec` / `MinDec` / `OrdMaxDec` / `OrdMinDec` / `MedianDec`) — a 38-digit fixed argument
is carried exactly, a float through the §8.8.1.5.1 conversion, the compare is exact, and the result lands ONCE at
the receiver. An ALL-float list keeps the float lane (each argument's content IS its double); an all-fixed list
keeps the exact Int128 selection (`MaxAt` / `MinAt`, PB65's D1). The arithmetic statistical family (SUM / MEAN /
RANGE / MIDRANGE — genuine equivalent arithmetic expressions) keeps the D16 native float lane a float operand
selects. Golden `pb65_selection_mixed_carriers`.

### The binary64 lane is entered on the ARGUMENT RUN, not on one argument (PB635).

`RenderFloat`'s body converts **every** argument through one binary64 conversion, so the lane is only enterable
when every argument HAS a binary64 value. §15.3 says which positions do: type 6 ("An arithmetic expression that
will always result in an integer value or an integer data item shall be specified") and type 10 ("Numeric. An
arithmetic expression or a numeric data item shall be specified"). Types 1/2/9, 3, 5, 11 and 13 name character,
bit and reference operands and have none. The dispatch nevertheless asked only `AnyRealArgument` — "is ANY
argument floating" — and for the one catalogued function that mixes the two, that moved the whole call:
`FIND-STRING` takes two string operands (§15.37.3 r1/r2) and an integer argument-3 (r3), so
`FIND-STRING(H ND START AFTER <COMP-2>)` converted `H` and `ND` to `double`, dropped LAST/ANYCASE, and named a
`FindStringReal` body that does not exist — **Roslyn CS0117 on the generated C#**, an internal failure escaping at
the wrong stage. It should not exist: §15.37.4's returned value is a character POSITION with no equivalent
arithmetic expression, so §15.4.1's "implementor-defined approximation of the value of that expression" — the
whole licence the lane rests on — is not granted for it.

**The rule now:** `IntrinsicArgumentRules.ArgumentRunIsAllNumeric` derives the precondition from the SPEC-VERIFIED
`Verified` schema (`Admissible(kind) is [Numeric]`, i.e. §15.3 types 6 and 10) and **fails open** — an unscreened
function, an undeclared position and the `'p'` negative-list kind all answer *yes*, so it can only ever take a
call OUT of the lane on the authority of a cited §15.x.3 rule. ⛔ It is a question about the POSITION and
deliberately not about the operand's class: under `--permissive` the DA6 leniency admits an alphanumeric operand
at a numeric position and digit-decodes it, so `MOD(<PIC X item> <COMP-2>)` must keep the lane, and reading the
operand would have reopened PB2's CS1503. The `Factorial` exclusion is a separate, CODOMAIN question — 33! is
~8.7e36, which no `FromDouble` tail can carry — and is now the declared set `IntrinsicRenderer.FloatLaneExempt`
rather than an inline `!= "Factorial"`.

The defect was reachable only under `--permissive` (PB248's §15.3 type-6 screen rejects a floating-point item at
an integer position under strict, COBOLNET1627), and the sweep measured **ten** functions crashing that way, not
one: FIND-STRING, ORD, INTEGER-OF-BOOLEAN, NUMVAL-F, TEST-NUMVAL, TEST-NUMVAL-C, TEST-NUMVAL-F,
INTEGER-OF-FORMATTED-DATE, SECONDS-FROM-FORMATTED-TIME and TEST-FORMATTED-DATETIME. Pinned by
`IntrinsicFloatLaneArgumentRunDriftTests` (the population is derived from the schema, and the probe is the emitted
C#), by `FloatIntegerArgumentPermissiveTests.Pb635FindString_UnderPermissive_KeepsItsOwnLaneAndItsPhrases` (the
four LAST × ANYCASE combinations over one float argument-3, plus the fixed-point control), and by the negative
`pb635-find-string-float-skip`.

### An exact-intake float body takes an exact argument UNNARROWED, through the one domain screen (PB999, PB1041).

§15.4.1 licenses the RETURNED value of the binary64 family as an implementor-defined approximation; it does not
license answering for a different ARGUMENT. The argument is the exact value a scaled item or an SDIDI holds, and a
body that narrows it to binary64 first is wrong wherever the function AMPLIFIES the narrowing error: past binary64's
RANGE (an SDIDI — a floating-point literal, a product past the Int128 window, a float-decimal item — reaches
10^±6144, §8.8.1.5.2), and wherever the body is ILL-CONDITIONED, its condition number |x·f′(x)/f(x)| unbounded on the
domain. Measured before the fix: `FUNCTION LOG10(1.0E-200 * 1.0E-200)` answered log10(+0.0) = −∞ → 0 where §15.56.4
r1 requires −400; SIN(10^400) computed sin(+∞) = NaN and RAISED EC-ARGUMENT-FUNCTION on an argument §15.82.3
restricts only to class numeric; SIN(3.14159265358979323) answered 1.22·10^−16 (the sine of π's binary64) where the
value is 8.46·10^−18; TAN(1.570796326794896619231321691639) answered 1.6·10^16 for 1.33·10^30; LOG(1 + 10^−26)
answered 0; ACOS(1 − 10^−26) answered 0 for 1.414·10^−13; EXP(700.1234567890123456789) was 150 ulps off.

The declared set `IntrinsicRenderer.WholeRangeBodies` names the bodies that take an exact argument, each with the
carriers it takes that way (`ExactIntake`): `Sqrt` takes an SDIDI only (`Sdidi` — its condition number is ½, so only
past binary64's RANGE is the substitute wrong); `Sin`, `Cos`, `Tan`, `Log`, `Log10`, `Acos`, `Asin`, `Exp`, `Exp10`
take every exact operand (`EveryExact` — ill-conditioned somewhere, so a scaled operand is lifted exactly by
`CobolDec.From` too). For such an operand `FloatBody`'s default arm (`LeadArg` → `ExactArg`) passes the `CobolDec`
itself to the runtime's `CobolDec` overload, which forms ON THE EXACT CARRIER the quantity the function is well
conditioned in and narrows only that:

- **SIN / COS / TAN** — the quadrant q and residue r of |x| = q·π/2 + r, r ∈ [−π/4, π/4]
  (`CobolIntrinsics.ReduceQuarterTurns`, a decimal Payne–Hanek reduction on the Int128 fixed point: |x|/2π ≡ Sig·g
  (mod 1) with g = frac(10^Exp/2π), read as nine base-10^18 limbs — 162 places — of the constant digit string
  `InverseTwoPiDigits`, 6320 places of 1/(2π), enough for the SDIDI's top place 10^6144 plus the reduction,
  re-derived by `InverseTwoPiDigitsDriftTests`; no arbitrary-precision type, COBOLNET_DESIGN §1.2 invariant 2). The
  reduction's error is under 2·10^−124 turns, some 80 digits below the smallest residue a 34-digit decimal is
  expected to reach, and r reaches the body as a double-double, so sin / cos / ±tan / −cot of it carries binary64's
  relative precision. |x| ≤ π/4 is the double body unchanged.
- **LOG / LOG10** — ln(1 + u) of the exact u = x − 1 within ½ of 1 (Kahan's log1p formulation); ln Sig + Exp·ln 10
  off the normal binary64 range.
- **ACOS / ASIN** — 2·asin(√((1 − |x|)/2)) over the exact 1 − |x| past |x| = ½ (π minus it, or π/2 minus it, with the
  constants as double-doubles).
- **EXP / EXP10** — e^i · e^f over the integer part i and the exact fraction f = x − i, for 1 ≤ |x| ≤ 1000 (400 for
  EXP10; beyond, the result is outside binary64 whatever the digits — the size error condition, below).
- **SQRT** — the root on the exact carrier (`CobolDec.Sqrt`) off the normal binary64 range.

Not members, each because its condition number is bounded by a constant on the whole domain, so the substitute's
result IS the approximation: ATAN (≤ 1), and SQRT for a scaled operand. ⛔ The domain members (LOG, LOG10, SQRT,
ACOS, ASIN) are still screened by the ONE §15.3 rule 14 screen (PB952): their operand is
`CobolIntrinsics.DomainDecAdmitted`, `DomainDec`'s own predicate (`AdmitsDec`) returning the admitted carrier —
`null` for a rejected argument, which the body turns into the NaN every rejected argument becomes — so no body
carries a domain test. A float operand IS its binary64 and keeps the double body, as a scaled operand of a `Sdidi`
member does. `WholeRangeBodiesDriftTests` pins each member's runtime overload and every arm's value against an
independent 140-digit oracle. Pinned by `tests/conformance/2002/pb999_log_trig_decimal_range`,
`tests/conformance/85/pb1041_trig_log_arc_exact_argument` and `tests/conformance/2002/pb1041_exp_and_wide_exact_argument`,
and the rate-side twin (PRESENT-VALUE's exact discount base, above) by
`tests/conformance/85/pb1000_present_value_rate_near_minus_one`. ANNUITY's twin is a BODY fix, not an intake one
(PB1310): its §15.9.4 1) b) denominator 1 − (1 + rate)^−n is evaluated as −expm1(−n·log1p(rate))
(`CobolIntrinsics.ExpMinusOne` / `LogOnePlus`, Kahan's formulations), because forming 1 + rate in binary64 is exactly
1.0 for any rate below 2^−53 — a COMP-2 rate included — and ANNUITY(1E-17 12) stored 0 for 1/12.

**The VARIADIC arm: an all-exact argument LIST is not narrowed before it cancels (PB1565).** The same licence reads
the same way for a list: VARIANCE (§15.98.4 r1 — deviations from FUNCTION MEAN), STANDARD-DEVIATION (§15.86.4 r1 —
SQRT of that) and PRESENT-VALUE (§15.74.4 r1 — a Σ of signed amounts) SUBTRACT their arguments from one another, so
narrowing each exact argument first cancels its low digits away: VARIANCE(100000000000000001 100000000000000002)
answered 0 for 0.25, STANDARD-DEVIATION 0 for 0.5, PRESENT-VALUE(0 100000000000000001 −100000000000000000) 0 for 1.
The declared set `IntrinsicRenderer.ExactListBodies` names them; for a list with NO floating argument `FloatBody`
passes every argument lifted exactly (`DecArgList` / `DecOf`, table(ALL) enumerations included) to the runtime's
`(CobolRounding mode, …CobolDec…)` overload, which evaluates the EAE on the SDIDI with the function's ONE Dec body
(`VarianceDec` / `StdDevDec` / `PresentValueDec`, in the native intermediate rounding mode) and narrows only the
result. PRESENT-VALUE's §15.74.3 r2 is then decided where every SDIDI evaluation of it decides it —
`PresentValueDec`'s exact compare through the one `PresentValueDomain` raise site — which is also what lets a
leading table(ALL) rate stay one `CobolDec[]` enumeration. A list with a floating argument keeps the binary64
body (its EAE is evaluated in binary64, CONFORMANCE.md DOC-A.1-92); an SDIDI argument was already routed to the
Dec bodies by `RenderNum`'s native Dec arm (PB69). `WholeRangeBodiesDriftTests` requires every variadic Float
catalog row to be a member and pins each overload. Pinned by `tests/conformance/85/pb1565_variadic_exact_argument_list`.

**The RANGE of the returned value: past binary64 is the size error condition (PB1566).** Under native arithmetic the
family's returned value IS a binary64 (CONFORMANCE.md DOC-A.1-92), so binary64's range is the range of the native
intermediate it is formed in, and §14.7.5 case 5 — checked, DOC-A.1-179 — makes a value outside it the size error
condition: EC-SIZE-OVERFLOW when "farther from zero", EC-SIZE-UNDERFLOW when a nonzero value is "nearer to zero than
is allowed for the intermediate data item" (the no-phrase rule 3). `CobolIntrinsics.InBinary64Range` is the one
check, raising `CobolSizeError` exactly as FACTORIAL does past the Int128 intermediate: EXP / EXP10 on every carrier
(e^x is never zero, so a finite argument's zero result is an underflow; a subnormal is in range; a non-finite
ARGUMENT keeps its DOC-A.1-70 disposition), and every `ExactListBodies` result through `NarrowResult`, which knows
the exact value. EXP(1000) used to print `Infinity` and `COMPUTE F = FUNCTION EXP(-1000) ON SIZE ERROR` took NOT ON
SIZE ERROR. Under a standard mode the SDIDI's own range answers the same way (§8.8.1.5.2 r2). Pinned by
`tests/conformance/2002/pb1566_exp_result_range`.

### An intrinsic-function-name the REPOSITORY identifies is not a user-defined word (PB65, §8.3.2.1 rule 5).

§8.3.2.1 rule 5: intrinsic-function-names may be user-defined words "except for … intrinsic function names
identified in a function-specifier in the REPOSITORY paragraph" — the prohibition that makes §8.4.3.2.3 SR2's
FUNCTION-less reference unambiguous. The REPOSITORY sets (`RepositoryIntrinsics` / `RepositoryAllIntrinsic`) were
filled and consulted by NOTHING at declaration time; `KeywordOmittedFunction` substituted a hand-written "a declared
data item wins" precedence, so under `FUNCTION HIGHEST-ALGEBRAIC INTRINSIC` a table named HIGHEST-ALGEBRAIC compiled
clean and `HIGHEST-ALGEBRAIC(A1)` silently read the table element where §15.43.4 requires +999. **The rule now:**
`DataBinder.DeclareUserWord(word, UserWordKind)` is THE declaration funnel of a user-defined word, and the screen
lives there — **COBOLNET1649** at the declaration; a catalogued name the REPOSITORY does NOT identify stays a legal
user-defined word (SQRT as a table, MOD as an item), and only for those does the data item win the FUNCTION-less
spelling. Golden `pb65_repository_r5_intrinsic_names`, negatives `pb65-repository-*`.

**Every type, one membership (kb/Work PB1083, §12.3.8.3 SR12/SR13, §12.3.8.4 GR14).** The funnel is called for
every §8.3.2.2 type — data-, record-, constant-, type-, condition-, index-, file-, report-, screen- and record-key-
names, the SPECIAL-NAMES words (alphabet-, class-, locale-, mnemonic-, ordering-, dynamic-length-structure-names,
symbolic-characters, switch condition-names), paragraph- and section-names, the unit's own program-name /
user-function-name / prototype-name (a contained program under its container's REPOSITORY), the REPOSITORY's own
specifier words (declared after the paragraph, so a specifier written before `FUNCTION ALL INTRINSIC` is caught),
and the OO words (object-class-, interface-, method- and property-names); `UserWordDeclarationDriftTests` parses
§8.3.2.2's list and fails on a type neither declared nor exempted with its reason. The membership is ONE function,
`DataBinder.IsRepositoryIntrinsic`, asked by the screen, `IntrinsicBinder.KeywordOmittedFunction` (and its
reserved-name arm) and `ReferenceResolver.IsFunctionBearing`; its intrinsic half,
`DataBinder.TryIntrinsicOfThisCompilation`, is also what `KeywordOmittedFunction` asks for "is this word an intrinsic
at all", and a named specifier is stored by its CANONICAL name, so every spelling of one function answers alike. The
word goes through `CobolWordsMap.Resolve` (GR14 —
an UNDEFINE'd or SUBSTITUTE'd-away name leaves the ALL list, a SUBSTITUTE literal-5 or EQUATE literal-2 joins it)
and its function's D8 window (`IntrinsicSig.IsDefinedAt` — Annex E.2 item 13's seven names are prohibited under ALL
only from 2023). Golden `2023/pb1083_repository_all_cobol_words`; negatives `pb1083-*`.

**The same funnel is the one-type-per-word census (kb/Work PB990, §8.3.2.2).** "*Within a source element, a given
user-defined word may be used as only one type of user-defined word*" is asked at `DeclareUserWord` too
(`CheckOneTypePerWord`): the binder keeps `word → the type it was first declared as` for ITS source element (one
`DataBinder` is one element; a name a containing element passes down is inherited, not declared, and a contained
element may re-declare it, §8.4.6.1), and a later declaration of another type is **COBOLNET2692** unless
`UserWordKinds.MayBeOneWord` allows it — the standard's three exceptions as DATA (a compilation-variable-name; a
level-number beside a paragraph- or section-name; the constant-name / data-name / property-name / record-key-name /
record-name group), which `UserWordDeclarationDriftTests.TheSharingRelation_IsRebuiltFromTheStandardsExceptions`
rebuilds from the text of `specs/ISO_COBOL.md`. Two declarations that are not declarations of a name the source
wrote do NOT reach it: a REPOSITORY specifier naming the unit itself is ignored (§12.3.8.3 SR11 / SR15), and a
declarative section's paragraph-name-OMITTED paragraph is registered anonymously (§14.4.3) rather than under the
section's own name. Goldens `85/pb990_user_word_types_one_per_word`, negatives `pb990-*`.

### A boolean EXPRESSION is an intrinsic argument (PB65, FMT-15.45.2 / §8.4.3.2.3 SR8).

`functionArgument` gained its `booleanExpression` alternative behind the ARGUMENT-scoped `boolArgAhead()` predicate
(the condition-scoped `boolExprAhead()` would have read a B-AND FOLLOWING the call —
`COMPUTE BR = FUNCTION BOOLEAN-OF-INTEGER(5, 8) B-AND BB` — as belonging to the numeric argument 5; the argument
scan stops at a depth-0 comma or the argument list's `)`). The operand is a `BoundBoolOperand` of class boolean
(`IntrinsicArgumentRules.ClassOf1`), imaged as its '0'/'1' string through the ONE `BooleanRenderer` (intercepted at
`OperandText.AsString`'s entry, which has the per-unit renderer). The lexer's boolean literal admits ZERO length
(`[01]*` — §8.3.3.4.4 GR4: B"" is a boolean literal; INTEGER-OF-BOOLEAN of it is 0). A BIT GROUP argument waits on
GROUP-USAGE (kb/Work PB79). Golden `pb65_boolean_expression_argument`.

### The §7.3.17 LEAP-SECOND directive reaches the date/time family (PB65, AR-15.79.3-4).

`>>LEAP-SECOND ON` was consumed and discarded. Now `LeapSecondDirectiveProcessor` (a line-count-preserving stage
like TURN / REF-MOD-ZERO-LENGTH) resolves the group's state → `Frontend.LeapSecondOn` → the ONE `DirectiveResults`
record `Bind` takes → `BindSession` / `DataBinder.LeapSecond` → the renderer's trailing `leapSecond: true` argument
(`LeapSecondFlag` — emitted only when ON, so every OFF emission is byte-identical) to SECONDS-FROM-FORMATTED-TIME,
TEST-FORMATTED-DATETIME, INTEGER-OF-FORMATTED-DATE, FORMATTED-TIME, FORMATTED-DATETIME and COMBINED-DATETIME. Under
ON: a seconds subfield may be 60 (§15.3.3.3 — `Analyze`'s field range), standard numeric time form is bounded at
86,401 (§7.3.17.4 GR4 — `SecondsOutOfStandardForm`), and 86,400.x is presented as 23:59:60.x. The REPORTED side
(GR2/GR4's "may a 60 / a value ≥ 86,400 be returned") stays the implementor's "never" (A.1 items 111 and 112 — the .NET clock
has no leap seconds). SR1 (not within a compilation unit) is COBOLNET1650 — the one obligation only that stage can
see; the OPERAND (§7.3.17.2's ON/OFF, with ON un-underlined so a bare directive selects it) is checked with every
other directive's, as COBOLNET1911 off the row's `directiveOperand` column (kb/Work PB794), and below 2002 the
directive is the introduction gate (construct `leap-second-directive-2002`). Goldens `pb65_leap_second_on` / `_off`.
**The floor is a fact about the exact value, on every carrier (PB1379).** The screen compares the LANDED (unscaled, scale)
pair, so a float or SDIDI seconds argument lands through `CobolDate.SecondsOfReal` / `SecondsOfDec`, which round toward
NEGATIVE infinity (a COMP-2 `-1.0E-10` lands negative, NaN lands out of the form, an SDIDI past the carrier saturates by sign
instead of raising a size error) — it was truncated to 0 first and formatted as midnight while the fixed-point and
COMBINED-DATETIME arms refused it. `CobolDate.TimeOfDay` is the ONE reading of whole seconds as hh:mm:ss shared by
FORMATTED-TIME, FORMATTED-DATETIME and LOCALE-TIME-FROM-SECONDS (86,400 under ON is 23:59:60 in all three — LOCALE-TIME-FROM-SECONDS
said 24:00:00, §15.54.4 r2). Goldens `pb1379_time_form_floor_float_carriers_off` / `_on`.

### The §15.3 type-6 integer-argument intake asks the VALUE'S integrality on every carrier (PB1526, PB638).

`RuntimeApi.IntegerArgOf` is the ONE family of landings behind `IntrinsicRenderer.AsInt` (bounded, `long`) and `AsIntWide`
(total, `Int128`), keyed on `NumX.Carrier` (scaled, SDIDI, binary64; the unsigned-wide lane funnels through
`NumericRenderer.DeU` first — the bounded intake had no arm for it and a COMP-5 PIC 9(19+) operand reached Roslyn as a
CS1503). Each landing asks `CobolNum.HasFraction` / `CobolDec.HasFraction` / the binary64 truncation test of the EXACT value
and raises EC-ARGUMENT-FUNCTION (§15.3's closing paragraph) before the truncating narrowing; `IntrinsicArgumentRules`'s
compile-time screen stays fail-open (it can only refuse the provably non-integral), so `FACTORIAL(X / 2 + 1)` with X = 5 is a
run-time condition. `IntrinsicCarrierAgreementDriftTests.EveryIntegerIntake_IsTotalOverTheNumXCarriers` walks `NumXCarrier`.
Goldens `pb1526_integer_argument_has_integral_value`, `pb638_unsigned_wide_item_as_integer_argument`.

## Edge cases

- FUNCTION LENGTH / BYTE-LENGTH of a reference-modified operand x(s:l) is a RUNTIME value over the view (§8.4.3.3.4 GR6 — the substring's own length; x(s:) ends where the item does); only fixed operands fold.
- FUNCTION LENGTH / BYTE-LENGTH of a group with an OCCURS DEPENDING table, dynamic-length items or dynamic-capacity tables beneath it is `VariableLengthGroupSum` (above) — the CURRENT extent/lengths/capacities, never the maximum; a type-name with an ODO subordinate IS the maximum (§15.50.4 r4a).
- MOVE ZERO to a numeric receiver = 0L; MOVE ZERO to an alphanumeric receiver = a '0'-filled string of the receiver width — same token, different materialization (must branch on receiver category).
- ALL literal repeat-to-width-then-truncate (§8.3.3.6): ALL "AB" into a PIC X(5) = "ABABA" (repeat until ≥ width, truncate from the right), applied before any JUSTIFIED.
- NUMVAL (§15.67), NUMVAL-C (§15.68), NUMVAL-F (§15.69) parse the spec-defined argument-1 content grammars. NUMVAL admits an optional leading sign (`+`/`-`) OR a trailing sign / `CR` / `DB`, a run of digits with an optional decimal separator; leading and trailing spaces are ignored, and embedded spaces are ignored only where they precede the first digit; a `CR`, `DB`, or minus sign makes the result negative (§15.67.3/§15.67.4). NUMVAL-C additionally admits a currency string and comma grouping separators — the currency string is argument-2 when supplied, else the SPECIAL-NAMES CURRENCY SIGN or the default currency sign (§15.68.3 r3) — and the returned value ignores the currency string and any grouping separators preceding the decimal separator (§15.68.4 r2). NUMVAL-F additionally admits an `E±n` exponent (one-to-four-digit exponent, §15.69.3). **A space never SPLITS a digit run in the NUMVAL-F form — neither the significand's nor the exponent's `n`** (kb/Work PB256): §15.69.3 r5's "ignored" means a space contributes nothing where r1's figure admits one, not that the runs on either side join, so an interior space ends the run and §15.95.4 r1 b) 1 reports the first non-space character following it (`TEST-NUMVAL-F("1E+1 2345")` = 6, not 9). The grounds — r1's figure, rendered from the printed page, drawing `n` as one contiguous metavariable, and §15.95.4 r1 b) 1's unqualified "a string of numeric characters" against sub-notes 2/3/4's explicit "of the significand" — are recorded as a determination in CONFORMANCE.md §3, because the two readings give different answers. The period is the decimal separator; under DECIMAL-POINT IS COMMA the comma is the decimal separator (and, for NUMVAL-C, the period becomes the grouping separator). Under native arithmetic the total number of digits shall not exceed 31 (§15.67.3 r3 / §15.68.3 r6 / §15.69.3 r2). Each family's TEST- validator and value function are projections of ONE positional scan (`NvScan` / `NvfScan`, PB60) — the value path never pre-normalizes. **Every decision the scan takes as a PARAMETER carries a matching coverage obligation** (kb/Work PB256): `commaMode`, `digitCap` and `anycase` are computed in the compiler (`IntrinsicRenderer.CommaFlag` / `DigitCapFlag` / `AnycaseFlag`, over `ctx.Data.DecimalPointIsComma` and `ArithmeticModes.NumvalDigitCap`), and a unit test that passes one as a literal exercises the runtime scan and nothing left of it — so each (function, flag) pair needs a conformance golden compiling that function under the source construct that turns the flag ON, and preferably as a MIRROR PAIR, since a dead arm passes either half alone. `IntrinsicModeFlagCoverageDriftTests` derives the pairs from the renderer's own arms crossed with the catalog and fails until such a golden exists, so a new flag-taking arm cannot land unmeasured. Under the STANDARD modes the value is the scan lifted EXACTLY to the SDIDI at the parsed scale (`NumvalDec/NumvalCDec/NumvalFDec`, §15.4.1 + §15.67.4 r1 / §15.68.4 r1 / §15.69.4 r3); under NATIVE arithmetic NUMVAL/NUMVAL-C carry the item-92 working scale, and NUMVAL-F — whose native value §15.69.4 r2 makes an approximation — follows the float family: binary64 (`NumvalFDouble`) in a receiver-less or float-receiver context, the exact Int128 parse for a fixed arithmetic receiver or a MOVE sender (`ReceiverContext.MoveSender`). CONFORMANCE.md item 92 records the determination.
- BASECONVERT (§15.12) and CONVERT's HEX source (§15.19.3 r4) trim ONLY the trailing fixed-width image pad, and only the SPACE character: a LEADING space is content and reaches the digit screen (EC-ARGUMENT-FUNCTION), and a DIGITLESS argument (all spaces) raises rather than fabricating "0"/empty — the guard counts digits consumed, never the accumulator value (PB59). BASECONVERT's digits are `0-9` + the UPPERCASE `A-F` only (§15.12.3 r2 names "the basic-letters A to F"; §8.1.3.1 Table 1 makes basic letters the capitals — §8.1.3.2 GR3a's case-insensitivity governs source text, not runtime data); CONVERT's HEX source deliberately admits both cases (§15.19.3 r4 says "hexadecimal digits", the term the hex-literal format defines over both). CONVERT's §15.19.3 r1 zero-length screen has a RUNTIME arm at the top of `Convert` (ISO 8.5.4's runtime zero-length shapes — a bind screen only sees the literal). CONVERT's source-format keys what argument-1's STORAGE must hold, and the static half of each rule is its REPRESENTATION, screened at bind through the ONE shared `IntrinsicArgumentRules.StaticUsageOf` (the usage axis class cannot express — Table 2 collapses the pointer usages, and class NUMERIC spans display digits and binary words): r4 = display or national usage, r5 = display (its NOTE — "distinct from simply requiring the string to be of class alphanumeric" — admits numeric and edited DISPLAY items), r6 = national, r7 = any usage EXCEPT index/message-tag/object-reference/pointer/function-pointer/program-pointer; the VALUE halves (digit validity, coded-set membership) are the runtime screens' territory, and §15.12.3 r1's display-or-national rule reuses the same axis when BASECONVERT's screen set lands (PB59 family 5a). CONVERT's ANY source reads the item's RAW STORAGE bytes (§15.19.3 r7 — "it is not necessary for the contents to be valid according to the usage") through the ONE storage channel `OperandText.AsStorageImage` (char==byte; each leaf shape delivers its documented representation — CONFORMANCE.md items 205/208/209/215, D-N1 UTF-16BE via `CobolBits.NatBytes`, packed bits via `CobolBits.Pack`; float/COMP-5 leaves have no defined image and stage loud), and the §15.19.4 r2/r4 HEX legs hex THE SOURCE'S OWN BITS padded trailing with zero bits to a whole destination character — r2's 8-bit pad is materialized by the packed BIT image itself, r4's 16-bit pad is the odd-byte zero append (`"A" ANUM NAT HEX` = 4100; the destination keyword picks only the digit repertoire, never a re-encoding). Returned values are capped at the documented §15.4 maximum, 8,191 character positions (CONFORMANCE.md item 93), raising EC-ARGUMENT-FUNCTION past it.
- **THE SUBSTITUTED RESULT OF A REJECTED FUNCTION REFERENCE HAS EXACTLY THREE MECHANISMS, AND A GUARD PICKS ONE — it never writes its own** (kb/Work PB383, PB470). §15.3 rule 14 (an argument rule violated) and §15.4 (the returned value longer than the implementor's maximum) both hand the result to the implementor when checking is off, and CONFORMANCE.md rows `DOC-A.1-90` / `DOC-A.1-93` state the determination once. The NUMERIC half is `ExceptionState.ArgumentError`'s own `return 0`; the TEXT half is the pair `ExceptionState.ArgumentErrorZeroLength` and `ExceptionState.ArgumentErrorSpaces(detail, positions)`, each raising through `ArgumentError` (one fatal path, one checking gate) and each returning ITS class value from the one holder `CobolNet.Runtime.Exceptions.ArgumentSubstitute` — which a `bool` screening predicate's caller reads directly, having already raised (`CobolDate.SecondsOutOfStandardForm`, `CobolDate.OffsetOutOfRange`), so no site anywhere spells a literal. `ArgumentSubstituteDriftTests` enforces that structurally over `src/Cobol.Net.Runtime/Intrinsics`: a returned string literal of nothing but spaces is, in that folder, always one of the two classes and never a computed result. All three make the raise and its substitute ONE expression, which is the whole point: while they were two statements, `BooleanOfInteger` answered the one-position boolean `"0"` where its row-93 sibling `BaseConvert` answered the zero-length value, for the same determination, silently and for a user's program. **A guard is not free to choose** — the choice belongs to the STANDARD (§15.87.4 r1 states "the returned value is of zero length" outright) or to the ROW, and the call site cites which: row 90 gives `CHAR`/`CHAR-NATIONAL` **one space** because their length is fixed by the function rather than by the rejected argument, gives a boolean result the **zero value of the returned item** (`BOOLEAN-OF-INTEGER` with a bad argument-1 but a valid argument-2 — argument-2 zero positions), and gives the **zero-length value** wherever the returned LENGTH derives from the rejected argument (`FORMATTED-DATE`/`-DATETIME`/`-TIME`, `BASECONVERT`, and `BOOLEAN-OF-INTEGER` when argument-2 itself is rejected — §15.13.4 r1 makes argument-2 the length, so nothing survives). The `LOCALE-DATE` / `LOCALE-TIME` / `LOCALE-TIME-FROM-SECONDS` family is the third case the row now names (PB470): §15.52.4 / §15.53.4 / §15.54.4 r3 make the returned length depend on the LOCALE, which the rejection of argument-1 leaves intact, so the zero-length class does not reach it and the general "spaces" clause does — **one** space, on §15.30.3 r1's own answer for an alphanumeric function whose length is content-derived and whose content is absent, since L10's culture patterns fix no width once the value is gone. The four guards cite `CobolLocale.LocaleSubstitutePositions`, the one place that number is decided. ⛔ The difference is invisible through a MOVE (§14.6.8.5 space-fills the receiver from a zero-length sender), which is how the wrong answer survived a year behind a green golden; every witness for this determination therefore reads the reference DIRECTLY — a bracketed DISPLAY (§14.9.11.4 GR1) beside a `FUNCTION LENGTH` read-out (§15.50.4 r3), in `2014/pb470_locale_argument_substitute` and its 2002 twin.
- Out-of-domain math (ACOS/ASIN of |x|>1, SQRT of negative, LOG of ≤0) → EC-ARGUMENT-FUNCTION with a defined default result (legacy returns 0) rather than a .NET exception. ⛔ **AN ARGUMENT-DOMAIN RULE IS DECIDED ON THE EXACT OPERAND, BEFORE THE binary64 CONVERSION — never inferred from a NaN, and never tested on the double (kb/Work PB246, then PB952).** §15.3 rule 14 is about the ARGUMENT's value, and the double a body computes on is not that value: a correctly-rounded conversion moves a value ACROSS a bound (`1.000000000000000001` in a `PIC 9V9(18)` item becomes 1.0, so `ASIN` answered π/2 under armed checking and raised nothing; a legal PRESENT-VALUE rate `-0.99999999999999999999` becomes the illegal −1.0; an SDIDI's `CobolDec.ToDouble` underflows a tiny negative to `−0.0`, which a `x < 0` test admits). So the seven §15.x.3 "The value of argument-1 shall be …" rules of the binary64 family — ACOS/ASIN §15.8.3/§15.10.3 r2 [−1, +1], SQRT §15.84.3 r2 and ANNUITY §15.9.3 r2 [0, ∞), LOG/LOG10 §15.55.3/§15.56.3 r2 (0, ∞), PRESENT-VALUE §15.74.3 r2 (−1, ∞) — are ONE catalog column (`IntrinsicSig.Domain` + `DomainRule`), applied by ONE renderer seam (`IntrinsicRenderer.DomainArg` → `RuntimeApi.DomainArg`) through ONE runtime screen per carrier (`CobolIntrinsics.DomainScaled` on the exact scaled integer, `DomainDec` on the SDIDI, `DomainReal` on a value that is already a binary64; a whole-range body's exact operand takes `DomainDecAdmitted`, the same predicate returning the admitted carrier instead of its binary64 — PB999). ⛔ **The table(ALL) lead is argument-1 too:** PRESENT-VALUE's rate as the first element of an ALL is screened on the element's exact carrier inside the enumeration (`CobolTable.AllArgs`'s `lead` lambda), never on the enumerated binary64 (kb/Work PB1000). ⛔ **And the screen is not the only reader that must see the exact value:** PRESENT-VALUE's EAE (§15.74.4 r1) divides by `1 + argument-1`, which CANCELS at the r2 bound, so the body takes the DISCOUNT BASE rather than the rate — `CobolIntrinsics.PresentValueBase` forms `1 + rate` on the exact carrier (`CobolDec.Add` after `DomainDecAdmitted`; a float rate's own binary64 is exact there by Sterbenz) and only then narrows. From the narrowed rate the legal −0.99999999999999999999 made a base of 0 and a value of ±∞ where §15.4.1 asks for an approximation of 100·10^20. A rejected argument raises and becomes NaN, so the body's NaN reaches the one §15.3 default in `RealResult`/`FromDouble`. The bodies carry NO domain test of their own, and `IntrinsicArgumentDomainDriftTests` derives the column from the standard's rule text, so a row with such a rule cannot omit it. (The Dec twins — `SqrtDec`, `AnnuityDec`, `PresentValueDec` — test their CobolDec argument exactly and are unaffected.) `Sqrt` carries `Math.Abs` on the result, because §15.84.4 r4 makes the native returned value **the absolute value** of the approximation and IEC 60559 mandates `sqrt(−0) = −0` exactly so that sqrt is *not* the absolute value there — `−0.0` is a legal argument-1 that a COMP-1/COMP-2 item reaches by ordinary underflow, and the unquantized float channel renders it WITH its sign. §15.86.4 r1's expression, `(FUNCTION SQRT (FUNCTION VARIANCE (argument-list)))`, is composed from those two FUNCTION bodies in both carriers rather than re-decided through a bare `Math.Sqrt` / `CobolDec.Sqrt`, so STANDARD-DEVIATION inherits both rules.
- FACTORIAL(n) for n≥21 overflows long and n large overflows Int128 → size-error / EC-ARGUMENT-FUNCTION (legacy clamped at 28! for decimal). The Int128 boundary: 33! ≈ 8.68e36 FITS (Int128.Max ≈ 1.70e38); **34! is the first overflow**; the runtime returns the EC default 0 for n > 33 or n < 0.
- RANDOM with vs without a seed argument — seeded form is deterministic per-call, unseeded shares one generator; the optional-trailing-arg arity must be modeled so the no-arg and one-arg forms both bind.
- FUNCTION WHEN-COMPILED (§15.99.3 r1) and CURRENT-DATE (§15.21.3) share the SAME 21-character structure: `YYYYMMDDHHMMSShh` in positions 1-16 followed by the UTC-offset subfield in positions 17-21 (position 17 = `+`/`−`/`0`, positions 18-19 = offset hours, positions 20-21 = offset minutes). WHEN-COMPILED carries the compilation timestamp, CURRENT-DATE the run-time clock; both are nondeterministic and both route through the injectable clock (the ONE `CobolDate.Format21` formatter), so conformance tests must inject a fixed clock.
- ADDRESS OF and SET … TO ADDRESS OF produce a ManagedPointer (managed ref), never a numeric address — and SET … TO NULL (the predefined address, an identifier — not a figurative constant; kb/Work PB1427) sets it to the null ManagedPointer, semantically distinct from LOW-VALUE. NULL is no intrinsic-function argument (§8.4.3.10.3 SR1 lists only the function-PROTOTYPE activation), so `FUNCTION LENGTH(NULL)` is COBOLNET2576.
- DAY-OF-WEEK ordinal: COBOL is 1=Monday..7=Sunday, .NET DayOfWeek is 0=Sunday — the (+6)%7+1 remap is easy to get wrong.
- HIGH-VALUE used in a comparison vs in a MOVE: as a MOVE source it fills the receiver width with U+FFFF; in a comparison it compares as the highest ordinal — both fall out of the U+FFFF mapping + ordinal Compare, in both classes (D5).
- INITIALIZE skips FILLER by default but INITIALIZE … REPLACING / WITH FILLER changes that; REDEFINES subordinates and items with the wrong category for the REPLACING clause are skipped (§14.9.20.4 GR5).
- Nested intrinsic calls as arguments (e.g. ACOS(FUNCTION ACOS(D/D))) — natural grammar recursion since P7 Step 12 (`functionCall` is a `primaryExpression` alternative inside the argument's `arithmeticExpression`).

## ISO citations

- ISO/IEC 1989:2023 §15 Intrinsic functions (the function library)
- §15.2 Types of functions — alphanumeric / boolean / national / numeric / integer / index (THE return-type classification, the spine of the binding model)
- §15.3 Arguments — number of arguments may be zero, one, more, or variable (the arity model)
- §15.3.1 Format arguments to international date and time functions (date/time intrinsic formats)
- §15.3 — table(ALL) subscript ("When ALL is specified as a subscript, the effect is as if each table element … were specified"): each occurrence passed as a separate argument
- §15.50 FUNCTION LENGTH — number of character positions in argument-1 (distinct from LENGTH OF byte size)
- §15.50.4 — FUNCTION LENGTH of a variable-length (ODO) group uses the current depending value
- §8.3.3.6 Figurative constant values — ZERO/SPACE/QUOTE/HIGH-VALUE/LOW-VALUE/ALL/symbolic, including the repeat-to-width-then-truncate rule and the highest/lowest-ordinal definition of HIGH/LOW-VALUE
- §8.4.2 — data category of a PICTURE (drives the result-category and figurative materialization)
- §14.9.20 INITIALIZE — REPLACING / TO VALUE / DEFAULT / FILLER handling
- §14.9.25 MOVE — alphanumeric/numeric move rules reused by intrinsic results and figurative stores
- §14.9.42 STOP RUN / run-unit termination — RETURN-CODE as exit code
- §8.8.1 — arithmetic operates on the algebraic VALUE of operands (intrinsic numeric results align scales through the same engine)

## Open questions (resolved in `COBOLNET_DESIGN.md` §18)

- Confirm the return-type discipline: should ANY intrinsic internal use decimal, or is long (exact)/double (float)/Int128 (wide) strictly sufficient? Recommendation: never decimal — exact-numeric via unscaled long+scale, float via double, wide via Int128. Owner already banned default decimal/BigInteger; confirming this extends to intrinsic INTERNALS.
- Confirm LENGTH OF / FUNCTION BYTE-LENGTH semantics under the byteless model: fold to a compile-time PIC+USAGE byte size kept in PicInfo, kept distinct from FUNCTION LENGTH (character positions). Is a synthesized compile-time byte-size table acceptable (vs declaring LENGTH OF unsupported until G6's byte boundary)?
- HIGH-VALUE/LOW-VALUE character mapping: RESOLVED by owner decision kb/Work R52 — U+FFFF/U+0000 in both classes; the byte 0xFF on a byte medium (D5).
- Scope confirmation: SCREEN SECTION, REPORT WRITER, and JSON/XML GENERATE/PARSE are designed only to the seam (registers reserved, deferred to their own subsystems). Confirm they are NOT part of this subsystem's graded deliverable.
- RETURN-CODE ownership: this design models it as a synthesized static long; the control-flow/CALL subsystem must write it on CALL RETURNING/GOBACK and the process exit code must read it. Confirm that cross-subsystem contract so two designs do not each create a RETURN-CODE field.
