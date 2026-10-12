## Entry 809 — 2026-07-11 18:32 PDT — P7 Step 9n: the FINAL WIRING — ProgramEmitter/DispatchEmitter/StatementEmitter + the `UnitEmitters` composition root; every host shim retargeted; the god class is a bind-host facade

**What (Step 9's last sub-commit — the emitter decomposition completes).** Four new spine classes; the
transitional host edges are GONE:
- **`StatementEmitter`** — the exhaustive `IBoundStatementVisitor<bool>` (the 79 Visits, verbatim from the
  Dispatch partial) + `EmitStatement`/`EmitStatementList`. Its ctor COPIES direct collaborator refs out of the
  root (every collaborator exists when the root news it), so dispatch reads fields, not a hub.
- **`DispatchEmitter`** — `EmitDispatcher`/`EmitDispatchMethod`/`EmitUseMachinery`/`EmitParagraphBody` (+ the
  `StatementChildren`-riding `ContainsNextSentence`), parameterized over program AND class units (the
  `__MDispatch` swap still rides `DispatchState.DispatchName`).
- **`ProgramEmitter`** — the run-unit orchestrator (the Call partial's emit half, `Call*` name prefixes
  dropped): module preamble, interface/class/program unit loop, program-class plumbing, entry wrapper. Owns the
  run-unit state (`NameAllocator` + the three Step-9b state objects) and the LIVE **`Current`**
  `UnitEmitters` root; `BeginUnit` (now here) re-creates `Current` at every unit switch.
- **`UnitEmitters`** — the PER-UNIT composition root: builds ctx/renderers + all 20 collaborators in acyclic
  ctor order, then property-wires the census's cyclic edges (verbs↔`Statements`↔`Ec`; `KeyedIo.SeqIo` — SeqIo's
  ctor already takes KeyedIo for the shared READ bodies, so the reverse edge can't be a ctor arg).
- **Every collaborator ctor retargeted** off `CSharpEmitter host` to direct refs (16 files): the EmitMove/
  ConvertSource → `MoveEmitter`; StoreArith/EmitArith/RcvFor → `ArithmeticEmitter`; the Ec* shim sheet →
  `EcEmitter`; the file-I/O common services → `SequentialIoEmitter`; EmitStatement(List) → `Statements`.
  `OoEmitter` now reads the per-unit set through `program.Current` (the LIVE-accessor hazard preserved: class
  units re-new mid-run) and takes the class table + interface forests from the immutable `BoundCompilation`
  (`comp.OoClasses`/`comp.InterfaceData`) — emission reads ZERO bind-host session state.
- **`CSharpEmitter` = the bind-host facade** (the RECORDED deviation): `Bind`/`EmitBound` (→ `new
  ProgramEmitter().Emit(comp)`) + `IOoBindHost` + the OO BIND half (Oo partial) + `_bindSession`/`_turnState`/
  `_ooClasses`/`_ooIfaceData`. The Dispatch/Call/Exceptions partials are DELETED; the dead `Turn` accessor and
  the emit-time `_turnState`/`_ooClasses` restores went with them (no emit-time reader existed — grep-proven).
  Ratchet: the Call partial's 14 pinned fragments relocated VERBATIM → `ProgramEmitter.cs` = 14 (exact-sum).

**One regression, caught by the net and fixed before commit (transparency).** The first conformance run came
back 3160/3166: the 6 failures were the interface-corpus legs — `OoEmitter.EmitInterfaceUnit` passed the live
`Refs` accessor (`program.Current.Refs`) into `BeginUnit`, but interfaces emit FIRST, before ANY unit has
begun, so `Current` was still null → NRE. The pre-9n code "worked" by passing the host's null-or-stale `_refs`
FIELD (unread — interface units emit no statements); the live-accessor rewrite turned that latent
null-argument into a null-DEREFERENCE. Fix: pass `null!` explicitly with the invariant documented (no resolver
CAN exist there, and none is consulted). Characterization couldn't catch it — no snapshot covers an
interface-bearing program (the conformance net did, exactly as designed).

**Verify (post-fix).** Compiler + sln Debug clean; 33 characterization (32 snapshots byte-exact + ratchet) ·
281 unit · 3166 conformance green — verdicts read as separate actions before the commit. Pure C# refactor — no
grammar touch, no legacy-guard leg required (phase doc §1). Stale-comment sweep: BinderDriver/
StatementBinder.Exceptions/EmitterState/SequentialIo/OoEmitter xrefs updated to the new homes.

