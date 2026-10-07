DISPATCH SPEC — wave {wave} ({letter}), {group}: {notes}
Brief: E:\COBOL\.claude\skills\workstream\templates\fix-lane-implementer-brief.md — read it WHOLE first; {{PB}} = {lead}.
Base: your worktree is cut from current main ({base}). CLAUDE.md rule 1 carries the ISO → GnuCOBOL → IBM/Micro Focus
precedence for implementation options; obey it.

Codes allocated: {codes}, in order; list the ones you use and RETURN the ones you do not.
Report: {S}\reports\{slug}-{lead}-report.md   (wave-prefixed, so a later agent never overwrites an earlier report)
Scratch:  {S}\{slug}\
Report your ACTUAL branch (`git branch --show-current`).
{pred}
THE GROUP IS ONE ROOT: {root}. Read every note first — {files} — they carry the forensic detail, the citations, the
probes and the CODE SITES. ⛔ ORIENT IN ONE CALL, BEFORE READING ANY SOURCE FILE: `python
tools/claude-skills/skills/agent-fleet/references/orient.py <every source file your notes name>` (a fresh worktree
has no submodules: if that path is missing, run `git submodule update --init tools/claude-skills` once; the project
settings come from `.agent-fleet.json`, so pass no flags) prints each file's outline with line numbers, the clauses it cites, the tests
that exercise it, what EARLIER implementers learned about it (the landed notes' code sites and mechanisms) and its
recent commits. Then read only the line ranges it points you to, and use `python scripts/spec/where.py <clause>`
for a rule no note locates. Do not re-survey the codebase: orientation (grep/read) was 46 % of every implementer's
tokens in waves 45–57, re-deriving what earlier waves had already learned.
Do not re-derive what the notes measured, but DO re-run every probe on your own build.
⛔ DRIFT RULES (MANDATORY-PRACTICES P11): before editing any file run `python scripts/spec/drift_rules.py <files>` and
honor every SPECIFIC rule it prints — the drift tests will enforce them at your gate anyway.
C# NAVIGATION (P13): find definitions, references and callers with the LSP tool (goToDefinition / findReferences / incomingCalls on the .cs file) before grepping; grep stays right for text, COBOL, docs and generated files.
LSP DIAGNOSTICS (P13, owner 2026-10-01): act on every diagnostic the language server reports for a file you edited (unnecessary using, unused variable, analyzer hints); fix it in the same change before you commit, or name in the report why you did not.

{body}

⛔ GRACEFUL STOP: before EACH new step check for TWO files, the owner's global stop {global_stop} and this fleet's own
stop {stop_file}; if EITHER exists, checkpoint-commit, write STATUS.md NEXT and your report, and return status SPLIT.
Never start a build or gate once one exists. No other STOP file is yours (another session's fleet stop is not).

⛔ HOW TO WAIT FOR A LONG JOB (measured 2026-09-22, wave 45): a workflow agent that ENDS ITS TURN while a background
build/gate is running is RETURNED BY THE HARNESS AND ITS BACKGROUND PROCESS IS KILLED. So:
1. Start the gate in the background, logging to a file:
     PowerShell (run_in_background): pwsh -NoProfile -File scripts/build-local.ps1 -Mode implementer -Priority BelowNormal *> <log>
2. Then BLOCK in the foreground until the verdict line appears, in chunks under the 10-minute tool limit:
     Bash (timeout 590000): timeout 580 bash -c 'until grep -qE "=== BUILD-LOCAL GATE: " "<log>"; do sleep 5; done' ; tail -3 "<log>"
   If it times out with no verdict, issue the SAME command again. Do not use `sleep`; do not end your turn.
3. Only after the verdict line is in hand: record it, checkpoint, write the report, return the structured result.

⚠ SIZING: if the group proves to be two mechanisms, finish the first at its root, checkpoint at the note boundary, and
return SPLIT rather than pass the 220-turn cap.

⛔ RE-PROBE FIRST, EVERY NOTE, on YOUR build (`dotnet build Cobol.Net.sln -c Debug`, expect 0/0). A note that no longer
reproduces is re-verdicted and reported DISCHARGED with the evidence — a real outcome, not a failure.

⛔ CITATIONS: `python scripts/spec/cite.py --check <clause> "<text>"` for EVERY § you write into code, a golden or the
report — including the ones the notes carry. A citation you did not --check is not a citation.

⛔ PUBLIC SKILLS (MANDATORY-PRACTICES P10): before work read E:\claude-skills\skills\engineering-standards\SKILL.md,
dotnet-engineering\SKILL.md and spec-oracle\SKILL.md; for the sibling sweep apply variant-analysis\SKILL.md (+ roslyn-analysis
for compiler-fact queries); before the report SELF-REVIEW your diff against E:\claude-skills\agents\pr-test-analyzer.md,
silent-failure-hunter.md and comment-analyzer.md (+ type-design-analyzer.md if you add/reshape a type). Project rules win on conflict.

⛔ SIBLING SWEEP (CLAUDE.md rule 4): every bug is a pattern. Which ARM of the dispatch did you fix, and where is the other?

GATE — ORDER, DON'T SKIP (owner 2026-09-28; kb/Work PB1708, PB1721): `pwsh scripts/build-local.ps1 -Mode implementer
-Priority BelowNormal` — nothing to derive, pass or trim. It takes a gate slot (`gate-slot: waiting, k ahead` is the cap
working: keep blocking), builds, and runs EVERY discovered case of Conformance, Unit and Characterization in two legs —
your added tests, your previous gate's reds and the cheapest cases your change reaches first — FAIL-FAST: a red in leg 1
stops it `RED/INCOMPLETE` with the remainder named; fix it and re-gate. Done only on `=== BUILD-LOCAL GATE: GREEN — …`,
printed only when every leg ran and every population is exact. ALWAYS `-Priority BelowNormal`; one gate per worktree.
⛔ `python scripts/semgrep/verify.py` must not increase any rule's count (train 57 dropped a cluster for +20 BigInteger).
Print a real verdict line; never leave a placeholder in the report.

GOLDENS: one positive at the introducing edition + one negative below it, a copy per edition only where behaviour differs.
Every golden/negative you ADD runs in leg 1 of your gate — quote its `UnitTestResult` from `TestResults/build-local/<run>/leg-1-Conformance.trx` (MANDATORY-PRACTICES I7).
LINUX GATE (I8, PB1732): after the Windows gate is green and COMMITTED, run `wsl -d Ubuntu --cd <your worktree> -- bash -lc 'bash scripts/linux-gate.sh --nice'` through the PowerShell tool (all four of CI's Linux legs, ~6 min). Quote its `=== LINUX GATE:` line in your report.
Parser + emitter + golden + manifest entry in ONE commit.

REGISTER: flip each note's `status` and write its `closes_rows` IN THE COMMIT THAT LANDS IT (with `closes_rows_reason:`
when it closes none). Run `python scripts/spec/work.py check`. Do NOT open a list anywhere. FIX WHAT YOU FIND (I9, owner 2026-09-30): an issue you find in a file you have already read and understood is FIXED in this same change, with its golden and its sibling sweep, unless the effort is so excessive that it needs a redesign beyond your slice or would blow your turn budget. Only then file a kb/Work note (id from the orchestrator's allocation) carrying its repro path and code site (file:line) and why it was too large to fix now, and list it in the report. A found issue left only in prose or the DEVLOG is a defect in your work.

CHECKPOINT: `git commit -m "WIP checkpoint: …"` after every mechanism and every gate, and after EVERY commit (the small
trailing ones too: a gate-red fix, a regenerated index) rewrite `STATUS.md` (DONE / NEXT / BLOCKED / GATE / batch
paths / codes used) with FIRST LINE `STATUS-AT: <output of git rev-parse HEAD>` — the commit it describes (P4;
STATUS.md is gitignored, so writing it never moves HEAD). A hook refuses your next commit and your finish while the
stamp is not HEAD (kb/Work/PB1701). RESUMING or MERGING a predecessor:
run `python tools/claude-skills/skills/agent-fleet/references/status_delta.py <that worktree>` FIRST and read the summary plus ONLY the commits it lists.
Turn cap 220 — at the cap, checkpoint, write NEXT, return SPLIT.

Report ≤ 60 lines, following `templates/implementer-report-template.md`, one section per note.
