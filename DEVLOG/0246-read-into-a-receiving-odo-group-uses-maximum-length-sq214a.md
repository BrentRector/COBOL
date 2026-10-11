## Entry 246 — READ INTO a receiving ODO group uses MAXIMUM length (SQ214A)

SQ214A's READ-TEST-GF-03 ("READ FULL ODO INTO PARTIAL ODO") sets the DEPENDING item to 5, then
`READ SQ-FS1 INTO ODO-RECORD`, expecting all 9 ODO occurrences ("123456789") to land in the
receiving group; we moved only 5 ("12345"). ISO §13.18.38 (OCCURS, general rules): when the
DEPENDING ON object is **inside** the group and the group is a **receiving** operand, the MAXIMUM
length of the group is used (so all occurrences are written); only a *sending* operand uses the
current depending value. `LocationResolver.ResolveWholeItem` already implements exactly this
(`receiving && dependOnInside` → `IrStaticLocation` at the compile-time max, not an
`IrOdoGroupLocation`) — but `FileIoLowerer.LowerRead` resolved the INTO target *without*
`receiving: true`, so the implied MOVE treated it as a sending operand and truncated to the depending
value. One-line fix: `ResolveLocation(read.Into, receiving: true)`.

Result: **SQ214A CLEAN** (0 FAIL*, deterministic), baselined. **NIST baselines 190 → 191** (… **34
SQ** …). Full guard ALL GREEN: 1000 unit / 348 integration (347+1 skip) / 191 NIST, 0 regressions.

