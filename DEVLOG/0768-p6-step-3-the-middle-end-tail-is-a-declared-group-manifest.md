## Entry 768 — 2026-07-11 10:04 PDT — P6 Step 3: the middle-end tail is a DECLARED group manifest — ProcedureBinding → UsageCollectionPass → StorageFormPass, one DAG with the resolve prefix

The formerly-hidden second half of the pipeline is now a real manifest. `BindPipeline` gains **`GroupTail()`** — three
`GroupBindPass` entries (`ProcedureBinding` [FilesResolved→ProcedureBound], `UsageCollectionPass`
[→UsageCollected], `StorageFormPass` [→StorageComputed]) run by `BinderDriver.Bind` over a `GroupBindContext`
(units + classes + session + the OO seam) — replacing P5's three DrivenByEmitter THROWER entries that existed only to
validate the DAG. **`ValidateFullChainOnce()`** validates resolve-prefix ++ group-tail as ONE monotone chain (called
from both the first `BindResolve` and the first `BinderDriver.Bind`); `LastResolvePhase` + the prefix filter are gone —
`Build()` IS the prefix. `IBindPass` metadata split into `IPassInfo` (Name/Requires/Produces) shared by both pass
shapes so one `ValidateDag` covers the whole chain; a new test pins the cross-section guard (group tail moved ahead of
the prefix → throws).

**StorageFormPass is now the manifest-ordered OWNER of the storage-form decision** (the doc's Step-3 intent): its
`Run(ctx)` = compiler-temp re-sync → `MarkStoreAsImage` (moved here from BinderDriver, where Step 2 had parked it out
of CodeGen) → the OO override-crossing harmonize (via the `IOoBindHost` seam — it only mutates `StoreAsImage`, and it
MUST sit between marking and classification) → `Compute`. `UsageCollectionPass.Run(ctx)` likewise encapsulates its
class/factory/unit collection incl. the OO formal groups. DEVIATION from the phase doc, already recorded in its
STATUS: the FILE whole-group loop is NOT folded in — P5 made it the `MarkFileRecordImageLeaves` RESOLVE pass because
statement binding consults `IsCharacterImage` (the SORT SD check, ST102A) — that ordering fact supersedes the doc's
"fold the FILE loop into StorageFormPass"; the pass doc now says so.

Neutrality: characterization 32/32 byte-identical, conformance 3158 identical, unit 270 (=269+1 new DAG test), legacy
guard green. Tooling: no new traversals — the group bodies are the verbatim relocated loops; the pass ORDER is the only
thing that moved (into the one declared manifest).

