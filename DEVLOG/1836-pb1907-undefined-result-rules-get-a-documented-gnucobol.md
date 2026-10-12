## Entry 1836 — 2026-10-01 15:20 PDT — PB1907: undefined-result rules get a documented, GnuCOBOL-aligned, pinned behavior (owner decision)

The six owed rows that no golden could witness to the standard (GR-14.6.10-1, INSPECT GR-14.9.22.4-13/-18/-21, UNSTRING
GR-14.9.48.4-18, WRITE GR-14.9.51.4-13) are all Annex A.2 undefined elements, and section 4.4 makes a run unit that allows
them conforming (`cite.py --check 4.4` OK). The owner declined a section 8 "no observable obligation" signature and
directed instead: determine the best behavior against other implementations, document it as what we do, and pin it with a
golden (kb/Work/PB1907). A three-agent survey (0.56 M tokens) measured ours on a pinned build and read GnuCOBOL's libcob;
GnuCOBOL 3.2.0 was then built in WSL (libgmp-dev, no DB/curses/XML/JSON) and every repro MEASURED, which overturned two of the
agents' read-from-source recommendations: INSPECT TALLYING overlap and the JUSTIFIED and numeric MOVE corners differ from
ours although the agents advised keeping ours. Owner decision: follow GnuCOBOL wherever it measurably differs (CLAUDE.md
rule 1), except WRITE's record length, which ISO defines. Differences to implement: INSPECT format 3 counter in target
(ours `0BBB`, GnuCOBOL and GR19 `3BBB`), INSPECT TALLYING overlap, three UNSTRING cases (sender, delimiter, TALLYING
overlap), WRITE content past record-name-1 (shared record area, not spaces), MOVE JUSTIFIED and numeric corners. Wave 1002
carries four Sonnet implementers, one per emitter.

**Process note.** `wsl -d Ubuntu -- bash /mnt/e/...` run from Git Bash has its path rewritten (`C:/Program Files/Git/mnt/e/...`)
and fails instantly; run WSL commands from the PowerShell tool.

