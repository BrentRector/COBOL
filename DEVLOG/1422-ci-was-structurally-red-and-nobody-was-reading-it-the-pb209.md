## Entry 1422 — 2026-08-31 17:32 PDT — CI was structurally red and nobody was reading it: the PB209 gate met the GPL never-commit doctrine, and the fetch script's own contract is the fix

Every `Build and Test` run since `66615f70` failed — both platforms, the same two tests — and five landings
pushed past it because the local battery is not a superset of CI (§0's own caution, now demonstrated). The
failing tests were PB209's `ExternalCorpusPopulationDriftTests`: built on the never-skip rule (a population the
gate cannot see must FAIL — a silent zero is the very defect PB209 registered), they collided with the
licensing doctrine that the GnuCOBOL corpus is GPL and NEVER committed — so every CI checkout lacked the
corpus by policy and the gate could never pass there. Both rules are right; the synthesis was already written
down in `scripts/fetch-gnucobol-tests.ps1`: fetch ON DEMAND. CI now does exactly that — an actions/cache step
for the pinned tarball plus the fetch script (sha256-verified, tests-tree-only extraction) before the unit
step in both failing jobs. Cold-cache-and-mirror-down fails at the FETCH step: an honest infrastructure red,
never a green that looked at nothing. The script's stale pre-PB209 contract sentence ("the adapter SKIPS")
is corrected to name both postures. Registered and landed as kb/Work PB277, which also records the process
lesson: a new gate with an environment precondition must state where the precondition HOLDS — the
green-earned rule has a platform axis.

