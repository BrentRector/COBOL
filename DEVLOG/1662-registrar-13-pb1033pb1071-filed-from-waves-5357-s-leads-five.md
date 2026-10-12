## Entry 1662 — 2026-09-23 09:04 PDT — REGISTRAR #13: PB1033–PB1071 filed from waves 53–57's leads, five notes extended

**What.** The leads of waves 53–57 and trains 51–57 (`leads-w53-w57.md`, about 110 lines, many duplicated across a
wave's lander and its implementers, plus the train-57 lander report) became **39 new notes, PB1033–PB1071**, one per
mechanism, plus **5 extensions** (PB310, PB674, PB244, PB165, PB651). Every probe was re-run on this worktree's own
Debug build of main `c54434a8d` at BelowNormal priority: about 60 compile-and-run probes, 50 of them on programs written
fresh. Every citation went through `cite.py --check`: 47 checks, all OK after one re-addressing (§8.8.4.2.2's OR-EQUAL
lines carry `<u>` markup, so the figure note is the checkable text). Wave 58's 24 in-flight notes were not touched;
PB1041 names PB999, which wave 58 is implementing.

**Wrong answers (silent).**
- An integer operand narrowed to `int` at emit time (PB1033). GO TO DEPENDING on 4294967297 goes to P1, and a
  ref-mod start of 4294967297 reads position 1 with EC-BOUND-REF-MOD checking on.
- The sort-merge EC family has no raise site: FILE-OPEN, RELEASE, SEQUENCE and ACTIVE (PB1036). `AFTER GS=41` and
  the declarative never runs.
- SIN, COS and TAN narrow an exact argument to binary64 (PB1041). sin(10^30) came out +0.00933 where the true value
  is −0.09012, and SIN(1E400) raised EC-ARGUMENT-FUNCTION.
- A method's USE BEFORE REPORTING never runs (PB1044).
- D-FRA does not round-trip when dynamic members sit on both sides of a fixed member (PB1053). The record reads back
  as `A=[AAKEY] KY=[CCC]`.
- An INVOKE BY REFERENCE numeric-DISPLAY formal copies out through the value channel: `[000]` where CALL gives
  `[   ]` (PB1064).
- A `>>DEFINE` inside a WHEN handler survives GR14's POP ALL (PB1066).
- A fixed-form copybook drops a `>>SOURCE FREE` line silently (PB1067).
- A RETURNING character result is stored with no width fit (PB1040).
- Run-unit state survives `ResetCurrent`, and a process-static scratch slot is shared (PB1069).
- A pointer member's byte view disagrees with DOC-A.1-216 (PB1071). **Owner visibility:** fixing it overturns
  PB231's golden pin.

**Crash.** An oversized report-writer integer (`COLUMN 77777777777`, `PAGE LIMIT 77777777777`) throws an unhandled
OverflowException out of the binder (PB1058).

**Legal source refused.**
- A dynamic table in an EXTERNAL record (PB1042).
- A local X shadowing a container's GLOBAL G.X (PB1047).
- REPORTS ARE R1 R2 on one FD (PB1050), and a method BY VALUE formal (PB1051). Both are COBOLNET0899 stagings found
  by the registrar with no owner.
- RENAMES THRU over a COMP leaf (PB1054).
- A table SORT nested under an OCCURS (PB1055).
- PAGE … integer-2 COLUMNS (PB1059).
- An address-identifier as an EVALUATE subject/object or a function argument (PB1060).

**Under-rejects.**
- `NOT >=` / `NOT <=` / `EQUAL THAN` in relation conditions (PB1034). There is no owner question: vendor extensions
  are parked by owner decision 2.
- §13.18.46.3 SR3 for READ/UNLOCK on a report file (PB1035).
- A zero CONSTANT as an integer-n (PB1039).
- GLOBAL on a 77, a 05 or a class constant (PB1045).
- ADDRESS OF object WORKING-STORAGE (PB1062).
- A restricted pointer BY CONTENT (PB1063).
- FLAG-02/FLAG-14 placement (PB1065).

**Diagnostics and process.**
- CLASS symbolic-character (PB1038).
- LINE-COUNTER under COBOLNET0899 (PB1049).
- A SORT dynamic key reported as a capability limit (PB1052).
- The LENGTH/BYTE-LENGTH 'numeric literal' catch-all and operand text losing its spaces (PB1068).
- The EC raise-site registry (PB1037).
- The report-section name reference question (PB1046, analysis).
- The §13.18.22.3 SR4 reading for EXTERNAL-FD pointer records (PB1043, an adjudication, not an owner question).
- The SR1 ambiguity message in three places (PB1048).
- The deferral censuses with no owners (PB1056).
- PB1030's remaining null arms (PB1057).
- Two integer-literal tests (PB1061).
- RefusalNodeDriftTests' ternary blind spot (PB1070).

**Extended, not duplicated.**
- PB310: COBOLNET0897 and COBOLNET1520 are raw literals.
- PB674: START LESS/`<=` and floating-point literals compile at `--std 85`, and the 85→2002 text is still missing.
- PB244: re-probed; the STRING leg needs an SR1 reading first.
- PB165: the per-formal length registry, shared with PB1040.
- PB651: permissive NUMVAL with two receivers stores scale 9.
- PB977's `closes_rows_reason` was corrected (FMT-12.3.7.2 is CONFORMS since PB829).
- A stale `<see cref="SeedInheritedGlobalIndex"/>` in `DataBinder.Linkage.cs` was rewritten.

**Verdicts.** The w55c pending batch was applied against PB1036: GR-14.9.40.4-12 and GR-14.9.24.4-7/-12 moved
GAP → PARTIAL. **GAP unchanged at 2060.** Seven CONFORMS rows are contradicted by the new notes and claimed, not
re-verdicted: GR-14.9.17.4-2 (PB1033), RV-15.82.4-1, RV-15.20.4-1 and RV-15.89.4-1 (PB1041), GR-13.18.45.4-2 (PB1054),
GR-14.2.3-8 (PB1064) and GR-14.9.28.4-14 (PB1066).

**Not filed.**
- Already fixed on main:
  - GLOBAL RD. PB369 works once the FD is GLOBAL, and COBOLNET2392 is right when it is not.
  - A record-less SD. It is now COBOLNET1837, §13.4.6.3 SR2.
  - PERFORM VARYING §14.9.28.3 SR5 b). COBOLNET2120 screens it.
  - The CALL lane of wave-48 lead 2. PB992 prints `[   ]`.
  - The stale SR-14.9.49.3-1 note text.
  - The PB993 finisher plan. It was implemented in w55c.
- Did not reproduce: the override ADDRESS-OF CS0115 (COMP-2 and COMP-5 both run).
- Not discriminated by its probe: the TYPE-clone index in a method.
- ISO-correct:
  - BLOCK CONTAINS 0 is refused by §5.5 1); the IBM idiom is a parked vendor extension.
  - SR-13.18.38.3-28 is enforced (COBOLNET1522, OccursDynamicGuardTests), and FROM 0 is accepted. It needs a verdict
    from the golden lane, as does the unwitnessed GR-13.18.2.4-1.
- D-DECLREF's GnuCOBOL severities belong to the lander's differential.

