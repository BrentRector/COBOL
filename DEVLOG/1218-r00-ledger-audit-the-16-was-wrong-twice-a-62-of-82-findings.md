## Entry 1218 â 2026-08-08 00:31 PDT â R00 ledger audit: the '16' was wrong twice â 62 of 82 findings measured fixed, 20 open, and the audit caught its own note lying

R00 asked one question: the batch-4 evidence ledger holds 82 findings, the residue block claimed 16, and
only 10 were ever transcribed â what is the TRUE open set? The answer required refusing two shortcuts:
trusting a note's status (R09 was once closed stale) and grepping by F-number (each batch numbers its own
findings, so bare F-token matches false-positive across files â my first mechanical scan "found" F1 in 92
DEVLOG entries and attributed F3 to seven unrelated CA items before I threw it away).

The instrument that worked: 49 probe programs over three rounds, each rebuilt from the finding's own repro,
run against the R10-landed tree, with the exit+diagnostic CLASSIFIED per finding â a COBOLNET1627 rejection
is a FIXED verdict, an unhandled CLR exception is an OPEN one. Plus two mechanical greps for the process
findings (clock-seam bypass, a comment citing an A.1 item the register lacks).

**Measured: 62 of 82 fixed/landed Â· 20 open â 15 new register notes (R14âR28).** The PB11/PB12/PB15/PB19/
PB22/PB38/PB40 family waves covered far more of the ledger than the residue block implied â the format
classifier, the per-position class/type screens, the argument-range ECs, the result-type tables and the
SDIDI exact family all verified live by probe. The open set clusters cleanly: the (stmt,loc)
exception-status channel (R14, with a fresh discovery â GO TO DEPENDING never gets an EC wrapper at all),
the keyword-omitted/bare-name resolution gaps (R15, R22), lexer literal gaps (R17), the float-family SDIDI
residue (R18), the FORMATTED-* seconds value channel (R24) and UTC-roll guard (R25), the
HIGHEST-ALGEBRAIC argument-shape pair (R26), the class-lattice INDEX hole (R27), and the 2002-window
owner question (R28, a decision note).

Two humbling data points, logged because the pattern is the lesson:
- **R00's own body was wrong about its own examples.** It mapped "F27âR09 via PB25"; the probe shows R09
  fixed F26 (the figurative crash) while F27's unreachable-screen defect was fixed by PB12. Of the note's
  five pre-audit orphan guesses, three were wrong in one direction or the other.
- **My own probes failed twice before they measured anything** â a bash heredoc quoting break, then six
  probes with OPTIONS in the wrong division (COBOL0001 on my own source). Both fixed and re-run before any
  verdict was credited; a probe that fails to compile is not evidence about the subject.

Also this entry: the R10 batch's comprehensive battery came back === BATTERY: ALL GREEN === with the
GnuCOBOL differential at 0 per-case flips â the unsigned-carrier change is batch-verified, not just
wave-local-verified. work.py next now ranks R14 (exceptions) Â· R18 (numerics) Â· R24 (date/time) as the
actionable head: 14 actionable items, every one probe-evidenced.

