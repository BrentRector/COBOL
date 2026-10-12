## Entry 1649 — 2026-09-22 20:12 PDT — REGISTRAR #12: PB986–PB1032 filed from waves 48–52's leads, seven notes extended, PB965 landed

**What.** The 86 leads of waves 48–52, their finishers and the train-49 lander's 16 (`leads-w48-w52.md`) became
**47 new notes, PB986–PB1032**, plus **7 extensions**. Every probe was re-run on the registrar's own
`dotnet build CobolSharp.sln -c Debug` at main `37f19e028` (about forty probe runs, 18 of them on programs written fresh under
`scratchpad/reg12/`), and every citation went through `cite.py --check` (39 checks, all OK after re-addressing — two
of them print a WRONG rule path, which became PB903's extension).

**Mechanisms filed.** Wrong answers: an object property in a PERFORM UNTIL condition hoisted once (PB987, K=09 vs
K=03); a native-resident numeric item unable to hold a character image (PB992, `H=[000]`); SORT not terminated by a
fatal implicit OPEN (PB993); LOG/LOG10 underflowing a legal decimal argument to 0 (PB999); PRESENT-VALUE's domain
screen on a binary64 image (PB1000); the PERFORM exception handler's implicit PUSH/POP ALL restoring TURN only
(PB1004); ACCEPT into ANY LENGTH transferring one character (PB1013); qualifier-dropping OCCURS KEY capture (PB1018);
a key after a dynamic-length member sliced at a fixed offset (PB1025). Legal source refused: RENAMES THRU over an
OCCURS group (PB986); SORT/MERGE on relative/indexed files (PB994); LINE … NEXT PAGE (PB1001); >>SOURCE inside an
omitted >>IF (PB1006); INVOKE BY CONTENT of a numeric function into an alphanumeric formal (PB1007); a GLOBAL LINKAGE
formal from a contained program (PB1009, backend CS errors); method DECLARATIVES (PB1010); ADDRESS OF a LINKAGE formal
(PB1019, backend CS errors); address-identifiers in INVOKE and relations (PB1021); EXTERNAL FD with an out-of-line
record (PB1026); the resolver's silent-null shapes (PB1030). Under-rejects: END marker names (PB988); §12.3.8.4
"specified previously" (PB989); §8.3.2.2 one type per word (PB990); SORT SR11 (PB995); LINE SR3/SR5 (PB1002);
PUSH/POP ALL placement (PB1005); CONSTANT LENGTH OF subscripts (PB1016); REPOSITORY SR1 beyond PROGRAM (PB1017);
WRITE ADVANCING identifier-2 (PB1023); the range IN alphabet at 85 (PB1015, analysis). Architecture/process: refusals
constructible without a diagnostic (PB1029), 0899's two jobs at the code layer (PB1028), PB938's end state (PB1031),
83 position-less bind diagnostics (PB1011), three one-rule-two-places (PB1003, PB1008, PB1024), inherited cites
(PB996), the DIAG arm blind to tests/ (PB997), the SET Format 15 gate message (PB998), a wrong INHERITS diagnostic
(PB1020), INVOKE's hand-written boolean store (PB1014), DLS residual rows (PB991), the §9.3.8.2.3 verdict round with
rule 7's same-type ruling (PB1012), an unattributed Unit red (PB1022), the implicit-AFTER adjudication (PB1027) and
**determination ids with no central allocation or duplicate check (PB1032)** — W re-used D-DL2.

**Extended, not duplicated.** PB606 (the probe it asked for is run: CALL NOT ON EXCEPTION runs BEFORE the propagated
condition is picked up — now a MAJOR defect claiming GR-14.9.4.4-3); PB605 (⛔ premise corrected: §14.9.4.3 SR3 makes a
function-identifier ILLEGAL as Format 1's BY CONTENT identifier-2, so the refusal is right and only the diagnostic is
wrong — rejects_legal_source cleared); PB546 (the group-receiver side and the dynamic-length national receiver);
PB903 (three more transcription defects: §13.18.34.4 GR7 b/c and §14.9.33.4 GR2 b indentation, §8.4.3.1.2's
colon-less Format 8/9); PB984 (DOC-A.1-92's duplicate — two different topics under one key); PB543 (re-verdict when
PB215 lands). **PB965 LANDED**: its last claim GR-14.2.3-8 is re-homed to PB970 (BINARY/PACKED) and PB992
(character channel).

**Leads that did not survive re-measurement.** The wave-48 RESUME crash on a PERFORMed declarative and PB892 Arm B are
fixed on main (the PERFORM probe prints `DECL / AFTER-PERFORM`, as §14.9.33.4 GR2 b) requires); COBOLNET0717's
method-order dependence does not reproduce in either method order; the VLG-into-fixed bind refusals were fixed by the
wave 50 C finisher. Not filed as owned or in flight: the gen-grammar-diagrams temp dir (PB376), PB655/PB764's leads,
PB244's shapes, PB970 arm 2, the FD dynamic-length WRITE crash (PB981's own scope).

**⚠ CONFORMS rows the new notes contradict** (claimed, not re-verdicted here — each note's fixer re-verdicts with its
golden): GR-14.9.1.4-3, GR-14.9.28.4-14, SR-8.4.2.3.3-2, SR-14.9.37.3-8, AR-15.74.3-2, RV-15.55.4-1, RV-15.56.4-1.
GAP unchanged at 2175. **Gate:** `=== WAVE-LOCAL GATE: GREEN (filter FullyQualifiedName~VcrDriftTests) ===` (Unit
28,794 passed — SpecTraceabilityInventory, DefectiveRowCoverage and DerivedVerdict among them); `work.py check` ✓ 1065
work items. **Friction:** the first two gate filters were refused by the population check (a DerivedVerdict term that
selects no Conformance test, then two terms that live only in Unit) — the registrar brief's filter predates PB708.

