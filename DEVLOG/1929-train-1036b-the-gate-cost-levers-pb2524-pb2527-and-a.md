## Entry 1929 — 2026-10-07 21:16 PDT — Train 1036b: the gate-cost levers (PB2524-PB2527) and a ReadyToRun runtime (PB2528)

**Why.** At 17:05 PDT the owner wrote "Something seems wrong when a fix takes 10 minutes and the gates take 10X
longer." The w1033 diagnosis read 22 implementer `verdict.json` files. A gate held its slot for 430-940 s, and about
230 s of that went to the twelve audits, which ran one after another inside the slot. Most of the roughly 600 s each
gate waited for a slot was other gates running their audits.

**The gate-cost levers — PB2524-PB2527 (landed).** PB2524: `run_gate_legs.py` runs the audits and the corpus fetch
before the gate queues for its slot, and times the wait (`slot_wait_s`). A red audit still ends the gate RED without
queuing. PB2525: `audit_doc_citations.py` checks every citation in-process over one loaded standard, with `cite.norm`
memoized, and the subprocess path is deleted: 78.2 s became 2.5 s, with `--all` output byte-identical over 949
citations. PB2526: every test host runs Server GC (`tests/Directory.Build.props`), and each gated assembly asserts its
own runtimeconfig. PB2527: the corpus runs in 24 partitions instead of 3, `OptionalWordSubsetDriftTests` became a
partitioned family of 8 (enrolled in the architecture oracle as `optword/<format>`), and a shared timings store under
the git common dir gives a fresh worktree a longest-first plan. Measured on one host with the arms alternated and the
cache off: the twelve audits went from 113.7 s to 28.3 s and now run outside the slot; the whole Conformance assembly
from 171.9/196.5 s to 95.7/68.0 s; the whole Unit assembly from 76.0/92.8 s to 49.0/65.8 s. A slot now holds 333 s for
a cold gate in a fresh worktree and 123 s for a warm re-gate. The evidence is in
`docs/rearchitecture/evidence/w1033-gate-levers/`, and DESIGN-test-build-ci 3.14.5 carries the numbers.

**ReadyToRun runtime — PB2528 (landed).** At 18:00 PDT the owner chose "Yes, gates and package (Recommended)". The
runtime project now publishes itself ReadyToRun once per platform (win-x64 and linux-x64, the CI runners), always
Release and incremental. The images flow to every test host and to the CLI's publish, and `AssemblyPackager.RuntimePath`
compiles and deploys each program against this platform's image. The CLI publish refuses to finish without every
image, and `ReadyToRunRuntimeDriftTests` checks the PE headers and that the tests and the package carry the same
bytes. The compile-cache key and the impact recorder were the other two arms and both are handled. Program start fell
about 27 % (104.7 to 74.5 ms and 102.8 to 76.5 ms; 24 corpus programs, 21 interleaved runs each). A runtime source
change costs about 4 s more to build. The gates now certify the Release R2R runtime, not the Debug one.

**Filed.** PB2529: `audit_code_citations.py` is now the costliest audit (18.5 s). PB2530: the lander's single leg has no
longest-first plan (modelled 188 s against 118 s); this changes the lander contract, so it is the owner's call. PB2531:
three more scripts compute the git common dir instead of calling `gate_slot.git_common_dir`. PB2537: attended landings
starve behind the loop's trains; a landing lease is proposed (note only).

**Landing.** This is the first train landed under the batched-gating trial (PB2515). Lander gate GREEN (Conformance 10,965 · Unit 32,607 · Characterization 36, one leg, run 20261008T040240Z-e9f98e); Linux gate GREEN (hooks, unit, characterization, conformance, guard). The architecture oracle differed in exactly 23 of 7,499 cases, all ADDED `optword/<format>`: PB2527 made `OptionalWordSubsetDriftTests` a partitioned family and enrolled it in the oracle, so these are new cases, not changed output. PB2528 changes no emitted C# or diagnostic. Re-recorded as ba9bfd1ad825.

