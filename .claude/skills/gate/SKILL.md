---
name: gate
description: Use before every commit and before every merge to choose and run the correct test gate - wave-local filtered (~2 min) per commit versus the comprehensive battery per accumulated batch - and to read the verdict without producing a false green.
---

> ⛔ **BASE SKILL FIRST.** Invoke `brent-tools:test-gate` (Skill tool) before reading on. If the plugin is not loaded
> (a cloud session receives no project marketplace), Read `tools/claude-skills/skills/test-gate/SKILL.md` instead
> (`git submodule update --init tools/claude-skills` if the path is missing). THEN apply this overlay: it carries only
> what is specific to WiseOwl COBOL — commands, paths, CI and push-main, the owner's dated decisions — and wins on
> conflict. Pinned: **brent-tools 1.9.0** (`tools/claude-skills`, kb/Work/PB1699).

# Gate

**Self-check first: is this a single wave/commit, or the batch's pre-merge?** Single wave means FILTERED. The owner
has corrected over-gating repeatedly; the full suite per fix is the bottleneck this model exists to remove.

## Always first

```
dotnet build CobolSharp.sln -c Debug
```

Build the **solution**, not one project. Building only the compiler project and then running `dotnet test
--no-build` tests whatever compiler was copied into the test bin at its LAST full build — a stale compiler. Local
goes green while CI fails.

## Per commit — wave-local (~2 min)

⭐ **Implementer variant (owner lever 2, 2026-09-13):** an implementer in its own worktree runs ONLY its own test classes plus `~Drift|~EditionGate` and the unfiltered Unit assembly; the corpus (`~CorpusRunner`) and NIST (`~Nist`) legs run once at the lander over the whole train (⛔ always in the landing union), then CI, then one battery per train. Exception: a change to the parser, preprocessor or NIST harness runs `~Nist` at the implementer too.

1. `dotnet test tests/Cobol.Net.Tests.Characterization` (full — it is seconds)
2. `dotnet test tests/Cobol.Net.Tests.Conformance --filter "FullyQualifiedName~<Area or the fix's own test class>"`
   - add `--filter "FullyQualifiedName~VersionMatrix"` for an edition gate
3. `dotnet test tests/Cobol.Net.Tests.Unit --filter "<the wave's tests>"`
4. A `cobol` CLI compile-and-run probe

**Do NOT run per commit:** the full Conformance suite, the full Unit suite, or the serial `scripts/guard.sh`.

## Per accumulated batch / pre-merge — comprehensive

- Full greenfield Conformance + full characterization
- The GnuCOBOL external differential, before AND after, diffing PER-CASE verdicts
- `scripts/guard-fast.sh` (parallel) when a legacy-shared seam was touched — never the serial `guard.sh`

- **CI's own Linux leg — comprehensive is not comprehensive without it.** Everything above runs on ONE host
  (this Windows machine, Debug). The workflow's `ubuntu-latest` jobs — `guard-fast.sh`'s NIST loop, the Linux
  conformance shards, Linux greenfield unit + characterization — and the Release build are verified by CI and by
  nothing else, so a test keyed on host ACL or file-lock semantics can be green here and red there. Read the
  verdict for the commit you pushed:
  `gh run list --branch main --commit <sha> --json databaseId,status,conclusion,displayTitle`, then
  `gh run view <id> --log-failed` on a red. `scripts/session-probe.ps1` prints the latest run's conclusion at
  session start, so a red main is never inferred from silence (`kb/Work/PB796`).
- ⛔ **AND IT IS NO LONGER OPTIONAL TO READ IT: `ci-gate` IS A REQUIRED STATUS CHECK ON `main`.** `ci-gate` is the
  workflow's terminal job — `if: always()`, `needs:` every other job, PASS only when each is `success` or
  `skipped` — and it is the ONE stable check name the protection can require (every matrix job's check name
  carries its shard AND its filter text, and every one of them is skipped on a docs-only push). Protection is
  `strict: false`, `enforce_admins: true`, force-pushes and deletions off. So the landing gate is a SCRIPT, not a
  habit: **`bash scripts/push-main.sh`** pushes HEAD to `ci/<short-sha>`, waits for `ci-gate` on that sha, and only
  then fast-forwards main — a bare `git push origin HEAD:main` of an unverified commit is refused by the server
  (owner decision 2026-09-06, question 23; `kb/Work/PB796`). It is idempotent: re-run it after a timeout.
- ⚠ **A docs-only push runs `changes` + `ci-gate` and nothing else** — seconds, not a matrix. `paths-ignore` was
  removed because a workflow that declines to START leaves a required per-commit check unsatisfiable; the same
  path list now lives in the `changes` job, one layer down, where it decides whether the MATRIX runs.

`guard-fast.sh` is **not** CI-complete: CI builds Release, local builds Debug. Any change whose semantics differ by
build configuration gets a local `-c Release` leg before push. Better: do not write configuration-divergent
compiler behavior.

SERIAL does not mean comprehensive-per-change. The grammar batch is serial (one shared parser, so implement
constructs one after another) but still gets ONE comprehensive gate for the whole batch.

## Reading the verdict — where false greens come from

The base's "Read the verdict line", "Flakes and attribution" and "A filter that matches nothing" sections apply as
written (full output to a file, no source edits while a gate runs, no flake without the test's name and an isolated
re-run). Here, additionally:

- **Never chain ANYTHING after a verdict command** (build, test, `build-local`, `push-main.sh`, battery) with `&&`,
  `||` or `;` — MANDATORY-PRACTICES P14, and the guard hook (`scripts/hooks/forbidden_commands.py` rule 5) BLOCKS
  it. To capture the status in the same call, append `; echo "EXIT=$?"`; read-only commands may follow that.
- `scripts/build-local.{ps1,sh}` normalizes a bare filter (`~X|~Y` → `FullyQualifiedName~X|…`) and prints the
  `=== WAVE-LOCAL GATE: ` verdict line — block on that line, never on the exit code.

## Read the failure before diagnosing it

Stack trace = emitter or runtime bug. `error CS####` = generated-code problem. A COBOLNET diagnostic = front-end
reject. Timeout = infinite loop. These are different problems; check which one you have first.
