## Entry 1663 — 2026-09-23 09:27 PDT — Battery #86 at train 57's head: every compiler leg green, seventeen differential flips, all seventeen licensed by the standard

**What ran.** One `bash scripts/battery.sh` in an isolated worktree pinned at main `c54434a8d` (train 57), after
copying the git-ignored GPL GnuCOBOL corpus in. 1,093 s wall. It covers trains 49 through 57 (DEVLOG 1648–1661),
registrar #12 and the ledger entries between them: 877 files under `src/` and `tests/`, +36,993 / −5,513 against
battery #85's head `0abe2c80f`. The inventory GAP moved 2198 → 2060 over the span.

**Every compiler leg is green.**
- Conformance **8350 / 8350**, full and unfiltered (11 m 50 s). Unit **29097 / 29097**. Characterization **33 / 33**.
  Over #85 that is +418 and +351.
- The guard's evidence-rule witnesses and the compiler-identity watchdog are green. NIST (cobol) gave
  **364 MATCH / 0 REGRESSION**, and the audit is CLEAN over a declared population of 376. The legacy legs gave
  1203 / 1203 and 503 / 504, with 1 skipped.
- All four static audits are clean. `audit_code_citations` found 0 (5,164 files scanned). `audit_doc_citations`
  found 0 MISFILED and 0 ELIDED, with 639 checked and 583 correct. `audit_evidence_supersession` found 0 UNMARKED
  (11 non-gating drifts).
- The battery's `audit_witness_loss` compares against the merge-base, which at a main head measures nothing. So I
  also ran it `--base 0abe2c80f` over the whole nine-train span: 0 unexcused, 18 retired, 24 re-sited, GREEN.
- The solution build had 0 warnings and 0 errors.

**The differential flipped seventeen cases, so the script said `NOT GREEN (rc=1)`.** That is a miss against #85's
0-flip bar, and it is recorded as one. I extracted every case, compiled it, ran the run-cases, and derived each
verdict from the standard. Every citation below went through `cite.py --check`.

- **Eight rejects became accepts.** Each run-case printed GnuCOBOL's expected output.
  - `run_fundamental:2459 / 2483 / 2507 / 2532 / 2577` use YYYYMMDD, YYYYDDD, INTRINSIC, RECURSIVE and
    AWAY-FROM-ZERO as data-names. §8.10 lists each one as a context-sensitive word, "reserved in the specified
    language construct or context", and nowhere else. PB655 + PB764 (train 54, `ae7719efd`) put the §8.9 gate on
    both parse paths.
  - `syn_functions:64` is `FUNCTION pi e intrinsic`. The intrinsic-function-specifier of §12.3.8.2 takes a LIST of
    names before INTRINSIC. PB974 (train 51, `ebc9c4994`) took the grammar from one name to a list.
  - `run_fundamental:1317` calls `f1()`. The §8.4.3.2.2 function-identifier makes the argument list inside the
    parentheses optional, and §8.4.3.2.3 SR5 even requires the empty form for a function-pointer. PB969 (train 51,
    `ebc9c4994`) admitted it. The output was `0.0004,00Y`.
  - `listings:1055` writes `ASSIGN TO DISK TEXTFILE-1-NAME`. §12.4.5.1 prints `{device-name-1 | literal-1} …`,
    and §12.4.5.2 SR5 leaves the allowable lists to the implementor. DOC-A.1-71 records the device-word-plus-name
    form. PB829 (train 49, `30f021356`) made ASSIGN TO a list.
- **Five accepts of GnuCOBOL-rejected source became agreed rejects.** Each rejected source breaks a rule:
  - `syn_definition:2853` has a USE outside DECLARATIVES, against §14.9.49.3 SR1. The code is COBOLNET2377, from
    PB361 (train 56, `a714fcb0e`).
  - `syn_file:999` has `GO TO` into a declarative procedure from the nondeclarative portion. §14.9.49.3 SR4 allows
    such a reference "only with a PERFORM statement". The code is COBOLNET2376, from PB362 (same commit). The PERFORM
    on the line before stays legal.
  - `syn_misc:619` has `IF 1 AND 2 > 1`, where a bare literal or numeric item stands as a condition. §8.8.4.2.1
    lists the simple conditions and those are not among them. The code is COBOLNET2318, from PB982 (train 51,
    `8af541fff`). The two abbreviated combined relations in the case stay legal, and they are not flagged, which
    matches GnuCOBOL line for line.
  - `syn_occurs:359` has `ASCENDING KEY IS X1 OF TAB-ENTRY1 OF TAB`, and no `TAB` exists. §13.18.38.3 SR3 applies.
    The code is COBOLNET2353, from PB1018 (train 54, `9749718f5`).
  - `syn_misc:4346` uses ENTRY (see the next group).
- **Four hollow accepts became rejects.** In `run_extensions:6030 / 6100 / 6122` (INSPECT … TRAILING) and
  `run_misc:4111` (ENTRY), #85's AGREE_ACCEPT carried COBOLNET1756. That means the program compiled a staged
  run-time abort: the "agreement" was a crash waiting to be reached. PB909 (train 49, `f96952b03`) made these
  COBOLNET2269 compile-time errors.
  - §14.9.22.2 prints only CHARACTERS / ALL / LEADING in a tallying-phrase, and CHARACTERS / ALL / LEADING / FIRST
    in a replacing-phrase.
  - ISO/IEC 1989 defines no ENTRY statement.
  - CLAUDE.md rule 1 is explicit that GnuCOBOL's non-ISO extensions are not followed.
- I re-baselined exactly those seventeen rows. A re-run over the corrected baseline prints
  `=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`. The totals moved from 577 / 469 / 237 / 40 to
  **573 / 473 / 242 / 35**. There was no NEW or REMOVED case and no case without a verdict.

**Complement, and one lead.**
- Thirteen codes are new to the corpus. Examples: COBOLNET2269 on 8 cases, and 2318, 2353, 2375, 2376 and 2377.
- COBOLNET1756, the staged-refusal warning, fell from 19 cases to 2.
- COBOLNET0864 no longer fires.
- 63 cases kept their verdict but changed their reason. Most of them went from a parse error to a named rule or a
  §8.9 reserved-word refusal.
- `syn_file:910` stays AGREE_ACCEPT with the new COBOLNET2375 warning. That is §14.9.49.3 SR3, a warning by the
  documented determination D-DECLREF.
- **LEAD, found through `syn_misc:4346`.** The ENTRY error masks the case's real subject, a duplicate
  `PROCEDURE DIVISION USING p p o`. Probed alone, that header is not refused by the binder. It reaches Roslyn and dies
  as `CS0102: … already contains a definition for '__lnk0'`. That is a backend failure where §14.2.2 SR1 ("A
  particular user-defined word shall not appear more than once as data-name-1") owes a COBOL diagnostic. The
  inventory row SR-14.2.2-1 is GAP. The lead is handed to the orchestrator for filing.

**Record.** Plan §0 BATTERY REFERENCE is rotated: CURRENT is #86, PREVIOUS is #85 (compressed), and #84 is dropped.
One sentence is appended to the in-flight panel of `ledger-in-flight.md`. The baseline TSV changed by the seventeen
rows. Battery #87, at the next train's head, must be ALL GREEN with 0 flips.

