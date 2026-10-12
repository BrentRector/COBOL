## Entry 325 — Report Writer page mechanics: PAGE HEADING/FOOTING + FIRST DETAIL → RW102A/103A/104A baselined (NIST 361→364)

Three more Report Writer tests, completing the page-positioning layer. **RW102A** (multi-field DETAIL) already
passed with no code change (RW Increment B's field collection handles multiple SOURCE fields per line) — 004/004,
baselined directly (Entry between, NIST 361→362). **RW103A** (PAGE HEADING + page regions) and **RW104A** (adds
PAGE FOOTING) needed the page mechanics: both now **014 OF 014, NO TEST(S) FAILED** (NIST 362→364).

Approach chosen from a **3-agent parallel recon workflow** (ISO §14.9/§13.18.35/§13.18.39 algorithm, the exact
RW103A/104A expected counter values, and an implementation design). The recon's key extractions: a body group's
LINE-COUNTER for detail N is **N+5** (FIRST DETAIL=6, LINE PLUS 1), the page advance fires at detail 21
(25+1 > LAST DETAIL 25) resetting to **LINE-COUNTER=6, PAGE-COUNTER=2**, and — critically (ISO §13.18.35.4.5b.3)
— the **first body group on a page is positioned at FIRST DETAIL, not LINE-COUNTER+advance**, with the PAGE
HEADING auto-presented first.

Design decision: **the runtime owns the page mechanics in `EmitGroup`** (no emitted conditional branching). On
the chronologically-first GENERATE, or when `LINE-COUNTER + advance > LAST DETAIL`, it presents the PAGE FOOTING
(overflow only) + form-feed + PAGE-COUNTER++ / LINE-COUNTER=0, presents the PAGE HEADING, and sets
`LINE-COUNTER = FIRST_DETAIL − advance` so the body group's relative advance lands exactly on FIRST DETAIL. This
reproduces the spec counters exactly and stays backward-compatible with RW101A/102A (no heading → FIRST DETAIL
defaults to 1, so the first detail still lands on line 1). Because RW103A/104A's PAGE HEADING/FOOTING source only
VALUE literals + SOURCE LINE-COUNTER/PAGE-COUNTER — all values the runtime owns — the runtime **composes and
presents them itself** from a field plan registered at INITIATE (kind 0=literal / 1=LINE-COUNTER / 2=PAGE-COUNTER),
evaluating the counters at presentation time (so the heading's SOURCE LINE-COUNTER prints its own line). Page
groups sourcing program data are a noted later increment (they'd need emitted presentation).

Changes:
- `ReportWriterRuntime.cs`: `InitiateReport` gains a FIRST DETAIL param + a `Started` flag and HeadingLine/
  FootingLine/HeadingFields/FootingFields; `RegisterPageGroup`/`RegisterPageField` build the runtime-composed
  heading/footing plans; `EmitGroup` does the full page mechanics; `PresentPageGroup` composes + writes a page
  group at its absolute LINE (with `FormatNumeric` for counter fields).
- `FileIoLowerer.LowerInitiate`: passes FIRST DETAIL and emits the heading/footing registration calls
  (`RegisterPageGroup`/`RegisterPageField`), classifying each field via `ClassifyPageField`/`ExtractLiteral`.
- `CilEmitter`: `InitiateReport` 6-arg signature + `RegisterPageGroup`/`RegisterPageField` dispatch branches.
- `scripts/guard.sh` + `tests/nist/valid/RW10{2,3,4}A.txt` baselines.

Guard **ALL GREEN** — 1040 unit / 347 integration / **364 NIST** (RW101A/102A/103A/104A MATCH), 0 regressions.
RW now **4/6** (RW301M flagging + RW302M GROUP INDICATE remain; RW301M/302M are the `…M` flagging variants).
DEVLOG entry 325.

