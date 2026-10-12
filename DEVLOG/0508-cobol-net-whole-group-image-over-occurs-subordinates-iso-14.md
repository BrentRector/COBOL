## Entry 508 — COBOL.NET: whole-group image over OCCURS subordinates (ISO §14.9 / §14.4)

After abbreviated conditions (Entry 507), NC211A's next stop (GF-23) was a runtime loud guard whose message —
`MOVE to mixed-usage group 'IF-TABLE' with a COMP/binary leaf (Tier-C byte path, deferred)` — was **misleading**:
IF-TABLE is `02 IF-ELEM PIC X OCCURS 12`, pure character, **no COMP leaf**. The real gap was that the §14.4 whole-group
image facility (`AsImage`/`FromImage`, used by whole-group MOVE / DISPLAY / compare) excluded ANY group containing an
OCCURS subordinate: `DataItem.IsCharacterImage` had a blanket `Occurs is null` guard, so such a group was treated as a
mixed-usage byte-island and loud-rejected. OCCURS inside a group is ubiquitous, so this is a real facility gap, closed
on its own merits (NC211A is the milestone, not the justification).

**The complete feature, from one mechanism (ISO §14.9: a group move treats the whole group, including every OCCURS
position, as one alphanumeric item):**
- `IsCharacterImage` no longer excludes OCCURS — an OCCURS table qualifies when its element is character-image
  (recursion handles nested groups). 
- `FieldEmitter.EmitImageMethods` emits, for an OCCURS member, `string.Concat(arr)` (scalar element) or
  `string.Concat(System.Array.ConvertAll(arr, e => e.AsImage()))` (group element) in `AsImage`, and a per-occurrence
  distribute loop in `FromImage`; `Physical.Width` carries the table's TOTAL width (per-occurrence × count).
- `DataItem.ImageWidth` (the group branch) now multiplies each child by its fixed-OCCURS count — the bug that first
  truncated `FromImage(CobolString.Store("AABBCC", 2))` to "AA"; every `ImageWidth` consumer that uses a group's value
  (group MOVE, file record width, Tier-B view, redefines class) wants this OCCURS-aware total.
- `MarkStoreAsImage` recurses INTO OCCURS, so a numeric-DISPLAY-in-OCCURS leaf becomes string-stored — its array is
  `string[]`, the image facility distributes it with no special case, AND its subscripted accesses go through the same
  `CobolNum.ParseDisplay`/`FormatDisplay` pipeline (verified: `ADD 1 TO NE(2)` on a whole-group-aliased `9(3)` table
  reads/writes correctly). So char + numeric-DISPLAY + nested-group all fall out of ONE mechanism — the complete
  feature, not a char-only subset. (ODO variable-length and genuine COMP/float Tier-C remain deferred — distinct
  models, loud.)

**Tests — SPEC-ANCHORED** (`GroupDataDifferentialTests`, +3): the result is derived from §14.9 (group move fills every
OCCURS position) / §8.8.4.1 (group compare as the concatenated image) and asserted on COBOL.NET, then cross-checked
against the legacy oracle — OCCURS of alphanumeric, of numeric-DISPLAY (whole-group + subscripted), and of a nested
group. Conformance 263 → 266; 14 unit; the 7 green NC golden programs unaffected. Greenfield-only.

NC211A now reaches GF-23 and runs the whole program, but FAILs there (and at GF-28/29) on a SEPARATE, pre-existing
known-latent: a `MOVE` of a signed numeric (`PIC S9` +1, overpunch) to an alphanumeric element emits the zoned image
"A" instead of the de-signed digit "1". ISO §14.9.25.4 GR6a: "if the sending operand is described as being signed
numeric, the operational sign is not moved." That de-signing fix is the next slice.

