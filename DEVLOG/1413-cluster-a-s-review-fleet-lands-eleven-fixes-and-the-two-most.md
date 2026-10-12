## Entry 1413 — 2026-08-31 06:07 PDT — Cluster A's review fleet lands: eleven fixes, and the two most valuable came from a probe that refuted the diff and a test that was finally allowed to fail

The burn-down cluster A implementation (PB173 + PB177 + PB178) was gated green before the review fleet ran.
Twenty-four confirmed findings came back, each with refuter corrections binding over the finder's original. This
entry is the fix pass. Every code-read finding marked PROBE was measured before it was touched, and two of those
probes changed what the fix had to be.

### The probe that refuted the diff's own claim

The change set's central non-obvious justification read: *"Rejecting the CLASS makes that path structurally
unreachable rather than merely unreached because a diagnostic happened to stop the compile."* Its regression test,
`Sr17RejectedClass_NeverYieldsADisjointDynamicStringStorage`, asserted exactly that.

The test's `Bind` helper runs `DataBinder.Bind(program)` and nothing else. `DataItem.Storage` has exactly two
writers, both inside `StorageFormPass`, which sits in `BindPipeline.GroupTail()` and is never reached that way. So
`Storage` was always `null`, `null is StorageForm.DynamicString` was always false, and the distinctive assertion
passed unconditionally — an inert assertion, in the one test written to police a *silent* mis-model. That is the
battery-#38 lesson from three days ago, arriving from the other direction.

Giving the test a `BindFull` helper that drives `BinderDriver.Bind` made the assertion real, and it **failed on the
first run**:

```
Sr17RejectedClass_NeverYieldsADisjointDynamicStringStorage [FAIL]
  a dynamic-length REDEFINES view must not be given its OWN native string storage
```

`StorageFormPass.Classify` orders its arms (1) `IsDynamicTable`, (1b) `IsDynamicLength → DynamicString`, and only
THEN (2) the REDEFINES-view arm. Arm 1b returns before the tier is ever consulted. A class dissolved by the
nested-anchor loop bypasses the tier loop entirely — the same hole from the other side. **What actually keeps the
disjoint storage out of a user's program is the fatal COBOLNET1698 diagnostic.** The tier verdict is
belt-and-braces, and worth keeping as such; the claim was not.

So the comments in `DataBinder.cs`, `RedefinesModel.cs` and `DESIGN-data-model.md` now say the honest thing, and
the test asserts the two facts that ARE load-bearing (the tier verdict, the fatal diagnostic) plus the MEASURED
storage form, with a control case proving the same item outside a REDEFINES class classifies identically. If
someone later teaches the pass to consult the tier, the test fails and the claim gets upgraded deliberately
instead of a doc-comment quietly out-running the code again.

### The four-sentence rule cited by number

COBOLNET1525 used to cite "§13.18.44 SR5" for both sides of a REDEFINES over a dynamic-capacity table. The
cluster-A repair read SR5's **fourth** sentence — "Neither the original definition nor the redefinition shall
include an occurs-depending table", which is COBOLNET0855's rule — concluded the clause was about something else,
and shipped a comment saying the OCCURS-bearing `data-name-2` is a case "which NO syntax rule literally names".

SR5's **first** sentence is *"The data description entry for data-name-2 shall not contain an OCCURS clause."*
That names it outright, and OCCURS DYNAMIC is Format 4 **of the OCCURS clause**. The clause NUMBER was right and
the SENTENCE was not — CLAUDE.md rule 1's inheritance failure, one level down from where it usually bites.

Measured consequence, and it is not confined to the dynamic case: `05 T PIC X(3) OCCURS 4. 05 R REDEFINES T PIC
X(12).` compiled CLEAN and ran, laying `R` over one occurrence's worth of storage. Screened nowhere.

The family is now three screens, one per rule: **COBOLNET1701** for the OCCURS-bearing object (any format, per
written entry), **COBOLNET1698** for SR17's variable-length group on either side, and **COBOLNET1525** narrowed to
the SUBJECT that IS a dynamic table — the one shape in the family for which "no syntax rule names it" is true, and
which §13.18.44.4 GR1's fixed association area decides against §8.5.1.9.1's "may vary during execution". Four
negative witnesses now stand either side of those boundaries, where the recode had left none.

The predicate needed both halves of a union, and each half is a trap this repo has already sprung: `Occurs is not
null` is the FIXED capacity and NULL for a Format-4 table (the CONTROL SR3 defect below), while `OccursSpec is not
null` is NULL for a plain keyless fixed table, which `OdoBindOccursSpec` deliberately leaves allocation-free. The
first cut used the second spelling and the fixed-OCCURS witness still compiled clean. It reads `IsTable` now.

### The bit channel had five more arms disagreeing with its own substrate

PB173 gave a GROUP-USAGE BIT group a ref-mod substrate in BIT positions. Five other sites still counted in packed
characters, and the review fleet found every one:

- **`ACCEPT XM(3:)`** — the omitted-length width read raw `Pic` (null for a group) and fell back to `ImageWidth` =
  ceil(m/8). On an 8-bit group that is `1 - 3 + 1 = -1`; at start 1 it is `1`, storing ONE character into an
  8-position slice. Undersized at *every* start, not merely past the packed width. This was the THIRD arm of the
  pad family, after `PlaceRenderer.Write`'s boolean pad and `MoveEmitter`'s figurative fill.
- **`BitImagePlace` had no §13.18.38.4 GR8 current-extent arm on either side** — and the character-unit ODO
  arithmetic was not just absent but *wrong* for sub-byte elements. `fixedChars = PhysicalWidth − elem × max` came
  out **negative** (2 − 1×4 = −2), so the whole bit-group operand rendered as the empty string. `fixed + occ ×
  elem` is an identity only in a unit where each occurrence starts where the last one ended, and §8.5.1.6.3's
  shared-byte runs break that in characters. `OdoGroupPlace` now carries `PositionsPerCharacter`; the prefix is
  the table's own placement offset (`BitLayout.StartBitOf` — never `total − elem × max`, which charges rule 4's
  trailing filler to the prefix); the character channel takes the ceiling. The bit arm now tracks the character
  twin position for position, which is the check that made it believable.
- **`OperandText.FieldAsString`'s ODO early return stole the bit group into the CHARACTER reader** — two alphabets
  on one operand. The bit test precedes it now and routes through `PlaceRenderer.SendingBits`. The
  `BoundaryImageChannelTests` allow-list row that promised "when [PB173] lands this row is the next one to route"
  is **deleted, not re-justified** — a justification that promises a future routing is a debt, and that row is
  where the debt gets paid.
- **The CALL/INVOKE boundary carried a bit group's OPERAND value into a `FromImage` that reads its PACKED image.**
  11001010 crossed BY REFERENCE, arrived in the callee as 00110001 — the eight bits of the character `'1'` — and
  came home as 00110000. Silent argument corruption, and a worktree at `876d8ab0` produced byte-identical output,
  so it is PRE-EXISTING and the refuter who said so was right. §14.2.3 GR8 makes the formal occupy "the same
  storage area", so the carrier is the STORAGE image.
- **`FUNCTION CONVERT(XM(1:3) ANY ANUM HEX)` returned `313130`** — the characters `'1','1','0'` — where §15.19.3 r7
  + §15.19.4 r2 want the packed byte `C0`, and where the unsliced twin already returned `CA`. The ELEMENTARY
  `USAGE BIT` slice took the same arm and predates the diff; one arm fixes both. The DISPLAY-form boolean control
  (§13.18.60.4 GR7 — one character per position) must NOT pack, and is pinned as such.
- **`StaticUsageOf` matched `IsGroup: false` + raw `Pic`**, so a bit GROUP had no static usage and slipped every
  keyword-keyed §15.19.3 screen: the `PIC 1(8) USAGE BIT` leaf was correctly rejected by r5 while the
  byte-identical group was ADMITTED. One rule, one usage, two verdicts.

### A regression this pass introduced, and the check that caught it

Widening `CallStringRead`'s group test to `p.Item.IsGroup` broke a ref-modded group argument: `RefModPlace.Item`
forwards to the INNER item, so `CALL "S" USING G(1:3)` rendered `CobolStr.RefMod(G.AsImage(),1,3).AsImage()` —
turning HEAD's single backend CS1061 into two. Both halves now carry an explicit `RefModPlace` arm (§8.4.3.3.4 GR6
— the ref-mod result is an elementary item over the slice), which also fixes the pre-existing WRITE half of that
crash: `CALL "S" USING G(1:3)` round-trips now, pinned in the same golden. Logged because the transparency rule
says to log it, and because the twin-arm question ("which arm did you fix?") is what found it.

### CONTROL: the dynamic shapes, the INDEX operand, and a message describing one limb of three

`ControlOperandShapeViolation`'s brand-new §13.18.16.3 SR3 arm tested `n.Occurs is not null`. That is NULL for a
Format-4 dynamic-capacity table, so both dynamic shapes escaped the compile-time screen and reached the
ReportWriterEmitter runtime loud — and SR7 structurally *cannot* cover the first of them, because §8.5.1.12.1
defines "variable-length group" over items SUBORDINATE to the group. It reads `n.IsTable` now, with a witness per
shape.

`CONTROL IS <index item>` is illegal source: §13.18.60.3 SR10 closes the set of contexts in which an index data
item may be referenced explicitly, and §8.4.5 makes a data-division clause naming a data item exactly such a
reference. It was a runtime loud; it is **COBOLNET1700** at bind now. The FLOAT half of the same guard is NOT a
syntax violation and stays loud — its read half works, but the prior-control RESTORE channel has no float arm, so
unguarding it would trade a runtime loud for a backend CS0029 — and the emitter's message now says what is
actually missing, per limb, instead of describing one limb for three.

`pb177-control-occurs` established its SR3 premise with `01 CG OCCURS 3 TIMES.`, which §13.18.38.3 SR1a forbids
outright — a witness leaning on a second, unscreened gap, green only because that gap is open. Re-authored onto an
05-level entry, and the three reject-at-85 CONTROL witnesses swapped `GOBACK` for `STOP RUN` so no leg is rejected
at 85 for a reason other than its own rule.

### Two items registered rather than improvised

- **[[PB204]]** — PB177's gated remainder (a variable-length group across a Format-2 CALL/INVOKE boundary, which
  §14.9.4.3 SR25 read with §14.8.2.2/§14.8.3.2 and §8.5.1.12 admit subject to a compatibility relation). It was
  parked as "a bare question for the owner"; it is not one — the no-deferral rule makes implementation the default
  and the campaign order is the director's call. Its interim posture was VERIFIED loud before the split
  (`COBOLNET1688 … a variable-length group has no fixed record window … deferred`), never silent.
- **[[PB205]]** — `CONTROL IS CX(1:3)` emits `() => CX`. `KeyReference` keeps only the qualification suffix and
  drops `refModPart`, so a ref-mod that §13.18.16.3 SR4 EXPRESSLY permits is silently discarded and the break is
  taken on the whole item. Registered rather than fixed because the fix needs `ReferenceResolver`'s ref-mod
  substrate wrap factored out for a data-division caller — spelling it a second time at the CONTROL site is the
  copy the one-rule-one-place law forbids. The CALL-boundary half of that cascade is already repaired above, so
  the downstream consumer is ready. SR2, SR6 and §13.18.16.4 GR6 are recorded in that note's body as the
  pre-existing debt of the same clause.

### Gate

Wave-local, filter covering every touched area (`~Accept|~Bit|~Report|~Control|~Redefines|~Convert|~Call|~Oo|~Udf|
~Image|~Corpus|~Negative|~Odo|~Occurs|~Intrinsic|~Group|~Dynamic|~Boolean|~Move|~Compute`): **Conformance
1905/1905 · Unit 5072/5072 · Characterization 33/33**, zero skipped, every leg carrying a verdict line. PB173,
PB177 and PB178 flip to `landed`; PB178's `wrong_answer` flips to `true`, which its own body had already said
while the frontmatter — the field `work.py next` ranks on — still said false. Battery #39 accrues.

