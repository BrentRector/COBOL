## Entry 469 — CI part 2: make the guard newline-platform-independent (CRLF golden data vs LF on Linux)

With the build fixed (468), the Linux CI guard job ran further and exposed a SECOND pre-existing problem the
build failure had masked: **22 integration tests failed on Linux** (pass on Windows). Root cause: a compiled
program's `DISPLAY` uses `Console.WriteLine` = the platform newline (`\r\n` on Windows, `\n` on Linux/CI), but all
golden data is CRLF — the integration tests assert against `"...\r\n..."` literals, and `tests/nist/valid/*.txt`
are CRLF (no `.gitattributes`). So on Linux every multi-line-output comparison mismatches. The 22 are exactly the
multi-line-DISPLAY tests; single-line tests and the already-newline-agnostic `Split('\r','\n')` tests pass on both.
The NIST loop (364 programs) would have hit the same wall next — its `normalize()` stripped trailing spaces but
NOT `\r`.

Fix = normalize on the COMPARISON side (the legacy engine is the differential oracle, retired at G8 — left
untouched; the COBOL.NET deliverable will instead set `Console.Out.NewLine` for deterministic output):
- `scripts/guard.sh` + `scripts/guard-run-group.sh` (the guard-fast path): `normalize()` now does
  `tr -d '\r' < "$1" | sed 's/ *$//; …'`. **CR-strip FIRST** — critical ordering: `s/ *$//` is a no-op while a
  trailing `\r` still sits at end-of-line, so squeezing spaces before stripping CR would leave the CRLF and LF
  sides unequal (a false REGRESSION). Idempotent on Windows.
- `tests/CobolSharp.Tests.Integration/EndToEndTestBase.cs`: a `NormalizeOutput(s) =>
  s.ReplaceLineEndings("\r\n").TrimEnd()` routes all three run helpers' captured stdout/stderr, so the `\r\n`
  expectations are platform-independent. Verified no test asserts LF-only multi-line output (those would break
  under CRLF-canonicalization) — they don't (a `\r\n`-emitting program couldn't have passed such an assert on
  Windows).

Verified: local Windows guard-fast ALL GREEN (NIST 364 MATCH, 1204 unit, 535 integration) — the change is
idempotent on Windows, zero regression. Pushed; **watching the Linux CI run go green before declaring CI fixed**
(the 364-program NIST loop has never executed on Linux, so the next platform issue — if any, e.g. filename
case-sensitivity — would surface there).

