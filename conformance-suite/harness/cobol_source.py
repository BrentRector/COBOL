"""COBOL source text as the conformance-suite generator reads it (DESIGN-conformance-suite.md §3, §4).

The generator (`generate.py`, the ONE reader of test headers) needs four facts from a test's PROGRAM text, never a
compilation of it: which COPY statements it writes (an undeclared copybook is refused), whether it observes an
exception condition (a run-time expectation is refused at 85), which operands its DISPLAY statements name, and what
the storage of each data item those operands reference holds, judged in the item's own source unit (the portability
lint, §3.3). This module computes exactly those, from the reference format to the tokens to the source units and their
data descriptions, and nothing a compiler would need beyond them: it is not a
COBOL parser, and a construct it cannot classify is reported as unclassifiable (the lint then refuses it) rather than
guessed.

Standard library only: the harness runs wherever Python 3 does (§5).

THE REFERENCE FORMAT (ISO 6.3, 6.4). Fixed form: columns 1-6 are the sequence number area, whose content "is defined by
the user and may consist of any character in the computer's coded character set" (§6.3.2); column 7 is the indicator
area; the program-text area begins in column 8 and ends at margin R, "a fixed position defined by the implementor"
(§6.3.1). This reader ends it at column 72, the 85 position; the lint refuses program text past column 72 in a portable
test and in the library text it copies, so no portable test depends on the choice. Free form (2002 on) has neither
area. In both, `*>` begins a floating comment outside a literal.
"""
from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import Callable, Iterable

#: The last column of the fixed-form program-text area this reader uses (the 85 margin R; §6.3.1 makes it the
#: implementor's at 2002 on, which is why the lint refuses text past it in a portable test).
FIXED_TEXT_END = 72
SEQUENCE_AREA = 6

#: A token: its kind, its text (a literal's text includes its prefix and quotes), the physical line it starts on and the
#: library text it came from (None for the test's own file), so a refusal names the file that holds the line.
WORD, ALPHANUMERIC, NATIONAL, BOOLEAN, NUMBER, PICTURE, PERIOD, PUNCT, DIRECTIVE = (
    "word", "alphanumeric-literal", "national-literal", "boolean-literal", "numeric-literal", "picture", "period",
    "punct", "directive")


@dataclass(frozen=True)
class Token:
    kind: str
    text: str
    line: int
    source: str | None = None

    @property
    def upper(self) -> str:
        return self.text.upper()


@dataclass(frozen=True)
class SourceLine:
    """One physical line: its number, its kind and the program text it contributes (comments removed)."""

    number: int
    raw: str
    kind: str  # "code", "comment", "continuation", "blank"
    text: str


# ── reference format ─────────────────────────────────────────────────────────────────────────────────────────────────

def _strip_floating(text: str, quote: str | None) -> tuple[str, str | None]:
    """`text` up to a floating comment indicator outside a literal, and the quote still open at its end (or None).

    `quote` is the literal open when the text begins (a fixed-form continuation of a literal)."""
    out = []
    i = 0
    while i < len(text):
        ch = text[i]
        if quote:
            out.append(ch)
            if ch == quote:
                if i + 1 < len(text) and text[i + 1] == quote:
                    out.append(text[i + 1])
                    i += 2
                    continue
                quote = None
            i += 1
            continue
        if ch in "\"'":
            quote = ch
            out.append(ch)
            i += 1
            continue
        if text.startswith("*>", i):
            break
        out.append(ch)
        i += 1
    return "".join(out), quote


def read_lines(text: str, source_format: str) -> list[SourceLine]:
    """Every physical line of `text` in `source_format` ("fixed" or "free"), classified, with its program text."""
    lines: list[SourceLine] = []
    quote: str | None = None
    for number, raw in enumerate(text.split("\n"), start=1):
        raw = raw.rstrip("\r")
        if source_format == "fixed":
            indicator = raw[SEQUENCE_AREA] if len(raw) > SEQUENCE_AREA else " "
            area = raw[SEQUENCE_AREA + 1:FIXED_TEXT_END]
            if indicator in "*/":
                lines.append(SourceLine(number, raw, "comment", ""))
                continue
            if indicator == "-":
                body = area.lstrip()
                if quote and body[:1] == quote:
                    body = body[1:]  # a continued literal resumes after its continuation quote (§6.3.3, 6.2.2)
                code, quote = _strip_floating(body, quote)
                lines.append(SourceLine(number, raw, "continuation", code))
                continue
            code, quote = _strip_floating(area, None)
        else:
            stripped = raw.lstrip()
            if stripped.startswith("*>"):
                lines.append(SourceLine(number, raw, "comment", ""))
                continue
            code, quote = _strip_floating(raw, None)
            # A free-form literal does not continue across lines without a continuation indicator.
            quote = None
        lines.append(SourceLine(number, raw, "code" if code.strip() else "blank", code))
    return lines


def program_text_past_margin(text: str) -> list[int]:
    """The physical lines of a fixed-form `text` holding program text past column FIXED_TEXT_END (§6.3.1). A comment
    line is no program text: wherever margin R lies, the rest of it is comment or outside the program-text area."""
    found = []
    for number, raw in enumerate(text.split("\n"), start=1):
        raw = raw.rstrip("\r")
        if len(raw) > SEQUENCE_AREA and raw[SEQUENCE_AREA] in "*/":
            continue
        if len(raw) > FIXED_TEXT_END and raw[FIXED_TEXT_END:].strip():
            found.append(number)
    return found


# ── tokens ───────────────────────────────────────────────────────────────────────────────────────────────────────────

_LITERAL = re.compile(r"""(?i)(NX|BX|UX|N|X|Z|B|U)?("(?:[^"]|"")*"|'(?:[^']|'')*')""")
_WORD = re.compile(r"[A-Za-z0-9](?:[A-Za-z0-9_-]*[A-Za-z0-9_])?")
_DECIMAL = re.compile(r"[0-9]+[.,][0-9]+(?:E[+-]?[0-9]+)?", re.IGNORECASE)
_DIRECTIVE = re.compile(r">>\s*([A-Za-z][A-Za-z0-9-]*)")
_LITERAL_KIND = {"N": NATIONAL, "NX": NATIONAL, "B": BOOLEAN, "BX": BOOLEAN}


def _join(lines: Iterable[SourceLine]) -> list[tuple[int, str]]:
    """Logical text runs: each code line, with a fixed-form continuation appended to the run it continues."""
    runs: list[tuple[int, str]] = []
    for line in lines:
        if line.kind == "continuation" and runs:
            number, text = runs[-1]
            runs[-1] = (number, text + line.text)
        elif line.kind in ("code", "continuation"):
            runs.append((line.number, line.text))
    return runs


def tokenize(lines: Iterable[SourceLine], source: str | None = None) -> list[Token]:
    """The tokens of the program text, in order, each naming `source` (the library text they come from, or None). A
    PICTURE character-string after PIC/PICTURE [IS] is one token."""
    tokens: list[Token] = []
    for number, text in _join(lines):
        i = 0
        while i < len(text):
            ch = text[i]
            if ch.isspace() or ch in ",;":
                i += 1
                continue
            last = tokens[-1].upper if tokens else ""
            if last in ("PIC", "PICTURE") or (last == "IS" and len(tokens) > 1 and tokens[-2].upper in ("PIC", "PICTURE")):
                if not (text[i:i + 2].upper() == "IS" and (i + 2 == len(text) or text[i + 2].isspace())):
                    end = i
                    while end < len(text) and not text[end].isspace():
                        end += 1
                    pic = text[i:end]
                    trailing_period = pic.endswith(".")
                    pic = pic.rstrip(".,;")
                    tokens.append(Token(PICTURE, pic, number))
                    if trailing_period:
                        tokens.append(Token(PERIOD, ".", number))
                    i = end
                    continue
            if m := _DIRECTIVE.match(text, i):
                tokens.append(Token(DIRECTIVE, m.group(1).upper(), number))
                i = m.end()
                continue
            if m := _LITERAL.match(text, i):
                prefix = (m.group(1) or "").upper()
                tokens.append(Token(_LITERAL_KIND.get(prefix, ALPHANUMERIC), m.group(0), number))
                i = m.end()
                continue
            if m := _DECIMAL.match(text, i):
                if not (m.end() < len(text) and (text[m.end()].isalnum() or text[m.end()] == "-")):
                    tokens.append(Token(NUMBER, m.group(0), number))
                    i = m.end()
                    continue
            if m := _WORD.match(text, i):
                word = m.group(0)
                tokens.append(Token(NUMBER if word.isdigit() else WORD, word, number))
                i = m.end()
                continue
            if ch == "." and (i + 1 == len(text) or text[i + 1].isspace()):
                tokens.append(Token(PERIOD, ".", number))
                i += 1
                continue
            if text.startswith("**", i):
                tokens.append(Token(PUNCT, "**", number))
                i += 2
                continue
            tokens.append(Token(PUNCT, ch, number))
            i += 1
    return tokens if source is None else [Token(t.kind, t.text, t.line, source) for t in tokens]


# ── COPY ─────────────────────────────────────────────────────────────────────────────────────────────────────────────

@dataclass(frozen=True)
class CopyStatement:
    """A COPY statement: the library text it names (a text-name, or a literal's content), its line and the library
    text holding it (None for the test's own file)."""

    name: str
    line: int
    replacing: bool
    source: str | None = None


def copy_statements(tokens: list[Token]) -> list[CopyStatement]:
    """Every COPY statement in `tokens` (ISO 7.2.3: COPY {text-name-1 | literal-1} [{OF|IN} ...] ...)."""
    found = []
    for i, tok in enumerate(tokens):
        if tok.kind != WORD or tok.upper != "COPY" or i + 1 >= len(tokens):
            continue
        target = tokens[i + 1]
        if target.kind == ALPHANUMERIC:
            name = target.text[1:-1]
        elif target.kind in (WORD, NUMBER):
            name = target.text
        else:
            continue
        replacing = False
        for later in tokens[i + 2:]:
            if later.kind == PERIOD:
                break
            if later.kind == WORD and later.upper == "REPLACING":
                replacing = True
        found.append(CopyStatement(name, tok.line, replacing, tok.source))
    return found


def expand_copies(tokens: list[Token], library: Callable[[str], list[Token] | None], depth: int = 0) -> list[Token]:
    """`tokens` with each COPY statement replaced by its library text's tokens, recursively (REPLACING not applied:
    the classification of names a REPLACING introduces is then unknown, which the lint reports, never guesses).

    `library(name)` returns the library text's tokens or None when the text is not available (the refusal of an
    undeclared COPY is the caller's)."""
    out: list[Token] = []
    i = 0
    while i < len(tokens):
        tok = tokens[i]
        if tok.kind == WORD and tok.upper == "COPY" and i + 1 < len(tokens) and depth < 16:
            target = tokens[i + 1]
            name = target.text[1:-1] if target.kind == ALPHANUMERIC else target.text
            j = i + 2
            while j < len(tokens) and tokens[j].kind != PERIOD:
                j += 1
            included = library(name)
            if included is not None:
                out.extend(expand_copies(included, library, depth + 1))
            i = j + 1
            continue
        out.append(tok)
        i += 1
    return out


# ── run-time expectations ────────────────────────────────────────────────────────────────────────────────────────────

#: The intrinsic functions that report the last exception status (ISO 15.28-15.33).
EXCEPTION_FUNCTIONS = frozenset({"EXCEPTION-FILE", "EXCEPTION-FILE-N", "EXCEPTION-LOCATION", "EXCEPTION-LOCATION-N",
                                 "EXCEPTION-STATEMENT", "EXCEPTION-STATUS"})


def observes_exception(tokens: list[Token]) -> Token | None:
    """The first token at which the program observes an exception condition (a TURN directive, an exception-name or an
    exception function), or None. §3.2: run-time violations are judged only where a program can observe them, from
    2002 on, by turning checking on and trapping the condition."""
    for tok in tokens:
        if tok.kind == DIRECTIVE and tok.text == "TURN":
            return tok
        if tok.kind == WORD and (tok.upper.startswith("EC-") or tok.upper in EXCEPTION_FUNCTIONS):
            return tok
    return None


# ── source units ─────────────────────────────────────────────────────────────────────────────────────────────────────

#: The paragraphs that open a source unit (ISO 11.2.1: the IDENTIFICATION DIVISION header is optional, so a unit
#: begins at its paragraph, never at the header). FACTORY and OBJECT open one only as a paragraph of their own.
_UNIT_IDS = frozenset({"PROGRAM-ID", "FUNCTION-ID", "CLASS-ID", "METHOD-ID", "INTERFACE-ID"})
_UNIT_PARAGRAPHS = frozenset({"FACTORY", "OBJECT"})
#: The words after END in an end marker (END PROGRAM, END METHOD, ...).
_UNIT_ENDS = frozenset({"PROGRAM", "FUNCTION", "METHOD", "CLASS", "FACTORY", "OBJECT", "INTERFACE"})
_DIVISIONS = {"ENVIRONMENT": "environment", "DATA": "data", "PROCEDURE": "procedure"}


@dataclass
class SourceUnit:
    """One source unit of a compilation group (a program, function, class, factory, object, method or interface
    definition): the tokens of its environment, data and procedure divisions, each without its header."""

    environment: list[Token] = field(default_factory=list)
    data: list[Token] = field(default_factory=list)
    procedure: list[Token] = field(default_factory=list)


def source_units(tokens: list[Token]) -> list[SourceUnit]:
    """The source units of `tokens` (COPY already expanded), in order. A contained program is a unit of its own (it
    follows its container's procedure division, and its end marker ends it). Text before any unit paragraph, such
    as a fragment that is only a division, forms one unit."""
    units = [SourceUnit()]
    division: str | None = None
    i = 0
    while i < len(tokens):
        tok = tokens[i]
        nxt = tokens[i + 1] if i + 1 < len(tokens) else None
        if tok.kind == WORD and nxt is not None and nxt.kind == WORD and nxt.upper == "DIVISION":
            division = _DIVISIONS.get(tok.upper)  # IDENTIFICATION / ID DIVISION holds no unit's data or statements
            i += 2
            continue
        opens = tok.kind == WORD and (tok.upper in _UNIT_IDS or (
            tok.upper in _UNIT_PARAGRAPHS and nxt is not None and nxt.kind == PERIOD
            and (i == 0 or tokens[i - 1].kind == PERIOD)))
        if opens:
            last = units[-1]
            if last.environment or last.data or last.procedure:
                units.append(SourceUnit())
            division = None
            i += 1
            continue
        if tok.kind == WORD and tok.upper == "END" and nxt is not None and nxt.upper in _UNIT_ENDS:
            division = None
            i += 2
            continue
        if division is not None:
            getattr(units[-1], division).append(tok)
        i += 1
    return units


def same_record_areas(environment: list[Token]) -> list[list[str]]:
    """The file-names of each record-area format SAME clause (SAME RECORD AREA FOR f1 f2 ...) in an environment
    division's tokens. ISO 12.4.6.4.4 GR 2: sharing the record area "is equivalent to an implicit redefinition of the
    area with records aligned on the leftmost byte position"."""
    groups = []
    for i, tok in enumerate(environment):
        if not (tok.kind == WORD and tok.upper == "SAME" and i + 1 < len(environment)
                and environment[i + 1].upper == "RECORD"):
            continue
        names = []
        j = i + 2
        while j < len(environment) and environment[j].kind == WORD and environment[j].upper not in (
                "SAME", "MULTIPLE", "APPLY"):
            if environment[j].upper not in ("AREA", "FOR"):
                names.append(environment[j].upper)
            j += 1
        groups.append(names)
    return groups


# ── data descriptions ────────────────────────────────────────────────────────────────────────────────────────────────

#: The classes the portability lint distinguishes. Only ALPHANUMERIC_CLASS and NUMERIC_EDITED_CLASS may be DISPLAYed by
#: a portable test (§3.3).
ALPHANUMERIC_CLASS = "alphanumeric"
NUMERIC_EDITED_CLASS = "numeric-edited"
NUMERIC_DISPLAY_CLASS = "numeric"
#: A signed numeric display item no SIGN SEPARATE clause applies to: "The implementor shall specify the position and
#: representation of the operational sign" (ISO 13.18.52.4 GR 4; GR 5 b for SIGN without SEPARATE).
NUMERIC_SIGNED_CLASS = "numeric (signed, its operational sign not separate)"
NUMERIC_COMPUTATIONAL_CLASS = "numeric (computational usage)"
NATIONAL_CLASS = "national"
BOOLEAN_CLASS = "boolean"
INDEX_CLASS = "index"
POINTER_CLASS = "pointer"
OBJECT_CLASS = "object reference"
SCREEN_CLASS = "screen item"
UNKNOWN_CLASS = "unclassifiable"
PORTABLE_CLASSES = frozenset({ALPHANUMERIC_CLASS, NUMERIC_EDITED_CLASS})
#: The classes whose storage holds only characters whose representation the standard fixes: an unsigned or separately
#: signed numeric display item holds digits and the basic special characters '+' and '-' (ISO 13.18.52.4 GR 6 b).
#: A DISPLAY of a group, or of an item sharing storage with another, shows that storage character for character.
_PORTABLE_STORAGE = frozenset({ALPHANUMERIC_CLASS, NUMERIC_EDITED_CLASS, NUMERIC_DISPLAY_CLASS})
#: The storage a SYNCHRONIZED item adds to a group: "The effect of this clause is defined by the implementor" (ISO
#: 13.18.55.4 GR 9), so the alignment and the unused bytes a group then holds are the implementor's.
_SYNCHRONIZED_STORAGE = "a SYNCHRONIZED item (alignment and slack bytes are the implementor's, ISO 13.18.55.4 GR 9)"
#: The prefixes of the composite descriptions: what a group holds, and what an item shares its storage with.
_HOLDING, _SHARING = "holding ", "sharing storage with "

#: The computational usages (ISO 13.18.60 USAGE clause, with the COMP-n spellings implementations add). Each is a
#: reserved word, so no data-name can match it.
_COMPUTATIONAL = re.compile(r"^(COMP(UTATIONAL)?(-[1-5X])?|BINARY(-(CHAR|SHORT|LONG|DOUBLE))?|PACKED-DECIMAL"
                            r"|FLOAT-(SHORT|LONG|EXTENDED|BINARY-(32|64|128)|DECIMAL-(16|34)))$")
_POINTER_USAGES = frozenset({"POINTER", "PROGRAM-POINTER", "FUNCTION-POINTER"})
_SECTIONS = frozenset({"FILE", "WORKING-STORAGE", "LOCAL-STORAGE", "LINKAGE", "REPORT", "SCREEN", "COMMUNICATION"})
#: An explicit USAGE DISPLAY: it decides the item's usage (no group usage is inherited past it) without a class of
#: its own.
_USAGE_DISPLAY = "display"


def picture_class(picture: str) -> str:
    """The class a PICTURE character-string gives (ISO 13.18.40): national, alphanumeric, boolean, numeric or
    numeric-edited. Repetition counts are irrelevant to the class and are removed first."""
    symbols = set(re.sub(r"\([0-9]+\)", "", picture.upper()))
    if "N" in symbols:
        return NATIONAL_CLASS
    if symbols & {"X", "A"}:
        return ALPHANUMERIC_CLASS
    if symbols and symbols <= {"1"}:
        return BOOLEAN_CLASS
    if symbols and symbols <= set("9SVP"):
        return NUMERIC_DISPLAY_CLASS
    return NUMERIC_EDITED_CLASS


def _usage_class(words: list[str]) -> str | None:
    for w in words:
        if w == "NATIONAL":
            return NATIONAL_CLASS
        if w == "INDEX":
            return INDEX_CLASS
        if w in _POINTER_USAGES:
            return POINTER_CLASS
        if w == "OBJECT":
            return OBJECT_CLASS
        if w == "BIT":
            return BOOLEAN_CLASS
        if _COMPUTATIONAL.match(w):
            return NUMERIC_COMPUTATIONAL_CLASS
        if w == "DISPLAY":
            return _USAGE_DISPLAY
    return None


@dataclass
class _Entry:
    level: int
    name: str | None
    picture: str | None = None
    usage: str | None = None  # the class a USAGE clause decides (or _USAGE_DISPLAY), if any
    group_usage: str | None = None
    constant: str | None = None  # the class of a constant entry's literal
    renames: bool = False
    screen: bool = False
    sign: str | None = None  # "separate" or "embedded": the SIGN clause of this entry, if any
    synchronized: bool = False
    redefines: str | None = None
    record_area: str | None = None  # the file-name of the FD or SD a level-1 record is subordinate to
    indexes: list[str] = field(default_factory=list)


def _entries(tokens: list[Token]) -> list[_Entry]:
    """The data description entries of one data division's tokens (COPY already expanded, the header excluded)."""
    entries: list[_Entry] = []
    section = ""
    record_area: str | None = None
    sentence: list[Token] = []
    for tok in tokens + [Token(PERIOD, ".", 0)]:
        if tok.kind != PERIOD:
            sentence.append(tok)
            continue
        words = [t.upper for t in sentence]
        if len(words) == 2 and words[1] == "SECTION" and words[0] in _SECTIONS:
            section, record_area = words[0], None
        elif words[:1] in (["FD"], ["SD"]) and len(words) > 1:
            record_area = words[1]
        elif words[:1] in (["RD"], ["CD"]):
            record_area = None  # a report's level-1 groups do not redefine one another (ISO 13.18.33.4 GR 3)
        elif sentence and sentence[0].kind == NUMBER and sentence[0].text.isdigit():
            e = _entry(sentence, section == "SCREEN")
            if e.level == 1:
                e.record_area = record_area
            entries.append(e)
        sentence = []
    return entries


def _entry(sentence: list[Token], screen: bool) -> _Entry:
    level = int(sentence[0].text)
    rest = sentence[1:]
    name = None
    if rest and rest[0].kind == WORD and rest[0].upper not in ("FILLER",) and not _is_clause_word(rest[0].upper):
        name = rest[0].upper
        rest = rest[1:]
    elif rest and rest[0].upper == "FILLER":
        rest = rest[1:]
    e = _Entry(level, name, screen=screen)
    words = [t.upper for t in rest]
    for i, tok in enumerate(rest):
        w = words[i]
        if tok.kind == PICTURE:
            e.picture = tok.text
        elif w == "RENAMES":
            e.renames = True
        elif w == "REDEFINES" and i + 1 < len(words):
            e.redefines = words[i + 1]
        elif w in ("SYNC", "SYNCHRONIZED"):
            e.synchronized = True
        elif w in ("LEADING", "TRAILING") and e.sign is None:
            e.sign = "embedded"
        elif w == "SEPARATE":
            e.sign = "separate"
        elif w == "CONSTANT":
            lit = next((t for t in rest[i + 1:] if t.upper not in ("AS", "IS", "GLOBAL")), None)
            e.constant = (ALPHANUMERIC_CLASS if lit is not None and lit.kind == ALPHANUMERIC else
                          NATIONAL_CLASS if lit is not None and lit.kind == NATIONAL else
                          NUMERIC_DISPLAY_CLASS if lit is not None and lit.kind == NUMBER else UNKNOWN_CLASS)
        elif w == "GROUP-USAGE" and i + 1 < len(words):
            j = i + 2 if words[i + 1] == "IS" and i + 2 < len(words) else i + 1
            e.group_usage = NATIONAL_CLASS if words[j] == "NATIONAL" else BOOLEAN_CLASS if words[j] == "BIT" else None
        elif w == "INDEXED":
            j = i + 1
            if j < len(words) and words[j] == "BY":
                j += 1
            while j < len(rest) and rest[j].kind == WORD and not _is_clause_word(words[j]):
                e.indexes.append(words[j])
                j += 1
    e.usage = _usage_class([w for w in words if w not in ("USAGE", "IS")])
    return e


_CLAUSE_WORDS = frozenset({
    "PIC", "PICTURE", "USAGE", "VALUE", "VALUES", "OCCURS", "REDEFINES", "RENAMES", "JUST", "JUSTIFIED", "BLANK",
    "SIGN", "SYNC", "SYNCHRONIZED", "EXTERNAL", "GLOBAL", "CONSTANT", "GROUP-USAGE", "TYPEDEF", "TYPE", "BASED",
    "ANY", "DYNAMIC", "ALIGNED", "PROPERTY", "SAME", "SELECT", "VALIDATE", "DEFAULT", "DESTINATION", "INVALID",
    "PRESENT", "CLASS", "INDEXED", "ASCENDING", "DESCENDING", "KEY", "DEPENDING", "TIMES", "TO", "ON", "BY", "IS",
    "LEADING", "TRAILING", "SEPARATE", "CHARACTER", "ZERO", "WHEN", "THRU", "THROUGH", "FALSE", "TRUE",
})


def _is_clause_word(word: str) -> bool:
    return word in _CLAUSE_WORDS or bool(_COMPUTATIONAL.match(word)) or word in _POINTER_USAGES or word in (
        "DISPLAY", "NATIONAL", "INDEX", "BIT", "OBJECT")


class _Tree:
    """The data description entries of one data division as the hierarchy their level-numbers make, with the usage,
    SIGN and SYNCHRONIZED clauses a group passes to its items (ISO 13.18.60.4 GR 1 "If the USAGE clause is specified
    or implied at a group level, it applies only to each elementary item in the group"; 13.18.52.4 GR 1-3; 13.18.55.4
    GR 1) and the storage areas the entries share."""

    def __init__(self, entries: list[_Entry], shared_record_areas: Iterable[Iterable[str]]) -> None:
        self.entries = entries
        n = len(entries)
        self.parent: list[int | None] = [None] * n
        self.kids: list[list[int]] = [[] for _ in range(n)]
        stack: list[int] = []
        for i, e in enumerate(entries):
            if e.level == 88:
                continue  # a condition-name holds no storage of its own
            if not 1 <= e.level <= 49:
                stack = []  # 66, 77 and 78 are no group's items, and end the record before them
                continue
            while stack and entries[stack[-1]].level >= e.level:
                stack.pop()
            if stack:
                self.parent[i] = stack[-1]
                self.kids[stack[-1]].append(i)
            stack.append(i)
        self.usage: list[str | None] = [None] * n
        self.sign: list[str | None] = [None] * n
        self.synchronized = [False] * n
        for i, e in enumerate(entries):  # a parent precedes its items
            p = self.parent[i]
            self.usage[i] = e.group_usage or e.usage or (self.usage[p] if p is not None else None)
            self.sign[i] = e.sign or (self.sign[p] if p is not None else None)
            self.synchronized[i] = e.synchronized or (p is not None and self.synchronized[p])
        self.storage = [self._own_storage(i) for i in range(n)]
        for i in range(n - 1, -1, -1):  # an item follows its group
            for k in self.kids[i]:
                self.storage[i] |= self.storage[k]
        self.shared: list[set[str]] = [set() for _ in range(n)]
        self._share(shared_record_areas)

    def is_group(self, i: int) -> bool:
        return bool(self.kids[i])

    def elementary_class(self, i: int) -> str:
        """The class of entry `i` as an elementary item, with the usage and SIGN clause its groups pass to it."""
        e = self.entries[i]
        if e.screen:
            return SCREEN_CLASS
        if e.renames or e.level in (66, 78):
            return UNKNOWN_CLASS
        if e.constant is not None:
            return e.constant
        usage = self.usage[i]
        if e.picture is None:
            return usage if usage not in (None, _USAGE_DISPLAY) else UNKNOWN_CLASS
        pic = picture_class(e.picture)
        if usage == NATIONAL_CLASS or pic == NATIONAL_CLASS:
            return NATIONAL_CLASS
        if usage not in (None, _USAGE_DISPLAY):
            return usage
        if pic == NUMERIC_DISPLAY_CLASS and "S" in e.picture.upper() and self.sign[i] != "separate":
            return NUMERIC_SIGNED_CLASS
        return pic

    def _own_storage(self, i: int) -> set[str]:
        """The descriptions of the storage entry `i` holds itself whose representation is the implementor's."""
        if self.entries[i].level == 88 or self.kids[i]:
            return set()
        cls = self.elementary_class(i)
        found = set() if cls in _PORTABLE_STORAGE else {f"a {cls} item"}
        if self.synchronized[i]:
            found.add(_SYNCHRONIZED_STORAGE)
        return found

    def _subtree(self, i: int) -> list[int]:
        out, todo = [], [i]
        while todo:
            j = todo.pop()
            out.append(j)
            todo.extend(self.kids[j])
        return out

    def _share(self, shared_record_areas: Iterable[Iterable[str]]) -> None:
        """Mark every item whose storage another entry also describes with that entry's storage: a REDEFINES (ISO
        13.18.44.4 GR 2 "When the same storage area is defined by more than one data description entry, the
        data-name associated with any of those data description entries may be used to reference that storage
        area"), the level-1 records of one FD or SD (13.18.33.4 GR 3) and the files of one SAME RECORD AREA clause
        (12.4.6.4.4 GR 2)."""
        entries = self.entries
        root = list(range(len(entries)))

        def find(x: int) -> int:
            while root[x] != x:
                root[x] = root[root[x]]
                x = root[x]
            return x

        for i, e in enumerate(entries):
            if e.redefines is None:
                continue
            for j in range(i - 1, -1, -1):  # the redefined entry: the last earlier entry at this level
                o = entries[j]
                if o.level == 88 or (1 <= o.level <= 49 and o.level > e.level):
                    continue
                if o.level == e.level and o.name == e.redefines:
                    root[find(i)] = find(j)
                elif o.level == e.level and o.redefines == e.redefines:
                    continue  # an earlier redefinition of the same area
                break
        file_root: dict[str, str] = {}

        def file_of(name: str) -> str:
            while file_root.get(name, name) != name:
                name = file_root[name]
            return name

        for files in shared_record_areas:
            names = [f.upper() for f in files]
            for other in names[1:]:
                if file_of(other) != file_of(names[0]):
                    file_root[file_of(other)] = file_of(names[0])
        first_record: dict[str, int] = {}
        for i, e in enumerate(entries):
            if e.level == 1 and e.record_area is not None:
                area = file_of(e.record_area)
                if area in first_record:
                    root[find(i)] = find(first_record[area])
                else:
                    first_record[area] = i
        areas: dict[int, list[int]] = {}
        for i in range(len(entries)):
            areas.setdefault(find(i), []).append(i)
        for members in areas.values():
            for m in members:
                others = set().union(*(self.storage[o] for o in members if o != m))
                for j in self._subtree(m):
                    self.shared[j] |= {_SHARING + d for d in others}
        for i in range(len(entries) - 1, -1, -1):  # a group shows the storage its items share
            for k in self.kids[i]:
                self.shared[i] |= self.shared[k]

    def display_classes(self, i: int) -> set[str] | None:
        """What a DISPLAY of entry `i` shows: its class, or for a group (ISO 13.18.29.4 GR 3: alphanumeric unless a
        GROUP-USAGE clause applies) what its storage holds; with what the storage it shares holds. None for a
        condition-name."""
        e = self.entries[i]
        if e.level == 88:
            return None
        if self.is_group(i):
            if e.group_usage is not None:
                found = {e.group_usage}
            elif self.storage[i]:
                found = {_HOLDING + d for d in self.storage[i]}
            else:
                found = {ALPHANUMERIC_CLASS}
        else:
            found = {self.elementary_class(i)}
        return found | self.shared[i]


def data_classes(tokens: list[Token], shared_record_areas: Iterable[Iterable[str]] = ()) -> dict[str, set[str]]:
    """`{data-name -> what a DISPLAY of each entry declaring it shows}` for one data division's tokens (COPY already
    expanded, the header excluded); `shared_record_areas` are the file-names of its unit's SAME RECORD AREA clauses.

    An elementary item's class comes from its PICTURE and from its own USAGE and SIGN clauses or those its groups
    pass to it; a signed numeric display item no SIGN SEPARATE clause applies to is classed apart, because its sign is
    the implementor's. A group is alphanumeric unless a GROUP-USAGE clause applies, and a DISPLAY of it shows its
    items' storage, so it is judged by what it holds. An item sharing storage (REDEFINES, a file's records, SAME
    RECORD AREA) is also judged by what the other descriptions of that storage hold. An index-name is an index. A
    name declared twice keeps the classes of both, so an unqualified reference the lint cannot tie to one entry is
    judged by the worst of them."""
    tree = _Tree(_entries(tokens), shared_record_areas)
    classes: dict[str, set[str]] = {}
    for i, e in enumerate(tree.entries):
        for idx in e.indexes:
            classes.setdefault(idx, set()).add(INDEX_CLASS)
        found = tree.display_classes(i)
        if e.name and found is not None:
            classes.setdefault(e.name, set()).update(found)
    return classes


# ── DISPLAY operands ─────────────────────────────────────────────────────────────────────────────────────────────────

#: Words that end a DISPLAY statement's operand list: its own phrases (ISO 14.9.11.2) and the statements and scope
#: words that can follow it.
_DISPLAY_STOP = frozenset({
    "UPON", "WITH", "NO", "END-DISPLAY", "ON", "NOT", "AT", "LINE", "COLUMN", "COL",
    "ACCEPT", "ADD", "ALLOCATE", "CALL", "CANCEL", "CLOSE", "COMMIT", "COMPUTE", "CONTINUE", "DELETE", "DISABLE",
    "DISPLAY", "DIVIDE", "ENABLE", "ENTER", "EVALUATE", "EXIT", "FREE", "GENERATE", "GO", "GOBACK", "IF",
    "INITIALIZE", "INITIATE", "INSPECT", "INVOKE", "MERGE", "MOVE", "MULTIPLY", "OPEN", "PERFORM", "PURGE", "RAISE",
    "READ", "RECEIVE", "RELEASE", "RESUME", "RETURN", "REWRITE", "ROLLBACK", "SEARCH", "SEND", "SET", "SORT", "START",
    "STOP", "STRING", "SUBTRACT", "SUPPRESS", "TERMINATE", "UNLOCK", "UNSTRING", "USE", "VALIDATE", "WRITE", "ELSE",
    "WHEN", "THEN", "OTHER", "ALSO",
})
#: Figurative constants DISPLAY shows as their alphanumeric representation (ISO 14.9.11.4 GR 3).
_FIGURATIVES = frozenset({"SPACE", "SPACES", "QUOTE", "QUOTES", "HIGH-VALUE", "HIGH-VALUES", "LOW-VALUE",
                          "LOW-VALUES", "ZERO", "ZEROS", "ZEROES"})


@dataclass(frozen=True)
class DisplayIssue:
    """A DISPLAY operand (or phrase) a portable test may not write, with why; `source` as its token's."""

    line: int
    source: str | None
    operand: str
    why: str


def _skip_parens(tokens: list[Token], j: int) -> tuple[int, bool]:
    """Past the balanced parenthesized groups at `j`; whether one held a ':' at its own depth (reference
    modification, ISO 8.4.3.3)."""
    refmod = False
    while j < len(tokens) and tokens[j].kind == PUNCT and tokens[j].text == "(":
        depth = 0
        while j < len(tokens):
            t = tokens[j]
            if t.kind == PUNCT and t.text == "(":
                depth += 1
            elif t.kind == PUNCT and t.text == ")":
                depth -= 1
                if depth == 0:
                    j += 1
                    break
            elif t.kind == PUNCT and t.text == ":" and depth == 1:
                refmod = True
            j += 1
    return j, refmod




def _why(bad: list[str]) -> str:
    """The reason a DISPLAY operand is refused, from the descriptions of what it shows."""
    return "a data item " + " / ".join(d if d.startswith((_HOLDING, _SHARING)) else "of class " + d for d in bad)


def display_issues(tokens: list[Token]) -> list[DisplayIssue]:
    """Every DISPLAY operand of the procedure divisions in `tokens` (a compilation group, COPY already expanded) the
    portability lint refuses (§3.3).

    ISO 14.9.11.4 GR 1 makes "any conversion of data required between literal-1 or the data item referenced by
    identifier-1 and the device" the implementor's, so a portable test DISPLAYs only alphanumeric literals,
    alphanumeric items and numeric-edited items, and a group only when every item it holds is one of those or an
    unsigned or separately signed numeric display item. A reference-modified numeric or numeric-edited item of usage
    display is alphanumeric (§8.4.3.3.4 GR 6 c), so it may be DISPLAYed unless its sign is the implementor's; one of
    computational usage shows its storage and may not. UPON names an implementor-defined device (14.9.11.3 SR 2),
    whose output no expected stdout can hold. Each source unit's DISPLAY statements are judged by that unit's own
    data descriptions; a name the unit does not declare (a GLOBAL or EXTERNAL item of another) by every unit's."""
    units = source_units(tokens)
    own = [data_classes(u.data, same_record_areas(u.environment)) for u in units]
    every: dict[str, set[str]] = {}
    for classes in own:
        for name, found in classes.items():
            every.setdefault(name, set()).update(found)
    issues: list[DisplayIssue] = []
    for unit, classes in zip(units, own):
        issues.extend(_unit_display_issues(unit.procedure, lambda name, c=classes: c.get(name) or every.get(name)))
    return issues


def _unit_display_issues(tokens: list[Token], lookup: Callable[[str], set[str] | None]) -> list[DisplayIssue]:
    """The refused DISPLAY operands of one procedure division's tokens, `lookup` giving a data-name's classes."""
    issues: list[DisplayIssue] = []
    i = 0
    while i < len(tokens):
        if not (tokens[i].kind == WORD and tokens[i].upper == "DISPLAY"):
            i += 1
            continue
        j = i + 1
        while j < len(tokens):
            t = tokens[j]
            if t.kind in (PERIOD, DIRECTIVE):
                break
            if t.kind == ALPHANUMERIC:
                j += 1
                continue
            if t.kind in (NATIONAL, BOOLEAN, NUMBER):
                issues.append(DisplayIssue(t.line, t.source, t.text, f"a {t.kind}"))
                j += 1
                continue
            if t.kind != WORD:
                issues.append(DisplayIssue(t.line, t.source, t.text,
                                           "not a literal or an identifier (an expression?)"))
                break
            w = t.upper
            if w == "UPON":
                issues.append(DisplayIssue(t.line, t.source, "UPON",
                                           "an implementor-defined device, not the standard display device"))
                break
            if w in _DISPLAY_STOP or w.startswith("END-"):
                break
            if w == "ALL" and j + 1 < len(tokens):
                lit = tokens[j + 1]
                if lit.kind != ALPHANUMERIC:
                    issues.append(DisplayIssue(lit.line, lit.source, "ALL " + lit.text,
                                               "the ALL form of a " + lit.kind))
                j += 2
                continue
            if w in _FIGURATIVES:
                j += 1
                continue
            if w in ("NULL", "NULLS"):
                issues.append(DisplayIssue(t.line, t.source, t.text, "a pointer figurative constant"))
                j += 1
                continue
            if w == "FUNCTION":
                name = tokens[j + 1].text if j + 1 < len(tokens) else ""
                issues.append(DisplayIssue(t.line, t.source, f"FUNCTION {name}", "a function result"))
                j, _ = _skip_parens(tokens, j + 2)
                continue
            # An identifier: a data-name with its qualifiers, subscripts and reference modification.
            k = j + 1
            while k + 1 < len(tokens) and tokens[k].upper in ("OF", "IN") and tokens[k + 1].kind == WORD:
                k += 2
            k, refmod = _skip_parens(tokens, k)
            found = lookup(w) or {UNKNOWN_CLASS}
            if refmod:
                found = {ALPHANUMERIC_CLASS if c in (NUMERIC_DISPLAY_CLASS, NUMERIC_EDITED_CLASS) else c for c in found}
            bad = sorted(c for c in found if c not in PORTABLE_CLASSES)
            if bad:
                issues.append(DisplayIssue(t.line, t.source, t.text, _why(bad)))
            j = k
        i = j
    return issues
