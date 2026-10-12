## Entry 1031 — 2026-07-26 21:38 PDT — First repairs land: 4 deterministic findings, a class swept to zero, and my own commit hook caught by its own path bug

Repairs begin, from the least-ambiguous end: findings with an exact line, an exact replacement, and a check that
can fail.

**Numeric literals (2, page 933, §15.73.3 FUNCTION PI).** Both PI constants carried a space injected between two
digits, and both breaks landed after the **29th fractional digit** — the signature of a fixed-width column split in
the extraction pass, not two independent typos. A whitespace-tokenising consumer truncates at the space and
silently gets a value wrong from the 30th decimal onward, and rule 3 states an EXACT required return value, so the
text is value-bearing rather than decorative.

The verifier had demanded the CLASS be swept rather than the two lines patched. Done: a regex for any long digit
run broken by a space found exactly these two in the whole file, and zero remain. Post-repair the literals carry 31
and 33 fractional digits, matching the verifier's pixel measurement of the printed page.

**Table 12 cell separators (2, pages 563 and 565).** The DELETE and WRITE rows flattened two ALTERNATIVE
conditional phrases onto one line, so they read as a single compound phrase. The verifier had already established
the printed break is semantic rather than word-wrap by measuring cell width against text width — the PDF breaks
~125 px early, at the phrase boundary. The three sibling rows had kept their separator, so the table contradicted
itself; all five multi-entry cells now agree.

**Three gates, each able to fail, run after the batch:** `--check` exit 0 · anchors still **1261** · catalog still
**3,790**. The stable catalog count is the interesting one — it is what proves these repairs changed RENDERING and
not rule structure. A repair that moved the denominator would mean I had altered what the standard says.

**⛔ My own hook blocked the commit, for the right reason and the wrong cause.** The PreToolUse DEVLOG guard fired
with `can't open file 'E:\CobolSharp\specs\scripts\hooks\devlog_staged.py'`. Hook commands run with the
SHELL's cwd, not the project root, and committing a submodule requires `cd specs` — so the relative path
`python scripts/hooks/...` resolved inside the submodule and hard-failed the tool call. Every one of the four hooks
had it. Now absolute, and verified by running the guard from inside `specs/` where it had failed.

Two lessons, both familiar by now. A guard that fails OPEN is useless, but one that fails CLOSED on its own bug
blocks legitimate work — and I had tested these hooks only from the project root, which is the one cwd where the
bug is invisible. And the hook was still right on the substance: I was about to commit without a DEVLOG entry.

**206 findings remain.** Next deterministic group is heading depth (19 findings), where the rule is mechanical —
heading level must equal dotted-number depth — and 18 of the same class were already swept in clause 13 with no
fallout.

