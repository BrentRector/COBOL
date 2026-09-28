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

{body}

⛔ GRACEFUL STOP: before EACH new step check for {S}\STOP; if it exists, checkpoint-commit, write STATUS.md NEXT and
your report, and return status SPLIT. Never start a build or gate once STOP exists.

⛔ HOW TO WAIT FOR A LONG JOB (measured 2026-09-22, wave 45): a workflow agent that ENDS ITS TURN while a background
build/gate is running is RETURNED BY THE HARNESS AND ITS BACKGROUND PROCESS IS KILLED. So:
1. Start the gate in the background, logging to a file:
     PowerShell (run_in_background): pwsh -NoProfile -File scripts/build-local.ps1 -Filter "<filter>" -Priority BelowNormal *> <log>
2. Then BLOCK in the foreground until the verdict line appears, in chunks under the 10-minute tool limit:
     Bash (timeout 590000): timeout 580 bash -c 'tail -n +1 -f "<log>" | grep -m1 -E "=== WAVE-LOCAL GATE: "' ; tail -3 "<log>"
   If it times out with no verdict, issue the SAME command again. Do not use `sleep`; do not end your turn.
3. Only after the verdict line is in hand: record it, checkpoint, write the report, return the structured result.

⚠ SIZING: if the group proves to be two mechanisms, finish the first at its root, checkpoint at the note boundary, and
return SPLIT rather than pass the 220-turn cap.

⛔ RE-PROBE FIRST, EVERY NOTE, on YOUR build (`dotnet build CobolSharp.sln -c Debug`, expect 0/0). A note that no longer
reproduces is re-verdicted and reported DISCHARGED with the evidence — a real outcome, not a failure.

⛔ CITATIONS: `python scripts/spec/cite.py --check <clause> "<text>"` for EVERY § you write into code, a golden or the
report — including the ones the notes carry. A citation you did not --check is not a citation.

⛔ PUBLIC SKILLS (MANDATORY-PRACTICES P10): before work read E:\claude-skills\skills\engineering-standards\SKILL.md,
dotnet-engineering\SKILL.md and spec-oracle\SKILL.md; for the sibling sweep apply variant-analysis\SKILL.md (+ roslyn-analysis
for compiler-fact queries); before the report SELF-REVIEW your diff against E:\claude-skills\agents\pr-test-analyzer.md,
silent-failure-hunter.md and comment-analyzer.md (+ type-design-analyzer.md if you add/reshape a type). Project rules win on conflict.

⛔ SIBLING SWEEP (CLAUDE.md rule 4): every bug is a pattern. Which ARM of the dispatch did you fix, and where is the other?

GATE — THE FILTER IS DERIVED, NEVER GUESSED (kb/Work PB1683): `python scripts/spec/impacted_tests.py --base
$(git merge-base HEAD origin/main) --plus "DisplayName~<each golden you added>|FullyQualifiedName~<each new test class>"`
prints it as its LAST stdout line — every Conformance test whose RECORDED execution reaches a file you changed, plus
`~Drift|~EditionGate` — and pass it UNCHANGED to `build-local.ps1 -Filter`, ALWAYS with `-Priority BelowNormal` (Unit and
Characterization run whole). Never trim it; never substitute name-guessed terms; a shared seam selects the corpus and
NIST tests by itself. ⛔ NEVER run the whole Conformance assembly — that is the lander's job — UNLESS impacted_tests.py
prints `FullyQualifiedName~.` (it cannot bound your change: no map for your base, a `.g4`/build file, a new src file…):
then run exactly that and quote its reason in the report.
⛔ `python scripts/semgrep/verify.py` must not increase any rule's count (train 57 dropped a cluster for +20 BigInteger).
Print a real verdict line; never leave a placeholder in the report.

GOLDENS: one positive at the introducing edition + one negative below it, a copy per edition only where behaviour differs.
Every golden/negative you ADD must RUN BY NAME at your gate (`DisplayName~<name>` or the corpus leg) — quote its pass line (MANDATORY-PRACTICES I7).
Parser + emitter + golden + manifest entry in ONE commit.

REGISTER: flip each note's `status` and write its `closes_rows` IN THE COMMIT THAT LANDS IT (with `closes_rows_reason:`
when it closes none). Run `python scripts/spec/work.py check`. Do NOT open a list anywhere — new defects are LEADS in your
report, each with its repro path and code site (file:line) so the registrar does not re-survey.

CHECKPOINT: `git commit -m "WIP checkpoint: …"` after every mechanism and every gate, and after EVERY commit (the small
trailing ones too: a gate-red fix, a regenerated index) rewrite `STATUS.md` (DONE / NEXT / BLOCKED / GATE / batch
paths / codes used) with FIRST LINE `STATUS-AT: <output of git rev-parse HEAD>` — the commit it describes (P4;
STATUS.md is gitignored, so writing it never moves HEAD). A hook refuses your next commit and your finish while the
stamp is not HEAD (kb/Work/PB1701). RESUMING or MERGING a predecessor:
run `python tools/claude-skills/skills/agent-fleet/references/status_delta.py <that worktree>` FIRST and read the summary plus ONLY the commits it lists.
Turn cap 220 — at the cap, checkpoint, write NEXT, return SPLIT.

Report ≤ 60 lines, following `templates/implementer-report-template.md`, one section per note.
