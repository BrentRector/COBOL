## Entry 493 — COBOL.NET REDEFINES/RENAMES (3/n): Tier B — the string-canonical model (the dominant real case)

Step 3: Tier B (ISO §13.18.44; COBOLNET_DESIGN §4.2) — a DISPLAY-homogeneous class (alphanumeric / DISPLAY-numeric /
numeric-edited views over one storage area). The class is ONE `string` backing of class-max width (the canonical's
VALUE seeds it, SR9); EVERY member — including the canonical — is a typed `(offset,width)` window over it, so exactly
one stored field exists and a write through any view is visible through every other (NO byte[]).
- `RedefViewPlace` (new `Place`): `Read()` = the character window (`CobolString.RefMod(backing, off+1, w)`); `Write` =
  `CobolString.SpliceInto` back into the backing. A numeric Tier-B view is flagged `StoreAsImage` (set in the
  classifier), so it rides the exact Slice-A pipeline — `FieldNum`→`CobolNum.ParseDisplay(window)`, the store path
  →`FormatDisplay`+splice — zero new emitter branches (the keystone simplification).
- Classification: `AssignClassOffsets` computes each member/subordinate's window offset within the class image
  (top-level members at 0, subordinates accumulate); Tier-B numeric leaves get `StoreAsImage`.
- `FieldEmitter`: a `PhysicalFields` helper is the ONE place the canonical-backing substitution + view suppression
  live — it yields an item's own field, OR a Tier-B class's single backing (once, at the canonical), skipping views;
  used uniformly by the root loop, struct decls, `ComposedInit`, and `AsImage`/`FromImage`. `PhysicalImageWidth`
  gives a group's true emitted width (a contained class counts its backing once, not its views) so a group that
  CONTAINS a redefines class (the CCVS COMPUTED-X shape) images correctly. `ImageInitOf` seeds the backing from the
  canonical's VALUE. `ReferenceResolver`/`OperandText`/`EmitGroupMove` integrate the view (its `Read()` is already the
  image; a group-view MOVE splices rather than `FromImage`).
- 8 Tier-B differential tests: numeric-view write/read (the latter via `ParseDisplay`), alphanumeric view over a
  numeric canonical, write-one-view-read-another coherence, partial-field group view, larger-redefiner (SR8),
  in-group backing (the COMPUTED-X nesting), original-VALUE-seeds-backing (SR9).

**Harness finding:** an `ADD 5 TO WS-B GIVING WS-C` test diffed (legacy 0030 vs cobolnet 0005) — the three-operand
`ADD … TO … GIVING` form drops the TO-operand from the sum; a separate arithmetic-binding gap (decoupled the test).
Updated `COBOLNET_REDEFINES_DESIGN.md` to mark the "overpunch deferred" sequencing dependency RESOLVED (CobolNum
already ships `FormatDisplaySigned`/`ParseDisplay`). Conformance 161→169 green, unit 13 green; legacy oracle untouched.

