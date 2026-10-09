# The WiseOwl COBOL runtime's text processing — collation · normalization · segmentation · locale · CLDR · cache

`Cobol.Net.Runtime` is the library every compiled COBOL program links against; this file is the map of its
**text-processing subsystems** — six folders that share one Unicode version, one design rule ("our own derived data,
so every host behaves identically") and one set of integration seams. Each folder has its own README with the depth;
this one shows how they fit and what a program pays. (kb/Work PB101, PB104, PB105, PB106.)

| Subsystem | Folder | One line |
|---|---|---|
| **Collation engine** | `Collation/` | the derived CLDR/UCA table (`CollationTable`), the streaming comparison and keys (`Collator`, `CollationKey`), the façade (`CollationEngine`); `.tailor` site overrides (`TailoringRules`) |
| **CLDR locale loader** | `Collation/CLDR/` | every CLDR collation file (135 locales), the rule-syntax parser, the builder that turns rules into a locale's table + settings |
| **Locale selection** | `Collation/Locale/` | `LocaleManager` (select for the run unit), `LocaleInfo`, `LocaleConfig` — over the run unit's ONE `LocaleState` |
| **Key cache** | `Collation/Cache/` | `CollationKeyCache` per collator — SORT/MERGE and INDEXED keys through it |
| **Normalization** | `Unicode/` | `UnicodeNormalizer`: NFD (the engine's own) and NFC (the host's), `CompareNormalized` |
| **Segmentation** | `Unicode/Segmentation/` | `GraphemeBreaker` / `GraphemeEnumerator` / `GraphemeCluster`: UAX #29 extended grapheme clusters from a derived table |
| **Initialization** | `Collation/CollationRuntime.cs` | `Initialize()` (cheap, every run unit), `Warmup()` (eager), `Status` |

Data: `data/unicode/` (pinned UCD 17.0.0 + CLDR release-48-2 files, Unicode License; provenance in
`data/unicode/SOURCES.md`); generators `scripts/collation/generate-collation-table.py`,
`scripts/collation/pack-cldr-collation.py`, `scripts/unicode/generate-grapheme-table.py`; three embedded artifacts
with manifests and drift tests. ⚖ No ISO/IEC text or table anywhere; the conformance statement (owner decision Q4,
verbatim): *"Implements collation behavior consistent with ISO/IEC 14651 through derived tables and CLDR/UCA data."*

## 1. The collation pipeline

```
   two texts (or one, for a key)
        │  a combining mark present? → NFD with the table's own decompositions (never the host's)      [Unicode/, Collation/Normalizer]
        ▼
   collation elements: code point → element sequence; contractions (longest, then discontiguous over
   unblocked marks); expansions; Hangul → jamo; implicit weights for Han/Tangut/Nushu/Khitan/unassigned    [CollationTable, CollationElementIterator]
        │
        ▼
   weights per level under CollationOptions — strength · alternate (maxVariable) · caseFirst · backwards   [Collator.WeightAt]
        │
        ├──► Compare: level 1 for both texts (identical prefix skipped at a context-safe boundary),
        │              level 2 only on a tie, then 3, then 4 (shifted), then the NFD code points (Identical)
        └──► GetKey: the same weights materialized level by level → CollationKey (CompareTo, ToByteArray)
                        └──► CollationKeyCache.For(collator).GetKey(text): build once, compare many
```

What reaches it from COBOL (all through the ONE `CobolCollation` carrier the compiler emits — `Values/Text/`):
relation conditions under an `ALPHABET … IS LOCALE` program collating sequence, SORT/MERGE keys, INDEXED file keys,
`MAX`/`MIN`, `ORD`/`CHAR`, `HIGH-VALUE`/`LOW-VALUE`, and `FUNCTION STANDARD-COMPARE` (the ISO/IEC 14651-style
four-level ordering; an `ORDER TABLE` literal may name a locale). The trailing-space truncation of §8.8.4.2.11 and
the `EC-LOCALE-INCOMPATIBLE` condition for an ill-formed operand live in the carrier (`LocaleCollation`), not in the
engine.

## 2. The normalization pipeline

`UnicodeNormalizer.Normalize(text, NFD | NFC)`, `IsNormalized`, `CompareNormalized(a, b, form)`
(`Unicode/README.md`). **NFD** is the collation table's own decomposition and canonical reordering (one NFD in the
runtime, UCD 17.0.0 — the host's ICU predates Unicode 16); **NFC** is the host's `string.Normalize` behind an
invariant-mode fallback (the upgrade path — composition data into the generated table — is documented). Nothing
in the collation path normalizes to NFC: UCA is defined over NFD and the engine decomposes on demand, so canonical
equivalence never depends on a caller normalizing first.

## 3. The segmentation pipeline

`GraphemeBreaker.Enumerate(text)` / `Count` / `Split` / `Truncate(text, n)` / `IsBoundary` / `NextBoundary`
(`Unicode/Segmentation/README.md`): one property lookup per code point (Grapheme_Cluster_Break,
Extended_Pictographic, Indic_Conjunct_Break, all from `Data/grapheme-break.bin`) and the UAX #29 rules with their
three pieces of state (regional-indicator run, emoji sequence, conjunct). Verified on every line of Unicode's
`GraphemeBreakTest.txt` 17.0.0.

Where it meets the other two: segmentation is **stable under normalization** (NFC and NFD have the same clusters,
canonically equivalent one for one), and a **cluster-safe truncation keeps a collation prefix** where a code-unit
cut can lose an accent. Keys are **not** built per cluster — a collation contraction may cross a cluster boundary
(Thai prevowel + consonant, Czech `ch`) and a key is level-major; both facts are pinned by tests, and the engine
keys whole texts.

## 4. The locale pipeline

```
   a locale tag ("es-ES", "de-DE-u-co-phonebk", "nb", "")
        │
        ▼  CldrLocaleLoader.ResolveCollation: the CLDR file chain (tag → explicit parent (nb → no, yue → zh_Hant) or
        │  truncation → root), the type (-u-co-, else <defaultCollation>, else standard), the -u- settings keys
        ▼  CldrTailoringBuilder.Build: rules → weights (insertions, renumbering, reordering, case bits) +
        │  CollationOptions (strength, alternate/maxVariable, caseFirst, backwards); CollationTable.Rebuild closes it
        │  canonically (the one table-building step both front-ends use)
        ▼  TailoringRules.ForLocale (a .tailor for the tag or its language — the site override) → WithTailoring
        ▼
   ResolvedLocaleCollation (table · options · what came from where · Unsupported · Notes) — cached per tag
        │
        ├──► CollationEngine.ForLocale / TableForLocale / GetKey / TryGetOrderingTable
        ├──► LocaleManager.GetLocale / SetLocale (writes RunUnit.Current.Locale — the ONE LocaleState) / LocaleInfo
        └──► LocaleCollation.Current (the IS LOCALE phrase: resolves the run unit's LC_COLLATE locale at each use)
```

The default locale is the run unit's L2 user default (owner decision Q2: `COBOL_USER_LOCALE`, else the process
culture, else the root), read once at run-unit activation; `LocaleManager.SetLocale(tag)` changes it for every
category (the shape `SET LOCALE LC_ALL` will take in design increment T1); nothing else holds current-locale state.

## 5. CLDR integration

`Collation/CLDR/README.md`. All 135 collation files of CLDR release-48-2 are embedded (with `bcp47/collation.xml`
and `supplementalData.xml`'s parent locales), a site directory (`COBOL_CLDR_DIR`, `Collation/CLDR/`) may add or
override files (`.xml` LDML or the documented `.json` mirror), every construct of the pinned release's rule syntax is
applied except `caseLevel` / `numericOrdering` / `hiraganaQ` (reported, never dropped — kb/Work PB107), and 29
locales are cross-checked pair-by-pair against the host's ICU. The hand-derived Spanish `.tailor` is kept in
agreement with the CLDR derivation by a test.

## 6. Cache integration

`Collation/Cache/README.md`. One `CollationKeyCache` per collator (LRU or size-based eviction, `CacheConfig`,
`COBOL_COLLATION_CACHE`, `COBOL_COLLATION_CACHE_EVICTION`). Where it is wired: `CobolSort` keys every record's
alphanumeric key window under a LOCALE sequence once and sorts by keys (`KeyColumns`); `IndexedConnector` compares
LOCALE-keyed file keys through `KeyOf`; `CollationEngine.GetKey(text, tag)` / `Collator.GetKeyCached` are the API
forms. Where it deliberately is not: `Collator.Compare` — the streaming comparison of two short operands is cheaper
than two cache lookups (measured in `tests/Cobol.Net.Benchmarks`).

## 7. Initialization and wiring

- **Runtime.** `RunUnit`'s constructor calls `CollationRuntime.Initialize()` — cheap and idempotent: it reads the
  key-cache configuration from the environment (unless a host called `CollationRuntime.ConfigureCache(...)` first)
  and, only when `COBOL_COLLATION_WARMUP` is set, warms up. Everything else stays LAZY: the root table (~230 KB
  deflated), the grapheme table, the CLDR pack and every locale's collation decode on first use — a program that
  never collates under a locale never pays for them. `CollationRuntime.Warmup(tag)` is the eager form for a
  latency-sensitive host (root table, grapheme table, CLDR pack, the default locale resolved and built, the
  normalizer probed; a default locale that cannot resolve is a `Status.Warning`, never an exception).
  `LocaleState` initializes the default locale from the environment when the run unit is created; a host that wants
  another calls `LocaleManager.SetLocale("…")` before the program's entry point — the compiled `Main` needs no change.
- **Compiler.** `cobol.exe` does not initialize the subsystem: compiling never collates. It reaches it only to
  VALIDATE names at compile time — an `ORDER TABLE` literal through `CollationEngine.TryGetOrderingTable`
  (COBOLNET1662 when it cannot resolve; a locale tag with CLDR data now resolves) — and that loads on demand. The
  emitted `__COLLATE` carrier is `LocaleCollation.Current` for the bare `IS LOCALE` phrase.
- **Hosts / tools.** `CollationEngine.*`, `LocaleManager.*`, `UnicodeNormalizer.*`, `GraphemeBreaker.*`,
  `CollationKeyCache.*` are the public surfaces; `CollationRuntime.Status` reports versions and state.
- **Environment variables — ONE registry.** `Control/RuntimeConfig.cs` enumerates every variable the runtime reads
  (`RuntimeConfig.All`, `Describe()`, `Find(name)`): `COBOL_USER_LOCALE` / `COBOL_SYSTEM_LOCALE` (the run unit's
  locale defaults), `COBOL_COLLATION_DIR` / `COBOL_CLDR_DIR` (site data), `COBOL_COLLATION_CACHE` /
  `COBOL_COLLATION_CACHE_EVICTION` / `COBOL_COLLATION_WARMUP`, `COBOLNET_CLOCK` (the clock pin), and the external
  switches' family `COBOL_<SWITCH-NAME>`. It is a DIAGNOSTIC registry, not a configuration system — each subsystem
  reads its own constant as before; the registry references those constants and `RuntimeConfigTests` scans the
  runtime sources in both directions (a new read or name literal that is not registered fails; a registered entry
  whose read disappeared fails). There is no appsettings file and no configuration framework: the runtime is
  dependency-free.

## 8. Tests and benchmarks

`tests/Cobol.Net.Tests.Unit/Collation/*` and `Unicode/*` (the engine, table, keys, tailoring, CLDR conformance,
CLDR loader/builder + ICU cross-check, key cache, locale manager, normalization, segmentation incl. the full
GraphemeBreakTest); `tests/Cobol.Net.Benchmarks/` (`CollationBenchmarks`, `CacheBenchmarks`, `CldrBenchmarks`,
`UnicodeBenchmarks` — each against the host's ICU / .NET implementation as baseline; run
`dotnet run -c Release --project tests/Cobol.Net.Benchmarks -- --filter *`).
