## Entry 1000 — 2026-07-23 00:28 PDT — Conformance fix-queue: CA15 + CA16 (files-io) — line-sequential over-length READ ('06' + remainder); OPTIONAL I-O absent-file create ('05'→'10')

Two files-io majors in the runtime `SequentialConnector` + `FileStatus` (no compiler/emit change; characterization
byte-identical).

**CA15 (§14.9.30.4 GR15 + NOTE 3 / §9.1.13.2 item 5).** A LINE-SEQUENTIAL READ of a record longer than the FD record
width must truncate on the right, report I-O status '06', and set the file position indicator to the next unread
character IN THE RECORD so the following READ continues the remainder. The `_lineSequential` READ branch did
`ReadLine()` + `Fit(line, RecordWidth)` (truncating) but left the status '00' and DISCARDED the tail (the next READ
read the FOLLOWING physical line) — silent data loss + mis-positioning. FIX: a new `'06'` `FileStatusCode.
LineRecordTooLong`; a `_lineRemainder` field (reset in OpenCore) modelling the GR15 FPI; the READ branch now services a
pending remainder before reading a new line, and splits an over-length line into `[..RecordWidth]` (status '06',
`_lineRemainder = [RecordWidth..]`) then the final ≤width chunk ('00'). The over-length read stays successful (GR15).

**CA16 (§14.9.27 GR17 + §14.9.30.4 GR21 rule e / GR24; §9.1.13.7 item 7).** An ABSENT OPTIONAL file opened I-O must be
CREATED (as if OPEN OUTPUT + CLOSE; OPEN '05'), the FPI set to 1, and the first READ on the now-empty file yields AtEnd
'10'. The IO OpenCore branch created ONLY a `_writer` for the absent-optional case and never a `_reader`, so the first
READ hit `_reader is null` → '47' (not-open-for-input) — categorically wrong (the connector IS open I-O), and '47'
flips the AT END control flow (status[0] != '1') so NOT AT END runs. FIX: the branch now creates the empty file when
absent then opens a ReadWrite `_reader` exactly like the existing-file path (REWRITE writes through
`_reader.BaseStream`, so no `_writer` needed), returning '05'; the first READ then finds no record → '10'.

**Verified (direct CLI + spec tests):** CA15 over-length 'ABCDEFGH' read as X(5) → `1:ABCDE FS=06` / `2:FGH   FS=00` /
`E FS=10` (was `1:ABCDE FS=00` then the 'FGH' tail lost). CA16 OPEN I-O of an absent OPTIONAL file → `OPEN FS=05` /
`ATEND FS=10` (was `47` / GOTREC). Both pinned in a new `SequentialFileIoSpecTests` (staging the input file / relying
on the empty temp dir — behaviours the self-contained differential goldens can't express).

**⚠ CORRECTION — a process misstep, and the CA39 fallout it hid (transparency).** The Entry-999 editions-gating
conformance run reported 3880/1, and I wrongly dispositioned the single failure as a "load-induced flake" (VersionMatrix
was clean, so I assumed the registry change couldn't be the cause) and committed editions-gating over it. That was a
discipline violation — I did not identify the failing test before calling it benign. This files-io run reproduced the
SAME 1-fail, so I finally identified it: **`ControlFlowDifferentialTests.ExitParagraph`**. It compiles its source at
edition **85** (the `DifferentialGolden` default), and CA39 now CORRECTLY rejects `EXIT PARAGRAPH` there (a 2002
feature, COBOLNET0900). So the compiler is right; the TEST was relying on the pre-CA39 bug. The real lesson is
`feedback_scan_all_similar`: when I gate a construct, I must sweep every test/golden that uses it at the now-rejected
edition — CA39 needed that sweep and I skipped it. FIX (folded into this commit): `ExitParagraph` now compiles at
edition 2002 (`DifferentialGolden.Assert(..., edition: 2002)`), the golden re-baked at 2002 (cobolnet==legacy; identical
"S1"/"AFTER" output — behaviour-preserving), the orphaned edition-85 golden removed. ControlFlowDifferentialTests 14/14.
The PerformFormat3* EXIT-PERFORM tests were unaffected (they compile at edition 2023).

**Gate:** characterization 33/33 (byte-identical — runtime-only), file-IO differential + spec suite 55/55, unit
575/575, ControlFlowDifferentialTests 14/14, greenfield Conformance 3883/3883 0-reg (this run, with the ExitParagraph
correction, returns the full suite to 0-fail — the genuine close of the Entry-999 red).

