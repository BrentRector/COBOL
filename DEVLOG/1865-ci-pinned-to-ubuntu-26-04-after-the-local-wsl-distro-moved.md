## Entry 1865 — 2026-10-04 12:10 PDT — CI pinned to `ubuntu-26.04` after the local WSL distro moved to 26.04; PB1957 filed; ledger refreshed (GAP 336)

The owner asked for CI and the local gates to be effectively the same build, so a defect cannot exist only in CI. Two trains
had each lost a cluster to a CI-only red (train 1007: ANTLR read the grammar as Cp1252 on the Windows runner; train 1013: the
`guard` job's NIST loop runs nowhere locally). A read-only survey (56 rows) is now `kb/Work/PB1957.md`, with the decision
for each row (make it identical, reproduce it locally, or record it as irreducible).

The first row is closed here. The local WSL distro `Ubuntu` was upgraded in place from 24.04.5 to 26.04.1 by the owner (WSL
itself to 3.0.1, kernel 6.18.40; `cobc` is now the packaged GnuCOBOL 3.2.0, because the pinned 3.2.0 source does not build
with GCC 15). Train 1014's Linux gate ran on the upgraded distro, GREEN on all three legs (unit 30,590, characterization 35,
conformance 10,384). Every `runs-on: ubuntu-latest` in `build-and-test.yml` (nine jobs) is now `ubuntu-26.04`, so the
runner image moves only when the repository says so; GitHub's own migration of `ubuntu-latest` to 26.04 starts 2026-10-19.
The `windows-latest` jobs are untouched (a pin is PB1957 row work). The current-state references in
`.claude/skills/gate/SKILL.md`, `scripts/push-main.sh` and `DESIGN-test-build-ci.md` follow. The ledger was refreshed after
train 1014 (117 trend points, artifact version 90).

**Files:** `.github/workflows/build-and-test.yml`, `.claude/skills/gate/SKILL.md`, `scripts/push-main.sh`,
`docs/rearchitecture/DESIGN-test-build-ci.md`, `kb/Work/PB1957.md`, `docs/rearchitecture/evidence/ledger-trend.json`,
`docs/rearchitecture/evidence/ledger-in-flight.md`. No diagnostic code used.

