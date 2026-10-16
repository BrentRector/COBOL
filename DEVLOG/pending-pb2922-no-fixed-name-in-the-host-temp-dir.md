## A dry run's steer lives under its own coordination directory, and no script puts a fixed name in the host's temp directory (PB2922)

At 03:38 PDT on 2026-10-16 ten landings started in the same second, and four of them went GATE RED in the SELF-TESTS
audit, before any slot or test leg, on one check: `test_orchestrate_steer.ps1` "a dry run leaves no steer behind" got
`'False 1'` (one got `'False 2'`). The check counted every `orchestrate-dryrun-*.handoff.operator.json` in
`[IO.Path]::GetTempPath()`, and `orchestrate.ps1 -DryRun -Unit` wrote its steer there under its own pid. So any other
dry run on the host was counted (the four gates' self-tests ran each other's), and kb/Work PB2922's second arm was
worse: the file was removed only in the supervisor's `finally`, which a process-tree kill skips, so one leaked file
would turn every later gate on the host RED until someone deleted it by hand. kb/Work PB2590 had once filed this
symptom under load. It was never load: it was a fixed, shared path.

The fix removes the shared path. A dry run's steer is written to `<CoordDir>\scratch\dryrun-<pid>\handoff.operator.json`
and removed with its directory when the run ends. Every supervisor start, holding the coordination directory's lock and
so sure no other supervisor of it is alive, reaps every other `dryrun-*` directory with a log line. The self-test's dry
runs already had a private coordination directory each (`testdata/orchestrate_test_lib.ps1`), so the check now counts
`dryrun-*` there and nowhere else. A new case plants the directory of a pid that has exited and expects it reaped,
said, and its steer never read. With the reap disabled it went RED (`got 'False True 1'`); with it the part is 23/23.
Nothing in this change ever wrote an `orchestrate-dryrun-*` file to TEMP, because the landing lane on this host was
still running the old check.

Every bug is a pattern, so the sweep took the mechanism, a fixed name joined to the host-wide temp directory, across
`scripts/` and `.claude/` in all three languages. `semgrep/verify.py` wrote its paths-stripped config to one fixed
TEMP file (now a `TemporaryDirectory`). `arch/r2_inputs.py`'s self-test used a fixed TEMP name as a fixture root it
never writes (now an anchored path). `guard-fast.sh` and `guard-verify.sh` put a dozen fixed `gf_*`/`gv_*` files
straight into `${TMPDIR:-/tmp}`: the Linux gate already gives the guard a private TMPDIR, but a hand run did not. Each
now makes its own `mktemp -d`; guard-fast keeps it only when it holds a non-MATCH's forensics, and guard-verify only when
its proof is red. `guard-verdict.sh`'s standalone forensics default now carries `$$`, as `guard.sh`'s does.
`measure-battery-determinism.sh`'s default output directory is a `mktemp`. The cloud and WSL setup scripts' installer
downloads and logs go to a `mktemp` directory removed on exit.

The drift test makes the next case automatic: `scripts/temp_names.py --self-test`, which `self_tests.py` discovers, so it
runs in every gate's SELF-TESTS audit and in CI. It catches 13 planted shapes (the ones that reached the tree), passes
9 run-unique ones, and then scans every tracked `.py`, `.ps1`, `.psm1` and `.sh` under `scripts/` and `.claude/`. A
finding is the temp root (`GetTempPath()`, `$env:TEMP`/`$env:TMP`, `tempfile.gettempdir()`, `/tmp`, `$TMPDIR`)
joined to a literal with no unique part, or assigned bare to a variable, which would hide every later join. Python is
read from its syntax tree, PowerShell and bash by line. Before the sweep it found 22 sites on this tree; afterwards it
finds none. `semgrep/verify.py` passes with every rule's count unchanged. The design doc (DESIGN-orchestrator-loop.md
section 4.8 and its coordination-directory table) and DOC_INDEX describe the new shape. PB2922 is landed with
`closes_rows: []` (process tooling, no ISO rule). PB2590's text now credits the steer arm to PB2922 and records that
nothing of its load arm is still open.
