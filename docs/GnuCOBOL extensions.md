# GnuCOBOL extensions — constructs WiseOwl COBOL does not support

> **STATUS: LIVE, PROVISIONAL, and NOT exhaustive.** This is the running register of non-ISO constructs found
> while running the GnuCOBOL testsuite through WiseOwl COBOL (plan §11 A4 / PHASE-14 Step 13, the external
> differential corpus). It exists so that support for them is a **deliberate future decision** rather than an
> accident of what we happened to notice. **Nothing here is scheduled** — the current mission is ISO/IEC
> 1989:2023 conformance across four editions (owner decision D13), and every row below is *outside* that
> target. Revisit after the compiler work completes.
>
> ⚖ **Licensing.** GnuCOBOL and its testsuite are GPL-3.0; this repository is BSL 1.1. Their test SOURCE and
> EXPECTED OUTPUT are never reproduced here. Short factual group titles/keywords and case IDs are citable
> identification (owner decision 2026-07-19). Descriptions below are our own words.
>
> ⚠ **Read the confidence column.** Rows marked **NEEDS VERIFICATION** have *not* been adjudicated against the
> ISO text. Some may turn out to be ISO constructs we wrongly reject — i.e. **our** bugs, not extensions. Do not
> cite this document as authority that something is non-ISO until its row says CONFIRMED.

## ⛔ FOUR categories, not two — classify before concluding

A construct we refuse falls into one of **four** buckets, and only the first belongs to this document as a
future-work candidate. Conflating them is the standing hazard here (owner, 2026-07-20: *"Some GnuCOBOL
extensions may simply be standard COBOL optional features we haven't yet implemented."*).

| Bucket | What it means | Right response |
|---|---|---|
| **1. VENDOR EXTENSION** | Not in ISO at all — a GnuCOBOL/MF/IBM/ACU invention. | Belongs here. Refusing it is correct. Adopting it later is a deliberate, optional choice. |
| **2. ISO OPTIONAL (Annex A.4), not claimed** | *Is* standard COBOL, in the A.4 optional-element list, and we have chosen not to claim support. | **NOT an extension.** Refusing it is the *conforming* posture (A.4.1: optional-element syntax is accepted only where support is claimed). Its home is `docs/CONFORMANCE.md` §5, which already dispositions all 13 A.4 modules. Implementing it later ADDS a claimed module — a conformance decision, not an extension decision. |
| **3. ISO MANDATORY, wrongly refused** | Standard, required, and we reject it. | **Our conformance bug.** Fix it; move the row to the "our bugs" table below. |
| **4. NEEDS VERIFICATION** | Not yet adjudicated against the ISO text. | Adjudicate with a cited §, then re-file into 1, 2 or 3. |

**A sharp check falls out of this:** if we refuse a construct belonging to an A.4 module that CONFORMANCE.md §5
marks **Claimed** or **Partial**, that is a red flag — we may be claiming support we do not deliver, which is a
false conformance claim rather than a mere gap. Mapping the differential's rejections onto A.4 modules gives:

| A.4 module | CONFORMANCE.md §5 disposition | Rejections | Reading |
|---|---|---|---|
| A.4.2 ACCEPT/DISPLAY screen handling | Not claimed | 57 | Bucket 2 — refusing is conforming |
| A.4.11 Report Writer | **Partial** | 35 | Mixed — the staged legs are known (COBOLNET0899 band); needs itemizing against the design doc's §5 |
| A.4.7 File sharing / record locking | **Claimed** | 6 | ✅ **Investigated 2026-07-20 — no bug.** `LOCK MODE IS EXCLUSIVE` is refused, but ISO §12.4.5.9 admits exactly `MANUAL` / `AUTOMATIC`; **EXCLUSIVE is not an ISO lock mode** (bucket 1). The `SHARING WITH …` forms all compile. |
| A.4.9 Locale support | **Claimed** (PB64 T1–T7; the claim completed by T6, 2026-08-28) | 4 | Implemented — the four GnuCOBOL LOCALE-function cases flipped to agreement at T4 |
| A.4.13 REWRITE FILE / WRITE FILE | Not claimed | 1 | Bucket 2 |
| A.4.14 VALIDATE | Not claimed | 1 | Bucket 2 |

That accounts for **104 of the 569** rejections as ISO-optional or already-dispositioned — i.e. correct
behaviour, not extensions and not gaps.

## How this register is produced

`scripts/gnucobol_differential.py` compiles every extracted case and buckets the outcome. The rows below come
from the `WE_REJECT_THEY_ACCEPT` bucket — programs GnuCOBOL accepts and we refuse. That bucket is **not** a bug
list: GnuCOBOL's default dialect is ISO **plus** its own extensions, and the suite pins vendor dialects
(`-std=mf/ibm/acu`) on ~90 further groups, so refusing those is *correct* behaviour for an ISO compiler. The
job of this document is to name what we refuse and why, so the residue that is genuinely ours can be separated
out and fixed.

Regenerate the evidence with:

```
python3 scripts/external_corpus.py ensure    # GPL corpus -> git-ignored tree (fetched when absent)
python3 scripts/gnucobol_differential.py     # -> report + the PER-CASE diff vs the committed baseline
```

The run ends with `=== DIFFERENTIAL: n PER-CASE FLIP(S) ===` against
`tests/external/gnucobol-verdict-baseline.tsv` (committed: `id⇥tier⇥verdict`, all three columns ours). **0 is
required**; a flip is named with its case ID and our first diagnostic. When a change legitimately moves a verdict,
regenerate with `--write-baseline` and attribute every changed row in that commit.

## Extension families

| # | Construct / family | What it is | Our behaviour today | Confidence |
|---|---|---|---|---|
| 1 | `BINARY-INT`, `BINARY-LONG` (GnuCOBOL spellings), `COMP-4` no-truncate semantics, `COMP-6` | Additional binary/packed usages beyond the ISO `BINARY-CHAR/SHORT/LONG/DOUBLE` set. `COMP-6` is an unsigned packed-decimal with no sign nibble. | Parse error at the usage keyword | **CONFIRMED** non-ISO (ISO §13.18.60.4 enumerates the standard usages; these are not among them) |
| 2 | `ASSIGN EXTERNAL name` / `ASSIGN DYNAMIC` in the file-control entry | GnuCOBOL file-assignment forms binding a file to an external/environment-supplied name | Parse error in FILE-CONTROL | **CONFIRMED** non-ISO (§12.4.5.2 ASSIGN has `TO`/`USING` forms only) |
| 3 | `EXTFH` external file handler callbacks | Third-party ISAM/file-handler interface (Micro Focus EXTFH ABI) | Parse error | **CONFIRMED** non-ISO — an interop ABI, not a language feature |
| 4 | `CBL_*` system routines (`CBL_ERROR_PROC`, directory/file routines, …) | GnuCOBOL's built-in callable runtime library, reached by `CALL "CBL_…"` | The CALL parses; the routine is simply not provided at run time | **CONFIRMED** non-ISO (implementor-supplied library) |
| 5 | `$SET`, `$DISPLAY`, `$IF` … (`$`-prefixed directives) | Micro-Focus-style directive syntax accepted by GnuCOBOL alongside ISO `>>` directives | Parse error at `$` | **CONFIRMED** non-ISO (ISO §7.3 directives are `>>`-led) |
| 6 | `>>LISTING` directive | Controls listing generation | Parse error | **NEEDS VERIFICATION** — confirm it is absent from the ISO §7.3 directive set |
| 7 | `ACCEPT … OMITTED` | Accept with no receiving item (screen/keyboard wait) | Parse error at `OMITTED` | **BUCKET 2 (likely)** — the ACCEPT/DISPLAY **screen** module is ISO **A.4.2**, dispositioned *Not claimed* in CONFORMANCE.md §5. If `OMITTED` is part of that module this is a documented optional non-support, not an extension. Verify against §14.9.1. |
| 7a | `LOCK MODE IS EXCLUSIVE` | A third lock mode beyond the ISO pair | Parse error at `EXCLUSIVE` | **CONFIRMED** vendor extension — ISO §12.4.5.9 admits exactly `MANUAL` / `AUTOMATIC`. Notable because A.4.7 is **Claimed**: verified 2026-07-20 that this is *not* a false claim. |
| 8 | `DISPLAY … UPON ENVIRONMENT-NAME` / `ACCEPT … FROM ENVIRONMENT` | Read/write process environment variables through the SPECIAL-NAMES device mechanism | Parse error | **NEEDS VERIFICATION** — ISO has `ACCEPT … FROM` device forms; confirm whether the ENVIRONMENT device is ISO or GnuCOBOL |
| 9 | Non-ISO intrinsic functions (`CONTENT-LENGTH`, `CURRENCY-SYMBOL`, `CONCAT`, …) | GnuCOBOL-supplied intrinsics outside ISO §15 | Rejected with **COBOLNET1501** (unknown intrinsic) — already a *named* diagnostic, not a bare parse error | **CONFIRMED** non-ISO for the ones outside §15; the §15 set must be cross-checked case by case |
| 10 | `JSON GENERATE` / `JSON PARSE`, `XML GENERATE` / `XML PARSE` | Document-format serialization statements (IBM-originated) | Rejected with **COBOL0313**, which names them explicitly as vendor-dialect constructs | **CONFIRMED** non-ISO |
| 11 | OSVS/IBM special registers: `CURRENT-DATE`, `TIME-OF-DAY`, `WHEN-COMPILED`, `LIN`, `COL` | Vendor special registers, gated in GnuCOBOL by `-std=ibm/osvs/mf` | Rejected | **CONFIRMED** vendor-dialect (GnuCOBOL itself gates them behind a non-ISO `-std`) |
| 12 | `CALL … BY VALUE literal SIZE IS n` | Explicit operand size on a BY VALUE argument | Parse error | **NEEDS VERIFICATION** — check §14.9.4 for a SIZE phrase |
| 13 | Reference modification applied to an intrinsic function result, e.g. `FUNCTION CONCATENATE(a b)(1:3)` | Ref-mod directly on a function result | Parse error at `(` | ⚠ **NEEDS VERIFICATION — LIKELY OUR BUG.** ISO §8.4.2.3 permits reference modification of a function identifier; if so this is a conformance gap, not an extension. **Triage first.** |
| 14 | `SPECIAL-NAMES CLASS` extended forms, extended `SHARING` phrases | Dialect variations on SPECIAL-NAMES / file sharing | Parse error | **NEEDS VERIFICATION** |

## Constructs initially bucketed here that turned out to be OUR bugs

Kept deliberately: they are the evidence that this bucket must be triaged, never adopted wholesale.

| Construct | Verdict | Resolution |
|---|---|---|
| Paragraph-less PROCEDURE DIVISION (sentences with no paragraph-name) | **OUR BUG** — ISO §14.4.3 permits it explicitly | Fixed 2026-07-20 (grammar + `AddAnonymousParagraph`); DEVLOG 931 |
| Fixed-form comment line with a blank sequence area (`*` in column 7, columns 1–6 blank) | **OUR BUG** — §6.3.2 makes the sequence area optional | Fixed 2026-07-20 (`IsFixedForm` heuristic); DEVLOG 931 |

## Constructs where WE are too lax (the opposite direction)

From the `WE_ACCEPT_THEY_REJECT` bucket — source the standard forbids and we accept. These are **conformance
bugs of ours**, tracked here only until they move to the fix queue; they are not extensions.

| Construct | Rule violated | Status |
|---|---|---|
| `OCCURS` on a level 01/66/77/88 entry | §13.18.38.3 SR1(a) — "shall not be specified in a data description entry that … has a level-number of 01, 66, 77, or 88" | Accepted at **all four** editions — unenforced SR (seed finding for §11 A2) |
| `RENAMES` naming a level 1/66/77/88 entry | §13.18.45.3 SR5 — "Neither data-name-2 nor data-name-3 shall refer to an entry that is described with level-number 1, 66, 77, or 88" | Accepted at **all four** editions — unenforced SR (seed finding for §11 A2) |

## Maintenance

Add a row whenever the differential surfaces a construct we refuse that is not ISO. Move a row out of the
NEEDS VERIFICATION state only after adjudicating it against `specs/ISO_COBOL.md` with a cited §. If adjudication
shows the construct IS ISO, move it to the "our bugs" table and open a fix item — that is the outcome this
register exists to make visible. Related: `docs/CONFORMANCE.md` (what we claim), plan §11 A4 (the campaign),
`docs/DOC_INDEX.md`.
