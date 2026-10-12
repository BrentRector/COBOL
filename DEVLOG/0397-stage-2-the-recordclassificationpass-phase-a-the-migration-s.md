## Entry 397 — Stage 2: the RecordClassificationPass (Phase A) — the migration's typed-vs-byte classifier

The numeric pipeline is fully on `CobolNum`; this opens the typed-flip phase (ADR Stage 2 → 3). A 4-agent
read-only **pipeline investigation** first mapped the territory: the `IrLocation` hierarchy
(`IrInstruction.cs:1246+`: Static/ElementRef/RefMod/OdoGroup/Cached, all carrying the `StorageLocation`
(Area,Offset,Length,PicDescriptor) quad), the MOVE/COMPARE dispatch points
(`CilDataEmitter.EmitMoveWithStandardSignature`, `CilComparisonEmitter`), `LocationResolver`, the `DataSymbol`
trigger fields, and the classifier insertion point (after `StorageLayoutComputer`, before lowering). Map saved.

**Decision: classifier-first (Stage 2 before Stage 3).** The `RecordClassificationPass` is the brain of the
migration (ADR §3 — which items go typed vs byte-island); it is fully unit-testable in isolation with **zero
codegen risk** and de-risks the hardest logic before any invasive Stage-3 flip. It is designed testable on
directly-constructed `DataSymbol`s: `Classify(items, categoryOf)`. Additive and **not yet consumed by codegen**
(Stage 2 keeps everything byte-backed regardless), so its current incompleteness changes no behavior.

**This slice = Phase A (data-division triggers) + the structural fixpoint:** REDEFINES (whole class, both
directions), RENAMES/66 (renaming item + FROM..THRU span), FD/SD record (`Area==FileSection`), LINKAGE
(`Area==LinkageSection`), IS EXTERNAL / IS GLOBAL, and edited items — plus REDEFINES-class closure and downward
island-membership transitivity to a fixpoint (monotone typed→byte, terminates). Default is typed; "any doubt →
byte" (`Get` defaults unseen items to byte). It correctly **omits** the cases the ADR keeps typed or defers to
Phase B: SYNC-alone (never triggers), USAGE POINTER (→ `ManagedPtr` typed), INDEX (→ int), OBJECT REFERENCE,
and ODO (→ `CobolTable` unless used as a whole-group operand — a Phase-B trigger).

**Phase B (procedure-division scan: refmod-of-numeric-DISPLAY, group MOVE/COMPARE/class-condition, CALL…USING
BY REFERENCE, ODO-whole-group, write-pattern) + the cross-edge fixpoint (Phase C) are the next slice** — required
before the classifier is consumed (ADR §3: complete before any flip). A full adversarial review of the *complete*
classifier will run before it is wired.

Process: implemented on `main` → 2-lens / 17-agent **adversarial review** → **0 confirmed, 15 refuted** (the
Phase-A logic verified correct — triggers correctly distinguished from Phase B/C, SYNC correctly omitted,
fixpoint + transitivity sound; refuted items were perf nits on tiny per-record lists + test-thoroughness notes).
Added 2 of the suggested coverage tests (chained REDEFINES, RENAMES-without-THRU).

Guard **ALL GREEN: 1159 unit / 481 integration / 364 NIST** (+15 classifier unit tests; additive — no
codegen/NIST change). Next: classifier Phase B/C, then the Stage-3 `IrDataSlot` dispatch + the first
character-data typed flip.

