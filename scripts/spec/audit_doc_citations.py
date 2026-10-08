#!/usr/bin/env python3
"""Find INHERITED citations in the docs — a quoted fragment that is real, but filed under the wrong clause.

    python scripts/spec/audit_doc_citations.py              # report (misfiled citations only)
    python scripts/spec/audit_doc_citations.py --check      # exit 1 on any finding (the gate)
    python scripts/spec/audit_doc_citations.py --all        # also list quotes found nowhere (noisy; see below)
    python scripts/spec/audit_doc_citations.py --self-test  # prove the check FAILS on a known defect

⛔ WHAT THIS LOOKS FOR, AND WHY IT IS NARROW ON PURPOSE. CLAUDE.md rule 1: "The failure mode is not inventing a
citation, it is INHERITING one — a queue entry or design doc carries a §, its quoted text is genuinely in the
standard, and the clause NUMBER is never re-derived before it propagates." That defect has an exact mechanical
signature: the quoted text IS in the spec, and it is NOT in the clause the doc names.

⛔ A CITATION IS WRITTEN IN MORE THAN ONE ORDER, AND ASSUMING ONE OF THEM IS HOW THIS AUDIT ACCUSED FOUR CORRECT
SITES (kb/Work PB379). The first version anchored on the § and took the next quote on the line — i.e. it could
read `§15.39.1 "depends on the type of argument-1"` and nothing else. This repository also writes:

  · the POSTFIX form, `"depends on the type of argument-1" §15.39.1,` — the citation follows what it supports.
    Of PB379's six reported "misfilings" TWO were this: the prefix reading blamed whichever § happened to sit
    nearest BEFORE the quote, which was the PREVIOUS quote's citation. Worse than the false positives was the
    blind spot behind them — a quote cited in the postfix form was never checked against its clause AT ALL, so a
    misfiling written that way was invisible. Reading both orders removed the false positives and put ~15 more
    citations under the check.
  · the BACK-REFERENCE form, `§15.2 … "no digits to the right of the decimal point" (item 5)` — the clause is
    named earlier in the sentence and the quote points back at it by rule number. That one is NOT made
    mechanical here and is not meant to be: a citation no tool can resolve is a citation nothing checks, which
    is the state rule 1 exists to end. The repair is to write the clause number next to the quote it supports.

THE ORDER RULE, then: the citation of a quoted fragment is the clause that FOLLOWS it and closes (a bracket, a
comma, a full stop, a `/` or `·` in a citation list) if there is one, else the nearest clause BEFORE it on the
same line. A `§` that opens an aside instead of closing — `"…the NATIONAL phrase" (§13.18.29 — and my first
draft cited …)` — is deliberately not read as the quote's citation: it is a pointer at a construct the quote
NAMES, and reading it as the source accused kb/Work PB15, which is correct.

⚠ THE SIGNAL IS SHARP, NOT PERFECT, and the first run proved it — do not treat a hit as a defect without reading
the line. `--find` reports the first clause containing the text, and a phrase can legitimately appear in several;
a doc may also name two clauses in one sentence, or quote a defined TERM rather than spec prose. The patterns
below are calibrated against both, and every remaining hit still deserves a human glance before an edit.

⚠ THE BROAD VERSION OF THIS CHECK IS WORTHLESS, and it was written first. Matching every `§N.N … "quoted text"`
pair and demanding the text be inside that clause reported 133 failures out of 183, essentially all of them its
own fault: quotes captured across a markdown blockquote marker, paraphrases attributed to a clause, quoted LABELS
(`"shall-not-with-APPLY-COMMIT"`, `"element; para; line"`) that were never claims about spec text at all, and
`§1.6`-style references to a DOC's own sections. A doc uses quotation marks for both quoting and naming, and
nothing in the text distinguishes them — so that check cannot be made precise and is not offered as a gate.
It survives behind `--all`, clearly labelled, because the residue is worth eyeballing occasionally.

This is the same lesson the figure audits taught (`plan §0`: "MY CHECKERS WERE BUGGIER THAN THE TRANSCRIPTION" —
76 findings went to 1 as three tool bugs came out). Confirm a measured defect before changing anything.

FOUND BY THE FIRST RUN (all corrected in the same change set):
  · `COBOLNET_DESIGN.md` cited "§12.3.7 GR7 k3 … distinct ascending" TWICE — a phrase that appears NOWHERE in the
    standard. A Phase-B agent inherited it into a fix-queue finding. The real rule is §12.3.7.4 GR7 1.3.
  · `COBOLNET_REDEFINES_DESIGN.md` cited §13.18.60 for GR4/GR11 text really in §13.18.60.4 — one level short,
    the CA37/CA38 shape.
  · `PHASE-11-scout-notes.md` cited §13.18.62 for text really in §13.18.63.4 GR4c.
"""
from __future__ import annotations

import argparse
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import citation_corpus  # noqa: E402
import cite as cite_mod  # noqa: E402

REPO = citation_corpus.REPO

#: A quoted fragment that could be spec prose: lower-case opening (so a sentence-initial capital, which a doc
#: would have had to re-case, is not read as a verbatim quotation) and long enough to be prose rather than a
#: label. ⚠ A 30-character floor, not 18, and it was earned: below that a quote is usually a defined TERM
#: ("implementor-defined", "the end of the PERFORM"), and a term legitimately appears in many clauses — so
#: --find locating it elsewhere says nothing about the clause the doc named.
QUOTED = r'["“](?P<q>[a-z][^"”\n]{30,90})["”]'

#: FORM 1 — the citation PRECEDES the quote: `§14.9.7.3 SR2: "this statement shall not …"`.
#: ⚠ `[^§"“\n]` between the clause and the quote — NOT `[^"“\n]`. A sentence may name two clauses before it
#: quotes ("never the §13.18.38 window — §14.2.3 GR8 says …"), and the citation being made is the NEAREST one.
#: The greedy form blamed the wrong clause and reported a CORRECT citation as misfiled.
PAT = re.compile(r'§(?P<cl>\d+(?:\.\d+){2,})[^§"“\n]{0,80}' + QUOTED)

#: FORM 2 — the citation FOLLOWS the quote and CLOSES it off: `"depends upon the argument types" §15.18.1)`.
#: Three calibrations, each of which stops this form from reading something that is not a citation:
#:  · `(?!\.\d)` — the clause number is taken WHOLE. Without it the greedy number backtracks against the
#:    `.`-in-the-terminator alternative and `§12.3.7.3 SR16g / SR17d` reads as a citation of §12.3.7 — a
#:    correct citation reported as one level short, which is the very defect shape this audit hunts.
#:  · a rule designator may sit between the clause and the terminator (`§14.9.7.3 SR2)`, `§15.2 item 5)`).
#:  · the citation must CLOSE — bracket, comma, stop, or the `/` and `·` of a citation list. A `§` followed by
#:    more prose is an aside about a construct the quote names, not the quote's source (see the order rule).
POST = re.compile(QUOTED + r'\s{0,2}[(\[]?(?:ISO\s+)?§\s?(?P<cl>\d+(?:\.\d+){2,})(?!\.\d)'
                  r'(?:\s+(?:SR|GR|item|rule)\s?\d+[a-z]?\)?)?\s*(?:[)\],.;:/·]|$)')

#: Marks that a "quote" is elided, marked up, or otherwise not verbatim — never a checkable citation.
#: ⚠ BOTH SPELLINGS OF THE ELLIPSIS, and the ASCII one was missing until the ELIDED arm measured its absence.
#: `"minimum ... logical record size in bytes"` (`FixedFileAttributes.cs:34`) and `"the first reference ... in
#: the run unit"` (`kb/Work/PB307`'s title) are authors marking an elision HONESTLY — exactly what `…` means
#: here — and the new arm reported both as defects because this tuple knew only U+2026. A convention that
#: depends on which of two identical-looking characters the author typed is not a convention.
NOT_VERBATIM = ("…", "...", ">", "*", "`")

#: ⛔ AND A MISFILING THAT IS BEING REPORTED AS A MISFILING IS NOT A DEFECT. The kb/Work note for this family is
#: a table of "this file says §X, the text is really in §Y"; every row spells a wrong pairing, on purpose, and
#: no per-line disclaimer survives being tabulated — the row IS `§8.6.6 | "shall not be specified …"`. A file may
#: declare itself with the marker below, which says "the § / quote pairings in here are QUOTED, not asserted".
#: It is a whole-file opt-out, it is visible in the file, and it is the same mechanism (and the same 40-line
#: window) `audit_code_citations.py` gives the note that names phantom clauses on purpose.
MARKER = "audit-doc-citations: names-misfilings"


#: ⛔ THE ELIDED-QUOTATION ARM (kb/Work PB900). A quotation that `--find` locates NOWHERE lands in `absent`,
#: which is the bucket this audit deliberately does not gate on: a doc quotes LABELS and paraphrases as well
#: as spec text and nothing distinguishes them. But ONE shape inside that bucket IS decidable — a quotation
#: that is the clause's OWN SENTENCE WITH WORDS DROPPED. `EmitterState.cs:29` and `ControlFlowEmitter.cs:70`
#: carried §14.9.14.4 GR5a as *"the most closely preceding, unterminated inline PERFORM"*; the standard says
#: *"the most closely preceding, and as yet unterminated, inline PERFORM"*. Right clause, right rule,
#: quotation marks around words the standard does not contain in that order — and BOTH audits reported zero
#: on it for as long as it shipped. A paraphrase cannot pass this test: its words do not appear IN ORDER
#: inside the clause. A LABEL cannot either. Only an elision can, which is what makes the arm gateable.
#: `SLACK` is what "with words dropped" is allowed to mean: the window in the standard that carries the
#: quote's words in order may be half again as long, plus three. Below `FLOOR` words a subsequence match is
#: a coincidence rather than a quotation, and that floor is what keeps an ordinary phrase out.
ELIDE_SLACK, ELIDE_FLOOR = 1.5, 5

_SPEC = None
_WORDS = None


def _spec() -> tuple[list[str], list]:
    """`specs/ISO_COBOL.md` as `cite.py` reads it — its lines and clause regions — loaded ONCE per process.

    ⛔ ONE CHECKER, CALLED IN-PROCESS (kb/Work PB2525). Every pairing used to be checked by spawning `cite.py
    --check` (and `--find`) as a fresh Python process that reloaded the standard: 843 processes, 75 of the audit's
    79 s under cProfile, about 89 ms each, while the audit's own logic cost about 3 s. `cite.check` and
    `cite.find` are the very functions the `cite.py` CLI calls, so the ruling is the same code either way."""
    global _SPEC
    if _SPEC is None:
        _SPEC = cite_mod.load()
    return _SPEC


def _clause_words() -> dict[str, list[str]]:
    """clause -> its own region as a WORD LIST, normalized the way `cite.py` compares. Built once."""
    global _WORDS
    if _WORDS is None:
        lines, regions = _spec()
        out: dict[str, list[str]] = {}
        for n, _t, s, e in regions:
            out.setdefault(n, []).extend(cite_mod.norm(" ".join(lines[s:e])).split())
        _WORDS = out
    return _WORDS


def elided_window(clause: str, quote: str) -> int | None:
    """The length of the TIGHTEST window of `clause`'s own words that carries `quote`'s words IN ORDER, or
    None when the quote is not an elision of anything the clause says.

    Scanning from every start keeps this exact rather than greedy: a greedy left-to-right match can consume
    an early occurrence of a common word ("the") and then run to the end of the clause, making a genuine
    elision look like a scatter."""
    want = cite_mod.norm(quote).split()
    if len(want) < ELIDE_FLOOR:
        return None
    hay = _clause_words().get(clause)
    if not hay:
        return None
    best = None
    for start in range(len(hay)):
        if hay[start] != want[0]:
            continue
        k, j = 1, start + 1
        while k < len(want) and j < len(hay):
            if hay[j] == want[k]:
                k += 1
            j += 1
        if k == len(want) and (best is None or j - start < best):
            best = j - start
    if best is None or best > len(want) * ELIDE_SLACK + 3:
        return None
    return best


def verdict_of(clause: str, quote: str) -> tuple[str, str | int | None]:
    """THE ONE RULING on a (clause, quote) pairing — `("ok"|"misfiled"|"elided"|"absent", evidence)`.

    ⛔ A PURE FUNCTION SO THE SELF-TEST DRIVES THE SAME CODE THE GATE DOES. It was inlined in `scan`, and the
    self-test re-implemented the MISFILED half beside it in four lines of its own — which is how the ELIDED arm
    could be added to `scan` and be invisible to `--self-test`: a gate arm nothing had ever seen fire
    (`feedback_green_gates_arent_evidence`). One ruling, two callers."""
    lines, regions = _spec()
    if cite_mod.check(lines, regions, clause, quote)[0] == 0:
        return "ok", None
    code, out = cite_mod.find(lines, regions, quote)
    head = out[0].splitlines()[0].strip() if out else ""
    if code == 0 and head.startswith("§"):
        return "misfiled", head
    if (w := elided_window(clause, quote)) is not None:
        return "elided", w
    return "absent", None


def citations(line: str):
    """Every checkable citation on ONE line, as (column, clause, quote, form), left to right.

    The POSTFIX form is resolved FIRST and its quote is then off limits to the prefix form: when both readings
    are available for one quoted fragment the citation written next to it wins, which is what makes
    `"a" §1.2.3, "b" §4.5.6` read as two citations rather than as one misfiling."""
    claimed: dict[tuple[int, int], tuple[str, str, str]] = {}
    for m in POST.finditer(line):
        claimed[m.span("q")] = (m.group("cl"), m.group("q"), "postfix")
    found = dict(claimed)
    for m in PAT.finditer(line):
        if m.span("q") not in claimed:
            found[m.span("q")] = (m.group("cl"), m.group("q"), "prefix")
    for span in sorted(found):
        clause, quote, form = found[span]
        if not any(t in quote for t in NOT_VERBATIM):
            yield span[0], clause, quote, form


def scan(paths, elided_out: list | None = None):
    """(misfiled, absent, ok) over `paths`, EVERY SITE reported and every distinct pairing checked ONCE.
    `elided_out`, when given, collects the ELIDED subset of `absent` — see `elided_window`.

    ⛔ ONE VERDICT PER PAIRING, ONE REPORT PER SITE, and the two were conflated until PB379. Deduplicating the
    REPORT hid siblings: the `§15.38.1 "depends on the type of argument-1"` pairing lives in three files, and a
    reader of the one reported line had no way to know the other two existed — precisely the sweep CLAUDE.md
    rule 4 demands, defeated by the tool. The ruling (`verdict_of`) is still made once per pairing, which is the
    part that costs anything."""
    verdict: dict[tuple[str, str], tuple[str, str | int | None]] = {}
    misfiled: list[tuple[str, int, str, str, str, str]] = []
    absent: list[tuple[str, int, str, str, str]] = []
    ok = 0
    for doc in paths:
        try:
            lines = doc.read_text(encoding="utf-8").splitlines()
        except OSError:
            continue
        if MARKER in "\n".join(lines[:40]):
            continue
        rel = str(doc.relative_to(REPO) if doc.is_relative_to(REPO) else doc).replace("\\", "/")
        for n, line in enumerate(lines, 1):
            for _col, clause, quote, form in citations(line):
                key = (clause, quote)
                if key not in verdict:
                    verdict[key] = verdict_of(clause, quote)
                kind, evidence = verdict[key]
                if kind == "ok":
                    ok += 1
                elif kind == "misfiled":
                    misfiled.append((rel, n, clause, quote, str(evidence), form))
                else:
                    absent.append((rel, n, clause, quote, form))
                    if elided_out is not None and kind == "elided":
                        elided_out.append((rel, n, clause, quote, int(evidence), form))
    return misfiled, absent, ok


#: ⛔ A GATE THAT HAS NEVER BEEN SEEN TO FAIL IS NOT EVIDENCE (the sibling audit's `--self-test` doctrine, and
#: `feedback_prove_the_watchdog_fails`). Each line below is fed through the real `citations()` + `cite.py`, and
#: each defect is paired with its REPAIRED twin, which must be silent. The postfix legs matter most: reading a
#: second citation ORDER is a WIDENING, and a widening that has never been seen to fire is not a widening.
#: ⛔ AND THE ARM EACH CASE IS ABOUT IS NAMED, not merely "a finding". `--self-test` used to assert a BOOLEAN,
#: which is how the ELIDED arm was added to `scan` and driven by nothing: every existing case still passed, the
#: self-test still printed PASS, and the new arm had never been seen to fire. A case now says WHICH ruling it
#: expects, so an arm that stops firing fails the case that names it rather than falling through to another.
SELF_TEST = [
    ("prefix, misfiled", "misfiled",
     'The COMMIT statement (ISO §8.6.6) "shall not be specified in the input or output procedure of a MERGE"'),
    ("prefix, correct", None,
     'The COMMIT statement (ISO §14.9.7.3) "shall not be specified in the input or output procedure of a MERGE"'),
    ("postfix, misfiled", "misfiled",
     'It "shall not be specified in the input or output procedure of a MERGE" (§8.6.6), it says.'),
    ("postfix, correct", None,
     'It "shall not be specified in the input or output procedure of a MERGE" (§14.9.7.3), it says.'),
    # ⛔ THE ELIDED ARM'S OWN DEFECT (kb/Work PB900 arm B), verbatim as it shipped in `EmitterState.cs:29` and
    # `ControlFlowEmitter.cs:70`: the right clause, the right rule, and quotation marks around words the
    # standard does not contain in that order — "and as yet" dropped out of the middle. `--check` FAILS on it,
    # `--find` locates it nowhere, and for as long as it shipped both audits reported zero.
    ("elided quotation", "elided",
     'an EXIT PERFORM targets (§14.9.14.4 GR5a) "the most closely preceding, unterminated inline PERFORM"'),
    ("elided quotation, repaired", None,
     'an EXIT PERFORM targets (§14.9.14.4 GR5a) "the most closely preceding, and as yet unterminated, inline '
     'PERFORM"'),
    # ⛔ AND THE PARAPHRASE THE ARM MUST NOT CLAIM. ELIDED is gateable only because a paraphrase cannot pass it:
    # its words are not in the clause IN ORDER. This one says what GR5a says, in other words, and lands in
    # `absent` where it belongs — the bucket this audit deliberately does not gate on.
    ("paraphrase, not an elision", "absent",
     'an EXIT PERFORM targets (§14.9.14.4 GR5a) "whichever inline PERFORM is innermost at that point"'),
    # ⛔ THE FALSE POSITIVE THE ORDER RULE EXISTS TO STOP: two postfix citations in one parenthesis, where the
    # prefix reading pairs each quote with the PREVIOUS quote's clause. Both pairings here are correct.
    ("postfix pair, both correct", None,
     'the wordings differ ("depends on the type of argument-1" §15.39.1, "depends upon the argument types" '
     '§15.18.1) and a prose key found ten of twenty'),
    # ⛔ AND THE ASIDE, which is not a citation of the quote at all: §13.18.29 is where GROUP-USAGE is defined,
    # the quote is §8.5.2.10's. Reading it as the source accused kb/Work PB15, which is correct.
    ("postfix aside, not a citation", None,
     'a group of category national as one "described with a GROUP-USAGE clause with the NATIONAL phrase" '
     '(§13.18.29 — and my first draft cited §13.18.27)'),
    # ⛔ AND THE CLAUSE NUMBER TAKEN WHOLE: without `(?!\.\d)` this reads as a citation of §12.3.7 and reports a
    # correct citation as one level short.
    ("postfix with a rule designator", None,
     '"specified with the LOCALE phrase" (§12.3.7.3 SR16g / SR17d) — of EITHER class'),
]


def self_test() -> int:
    ok = True
    for name, want_arm, line in SELF_TEST:
        got = []
        for _col, clause, quote, _form in citations(line):
            kind, _evidence = verdict_of(clause, quote)
            if kind != "ok":
                got.append((kind, f"§{clause} \"{quote[:40]}\""))
        arms = {k for k, _ in got}
        good = (arms == {want_arm}) if want_arm else not arms
        ok &= good
        want = f"{want_arm:8}" if want_arm else "silent  "
        print(f"  {'ok  ' if good else 'FAIL'} {want} on {name}" + (f"  (got {got})" if not good else ""))
    # The whole-file opt-out is a SUPPRESSION, and a suppression that suppresses nothing is not one: the same
    # defect line must fire without the marker and be silent with it.
    import tempfile
    with tempfile.TemporaryDirectory() as d:
        p = pathlib.Path(d) / "note.md"
        defect = SELF_TEST[0][2]
        p.write_text(defect + "\n", encoding="utf-8")
        fires = bool(scan([p])[0])
        p.write_text(f"<!-- {MARKER} -->\n{defect}\n", encoding="utf-8")
        silent = not scan([p])[0]
    print(f"  {'ok  ' if fires else 'FAIL'} fires   on a file WITHOUT the {MARKER!r} marker")
    print(f"  {'ok  ' if silent else 'FAIL'} silent  on the same file WITH it")
    ok &= fires and silent
    # ⛔ THE RULING IS IN-PROCESS (kb/Work PB2525): a process spawned per pairing was 75 of the audit's 79 s. With
    # process creation made to fail, every ruling must still come back — from the one standard loaded once.
    import subprocess
    spawned: list[str] = []

    def refuse(*args, **kwargs):
        spawned.append(str(args[0] if args else kwargs.get("args"))[:80])
        raise RuntimeError("the audit spawned a process to check a citation")

    real_popen, subprocess.Popen = subprocess.Popen, refuse
    try:
        try:
            rulings = [verdict_of(c, q)[0] for _n, _w, line in SELF_TEST for _col, c, q, _f in citations(line)]
        except RuntimeError:
            rulings = []
    finally:
        subprocess.Popen = real_popen
    loaded_once = _spec() is _spec()
    inproc = not spawned and len(rulings) >= len(SELF_TEST) and loaded_once
    print(f"  {'ok  ' if inproc else 'FAIL'} in-process: every ruling made with no process spawned, the standard "
          f"loaded once" + ("" if inproc else f"  (spawned {spawned[:1]}, {len(rulings)} rulings)"))
    ok &= inproc
    print("SELF-TEST:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", action="store_true", help="exit 1 on any finding (the gate)")
    ap.add_argument("--all", action="store_true", help="also list quotes found nowhere in the spec (noisy)")
    ap.add_argument("--self-test", action="store_true", help="prove the check fires on a real defect")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    if args.self_test:
        return self_test()

    # ⛔ SOURCE DOC-COMMENTS TOO, AND THE GLOB LIVES IN ONE PLACE. Scanning only docs/ measures half the prose:
    # an XML comment above the implementing method carries a citation at the same density and with the same
    # authority as the design doc, and is read by the same person for the same reason. PB3 proved it — the
    # fabricated "GR7 k3" survived a full docs/ sweep in THIRTEEN places across five source files, including the
    # comments on the one implementation that gets the rule right. `citation_corpus.all_files()` is that glob,
    # shared with `audit_code_citations.py` so a file cannot be covered by one citation rule and not the other.
    # ⛔ AND THE GOLDEN HEADERS ARE IN IT. A `.cob` fixture's header is where it records the rule it exists to
    # pin, and it quotes the standard to do so — `pb43_usage_bit_occupies_bits.cob` carried a FABRICATED
    # quotation of §8.5.1.6.3 ("aligned at the first bit position of the first available byte"; the standard
    # says "Alignment of all other bit data items within a record, when a SYNCHRONIZED clause is not specified,
    # is at the first bit position of the first available byte"). Nothing was reading those headers.
    elided: list = []
    misfiled, absent, ok = scan(citation_corpus.all_files(), elided)

    print(f"{ok + len(misfiled) + len(absent)} verbatim-shaped citations checked · {ok} correct")
    print(f"⛔ {len(misfiled)} MISFILED (the text is real, the clause is wrong) — these are defects\n")
    for rel, n, clause, quote, where, form in misfiled:
        print(f"  {rel}:{n} ({form})\n     says §{clause}  \"{quote[:70]}\"\n     really {where}\n")

    print(f"⛔ {len(elided)} ELIDED (the clause is right and the quotation marks are around words the "
          f"standard does not contain in that order) — these are defects\n")
    for rel, n, clause, quote, window, form in elided:
        print(f"  {rel}:{n} ({form})\n     says §{clause}  \"{quote[:70]}\"\n"
              f"     §{clause} carries those {len(cite_mod.norm(quote).split())} words in order "
              f"across {window} — run `cite.py --find` on a fragment and quote what it prints\n")

    if args.all:
        print(f"\n— {len(absent)} quote(s) not found anywhere in the spec. MOSTLY NOT DEFECTS: a doc quotes labels")
        print("  and paraphrases as well as spec text, and nothing distinguishes them mechanically. Eyeball only.\n")
        for rel, n, clause, quote, _form in absent:
            print(f"  {rel}:{n}: §{clause} \"{quote[:70]}\"")

    return 1 if ((misfiled or elided) and args.check) else 0


if __name__ == "__main__":
    sys.exit(main())
