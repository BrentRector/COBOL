## Entry 726 — 2026-07-09 19:14 PDT — PHASE-03 Step 14h.2: the self-identifying statement gates → parse-arm

The first gate-relocation commit on the 14h.1 foundation. Six statement gates whose IDENTITY is a single dedicated
grammar rule move from the bound-arm (and one from bind-time) to the `ParseArm`, firing on RECOGNITION:

| Gate | from | parse-arm override (rule) | where-string (verbatim) |
| --- | --- | --- | --- |
| `UnlockStatement2002` | bound `case BoundUnlock` | `VisitUnlockStatement` | "the UNLOCK statement" |
| `Free2002` | bound `case BoundFree` | `VisitFreeStatement` | "the FREE statement" |
| `AlterRemoved2002` | bound `case BoundAlter` | `VisitAlterStatement` | "the ALTER statement" |
| `DeleteFile2023` | bound `case BoundKeyedDeleteFile` | `VisitDeleteFileStatement` | "the DELETE FILE statement" |
| `SetAddress2002` | bound `case BoundSetAddressOfBased`/`BoundSetPointer{Address}` | `VisitSetAddressStatement` | "SET ADDRESS OF (ISO §14.9.39 Format 7)" |
| `Allocate2002` | **bind-time** `StatementBinder.Ptr.cs` | `VisitAllocateStatement` | "the ALLOCATE statement" |

Two of these are the DEVLOG-724 bug FIXES, not mere relocations: `DeleteFile2023` (an undeclared file returns a
`BoundUnsupported` before a `BoundKeyedDeleteFile` — the bound-arm dropped the 0900) and `Allocate2002` (a bad
RETURNING returns a `BoundNop`; that is exactly why 4bfa1418 reverted it to bind-time — now the parse-arm carries it
correctly). `SetAddress2002` unifies its TWO former bound shapes (receiver `SET ADDRESS OF x TO p` + sender `SET p TO
ADDRESS OF x`) under the ONE `setAddressStatement` rule → one override, one Check per statement. Each gate fires once
per statement on presence (never per operand — a compound ALTER / multi-pointer FREE is one node); byte-identical to
the former gates for well-formed programs, a strict SUPERSET (0900 now also names the edition on the malformed
below-edition paths the bound-arm dropped) for the rest — no contains-based test regresses.

Battery: greenfield conformance 3114 · unit 227 · characterization 32 (fresh CobolSharp.sln build); INV-1-strong +
FULL legacy guard unaffected (14h is greenfield-only, no grammar change). Recon wf_be806171-b25.

