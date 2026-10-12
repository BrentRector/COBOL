## Entry 1326 — 2026-08-18 17:25 PDT — PB101 (collation, half 1 of 2): the DERIVED collation engine lands — a CLDR/UCA-generated table, streaming multi-level compare, keys, tailoring files, and 0 violations on the full CLDR conformance test

**The owner's collation guidance arrived** (the item DEVLOG 1325 paused PB64 for): build the ISO/IEC-14651-consistent
collation subsystem LEGALLY — no ISO text or tables — from Unicode CLDR/UCA data as a generated derived table, with a
key builder, an engine, tailoring files, an IR-level comparison hook, runtime components and documentation, and
integrate it into the compiler pipeline. The prompt named placeholder paths (`COBOLSharp/Collation/`, an `IR/OpCode.cs`,
a `Lowering.cs`); this compiler has no separate IR — it binds to a typed bound tree and emits C# through Roslyn — so
the mapping onto the real repository is recorded once, in `kb/Work/PB101.md`, and this entry lands the ENGINE half.
The compiler-integration half (the `CobolCollation` carrier the design's T2 describes, the LOCALE arm of T3, and
STANDARD-COMPARE / ORDER TABLE of T7) follows in the next entry. The owner asked mid-session why not simply use .NET's
`CompareInfo`; the answer is in the README §1 and was then *measured* (below): the host's ICU is the variable a
decade-stable compiler cannot depend on.

**What was built** — `src/Cobol.Net.Runtime/Collation/` (namespace `CobolNet.Runtime.Collation`), documented in its
`README.md`:

- **The generator** `scripts/collation/generate-collation-table.py` over `data/unicode/` — the pinned CLDR release-48-2
  root collation (`allkeys_CLDR.txt`, UCA 17.0.0), the DUCET of the same version (its `@version` cross-check only),
  and UCD 17.0.0 `UnicodeData.txt` / `PropList.txt` / `Blocks.txt`; the Unicode License and a `SOURCES.md` with URLs,
  versions and SHA-256s sit beside them. It emits `Collation/Data/root-collation.bin` (deflate, 228 KB; format
  documented in `CollationTable.cs`) plus a manifest that pins every input and the output by hash. ONE derivation:
  primaries are shifted left 4 bits, because the root primaries are dense (28,070 of 28,335 adjacent pairs differ by 1)
  and a tailoring must be able to insert between two of them (Spanish ñ between n and o). Implicit weights follow
  UTS #10 rev. 53 Table 16 with its 17.0 siniform bases (Tangut FB00, Tangut Components FB01, Nushu FB02, Khitan FB03,
  core Han FB40, other Han FB80, everything else FBC0) — the ranges DERIVED from the UCD, never hand-kept.
- **`CollationTable`** (root + `WithTailoring` → a NEW immutable table; `Lookup`, `GetElements`, Hangul through jamo,
  Table 16 implicit weights, non-starters, canonical decompositions) · **`CollationElementIterator`** (the streaming
  UTS #10 S2 walk: longest-match contractions including the DISCONTIGUOUS S2.1.1–S2.1.3 case, expansions, unpaired
  surrogates ordered as their code unit) · **`Normalizer`** (NFD from the table's OWN data) · **`Collator`** (table ×
  `CollationStrength` 1–5 × `AlternateHandling` NonIgnorable/Shifted; `Compare` streams level 1 for both texts and
  walks level 2 only on a tie — one pass, no allocation, in the common case; `GetKey`) · **`CollationKey`** (per-level
  weight lists, `CompareTo`, `ToByteArray`) · **`CollationEngine`** (the static façade the prompt named: `Compare(a,b)`,
  `Root` = CLDR default, `Standard` = the ISO/IEC 14651-style four-level Shifted ordering STANDARD-COMPARE's default
  table `"ISO 14651_2020_TABLE1"` names, `StandardAtLevel(n)`, `ForLocale(tag)`, `TryGetOrderingTable(name)`) ·
  **`TailoringRules`** (the `.tailor` format — `U+cp primary secondary tertiary [variable]`, contractions and bracketed
  expansions, `@version` guard, `@locale`; disk lookup via `COBOL_COLLATION_DIR` / `Collation/` beside the app, then
  the embedded files; language fallback) · **`Tailoring/en-US.tailor`, `fr-FR.tailor`** (root order — CLDR's own
  statement for both), **`es-ES.tailor`, `es.tailor`** (CLDR `&N<ñ<<<Ñ`, derived to numeric weights; canonical closure
  of a tailored code point is automatic on apply, so `n + U+0303` follows ñ).

**Verification.** 48 unit tests (`CollationTableTests`, `CollationEngineTests`, `CollationTailoringTests`,
`CollationConformanceTests`): the manifest ⇄ data ⇄ table hash drift test; ASCII + Latin-1 explicit; source weights
(scaled); ß/æ expansions; canonical equivalence incl. reordering; the Thai prevowel contraction; Hangul and Han;
Shifted vs NonIgnorable (`"a-b" = "ab"` through level 3 under Standard, `<` at level 4); keys ⇄ compare agreement over
a mixed corpus under four collators; the **ICU cross-check** — the root order agrees with the host's
`CompareInfo.Invariant` on a Latin-1/Greek/Cyrillic corpus; and the **CLDR conformance test files** — deterministic
1-in-25 samples committed, the full files behind `COBOLNET_UCA_CONFORMANCE_DIR`: **206,298 + 227,809 lines, 0
violations** (NON_IGNORABLE and SHIFTED, at Identical strength).

**The finding that changed the design mid-way.** The first cut leaned on `string.Normalize(FormD)` for canonical
reordering. The conformance samples then failed on FOUR lines, all with combining marks new in Unicode 16 (U+1ADB,
U+1AD8, U+10D6A): PowerShell on the same host confirmed `.NET` reorders `1AC2 0334` (Unicode 14) but leaves `1ADB 0334`
untouched — the Windows-bundled ICU predates 16.0. That is precisely the host-dependence the derived table exists to
remove, so NFD is now computed from the table's own UCD 17.0 data (2,081 canonical decompositions, fully expanded
and reordered at generation time; canonical ordering by the table's combining classes at run time), and the samples
and the full files pass. The identical-level tie-break also compares by CODE POINT, not UTF-16 unit (a supplementary
character sorts after U+E000–U+FFFF), which the conformance ordering requires.

**Friction, honestly.** The tool harness decodes `\uXXXX` escapes inside my tool inputs into raw characters — a
`"\0"` became a NUL byte in a test source, `'\U00000300'` a raw combining mark in `Collator.cs`; lone surrogates
survived only because they cannot be encoded. A backslash-free fixer (`scratchpad/fix_escapes.py`) re-escapes such
characters as C# `\U000XXXXX`, and every later escape was written that way. Two of my own test expectations were
wrong and the engine right (acute 0024 sorts before grave 0025; `"a-c" ≈ "ac" > "ab"` under Shifted): both are
now data-driven or corrected.

**Register.** `kb/Work/PB101.md` (HALF: engine landed, integration open) — `work.py next` lists PB101 then PB64.

