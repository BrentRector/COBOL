## Entry 1950 — 2026-10-09 12:27 PDT — Train 1050: the gates' serial valleys removed (PB2880); PB2881-PB2885 filed

Owner, 2026-10-09: "give this to Mythos session to figure out using Opus - Those serial steps are what PB2880 targets",
and for the landing: "Land it alone now" (a one-change train; nothing else could be ready before the weekly reset).
Wave 1050 A (Opus, the Mythos lane on account 1) measured both landing gates with the counter API (`\Processor(_Total)`
every 2 s, `Get-Process` CPU deltas, `/proc/stat` inside WSL), never Task Manager. PB2880's hypothesis was refuted: during
the parallel Linux legs the VM was 97-99 % busy with 0 % iowait and no blocked processes, so the legs take about twice
as long as alone because they share 32 CPUs. The waste was in the serial steps around the legs, and two are gone:

- The Linux gate's 34.4-s prelude: Linux git ran `git status` on the `/mnt` tree, re-hashed all 16,278 files across 9P
  because the index's stat data came from Windows git, and WROTE the worktree's index (a sibling defect). Every read of
  the tree now goes through `linux-gate.sh#wgit`, the tree's own git (`git.exe` for a Windows-drive tree), with
  `--no-optional-locks`; start to build fell from ~44 s to ~10 s. `LinuxGateDriftTests` forbids a direct `git -C`
  (mutation-tested).
- The Windows lander gate's 42-s audit phase, serial before the build: the audits and the post-build self-tests now
  run beside the build and legs through one mechanism, `run_gate_legs.py#Gate._beside`, joined before the population
  check and on every early end (a raising piece is a named RED); the old private pool is deleted. Implementer audits
  stay ahead of the slot (fail fast, PB2523).

Linux gate 239 to ~215 s; lander gate 247.6 to 208.3 s. Behaviour unchanged: exact populations (Conformance 11,325,
Unit 33,042, Characterization 36; NIST 362 MATCH + 2 TERMINATES). `DESIGN-test-build-ci.md` §3.14.3 and §3.15 are
current (§3.15 was stale since PB2879, fixed under I9). The four valleys measured but not removed are filed:
PB2881 (the Conformance leg is latency-bound, not thread-bound), PB2882 (self-test long poles in the implementer's
audit phase), PB2883 (the Linux unit leg's unbalanced NIST partition, 162 s against 54), PB2884 (the build's
10-80 % CPU). PB2880 landed.

Oracle (step 3c): `compare_oracle.py` on f3387ba8d reported DIFFERENT, 6 of 7,695 cases, every one ADDED and none
CHANGED: the new conformance cases of PB2739 (3), PB2844 (2) and PB2757 (1), which train 1049 landed without
re-recording the baseline (step 3c skipped in an operator hand-landing). This train's change compiles nothing, so none
is its own; the baseline is re-recorded (e345db425878 to f3387ba8d9b5), and PB2885 is filed: nothing mechanical
refuses a landing whose oracle baseline is behind the tree.

