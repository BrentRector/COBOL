#!/usr/bin/env python3
"""PHASE A of the spec-conformance review — build the rule catalog (THE DENOMINATOR).

`docs/rearchitecture/DESIGN-spec-conformance-review.md` defines P14 Step-0 DONE as every normative ISO/IEC
1989:2023 rule having a located implementation, a spec-verified verdict, and a spec-derived test. None of that is
measurable without first enumerating the rules — the thing the two prior audits never did (they were bounded
SAMPLING). This script produces that enumeration.

The spec transcription is regular enough to parse directly:

    <a id="section-8-4-3-13-3"></a>
    ## 8.4.3.13.3 Syntax rules

    1) Identifier-1 shall be of category alphanumeric or national.
    2) ...
       a) sub-item
       b) sub-item

So a rule is (section-number, kind, ordinal) and its text runs to the next ordinal or heading.

COMPLETENESS IS THE WHOLE POINT: an omitted rule is silent false confidence — the catalog would report a
denominator smaller than reality and every later percentage would be wrong in the flattering direction. So the
script self-checks: every rule-block heading in the file must yield at least one rule, and any that yields zero is
reported as a PARSE GAP rather than silently dropped.

Usage:
    python scripts/spec/extract_rule_catalog.py                 # write the catalog + print the summary
    python scripts/spec/extract_rule_catalog.py --check         # parse only; non-zero exit if gaps exist
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
from collections import Counter

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from cite import CORRECTION_NOTE  # noqa: E402  — ONE definition of what an editorial correction note looks like

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC = REPO / "specs" / "ISO_COBOL.md"
OUT = REPO / "docs" / "rearchitecture" / "spec-rule-catalog.json"
#: The COMMITTED inventory of clauses that carry ``N)``-shaped rules this extractor does not harvest — the
#: manifest the content-keyed scan compares against, so "nothing new fell out of the denominator" is an
#: observation rather than a memory. See unharvested_rule_blocks() and fix-queue PB29.
UNHARVESTED = REPO / "docs" / "rearchitecture" / "spec-unharvested-rule-blocks.json"

# A numbered section heading, e.g. "## 8.4.3.13.3 Syntax rules" or "### 14.9.28.4 General rules".
HEADING = re.compile(r"^#{2,6}\s+(?P<num>\d+(?:\.\d+)*)\s+(?P<title>.+?)\s*$")
PAGE = re.compile(r"^##\s+Page\s+(\d+)\s*$")
# Any markdown heading — used to CLOSE a rule block (see the loop). Annex headings carry no section number.
ANY_HEADING = re.compile(r"^#{1,6}\s+\S")
# A GENERAL-FORMAT DIAGRAM in the transcription. Every diagram is a `<pre style="line-height:1">` block today; the
# fenced (also inside a `>` blockquote, as §7.3.23.2 still is) and `$$` forms are the transcription's earlier
# renderings. Keying `has_diagram` on those two alone left it False on all 322 FMT rows once the diagrams moved to
# <pre> — a field that could never report the one thing it names (kb/Work PB1536).
FMT_DIAGRAM = re.compile(r"<pre\b|^[\s>]*```|\$\$", re.M)
# The numbered-and-named format label of a multi-format construct ("Format 1 (replacing):"), §5.2.1.
FMT_LABEL = re.compile(r"^\s*Format\s+\d+", re.M)
# PAGE-BREAK FURNITURE. At every printed page boundary the transcription injects a running header, and it is
# emitted AS A HEADING — inside rule blocks. Treating it as a block terminator truncates every rule block that
# spans a page break (it cost 896 rules when first tried). Skip it instead. Three transcribed shapes, the same
# three scripts/fix_spec_pagebreaks.py had to learn the hard way; missing the bare form is the dangerous one.
RUNNING_HEADER = re.compile(r"^#{1,6}\s*(?:\*\*)?ISO/IEC\s+1989:2023\s*\(E\)(?:\*\*)?\s*$")
# ALL page-break furniture, in every transcribed shape. These lines land INSIDE rule prose at a page boundary and
# were being absorbed as rule TEXT: 592 of 3,247 rules (18%) carried an anchor, a rule, a running header or a
# licence footer spliced into the normative sentence. A rule whose text is contaminated is worse than a missing
# one — it reads as authoritative and would be cited that way. Skipped at the line level, before buffering.
FURNITURE = re.compile(
    r"^\s*(?:"
    r"-{3,}"                                              # the --- separators around a page block
    r'|<a\s+id="[^"]*"></a>'                              # ANY anchor — page-N and section-N-N both leak in
    r"|(?:\*\*)?ISO/IEC\s+1989:2023\s*\(E\)(?:\*\*)?"    # running header: bold or bare (no leading #)
    r"|(?:\d+\s+)?(?:©|\(c\))?\s*ISO/IEC\s+2023"        # copyright footer
    r"|Licensed to .*"                                    # licence footer
    r")\s*$", re.I)
# ⛔ THE IN-CLAUSE GROUP LABEL — the standard's run-in sub-heading INSIDE a rule list ("SEQUENTIAL FILES",
# "RELATIVE FILES", "FORMAT 3", "ALL FORMATS", "FORMATS 1, 2 AND 4", "CLASS SPECIFIER"). It scopes the rules that
# follow it and is NEITHER a heading NOR rule prose, which is exactly the pair of mistakes this project made with
# it (fix-queue PB689):
#   · read as a HEADING it TRUNCATES the block. `specs/ISO_COBOL.md` rendered nine of them as `## FORMAT n` /
#     `## SEQUENTIAL FILES`; the block scan closes at any heading, so §14.9.51.4 (WRITE) contributed 16 of its 42
#     general rules and the traceability inventory's denominator — the number v1.0 is defined against (D13) —
#     was silently short by 60 rows across five clauses. GR-14.9.51.4-23, the line-sequential '71' status, was
#     one of the rules that had no row to verdict.
#   · read as PROSE it CONTAMINATES the preceding rule: seven catalog rows ended "... is set to '44'. RELATIVE
#     FILES" — the same splicing FURNITURE above exists to prevent.
# THE PRINTED PAGE SETTLES WHAT IT IS. Measured in the PDF text layer on pp146, 463, 609, 680 and 820: every one
# is Cambria 10.7pt flags=4 at the rule-number margin — byte-identical typography to the rule text around it —
# where a real clause heading is Cambria,Bold 11.7pt flags=20 ("14.9.51.4  General rules", p817). So the standard
# prints no heading here, and the transcription's own convention agrees: 209 of the 218 labels are plain lines.
# ⛔ KEYED ON SHAPE, NOT ON A VOCABULARY. A list of label spellings is the hand-maintained-list anti-pattern and
# would have to grow for every new one; the shape was DERIVED by measurement instead — 301 all-caps standalone
# lines inside rule blocks, 37 distinct, and every single one is a group label (zero false positives). The
# optional `#` prefix is matched so a transcription that heading-ifies one is absorbed rather than obeyed; those
# are reported as transcription defects to repair (see `heading_labels` below), never silently tolerated.
GROUP_LABEL = re.compile(r"^(?:#{1,6}\s+)?(?P<label>[A-Z][A-Z0-9 \-/&,]*)\s*$")
# A top-level rule ordinal at the start of a line. The transcription uses BOTH "1)" and "1." — an earlier version
# matched only "1)" and silently dropped every rule in §8.8.3.2, §11.9.11.2, §13.18.24.3 and §14.9.30.3. The
# completeness self-check is what surfaced it; without it the denominator would have been quietly short.
# Sub-items ("a)") are indented and stay with their parent rule.
# The label's delimiter is ESCAPED in the transcription (`1\)`, `1\.`) because an unescaped `1)` at column 0
# is a Markdown ordered-list marker — it renders as `1.`, which collides with the standard's own third level,
# and it re-parses the rule's text as block syntax, which ate the leading `>` of eight rules. The backslash is
# optional here so the catalog reads both forms: this regex is the DENOMINATOR of the P14 traceability
# inventory, and a silent zero-rule extraction would read as "no rules in this clause" rather than as a break.
# ⛔ The DELIMITER FORM (")" vs ".") is captured because it is load-bearing (fix-queue PB39): §15.68.3 writes its
# top-level rules `1\)` … and its nested list `1.` …, and the form — not any arithmetic on the ordinals — is what
# separates them. DEVLOG 1167 proved every ordinal-only heuristic wrong on that clause.
ORDINAL = re.compile(r"^(?P<n>\d+)\\?(?P<form>[.)])\s+(?P<text>.*)$")

# An INDENTED numbered item inside a rule's body — the standard's nested lists ("GR 4 a) 1. 2. 3. …").
#
# ⛔ THE ROW SHAPE, DECIDED FOR THE WHOLE CATALOG BY THE OWNER (kb/Work PB55, 2026-08-08, "THE RULE IS THE
# UNIT"): **one row per TOP-LEVEL numbered rule of a rule block; every sub-item — lettered `a)`, or numbered
# under one, at any depth — is FOLDED into its parent rule's text and is addressed by CITATION PATH, never by a
# row of its own.** ⚠ That decision's WAVE (absorbing the 200-odd remaining `L` rows of lists the transcription
# flattened whole) is owner-scheduled for AFTER the defect burn-down, and this file does not run it: what is
# enforced here is only that an indentation ACCIDENT can no longer decide the shape (kb/Work PB698). Three
# independent things said the same as the owner and the extractor was the only dissenter:
#   · the standard's own cross-references address a sub-item as a PATH ON ITS PARENT — "General rule 12c)",
#     "General rule 4b)", 21 such citations — and never renumber one into the top-level sequence;
#   · `scripts/spec/cite.py` resolves that path (`--check 14.9.41.3 "…"` → `6) b) 2.`), so folding costs a
#     sub-item NOTHING in citability;
#   · `scripts/spec/audit_catalog_coverage.py` states the invariant outright in `printed_ordinals`: "Sub-items
#     ('a)', indented '1.') are not column-0 and are not counted; they are part of their parent rule."
# A row whose text cannot be read without its parent is not a rule but a FRAGMENT — the row once at
# `SR-14.9.41.3-L2.2` read "It has the same class, category, and usage as that record key.", whose subject and
# referent both live in the item above it. That is the same silent-false-confidence class as PB689's
# contamination, in the other direction.
#
# The transcription flattened the TAIL of some nested lists onto column 0 at a PAGE BREAK (§14.9.41.3 SR6 b) 2.
# and 3. are printed indented under b) on PDF page 785 / folio 755 — the ordinal glyph measured at x0 115.05 on
# a page whose body margin is 80.04 — and were transcribed at column 0). All 23 such items, in 14 clauses, were
# measured on the PDF and REPAIRED in the transcription by PB698, so this arm catches nothing today and is a
# net for the next one. Such a tail is EVIDENCE the segmenter needs: a column-0 ordinal that CONTINUES
# the most recent indented run, and does NOT continue the innermost open level, is that nested list's tail, so it
# is buffered into the parent rule exactly as its indented siblings are — the indentation accident can no longer
# decide what a row IS. Every one is REPORTED (fatal under `--check`) so the transcription gets repaired rather
# than quietly worked around, which is the same posture `heading_labels` takes for PB689's group labels.
INDENTED_ORDINAL = re.compile(r"^\s+(?P<n>\d+)\\?[.)]\s")

# The rule-block kinds the standard uses. Intrinsic functions use argument/returned-value rules instead of SR/GR.
# SINGULAR forms are load-bearing, not a nicety: the standard writes "Syntax rule" when a block holds exactly one
# (§10.6.3, §11.9.4, §13.4.3, …). A plural-only map skipped 15 whole blocks — invisible to the empty-block check,
# because a block that is never RECOGNISED is never counted as seen. The TOC cross-check below is what caught it.
# ⛔ KEYED ON THE SINGULAR CANONICAL FORM ONLY — every plural variant is normalised into it by `kind_of`.
# The literal-spelling map came back a SECOND time: it carried "argument rules"/"argument rule" and still missed
# §15.10.3, §15.13.3 and §15.100.3, which the standard heads "Argument**s** rule(s)". Ten normative rules were
# absent from the denominator, and the burn-down was measuring against a total that was short — the exact "silent
# false confidence" DESIGN-spec-conformance-review.md §7 names. Adding two more spellings would have set up the
# third occurrence, so the pluralisation is now normalised away and the unrecognised-heading check below fails loudly
# on any rule-shaped heading this map does not know.
#
# ⛔ "Editing rules" AND "Precedence rules" ARE NOT A THIRD AND FOURTH KIND — the standard classifies them
# ITSELF, and this map records that classification rather than inventing vocabulary for it:
#   §5.3.3  "The rules of the PICTURE clause specified in 13.18.40.5, Editing rules, are General rules."
#   §5.3.2  "The rules of the PICTURE clause specified in 13.18.40.6, Precedence rules, are syntax rules."
# (both validated with `scripts/spec/cite.py --check`). So this is the SAME defect class as the pluralisation
# above — a heading spelling the map did not know — not a change to what the denominator MEANS. 17 rules, all
# core PICTURE editing: 15 GR + 2 SR. Owner decision 2026-07-30.
# §5.3 "Rules" itself stays OUT and is the reason the two above come in: it is the clause that DEFINES the
# taxonomy (§5.3.1–5.3.3 is prose about what syntax and general rules are) and carries no numbered rules of its
# own, so there is nothing there to extract.
KINDS = {
    "syntax rule": "SR",
    "general rule": "GR",
    "argument rule": "AR", "argument": "AR",
    "returned value rule": "RV", "returned value": "RV",
    "editing rule": "GR",       # §13.18.40.5 — typed by §5.3.3, not a new kind
    "precedence rule": "SR",    # §13.18.40.6 — typed by §5.3.2, not a new kind
}

#: Heading titles that name a rule block in some spelling. Anything matching this and NOT resolving through
#: KINDS is a block the extractor would silently skip.
RULE_SHAPED = re.compile(r"\brules?\b", re.IGNORECASE)

#: Rule-shaped headings DELIBERATELY outside the denominator, each with the reason it carries no extractable
#: rule. ⛔ A guard that reports an expected condition on every run is a guard people learn to ignore — the same
#: cry-wolf failure §0 names for line-number code-locations — so a decided exclusion is recorded HERE rather than
#: left to reappear as a warning forever. Anything NOT in this dict still fails loudly.
EXCLUDED_BLOCKS = {
    "5.3": "defines the rule taxonomy itself (§5.3.1–5.3.3 is prose about what syntax and general rules ARE) "
           "and carries no numbered rules of its own — it is the clause that CLASSIFIES §13.18.40.5/.6, which "
           "is why those two are in KINDS. Owner decision 2026-07-30.",
}

#: Recognised rule blocks that legitimately yield ZERO numbered rules, so "0 rules" is the correct answer rather
#: than a parse failure. Same cry-wolf argument as EXCLUDED_BLOCKS.
KNOWN_EMPTY_BLOCKS = {
    "15.4": "its numbered rules live in the SUB-CLAUSE §15.4.1 'Numeric and integer functions', whose prose "
            "heading KINDS does not recognise — so this heading resolves to RV and correctly yields nothing of "
            "its own. ⛔ THIS WAS AN UNOWNED PARSE GAP FOR MONTHS and the content-keyed scan explained it "
            "(fix-queue PB29, 2026-08-04): the same defect class as §8.8.1, one level down. §15.4.1 is now "
            "ADMITTED in spec-unharvested-rule-blocks.json and contributes its 6 RV rules — including r1, 'the "
            "returned value shall equal the value of the equivalent arithmetic expression', which PB38 and the "
            "D3 correction both turn on. Declared here rather than left as a warning forever.",
    "13.18.40.6": "the PICTURE precedence rules are UNNUMBERED prose plus Table 10 and Table 11 — the normative "
                  "content IS the two precedence matrices, which have no per-rule ordinal to extract. Typed SR "
                  "by §5.3.2 so the heading resolves; it contributes no rows. Owner decision 2026-07-30.",
}


def canonical_title(title: str) -> str:
    """Lower-case, de-punctuated, and SINGULARISED on both words of the block name.

    The standard is inconsistent in both positions independently — "Syntax rule" / "Syntax rules" /
    "Argument rules" / "Arguments rules" / "Arguments rule" all occur — so canonicalising is the only form that
    does not need a new map entry per combination.
    """
    t = title.strip().lower().rstrip(".")
    t = re.sub(r"\brules\b", "rule", t)
    t = re.sub(r"\barguments\b", "argument", t)
    t = re.sub(r"\bvalues\b", "value", t)      # §15.69.4 "Returned values rules" — a THIRD pluralisation axis
    return t


def kind_of(title: str) -> str | None:
    return KINDS.get(canonical_title(title))


def unharvested_rule_blocks(lines: list[str], titles: dict[str, str]) -> dict[str, dict]:
    """Every clause whose BODY carries an ascending ``N)`` list that this extractor does not harvest.

    ⛔ THE THIRD INSTANCE OF ONE DEFECT CLASS, AND THE FIRST DETECTOR THAT CAN SEE IT (fix-queue PB29).
    The denominator has been short twice — 3,790 → 3,846 (heading spellings the literal map did not know) →
    3,861 (§13.18.40.5/.6, typed by §5.3) — and both fixes extended a HEADING-keyed guard. ``RULE_SHAPED``
    reports a heading containing the word "rule" that ``KINDS`` cannot resolve, which is exactly why it cannot
    see this one: §8.8.1.2 is headed *"Native, standard-binary, and standard-decimal arithmetic"* and its seven
    numbered rules sit under ordinary prose. A guard keyed on what a block is CALLED can never find a block that
    is called something else, so this one is keyed on what a block CONTAINS.

    ⚠ THE RESULT IS AN UPPER BOUND, NOT A DEFECT COUNT, AND IT MUST NOT BE ADDED TO THE DENOMINATOR WHOLESALE.
    The standard numbers ordinary prose enumerations the same way it numbers rules — §8.3.2.2 "User-defined
    words" lists the KINDS of user-defined word, not fifteen requirements. Admitting all of them would be the
    mirror of the short denominator, and the denominator defines v1.0 (D13). So each clause is adjudicated and
    recorded in the committed manifest beside this script; this function only MEASURES.
    """
    found: dict[str, dict] = {}
    cur: tuple[str, str] | None = None
    body: list[str] = []

    def close() -> None:
        if cur is None:
            return
        num, title = cur
        # ⛔ COUNT THE LISTS, NOT ONLY THE ITEMS — the distinction is the whole triage signal, and reporting only
        # a total actively misleads. §8.3.2.2 "User-defined words" reports 15 items, which reads like a numbered
        # rule block; they are FIVE nested sub-lists inside prose, each restarting at 1 (1-2-3, 1-2, 1-6, 1-2,
        # 1-2). A rule block is normally ONE ascending list, so `lists == 1` is weak evidence FOR admitting the
        # clause and `lists > 1` is weak evidence for a prose enumeration — neither is proof, which is why each
        # clause is still adjudicated, but the shape is what makes 100 clauses triageable at all.
        # The real rule-block parser above tracks the same phenomenon as `sublist`; this mirrors it.
        ordinals, lists, last = 0, 0, 0
        for ln in body:
            if m := ORDINAL.match(ln):
                n = int(m.group("n"))
                if n == 1:
                    lists, last = lists + 1, 1
                    ordinals += 1
                elif n == last + 1:              # ascending within the current list
                    ordinals, last = ordinals + 1, n
                # anything else is a stray "3)" mid-prose and is not counted
        if ordinals and kind_of(title) is None:
            found[num] = {"title": title, "ordinals": ordinals, "lists": lists, "longest": max_run(body)}

    def max_run(lines_: list[str]) -> int:
        """The longest single ascending run — the size of the biggest candidate rule list in the clause."""
        best, run, last = 0, 0, 0
        for ln in lines_:
            if m := ORDINAL.match(ln):
                n = int(m.group("n"))
                if n == 1:
                    run, last = 1, 1
                elif n == last + 1:
                    run, last = run + 1, n
                else:
                    continue
                best = max(best, run)
        return best

    for line in lines:
        if ANY_HEADING.match(line):
            if (m := HEADING.match(line)) and not line.lstrip().startswith("["):
                close()
                cur, body = (m.group("num"), m.group("title")), []
                continue
            close()
            cur, body = None, []
            continue
        if cur:
            body.append(line)
    close()
    return found


def subject_for(num: str, titles: dict[str, str]) -> str:
    """The nearest enclosing ancestor section that is not itself a rule block — the construct being ruled."""
    parts = num.split(".")
    for cut in range(len(parts) - 1, 0, -1):
        parent = ".".join(parts[:cut])
        title = titles.get(parent)
        if title and kind_of(title) is None:
            return title
    return titles.get(num, "?")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="parse only; exit non-zero if parse gaps exist")
    args = ap.parse_args()

    # The Windows console defaults to cp1252 and cannot encode the section sign or the warning glyph.
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001 - a reporting nicety must never fail the extraction
        pass

    if not SPEC.exists():
        sys.exit(f"spec not found: {SPEC}\nRun: git submodule update --init --recursive")

    # The clauses ADMITTED to the denominator by adjudication (PB29) — section ⇒ declared rule kind. Read from
    # the manifest so admitting a clause is a data edit with a recorded reason, never a code edit.
    # ⛔ A kind need not be uniform across a clause, and the two non-uniform shapes are EXPLICIT so they cannot
    # be confused (DEVLOG 1166 recorded the limit; these forms are its removal):
    #   "SR"                                   — every rule in the clause is this kind;
    #   {"sublists": {"1": "SR", "2": "GR"}}   — kind per SUB-LIST (§8.3.2.2: five normative sub-lists, two
    #                                            constrain where words may be WRITTEN ⇒ SR per §5.3.2, three
    #                                            state effect or treatment ⇒ GR per §5.3.3);
    #   {"items": {"1": "SR", "4": "GR"}}      — kind per ITEM of a SINGLE list (§8.3.3.3.2: items 1-3
    #                                            constrain the literal's written form ⇒ SR, item 4 states its
    #                                            value ⇒ GR). Requires the clause to segment as one list.
    # A sub-list or item the map does not know is a manifest/segmentation DISAGREEMENT and is FATAL at flush —
    # silently guessing a kind is the mis-typing this mechanism exists to prevent.
    admitted: dict[str, str | dict] = {}
    if UNHARVESTED.exists():
        for sec, info in json.loads(UNHARVESTED.read_text(encoding="utf-8"))["blocks"].items():
            if info.get("disposition") == "rules":
                admitted[sec] = info["kind"]

    lines = SPEC.read_text(encoding="utf-8", errors="replace").splitlines()
    titles: dict[str, str] = {}

    # Pass 1 — every numbered heading, so a rule block can name its subject.
    for line in lines:
        if (m := HEADING.match(line)) and not line.lstrip().startswith("["):
            titles.setdefault(m.group("num"), m.group("title"))

    # Pass 2 — walk rule blocks and collect ordinals.
    rules: list[dict] = []
    blocks_seen: list[tuple[str, str]] = []      # (section-number, kind)
    blocks_with_rules: set[tuple[str, str]] = set()

    page = 0
    cur: tuple[str, str] | None = None           # (section-number, kind) of the block being read
    buf: list[str] = []
    ordinal = 0
    # A rule block can contain MORE THAN ONE numbered list — the standard restarts numbering under an unheaded
    # sub-list (e.g. §7.2.3.4 General rules runs 1..n for COPY, then restarts 1..n for text-word matching). Keyed
    # on (kind, section, ordinal) alone, 173 rules collided and silently overwrote each other — which would have
    # carried the WRONG verdict forward on every inventory refresh. Track the sub-list and qualify the id.
    #
    # ⛔ SEGMENTATION IS DECIDED BY THE DELIMITER FORM AND THE INDENTED-RUN EVIDENCE, NEVER BY ORDINAL ARITHMETIC
    # ALONE (fix-queue PB39, DEVLOG 1167). The old rule — "an ordinal that goes backwards opens a sub-list, and
    # the sub-list never returns to its parent" — filed 326 top-level rules under sub-list ids the standard does
    # not use (SR-12.3.7.3-L7.18 for what the standard calls "§12.3.7.3 syntax rule 18"). The mechanism, read
    # from the transcription itself:
    #   · whatever delimiter form a run opened with is that level's form; a change of form opens a NEST
    #     (§15.68.3: `1\)..5\)`, nested `1...7.`, then `6\) 7\)` returning to the top level);
    #   · EXCEPT that the transcription also drifts form mid-run — §13.18.40.3 writes SR 1-5 as `N.` and SR 6-37
    #     as `N\)` — so an ordinal that STRICTLY CONTINUES an open level's run (n == last+1) belongs to that
    #     level regardless of form, preferring a form-matched level when two continue with the same n (§9.3.6);
    #   · EXCEPT that a column-0 ordinal continuing the most recent INDENTED run is the flattened TAIL of that
    #     nested list, not a rule (§14.9.13.4 GR4a: `1\.-3\.` indented, `4.-6.` at column 0; §15.30.3 GR2b).
    #     ⛔ That case never reaches `place()` any more — it is decided in the scan loop by `flattened_tail`
    #     and BUFFERED INTO THE PARENT RULE (fix-queue PB698). It used to open a sub-list here, which gave 27
    #     sub-item FRAGMENTS rows of their own across 14 clauses while every other sub-item in the standard was
    #     folded, so an indentation accident at a page break was deciding what a row IS.
    # A stack of open levels [form, sublist-index, last-ordinal] implements this; `levels[0]` is the block's top
    # level (sublist 1, plain ids). Anything the four cases above cannot place is counted in `unexplained` and
    # FAILS --check: an unexplained ordinal is a new segmentation shape, and silently guessing at it is how the
    # 326 wrong ids happened the first time.
    levels: list[list] = []                      # [form, sublist-index, last-ordinal], innermost last
    next_sublist = 2
    last_indented = 0
    cur_sublist = 1
    cur_kinds: dict[str, str] | None = None      # per-sublist kind map for a MIXED-kind admitted block
    unexplained: list[tuple[str, int, str]] = []   # (section, ordinal, why)
    #: In-clause group labels the TRANSCRIPTION rendered as markdown headings — absorbed here so they cannot
    #: truncate a block, and reported so the transcription gets repaired rather than quietly worked around.
    heading_labels: list[tuple[str, str]] = []     # (section, label)
    #: Nested-list tails the TRANSCRIPTION flattened onto column 0 — folded into their parent rule here so the
    #: row shape is indentation-independent, and reported so the transcription gets repaired (fix-queue PB698).
    flattened_tails: list[tuple[str, int, int, str]] = []   # (section, line-number, ordinal, text)
    # The page a rule STARTS on. flush() runs when the NEXT ordinal or heading arrives, by which time a
    # "## Page N" marker may already have advanced `page` — so flushing with the live value mis-attributes the
    # last rule of every block that ends near a page boundary (verified against the rendered PDF: GR-7.3.12.4-6
    # is printed on page 95 and was recorded as 96).
    rule_page = 0
    in_correction_note = False                   # inside a `> ⚠ **CORRECTED …` blockquote paragraph (R66)

    def flattened_tail(n: int, form: str) -> bool:
        """Is this column-0 ordinal the flattened TAIL of the nested list currently open in the rule's body?

        The precedence is `place()`'s, unchanged: a strict same-form continuation of the innermost open level is
        a real rule and wins outright (that is what keeps `GR3` with an indented `1.` to `3.` list from
        swallowing `4)`), and only then does an ordinal that continues the indented run count as the tail.
        """
        if not levels or not last_indented:
            return False
        form0, _sublist, last = levels[-1]
        if form == form0 and n == last + 1:
            return False
        return n == last_indented + 1

    def place(n: int, form: str) -> int:
        """File a column-0 ordinal on the level stack; returns the sub-list index for its id."""
        nonlocal levels, next_sublist, last_indented
        if not levels:
            levels = [[form, 1, n]]
            last_indented = 0
            return 1
        top = levels[-1]
        # 1. strict same-form continuation of the innermost open level
        if form == top[0] and n == top[2] + 1:
            top[2] = n
            last_indented = 0
            return top[1]
        # 2. (was: the flattened tail of an indented nested list — now handled by `flattened_tail` in the scan
        #    loop, which folds it into the parent rule instead of giving the fragment a row. PB698.)
        # 3. a same-form restart — a new sub-list, nested on the stack so its parent's run stays open
        if form == top[0] and n <= top[2]:
            levels.append([form, next_sublist, n])
            next_sublist += 1
            last_indented = 0
            return levels[-1][1]
        # 4. an ordinal that strictly continues an OPEN level's run closes every level above it and continues
        #    it — the form may drift (§13.18.40.3). Form-matched levels win over form-drifted ones (§9.3.6).
        for form_matched in (True, False):
            for i in range(len(levels) - 1, -1, -1):
                if n == levels[i][2] + 1 and (levels[i][0] == form) == form_matched:
                    del levels[i + 1:]
                    levels[i][0] = form
                    levels[i][2] = n
                    last_indented = 0
                    return levels[i][1]
        # 5. an opening ordinal — a new nest
        if n == 1:
            levels.append([form, next_sublist, n])
            next_sublist += 1
            last_indented = 0
            return levels[-1][1]
        # 6. unexplained — filed as a nest so nothing is dropped, and reported loudly (fatal under --check)
        unexplained.append((cur[0] if cur else "?", n,
                            f"{n}{form} continues no open level "
                            f"(levels={[(f, s, l) for f, s, l in levels]}, indented-run last={last_indented})"))
        levels.append([form, next_sublist, n])
        next_sublist += 1
        last_indented = 0
        return levels[-1][1]

    def flush() -> None:
        nonlocal buf, ordinal
        if cur and ordinal and (text := " ".join(x.strip() for x in buf).strip()):
            sec, kind = cur
            if cur_kinds is not None:
                if "sublists" in cur_kinds:
                    kind = cur_kinds["sublists"].get(str(cur_sublist))
                elif "items" in cur_kinds:
                    kind = cur_kinds["items"].get(str(ordinal)) if cur_sublist == 1 else None
                else:
                    kind = None
                if kind is None:
                    # A sub-list or item the map does not know is a manifest/segmentation DISAGREEMENT —
                    # writing a guessed kind would be the silent mis-typing the dict form exists to prevent.
                    sys.exit(f"FATAL: {sec} sub-list {cur_sublist} ordinal {ordinal} has no kind in its "
                             f"per-sublist/per-item manifest entry ({json.dumps(cur_kinds)}). Re-adjudicate "
                             f"the clause against the current segmentation before regenerating.")
            rid = f"{kind}-{sec}-{ordinal}" if cur_sublist == 1 else f"{kind}-{sec}-L{cur_sublist}.{ordinal}"
            rules.append({
                "id": rid,
                "section": sec,
                "kind": kind,
                "ordinal": ordinal,
                "sublist": cur_sublist,
                "subject": subject_for(sec, titles),
                "text": re.sub(r"\s+", " ", text),
            })
            blocks_with_rules.add(cur)
        buf, ordinal = [], 0

    for line_no, line in enumerate(lines, start=1):
        if m := PAGE.match(line):
            page = int(m.group(1))
            continue
        # ANY heading closes the current rule block. Terminating only on a NUMBERED heading let §16.2.2.2 — the
        # last rule block in the standard's body — run straight through the un-numbered annex headings ("## Annex
        # A (normative)"), turning every "N." line in Annexes A-F into a fake rule: 601 phantoms in one block.
        # A block that never ends is as wrong as a block never seen, and inflates the denominator instead of
        # shortening it, which is the more dangerous direction because it looks like thoroughness.
        if RUNNING_HEADER.match(line) or FURNITURE.match(line):
            continue
        # ⛔ AN IN-CLAUSE GROUP LABEL IS NOT A BLOCK BOUNDARY AND IS NOT RULE TEXT (fix-queue PB689). It is
        # skipped in ONE place, before both the heading branch and the buffering branch, so the truncation and
        # the contamination cannot come back independently. Only inside an open block: outside one, an all-caps
        # heading is a document division ("## BIBLIOGRAPHY") and must keep terminating nothing.
        if cur is not None and (gl := GROUP_LABEL.match(line)):
            if line.lstrip().startswith("#"):
                heading_labels.append((cur[0], gl.group("label")))
            continue
        if ANY_HEADING.match(line) and not HEADING.match(line):
            flush()
            cur = None
            continue
        if (m := HEADING.match(line)) and not line.lstrip().startswith("["):
            flush()
            num, title = m.group("num"), m.group("title")
            # ⛔ A CLAUSE MAY BE A RULE BLOCK WITHOUT ITS HEADING SAYING SO, AND THAT IS DECLARED IN DATA, NOT
            # HERE (fix-queue PB29). §8.8.1.2 is headed "Native, standard-binary, and standard-decimal
            # arithmetic" and carries seven numbered rules, two of which PB28 proved the compiler violates —
            # §5.3.1 settles that they ARE rules ("Except for intrinsic functions, rules are categorized as
            # syntax rules and general rules", validated with cite.py). Adding such titles to KINDS would be a
            # hand-maintained list of prose headings, which is the shape this project keeps deleting; the
            # ADMITTED set lives in `spec-unharvested-rule-blocks.json` beside the reason each was admitted, and
            # the same file's `pending` rows are the worklist of clauses nobody has judged yet.
            k = kind_of(title) or admitted.get(num)
            levels, next_sublist, last_indented, cur_sublist = [], 2, 0, 1
            if isinstance(k, dict):
                # Per-sublist kinds: cur carries a hashable token for the seen/with-rules bookkeeping; the
                # real kind is resolved per rule at flush() from cur_kinds.
                cur, cur_kinds = (num, "MIXED"), k
                blocks_seen.append(cur)
            elif k:
                cur, cur_kinds = (num, k), None
                blocks_seen.append(cur)
            else:
                cur, cur_kinds = None, None
            continue
        if cur is None:
            continue
        # ⛔ AN EDITORIAL CORRECTION NOTE IS NOT RULE TEXT (R66). `> ⚠ **CORRECTED — see the Addendum (Cn).**` flags a
        # line the transcription corrects and QUOTES THE PRINTED FORM so cite.py finds either; buffered into the rule it
        # would put the defective printed text inside the catalog's rule text, beside the corrected one. The note is a
        # blockquote paragraph: every `>` line after its first belongs to it.
        if CORRECTION_NOTE.match(line) or (in_correction_note and line.startswith(">")):
            in_correction_note = True
            continue
        in_correction_note = False
        if m := ORDINAL.match(line):
            n = int(m.group("n"))
            # ⛔ A FLATTENED NESTED-LIST TAIL IS PART OF ITS PARENT RULE, NOT A ROW (PB698). Decided BEFORE the
            # flush, because flushing is what would have ended the parent rule and started a new one.
            if ordinal and flattened_tail(n, m.group("form")):
                flattened_tails.append((cur[0], line_no, n, m.group("text")[:80]))
                last_indented = n
                buf.append(line)
                continue
            flush()
            cur_sublist = place(n, m.group("form"))
            ordinal = n
            rule_page = page
            buf = [m.group("text")]
        elif ordinal:
            if im := INDENTED_ORDINAL.match(line):
                last_indented = int(im.group("n"))
            buf.append(line)
    flush()

    # ---- completeness self-check -----------------------------------------------------------------------------
    # §5.3.2-5.3.5 are the CONVENTIONS clause: prose DEFINING what a syntax/general/argument/returned-value rule
    # is. They are rule-block headings that correctly contain no rules. Every other empty block is a parse gap.
    EXPECTED_EMPTY = {"5.3.2", "5.3.3", "5.3.4", "5.3.5"}
    gaps = [b for b in blocks_seen if b not in blocks_with_rules and b[0] not in EXPECTED_EMPTY]

    # ---- General formats — the SYNTAX DIAGRAMS ---------------------------------------------------------------
    # These are the artifacts the ANTLR grammar was written FROM, and the ones transcription loss damages most
    # (a dropped choice-indicator bar makes legal source look illegal). They carry no numbered ordinals, so the
    # rule scan never saw them and the denominator omitted all 320. Each becomes one FMT row: the unit of
    # "verify this diagram against the grammar rule that implements it".
    #
    # ⛔ A ROW IS A FORMAT THE CLAUSE PRINTS, NOT A HEADING THAT SAYS "General formats" (kb/Work PB1536 Q2). The
    # scan used to emit a row for ANY heading so titled, and §5.2 — the description-techniques clause ABOUT
    # general formats, whose only body is its own sub-clause §5.2.1 — manufactured FMT-5.2: a GAP row whose
    # "text" was a bare anchor, which no grammar rule could ever implement. So a candidate heading yields a row
    # only when its body carries what §5.2.1 says a general format IS: a diagram, or the numbered-and-named
    # "Format N" labels of a multi-format construct ("the general format is separated into multiple formats that
    # are numbered and named" — cite.py --check 5.2.1 → OK). A candidate without either is one of two shapes,
    # and they are told apart structurally rather than by a list of section numbers:
    #   · a PARENT clause — its next numbered heading is its own sub-clause (§5.2 → §5.2.1): the clause is a
    #     topic, not a construct's format. Skipped, and printed, so the exclusion is an observation.
    #   · a LEAF whose diagram the TRANSCRIPTION LOST (§15.78.2 REVERSE printed its format as bare text until
    #     wave 60): still a row — dropping it would shrink the denominator silently — and a LOUD self-check
    #     failure (`--check` exits 1), because the grammar cannot be verified against a diagram that is not there.
    # `CatalogCoverageDriftTests.EveryFmtRow_CarriesADiagramOrAFormatLabel` holds the same bar over the written
    # catalog, independently of this code.
    fmt_sections: list[tuple[str, int]] = []   # (section, heading-line)
    numbered_at: list[tuple[int, str]] = []    # every numbered heading, in order — to see a candidate's next one
    for i, line in enumerate(lines):
        if (m := HEADING.match(line)) and not line.lstrip().startswith("[") and not PAGE.match(line):
            numbered_at.append((i, m.group("num")))
            if re.fullmatch(r"general formats?\.?", m.group("title").strip(), re.I):
                fmt_sections.append((m.group("num"), i))
    next_numbered = {at: nxt for (at, _), (_, nxt) in zip(numbered_at, numbered_at[1:])}

    fmt_parents: list[str] = []     # candidate headings that are clauses ABOUT formats, not a construct's format
    fmt_undrawn: list[str] = []     # leaf format clauses whose transcription carries no diagram — a repair
    for sec, at in fmt_sections:
        body: list[str] = []
        for line in lines[at + 1:]:
            if ANY_HEADING.match(line) and not RUNNING_HEADER.match(line) and not PAGE.match(line):
                break
            body.append(line)
        text = "\n".join(body).strip()
        has_diagram = bool(FMT_DIAGRAM.search(text))
        labels = len(FMT_LABEL.findall(text))
        if not has_diagram and not labels:
            if next_numbered.get(at, "").startswith(sec + "."):
                fmt_parents.append(sec)
                continue
            fmt_undrawn.append(sec)
        rules.append({
            "id": f"FMT-{sec}", "section": sec, "kind": "FMT", "ordinal": 0, "sublist": 1,
            "subject": subject_for(sec, titles),
            # How many numbered Formats the section declares — Format 1 / Format 2 usually map to DIFFERENT
            # grammar alternatives, so this is a size signal for the verification work, not decoration.
            "formats": labels or 1,
            "has_diagram": has_diagram,
            "has_figure_notes": "Figure notes" in text,
            "text": re.sub(r"\s+", " ", text)[:4000],
        })

    # ---- Annex A.1 — the implementor-defined language element list -------------------------------------------
    # D13 defines "100% conforming" as the mandatory core complete PLUS every required implementor-documentation
    # item, so these ARE inventory rows and were missing from the denominator entirely. The heading is
    # "### **A.1 ...**" — bold inside the heading, so it carries no leading digit and the numbered-heading scan
    # never saw it. Items are "N) <header> (<paraphrase>). This item is <required|optional|conditionally
    # required>. This item [, if provided ...,] shall be documented ... (<cross-reference>)".
    a1_start = next((i for i, l in enumerate(lines) if re.match(r"^#{2,6}\s+\*{0,2}A\.1\b", l)), None)
    a1_items = 0
    if a1_start is not None:
        page = 0
        for line in lines[:a1_start]:
            if m := PAGE.match(line):
                page = int(m.group(1))
        buf, ordinal, rule_page = [], 0, page

        def flush_a1() -> None:
            nonlocal buf, ordinal, a1_items
            if ordinal and (text := re.sub(r"\s+", " ", " ".join(x.strip() for x in buf)).strip()):
                # Classify from the item's OWN words. Three items (28, 176, 200) do not use the standard
                # "This item is X" clause, and defaulting them to "required" silently inflated that bucket by
                # exactly 3 — the discrepancy against DEVLOG 932's independent count. An item that states no
                # requirement is marked UNCLASSIFIED and surfaced, never bucketed by assumption.
                low = text.lower()
                if "conditionally required" in low or "this item is conditional" in low:
                    req = "conditionally required"
                elif "this item is optional" in low or "feature is optional" in low:
                    req = "optional"
                elif "this item is required" in low:
                    req = "required"
                else:
                    req = "unclassified"
                rules.append({
                    "id": f"DOC-A.1-{ordinal}", "section": "A.1", "kind": "DOC", "ordinal": ordinal,
                    "sublist": 1, "subject": text.split(".")[0][:120],
                    "requirement": req, "documented": "shall be documented" in low, "text": text,
                })
                a1_items += 1
            buf, ordinal = [], 0

        for line in lines[a1_start + 1:]:
            if m := PAGE.match(line):
                page = int(m.group(1))
                continue
            if RUNNING_HEADER.match(line) or FURNITURE.match(line):
                continue
            if ANY_HEADING.match(line):          # A.2 (or any later annex heading) ends the list
                flush_a1()
                break
            if m := ORDINAL.match(line):
                flush_a1()
                ordinal, rule_page = int(m.group("n")), page
                buf = [m.group("text")]
            elif ordinal:
                buf.append(line)
        else:
            flush_a1()

    # ---- integrity critic: the transcription must contain the STANDARD, not a description of it -------------
    # Page 28 was found to hold an AI-generated SUMMARY of the Introduction plus a refusal message ("I can quote
    # specific short passages ... but I'm not able to reproduce the full page verbatim"), presented as spec
    # content. That is categorically worse than OCR loss: the text is not the standard at all, so any rule or
    # citation derived from it is unfounded, and it destroyed the sentence naming which annexes are NORMATIVE.
    # A whole-file sweep found this on exactly one page — this check exists so it stays that way.
    #
    # LOUD FAILURE IS THE RULE. A page that cannot be transcribed verbatim is never summarised — it carries the
    # sanctioned marker `<!-- TRANSCRIPTION-FAILED: page N — reason -->` and nothing else. That marker is
    # greppable, counted, and impossible to mistake for the standard, whereas a summary READS like content, which
    # is exactly why page 28's hole survived for months. A known missing page is recoverable; a page quietly
    # replaced by prose ABOUT the page is not. (COBOLNET_DESIGN §1.4 applied to the spec itself.)
    META = [
        (re.compile(r"\bI'?m not able to\b|\bI can quote\b|\bI'?m happy to help\b", re.I), "assistant refusal/offer"),
        (re.compile(r"here is a summary|summary and key excerpts|here are the key", re.I), "summary preamble"),
        (re.compile(r"\bcopyrighted standards document\b|\breproduce the full page\b", re.I), "copyright refusal"),
        (re.compile(r"<!--\s*TRANSCRIPTION-FAILED", re.I), "declared transcription gap (tracked, not hidden)"),
    ]
    meta_hits = []
    # ⛔ RE-KEYED ONTO THE CLAUSE HIERARCHY (the halt this replaces is discharged). Pages were removed from the
    # transcription in 2026-07-27's de-paging, so this builder's per-rule PAGE column had nothing left to record
    # and the script stopped rather than stamp a column of zeroes. The column is simply GONE now: a rule is cited
    # by its clause — 14.9.41.2 GR3, never "page 754" — so the field was dead weight even while pages existed.
    # The internal page bookkeeping below is retained only because the figure/FMT scan shares the loop.
    pg = 0
    for i, line in enumerate(lines):
        if m := re.match(r'^<a id="page-(\d+)"></a>', line):
            pg = int(m.group(1))
            continue
        for pat, label in META:
            if pat.search(line):
                meta_hits.append((pg, i + 1, label, line.strip()[:100]))
                break
    if meta_hits:
        # LOUD in a normal run, FATAL under --check. Deliberate split: the catalog must stay regenerable while
        # the reconciliation ledger is still being collected (repairs come after the sweep), but the GATE must
        # refuse to call the transcription sound while any page of it is not the standard.
        declared = [h for h in meta_hits if h[2].startswith("declared")]
        hidden = [h for h in meta_hits if not h[2].startswith("declared")]
        print(f"\n⚠ {len(meta_hits)} line(s) of NON-SPEC TEXT inside the standard, "
              f"page(s) {sorted({h[0] for h in meta_hits})} "
              f"({len(hidden)} hidden as content, {len(declared)} declared gap(s)).")
        print("  These are NOT the spec — a rule or citation derived from such a page is unfounded:")
        for pg_, ln, label, txt in meta_hits[:15]:
            print(f"      p{pg_} line {ln} [{label}] {txt}")
        if args.check:
            sys.exit(1)

    # ---- completeness critic: the spec's own TOC ------------------------------------------------------------
    # The empty-block check has a BLIND SPOT — it can only verify blocks it RECOGNISED. A block whose heading was
    # never matched (an unhandled title form, or a heading a page break demoted to bold text and stripped of its
    # anchor, which has happened in this transcription) is invisible to it. The TOC is an independent witness of
    # what the printed standard contains, so cross-check against it.
    toc = {(n, k.strip().lower()) for n, k in
           re.findall(r"\[(\d+(?:\.\d+)*)\s+((?:Syntax|General|Argument|Returned value)\s+rules?)\]\(#section-",
                      "\n".join(lines))}
    seen = {(n, (titles.get(n) or "").strip().lower().rstrip(".")) for n, _ in blocks_seen}
    unseen = sorted(toc - seen, key=lambda x: [int(p) for p in x[0].split(".")])
    if unseen:
        print(f"\n✗ FATAL: {len(unseen)} rule block(s) listed in the spec TOC were never parsed as headings.")
        print("  A block that is never recognised is never counted — the denominator would be short and the")
        print("  empty-block check could not see it. Investigate the heading form before trusting this catalog:")
        for n, k in unseen[:20]:
            print(f"      §{n}  {k}")
        sys.exit(1)

    # Rule ids are the inventory's PRIMARY KEY — a duplicate silently carries the wrong verdict forward on every
    # refresh, and drops rules from any per-rule view. This is not a warning; it invalidates the catalog.
    dupes = [rid for rid, n in Counter(r["id"] for r in rules).items() if n > 1]
    if dupes:
        print(f"\n✗ FATAL: {len(dupes)} DUPLICATE rule id(s) — the inventory key is not unique:")
        for rid in dupes[:10]:
            print(f"      {rid}")
        sys.exit(1)

    # ⛔ THE SEGMENTATION DRIFT GUARD (fix-queue PB39). Every column-0 ordinal must be EXPLAINED — a level
    # continuation, a nested-list tail behind an indented run, a same-form restart, or a list opening at 1.
    # An unexplained ordinal means the transcription carries a list shape the segmenter has never seen; guessing
    # at it is how 326 rules got ids the standard does not use. Zero on the whole 2023 transcription — proven to
    # fail by perturbation before being trusted — so any nonzero here is NEW evidence, not noise.
    if unexplained:
        print(f"\n⛔ {len(unexplained)} ordinal(s) the segmenter could not EXPLAIN — filed as nests, ids suspect:")
        for sec, n, why in unexplained[:15]:
            print(f"      §{sec} rule {n}: {why}")
        print("   Read the clause (and the printed page) and extend the segmentation evidence; do not ship these ids.")

    # ⛔ THE TRANSCRIPTION DEFECT PB689 WAS BUILT ON, REPORTED RATHER THAN ABSORBED IN SILENCE. The extractor no
    # longer LOSES rules to a heading-ified group label, but a transcription that prints one is still wrong
    # against the page (body type, not a heading) and still misleads every human reader and every other tool
    # that keys on headings. Demote it to a plain line in specs/ISO_COBOL.md.
    if heading_labels:
        print(f"\n⛔ {len(heading_labels)} in-clause GROUP LABEL(S) transcribed as a markdown HEADING — the shape")
        print("   that cost 60 rules in five clauses (PB689). The printed standard sets these in BODY type;")
        print("   demote each to a plain line in the transcription:")
        for sec, label in heading_labels:
            print(f"      §{sec}  '{label}'")

    # ⛔ THE SAME POSTURE FOR PB698's DEFECT: the extractor no longer lets a flattened nested-list tail become a
    # row, but a transcription that outdents one still makes `cite.py` report the WRONG printed path (§14.9.41.3
    # SR6 b) 2. and 3. both resolved to "6) b) 1." while the printed page indents them under b)), and a wrong
    # citation path is the one thing CLAUDE.md rule 1 exists to prevent. Repair the indentation on the page.
    if flattened_tails:
        print(f"\n⛔ {len(flattened_tails)} nested-list TAIL item(s) transcribed at COLUMN 0 — folded into the")
        print("   parent rule here, but `cite.py` still mis-reports their printed path (PB698). Indent each to")
        print("   its printed level in specs/ISO_COBOL.md (measure the page: the item's x0 against the body margin):")
        for sec, ln, n, text in flattened_tails:
            print(f"      §{sec}  line {ln}  '{n}. {text}'")

    by_kind = Counter(r["kind"] for r in rules)
    top = Counter(r["section"].split(".")[0] for r in rules)

    print(f"spec:            {SPEC.relative_to(REPO)}  ({len(lines):,} lines)")
    print(f"rule blocks:     {len(blocks_seen)}")
    print(f"RULES EXTRACTED: {len(rules)}")
    print()
    for k, label in (("SR", "syntax rules"), ("GR", "general rules"),
                     ("AR", "argument rules"), ("RV", "returned value rules"),
                     ("DOC", "Annex A.1 implementor-defined items"),
                     ("FMT", "general formats (syntax diagrams)")):
        if by_kind.get(k):
            print(f"   {by_kind[k]:5d}  {k}  {label}")
    print()
    print("   by top-level clause:")
    # Annex clauses sort after numbered ones ("A.1" is not an int).
    for sec, n in sorted(top.items(), key=lambda kv: (0, int(kv[0])) if kv[0].isdigit() else (1, 0)):
        print(f"      §{sec:<3s} {n:5d}")

    expected_empty = [(s, k) for s, k in gaps if s in KNOWN_EMPTY_BLOCKS]
    gaps = [(s, k) for s, k in gaps if s not in KNOWN_EMPTY_BLOCKS]
    for sec, _kind in expected_empty:
        print(f"\n· §{sec} yielded 0 rules, as declared: {KNOWN_EMPTY_BLOCKS[sec]}")

    if gaps:
        print(f"\n⚠ {len(gaps)} PARSE GAP(S) — rule-block headings that yielded ZERO rules.")
        print("  An omitted rule is silent false confidence; investigate before trusting the denominator:")
        for sec, kind in gaps[:25]:
            print(f"      {kind} §{sec}  ({titles.get(sec, '?')})")
        if len(gaps) > 25:
            print(f"      ... and {len(gaps) - 25} more")
    else:
        print("\n✓ no parse gaps — every rule-block heading yielded at least one rule")

    # ⛔ THE CHECK THAT WOULD HAVE CAUGHT THE MISSING TEN. A parse gap is a block that was RECOGNISED and then
    # yielded nothing; a block whose heading spelling is unknown is never recognised, so it is never counted as a
    # gap and never appears in any total — the denominator is simply short, silently. This walks every numbered
    # heading whose title contains "rule" and reports the ones KINDS cannot resolve.
    unrecognised = sorted(
        ((num, title) for num, title in titles.items()
         if RULE_SHAPED.search(title) and kind_of(title) is None and num not in EXCLUDED_BLOCKS),
        key=lambda kv: kv[0])
    for num, why in sorted(EXCLUDED_BLOCKS.items()):
        print(f"\n· §{num} '{titles.get(num, '?')}' is deliberately outside the denominator: {why}")
    if unrecognised:
        print(f"\n⛔ {len(unrecognised)} rule-shaped heading(s) the KINDS map does not recognise — their rules are")
        print("   ABSENT FROM THE DENOMINATOR, and absent silently (an unrecognised block is never a parse gap):")
        for num, title in unrecognised:
            print(f"      §{num}  '{title}'")
        print("   Add the CANONICAL singular form to KINDS, or extend canonical_title's normalisation.")
    else:
        print("✓ every rule-shaped heading in the spec resolves to a known rule kind")

    # ⛔ THE CONTENT-KEYED CHECK — the one the two previous denominator corrections could not have made.
    # See unharvested_rule_blocks(). Measured against a COMMITTED MANIFEST rather than a remembered number, so a
    # newly-transcribed clause carrying rules is LOUD instead of silently absent from the denominator.
    measured = unharvested_rule_blocks(lines, titles)
    declared = json.loads(UNHARVESTED.read_text(encoding="utf-8"))["blocks"] if UNHARVESTED.exists() else {}
    added = sorted(set(measured) - set(declared))
    removed = sorted(set(declared) - set(measured))
    changed = sorted(s for s in set(measured) & set(declared)
                     if measured[s]["ordinals"] != declared[s]["ordinals"])
    pending = {s: v for s, v in declared.items() if v.get("disposition") == "pending"}

    print(f"\n⛔ CONTENT-KEYED SCAN — clauses carrying an ascending 'N)' list that this extractor does NOT harvest:")
    print(f"      {len(measured):4d} clauses, {sum(v['ordinals'] for v in measured.values()):5d} ordinals (an UPPER BOUND — see the docstring)")
    print(f"      {len(pending):4d} clauses, {sum(v['ordinals'] for v in pending.values()):5d} ordinals awaiting a denominator disposition (fix-queue PB29)")
    if added or removed or changed:
        print("\n⛔ THE UNHARVESTED SET HAS DRIFTED FROM ITS MANIFEST — the denominator may now be short again.")
        for s in added:
            print(f"      + §{s} ({measured[s]['ordinals']} ordinals) '{measured[s]['title']}' — NEW, undeclared")
        for s in removed:
            print(f"      - §{s} — declared but no longer measured (harvested now, or the transcription moved)")
        for s in changed:
            print(f"      ~ §{s}  {declared[s]['ordinals']} → {measured[s]['ordinals']} ordinals")
        print(f"   Adjudicate each and update {UNHARVESTED.relative_to(REPO)}: a clause is either a RULE block")
        print("   (give its heading a kind in KINDS) or a prose enumeration (disposition 'not-rules', with why).")
    else:
        print("      ✓ matches the committed manifest exactly — no clause has silently left the denominator")

    # The general-format bar (kb/Work PB1536 Q2) — see the FMT scan above.
    for sec in fmt_parents:
        print(f"\n· §{sec} '{titles.get(sec, '?')}' is titled 'General format(s)' but is a PARENT clause (its body is"
              " its own sub-clauses, no diagram, no Format label) — a clause ABOUT formats, not a construct's format;"
              " no FMT row")
    if fmt_undrawn:
        print(f"\n⛔ {len(fmt_undrawn)} general-format clause(s) whose transcription carries NO DIAGRAM and no Format "
              "label — the diagram was lost in transcription. Render the printed page and restore it as a "
              '<pre style="line-height:1"> block with <u> on the required words:')
        for sec in fmt_undrawn:
            print(f"      §{sec}  ({subject_for(sec, titles)})")
    else:
        print("✓ every general-format row carries a diagram or a Format label")

    if args.check:
        return 1 if (gaps or unrecognised or added or removed or changed or unexplained
                     or heading_labels or flattened_tails or fmt_undrawn) else 0

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps({
        "generated_from": str(SPEC.relative_to(REPO)),
        "rule_count": len(rules),
        "blocks": len(blocks_seen),
        "parse_gaps": [{"section": s, "kind": k} for s, k in gaps],
        "rules": rules,
    }, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"\nwrote {OUT.relative_to(REPO)}  ({OUT.stat().st_size / 1_048_576:.1f} MB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
