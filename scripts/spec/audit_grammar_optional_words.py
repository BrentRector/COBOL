#!/usr/bin/env python3
"""Cross-reference MEASURED underlining against the grammar's keyword spelling, to find optional words we require.

THE DEFECT CLASS. ISO §5.2.3: an uppercase word printed WITHOUT an underline is an OPTIONAL WORD — it may be
written or omitted with no change of meaning. §5.2.2: an underlined one is REQUIRED. That distinction is carried
ENTIRELY by typography, it is the first thing a transcription loses, and a grammar written from a transcription
that lost it will reject legal COBOL.

It has already produced real compiler bugs: `ON` in SIZE ERROR and ON EXCEPTION (DEVLOG 1041), `FROM`, `AFTER`,
`SECONDS` and `TO` in the MCS statements (DEVLOG 1042), `ON` in READ … ADVANCING ON LOCK (kb/Work PB331), and
`WITH` in START … WITH LENGTH plus `AFTER`/`PROCEDURE` in USE (kb/Work PB332). Each was found by hand. This
automates the search.

HOW. For every general format PRINTED IN THE STANDARD — all of them, not the statements alone:

  * the transcription declares the population: every `N.N…  General format[s]` heading in `specs/ISO_COBOL.md`;
  * the PAGE and the EXTENT come from `clause_page.index()`, the one clause→page locator — the format runs from
    its own heading to the NEXT clause heading, across a page break when it takes one;
  * the words in that band are measured — underlined or not — from the PDF's vector rectangles;
  * the grammar rules that spell the construct are its CLOSURE: the rules naming the construct's leading keyword,
    plus every rule reachable from them that NOTHING ELSE references. That closure is what makes helper rules
    visible — `readAdvancingOnLock` never mentions READ, and a keyword-only scan could never have found PB331;
  * a word printed WITHOUT an underline but appearing as a BARE (non-optional) token is reported.

⛔ IT ASSERTS ITS OWN POPULATION, and that assertion is the point. Until 2026-09-05 this tool located its pages
from the markdown's `#page-N` anchors; DE-PAGING DELETED THE ANCHORS, so it located 0 general formats, scanned
0 pages, printed `0 candidate(s)` and exited 0 — a silently green gate over the whole grammar while READ, START
and USE each rejected legal source (kb/Work PB332). Every count below is now checked against the population it
claims to have measured, and a shortfall is a NON-ZERO EXIT, never a clean zero.

WHAT IT CANNOT DECIDE, and deliberately does not pretend to. A word may be required in one format and optional
in another — `TO` is underlined in `ADD … TO` and bare in `SEND … TO`, and `KEY` is underlined in START's KEY
phrase and bare in its INVALID KEY phrase. This tool matches at CONSTRUCT granularity and drops any word that is
underlined ANYWHERE in the construct's formats, so it under-reports rather than over-reports, and what it does
report still needs a human. It reports CANDIDATES, not findings.

⚠ CONFIRM EVERY CANDIDATE AGAINST THE RAW RECTANGLES BEFORE CHANGING THE GRAMMAR. Two-letter words are the
weak case for underline detection — a 9 pt width floor once made this toolchain accuse two CORRECT pages
(`IN` p322, `TO` p673), and acting on that would have damaged the reference document. `figure_extract.py`'s
docstring records the fix; the discipline stands regardless.

    python scripts/spec/audit_grammar_optional_words.py
    python scripts/spec/audit_grammar_optional_words.py --construct USE
    python scripts/spec/audit_grammar_optional_words.py --clause 14.9.41.2 --show-format
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys
from typing import NamedTuple

# The grammar is SPLIT ACROSS TWELVE FILES — CobolParserCore.g4 plus Grammar/Core/*.g4. An earlier version of this
# script read only the root file and therefore scanned a fraction of the rules, reporting a reassuring "3
# candidates" that was an artifact of not looking. `grammar_source` is THE reader of all of them (kb/Work PB3204):
# it excludes the build-time COPIES under obj/, blanks comments and actions, and refuses an empty population.
import grammar_source

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)

#: The printed body band, borrowed from `render_figure` so the running header and the folio/licence footer can
#: never enter a format's measured band when the format spills across a page break.
BODY_TOP, BODY_BOTTOM = 60.0, 730.0

RESERVED = re.compile(r"^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*$")
# Words that are structural in the grammar rather than COBOL keywords, or that carry their own rules.
SKIP = {"ISO", "IEC", "NOTE", "FORMAT", "FORMATS", "GENERAL", "TRUE", "FALSE", "ALL"}

#: A markdown clause heading: `##### 14.9.49.2 General formats`.
MD_HEADING = re.compile(r"^#{2,6}\s+(\d+(?:\.\d+)*)\s+(\S.*?)\s*$")
#: The sub-clause that carries the printed diagram(s). Singular in most clauses, plural where there are several.
GENERAL_FORMAT_TITLE = re.compile(r"^General formats?$")
#: A construct's ANCHOR KEYWORD is the leading all-caps COBOL word of its clause title — `USE statement`,
#: `OCCURS clause`, `>>DEFINE directive`, `ABS function`. A title that does not start with one (`Qualification`,
#: `Report description entry`) has no anchor; those formats are counted and named, never silently dropped.
ANCHOR = re.compile(r"^>{0,2}([A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*)\b")


class Format(NamedTuple):
    """One printed general format, as the TRANSCRIPTION declares it."""
    clause: str                 # the general-format sub-clause, e.g. "14.9.49.2"
    parent: str                 # the construct's clause, e.g. "14.9.49"
    title: str                  # the construct's clause title, e.g. "USE statement"
    keyword: str | None         # its anchor keyword, e.g. "USE" — None for a prose-titled construct


def declared_formats(md_text: str) -> list[Format]:
    """Every general format the transcription declares, in document order — this tool's expected population.

    A general format is a LEAF clause. §5.2 is titled "General formats" too, but it is the clause that DEFINES
    the notation (§5.2.1 General … §5.2.10 Meta-terms live under it) and prints no diagram of its own; the
    has-no-sub-clauses test excludes it structurally, which is one clause out of 321 and needs no hand list.
    """
    titles: dict[str, str] = {}
    order: list[str] = []
    for line in md_text.splitlines():
        m = MD_HEADING.match(line)
        if m:
            titles.setdefault(m.group(1), m.group(2))
            order.append(m.group(1))
    out: list[Format] = []
    seen: set[str] = set()
    for clause in order:
        if clause in seen or not GENERAL_FORMAT_TITLE.match(titles.get(clause, "")):
            continue
        seen.add(clause)
        if any(c.startswith(clause + ".") for c in titles):
            continue                                # a container clause, not a printed diagram — see §5.2
        parent = ".".join(clause.split(".")[:-1])
        title = titles.get(parent, "")
        a = ANCHOR.match(title)
        out.append(Format(clause, parent, title, a.group(1) if a else None))
    return out


def parser_rules() -> dict[str, grammar_source.Rule]:
    """Rule name -> the parser rule, read by `grammar_source` (comments and `{…}` actions blanked, literals intact).

    A comment is prose ABOUT the rule, so a keyword written in one is not a token the parser requires; an ACTION
    or SEMANTIC PREDICATE is target-language code and can never hold a token reference —
    `{facilityWord("RECEIVE")}?` in the `statement` dispatch once made that 200-alternative rule look like the
    RECEIVE statement's own spelling and dragged 233 rules into its closure. A name declared in two files keeps
    the later file's rule, in `grammar_source.grammar_files` order.
    """
    return {r.name: r for r in grammar_source.read() if r.kind == "parser"}


_TOKEN = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")

#: What is left of a lexer body once its quoted literals, its `->` action, and its punctuation are removed. A
#: KEYWORD token's body is nothing but literals; a LEXICAL CLASS names fragments, character sets or predicates.
_LEX_LITERAL = re.compile(r"'((?:[^'\\]|\\.)*)'")


def keyword_spellings() -> dict[str, set[str]]:
    """Lexer token -> the COBOL words it spells, for tokens that spell WORDS and nothing else.

    ⛔ THIS IS THE LINE BETWEEN A COBOL WORD AND A LEXICAL CLASS, and it is drawn STRUCTURALLY — a token whose
    rule is one or more quoted literals (`RESERVE : 'RESERVE' ;`, `ZERO : 'ZERO' | 'ZEROS' | 'ZEROES' ;`) spells
    words that the standard can print; one whose rule names fragments, character sets or predicates (IDENTIFIER,
    INTEGERLIT, PIC_STRING) spells a class of user text that appears on no printed page as itself.

    The distinction is what makes `spells_this_construct` safe. That test asks whether a rule demands a word the
    construct's printed format never prints, and `decimalPointClause : DECIMAL_POINT IS? IDENTIFIER` demands
    IDENTIFIER — which is printed nowhere in the standard, in any format. Without this split the test would
    disqualify every rule with an operand token and the audit would report nothing at all, silently: precisely
    the failure mode the population assertion below exists to prevent.

    ⛔ A LITERAL IS ONLY A WORD WHEN IT IS SHAPED LIKE ONE. `DOT : '.' ;` is literal-only too, and admitting it
    cost the whole SPECIAL-NAMES and OBJECT-COMPUTER closures on the first cut of this function: every paragraph
    rule spells `… DOT`, no printed format's word list contains a bare period, so `spells_this_construct`
    disqualified the ANCHOR and the audit reported ZERO candidates for both paragraphs while nine of their
    clauses demanded an optional word. The old-grammar control run is what caught it — a filter is only ever
    evidence about what it RETURNED (feedback_measure_the_selectors_complement).
    """
    out: dict[str, set[str]] = {}
    for rule in grammar_source.read():
        if rule.kind != "lexer":                                         # a fragment is never a token of its own
            continue
        body = re.sub(r"->.*$", "", rule.rhs, flags=re.S)               # a lexer command, not part of the match
        lits = [w for w in _LEX_LITERAL.findall(body) if RESERVED.match(w.upper())]
        if not lits:
            continue
        if re.sub(r"'(?:[^'\\]|\\.)*'|[\s|()?*+]", "", body):           # anything but literals and alternation
            continue
        out.setdefault(rule.name, set()).update(w.upper().replace("-", "_") for w in lits)
    return out


def alternatives(body: str) -> list[str]:
    """One rule body split into its TOP-LEVEL alternatives (a `|` inside `(…)` belongs to an inner group)."""
    body = body[body.find(":") + 1:]
    stop = body.rfind(";")
    if stop >= 0:
        body = body[:stop]
    out, depth, start = [], 0, 0
    for i, c in enumerate(body):
        if c == "(":
            depth += 1
        elif c == ")":
            depth -= 1
        elif c == "|" and depth == 0:
            out.append(body[start:i])
            start = i + 1
    out.append(body[start:])
    return out


def scan_alternative(alt: str) -> list[tuple[str, bool]]:
    """(token, may-be-omitted) for every UPPERCASE token in one alternative.

    A token may be omitted when it carries a `?`/`*` suffix of its own, or when ANY enclosing group does. That
    is the whole optionality question the audit asks, and it has to be read from the STRUCTURE: the previous
    regex only looked at the character after the token, so a required word inside `( … )?` read as required.
    """
    toks: list[list] = []
    stack: list[list[int]] = [[]]
    i, n = 0, len(alt)
    while i < n:
        c = alt[i]
        if c == "(":
            stack.append([])
            i += 1
            continue
        if c == ")":
            group = stack.pop() if len(stack) > 1 else []
            i += 1
            if i < n and alt[i] in "?*":
                for k in group:
                    toks[k][1] = True
                i += 1
            elif i < n and alt[i] == "+":
                i += 1
            stack[-1].extend(group)
            continue
        m = _TOKEN.match(alt, i)
        if not m:
            i += 1
            continue
        word = m.group(0)
        i = m.end()
        optional = i < n and alt[i] in "?*"
        if i < n and alt[i] in "?*+":
            i += 1
        if re.fullmatch(r"[A-Z][A-Z0-9_]*", word):
            toks.append([word, optional])
            stack[-1].append(len(toks) - 1)
    return [(w, o) for w, o in toks]


def required_tokens(rules: dict[str, str]) -> dict[str, set[str]]:
    """Rule -> the tokens it makes MANDATORY somewhere in its spelling.

    Two structural exclusions, each earned by a false positive this audit produced:

    * a SINGLE-TOKEN ALTERNATIVE in a rule that has several is a NAME alternative, not a format spelling —
      `cobolWord`'s `| RECEIVE |` says RECEIVE may be a user-defined word, and `readDirection`'s `NEXT |
      PREVIOUS` is a printed choice where one arm is not underlined. Neither is a word the parser demands;
    * a rule whose ENTIRE body is one token and that is only ever referenced with a `?`/`*` suffix —
      `timesKeyword : TIMES ;` used as `timesKeyword?` already permits `OCCURS 5`, so TIMES is not demanded.
      Contrast `startWithLength : WITH LENGTH …` used as `(startWithLength)?`: omitting the whole PHRASE is
      legal, writing it without WITH is not, so WITH stays a finding.
    """
    optional_only: dict[str, bool] = {}
    for owner, body in rules.items():
        for m in re.finditer(r"(?<![A-Za-z0-9_])([a-z][A-Za-z0-9_]*)[ \t]*([?*+]?)", body):
            name = m.group(1)
            if m.start() == 0 and name == owner:
                continue                                  # the rule's own head is a definition, not a reference
            if name in rules:
                # ⚠ `"" in "?*"` is True — the suffix must be tested for MEMBERSHIP in a set, not a substring.
                optional_only[name] = optional_only.get(name, True) and m.group(2) in {"?", "*"}
    out: dict[str, set[str]] = {}
    for name, body in rules.items():
        alts = alternatives(body)
        spellings: list[tuple[str, ...]] = []             # the mandatory token SEQUENCE of each alternative
        is_spelling: list[bool] = []                      # …and whether it is a FORMAT rather than a name list
        for alt in alts:
            scanned = scan_alternative(alt)
            spellings.append(tuple(w for w, opt in scanned if not opt))
            # A bare name alternative contributes no demand of its own — but it is still a real spelling that
            # can subsume a sibling's, which is why the sequence above is recorded either way.
            is_spelling.append(not (len(alts) > 1 and len(scanned) == 1
                                    and not re.search(r"(?<![A-Za-z0-9_])[a-z][A-Za-z0-9_]*", alt)))
        need: set[str] = set()
        for i, seq in enumerate(spellings):
            if not is_spelling[i]:
                continue
            for k, w in enumerate(seq):
                # ⛔ A SIBLING ALTERNATIVE MAY ALREADY SPELL THIS FORM WITHOUT THE WORD, and then the RULE does
                # not demand it even though this alternative does. `performLocationPhrase : WITH LOCATION |
                # {is2023()}? LOCATION` writes the two spellings of one optional word as two arms, so WITH is
                # mandatory in the first and the rule still accepts `LOCATION` — reporting it wasted a whole
                # confirmation pass (kb/Work PB695).
                # ⚠ THE COMPARISON IS ON THE ORDERED SEQUENCE, not on the required SET. With sets, the old
                # `switchOnClause : ON STATUS IS cobolWord | ON IS? cobolWord` would have subsumed its own IS
                # (the sibling's required set {ON} is a subset of {ON, STATUS}) and hidden a REAL rejection of
                # `ON STATUS condition-name-1`. Deleting the word must leave a sequence some sibling spells
                # EXACTLY — which `ON STATUS cobolWord` is not, and `LOCATION` is.
                if any(other == seq[:k] + seq[k + 1:] for j, other in enumerate(spellings) if j != i):
                    continue
                need.add(w)
        if len(need) == 1 and optional_only.get(name, False):
            whole = [w for alt in alts for w, _ in scan_alternative(alt)]
            if len(whole) == 1:
                need = set()                              # the token IS the optional unit — already omittable
        out[name] = need
    return out


def closure(rules: dict[str, str], anchors: set[str]) -> set[str]:
    """The rules that spell ONE construct: its anchor rules plus every rule EXCLUSIVELY reachable from them.

    ⛔ THIS IS WHY HELPER RULES ARE VISIBLE AT ALL. `readAdvancingOnLock` does not contain the word READ and
    `startWithLength` does not contain START, so a scan of "rules mentioning the keyword" can never see the very
    rules that carry a statement's optional words — PB331's `ADVANCING ON LOCK` lived in one of them.

    "Exclusively reachable" is a STRUCTURAL test, not a hand-maintained exclusion list: a rule joins the closure
    only when EVERY rule that references it is already in the closure. `useOnTarget` (referenced by
    `useStatement` alone) joins; `fileName`, `dataReference`, `statementBlock` and `retryPhrase` — referenced
    from all over the grammar — never do, because they are not this construct's private spelling.
    """
    names = set(rules)
    refs = {n: {r for r in re.findall(r"(?<![A-Za-z0-9_])([a-z][A-Za-z0-9_]*)", b) if r in names and r != n}
            for n, b in rules.items()}
    referrers: dict[str, set[str]] = collections.defaultdict(set)
    for n, rs in refs.items():
        for r in rs:
            referrers[r].add(n)
    inside = set(anchors)
    changed = True
    while changed:
        changed = False
        for n in sorted(inside):
            for r in sorted(refs.get(n, ())):
                if r not in inside and referrers[r] <= inside:
                    inside.add(r)
                    changed = True
    return inside


def band_words(doc, cache: dict[int, list], start, nxt) -> list[dict]:
    """Every word printed between one clause heading and the next, across a page break if it takes one."""
    from figure_extract import extract

    last = nxt.page if nxt is not None and nxt.page >= start.page else doc.page_count
    words: list[dict] = []
    for page in range(start.page, min(last, doc.page_count) + 1):
        if page not in cache:
            cache[page] = extract(doc[page - 1])
        top = start.y if page == start.page else BODY_TOP
        bottom = nxt.y if (nxt is not None and page == nxt.page) else BODY_BOTTOM
        words += [w for w in cache[page] if top <= w["y0"] <= bottom]
    return words


def measure(words: list[dict]) -> tuple[set[str], set[str]]:
    """(never-underlined words, underlined-somewhere words) over one construct's printed formats."""
    by_line: dict[int, list[dict]] = collections.defaultdict(list)
    for w in words:
        by_line[round(w["y0"] / 3)].append(w)
    plain: set[str] = set()
    underlined: set[str] = set()
    for row in by_line.values():
        text = " ".join(w["text"] for w in row)
        if re.search(r"ISO/IEC\s*1989", text):
            continue
        # A row carrying an all-lowercase word is prose or a metavariable line, not a diagram row of keywords.
        if any(re.fullmatch(r"[a-z]{2,}", w["text"].strip(" []{}.,;:()")) for w in row):
            continue
        for w in row:
            t = w["text"].strip(" []{}.,;:()")
            if len(t) > 1 and RESERVED.match(t) and t not in SKIP:
                (underlined if w["underlined"] else plain).add(t)
    # A word underlined ANYWHERE in this construct is required in one of its formats; only words that are never
    # underlined here are unambiguous optional-word candidates.
    return plain - underlined, underlined


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--construct", help="check only this construct's anchor keyword, e.g. USE")
    ap.add_argument("--clause", help="check only this general-format clause, e.g. 14.9.41.2")
    ap.add_argument("--show-format", action="store_true", help="print the measured words of each format checked")
    ap.add_argument("--list-unanchored", action="store_true",
                    help="name the prose-titled formats that carry no anchor keyword")
    args = ap.parse_args()
    for stream in (sys.stdout, sys.stderr):        # the failure branch writes to stderr, and it must be legible
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives in a PRIVATE submodule; the public repository carries only the Markdown transcription at specs/ISO_COBOL.md. This tool measures the printed page, so it needs the PDF: "
                 "git submodule update --init specs-private")
    import fitz

    sys.path.insert(0, str(HERE))
    import clause_page

    doc = fitz.open(PDF)

    # ---- the grammar side --------------------------------------------------------------------------------
    grammar = parser_rules()                      # ⛔ grammar_source.read refuses an empty population
    rules = {n: r.body for n, r in grammar.items()}
    origin = {n: r.file.name for n, r in grammar.items()}
    needed = required_tokens(rules)
    keywords = keyword_spellings()
    print(f"grammar files scanned: {len(grammar_source.grammar_files())}   parser rules: {len(rules)}   "
          f"rules demanding at least one token: {sum(1 for v in needed.values() if v)}   "
          f"lexer tokens spelling COBOL words: {len(keywords)}")

    def spells_this_construct(rule: str, printed: set[str]) -> bool:
        """Does this rule spell the construct whose format printed `printed`, or is it a same-token stranger?

        ⛔ THE CLOSURE MATCHES ON ONE TOKEN, AND ONE TOKEN IS NOT A CONSTRUCT. The anchor set is "every rule
        demanding the clause title's leading word", so the FILE STATUS clause (§12.4.5.8.2, whose `[FILE]` is
        un-underlined) drew in `deleteFileStatement`, `fileSection`, `multipleFileClause`, `readStatement`,
        `rewriteStatement`, `writeStatement` and `valueOfClause` — seven rules that merely also demand a FILE,
        and all seven were reported against a page none of them is printed on. The SPECIAL-NAMES paragraph did
        the same to `channelClause`, a vendor extension the standard never prints at all, and the INITIALIZE
        statement and the OPTIONS INITIALIZE clause did it to each other in both directions.

        The test is structural and needs no exclusion list: a rule spells THIS construct only if every COBOL
        WORD it demands is printed in this construct's own format. `deleteFileStatement` demands DELETE, which
        the FILE STATUS clause never prints; `channelClause` demands CHANNEL, which the standard never prints;
        `initializeCategoryToValue` demands VALUE, which the OPTIONS INITIALIZE clause never prints. Operand
        tokens (IDENTIFIER, INTEGERLIT …) are exempt by construction — see `keyword_spellings`.
        """
        return all(sp & printed for t in needed[rule] if (sp := keywords.get(t)))

    # ---- the spec side -----------------------------------------------------------------------------------
    formats = declared_formats(SPEC_MD.read_text(encoding="utf-8"))
    heads = clause_page.index(doc)
    where = {}
    for i, h in enumerate(heads):
        where.setdefault(h.clause, i)

    located = [f for f in formats if f.clause in where]
    unresolved = [f for f in formats if f.clause not in where]
    anchored = [f for f in located if f.keyword]
    print(f"general formats declared by the transcription: {len(formats)}   "
          f"located on the printed page: {len(located)}   with an anchor keyword: {len(anchored)}")
    if args.list_unanchored:
        for f in located:
            if not f.keyword:
                print(f"    unanchored  {f.clause:<14} {f.title}")

    selected = located
    if args.construct:
        selected = [f for f in selected if f.keyword == args.construct.upper()]
    if args.clause:
        selected = [f for f in selected if f.clause == args.clause]
    if (args.construct or args.clause) and not selected:
        sys.exit(f"⛔ no located general format matches {args.construct or args.clause!r} — "
                 f"the transcription declares {len(formats)} and {len(located)} resolve to a printed page")

    print(f"formats checked this run: {len(selected)}\n")

    cache: dict[int, list] = {}
    candidates = 0
    measured = 0
    empty: list[Format] = []
    for f in selected:
        i = where[f.clause]
        words = band_words(doc, cache, heads[i], heads[i + 1] if i + 1 < len(heads) else None)
        if not words:
            empty.append(f)
            continue
        measured += 1
        plain, underlined = measure(words)
        if args.show_format:
            print(f"  {f.clause:<14} p{heads[i].page:<5} {f.title}\n"
                  f"      required: {' '.join(sorted(underlined)) or '(none)'}\n"
                  f"      optional: {' '.join(sorted(plain)) or '(none)'}")
        if not plain or not f.keyword:
            continue
        printed = {w.replace("-", "_") for w in plain | underlined}
        tok = f.keyword.replace("-", "_")
        anchors = {n for n in rules if tok in needed[n] and spells_this_construct(n, printed)}
        for rule in sorted(closure(rules, anchors)):
            if not spells_this_construct(rule, printed):
                continue
            for w in sorted(plain):
                # ⛔ THE CONSTRUCT'S OWN ANCHOR KEYWORD IS NEVER A CANDIDATE. It is the word this format was
                # LOCATED by, so the anchor rule demands it by construction and reporting it is circular. It
                # reaches `plain` whenever the printed format leaves the head word un-underlined — folio 604
                # does exactly that to CONTINUE, whose whole format is `CONTINUE [ AFTER … SECONDS ]` with
                # rules under AFTER and SECONDS only (kb/Work PB695).
                if w == f.keyword:
                    continue
                if w.replace("-", "_") in needed[rule]:
                    print(f"  CANDIDATE  {f.keyword:<14} {f.clause:<12} p{heads[i].page:<5} "
                          f"{w:<12} required in {origin[rule]}#{rule}")
                    candidates += 1
    doc.close()

    # ---- ⛔ THE POPULATION ASSERTION ----------------------------------------------------------------------
    # Every count above is checked against the population it claims to have measured. A tool that measured
    # NOTHING must never be able to print a clean zero: that is precisely how this one stayed green while three
    # statements rejected legal COBOL (kb/Work PB332).
    print(f"\n{candidates} candidate(s) — CONFIRM each against the raw rectangles before editing the grammar")
    # (The grammar half of the population — at least one .g4 file and one parser rule — is asserted by
    # `grammar_source.read`, which refuses before a single format is measured.)
    failures: list[str] = []
    if not formats:
        failures.append(f"NO `General format` heading was found in {SPEC_MD} — the transcription declares none")
    if unresolved:
        failures.append(f"{len(unresolved)} declared general format(s) DID NOT RESOLVE to a printed page: "
                        + ", ".join(f.clause for f in unresolved[:12])
                        + (" …" if len(unresolved) > 12 else ""))
    if not anchored:
        failures.append("NO located general format carries an anchor keyword — the clause-title anchor is broken")
    if empty:
        failures.append(f"{len(empty)} located general format(s) measured ZERO words on the page: "
                        + ", ".join(f.clause for f in empty[:12]) + (" …" if len(empty) > 12 else ""))
    if selected and not measured:
        failures.append("every selected general format measured zero words — the audit inspected nothing")
    if failures:
        print("\n⛔ POPULATION ASSERTION FAILED — this run measured less than it claims:", file=sys.stderr)
        for line in failures:
            print(f"   · {line}", file=sys.stderr)
        print("   A clean zero from this tool is only meaningful when it measured the whole population.",
              file=sys.stderr)
        return 1
    print(f"population OK: {measured} general format(s) measured, {len(rules)} parser rules scanned")
    return 0


if __name__ == "__main__":
    sys.exit(main())
