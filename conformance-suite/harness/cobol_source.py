"""COBOL source text as the conformance-suite generator reads it (DESIGN-conformance-suite.md §3, §4).

The generator (`generate.py`, the ONE reader of test headers) needs four facts from a test's PROGRAM text, never a
compilation of it: which COPY statements it writes (an undeclared copybook is refused), whether it observes an
exception condition (a run-time expectation is refused at 85), which operands its DISPLAY statements name, and the
class of each data item those operands reference (the portability lint, §3.3). This module computes exactly those, from
the reference format to the tokens to the data descriptions, and nothing a compiler would need beyond them: it is not a
COBOL parser, and a construct it cannot classify is reported as unclassifiable (the lint then refuses it) rather than
guessed.

Standard library only: the harness runs wherever Python 3 does (§5).

THE REFERENCE FORMAT (ISO 6.3, 6.4). Fixed form: columns 1-6 are the sequence number area, whose content "is defined by
the user and may consist of any character in the computer's coded character set" (§6.3.2); column 7 is the indicator
area; the program-text area begins in column 8 and ends at margin R, "a fixed position defined by the implementor"
(§6.3.1). This reader ends it at column 72, the 85 position; the lint refuses program text past column 72 in a portable
test, so no portable test depends on the choice. Free form (2002 on) has neither area. In both, `*>` begins a floating
comment outside a literal.
"""
from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import Callable, Iterable

#: The last column of the fixed-form program-text area this reader uses (the 85 margin R; §6.3.1 makes it the
#: implementor's at 2002 on, which is why the lint refuses text past it in a portable test).
FIXED_TEXT_END = 72
SEQUENCE_AREA = 6

#: A token: its kind, its text (a literal's text includes its prefix and quotes) and the physical line it starts on.
WORD, ALPHANUMERIC, NATIONAL, BOOLEAN, NUMBER, PICTURE, PERIOD, PUNCT, DIRECTIVE = (
    "word", "alphanumeric-literal", "national-literal", "boolean-literal", "numeric-literal", "picture", "period",
    "punct", "directive")


@dataclass(frozen=True)
class Token:
    kind: str
    text: str
    line: int

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
    """The physical lines of a fixed-form `text` holding program text past column FIXED_TEXT_END (§6.3.1)."""
    found = []
    for number, raw in enumerate(text.split("\n"), start=1):
        raw = raw.rstrip("\r")
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


def tokenize(lines: Iterable[SourceLine]) -> list[Token]:
    """The tokens of the program text, in order. A PICTURE character-string after PIC/PICTURE [IS] is one token."""
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
    return tokens


# ── COPY ─────────────────────────────────────────────────────────────────────────────────────────────────────────────

@dataclass(frozen=True)
class CopyStatement:
    """A COPY statement: the library text it names (a text-name, or a literal's content) and its line."""

    name: str
    line: int
    replacing: bool


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
        found.append(CopyStatement(name, tok.line, replacing))
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


def observes_exception(tokens: list[Token]) -> int | None:
    """The first line at which the program observes an exception condition (a TURN directive, an exception-name or an
    exception function), or None. §3.2: run-time violations are judged only where a program can observe them, from
    2002 on, by turning checking on and trapping the condition."""
    for tok in tokens:
        if tok.kind == DIRECTIVE and tok.text == "TURN":
            return tok.line
        if tok.kind == WORD and (tok.upper.startswith("EC-") or tok.upper in EXCEPTION_FUNCTIONS):
            return tok.line
    return None


# ── data descriptions ────────────────────────────────────────────────────────────────────────────────────────────────

#: The classes the portability lint distinguishes. Only ALPHANUMERIC_CLASS and NUMERIC_EDITED_CLASS may be DISPLAYed by
#: a portable test (§3.3).
ALPHANUMERIC_CLASS = "alphanumeric"
NUMERIC_EDITED_CLASS = "numeric-edited"
NUMERIC_DISPLAY_CLASS = "numeric"
NUMERIC_COMPUTATIONAL_CLASS = "numeric (computational usage)"
NATIONAL_CLASS = "national"
BOOLEAN_CLASS = "boolean"
INDEX_CLASS = "index"
POINTER_CLASS = "pointer"
OBJECT_CLASS = "object reference"
SCREEN_CLASS = "screen item"
UNKNOWN_CLASS = "unclassifiable"
PORTABLE_CLASSES = frozenset({ALPHANUMERIC_CLASS, NUMERIC_EDITED_CLASS})

#: The computational usages (ISO 13.18.60 USAGE clause, with the COMP-n spellings implementations add). Each is a
#: reserved word, so no data-name can match it.
_COMPUTATIONAL = re.compile(r"^(COMP(UTATIONAL)?(-[1-5X])?|BINARY(-(CHAR|SHORT|LONG|DOUBLE))?|PACKED-DECIMAL"
                            r"|FLOAT-(SHORT|LONG|EXTENDED|BINARY-(32|64|128)|DECIMAL-(16|34)))$")
_POINTER_USAGES = frozenset({"POINTER", "PROGRAM-POINTER", "FUNCTION-POINTER"})
_SECTIONS = frozenset({"FILE", "WORKING-STORAGE", "LOCAL-STORAGE", "LINKAGE", "REPORT", "SCREEN", "COMMUNICATION"})


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
            return None
    return None


@dataclass
class _Entry:
    level: int
    name: str | None
    picture: str | None = None
    usage: str | None = None  # the class a USAGE clause decides, if any
    group_usage: str | None = None
    constant: str | None = None  # the class of a constant entry's literal
    renames: bool = False
    screen: bool = False
    indexes: list[str] = field(default_factory=list)


def _entries(tokens: list[Token]) -> list[_Entry]:
    """The data description entries of the DATA DIVISION in `tokens` (COPY already expanded)."""
    start = end = None
    for i, tok in enumerate(tokens):
        if tok.kind == WORD and i + 1 < len(tokens) and tokens[i + 1].upper == "DIVISION":
            if tok.upper == "DATA":
                start = i + 2
            elif tok.upper == "PROCEDURE" and start is not None:
                end = i
                break
    if start is None:
        return []
    body = tokens[start:end]
    entries: list[_Entry] = []
    section = ""
    sentence: list[Token] = []
    for tok in body + [Token(PERIOD, ".", 0)]:
        if tok.kind != PERIOD:
            sentence.append(tok)
            continue
        words = [t.upper for t in sentence]
        if len(words) == 2 and words[1] == "SECTION" and words[0] in _SECTIONS:
            section = words[0]
        elif sentence and sentence[0].kind == NUMBER and sentence[0].text.isdigit():
            entries.append(_entry(sentence, section == "SCREEN"))
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
    usage = _usage_class([w for w in words if w not in ("USAGE", "IS")])
    e.usage = usage
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


def data_classes(tokens: list[Token]) -> dict[str, set[str]]:
    """`{data-name -> the classes of the entries that declare it}` for the DATA DIVISION in `tokens`.

    A group item is alphanumeric unless its GROUP-USAGE (ISO 13.18.29) or USAGE is national or bit; an elementary
    item's class comes from its PICTURE and USAGE; an index-name is an index. A name declared twice keeps both
    classes, so an unqualified reference the lint cannot tie to one entry is judged by the worst of them."""
    entries = _entries(tokens)
    classes: dict[str, set[str]] = {}

    def add(name: str | None, cls: str) -> None:
        if name:
            classes.setdefault(name, set()).add(cls)

    for i, e in enumerate(entries):
        for idx in e.indexes:
            add(idx, INDEX_CLASS)
        if e.level == 88:
            continue
        if e.screen:
            add(e.name, SCREEN_CLASS)
            continue
        if e.renames or e.level in (66, 78):
            add(e.name, UNKNOWN_CLASS)
            continue
        if e.constant is not None:
            add(e.name, e.constant)
            continue
        following = next((n for n in entries[i + 1:] if n.level not in (66, 77, 78, 88)), None)
        is_group = (e.level not in (77,) and e.picture is None and following is not None
                    and 1 <= following.level <= 49 and following.level > e.level)
        if is_group:
            add(e.name, e.group_usage or (e.usage if e.usage in (NATIONAL_CLASS, BOOLEAN_CLASS) else None)
                or ALPHANUMERIC_CLASS)
            continue
        if e.picture is not None:
            pic = picture_class(e.picture)
            if e.usage == NATIONAL_CLASS or pic == NATIONAL_CLASS:
                add(e.name, NATIONAL_CLASS)
            elif pic in (NUMERIC_DISPLAY_CLASS,) and e.usage == NUMERIC_COMPUTATIONAL_CLASS:
                add(e.name, NUMERIC_COMPUTATIONAL_CLASS)
            elif e.usage in (BOOLEAN_CLASS,):
                add(e.name, BOOLEAN_CLASS)
            else:
                add(e.name, pic)
            continue
        add(e.name, e.usage or UNKNOWN_CLASS)
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
    """A DISPLAY operand (or phrase) a portable test may not write, with why."""

    line: int
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


def display_issues(tokens: list[Token], classes: dict[str, set[str]]) -> list[DisplayIssue]:
    """Every DISPLAY operand of the PROCEDURE DIVISION in `tokens` the portability lint refuses (§3.3).

    ISO 14.9.11.4 GR 1 makes "any conversion of data required between literal-1 or the data item referenced by
    identifier-1 and the device" the implementor's, so a portable test DISPLAYs only alphanumeric literals,
    alphanumeric items and numeric-edited items. A reference-modified numeric or numeric-edited item of usage display
    is alphanumeric (§8.4.3.3.4 GR 6 c), so it may be DISPLAYed; one of computational usage shows its storage bytes
    and may not. UPON names an implementor-defined device (14.9.11.3 SR 2), whose output no expected stdout can hold."""
    issues: list[DisplayIssue] = []
    start = next((i for i, t in enumerate(tokens)
                  if t.upper == "PROCEDURE" and i + 1 < len(tokens) and tokens[i + 1].upper == "DIVISION"), None)
    if start is None:
        return issues
    i = start
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
                issues.append(DisplayIssue(t.line, t.text, f"a {t.kind}"))
                j += 1
                continue
            if t.kind != WORD:
                issues.append(DisplayIssue(t.line, t.text, "not a literal or an identifier (an expression?)"))
                break
            w = t.upper
            if w == "UPON":
                issues.append(DisplayIssue(t.line, "UPON", "an implementor-defined device, not the standard display "
                                                           "device"))
                break
            if w in _DISPLAY_STOP or w.startswith("END-"):
                break
            if w == "ALL" and j + 1 < len(tokens):
                lit = tokens[j + 1]
                if lit.kind != ALPHANUMERIC:
                    issues.append(DisplayIssue(lit.line, "ALL " + lit.text, "the ALL form of a " + lit.kind))
                j += 2
                continue
            if w in _FIGURATIVES:
                j += 1
                continue
            if w in ("NULL", "NULLS"):
                issues.append(DisplayIssue(t.line, t.text, "a pointer figurative constant"))
                j += 1
                continue
            if w == "FUNCTION":
                name = tokens[j + 1].text if j + 1 < len(tokens) else ""
                issues.append(DisplayIssue(t.line, f"FUNCTION {name}", "a function result"))
                j, _ = _skip_parens(tokens, j + 2)
                continue
            # An identifier: a data-name with its qualifiers, subscripts and reference modification.
            k = j + 1
            while k + 1 < len(tokens) and tokens[k].upper in ("OF", "IN") and tokens[k + 1].kind == WORD:
                k += 2
            k, refmod = _skip_parens(tokens, k)
            found = classes.get(w) or {UNKNOWN_CLASS}
            if refmod:
                found = {ALPHANUMERIC_CLASS if c in (NUMERIC_DISPLAY_CLASS, NUMERIC_EDITED_CLASS) else c for c in found}
            bad = sorted(c for c in found if c not in PORTABLE_CLASSES)
            if bad:
                issues.append(DisplayIssue(t.line, t.text, "a data item of class " + " / ".join(bad)))
            j = k
        i = j
    return issues
