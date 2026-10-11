## Entry 1304 — 2026-08-18 03:42 PDT — PB70 lands: a GROUP can be reference-modified — one SR1 rule, one group-image channel, one group-image store, and a receiving chokepoint that can no longer drop a receiver silently

**PB70 — what the note said, and what the sweep found.** The note's repro (`01 TB. 05 EL PIC X OCCURS 3.` +
`MOVE TB(1:1) TO EL(3)`) died at run time with `NotImplementedCobolFeatureException: reference 'TB(1:1)'`. The
2026-08-09 sweep had already shown it was WORSE: every group ref-mod as a SENDING operand (relation, DISPLAY,
FUNCTION ORD argument) died the same way, and a group ref-mod as a RECEIVER — `MOVE "Z" TO TB(2:1)` — compiled
clean, ran to exit 0 and changed NOTHING: `ExpressionBinder.ResolveTargets` is `Select(ResolveReceiving)
.OfType<Place>()`, `ResolveReceiving` returned the resolver's null, and the receiver was dropped from the list
(`MOVE "Z" TO OK1 TB(2:1) OK2` moved into OK1 and OK2 and skipped TB). Root cause, one gate:
`ReferenceResolver.ResolvePlace` tested `item.Pic?.Category` against four categories, and a group's Pic is null.

**The spec.** §8.4.3.3.3 SR1 names identifier-1's admissible shapes — bullet 3 "an elementary data item of category
alphanumeric or an alphanumeric group item", bullet 9 "a group item that is neither a strongly-typed group nor a
variable-length group" (§8.5.1.12: a dynamic-length item or dynamic-capacity table subordinate to it — an
occurs-depending group is FIXED-length and admitted); §8.4.3.3.4 GR6: the unique data item "is considered to be an
elementary data item without the JUSTIFIED clause", class and category alphanumeric for a group. Every excluded shape
(a strongly-typed group, a numeric item of BINARY / PACKED / COMP-5 / float usage, an edited or numeric leaf
subordinate to a strongly-typed group, an index item, a pointer, an object reference) was ALSO a run-time
NotImplemented before — the wrong stage for illegal source.

- **One rule.** `ReferenceResolver.RefModExclusion(item)` is SR1 read as an exclusion test — the reason an item may
  NOT be identifier-1, or null. An exclusion is **COBOLNET1647** at bind. A leaf under a strong group keeps its own
  bullets: an alphanumeric leaf is admitted (bullet 3 carries no "not subordinate" clause), a numeric one is not.
- **One channel.** `GroupImagePlace(inner)` — the `NumericImagePlace` shape for groups: Read is the generated
  `AsImage()` (an occurs-depending group with data-name-1 outside sends its CURRENT-count part, §13.18.38 GR8, so a
  position past the count is a bounds violation and not a read of the unused area); a Tier-C group (a float /
  COMP-5 / INDEX leaf) has no image and stays the loud island; the resolver wraps an admitted group's place in it
  (a Tier-B REDEFINES view is already a character window). The `RefModPlace` Read/Write arms needed NO change.
- **One store.** `PlaceRenderer.WriteGroupImage(group, image, context)` — `FromImage`, the GR8a current-extent
  splice, the Tier-B window, the OCCURS DYNAMIC receiving accessor, the Tier-C loud — written ONCE. It replaced
  FIVE copies of the same rule (MoveEmitter's group receiver, InspectEmitter.EmitStore, StringEmitter.WriteImage,
  AcceptDisplayEmitter ×2), four of them carrying the same twelve-line "V59 RESIDUE (DA5)" paragraph verbatim.
  The INSPECT and STRING image stores now take a ref-mod receiver FIRST — over a group inner they fell into the
  group arm (`Item.IsGroup` is true through the decorator) and would have called `.FromImage` on a string.
- **One reader.** `RefModPlace.Category` — the unique item's category (`CategoryOf` over a PICTURE, ALPHANUMERIC
  over a group). Six consumers each spelled `Pic is {} ip ? CategoryOf(ip) : Alphanumeric`; `MoveBinder`'s
  returned null for a group ref-mod (so a MOVE into it took the conversion-free GR4 group path where Table 16
  governs the elementary alphanumeric receiver — `MOVE 1.5 TO GP(1:2)` now rejects COBOLNET0819, SR10).
- **The chokepoint.** `ResolveReceiving` never returns null silently: the resolver's `_diagnosed` set (an undefined
  name, SR3 ref-mod-of-ref-mod, the SR1 exclusion) is what `WasDiagnosed` answers, and an UNDIAGNOSED null — a
  name that resolved but whose reference shape this compiler does not implement as a receiver (a RENAMES span over
  a COMP leaf, today) — is COBOLNET0899 recognized-not-implemented, so the compilation fails instead of the
  statement running one receiver short. `.OfType<Place>()` stays; it can no longer hide anything.

**Measured** (golden `pb70_group_reference_modification`, 20 shapes): `TB(1:1)` → EL(3) gives ABA; `MOVE "Z" TO OK1
TB(2:1) OK2` writes all three; a relation and a DISPLAY over a group with a numeric leaf read its zoned positions;
ORD("A") − 1 = 65, LENGTH(GP(2:4)) = 4; stores across leaf boundaries land positionally (`MOVE "12" TO GP(3:3)`
leaves GB's image "12 "); INSPECT REPLACING / TALLYING over slices; UNSTRING both ways; a figurative; UPPER-CASE
of a slice into a slice; a class condition; EVALUATE; an occurs-depending group's current-count part (a store past
it lands in the current positions only). Negatives: the four SR1 exclusions, the Table-16 crossing, and the
receiving-residue shape. Verdicts SR-8.4.3.3.3-1, GR-8.4.3.3.4-2, GR-8.4.3.3.4-6 → CONFORMS (GAP 4045 → 4042). The
note's flags are corrected (wrong-answer + silent, MAJOR).

**Found on the way, registered:** PB88 — `STRING … INTO GP(3:3)` is ILLEGAL (§14.9.43.3 SR4 "identifier-3 shall not
be reference-modified") and the binder stages it as `BoundUnsupported` (a run-time NotImplemented) where a bind-time
rejection is owed; SR5/SR6/SR11 likewise. My first probe wrote that statement believing it legal — the spec, not the
probe, decides.

**Gates (wave-local).** Solution build · Characterization 33/33 · Conformance
`Corpus|Negative|RefMod|Reference|Move|Group|Inspect|String|Unstring|Accept|SpecTraceability|Redefines|Odo|Occurs|Table16|Strong|Renames|Oo|Call|Length`
**1532/1532** · Unit `SpecTraceabilityInventory|RefMod|Move|Place|Registry|Diagnostic|Manifest|Guard|Group|Table16|Emit`
**482/482** (after the code-location symbols were repointed at `ResolveImpl` / `Category` / `CategoryOf`) · CLI probes.
Docs: COBOLNET_DATA_MODEL_DESIGN.md (the Place catalog — GroupImagePlace, RefModPlace.Category, WriteGroupImage,
RefModExclusion, the chokepoint), DIAGNOSTICS.md regenerated (COBOLNET1647), kb/Work PB70 → landed, PB88 new, plan §0.
Battery owed for the PB84–PB88 batch. Next: PB71.


