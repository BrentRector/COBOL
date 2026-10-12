## Entry 1779 — 2026-09-29 12:42 PDT — Train 72: the ordered gate lands (M12 PB1719 + M13 PB1721, gate cap N = 1 from PB1720)

Train 72 carried wave 72's group H, which had merged its same-file predecessor group G, so the train lands two
mechanisms of the "order, don't skip" gate (owner, 2026-09-28, kb/Work PB1708) as two commits.

**G — PB1719, M12: the in-assembly leg filter.** `tests/_shared/GateLegs.cs` adds `GateTestFramework`, which
`tests/Directory.Build.props` names in every test project. It is xunit's own framework with one change to its
executor. When the gate driver hands it all three `COBOLNET_GATE_*` variables, and the plan's bytes hash to the digest
it was handed, it runs exactly one leg of the plan in rank order and writes an identity record: the digest, the leg,
the test assembly's MVID and SHA-256, the product assemblies' hashes, and the keys it received and ran. With no
handshake it is xunit exactly, which covers CI, the battery, an IDE and the lander's single leg. A partial or stale
handshake turns every case into an execution error, a `Failed!` run, never a silent whole or partial one. The
orderers are checked to be permutations. The impact-recording build names its own `ImpactTestFramework`, which now
derives from the gate's. G's worktree run also found two defects in the Linux gate. `*.sh` was checked out CRLF, and
the exported `GIT_DIR` let `gate_slot.py`'s self-test write into the real repository. The self-tests now drop git's
`--local-env-vars`, and `.gitattributes` keeps `*.sh` LF. Main had meanwhile replaced the exporting script with the
clone design (Entries 1777 and 1778), so G's `linux-gate.sh` hunk resolved to main's version, as Entry 1778 planned.
DESIGN section 3.15 keeps the clone design and adds G's two guards and the line-ending rule.

**H — PB1721 + PB1720, M13: the ordered gate and its cap.** `scripts/run_gate_legs.py` is now the one gate, and
`build-local -Mode implementer|lander` only sets the priority and calls it. In order it takes the worktree's gate lock
(a second gate is refused, naming the holder), a gate slot (implementer only), and runs the one audit list, the corpus
fetch, the build, a hash of the binaries, and the three listings concurrently. It then builds the plan and runs the
legs with the three assemblies concurrently. An implementer gate has two legs and fails fast: a leg-1 red is
`RED/INCOMPLETE`, and every case left unrun is named in the run directory. A lander gate is one leg with no plan and
no slot. Every gate ends with the population check against `--list-tests`, the identity check, one
`=== BUILD-LOCAL GATE:` line and `verdict.json`. `-Filter`, the filter line and `--plus` were deleted along with every
caller the M13 row named, plus the ones it missed (the lander agent, the golden-lander brief, the report template,
`wf_rolling_wave.js`, two drift tests and the hook messages). Slot holders' children now run with persistent build servers off. PB1720
measured the cap on the shared host with cold gates: one implementer gate costs the lander's Conformance leg 1.24x and
1.30x its quiet time, and two cost 1.52x and 1.58x. Only N = 1 meets the 1.25x line, so `DEFAULT_SLOTS = 1`.
Implementer gates now queue repository-wide at 281–449 s each under load, so a 7-implementer wave can wait more than
30 minutes for a gate. That is the owner's to weigh against the lander's 1.25x.

**The train.** The lander rebased H's branch onto `fcfc593e3`, which had six conflicts. The four practice and brief
conflicts take M13's gate text and main's clone-design Linux-gate line. The design doc and `linux-gate.sh` are
resolved as above. Four places still described the superseded Linux gate: the two self-test comments said
`linux-gate.sh` exports `GIT_DIR`, CLAUDE.md said "about 5 minutes", and practice L10 said "on the Windows-built
binaries". The lander corrected them in the same commits. The gate is the new driver itself, run by the lander:
`=== BUILD-LOCAL GATE: GREEN — Conformance 9,321/9,321 · Unit 29,728/29,728 · Characterization 35/35 cases ran
(skipped 0) in 1 of 1 leg(s) · lander mode ===`, with every audit at zero. The legacy Integration suite passed 503 with 1
skipped. The Linux gate printed `=== LINUX GATE: GREEN (legs unit characterization conformance) ===`. Semgrep passed,
with `raw-diagnostic-code-literal` 301 → 300 and every other rule unchanged. The review pass (step 5b) found nothing,
and no cluster was dropped. GAP 866 → 866: this is process tooling, with no inventory rows. The implementer's leads are
filed. PB1729 is extended: transient `Access … is denied` reads under concurrent gates hit two more drift tests and
MSBuild's read of the shared NuGet cache, so the cause is host load, not a reset. The new notes are PB1733 (a corpus
`must run:` red with an empty detail), PB1734 (the public test-gate skill predates M13), PB1735 (`registrar-brief.md`
still gates with a raw filtered `dotnet test`) and PB1736 (the guard hook's VERDICT regex matches `build-local.*`
anywhere in a segment; this lander tripped it twice on read-only commands).

