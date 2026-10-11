## Entry 188 — 2026-05-28: ODO variable-length group sizing (runtime length, receiving=max) — NC247A 100%

**Problem (NC247A, 7 FAIL*):** a group containing a trailing OCCURS DEPENDING ON table was
always sized at its compile-time maximum. So `IF GRP-ODO = WRK-GRP-00019` (with DOI=3)
compared 19 bytes instead of the active 13, and the same over-long length broke INSPECT,
MOVE, STRING, and UNSTRING on partial ODO groups.

**Fix (IR + resolver + emitter):**
- New `IrOdoGroupLocation` whose byte length is computed at runtime:
  `length = maxLength - (maxOccurs - dependingOnValue) * elementSize` (i.e. fixedPart +
  activeCount*elementSize). `CilLocationEmitter.EmitOdoGroupLocationArgs` decodes the
  DEPENDING ON field and computes the length inline.
- `LocationResolver.ResolveWholeItem` detects a trailing DEPENDING ON table beneath any
  whole-item reference (WS/LOCAL/FILE areas) and produces the ODO location.

**Spec subtlety (ISO 1989:1985 OCCURS GR 7), surfaced by MOV-TEST-F1-6:** when the
DEPENDING ON object is *within* the group, a **sending** operand uses the current value but
a **receiving** operand uses the **maximum** length (so a group MOVE writes every occurrence,
not just the receiver's current count). Threaded a `receiving` flag through
`ResolveExpressionLocation`/`ResolveLocation`; the MOVE target passes `receiving: true` and
keeps the max-length static location when its DEPENDING ON object is internal. Sending
operands (comparisons, MOVE source, STRING/UNSTRING/INSPECT source) keep the runtime length.

NC247A: 7→0 FAIL*, now 20/21 (1 NIST DELETE). Guard green.

---

