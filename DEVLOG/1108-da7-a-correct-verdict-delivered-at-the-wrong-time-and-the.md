## Entry 1108 - 2026-07-29 16:30 PDT - DA7: a correct verdict delivered at the wrong time, and the question I had to settle before believing my own previous commit

Three constructs - an elementary COMP operand of INSPECT, and a COMP receiver of STRING or UNSTRING - are
GENUINELY ILLEGAL. The compiler already refused all three, so its verdict was right and no conforming program was
being rejected. The defect was the STAGE: each was reported as a run-time NotImplementedCobolFeatureException, so
an illegal program compiled clean and crashed only when control reached the statement, where the standard promises
a syntax error. New diagnostic `COBOLNET1626` (`character-operand-usage`).

**⛔ BUT FIRST I HAD TO SETTLE A QUESTION ABOUT THE COMMIT I HAD JUST PUSHED.** Reading the syntax rules for this
work surfaced something uncomfortable: §14.9.48.3 SR4 requires UNSTRING's receiver be "described implicitly or
explicitly as usage display", and §14.9.43.3 SR1 says the same of every STRING identifier bar the POINTER. A group
containing a COMP leaf is not uniformly display. So had entry 1107's DA5 fix - which had just ENABLED
STRING/UNSTRING into exactly such a group - started accepting ILLEGAL source?

That mattered enough to stop and answer, and the answer is no, on text rather than on my earlier inference:

  · §14.9.43.4 **GR3a** (cite.py-verified): characters are transferred into identifier-3 "in accordance with the
    MOVE statement rules for alphanumeric-to-alphanumeric moves". So whatever an alphanumeric MOVE may deposit
    into a group, STRING may deposit into the SAME group. That converts entry 1107's empirical argument
    ("MOVE works, therefore STRING must") into a cited rule.
  · §14.9.22.3 **SR1** goes further and settles it outright, naming "an alphanumeric or national **group item**" as
    a valid INSPECT identifier-1 and applying the usage requirement only to an ELEMENTARY operand.

So usage is an ELEMENTARY property, the group receiver is governed by the group-move semantics, and DA5 stands -
now on a textual basis instead of a consistency argument. I went back and put both citations into the five DA5
comments, replacing the inference that was there. ⚠ And a label correction on the way: I would have written that
first one as GR5a from my line-window numbering; cite.py reports §14.9.43.4 **3) a)**. The exact defect class of
entry 1103, caught before it propagated, which is the whole reason that check exists.

**WHAT THE FIX COST, PER SITE.** Two were pure staging - `InspectBinder` and the UNSTRING arm of
`StringUnstringBinder` already ran the check in the binder and merely returned `BoundUnsupported`. The STRING SR1
check did NOT exist in the binder at all; it lived only in `StringEmitter` as a run-time loud stage, so it had to
be added where the receiver is resolved.

**THE FALSE-POSITIVE CHECK IS THE POINT.** Adding a diagnostic risks rejecting legal source, which would be a far
worse defect than the one being fixed. Verified explicitly: the three illegal forms now give `COBOLNET1626` at
compile time, while `INSPECT <alnum>`, `STRING/UNSTRING INTO <group>` (including a COMP-leaf group), and
`STRING/UNSTRING INTO <numeric DISPLAY>` all still compile and run. Edition-invariant and confirmed firing at 85,
2002, 2014 and 2023 - all three rules are unchanged across the editions, so this is deliberately NOT gated and no
introduction axis applies.

**THE DOC-SYNC GATE EARNED ITS KEEP.** The full unit suite went red on
`DiagnosticRegistryDriftTests.DiagnosticsDoc_IsInSync_WithTheCatalogue` - `docs/DIAGNOSTICS.md` was stale the
moment 1626 was registered - and the failure message named the generator to run. That is exactly what a
registry-drift test is for: the doc cannot silently fall behind the catalogue.

**Also re-measured, having flagged it as carried three times:** characterization 33/33, freshly run rather than
carried forward from the step-4 gate. It matches, so the carried figure was accurate - but it is now measured.

**Gates.** Greenfield Unit 942/942, zero skipped. Negative corpus 141/141 (+3, each asserted to reject at all four
editions). FULL Conformance re-run. Characterization 33/33 fresh.

