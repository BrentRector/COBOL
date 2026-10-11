## Entry 836 — 2026-07-12 02:30 PDT — P7 Step 11c/11d: RefModPlace + NumericImagePlace → PlaceRenderer; subscript→BoundExpr DEFERRED to D10 (owner decision)

**The fork.** Structural `Place`'s design (`IndexSegment(BoundExpr)`) assumes a subscript / ref-mod position is an
expression, but today it is a flat SUBSCRIPT-mode token stream that `ReferenceResolver.RenderSegment` renders to a C#
`long` string — there is NO `BoundExpr` path, and the only tooling-correct way to get one (removing the SUBSCRIPT lexer
mode) is the D10 sub-track owned by PHASE 15 §"CUT 2.5", blocked until the legacy cutover. Producing a `BoundExpr`
subscript now would mean hand-rolling a subscript parser — the exact anti-pattern D10/Step 12 exist to delete.

**Owner decision: defer subscript→`BoundExpr` to D10/PHASE 15.** Step 11 moves the runtime-call text + access-path
composition off the bound tree into `PlaceRenderer` NOW; a subscript index stays the rendered `string` it is today (a
`Place`/segment field — byte-exact), and the subscript→`BoundExpr` conversion FOLDS INTO D10 when the flat-token
mechanism is actually removed. The R5 neutrality test is scoped to string-returning RENDER METHODS (gone), not the
deferred subscript-string fields. Recorded in the PHASE-07 Step 11 plan + [[project_phase04_d10_deferral]].

**Landed (the decorator tier — all four now migrated).** **11c** `RefModPlace`: `Read`/`Write`/`WriteFill` (§8.4.2.4
ref-mod; the §14.6.8.6 boolean-zero splice pad) render in `PlaceRenderer` over `Inner` + the transitional
`Start`/`Length` strings (`RuntimeApi.StrRefMod`/`StrSpliceInto`); record methods → the `RenderedElsewhere()`
tripwire. `RefModPlace` is a top-level result (never an `Inner`), so throwing is safe — and migrating it routes its
`Inner` through `PlaceRenderer`, which UNBLOCKED **11d** `NumericImagePlace` (its only wrappers are `RefMod`/`Renames`,
plus emit-time `OoEmitter` constructions that 11a already routed): its `FormatDisplay`/`StoreDisplay` render moved
too. This is the migration reverted in 835 for the top-down rule — now safe because both wrappers precede it.

**Battery green at each:** characterization 33/33 (byte-exact + ratchet), unit 281/281, conformance 3166/3166 (NC224A,
the 835 red, now passes). NEXT: the leaf subtypes (`MemberPlace` access-path composition, `DynTablePlace` fold,
`RedefViewPlace`, `CapacityRegisterPlace`) + the binder-side string leaks + the `Place.Read()/Write()` delete.

