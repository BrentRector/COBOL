#!/usr/bin/env python3
"""generate.py — the ONE reader of the conformance-suite test headers (DESIGN-conformance-suite.md §3, §4, §9).

    python conformance-suite/harness/generate.py            # refuse, or write the manifests and print completeness
    python conformance-suite/harness/generate.py --out DIR  # the same, written to DIR instead of suites/
    python conformance-suite/harness/generate.py --check    # the refusals only, writing nothing (a gate audit)
    python conformance-suite/harness/generate.py --self-test

A test is one COBOL program under `cases/<clause>/` (or `wiseowl/cases/<clause>/`, WiseOwl's own regression tests)
whose metadata block opens the file (§3):

          *>--- conformance-case
          *> id: compute-multi-receiver-size-error
          *> rules: [GR-14.9.8.4-2]
          *> accept: [85, 2002, 2014, 2023]
          *> source-format: fixed
          *>---

In fixed form each block line has `*` in column 7, so every compiler at every edition reads it as a comment line; in
free form it starts with `*>`. The fields, their meaning and every refusal are the design's §3.1 and §4; this module is
their one implementation, and the manifests it writes are the only input the runners read (§5, §10: never two parsers
of the headers).

WHAT IT WRITES (build output, never committed; `.gitignore` names `conformance-suite/suites/`):
  suites/<edition>/manifest.json  the suite of that edition: every portable test of `cases/` (not `_unassigned/`, not
                                  `portable: unverified`) that is accepted or flagged there, with its expectation.
  suites/wiseowl/manifest.json    everything WiseOwl COBOL runs: every test of both trees at every edition it names,
                                  portable or not, and `wiseowl/diagnostics.json`'s refusals (§4).
  suites/completeness.json        the completeness report (below), which it also prints.
`--out DIR` writes the same tree to DIR instead: WiseOwl's in-process runner (S1c) generates into a directory of its
own, so a test run never rewrites the repository's `suites/` under a concurrent `run.py`. Whichever directory it
writes, a previous run's manifest directories and report there are replaced, and nothing else is ever removed: a
directory that holds anything else (or a path that is not a directory) is refused, exit 2, with nothing deleted.

THE COMPLETENESS REPORT (§4) is per edition and per WITNESS KIND, never one count: each inventory row whose band
holds the edition is counted once, under the first of these that holds — documented non-support ("unwitnessed until
implemented"); a suite test; a documented determination (docs/CONFORMANCE.md §7); an owner-signed derivation; only
WiseOwl's own tests (a spec-derived test-ref, or a test of the wiseowl manifest); no witness. The suite's own measure is
the rows a program can observe (all but documented non-support and derivations) witnessed by a suite test.

`--check` is an AUDIT of the gate (scripts/run_gate_legs.py AUDITS) and of CI's `audits` job (§9): it reads only the
tree, so a refused test is red in seconds, before the gate slot and the build. `--self-test` is discovered by
scripts/self_tests.py.
"""
from __future__ import annotations

import argparse
import itertools
import json
import re
import shutil
import sys
import tempfile
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Callable

HARNESS = Path(__file__).resolve().parent
SUITE = HARNESS.parent
REPO = SUITE.parent
sys.path.insert(0, str(HARNESS))
sys.path.insert(0, str(REPO / "scripts" / "spec"))

import cobol_source as cobol  # noqa: E402  (this directory's: the program-text facts the refusals need)
import config  # noqa: E402  (this directory's: the harness's adapter, profile and baseline formats)
import generated_output  # noqa: E402  (the one way a generator clears the directory it writes: never wholesale)
import inventory_schema  # noqa: E402  (the inventory's one schema: its editions, its evidence predicates)

CONSTRUCTS_PATH = REPO / "tests" / "version-matrix" / "constructs.json"
RESERVED_WORDS_PATH = REPO / "tests" / "version-matrix" / "reserved-words.json"
SPEC_PATH = REPO / "specs" / "ISO_COBOL.md"
IMPLEMENTOR_DEFINED_PATH = SUITE / "implementor-defined.json"
DIAGNOSTICS_PATH = SUITE / "wiseowl" / "diagnostics.json"
OUT = SUITE / "suites"
VERDICT = "=== CONFORMANCE SUITE: "
#: The completeness report's file name, beside the manifest directories in the output directory.
COMPLETENESS_NAME = "completeness.json"

#: The two trees of tests: the suites' (`cases/`) and WiseOwl's own regression tests (`wiseowl/cases/`, §4).
TREES = {"cases": Path("cases"), "wiseowl": Path("wiseowl") / "cases"}
UNASSIGNED = "_unassigned"
#: A clause folder: the ISO clause number, then a hyphen and a name (`14.9.8-COMPUTE`), or `_unassigned`.
CLAUSE_FOLDER = re.compile(r"^(?P<clause>[0-9]+(?:\.[0-9]+)*)-[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$")
TEST_ID = re.compile(r"^[A-Za-z0-9][A-Za-z0-9_-]*$")
OPEN_MARK, CLOSE_MARK = "*>--- conformance-case", "*>---"
#: The fields of §3.1 (and `constructs`), each with its value's shape.
FIELDS = {
    "id": "scalar", "rules": "list", "accept": "list", "flag": "map", "control": "scalar", "requires": "list",
    "implementor-defined": "list", "files": "list", "source-format": "scalar", "portable": "scalar",
    "constructs": "list",
}
FLAG_FIELDS = {"kind": "scalar", "at": "list", "why": "scalar"}
#: §3.2: the kinds of standard-conformance diagnostic a flagged test requires.
FLAG_KINDS = ("syntax", "obsolete", "archaic", "nonstandard", "processor-dependent")
#: §3.2: only syntax rules and general formats may carry `flag: syntax` (a general rule needs `why`).
SYNTAX_RULE_KINDS = ("SR", "FMT")
SOURCE_FORMATS = ("fixed", "free")
#: The editions at which free form exists (ISO 6.4; free-form reference format is a 2002 introduction).
FIRST_FREE_FORM = "2002"
#: §3.2: run-time violations are judged only from 2002 on; at 85 a violated rule's result is undefined.
FIRST_RUN_TIME = "2002"
#: §3.2: the fixed-form marked line carries `<<` at the start of its sequence number area, which "may consist of
#: any character in the computer's coded character set" at every edition (ISO 6.3.2); a free-form line ends with the
#: floating comment `*> <<`.
FIXED_MARK = "<<"
FREE_MARK = re.compile(r"\*>\s*<<\s*$")
OUTPUT_PART = re.compile(r"^(?P<item>[0-9]+)=(?P<value>[a-z0-9]+(?:-[a-z0-9]+)*)$")
REQUIRES = re.compile(r"^(?:A\.4\.(?P<module>[0-9]+)|A\.3-(?P<element>[0-9]+))$")


# ── the model ────────────────────────────────────────────────────────────────────────────────────────────────────────

@dataclass(frozen=True)
class Refusal:
    path: str
    line: int
    message: str

    def __str__(self) -> str:
        return f"{self.path}:{self.line}: {self.message}"


@dataclass(frozen=True)
class Flag:
    kind: str
    at: tuple[str, ...]
    why: str | None


@dataclass
class Case:
    """One test, as its header and its directory state it."""

    tree: str  # "cases" or "wiseowl"
    folder: str  # the clause folder's name
    path: Path  # the .cob, relative to the suite root
    test_id: str
    source_format: str
    rules: tuple[str, ...]
    accept: tuple[str, ...]
    flag: Flag | None
    control: str | None
    requires: tuple[str, ...]
    implementor_defined: tuple[int, ...]
    files: tuple[str, ...]
    constructs: tuple[str, ...]
    portable: bool
    marked_line: int | None
    #: `(edition or None, ((item, value), ...)) -> .out path`: every expected-output file, parsed from its name.
    outputs: dict[tuple[str | None, tuple[tuple[int, str], ...]], Path] = field(default_factory=dict)
    #: The COBOL words of the program and of the library text it copies (upper case, each holding a letter), and the
    #: user-defined words among them that the program defines (cobol_source.defined_names): what `entry_for` reduces to
    #: the words a runner checks against a profile's extra reserved words (§5), so no runner reads the program.
    words: frozenset[str] = frozenset()
    defined: frozenset[str] = frozenset()

    @property
    def in_suite(self) -> bool:
        """Does this test belong to the per-edition suites (§4)? Only a cleared test of `cases/` with a clause."""
        return self.tree == "cases" and self.folder != UNASSIGNED and self.portable


@dataclass
class Inputs:
    """What the refusals and the report read besides the tests: the inventory, the construct registry, the annexes."""

    schema: inventory_schema.Schema
    rows: dict[str, dict[str, Any]]
    constructs: dict[str, tuple[int, int | None]]
    #: `A.4.<n>` for each optional module and `A.3-<n>` for each processor-dependent element the standard lists.
    requirable: frozenset[str]
    #: Annex A.1 item -> the values the standard enumerates for it (implementor-defined.json).
    implementor_defined: dict[int, tuple[str, ...]]
    #: wiseowl/diagnostics.json: test id -> {"refuse-at": [...], "expect": "..."}.
    diagnostics: dict[str, dict[str, Any]]
    #: The two evidence predicates of the report, injectable so the self-test can drive every witness kind.
    determined: Callable[[dict[str, Any]], bool]
    derived: Callable[[dict[str, Any]], bool]
    #: edition -> the words the standard reserves there (tests/version-matrix/reserved-words.json), and the standard's
    #: other keywords (its context-sensitive words, intrinsic function names and compiler-directive words, ISO 8.10-8.12).
    #: A manifest entry's `words` are the program's words that are none of these, and the names it defines that its
    #: edition does not reserve: the uses of a user-defined word a compiler's added reserved word can stop (ISO 4.2.10).
    reserved: dict[str, frozenset[str]] = field(default_factory=dict)
    keywords: frozenset[str] = frozenset()

    @property
    def editions(self) -> list[str]:
        return self.schema.editions

    def band(self, rule_id: str) -> list[str]:
        return self.schema.split(self.rows[rule_id].get("editions", ""), self.schema.editions_sep)

    def window(self, construct: str) -> list[str]:
        """The editions at which a construct exists: introducedIn <= V < removedIn (VERSION_TEST_MATRIX_DESIGN.md §2)."""
        start, end = self.constructs[construct]
        return [e for e in self.editions if int(e) >= start and (end is None or int(e) < end)]


# ── the header ───────────────────────────────────────────────────────────────────────────────────────────────────────

_VALUE_TOKEN = re.compile(r'\s*(?:(?P<punct>[\[\]{},:])|"(?P<quoted>(?:[^"]|"")*)"|(?P<bare>[^\s\[\]{},:"]+))')


class HeaderValueError(Exception):
    """A header value that is not a scalar, a [list] or a {map}."""


def parse_value(text: str) -> Any:
    """A header value: a scalar, a flow list `[a, b]` or a flow map `{k: v, k: [a]}` (the YAML flow subset §3 uses)."""
    tokens: list[tuple[str, str]] = []
    pos = 0
    text = text.strip()
    while pos < len(text):
        m = _VALUE_TOKEN.match(text, pos)
        if not m or m.end() == pos:
            raise HeaderValueError(f"cannot read {text[pos:]!r}")
        pos = m.end()
        if m.group("punct"):
            tokens.append(("p", m.group("punct")))
        elif m.group("quoted") is not None:
            tokens.append(("s", m.group("quoted").replace('""', '"')))
        elif m.group("bare"):
            tokens.append(("s", m.group("bare")))
    if not tokens:
        raise HeaderValueError("an empty value")
    value, rest = _parse(tokens, 0)
    if rest != len(tokens):
        raise HeaderValueError(f"text after the value: {text!r}")
    return value


def _parse(tokens: list[tuple[str, str]], i: int) -> tuple[Any, int]:
    kind, text = tokens[i]
    if kind == "s":
        return text, i + 1
    if text == "[":
        items: list[Any] = []
        i += 1
        while i < len(tokens) and tokens[i] != ("p", "]"):
            item, i = _parse(tokens, i)
            items.append(item)
            if i < len(tokens) and tokens[i] == ("p", ","):
                i += 1
            elif i < len(tokens) and tokens[i] != ("p", "]"):
                raise HeaderValueError("a list's items are separated by ','")
        if i >= len(tokens):
            raise HeaderValueError("a list without its ']'")
        return items, i + 1
    if text == "{":
        entries: dict[str, Any] = {}
        i += 1
        while i < len(tokens) and tokens[i] != ("p", "}"):
            if tokens[i][0] != "s" or i + 1 >= len(tokens) or tokens[i + 1] != ("p", ":"):
                raise HeaderValueError("a map entry is 'key: value'")
            key = tokens[i][1]
            if key in entries:
                raise HeaderValueError(f"the key {key!r} twice")
            entries[key], i = _parse(tokens, i + 2)
            if i < len(tokens) and tokens[i] == ("p", ","):
                i += 1
            elif i < len(tokens) and tokens[i] != ("p", "}"):
                raise HeaderValueError("a map's entries are separated by ','")
        if i >= len(tokens):
            raise HeaderValueError("a map without its '}'")
        return entries, i + 1
    raise HeaderValueError(f"unexpected {text!r}")


@dataclass
class Header:
    fields: dict[str, Any]
    lines: dict[str, int]
    source_format_seen: str  # the form the block itself is written in: "fixed" or "free"
    end_line: int


def _block_line(raw: str) -> tuple[str | None, str]:
    """The form a metadata line is written in and its text from `*>` on: fixed form puts the `*` in column 7 (the
    indicator area, so the line is a comment line at every edition; columns 1-6 are the sequence number area), free
    form puts `*>` in column 1."""
    if raw[6:8] == "*>":
        return "fixed", raw[6:].rstrip()
    if raw.startswith("*>"):
        return "free", raw.rstrip()
    return None, raw


def read_header(text: str) -> tuple[Header | None, list[tuple[int, str]]]:
    """The metadata block that opens `text` (§3), and every problem with its form, as (line, message)."""
    problems: list[tuple[int, str]] = []
    lines = [line.rstrip("\r") for line in text.split("\n")]
    seen, first = _block_line(lines[0])
    if seen is None or first != OPEN_MARK:
        return None, [(1, f"the file does not open with the metadata block's first line `{OPEN_MARK}`, its `*>` in "
                          f"column 7 (fixed form) or column 1 (free form) (§3)")]
    fields: dict[str, Any] = {}
    where: dict[str, int] = {}
    for number, raw in enumerate(lines[1:], start=2):
        form, line = _block_line(raw)
        if form is None:
            problems.append((number, f"the metadata block ends without its closing line `{CLOSE_MARK}`"))
            return None, problems
        if form != seen:
            problems.append((number, f"a metadata line in {form} form inside a block that opened in {seen} form"))
        body = line[2:]
        if line == CLOSE_MARK:
            return Header(fields, where, seen, number), problems
        if not body.startswith(" ") or ":" not in body:
            problems.append((number, f"a metadata line is `*> <field>: <value>`, not {line!r}"))
            continue
        key, _, value = body.strip().partition(":")
        key = key.strip()
        if key not in FIELDS:
            problems.append((number, f"unknown field {key!r} (the fields are §3.1's: {', '.join(FIELDS)})"))
            continue
        if key in fields:
            problems.append((number, f"the field {key!r} twice"))
            continue
        try:
            fields[key] = parse_value(value)
            where[key] = number
        except HeaderValueError as e:
            problems.append((number, f"{key}: {e}"))
    problems.append((len(lines), f"the metadata block has no closing line `{CLOSE_MARK}`"))
    return None, problems


# ── reading a tree ───────────────────────────────────────────────────────────────────────────────────────────────────

class Reader:
    """Reads every test of both trees under `root` and states every refusal of §4, as data."""

    def __init__(self, root: Path, inputs: Inputs) -> None:
        self.root = root
        self.inputs = inputs
        self.refusals: list[Refusal] = []
        self.cases: list[Case] = []
        self.sources: dict[str, str] = {}

    def refuse(self, path: Path | str, line: int, message: str) -> None:
        self.refusals.append(Refusal(str(path).replace("\\", "/"), line, message))

    # ── shape of a single field ──

    def _shaped(self, rel: Path, header: Header, key: str, shape: str) -> Any:
        value = header.fields.get(key)
        if value is None:
            return None
        ok = (shape == "scalar" and isinstance(value, str)) or (shape == "list" and isinstance(value, list) and all(
            isinstance(v, str) for v in value)) or (shape == "map" and isinstance(value, dict))
        if not ok:
            self.refuse(rel, header.lines[key], f"{key} is a {shape}, not {value!r}")
            return None
        return value

    def _editions(self, rel: Path, line: int, key: str, values: list[str]) -> tuple[str, ...]:
        """A list of editions in the schema's one order, each once (the inventory's spelling rule, kb/Work PB3172)."""
        if not values:
            return ()
        try:
            spelled = self.inputs.schema.spell_editions(values)
        except ValueError as e:
            self.refuse(rel, line, f"{key}: {e}")
            return ()
        if spelled.split(self.inputs.schema.editions_sep) != values:
            self.refuse(rel, line, f"{key} lists its editions in ascending order: [{spelled.replace(',', ', ')}]")
        return tuple(spelled.split(self.inputs.schema.editions_sep)) if spelled else ()

    # ── one test ──

    def read_case(self, tree: str, folder: str, cob: Path) -> Case | None:
        rel = cob.relative_to(self.root)
        text = cob.read_text(encoding="utf-8")
        header, problems = read_header(text)
        for line, message in problems:
            self.refuse(rel, line, message)
        if header is None:
            return None
        h = header
        test_id = self._shaped(rel, h, "id", "scalar")
        if test_id is None:
            self.refuse(rel, 1, "no id")
            return None
        if test_id != cob.stem:
            self.refuse(rel, h.lines["id"], f"id {test_id!r} is not the file's base name {cob.stem!r} (§3.1)")
        if not TEST_ID.match(test_id):
            self.refuse(rel, h.lines["id"], f"id {test_id!r} is letters, digits, '-' and '_' (an expected output "
                                            f"is named <id>.<...>.out, so an id holds no '.')")
        fmt = self._shaped(rel, h, "source-format", "scalar")
        if fmt is None:
            self.refuse(rel, 1, "no source-format: every test states it, because compilers differ in their default "
                                "(§3.1)")
            fmt = h.source_format_seen
        elif fmt not in SOURCE_FORMATS:
            self.refuse(rel, h.lines["source-format"], f"source-format is fixed or free, not {fmt!r}")
            fmt = h.source_format_seen
        elif fmt != h.source_format_seen:
            self.refuse(rel, h.lines["source-format"], f"source-format is {fmt} but the metadata block is written in "
                                                       f"{h.source_format_seen} form")
        rules = tuple(self._shaped(rel, h, "rules", "list") or ())
        if "rules" not in h.fields:
            self.refuse(rel, 1, "no rules: every test names the inventory rule-ids it witnesses (`rules: []` only in "
                                f"{UNASSIGNED}/)")
        accept = self._editions(rel, h.lines.get("accept", 1), "accept", self._shaped(rel, h, "accept", "list") or [])
        flag = None
        raw_flag = self._shaped(rel, h, "flag", "map")
        if raw_flag is not None:
            flag = self._flag(rel, h.lines["flag"], raw_flag)
        portable_value = self._shaped(rel, h, "portable", "scalar")
        if portable_value is not None and portable_value != "unverified":
            self.refuse(rel, h.lines["portable"], f"portable is `unverified` or absent, not {portable_value!r} (§3.1)")
        items = []
        for v in self._shaped(rel, h, "implementor-defined", "list") or []:
            if not v.isdigit():
                self.refuse(rel, h.lines["implementor-defined"], f"implementor-defined names Annex A.1 item numbers, "
                                                                 f"not {v!r}")
            else:
                items.append(int(v))
        case = Case(
            tree=tree, folder=folder, path=rel, test_id=test_id, source_format=fmt, rules=rules, accept=accept,
            flag=flag, control=self._shaped(rel, h, "control", "scalar"),
            requires=tuple(self._shaped(rel, h, "requires", "list") or ()), implementor_defined=tuple(items),
            files=tuple(self._shaped(rel, h, "files", "list") or ()),
            constructs=tuple(self._shaped(rel, h, "constructs", "list") or ()),
            portable=portable_value is None, marked_line=None)
        self.sources[test_id] = text
        self._check_program(case, h, text)
        return case

    def _flag(self, rel: Path, line: int, raw: dict[str, Any]) -> Flag | None:
        for key in raw:
            if key not in FLAG_FIELDS:
                self.refuse(rel, line, f"flag has kind, at and why, not {key!r} (§3.2)")
        kind = raw.get("kind")
        if kind not in FLAG_KINDS:
            self.refuse(rel, line, f"flag kind is one of {', '.join(FLAG_KINDS)}, not {kind!r} (§3.2)")
            return None
        at = raw.get("at")
        if not isinstance(at, list) or not at or not all(isinstance(e, str) for e in at):
            self.refuse(rel, line, "flag at lists the editions at which the diagnostic is required (§3.2)")
            return None
        why = raw.get("why")
        if why is not None and not isinstance(why, str):
            self.refuse(rel, line, "flag why is a sentence")
            why = None
        return Flag(kind, self._editions(rel, line, "flag at", at), why)

    # ── the refusals that read the program text ──

    def _check_program(self, case: Case, h: Header, text: str) -> None:
        rel = case.path
        lines = cobol.read_lines(text, case.source_format)
        tokens = cobol.tokenize(lines)
        # The marked line (§3.2).
        marks = []
        for number, raw in enumerate(text.split("\n"), start=1):
            raw = raw.rstrip("\r")
            if number <= h.end_line:
                continue
            if (case.source_format == "fixed" and raw[:cobol.SEQUENCE_AREA].startswith(FIXED_MARK)) or (
                    case.source_format == "free" and FREE_MARK.search(raw)):
                marks.append(number)
        if case.flag is not None:
            if len(marks) != 1:
                how = ("`<<` opening the sequence number area (columns 1-6)" if case.source_format == "fixed"
                       else "a closing floating comment `*> <<`")
                self.refuse(rel, h.lines["flag"], f"a flagged test marks exactly one line, with {how}; it marks "
                                                  f"{len(marks)} (§3.2)")
            else:
                case.marked_line = marks[0]
        elif marks:
            self.refuse(rel, marks[0], "a marked line `<<` in a test that is not flagged")
        # COPY of an undeclared copybook (§4).
        declared = set(case.files)
        copies = self._all_copies(case, tokens)
        for stmt in copies:
            if not any(f == stmt.name or Path(f).stem == stmt.name for f in declared):
                self.refuse(stmt.source or rel, stmt.line, f"COPY {stmt.name}: the library text is not a declared "
                                                           f"file of this test (`files`, §3.1)")
        # A run-time expectation at 85 (§3.2), in the program or in the library text it copies.
        expanded = cobol.expand_copies(tokens, lambda name: self._library(case, name))
        case.words = frozenset(t.text.upper() for t in expanded
                               if t.kind == cobol.WORD and any(ch.isalpha() for ch in t.text))
        case.defined = frozenset(cobol.defined_names(expanded))
        observed = cobol.observes_exception(expanded)
        if observed is not None:
            early = [e for e in case.accept if self.inputs.editions.index(e) < self.inputs.editions.index(
                FIRST_RUN_TIME)]
            if early:
                self.refuse(observed.source or rel, observed.line,
                            f"a run-time expectation (it observes an exception condition) accepted at "
                            f"{', '.join(early)}: run-time violations are judged only from {FIRST_RUN_TIME} on (§3.2)")
        # Free form before it existed.
        if case.source_format == "free":
            first = self.inputs.editions.index(FIRST_FREE_FORM)
            early = sorted({e for e in case.accept + (case.flag.at if case.flag else ())
                            if self.inputs.editions.index(e) < first}, key=self.inputs.editions.index)
            if early:
                self.refuse(rel, h.lines.get("source-format", 1), f"free form at {', '.join(early)}: the free-form "
                                                                  f"reference format is a {FIRST_FREE_FORM} feature")
        # The portability lint (§3.3), for a test the suites carry: on the program text the compiler reads, the
        # test's own and every library text it copies.
        if case.in_suite:
            if case.source_format == "fixed":
                texts = [(rel, text)] + [t for t in (self._library_text(case, n) for n in
                                                     dict.fromkeys(s.name for s in copies)) if t is not None]
                for path, program_text in texts:
                    for number in cobol.program_text_past_margin(program_text):
                        self.refuse(path, number, f"program text past column {cobol.FIXED_TEXT_END}: margin R is "
                                                  f"implementor-defined (ISO 6.3.1)")
            for issue in cobol.display_issues(expanded):
                self.refuse(issue.source or rel, issue.line, f"portability lint: DISPLAY {issue.operand}: "
                                                             f"{issue.why} (§3.3; ISO 14.9.11.4 GR 1 makes its device "
                                                             f"form the implementor's)")

    def _library_text(self, case: Case, name: str) -> tuple[Path, str] | None:
        """The declared library text `name` names (a file of `files`, by its name or its stem): its path relative to
        the tree and its text, or None when no declared file is there."""
        for f in case.files:
            if f == name or Path(f).stem == name:
                rel = case.path.parent / f
                path = self.root / rel
                return (rel, path.read_text(encoding="utf-8")) if path.is_file() else None
        return None

    def _library(self, case: Case, name: str) -> list[cobol.Token] | None:
        found = self._library_text(case, name)
        if found is None:
            return None
        rel, text = found
        return cobol.tokenize(cobol.read_lines(text, case.source_format), _posix(rel))

    def _all_copies(self, case: Case, tokens: list[cobol.Token]) -> list[cobol.CopyStatement]:
        """The COPY statements of the program and, recursively, of the declared library texts it copies."""
        found = list(cobol.copy_statements(tokens))
        seen: set[str] = set()
        queue = [s.name for s in found]
        while queue:
            name = queue.pop()
            if name in seen:
                continue
            seen.add(name)
            included = self._library(case, name)
            if included is not None:
                nested = cobol.copy_statements(included)
                found.extend(nested)
                queue.extend(s.name for s in nested)
        return found

    # ── a folder ──

    def read_tree(self) -> None:
        if not (self.root / TREES["cases"]).is_dir():
            # A missing tree is a refusal, never a GREEN over nothing: the audit would pass a deleted suite.
            self.refuse(TREES["cases"], 1, "there is no cases/ tree: the suites' tests are missing")
        for tree, sub in TREES.items():
            base = self.root / sub
            if not base.is_dir():
                continue
            for folder in sorted(p for p in base.iterdir()):
                rel_folder = folder.relative_to(self.root)
                if not folder.is_dir():
                    self.refuse(rel_folder, 1, "a file directly under the tree: every test lives in a clause folder")
                    continue
                if folder.name != UNASSIGNED and not CLAUSE_FOLDER.match(folder.name):
                    self.refuse(rel_folder, 1, f"a clause folder is named <ISO clause>-<Name> (`14.9.8-COMPUTE`) or "
                                               f"{UNASSIGNED}, not {folder.name!r} (§4)")
                    continue
                self._read_folder(tree, folder)

    def _read_folder(self, tree: str, folder: Path) -> None:
        cases = []
        for cob in sorted(folder.glob("*.cob")):
            case = self.read_case(tree, folder.name, cob)
            if case is not None:
                cases.append(case)
        claimed: set[str] = set()
        for case in cases:
            claimed.add(case.path.name)
            claimed.update(case.files)
            self._outputs(case, folder)
            claimed.update(p.name for p in case.outputs.values())
        for f in sorted(folder.iterdir()):
            rel = f.relative_to(self.root)
            if f.is_dir():
                self.refuse(rel, 1, "a folder inside a clause folder: a test's files lie beside it")
            elif f.name not in claimed and not f.name.endswith(".cob"):
                self.refuse(rel, 1, "a file no test of this folder declares (`files`) or expects as its output")
        for case in cases:
            for name in case.files:
                if not (folder / name).is_file():
                    self.refuse(case.path, 1, f"files names {name!r}, which is not beside the test")
                if "/" in name or "\\" in name:
                    self.refuse(case.path, 1, f"files names {name!r}: a declared file lies beside the test")
        self.cases.extend(cases)

    def _outputs(self, case: Case, folder: Path) -> None:
        """The expected-output files `<id>[.<edition>][.<item>=<value>...].out` (§3.1, §3.3)."""
        for out in sorted(folder.glob(case.test_id + ".*out")):
            parts = out.name[len(case.test_id):-len(".out")]
            if not out.name.endswith(".out") or not (parts == "" or parts.startswith(".")):
                continue
            parts_list = [p for p in parts.split(".") if p] if parts else []
            edition = None
            if parts_list and parts_list[0] in self.inputs.editions:
                edition = parts_list.pop(0)
            assignment = []
            bad = False
            for p in parts_list:
                m = OUTPUT_PART.match(p)
                if not m:
                    self.refuse(out.relative_to(self.root), 1, f"an expected output is <id>[.<edition>]"
                                                               f"[.<item>=<value>].out; {p!r} is neither")
                    bad = True
                    break
                assignment.append((int(m.group("item")), m.group("value")))
            if not bad:
                case.outputs[(edition, tuple(sorted(assignment)))] = out.relative_to(self.root)


# ── the refusals that read more than one test ────────────────────────────────────────────────────────────────────────

def check_cases(reader: Reader) -> None:
    """Every refusal of §4 that relates a test to the inventory, the registry, its control or the other tests."""
    inputs = reader.inputs
    by_id: dict[str, Case] = {}
    for case in reader.cases:
        if case.test_id in by_id:
            reader.refuse(case.path, 1, f"id {case.test_id!r} is also {by_id[case.test_id].path} (ids are unique "
                                        f"across both trees)")
        else:
            by_id[case.test_id] = case
    for case in reader.cases:
        rel = case.path
        has_diagnostic = case.test_id in inputs.diagnostics
        if not case.accept and case.flag is None and not has_diagnostic:
            reader.refuse(rel, 1, "the test is neither accepted nor flagged anywhere, and wiseowl/diagnostics.json "
                                  "has no entry for it")
        if case.flag is not None and set(case.flag.at) & set(case.accept):
            both = sorted(set(case.flag.at) & set(case.accept), key=inputs.editions.index)
            reader.refuse(rel, 1, f"accepted and flagged at {', '.join(both)}")
        # Rules: known, filed under this clause, present at each edition the test is judged at.
        for rule in case.rules:
            if rule not in inputs.rows:
                reader.refuse(rel, 1, f"rules names {rule}, which the traceability inventory does not have")
        known = [r for r in case.rules if r in inputs.rows]
        if case.folder != UNASSIGNED:
            if not case.rules:
                reader.refuse(rel, 1, f"a test with no rules belongs in {UNASSIGNED}/ (§4, §6 step 1)")
            m = CLAUSE_FOLDER.match(case.folder)
            if m and known and not any(inventory_schema.section_matches(m.group("clause"), inputs.rows[r]["section"])
                                       for r in known):
                reader.refuse(rel, 1, f"filed under clause {m.group('clause')}, which none of its rules belongs to "
                                      f"(move it to the clause of its rules)")
        for e in case.accept:
            for r in known:
                if e not in inputs.band(r):
                    reader.refuse(rel, 1, f"accepted at {e}, where {r} does not exist (its band is "
                                          f"{inputs.rows[r]['editions']})")
        # The construct registry: f(case, V) of VERSION_TEST_MATRIX_DESIGN.md §2.
        windows = {}
        for c in case.constructs:
            if c not in inputs.constructs:
                reader.refuse(rel, 1, f"constructs names {c!r}, which tests/version-matrix/constructs.json does not "
                                      f"have")
            else:
                windows[c] = inputs.window(c)
        for e in case.accept:
            for c, window in windows.items():
                if e not in window:
                    reader.refuse(rel, 1, f"accepted at {e}, where the construct {c} does not exist (constructs.json: "
                                          f"{', '.join(window) or 'no edition'})")
        if case.flag is not None:
            _check_flag(reader, case, known, windows, by_id)
        elif case.control is not None:
            reader.refuse(rel, 1, "control names the accepted twin of a FLAGGED test; this test is not flagged")
        # Annexes.
        for name in case.requires:
            if name not in inputs.requirable:
                reader.refuse(rel, 1, f"requires names {name!r}, not an Annex A.4 module `A.4.<n>` or an Annex A.3 "
                                      f"element `A.3-<n>` of the standard")
        _check_outputs(reader, case)
    # wiseowl/diagnostics.json (§9).
    for test_id, entry in inputs.diagnostics.items():
        case = by_id.get(test_id)
        if case is None:
            reader.refuse(DIAGNOSTICS_PATH.relative_to(SUITE), 1, f"an entry for {test_id!r}, which is no test")
            continue
        at = entry.get("refuse-at")
        expect = entry.get("expect")
        if not isinstance(at, list) or not at or not isinstance(expect, str) or not expect:
            reader.refuse(DIAGNOSTICS_PATH.relative_to(SUITE), 1, f"{test_id}: an entry is {{\"refuse-at\": "
                                                                  f"[<editions>], \"expect\": \"<code or text>\"}}")
            continue
        try:
            spelled = inputs.schema.spell_editions(at).split(inputs.schema.editions_sep)
        except ValueError as e:
            reader.refuse(DIAGNOSTICS_PATH.relative_to(SUITE), 1, f"{test_id}: refuse-at: {e}")
            continue
        if overlap := sorted(set(spelled) & set(case.accept), key=inputs.editions.index):
            reader.refuse(DIAGNOSTICS_PATH.relative_to(SUITE), 1, f"{test_id}: WiseOwl refuses it at "
                                                                  f"{', '.join(overlap)}, where the test must be "
                                                                  f"accepted")
    for case in reader.cases:
        if case.flag is not None and case.test_id not in inputs.diagnostics:
            reader.refuse(case.path, 1, "a flagged test needs an entry in wiseowl/diagnostics.json: the editions at "
                                        "which WiseOwl COBOL refuses it and what it reports (§4, §9)")


def _check_flag(reader: Reader, case: Case, known: list[str], windows: dict[str, list[str]],
                by_id: dict[str, Case]) -> None:
    inputs = reader.inputs
    flag = case.flag
    assert flag is not None
    rel = case.path
    # §3.2: only syntax rules and general formats may carry flag: syntax; a general rule needs its justification.
    if flag.kind == "syntax":
        for r in known:
            kind = inputs.rows[r].get("kind")
            if kind == "GR" and not flag.why:
                reader.refuse(rel, 1, f"flag syntax witnessing the general rule {r}: state why its violation is "
                                      f"syntactically detectable (`why`, §3.2)")
            elif kind not in SYNTAX_RULE_KINDS + ("GR",):
                reader.refuse(rel, 1, f"flag syntax witnessing {r}: only syntax rules and general formats are "
                                      f"syntax violations (§3.2)")
    # Each edition it is flagged at: inside every rule's band, or outside a construct's window (the construct does
    # not exist there, so using it is the violation: f(case, V) = REJECT).
    for e in flag.at:
        outside = [c for c, w in windows.items() if e not in w]
        if outside:
            continue
        for r in known:
            if e not in inputs.band(r):
                reader.refuse(rel, 1, f"flagged at {e}, where {r} does not exist (its band is "
                                      f"{inputs.rows[r]['editions']}) and none of its constructs is absent")
    # Every flagged test has a control, accepted at every edition it is flagged at (§3.2, §9).
    if case.control is None:
        reader.refuse(rel, 1, "a flagged test names its control, the same program with the violation removed (§3.2)")
        return
    control = by_id.get(case.control)
    if control is None:
        reader.refuse(rel, 1, f"control {case.control!r} is no test")
        return
    if control.flag is not None and set(control.flag.at) & set(flag.at):
        reader.refuse(rel, 1, f"control {case.control} is itself flagged")
    missing = [e for e in flag.at if e not in control.accept]
    if missing:
        reader.refuse(rel, 1, f"control {case.control} is not accepted at {', '.join(missing)}, where this test is "
                              f"flagged (§9)")
    if case.in_suite and not control.in_suite:
        reader.refuse(rel, 1, f"control {case.control} is not a cleared suite test, so the suites would carry this "
                              f"flagged test without its control")


def _check_outputs(reader: Reader, case: Case) -> None:
    """One expected output for each accepted edition and each value of its A.1 items; no output no edition uses."""
    inputs = reader.inputs
    rel = case.path
    values = []
    for item in case.implementor_defined:
        if item not in inputs.implementor_defined:
            reader.refuse(rel, 1, f"implementor-defined names A.1 item {item}, whose values implementor-defined.json "
                                  f"does not enumerate: a test of an item whose values the standard does not "
                                  f"enumerate is WiseOwl-only (§3.3)")
            return
        values.append([(item, v) for v in inputs.implementor_defined[item]])
    used: set[Path] = set()
    for e in case.accept:
        for combo in itertools.product(*values):
            key = tuple(sorted(combo))
            path = case.outputs.get((e, key)) or case.outputs.get((None, key))
            if path is None:
                named = "".join(f".{i}={v}" for i, v in key)
                reader.refuse(rel, 1, f"accepted at {e} with no expected output {case.test_id}{named}.out or "
                                      f"{case.test_id}.{e}{named}.out")
            else:
                used.add(path)
    for path in sorted(set(case.outputs.values()) - used):
        reader.refuse(path, 1, "an expected output no accepted edition (or A.1 value) of its test uses")


# ── the manifests and the report ─────────────────────────────────────────────────────────────────────────────────────

def _posix(p: Path) -> str:
    return p.as_posix()


def entry_for(case: Case, edition: str, inputs: Inputs) -> dict[str, Any]:
    """A test's manifest entry at one edition: what a runner needs, and nothing it would have to re-derive."""
    reserved = inputs.reserved.get(edition, frozenset())
    entry: dict[str, Any] = {
        "id": case.test_id, "clause": case.folder, "source": _posix(case.path), "source-format": case.source_format,
        "rules": list(case.rules), "files": [_posix(case.path.parent / f) for f in case.files],
        "requires": list(case.requires),
        "words": sorted((case.words - reserved - inputs.keywords) | (case.defined - reserved)),
    }
    if edition in case.accept:
        expected = {}
        values = [[(i, v) for v in inputs.implementor_defined[i]] for i in case.implementor_defined]
        for combo in itertools.product(*values):
            key = tuple(sorted(combo))
            path = case.outputs.get((edition, key)) or case.outputs[(None, key)]
            expected[".".join(f"{i}={v}" for i, v in key)] = _posix(path)
        entry.update({"expect": "accept", "implementor-defined": list(case.implementor_defined),
                      "expected-output": expected})
    else:
        assert case.flag is not None and case.marked_line is not None
        entry.update({"expect": "flag", "flag": case.flag.kind, "line": case.marked_line, "control": case.control})
    return entry


def manifests(cases: list[Case], inputs: Inputs) -> dict[str, dict[str, Any]]:
    """`{manifest name -> content}`: one per edition, and the wiseowl manifest (§4)."""
    out: dict[str, dict[str, Any]] = {}
    ordered = sorted(cases, key=lambda c: c.test_id)
    for e in inputs.editions:
        tests = [entry_for(c, e, inputs) for c in ordered
                 if c.in_suite and (e in c.accept or (c.flag is not None and e in c.flag.at))]
        out[e] = {"edition": e, "generator": "conformance-suite/harness/generate.py", "tests": tests}
    wiseowl: dict[str, Any] = {"generator": "conformance-suite/harness/generate.py", "editions": {}, "diagnostics": {}}
    for e in inputs.editions:
        wiseowl["editions"][e] = [
            {**entry_for(c, e, inputs), "tree": c.tree, "portable": c.portable, "in-suite": c.in_suite}
            for c in ordered if e in c.accept or (c.flag is not None and e in c.flag.at)]
    by_id = {c.test_id: c for c in cases}
    for test_id in sorted(inputs.diagnostics):
        entry = inputs.diagnostics[test_id]
        case = by_id[test_id]
        # What a runner needs to compile the refused program as the suite compiles it: its source, its reference
        # format and its declared files (a copybook it COPYs), never a re-read of its header.
        wiseowl["diagnostics"][test_id] = {"source": _posix(case.path), "source-format": case.source_format,
                                           "files": [_posix(case.path.parent / f) for f in case.files],
                                           "refuse-at": list(entry["refuse-at"]), "expect": entry["expect"]}
    out["wiseowl"] = wiseowl
    return out


#: The witness kinds of the completeness report, in the order a row is counted under the first that holds (§4).
WITNESS_KINDS = ("documented-non-support", "suite", "determination", "derivation", "wiseowl-only", "none")
NON_SUPPORT = "DOCUMENTED-NON-SUPPORT"


def completeness(cases: list[Case], inputs: Inputs) -> dict[str, dict[str, int]]:
    """Per edition, the inventory rows of that edition counted by witness kind, and the suite's own measure."""
    suite_rules = {e: set() for e in inputs.editions}
    wiseowl_rules = {e: set() for e in inputs.editions}
    for c in cases:
        for e in set(c.accept) | set(c.flag.at if c.flag else ()):
            (suite_rules if c.in_suite else wiseowl_rules)[e].update(c.rules)
    report: dict[str, dict[str, int]] = {}
    schema = inputs.schema
    for e in inputs.editions:
        counts = dict.fromkeys(WITNESS_KINDS, 0)
        for rule_id, row in inputs.rows.items():
            if e not in inputs.band(rule_id):
                continue
            if row.get("verdict") == NON_SUPPORT:
                kind = "documented-non-support"
            elif rule_id in suite_rules[e]:
                kind = "suite"
            elif inputs.determined(row):
                kind = "determination"
            elif inputs.derived(row):
                kind = "derivation"
            elif rule_id in wiseowl_rules[e] or any(
                    schema.is_spec_derived(r) for r in schema.split(row.get("test-ref", ""), schema.test_ref_sep)):
                kind = "wiseowl-only"
            else:
                kind = "none"
            counts[kind] += 1
        counts["rows"] = sum(counts[k] for k in WITNESS_KINDS)
        counts["observable"] = counts["rows"] - counts["documented-non-support"] - counts["derivation"]
        report[e] = counts
    return report


def print_report(report: dict[str, dict[str, int]]) -> None:
    print("=== COMPLETENESS, by witness kind (DESIGN-conformance-suite.md §4) ===")
    head = ["edition", "rows", *WITNESS_KINDS]
    widths = [max(len(h), 5) for h in head]
    print("  " + "  ".join(f"{h:>{w}}" for h, w in zip(head, widths)))
    for e, c in report.items():
        cells = [e, str(c["rows"]), *(str(c[k]) for k in WITNESS_KINDS)]
        print("  " + "  ".join(f"{v:>{w}}" for v, w in zip(cells, widths)))
    for e, c in report.items():
        print(f"  suite measure at {e}: {c['suite']} of the {c['observable']} rows a program can observe are "
              f"witnessed by a suite test; documented non-support ({c['documented-non-support']}) is unwitnessed "
              f"until implemented")


# ── inputs from the repository ───────────────────────────────────────────────────────────────────────────────────────

def annex_names(spec_text: str) -> frozenset[str]:
    """`A.4.<n>` for each optional module heading of Annex A.4 (A.4.1 is its General) and `A.3-<n>` for each numbered
    processor-dependent element of A.3, read from the standard's transcription."""
    names = {f"A.4.{n}" for n in re.findall(r"(?m)^#### A\.4\.([0-9]+) ", spec_text) if n != "1"}
    a3 = re.search(r"(?ms)^### A\.3 .*?(?=^### A\.4 )", spec_text)
    if a3:
        names |= {f"A.3-{n}" for n in re.findall(r"(?m)^([0-9]+)\\\) ", a3.group(0))}
    return frozenset(names)


def load_implementor_defined(path: Path, rows: dict[str, dict[str, Any]]) -> tuple[dict[int, tuple[str, ...]], list[str]]:
    """implementor-defined.json: the Annex A.1 items whose values the standard enumerates (§3.3), and its problems."""
    problems: list[str] = []
    raw = json.loads(path.read_text(encoding="utf-8")) if path.is_file() else {"items": {}}
    items: dict[int, tuple[str, ...]] = {}
    for key, entry in raw.get("items", {}).items():
        values = entry.get("values")
        if not key.isdigit():
            problems.append(f"implementor-defined.json: item {key!r} is an Annex A.1 item number")
            continue
        if not isinstance(values, list) or len(values) < 2 or len(set(values)) != len(values) or not all(
                isinstance(v, str) and re.match(r"^[a-z0-9]+(?:-[a-z0-9]+)*$", v) for v in values):
            problems.append(f"implementor-defined.json: item {key} enumerates two or more distinct lower-case values")
            continue
        row = entry.get("row")
        if row != f"DOC-A.1-{key}" or row not in rows:
            problems.append(f"implementor-defined.json: item {key} names its inventory row DOC-A.1-{key}")
            continue
        if not entry.get("citation"):
            problems.append(f"implementor-defined.json: item {key} cites the rule that enumerates its values")
            continue
        items[int(key)] = tuple(values)
    return items, problems


def reserved_words() -> dict[str, frozenset[str]]:
    """edition -> the words reserved-words.json records the standard reserving at that edition (its `r<edition>`)."""
    words = json.loads(RESERVED_WORDS_PATH.read_text(encoding="utf-8"))["words"]
    editions = inventory_schema.load_schema().editions
    return {e: frozenset(w["word"] for w in words if w.get(f"r{e}")) for e in editions}


#: §8.10 is a table whose first column is the word; §8.11 lists one name a line; §8.12 is a table of words.
_KEYWORD_SECTIONS = (("8.10", "first cell"), ("8.11", "line"), ("8.12", "every cell"))
_KEYWORD = re.compile(r"^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*$")


def standard_keywords(spec_text: str) -> frozenset[str]:
    """The standard's context-sensitive words (ISO 8.10), intrinsic function names (8.11) and compiler-directive
    words (8.12), read from its transcription."""
    words: set[str] = set()
    for number, shape in _KEYWORD_SECTIONS:
        section = re.search(rf"(?ms)^### {re.escape(number)} .*?(?=^### )", spec_text)
        if section is None:
            raise ValueError(f"specs/ISO_COBOL.md has no section {number}")
        for line in section.group(0).splitlines()[1:]:
            if shape == "line":
                cells = [line.strip()]
            else:
                cells = [c.strip().strip("*") for c in line.strip().strip("|").split("|")] if line.startswith("|") else []
                cells = cells[:1] if shape == "first cell" else cells
            words.update(c for c in cells if _KEYWORD.match(c))
    return frozenset(words)


def suite_ids(cases: list[Case], editions: list[str]) -> dict[str, set[str]]:
    """edition -> the ids of the tests its suite holds (what a baseline may name)."""
    return {e: {c.test_id for c in cases if c.in_suite and (e in c.accept or (c.flag is not None and e in c.flag.at))}
            for e in editions}


def repository_inputs() -> tuple[Inputs, list[str]]:
    schema = inventory_schema.load_schema()
    rows = {r["rule-id"]: r for r in inventory_schema.load_inventory()}
    constructs = {c["id"]: (int(c["introducedIn"]), int(c["removedIn"]) if c.get("removedIn") else None)
                  for c in json.loads(CONSTRUCTS_PATH.read_text(encoding="utf-8"))["constructs"]}
    a1, problems = load_implementor_defined(IMPLEMENTOR_DEFINED_PATH, rows)
    diagnostics = json.loads(DIAGNOSTICS_PATH.read_text(encoding="utf-8")) if DIAGNOSTICS_PATH.is_file() else {}
    register = schema.register_items()
    spec = SPEC_PATH.read_text(encoding="utf-8")
    inputs = Inputs(
        schema=schema, rows=rows, constructs=constructs, requirable=annex_names(spec),
        implementor_defined=a1, diagnostics={k: v for k, v in diagnostics.items() if not k.startswith("$")},
        determined=lambda row: schema.anchor_for(row) is not None and row.get("rule-id") in register,
        derived=schema.derivation_stands, reserved=reserved_words(), keywords=standard_keywords(spec))
    return inputs, problems


# ── running ──────────────────────────────────────────────────────────────────────────────────────────────────────────

def generate(root: Path, inputs: Inputs, problems: list[str], out: Path | None,
             echo: bool = True) -> tuple[int, Reader]:
    """Read and refuse; when nothing is refused and `out` is given, write the manifests and the report there."""
    reader = Reader(root, inputs)
    reader.read_tree()
    check_cases(reader)
    for p in problems:
        reader.refuse(IMPLEMENTOR_DEFINED_PATH.name, 1, p)
    # The harness's configuration (§5): every adapter, profile and baseline, against what only the repository knows.
    for rel, message in config.check_tree(root, inputs.editions, inputs.requirable, inputs.implementor_defined,
                                          suite_ids(reader.cases, inputs.editions)):
        reader.refuse(rel, 1, message)
    if reader.refusals:
        if echo:
            for r in sorted(reader.refusals, key=lambda r: (r.path, r.line, r.message)):
                print(f"  REFUSED {r}")
        print(f"{VERDICT}RED — {len(reader.refusals)} refusal(s) over {len(reader.cases)} test(s) ===")
        return 1, reader
    if out is not None:
        written = manifests(reader.cases, inputs)
        unusable = generated_output.prepare(out, (set(written) | {COMPLETENESS_NAME}).__contains__)
        if unusable is not None:
            print(f"{VERDICT}RED — cannot write to {_posix(out)}: {unusable}; name a new directory, or one holding "
                  f"only a previous run's output (nothing was deleted) ===")
            return 2, reader
        for name, content in written.items():
            (out / name).mkdir(parents=True, exist_ok=True)
            (out / name / "manifest.json").write_text(json.dumps(content, indent=2) + "\n", encoding="utf-8",
                                                       newline="\n")
        report = completeness(reader.cases, inputs)
        (out / COMPLETENESS_NAME).write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8", newline="\n")
        print_report(report)
    suites = sum(1 for c in reader.cases if c.in_suite)
    print(f"{VERDICT}GREEN — {len(reader.cases)} test(s), {suites} in the suites, none refused"
          f"{'' if out is not None else ' (--check: nothing written)'} ===")
    return 0, reader


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--check", action="store_true", help="the refusals only, writing nothing (the gate audit)")
    ap.add_argument("--self-test", action="store_true", help="prove every refusal and the manifests on planted trees")
    ap.add_argument("--out", type=Path, metavar="DIR", help="write the manifests and the report to DIR (default: suites/)")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    if args.check and args.out is not None:
        ap.error("--check writes nothing, so it takes no --out")
    inputs, problems = repository_inputs()
    rc, _ = generate(SUITE, inputs, problems, None if args.check else (args.out or OUT))
    return rc


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────────

def _planted_inputs(diagnostics: dict[str, dict[str, Any]] | None = None) -> Inputs:
    schema = inventory_schema.load_schema()

    def row(rule_id: str, section: str, kind: str, editions: str, verdict: str = "CONFORMS", test_ref: str = "",
            derivation: str = "") -> dict[str, Any]:
        return {"rule-id": rule_id, "section": section, "kind": kind, "editions": editions, "verdict": verdict,
                "test-ref": test_ref, "derivation": derivation, "code-location": "src/x.cs", "notes": "n"}

    rows = {r["rule-id"]: r for r in (
        row("GR-1.1.1-1", "1.1.1", "GR", "85,2002,2014,2023"),
        row("SR-1.1.1-1", "1.1.1", "SR", "85,2002,2014,2023"),
        row("SR-1.1.1-2", "1.1.1", "SR", "2002,2014,2023"),
        row("AR-1.1.1-1", "1.1.1", "AR", "85,2002,2014,2023"),
        row("SR-2.2.2-1", "2.2.2", "SR", "85,2002,2014,2023"),
        row("GR-1.1.1-2", "1.1.1", "GR", "2002,2014,2023"),
        row("GR-1.1.1-3", "1.1.1", "GR", "85,2002,2014,2023", verdict=NON_SUPPORT),
        row("GR-1.1.1-4", "1.1.1", "GR", "85,2002,2014,2023", test_ref="unit:Some.Test_PinnedToSpec"),
        row("GR-1.1.1-5", "1.1.1", "GR", "85,2002,2014,2023", derivation="d"),
        row("DOC-A.1-9", "1.1.1", "DOC", "85,2002,2014,2023"),
        row("DOC-A.1-10", "1.1.1", "DOC", "85,2002,2014,2023"),
        row("RV-2.2.2-1", "2.2.2", "RV", "85,2002,2014,2023"),
    )}
    return Inputs(
        schema=schema, rows=rows, constructs={"c-2002": (2002, None), "c-gone-2002": (85, 2002)},
        requirable=frozenset({"A.4.11", "A.3-2"}), implementor_defined={9: ("one", "two")},
        diagnostics=diagnostics if diagnostics is not None else {
            "flagged": {"refuse-at": ["85", "2002", "2014", "2023"], "expect": "X1"},
            "free-flag": {"refuse-at": ["2002"], "expect": "X2"}},
        determined=lambda r: r["rule-id"] in ("DOC-A.1-9", "DOC-A.1-10"), derived=lambda r: bool(r.get("derivation")),
        reserved={"85": frozenset({"IDENTIFICATION", "DIVISION", "PROGRAM-ID", "DATA", "WORKING-STORAGE", "SECTION",
                                   "PIC", "VALUE", "PROCEDURE", "DISPLAY", "STOP", "RUN"})},
        keywords=frozenset({"STOP", "W-E"}))


def _fixed(header: list[str], body: list[str]) -> str:
    lines = ["      *>--- conformance-case", *("      *> " + h for h in header), "      *>---"]
    return "\n".join(lines + body) + "\n"


_PROGRAM = ["       IDENTIFICATION DIVISION.", "       PROGRAM-ID. P.", "       DATA DIVISION.",
            "       WORKING-STORAGE SECTION.", "       01 W-A PIC X(3) VALUE \"ABC\".", "       01 W-N PIC 9(3).",
            "       01 W-E PIC ZZ9.", "       PROCEDURE DIVISION.", "       MAIN-PARA.",
            "           DISPLAY \"OK \" W-A W-E", "           STOP RUN."]


def _plant(root: Path, files: dict[str, str]) -> None:
    for rel, text in files.items():
        p = root / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text, encoding="utf-8", newline="\n")


def self_test() -> int:
    rc = 0

    def check(name: str, ok: bool, got: object = None) -> None:
        nonlocal rc
        print(("  ok: " if ok else "  SELF-TEST FAILED: ") + name + ("" if ok else f" — got {got!r}"))
        rc |= 0 if ok else 1

    print("=== generate.py --self-test ===")
    good = {
        "cases/1.1.1-Rule/accepted.cob": _fixed(["id: accepted", "rules: [GR-1.1.1-1]", "accept: [85, 2002, 2014, 2023]",
                                                 "source-format: fixed"], _PROGRAM),
        "cases/1.1.1-Rule/accepted.out": "OK ABC  0\n",
        "cases/1.1.1-Rule/per-edition.cob": _fixed(["id: per-edition", "rules: [GR-1.1.1-2]", "accept: [2002, 2014, 2023]",
                                                    "constructs: [c-2002]", "source-format: fixed"], _PROGRAM),
        "cases/1.1.1-Rule/per-edition.out": "a\n",
        "cases/1.1.1-Rule/per-edition.2023.out": "b\n",
        "cases/1.1.1-Rule/valued.cob": _fixed(["id: valued", "rules: [DOC-A.1-9]", "accept: [85, 2002, 2014, 2023]",
                                               "implementor-defined: [9]", "source-format: fixed"], _PROGRAM),
        "cases/1.1.1-Rule/valued.9=one.out": "1\n",
        "cases/1.1.1-Rule/valued.9=two.out": "2\n",
        "cases/1.1.1-Rule/flagged.cob": _fixed(
            ["id: flagged", "rules: [SR-1.1.1-1]", "flag: {kind: syntax, at: [85, 2002, 2014, 2023]}",
             "control: accepted", "source-format: fixed"], _PROGRAM[:9] + ["<<         MOVE 1 TO W-A"] + _PROGRAM[9:]),
        "cases/1.1.1-Rule/copying.cob": _fixed(["id: copying", "rules: [GR-1.1.1-1]", "accept: [85]",
                                                "files: [CPY1.cpy]", "source-format: fixed"],
                                               _PROGRAM[:4] + ["       COPY CPY1."] + _PROGRAM[4:]),
        "cases/1.1.1-Rule/CPY1.cpy": "      * a comment line of a library text runs on past column 72 to 80 COMMENT1\n"
                                     "       01 W-C PIC X.\n",
        "cases/1.1.1-Rule/copying.out": "OK ABC  0\n",
        "cases/1.1.1-Rule/unverified.cob": _fixed(["id: unverified", "rules: [GR-1.1.1-1]", "accept: [85]",
                                                   "portable: unverified", "source-format: fixed"],
                                                  _PROGRAM[:9] + ["           DISPLAY W-N"] + _PROGRAM[9:]),
        "cases/1.1.1-Rule/unverified.out": "000\n",
        "cases/_unassigned/loose.cob": _fixed(["id: loose", "rules: []", "accept: [2023]", "source-format: fixed"],
                                              _PROGRAM),
        "cases/_unassigned/loose.out": "OK ABC  0\n",
        "wiseowl/cases/1.1.1-Rule/own.cob": _fixed(["id: own", "rules: [GR-1.1.1-1]", "accept: [2023]",
                                                    "source-format: fixed"],
                                                   _PROGRAM[:9] + ["           DISPLAY W-N"] + _PROGRAM[9:]),
        "wiseowl/cases/1.1.1-Rule/own.out": "000\n",
        "cases/2.2.2-Free/free-test.cob": "*>--- conformance-case\n*> id: free-test\n*> rules: [RV-2.2.2-1]\n"
                                          "*> accept: [2002]\n*> source-format: free\n*>---\n"
                                          + "\n".join(line.strip() for line in _PROGRAM) + "\n",
        "cases/2.2.2-Free/free-test.out": "OK ABC  0\n",
        "cases/2.2.2-Free/free-flag.cob": "*>--- conformance-case\n*> id: free-flag\n*> rules: [SR-2.2.2-1]\n"
                                          "*> flag: {kind: syntax, at: [2002]}\n*> control: free-test\n"
                                          "*> source-format: free\n*>---\nIDENTIFICATION DIVISION.\nPROGRAM-ID. F.\n"
                                          "PROCEDURE DIVISION.\nMOVE 1 TO X. *> <<\nSTOP RUN.\n",
    }
    with tempfile.TemporaryDirectory() as d:
        root = Path(d)
        _plant(root, good)
        inputs = _planted_inputs()
        rc_gen, reader = generate(root, inputs, [], root / "suites")
        check("arm: a well-formed tree is GREEN", rc_gen == 0, [str(r) for r in reader.refusals])
        if rc_gen == 0:
            m85 = json.loads((root / "suites/85/manifest.json").read_text(encoding="utf-8"))
            m2023 = json.loads((root / "suites/2023/manifest.json").read_text(encoding="utf-8"))
            mw = json.loads((root / "suites/wiseowl/manifest.json").read_text(encoding="utf-8"))
            ids85 = [t["id"] for t in m85["tests"]]
            check("arm: the 85 suite holds its accepted, flagged, valued and copying tests, and not a 2002 one",
                  ids85 == ["accepted", "copying", "flagged", "valued"], ids85)
            pe = {t["id"]: t for t in m2023["tests"]}
            check("arm: a per-edition output is the edition's own file at that edition",
                  pe["per-edition"]["expected-output"] == {"": "cases/1.1.1-Rule/per-edition.2023.out"}, pe)
            m2014 = {t["id"]: t for t in json.loads((root / "suites/2014/manifest.json").read_text(
                encoding="utf-8"))["tests"]}
            check("arm: ... and the shared .out at an edition without one",
                  m2014["per-edition"]["expected-output"] == {"": "cases/1.1.1-Rule/per-edition.out"}, m2014)
            check("arm: an A.1-valued test carries one expected output per enumerated value",
                  pe["valued"]["expected-output"] == {"9=one": "cases/1.1.1-Rule/valued.9=one.out",
                                                      "9=two": "cases/1.1.1-Rule/valued.9=two.out"}, pe["valued"])
            check("arm: a flagged test carries its kind, its marked line and its control",
                  {k: pe["flagged"][k] for k in ("expect", "flag", "line", "control")}
                  == {"expect": "flag", "flag": "syntax", "line": 17, "control": "accepted"}, pe["flagged"])
            m2002 = {t["id"]: t for t in json.loads((root / "suites/2002/manifest.json").read_text(
                encoding="utf-8"))["tests"]}
            check("arm: a free-form test marks its line with a closing `*> <<`",
                  m2002["free-flag"]["line"] == 11 and m2002["free-flag"]["control"] == "free-test", m2002)
            check("arm: a test's declared files reach its entry",
                  m85["tests"][1]["files"] == ["cases/1.1.1-Rule/CPY1.cpy"], m85["tests"][1])
            check("arm: an entry names its clause folder, and its words without the edition's reserved ones (a "
                  "copied library text's words included)",
                  m85["tests"][1]["clause"] == "1.1.1-Rule"
                  and m85["tests"][1]["words"] == ["MAIN-PARA", "P", "W-A", "W-C", "W-E", "W-N"], m85["tests"][1])
            check("arm: ... a word that is neither reserved nor a keyword stays (2023 here reserves none); a keyword "
                  "the program does not define goes (STOP); a keyword it defines as a name stays (W-E)",
                  "DISPLAY" in pe["accepted"]["words"] and "STOP" not in pe["accepted"]["words"]
                  and "W-E" in pe["accepted"]["words"], pe["accepted"]["words"])
            check("arm: an unverified test, an _unassigned test and a wiseowl test are in no suite",
                  not {"unverified", "loose", "own"} & {t["id"] for m in (m85, m2023) for t in m["tests"]})
            w2023 = {t["id"]: t for t in mw["editions"]["2023"]}
            check("arm: ... and all three are in the wiseowl manifest",
                  {"loose", "own"} <= set(w2023) and "unverified" in {t["id"] for t in mw["editions"]["85"]}, w2023)
            check("arm: the wiseowl manifest carries diagnostics.json's refusals",
                  mw["diagnostics"]["flagged"]["expect"] == "X1", mw["diagnostics"])
            check("arm: ... each with what compiles the refused program: its source, reference format and files",
                  {k: mw["diagnostics"]["free-flag"][k] for k in ("source", "source-format", "files", "refuse-at")}
                  == {"source": "cases/2.2.2-Free/free-flag.cob", "source-format": "free", "files": [],
                      "refuse-at": ["2002"]}, mw["diagnostics"]["free-flag"])
            report = json.loads((root / "suites/completeness.json").read_text(encoding="utf-8"))
            r85 = report["85"]
            check("arm: completeness counts each row once under its first witness kind",
                  {k: r85[k] for k in WITNESS_KINDS} == {"documented-non-support": 1, "suite": 3, "determination": 1,
                                                         "derivation": 1, "wiseowl-only": 1, "none": 3}
                  and r85["rows"] == 10 and r85["observable"] == 8, r85)
            check("arm: a row not in the edition's band is not counted there (GR-1.1.1-2, SR-1.1.1-2 are 2002 on)",
                  report["2002"]["rows"] == 12, report["2002"])
        shutil.rmtree(root / "suites", ignore_errors=True)
        rc_check, _ = generate(root, inputs, [], None)
        check("arm: --check writes nothing", rc_check == 0 and not (root / "suites").exists())
        # --out DIR names a directory the user chose: only what a previous run wrote there is ever removed (R1-1).
        mine = root / "mine"
        (mine / "precious").mkdir(parents=True)
        (mine / "todo.txt").write_text("keep\n", encoding="utf-8", newline="\n")
        (mine / "precious" / "notes.txt").write_text("keep\n", encoding="utf-8", newline="\n")
        rc_mine, _ = generate(root, inputs, [], mine, echo=False)
        check("arm: an output directory holding anything the generator does not write is refused, and nothing in it "
              "is deleted or written", rc_mine == 2 and sorted(p.name for p in mine.iterdir()) == ["precious", "todo.txt"]
              and (mine / "todo.txt").is_file() and (mine / "precious" / "notes.txt").is_file(),
              sorted(p.name for p in mine.iterdir()))
        rc_file, _ = generate(root, inputs, [], mine / "todo.txt", echo=False)
        check("arm: an output path that is a file is refused, and the file is kept",
              rc_file == 2 and (mine / "todo.txt").read_text(encoding="utf-8") == "keep\n")
        rc_first, _ = generate(root, inputs, [], root / "suites", echo=False)
        (root / "suites" / "85" / "stale.json").write_text("{}", encoding="utf-8", newline="\n")
        (root / "suites" / "completeness.json").write_text("{}", encoding="utf-8", newline="\n")
        rc_again, _ = generate(root, inputs, [], root / "suites", echo=False)
        check("arm: a previous run's output is replaced whole: its manifest directories and report are rewritten",
              rc_first == 0 and rc_again == 0 and not (root / "suites" / "85" / "stale.json").exists()
              and json.loads((root / "suites" / "completeness.json").read_text(encoding="utf-8")) != {}
              and (root / "suites" / "wiseowl" / "manifest.json").is_file())
        shutil.rmtree(root / "suites")
        (root / "suites").mkdir()
        rc_empty_dir, _ = generate(root, inputs, [], root / "suites", echo=False)
        check("arm: an empty output directory is written to", rc_empty_dir == 0
              and (root / "suites" / "85" / "manifest.json").is_file())

    bad = {
        "no-header.cob": ("cases/1.1.1-Rule/no-header.cob", "      * nothing\n", "does not open with"),
        "id": ("cases/1.1.1-Rule/wrong-name.cob",
               _fixed(["id: other-name", "rules: [GR-1.1.1-1]", "accept: [85]", "source-format: fixed"], _PROGRAM),
               "is not the file's base name"),
        "rule": ("cases/1.1.1-Rule/unknown-rule.cob",
                 _fixed(["id: unknown-rule", "rules: [GR-9.9.9-9]", "accept: [85]", "source-format: fixed"], _PROGRAM),
                 "the traceability inventory does not have"),
        "format": ("cases/1.1.1-Rule/no-format.cob", _fixed(["id: no-format", "rules: [GR-1.1.1-1]", "accept: [85]"],
                                                           _PROGRAM), "no source-format"),
        "band": ("cases/1.1.1-Rule/band.cob",
                 _fixed(["id: band", "rules: [GR-1.1.1-2]", "accept: [85, 2002]", "source-format: fixed"], _PROGRAM),
                 "accepted at 85, where GR-1.1.1-2 does not exist"),
        "construct": ("cases/1.1.1-Rule/construct.cob",
                      _fixed(["id: construct", "rules: [GR-1.1.1-1]", "accept: [85, 2002]", "constructs: [c-2002]",
                              "source-format: fixed"], _PROGRAM), "the construct c-2002 does not exist"),
        "removed": ("cases/1.1.1-Rule/removed.cob",
                    _fixed(["id: removed", "rules: [GR-1.1.1-1]", "accept: [85, 2002]", "constructs: [c-gone-2002]",
                            "source-format: fixed"], _PROGRAM), "accepted at 2002, where the construct c-gone-2002"),
        "unknown construct": ("cases/1.1.1-Rule/what.cob",
                              _fixed(["id: what", "rules: [GR-1.1.1-1]", "accept: [85]", "constructs: [nope]",
                                      "source-format: fixed"], _PROGRAM), "constructs.json does not have"),
        "no control": ("cases/1.1.1-Rule/no-control.cob",
                       _fixed(["id: no-control", "rules: [SR-1.1.1-1]", "flag: {kind: syntax, at: [85]}",
                               "source-format: fixed"], _PROGRAM[:9] + ["<<         MOVE 1 TO W-A"] + _PROGRAM[9:]),
                       "names its control"),
        "control not accepted": ("cases/1.1.1-Rule/late-control.cob",
                                 _fixed(["id: late-control", "rules: [SR-1.1.1-1]", "flag: {kind: syntax, at: [85]}",
                                         "control: per-edition-ok", "source-format: fixed"],
                                        _PROGRAM[:9] + ["<<         MOVE 1 TO W-A"] + _PROGRAM[9:]),
                                 "is not accepted at 85"),
        "gr flag": ("cases/1.1.1-Rule/gr-flag.cob",
                    _fixed(["id: gr-flag", "rules: [GR-1.1.1-1]", "flag: {kind: syntax, at: [85]}",
                            "control: ok", "source-format: fixed"],
                           _PROGRAM[:9] + ["<<         MOVE 1 TO W-A"] + _PROGRAM[9:]),
                    "state why its violation is syntactically detectable"),
        "unmarked": ("cases/1.1.1-Rule/unmarked.cob",
                     _fixed(["id: unmarked", "rules: [SR-1.1.1-1]", "flag: {kind: syntax, at: [85]}", "control: ok",
                             "source-format: fixed"], _PROGRAM), "marks exactly one line"),
        "stray mark": ("cases/1.1.1-Rule/stray.cob",
                       _fixed(["id: stray", "rules: [GR-1.1.1-1]", "accept: [85]", "source-format: fixed"],
                              _PROGRAM[:9] + ["<<         MOVE 1 TO W-A"] + _PROGRAM[9:]), "not flagged"),
        "run-time at 85": ("cases/1.1.1-Rule/runtime.cob",
                           _fixed(["id: runtime", "rules: [GR-1.1.1-1]", "accept: [85, 2002]", "source-format: fixed"],
                                  ["       >>TURN EC-ALL CHECKING ON"] + _PROGRAM), "run-time expectation"),
        "undeclared copy": ("cases/1.1.1-Rule/copy-undeclared.cob",
                            _fixed(["id: copy-undeclared", "rules: [GR-1.1.1-1]", "accept: [85]",
                                    "source-format: fixed"], _PROGRAM[:4] + ["       COPY NOPE."] + _PROGRAM[4:]),
                            "COPY NOPE: the library text is not a declared file"),
        "lint numeric": ("cases/1.1.1-Rule/lint-numeric.cob",
                         _fixed(["id: lint-numeric", "rules: [GR-1.1.1-1]", "accept: [85]", "source-format: fixed"],
                                _PROGRAM[:9] + ["           DISPLAY W-N"] + _PROGRAM[9:]),
                         "DISPLAY W-N: a data item of class numeric"),
        "lint function": ("cases/1.1.1-Rule/lint-function.cob",
                          _fixed(["id: lint-function", "rules: [GR-1.1.1-1]", "accept: [85]", "source-format: fixed"],
                                 _PROGRAM[:9] + ["           DISPLAY FUNCTION LENGTH(W-A)"] + _PROGRAM[9:]),
                          "a function result"),
        "lint national": ("cases/1.1.1-Rule/lint-national.cob",
                          _fixed(["id: lint-national", "rules: [GR-1.1.1-1]", "accept: [2002]", "source-format: fixed"],
                                 _PROGRAM[:9] + ["           DISPLAY N\"AB\""] + _PROGRAM[9:]),
                          "a national-literal"),
        "lint numeric literal": ("cases/1.1.1-Rule/lint-numlit.cob",
                                 _fixed(["id: lint-numlit", "rules: [GR-1.1.1-1]", "accept: [85]",
                                         "source-format: fixed"],
                                        _PROGRAM[:9] + ["           DISPLAY 12"] + _PROGRAM[9:]), "a numeric-literal"),
        "lint upon": ("cases/1.1.1-Rule/lint-upon.cob",
                      _fixed(["id: lint-upon", "rules: [GR-1.1.1-1]", "accept: [85]", "source-format: fixed"],
                             _PROGRAM[:9] + ["           DISPLAY W-A UPON CONSOLE"] + _PROGRAM[9:]),
                      "an implementor-defined device"),
        "lint margin": ("cases/1.1.1-Rule/lint-margin.cob",
                        _fixed(["id: lint-margin", "rules: [GR-1.1.1-1]", "accept: [85]", "source-format: fixed"],
                               _PROGRAM[:-1] + ["           STOP RUN." + " " * 52 + "SEQ00001"]),
                        "program text past column 72"),
        "no output": ("cases/1.1.1-Rule/no-output.cob",
                      _fixed(["id: no-output", "rules: [GR-1.1.1-1]", "accept: [85]", "source-format: fixed"],
                             _PROGRAM), "accepted at 85 with no expected output"),
        "orphan output": ("cases/1.1.1-Rule/orphan.2014.out", "x\n", "an expected output no accepted edition"),
        "orphan file": ("cases/1.1.1-Rule/stray.txt", "x\n", "a file no test of this folder declares"),
        "a1 unknown": ("cases/1.1.1-Rule/a1-unknown.cob",
                       _fixed(["id: a1-unknown", "rules: [GR-1.1.1-1]", "accept: [85]", "implementor-defined: [56]",
                               "source-format: fixed"], _PROGRAM), "whose values implementor-defined.json does not"),
        "requires": ("cases/1.1.1-Rule/needs.cob",
                     _fixed(["id: needs", "rules: [GR-1.1.1-1]", "accept: [85]", "requires: [A.4.99]",
                             "source-format: fixed"], _PROGRAM), "not an Annex A.4 module"),
        "misfiled": ("cases/1.1.1-Rule/misfiled.cob",
                     _fixed(["id: misfiled", "rules: [RV-2.2.2-1]", "accept: [85]", "source-format: fixed"], _PROGRAM),
                     "filed under clause 1.1.1, which none of its rules belongs to"),
        "no rules outside _unassigned": ("cases/1.1.1-Rule/norules.cob",
                                         _fixed(["id: norules", "rules: []", "accept: [85]", "source-format: fixed"],
                                                _PROGRAM), "a test with no rules belongs in _unassigned/"),
        "folder": ("cases/COMPUTE/folder.cob", _fixed(["id: folder", "rules: [GR-1.1.1-1]", "accept: [85]",
                                                       "source-format: fixed"], _PROGRAM), "a clause folder is named"),
        "free at 85": ("cases/2.2.2-Free/free85.cob",
                       "*>--- conformance-case\n*> id: free85\n*> rules: [RV-2.2.2-1]\n*> accept: [85]\n"
                       "*> source-format: free\n*>---\nSTOP RUN.\n", "free form at 85"),
        "format mismatch": ("cases/1.1.1-Rule/mismatch.cob",
                            _fixed(["id: mismatch", "rules: [GR-1.1.1-1]", "accept: [85]", "source-format: free"],
                                   _PROGRAM), "the metadata block is written in fixed form"),
        "unknown field": ("cases/1.1.1-Rule/field.cob",
                          _fixed(["id: field", "rules: [GR-1.1.1-1]", "accept: [85]", "reject: [85]",
                                  "source-format: fixed"], _PROGRAM), "unknown field 'reject'"),
        "unsorted": ("cases/1.1.1-Rule/unsorted.cob",
                     _fixed(["id: unsorted", "rules: [GR-1.1.1-1]", "accept: [2002, 85]", "source-format: fixed"],
                            _PROGRAM), "lists its editions in ascending order"),
        "neither": ("cases/1.1.1-Rule/neither.cob",
                    _fixed(["id: neither", "rules: [GR-1.1.1-1]", "source-format: fixed"], _PROGRAM),
                    "neither accepted nor flagged"),
        "undiagnosed": ("cases/1.1.1-Rule/undiagnosed.cob",
                        _fixed(["id: undiagnosed", "rules: [SR-1.1.1-1]", "flag: {kind: syntax, at: [85]}",
                                "control: ok", "source-format: fixed"],
                               _PROGRAM[:9] + ["<<         MOVE 1 TO W-A"] + _PROGRAM[9:]),
                        "needs an entry in wiseowl/diagnostics.json"),
        "control without flag": ("cases/1.1.1-Rule/ctl-unflagged.cob",
                                 _fixed(["id: ctl-unflagged", "rules: [GR-1.1.1-1]", "accept: [85]", "control: ok",
                                         "source-format: fixed"], _PROGRAM), "this test is not flagged"),
        "accepted and flagged": ("cases/1.1.1-Rule/both.cob",
                                 _fixed(["id: both", "rules: [SR-1.1.1-1]", "accept: [85]",
                                         "flag: {kind: syntax, at: [85]}", "control: ok", "source-format: fixed"],
                                        _PROGRAM[:9] + ["<<         MOVE 1 TO W-A"] + _PROGRAM[9:]),
                                 "accepted and flagged at 85"),
        "flag kind": ("cases/1.1.1-Rule/loud.cob",
                      _fixed(["id: loud", "rules: [SR-1.1.1-1]", "flag: {kind: loud, at: [85]}", "control: ok",
                              "source-format: fixed"], _PROGRAM), "flag kind is one of"),
        "flag syntax on an AR row": ("cases/1.1.1-Rule/ar-flag.cob",
                                     _fixed(["id: ar-flag", "rules: [AR-1.1.1-1]", "flag: {kind: syntax, at: [85]}",
                                             "control: ok", "source-format: fixed"],
                                            _PROGRAM[:9] + ["<<         MOVE 1 TO W-A"] + _PROGRAM[9:]),
                                     "only syntax rules and general formats"),
        "flagged outside the band": ("cases/1.1.1-Rule/early-flag.cob",
                                     _fixed(["id: early-flag", "rules: [SR-1.1.1-2]", "flag: {kind: syntax, at: [85]}",
                                             "control: ok", "source-format: fixed"],
                                            _PROGRAM[:9] + ["<<         MOVE 1 TO W-A"] + _PROGRAM[9:]),
                                     "flagged at 85, where SR-1.1.1-2 does not exist"),
        "unverified control": ("cases/1.1.1-Rule/weak-ctl.cob",
                               _fixed(["id: weak-ctl", "rules: [SR-1.1.1-1]", "flag: {kind: syntax, at: [85]}",
                                       "control: rough", "source-format: fixed"],
                                      _PROGRAM[:9] + ["<<         MOVE 1 TO W-A"] + _PROGRAM[9:]),
                               "is not a cleared suite test"),
        "missing declared file": ("cases/1.1.1-Rule/gone.cob",
                                  _fixed(["id: gone", "rules: [GR-1.1.1-1]", "accept: [85]", "files: [GONE.cpy]",
                                          "source-format: fixed"], _PROGRAM), "which is not beside the test"),
        "exception observed in a copybook at 85": ("cases/1.1.1-Rule/ECCPY.cpy",
                                                   "           IF FUNCTION EXCEPTION-STATUS = SPACE CONTINUE "
                                                   "END-IF.\n", "run-time expectation"),
        "lint margin in a copybook": ("cases/1.1.1-Rule/MRGCPY.cpy",
                                      "       01 W-C PIC X." + " " * 52 + "PIC 9(3)\n",
                                      "program text past column 72"),
        "lint display in a copybook": ("cases/1.1.1-Rule/DSPCPY.cpy", "           DISPLAY W-N\n",
                                       "DISPLAY W-N: a data item of class numeric"),
        "a profile claiming no module of the standard": (
            "profiles/vendor.json", json.dumps({"compiler": "vendor", "editions": ["85"], "modules": {"A.4.99": "x"},
                                                "implementor-defined": {"9": "one"}}),
            "A.4.99 is not an Annex A.4 module of the standard"),
        "a profile with no value of an enumerated A.1 item": (
            "profiles/silent.json", json.dumps({"compiler": "silent", "editions": ["85"]}),
            "states no value of Annex A.1 item 9"),
        "a baseline of no adapter": (
            "baselines/nobody.json", json.dumps({"compiler": "nobody", "verdicts": {"85": {"ok": "PASS"}}}),
            "a baseline of 'nobody', which has no adapter"),
        "duplicate id": ("wiseowl/cases/1.1.1-Rule/ok.cob",
                         _fixed(["id: ok", "rules: [GR-1.1.1-1]", "accept: [85]", "source-format: fixed"], _PROGRAM),
                         "is also"),
    }
    base = {
        "cases/1.1.1-Rule/ok.cob": _fixed(["id: ok", "rules: [GR-1.1.1-1]", "accept: [85, 2002, 2014, 2023]",
                                           "source-format: fixed"], _PROGRAM),
        "cases/1.1.1-Rule/ok.out": "OK ABC  0\n",
        "cases/1.1.1-Rule/per-edition-ok.cob": _fixed(["id: per-edition-ok", "rules: [GR-1.1.1-2]", "accept: [2002]",
                                                       "source-format: fixed"], _PROGRAM),
        "cases/1.1.1-Rule/per-edition-ok.out": "x\n",
        "cases/1.1.1-Rule/orphan.cob": _fixed(["id: orphan", "rules: [GR-1.1.1-1]", "accept: [85]",
                                               "source-format: fixed"], _PROGRAM),
        "cases/1.1.1-Rule/orphan.out": "x\n",
        "cases/1.1.1-Rule/no-header.out": "x\n",
    }
    for name in ("wrong-name", "unknown-rule", "no-format", "band", "construct", "removed", "what", "copy-undeclared",
                 "lint-numeric", "lint-function", "lint-numlit", "lint-upon", "lint-margin", "a1-unknown", "needs",
                 "misfiled", "norules", "mismatch", "field", "unsorted", "runtime", "stray"):
        base[f"cases/1.1.1-Rule/{name}.out"] = "x\n"
    base["cases/1.1.1-Rule/band.2002.out"] = "x\n"
    base["cases/1.1.1-Rule/construct.2002.out"] = "x\n"
    base["cases/1.1.1-Rule/removed.2002.out"] = "x\n"
    base["cases/1.1.1-Rule/runtime.2002.out"] = "x\n"
    base["cases/1.1.1-Rule/unsorted.2002.out"] = "x\n"
    base["cases/1.1.1-Rule/lint-national.out"] = "x\n"
    base["cases/2.2.2-Free/free85.out"] = "x\n"
    base["cases/COMPUTE/folder.out"] = "x\n"
    base["wiseowl/cases/1.1.1-Rule/ok.out"] = "x\n"
    for name in ("ctl-unflagged", "gone", "ec-copy", "copy-margin", "copy-display"):
        base[f"cases/1.1.1-Rule/{name}.out"] = "x\n"
    base["cases/1.1.1-Rule/ec-copy.cob"] = _fixed(["id: ec-copy", "rules: [GR-1.1.1-1]", "accept: [85]",
                                                   "files: [ECCPY.cpy]", "source-format: fixed"],
                                                  _PROGRAM[:9] + ["           COPY ECCPY."] + _PROGRAM[9:])
    base["cases/1.1.1-Rule/copy-margin.cob"] = _fixed(["id: copy-margin", "rules: [GR-1.1.1-1]", "accept: [85]",
                                                       "files: [MRGCPY.cpy]", "source-format: fixed"],
                                                      _PROGRAM[:5] + ["       COPY MRGCPY."] + _PROGRAM[5:])
    base["cases/1.1.1-Rule/copy-display.cob"] = _fixed(["id: copy-display", "rules: [GR-1.1.1-1]", "accept: [85]",
                                                        "files: [DSPCPY.cpy]", "source-format: fixed"],
                                                       _PROGRAM[:9] + ["           COPY DSPCPY."] + _PROGRAM[9:])
    base["cases/1.1.1-Rule/rough.cob"] = _fixed(["id: rough", "rules: [GR-1.1.1-1]", "accept: [85]",
                                                 "portable: unverified", "source-format: fixed"], _PROGRAM)
    base["cases/1.1.1-Rule/rough.out"] = "x\n"
    diagnostics = {t: {"refuse-at": ["85"], "expect": "X"} for t in (
        "no-control", "late-control", "gr-flag", "unmarked", "loud", "ar-flag", "early-flag", "weak-ctl")}
    diagnostics["ghost"] = {"refuse-at": ["85"], "expect": "X"}
    diagnostics["ok"] = {"refuse-at": ["85"], "expect": "X"}
    with tempfile.TemporaryDirectory() as d:
        root = Path(d)
        _plant(root, base | {path: text for path, text, _ in bad.values()})
        rc_bad, reader = generate(root, _planted_inputs(diagnostics), ["implementor-defined.json: planted"], None,
                                  echo=False)
        refused = [str(r) for r in reader.refusals]
        check("arm: a tree with refused tests is RED", rc_bad == 1)
        for name, (path, _, want) in bad.items():
            # A refusal is filed against the planted file, or against the folder holding it.
            hit = [r for r in reader.refusals if (path == r.path or path.startswith(r.path + "/")) and want in r.message]
            check(f"arm: {name} is refused ({want})", bool(hit), [r for r in refused if r.startswith(path)][:6])
        check("arm: a diagnostics.json entry naming no test is refused",
              any("an entry for 'ghost', which is no test" in r for r in refused), refused[:3])
        check("arm: a diagnostics.json refusal where the test must be accepted is refused",
              any("ok: WiseOwl refuses it at 85, where the test must be accepted" in r for r in refused))
        check("arm: a problem in implementor-defined.json is refused", any("planted" in r for r in refused))
        check("arm: a refused tree writes nothing", not (root / "suites").exists())
    with tempfile.TemporaryDirectory() as d:
        rc_empty, reader = generate(Path(d), _planted_inputs({}), [], None, echo=False)
        check("arm: a tree with no cases/ is RED, never a GREEN over nothing",
              rc_empty == 1 and any("there is no cases/ tree" in r.message for r in reader.refusals))

    # The program-text reader's own arms.
    def fixed_tokens(*lines: str) -> list[cobol.Token]:
        return cobol.tokenize(cobol.read_lines("\n".join(lines), "fixed"))

    data = [
        "      * a comment line",
        "       01 A PIC S9(4)V99 USAGE COMP.",
        "       01 B PIC X(4)  VALUE 'IT''S'.",
        "       01 C PIC -ZZ9.99.",
        "       01 G. 05 G1 PIC X. 05 G2 PIC 9.",
        "       01 N1 PIC N(3).",
        "       01 K CONSTANT AS 12.",
        "       01 T OCCURS 3 INDEXED BY TX PIC X.",
        "      -    ",
    ]
    classes = cobol.data_classes(fixed_tokens(*data))
    check("arm: the reader classifies items by PICTURE, USAGE, group and constant",
          {k: classes.get(k) for k in ("A", "B", "C", "G", "G1", "G2", "N1", "K", "TX", "T")} == {
              "A": {cobol.NUMERIC_COMPUTATIONAL_CLASS}, "B": {cobol.ALPHANUMERIC_CLASS},
              "C": {cobol.NUMERIC_EDITED_CLASS}, "G": {cobol.ALPHANUMERIC_CLASS}, "G1": {cobol.ALPHANUMERIC_CLASS},
              "G2": {cobol.NUMERIC_DISPLAY_CLASS}, "N1": {cobol.NATIONAL_CLASS}, "K": {cobol.NUMERIC_DISPLAY_CLASS},
              "TX": {cobol.INDEX_CLASS}, "T": {cobol.ALPHANUMERIC_CLASS}}, classes)
    proc = fixed_tokens("       DATA DIVISION.", *data, "       PROCEDURE DIVISION.",
                        "           DISPLAY \"X\" G G2 (1:1) A (1:2) SPACE B *> a floating comment DISPLAY C",
                        "           IF B = SPACE DISPLAY C END-IF.")
    issues = [(i.operand, i.why) for i in cobol.display_issues(proc)]
    check("arm: a reference-modified numeric display item is alphanumeric; a computational one is not; a floating "
          "comment is no operand", issues == [("A", "a data item of class " + cobol.NUMERIC_COMPUTATIONAL_CLASS)],
          issues)

    def refused(data: list[str], displays: list[str], environment: tuple[str, ...] = ()) -> list[str]:
        """The operands the lint refuses in a one-unit program with `data` and one DISPLAY per `displays` item."""
        lines = (["       ENVIRONMENT DIVISION.", *environment] if environment else []) + [
            "       DATA DIVISION.", *data, "       PROCEDURE DIVISION.",
            *("           DISPLAY " + d for d in displays), "           STOP RUN."]
        return [i.operand + (" (" + i.why + ")" if "holding" in i.why or "sharing" in i.why else "")
                for i in cobol.display_issues(fixed_tokens(*lines))]

    # ISO 13.18.60.4 GR 1: a group's USAGE applies to each elementary item in it (branch review R1-1).
    got = refused(["       WORKING-STORAGE SECTION.", "       01 GC USAGE COMP.", "           05 A1 PIC 9(4) VALUE 1.",
                   "       01 GN USAGE NATIONAL.", "           05 N2 PIC 9(3).",
                   "       01 GG GROUP-USAGE NATIONAL.", "           05 M PIC N(2)."],
                  ["A1 (1:2)", "N2 (1:3)", "GG", "M (1:1)"])
    check("arm: an item takes its group's USAGE (computational, national) and GROUP-USAGE",
          got == ["A1", "N2", "GG", "M"], got)
    # ISO 13.18.52.4 GR 4-6: only a separate sign is the standard's '+' / '-' (branch review R1-2 a).
    got = refused(["       WORKING-STORAGE SECTION.", "       01 S PIC S9(3) VALUE -12.",
                   "       01 SL PIC S9(3) SIGN LEADING.", "       01 SS PIC S9(3) SIGN LEADING SEPARATE.",
                   "       01 GS SIGN TRAILING SEPARATE.", "           05 S2 PIC S9(2).",
                   "           05 S3 PIC S9(2) SIGN LEADING."],
                  ["S (1:3)", "SL (1:3)", "SS (1:4)", "S2 (1:3)", "S3 (1:2)"])
    check("arm: a reference-modified signed item is refused unless a SIGN SEPARATE clause applies (its own, or its "
          "group's when it has none)", got == ["S", "SL", "S3"], got)
    # A group shows its items' storage (branch review R1-2 b), with SYNCHRONIZED slack bytes (13.18.55.4 GR 9).
    got = refused(["       WORKING-STORAGE SECTION.", "       01 H.", "           05 HB PIC 9(4) COMP VALUE 1.",
                   "           05 FILLER PIC X.", "       01 H2.", "           05 FILLER PIC S9.",
                   "       01 H3.", "           05 H3A PIC X(2).", "           05 H3B PIC 9(2).",
                   "           05 H3C PIC -9.", "           05 H3D PIC S9 SIGN SEPARATE.",
                   "       01 H4.", "           05 H4A PIC X(2) SYNC.",
                   "       01 H5.", "           05 H5S.", "               10 H5B PIC 9 COMP."],
                  ["H", "H (1:2)", "H2", "H3", "H4", "H4A", "H5S", "H5"])
    check("arm: a group is refused when it holds a computational, signed or synchronized item, at any depth",
          [g.split(" (")[0] for g in got] == ["H", "H", "H2", "H4", "H5S", "H5"]
          and all("holding a" in g for g in got), got)
    # ISO 13.18.44.4 GR 2, 13.18.33.4 GR 3, 12.4.6.4.4 GR 2: storage described twice shows what either holds.
    got = refused(["       FILE SECTION.", "       FD F1.", "       01 F1-A PIC X(4).",
                   "       01 F1-B PIC S9(4) COMP.", "       FD F2.", "       01 F2-A PIC X(4).", "       FD F3.",
                   "       01 F3-A PIC X(4).", "       WORKING-STORAGE SECTION.", "       01 R1 PIC X(4).",
                   "       01 R2 REDEFINES R1 PIC 9(4) COMP.", "       01 R3 PIC X(4).", "       01 R4 REDEFINES R3 PIC 9(4).", "       01 R5.",
                   "           05 R5A PIC X(2).", "           05 R5B REDEFINES R5A PIC S99.",
                   "           05 R5C PIC X(2)."],
                  ["F1-A", "F2-A", "F3-A", "R1", "R3", "R5A", "R5C", "R5"],
                  environment=("       INPUT-OUTPUT SECTION.", "       I-O-CONTROL.",
                               "           SAME RECORD AREA FOR F2 F1."))
    check("arm: an item sharing storage (REDEFINES, a file's records, SAME RECORD AREA) is judged by what the other "
          "descriptions hold", [g.split(" (")[0] for g in got] == ["F1-A", "F2-A", "R1", "R5A", "R5"]
          and all("sharing storage with a" in g for g in got[:4]) and "holding a" in got[4], got)
    # Each source unit is judged by its own data descriptions (branch review R1-4).
    units = fixed_tokens(
        "       IDENTIFICATION DIVISION.", "       PROGRAM-ID. P1.", "       DATA DIVISION.",
        "       WORKING-STORAGE SECTION.", "       01 X PIC X(4).", "       01 GX PIC X(2) GLOBAL.",
        "       PROCEDURE DIVISION.", "           DISPLAY X", "           CALL \"P2\"", "           STOP RUN.",
        "       IDENTIFICATION DIVISION.", "       PROGRAM-ID. P2.", "       DATA DIVISION.",
        "       WORKING-STORAGE SECTION.", "       01 X PIC S9(4) COMP VALUE -1.", "       01 Y PIC X(2).",
        "       01 U USAGE DISPLAY PIC X.", "       PROCEDURE DIVISION.", "           DISPLAY X Y GX U",
        "           GOBACK.", "       END PROGRAM P2.", "       END PROGRAM P1.")
    got = [(i.line, i.operand, i.why) for i in cobol.display_issues(units)]
    check("arm: a contained program's DISPLAY is judged by its own entries, a GLOBAL name by its container's",
          got == [(19, "X", "a data item of class " + cobol.NUMERIC_COMPUTATIONAL_CLASS)], got)
    check("arm: a comment line past column 72 is no program text; a code line is",
          cobol.program_text_past_margin("      * comment" + " " * 60 + "SEQ\n" + "       MOVE A TO B." + " " * 53
                                         + "SEQ\n") == [2])
    named = cobol.defined_names(fixed_tokens(
        "       PROGRAM-ID. PROG1.",
        "       FILE-CONTROL. SELECT OPTIONAL F1 ASSIGN TO \"F1\".",
        "       FD F1.",
        "       01 PREVIOUS PIC X. 88 IS-Y VALUE \"Y\".",
        "       01 FILLER PIC X.",
        "       PROCEDURE DIVISION.",
        "       MAIN-PARA.",
        "           READ F1 PREVIOUS RECORD AT END CONTINUE END-READ.",
        "       S1 SECTION.",
    ))
    check("arm: the defined names are the data, condition, file, program, paragraph and section names (a keyword "
          "use of PREVIOUS is not a definition; FILLER names nothing)",
          named == {"PROG1", "F1", "PREVIOUS", "IS-Y", "MAIN-PARA", "S1"}, named)
    keywords = standard_keywords(SPEC_PATH.read_text(encoding="utf-8"))
    check("arm: the standard's keywords hold a context-sensitive word (8.10), a function name (8.11) and a "
          "directive word (8.12), and no table heading", {"PREVIOUS", "ABS", "IMP"} <= keywords
          and not {"CONTEXT-SENSITIVE", "LANGUAGE"} & keywords, sorted(keywords)[:12])
    cont = fixed_tokens("           DISPLAY \"ABC", "      -    \"DEF\" W-X.")
    check("arm: a continued literal is one token", [t.text for t in cont][:2] == ["DISPLAY", "\"ABCDEF\""],
          [t.text for t in cont])
    check("arm: free form strips a floating comment and reads a line-initial one as a comment",
          [t.text for t in cobol.tokenize(cobol.read_lines("*> all comment\nMOVE A TO B *> rest\n", "free"))]
          == ["MOVE", "A", "TO", "B"])
    print("=== generate.py --self-test: " + ("ALL GREEN" if not rc else "FAILED") + " ===")
    return rc


if __name__ == "__main__":
    for _stream in (sys.stdout, sys.stderr):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass
    sys.exit(main(sys.argv[1:]))
