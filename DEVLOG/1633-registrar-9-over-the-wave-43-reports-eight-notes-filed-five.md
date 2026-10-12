## Entry 1633 — 2026-09-22 06:23 PDT — REGISTRAR #9 over the wave-43 reports: eight notes filed, five extended, and TWO of the four handed-forward leads did not survive their own re-measurement

**The lead-filing pass over wave 43's four implementer reports (PB484, PB571, PB620, PB450), the train-44
lander report and the two workflow result files.** The register is **991 notes**; it moved no verdict, so
**GAP is 2276 before and after**. Every probe was RE-RUN on this worktree's own `dotnet build CobolSharp.sln
-c Debug` (`0 Warning(s), 0 Error(s)`) at main `c982251df` and every citation re-derived with
`cite.py --check`, which is the whole point of the pass: a registrar that copies a report's measurement
forward is a second place for the report's mistakes to live.

**⛔ TWO OF THE FOUR LEADS THE LANDER HANDED FORWARD ARE WRONG WHERE THEY ARE MOST QUOTABLE, AND BOTH WERE
REPEATED VERBATIM BY A SECOND AGENT.** Lead 1 — `PLUS` and `+` are synonyms and the report grammar admits only
the word — is real: `03 LINE + 1.` and `03 COLUMN + 2 PIC X.` are `COBOL0001: unexpected '+'` while the
identical program spelled `PLUS` compiles clean, and §13.18.35.3 SR1, §13.18.14.3 SR2 and §13.18.37.3 SR2 each
print *"PLUS and + are synonyms."* But BOTH the implementer report and the lander report add *"plus the
screen-section twin in `CobolScreen.g4` (same pair, plus MINUS / en-dash)"*, and the screen grammar is already
right: `screenLineClause` and `screenColumnClause` admit `PLUS | PLUSWORD | MINUS` today, and the file's own
comment explains why the WORD `MINUS` arrives as an `IDENTIFIER`. The report LINE clause's printed format has
no MINUS at all — §13.18.35.2 Format 1 prints `{PLUS | +}` and Format 2 (screen) prints the four-way group —
so the two clauses have different sign vocabularies and the sweep the lead asks for would edit a file that
needs no editing (kb/Work **PB951**). Lead 3 — ACOS/ASIN enforce their domain with a binary64 NaN artifact —
is real and worse than reported, but its most quotable sentence, *"ASIN returns π/2 where §15.3 requires 0"*,
is not in §15.3: rule 14 makes the result implementor-defined **only when checking is not enabled**, and
neither §15.8.4 nor §15.10.4 names a substitute. The defect does not need the claim and is stronger without
it — under ARMED checking the condition is not set at all (kb/Work **PB952**).

**The measurement that makes PB952 more than a sibling sweep.** `01 JUST-OVER PIC 9V9(18) VALUE
1.000000000000000001.` with `>>TURN EC-ARGUMENT-FUNCTION CHECKING ON` prints `ACOS-JUST-OVER=000000{` and
`ASIN-JUST-OVER=157079F` — +0.000000 and +1.570796 — and the run continues, while the same program's
`FUNCTION ACOS(2)` terminates the run unit, so the directive is live. The COBOL item holds a value greater
than 1 exactly; it is the conversion into the body's `double` that rounds it to 1.0, after which `Math.Acos`
is perfectly happy. **Both rows are CONFORMS today** — `AR-15.8.3-2`'s own inventory note says the witness
used `±1.000000001`, nine decimals out, which binary64 still distinguishes from 1.0. The green is evidence
about the magnitude the witness CHOSE, not about the rule, and the landing that fixes this re-adjudicates both
rows. kb/Work **PB246** argued in this very file that the NaN artifact is not total — for SQRT — and did not
sweep the two functions four lines above it.

**Eight notes filed: PB951–PB958.** Beyond the two above: **PB953** — `record_verdicts.py` assigns all six
`ADJUDICATED` fields (`row[field] = rec.get(field, "")`), so a batch that restates fewer witnesses SUBTRACTS
evidence and one that omits `derivation`/`notes` erases them, and **nothing can detect it afterwards**: a row
left with fewer, still-resolving references looks exactly like a row that never had more. It has happened
twice, both repaired by hand in wave 43. **PB954** — a recovery `PicInfo`'s category is read as an ANALYSIS,
so `01 W-BAD PIC 9V9V9.` draws the correct COBOLNET1934 and then a false `COBOLNET0844: item 'W-BAD' of
category alphanumeric is not a numeric operand`; PB453 repaired exactly one reader of that placeholder and the
`PicInfo` comment states the defect in the compiler's own words. **PB955** — the 31-digit limit on a fixed-point
data ITEM is §13.18.40.3 SR14 and the report listed six sites citing §8.3.3.3.2 (LITERALS) for it; the re-sweep
found **eight**, including the per-edition twin of a listed golden and a `receiver`, and neither citation audit
can see the class because SUBJECT fires only when the context names another CONSTRUCT. **PB956** — BASED data
or an `ADDRESS OF` target anywhere in a CLASS definition's data divisions refuses the whole compilation, where
§13.18.5.3's two syntax rules bar neither and `CONFORMANCE.md` calls the OO core mandatory surface and
implemented. **PB957** — the NEXT GROUP clause is refused by name at every edition and its **fourteen**
inventory rows were unadjudicated AND unowned; it surfaced only because PB951's `+` probe hit a loud stage
first, and it is DOCUMENTED in A.4.11's staged-loud sentence, which is the point: documented is not owned.
**PB958** — §15.10.4 rule 1 spells π as the letter `p` while §15.8.4 and §15.11.4 three lines away carry the
real character; it was NOT hand-repaired, because the repair moves `rule-digest` under `RV-15.10.4-1`'s live
CONFORMS verdict and therefore needs a catalog rebuild plus a `record_verdicts` batch.

**⭐ TWO DEFECTS IN THE REGISTER ITSELF, AND BOTH PUT A PHANTOM AT THE TOP OF THE HARM-RANKED QUEUE.**
(1) **PB857 and PB590 were the same defect filed twice** — the BOOLEAN class condition — and PB590's train-44
landing flipped only the note it was dispatched against, so PB857 went on ranking in `work.py next` as an open
MAJOR `rejects_legal_source` for nine days. `IF BD IS BOOLEAN` prints `BOOL` today; PB857 is `landed` with
`closes_rows: []` and a reason, because PB590 already recorded the rows and claiming them twice is the defect
the register exists to prevent. (2) **PB450 stayed open holding one row for a mechanism it did not own** —
its own body says so — so the row (`SR-14.9.39.3-18`) moved to PB956 and PB450 is `landed`. An open MAJOR note
with no remaining work is worse than a closed one: it outranks real work. The durable answer to (1) is the
`closes_rows` back-link already scheduled as PB245.

**PB225 was the ⭐ lead, and the register already owned it.** The PB571 report's "seven of §8.8.4.4.2's
fourteen class-condition alternatives" is PB225, open since the defective-row wave — so it was EXTENDED, not
duplicated, and the extension is not cosmetic: the note said the program *"compiles CLEAN and throws
NotImplementedCobolFeatureException at the condition's first evaluation"* and all seven words are now refused
at COMPILE time at 2014+ (`crashes` → `rejects_legal_source`, MINOR → MAJOR). Two facts neither report
carried: `FLOAT-NOT-A-NUMBER-QUIET` alone has no lexer token, so it draws COBOLNET1639 where the other six
draw COBOLNET0901; and at `--std 2002` all seven draw COBOLNET1639, which is the RIGHT answer below the
introducing edition. The note's proposed fix shape is also stale — PB571 deleted the five-way resolve it names
— so each alternative is now one grammar alternative + one `ClassConditionModel` row + one renderer arm.

**Gates, read as output.** `work.py check` → `✓ 991 work items, all well-formed` (and it FIRED once, on
PB857's H1 still reading `OPEN` after the frontmatter said `landed` — exactly the lie it exists to catch).
`dotnet test tests/Cobol.Net.Tests.Unit --no-build --filter
"…DefectiveRowCoverage|…SpecTraceabilityInventory|…WorkRegister"` → `Failed: 0, Passed: 18`, and
`DefectiveRowCoverage` alone → `Failed: 0, Passed: 3`, run separately to prove the term is not dead. No
compiler code changed. `gen_conformance_notes.py` refreshed the burn-down view (4,348 items · 2,276 GAP);
`kb/Conformance/` is gitignored, so the refresh session-probe asks for produces no diff — worth knowing before
the next agent hunts for one.

