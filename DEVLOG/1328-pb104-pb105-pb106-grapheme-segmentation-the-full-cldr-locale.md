## Entry 1328 — 2026-08-18 22:20 PDT — PB104 / PB105 / PB106: grapheme segmentation, the FULL CLDR locale loader (every CLDR locale's order derived from its rules), the collation key cache — and the repository-wide integration of the six text subsystems

**The ask.** The owner's second "hard implementation mode" guidance (the evening of the 18th, after PB101): three
more subsystems — (5) UAX #29 grapheme segmentation, (6) a FULL CLDR locale loader ("load CLDR collation data from
JSON/XML files … extract primary/secondary/tertiary weights, tailoring rules"), (7) a collation key caching layer —
and then a repository-wide integration plan: initialization, `LocaleManager` over CLDR, tests, benchmarks, a top-level
README. Done inline (no fleet — the tree stayed buildable throughout), three kb/Work notes, one residue note (PB107).

**What is not literally what was asked, and why (recorded once, in the register).** Two premises of the brief do
not match the material: (a) CLDR collation files carry RULES (`&N<ñ<<<Ñ`), not weights — so "extract weights" became
a BUILDER that turns rules into weights over the derived table (rule 5: the shape that makes every locale automatic
rather than the one that fits the sentence); (b) "before building keys: normalize to NFC, segment into grapheme
clusters, build keys per cluster" is not how UCA works — UCA is defined over NFD (the engine decomposes on demand),
a collation contraction may span a cluster boundary (Thai prevowel + consonant; Czech `ch`), and a key is
level-major, so per-cluster keys are not the text's key. The engine keys whole texts; a test pins all three facts
(`KeysAreNotPerCluster_ContractionsCrossClusterBoundaries`), and the segmentation README states what segmentation IS
to collation (clusters survive normalization; a cluster-safe truncation keeps a collation prefix — `Truncate`).

**PB105 — the CLDR loader/builder is the big one.**
- Data: all 135 `common/collation/*.xml` of release-48-2 (+ `bcp47/collation.xml`, `supplementalData.xml` for
  `parentLocales`) pinned under `data/unicode/cldr/`, packed deterministically (`pack-cldr-collation.py`, 907 KB)
  and embedded. `FractionalUCA.txt` + `PropertyValueAliases.txt` pinned for the generator: table **format 2** carries
  the CLDR REORDERING GROUPS (173: space, punct, symbol, currency, digit, then one contiguous primary range per
  script — assigned by the `FDD1` first-primary markers, contiguity asserted, and the `*` variable set asserted equal
  to space+punct) and the elements' CASE BITS (DUCET tertiaries 0008–000C).
- Engine: `CollationOptions` (strength, alternate, **maxVariable** by group, **caseFirst** as ICU's inverted case bits
  Upper < Mixed < Lower, **backwards secondaries** compared from the end — the prefix skip is not taken for those);
  `WeightMap` (root → table per level), `TailoringPlan` + `CollationTable.Rebuild` — the ONE construction step for
  both front-ends (`.tailor` weights translate through the maps).
- Parser: LDML via `XDocument` (my first `XmlReader` loop skipped a node after `ReadElementContentAsString` and merged
  collations — caught by the parse-everything drift test), the JSON mirror (CLDR publishes none), the whole rule syntax
  incl. `<*` ranges, `|` prefixes, `/` extensions, every logical reset position, `[import]`, `[suppressContractions
  [set]]`, Pattern_White_Space (Arabic files put LRMs between rules). **0 unsupported constructs over 135 files.**
- Loader: the parent chain per LDML — the `component="collations"` table plus the general table's PLAIN entries
  (nb → no; nb.xml is empty since CLDR 46), but NOT its `nonlikelyScript` entries (main-component only: zh_Hant's
  collation parent stays zh, where `stroke` lives) — checked against ICU's own coll bundles (nb: `%%Parent{"no"}`;
  zh_Hant: none). `-u-` keys (co/ka/kb/kc/kf/kh/kk/kn/kr/ks/kv; `-u-co-private-unihan` is two subtags).
- Builder: per-level slot lines, insertion after the anchor, `[before]`, special positions from the table,
  canonical closure over composites (Vietnamese ả ắ ậ; ặ needs the DISCONTIGUOUS match a + dot-below + breve →
  tailored ă), numbering with gap overflow → shift, reordering as a tile permutation with the space after
  `[last regular]` folded into Hani (Chinese pinyin moves with `[reorder Hani]`), case bits from the string (Mixed
  for "Aa" — Danish `AA < Aa < aa` under caseFirst upper, exactly ICU), prefix relations as contractions with the
  position moving to the string's own elements (the ja length mark), quaternary as identity (reported).
- **Verified against ICU**: 29 locales pair-by-pair on hand-written corpora — zero disagreements, except Latvian
  y/ī, which the two releases' rule files explain (CLDR 42 `&I<<y<<<Y`; 48-2 `&I<y<<<Y<ī<<<Ī`) and which the test
  allow-lists with that citation. The Spanish `.tailor` (hand-derived on the 18th) is kept in agreement with the CLDR
  derivation by a test. `es` builds in 6 ms, `zh` in 50 ms — once per process; resolved 20 ns.

**PB104 — segmentation:** the derived property table (GCB + Extended_Pictographic + InCB, UCD 17.0.0 — 17.0
widened the conjunct rule to Myanmar/Khmer/Tai Tham …), `GraphemeBreaker`/`GraphemeEnumerator`/`GraphemeCluster`,
every line of `GraphemeBreakTest.txt` (766) green on the first run, 3.5× faster than the host's `StringInfo`, 0 B.

**PB106 — the cache:** per-collator `CollationKeyCache`, wired where values recur — `CobolSort.KeyColumns` (every
record's key columns decoded ONCE: numeric values, `CollationKey`s under a LOCALE sequence, sliced windows
otherwise — n decodes instead of 2·n·log n walks; also a win for the native path) and `IndexedConnector.KeyCompare`
(LOCALE keys via `KeyOf`) — and NOT into `Collator.Compare`: the streaming compare of two short operands (27 ns)
beats two lookups (48 ns); keys win 16.6× on record-shaped case-different texts. Two measured mistakes on the way:
`ConcurrentDictionary.Count` on every insert (every bucket lock, ~18 µs) and `ToArray()` per eviction batch made a
miss under churn 27× a build; an interlocked count and a lock-free enumeration made it ≈2×. A CLOCK variant was
tried (cheaper still) and dropped: only approximately LRU on a burst, and the exact-LRU test said so.
`CacheEntry.Timestamp` via `DateTimeOffset.UtcNow` failed `ClockSeamDriftTests` (the runtime reads no wall clock
outside the run unit's clock seam) → `Age`/`IdleFor` from Stopwatch ticks.

**Integration:** `CollationEngine.ResolveLocale(tag)` (CLDR first, `.tailor` on top; cached) →
`ResolvedLocaleCollation`; `LocaleManager`/`LocaleInfo`/`LocaleConfig` over it (`Cldr`, `Options`, `Unsupported`,
`CldrLocales`); `TryGetOrderingTable` resolves any CLDR locale (an `ORDER TABLE` literal may name `de-u-co-phonebk`);
`CollationRuntime.Initialize()` from every `RunUnit` (cheap: the cache config from the environment; the tables stay
lazy — a program that never collates under a locale never pays), `Warmup()` eager for latency-sensitive hosts,
`COBOL_COLLATION_WARMUP`; the compiler untouched (compiling never collates); `src/Cobol.Net.Runtime/README.md` puts
the six subsystems' pipelines side by side; benchmark classes `CacheBenchmarks`, `CldrBenchmarks`,
`UnicodeBenchmarks` (numbers in the harness README, run 3).

**Gates:** solution Debug + Release clean · Unit FULL 4425/4425 · the collation suites incl. the CLDR conformance
samples green · **battery #22 on the final tree ALL GREEN** (Conformance 4815/4815 · Unit 4425/4425 · Char 33/33 ·
NIST 353/0 audit-clean · differential 0 flips; battery #21, run 20 minutes earlier on the tree minus the last guard
fix — `CollationEngine.IsKnownLocale`, so an `ORDER TABLE "MY_TABLE"` literal cannot resolve to Burmese through
the CLDR parent chain — was also ALL GREEN).

