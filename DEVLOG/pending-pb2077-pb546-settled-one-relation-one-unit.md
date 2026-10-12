## PB2077 and PB546 settled on the text: one relation for both INVOKE lanes, one unit reader for the national leaf (decisions PB3098, PB3099)

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
