## Entry 1382 — 2026-08-29 08:28 PDT — PB140 LANDED: the I-O status discipline — the '43' gate drops at the ONE chokepoint, the fail-open registry is loud, CLOSE gets its failure boundary, and openness splits from the FPI

Five seams, one discipline (kb/Work PB140). THE '43' GATE (§9.1.13.7 3): PrevOpWasSuccessfulRead now drops
in the FileConnector.Status SETTER — every status assignment is an operation's outcome under §9.1.13.1's
closed statement set — and the READ terminals re-arm it through the one ReadSucceeded helper. The four
leaking statements (UNLOCK, DELETE FILE, the already-open '41' OPEN arm, the sharing '38'/'61' arms) close
by construction; the '46' poison stays site-managed (an intervening WRITE establishes no next record). THE
FAIL-OPEN REGISTRY: FileRegistry.Status's '00'-for-unknown default and the keyed dispatchers'
'30'-without-SetStatus fall-throughs are replaced by the loud Require/MisroutedVerb invariants — reachable
only through a compiler defect now that the ONE §13.4.6.3 SR3 screen (COBOLNET1692,
StatementValidation.ScreenSortMergeFile) rejects an SD file-name in CLOSE / DELETE / DELETE FILE at bind
time and the OPEN/WRITE/READ/REWRITE loud-stage seam routes through the same screen. §14.9.6.3 SR1's
organization restriction on REEL/UNIT/NO REWIND is COBOLNET1693. CLOSE: FileConnector.Close carries its
OPEN twin's exception mapping ('30', §9.1.13.6 item 1) where a flush failure previously aborted the run
unit; the sequential streams null in a finally; a completed CLOSE — successful or not — leaves the open
mode (GR8); CLOSE WITH LOCK locks only on a SUCCESSFUL close (GR1 — the unconditional _locked.Add poisoned
every later OPEN after a '42'). OPENNESS ≠ FPI: one base _openMode bit replaces the three per-connector
predicates, SequentialConnector.IsOpen no longer folds OptionalAbsent in, and NO CloseCore clears
OptionalAbsent — the FPI survives a CLOSE unchanged exactly as GR6 states, so CLOSE UNIT on an absent
optional input file answers '00' (no unit processing) instead of '07'. DELETE FILE: the presence probe is
File.GetAttributes (File.Exists classified a present-but-unobservable file as the successful-'05' absent),
and the IOException arm classifies a medium refusal (ERROR_WRITE_PROTECT / EROFS) as GR17's '37' via the
unit-pinned FileStatusCode.ForDeleteFileFailure.

TWO GATE REDS, both the fix's own edges, both re-derived from the spec rather than accommodated: the
file_sharing_seq golden PINNED THE LEAK — its REWB leg re-executed a REWRITE after that REWRITE's own '51'
conflict and expected '00', but §9.1.13.7 3) makes the failed REWRITE itself the last executed I-O
statement, so the retry is '43' and record 2 keeps the other connector's MMMMM (the RETRY phrase, which
retries WITHIN one statement execution, is the sanctioned path); and the FLAG-02 I-O-STATUS-07 unit fixture
rode the INDEXED ReadPreviousProgram — its CLOSE … WITH NO REWIND is exactly the §14.9.6.3 SR1 shape
COBOLNET1693 now rejects, so it gets its own SEQUENTIAL program.

Verdicts: GR-14.9.10.4-2 DIVERGES → CONFORMS, GR-14.9.10.4-10/-16/-17 PARTIAL → CONFORMS, GR-14.9.6.4-1/
-6/-8 PARTIAL → CONFORMS, SR-14.9.6.3-1 / SR-14.9.10.3-3 NOT-IMPLEMENTED → CONFORMS, GR-14.9.6.4-4
DIVERGES → PARTIAL (the NO REWIND '07' leg is PB141's CloseKind row). Four goldens (85/2002/2014/2023),
five negatives, thirteen runtime unit facts over a PRIVATE FileRegistry instance.

