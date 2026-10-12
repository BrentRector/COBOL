## Entry 244 — Open-mode I-O status + per-program file isolation (SQ130A, SQ156A)

Two more SQ runtime-tail tests, both open-mode I-O status bugs in `SequentialFileHandler`, plus the
test-isolation fix they forced.

**SQ156A — WRITE to a sequential file open in I-O mode must be status 48.** `Write` only rejected
INPUT mode; it let an I-O-mode WRITE through and returned 00. Per ISO §9.1.13.7 item 8a, for a
sequential-access file WRITE is valid only in OUTPUT or EXTEND mode (I-O supports READ/REWRITE).
Changed the guard to `_openMode != Output && _openMode != Extend → 48`.

**SQ130A — OPEN I-O (or EXTEND) on a missing non-optional file must be status 35.** `Open` opened
I-O with `FileMode.OpenOrCreate` and EXTEND with `FileMode.Append`, both of which silently CREATE a
missing file, so the absent-file test saw 00. Per ISO §9.1.13.4 item 5, OPEN INPUT/I-O/EXTEND on a
non-optional file that is not present is status 35; an *optional* missing file is created with status
05 (§9.1.13.2 item 5a). Added the existence check to the I-O and EXTEND arms (INPUT already had it)
and return 05 when an optional file is created on I-O/EXTEND open.

**Test isolation (the fix SQ130A forced).** SQ130A and SQ156A both `SELECT SQ-FS1 ASSIGN TO XXXXX014`
— and `XXXXX014` is an *unmapped* placeholder, so the assign target stayed a bare word and the host
path fell back to the COBOL file-name → both programs used the SAME physical `sq-fs1.txt`. SQ156A
creates that file (OPEN OUTPUT); SQ130A requires it ABSENT. So once SQ156A ran, SQ130A's absent-file
test would see the leftover file and fail on the guard's next run — non-idempotent. Root fix in
`Binder`: a *literal* ASSIGN target ("TFIL1", "TF002") is an explicit, possibly-shared physical name
(NIST producer/consumer files) and is used verbatim; a *non-literal* target names a file PRIVATE to
the program, so it is now qualified with the program-id (`SQ130A-SQ-FS1` → `sq130a-sq-fs1.txt`). Two
programs that reuse the same SELECT name no longer collide, and an absent-file test stays absent
across runs. Verified SQ130A/SQ156A both 0 FAIL* on back-to-back reruns (idempotent); SQ130A never
creates its file (OPEN I-O → 35), SQ156A writes only its own `sq156a-sq-fs1.txt`.

Result: **SQ130A and SQ156A CLEAN**, baselined → `tests/nist/valid/` + `scripts/guard.sh`. **NIST
baselines 184 → 186** (… **28 SQ** …). Full guard ALL GREEN: 1000 unit / 348 integration (347+1 skip)
/ 186 NIST, **0 regressions** — the per-program-name change (touching every non-literal-ASSIGN file
across all tests) broke nothing. SQ session arc: 23 → 28 baselined (SQ128A/130A/149A/154A/156A).
Remaining SQ FAIL* tail: SQ214A (ODO full→partial read), SQ106A (variable-length WRITE status),
SQ107A/109M/110M/115A/116A/124A/220–224A, plus 10 COMPILE_FAIL parse forms.

