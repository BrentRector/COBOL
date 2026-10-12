## Entry 256 — Variable-length relative records (RECORD IS VARYING) → RL206A/RL207A

RL206A creates a relative file with `RECORD IS VARYING IN SIZE FROM 120 TO 140 DEPENDING ON WRK-SIZE`,
writing records of varying length and verifying on read-back that the DEPENDING item is restored — it
failed 22× "WRONG LENGTH RECORD" because relative slots were fixed-width and tracked no per-record
length. Taught the relative subsystem variable-length records, mirroring the sequential model (DEVLOG
245/247) but slot-addressed:
- `RelativeFileHandler`: an `IsRecordVarying` flag; `WriteVariable` stores the record at its actual
  length (the caller's depending-on byte count) instead of padding to the slot width; each read sets
  `_lastRecordLength` to the stored record's length (fixed files report the constant length, unchanged);
  persistence gains a length-aware format — each slot is `[4-byte LE length][max-width data]`, gap =
  length `0xFFFFFFFF` — so the length round-trips across run units (RL206A closes the OUTPUT file and
  reopens it INPUT to verify). `Write`/`WriteVariable` share `SelectWriteSlot`.
- Lowerer: `IsVaryingRecord` generalizes `IsVaryingSequential` to RELATIVE (explicit `RECORD VARYING`
  only, to stay in lockstep with the runtime flag); the variable-write, length-store, and read-into-
  largest paths now apply to relative varying files.
- Binder/runtime: emit `FileRuntime.SetRelativeVarying(name, true)` so the handler's flag matches the
  compiler's variable-write decision.

**The bug that ate the afternoon (logged honestly).** After all the above, RL206A still showed 22
FAIL* — and instrumentation showed neither the fixed nor the variable write, nor even the relative
Open or file registration, ever ran. The cause: my new `IrRuntimeCall("FileRuntime.SetRelativeVarying")`
fell through the CilEmitter's hardcoded runtime-call if-else chain to its `// Other runtime calls: NOP
for now` tail — the call was silently dropped, but its two pushed arguments were left on the stack →
malformed CIL → `InvalidProgramException` at `RL206A.Main()` before any I/O. I was misled for several
iterations by a STALE `rl206a.txt` (the crashing runs left an old 22-FAIL* report in place; I was
re-reading it instead of seeing the crash). Two lessons: (1) a new emitted runtime call needs an
explicit CilEmitter case — the fallthrough is a silent NOP, not an error; (2) check the exit code and
regenerate (rm) the expected output file before trusting it. Fix: added the `SetRelativeVarying`
emission case (string,bool) mirroring `SetRelativeAccess`.

Result: **RL206A CLEAN** (varying create+verify) and **RL207A CLEAN** (consumes RL206A's varying file).
Baselined the RL206A→RL207A pair consecutively over TF021, with the fixed-format producer RL209A after
them (each producer opens OUTPUT, re-creating TF021 in its own format, so the varying and fixed TF021
chains coexist). **NIST baselines 208 → 210** (94 NC + 42 IF + 12 SM + 4 IC + 39 SQ + **17 RL** + 2 IX).
Full guard ALL GREEN: 1000 unit / 348 integration / 210 NIST, 0 regressions. Remaining relative: RL208A
(2 FAIL* — a 5-record discrepancy in the RL207A→RL208A delete/update chain), and RL210A/RL211A (the
harder `RECORD IS VARYING` with an OCCURS DEPENDING table inside the record — format-3, ISO 3.8.4 GR
10B — a record-length-from-ODO case distinct from the simple DEPENDING item here).

