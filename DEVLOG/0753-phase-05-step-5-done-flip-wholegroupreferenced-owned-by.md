## Entry 753 — 2026-07-10 22:15 PDT — PHASE 05 Step 5 DONE (flip): `WholeGroupReferenced` owned by `UsageCollectionPass`; `ReferenceResolver` mid-resolve mutation DELETED

The Step-5 FLIP: `UsageCollectionPass.Collect` now populates `DataBinder.WholeGroupReferenced` directly (writes the
real set, runs post-bind BEFORE `MarkStoreAsImage`), and the over-inclusive mid-resolve mutation is GONE.

**Deleted:** `ReferenceResolver.cs:277,300` (the two `if(item.IsGroup) WholeGroupReferenced.Add(item)` — the actual
"resolver mutates shared state mid-resolve" smell); `DataBinder.Oo.cs:282,284` (the OO formal/RETURNING direct adds —
now supplied to `UsageCollectionPass` via `OoFormalGroups` for both the object `Methods` and factory `FactoryMethods`
halves); the parallel `WholeGroupReferencedV2` field + `UsageCollectionPass.EffectDiff` + the D1 set-diff scaffolding.
FILE record areas remain owned by `MarkFileRecordImageLeaves` (their image flags are needed DURING binding by the SORT
binder), so `UsageCollectionPass` no longer re-adds them.

**Verification by OUTPUT (owner-directed), across the FULL greenfield battery — which caught two real effects:**
1. **A visitor GAP (regression, fixed):** `dyn_nested_group_move` (OCCURS DYNAMIC, 2014) emitted `B5=[  ]` instead of
   `[00]` — the visitor OVER-collected a `DynTablePlace` (a `MOVE "AB7" TO G(5)` into a group element of a dynamic
   table). A dynamic element is a typed `CobolDynTable<T>` struct converted by the TABLE codec (FromImage/AsImage on
   the element), never the item image mechanism — so its leaves must stay native. Fixed: `Visitor.P` skips
   `DynTablePlace` (matching the legacy dynamic-resolution path, which never added dynamic groups). My earlier
   NIST-only `EffectDiff` missed this because OCCURS DYNAMIC is a 2014 crafted-corpus feature — the CONFORMANCE runtime
   runner (`CorpusRunnerTests`) is the net that caught it.
2. **One expected, output-neutral emit change (re-baselined with proof):** `char_initialize` — `INITIALIZE WS-REC` is
   member-wise (§14.9.20 — each leaf to its category default), NOT a whole-group image fill; legacy over-flagged `WS-N`
   (image `string`) because it resolved the group, V2 correctly keeps it `long`. Runtime verified identical (`DISPLAY
   WS-N` → `000` either way); the characterization snapshot re-baselined (the ONLY snapshot that changed — `WS_N` field
   `string`→`long`, the `AsImage`/`FromImage` codec now routing it through `FormatDisplay`/`ParseDisplay`).

**Adversarial visitor-completeness review (`wf_a42e17c8-bca`) — 6 latent gaps found + ALL FIXED** (the corpus didn't
exercise them, so the battery was green — exactly the uncovered miscompiles a review catches; each mirrors the
proven-complete `VersionConformancePass.Recurse`): the `Visitor.Stmt` switch omitted **BoundKeyedDelete / BoundKeyedStart
/ BoundKeyedDeleteFile** (their INVALID KEY / ON EXCEPTION nested handlers never walked; + the START key `Operand`),
**BoundRelease** (`RELEASE rec FROM group` ≡ a whole-group MOVE, §14.9.32 GR4), **BoundInvokeUniversal** (receiver /
args / returning), and **BoundGoback** (`GOBACK RETURNING group` source); and `Collect` dropped the program/function
**`LinkageReturning`** group (only USING formals were re-added). Also DELETED the now-dead early-resolve of every
formal in `CallBindUnitProcedure` (`unit.Refs.ResolveItem` existed ONLY for the removed whole-group side effect —
confirmed dead: conformance stayed green) + fixed its stale comment.

**Battery:** greenfield conformance **3157** (all runtime differential + `CorpusRunnerTests` + `CorrespondingDifferentialTests`)
· unit **258** · characterization **32** (`char_initialize` re-baselined) · FULL legacy guard NIST **353 MATCH / 0 diffs**
(greenfield-only change — legacy compiler + frontend untouched).

**RESUME AT: PHASE 05 Step 6** — the prove-then-delete GATE checkpoint (Storage/RecordLayout/WholeGroup all proven; run
the full battery + guard + snapshots as the green light before the remaining deletions).

