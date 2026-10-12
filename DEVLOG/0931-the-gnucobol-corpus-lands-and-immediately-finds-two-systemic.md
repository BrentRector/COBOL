## Entry 931 — 2026-07-20 00:40 PDT — The GnuCOBOL corpus lands and immediately finds TWO systemic bugs our whole battery was blind to

The owner pulled PHASE-14 Step 13 forward: retrieve the GnuCOBOL testsuite and run it through COBOL.NET,
because "they should identify places where we got the grammar wrong." They did, within minutes of the harness
working — and the harness itself taught the sharper lesson.

**THE INFRASTRUCTURE** (committed `fb6538b4`): `fetch-gnucobol-tests.ps1` (GnuCOBOL 3.2 pinned, SHA256-verified,
into the git-ignored tree) · `gnucobol_extract.py` (a bracket-balanced m4/autotest parser — regex cannot do it,
the quoting nests; validated at **1346 groups / 3503 checks / ZERO extraction failures**) ·
`gnucobol_differential.py`. Licensing per the owner's ruling: their SOURCE and EXPECTED OUTPUT never leave the
ignored tree; short factual group TITLES and KEYWORDS are citable identification, which is what makes the ledger
usable for triage.

**⛔ MY HARNESS FABRICATED 1046 FINDINGS, AND I RELAYED THEM.** The first run reported
`WE_REJECT_THEY_ACCEPT = 1046, AGREE_ACCEPT = 0`. I noted in passing that it was "almost certainly my harness"
and then, one paragraph later, narrated it as though the paragraph-less bug "explained the 1046". It explained
none of it: aggregating the error text showed **all 1046 were `<<RUNNER-ERROR FileNotFoundError>>`** — on
Windows, `subprocess.run` with a relative forward-slash path raises WinError 2 even though `os.path.exists()` is
True, so the compiler was never invoked once. My `except Exception → we_ok = False` laundered a dead harness into
a thousand plausible "compiler rejects legal COBOL" findings. **That is the exact defect class I have spent this
session hunting — a wrong answer that looks like a finding — reproduced inside my own instrument.** Two hardening
changes: a harness failure now returns a distinct `RUNNER_ERROR` verdict rather than a compiler verdict, and the
run **ABORTS LOUDLY above 5% harness failures** because past that every downstream number is noise.
`AGREE_ACCEPT = 0` against 1323 real programs is not a plausible result for any compiler and I should have
stopped there.

**BUG 1 — we rejected PARAGRAPH-LESS procedure divisions.** §14.4.3: a paragraph is a paragraph-name plus
sentences *"or, if the paragraph-name is omitted, one or more successive sentences following the procedure
division header or a section header."* Our grammar had **no alternative for it at all** — every `procedureUnit`
had to open with a paragraph or section name. Adding `sentence*` made such programs PARSE and then silently
produce NO OUTPUT, because `ProcedureTableBuilder` walks `procedureUnit()` and never saw them — the
parse-but-drop class, worse than rejecting. So the binder half was required too: `AddAnonymousParagraph` takes a
pc so the dispatcher runs it but is registered in **NO name map** — having no paragraph-name it must not be a
PERFORM/GO TO target (§8.4.2.2 resolves procedure-NAMES), and a synthetic name would both make it referenceable
and risk colliding with a user word. Wired at all three sites (PD header, SECTION header, method PDs); verified
by execution, including `PERFORM S1` correctly entering a section whose first paragraph is unnamed.

**BUG 2 — we rejected fixed-form COMMENT LINES when the sequence area is blank.** `IsFixedForm` REQUIRED a
numeric sequence area:
```csharp
return totalLines > 0 && hasNumericSequence && fixedIndicators * 100 / totalLines > FixedFormThresholdPercent;
```
Columns 1–6 are OPTIONAL (§6.2.1 — they "may be used to label a source line"). With them blank the file was
classified **FREE**-form, where a `*` in column 7 is not a comment indicator but a stray token — so an ordinary
comment line became a syntax error. Now either signal suffices: a numeric sequence area **OR** a real column-7
indicator glyph, both still behind the same structural ratio that keeps genuinely free-form source (code at
column 1 ⇒ letters in cols 1–6 ⇒ `seqOk` fails) out of the fixed branch. Verified against all three shapes.

**WHY BOTH HID FOR SO LONG — the same root cause.** NIST/CCVS is punched-card-era source: it *always* fills the
sequence area and *always* writes paragraph names. Our entire regression net inherited that shape, so 3701
conformance + 313 unit + 33 characterization + 667 legacy + 350 NIST tests are ALL green and ALL blind to both.
An in-house corpus cannot find what the in-house corpus's own conventions conceal. This is the whole argument for
Campaign B, demonstrated on day one.

**MEASURED IMPACT** (same corpus, three runs): harness fixed → `AGREE_ACCEPT 412`; +paragraph-less fix and
+reference-format fix → **`AGREE_ACCEPT 477`, `WE_REJECT_THEY_ACCEPT 634 → 569`**. Agreement 44% → 49% of 1323.

**WHAT REMAINS, AND HOW TO READ IT.** GnuCOBOL's default dialect is ISO **plus extensions** and the suite pins an
ISO edition on only ~63 groups, so `WE_REJECT_THEY_ACCEPT` is *not* a bug list — the long tail is visibly
vendor surface we are RIGHT to reject (`BINARY-INT`, `COMP-6`, `ASSIGN EXTERNAL`, `DISPLAY UPON ENVIRONMENT`,
JSON/XML GENERATE → our COBOL0313). The higher-confidence direction is **`WE_ACCEPT_THEY_REJECT = 107`**
(6 of them ISO-PINNED): source GnuCOBOL's *permissive* default refused and we accepted — prime "our grammar is
too lax" leads, needing no dialect adjudication first. That is the next triage front.

