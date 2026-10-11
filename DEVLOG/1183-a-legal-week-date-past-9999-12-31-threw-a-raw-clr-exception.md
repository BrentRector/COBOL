## Entry 1183 — 2026-08-05 21:12 PDT — a legal week date past 9999-12-31 threw a raw CLR exception out of THREE intrinsics, and the fix's whole design is that the three must NOT agree

**PB23.** `INTEGER-OF-FORMATTED-DATE("YYYYWwwD", "9999W527")` aborted the run unit with
`System.ArgumentOutOfRangeException` from inside `ISOWeek.ToDateTime` — neither a COBOL value nor a COBOL exception
condition, where **§15.3 rule 14** permits only EC-ARGUMENT-FUNCTION or the implementor-defined result.

**Why no range table could have caught it.** §15.3.1.7 constrains a week date's subfields *individually* — year
1601–9999, week 01–52 (01–53 in a long ISO year), day-of-week 1–7 — and `9999W526`/`9999W527` satisfy **every one
of them**. It is the COMBINATION that lands outside the calendar: 9999-12-31 *is* ISO 9999-W52-5, so days 6 and 7
of that week are 10000-01-01 and 10000-01-02. The queue entry's correction to the already-landed row
`AR-15.79.3-5` — that the window is TWO days, not the one day that row cited — is confirmed by measurement.

**The sweep changed the size of the defect.** The crash sits in the SHARED `Analyze`, so it escaped THREE
intrinsics, not the one the entry named: `TEST-FORMATTED-DATETIME` and `SECONDS-FROM-FORMATTED-TIME` faulted
identically, and both week formats (`YYYYWwwD`, `YYYY-Www-D`) reach it. ✅ The sibling construction sites were
MEASURED rather than reasoned about, and they are clean: `"99990231"`→7, `"9999366"`→7, `"9999W531"`→7 are each
rejected at the right character, because the range table already correlates day-with-month and
day-of-year-with-year. Only the ISO-week site can overflow.

**⛔ THE OBVIOUS FIX IS WRONG, AND WOULD HAVE BROKEN A FUNCTION THAT WAS WORKING.** Having `Analyze` return an
error position for these values would make TEST-FORMATTED-DATETIME call them INVALID — and they are not. So the
"no integer date form" signal is deliberately kept distinct from the format-validity result, and the three
functions diverge on purpose:

- **INTEGER-OF-FORMATTED-DATE raises.** §15.48.4 r1 makes its result "the integer date form equivalent", §15.5.2
  caps that at "the value of FUNCTION INTEGER-OF-DATE (99991231), which is 3,067,671", and there is no
  equivalent — so rule 14's "incorrect value … FOR THE RETURNED VALUE" arm applies exactly.
- **TEST-FORMATTED-DATETIME returns 0.** The decisive argument is structural, not stylistic: §15.92.4 requires any
  non-zero answer to be "the ordinal character position at which the first error … was detected", and **no
  CHARACTER of `9999W526` is in error** — the identical `'6'` in `2009W526` is valid. The defect is a property of
  the whole date, which that return type cannot express.
- **SECONDS-FROM-FORMATTED-TIME returns its seconds** (§15.79.4 reads only the time subfields).

⚖ Recorded as an INTERPRETATION with its alternative named: §15.92.4's "format problems **or range problems**" can
be read as covering the composite overflow (answer 8). The external oracle is **silent** — the GnuCOBOL corpus
exercises `1601W531`→7 and `2009W531`→0, both of which we already match, but has nothing at the 9999 boundary. If
the owner prefers the other reading, `TestFormattedDatetime`'s `out _` is the one place to change.

**The fix.** `ISOWeek.ToDateTime` is gone. §15.3.1.7 fixes week 1 as "the week that includes January 4", so that
week's Monday is computed directly and every later day is a plain integer offset from it — the arithmetic CANNOT
overflow, which is the property that matters when the input is legal. The result is then bounds-checked against
§15.5.2's ceiling on EVERY path rather than only the week one, so the calendar and ordinal paths stop depending on
their range expressions each independently staying right.

**⚠ REPLACING A LIBRARY CALL WITH HAND-DERIVED CALENDAR ARITHMETIC IS THE RISK, SO IT IS PROVEN, NOT ARGUED.**
`CobolDateIntegerDateRangeTests` compares the new computation against `ISOWeek` over **more than 200,000 week
dates spanning 1601–9999** — every year's first, second and last weeks across all seven days, plus exhaustive
sweeps of eight years chosen for their shape (long ISO years, century non-leap years, both endpoints) — asserting
agreement wherever `ISOWeek` can express an answer and the §15.3 default where it throws. It asserts its own
population, so a sweep that silently compared nothing would fail rather than pass. Hand-derived calendars are
right at the boundary you tested and wrong three centuries away; this one is not.

**⛔ AND THE INVENTORY'S OWN NAMED TEST GAP IS CLOSED — the row told me what fixture was missing.**
`RV-15.48.4-1` recorded that NOTHING exercised a COMBINED format for this function, that the §15.48.4 NOTE was
"verified by code reading ONLY and never by a test", and that "Annex D.31.5.8's own worked example … is the obvious
fixture and is absent." It is now present, verbatim from the standard: `"YYYYMMDD"/"19950215"` and
`"YYYYMMDDThhmmss.ss+hhmm"/"19950215T05142781+0500"` both → 143951, and D.31.5.9's `"hhmmss.ss+hhmm"/"05142781+0500"`
and its combined form both → 18867.81. That is the leg that faulted — a purely TIME-side question was computing a
date — so it is the leg that most needed a test.

**Inventory.** `AR-15.48.3-4`, `AR-15.48.3-5`, `RV-15.48.4-1` and `AR-15.79.3-5` moved **DIVERGES → CONFORMS**
with test-refs, merged through `scripts/spec/record_verdicts.py` (verdicts are never hand-written; the tool
validates shape and the C# gate checks that every reference resolves on disk). **GAP 3907 → 3903.** All four rows
had independently written "ONE ROOT CAUSE, ONE FIX … do not triple-count" — and they were right.

**Gate (wave-local).** Conformance `~Date|~Time|~Intrinsic|~Corpus` **614/614** · Unit
`~Date|~Intrinsic|~Drift` **2856/2856** · the inventory gate **10/10**, which is what proves the four new
test-refs actually resolve. Zero skipped. Golden
`tests/conformance/2023/pb23_week_date_beyond_integer_date_form`, 13 sections, EC-ARGUMENT-FUNCTION checking ON,
every value derived from the spec before the run.

