## PB3029: one line ending, LF, in every tracked text file and every checkout; PB3097: the finisher role loads again

### PB3029: LF everywhere, enforced

The owner, 2026-10-11 ~12:35 PDT: "We should probably standardize on a single line-ending format. CRLF vs others keeps
biting us." The object database was already LF for 15,234 files. Under `core.autocrlf=true`, though, every Windows
checkout was CRLF, so a Windows gate and a Linux gate read different bytes from the same commit.

- `.gitattributes` now opens with `* text=auto eol=lf`. The narrower `*.sh` and Workflow `.js` rules it subsumes are
  gone. Files that must keep their bytes are declared `-text` with their reasons: the pinned Unicode/CLDR inputs, and the
  pb1496 lone-CR fixture.
- `scripts/line_endings.py --check` is the one check. It has three arms. INDEX and WORKING TREE refuse a CR in any
  tracked text file. WRITERS refuses a Python text-mode write that does not state `newline=`. It runs as a gate audit,
  in battery phase -1, in CI's `audits` job and as a `windows-build-test` step. An empty population is RED.
  `--fix` rewrites a pre-rule checkout. `--fix-writers` is the codemod: it inserts after the last code token, so a
  comment never receives the keyword (review R1-1), and it re-scans its own result.
- One normalization made every blob LF: 8 files, 672 lines. Seven of them git had stored as binary because a stray CR
  rode in with their CRLF.
- 245 Python writers in 90 files, plus the 44 that main has added since, state `newline="\n"`. The PowerShell generators
  write through `scripts/LfText.psm1` `Set-LfContent`. Eight dead `.Replace("\r\n", "\n")` calls and the comments that
  claimed a CRLF checkout are gone.

### The finisher: the merge, and `--fix` leaving a dirty index

`enqueue.py land` first stopped on nine conflicts against main. Main had deleted `landing_lease.py`, `train_apply.py` and
`train_measure.py` (PB3024), and the finisher took those deletions. The DOC_INDEX and DESIGN §3.17 conflicts were
resolved against both sides: main's merge-queue and DEVLOG text, plus this branch's line-endings paragraph. For the four
Python conflicts the finisher took main's text and re-ran `--fix-writers`, so no hunk was merged by hand.

**Defect in the tool, fixed at its root.** `--fix` rewrote a CRLF working tree to LF but left the index's cached stat
data, which still held the CRLF sizes. Git's `ie_modified` treats a size mismatch on a nonzero recorded size as a
change without comparing content. As a result, `git status --porcelain` listed 15,179 files ` M` while `git diff` was
empty, and enqueue.py's dirty-tree check refused the landing. `refresh_index` now re-states every entry whose cleaned
content (`git hash-object --stdin-paths`) is already the staged blob. A real edit stays modified and unstaged. This
also covers a generator that writes LF over a CRLF checkout (`LfText.psm1`). The self-test pins it: without the re-stat,
the rewritten file is listed and two checks fail.

### PB3097: `cobol-finisher` never loaded

The unquoted `description:` in `.claude/agents/cobol-finisher.md` held `DONE: its commits`, which is invalid YAML.
Claude Code never loaded the role, so `Agent(subagent_type: "cobol-finisher")` answered "not found", and every enqueue
unit's finisher dispatch failed. `tooling_check.py` printed `OK` because it only tested that the file existed. The
description is now valid YAML. The readiness check now parses every agent, skill and command frontmatter with PyYAML.
It requires a mapping that names its own file or directory and has a non-empty description, and it reports a failure
as ASK-OWNER, never OK. Its self-test plants the exact defect and also runs over the live `.claude/`. PyYAML is pinned
in CI's `audits` and `windows-build-test` jobs and listed in the WSL and cloud setups.
