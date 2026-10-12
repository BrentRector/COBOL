## Entry 1007 — 2026-07-23 02:52 PDT — Conformance fix-queue: CA35 (picture-usage-value) — USAGE BINARY/COMP/PACKED-DECIMAL requires a numeric PICTURE (ISO §13.18.60.3 SR3), no longer silently misbound as alphanumeric

**CA35 (CONFORMANCE-FIX-QUEUE, MAJOR/S) landed.** ISO §13.18.60.3 SR3: an elementary item whose USAGE specifies
BINARY / COMPUTATIONAL / PACKED-DECIMAL "shall be specified only with a picture character-string that describes a
numeric item." `PictureAnalyzer.Analyze` already guards the sibling cases — USAGE BIT vs a non-boolean picture (SR5)
and USAGE NATIONAL vs an alphabetic/alphanumeric picture (SR12) — before the category dispatch, but had NO analogous
guard for Binary/Comp/Packed. So `01 A PIC XX COMP` reached the `anyAlpha` return and silently bound as category
Alphanumeric with the numeric usage DROPPED — a program ISO rejects, accepted, with the declared representation lost.

**The fix.** A one-branch SR3 guard added immediately before the `anyAlpha` return, mirroring the BIT/NATIONAL guards:
`if (anyAlpha && usage is Usage.Binary or Usage.Comp5 or Usage.Packed)` → `COBOLNET0881` (the established
usage/picture-mismatch code the BIT/NATIONAL checks already emit — no new descriptor) then recover to Display
(matching the sibling recovery so the doomed emit stays crash-free). Scoped to the three usages SR3 names that take
an explicit PICTURE (Binary = COMP/BINARY, Packed = PACKED-DECIMAL/COMP-3, Comp5 = COMPUTATIONAL-5); the picture-less
BINARY-CHAR/-SHORT/-LONG/-DOUBLE are rejected by the pre-existing COBOLNET0870 SR8 guard. The message names the COBOL
keyword (BINARY / PACKED-DECIMAL / COMPUTATIONAL-5), not the internal enum member.

**Process miss caught (the stale-DLL lesson, re-learned).** First test run showed the 2 new negatives NOT rejecting.
Root cause was NOT the guard — I had built only `src/Cobol.Net.Compiler`, so the `--no-build` test bin and the CLI bin
still carried the pre-CA35 compiler DLL (`feedback_fresh_build_before_no_build_test`). A full `CobolSharp.sln` build
propagated the guard and both negatives reject. (The guard itself was correct on the first write — verified by CLI
smoke after the solution build: `PIC XX COMP` → COBOLNET0881, `PIC 9(4) COMP VALUE 12` → runs, prints 0012.)

**Strictness sweep (CA39 lesson).** A targeted corpus grep for an X/A picture carrying a COMP/BINARY/PACKED usage
found 0 genuine cases (the one hit, `CM102M.cob PIC X(9) VALUE "COMPILER."`, is "COMP" inside a string literal, not a
USAGE). Gate: corpus runner 334/334 (both new negatives reject, all positives compile) · characterization 33/33
byte-identical · full Conformance (batch strictness gate). Goldens: `conformance/negative/ca35_comp_alpha_picture`
(COMP → Usage.Binary) + `ca35_packed_alpha_picture` (PACKED-DECIMAL → Usage.Packed), edition-invariant (reject-at 85
2002 2014 2023). **17 landed / 29 fix-ready remain.** NEXT = the arithmetic batch (CA4 ADD/SUBTRACT-GIVING composite,
CA5 division rounding, CA6 binary-usage composite exclusion).

