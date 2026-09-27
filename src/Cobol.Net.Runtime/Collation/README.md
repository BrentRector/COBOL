# The WiseOwl COBOL collation subsystem — `Cobol.Net.Runtime/Collation/`

> **Conformance statement (owner decision Q4, 2026-08-18, verbatim and never reworded):**
> *"Implements collation behavior consistent with ISO/IEC 14651 through derived tables and CLDR/UCA data."*
>
> ⚖ **Legal posture.** No ISO/IEC 14651 text, table or template is embedded, quoted or read anywhere in this
> subsystem or its generator. Every weight derives from Unicode data (the CLDR root collation and the Unicode
> Character Database, redistributed under the Unicode License — `data/unicode/LICENSE-UNICODE.txt`); ISO/IEC 14651's
> Common Template Table is kept synchronized by its editors with the same Unicode data, which is why a table
> derived from CLDR/UCA behaves consistently with it.

This directory is the multi-level (culturally correct) ordering of text that COBOL reaches through a locale-based
program collating sequence (`ALPHABET … IS LOCALE`), through `FUNCTION STANDARD-COMPARE` with the `ORDER TABLE`
clause, and through every consumer of a collating sequence (relation conditions, `SORT`/`MERGE`, indexed-file
keys, `MAX`/`MIN`, `HIGH-VALUE`/`LOW-VALUE`, `ORD`/`CHAR`). Everything COBOL-specific (trailing-space truncation,
the `EC-…` conditions, the `CobolCollation` carrier the compiler emits) lives *outside* this directory, in the
runtime's COBOL layer; what is here is a self-contained, language-neutral collation engine.

## 1. Why a derived table and not `System.Globalization.CompareInfo`

.NET's `CompareInfo` is UCA-based (ICU) and orders text well — but its answer depends on **which ICU is present at
run time**: the Windows build's bundled `icu.dll`, a Linux distribution's `libicu`, or (under
`InvariantGlobalization=true`) no ICU at all, in which case comparison silently degrades to ordinal. A compiler
whose INDEXED files must stay in key order for a decade and whose `SORT` must produce the same output on every
host cannot let its collating sequence float with the operating system. This was measured, not assumed: on the
development host (Windows 11, .NET 10) the bundled ICU predates Unicode 16 and leaves U+1ADB / U+10D6A unordered
under `string.Normalize`, so the CLDR conformance test fails there through the host normalizer and passes through
the table's own data (§4).

The derived table gives one order everywhere, versioned (`UcaVersion` = the CLDR/UCA data version, "17.0.0"),
regenerated only deliberately, and it exposes what the standard needs and `CompareOptions` cannot express: the
four ordering **levels** individually (`STANDARD-COMPARE`'s argument-4), the ISO/IEC 14651 default treatment of
punctuation (ignored through level 3, weighted at level 4 — UCA "shifted"), tailoring, and per-character weights
for `ORD`/`CHAR`/`HIGH-VALUE`. `CompareInfo` remains a *cross-check oracle* in the tests
(`CollationEngineTests.Root_AgreesWithTheHostIcuRootCollation_OnACorpus`).

## 2. What is here

| File | Role |
|---|---|
| `CollationElement.cs` | `CollationElement(Primary, Secondary, Tertiary, IsVariable, Case)` — one weight triple with its variable flag and CASE BITS (`ElementCase` Lower / Mixed / Upper — what `caseFirst` orders by); the enums `CollationStrength` (Primary=1 … Quaternary=4, Identical=5), `AlternateHandling` (NonIgnorable, Shifted), `CaseFirst` (Off, Upper, Lower), `MaxVariable` (Space, Punct, Symbol, Currency). |
| `CollationTable.cs` | The DERIVED table: code point / contraction → element sequence, non-starters (combining classes), canonical decompositions, UTS #10 Table 16 implicit weights (Han, Tangut, Nushu, Khitan, unassigned), Hangul syllable decomposition, and the CLDR **reordering groups** (`ReorderGroups`: space, punct, symbol, currency, digit, then one contiguous primary range per script — what `[reorder]` permutes and `MaxVariable` reads). `CollationTable.Root` loads the embedded `Data/root-collation.bin` once per process. `Lookup(cp)` (first element), `GetElements(cp)` (whole sequence), `WithTailoring(rules)` (a NEW table; the base is immutable), the root → table `PrimaryMap`/`SecondaryMap`/`TertiaryMap` a renumbered tailored table records (`WeightMap`), and the ONE construction step every tailoring goes through (`Rebuild(TailoringPlan)`). |
| `CollationElementIterator.cs` | The streaming UTS #10 S2 walk: code point decoding, longest-match contractions incl. discontiguous matching (S2.1.1–S2.1.3), expansions, Hangul, implicit weights. Allocation-free on the common path. |
| `Normalizer.cs` | NFD from the table's own data (canonical decomposition + canonical reordering) — applied only when a text holds a combining mark, or (at Identical strength) any decomposable character. |
| `Collator.cs` | `CollationOptions` (strength, alternate, maxVariable, caseFirst, backwards secondaries — the UTS #35 settings the engine implements) and the `Collator`: a configured comparison over one table. `Compare` streams level by level (level 1 for both strings; only on a tie level 2; …; level 2 from the END under backwards secondaries); `GetKey` materializes a `CollationKey`, `GetKeyCached` through the key cache. `Collator.Root` = root table, tertiary, non-ignorable (the CLDR/ICU default). |
| `CollationKey.cs` | The materialized sort key: per-level weight lists (`Primary`, `Secondary`, `Tertiary`, `Quaternary`), `CompareTo`, `ToByteArray()` (a byte image whose ordinal order equals `CompareTo`). |
| `CollationEngine.cs` | The static façade: `Compare(a, b)` (root default), `Root`, `Standard` (the ISO/IEC 14651-style four-level ordering STANDARD-COMPARE's default table `"ISO 14651_2020_TABLE1"` names), `StandardAtLevel(n)`, `For(table, options)`, `ForLocale(tag)`, `ResolveLocale(tag)` (→ `ResolvedLocaleCollation`: the CLDR collation of the tag, then the `.tailor` layer, the settings, what is unsupported), `TableForLocale(tag)`, `GetKey(text, tag)` (cached), `TryGetOrderingTable(name)`. All results are cached and thread-safe. |
| `TailoringRules.cs` | The `.tailor` file format (numeric weights, root scale, the SITE-OVERRIDE layer), loading (disk directories, then embedded resources), locale resolution with language fallback, and `Apply(table)`. |
| `CLDR/` (`README.md`) | **The CLDR locale loader** (kb/Work PB105): every CLDR release file (135 locales, embedded), the LDML/JSON parser, the rule-syntax parser, `CldrLocaleLoader.Load` / `ResolveCollation` (parent chain, `-u-` keys) and `CldrTailoringBuilder` — CLDR rules → a tailored table + settings. The primary source of every locale's order. |
| `Cache/` (`README.md`) | **The collation key cache** (kb/Work PB106): a thread-safe per-collator `CollationKeyCache` (LRU / size-based eviction, `CacheConfig`), what SORT/MERGE and the indexed-file key comparison key their values through. |
| `Locale/` | **Locale selection** (`LocaleManager` / `LocaleInfo` / `LocaleConfig`): select a locale for the run unit; reports the CLDR collation, the `.tailor` layer, the settings, what is unsupported. |
| `CollationRuntime.cs` | Initialization: `Initialize()` (cheap, every run unit — the cache configuration from the environment), `Warmup(tag)` (eager: tables, CLDR, the default locale), `Status`. |
| `../Unicode/README.md` | The sibling **normalization** subsystem (`UnicodeNormalizer`): the public NFD/NFC surface — NFD IS this engine's table-driven `Normalizer` (one NFD), NFC wraps the host. `../Unicode/Segmentation/README.md`: grapheme cluster segmentation (UAX #29) — and why keys are per text, not per cluster. |
| `Tailoring/*.tailor` | The shipped tailorings: `en-US`, `fr-FR` (root order, header-only), `es-ES` and `es` (Spanish ñ — a site override that a drift test keeps in agreement with the CLDR derivation). |
| `Data/root-collation.bin` | The generated table (deflate; ~228 KB). `Data/root-collation.manifest.json` records the UCA version, the SHA-256 of every input and of the output, statistics, and the Table 16 ranges. |
| `../../../scripts/collation/generate-collation-table.py` | The generator. `../../../data/unicode/` holds its pinned inputs and `SOURCES.md` their provenance. |

## 3. The derived table

**Source.** The CLDR root collation (`allkeys_CLDR.txt`, CLDR release-48-2 = UCA 17.0.0) — the DUCET as CLDR
tailors it for its root locale (U+FFFE lowest, U+FFFF highest, and CLDR's grouping of spaces, punctuation,
symbols, currency signs and digits) — plus the UCD 17.0.0 files the algorithm needs (combining classes, canonical
decompositions, `Unified_Ideograph`, block boundaries, assigned ranges).

**Derivation.** Every explicit mapping is taken as-is, with ONE transformation: primaries are shifted left by
`PrimaryShift` (4 bits), because the root primaries are dense (adjacent letters differ by 1) and a tailoring must
be able to place a character strictly *between* two of them (Spanish ñ between n and o). Order is preserved
exactly; secondaries and tertiaries carry the source values; the `*` variable marking becomes `IsVariable`.
`Lookup('a')` therefore reports primary `0x23EC0` for the source `0x23EC`. Everything the source file does not
list is computed the way UTS #10 says: Hangul syllables through their conjoining jamo (§3.12 of the Unicode
Standard); Han / Tangut / Nushu / Khitan ideographs and unassigned code points through Table 16's two-element
implicit weights (`[.AAAA.0020.0002][.BBBB.0000.0000]`, with the 17.0 siniform bases FB00–FB03, core Han FB40,
other Han FB80, everything else FBC0 — the ranges are derived from the UCD by the generator and recorded in the
manifest, never hand-maintained). An unpaired surrogate is walked as its own code unit and takes an implicit
weight, so ill-formed text still orders deterministically; `Collator.IsWellFormed` reports it.

**Format** (`Data/root-collation.bin`, format 1): `"CNCT"`, u32 raw length, raw-deflate payload of — u16 format,
u8 primary shift, two length-prefixed UTF-8 strings (UCA version, source tag), the element pool (u16 primary, u16
secondary, u8 tertiary, u8 flags), the single-code-point mappings (u32 cp, u32 pool offset, u8 count; sorted), the
contractions (u8 n, n × u32 cp, u32 offset, u8 count), the non-starters (u32 cp, u8 ccc), the implicit ranges (u32
first, u32 last, u16 base, u32 subtract, u8 kind), the canonical decompositions (u32 cp, u8 n, n × u32 cp — fully
expanded and canonically ordered). Everything little-endian. `CollationTable.Decode` reads it; the manifest's
`outputSha256` pins it.

**Statistics (17.0.0):** 38,787 single mappings · 974 contractions (2–3 code points) · 41,724 pooled elements ·
968 non-starters · 23 implicit ranges · 2,081 canonical decompositions · 618 KB raw / 228 KB deflated.

## 4. The comparison

`Collator.Compare(a, b)` is UTS #10 S1–S4, streamed:

1. **Normalize when needed.** If a text holds a non-starter, it is decomposed and canonically reordered with the
   table's own data (`Normalizer`) — never with the host's normalizer, for the reason in §1. At Identical strength a
   text is also normalized when it holds any decomposable character. Otherwise it is walked as-is: the CLDR/UCA
   data is canonically closed, so a precomposed character's explicit elements equal its decomposition's.
2. **Produce collation elements** (`CollationElementIterator`): decode code points; try the longest contraction
   starting here (contiguously, then extended over unblocked following non-starters — S2.1.1–S2.1.3, with a
   consumed mark skipped when the cursor reaches it); Hangul → jamo; explicit mapping → its elements; anything else
   → implicit weights.
3. **Compare level by level.** Level 1 walks both texts comparing the non-zero primaries (a proper prefix is less;
   the end sorts before every weight); only on a tie is level 2 walked, then 3, then (under Shifted) 4, then — at
   Identical strength — the NFD code point sequences (by code point, not UTF-16 unit). The common
   unequal-at-primary case is one pass and no allocation.

**Alternate handling** (UTS #10 Table 12): under `NonIgnorable` (the CLDR/ICU default) variable elements —
spaces and punctuation, the CLDR `maxVariable=punct` set the source marks with `*` — keep their primaries; under
`Shifted` (the ISO/IEC 14651 default) they are 0 at levels 1–3 and contribute their primary at level 4, a
primary-ignorable element that follows one is dropped everywhere, and every other element takes the maximum weight
at level 4. So `"a-b"` and `"ab"` are equal through level 3 and differ at level 4 under `CollationEngine.Standard`,
while `"a-b" < "ab"` at level 1 under `Root`. **`MaxVariable`** widens or narrows the variable set by reordering
group (`Space`, `Punct` (default), `Symbol`, `Currency`).

**Strength** truncates the levels considered; `Quaternary` under `NonIgnorable` behaves as `Tertiary` (there is no
fourth level to weigh); `Identical` adds the NFD tie-break, giving a total order over canonically inequivalent
texts (what an INDEXED file needs to keep distinct keys distinct).

**`CaseFirst`** (Danish `[caseFirst upper]`): the element's case bits — Upper / Mixed / Lower, from the DUCET
uppercase tertiaries (0008–000C) for root elements, from the tailored string's letters for CLDR-tailored ones —
are compared BEFORE the tertiary weight (upper-first: Upper < Mixed < Lower; lower-first the reverse), so
`A < a` and `AA < Aa < aa` under Danish. **Backwards secondaries** (Canadian French `[backwards 2]`): the level-2
weights are compared from the END of the texts (`cote < côte < coté < côté`); the identical-prefix skip is not
taken for such collators.

**Keys.** `Collator.GetKey(text)` materializes the same weights into a `CollationKey`; `CompareTo` orders keys as
`Compare` orders texts (a drift test asserts the agreement over a mixed corpus under four collators);
`ToByteArray()` yields a byte image whose ordinal order equals `CompareTo` — for external index structures.

## 5. Tailoring

A locale's order is built in TWO layers over the root table — never a mutation of it (`CollationEngine.ResolveLocale`):

1. **The CLDR collation of the tag** (`CLDR/README.md`): the locale's rules from the pinned CLDR release, found along
   the CLDR parent chain (its file, its parents', root), for the requested type (`de-u-co-phonebk`) or the file's
   default — turned into weights by `CldrTailoringBuilder` (insertions between root weights, renumbering where a gap
   overflows, script reordering, canonical closure, case bits) together with the settings the rules declare
   (`CollationOptions`). This is the primary source: Spanish ñ, Danish æ ø å + caseFirst, Czech ch, Vietnamese tone
   marks, Russian Cyrillic-first, Chinese pinyin … 135 locale files, cross-checked against the host's ICU.
2. **A `.tailor` file** for the tag or its language (`TailoringRules`, format below) — the SITE-OVERRIDE layer:
   numeric weights in the root scale, applied on top through `table.WithTailoring(rules)` (translated through the
   CLDR-tailored table's `WeightMap`s when it renumbered). The shipped `es-ES` / `es` files reproduce the CLDR
   derivation (a drift test keeps them in agreement); `en-US` / `fr-FR` are header-only ("the root order is
   valid"). A site drops an edited copy into `COBOL_COLLATION_DIR` (or `Collation/` / `Collation/Locales/` beside
   the application) to override a weight without touching CLDR data.

Both go through ONE table construction (`CollationTable.Rebuild(TailoringPlan)`); the result is cached per tag.

**Format** — one mapping per line, `#` comments, blank lines ignored, every number HEXADECIMAL, every weight in
the derived scale the table reports (`Lookup`):

```
@version 17.0.0                 # optional — refused (InvalidOperationException) when it differs from the table's UCA version
@locale es-ES                   # optional — the tailoring's name
# code point   primary secondary tertiary [variable]     — one element (the minimal form)
U+00F1         25718 0020 0002                            # ñ: right after n at level 1
U+00D1         25718 0020 0008                            # Ñ: same primary, uppercase tertiary
# a CONTRACTION: several code points, each with its U+ prefix, then the element(s)
U+006E U+0303  25718 0020 0002
# an EXPANSION: bracket each element
U+00E6         [23EC0 0020 0004] [0000 011F 0004] [24530 0020 0004]
```

To place X immediately after Y at the primary level, give X a primary between `Lookup(Y).Primary` and the next
root primary (any of the 15 free values); the shipped `es.tailor` shows the derivation from CLDR's `&N<ñ<<<Ñ`.
An entry REPLACES the whole element sequence of its code point / contraction. **Canonical closure is automatic:**
a tailored code point whose canonical decomposition is a different sequence gets that sequence registered as a
contraction with the same elements, so `ñ` and `n + U+0303` keep collating identically. Errors name the file and
line (`FormatException`); a duplicate mapping is an error, not "last wins".

**Locale lookup** (`TailoringRules.ForLocale`, `CollationEngine.ForLocale/TableForLocale`): `<tag>.tailor` is
searched — the exact tag (`es_ES` → `es-ES`, case-insensitively), then the language subtag — first in the directory
named by the environment variable **`COBOL_COLLATION_DIR`**, then in `Collation/` beside the running application,
then among the tailorings embedded in `Cobol.Net.Runtime.dll` (`Collation/Tailoring/*.tailor`). A locale with no
file collates by the root order, which IS the CLDR order for English, French, German and most European languages
(CLDR's `en.xml`/`fr.xml`: "The root collation order is valid for this language"); `en-US.tailor` and `fr-FR.tailor`
exist so those locales resolve to an explicit, versioned answer and as templates for site-specific overrides.
The "traditional" Spanish variant (ch, ll as letters) is CLDR type `traditional`, not the shipped default.

`ORDER TABLE ordering-name IS literal` names are resolved by `CollationEngine.TryGetOrderingTable`: the standard's
default `"ISO 14651_2020_TABLE1"` (case-insensitive, space/underscore interchangeable) → the root table under the
Standard configuration; a locale tag with a tailoring → that tailored table; a locale tag .NET recognizes → the
root table; anything else → not supported (the COBOL layer raises `EC-ORDER-NOT-SUPPORTED`).

## 6. How the compiler reaches it

The compiler never calls the engine directly. Its collating sequences travel in ONE runtime carrier — the
`CobolCollation` abstract class of `Runtime/Values/Text/` (arms: the native code-unit order, an `ALPHABET`
literal-phrase table, its national twin, and the LOCALE / ordering-table arm over this engine) — and every
comparison consumer (`CobolString.Compare`, `CobolSort`, the indexed-file key comparers, `MAX`/`MIN`, `ORD`/`CHAR`,
`HIGH-VALUE`/`LOW-VALUE`) takes that carrier. Which arm a program gets is decided at bind time from the ALPHABET /
PROGRAM COLLATING SEQUENCE / ORDER TABLE clauses; the locale arm resolves the run-unit's current locale (or the
named one) at each use, as §12.3.7.4 GR7e requires. `docs/rearchitecture/DESIGN-locale-facility.md` §4.4/§4.9 is
the design of that carrier and of its consumers; this README stops at the engine. (Landing state: the carrier (T2)
and the LOCALE arm for the CURRENT-locale `IS LOCALE` phrase with its consumers (T3) landed 2026-08-18 — kb/Work
PB101; the named `IS LOCALE locale-name` form waits for the LOCALE clause (T1); STANDARD-COMPARE / ORDER TABLE (T7)
per the design's §12 table and kb/Work/PB101.md.) The consumers that compare the SAME values many times — SORT/MERGE
key columns and the indexed-file key comparison — key their values once through the **key cache**
(`Cache/README.md`; `CobolCollation.SupportsKeys` / `KeyOf`); a relation condition streams (`Collator.Compare`),
which is cheaper for one comparison. Nothing is initialized eagerly (`CollationRuntime`): the derived tables decode
on first use, and `COBOL_COLLATION_WARMUP` or `CollationRuntime.Warmup()` loads them up front for a
latency-sensitive host.

## 7. Verification

- `CollationTableTests` — the table loads with the manifest's versions/counts; ASCII + Latin-1 are explicit;
  known weights match the source (scaled); expansions, Hangul, Table 16 implicit weights, non-starters; the
  **drift test** that the committed data, table and manifest agree (SHA-256).
- `CollationEngineTests` — the root order (case tertiary, accents secondary, digits before letters, forward
  secondaries), strengths, ß/æ expansions, canonical equivalence incl. reordering, the Thai contraction, Hangul and
  Han, shifted vs non-ignorable, keys ⇄ compare agreement over a corpus, ill-formed input, and the **ICU
  cross-check** of the root order on a Latin-1/Greek/Cyrillic corpus.
- `CollationTailoringTests` — the format, the shipped files, resolution and fallback, Spanish ñ (incl. canonical
  closure), version guard, disk loading, ordering-table names.
- `CollationConformanceTests` — the CLDR conformance test files (`CollationTest_CLDR_NON_IGNORABLE_SHORT.txt`,
  `CollationTest_CLDR_SHIFTED_SHORT.txt`, release-48-2): every consecutive pair non-decreasing at Identical
  strength. The committed 1-in-25 samples run always; the full files run when `COBOLNET_UCA_CONFORMANCE_DIR`
  names their directory. **Full run, 2026-08-18: 206,298 + 227,809 lines, 0 violations.**
- `CldrLocaleLoaderTests` — every pack file parses (0 unsupported constructs), the rule syntax, the JSON mirror,
  the `-u-` keys and the parent chain, and the locale orders (Spanish, German phonebook, Danish, Swedish, Canadian
  French, Russian / Croatian / Arabic / Hebrew reordering, Czech, Hungarian, Vietnamese, Thai, POSIX, Chinese,
  Japanese, Korean), keys ⇄ compare agreement per locale, the `.tailor` ⇄ CLDR agreement for Spanish, ordering-table
  names. `CldrIcuCrossCheckTests` — 29 locales pair-by-pair against the host's ICU (a documented release
  difference allowed).
- `CollationKeyCacheTests` — the cache (§ `Cache/README.md`); `LocaleManagerTests` — selection, CLDR reporting,
  `CollationRuntime` initialization and warm-up; `GraphemeBreakerTests` — segmentation, the full
  `GraphemeBreakTest.txt`, and the interplay with normalization and collation.

## 8. Regenerating

```
python scripts/collation/generate-collation-table.py        # reads data/unicode/, writes Data/root-collation.bin + manifest
dotnet test tests/Cobol.Net.Tests.Unit --filter FullyQualifiedName~Collation
```

```
python scripts/collation/pack-cldr-collation.py             # reads data/unicode/cldr/, writes CLDR/Data/cldr-collation.zip + manifest
python scripts/unicode/generate-grapheme-table.py           # reads data/unicode/, writes ../Unicode/Segmentation/Data/grapheme-break.bin + manifest
```

Bump the pinned inputs by replacing the files under `data/unicode/` (record the new URLs/versions in
`data/unicode/SOURCES.md`), regenerate all three artifacts, re-run the tests — including the full conformance files
of the SAME CLDR tag and the ICU cross-check — and commit data, tables, pack, manifests and any tailoring whose
`@version` changes, together. A `.tailor` file's numeric weights are relative to the table version its `@version`
names; the version guard refuses a stale one. The table format is 2 (reordering groups + case bits); the runtime
still decodes format 1.

## 9. The pipeline at a glance

```
text ──► (combining mark? → NFD, the table's own) ──► collation elements (contractions, expansions, Hangul, implicit)
     ──► level 1 … level 4 weights per CollationOptions (strength · alternate/maxVariable · caseFirst · backwards)
     ──► Compare (streamed) │ GetKey (materialized) ──► CollationKeyCache (SORT/MERGE, INDEXED keys, GetKey API)

locale tag ──► CldrLocaleLoader.ResolveCollation (file chain, -u-co- type, -u- keys)
           ──► CldrTailoringBuilder (rules → table + options)  ──► .tailor layer (site override)
           ──► ResolvedLocaleCollation (cached per tag) ──► LocaleManager / LocaleCollation / ORDER TABLE
```

`../../README.md` (the runtime's text-processing overview) puts the collation, normalization, segmentation, locale,
CLDR and cache pipelines side by side.
