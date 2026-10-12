## Entry 1646 — 2026-09-22 17:22 PDT — Battery #85 at train 48's head: every compiler leg green, three differential flips, all three conformance fixes

**What ran.** One `bash scripts/battery.sh` in an isolated worktree pinned at main `0abe2c80f` (train 48), after copying
the git-ignored GPL GnuCOBOL corpus in. 1,230 s wall. It covers trains 47 and 48 together (DEVLOG 1642 and 1644),
plus registrar #11 between them: 337 files under `src/` and `tests/`, +9,563 / −1,944 against battery #84's head
`ec095d120`.

**Every compiler leg is green.**
- Conformance **7932 / 7932**, full and unfiltered (12 m 15 s). Unit **28746 / 28746**. Characterization **33 / 33**.
  Both populations match train 48's own whole-assembly gate exactly. Over #84 that is +109 and +58.
- The guard's evidence-rule witnesses and the compiler-identity watchdog are green. NIST (cobol) gave
  **364 MATCH / 0 REGRESSION**, and the audit is CLEAN over a declared population of 376. The legacy legs gave
  1203 / 1203 and 503 / 504, with 1 skipped.
- All four static audits are clean. `audit_code_citations` found 0 over 4,807 files. `audit_doc_citations` found
  0 MISFILED and 0 ELIDED, with 563 checked and 512 correct. `audit_evidence_supersession` found 0 UNMARKED.
- The battery's `audit_witness_loss` compares against the merge-base. At a head that IS main, that comparison
  measures nothing. So I also ran it `--base ec095d120` over the whole two-train span: 0 unexcused, 2 retired,
  3 re-sited, GREEN.
- The solution build had 0 warnings and 0 errors.

**The differential flipped three cases, so the script said `NOT GREEN (rc=1)`.** That is a miss against #84's 0-flip
bar, and it is recorded as one. All three went `WE_REJECT_THEY_ACCEPT → AGREE_ACCEPT`, and all three had been
`COBOL0001` parse errors. I compiled and RAN each one here, and each printed GnuCOBOL's expected output.
- `run_extensions:2101` and `run_misc:190` are header-less `PROGRAM-ID.` programs. §11.2.1's general format
  brackets `[ IDENTIFICATION DIVISION. ]`, so the header is optional. Train 48 made it optional as the PB829
  residue, gated below 2002 as `identification-header-optional-2002`.
- `run_extensions:2101` also nests a program named `prog` inside `PROG`. By COBOL word equivalence that is the same
  name. Neither §8.4.6.3 ("the names assigned to programs that are contained directly or indirectly within the same
  outermost program shall be unique within that outermost program") nor §11.10.3 SR3 refuses it. Both constrain the
  programs CONTAINED within the outermost program, and the outermost is not one of them.
  `CheckDefinitionNameUniqueness` reads the rule the same way.
- `run_misc:3430` is `SORT ROW1` with the statement's KEY phrase omitted. Both tables' OCCURS clauses carry KEY
  phrases, so §14.9.40.3 SR15 admits it, and §14.9.40.4 GR21 takes the order from the table (PB846, train 47). The
  output was `0403020101020304`, as expected.
- Every citation above went through `cite.py --check`. The first one I tried, `§11.10.2` for SR3, FAILED the check.
  The rule is in §11.10.3.
- I re-baselined exactly those three rows. A re-run of the differential over the corrected baseline prints
  `=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`. The totals are now 577 / 469 / 237 / 40 (they were 580 / 466 / 237 /
  40). There was no NEW or REMOVED case and no case without a verdict.

**Complement.** Trains 47 and 48 added nine codes: COBOLNET2234, 2237–2243 and 2247.
- Only COBOLNET2241 fires on the corpus (§12.3.7.3 SR8, the implementor-name table from PB716/PB862). It fires
  once, on `syn_misc:1583`, which stays `AGREE_REJECT`.
- The other eight fire on nothing. The corpus is no evidence about them, and their evidence is their goldens.
- Fifteen cases kept their verdict but changed their reason:
  - Seven gained only the COBOL0307 hint beside their parse error.
  - One gained COBOLNET2241.
  - Seven went from a parse error to a deeper, cited reject, because the grammar now admits the header-less and
    KEY-less shapes. Examples: `run_extensions:2149` now gets COBOLNET1795 (§11.10.3 SR2), and `syn_misc:6499` gets
    COBOLNET1757 (the refusing half of SR15).

**Record.** Plan §0 BATTERY REFERENCE is rotated: CURRENT is #85, PREVIOUS is #84 (compressed), and #83 is dropped.
One sentence is appended to the in-flight panel of `ledger-in-flight.md`. The baseline TSV changed by the three
rows. Nothing was filed in `kb/Work/`: no red went unattributed, and no defect was found. Battery #86, at the next
train's head, must be ALL GREEN with 0 flips.

