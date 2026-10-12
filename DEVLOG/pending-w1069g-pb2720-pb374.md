## PB2720 + PB374: a paragraph header after a sentence on its line; the asynchronous messaging facility named at every surface and gated on introduction

Wave 1069 group G, the parser-core cluster (`CobolParserCore.g4`, `CobolParserCoreBase.cs`, `EcNameResolution.cs`).

### PB2720: no line-position predicate on a paragraph header

Re-probed at ddee217e3: free-form `P1. DISPLAY "A". PERFORM P2. STOP RUN. P2. DISPLAY "B".` on one line drew
`error COBOL0001: cannot parse construct near 'P2'`, and `S1 SECTION. P1. ...` failed at P1, while a mid-line
`S2 SECTION.` parsed. `paragraphName` carried `{IsAtLineStart()}?` on both alternatives, a guard from DEVLOG 0063
against a WRITE that left `LINES` unconsumed. ISO §14.4.3 defines a paragraph by its name and separator period with
no position rule, and §6.4.1 lets free-form text be written anywhere on a line. The predicate is gone and
`IsAtLineStart` is deleted (no other caller). `sentence : statement+ DOT` already makes a header follow only a
separator period, so an unconsumed word is a syntax error, never a phantom paragraph. A one-word statement written
mid-line after a period (`GOBACK.`, `COMMIT.` at 2023) still parses as a statement, as it does at the start of a
line, and `COMMIT.` at 2014, where COMMIT is a user word, is a paragraph either way. Golden
`2002/pb2720_paragraph_header_mid_line` covers free form and a fixed-form segment: section and paragraph headers
mid-line, an empty paragraph followed by a header on the same line, and a qualified PERFORM THRU. There is no
negative because no edition rejects the shape. Two §14.2.2 SR12 negatives (`l1c22-format3-in-program`,
`-in-method`) pinned the old parse-error text: `METHOD-ID. SHOWIT.` now parses as two headers, and the Format-3
procedure division is refused one line later at its `PROCEDURE DIVISION` header. That is still a parse-level
rejection at every reject-at edition, and their `.err` files now carry the new position. COBOL-85's area-A rule is not enforced for any header (a header at
the start of a line in area B already compiled), so it is reported as a lead.

### PB374: the MCS module's remaining surfaces

- **EC-MCS exception-names** compiled in silence at 2023 (`>>TURN EC-MCS-ABNORMAL-TERMINATION CHECKING ON`, exit 0).
  Asynchronous messaging is Annex A.3 item 4 (processor-dependent), so §4.2.6's compile-time warning is owed and
  refusing the names would be the wrong posture. `EcNameResolution.DeclinedEcFamilies` gains an `EC-MCS` row whose
  descriptor is the Warning `McsFacilityUnsupported` (COBOLNET1578). `Advise` reads the row's posture through
  `EditionContext.Refuses`, the one rule `Declined` reports by: an accepted row names the facility and resolves the
  name, and a refused row refuses as before. A RAISE still raises the
  condition (§14.9.29.4 GR1) and reaches a USE declarative or a WHEN phrase. The 1578 title now covers both
  surfaces. Golden `2023/pb374_ec_mcs_names_accept_inert`; negative `pb374-ec-mcs-name-below-2023` (COBOLNET0878 at
  2002/2014); a witness test counts the five warnings (TURN, USE, RAISE ×2, WHEN).
- **USAGE MESSAGE-TAG** was discharged: COBOLNET1943 has refused it by name since PB487, because §13.18.60.4 GR9
  leaves it no inert reading.
- **RECEIVE/SEND at --std 85.** Both verbs are reserved at COBOL-85 (the communication module) and at 2023, and
  are user words at 2002/2014. `facilityWord` gated on reservation, so the 2023 formats compiled at 85 under a 1578
  that named the 2023 facility, while 2002/2014 got a raw COBOL0001. The two arms are now the union grammar
  (`{wordAhead(...)}?`), and `VersionConformancePass.VisitMcsReceiveStatement`/`VisitMcsSendStatement` gate the new
  construct row `async-messaging-2023` when the statement is recognized: 85, 2002 and 2014 now answer COBOLNET0900. The binder names
  the facility only where the construct is available. VCR Table 2 row 38 changes from `todo` to
  `gate:async-messaging-2023`. Negative `pb374-async-messaging-below-2023` (reject-at 85 2002 2014) and a witness
  theory asserting no 1578 below 2023. RECEIVE and SEND stay user words at 2002/2014; a declared data item in MOVE
  and DISPLAY operand lists and a paragraph `RECEIVE-P` were re-probed at both editions.

### Also fixed

CONFORMANCE.md §4 had two stale sentences: EC-SCREEN names have been refused (COBOLNET1707) since PB260, and
COBOLNET1578 has been asserted since PB1198. `CobolParserCoreBase` had `facilityWord`'s summary on top of
`reservedHere`, which left `reservedHere` with a `<para>` outside its summary; the summary is now on
`facilityWord` and says which verbs it may gate.

No inventory row changed: there are no rows for §14.4.3 or §6.4.1, and the RECEIVE/SEND rows stay
DOCUMENTED-NON-SUPPORT.

### Branch review round 1

R1-1: the first cut of `Advise` decided the posture with `if (descriptor.Severity == EditionSeverity.Warning)`, a
second, incomplete copy of the refuse rule `EditionContext.Declined` applies (Error refuses unless the descriptor is
`PermissiveInert` and the compile is `--permissive`). Under `--std 2023 --permissive`, `RAISE EXCEPTION
EC-VALIDATE-CONTENT` got the COBOLNET1710 warning that accepts the name, then `Advise` returned false, and the compile
died as COBOLNET2362 (a refusal with no rule-naming diagnostic). Main failed the same way, since its `Advise` returned
false for every declined row. The rule now has one home, `EditionContext.Refuses`, which `Declined` reports by and
`Advise` binds by. Under `--permissive` the EC-VALIDATE and commit-and-rollback names are accepted and resolve; a
RAISE of one reaches its USE declarative. EC-SCREEN's descriptor is not `PermissiveInert`, so its names are still
refused. Swept the other `Declined` callers: none branches on the posture (the data-division and APPLY COMMIT sites
only report), so `Advise` was the only binding site. New theory
`DeclinedModuleExceptionName_Permissive_IsAcceptedAndResolves` reads the six strict negatives under `--permissive`,
and `…_RaiseReachesTheDeclarative_ScreenStillRefused` runs the reviewer's scenario and the EC-SCREEN complement.
R1-2: appending the PB374 note's Landed section had split its Code site line; the line is whole again.
