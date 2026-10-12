## Entry 1784 — 2026-09-29 14:53 PDT — Train 74: RETRY waits for outside holders, '49' first, SELECT/FD pairing (B2); separators and literal repertoire (A2); externalized names, static storage, >>PROPAGATE, COMMON scope (D2)

Train 74 carries the three wave-73 groups whose implementers died near the end (A, B, D). Finishers completed each
from its checkpoint: B2 and D2 are SPLIT, A2 is DONE. Each cluster was applied from its branch diff onto train 73's
main (`66c0eebac`), with the implementers' own DEVLOG hunks left out in favour of this one entry.

**Cluster B2 — PB1163, PB1194, PB1077 (+PB1237, PB1290); SPLIT (PB322 and PB833 not started).**
- **PB1163.** RETRY FOREVER now waits for a holder outside the run unit. The loop used to assume that no holder could
  let go, so FOREVER gave up after one re-attempt even when another process released the file moments later. Every
  attempt now says who refused it (`Runtime/IO/RetryAttempt.cs`). `FileRegistry.RetryLoop` keeps polling FOREVER
  only while the holder is outside the run unit, and paces re-attempts against such a holder at `RetryInterval`
  (100 ms, the §14.7.9.3 GR1 implementor interval, A.1 item 165). A holder inside the run unit still ends in the A.1
  item 109 deadlock status. DELETE FILE asks the sharing question only after GR13, because the host probe cannot tell
  this connector's own handle from a foreign one. The test seam is `PauseObserver`, which reports a pause without
  sleeping, so no test times anything.
- **PB1194.** REWRITE and DELETE through a connector not open I-O answer '49' before any lock check (§14.9.35.4 GR3,
  §14.9.10.4 GR1). This is one non-virtual `MutationTargetRecordId` template over `MutationOpenModeGuard`; it
  replaced five hand copies across the three organizations. `LocksEffective` now needs an open connector.
- **PB1077 / PB1237 / PB1290.** SELECT/FD pairing is now enforced in both directions:
  - a second SELECT of the same file-name (§12.4.5.2 SR2, COBOLNET2634);
  - a SELECT with no FD or SD (SR3, COBOLNET2635);
  - an FD or SD that no SELECT names (§13.4.5.3 SR1 / §13.4.6.3 SR1, COBOLNET2636).

  A class-level FILE-CONTROL, which §12.4.3 SR1 already forbids, is left to PB1076.
- **Goldens.** `2002/w73b_pb1194_mutation_open_mode_first`, `85/w73b_pb1077_select_scope_per_program` and four
  negatives. Gate 2 on the branch caught a version-matrix probe with an SD and no SELECT, which is now illegal
  source, and the probe was corrected.
- **Rows.** Batches pb1163/pb1194/pb1077: 8 rows CONFORMS. PB322 (14 rows) and PB833 (GR-9.1.15-2, DOC-A.1-75) stay
  open with their mechanisms named in the finisher's report. Codes 2634–2636 were used and 2637 is returned.

**Cluster A2 — PB1394, PB1441, PB1728, PB1162; PB758 re-scoped.**
- **PB1394.** `Frontend/Parsing/SeparatorRule.cs` decides the §8.3.5 separator-context rules once, after lexing,
  from the source characters beside each token. Before, `MOVE 1 TO N,M` printed `001001`, `DISPLAY N.DISPLAY M`
  ran as two statements, and `DISPLAY "AB"N` compiled.
  - Rule 2: a comma or semicolon must be followed by a space (COBOLNET2631). The decimal comma under DECIMAL-POINT
    IS COMMA is exempt.
  - Rule 3: a period must be followed by a space (2632).
  - Rule 5: a literal's delimiters must be separated (2633).

  A lone `;` moved to the HIDDEN channel so the rule sees it. The parse-recovery hints COBOL0301/0302, which
  restated rule 5 only after a parse error, are deleted.
- **PB1441.** Every prefixed literal body lexes as `<prefix> STR_BODY`. The content repertoire of X/NX/BX/B literals
  (§8.3.3.2.3 SR5, §8.3.3.5.3 SR4, §8.3.3.4.3 SR2/SR3) is `CobolLiteral.RepertoireViolation`, inside the one ordered
  `CobolLiteral.SyntaxViolation` (COBOLNET2630). Directive operands ask the same entry, and `LiteralScreenDriftTests`
  pins exactly two callers. `DecodeHex` became `DecodeHexGroups`, which never throws.
- **PB1728.** `CobolLexer.g4#CutTokenTo` is the only way a lexer action gives characters back (seek plus column).
  Both PIC_STRING trims use it, including the trailing-period trim, an unreported sibling. A token differential over
  17,055 inputs changed columns only.
- **PB1162.** It no longer reproduced (fixed by PB1465). Its witness goldens landed and its six rows were re-verdicted.
- **PB758.** It reproduces, but D-RW1 (PB655) documents END-INVOKE as an extension reserved word, so the lexer fix
  it names would overturn a determination. Its residue is the §4.2.10 3) warning mechanism, which **PB1525 already
  owns**. The lander found that note and recorded the pointer in PB758 instead of filing a duplicate.
- **Rows.** Batch w73a: 13 rows CONFORMS.

**Cluster D2 — PB1539, PB1069, PB1119, PB1460; SPLIT (PB1422 not started, codes 2644/2645 returned).**
- **PB1539.** An externalized name is formed and mapped by ONE function, `Runtime/Control/ExternalizedNames`, per
  DOC-A.1-68: leading and trailing spaces are removed, and names compare ignoring case. Before, an AS literal was
  registered untrimmed while every CALL target was trimmed. The rule is pinned by
  `ExternalizedNameFormationDriftTests`. An AS literal written with padding warns (COBOLNET2642), and an all-space
  one is refused (2643).
- **PB1069.** Static storage is adopted by the run unit: `RunUnit.AdoptStaticStorage` at first activation, and
  `RunUnit.Terminate` resets it at the end. `Terminate` is the one epilogue that both `ProgramTable.RunMain` and
  `RunUnit.Run` call (§14.6.2.3.2 case 1; §14.6.11 2/3/4/6).
- **PB1119.** >>PROPAGATE ON now does something. The toggles fold per program, function and method
  (`PropagateState`, over the shared `DirectiveTimeline`, whose copy in RefModZeroLengthState was deleted). At the
  one fatal default, `EcEmitter.EmitSelection`, an unhandled fatal condition (-3) is staged as the last exception
  and the element returns as a GOBACK would (§14.6.13.1.3 6); §7.3.21.4 GR1–GR4). `__IoCheckEc` does the same for
  EC-I-O, and the CALL/INVOKE pickup does it for exception objects (§14.6.13.1.5 item 3). An element not under
  PROPAGATE ON emits byte-identical code. Determinations are in CONFORMANCE.md D-PROP. A main program propagates
  nothing. A nonfatal level-3 condition is not propagated, a reading of §7.3.21.4 GR2 that the owner may confirm.
- **PB1460.** `ProgramNameScope.CommonProgramReferable` is the one spelling of §8.4.6.3 2)'s COMMON exception. The
  bind-time AS NESTED table and `ProgramTable.ResolveVisible` both ask it. Before, `CALL … AS NESTED` of a
  non-recursive COMMON program from inside its own subtree compiled clean and then raised EC-PROGRAM-NOT-FOUND.
- **Rows.** Batches w73d-PB1539 (re-sited), w73d-PB1069, w73d2-PB1119 and w73d2-PB1460. DOC-A.1-68 stays DIVERGES on
  its CALL-CONVENTION arm, which PB1383 owns.
- **Leads.** Two were filed. `BinderDriver.NestedCallablesOf` keys on the program-name where the run time keys on
  the externalized CallName (**PB1756**; reasoned from the code, repro not yet built). The second lead was
  discharged, not filed: the CLI refuses `2014/propagate_directive.cob` with COBOLNET2616 where the corpus harness
  passes it, but the CLI is right. Its `>>` sits in the column-7 indicator area, the CLI's default reference format
  is FIXED (PB1362, §7.3.24.3 2)), and the in-repo harnesses select `auto` by the design PB1362 recorded.

**The train.**
- **Merge.**
  - Every conflicted corpus manifest was whole-element and taken both ways. Each enabled count was checked against
    main plus the cluster's additions, and there are no duplicates.
  - `DiagnosticCatalog.cs` and `DIAGNOSTICS.md` were taken both ways in code order.
  - `DRIFT_RULES.md` was regenerated (238 drift tests).
  - One real code conflict was merged by hand. Train 73's PB1445 added the §8.3.3.3.2 2) SIGN screen to
    `LiteralScreenPass` while A2 moved its token set to `LiteralTokens.Types` and its rules to
    `CobolLiteral.SyntaxViolation`. The merge keeps both: the sign walk, and the one-entry token screen. The
    `DESIGN-version-conformance-pipeline.md` row now names all four rules.
  - The inventory hunks applied cleanly. A dry run of all eight verdict batches on the merged tree changed 0 GAP.
- **Gate.** The lander gate was GREEN on run `20260929T214214Z-fddf65`: Conformance 9,381/9,381, Unit
  29,827/29,827, Characterization 35/35. The legacy Integration suite passed (503 passed, 1 skipped).
  `=== LINUX GATE: GREEN ===` on `80eff3e75`.
- **Semgrep.** `cobolnet-raw-diagnostic-code-literal` went 301 → 297, and the baseline is locked there. No count
  rose.
- **CI audits run locally.** Code citations 0, doc citations 0, evidence supersession 0, witness loss GREEN,
  drift_rules current, `work.py check` OK.
- **Review.** A full-code pass over the train's source diff found 0 findings and dropped 0 clusters.
- **Result.** **GAP 849 → 818.** No cluster was dropped. Codes claimed: COBOLNET2630–2636 and 2642–2643. Leads
  **PB1755** (EditionHarness copybook write race) and **PB1756** were filed; PB1757–PB1759 are returned unused.

