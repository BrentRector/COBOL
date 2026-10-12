## Entry 1318 — 2026-08-18 12:28 PDT — PB93: an unresolved REDEFINES / RENAMES operand is diagnosed, a REDEFINES binds only to a PRECEDING sibling, a redefinition of a redefinition is SR7's error

**What landed.** kb/Work **PB93** (MAJOR, under-rejects): `01 CPY-B REDEFINES NOPE PIC 9(2).` with no `NOPE`
compiled clean — `DataBinder.ResolveRedefines` reported only the METHOD-scope miss (1577); the program-scope miss
left `RedefinesTargetName` set against a null `RedefinesTarget`, so the layout (`BitLayout`, `ImageWidth`,
`ByteWidth` — which skip a redefiner as an overlay) and the emitter (which gave it its own field) disagreed: a
storage shape no edition defines. The same whole-scope `FirstOrDefault` bound a REDEFINES to a LATER sibling
(§13.18.44.3 SR10 says the redefining entries FOLLOW the entries defining the area) and picked the FIRST of
duplicate names (NOTE 1 wants the nearest preceding). And the level-66 RENAMES loop `continue`d silently on a
FROM / THRU naming nothing, leaving a picture-less alias every later reference failed on with an unrelated
message. Now: the lookup is the NEAREST PRECEDING same-level sibling
(`scope.TakeWhile(not the subject).LastOrDefault(name)`), a miss is **COBOLNET1654** and the entry binds as an
ordinary item (`RedefinesTargetName` cleared — no half-state); a REDEFINES naming an entry that is itself a
redefinition is **COBOLNET1656** — SR7 ("each specify as data-name-2 the data-name of the entry that originally
defined the area") — error strict, warning + the anchor-chased chain under `--permissive` (the
documented-dialect-leniency seam; GnuCOBOL / IBM accept chains); an unresolved RENAMES operand is **COBOLNET1655**
(§13.18.45.3 SR4), positioned at the level-66 entry. Sweep: OCCURS DEPENDING ON (0851), SAME AS, TYPE (1530)
already diagnosed their unresolved names.

**Evidence.** Negatives `pb93-redefines-undefined-target`, `pb93-redefines-later-sibling` (1654),
`pb93-redefines-of-redefinition` (1656), `pb93-renames-undefined-operand` (1655), each at 85 and 2023;
`RedefinesTargetTests` (5 — the permissive chain runs with the ONE shared area: `MOVE "xy" TO C` displays
`xyxyxy`; the legal REDEFINES + RENAMES shapes still bind). Inventory SR-13.18.44.3-4/-7/-10 and SR-13.18.45.3-4
→ CONFORMS (GAP 3964 → 3960).

**Found on the way, registered, NOT fixed here.** **PB96** — a RENAMES range that spans a REDEFINES view
double-counts the view: `66 AC RENAMES A THRU C.` over `05 A PIC X(2). 05 B REDEFINES A PIC X(2). 05 C PIC X(2).`
is "ababcd" / LENGTH 6 where the record area is "abcd" / 4 (the span walk sums every leaf between FROM and THRU,
redefining leaves included). Wrong answer, silent — next after PB94.

**Gate.** Characterization 33/33 · Conformance (filtered: Redefines/Renames/Nist/VersionMatrix/Data/Storage/Layout/
Record/Corpus) 3301/3301 · Unit (filtered) 3369/3369.

