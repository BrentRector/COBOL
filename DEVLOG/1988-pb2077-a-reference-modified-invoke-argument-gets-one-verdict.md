## Entry 1988 — 2026-10-12 21:48 PDT — PB2077: a reference-modified INVOKE argument gets one verdict on the typed and the universal lane

The typed and the universal INVOKE lanes answered one §9.3.6 / §14.8.2 question with two mechanisms and gave opposite
verdicts on a reference-modified argument. Re-probed on this build: the typed lane ran `X(1:5)` into a `PIC X(5)`
formal (`TP:ABCDE`), refused `X(1:6)` into a 4-byte group formal and refused `X(1:N)` outright, and ran a whole
`PIC X(10)` into the group; the universal lane raised EC-OO-METHOD, ran the group, and refused the whole item. PB3098
settled the question on the text (§8.4.3.3.4 GR5 / GR6 give the unique data item no PICTURE clause, so §9.3.6 match
rule 3 e) matches it only to a formal with none, an alphanumeric group, and §14.8.2.2 rule 1 bounds the group), and
COBOLNET_OO_DESIGN D6 item 6 set the shape: the typed lane asks the universal switch's relations.

**The fix.** `OoBinder.ByReferenceArgumentReason` is the typed lane's one BY REFERENCE entry. It builds both
descriptions with `ActivationDescriptions.OfPlace` / `OfFormal` and asks `ActivationRelations.MatchViolation`, a new
relation holding the description half of match rule 3 (`Matches` is now it plus the ACTIVE-CLASS value half), then
`ParameterViolation`. GR7 c)'s ANY LENGTH ban bans the bound method's description, not a pair, so it left
`ParameterViolation` for the universal switch's unconditional stop (which already raised it), and the typed lane never
meets it; the shipped runtime surface is unchanged (the new members are unshipped, so the ABI pin holds). An ACTIVE-CLASS formal
keeps the static §14.8.2.3.2 rule 4 arm. The binder's hand-written `RefModPlace` arm and its `DescriptionMismatch` call
are deleted, along with a dead `OoConformanceError` wrapper. A slice BY CONTENT now takes the MOVE rules like any
operand (§9.3.6 4 d)), so a slice BY CONTENT into a group formal binds where it was refused.

Three pieces the one relation needed:
- §9.3.6 3 e) lists ANY LENGTH, so read literally no fixed argument matches an ANY LENGTH formal. D-RM1 b) reads it
  through §14.8.2.3.2 "Additionally" d): `ActivationDescription.AnyLengthClauses` carries the clauses an item would
  have at ANY LENGTH, and the relation compares those, so only the length is relaxed. Both lanes read it. Through a
  universal receiver a `PIC X(4)` into `PIC X ANY LENGTH` now matches and then meets GR7 c) (EC-OO-UNIVERSAL instead of
  EC-OO-METHOD; `pb480_universal_any_length_omitted` row 1 re-derived). The CALL lane's run-time rule 2 d) and returning 5)
  arms still compare only category and usage; reading the any-length form there would make an old module's
  description refuse what it passed (a call-ABI bump), so it is a lead. The boundary-layout hash moved only because
  every description now carries the field, which no CALL relation reads: re-pinned, the call ABI unchanged.
- `OfReferenceModification` takes GR6's category from the one reader, so an alphabetic slice stays alphabetic. When
  the slice's length is a run-time value, its compile-time size is the maximum extent (D-RM1 a)).
- A bit / national group is "described with PICTURE 1(m)" / "N(m)" (§13.18.29.4 GR1 b) / GR2 b)), but its
  description carried no clauses, so the universal lane refused a national group into `PIC N(5)` and matched two
  national groups of different lengths. Its clauses are that picture's now (`pb480_universal_match_relations` row TQ:
  EC-OO-METHOD, re-derived).
- `CallEmitter.AreaOf`: a slice whose length is a run-time value states no storage area. Before, the group formal was
  laid over identifier-1 and read and wrote past a short slice (`X(3:N)`, N = 2: `TG:CDEF`, four bytes written). Now it
  holds `CD  ` and writes back through the slice (`ABwxEFGHIJ`).

**Goldens.** `conformance:2002/pb2077_refmod_argument_both_lanes` (six typed rows that run, eight universal rows);
negatives `pb2077-refmod-into-elementary-by-reference`, `pb2077-refmod-shorter-than-group-formal` and
`pb2077-elementary-into-group-formal`. New drift test `TypedAndUniversalInvokeVerdictsAgreeDriftTests`. Its Roslyn walk
checks that `OoBinder` calls no `DescriptionMismatch` and that no `if` of `OoBindInvocationArg` branches on a slice.
It also checks that the binder agrees with the relation over every elementary, sliced and ANY LENGTH pair, and that
PB3098's typed rows compile to their verdicts. No inventory rows: PB2077 claimed none.

**Sibling sweep.** The prototype CALL and user-function lanes ask the same §14.8.2.3.2 rule 2 sentence through
`ParameterConformance.CheckArgument` and still accept a slice BY REFERENCE. That lane is PB3101's restructure (one
bind-time entry, the comparator deleted), so it is not re-filed; PB3101 records what this landing took.

**Gate.** `TestResults/build-local/20261013T035906Z-aab809` at `13e927143`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating,
PB2515): GREEN` (Conformance 4,937/11,498 · Unit 32,634/33,476 · Characterization 6/36). One red on the way:
`RuntimeAbiPinDriftTests` (a shipped-signature change), answered by keeping the shipped surface. An extra
whole-population Conformance run found four more reds leg 1 never reached (above), all fixed; the whole Unit assembly
ran 33,509/33,509. Self-review: the relation builds a reason string only on a mismatch; the CALL lane's ANY LENGTH
arms and a slice of a non-static item are leads in the report.

**Branch review round 1.** Two findings, both fixed. A slice of an alphanumeric GROUP (`G(1:N)`, `G(3:)`) was measured as zero positions, because `OfReferenceModification` read identifier-1's length through `OperandPic`, which a group lacks; the typed lane refused it into every group formal (COBOLNET0828) while the universal lane ran it. It reads `RefModPlace.StaticInnerPositions` now, the one position reader, and `CallEmitter.AreaOf` and `ConditionBinder` use it too. Re-probed: both lanes print `TG:ABCD|` and `TG:CDEF|`. And a JUSTIFIED argument refused by an ANY LENGTH formal was told its LENGTH differed, the one clause the rule relaxes: `FirstClauseDifference` now diffs the pair the relation compared and names `JUSTIFIED`. Golden rows on both lanes and six drift-test cases witness them.
The review finisher's gate: `TestResults/build-local/20261013T042344Z-bcfb55`, `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 5,074/11,502 · Unit 32,690/33,482 · Characterization 6/36).

