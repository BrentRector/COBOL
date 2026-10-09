# DESIGN — Edition / Version-Correctness Framework ("N per-edition compilers")

> Status: DESIGN — **IMPLEMENTED by rearch PHASE 02.** The
> Open Questions (§6) are resolved-by-implementation except Q4 (the behavior-variant matrix), an explicit P3
> follow-on.
> Scope: the four-compilers-in-one machinery — construct registry, reserved words, gating
> (introduction / removal / obsolete / behavior-variant), `EditionContext` threading, the version
> test matrix, and the version-gating audit. Upholds the HARD INVARIANTS (typed-native, spec-first,
> battery-green-throughout, singular pattern, four editions in one).
> SSOT cross-refs: `docs/COBOLNET_DESIGN.md` §16 (G0–G8), `docs/VERSION_TEST_MATRIX_DESIGN.md`,
> `docs/VERSION_CHANGE_REFERENCE.md`.

> **Consumer of these primitives.** This doc owns the version-gating FRAMEWORK PRIMITIVES (`EditionInfo`,
> `IDiagnosticSink`, `ConstructRegistry`, `constructs.json`, `EditionSeverityPolicy`) — the single version table. Their
> sole consumer is ONE `VersionConformancePass` over the bound tree: parse is a SUPERSET (no edition predicates; each
> version-gated rule carries a committed-match construct-id annotation), bind is edition-AGNOSTIC (zero `Check` calls save the UDF exception; DESIGN-version-conformance-pipeline §1.1 is the complete exception ledger),
> the pass is the sole gate, and emit runs only on a clean tree. ⚠ **NO `.Syntax` back-ref on
> bound nodes** — the pass is TWO-ARM (bound-node type/attribute + a presence-based parse-tree arm for syntactic
> introduction/removal gates); see
> `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`.

## 1. The current problem (grounded in the AS-BUILT code)

The edition subsystem is *conceptually* already singular — `ConstructRegistry.Check` is the one
gating funnel and `EditionContext.Removed` is the one severity seam — but it is **physically
fragmented across three namespaces and two assemblies with no shared low layer**, which forces
duplication the drift tests only partially protect.

Assembly layering (verified): `Runtime` → `Frontend` → `Compiler` → `Cli`. **`Frontend` has no
project reference to `Compiler`.** The canonical registry lives *above* the frontend, so the
frontend physically cannot see it. Every duplication below is a symptom of that one fact.

| # | Problem | Evidence |
|---|---------|----------|
| P1 | **Registry invisible to the frontend** → the parse-layer re-encodes the same edition metadata. `EditionGateHints` (Frontend, `namespace legacy-root.Parsing`) hand-copies `(Display, IntroducedIn, Citation, RowId)` for ~30 constructs into `Gate` records that already exist as `ConstructDialectStatus` rows. | `src/Cobol.Net.Frontend/Parsing/EditionGateHints.cs:35-63`; the same rows in `Validation/ConstructDialectStatus.cs:60-168` |
| P2 | **Severity policy reimplemented twice more in the frontend.** `ReferenceFormatProcessor.EditionGates` and `CopyProcessor` each inline `if (permissive) ReportWarning else ReportError` instead of the one `EditionContext.Removed` seam. | `ReferenceFormatProcessor.cs:120-148`; `CopyProcessor` (COPY REPLACING gate) |
| P3 | **`EditionGateHints` reverse-engineers what the `{isXXXX()}?` gate already knew.** ~30 signatures pattern-match `(offending token, rule-stack, lookahead windows)` to recover the construct identity that the failing predicate discarded. Brittle (empirically derived), and a documented residue of un-mappable cases (`SET…TO objref`). The inline method invocation LEFT that residue with kb/Work PB428: it is no longer predicate-gated at all — it parses at every edition and the `VersionConformancePass` names 2002 on RECOGNITION, which the §8.7.4 `::` token makes unambiguous. | `EditionGateHints.cs:72-175` |
| P4 | **Two parallel mechanisms for removed / introduction gating** (singular-pattern violation). Most gates route through `ConstructRegistry.Check`, but a handful stay inline with pinned codes: `0816` END-ACCEPT (intro), `0803` ROUNDED MODE (intro), `0810` ALTER / `0811` bare GO TO / `0882` CALL ON OVERFLOW (removed). None are registry rows, so **none enter the version matrix**. | `StatementBinder.Accept.cs:45`, `StatementBinder.cs:1229`, `StatementBinder.AlterSwitches.cs:119,137`, `StatementBinder.Call.cs:176` |
| P5 | **`DialectLevel` is double-sourced.** `CobolParserCoreBase.DialectLevel` (Frontend, `{ get; set; } = 85`), `Frontend.DialectLevel` (init = 85), and `EditionContext.DialectLevel` (Compiler) are three independent stores set from `Options.DialectLevel` by hand; a fourth default (`Options` = 2023, CLI `--std` = 2023-or-85). Four defaults that must be kept equal by convention. | `CobolParserCoreBase.cs:17`, `Frontend.cs:45`, `EditionContext.cs:29`, `CompilerDriver.cs:38` |
| P6 | **`EditionContext` is an overloaded "edition" object that is really `EditionInfo + DiagnosticSink`.** It carries the immutable edition facts (DialectLevel/Permissive/MaxDigits) *and* two `public List<string>` accumulators (`Diagnostics`, `Warnings`) that are stringly-typed (`$"error {code}: {message}"`) and mutable-exposed. 290 call sites. The "edition" name mislabels a diagnostic sink. | `EditionContext.cs:37-56` |
| P7 | **Edition metadata lives in 3+ representations, drift-protected in only some.** `constructs.json` ↔ `ConstructRegistry.Entries` (drift-tested both ways ✓); `reserved-words.json` ↔ `ReservedWords.Table.cs` (drift-tested ✓); but `EditionGateHints` (P1), the frontend preprocessor gates (P2), and the inline gates (P4) are **not** bound to the registry, and `VERSION_CHANGE_REFERENCE.md` is a hand-maintained rendering that has gone **stale** (117 `TODO` rows; e.g. CALL ON OVERFLOW row shows TODO though `0882` is live). | drift tests present for 2 of ≥5 encodings; `docs/VERSION_CHANGE_REFERENCE.md` |
| P8 | **The version-gating track is unaudited.** The matrix harness (`VersionMatrixTests`) computes expected accept/reject from `constructs.json`, which is good — but VCR status is hand-ticked, not harness-driven, so it cannot be trusted as the gap list. Genuine holes exist (e.g. SYNCHRONIZED-on-group emits a generic `COBOL0001` under both 2002 and 2023, no gate). | critique `iso-pending` [HIGH]; `docs/VERSION_CHANGE_REFERENCE.md` |
| P9 | **`EditionValidator` hard-couples to generated grammar token ints + positional heuristics** (`CheckedTokenTypes` HashSet of `CobolLexer.*` constants, `IsProvableUserWordPosition` rule-shape switch). Correct but fragile against grammar edits, and it lives in `Validation` while consuming `Frontend` generated types. | `EditionValidator.cs:268-388` |

**What is already good and MUST be preserved:** the `Check`-is-the-one-funnel discipline; the
`constructs.json`/`reserved-words.json` drift tests; the `EditionContext.Removed` severity seam;
the `ConstructAvailability` verdict shape; the `pending`-row scheduling mechanism; the conservative
"only high-confidence reserved words reject" policy; the rich ISO §-citation on every row.

---

## 2. Target design

### 2.1 One new lowest-layer assembly: `Cobol.Net.Editions`

Create `src/Cobol.Net.Editions/` (namespace `CobolNet.Editions`), referenced by **both** `Frontend`
and `Compiler` (and transitively `Cli`). It depends only on the ANTLR runtime types it needs for the
parse-layer helper (or, preferably, nothing — see §2.5). This is the single fix for the root cause
(P1/P2/P7): the registry, reserved words, severity policy, and edition value now sit *below* the
frontend, so every layer consumes the *same* metadata and the *same* policy.

```
Cobol.Net.Editions           (NEW — bottom of the compiler stack)
  EditionInfo.cs             immutable edition value + capacity
  EditionSeverity.cs         Error | Warning
  EditionSeverityPolicy.cs   the ONE strict/permissive decision
  EditionDiagnostic.cs       structured diagnostic record
  IDiagnosticSink.cs         report channel (frontend + compiler both implement/consume)
  EditionCodes.cs            (moved) 0900-0903 band + pinned 08xx consts
  Constructs.g.cs            (GENERATED) const id per constructs.json row
  ConstructDialectStatus.cs  (moved) verdict record
  ConstructRegistry.g.cs     (GENERATED from constructs.json) Entries + Find + Check
  ConstructRegistry.cs       hand-written Check() gating funnel (partial)
  ReservedWordEntry.cs       (moved)
  ReservedWords.Table.g.cs   (GENERATED — as today)
  ReservedWords.cs           (moved) Find + ReservedWordSet
```

### 2.2 `EditionInfo` — the immutable edition value (replaces the edition half of `EditionContext`)

```csharp
namespace CobolNet.Editions;

/// The targeted ISO edition + strict/permissive axis. Immutable value — threaded, never a global.
public readonly record struct EditionInfo(int Year, bool Permissive = false)
{
    public static readonly EditionInfo Latest = new(2023);
    public static EditionInfo Of(int year, bool permissive = false) => new(Validate(year), permissive);

    /// Fixed-point digit capacity: 18 at '85, 31 at 2002+ (ISO §8.3.3.3.2 / §14.7 / §13.18.40).
    public int MaxDigits => Year < 2002 ? 18 : 31;

    public bool Has(int introducedIn) => Year >= introducedIn;
    private static int Validate(int y) => y is 85 or 2002 or 2014 or 2023 ? y
        : throw new ArgumentOutOfRangeException(nameof(y), y, "edition must be 85/2002/2014/2023");
}
```

`EditionInfo` is the **single source of `DialectLevel`** (fixes P5). It is constructed once in
`CompilerDriver.Compile` from `Options` and threaded: the parser's base class holds an `EditionInfo`
(read only by the two load-bearing forward-detect predicates, e.g. `Edition.Has(2002)` in the OPEN
clause's `{is2002() || retryPhraseAhead()}?`), the frontend preprocessor takes it, and the
compiler driver carries it into the `VersionConformancePass`. No component owns a second settable
`DialectLevel`.

### 2.3 `EditionSeverity` + `EditionSeverityPolicy` — the ONE severity decision

```csharp
public enum EditionSeverity { Error, Warning }

/// The single strict/permissive policy (fixes P2/P4). Every removed/reserved/obsolete emit site
/// asks HERE — never a local `if (permissive)`.
public static class EditionSeverityPolicy
{
    public static EditionSeverity For(ConstructAvailability verdict, EditionInfo ed) => verdict switch
    {
        ConstructAvailability.NotYetIntroduced => EditionSeverity.Error,   // both axes: nothing to run
        ConstructAvailability.Removed          => ed.Permissive ? EditionSeverity.Warning : EditionSeverity.Error,
        ConstructAvailability.Obsolete         => EditionSeverity.Warning, // always: still conforming
        _                                      => EditionSeverity.Warning,
    };
}
```

### 2.4 `IDiagnosticSink` + `EditionDiagnostic` — structured, not stringly-typed (fixes P6)

```csharp
public readonly record struct EditionDiagnostic(
    string Code,              // COBOLNET0900 … or a pinned 08xx
    EditionSeverity Severity,
    string ConstructId,       // the constructs.json row id (empty for non-registry diagnostics)
    string Message,
    string Where,             // "FD OUT-FILE", "paragraph P1" …
    string Citation,          // ISO § / VCR row
    SourceSpan? Location = null);

public interface IDiagnosticSink
{
    void Report(in EditionDiagnostic d);
}
```

`SourceSpan` is the frontend's existing location type (or a shared minimal `(path,line,col)` moved
into `Editions`/`Common`). Both the frontend `DiagnosticBag` and the compiler's diagnostic collector
implement/adapt `IDiagnosticSink`, so `ConstructRegistry.Check` writes the **same structured
diagnostic** from both layers. Rendering to text (`error COBOLNET0900: …`) happens once, at the CLI
boundary — the `List<string>` accumulators are deleted.

### 2.5 `ConstructRegistry.Check` — the one funnel, now sink-based and layer-neutral

```csharp
public static partial class ConstructRegistry
{
    // GENERATED half (ConstructRegistry.g.cs): Entries + _byId + Find.
    public static ConstructDialectStatus Require(string id) =>
        Find(id) ?? throw new ArgumentException($"unregistered construct id '{id}'", nameof(id));

    /// THE gating entry point. Layer-neutral: takes EditionInfo + a sink, so the FRONTEND can call it
    /// too (deleting EditionGateHints' metadata copy). `where` localizes the diagnostic.
    public static void Check(EditionInfo ed, IDiagnosticSink sink, string id, string where, SourceSpan? at = null)
    {
        var c = Require(id);
        var verdict = c.StatusAt(ed.Year);
        if (verdict == ConstructAvailability.Available) return;
        var sev = EditionSeverityPolicy.For(verdict, ed);
        var code = verdict switch
        {
            ConstructAvailability.NotYetIntroduced => c.RemovedIn is null ? c.DiagnosticCode : EditionCodes.Introduction,
            ConstructAvailability.Removed          => c.DiagnosticCode,
            _                                      => EditionCodes.ObsoleteFlag,
        };
        sink.Report(new EditionDiagnostic(code, sev, c.Id, MessageFor(c, verdict, ed), where, c.Citation, at));
    }
}
```

Note the `Check` body is *identical* to today's logic (`ConstructDialectStatus.cs:183-203`); only the
plumbing changes (EditionInfo + sink instead of `EditionContext`). This keeps behavior byte-stable
for the migration.

### 2.6 Generate the registry FROM `constructs.json` (fixes P7 — one source, auto-checked)

`constructs.json` becomes the **single source**. `ConstructRegistry.Entries`, the `Constructs.*` id
consts, and (unchanged) `ReservedWords.Table` are **generated**, not hand-maintained:

- Add a Roslyn **incremental source generator** `ConstructRegistryGenerator` in a new
  `src/Cobol.Net.Editions.SourceGen/` project. It reads `constructs.json` (registered as an
  `AdditionalFiles` item) and emits `ConstructRegistry.g.cs` (`Entries` list) and `Constructs.g.cs`
  (`public const string LabelRecordsRemoved2002 = "label-records-removed-2002";` … from the ids).
- The `Display`, `DiagnosticCode`, and `Citation` fields — today only in the C# — move **into
  `constructs.json`** (`display`, `expectDiagnostic`, `vcr`/`citation` already partly there) so the
  generator has everything. This deletes the hand-written 110-row `Entries` array.
- Magic-string ids at call sites become `Constructs.LabelRecordsRemoved2002` (compile-checked;
  greppable to one definition). `Check(ed, sink, Constructs.DeleteFile2023, where)`.
- The existing `ConstructRegistryDriftTests` becomes redundant for json↔registry (they are now the
  same source) but is **kept and repurposed** to assert the generator output is in sync with a
  committed snapshot, and that every `Constructs.*` const is referenced somewhere (no orphan rows).

If a source generator is judged too heavy (Open Question Q1), the fallback is a **pre-build MSBuild
target** running `scripts/gen-constructs.ps1` (mirroring `gen-reserved-words.ps1`) with the failed
regen failing the build — same "generated is a build output" discipline as the ANTLR parser.

### 2.7 Parse-layer: SUPERSET parse, no edition predicates (fixes P1/P3 — `EditionGateHints` deleted)

The grammar recognises the union of all editions. A version-gated rule never gates at parse time —
it *parses*, and the construct is diagnosed by the ONE `VersionConformancePass` over the bound tree
(`docs/rearchitecture/DESIGN-version-conformance-pipeline.md` §2):

- Each version-gated rule carries a **committed-match construct-id annotation** (a grammar action +
  side-table storage keyed by parse context). Identity flows *forward* from the rule that owns it, never *backward* from a
  token-window heuristic, and it is recorded only on a COMMITTED match — never inside speculative
  prediction.
- The only surviving predicates are the two **load-bearing forward-detects**, where a construct's
  token is a legal §8.9 user-defined word below its edition and ungating would change tokenization:
  the OPEN clause's `{is2002() || retryPhraseAhead()}?` and the `boolExprAhead()`-based
  boolean-condition ENTRY. They resolve lexical ambiguity only; the edition diagnostic still comes
  from the pass. Fail-safe: a missed detect degrades to a neutral parse error, never a wrong edition
  claim.
- The reverse-engineering signature table (`EditionGateHints` / `ReservedWordEditionHints`) does not
  exist in the end state: the reservation-word residue folds into bind-time
  `ConstructRegistry.Check` gating, and the vendor JSON/XML `COBOL0313` disposition relocates to
  `CobolErrorStrategy` as a token-keyed vendor hint (it is a parse-error re-diagnosis of
  hard-reserved tokens, not an ISO edition gate).

This is the singular design: every edition diagnostic has ONE origin — the `VersionConformancePass`
— which also removes the "duplicate diagnostic for an edition-gated construct" smell.

### 2.8 Fold the inline gates into the registry (fixes P4)

The five inline gates (`0816`, `0803`, `0810`, `0811`, `0882`) become `constructs.json` rows with
their **pinned codes kept** (`DiagnosticCode` field), and their call sites become
`ConstructRegistry.Check(ed, sink, Constructs.EndAccept2002, where)` etc. They then automatically
enter the version matrix (positive+negative fixtures required by the drift discipline). ALL `Check`
call sites funnel into the ONE `VersionConformancePass` over the bound tree; the binder itself is
edition-AGNOSTIC (zero `Check` calls save the UDF exception; the §1.1 ledger is the complete remainder) — the funnel runs after bind, before emit.

### 2.9 `EditionValidator` is absorbed by the `VersionConformancePass` (P9)

The raw-parse-tree removal gate + §8.9 reserved-word funnel live in the ONE `VersionConformancePass`,
which absorbs and replaces `EditionValidator`:

- The §8.9 reserved-word funnel moves into the pass; `CheckedTokenTypes` and
  `IsProvableUserWordPosition` remain grammar-coupled (legitimate for a syntax-facing check) but
  report through the pass's shared sink.
- The `_reservedWords`/`ReservedWordSet` seam is unchanged (already correct for the future
  `COBOL-WORDS` directive).
- All diagnostics go to the shared `IDiagnosticSink`; emit runs only on a clean tree.

### 2.10 The version-gating audit as a first-class workstream (fixes P7/P8)

- **Make the harness the single source of VCR status.** Generate `docs/VERSION_CHANGE_REFERENCE.md`
  from `constructs.json` + the harness result (a `scripts/gen-vcr.ps1` + a `--emit-status` mode of
  `VersionMatrixTests`), so a row's status is *derived* from a passing (construct × edition) fixture,
  never hand-ticked. A stale ledger becomes structurally impossible.
- **Every VCR row needs a positive + negative fixture** in `constructs.json` (the schema already
  supports `expectDiagnostic` / `expectDiagnosticBelow`); the audit backfills the missing
  85→2002 and 2002→2014 row sets the doc admits are absent, and adds the known holes (SYNCHRONIZED-
  on-group, etc.) as `pending` rows so they are catalogued and LOUD rather than a generic
  `COBOL0001`.
- **Behavior-variant gating** (rows that *change semantics* rather than accept/reject) gets an
  explicit fixture kind: a `variant` field on the construct row carrying the expected *output*
  difference per edition, asserted by a new `VersionBehaviorMatrixTests` running the program under
  each `--std` and diffing stdout. This is the currently-weakest leg of "four compilers in one".

### 2.11 The nonstandard-extension register (ISO §4.2.10; kb/Work PB1525)

`ConstructRegistry` answers "at which EDITION does an ISO construct exist". A nonstandard extension is the other
question — a construct ISO does not define at any edition — and §4.2.10 owes the user an answer in three parts:
documentation that identifies the extensions for which support is claimed (and the reserved words added for them), a
user-invocable compile-time warning mechanism, and that mechanism flagging only syntactically distinguishable
extensions. All three are read from ONE table, `NonstandardExtensionRegister` (`Cobol.Net.Editions`,
`NonstandardExtension.cs`): a row per extension (id, display, support, origin, standard form, distinguishable, usage
spellings, reserved words). It is hand-authored C# and not generated JSON because nothing outside the compiler and its
tests reads it; `docs/CONFORMANCE.md` §3.2 is the documentation half, held equal to it by
`NonstandardExtensionRegisterDriftTests`, which also joins it to `cobol-words.json` `extensionReserved`, to the
grammar's `usageKeyword` (a silent new vendor usage word fails) and to the binder sites. The warning is
`cobol --flag-extensions` → `CompilerDriver.Options.FlagExtensions` → `EditionContext.FlagExtensions` (the
`--sign-encoding` thread), emitted ONLY through `EditionContext.Extension` as COBOLNET2894. A usage spelling is found by
row in the one usage funnel; a statement or phrase is one `Extension(ExtensionIds.X, …)` call at the one place its
binder accepts it. Not rows: constructs `--permissive` accepts (a violation of a standard rule, flagged
unconditionally through `EditionContext.Removed`) and implementor latitude (`--sign-encoding`).

---

## 3. Current → target module changes

| Action | From | To | Why |
|--------|------|----|-----|
| create | — | `src/Cobol.Net.Editions/` (assembly + `CobolNet.Editions` ns) | The shared low layer both Frontend and Compiler reference; root-cause fix for P1/P2/P7. |
| create | — | `src/Cobol.Net.Editions.SourceGen/` (`ConstructRegistryGenerator`) | Generate registry + id consts from `constructs.json` (P7); single source, auto-checked. |
| split | `Binding/EditionContext.cs` | `Editions/EditionInfo.cs` (immutable value + `MaxDigits`/`CheckDigitCapacity`) **+** `Compiler`'s diagnostic collector implementing `IDiagnosticSink` | End the "edition = diagnostic sink" mislabel (P6); single DialectLevel source (P5). |
| create | — | `Editions/IDiagnosticSink.cs`, `Editions/EditionDiagnostic.cs`, `Editions/EditionSeverity.cs`, `Editions/EditionSeverityPolicy.cs` | Structured diagnostics + the ONE severity policy consumed by all three emit layers (P2/P4/P6). |
| move | `Validation/EditionCodes.cs` | `Editions/EditionCodes.cs` | Band constants must be visible to the frontend. |
| move + split | `Validation/ConstructDialectStatus.cs` (record + registry + `Check`) | `Editions/ConstructDialectStatus.cs` (verdict record), `Editions/ConstructRegistry.g.cs` (generated `Entries`), `Editions/ConstructRegistry.cs` (`Check` funnel, sink-based) | Registry below the frontend; `Entries` generated not hand-written (P1/P7). |
| create | — | `Editions/Constructs.g.cs` (generated id consts) | Compile-checked ids; greppable to one definition (P3/P7). |
| move | `Validation/ReservedWords.cs`, `Validation/ReservedWords.Table.cs` | `Editions/ReservedWords.cs`, `Editions/ReservedWords.Table.g.cs` | Reserved-word truth below the frontend (available to the parser's future COBOL-WORDS pass). |
| delete | `Frontend/Parsing/EditionGateHints.cs` (207 loc) | — (the residue folds into superset parse + bind-time `ConstructRegistry.Check` gating funnelled through the `VersionConformancePass`; the vendor JSON/XML `COBOL0313` disposition relocates to `CobolErrorStrategy` as a token-keyed vendor hint) | Eliminate the reverse-engineering table (P1/P3). |
| refactor | `Frontend/Parsing/CobolParserCoreBase.cs` | add `EditionInfo Edition`; `is85/2002/2014/2023` delegate to `Edition.Has` (used only by the two load-bearing forward-detects) | Single DialectLevel source (P5). |
| refactor | `Frontend/Preprocessor/ReferenceFormatProcessor.cs` (`EditionGates` inner class) & `CopyProcessor` | consume `EditionSeverityPolicy` + `ConstructRegistry.Check` over a shared sink; delete the inline `if(permissive)` | One severity policy (P2). |
| refactor (×5) | inline gates `StatementBinder.Accept.cs:45` (0816), `StatementBinder.cs:1229` (0803), `AlterSwitches.cs:119/137` (0811/0810), `Call.cs:176` (0882) | `constructs.json` rows (pinned codes) + `ConstructRegistry.Check` call sites | Kill the parallel gating mechanism; put them in the matrix (P4). |
| absorb + delete | `Validation/EditionValidator.cs` | the `VersionConformancePass` (its §8.9 reserved-word funnel moves into the pass) | One edition-gating funnel; shared metadata + sink (P9). |
| rename | generated ns `legacy-root.Generated` (in assembly `Cobol.Net.Frontend`) | `CobolNet.Frontend.Generated` (one MSBuild property in `Invoke-Antlr4CSharp.ps1`) | Remove the stale legacy namespace/alias (adjacent cleanup; enables clean `Editions`/`Frontend` refs). |
| create | — | `scripts/gen-vcr.ps1` + `VersionBehaviorMatrixTests` | Harness-driven VCR status + behavior-variant gating (P8). |
| update | `docs/VERSION_CHANGE_REFERENCE.md` | generated output; `docs/DOC_INDEX.md` row | Ledger can no longer go stale (P7/P8). |
| delete/quarantine | `CobolParserJsonXml.g4`, `CobolExtensionsJsonXml.g4` (jsonStatement/xmlStatement) | vendor axis (out of ISO scope) | Non-ISO; the edition framework must never map them to the 0900 band (already handled as `COBOL0313` — keep that, don't regress). |

---

## 4. Migration notes (keep the battery green throughout)

The battery: greenfield conformance + unit + characterization + the FULL NIST legacy guard (current
counts live in the STATUS banners), plus
the edition-specific suites (`VersionMatrixTests`, `ConstructRegistryDriftTests`,
`ReservedWordsDriftTests`, `EditionGateDiagnosticTests`, `ReservedWordPosition*`,
`MoveEditionDifferentialTests`, `CallExceptionPhraseEditionTests`, `DataSkeletonEditionTests`).
Behavior must stay byte-stable; the `Check` body is copied verbatim, so no diagnostic text changes.

Ordered, each step ends green:

1. **Stand up `Cobol.Net.Editions`, move leaf types.** Move `EditionCodes`, `ConstructDialectStatus`,
   `ConstructRegistry` (still hand-written `Entries`), `ReservedWords*` into the new assembly. Add
   `EditionInfo`, `IDiagnosticSink`, `EditionSeverity`, `EditionSeverityPolicy`, `EditionDiagnostic`.
   Reference `Editions` from `Frontend` and `Compiler`. **Keep `EditionContext` as a thin adapter**
   that wraps `EditionInfo` + a `List<string>`-backed `IDiagnosticSink` and exposes the old
   `Error/Warning/Removed/Diagnostics/Warnings/DialectLevel/MaxDigits` members unchanged. All 290 call
   sites compile untouched. Run full battery. *(No behavior change — pure relocation + adapter.)*
2. **Generate the registry.** Add the source generator (or MSBuild gen), enrich `constructs.json`
   with `display`/`citation`/`diagnosticCode`, delete the hand-written `Entries`. The drift test flips
   to "generator output == committed snapshot". Run battery. *(No behavior change.)*
3. **Introduce `Constructs.*` id consts** at call sites (mechanical find/replace of magic strings).
   Battery.
4. **Fold the five inline gates** into rows + `Check`, with positive/negative fixtures. Battery + the
   new matrix rows must pass. *(This is the only step that could shift a diagnostic; the pinned codes
   are preserved, so message text is stable — assert via `EditionGateDiagnosticTests`.)*
5. **Retire the parse-layer edition gates (the bind-time gating migration).** Drop the edition
   `{isXXXX()}?` predicates batch by batch, each construct gaining a bind-time
   `ConstructRegistry.Check` at its recognition point (the residue-first batches of
   `DESIGN-version-conformance-pipeline.md` §5; a shared `.g4` change needs the FULL legacy guard
   each batch). Only the two load-bearing forward-detects (`retryPhraseAhead()`, `boolExprAhead()`)
   survive, for tokenization only. When the last batch lands, **delete `ReservedWordEditionHints`**
   (the vendor JSON/XML `COBOL0313` disposition moves to `CobolErrorStrategy` as a token-keyed
   vendor hint). Battery.
6. **Frontend preprocessor** switches to `EditionSeverityPolicy` + `Check`; delete the inner
   `EditionGates` severity duplication. Battery.
7. **Retire the `EditionContext` adapter.** Migrate the 290 sites to `EditionInfo` + `IDiagnosticSink`
   in slices (per binder partial / per emitter partial), deleting the adapter last. This is the
   largest mechanical step; it is safe because the adapter guarantees identical behavior until each
   site is moved. Battery after each slice.
8. **VCR generation + behavior-variant matrix + the audit backfill.** Independent of 1–7; can proceed
   in parallel once the registry is the single source.

Rollback granularity is per-step; steps 1–3 and 8 are pure-additive/relocation and cannot regress
behavior.

---

## 5. Risks

- **R1 — ANTLR speculative predicate evaluation — ⛔ MATERIALIZED; forward stamping
  ABANDONED.** Predicate stamping records a gate whenever ANTLR EVALUATES a
  hoisted `{Gate}?` predicate — which happens SPECULATIVELY, at the stuck token, during a FAILING
  prediction/recovery, not only where the construct actually appears. The attempted mitigations
  (furthest-token-wins + reset-per-parse, plus an added `Consume()`-reset and a JSON/XML guard) each
  patched a sub-case but the general failure is intrinsic: an ordinary typo (`IF W = .`, a stray `)`,
  the unsupported `SUPPRESS`) records a gate for a construct the program never used and emits a
  confidently-wrong "requires COBOL-YYYY". An adversarial review found it; it was
  reproduced on the CLI. **A forward stamp cannot reliably mean "the user WROTE this construct."**
  This is why the construct-id annotation records identity only on a COMMITTED match (§2.7).
  *Resolution — the bind-time gating migration:* edition
  introduction-gating moved to BIND time construct by construct — first every HARD-reserved
  construct (ALLOCATE, INVOKE, CLASS-ID, DELETE FILE, GOBACK RETURNING, LOCK MODE, the record-lock
  phrases, … — 24 constructs / 11 clusters), then the **reservation-word residue** (XOR/EXCLUSIVE-OR,
  the boolean operators, SHARING/RETRY/UNLOCK, PROPERTY — tokens that are legal §8.9 user-defined
  words below their edition) — each gated at its recognition point through the ONE
  `ConstructRegistry.Check` funnel, removing both the parse predicates and the reverse signatures.
  Only the two load-bearing forward-detects (`retryPhraseAhead()` on the OPEN clause,
  `boolExprAhead()` on the boolean-condition ENTRY) survive, for tokenization only. The transitional
  recognizer `ReservedWordEditionHints` (metadata registry-driven, so P1/P7 duplication is gone)
  covered the not-yet-migrated residue and is then DELETED; its vendor JSON/XML `COBOL0313` branch
  relocates to `CobolErrorStrategy` as a token-keyed vendor hint. All edition diagnosis funnels into
  the ONE `VersionConformancePass` — the recognizer does not exist in the end state.
- **R2 — 290-site `EditionContext` migration churn.** *Mitigation:* the adapter (step 1) lets the
  rename land with zero behavior risk; sites migrate in independently-testable slices; the adapter is
  deleted only when the last site is gone.
- **R3 — Source generator toolchain burden** (must regen portably on both OSes, fail the build on
  failure, java+pwsh already prerequisites for ANTLR). *Mitigation:* Q1 fallback to an MSBuild
  pre-build script mirroring the proven `gen-reserved-words.ps1` discipline.
- **R4 — Enriching `constructs.json` with `display`/`citation` could desync from ISO wording.**
  *Mitigation:* the citations move verbatim from the current `ConstructDialectStatus` rows; the drift
  test now guards the *rendered* doc too.
- **R5 — `EditionInfo` as a `record struct` threaded widely** risks accidental defaulting (a `default`
  `EditionInfo` is `Year=0`). *Mitigation:* `Year=0` is invalid; add a guard in `Check`/`Has` that
  throws on an unvalidated year, and default the parser/driver fields to `EditionInfo.Latest`
  explicitly (never `default`).
- **R6 — Behavior-variant matrix is genuinely new surface** (running programs per edition and diffing
  output) and may reveal many un-gated semantic deltas at once. *Mitigation:* land it as `pending`
  rows first (catalogued, loud), then burn down; it is a *discovery* tool, not a gate on the rename.

---

## 6. Open questions for the owner

1. **Q1 — Registry generation mechanism? ✅ RESOLVED: committed PowerShell script.** `scripts/gen-constructs.ps1`
   emits the committed `.g.cs` (mirroring `gen-reserved-words.ps1`), NOT a Roslyn source generator; the
   diagnostics doc uses the same discipline via `scripts/gen-diagnostics-doc.ps1` (the drift test renders it).
2. **Q2 — Delete `EditionGateHints` outright, or keep a registry-driven thin recognizer? ✅ RESOLVED:
   deleted — the recognizer does not exist in the end state.** Forward predicate stamping was disproved
   (R1); the bind-time gating migration then folds EVERY gated construct — including the
   reservation-word residue whose tokens double as §8.9 user words (the lexical ambiguity is resolved by
   the two load-bearing forward-detects `retryPhraseAhead()`/`boolExprAhead()`) — into bind-time
   `ConstructRegistry.Check` funnelled through the ONE `VersionConformancePass`. The transitional
   `ReservedWordEditionHints` recognizer (metadata registry-driven) covered the residue only until its
   batch landed, then is deleted; the vendor JSON/XML `COBOL0313` disposition
   relocates to `CobolErrorStrategy` as a token-keyed vendor hint.
3. **Q3 — Assembly boundary? ✅ RESOLVED: dependency-free.** `Cobol.Net.Editions` references no ANTLR runtime;
   `Check` takes primitives (`EditionInfo` + `IDiagnosticSink`), and any parse-context mapping stays in the
   frontend. Cleaner layering, as recommended. Its one project reference is `Cobol.Net.Runtime`, the dependency-free
   foundation layer below it, for the Annex C fold (`CobolNames`) that keys its word tables (kb/Work PB1965).
4. **Q4 — Scope of the behavior-variant matrix (§2.10)? ⏭ DEFERRED to P3.** Running every construct
   under all four `--std` values and diffing stdout is the P3 version-gating audit / `VersionBehaviorMatrixTests`,
   explicitly out of P2 scope. This is the one open question P2 does NOT resolve.
5. **Q5 — `EditionContext` final name? ✅ RESOLVED by implementation: kept `EditionContext` behind the adapter.**
   It is split into `EditionInfo` + an `IDiagnosticSink` while keeping the public surface + name so the ~290
   sites compile untouched. Renaming the collector to its final name (`DiagnosticSink` /
   `CompileDiagnostics`) + retiring the adapter is the optional P7 churn.
6. **Q6 — General diagnostic-descriptor registry here or a separate dimension? ✅ RESOLVED: seeded HERE, broader
   registry deferred to P7.** The edition band + the `COBOLNET0899` split + the reused `COBOLNET1533` live in
   `Cobol.Net.Editions/Diagnostics/DiagnosticCatalog` (`docs/DIAGNOSTICS.md` + `DiagnosticRegistryDriftTests`).
   The full every-code→descriptor migration + folding the frontend's parse-layer descriptors / its 3-value
   `DiagnosticSeverity` into this home is the P7 follow-on.