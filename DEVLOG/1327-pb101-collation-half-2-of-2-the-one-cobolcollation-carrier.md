## Entry 1327 — 2026-08-18 19:20 PDT — PB101 (collation, half 2 of 2): the ONE `CobolCollation` carrier (T2), the LOCALE arm reachable from `ALPHABET … IS LOCALE` (T3), `ORDER TABLE` + `STANDARD-COMPARE` (T7), the engine's identical-prefix skip — and the four owner-requested subsystems (test suite, benchmark harness, Unicode normalization, locale selection)

**What landed — the integration half.** The compiler-integration half of the owner's collation guidance, in the three
moves the adopted locale design (§12) sequenced as T2, T3 and T7, plus one engine optimization the new benchmark harness
forced:

- **T2 — the carrier collapse (behaviour-free for every existing program).** `Runtime/Values/Text/CobolCollation.cs` is
  the ONE collating-sequence carrier (`Compare`, `Weight`, `PositionCount`, `CharAt`, `HighValue`, `LowValue`, and ONE
  `ThruMember` on the base with the §14.7.8 EC-RANGE-INVALID guard). `AlphanumericCollation` and `NationalCollation`
  became its two table arms — their `Compare` bodies moved in from the `CobolString.Compare(…, ushort[])` and
  `(…, NationalCollation)` overloads, which are gone; `CobolString.Compare`/`ThruMember` collapsed to {`char pad`,
  `CobolCollation`}; `CobolSort.Sort/Merge`, `CobolFile.RegisterIndexed/AddAlternateKey` (+ `FileRegistry`,
  `IndexedConnector`), `MaxString`/`MinString`/`OrdMax`/`OrdMin`, `Char`/`CharNational`/`Ord` take the carrier (the two
  identical `Ord` bodies are one). The NATIVE order has no object — the two-argument compare and a `null` slot — so an
  ordinary program's generated text is unchanged (corpus green, goldens byte-identical). Compile side:
  `Binding/CollatingModel.cs` gains `AlphabetDef(Table | Locale)` and `LocaleCollatingSpec`, `NationalAlphabetDef` a
  `Locale` slot; `Alphabets` maps to defs, the PCS / `BoundSort*.Collating` / `FileModel.PrimeKeyCollation` are defs; and
  `CodeGen/Roslyn/CollationEmit.cs` is the ONE place that spells a carrier's constructor (`__COLLATE`/`__COLLATE_NAT`
  for the PCS, an inline instance for a statement or file alphabet). One trap the corpus caught within seconds: the
  emitted NAMED arguments `primeWeights:`/`weights:` were string literals, not `nameof`, so the runtime rename broke every
  generated indexed-file registration until `RuntimeApi` was renamed to match — the corpus filter is exactly the gate for
  that.
- **T3 — the LOCALE arm, for the CURRENT-locale form.** `ALPHABET name IS LOCALE` (no locale-name; §12.3.7.2, 2002+; the
  new `alphabet-locale-2002` construct row gates it at 85) binds to `LocaleCollatingSpec(null)`; as the PROGRAM COLLATING
  SEQUENCE it makes every alphanumeric relation a LOCALE-BASED comparison (§8.8.4.2.7/.11), and SORT/MERGE (§14.9.40 GR5b),
  indexed keys (owner decision Q3, L8), MAX/MIN, ORD/CHAR (L7) and HIGH-/LOW-VALUE (§8.3.3.6.4) follow through the same
  carrier. `Runtime/Values/Text/LocaleCollation.cs` applies §8.8.4.2.11's operand rule (trailing spaces truncated, all-spaces
  → one space, no padding — on SPANS, no substring per fixed-width operand), resolves the run unit's current LC_COLLATE
  locale AT EACH USE (§12.3.7.4 GR7e) through the new minimal `Runtime/Control/LocaleState.cs` on `RunUnit` (the T1 seam: the
  two Q2 defaults from `COBOL_USER_LOCALE`/`COBOL_SYSTEM_LOCALE`, else the .NET culture, else root, read once; `Set` is
  where SET LOCALE lands), and compares through the derived engine (determination **L11**: the locale's CLDR default — its
  tailoring over the root table, tertiary, non-ignorable). **L6 was re-derived** over the table: every well-formed code
  point is ordered, so EC-LOCALE-INCOMPATIBLE fires for an unpaired surrogate — and a test fires it. ORD/CHAR/HIGH/LOW
  materialize a 65,536-entry order vector once per collator (L7 as landed; HIGH-VALUE is U+FFFF and LOW-VALUE U+0000 under
  every CLDR table, so the figurative constants still fold at compile time). The NAMED form `IS LOCALE locale-name-2`
  (§12.3.7.3 SR24) stays refused by name until T1's LOCALE clause; the negative fixture `pb100-alphabet-locale-phrase` now
  documents exactly that split. Golden `2002/pb101_alphabet_locale_pcs`: apple < Zebra, zebra < Zebra, the trailing-space
  rule, a table SORT ordering apple Apple zebra Zebra, MAX = Zebra, ORD a < A < b, CHAR(ORD("Q")) = "Q" — every assertion
  holds under EVERY CLDR locale, so the run is host-independent.
- **T7 — `ORDER TABLE` + `STANDARD-COMPARE`**, landed by an Opus 5 subagent from a written brief in the shared tree and
  gated here (the agent could not build: see the process defect below): grammar `orderTableClause` (`ORDER` is a
  cobolWord — reserved at 2002+ through the §8.9 funnel — `TABLE` a token; the predicate `orderTableAhead()` is NOT
  edition-gated because `TABLE` is reserved at every edition, so the 85 gate can fire on recognition), `DataBinder.
  OrderTableBind` (ONE clause per paragraph — §12.3.7.2 brackets it without an ellipsis; SR10/SR11; a literal-9 the engine
  cannot resolve is a legal program with a defined run-time outcome, so it WARNS — new COBOLNET1662 — and every reference
  sets EC-ORDER-NOT-SUPPORTED, §15.85.4 r2), `BindStandardCompare` (ArgKinds `ssoi` — the catalog's `ssis` had read the
  format backwards; `'o'` = §15.3 argument type 12, with a new `NonOperandArgumentKinds` disposition table and a drift fact
  so a non-operand code can never again be a dead column — the PB1 trap closed generally; COBOLNET1663 for r5/r6
  violations), the `OrderingTable` rider on `BoundIntrinsicCall`, the renderer arm, and `CobolIntrinsics.StandardCompare`
  over `CollationEngine.Standard`/`StandardAtLevel` (levels 1–4; absent = 4 per r1; the r4 trim IS `LocaleCollation.
  TrimForLocale`; r3's national conversion is the identity on D-N1; `"<"`/`"="`/`">"`, length 1). EC-ORDER-NOT-SUPPORTED
  got the full ambient-gate machinery (`CheckingFlags`, `ExceptionState.OrderNotSupportedError`, an `EcEmitter` fatal
  gate row, the `EcBinder` intrinsic-bearing-statement gate) rather than a bare `Set` — §14.6.13.1.1 says an unchecked
  condition is not raised, and a bare `Set` could never have reached a USE declarative; checking off answers `"="` (the
  §14.6.13.1.3 #8 implementor choice, documented). `EcNameResolution` no longer refuses the name. Rows `order-table-2002`
  + `standard-compare-2002`; VCR row 7.19 states the derived edition edge and how to overturn it. Goldens
  `2002/pb101_standard_compare` (levels 1–4, the omitted level, `"a-b"` = `"ab"` at 3 and `<` at 4, resume < Résumé,
  the trim, length 1, the default table) and `2002/pb101_ec_order_not_supported` (`>>TURN … CHECKING ON` + a USE
  declarative observing the condition: `HANDLED=EC-ORDER-NOT-SUPPORTED`, the MOVE abandoned, `AFTER`) — both `.out`s
  produced by RUNNING the programs and checked line by line against §15.85.4 before pinning; negative
  `standard-compare-unknown-ordering-name` (COBOLNET1663). CONFORMANCE.md §2 row 25 → CLAIMED with the verbatim
  statement; §4 item 5 and the A.4.9 row → item 10's ALPHABET half (bare form) and item 11 claimed, the rest refused by
  name until their increment.
- **The engine's identical-prefix skip.** The benchmark harness (below) measured `Compare_LongStrings` at 17.9 µs for
  1.5 KB keys differing in the last character — 11.7 ns/char against ICU's 0.29 (446 ns): a 40× gap where short keys sat
  at 5×, i.e. a different per-character regime, not a constant factor — because the streaming compare walked the
  identical prefix at every level. `Collator.Compare` now skips the common prefix of the (normalized) texts, backed up
  to a CONTEXT-SAFE boundary — not inside a surrogate pair, between two starters (a combining-mark run is never split:
  a discontiguous contraction may reach across marks), and with no contraction START within
  `CollationTable.MaxContractionLength` code points before it (a contraction, contiguous or discontiguous, extends
  forward from its first code point only) — for the state-free non-ignorable collators (every locale sequence; the
  shifted lane's level-4 "following a variable" state depends on the prefix, and STANDARD-COMPARE's arguments are
  short). The full CLDR conformance files ran again afterwards: **206,298 + 227,809 lines, 0 violations** — the
  NON_IGNORABLE file exercises the skip on every consecutive pair.

**The four owner-requested subsystems (one pass, as directed; the fan-out was two Opus 5 agents in isolated worktrees
for the two that depended only on the committed engine, the other two by the main line):**

- **(A) The test suite** — `tests/Cobol.Net.Tests.Unit/Collation/`: `CollationEngineTests` (+ the requested ASCII /
  case / accent / multi-character / equality-less-greater cases and a tailoring test with the shipped Spanish ñ and a
  synthetic ä-after-z tailoring), `CollationKeyTests` (new: one weight per level for "a", three primaries for "abc",
  ignorables and expansions, equal texts → equal keys, case-only difference at tertiary only, accent-only at secondary
  only, other collators' level counts, the byte image ordering like `CompareTo`), `CollationTableTests` (+ the requested
  key ASCII lookups, A/a primary equality with tertiary difference, á sharing a's primary and differing at secondary —
  read from the SECOND element of its expansion, as a real UCA table has it), plus `CollationTailoringTests`,
  `CollationConformanceTests`, `CobolCollationTests` (the carrier: L6/L7/L11, the base-class ThruMember, the drift test
  that no runtime surface takes a raw `ushort[]` or a concrete arm any more) and `LocaleManagerTests`.
- **(B) The benchmark harness** — `tests/Cobol.Net.Benchmarks/` (BenchmarkDotNet 0.15.8, in the solution, central
  package version; `dotnet run -c Release --project tests/Cobol.Net.Benchmarks -- --filter *Collation*`): the requested
  five benchmarks plus the shifted/Standard, span, LOCALE-carrier, tailored-Spanish variants and `string.CompareOrdinal`
  / host-ICU `CompareInfo` baselines per category, `MemoryDiagnoser` on. Its README carries the measured tables. What
  it proved before the prefix skip: every ASCII comparison allocates 0 B; `CaseOnly / LastChar` = 3.01× (the level
  model, to three digits); root vs host ICU ~5× on short pairs, 1.09× on short sort keys; tailoring is free; shifted
  costs 1.54×; the mixed-script pair allocates 93 B (the NFD path — noted as the next engine follow-up). And the long-key
  outlier that became the prefix skip.
- **(C) The Unicode normalization subsystem** — `src/Cobol.Net.Runtime/Unicode/` (`UnicodeNormalizationForm` {NFC, NFD},
  `UnicodeNormalizer.Normalize` / `IsNormalized` / `CompareNormalized` / `IsNfcAvailable` / `NfdUnicodeVersion`,
  README): NFD IS the collation engine's table-driven decomposition (ONE NFD in the runtime — host-independent, the
  table's Unicode 17.0.0), NFC wraps the host's `string.Normalize(FormC)` with the invariant-globalization fallback and
  the upgrade path named (composition data + `Full_Composition_Exclusion` into the generated table). 16 tests; the
  NFD-vs-.NET cross-check over all 2,081 decomposable code points: 2,061 identical, the 20 differences EXACTLY the
  Unicode 16 script additions the host's ICU lacks — the ICU-lag finding of DEVLOG 1326 measured a second way.
- **(D) The locale-selection system** — `src/Cobol.Net.Runtime/Collation/Locale/` (`LocaleManager.SetLocale` /
  `CurrentLocale` / `GetLocale` / `ResetLocale`, `LocaleInfo` {Name, TailorFilePath, Table, IsTailored,
  IsRecognizedCulture, Collator}, `LocaleConfig` {DefaultLocale = the L2 user default, TailoredLocales,
  SupportedLocales, IsSupported}) over the run unit's ONE `LocaleState` and the engine's cached tailored tables — a
  selection is what a running program's `ALPHABET … IS LOCALE` comparisons see, no second store, no table mutation;
  `TailoringRules` gained the `Collation/Locales/` search directory (the owner's layout), `SearchDirectories` /
  `TailoringsIn` / `DescribeSearch`, and the `0x` code-point prefix of the owner's sample line. No start-up call is
  needed (the run unit initializes from the environment); a host overrides with `LocaleManager.SetLocale(...)` before
  running the program.

**Two process defects the agents found, fixed and registered.** [[PB102]] — `data/unicode/*.txt|xml` were not
declared `-text`: under `core.autocrlf=true` a fresh Windows clone (or an agent worktree) checks them out CRLF, every
manifest-pinned SHA-256 changes and the table drift test fails on a tree nobody edited; `.gitattributes` fixed. [[PB103]]
— `scripts/hooks/fleet_active_build.py` self-blocked agents: a worktree agent's own transcript is one of the "live" ones
(the window never clears), and a MAIN-tree agent hits the same wall (the T7 agent could not run its gate; the main line
gated its work); the hook now allows builds whose cwd is under `.claude/worktrees/` and excludes the caller's own
transcript — verified in both directions.

**Verification (this tree).** Solution builds Debug and Release, 0 warnings · Unit filter over every touched subject
(collation · locale · unicode · standard-compare · intrinsic · EC names · drift · constructs · national · sort ·
special-names · diagnostics) **3386 / 3386** · Conformance corpus + intrinsic/EC/sort/file/figurative/national
differential filter **1099 / 1099** (the three new positive goldens and the negative in the population) ·
VersionMatrix + negatives + edition gates **2480 / 2480** · characterization **33 / 33** · the full CLDR conformance
files **0 violations** with the prefix skip · `DIAGNOSTICS.md` regenerated (the two new codes) · benchmark harness
run in Release after the prefix skip (its README carries the tables). The comprehensive battery follows this commit
(plan §0's battery #20).

**Design and register.** `DESIGN-locale-facility.md` header + §4.4.1/.3/.4/.5/.6, §4.9, §11, §12 re-based on the derived
engine (T2 ✅, T3 ◐ current-locale form, T7 ✅). `Collation/README.md` §6 states the landed state; `CONFORMANCE.md` §4 item 5,
§2 rows 25/41–42 and the A.4.9 row flipped; `COBOLNET_DESIGN.md` §0.5 and `DOC_INDEX.md` register the two READMEs.
`kb/Work/PB101.md` → LANDED (residue named: T1 carries the named `IS LOCALE` form and the SORT snapshot); `work.py next`
returns to PB64 (T1). The 18 §15.85 / ORDER TABLE traceability rows are adjudicated through `record_verdicts.py` (17 CONFORMS,
SR-12.3.7.3-11 PARTIAL — this landing owns literal-9's half only) and the drift gate resolves every reference — GAP 3933 → 3916.
Plan §0 carries the live state.

