## Entry 2127 — 2026-10-16 18:12 PDT — PB3293: a CLDR reset, extension or prefix string is read by the one S2.1 walk over the rules built so far

**Mechanism.** `CldrTailoringBuilder.Builder.Lookup` read the current elements of a reset string (`&X`), an extension
(`x/y`) and a prefix (`p|x`) by whole key: an exact tailored entry, else the root's `GetElements(ReadOnlySpan<int>)`,
which took an exact whole-key contraction or concatenated per code point. It never walked the mappings the rules had
built so far and never normalized the string. ICU's CollationBuilder reads all three as `getCEs` of the NFD string
over the data built so far (`addReset`, `addRelation`), so every locale whose rules reset on a string HOLDING an
earlier-tailored character or contraction was derived differently from ICU.

**Re-probe.** A temporary probe beside the builder compared the two reads over all 160 CLDR release-48-2 tailorings:
2,469 reset strings differ (sa/kn/bn traditional 1,714, my 254, zh stroke/pinyin/zhuyin 356, ar 87, dz 17, root emoji
15, km 6, eor 8, cu 5, th 2, and one each in ko search, ja, ja unihan, ja private-kana, bo), 20 of them not in NFD; no
CLDR extension or prefix string differs. The host's ICU confirms the wrong answer: zh-CN `&'(一)'<<<㈠` gave ㈠ the
implicit weight of U+4E00 instead of its pinyin one (ours ㈠ < ㈦ < ㈢, ICU ㈦ < ㈢ < ㈠), and ar-SA `&ئا=ﯪ` read the
root's ئ instead of the tailored one (ours ﯪ ≠ ئا, ICU equal).

**Fix.** The UTS #10 S2.1 walk (code point cursor, longest contiguous contraction, discontiguous extension over
unblocked non-starters) moves out of `CollationElementIterator` into `ContractionWalker`, which the iterator now
drives for S2.1 while it keeps S2.2 (fetching elements). The builder's new `BuiltSoFar` holds the mappings built so
far in the walk's shape: each tailored string keyed by its NFD, a first code point's root contractions copied in on
first use (ICU's `copyFromBaseCE32`), the `[suppressContractions]` ones never; `ElementsOf` NFDs a string and walks
it with the same `ContractionWalker`. `CollationTable.WithContraction` is the one replace-or-insert-longest-first rule
both contraction lists use, and `CollationTable.MapsItsNfdSpelling` the one rule for which keys map their NFD
spelling (not a key led by a Hangul syllable, which the engine never meets as a single): Rebuild's closure and
`BuiltSoFar.Map` both read it, so a reset never reads a jamo mapping the built table does not have (found in
self-review; its witness was seen red without it). Deleted: `Builder.Lookup`, `CollationTable.GetElements(ReadOnlySpan<int>)` (public, no
other caller; `RuntimeAbiPinDriftTests` re-pinned alone, major 6 kept, because no module this compiler emits calls
it) and the write-only `_skipped` counter. The CLDR and collation READMEs describe the walk.

**Witnesses.** `CldrLocaleLoaderTests.ResetExtensionAndPrefix_WalkTheirNfdOverTheRulesBuiltSoFar`: zh pinyin, ar, and
one synthetic rule set per arm (a reset holding a tailored contraction, an extension and a prefix holding one, a
reset not in NFD, the discontiguous match, a Hangul-led key); each case was seen red on the old read (the Hangul
case without the shared rule). `CldrIcuCrossCheckTests`
gains the zh-CN and ar-SA pairs (red on the old read: 4 and 3 disagreements with the host ICU). Golden
`tests/conformance/2002/pb3293_locale_reset_walk.cob` (PB3293ZH, LOCALE "zh-u-co-pinyin"): ㈦ < ㈢ < ㈠, derived
from zh.xml's pinyin order and its `&'(一)'<<<㈠` resets. No negative: the rule is the runtime's CLDR derivation, not an
edition-gated construct.

**Rows.** None: PB3293 claimed no inventory row (the ISO rows, §12.3.7.4 GR7 e and §8.8.4.2.11, defer the order to
the locale); `closes_rows: []` with its reason.

