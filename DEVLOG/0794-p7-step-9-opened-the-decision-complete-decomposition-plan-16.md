## Entry 794 — 2026-07-11 16:01 PDT — P7 Step 9 opened: the decision-complete decomposition plan (16-file coupling census) + 9a `NameAllocator`

**Design first (the phase doc's Step 9 AS-BUILT PLAN block — recorded BEFORE code moved, per
[[feedback_plans_in_repo]]).** A fresh 16-agent coupling census (`wf_d677d614-5fb`; the p7-audit anchors predate
Steps 1–8) mapped every partial's instance-field reads/writes + cross-partial method edges. Load-bearing findings:
the hub edges are `EmitMove`/`EmitStatementList`/`StoreArith`+`EmitArith`/`EmitUseHook`+`EmitStoreFileStatus`/
`ConvertSource`; counters are file-local except `_storeTmpCounter` (core+Oo) and `_ecCounter` (core+EC+Call); the
EC scratch trio (`_sizeErrVar`/`_sizeErrEcVar`/`_ecInfo`) interlocks the Exceptions partial with arithmetic; and
the EC↔statement cycle (`EcEmitChecked` → `EmitStatement`; statements contain EC-checked children) proves NO pure
bottom-up extraction order exists — a composition root must wire the cycle (during migration the host IS the
root; at 9n a per-unit `UnitEmitters` root replaces it). Sub-commits 9a–9n sequenced in the phase doc; the
audit-flagged risks each got an explicit answer there (host survival = `CSharpEmitter` stays the thin bind-host
facade until P9; counters = ONE `NameAllocator`; dispatcher/EC/CALL state = three typed per-scope objects).

**9a landed: `NameAllocator`.** The 15 run-unit unique-name counters scattered over 9 partials (the audit's
byte-exactness risk: per-verb emitter instances would renumber every temp) collapse to the ONE allocator the
DESIGN §2.5 collaborator table already planned — 15 distinct `Next*()` methods preserving each counter's own
sequence; ONE instance per run unit (created in `CallEmitRunUnit`), threaded onto all three per-unit
`EmitContext` constructions as `Names`. ~47 use sites re-pointed mechanically; the 15 fields deleted.

**Verify.** Sln Debug clean; 281 unit · 33 characterization (32 snapshots byte-exact — the counter-sequence
proof) · conformance verdict read before commit as a separate action (the 792 rule).

**What (Step 7, adapted per the premise audit's findings — deviations recorded in the phase ledger).**
- `Binding/Bound/MoveClassifier.cs` NEW: `enum MoveKind {RefModSlice, Group, GroupToElementary,
  FigurativeToNumericImage, Convert}` + `MoveClassifier.Kind(source, target)` — ONE pure function over
  bind-time facts mirroring the pre-P7.7 EmitMove dispatch order EXACTLY (§14.9.25.4 GR4's
  elementary-vs-group decision incl. the sender side; the S/Q/H/L-figurative/non-digit-ALL-into-numeric
  pre-2023 fill shape §14.9.25.3 SR5). `IsGroupSender` moved here (the ONE GR4 sender-side test).
- `BoundMove.Kinds` is a COMPUTED record property (`MoveClassifier.Classify(Source, Targets)`) — every
  construction classifies automatically, including the emitter's 11 synthetic implicit-MOVE sites (READ INTO /
  WRITE FROM / RETURN INTO / INITIALIZE / CORRESPONDING / RETURNING), which needed ZERO call-site changes and
  CANNOT bypass the classifier (the audit's synthetic-constructor risk, dissolved).
- `EmitMove` is a pure per-kind renderer (`switch (m.Kinds[i])`); `TryEmitFigurativeToNumericImage` →
  `EmitFigurativeToNumericImage` (eligibility is the node's kind; an unmatched shape throws — classifier/renderer
  drift is an ICE, not a fallthrough).
- **DEVIATIONS from the doc's sketch (audit-mandated):** (1) Kind is PER TARGET, not scalar — one MOVE can have
  a ref-mod slice, a group, and an elementary receiver at once. (2) NO `StorageForm TargetForm` on the node —
  `DataItem.Storage` is null while the binder runs (StorageFormPass is group-tail, and the MOVE's own bind
  records the deciding facts); the emitter reads the by-then-settled `item.Storage` projection. (3)
  `ConvertSource`'s receiver-category switch STAYS as the Convert-kind renderer — it is shared infrastructure
  (Report Writer's synthetic print items call it with NO bound node; INITIALIZE routes through EmitMove for
  identical conversion semantics); what died is the emitter-side DISPATCH re-derivation, which was the actual
  bind-once violation. (4) `VersionConformancePass.GateMove` is NOT repointed this step: it classifies
  sender×receiver CATEGORY legality (§14.9.25.3 SR5 edition rows), a different axis than MoveKind's structural
  dispatch — noted as a possible consumer if a future kind gains category precision.

**Step 8 — reconciled as DONE-BY-P5 (the audit's verdict):** the mutable flag, `MarkStoreAsImage`, both
re-syncs, and the prove-then-delete corpus assert were all landed/retired by P5.7 (DEVLOG 777); the ~24
remaining CodeGen `StoreAsImage` reads are the READ-ONLY projection over `Storage` — exactly the step's
intended end-state, encapsulated. The phase doc's Step-8 text + verify criterion are annotated accordingly;
the residual stale MarkStoreAsImage PROSE (5 comments) is swept in this commit.

**Verify.** Sln Debug + Release clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots
byte-exact + ratchet). Commit gated on the verdict line read as a separate action (the 792 rule).

