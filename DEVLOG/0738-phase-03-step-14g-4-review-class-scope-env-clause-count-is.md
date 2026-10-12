## Entry 738 — 2026-07-10 00:20 PDT — PHASE-03 Step 14g.4 review: class-scope env-clause count is now the spec-correct 1× (surfaced a latent OO-env bind bug)

An adversarial find→verify review of the 14g.4 commit (`81dd6c37`, one subagent, H1–H5) found **1 confirmed COUNT
divergence** — but investigation shows the parse-arm is MORE correct than the code it replaced, so this is a
disposition + regression-pin, not a parse-arm fix. (H1 shared-rule, H2 PD RETURNING/RAISING scope partition [incl.
nested-program included / interface-method excluded], H4 where-strings, H5 CheckOnly parity — all cleanly refuted.)

**The finding (H3).** For a CLASS, the five env-division gates (SHARING/LOCK-MODE + the three SPECIAL-NAMES FOR sites)
diverged in COUNT from the former bind-time Checks. Root cause is a PRE-EXISTING latent binder bug the migration
surfaced: `OoReparentClassData`/`OoReparentFactoryData` (CSharpEmitter.Oo.cs:143/155) each ADOPT the class-level
`environmentDivision` as a fallback child, and the binder reads the SINGULAR `environmentDivision()` accessor + runs the
factory binder unconditionally — so the class-level env is bound **2×** (no object/factory env → both reparent binders
adopt it), **0×** (both halves have their own env → the class env is shadowed in both), or 1× (one half has its own).
The former bind-time gate therefore fired 2×/0×/1×; the parse-arm fires **exactly 1× per clause node** — the spec-correct
count (one clause = one 0900). Matching the old count would mean replicating a bug.

**Disposition: keep the parse-arm (correct); pin the correct 1×; flag the underlying bind bug.** Byte-neutral holds for
every PROGRAM-unit scope (the 14g.4 witnesses); for CLASS scope the gate count is now the spec-correct 1× — a correction,
never a regression, and the compile VERDICT never changes (a below-2002 class already fails on its CLASS-ID 0900). Added
`ConfigPdEditionTests.ClassLevelSpecialNamesFor_At85_GatesExactlyOnce` (the 2× case → 1) + `…_Shadowed_…` (the 0× case →
1). ⚠ **FLAGGED LATENT BUG (not fixed here — out of scope for a gate migration, and a safe fix means reworking OO env
visibility-vs-registration):** the class-level env double/zero-BIND also mis-processes NON-gate class-level config —
CURRENCY/DECIMAL-POINT/ALPHABET/switch registration and SELECT file-models are bound 0/1/2× (mostly idempotent, but the
shadow-0× case SILENTLY IGNORES a class-level SPECIAL-NAMES/FILE-CONTROL in a valid 2002+ class). A dedicated OO-env fix
should bind the class-level env exactly ONCE while keeping it visible to both halves for reference resolution.

Battery: greenfield conformance **3149** (+2 witnesses) · unit 223 · characterization 32. Test-only change
(`VersionConformancePass` unchanged) — CI runs the full legacy guard as backstop. RESUME AT Step 14g.5.

