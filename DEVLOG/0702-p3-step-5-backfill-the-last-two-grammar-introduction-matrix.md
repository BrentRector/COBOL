## Entry 702 — 2026-07-08 16:14 PDT — P3 Step 5 — backfill the last two grammar-introduction matrix rows (the matrix is now gate-complete)

**PHASE 03 step 5.** The recon (DEVLOG 697) already showed the matrix was near-complete after P2: every binder
`ConstructRegistry.Check` id is a `constructs.json` row by construction (`Constructs.*` is generated from the json),
and the 34 grammar `{isXXXX()}?` predicates map to existing rows — EXCEPT two residue introduction gates that had
no row. Added both (verify-by-running set the metadata + status):

- **`procedure-raising-2002`** (PROCEDURE DIVISION … RAISING, §14.2.2). Verify-by-running: binds at 2002+ (a
  level-3 EC-USER SR fires, `COBOLNET0858`), but below 2002 the `{is2002()}?` gate yields a GENERIC `COBOL0001`
  ("no viable alternative at 'RAISING'"), not `COBOLNET0900`. → `status:"pending"`; making the below-edition
  diagnostic loud (0900) is Step-10 loud-hole work, and the row activates then.
- **`inline-method-invocation-2023`** (`identifier(args)`, §8.4.3; the sole rule salvaged from the deleted
  JSON/XML fragment). The `identifier(args)` form has no distinctive token (ambiguous with subscripting /
  function-reference), so it can't be cleanly gated as rows-only → `status:"pending"` (the OO wave owns it).

Registry regenerated (`gen-constructs.ps1`: 97 rows → Entries / 97 Constructs ids / 57 GateId members, +2).
There are **zero `{is2014()}?` grammar gates** (2014 features gate at bind time and already have rows), so no 2014
grammar gaps. The broad 85→2002 VCR *narrative* growth (Table 7) is blocked on the ISO/IEC 1989:2002 standard
(not in the repo — the VCR's own documented scope limit); left as incremental "grow as researched," NOT
fabricated from the 2023 spec.

Data + generated files only (no `src/` behavior change; the 2 rows are `pending`, unreferenced by any gate).
**Battery:** conformance 2058 · unit 227 · characterization 32 GREEN; FULL legacy guard NIST **353 MATCH**, legacy
unit 1196, integration 607 GREEN. `ConstructRegistryDriftTests` + `VersionMatrixTests.PendingRows_…` +
`VcrDriftTests` green. RESUME AT step 7 (behavior-variant matrix).

