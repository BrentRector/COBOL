# {PB} — implementer report (≤ 60 lines; the lander reads every line, so nothing decorative)

**Status:** DONE | SPLIT | PAUSED · **turns:** N of cap · **worktree:** `<path>` · **branch:** `<branch>` · **base:** `<sha>` · **HEAD:** `<sha>`
**diffstat:** `git diff --stat <base>..HEAD` last line · **codes used:** COBOLNET#### (unused ones returned: ####)

## Reproduced?
Two to four lines: the probe program(s) by path, the BEFORE output, the AFTER output, on this worktree's own build.
State plainly anything in the note that did NOT hold on today's tree.

## Rule
The clause(s) and rule numbers, each with its `cite.py --check` line (`OK §x.y.z n)`), one per line. A determination the
owner may overturn is marked ⚠ DETERMINATION with the reading chosen and the reading rejected.

## Fix shape
Three to six lines: the ONE place the rule now lives, what was deleted, which sibling arms were fixed (answer "which arm
did you fix?" explicitly), and any drift test that pins the shape. Name files only where the lander must look.

## For the next implementer (⛔ required — it becomes `orient.py`'s LEARNED section)
One line per source file you changed: `path#Member` — the entry point that now owns the rule, the helper to REUSE
(not copy), and the trap that cost you turns. The same lines go into each note's `## Landing` section, because
`tools/claude-skills/skills/agent-fleet/references/orient.py` reads landed notes, and this is the only way the next wave starts where you finished.

## Goldens
One line each: path · PROGRAM-ID · editions it runs at · how the expected value was derived (rule, not measurement).
Negatives: path · `reject-at:` editions · code. (One positive at the introducing edition + one negative below it is the
default; a copy per edition only where the rule's behaviour differs by edition.)

## Rows
`record_verdicts` verbatim summary lines (records / rows changed / verdicts / GAP a → b) and the batch path.
Notes flipped (id → status). Rows deliberately NOT closed, each with the residual and its owner.

## Gate
The gate's run directory, its three `=== POPULATION` lines and the `=== BUILD-LOCAL GATE: … ===` line verbatim; each
red on the way named and attributed in one line.

## Self-review
One line per lens — architecture · full code · performance · duplication — naming what was found and fixed, or "nothing".

## Overlap for the lander
Files a sibling worktree also edits, and the keep-both rule for each.

## New defects (note-ready; ⛔ no kb/Work note created — ids are central)
One paragraph each: title · harm flags · repro · rule + cite line · code site. A lead that shares a root cause with
another lead here or in the register says so, so the registrar can file ONE note.

## Draft DEVLOG paragraph
One paragraph, numbered and inserted by the lander.
