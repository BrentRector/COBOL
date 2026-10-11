## Entry 649 — 2026-07-06 22:29 PDT — M2-OO-1i adversarial review: 8 confirmed defects FIXED (incl. the predicted THIRD class-emit gap + a finalizer data race)

Ran a find→verify review workflow (wf_7355579f-e66, 4 reviewers → per-finding adversarial verification) over the five
M2-OO-1i commits (644-648). 11 candidates, **9 confirmed (8 unique — the SD landmine was found by two dimensions),
1 refuted, 1 verify agent hit the retry cap.** All 8 fixed here in one change set.

**Class-emit gaps — the SAME shape as the two caught during implementation (inc 3 `using`, inc 5 external backing); the
review predicted a third and found THREE:**
- **[MED] REPORT SECTION in an OBJECT/FACTORY paragraph → CS0103 / no report.** The Report Writer is a COMPLETE
  subsystem; `OoEmitTypeHalf` simply never called `RwEmitReportMembers` (engine fields) / `RwEmitReportConstruction`
  (build the engines in the ctor). ⚠ I first planned to LOUD-GATE this — the owner corrected me (gating a finished
  feature is backwards), so it is WIRED instead, mirroring the program path (`__Activate`). Golden `oo_object_report`
  (a per-object report FD, INITIATE/GENERATE/TERMINATE in a method, read back → `R=REPORT`). *(NIST already
  byte-locks the Report Writer at the program level: RW101A-104A in scripts/guard.sh; this is the first OO report.)*
- **[MED] OBJECT-paragraph SD (sort file) → CS0103.** `OoQualifyClassFiles` set `InstanceKeyField` on an SD, but
  `OoEmitFileMembers`/`EmitFileRegistration` skip SDs (`!IsSortMerge`) — so `FileKeyExpr(sd)` named an undeclared
  `this.__fkey_X`. Fixed: an SD keeps a static `Class::SORT::name` key (no InstanceKeyField). Golden `oo_method_sort`
  (an OBJECT SD sorted by a method → `S1=AAA/S2=BBB/S3=CCC`) — the fix makes SORT-in-object fully work.
- **[MED] Method file verb under `>>TURN EC-I-O … CHECKING` → `__IoCheckEc` undeclared (CS0103).** The class type
  never emitted the EC-I/O machinery. Fixed: `if (bound.Ec is { HasIoChecked: true }) EcEmitIoCheckEc(bound, w);` in
  `OoEmitTypeHalf` (a class has no declaratives, so this reduces to the §9.1.13.1 status→EC bridge). Also cleared a
  latent `_useDecls` cross-unit bleed. (Verified: an object file + EC-I-O checking now compiles + runs.)

**Runtime correctness:**
- **[MED] `~CobolObject` finalizer mutated non-thread-safe static registries from the GC thread** — a data race I
  introduced with the per-object finalizer (`Files`/`Locked`/keyed dicts are single-thread; a finalizer-thread
  `CloseAndDrop` concurrent with a mutator-thread `Open`/`Register` is UB on a plain `Dictionary`). Fixed: the
  finalizer only ENQUEUES the key (lock-free `ConcurrentQueue`); the mutator thread drains + does the real close at
  `Init`/`Open`/`CloseAll` (§9.1.4's NOTE licenses the GC-deferred timing).
- **[MED] EXTERNAL keyed (RELATIVE/INDEXED) connector not idempotent on re-registration** (pre-existing; newly
  exercised by an object EXTERNAL keyed FD) — `RegisterRelative`/`RegisterIndexed` lacked the `::EXT::` guard the
  sequential `Register` has (§13.18.22.4 GR4a), so a second describer clobbered the live connector. Guard added.
- **[LOW] `CloseAndDrop` didn't drop keyed connectors** (`Files.Remove` only touches the sequential dict) → a
  per-object RELATIVE/INDEXED connector leaked its registry entry. Added `KeyedDrop`.

**Diagnostics (spec fidelity):**
- **[MED] COBOLNET1519 cited the wrong § for REPORT/SCREEN** — REPORT `§13.9`→**§13.8.3 SR1**, SCREEN `§13.10`→
  **§13.9.3 SR1** (§13.10 is "Constant entry"). A cited § must MATCH (feedback_spec_fidelity_discipline).
- **[MED] GLOBAL ban enforced only on FDs, not DATA items.** §13.18.27.3 SR4 bars GLOBAL on a level-01 data item in a
  factory/instance/method too. Added the data-item guard (`OoGateClassGlobal` over `CallGlobalRoots` for object/
  factory; a `GateMethodGlobal` scan of method WS/LOCAL-STORAGE/LINKAGE — the method-WS case was mislabeled 0899
  "not implemented" when it is spec-FORBIDDEN). Facts `ObjectData_Global_1520`, `MethodData_Global_1520`.

Refuted: a duplicate keyed-drop finding re-cast as a benign memory nit (the confirmed LOW version covers it). Battery:
**1973 conformance (+4) · 216 unit · legacy integration 81 GREEN**, zero regressions. Closes all 8 confirmed findings.

