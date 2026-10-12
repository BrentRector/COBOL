## Entry 430 — Stage-4 pointers slice 1a: the BASED clause (no storage) + pointer grammar

First implementation step of the approved Stage-4 pointer work. Added the COBOL-2002 grammar (approved DEVLOG 429):
a `BASED` lexer token; `basedClause : {is2002()}? BASED` in `dataDescriptionClause`; and the `SET pointer TO
ADDRESS OF identifier` sender alternative on `setAddressStatement` (the `SET ADDRESS OF … TO …` receiver form already
existed). The parser auto-regenerates from the `.g4` on build; verified the pointer probe parses under
`--standard cobol2002`.

Bound the `BASED` clause to a new `DataSymbol.IsBased` (SemanticBuilder mirrors the `IsExternal`/`IsGlobal` pattern),
and made **`StorageLayoutComputer.LayoutItem` skip `IsBased` items entirely** — no offset advance, no
`StorageLocation`. This fixes a real ISO non-conformance the investigation (DEVLOG 429) found: previously `BASED`
lexed as a generic noise word and the item wrongly got WORKING-STORAGE bytes; per §13.18.5 a BASED item has NO
storage until `SET ADDRESS OF`/`ALLOCATE` gives it an address.

`BASED`/pointer keywords are now always-reserved tokens (the existing precedent — `POINTER`/`ADDRESS`/`NULL_` already
are, accepted only under `is2002()`); the full guard confirms **zero corpus regression** (no program uses `BASED`).
Guard **ALL GREEN: 1196 / 507 / 364**. The `SET ADDRESS OF` forms parse but are not yet bound (inert, as the receiver
form already was) — **slice 1b binds them + adds the `ManagedPointer` pointer field, `ADDRESS OF`, and the BASED
deref** (a pointer-relative `IrLocation`).

