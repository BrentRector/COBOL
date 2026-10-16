#!/usr/bin/env python3
"""THE ONE READER of the ANTLR grammar under `src/Cobol.Net.Frontend/Grammar`: which rules it declares, where, and
with what body.

    python scripts/spec/grammar_source.py              # the population: files, parser, lexer and fragment rules
    python scripts/spec/grammar_source.py --self-test  # prove every trap below is handled, on planted grammar

⛔ WHY ONE MODULE (kb/Work PB3204, an R2 finding of the PB1754 architecture review; DESIGN-architecture-review
§8.2). "Which rules does the grammar declare, where, and with what body" is ONE fact about the grammar, and three
scripts each re-derived it with their own rule-head regex and their own comment handling: the Phase-B dossier
(`phase_b_batch.py`), the frozen-evidence audit (`audit_evidence_supersession.py`) and the optional-word audit
(`audit_grammar_optional_words.py`). The comment lesson two of them had learned never reached the third: the
dossier's end-of-rule test did not strip a trailing comment, so `methodDefinition` (whose `;` line carries a
`// …` note) ran on and swallowed `methodName`, and `cceRelationOrBoolean` swallowed the whole DISPLAY directive
rule — a dossier for either construct quoted another rule's text in its place, and nothing reported the loss.

HOW A RULE IS READ — on the BLANKED text, never line by line:

  · `blank` replaces every comment (`// …`, `/* … */`) and every action or semantic predicate (`{ … }`, nested)
    with spaces, keeping each newline, so a column and a line number mean the same thing in the blanked text as in
    the source. Quoted literals (`'…'`, honouring `\\'`) and lexer character sets (`[…]`, honouring `\\]`) are
    copied through untouched, because each can hold a quote, a slash, a brace or a semicolon that is TEXT, not
    syntax: `SEMI : ';' ;` and `STR_BODY : … ~['\\n] …` are in this grammar.
  · a rule HEAD is an identifier at column 0 (after an optional `fragment`) whose next non-blank character is
    the `:`. That one structural test covers both head styles the grammar uses (`name : …` and the name alone on
    its line with the `:` below it, a comment between them) and excludes every ANTLR declaration (`grammar X;`,
    `options { … }`, `mode M;`, `import …;`) without a list of ANTLR keywords to maintain.
  · the rule ENDS at the first `;` after its `:` that is outside a literal and a character set. Comments and
    actions are already blank, so a trailing `;   // note` and a predicate's `{ f(); }?` cannot end a rule early
    or late — the defect above, removed by construction rather than by a fourth regex.

⛔ THE POPULATION IS ASSERTED: `read` refuses a grammar directory with no `.g4` file or no parser rule, and a rule
whose `;` never comes is an error naming its file and line — never a silently shorter rule list.
"""
from __future__ import annotations

import argparse
import functools
import pathlib
import re
import sys
from typing import NamedTuple

REPO = pathlib.Path(__file__).resolve().parents[2]
GRAMMAR_DIR = REPO / "src" / "Cobol.Net.Frontend" / "Grammar"
#: Build output that can hold COPIES of the grammar (the ANTLR task's `obj/antlr-lib`): reading it would count
#: every rule twice.
BUILD_DIRS = {"obj", "bin"}

#: A rule head on the blanked text: column 0, an optional `fragment`, the name, then (tested separately) the `:`.
HEAD = re.compile(r"^(fragment[ \t]+)?([A-Za-z][A-Za-z0-9_]*)", re.M)
#: The rule's own `:`, after any blank (comment) lines between it and the head.
COLON = re.compile(r"\s*:")


class Rule(NamedTuple):
    """One grammar rule as the source declares it."""

    name: str
    kind: str                  #: "parser" (lower-case head), "lexer" (upper-case head) or "fragment" (a lexer fragment)
    file: pathlib.Path
    first_line: int            #: 1-based line of the head
    last_line: int             #: 1-based line of the terminating `;`
    body: str                  #: from the NAME through the `;`, comments and actions blanked, literals intact
    source: tuple[str, ...]    #: the raw source lines first_line..last_line, comments and all
    lead: tuple[str, ...]      #: the raw `//` comment block directly above the head (citations live there)

    @property
    def rhs(self) -> str:
        """The blanked text between the rule's `:` and its terminating `;` — the alternatives."""
        return self.body[self.body.index(":") + 1:-1]


def _skip_quoted(text: str, i: int, close: str) -> int:
    """The index just past the `close` that ends the literal or set opened at `text[i]`, honouring `\\` escapes."""
    j, n = i + 1, len(text)
    while j < n and text[j] != close:
        j += 2 if text[j] == "\\" else 1
    return min(j + 1, n)


def blank(text: str) -> str:
    """ANTLR source with comments, actions and predicates replaced by spaces; newlines, literals and sets kept."""
    out: list[str] = []
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c == "'":
            j = _skip_quoted(text, i, "'")
        elif c == "[":
            j = _skip_quoted(text, i, "]")
        elif text.startswith("//", i):
            j = text.find("\n", i)
            j = n if j < 0 else j
            out.append(" " * (j - i))
            i = j
            continue
        elif text.startswith("/*", i):
            j = text.find("*/", i + 2)
            j = n if j < 0 else j + 2
            out.append(re.sub(r"[^\n]", " ", text[i:j]))
            i = j
            continue
        elif c == "{":
            depth, j = 1, i + 1
            while j < n and depth:
                depth += {"{": 1, "}": -1}.get(text[j], 0)
                j += 1
            out.append(re.sub(r"[^\n]", " ", text[i:j]))
            i = j
            continue
        else:
            j = i + 1
        out.append(text[i:j])
        i = j
    return "".join(out)


def _terminator(blanked: str, i: int) -> int:
    """The index of the `;` that ends the rule whose body starts at `i`, or -1: literals and sets are skipped."""
    n = len(blanked)
    while i < n:
        c = blanked[i]
        if c == ";":
            return i
        i = _skip_quoted(blanked, i, "'") if c == "'" else _skip_quoted(blanked, i, "]") if c == "[" else i + 1
    return -1


def parse(text: str, file: pathlib.Path) -> list[Rule]:
    """Every rule one `.g4` file declares, in source order."""
    blanked = blank(text)
    raw = text.split("\n")
    rules: list[Rule] = []
    pos = 0
    while (m := HEAD.search(blanked, pos)) is not None:
        colon = COLON.match(blanked, m.end())
        if colon is None:
            pos = m.end()
            continue
        end = _terminator(blanked, colon.end())
        first = blanked.count("\n", 0, m.start()) + 1
        if end < 0:
            raise ValueError(f"{file}:{first}: rule `{m.group(2)}` has no terminating ';'")
        last = blanked.count("\n", 0, end) + 1
        lead: list[str] = []
        j = first - 2
        while j >= 0 and raw[j].lstrip().startswith("//"):
            lead.insert(0, raw[j])
            j -= 1
        name = m.group(2)
        kind = "fragment" if m.group(1) else "parser" if name[0].islower() else "lexer"
        rules.append(Rule(name, kind, file, first, last, blanked[m.start(2):end + 1],
                          tuple(raw[first - 1:last]), tuple(lead)))
        pos = end + 1
    return rules


def grammar_files(grammar_dir: pathlib.Path = GRAMMAR_DIR) -> list[pathlib.Path]:
    """Every `.g4` under the directory, sorted, build output excluded."""
    return sorted(f for f in grammar_dir.rglob("*.g4") if not BUILD_DIRS & set(f.relative_to(grammar_dir).parts))


@functools.lru_cache(maxsize=None)
def read(grammar_dir: pathlib.Path = GRAMMAR_DIR) -> tuple[Rule, ...]:
    """Every rule of every grammar file, read once per process. ⛔ Refuses an empty population."""
    files = grammar_files(grammar_dir)
    if not files:
        raise SystemExit(f"⛔ no .g4 file under {grammar_dir} — a grammar reader that read nothing is not empty, "
                         f"it is broken")
    rules = tuple(r for f in files for r in parse(f.read_text(encoding="utf-8"), f))
    if not any(r.kind == "parser" for r in rules):
        raise SystemExit(f"⛔ {len(files)} grammar file(s) under {grammar_dir} declared NO parser rule")
    return rules


def self_test() -> int:
    """Each trap the three private readers fell into, planted once, plus the real grammar's own invariants."""
    planted = "\n".join([
        "parser grammar Planted;",                                   # 1  a declaration, not a rule
        "options { tokenVocab=PlantedLexer; }",                      # 2  an options block, not a rule
        "// the lead comment, where a citation lives",               # 3
        "methodDefinition",                                          # 4  name alone; the `:` comes later
        "    // a comment between the head and its colon",           # 5
        "    : METHOD_ID methodName",                                # 6
        "    ;   // the terminating ';' carries a trailing comment", # 7  PB3204's case
        "methodName : IDENTIFIER {isOk();}? ;   // a predicate holds a ';'",  # 8
        "fragment Q_BODY : '\\'' (~['\\n] | '\\'\\'')* '\\'' ;",     # 9  a set holding a quote
        "SEMI : ';' ;",                                              # 10 a literal holding a ';'
        "/* a block comment",                                        # 11
        "   ghostRule : NOT_A_RULE ; */",                            # 12 inside a comment: no rule
        "mode PLANTED_MODE;",                                        # 13 a mode, not a rule
        "lastRule: A",                                               # 14
        "  | B ;",                                                   # 15
    ])
    got = {r.name: r for r in parse(planted, pathlib.Path("Planted.g4"))}
    checks = [
        ("only the five real rules are read (no declaration, mode or commented-out rule)",
         sorted(got) == sorted(["methodDefinition", "methodName", "Q_BODY", "SEMI", "lastRule"])),
        ("a ';' with a trailing comment ends its rule, so the NEXT rule is still read (PB3204)",
         "methodName" in got and (got["methodDefinition"].first_line, got["methodDefinition"].last_line) == (4, 7)),
        ("a head whose ':' follows a comment line is a rule", "methodDefinition" in got),
        ("the lead comment block is the raw `//` lines directly above the head",
         got.get("methodDefinition", got["SEMI"]).lead == ("// the lead comment, where a citation lives",)),
        ("a predicate's ';' does not end its rule, and the predicate is blanked",
         "isOk" not in got["methodName"].body and got["methodName"].last_line == 8),
        ("a fragment is read as a fragment, through a set holding a quote", got.get("Q_BODY", got["SEMI"]).kind
         == "fragment" and got["Q_BODY"].last_line == 9),
        ("a literal ';' is text, not the terminator", got["SEMI"].rhs.strip() == "';'"),
        ("kinds follow the head's case", got["SEMI"].kind == "lexer" and got["lastRule"].kind == "parser"),
        ("a multi-line rule's body and source span its lines",
         got["lastRule"].rhs.split() == ["A", "|", "B"] and len(got["lastRule"].source) == 2),
        ("the comment is blanked from the body but kept in the source",
         "trailing" not in got["methodDefinition"].body and "trailing" in got["methodDefinition"].source[-1]),
    ]
    try:
        parse("broken : A B\n", pathlib.Path("Broken.g4"))
        checks.append(("an unterminated rule is an error, never a silently shorter list", False))
    except ValueError:
        checks.append(("an unterminated rule is an error, never a silently shorter list", True))
    rules = read()
    spans: dict[pathlib.Path, list[Rule]] = {}
    for r in rules:
        spans.setdefault(r.file, []).append(r)
    checks += [
        ("the real grammar: every rule's span is in order and no two rules of one file overlap",
         all(a.last_line < b.first_line and a.first_line <= a.last_line
             for rs in spans.values() for a, b in zip(rs, rs[1:]))),
        ("the real grammar: every rule body starts with its own name and ends with its ';'",
         all(r.body.startswith(r.name) and r.body.endswith(";") for r in rules)),
    ]
    private = private_readers()
    checks.append((f"no other script declares a private grammar reader ({', '.join(PRIVATE_READER_NAMES)})"
                   + (f": {', '.join(private)}" if private else ""), not private))
    ok = True
    for label, passed in checks:
        print(f"  {'ok  ' if passed else 'FAIL'} {label}")
        ok &= passed
    print("SELF-TEST:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


#: The names the three private readers this module replaced were declared under (kb/Work PB3204). One of them
#: reappearing in another script is the duplication coming back, so the self-test refuses it.
PRIVATE_READER_NAMES = ("grammar_rules", "strip_comments", "RULE_HEAD", "LEXER_RULE")


def private_readers(root: pathlib.Path = REPO / "scripts") -> list[str]:
    """`file:line name` for every top-level function or constant under `root` declared with a retired reader's name."""
    import ast
    here = pathlib.Path(__file__).resolve()
    found = []
    for f in sorted(root.rglob("*.py")):
        if f.resolve() == here:
            continue
        for node in ast.parse(f.read_text(encoding="utf-8"), str(f)).body:
            names = ([node.name] if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)) else
                     [t.id for t in node.targets if isinstance(t, ast.Name)] if isinstance(node, ast.Assign) else [])
            found += [f"{f.relative_to(REPO).as_posix()}:{node.lineno} {n}" for n in names
                      if n in PRIVATE_READER_NAMES]
    return found


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true", help="prove every trap is handled, on planted grammar")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    rules = read()
    kinds = {k: sum(1 for r in rules if r.kind == k) for k in ("parser", "lexer", "fragment")}
    print(f"{len(grammar_files())} grammar file(s) under {GRAMMAR_DIR.relative_to(REPO).as_posix()}: "
          + " · ".join(f"{n} {k} rule(s)" for k, n in kinds.items()))
    return 0


if __name__ == "__main__":
    sys.exit(main())
