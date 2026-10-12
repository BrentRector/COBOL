## Entry 1636 — 2026-09-22 11:42 PDT — REGISTRAR #10 over the wave-45 reports: seven notes filed, four leads dropped with the reason, and the loudest one — a fixed-length group reaching a variable-length formal EMPTY — reproduced on main today

The lead-filing pass over wave 45's seven implementer results (groups A–G) and the train-46 lander report. Train 46
had NOT landed when this ran, so main was `f503888da`. Every probe was re-run on this registrar's own
`dotnet build CobolSharp.sln -c Debug` (0 warnings / 0 errors). Every citation was re-derived with `cite.py --check`.
Every lead was grepped against `kb/Work/` before it became a note. No verdict moved: **GAP 2276 before and after.**
The register goes from 993 to 1000 notes.

**Filed (seven):**

- **PB961** (MAJOR, wrong answer, silent). `SET CONTENT OF <FLOAT-BINARY-32> TO FLOAT-NOT-A-NUMBER-SIGNALING` stores
  `0x7FC00001`, a QUIET NaN. The emitted `Int32BitsToSingle(0x7F800001)` is right. `CobolNum.FormatImageFloat` takes a
  `double`, and the float→double widening sets the quiet bit. binary64 is correct. Group A's lead said "likely the JIT
  constant-folds through a double". The mechanism is the image lane's parameter type, read off the emitted C#.
  DOC-A.1-176's "cannot quiet the value" is false for binary32. Re-measured through a REDEFINES window, because
  PB225's class condition is not on main.
- **PB962** (MAJOR, crash). A conforming `PIC 9(3)` RETURNING pair whose content is not a digit image (spaces, after a
  zero-length MOVE) aborts with EC-PROGRAM-ARG-MISMATCH, because `CallAbi.StoreReturn(string)` re-parses the text.
  §14.8.3.3 admits the pair and §14.9.4.4 GR4 requires the placement. The design comment's premise, that a
  cross-carrier pair implies a non-conforming one, is what fails.
- **PB963** (analysis). GR-7.3.25.4-5/-6/-8 are GAP and unclaimed. Two probes show the >>TURN file-name-1 ON and OFF
  arms behaving to the rule on main, including §14.6.13.1.3 5)'s termination after the declarative. The GR5
  within-a-statement sentence has never been measured. This is a golden-round item that becomes a defect only if GR5
  misbehaves.
- **PB964** (MAJOR, wrong answer, silent). On a LINE SEQUENTIAL file, a WRITE … AFTER ADVANCING followed by a plain
  WRITE writes `AAAAABBBBB` on ONE physical line. A COBOL read-back with the same record length splits it again,
  which is why nothing saw it. Group D found it on train 46's new `_lineOpen` state, but it **reproduces on main**, at
  `SequentialConnector` ≈980, where the `!_lineSequential` guard keeps §14.9.51.4 GR25's implicit AFTER 1 LINE off
  line-sequential files. Clustered with PB864.
- **PB965** (MAJOR, wrong answer, silent). A fixed-length group passed BY REFERENCE to a variable-length formal
  arrives EMPTY (`LCAP=0`, blank fields) and the callee's store never comes back. Reproduced in the nested CALL AND
  in a separate sibling program. PB204's goldens only ever measured the VLG↔VLG pair. The THROUGH-alias form group F
  reported is still refused on main, and becomes this same silent wrong answer when PB907 lands. **This is the one an
  implementer should take first.**
- **PB966** (MAJOR, rejects legal source). MOVE CORRESPONDING refuses a level-66 THROUGH alias with COBOLNET1757,
  although §13.18.45.4 GR2 makes it a group and §14.9.25.3 SR12 asks for nothing more. The note carries the §14.7.6 1)
  qualifier-chain reading the fixer must survey and record.
- **PB967** (analysis, process). No `audit_*.py --check` runs in `build-and-test.yml` or `push-main.sh`, so the required
  `ci-gate` enforces none of the citation or witness invariants. Some audits read the private `specs/` submodule that
  CI deliberately does not fetch, so the note's first step is to classify each script spec-free or spec-bound by
  running it without the submodule.

**Dropped, with the reason:** group A's `Pic?.Category`-guarded-by-`IsGroup` sweep, because the reporter measured it
behaviour-neutral today and PB760 owns the family. Group B's PB887 remainder and group E's PB843 remainder, because
both are already open notes and their own implementers' reports carry the split. Group D's Linux read-only sharing
lead, because it is PB833.

