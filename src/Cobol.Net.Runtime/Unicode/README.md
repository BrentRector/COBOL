# The WiseOwl COBOL Unicode normalization subsystem — `Cobol.Net.Runtime/Unicode/`

The public, stable place to ask "put this text into a canonical form" — `UnicodeNormalizer.Normalize`,
`IsNormalized`, `CompareNormalized`, `IsNfcAvailable`, in the namespace `CobolNet.Runtime.Unicode`.

It is a THIN, deliberate surface over work that already exists: **NFD is the collation engine's own table-driven
decomposition** (`../Collation/Normalizer.cs`), not a second implementation of it. **NFC** is the host's
composition, wrapped so that a host without a normalizer degrades to the identity instead of throwing.

## 1. Why normalization comes before comparison

The same text can be spelled in more than one sequence of code points, and Unicode calls the spellings
*canonically equivalent* — they are the same characters, and any process that treats them differently is wrong:

| One text | Spelling A | Spelling B |
|---|---|---|
| é | U+00E9 (one precomposed code point) | U+0065 "e" + U+0301 COMBINING ACUTE ACCENT |
| ệ | U+1EC7 | U+0065 + U+0323 + U+0302 — and also U+00EA "ê" + U+0323, and U+1EB9 + U+0302 |
| a with a dot below and an acute | "a" + U+0323 + U+0301 | "a" + U+0301 + U+0323 — the marks in the other order |
| 각 | U+AC01 (one Hangul syllable) | U+1100 + U+1161 + U+11A8 (its conjoining jamo) |

Three distinct sources of variation: **precomposition** (row 1–2), **the order of the combining marks** (row 3 —
marks that do not interact typographically may be typed in either order), and **algorithmic composition** (row 4 —
Hangul syllables are composed arithmetically from jamo).

A text arriving from a file, a terminal, an operating system or another program has no guaranteed spelling:
macOS has long stored file names decomposed, most of the web is composed, and a keyboard produces whichever its
layout emits. So `=`, `<`, a SORT, an INDEXED file key and a hash over raw code units all answer questions about
the SPELLING when the program is asking about the TEXT. Normalization is the step that removes the difference: both spellings of a
text have the same NFD and the same NFC, so a comparison performed after normalizing compares characters.

## 2. NFC and NFD

Both are *canonical* forms — same equivalence class, no information gained or lost, and the mapping is idempotent
(normalizing an already-normalized text returns it unchanged):

- **NFD — canonical decomposition.** Replace every character by its full canonical decomposition (recursively:
  U+1EC7 → U+0065 U+0323 U+0302), decompose Hangul syllables into their jamo, then put every run of combining
  marks into a fixed order by their canonical combining class (the Unicode Standard's Canonical Ordering
  Algorithm, §3.11). The result is the longest canonical spelling — and the useful one, because two equivalent
  texts become *identical code point sequences*.
- **NFC — canonical composition.** Decompose first, then recompose the pairs Unicode says may be recomposed (the
  composition mappings, minus the `Full_Composition_Exclusion` set — which is why NFC is not simply "the inverse
  of NFD": a few characters decompose but must never be put back together). The result is the shortest canonical
  spelling, and the interchange form most other systems expect.

Deliberately absent: **NFKC / NFKD**, the *compatibility* forms. They fold distinctions that carry meaning — "ﬁ"
becomes "fi", a superscript ² becomes "2", a full-width character becomes its ASCII twin — which changes the text.
Nothing in a compiler's collating, key-building or comparison path is allowed to do that, so this subsystem does
not offer it.

## 3. How this sits on the collation engine — ONE NFD

`../Collation/README.md` is the engine. What matters here:

- The engine computes NFD from **the derived collation table's own data** — the canonical decomposition mappings
  and combining classes baked into `Collation/Data/root-collation.bin` — never from the host. The reason is
  measured, not hypothetical: the development host's bundled ICU predates Unicode 16 and leaves characters
  unordered that the table orders (`Collation/README.md` §1/§4). A compiler whose INDEXED files must stay in key
  order for a decade cannot let canonical equivalence float with the operating system.
- `UnicodeNormalizer.Normalize(text, NFD)` therefore calls straight into `Collation.Normalizer.ToNfd(text,
  CollationTable.Root)`. **It adds no decomposition data and no second algorithm** — if the two ever disagreed,
  a text could compare equal under the engine and unequal after being "normalized", which is exactly the class of
  bug this project treats as architectural. One rule, one place.
- The fast path is the engine's own predicate, `Collation.Normalizer.NeedsNfd(text, table, forIdentical: true)`:
  false means the text holds no decomposable code point, no Hangul syllable and no combining mark, so it IS its
  own NFD and is returned **by reference**, unallocated. Most COBOL data is ASCII, so this is the usual case.

**When does the engine normalize by itself, and when not?** `Collator.Compare` decomposes a text when it holds a
combining mark (canonical reordering may apply, and a precomposed base must decompose so its marks take part), and
— at `CollationStrength.Identical`, where the tie-break compares NFD code point sequences — whenever it holds any
decomposable character. Otherwise it walks the text as it is, which is correct because the CLDR/UCA data is
canonically closed: a precomposed character's collation elements are by construction those of its decomposition.

So **canonical equivalence is already handled**: `CollationEngine.Compare` of the precomposed é (U+00E9) against
"e" + U+0301 is 0 without any help from this subsystem. `CompareNormalized` exists for two other reasons: (a) a
caller whose semantics are stated in NFC can get an NFC-normalized comparison, and (b) the normalization becomes
an EXPLICIT, visible step —
the text the comparison saw is a text the caller can print, log, hash or store. It normalizes both operands and
then delegates to `CollationEngine.Compare`, and it returns the same sign as comparing the unnormalized texts for
every canonically equivalent pair (a test asserts this over a corpus).

## 4. The host dependence of NFC, and the invariant-mode fallback

NFD is host-independent; **NFC is not**. Composition needs data the derived table does not carry — the canonical
composition pairs and the `Full_Composition_Exclusion` property — so `Normalize(text, NFC)` calls
`string.Normalize(NormalizationForm.FormC)` and inherits the host's Unicode version. Two consequences:

- A character added to Unicode after the host's ICU was built may decompose here (table data, 17.0.0 — see
  `UnicodeNormalizer.NfdUnicodeVersion`) and not compose there. The tests' NFD-versus-.NET cross-check counts
  exactly those characters instead of failing on them.
- On a host built with `InvariantGlobalization=true` there is **no normalizer at all**: .NET's normalization APIs
  throw for any non-ASCII text. `UnicodeNormalizer.IsNfcAvailable` probes this once per process (does
  "e" + U+0301 compose to U+00E9?); when it is false, `Normalize(text, NFC)` returns the text unchanged and
  `IsNormalized(text, NFC)` answers true — the identity, never an exception. Ill-formed UTF-16 (an unpaired
  surrogate) is likewise returned unchanged, matching NFD, which passes such a code unit through as itself.

**This asymmetry is acceptable** because nothing in the compiler or the runtime needs NFC to be correct: collation,
sort keys, indexed-file keys and canonical equivalence are all decided in NFD from the table's own data. NFC is
offered for callers handing text to the outside world in its composed spelling.

**Upgrade path (the follow-up, when NFC must become host-independent too):** teach
`scripts/collation/generate-collation-table.py` to emit the **canonical composition pairs plus the
`Full_Composition_Exclusion` set** into `root-collation.bin` (both derive from the UCD files already pinned under
`data/unicode/`), add the composition step beside `Collation/Normalizer.ToNfd`, and route `Normalize(…, NFC)`
through it. `IsNfcAvailable` then becomes permanently true and the host's Unicode version stops mattering
anywhere in this runtime.

## 5. What is here

| File | Role |
|---|---|
| `UnicodeNormalizationForm.cs` | `NFC` / `NFD` — the two canonical forms this subsystem offers. |
| `UnicodeNormalizer.cs` | `Normalize`, `IsNormalized`, `CompareNormalized`, `IsNfcAvailable`, `NfdUnicodeVersion`. Static, thread-safe, no state beyond the once-probed NFC flag and the immutable collation table. |

## 6. Verification

`tests/Cobol.Net.Tests.Unit/Unicode/UnicodeNormalizerTests.cs`: the worked examples of §1 in both directions;
canonical reordering; Hangul syllable ⇄ jamo; the by-reference fast path; idempotence; `IsNormalized`; null and
ill-formed input; `CompareNormalized` agreeing in sign with `CollationEngine.Compare` over a corpus of
canonically equivalent and inequivalent pairs, in both forms; and the **cross-check** that our NFD equals .NET's
for every code point the table calls decomposable (0–0x10FFFF, 2,081 of them), tolerating a difference only where
the host does not know the character decomposes at all — those are counted and reported. The NFC tests are inert,
by design, on a host where `IsNfcAvailable` is false.

## 7. The sibling: grapheme cluster segmentation

`Segmentation/` (README there; kb/Work PB104) is the third text subsystem — UAX #29 extended grapheme clusters from
WiseOwl COBOL's own derived property table. Normalization never moves a cluster boundary (NFC and NFD of a text have the
same clusters, canonically equivalent one for one — `GraphemeBreakerTests.Segmentation_IsStableUnderNormalization`),
so a text may be counted or cut by cluster before or after normalizing with the same result; and a cluster-safe cut
keeps a collation prefix where a code-unit cut can lose an accent. `../README.md` puts the three pipelines side by side.
