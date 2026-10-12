## Entry 1920 — 2026-10-07 13:18 PDT — Train 1034: five clusters land (E PB311 the r6 witness, D PB1457 SEARCH ALL key ref-mod, F PB947 SR24 letters, C PB244 cell ODO tail and tables of variable-length elements, B PB2095+PB2096+PB2280 CALL-boundary aliasing); PB2468–PB2469 filed; GAP 156 → 155

Wave 1033's five branches, in the manifest's order E, D, F, C, B (B SPLIT: three of its five notes), one commit each
and this entry in the train commit. Every branch was cut at `37f2b1311`, which is still origin/main, so no rebase.

**E — PB311: the code was right, the witness was wrong.** The note said DATE-TO-YYYYMMDD's r6 check used the sum
argument-2 + argument-3 where §15.23.3 r6 says "the year at the time of execution". The implementer's re-probe showed
the sum is correct: §15.23.1 defines that year ("Argument-3 specifies the year at the time of execution"; the lander
re-ran `cite.py --check 15.23.1` and found the sentence in the clause's general rule), and r5 supplies the clock year
only when argument-3 is omitted, so the reading is §15.25.3 r6's and §15.100.3 r6's. The row's test-ref was the DAY
sibling's line. The fix is a witness: `2002/pb311_date_day_windowing_r6_bounds` pins both bounds on DATE and DAY with
argument-3 far from the clock year (8299/1700 legal; 8300/1700, -401/2100 and 7000/3000 out of window, the last one a
case the clock-keyed reading would accept), plus two clock-pinned unit facts. The witness batch re-recorded
AR-15.23.3-6 and AR-15.25.3-6 (already CONFORMS). PB311 landed.

**D — PB1457: SEARCH ALL Format 2 key operands never screened for a reference modifier.** `WHEN TK(IX)(1:2) = "AB"`
compiled clean at every edition and searched on the whole key; so did an AND data-name-2. §14.9.37.2 prints
data-name-1 / data-name-2 and §8.4.3.3.3's NOTE forbids reference modification wherever data-name-n is printed, but
§14.9.37.3 SR8 requires the subscript, so the shared data-name-n screen could not be asked whole. Its
reference-modifier arm is split out (`DataBinder.ScreenReferenceModifier`, the NOTE worded once in
`ReportReferenceModified`), and `SearchAllFormat2Rules.CheckKeySide` asks it for every key operand: COBOLNET2024. The
sibling sweep covered every procedure-division format that prints data-name-n; the others were already refused or do
not parse. Two negatives (85-2023) and an 85 positive for the still-legal reference-modified sending operand. Batch
re-recorded on the merged tree: SR-8.4.3.3.3-5 PARTIAL → CONFORMS. The implementer's one lead, a condition-name written
with a reference modifier (`IF CN(1:2)`, `WHEN CN(IX)(1:2)`) compiling clean, is filed as **PB2468**.

**F — PB947: SR24 c) paraphrased §3.11.** Letter c) asked `IsGroup && GroupUsage is None`, two of §3.11's four
exclusions, so a variable-length group with a COMP member was refused under c) instead of h), and a strongly-typed
group instance drew a wrong COBOLNET1976 beside COBOLNET1537. Row c now asks `ItemCategory.IsAlphanumericGroup` and
row h `VariableLengthCompatibility.IsVariableLength`, so the letters are disjoint. A new 2023 negative pins letter h,
all 13 `pb488-cond-var-*.err` files now name their letter, and xUnit plus unit pins cover the strong instance.
SR-13.16.3-24 re-witnessed. The implementer's duplication lead (about ten sites restating §8.5.1.12.1 as `IsGroup &&
HasVariableLengthSubordinate`) is filed as **PB2469** for the R1 pass, with the sites re-located on this tree.

**C — PB244 (stays open): two variable-length shapes now compose.** (1) A cell-backed (EXTERNAL) group holding a
dynamic item and an OCCURS DEPENDING table: the table is the window's trailing storage (§13.18.38.3 SR22), carried as
`CellOdoTail` through the `StorageCell` group helpers, the layout's last component, cut to the operand's count
(§13.18.38.4 GR8). (2) A group whose ODO or dynamic-capacity table has variable-length ELEMENTS: a one-way capability
`DataItem.CurrentImageCapable` over the one member law, `VarPartKind.OdoTable`, `CobolTable.ConcatImages`, and
`BoundIntrinsicCall.OverCurrentImage` so FUNCTION LENGTH / BYTE-LENGTH equal the displayed width (A.1 item 57 records
the determination). Found and fixed on the way: an ODO table's element struct took a `__odo` parameter
(`HasOdoBeneath` / `OdoModel.TableUnder`; the intrinsic binder's duplicate deleted). The note's claim that an EXTERNAL
record with a dynamic item in a fixed OCCURS element was "still COBOLNET0899" did not hold: that code is gone and the
shape displays. Two 2014 goldens, two negatives (85, 2002). Open in PB244: a cell-backed table of variable-length
elements, the carrier consumers (MOVE, comparison, CALL, records) of such a group, and an adjudication of the
§8.5.1.12.3 matching rule for a dynamic-capacity table of variable-length elements.

**B — PB2095, PB2096, PB2280 (SPLIT; PB1940 and PB165 stay open).** PB2095: a BY REFERENCE bit item had no storage
area; `CallEmitter.AreaOf` now states it at the bit offset over `BitsPerCharacter` (§14.9.4.3 SR6 byte alignment).
PB2096: a contained program's forward of its container's GLOBAL formal lost the argument area and presence;
`GlobalBridgesOf` bridges the `__lna` area (`CallBridgeKind.ArgumentArea`) and `BoundUnit.InheritedFormals` feeds the
forwarding arms. PB2280: two compatible variable-length groups of different shapes crossed a CALL by aliasing the
carrier whole; `CobolArg.Layout` (int[]) became `CobolArg.Atoms` (`GroupAtom[]`), and the adapters reshape and overlay
as the INVOKE lanes do, with EC-PROGRAM-ARG-MISMATCH for an incompatible run-time pair. One golden each. PB1940 was
re-probed: BY REFERENCE works since PB2087, BY CONTENT (COBOLNET1688) and RETURNING (COBOLNET1736) remain, sited in
the note. PB2281 is extended: the span walk can now fold into Reshape/Overlay.

**The train.** Lander gate (whole population, one leg), run `20261007T200207Z-5f756f`: GREEN — Conformance
10,909/10,909 · Unit 31,734/31,734 · Characterization 35/35. Linux gate GREEN on the train head. semgrep counts equal
before and after (3 / 46 / 2 / 273). The only merge conflict was `tests/conformance/2002/manifest.json` (E and B each
appended entries): both sides kept, 747 → 750. D's and F's inventory hunks were not merged as JSON; their batches were
re-applied with `record_verdicts.py` on the merged tree, and each result matched the implementer's own inventory
change line for line. No diagnostic code was claimed (COBOLNET3005-3019 all returned).
**Oracle:** 64 of 7,430 cases differ from `e68c33651ec0`, every one attributed: 12 ADDED (the goldens above);
B PB2280, 42 CSHARP: the boundary description `System.Array.Empty<int>()` / `int[]` layout literal becomes
`System.Array.Empty<GroupAtom>()` / a `GroupAtom[]` at each group formal adoption and argument (31 corpus programs
and NIST IC103A, IC106A, IC108A, IC112A, IC114A, IC203A, IC207A, IC209A, IC216A, IC227A, IC235A); one of the 31,
pb1009, also gains the contained program's `__lna` argument-area bridges (PB2096); B PB2095, 5 CSHARP: a BY REFERENCE bit item's
argument now carries `Area: new CellPointer(cell, (bitOffset) / 8)` where it carried none (pb240, pb132, pb173, pb46,
w61b_pb1166); C PB244, 5 CSHARP: the cell helpers take the `CellOdoTail` argument (`default` for a group without the
table: pb1026, pb1042 ×2), and two groups holding a dynamic table of variable-length elements gain a `CurrentImage()`
over `__e.CurrentImage()` (pb1411, pb1716). No DIAGNOSTICS difference. The baseline is re-recorded on the train's tree
as `bc1b9318fa82`. **Review** (full-code pass over the five clusters): 1 finding, fixed in C's commit. The comment in
`AcceptDisplayEmitter.EmitDisplay` still described the national-member width divergence PB327 dissolved, and it now
contradicted the A.1 item 57 row C had just edited. No correctness finding; no cluster dropped. GAP 156 → 155.

