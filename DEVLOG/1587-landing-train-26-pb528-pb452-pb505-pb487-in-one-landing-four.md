## Entry 1587 — 2026-09-09 17:34 PDT — Landing train 26: PB528 + PB452 + PB505 + PB487 in one landing — four clusters, GAP 2643 → 2609, and three composition seams only the one-at-a-time build could see

**PB528 — a Format-1 PICTURE now has a COMPOSITION validator: §13.18.40.6 Table 10 as DATA, symbol order as
ROLE ASSIGNMENT.** ISO §13.18.40.3 SR2 states two obligations and the compiler had only ever answered the
first: character-string-1's symbols "shall be picture symbols" (the membership loop behind COBOLNET0808) *and*
they "shall form an allowable combination", whose allowable combinations "are specified in 13.18.40.6,
Precedence rules". Nothing anywhere read a picture symbol's POSITION, so every composition rule of the clause
was unenforced at once — and several illegal strings did not merely compile, they RAN: `PIC 99.99.99` turned
123456 into `56.00.00`, `PIC ZZ**9` rendered 7 as `  **7` with two different replacement characters in one
item, `PIC +999+` and `PIC $999$` were silently reinterpreted as floating strings, and `PIC S9(5) BLANK WHEN
ZERO` discarded the sign. The new `Binding/PictureComposition.cs` carries Table 10 as a 24×24 table in the
standard's printed order — verified cell-for-cell against the canonical PDF by ruling-line geometry, 0 of 24
rows differing from the transcription — and decides a symbol's row and column by ROLE ASSIGNMENT, searching the
position-ambiguous kinds (fixed currency, non-floating `+`/`-`, `P`, floating currency, `Z`/`*`, floating
`+`/`-`) rather than guessing; that is why SR25, SR26 and SR27 need no code of their own, because they ARE the
empty leading-sign row and the empty trailing-sign/`CR`/`DB` columns, and a blank cell is a prohibition binding
every ordered pair, adjacent or not. Beside it sit SR12 a/b and SR16–SR24 in a fixed most-specific-first order,
so the message names the rule a reader would reach for. The comma/period roles are a PARAMETER, not a constant —
SR13 makes every rule written for the period read for the comma under DECIMAL-POINT IS COMMA and back —
threaded `DataBinder → Analyze → PictureComposition` across four call sites; and ONE `FloatingString` definition
(§13.18.40.5 rule 6 adjacency) is shared by the rules and the geometry. The walk is an O(n) bit-set with a scale
gate, because the naive pair comparison made `PIC X(30000)` 9×10⁸ comparisons. It runs on the fixed-point
Format-1 path BEFORE the geometry derivation, since that derivation reads the symbol multiset and presumes a
well-formed string. New COBOLNET1934 (the named syntax rules) and COBOLNET1935 (the table). The re-probe turned
up a NIST card error rather than a compiler one: `IX110A` writes `PIC P`, which no row of Table 10 admits —
corrected to `PIC 9` in `tests/nist/programs/IX110A.cob` and in the copy inside `IX109A.cob`, `newcob.val`
untouched, NIST still 349/349. Goldens: three negatives (18 + 8 + 3 entries) and three positives, plus
`PictureTable10DriftTests`, which re-parses Table 10 out of the spec on every build and was proved failing
before it passed.

**PB452 — SET Format 15 lands WHOLE; the note deliberately stays OPEN for Format 8.** PB452 covered two SET
formats and only one is done, so `kb/Work/PB452.md` keeps `status: open` with an exact NEXT plan and the residue
is also filed as PB817 — reading this landing as closing PB452 is the one mistake it invites. What landed: the
false non-support claim in `IntrinsicArgumentRules.ClassOfCategory`, which documented its behaviour as
"documented non-support (COBOLNET1564, Annex A.3)" — a real code and a real annex, describing something else —
is gone, and a sibling sweep over every A.3/A.4 claim under `src/` found no second false one. `AlgebraicRanges`
is the ONE extreme-value evaluator, extracted from `IntrinsicBinder.BindAlgebraicFold` and
`ArithmeticModes.IntermediateExtremes` with drift tests, because Format 15's GR32 needs the identical answer and
three copies is where a divergence becomes a wrong answer. Format 15 itself is end to end: six lexer tokens with
`cobol-words.json` reservation gating, `setContentStatement`/`setContentValue`/`setContentSign`,
`SetBinder.BindSetContent` enforcing SR31 (identifier-14 shall reference a NUMERIC data item — numeric-edited is
a different category under §8.5.2.13, so COBOLNET1938 refuses it, which is exactly where this rule is narrower
than the §15.43/§15.58 intrinsics Annex D.32 offers as alternatives), SR31 a)/b) (the SIGN phrase is required
where the signed item's extremes have different absolute values — the two's-complement containers of
§13.18.60.4 GR12 — COBOLNET1939) and SR32 (the three IEEE value words need a STANDARD floating-point usage per
§3.166/§3.167, which FLOAT-SHORT, FLOAT-LONG, FLOAT-EXTENDED, COMP-1 and COMP-2 are not — COBOLNET1940). The
bound node carries the IeeeSpecial CHOICE and the sign, never a C# expression, so a second `ICodeGenBackend`
spells the special its own way. DOC-A.1-176 (the NaN payload) is recorded in CONFORMANCE.md §7.

**PB505 — the Format-2 (table) VALUE over the whole population §13.18.63.3 admits.** ONE staged COBOLNET0899
stood in for SIX syntax rules and refused conforming source, because the screen ran at ENTRY bind — before
`DataItem.Parent` exists — so it could not see the OCCURS chain it had to reason about. A staging in the wrong
PASS is not a staging; it is a wrong answer with a polite message. `ValidateTableValues` split into
`ScreenTableValueLiterals` (the all-formats literal screen, which needs no forest and stays at entry bind) and
the new post-forest pass `ResolveTableValues`, declared in `BindPipeline` after `DynamicResolve` and walking
`OccursChainOf` root-downward: SR18 → COBOLNET1944, SR20/SR21 count → COBOLNET1945, per-dimension ceilings
1586/1587, SR21 ordering as the LEXICOGRAPHIC tuple compare GR12 implies, SR22 both arms 1588, SR23 →
COBOLNET1946. It then resolves §13.18.63.4 GR12–GR16 into `TableValuePlan` (`Subscripts`, `TableValueDim`, and a
`TableValueOdometer` with Step/Resolve/DefaultTo/InitialCapacity, using §8.5.1.9.1's implementor ceiling of
1,000,000 for an unbounded dynamic dimension). `DataItem.ValueAt(subs)` is the ONE reader and all THREE storage
lanes thread an occurrence context to it — the third, the packed BIT carrier, was silently dropping table VALUEs
outright, writing `[0000|0000|0000]` where the source asked for `[1010|0101|1010]`, and the new drift test
`AllStorageLanes_SeedTheSameOccurrences` is what keeps the three honest. Also fixed at the same root: `CloneItem`
and `CopyEntryDescription` did not copy `TableValues`, so a TYPE or SAME AS clone lost every table VALUE it
inherited; and a GROUP entry's table VALUE now deposits GR5's area per occurrence AND rides the group literal
screen. `MultiDimension_StagedGap0899` — a GREEN test that pinned the staging, and therefore read as a
DECISION — is deleted; `ValueFormat2Tests` goes 27 → 51. Plan §0's open-GAPs line loses the Format-2 staging.

**PB487 — the §13.16.2 Format-1 clause list is CLOSED, and a live compiler crash goes with it.**
`dataDescriptionClause` ended in a vendor-extension catch-all, `genericDataClause`, that swallowed any trailing
word sequence and threw it away, so an unrecognized word, the never-implemented ALIGNED clause and the bare
`01 M MESSAGE-TAG.` were all ACCEPTED and DISCARDED — the entry bound with a data description the programmer did
not write. The list is now closed against the general format as rendered from the printed page, with
`unrecognizedDataClause` as an error production placed LAST that the binder refuses BY NAME at every edition and
strictness (COBOLNET1941). ALIGNED became a real clause (lexer token, a `cobol-words.json` row whose reservation
gate is DERIVED, `alignedClause`, a `constructs.json` row with a ParseArm gate, `DataItem.IsAligned`, a
post-forest `CheckAlignedClauses` for §13.18.1.3 SR1 → COBOLNET1942, and §13.18.1.4 GR1/GR2 at the ONE
bit-layout site); MESSAGE-TAG became a real usage arm refused as the DECLINED Annex A.3 item 4 data surface
(COBOLNET1943). The re-architecture is the part that outlives the clauses: §13.16.3 SR12, SR13, SR17 and SR18
each carried a hand-written `||` list of "which clauses were written", each INCOMPLETE — SR17 blind to nine of
fourteen, because a clause processed after the entry is built leaves no decode flag to test — and they now read
ONE `DataClauseKind` set taken from the PARSE TREE, with `DataClauseKindDriftTests` reflecting over the
GENERATED parser in both directions. And `01 M.` followed by a MOVE produced NO diagnostic and an unhandled
NullReferenceException: §13.16.3 SR8's REQUIREMENT half was enforced only for NATIONAL and BIT. It is now
`CheckPictureRequired` (COBOLNET0881) with a FIGURATIVE-ONLY carve-out per SR9 and §7.3.15.4 GR4 k — narrow on
purpose, because the wider carve-out was MEASURED to print `A` for `01 B VALUE "AB".`, and `kb/Work/PB504.md`
gains that shortcut-not-to-take. §8.5.1.6.3's bit-placement rule, written three times, became
`BitLayout.SharesByteWith` + `BitLayout.StrideBits`.

**The train.** Four clusters, brought in ONE AT A TIME with a solution build and that cluster's own drift tests
between each — which is what found the three composition seams no cluster's own gate could have. Every cluster
here edits the subsystem train 25 restructured, and generated artifacts were REGENERATED on the merged tree
rather than merged four ways: `docs/DIAGNOSTICS.md`, `CobolWords.g4` + `CobolLexerWordSet.g.cs` (118 words —
train 25's ACTIVE-CLASS and PB452's six Format-15 words and PB487's ALIGNED/MESSAGE-TAG together) and
`Constructs.g.cs` + `ConstructRegistry.g.cs` (228 rows). Seam one: `UsageFamilies.IsPictureless` did not contain
`Usage.MessageTag`, so a PICTURE beside it would have been accepted — §13.16.3 SR8 names message-tag in its
list (checked mechanically), and `PicturelessUsageSetDriftTests`, which re-reads that sentence out of the spec,
had been holding message-tag open as a FORWARD OBLIGATION by asserting it unmodelled; PB487 discharges it, so
the expected set is now empty. Seam two: train 25's PB389 turned the object-reference descriptor into a tuple
while PB487's diff carried the older spelling as unchanged context — main's arm kept, PB487's MESSAGE-TAG arm
inserted beside it. Seam three: `UsageInheritanceDriftTests` flagged MESSAGE-TAG as a §13.18.60.4 GR1 asymmetry,
the group spelling drawing COBOLNET1724 (§13.18.60.3 SR14) and the elementary one not. Measured, not assumed —
and it is the cell `kb/Work/PB819` already registers for FUNCTION-POINTER, from the identical cause: SR14's arm
B keys on the RESOLVED class, which a usage whose KEYWORD is refused never acquires. PB819 gains the second cell
and the confirmation that its own sibling sweep predicted this one; the drift test's carve-out widened from
"staged" to the situation it always meant, as ONE predicate `KeywordRefused` covering COBOLNET0899 and
COBOLNET1943, and `Sr14PhraseOf`'s stale "MESSAGE-TAG has no Usage member yet" sentence was corrected to match
the arm directly below it. One report claim did not survive verification: PB528's says its 2023 golden was
"registered in the three manifests", and it was registered in the 2023 manifest's PENDING list; the golden was
measured on the merged tree, compiles clean and prints its `.out` byte for byte, so it moved to `enabled` — the
wave that lands a feature is what enables its golden. Eleven diagnostic codes were CLAIMED, each inside the
range its cluster was assigned and with no collision across them: 1934/1935 (PB528), 1938/1939/1940 (PB452),
1944/1945/1946 (PB505), 1941/1942/1943 (PB487); 1936 and 1937 were reserved, never used, and return to the pool.
All seven verdict batches were re-applied on the merged tree in manifest order as sequential runs — PB505's
three and PB452's two carry duplicate rule-ids across files — and every one replayed to the content the cluster
patches already carried, so **GAP 2643 → 2609, exactly the −34 predicted (−10 −6 −6 −12)** with the denominator
unchanged at 4,348. (The train measured −34 from 2659; the PB451/PB496 golden round landed −16 while
this train was gating, the train was rebased onto it, and the merged inventory was RE-MEASURED rather than
assumed — the two sets of rows are disjoint.) No cluster was dropped. PB528, PB505 and PB487 flip to `landed`; PB452 stays `open` by
design. Semgrep went DOWN and the improvement was locked in (`cobolnet-raw-diagnostic-code-literal` 423 → 418).

