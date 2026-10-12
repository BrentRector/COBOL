## Entry 1924 — 2026-10-07 17:36 PDT — Train 1034 (wave 1034: C, E, D, A, F): BASECONVERT bases, character positions, property receivers per receiver, variable-length and slot-carried group areas, pointer member images

Five clusters of wave 1034 in one landing, in the manifest's order C, E, D, A, F
(`E:\COBOL-coord\scratch\train1034-manifest.json`), one commit each on origin/main `4dc41d0e3`. Every cluster applied
cleanly onto the train; no inventory row moved, no diagnostic code was claimed, and none was dropped.

**C — PB2079 + PB1931.** ISO §15.12.3 rule 1 says two things on two axes: argument-1's USAGE (display or national) and
"Argument-2 and argument-3 shall be positive nonzero numeric integer literals or data items", an ordinary class rule that
Table 21 prints as Int2, Int3. BASECONVERT sat in `DeliberatelyUnscreened`, so an alphanumeric base printed `R=26`, a USAGE
POINTER base crashed Roslyn (CS1503) and an ADDRESS OF base aborted at run time. Its `Verified` schema is now
`[UnscreenedKind, 'i', 'i']` with `DataItemOrLiteralOnly` on both bases; `UnscreenedKind` is a DECLARED position whose rule
is not a class (argument-1's usage stays `CheckBaseConvertArgs`'), pinned by two new drift tests. CONCAT's stale row in
`DeliberatelyUnscreened` (it was in both tables) is deleted. The `--permissive` coercion is offered only for an operand that
holds characters (`ReportClass` over `IsDigitDecodable`, the §8.8.1.1 screen's own predicate), so `BASECONVERT("1A" P 10)`
and `ABS(P)` under `--permissive` are errors instead of Roslyn crashes. Determination (small): "literals or data items" is
read literally, so a nested function or expression as a base is refused, as FIND-STRING's §15.37.3 r3 already is. PB1931
(ADDRESS OF a BY REFERENCE formal) no longer reproduces on any arm the implementer rebuilt — PB2087 and PB2089 put the formal
on the argument's cell — and is discharged with the witness golden `2002/pb1931_address_of_by_reference_formal` (CALL,
INVOKE and FUNCTION formals). Goldens: `2023/pb2079_baseconvert_data_item_bases`; four `pb2079-baseconvert-*-base` negatives
(COBOLNET1627); `pb59-baseconvert-noninteger-base` now expects COBOLNET1627.

**E — PB1966 + PB1914.** Reference format is described in character positions (§6.1 1) a)) whose meaning the implementor
specifies (§6.1 1) c)); DOC-A.1-157 said one UTF-16 unit, so a line with N supplementary-plane letters (Adlam, Osage: Annex
B extended letters, §8.1.3.2) put margin R N positions early and legal source ending at position 72 failed. Determination: a
position is one CHARACTER (code point); rejected: the UTF-16 unit (the bug) and the grapheme (a combining mark is a
character, as §8.1.3.2 GR4 c) counts a word's length). `CharacterPositions` is the one definition and `FixedFormLine` the one
reader of a fixed line's columns; `ReferenceFormatProcessor`'s column constants and `ProgramTextArea` are deleted and every
caller changed (the Auto detector, the free-form 255 limit, tab stops, every diagnostic column), held by
`PhysicalLinesDriftTests.NoPipelineStage_ReadsAColumnAsAStringIndex`. PB1914: a `D` line between the parts of a continued
literal cut the literal; `LogicalLineBuilder.EmitDebugging` now reads a debugging line like source (its literal state
tracked, a blank one discarded) and passes it over like a comment line while the line before it awaits a continuation
(§6.3.5 2); GnuCOBOL 3.2.0 agrees without `-fdebugging-line`). Found on probing and fixed with it: a `D` line that itself
opens a continued literal lost the literal. WITH DEBUGGING MODE plus an interrupting `D` line is still accepted silently
where GnuCOBOL rejects it: docs/CONFORMANCE.md D-DEBUG, and the lander filed it as kb/Work/PB2490. Goldens:
`2023/pb1966_supplementary_letters_margin_r`, negative `pb1966-supplementary-letters-past-margin-r` (guards margin R
outward), `85/pb1914_debugging_line_between_literal_parts`, `85/pb1914_debugging_line_continued_under_clause`.

**D — PB2073 + PB2078.** PB2073: `CALL "P" USING BAL OF FUNCTION F (7)` was refused COBOLNET1677 because the property
probe could not see a function-identifier's object; a `DescribedByFunction` arm answers with the function's RETURNING item
(§8.4.3.2.4 GR1) through the new `IntrinsicBinder.ReturningItemOf`, and the §14.9.4.3 SR20 omission of BY CONTENT now
holds for it on CALL and INVOKE. PB2078: a receiving object property was fetched at the start of its statement and stored at
its end, so `ADD 1 TO I, BAL OF AR(I)` read AR(1) and set AR(2) and `ADD 1 TO BAL OF D, BAL OF D` gave 101 where §14.7.7
4) b) ("Item identification for the receiving data items is done as each data item is accessed") reads 102. The binder of
a statement that stores its receivers one at a time CLAIMS its property receivers (`DataBinder.OoClaimInterleavedReceiver`:
`ExpressionBinder.ReceiverOf`, DIVIDE REMAINDER, `MoveBinder.Bind`), `OoWrapPropertyOps` gives each claimed op a
`ReceiverBracket` under `BoundReceiverBrackets`, and `ReceiverBracketEmitter.Receive` emits the GET before and the SET after
THAT receiver's store (the SET skipped when its own store raised a size error; an unplaced bracket fails the compile).
PB2078 stays OPEN with its residue written in: ACCEPT, INITIALIZE, STRING/UNSTRING, INSPECT, SET, READ/RETURN INTO and
CALL/INVOKE RETURNING keep the statement-level GET/SET and the COBOLNET0899 refusal (`READ f INTO P NOT AT END DISPLAY P`
reads P before the SET). Goldens: `2023/pb2073_property_of_function_by_content_omitted`, negative
`pb2073-property-of-function-not-object`, `2002/pb2078_property_receivers_in_turn`, negative
`pb2078-property-receiver-selected-by-value-accept` (replaces `pb1425-property-receiver-selected-by-value`);
`ReceiverBracketDriftTests`.

**A — PB2094 + PB1940 (SPLIT: PB165 stays open).** PB2094: a variable-length group formal did not share its argument's
storage (§14.2.3 GR8: "operates as if the formal parameter occupies the same storage area as the argument"); `V V` with a
dynamic-length item printed `AFTER=H/ABC/`. A variable-length group is now a cell area like any group: the area states its
first component ordinal (`CellPointer.DynBase`) and its §8.5.1.12 atoms (`Shape`), a formal is laid over it only when
`GroupCompatibility.LaysOver` (same storage, or a prefix), else it takes its own seeded fresh cell, and references through a
pointer-routed class number components from `CobolPtr.DynBaseOf`. The note's ODO repro no longer reproduced (ODO storage is
fixed and has been an area since PB2087). PB1940: a strongly-typed group with an object-reference or pointer leaf was refused
BY CONTENT (COBOLNET1688) and as RETURNING (COBOLNET1736); on the same cell carrier BY CONTENT is now a detached area copy
(`ContentRecord` over `StorageCell.CopyArea`, GR9), RETURNING a store into the receiver's area (§14.6.5), and a MOVE between
two such groups a leaf vector; the function-RETURNING sibling is fixed with it. Gate-found sibling: `RedefViewPlace
.ImageCapable` lacked the PB189 element law. PB165's prototype-lane run-time check was re-probed and sited in its note (reuse
`ActivationDescription`, do not extend `BoundaryItem`) and left for a finisher. Goldens:
`2014/pb2094_vlg_formal_shares_argument_area`, `2002/pb1940_strong_objref_group_content_returning`; the negative
`pb2087-strong-pointer-group-by-content` is retired (legal source that now runs; its case is in the PB1940 golden).

**F — PB1071 + PB927. ⚠ This overturns the pin of PB231 in `2002/pb231_based_pointer_leaf` (owner visibility requested by
PB1071).** A pointer MEMBER of a shared storage area kept its value in the cell's managed slot while its 8 positions stayed
PB231's space fill, so a NULL member read as spaces and `SET P UP BY 3` moved a BINARY-DOUBLE window over it by +0000.
§14.9.3.4 GR9 ("data items of class object or class pointer in the allocated storage are initialized to null") and §14.9.39.4
GR20 ("incremented … by the number of bytes") meet DOC-A.1-216 (PB970), which gives NULL the zero image. Determination: the
member's positions hold `PointerImage`'s image in every lane; rejected: spaces in the cells while the CALL boundary shows the
image, which is two arms for one storage. `PointerImage` stays the only author, written by `CobolPtr.SlotWrite<T>` beside
every slot store, seeded by `CobolPtr.Allocate(…, nullImageAt)` at every OCCURS occurrence and by the cell seeds, and
rendered by the record struct's one-way `AsImage`. PB927's named file was already fixed (PB465); `NulByteDriftTests` now
guards the mechanism. Goldens: `2002/pb1071_pointer_member_image`, negative `pb1071-pointer-member-image-85`; re-derived for
the image: `pb231_based_pointer_leaf`, `pb244_pointer_group_transfer`, both `pb1901` pointer goldens and two
`TierCRejectionTests` assertions (whose method names still say PlaceholderPositions: kb/Work/PB2491, which also corrects the
report's claim that no witness retirement exists — `record_verdicts` has `retire-witnesses`).

**The train.** Gate (`build-local -Mode lander`, the whole population in one leg) GREEN on the merged tree, then again after
the review fix: run `20261008T002250Z-445b18`, Conformance 10,933/10,933 · Unit 32,560/32,560 · Characterization 35/35.
Linux gate GREEN (hooks, unit, characterization, conformance, guard). semgrep verify equal to baseline (BigInteger 46,
decimal 2, raw codes 273, bound-node text 3). CI audits run locally: code and doc citations 0 findings, evidence
supersession 0, witness loss GREEN vs the base. Inventory GAP 153 → 153 (no row claimed or closed). REVIEW: 1 finding, 1
fixed in the train, 0 clusters dropped — C's `--permissive` fix covered the class screen but CONCAT's §15.18.3 r2/r3 arm still
reported through the coercible path, so `CONCAT("A" PTR)` printed "--permissive accepts it as a coercion extension" beside the
class error that refuses it; the arm now reports through `ReportClass`, and `IntrinsicPointerArgumentPermissiveTests` holds
both fixtures refused under `--permissive` with no coercion text (and the alphanumeric control still warning). The composition
also cleared one of F's leads: `MOVE R TO R2` between two BASED typed records with a pointer leaf (Tier-C abort when F
reported it) prints the right values on the train, by A's leaf-vector MOVE. **Oracle** (`compare_oracle.py`, L11):
DIFFERENT, 152 of 7,453 vs `90babefc36a7`, attributed by capturing at each cluster checkpoint. C+E+D: 17, all goldens ADDED or
REMOVED plus two diagnostic streams of C's (`pb58-concat-pointer` loses the `--permissive` offer for a pointer;
`pb59-baseconvert-noninteger-base` is now the schema's COBOLNET1627); E and D change no existing program's C#. A: 94 — 67
only the two new `CobolArgAdapt.Area` arguments (`, null, null` for every non-variable-length formal, neutral), 24 programs
of two kinds — a reference through a pointer-routed formal gains `CobolPtr.DynBaseOf` as its component-ordinal base (NIST
IC108A and IC209A among them), and a variable-length group passed BY REFERENCE is claimed onto a cell (its StorageCell
member and bridge emitted) — plus its 2 goldens and 1 retired negative. F: 55 — typed
`SlotWrite<T>` stores, `PointerImage.NullImage` cell seeds and `Allocate` null-image offsets, record-struct `AsImage` rendering
`PointerImage.Of`, an ALLOCATE comment gaining GR9, and its re-derived goldens' own sources. Every class is its cluster's fix;
none is unexplained. Baseline re-recorded as `436a64b50ac0`. Leads filed: PB2489 (`INVOKE BASE "NEW"` fails the backend
compile, CS0117 on `BASE__FACTORY.__Instance`; §16.1), PB2490, PB2491; PB2492 and PB2493 returned unused.

