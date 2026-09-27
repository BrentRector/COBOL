# Unicode grapheme cluster segmentation — `Runtime/Unicode/Segmentation/`

The third text subsystem beside collation (`Runtime/Collation/`) and normalization (`Runtime/Unicode/`): where those
answer *how do two texts order* and *are two spellings the same text*, this answers **how many characters does a
reader see, and where may a text be cut without breaking one**. It implements UAX #29 *extended grapheme clusters*
from WiseOwl COBOL's own derived property table, so every host segments identically whatever Unicode version its
runtime carries (kb/Work PB104).

| File | What |
|---|---|
| `GraphemeCluster.cs` | one cluster: the source string, `Start`/`Length` (UTF-16 code units), `Span`/`Memory`/`ToString()`, `CodePoints` (array) and `EnumerateCodePoints()` (allocation-free), `CodePointCount`, `FirstCodePoint`, `IsSingleCodePoint` |
| `GraphemeEnumerator.cs` | `IEnumerable<GraphemeCluster>` over one text — a struct with a struct enumerator, so `foreach` allocates nothing; `Count`, `ToArray()` |
| `GraphemeBreaker.cs` | the rules and the API: `Enumerate(text)`, `Split(text)`, `Count(text)`, `Truncate(text, maxClusters)`, `IsBoundary(text, index)`, `NextBoundary(text, start)`; the properties `GetBreakProperty(cp)`, `IsExtendedPictographic(cp)`, `GetIndicConjunctBreak(cp)`; `UnicodeVersion` |
| `Data/grapheme-break.bin` + `.manifest.json` | the derived property table (embedded resource `Unicode/Segmentation/Data/grapheme-break.bin`) and its provenance |

Generator: `scripts/unicode/generate-grapheme-table.py` over the pinned `data/unicode/GraphemeBreakProperty.txt`,
`emoji-data.txt` (Extended_Pictographic) and `DerivedCoreProperties.txt` (Indic_Conjunct_Break) — UCD 17.0.0, the
same Unicode version as the collation table (`GraphemeBreakerTests.Table_MatchesTheManifest_AndThePinnedInputs`
pins both). Regenerate + recommit data, table and manifest together.

## 1. What a grapheme cluster is

A **grapheme cluster** is what a reader perceives as one character, which is often several code points: a base
letter with its combining marks (`e` + COMBINING ACUTE), a Hangul syllable written as conjoining jamo (ᄀ ᅡ ᆨ = 각),
an emoji with a skin-tone modifier, a family emoji joined by ZERO WIDTH JOINERs (11 code units), a flag (two regional
indicators), CR + LF, a Devanagari conjunct (KA + VIRAMA + TA). UAX #29 defines the boundaries by a small set of
rules over three code point properties (Grapheme_Cluster_Break, Extended_Pictographic, Indic_Conjunct_Break); the
data changes with every Unicode version (17.0 widened the conjunct rule to Myanmar, Khmer, Tai Tham …), which is why
the runtime carries its own table rather than the host's.

The rules, in the engine's words (`GraphemeBreaker.NextBoundary`; the walk carries three pieces of state — the
regional-indicator run, the emoji-sequence flag, the conjunct state): the text's ends are boundaries; CR + LF is not;
a control, CR or LF breaks before and after itself; the Hangul jamo sequences L+(L|V|LV|LVT), (LV|V)+(V|T),
(LVT|T)+T are not broken; nothing breaks before an Extend or ZWJ, nor before a SpacingMark, nor after a Prepend; a
conjunct — a consonant, then extends/linkers with at least one linker, then a consonant — is one cluster (GB9c); an
Extended_Pictographic, then extends, then a ZWJ, then another pictograph is one emoji sequence (GB11); regional
indicators pair up from the start of their run (GB12/13); everything else is a boundary (GB999). Verified against
**every line of Unicode's `GraphemeBreakTest.txt` 17.0.0** (766 cases; `tests/Cobol.Net.Tests.Unit/TestData/segmentation/`).

An unpaired surrogate is walked as its own code unit (property Other): its own cluster, taking a following mark like
any base — the same robustness rule the collation engine applies.

## 2. Why segmentation matters to collation and normalization

- **Normalization does not move cluster boundaries** — NFC and NFD of a text have the same clusters, canonically
  equivalent one for one (`Segmentation_IsStableUnderNormalization`). Segmentation is therefore *form-independent*:
  count or cut a text before or after normalizing, the answer is the same.
- **A cluster-safe cut keeps a collation prefix.** Cutting a text at a code-unit boundary inside a cluster changes
  what the kept part *is*: `"cafe" + U+0301` cut after four code units keeps `"cafe"` — the accent is gone and the
  kept text now collates equal to `"cafe"`, not after it. `GraphemeBreaker.Truncate(text, n)` never does that
  (`ClusterSafeTruncation_KeepsACollationPrefix`). Any place that shortens text for display, a key prefix, or a
  message should truncate by cluster.
- **Keys are NOT built per cluster** — and this README says so because the integration brief asked for exactly
  that. A collation *contraction* may span a cluster boundary: Thai SARA E + KO KAI are two clusters and one
  contraction (the prevowel reorders); Czech `ch` is two clusters and one letter; and a UTS #10 key is
  level-major (all primaries, then all secondaries …), so concatenating per-cluster keys does not give the
  text's key. `KeysAreNotPerCluster_ContractionsCrossClusterBoundaries` pins both facts. The engine walks the
  whole text (in NFD, which UCA is defined over — an NFC pass first would only be undone) and the key cache
  (`Collation/Cache/`) caches whole-text keys.

## 3. Where it is used

Today: the public API above (hosts, tools, the benchmark harness), the tests, and `Collation/README.md`'s account of
the safe-truncation duty. Inside the COBOL runtime the character positions the standard defines are code units
(alphanumeric/national items), so no COBOL statement segments by cluster — a future `FUNCTION` for
user-perceived length or a display-width facility would call `GraphemeBreaker.Count`.

## 4. Performance

`GraphemeBreaker.Count`/`Enumerate`: one table lookup per code point (a 64 K byte array for the BMP, a binary search
over 1,631 ranges above it) plus the constant-state rule check; the enumerator allocates nothing.
`tests/Cobol.Net.Benchmarks` (`UnicodeBenchmarks`, `Segment-*` categories) measures it against the host's
`StringInfo`; numbers in the harness README.

## 5. Legal

Unicode data only (Unicode License v3, `data/unicode/LICENSE-UNICODE.txt`); UAX #29 is the *specification* the
rules follow, restated in the code's own words. No ISO/IEC text or table anywhere.
