## Entry 434 — Stage-4 pointers slice 1b boundary 2: ADDRESS OF / SET ADDRESS OF / BASED-deref

Completes pointer slice 1b. Building on boundary 1's managed-pointer fields (Entry 433), this lands the three
statements that give a pointer a real address and dereference through it — all on the single `ManagedPointer`
representation, no 8-byte handle.

- **`SET p TO ADDRESS OF x`** — build a `ManagedPointer` over x's storage (`ManagedPointer.CreateByReference(buffer,
  offset, length)` from x's byte location) and store it into `_PTR_p`. The addressed item x is demoted to byte
  (classifier **trigger 6**, now reachable) so its `Buffer` is a real `byte[]`.
- **`SET ADDRESS OF b TO p`** — copy p's managed reference into the BASED item b's data-address pointer `_PTR_b`
  (FromPointer; b's `_PTR_` field was already registered by boundary 1's pass).
- **a reference to a BASED item b** — resolved to a new `IrBasedDerefLocation { PtrField, Length, Pic }` that pushes
  (`Buffer`, `Offset`, `Length`) from `_PTR_b` (`CilLocationEmitter.EmitBasedDerefArgs`, mirroring the LINKAGE
  `ManagedPointer` load) → the existing byte engine reads (DISPLAY) and writes (MOVE) the pointed-to storage
  unchanged. A separate `IrLocation` kind because `IrStaticLocation` hard-binds a fixed (area, offset); a BASED
  deref's buffer+offset are read from the pointer at runtime.

**Binding.** `DataStatementBinder.BindSet` gains the `setAddressStatement` branch (grammar landed slice 1a, Entry
430); `BindSetAddress` distinguishes the two grammar alternatives by token order (`ADDRESS` keyword before vs after
the first data reference) and emits a `BoundSetPointerStatement` (FromPointer for `SET ADDRESS OF`, FromAddressOf for
`ADDRESS OF` as a sender). `LowerSetPointer`/`EmitPointerStore` already handled FromAddressOf from boundary 1.
`LocationResolver` intercepts a whole-item BASED reference (both overloads) → `BuildBasedDeref` (length via
`FieldSizeCalculator`, descriptor via `CompilerPicDescriptorFactory`); `IrLocationExtensions.GetPic` +
`CilLocationEmitter.EmitLocationArgs` gain the `IrBasedDerefLocation` arm.

New conformance test `tests/conformance/2002/based_pointer.cob` (+`.out`): `SET P TO ADDRESS OF X; SET ADDRESS OF B
TO P; DISPLAY "B=" B` → `B=HELLO`; `MOVE "WORLD" TO B; DISPLAY "X=" X` → `X=WORLD` (the write through the BASED item
updates X's storage — proving the deref works as both sender and receiver). Guard ALL GREEN (**1196 / 508 / 364**).
**Pointer slice 1b COMPLETE.** Next: pointer arithmetic (`SET p UP/DOWN BY n`, pointer relations) then `ALLOCATE`/
`FREE` (§10.4 slices 2–3), then Stage-4 **OO → .NET classes**.

