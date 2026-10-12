## Entry 761 — 2026-07-11 02:20 PDT — EXEC STEP A ~~COMPLETE~~ (see Entry 762 correction) / P7 §6c: StoreKindOf → StoreKindVisitor (the last 6b–6f consumer); the flagged BoundKeyedDelete "bug" resolved a NON-bug

The final consumer. `BoundStores.StoreKindOf(BoundStatement, DataItem)` → `StoreKindVisitor :
IBoundStatementVisitor<StoreKind?>` carrying `item` (the local funcs `Hit`/`TargetHit`/`ReceiversHit`/`Kids`/
`StoreOrKids`/`InitStores` are now instance methods; recursion `StoreKindOf(child, item)` → `child.Accept(this)`);
`StoreKindOf(s, item) => s.Accept(new StoreKindVisitor(item))`. A FAITHFUL, byte-neutral relocation — each of the 79
arms transcribed verbatim, the compiler proving all 79 covered with no duplicate. The 9 leaves that rode the former
`_ => null` "stage LOUD, never guess" catch-all (`BoundAllocate`/`BoundFree`/`BoundInvokeUniversal`/`BoundRaiseObject`/
the four pointer/object `BoundSet*`/`BoundSetCapacity`) are now explicit `=> null` Visits — so a NEW bound-statement
leaf can no longer silently join the stage-loud bucket; it is a compile error until classified. This path is NOT
byte-exact-characterized (its client is the OO property-ref desugar), so beyond the green battery (3158 conf + 32 char
+ 269 unit UNCHANGED) I verified the conversion arm-by-arm against the old switch.

**The flagged `BoundKeyedDelete`-misses-`InvalidKey` "latent bug" (DEVLOG 760 handoff) was investigated and RESOLVED as
a NON-bug.** `StatementBinder.BindStatement` marks the pending-property-ops list on entry and drains its own suffix, and
EVERY nested body binds through its own `BindStatement` (via `BindBlocks`/`Select(BindStatement)`) — so a property ref
resolved inside a handler is drained by THAT statement's wrap, not the enclosing one. A property temp therefore lives
only in the carrying statement's DIRECT operands/condition, never in a separately-wrapped nested handler. So
`StoreKindOf`'s recursion into handler bodies is defensively total (it never actually finds the temp there), and
`BoundKeyedDelete` omitting that recursion is equivalent, not a silent-lost-store. Recorded the reasoning in the
`BoundStores` `<remarks>` (and noted a redundant-recursion cleanup as a separate, non-byte-covered future option).

**⛔🎉 EXEC STEP A (PHASE-07 Step 6, the exhaustive bound-tree visitor) is COMPLETE (6a–6g).** The Roslyn source
generator emits the 7 visitor interfaces + `Accept` + `StatementChildren`, and every hand-maintained dispatch/walk is
converted onto it: emitter `EmitStatement` (6b), `NumericRenderer` (6d), `ConditionRenderer` (6e), `OperandText` +
`ContainsNextSentence`/`AlterCollectFields` (6f), `StoreKindOf` (6c). Not one loud `_ =>`/`default:` dispatch arm
survives across the codebase — a missing arm is a compile error everywhere. This is the tooling foundation the owner's
"leverage the tooling" review called for; it structurally eliminates the completeness-bug class (the original
PHASE-05 `UsageCollectionPass` gaps). NEXT overall: Exec Step B = P6 (the Real Binder — `SymbolTable`/`BoundCompilation`/
`BindPipeline`); the rest of PHASE-07 (structural `Place`, god-class decomposition, `ICodeGenBackend`) is Exec Step D,
after P6.

