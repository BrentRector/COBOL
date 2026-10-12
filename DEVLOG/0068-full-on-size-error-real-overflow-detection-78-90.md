## Entry 068 — 2026-03-15: Full ON SIZE ERROR — Real Overflow Detection — 78/90

### The Change

Replaced the stubbed SIZE ERROR (always false) with real overflow detection.

**PicRuntime**: All 8 arithmetic methods (Multiply/Add/Subtract/Divide × field/literal)
now take `ref ArithmeticStatus status`. Before encoding the result, each checks
`WouldOverflow(value, destPic)`. If overflow detected: sets `status.SizeError = true`,
does NOT modify the destination, returns immediately.

**WouldOverflow** checks per usage:
- DISPLAY: scaled integer digit count > TotalDigits
- COMP/BINARY: value outside short/int/long range for 2/4/8 bytes
- COMP-3: digit count > packed capacity ((length × 2) - 1)
- Divide by zero: always SIZE ERROR

**ArithmeticStatus**: Changed from auto-property to public field for direct CIL
`ldfld` access (auto-property's backing field is private, GetField returns null).

**CilEmitter**: One `ArithmeticStatus` local per method (lazy). Before each
arithmetic call: `initobj` (zero-init), after args: `ldloca` (pass by ref).
Updated all reflection `GetMethod` calls to include `ArithmeticStatus&` type.

**IrLoadSizeError**: New IR instruction. CIL: `ldloc status; ldfld SizeError; stloc cond`.
Replaces the `IrSetBool(false)` stub in LowerMultiply's conditional branching.

### Bug Found During Implementation

`ArithmeticStatus.SizeError` was an auto-property (`{ get; set; }`), not a field.
CIL `ldfld` on an auto-property's backing field fails because `GetField("SizeError")`
returns null. Fixed by changing to a plain public field.

### Defensive Check Suggestion

Should add: unit tests for WouldOverflow with boundary values for each usage kind.
Should add: assertion that all arithmetic GetMethod calls return non-null.

### Result

**NC101A: 78/90 pass** (was 54/90). +24 tests from real SIZE ERROR detection.
This is the single largest test improvement in the session.
35/35 category unit tests pass.

---

---

