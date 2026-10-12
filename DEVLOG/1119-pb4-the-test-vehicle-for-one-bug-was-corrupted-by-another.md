## Entry 1119 - 2026-07-29 22:30 PDT - PB4: the test vehicle for one bug was corrupted by another, and the other was silent data loss in VALUE

**HOW IT WAS FOUND, because the provenance is the useful part.** I was building a repro for PB3 (ORD under a
custom collating sequence) and wrote `01 HI PIC X VALUE X"FF".` The program reported `ORD(HI) = 89`. Nothing about
89 fits a collating-sequence theory — and 89 − 1 = 0x58 = **'X'**. The item held the letter X. The VALUE clause
had stored the literal's own SOURCE TEXT and truncated it to the picture.

**So the instrument was broken, not the thing it was measuring** — and PB3's agent report had been taken through
this same distortion, which is why its numbers never reconciled with mine.

**THE DEFECT.** §8.3.3.2 makes a hexadecimal literal one FORM of an alphanumeric literal — "each pair of
hexadecimal digits represents a single character" — so every position that accepts an alphanumeric literal accepts
it. **Five did not**, each failing differently:

    01 B PIC X(2) VALUE X"4142"          ->  [X"]     the source text, truncated to the picture
    01 A PIC X(4) VALUE ALL X"41"        ->  [ALLX]
    05 E OCCURS 2 PIC X(2) VALUE X"4142" ->  as VALUE
    88 IS-AB VALUE X"4142"               ->  the condition never matched
    MOVE ALL X"41" TO M                  ->  parsed, then died at RUN time on 'figurative constant ALLX"41"'
    MOVE X"4142" TO M                    ->  [AB]     correct, all along

**The data division and the procedure division disagreed about what the same literal meant**, and four of the five
were SILENT. A program initializing a field with a hex literal got the source text and no diagnostic.

**⛔ ROOT CAUSE: ONE RULE WRITTEN DOWN TWICE, BOTH COPIES WRONG THE SAME WAY.** `CobolLiteral` carried the
prefix-letter list in `Decode` and again in `IsStringLiteral`, and **neither included `X`**. A hex literal was
therefore simultaneously *not recognised as a literal* and *not decoded as one* — which is precisely why the five
sites failed in five different shapes rather than one: each had grown its own compensation on top of a decoder
that quietly handed back its input unchanged.

That also explains the near-miss. The obvious fix is a `DecodeHex` arm in `ValueInitializer`, where the symptom
is. That would have been the **fifth copy of the dispatch DA3 already found three of** — and it would have fixed
one of five sites. The list is now a single `PrefixLetters` constant behind one `SplitLiteral` helper, and
`Decode`/`IsStringLiteral` are two lines each over it. The last site was
`ExpressionBinder.FigurativeOperand`, whose own comment read "(ALL HEXLIT / NULL stay a later slice)" — while the
grammar had listed `ALL HEXLIT` as an alternative all along. The parser had been ready for years; only the binder
arm was missing.

⚠ **The X-prefix guard is load-bearing and nearly went wrong.** `DecodeHex` returns the empty string for anything
it does not recognise, and `Decode` contracts to return a non-literal unchanged. Delegating on a leading `X`
alone would have turned the ordinary word `XYZ` into `""`. The delegation keys on the full parsed shape, and the
golden pins `XYZ` so a future simplification cannot quietly reintroduce it.

**AND WITH THE INSTRUMENT REPAIRED, PB3 IS HALF REFUTED.** Re-run against the fixed compiler, the ALSO collapse
the report blamed is CORRECT: `ORD("A")` = `ORD("B")` = 1 (§12.3.7 GR L3.6 — ALSO assigns one ordinal position),
`ORD("C")` = 67, `ORD(X"FF")` = **255**, all matching the derivation {A,B} at 1 · 0x00–0x40 at 2–66 · 0x43–0xFF at
67–255. What survives is narrower and real: `ORD(U+0100)` = **257**, so position **256 is occupied by nothing**.
`Ord` is `c < weights.Length ? weights[c] + 1 : c + 1`, and the fallback ignores the collating sequence entirely,
numbering a character one past the table by a different rule than its neighbour.

⛔ I also declined to inherit the report's citation. It cited "§12.3.7 GR7 k3" for unspecified characters taking
"distinct ascending positions with no gap"; **no clause of that content is in the rule catalog.** What the spec
does give is L3.2 — "the order in which the literals appear … specifies, in ascending sequence, the ordinal
number" — and §15.70.4 r1 returning "the ordinal position", which a sequence containing a hole does not have.
The exact value for U+0100 is under-specified (the UTF-16 repertoire is our own documented choice, CA26); that it
cannot leave a gap is not. PB3 stays OPEN, re-scoped, and needs a collating structure spanning the repertoire
rather than a 256-entry array — a data-structure change, not a one-liner.

**GOLDEN** `pb4_hex_literal_value` — all five sites plus the `XYZ` guard, every expected value from §8.3.3.2
arithmetic (X"41" = 'A', X"4142" = "AB").

**GATES.** Unit **963/963**, characterization **33/33**. This changes the CANONICAL literal decoder, so the full
Conformance suite is the gate that matters.

