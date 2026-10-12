## Entry 1738 — 2026-09-27 12:39 PDT — One-call orientation: scripts/spec/orient.py carries what earlier implementers learned

Owner, 2026-09-27: "It seems we can optimize orientation. Isn't there over many waves, more of the same orientation?
Can we reduce the repeated work?" Reading and searching was measured at 46 % of implementer tokens (waves 45–57), and
every wave re-derives the same facts about the same files. `where.py` (which files cite a clause), P6 (start from the
note's code sites) and P13 (LSP before grep) all shorten the SEARCH. None of them carries forward what an earlier
implementer LEARNED about a file, although that knowledge is already on disk: the landed kb/Work notes' code sites
and mechanisms, commit subjects, and the tests that exercise the file.
`scripts/spec/orient.py <files…>` prints, per file:
- an outline of types and members with line numbers;
- its § citations by count;
- the test files that name its type;
- LEARNED: each landed/retired note that named it, newest first, with the note's sentences about this file;
- the open notes that name it;
- its last commits.
It writes nothing and derives every section on each run, so it cannot go stale and adds no document (rules 6 and 8).
Measured: `ReportWriter.cs` (about 1,100 lines, 38 landed and 40 open notes) comes down to 86 lines, and
`ReferenceFormatProcessor.cs` (843) to 45, in 0.3 s. The implementer dispatch-spec template now says to run it FIRST,
before reading any source file. MANDATORY-PRACTICES P6 says the same, and `check_practices.py` requires `orient.py` in
every rendered spec. The check was proven to fail: an old wave-66 spec now reads RED. Waves 65/66 were dispatched
before this; wave 67 is the first to use it. The measure is orientation tokens per implementer against the 46 %
baseline.

