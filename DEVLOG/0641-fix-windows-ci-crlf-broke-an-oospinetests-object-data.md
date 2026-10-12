## Entry 641 — 2026-07-07 00:35 PDT — Fix: Windows-CI CRLF broke an OoSpineTests object-data injection (the M2-OO-1h steps 2-4 CI reds)

⚠ **The M2-OO-1h steps 2-4 pushes ran RED on the WINDOWS CI job** (the Linux greenfield job + the full legacy
guard were GREEN) on ONE unit test: `MethodOdo_DependingOnVisibleObjectData` failed with a bind error
(`COBOLNET0851 'OCNT' is not defined`). Root cause: `DriverAndClass` builds source from a `$$"""…"""` raw-string
literal, which is CRLF on a Windows checkout (autocrlf) but LF on Linux; the object-data injection uses
`.Replace("PROCEDURE DIVISION.\nMETHOD-ID. M.", …)` with an LF target, so on Windows CRLF the Replace was a
silent no-op → the object `OCNT` was never injected → the ODO depending-name's visible-object fallback had
nothing to resolve. **I could not catch this locally — my working copy is LF, so the Replace matched and the test
passed;** only the Windows CI CRLF checkout exposed it. (Other Replace-using tests passed on Windows because their
assertions are insensitive to the injection — e.g. a "target not found" that fires whether the object item is
absent or merely out of scope; those were silently testing a weaker scenario.)

**Fix (root cause):** `DriverAndClass` normalizes its output to LF (`.Replace("\r\n", "\n")`) so every test's
`.Replace("…\n…")` object-data injection matches on BOTH CI platforms; the compiler reads LF source fine. This
also strengthens the insensitive tests (their injections now actually apply). The M2-OO-1h data-model work itself
is correct and green (the goldens byte-matched on Windows CI too — only the raw-string injection was CRLF-fragile).
DEVLOG 638-640 are functionally complete; this is the test-infra fix that greens their CI.

