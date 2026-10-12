## Entry 781 — 2026-07-11 13:21 PDT — P5.11b: `StrongTypeModel` extracted off `DataItem`

**What.** PHASE-05 Step 11b (DESIGN-data-model §2.4; DESIGN-module-topology row 19): the strong-typing overlay —
`StrongRoot` / `IsStrongGroup` / `IsStronglyTyped` / `TypeAnchor` / `SameStrongType` / `RelativeMemberPath` — moves
from `DataItem` instance members to `Binding/Model/StrongTypeModel.cs` static helpers. `DataItem` keeps only the
STORED facts `ExpandTypes` writes (`StrongType`, `TypeName`); the derived tree-walking predicates and the §8.5.3.3
same-type test are a use-restriction OVERLAY consulted at 6 check sites, not core record shape. Per the doc's
"prefer repointing callers": NO forwarding props — all 6 sites repointed (`CheckStrongTypeDeclarations` ×2
[§13.18.57.3 SR4/SR3], `AllItems`' TypeAnchor filter, `CheckStrongMove` [§14.9.25.3 SR2], the class-condition gate
[§8.8.4.4.3 SR1], the relation gate [§8.8.4.2.3 SR1]). The two extra predicates beyond the doc's named four ride
along — they sit in the same removed span and are the same overlay (IsStrongGroup/IsStronglyTyped).

**Verify.** Sln Debug + Release clean; 3166 conformance · 281 unit · 32 characterization; legacy guard MATCH.

