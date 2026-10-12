## Entry 1153 — 2026-08-03 15:07 PDT — §5b step 3: the batch key already generalised; the real gate was per-agent SIZE — and a Linux CI red of my own making

Step 3 read: "`phase_b_batch.py` fans out ONE AGENT PER FUNCTION, which works because §15 is a list of
functions. §14 is STATEMENTS and §13 is CLAUSES; a per-function batch has no meaning there. Before the first
§14 batch, `phase_b_batch.py` needs a per-STATEMENT subject key." **Measured, that premise is wrong — the third
§5b premise in a row to fall to its own measurement, which is now worth noticing as a pattern rather than an
anecdote.**

**THE TOOL NEVER HAD A PER-FUNCTION KEY.** It groups by the catalog's `subject`, and that field is already the
statement name in §14 (`SET statement`, `READ statement` — 54 subjects over 1,141 rules) and the clause name in
§13 (`PICTURE clause`, `VALUE clause` — 78 over 983). It generalised all along; nobody had looked.

**WHAT DOES NOT GENERALISE IS THE SIZE, and that is a real gate.** The four §15 batches that have actually run —
fanned out, adversarially refuted, landed — topped out at **18 rules per agent**. §14 has **18 subjects over 25
rules and 5 over 40**, with SET at **98**; §13 has 9 over 25 and PICTURE at 72; §12's SPECIAL-NAMES is 81.
Handing one agent 98 interlocking rules is a five-fold departure from the only shape with evidence behind it,
and it would have degraded quietly — an over-long batch does not fail, it just adjudicates worse.

**THE FIX — `--max-rules` (default 20), splitting at CLAUSE boundaries first.** Every construct in the standard
is laid out the same way: `.2` general format, `.3` syntax rules, `.4` general rules. Those are genuinely
different questions — SR asks "does the grammar admit exactly this?", GR asks "does the implementation DO
this?" — so the seam is free, and it separates §5b's FMT+SR vein from its GR vein without anyone arranging it.
An arbitrary cut at rule 20 would slice a rule list mid-argument instead. Three properties matter:

- **A subject that FITS is not split at all.** §15's proven shape is one agent seeing a whole function, because
  its argument rules and returned-value rules interlock. Verified as a regression: a §15 batch still produces
  12 subjects → 12 files, byte-identically. Only an oversized subject is cut.
- **A split part knows it is a slice.** It carries `part i of n`, its sibling slugs, the WHOLE subject's clause
  map, and an instruction to say so in the notes rather than guess when a verdict depends on a rule it cannot
  see. Adjudicating GR7 unaware that SR12 exists is how a verdict comes back confidently wrong.
- **A partition invariant asserts nothing is lost or duplicated.** A dropped rule would never be issued to any
  agent, and nothing downstream would say so: the batch would look complete, the verdicts would merge cleanly,
  and the inventory would simply never move for it. That is §11 A12c one register over — a missing item read as
  no item — so it is asserted where it is cheap.

⚠ **The first version put a construct's ONE general-format rule in its own agent file** (SET → 7 parts, one of
them a single FMT row): a whole fan-out slot spent on one rule. The packer now absorbs a small group into its
larger neighbour when it fits — SET is 6 parts of 19/18/17/15/15/14.

**PLANNING NUMBERS THIS UNLOCKS**, which the campaign did not have: §14 = **90 agent files** · §13 = 99 ·
§12 = 29 · §8 = 36 · §7 = 27 · §11 = 16 · §15 remaining = 35.

**⛔ AND A CI RED THAT WAS MINE.** The instrument wave's `ProcessObservationDriftTests` failed the ubuntu
"Greenfield unit tests" job at 3633/3634 while the Windows job was green — the only red in either run. The
helper took ONE cmd.exe string and, on POSIX, string-replaced `>nul` for `>/dev/null`. That covered the token I
thought of and nothing else, so `exit /b 7` reached `/bin/sh` verbatim: measured under WSL, `sh -c "exit /b 7"`
returns **rc=2**, which is exactly the "Values differ". **A cross-platform helper built by TRANSLATION and
validated on ONE platform is not cross-platform.** The commands are now written per-OS at each call site's
intent (`ExitsWith`, `Echoes`, `RunsForever`), so there is no substitution left to be incomplete.
⚠ **Read the tests that PASSED, too.** The two `RunsForever` tests were green on Linux despite `ping -n 30
127.0.0.1` being Windows syntax — iputils happens to keep running, so the timeout still fired and they passed
**for the wrong reason**, one behaviour change from proving nothing. That is the same defect class the wave was
built to close, sitting inside the wave's own guard.
I had `feedback_wsl_linux_repro` — *build on Windows, run under WSL* — and did not apply it when the file was
written. Verified there before pushing this time: 6/6 on Linux, 6/6 on Windows, against a ~30-minute CI round
trip. The memory now names the TRIGGER (writing any per-OS branch) rather than the symptom (a red CI).

