## Entry 1974 — 2026-10-12 15:41 PDT — PB2077 and PB546 settled on the text: one relation for both INVOKE lanes, one unit reader for the national leaf (decisions PB3098, PB3099); PB1780 and PB1762 re-measured and given their final shape: one relation family at bind time (PB3101), one raisable-name structure with its registry (PB3102)

### PB2077 and PB546 settled on the text: one relation for both INVOKE lanes, one unit reader for the national leaf (decisions PB3098, PB3099)

The operator asked the Mythos session (2026-10-12, task w1069y) to settle the two questions that held PB2077 and
PB546 `blocked: true` on an owner adjudication, by CLAUDE.md rule 1's precedence, and to design one mechanism each. No
code changed in this landing; both notes are unblocked with an apply-ready Fix shape, two decision notes record the
derivations, and the design docs carry the mechanisms.

### PB2077 — a reference-modified argument of a method (decision PB3098)

The text decides it. §8.4.3.3.4 GR5/GR6 define the unique data item with class, category, usage and no JUSTIFIED
clause, and list no PICTURE clause; where the standard wants an as-if picture it says so (§13.18.2.4 GR1 b),
§13.18.29.4 GR2 b)). §9.3.6 match rule 3 e) and §14.8.2.3.2 rule 2 are clause-identity lists, so BY REFERENCE the slice
matches and conforms only to an alphanumeric group formal of no more bytes (3 c), §14.8.2.2 rule 1); an elementary
formal takes it BY CONTENT (4 d)). §14.9.23.4 GR7 b) locates the method by §9.3.6 for EVERY INVOKE, so the typed lane
asks the match as well as the compile-time conformance of §14.9.23.3 SR5 c): one relation, read at two times. GnuCOBOL
3.2 was probed through the one place it applies the same sentence, a program-prototype CALL: its check reads
identifier-1's description and ignores the modifier (`Y(1:5)` and `Y(1:3)` into `PIC X(5)` are silent when `Y` is
`PIC X(5)`), so it has no answer. Today the typed lane accepts `X(1:5)` into `PIC X(5)` (an under-reject) and refuses
`X(1:6)` into a 4-byte group (legal); the universal lane is right in every row. A sibling fell out: a whole `PIC X(n)`
into a group formal, accepted typed and refused universal, is refused on both by 3 e).

The mechanism (COBOLNET_OO_DESIGN D6 item 6): the typed lane asks the universal switch's two relations,
`ActivationRelations.DescriptionMatches` and `ParameterViolation`, over the same `ActivationDescriptions` the box is
built from; `OoBinder`'s hand-written reference-modification arm and its `DescriptionMismatch` call for INVOKE arguments
go; `DescriptionMismatch` stays the pair-mode comparator. Two readings the text leaves open are recorded as
CONFORMANCE.md D-RM1: a run-time slice length takes rule 1 on its maximum extent at compile time (§14.8.2.2's
occurs-depending model) and the evaluated length at the boundary; the ANY LENGTH item of 3 e) is read through
§14.8.2.3.2 d)/e). The drift test pairs the binder's entry with the relation over a description table and refuses a
second comparator structurally.

### PB546 — one national leaf, two widths (decision PB3099)

The byte reading PB327 took on 2026-09-05 is what the text derives: an alphanumeric group is measured in alphanumeric
character positions (§15.50.4 r3, §8.4.3.3.4 GR1/GR5 a), §14.9.25.4 GR4, §14.8.2.2 rule 1, §14.9.11.4 GR1), and a
national character is "a uniform size equal to or a multiple of" an alphanumeric one (§13.18.60.4 GR8), two bytes
under D-N1. GnuCOBOL 3.2 measures the same unit. So the owner is not asked and DOC-A.1-57 stands as a derivation.

Re-measured on `ddee217e3`, the note's consumer list is mostly green: LENGTH, BYTE-LENGTH, reference modification,
ORD per byte, a group MOVE, a CALL BY REFERENCE window, the literal-into-group move (registrar 12), the group-with-
national-leaf INVOKE conformance on both lanes (registrar 14b), the national-form numeric item whole and sliced, and
an FD record of national-form leaves on disk. Two sites remain, one mechanism: a byte count that decides "national" by
the item's CATEGORY, so a usage-national item of category boolean takes the one-byte arm — `BYTE-LENGTH(BN(1:2))` is 2
where 4 is due (registrar 15c), and a group moved into a `PIC N DYNAMIC LENGTH` item is converted character by
character where GR4 copies bytes (`LENGTH(DN)` 6 where 4 is due; D-N8 completes the odd tail with GR4's space, the
fixed receiver's existing behavior).

The mechanism (COBOLNET_DATA_MODEL_DESIGN D-N1): `PicInfo.BytesPerPosition`, the one place the unit is spelled, read by
every byte-width, position-bit, image-contribution, record-unit and `NatBytes` site; `NationalUnitDriftTests` keeps
every national-unit call guarded by a usage test or reached through the reader, never by the category alone; the
sanctioning unit test becomes the rule for all five usage-national forms.

### Mechanics

GnuCOBOL probes under WSL (`cobc -x -Wall`), the compiler's probes on the tree's Debug `cobol.exe`; every citation
through `cite.py --check`. The scratchpad probes are not committed (GPL-adjacent GnuCOBOL output stays out of the
repository).

### PB1780 and PB1762 re-measured and given their final shape: one relation family at bind time (PB3101), one raisable-name structure with its registry (PB3102)

The operator's second Mythos task of 2026-10-12 (w1069x, owner-approved): the same shape as w1069y — measure on main,
settle readings by rule 1, design one mechanism each in the deep-dive, leave defect notes the fix lane plans. No code
changed in this landing.

### PB1780 — activation-argument conformance (defect PB3101)

Since the analysis was written (2026-09-30), items 1-6 and 8 of its decomposition landed: the universal lane asks
`ActivationRelations` over `ActivationDescriptions`, the string descriptor is gone, BY VALUE methods, the literal and
expression arms, RETURNING through SET, strong groups with object leaves, ANY LENGTH returning items. What remains is
the sentence the research agent wrote and no landing took: the bind-time lanes still ask `OoConformance.DescriptionMismatch`
over `DataItem`s. Measured on `ddee217e3`: a reference-modified argument BY REFERENCE runs into a prototyped CALL's,
a function's and a typed INVOKE's `PIC X(5)` formal (§14.8.2.3.2 rule 2 refuses it — the PB3098 reading), and a whole
`PIC X(4)` runs into a typed INVOKE group formal (§9.3.6 3 e) refuses it); the universal lane is right in every row.
The standard writes the description identity once (§9.3.5.3's eight clauses) and asks it from five places; the
compiler writes it twice, in two currencies, and a drift test over elementary pictures held them together on one axis.

PB3101's shape, in COBOLNET_OO_DESIGN D6 item 7: `ActivationRelations` gains `DescriptionMatches`, the universal-only
GR7 c) arm and `SameSignature` (§9.3.8.2.3, §11.7.3 SR3, §13.7.3 SR2, with `GroupAtom` carrying each leaf's identity);
`ParameterConformance.CheckArgument` is the one bind-time entry for CALL, function and INVOKE, building the descriptions
and asking the relations; pair mode (override, implements, interface, prototype, the variable-length MOVE element test)
asks `SameSignature`; the comparator family, the two GR8 reductions and `StrongTypeModel.SameElementaryLayout` are
deleted; PB613 and PB1576 close inside it. The owner's rule of the day — a decade-long production compiler,
rearchitecture allowed where the principle needs it, cost against time — is why the note asks for one Opus slot and a
review round rather than a sixth comparator correction. D6 item 6, written by w1069y, is item 7's INVOKE step: PB2077
lands inside PB3101, or first with its pair-mode allowance transitional, so the comparator has one fate.

### PB1762 — the exception-names a statement can raise (defect PB3102)

Two of its three tasks landed on 2026-09-30 (PB1453: the operand walker with EC-PROGRAM-NOT-FOUND; PB1531: the closed
catalog). Re-measured: 35 of the catalog's 135 level-3 names occur nowhere outside it (down from 64 of 130) — 18 of
the 22 `EC-*-IMP` (the implementation sets only EC-PROGRAM-IMP), the declined MCS, screen and VALIDATE families (seven,
four and three names), `EC-BOUND-FUNC-RET-VALUE`, and two I-O names the status map raises; the four intrinsic-family gates still sit beside the walker. PB3102's shape, in
COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D13: one enumeration `EcBinder.DeclaredRaisableNames` (node-kind arms ∪ operand
declarations ∪ ambient), the intrinsic families as operand declarations, the runtime's raise literals collected by a
Roslyn walk, and `EcRaiseSiteRegistryDriftTests` over the closed catalog with a `DeclinedRaiseSites` table whose every
entry cites a note or a CONFORMANCE determination. PB1037's brief is that test; it closes with PB3102.

### Mechanics

Probes on the tree's Debug `cobol.exe`; the name scan over `src/`; every citation through `cite.py --check`.

