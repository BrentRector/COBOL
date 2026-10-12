## Entry 252 — PIC-aware relative key (COMP keys) + producer/consumer chain baselining → RL102A/RL103A

The relative subsystem (251) only worked for DISPLAY relative keys: `FileRuntime.SetRelativeKey`/
`ReadByKey` read the RELATIVE KEY bytes as ASCII and `IrStoreRelativeKey` wrote them with
`MoveIntToField` (ASCII). But the RELATIVE KEY is routinely `USAGE COMP` (binary) — e.g. RL102A's
`RL-FR1-KEY PIC 9(09) COMP`, RL103A's `RL-FS1-KEY PIC 9(08) COMP` — so RANDOM reads/rewrites missed
(RL102A 7 FAIL*, count 1/501) and the §14.9.30 GR25 "move the record's relative number into the key"
wrote garbage (RL103A "KEY VS RECORD" mismatch). The relative key is a numeric data item; its value
must be decoded/encoded per its PICTURE/USAGE, not treated as ASCII text.

Fix — convey the key as a PIC-aware integer, reusing the same `PicRuntime.DecodeNumeric` /
`EncodeNumeric` the subscript, EVALUATE, and MOVE paths use:
- `EmitSetRelativeKey` now emits `EmitLocationArgsWithPic` → `PicRuntime.DecodeNumeric(area,off,len,pic)`
  → `Convert.ToInt32` → `FileRuntime.SetRelativeKey(name, int)` (signature changed from bytes to int).
  Used before every random/dynamic WRITE/REWRITE/DELETE **and** now before a relative keyed READ.
- `RelativeFileHandler.ReadByKey` uses that pending key (the byte form mis-decodes a COMP key); the
  keyed-READ lowering emits `IrSetRelativeKey` first (INDEXED keyed reads keep their alphanumeric key
  bytes — unchanged).
- `EmitStoreRelativeKey` now emits `EmitLocationArgsWithPic` → `GetRelativeSlot` → `Convert.ToDecimal`
  → `PicRuntime.EncodeNumeric(area,off,len,pic,value)` (inverse), so the relative number round-trips
  into a COMP key. (DISPLAY keys still work — DecodeNumeric/EncodeNumeric handle all usages; RL107A
  unaffected.)

**Producer/consumer chain baselining.** Most RL tests are multi-program chains over one shared
literal file (`XXXX[PD]nn → TFnn`): RL101A creates 500 records → RL102A REWRITEs 100 → RL103A
verifies. The guard runs NIST_TESTS in list order in one directory **without cleaning data files
between tests**, so a chain baselines if its members are consecutive and ahead of any other producer
of the same file. Verified RL101A→RL102A→RL103A all 0 FAIL* and deterministic across repeated chain
runs; baselined RL102A + RL103A with the three ordered consecutively in `scripts/guard.sh` (with a
comment documenting the ordering invariant).

Result: **RL102A and RL103A CLEAN** (COMP-key RANDOM read/rewrite + sequential update/verify).
**NIST baselines 197 → 199** (… **7 RL** …). Full guard ALL GREEN: 1000 unit / 348 integration
(347+1 skip) / 199 NIST, **0 regressions** (RL107A DISPLAY-key + the FileIO integration tests
unaffected by the decode/encode change). Remaining relative tail: the no-KEY `RELATIVE`/`RECORD`
leniency (RL117A/IX — non-conformant CCVS source, deferred), more producer/consumer chains
(RL201A/202A→RL203A/etc.), RL110A sequential creation, RL210A multiple-record-format/ODO; COMP-keyed
START still uses the ASCII `ParseKey` (deferred — no current test needs it).

