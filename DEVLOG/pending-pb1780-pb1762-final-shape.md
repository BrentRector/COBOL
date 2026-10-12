## PB1780 and PB1762 re-measured and given their final shape: one relation family at bind time (PB3101), one raisable-name structure with its registry (PB3102)

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
