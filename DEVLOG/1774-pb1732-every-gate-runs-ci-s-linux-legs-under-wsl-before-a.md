## Entry 1774 — 2026-09-29 00:50 PDT — PB1732: every gate runs CI's Linux legs under WSL before a push (scripts/linux-gate.sh)

**Why.** Train 71b's first push went red in CI's Linux unit job on a Windows path literal in group F's new test
(`GateLegDriftTests.Arm5` planted `E:\COBOL-wt\battery87`, which `Path.GetFullPath` treats as relative on Linux).
Every Windows gate had it green. It cost a ~30-minute CI round trip and F (M12), with M13 behind it, to the next
session.
- No gate before CI had ever run on Linux.
- The WSL-repro practice existed only in the orchestrator's memory, so no brief carried it.
- The owner asked why we didn't test under WSL first, then: "Build it now so it's not lost".

**`scripts/linux-gate.sh`** runs CI's Linux test legs from any tree:
`wsl -d Ubuntu --cd <tree> -- bash -lc 'bash scripts/linux-gate.sh'`.
- **`unit` and `characterization`** run `--no-build` on the Windows-built binaries.
  - On F's branch this reproduced CI exactly: one red, Arm5, in 143 s.
  - Two WSL-only false reds had to be removed first. A worktree's `.git` names an `E:/` gitdir Linux git cannot
    open, and /mnt trees trip git's ownership check. The script exports a translated `GIT_DIR`, `safe.directory`
    and `core.autocrlf` (Git for Windows keeps that in its SYSTEM config, so without it a clean tree read as 9,543
    modified files).
- **`conformance`** cannot reuse the Windows binaries. The tests find their goldens through `[CallerFilePath]`, which a
  Windows build bakes in as `E:\…` paths: 363 false reds. So it builds committed HEAD on Linux in a `git archive`
  snapshot under `~/linux-gate/<tree>`. `pwsh` and Java are now in WSL, so the ANTLR pre-build runs; the build takes
  ~15 s.
- **Measured on main at 9a0ab2dd4:** all three legs GREEN in 244 s wall.
  - Unit 29,746/29,746.
  - Characterization 33/33.
  - Conformance 9,319/9,319, in 91 s after the Linux build.
- **The gate's own first real finding was its own script.** `GateLegDriftTests.Arm6` (M14, landed tonight) requires
  every `dotnet test` caller to run scrubbed of gate-handshake variables. `linux-gate.sh` now runs each leg through
  `test_population.py scrubbed dotnet test`, and the caller audit lists it as a 13th site.

**A detector, built and deleted.** The owner suggested identifying platform-dependent code and testing only that on
both platforms. `scripts/platform_sensitive.py` (ten patterns, self-tested) did flag F's exact failing line and
passed B's branch. But at ~4 minutes for the whole Linux population no selection is worth its misses (PB1708, "order,
don't skip"), and a detector with no job is a second mechanism. It was deleted before commit, and every gate runs
every leg.

**Wiring:**
- MANDATORY-PRACTICES **I8** (implementers: all three legs after a green, committed Windows gate) and **L10**
  (landers: all three before `push-main`; a red drops its cluster).
- `fix-lane-implementer-brief.md` 7b, `dispatch-spec-implementer.md`, `lander-train-brief.md` 3b and
  `lander-brief.md` 3b; `check_practices.py` refuses a brief without `linux-gate.sh`.
- **`LinuxGateDriftTests`**: every test project a Linux job of `build-and-test.yml` runs is a leg of the script. Seen
  RED on a planted missing leg.
- Docs: DESIGN-test-build-ci.md section 3.15 (and the 3.14.6 cap note), DOC_INDEX, DRIFT_RULES (regenerated), and
  CLAUDE.md "Testing".
- The public agent-fleet skill gets the general practice (claude-skills v1.13.2, pending the owner's push).

**Gates.** Windows Unit 29,746/29,746. Linux gate GREEN on main. `check_practices` GREEN; `work.py check` passes;
citation audit 0.

