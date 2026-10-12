## Entry 1603 — 2026-09-13 13:30 PDT — Registrar #5: the post-registrar-#4 report backlog becomes five notes and four extensions (PB871–PB875) — and the pass's own edit exposed a gate that fails OPEN

**The pass.** A LEAD-FILING pass over the "New defects" sections of the twenty-four implementer, finisher and
lander reports that arrived AFTER registrar #4 landed `a1d1b200`. Method unchanged from #4: every lead's probe
was REBUILT from the report paragraph and RE-RUN on this worktree's own `dotnet build CobolSharp.sln -c Debug`
at `a2255819`, and every citation re-derived with `python scripts/spec/cite.py --check` before it entered a note.
Thirty-eight leads were read; **twenty-six were already owned** by PB835–PB870 or by an older note and were
dropped with the owner named, which is the pass working as intended — registrar #4 had already filed most of
this backlog.

**Notes filed (5), ids contiguous from PB871; last id used PB875.**

* **PB871** (MAJOR · wrong_answer, silent) — a DYNAMIC-LENGTH elementary item as a RECEIVING operand of any verb
  but MOVE stores through its PICTURE's ONE character position. Measured: `STRING "XY" INTO` an item holding
  eight characters leaves `LEN=0008 [XYCDEFGH]`; `UNSTRING` and `ACCEPT FROM DATE` both store `LEN=0001`.
  §8.5.1.10.4 is written over "a receiving operand", not over MOVE. **The report's INSPECT claim is REFUTED** —
  INSPECT as a sender reads the current length (`CT=008`) and INSPECT REPLACING preserves it — and a fourth arm
  the report did not derive is added: `INITIALIZE` gives `LEN=0000` where §8.3.3.6.4 GR3 b) makes the figurative
  sender one character. `grep` proves the mechanism is singular: `StringEmitter`, `AcceptDisplayEmitter`,
  `InspectEmitter` and `InitializeEmitter` contain no occurrence of `IsDynamicLength` / `DynStore` / `DynMaxSize`;
  the only receiving-store arm is one `if` written twice inside `MoveEmitter`.
* **PB872** (MINOR · process_only) — `DeclaredForest()` yields every LINKAGE root TWICE (`BindEntries` adds it to
  `_roots` unconditionally at `DataBinder.cs:750` AND `CallBindLinkage` adds it to `LinkageRoots`), so one
  violation draws two diagnostics. Measured with the control and the subject in ONE compilation: `WSA` once,
  `LKA` twice. The method's own doc comment states the violated invariant. The ALIGNED gate hides it by mutating
  `IsAligned` — so four gates' silence is evidence about their mutation, not about the forest.
* **PB873** (MAJOR · wrong_answer, silent) — a SIGNED numeric argument loses its operational sign crossing into a
  SEPARATELY-COMPILED program whose formal is image-carried: `00123D` (+4) where the local control shows `00123M`
  (−4). BOTH arms measured (`CallAbi.Text` :358 and `CallAbi.TextValue` :408, `Signed = false` hard-coded in
  each). §14.2.3 GR11 makes the LINKAGE description authoritative. `kb/Work/PB834` recorded this lead and
  explicitly declined to claim it; this is the note it asked for. **The shape that hid it is recorded**: make the
  CALLER's argument image-carried too and the image travels intact and the probe passes.
* **PB874** (MINOR · process_only) — the NIST differential, the only wave-local leg whose oracle COBOL.NET did
  not author, is in the FILTERED assembly: `build-local` runs Unit and Characterization unfiltered and
  Conformance under the caller's `-Filter`, and `NistDifferentialTests` (356 cases, measured) lives in
  Conformance. PB837 is exactly the class of defect this hides.
* **PB875** (MINOR · process_only) — **found by this pass, on this pass.** `DefectiveRowCoverageDriftTests`'s
  frontmatter reader tests `value.StartsWith('[') && value.EndsWith(']')` and otherwise takes `[]`, so a note
  whose `inventory_rows` list is WRAPPED across two lines silently claims NOTHING. Extending PB390's list in the
  ordinary YAML way turned the gate red naming PB390's four ORIGINAL rows; re-joining the list onto one line,
  with no other change, turned it green. It fails OPEN twenty lines below a doc comment promising the opposite
  default, and `work.py check` calls the same note well-formed — two readers, one file format. Exposure measured:
  one note carries the shape today (PB205) and it is `landed`, so no row is uncovered — the gate is green for the
  right answer and the wrong reason.

**Notes extended rather than duplicated (4).**

* **PB390** — five more sites of its own mechanism (a decided SYNTAX RULE shipped as `BoundUnsupported`):
  `SearchBinder.cs:27/:29`, `ReportWriterBinder.cs:65/:96/:106/:123`, `EvaluateBinder.cs:55`,
  `Bound/StatementBinder.cs:452`, both run-time aborts re-measured. **The SEARCH message's rule number is WRONG,
  not merely truncated** — it says §14.9.37 SR1, which is the reference-modification rule; the INDEXED-phrase
  rule is SR2. Three unowned GAP rows claimed (`SR-14.9.16.3-1`, `SR-14.9.21.3-1`, `SR-14.9.46.3-1`);
  `SR-14.9.37.3-2` deliberately left with `kb/Work/PB443`. The CORRECT shape is already in the same file,
  seventeen lines below, at the SUPPRESS site.
* **PB838** — the auditor's SECOND blindness: a citation that omits the subclause (`§14.9.37 SR1`,
  `§14.9.13 SR`) is invisible to `audit_code_citations.py`, and these three are in SHIPPED DIAGNOSTIC TEXT —
  one step closer to the user than a code comment. Stated as a rule the auditor can decide from the literal alone.
* **PB853** — the converse arm measured, and it ANSWERS the PB504 lead's open adjudication from §13.15.3 SR10
  itself: `03 COLUMN 1.` (COLUMN, no PICTURE) is refused — correctly in outcome, under §13.16 rather than under
  the rule that decides it — while `03 COLUMN 1 PIC 999.` sails through. One screen, keyed on the wrong attribute.
  `SR-13.15.3-10` therefore needs BOTH goldens, or it closes on a witness that proves nothing.
* **PB482** — the SUM clause's OTHER operand: `UPON D-LINE OF R-TWO` loses its qualifier and binds to the current
  report's own detail. Measured wrong answer on legal source — `005` where the counter names a detail that is
  never GENERATEd, so `000` is owed; §13.18.54.3 SR7 admits exactly one qualifier and the binder admits none.
  Claims `SR-13.18.54.3-7` (GAP, unclaimed). The PB489 report asked for this to fold into PB205; PB205 is
  `landed`, so folding it there would have hidden it from `work.py next` — it went to PB482, which is open and
  owns the addend four lines away.

**Leads dropped, with the owner named (26).** PB506 A/B/C → PB852 / PB551 / PB853. PB721 a/b → PB858 / PB859.
PB526 a → PB854; b was already recorded NOT REPRODUCED by #4. PB646-fin N1/N2/N3 → PB856 / PB541 / PB839.
PB355 1/2 → PB838 / PB839. PB344-fin2 a → PB837. PB394 1/2 → PB394 itself / PB842. PB415 a/b → PB844 / PB845.
PB640 a/c → PB834 / PB841. PB367b a/b → PB840 / PB841. PB256 A/B → PB835 / PB869. PB829 a–e → PB862 / PB829.
PB771 a/b/c → PB860 / PB861 / PB833. PB445-fin → PB846. PB500 1/2/3 → PB851 / PB839 / PB838. PB337 1/2 → PB836 /
PB839. PB398 A/B → PB843. PB530 N2/N3 → PB855. PB562 → PB736 (which already carries the fixed shared temp path
as the root cause). PB535 → cross-references only. **PB530 N1 was re-measured and is DISCHARGED**: row
`SR-13.18.40.3-24` already carries `conformance:negative/pb530-picture-editing-extended-count` in its `test-ref`.

**Gates** (final tree): `dotnet build CobolSharp.sln -c Debug` 0 errors · `work.py check` ✓ 908 work items, all
well-formed · the three drift tests `Passed! - Failed: 0, Passed: 47` (RED first, on PB875's mechanism — recorded
above rather than quietly fixed) · `audit_doc_citations.py --check` 441 checked, ⛔ 0 MISFILED ·
`gen_conformance_notes.py` 4348 items · 2501 GAP, no diff. The GAP is unchanged by design: this pass files
defects and moves no verdict.

