## Entry 268 — OPEN-absent / SELECT OPTIONAL cluster: per-program file isolation + optional-absent INPUT → SQ141A/142A/203A

The last SQ FAIL* tests, all rooted in two defects in how an *absent* sequential file is opened for INPUT.

**1. `XXXXX001`/`XXXXX002` defeated per-program test isolation.** The NIST preprocessor special-cased these two
data-file ASSIGN targets to a *shared* literal (`XXXXX001 → "TFIL1"`, `XXXXX002 → "TFIL2"`) **before** the
organization-aware mapping (DEVLOG 255) that is supposed to keep SEQUENTIAL targets program-id-qualified for
isolation (DEVLOG 244). So SQ142A's `SELECT SQ-FS1 ASSIGN TO XXXXX001` (SEQUENTIAL, never written by the
program) resolved to the shared `tfil1.txt`, which a *prior* test had created — the "absent file" was present,
so `OPEN INPUT` returned `00` instead of `35`. Fix: deleted the blanket `XXXXX001/002 → TFIL1/TFIL2`
replacement so both flow through the org-aware path like every other `XXXXX###` — RELATIVE/INDEXED → shared
`"TF###"`; SEQUENTIAL → left as the implementor-name, which the Binder qualifies as `{program}-{file}`. Broad
blast radius (every SQ/IX/ST sequential data file), but the comment at that site already declared this exact
intent; the literal was the holdout. Guard confirmed zero regressions.

**2. `OPEN INPUT` on a SELECT OPTIONAL file that is not present must *succeed* and position at end-of-file
(ISO §9.1.13.2).** `SequentialFileHandler.Open(Input)` returned status `05` for an optional-absent file but
opened no stream, so `IsOpen` stayed false and the first `READ` hit the not-open guard → `47`, not the
AT END `10` the spec requires. Added `_optionalAbsentInput`: set on optional-absent `OPEN INPUT` (still
returns `05`), makes `IsOpen` true, and routes every `READ` to AT END (`10`) reading no record; reset at
OPEN/CLOSE. This makes the file-status-driven control flow correct for both phrasings:
- **SQ203A GF-02** (`READ … AT END`): the AT END phrase now fires → PASS.
- **SQ203A GF-03** (no AT END phrase, `USE AFTER STANDARD EXCEPTION ON INPUT` declarative): the READ now
  stores `10` into FILE STATUS `GRP-STATUS-KEY-2`; the declarative (already lowered on the no-phrase READ
  path via `EmitUseDeclarative`, gated by `ShouldRunUseDeclarative`, which treats `10` as an exception) runs
  and, seeing the status' first digit `"1"`, sets `EOF-FLAG`. (Previously the READ returned `47` → first
  digit `"4"`, which is exactly the `COMPUTED= 4 / CORRECT = 1` the failing baseline reported.)
- **SQ141A** (declarative-not-executed) and **SQ142A** (status 35): both resolve from fix #1 — the file is
  now genuinely absent, so `OPEN INPUT` returns `35`, the OPEN USE declarative fires (SQ141A), and the
  status is correct (SQ142A).

SQ141A 1/1, SQ142A 1/1, SQ203A 4/4 (all GF tests), all baselined. Guard ALL GREEN: 1000 unit / 347
integration / **232 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ + 19 RL + 2 IX), 0 regressions. The SQ
suite FAIL* tail is now clear — remaining SQ non-baselined are flagging modules (SQ303M/SQ401M, no CCVS
report) and the two runtime hangs (SQ105A var-REWRITE / SQ114A surveyed earlier). Next group: IX indexed
runtime, then ST sort/merge.

