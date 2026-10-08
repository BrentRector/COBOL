# The CLDR locale loader — `Runtime/Collation/CLDR/`

WiseOwl COBOL's collation is CLDR from top to bottom: the **root order** is the derived table generated from CLDR's
`allkeys_CLDR.txt` (`Collation/README.md`), and — since kb/Work PB105 — every **locale order** is derived from the
locale's CLDR collation *rules* by the loader and builder in this folder. The hand-derived `.tailor` files of
`Collation/Tailoring/` are now a site-override format on top; the derivation that produced them (Spanish ñ from
`&N<ñ<<<Ñ`) is done mechanically for all 135 locale files of the pinned release.

| File | What |
|---|---|
| `CldrLocaleData.cs` | the data model: `CldrLocaleData` (identity, default type, the `CldrCollation`s), `CldrCollation` (type/alt/draft, rules text, parsed `Rules`, `Settings`, `Imports`, `Unsupported`), `CldrSettings`, the rule records (`CldrReset`, `CldrRelation`, `CldrImportRule`), `CldrSpecialPosition`, `CldrRelationStrength` |
| `CldrParser.cs` | LDML XML (`<identity>`, `<defaultCollation>`, `<collation type= alt= draft=><cr>`), the JSON mirror (§3), the **rule syntax** (§2) and the UnicodeSet subset of `[suppressContractions]` / `[optimize]` |
| `CldrLocaleLoader.cs` | `Load(name)` / `LoadExact` / `Root`, the parent chain (`Chain`, `ParentOf`), `ResolveCollation(tag)` (which collation of which file a BCP 47 tag means, `-u-` keys included), the sources (§4), `CldrLocaleTag` |
| `CldrTailoringBuilder.cs` | rules → a tailored `CollationTable` + `CollationOptions` (§5) |
| `Data/cldr-collation.zip` + `.manifest.json` | the pack of CLDR release files (embedded resource `Collation/CLDR/Data/cldr-collation.zip`) |

## 1. What CLDR is, and what it gives collation

The **Unicode Common Locale Data Repository** (CLDR, unicode.org/cldr) is the locale data behind ICU, Android, iOS
and most of the industry: per-locale formats, names, and — for collation — the **root collation** (an adjustment of
the DUCET, published as `allkeys_CLDR.txt`) plus, per language, a **tailoring** in the LDML collation syntax
(`common/collation/<locale>.xml`): rules that place strings relative to others, e.g. Spanish `&N<ñ<<<Ñ` (ñ is a
letter right after n; Ñ is its case variant), Danish `&[before 1]ǀ<æ<<<Æ<<ä<<<Ä<ø<<<Ø…<å<<<Å<<<aa<<<Aa<<<AA` with
`[caseFirst upper]`, Russian `[reorder Cyrl]`, Czech `&H<ch<<<cH<<<Ch<<<CH`, Canadian French `[backwards 2]`.
CLDR carries **rules, not weights**; the weights come from the root table, and turning rules into weights is the
builder's job. Files may `[import]` other locales' collations (German search imports root's search and its own
phonebook), define several types (`standard`, `search`, `phonebook`, `traditional`, `pinyin`, `stroke`, `emoji`,
`eor` …) and name a `<defaultCollation>` (Traditional Chinese: `stroke`).

Pinned release: **CLDR release-48-2** (UCA/UCD 17.0.0 — the same as the root table), all 135 files of
`common/collation/`, `common/bcp47/collation.xml` (the `-u-` keys) and `common/supplemental/supplementalData.xml`
(the `<parentLocales>`), under `data/unicode/cldr/`; provenance and hashes in `data/unicode/SOURCES.md` and the pack
manifest. Unicode License v3 throughout.

## 2. The rule syntax the parser reads (UTS #35 Part 5 / ICU)

Everything the pinned release uses (a drift test parses every file with **0 unsupported constructs**):

| Construct | Meaning | Applied |
|---|---|---|
| `&X` | reset: the position of string X (tailored earlier in these rules, else the root's) | yes |
| `&[before 1|2|3]X` | reset to the position just before X at that level | yes |
| `&[first/last tertiary ignorable]`, `…secondary ignorable`, `…primary ignorable`, `[first/last variable]`, `[first/last regular]`, `[first/last implicit]`, `[first/last trailing]` | logical reset positions, read from the table | yes |
| `<`, `<<`, `<<<`, `=` | primary / secondary / tertiary difference / identity, relative to the current position | yes |
| `<<<<` | quaternary difference (Japanese kana) | as identity at levels 1–3; reported |
| `<* abc`, `<<* x-z` (also `<<<*`, `=*`) | one relation per character; `-` = a range | yes |
| `X/Y` | expansion: X's elements followed by Y's | yes |
| `P\|X` | prefix context: X after P | as the contraction P+X (noted) |
| contractions (`ch`, `dzs`, `เ`+consonant …) | multi-code-point strings | yes |
| `'…'`, `''`, `\uXXXX`, `\UXXXXXXXX`, `\x{…}` | quoting and escapes | yes |
| `# …` | comment | yes |
| `[import loc-u-co-type]` | the imported collation's rules inserted here (recursively; cycle-checked) | yes |
| `[strength n]`, `[alternate shifted\|non-ignorable]`, `[maxVariable space\|punct\|symbol\|currency]`, `[caseFirst upper\|lower\|off]`, `[backwards 2]`, `[normalization on\|off]` | settings → `CollationOptions` | yes |
| `[reorder code…]` | script/group reordering (§5) | yes |
| `[suppressContractions [set]]` | remove the root's contractions starting with those code points | yes |
| `[optimize [set]]` | a hint | accepted, ignored |
| `[caseLevel on]`, `[numericOrdering on]`, `[hiraganaQ on]` | not implemented | recorded in `Unsupported` |

Rule white space is Unicode *Pattern_White_Space* (the Arabic files separate rules with LEFT-TO-RIGHT MARKs).

## 3. The JSON form

CLDR publishes no JSON for collation (the `cldr-json` packages omit it, because the rules are ICU syntax), so the
loader accepts a mirror of its own for sites that generate data: an object with `locale` (or
`language`/`script`/`territory`/`variant`), optional `version`, and `collations` — either an object keyed by type
(with an optional `defaultCollation` member) whose values are `{ "rules": "…", "alt": …, "draft": …, "references": … }`
objects or plain rules strings, or an array of `{ "type": "…", "rules": "…" }` objects:

```json
{ "locale": "es", "collations": { "defaultCollation": "standard",
    "standard": { "rules": "&N<ñ<<<Ñ" },
    "traditional": { "rules": "&N<ñ<<<Ñ &C<ch<<<Ch<<<CH &l<ll<<<Ll<<<LL" },
    "search": "[import und-u-co-search] &N<ñ<<<Ñ" } }
```

`CldrParser.ParseJson`; a `.json` file in a site directory is read like an `.xml`.

## 4. Sources and the fallback chain

`CldrLocaleLoader` looks for `<tag>.xml` / `<tag>.json` (CLDR spelling: `de_AT`, `sr_Latn`) in the directory named
by **`COBOL_CLDR_DIR`**, then in **`Collation/CLDR/`** beside the application, then in the **embedded pack**. Parsed
files are cached per process (`ClearCache()` forgets them).

The **chain** of a tag follows CLDR locale inheritance: the tag itself; then its explicit parent when
`supplementalData.xml`'s `<parentLocales>` names one — the `component="collations"` table (yue → zh_Hant, yue_CN →
zh_Hans, sr_Cyrl_ME → sr_ME) and the general table's plain entries (**nb → no**, whose file holds the Norwegian
rules since CLDR 46), but not its `localeRules="nonlikelyScript"` entries (zh_Hant → root, sr_Latn → root), which
LDML reserves for the main component ("not used for components where text is not mixed, such as collations") — so
zh_Hant's collation parent stays zh, where its `stroke` default lives; else the tag with its last subtag dropped;
then `root`. `ResolveCollation(tag)` walks that chain for the type the tag asks for (`-u-co-phonebk` → `phonebook`;
else the most specific file's `<defaultCollation>`; else `standard`); a type no file of the chain defines falls back
to the chain's default type (reported); a locale no file covers is the **root order** — which *is* the CLDR order
for English, German, French, Dutch, Italian, Portuguese and many more (their files define only `search`, or nothing).

BCP 47 `-u-` keys (`CldrLocaleTag`, pinned against `bcp47/collation.xml`): `co` type, `ka` alternate, `kb`
backwards, `kf` caseFirst, `ks` strength, `kv` maxVariable, `kr` reorder codes, `kk` normalization; `kc` caseLevel,
`kn` numeric, `kh` hiraganaQ are parsed and reported unsupported.

## 5. The builder — rules to weights

`CldrTailoringBuilder.Build(selection, name)` (see the class summary for the algorithm):

1. Each weight level of the root table is an ordered **line** of its distinct weights. A reset reads X's elements as
   the current position; a relation of strength N gives the string a copy of the last element with a **new slot
   inserted immediately after** the anchor's on line N and the common weights below it; `=` copies; `[before N]` steps
   back one slot; extensions append; prefixes become contractions; starred relations expand.
2. **Canonical closure**: every precomposed character whose decomposition contains a tailored sequence is re-derived
   from its components (Vietnamese tone-mark rules reach every ả ắ ậ …; discontiguous marks handled per UTS #10
   S2.1.1–S2.1.3), and every tailored precomposed letter also maps its decomposed spelling — so a text orders the same
   however it is spelled.
3. **Numbering**: a slot inserted between two adjacent root weights takes a free value between them (root primaries
   are spaced 16 apart for exactly this); where more slots were inserted than the gap holds, every higher root weight
   is **shifted up** — the table records the root → table `WeightMap`s so a `.tailor` layer (root-scale weights) still
   lands right. Tertiary lines renumber for almost every `<<<` (root tertiaries are dense); it costs a pass over the
   pool.
4. **Reordering**: `[reorder …]` permutes the **reordering groups** (space, punct, symbol, currency, digit, then one
   group per script — read from the table, which the generator derives from CLDR's FractionalUCA markers): the
   special groups stay first unless named; the named codes follow; `others` (or the rest) in root order. The space
   between the last regular script and the Han implicit range — where `&[last regular]<…` puts Chinese pinyin,
   stroke and zhuyin orders — belongs to the Hani tile, so those tailored primaries move with Hani
   (`[reorder Hani Bopo]` puts Chinese characters before Latin, as ICU does).
5. **Case bits**: a tailored string's `ElementCase` is Upper / Lower / Mixed by its letters (`Aa` is Mixed), which
   is what `[caseFirst upper]` (Danish) orders by, ICU-style: Upper < Mixed < Lower.
6. Settings become a `CollationOptions` (`[alternate shifted]` Thai, `[caseFirst upper]` Danish, `[backwards 2]`
   Canadian French, `[strength]`, `[maxVariable]`); `[suppressContractions]` removes root contractions;
   `[import]`s are expanded first (settings: imports', then the importer's own on top).

The output is a `TailoringPlan` and `CollationTable.Rebuild` — the same construction a `.tailor` file goes through,
so there is one table-building mechanism, two front-ends.

**Verified**: 29 locales cross-checked pair-by-pair against the host's ICU (`CldrIcuCrossCheckTests`: es, da, sv,
fr-CA, cs, hu, vi, ru, hr, th, ar, he, tr, pl, lt, fi, is, sk, ro, nb, et, lv, sl, uk, el, ja, ko, zh, de) with zero
disagreements except the pairs a documented CLDR release change explains (Latvian y/ī, changed between CLDR 42 and
48); every locale's `Compare` and keys agree; build times: es < 1 ms, da ~1 ms, vi ~5 ms, zh ~220 ms
(`CldrBenchmarks`).

**Not implemented** (reported per collation in `CldrCollation.Unsupported` / `LocaleInfo.Unsupported`, never
silently dropped): `caseLevel`, `numericOrdering`, `hiraganaQ`; a `<<<<` relation is applied as an identity at
levels 1–3 (Japanese kana length-mark distinctions collapse to equal). Registered as kb/Work PB107.

## 6. Integration with `CollationTable` and `LocaleManager`

`CollationEngine.ResolveLocale(tag)` (cached per tag) = `CldrLocaleLoader.ResolveCollation(tag)` →
`CldrTailoringBuilder.Build` over the root table → then `TailoringRules.ForLocale(baseTag)` (a `.tailor` for the tag
or its language — site override) layered on top via `CollationTable.WithTailoring` → a `ResolvedLocaleCollation`
(table, options, the CLDR selection, the tailoring, `Unsupported`, `Notes`). `CollationEngine.ForLocale`,
`TableForLocale`, `TryGetOrderingTable` (an `ORDER TABLE` literal may name a locale) and `LocaleManager.GetLocale` /
`SetLocale` / `LocaleInfo` all read that one resolution; `LocaleConfig.CldrLocales` / `SupportedLocales` /
`IsSupported` derive from what exists. Every `ALPHABET … IS LOCALE` comparison, SORT/MERGE, indexed key and
STANDARD-COMPARE over a locale ordering table therefore collates by the locale's CLDR rules.

## 7. Legal

CLDR data under the Unicode License v3 (`data/unicode/LICENSE-UNICODE.txt`); UTS #35 Part 5 is the specification
the parser and builder follow, in their own words. Nothing here reads, copies or embeds ISO/IEC 14651 text or its
tables; WiseOwl COBOL's conformance statement is unchanged: *"Implements collation behavior consistent with ISO/IEC 14651
through derived tables and CLDR/UCA data."*
