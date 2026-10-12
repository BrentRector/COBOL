## Entry 470 — CI part 3: case-insensitive CALL-target assembly resolution (Linux), + full Linux guard verified in WSL

The newline fix (469) cut the Linux integration failures 22 → 2; the last two (`CallReturning_IntoWorkingStorage`,
`FunctionIdUnit_CompilesAsCallableProgram`) were a different, advisor-predicted issue: **filename
case-sensitivity**. `CompileMultipleAndRun` writes source files lowercase (`adder.cob` → `adder.dll`) while the
PROGRAM-ID and the `CALL "ADDER"` target are uppercase. The runtime's `CobolProgramRegistry.DiscoverProgram`
probed `Path.Combine(appDir, programId + ".dll")` = `ADDER.dll`; on Windows's case-insensitive FS that resolves to
`adder.dll`, but on Linux/CI it misses → CALL fails. COBOL program-names are case-insensitive (ISO), so the
resolver must tolerate any on-disk case: added a case-insensitive directory-scan fallback when the exact-name probe
misses (`Directory.EnumerateFiles(appDir,"*.dll").FirstOrDefault(name eq programId+".dll", OrdinalIgnoreCase)`).
Fast path (exact match) unchanged; the scan only runs on a miss.

**Verified on real Linux via WSL** (user-greenlit; .NET 10 runtime installed there). Because compiled programs are
portable IL and `global.json` only gates *build* commands, I ran the Windows-built CLI + runtime under WSL's
runtime directly — no Linux rebuild needed:
- the exact CALL case-mismatch repro (lowercase files, `CALL "ADDER"`) → **`RESULT=0042`** (was failing pre-fix);
- the **entire guard NIST loop on Linux → 364 MATCH, 0 problems, ALL GREEN** (confirming 469's `normalize()` CR-strip
  handles every written-report-file and stdout comparison across NC/IF/SM/IC/SQ/RL/IX/ST/RW).

So all three pre-existing CI failures are now resolved (build/Generated_temp 468; newline 469; CALL-case 470), each
independent of the COBOL.NET G0 work. Pushing for a final all-green CI confirmation.

