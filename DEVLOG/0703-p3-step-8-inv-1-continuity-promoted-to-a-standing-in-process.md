## Entry 703 — 2026-07-08 16:38 PDT — P3 Step 8 — INV-1 continuity promoted to a standing in-process gate (full sweep) + INV-1-strong-2023 CI leg

**PHASE 03 step 8 (exit criteria 1 + 4).** Moved the INV-1 continuity sweep from the out-of-band bash script into
the authoritative in-process test surface, and guarded INV-1-strong per CI.

- **Continuity (exit 1):** extended the EXISTING `VersionMatrixTests.Cobol85Program_StillCompilesAtLaterEdition`
  from a 13-row `[InlineData]` seed to the FULL witness set via `[Theory][MemberData(ContinuityCells)]` — the
  corpus green∪divergent set (349) × {2002,2014,2023} = **1047 cells**. Each cell: compile PERMISSIVE (a break is
  a regression) + compile STRICT (a rejection must carry a recognized edition-band code COBOLNET08xx/09xx, never a
  generic COBOL0001). Uses `EditionHarness.CompileNist(..., checkOnly:true)` (parse+bind, no Roslyn — the verdict
  is settled pre-backend, DEVLOG 627); xUnit runs the cells in parallel (~24s). Green.
- **INV-1-strong (exit 4):** the full golden battery byte-exact at `--std 2023 --permissive` is the EXISTING
  env-parameterized `NistDifferentialTests` (`COBOLNET_NIST_STD=2023 COBOLNET_NIST_PERMISSIVE=1`) — verified
  locally **349/349 byte-exact** (98s). Guarded per-CI by repurposing the `inv1-sweep` job → `inv1-strong-2023`
  (runs that test re-editioned). The former bash continuity CI step is retired (the in-process test is
  authoritative, cross-platform); `scripts/version-continuity-sweep.sh` stays as a local CLI convenience.

**MECHANISM CORRECTION (owner feedback, transparency):** I first wrote a new `VersionContinuitySweepTests` class
using `[Fact]` + a manual `Parallel.ForEach` loop — a THIRD ad-hoc test mechanism alongside the repo's
`[Theory][MemberData]` convention, and it ignored that the continuity test ALREADY existed (the 13-row seed). The
owner flagged it ("why a new mechanism instead of integrating / migrating?"). Reverted the class; extended the
existing `[Theory]` to the full `[MemberData]` sweep instead. **Lesson (owner-corrected):** the rule is NOT "never
fork" — it is ONE mechanism per job, and it must be the BEST one: fork a new mechanism ONLY if it is genuinely
better (more maintainable / understandable / a better implementation), and then MIGRATE ALL existing usages to it;
if it is not better, use the existing one. Two coexisting mechanisms for one job is the anti-pattern. Here
`[Fact]`+`Parallel.ForEach` was NOT better than the established `[Theory][MemberData]` (it loses per-cell
granularity + reporting; the suite runs 3092 `[Theory]` cells fine), so the existing mechanism won — forking
without that better-justification was the error. [[feedback_singular_pattern]]

Test + CI only (no `src/` change; `EditionHarness.CompileNist` gained a `checkOnly` param). **Battery:**
conformance **3092** (+1034 continuity cells) · unit 227 · characterization 32 GREEN; FULL legacy guard NIST
**353 MATCH**, legacy unit 1196, integration 607 GREEN. RESUME AT step 7 (behavior-variant matrix).

