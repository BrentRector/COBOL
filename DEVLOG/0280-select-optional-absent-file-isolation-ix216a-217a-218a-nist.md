## Entry 280 — SELECT-OPTIONAL absent-file isolation → IX216A/217A/218A (NIST 265→268; IX 38/42)

Cracked the IX216A/217A/218A wall left at the end of 279 (OPEN INPUT / EXTEND / READ of a *not-existing*
OPTIONAL indexed file, which must give status 05 — "file created/not present" — and READ → AT END 10, never
00 from a leftover shared file). Three coordinated changes:

**1. SELECT-scoped, OPTIONAL-aware X-card mapping (`NistPreprocessor`).** The old mapping had `XXXX[PD]### →
"TF###"` GLOBAL plus a SELECT-region pass that mapped only RELATIVE/INDEXED `XXXXX###`. That shared an
OPTIONAL file's produce/permanent target with whatever producer created `TF###` first, so the "absent"
optional file looked present. Reworked into ONE pass over each SELECT entry: `XXXXD###` (consume) is ALWAYS
shared (a consumer deliberately reads another program's file — SM203A↔SM204A, SQ203A's "FILE PRESENT" test);
`XXXXP###` and the RELATIVE/INDEXED `XXXXX###` are shared too, but ONLY when the SELECT is NOT `SELECT
OPTIONAL`. An OPTIONAL file's target is left as an implementor-name so the Binder qualifies it per program-id
— so an absent-optional file is genuinely absent per run unit. IX216A uses `XXXXX025` (INDEXED), IX217A/218A
use `XXXXP024/025`; all are SELECT OPTIONAL, so all stay program-qualified.

**2. `IndexedFileHandler` OPEN I-O on a missing OPTIONAL file → 05** (was throwing/35), matching the
SequentialFileHandler optional-absent path from 268. EXTEND already created-on-absent.

**3. Guard start-clean (`scripts/guard.sh`).** Added `rm -f tests/nist/output/*.txt` at the top of the NIST
section. An absent-file test that *creates* its optional file on run N would see it present on run N+1 and
pass-once-then-fail-forever; the start-clean makes the guard deterministic from any prior state. Producer/
consumer chains still rebuild WITHIN a run because producers precede consumers in NIST_TESTS order (the loop
still does not clean BETWEEN tests, so `TF###` carries over once created).

**The detour worth recording (transparency).** Change #3 surfaced a regression in SM204A — a previously-green
consumer that reads RCD-1..7 (97532, 23479, …) written by SM203A over `XXXXD002`/TF002. I first suspected the
start-clean had broken the chain, but a stash-to-HEAD A/B proved SM203A→SM204A round-trips fine from clean at
HEAD and ONLY my preprocessor rewrite broke it. Root cause: `NormalizeToFreeForm` rewrites fixed-form `*`
comment lines into free-form `*> …` lines that *survive* in the text the preprocessor sees, and CCVS puts
three such comment lines — one ending `…DURING EXTRACTION.` — BETWEEN `SELECT TEST-FILE ASSIGN TO` and the
`XXXXD002.` operand. My first SELECT-region regex `SELECT\b[\s\S]*?\.` stopped at that comment's period,
leaving `XXXXD002` (after it) unmapped → producer and consumer each qualified TF002 per-program and stopped
sharing → SM204A read an empty file. Fix attempt #2 made the body skip whole `*> …` comment lines
(`(?:\*>[^\n]*\n|[^.])*\.`) so it spans to the entry's REAL period — which fixed SM204A but regressed
SQ130A/141A/142A: those comment-skip bodies now ran past a *commented-out* optional scratch SELECT (indicator
`P`, the INDEXED `RAW-DATA` on X-62) and, because that comment block says INDEXED, wrongly mapped the
following SEQUENTIAL `XXXXX001`/`XXXXX014`, destroying the per-program isolation those absent-file status
tests rely on. Fix #3 (final): anchor the match to a REAL-CODE select line, `(?m)^[ \t]*SELECT\b…`, so a
"SELECT" sitting inside a `*> …` comment is never a match start. This is the ISO reference-format rule
(indicator `*` = comment, carries no source; comment lines are transparent to statement structure) applied
directly — not a per-test patch. SM204A + SQ130A/141A/142A + IX216A/217A/218A all green together.

Guard ALL GREEN: 1000 unit / 347 integration / **268 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ + 19 RL +
**38 IX**), 0 regressions. IX now **38/42**. Remaining: IX215A (REDEFINES-of-key + three identically-named
qualified keys — needs qualified-name key resolution + duplicate-key disambiguation; deep), IX110A
(order-fragile — IX103A's delete test depletes TF024 before it), IX301M/IX401M (flagging modules, no CCVS
report — excluded by design).

