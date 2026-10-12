## Entry 1438 — 2026-09-02 21:54 PDT — Annex A.4.2 screen handling stops being accepted-and-warned: the
largest declined module in the standard is REFUSED by name, 163 rows, and the two declined-facility seams
become one

The A.4.2 module-witness wave. `kb/Work` **PB260** asked for one thing — a test proving the *documented*
screen posture is what ACTUALLY happens — and the measurement refuted the document. Of the module's **seven**
source shapes, exactly ONE was diagnosed, and that one by a bare `"COBOLNET1560"` string literal with no
`DiagnosticDescriptor`: no `docs/DIAGNOSTICS.md` row, and `DiagnosticCatalog` still calling its band
"earmarked". Of the other six, **two were silent reinterpretations as a supported format** (`ACCEPT
screen-name-1` and `DISPLAY screen-name-1` are token-identical to their format-1 DEVICE forms and bound as
those — a declined facility that compiled, ran, and moved the wrong data), **two were a silent accept** (the
SPECIAL-NAMES `CURSOR` / `CRT STATUS` clauses, which parsed and no binder read; and the OPTIONS paragraph's
`INITIALIZE … SCREEN` target, the SEVENTH shape, which neither PB260 nor the A.4.2 selector had named — it
set `OptionsSections.Screen` and nothing anywhere read the flag), and one was a generic parse error.

**The posture is a REFUSAL, and the spec says so.** Annex A.4.1: "An implementation shall accept the syntax
and provide the functionality for an optional element only when support for that language element is claimed
by the implementor." That is a prohibition on ACCEPTING — the opposite of §4.2.6's licence for a
PROCESSOR-DEPENDENT element, which is accept-and-flag and is why COBOLNET1578/1579/1580 stay warnings.
`docs/CONFORMANCE.md` §5's own preamble had said it all along ("a parse error or a named error is the
conforming posture") while §4 item 4 and the §5 A.4.2 row described a warning; both are rewritten here, as is
§1, the ⚖ asymmetry note, the §4 preamble, the §11.9.10.4 INITIALIZE note and §6's code-side pointer.

**The mechanism.** `src/Cobol.Net.Compiler/Binding/ScreenFacility.cs` is the one funnel, and the clause table
is keyed by GRAMMAR RULE NAME so `ScreenFacilityConstructDriftTests` can read `CobolScreen.g4` and fail when
an alternative has no row (proved to fail by deleting the BELL row). **TWO codes, split at the DIVISION
boundary**: `COBOLNET1560` `screen-facility-unsupported` for the section header, every screen description
entry clause, the SPECIAL-NAMES clauses and the OPTIONS target; `COBOLNET1707` `screen-statement-unsupported`
for ACCEPT format 3, DISPLAY format 2, SET format 6 ATTRIBUTE and the EC-SCREEN names. One code would have
made every statement witness pass on the SECTION's diagnostic, since a screen-name can only be declared in a
SCREEN SECTION (feedback_green_gates_arent_evidence). 41 negative witnesses `tests/conformance/negative/a42-*`,
each `.err` carrying the code AND the named construct; 35 carry rows, 6 are supernumerary by design (five
near-miss shared clauses and the `--std 85` posture witness).

**ONE seam, not two — the reconciliation this landing owed.** Witness cluster A had already introduced
`EditionContext.Declined(descriptor, seen)` as THE declined-element seam; this wave, cut from the same base,
independently added `EditionContext.Report(descriptor, message)` for the same job, and both were on the tree
at merge time. They are folded into `Declined`: the site passes the `seen` phrase naming what the program
wrote and the descriptor's Title carries the module licence, so the emitted text opens with the witness
discriminator (`COBOLNET1560: the AUTO clause (ISO §13.18.3): A SCREEN SECTION construct …`) and the severity
still lives in the catalogue, never in a local test at the site (feedback_one_mechanism_per_job). The same
collision had produced TWO descriptors for code 1560 — witness A's `ScreenSectionUnsupported` (Warning) and
this wave's `ScreenFacilityUnsupported` (Error); the Warning one is deleted and the catalog's band header
moves 1560 from the ACCEPT-INERT half to the REFUSE half, which is where the licence puts it.

**A sibling defect, and it rejects legal source.** The sweep step asked what the module's words cost a program
that never mentions a screen. §8.10 makes `BACKGROUND-COLOR`, `FOREGROUND-COLOR` and `REVERSE-VIDEO`
context-sensitive and §8.9 reserves them NOWHERE, so `01 REVERSE-VIDEO PIC X.` has always been legal COBOL and
has always been a parse error here; `CRT` / `CURSOR` are §8.9-reserved only from 2002, so `01 CURSOR PIC X.`
is legal COBOL-85 and was likewise refused. All five are fixed (`CRT`/`CURSOR` reservation-gated), plus the
arm a name-slot-only fix would have missed: `specialNameEntry`'s `cursorClause` / `crtStatusClause`
alternatives are gated on `reservedHere`, so `SPECIAL-NAMES. CURSOR IS FOO.` at `--std 85` keeps its
implementor-switch reading instead of drawing a screen verdict (feedback_two_arm_dispatch). The complement was
MEASURED, not guessed: **22 more §8.10 words of the same shape** outside this module are still parse errors on
legal source, which is why `kb/Work` **PB301** lands at `status: half` and names all 22.

**What the merge cost, and what it revealed.** The wave was cut from `9f11a8d6`; the golden lane had since
landed three POSITIVE goldens (`2023/pb260_screen_facility_witness`, `_screen_locale_sign_witness`,
`pb260_accept_screen_reference`) and four `DocumentedNonSupportWitnessTests` methods pinning the WARNING
posture. A refusal cannot be witnessed by a program that compiles, so those goldens and those four tests are
removed — every inventory row they closed is re-verdicted in this same commit onto the `a42-*` negatives
(checked row by row: all eleven). The class keeps its COMMIT/ROLLBACK and VALIDATE witnesses, and its doc now
says why screen handling left it. The one manifest-PENDING negative entry,
`negative/pb260-accept-screen-at-phrase`, was pending because "the emitted text is not yet measured"; it is
measured now, so it ships ENABLED with its `.err` and the pending list is empty.

**`kb/Work` PB302 closes with it.** Annex A.4.2 item 10 makes the EC-SCREEN condition NAMES optional in six
positions that need no SCREEN SECTION — the RAISING phrases of EXIT / GOBACK / the procedure division header,
USE, the PERFORM WHEN phrase, RAISE and `>>TURN`. All six reached no diagnostic at all, so
`>>TURN EC-SCREEN-LINE-NUMBER CHECKING ON` compiled clean against a facility that does not exist. §14.6.13.1.1
licenses raising NO condition for such a name; it does not license ACCEPTING it. All six now resolve through
one EC-name seam and draw 1707.

**Three findings the deriver got wrong, recorded so nobody re-derives them.** (1) The proposed SET fixture
`SET SG ATTRIBUTE HIGHLIGHT TO ON` is ILLEGAL source — the printed §14.9.39.2 Format 6 has no `TO` (caught by
rendering the page, not by reading the OCR). (2) The A.4.1 ¶2 licence the deriver cited for the omitted USING
clause is the wrong half of the paragraph: "even if not explicitly listed" attaches to the RULES hanging off a
listed element and cannot make an unlisted CLAUSE optional — the licence is item 20, the screen description
entry, which the USING clause is reachable only from, and the code cites that. (3) `--check` caught two of the
implementer's own citations mid-wave (a USE witness §14.6.4 → §14.9.49; a TURN witness §14.6.13.1.1 →
§7.3.25) — the failure mode is inheriting a clause number, not inventing one.

**Rows and gates.** `record_verdicts` closed **163** A.4.2 rows DOCUMENTED-NON-SUPPORT, GAP **3379 → 3227**
(the batch is exactly PB260's `inventory_rows`, verified id-for-id; eleven of the 163 had been closed by the
golden lane on the now-removed positive witnesses and are re-verdicted, which is why the delta is 152 and not
163). Build 0 W / 0 E; Conformance wave filter Passed 3644/3644 (5 m 10 s); Unit wave filter Passed 146/146;
Characterization Passed 33/33; `work.py check` 426 items well-formed; `audit_annex_a1.py --check` no findings;
semgrep unchanged-or-down against the PB175 baseline (`no-biginteger` 36 = 36, `raw-diagnostic-code-literal`
475 → **474**, the bare 1560 literal this wave replaced). `docs/DIAGNOSTICS.md` is GENERATED — the hand-merged
conflict resolution was replaced by `scripts/gen-diagnostics-doc.ps1`, and the word set by
`scripts/gen-cobol-words.ps1`, which reproduced the merged `cobol-words.json` byte-for-byte.

