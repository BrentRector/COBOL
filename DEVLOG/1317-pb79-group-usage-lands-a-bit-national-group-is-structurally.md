## Entry 1317 — 2026-08-18 11:53 PDT — PB79: GROUP-USAGE lands — a bit / national group is structurally a group and semantically an elementary boolean / national item (D20); PB95: every bare usage keyword parses

**What landed.** kb/Work **PB79** (MAJOR, rejects legal source): `01 NG GROUP-USAGE NATIONAL.` /
`01 BG GROUP-USAGE BIT.` were parse errors — GROUP-USAGE was a §8.9 reserved word no clause consumed — which held
seven §13.18.29 inventory rows GAP and the bit-group / national-group legs of `FUNCTION LENGTH` (RV-15.50.4-1/2,
PARTIAL since PB61) unreachable. The design question was settled first (data-model design **D20**): a GROUP-USAGE
group must stay a GROUP for everything STRUCTURAL (children, layout, the record struct, REDEFINES tiers,
INITIALIZE / CORRESPONDING walks, file images) and become an ELEMENTARY boolean / national item for everything
CATEGORICAL — §13.18.29.4 GR1b/GR2b's "treated as though it were an elementary data item … PICTURE 1(m) / N(m)".
So: `DataItem.GroupUsage` (None/Bit/National, propagated to subordinate groups by the usage-inheritance walk),
`DataItem.AsIfPic` (`1(m)` = the exact bit extent, `N(m)` = the national positions), and **`OperandPic => Pic ??
AsIfPic` — THE ONE READER for category** — every site that decides class, category, a Table 16 row, LENGTH
positions, ref-mod admissibility, comparison class, string-operand-ness or boolean-operand-ness reads it (Table 16
both sides, `MoveClassifier`, `MoveReceiverCategory`, `IntrinsicResultType.OperandCategory`, `ClassOfPlace`,
`RefModPlace.Category`, `LengthPositions`, the INSPECT / STRING / UNSTRING sizes, the relation renderer's category
and anchor width, the boolean-expression operand check). `IsGroup` / `IsElementary` / `Children` keep their
structural meaning — the 116 structural sites are untouched by construction; `IsAsIfElementary` names the
GR1b/GR2b cases where an operation takes the elementary path over a group.

Grammar and registries: `GROUP_USAGE : 'GROUP-USAGE'` (a hyphenated keyword token; a user word below 2002 via
`cobolWord` — `cobol-words.json` row + `gen-cobol-words.ps1`; in the funnel's `CheckedTokenTypes`, so an '85 item
NAMED GROUP-USAGE compiles and is 0901 at ≥2002); `groupUsageClause : GROUP_USAGE IS? (BIT | NATIONAL)`;
`constructs.json` row `group-usage-clause-2002` (`ParseArm.VisitGroupUsageClause` → 0900 below 2002). Layout:
`BitLayout.IsBitItem` (a bit leaf OR a bit group) drives §8.5.1.6.3 rule 1's same-level byte sharing; `ExtentBits`
omits rule 4's trailing filler for a bit group (the NOTE's "record that is entirely a bit group"), so
`FUNCTION LENGTH` of a 12-bit group is 12 and `BYTE-LENGTH` 2. The implied USAGE BIT / NATIONAL rides the SAME
`PicPending` adjudication a group-level `USAGE BIT / NATIONAL` clause rides — one walk, two spellings — **and that
walk now APPLIES the implied bit form to PICTURE-1 leaves without their own USAGE (§13.18.60.4 GR1):**
`01 G USAGE BIT. 05 GA PIC 1(5). 05 GB PIC 1(3).` validated its leaves and left them display-form (8 bytes); it
is 1 byte now — a sibling defect of the same rule, fixed in place. Emitter: a bit group's operand face is
`AsBits()` / `FromBits()` (`GroupImageCodec.EmitBitMethods` — the subordinates' boolean positions concatenated;
`OperandText`'s as-if arm, `PlaceRenderer.Write`'s as-if arm, `BooleanRenderer`, the relation renderer's
`BoolRead`); the packed byte image stays `AsImage()`/`FromImage()`, and a bit group can be a MEMBER of a
§8.5.1.6.3 run in an alphanumeric group (`PhysicalModel` run members are bit items; a group member's carrier is
`X.AsBits()`, its distribution `X.FromBits(slice)`). A national group's operand face IS its character image
(`AsImage()`), stored through the ONE group-image store — Table 16's NATIONAL row on both sides of a MOVE.
Diagnostics: **COBOLNET1653** (`GroupUsageRule`) for SR1 (an elementary entry, an entry with no subordinates, a
strongly-typed group, a variable-length group), SR2/SR3's explicit USAGE on the subject and a subordinate group
declaring the other usage; the leaf conformance is the shared 0881 / national-form-staged 0899 legs, now
positioned at each LEAF (PB82's cursor).

**PB95, found and fixed on the way.** Writing the bit golden, `01 BR PIC 1(12) BIT.` was "unexpected 'BIT'":
`usageClause` carried a HAND LIST of bare (USAGE-less) alternatives beside the full form, and the six it omitted
— POINTER, OBJECT REFERENCE, NATIONAL, BIT, PROGRAM-POINTER, FUNCTION-POINTER — are exactly the words a hand list
is likeliest to miss (cobolWord-admitted or rule-shaped). §13.18.60.2 makes `[USAGE IS]` optional for the WHOLE
alternative set. `usageClause` is now ONE `(USAGE IS?)? usageKeyword binarySign? noSignPhrase?` alternative, so
every spelling carries the `usageKeyword` node and both readers (`DataBinder.UsageKeyword`, the legacy
`SemanticBuilder`) lost their bare-form workarounds. No edition predicate: in `05 BIT.` the entry-NAME alternative
is tried first (an '85 item named BIT — probed: `05 BIT PIC X. 05 NATIONAL PIC X.` at 85 still displays), and
`05 X BIT.` reads as the usage, edition-named downstream — the superset-parse / bind-narrow direction.

**Evidence.** `2023/pb79_group_usage_national` (LENGTH 5 / BYTE-LENGTH 10; MOVE pad/truncate both ways; national
comparison; INSPECT TALLYING 4; ref-mod read `NG(2:3)` and write `NG(4:2)`; a nested national group),
`2023/pb79_group_usage_bit` (LENGTH 12 / BYTE-LENGTH 2; MOVE to a bit item, a display boolean and a shorter bit
item; MOVE into the group distributing to B1/B2/B3; equality; `B-OR` on the group; a nested bit group; the
group-level USAGE BIT leaves at 1 byte; a bit group sharing a byte with a preceding same-level bit item inside an
alphanumeric group — BYTE-LENGTH 2 with a trailing PIC X), `2023/pb95_bare_usage_keywords`; negatives
`pb79-group-usage-explicit-usage` / `-not-a-group` / `-subordinate-conflict` (1653) and `-85` (0900). Ten
inventory verdicts → CONFORMS (FMT-13.18.29.2, SR-13.18.29.3-1..3, GR-13.18.29.4-1..3, RV-15.50.4-1/2 re-adjudicated
from PARTIAL, FMT-13.18.60.2): GAP 3974 → 3964. Docs: `COBOLNET_DATA_MODEL_DESIGN.md` D20 (new) + D19's OUT
sentence; `DIAGNOSTICS.md` regenerated; `constructs.json` / `cobol-words.json` + their generated twins.

**Residue, stated.** National-form NUMERIC subordinates of a national group (SR3's SIGN IS SEPARATE half) are the
Phase 4a national-form staged legs (0899) — the same status they have outside a national group. GnuCOBOL 3.2
parses GROUP-USAGE and marks both phrases pending, so no latitude survey applied — the standard fully specifies the
semantics.

**Gate.** Characterization 33/33 · Conformance FULL 4739/4739 · Unit (filtered) 3559/3559 + the inventory / word-set / VCR drift tests 49/49. Battery #15 owed
for the PB79 + PB95 batch with the next landing.

