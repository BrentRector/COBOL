## Entry 472 — COBOL.NET G0 (step 4/5): add the new test projects (Cobol.Net.Tests.Unit + .Conformance)

G0 step 4 of 5 (`docs/COBOLNET_DESIGN.md` §17 §1.5). Now that the compiler is a library (step 3), stand up the
greenfield test projects — the validation harness the G2 bound-tree work needs (the advisor's "G0 is a prerequisite
to G2 validation" point: those test projects ARE created here).

- **`tests/Cobol.Net.Tests.Unit`** — xUnit, references `Cobol.Net.Compiler` + `Cobol.Net.Runtime`. A
  `CobolNetTestBase` compiles a COBOL source string via `CompilerDriver` into an isolated temp dir, runs the
  produced assembly, and returns its stdout (newline-canonicalized to `\r\n`, applying the CI lesson from 469 so
  the harness is platform-independent from day one). `CompilerDriverTests` anchors current G2/G3 capability:
  `DISPLAY` of a literal, typed `PIC X`/`PIC 9` MOVE+DISPLAY (`NAME=BOB   ` / `N=0005`), and the
  `SourceNotFound` outcome. 3 tests, green.
- **`tests/Cobol.Net.Tests.Conformance`** — xUnit scaffold for the differential/conformance harness. A
  `DifferentialHarness` documents the G5 build-out (the `ICompilerUnderTest` oracle-vs-COBOL.NET stdout diff over
  the 364 NIST programs, §2/§18.7) and holds one build-anchoring smoke until the PC-dispatcher (G4) + files/
  interprogram (G5) exist — explicitly NO red NIST theories before G5. 1 test, green.
- `Cobol.Net.Compiler` gains `InternalsVisibleTo Cobol.Net.Tests.Unit`; both projects added to the solution
  (under a `tests` solution folder).

Verified: full-solution build clean Debug AND Release (mirrors the CI Windows job, warnings-as-errors); the new
Unit (3) + Conformance (1) tests pass; legacy oracle guard ALL GREEN (untouched). The new test projects are built
by the solution build but not yet wired into `guard.sh`/CI's run set (they stay scaffolds until they carry real
coverage — wiring is step 5 / G5). NEXT: G0 step 5 (scripts/CI/sln consistency + the optional `.sln` rename), then
G2 (bind → bound tree → `Place`; data model; numerics).

