## Entry 436 — Stage-4 pointers slice 2: pointer arithmetic (SET p UP/DOWN BY n)

Pointer slice 2 (docs/RECORD_STRUCT_STORAGE_DESIGN.md §10.4, ISO §14.9.39 Format 10 GR20). `SET p UP|DOWN BY n`
adjusts the managed pointer's address by n **bytes** — `Offset + n` (UP) / `Offset - n` (DOWN) — preserving
Buffer/Length/Pic.

- **Bind:** `BindSetIndex` detects a pointer target (`Category==Pointer`) and emits a new
  `BoundPointerArithStatement(Pointer, IsUp, Delta)` instead of the numeric index-set path.
- **IR:** `IrPointerAdjust(PtrField, Delta, IsUp)`; `LowerPointerArith` lowers the byte delta to an `IrExpression`.
- **Emit:** `new ManagedPointer(p.Buffer, p.Offset ± (int)delta, p.Length, p.Pic)` → `stsfld _PTR_p`
  (`Convert.ToInt32` on the decimal delta — the same idiom as subscript lowering; the `ManagedPointer` ctor reused
  via `CilDataEmitter.GetManagedPointerCtor`).

Conformance `tests/conformance/2002/pointer_arith.cob`: P → a 10-byte buffer; `SET P UP BY 4` / `DOWN BY 2` move it
within the buffer; a BASED 1-byte item rebased onto P reads the byte at the current address → `C0=A`, `C4=E`,
`C2=C`. The EC checks of GR18–20 (EC-DATA-PTR-NULL / EC-SIZE-ADDRESS / EC-RANGE-PTR) belong to the deferred EC
subsystem (M2-PROC-4). guard-fast ALL GREEN (1196 / 509 / 364), NIST verdicts byte-identical to serial. Next: slice
3 (`ALLOCATE` / `FREE`).

