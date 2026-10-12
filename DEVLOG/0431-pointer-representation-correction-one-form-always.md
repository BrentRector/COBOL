## Entry 431 — Pointer representation correction: ONE form (always ManagedPointer), no 8-byte byte handle

Design correction (owner caught it). DEVLOG 429 / §10.2 framed a "byte-backed pointer keeps the 8-byte handle floor"
alongside the typed `ManagedPointer` — that's **two representations for one concept**, the dual pattern the
singular-pattern directive ([[feedback_singular_pattern]]) rejects. Corrected: a pointer value is **ALWAYS a
`ManagedPointer`; the 8-byte byte handle is eliminated** (it was a Phase-1 placeholder). Pointers are always-typed,
**NOT gated by `EnableTypedFields`** (a managed reference is the only correct representation).

The reason the dual form is wrong, not just inelegant: a `ManagedPointer` **cannot be serialized to stable bytes**
(the `Buffer` ref is GC-relocatable), so the §2.5 byte-materialize floor — which works for `string`/`long`/`decimal`
because they round-trip through bytes losslessly — **does not apply to pointers**; there is nothing to fall back to,
and an 8-byte handle couldn't hold a real pointer anyway. The cases that would have wanted a byte image are all ISO
**implementor-defined / non-portable** and get a diagnostic / undefined treatment, NOT a synthesized handle:
REDEFINES a pointer↔bytes; a pointer written to a file. `CALL … BY REFERENCE` of a pointer aliases the caller's
`ManagedPointer` *field*; EXTERNAL pointer = a shared field. `LENGTH OF` a pointer reports the logical
implementor-defined 8 without an 8-byte storage slot. Phase-1 `NULL`/`SET`/`=` are reimplemented on `ManagedPointer`
observably-identically (the `pointer_data` conformance test stays green). §10.2 + memory updated. This SIMPLIFIES
slice 1b (one representation, no dual path).

