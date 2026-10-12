## Entry 1672 — 2026-09-24 09:56 PDT — Registrar 14f: wave-58/59 leads filed as PB1310–PB1324, eight notes extended, six CONFORMS rows re-verdicted PARTIAL

**What this was.** Registrar 14f ran a lead-filing pass, not an adjudication batch, over `leads-w58.md`. That file
held 53 leads from the wave-58 implementers (KA–KJ), battery #86, registrar #13, and the train-58/59 landers.
Duplicates collapse them to about 30 distinct findings. Every lead was grepped against `kb/Work/` first. Every
repro was re-run once on this worktree's own `dotnet build CobolSharp.sln -c Debug` of main `071ff04ea`: 26
programs, 9 of them written fresh because the lead named only a code site. Every citation went through
`cite.py --check`.

**Fifteen new notes, one per mechanism.**
- Wrong answers, all silent:
  - **PB1310**: the binary64 intrinsic lane loses a tiny argument to cancellation. `ANNUITY(1E-20 12)` stores
    0 (§15.9.4 1) b) gives about 1/12), and `ACOS(1−1E-30)` gives 0.
  - **PB1317**: EC-BOUND-OVERFLOW with RESUME NEXT STATEMENT abandons the implicit growth. Registrar 14f
    MEASURED it: CAP stays 1 and E(5)=000, where §8.5.1.9.6 1) says the operation continues. The KE report had
    it from code reading only.
- Crashes:
  - **PB1318**: `DISPLAY LINAGE-COUNTER` aborts the run unit ("computed expression in a string context").
  - **PB1323**: `PROCEDURE DIVISION USING p p` reaches Roslyn (CS0102 `__lnk0`, exit 70). §14.2.2 SR1 is
    enforced nowhere.
- Rejects legal source: **PB1316**, a constant-name as a report SOURCE operand, COBOLNET0899 (§13.10.3 SR2).
  Registrar 14f found it itself while probing PB1045.
- Diagnostic quality: **PB1312** (UsageInheritancePass judges an implied or promoted picture as written: a
  false 0881, and BWZ reported under SR3 instead of §13.18.8.3 SR2), **PB1313** (about 30 `GetText()` message
  sites erase OF/IN), **PB1314** (SET … TO FALSE reported as TO TRUE), **PB1315** (COBOLNET1727 and the RD SOURCE
  refusal carry no location) and **PB1321** (a re-coded SEARCH phrase is followed by ANTLR's own COBOL0001).
- Process and structure: **PB1311** (the ANNUITY / PRESENT-VALUE Dec lanes re-screen their domain), **PB1320**
  (no drift test that every `.g4` parser rule has a reader), **PB1322** (orphan `<summary>` blocks and a stale
  `Gr3Width` cref; `GenerateDocumentationFile` is off, so no cref is ever resolved) and **PB1324** (the
  implementer seam filter never selects `*DifferentialTests`).
- Analysis: **PB1319**, the COBOL-85 SIGN rules were never read.

**Eight notes extended rather than duplicated:**
- **PB917**: duplicate PIC/VALUE compile clean, and `UnrepeatedElements.AtMostOnce` is the landed mechanism.
- **PB493**: a report group SIGN prints `00N` for −5.
- **PB1059**: `LAST CONTROL HEADING` has no grammar surface, and the note now claims `SR-13.18.39.3-4`.
- **PB906**: three more stale `GuessCobolIntent` hints (COBOL0100/0103/0107).
- **PB376**: five recurrences, plus registrars 14e and 14f overwriting each other's scripts in one shared
  scratchpad, the class this note names.
- **PB1045**: GLOBAL residences a), d) and e) are measured as accepted. c) cannot be measured, because the
  screen module is not provided.
- **PB942**: the "widened" lead does not reproduce, only one 1625.
- **PB113**: its LOCALE+SIGN non-arm is corrected; §13.18.52.3 SR1's report bullet excludes the pair.

**Discharged with evidence, no note:**
- A `>>TURN` in fixed-form area B is honoured on this build, and §7.3.3 SR3 allows it.
- `05 B REDEFINES A TYPE T` is illegal (§13.16.3 SR14, COBOLNET2150), so the "type-supplied VALUE under
  REDEFINES" question cannot arise. Its cascade 0881 went into PB1312.
- `MOVE ALL "57" TO ZZ9` at 85 prints 575, which is the value under either reading.

**Verdicts.** Six CONFORMS rows that an open note contradicts, each note's repro re-run here, were re-verdicted
PARTIAL through `record_verdicts.py`: GR-14.9.17.4-2 (PB1033), GR-13.18.45.4-2 (PB1054), GR-14.2.3-8 (PB1064),
GR-14.9.28.4-14 (PB1066), RV-15.9.4-1 (PB1310) and FMT-13.16.2 (PB917).
**GAP 1839 → 1845** (+6).

PB1041's three RV rows were NOT re-verdicted. Its headline repro, SIN/COS of 10**30, now prints the correct
−0.09011690, which PB999's re-landing in train 59 fixed. PB1041 needs a discharge check, not a re-verdict.

**Golden and adjudication lane inputs, not notes:** SR-13.18.38.3-28 (enforced, COBOLNET1522, no verdict),
GR-13.18.2.4-1 (unwitnessed), FMT-8.8.4.7.2 and FMT-11.9.10.2 (the figurative half is enforced, adjudication
only), and SR-14.9.40.3-14 c)/d) (unprobed). PB1071's owner-visibility item is already in its note.

**Finisher mechanics.** Registrar 14f stopped on the STOP file while it waited for 14e, with its work in a WIP
commit (`d1ca1d474` on `071ff04ea`). The finisher waited for Registrar 14e to reach origin/main, then cherry-picked
the notes onto it (`fc95fb889`). It took origin/main's `traceability-inventory.json` and re-recorded the batch with
`record_verdicts.py` on that tree, never merging JSON hunks. The batch was regenerated first, and it asserted that
all six rows were still CONFORMS. Result: rows changed 6, **GAP 1839 → 1845**. Three notes conflicted with the
14a/14d extensions, and both sides of each were kept. PB917 kept 14a's file-control sibling. PB1059 took the union
of the frontmatter, so 14d's cluster and rows stay alongside 14f's PB1316. PB1045 had a real conflict: 14f's
closing line said the 77 and 05 arms were still open, but 14d had re-probed them and found them refused
(COBOLNET2404, PB518), and 14f never re-measured them. The line now says the open part is the SR4 arm.

**The re-verdicts falsified ten landed closes_rows claims.** The whole Unit assembly caught this with
`ClosesRowsBackLinkDriftTests`; 14f's filtered gate had not. The rows had been claimed closed by PB1004, PB441
(GR-14.9.28.4-14), PB395 (GR-14.9.17.4-2), PB487, PB511, PB829 (FMT-13.16.2), PB56 (RV-15.9.4-1), PB907, PB96
(GR-13.18.45.4-2) and PB992 (GR-14.2.3-8). Each claim was withdrawn from `closes_rows`, with a paragraph in the
note naming the open owner, as 14a did for PB829. Where a list emptied, a `closes_rows_reason` was written.

**Gate** (finisher worktree, on `fc95fb889` + this change): `dotnet build CobolSharp.sln -c Debug` exit 0. The
WHOLE Unit assembly gave `Failed: 2, Passed: 29188`. The two reds are `ExternalCorpusPopulationDriftTests`, and
both are attributed: "EXTERNAL POPULATION ABSENT - no corpus at tests/external/gnucobol/tests/testsuite.src",
because the GPL corpus is never fetched into a worktree. `work.py check` reported 1325 work items, all
well-formed.

