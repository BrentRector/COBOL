#!/usr/bin/env python3
"""The P14 traceability-inventory schema, loaded from data — imported by every Python consumer.

⛔ WHERE THE RULES LIVE. The verdict vocabulary, each verdict's `resolves` flag and each verdict's required
evidence fields are DATA, in `tests/version-matrix/inventory-schema.json`. Nothing here — and nothing in
`build_inventory.py`, `record_verdicts.py` or the C# gate — carries its own copy of that list. Adding a verdict is
an edit to the JSON and nothing else. This is CLAUDE.md rule 5 applied to the thing most likely to drift: the DA
wave's whole lesson (`feedback_one_rule_one_place`) was that every symptom traced back to one rule written down in
more than one place.

⚖ WHAT IS AND IS NOT DUPLICATED. Two engines evaluate these rules at two different times: this module at RECORD
time, and `SpecTraceabilityInventoryDriftTests` at BATTERY time. Both read the same JSON, so neither hard-codes a
verdict name, a flag or a field — what exists twice is the ten-line evaluator, not the rule. And the divergence
that would matter is itself gated: the C# side asserts every row's stored `state` equals the state the schema
implies, so a Python evaluator that drifted from the C# one turns the battery red instead of quietly inflating the
burn-down.

DIVISION OF LABOUR between the two, deliberately with NO overlap:
  · SHAPE      — verdict is in the vocabulary, required fields present, editions legal, rule-id known, and — for a
                 kind that declares evidence rules (`kinds`) — the computed register anchor is present and every
                 fragment on an `anchored-files` path is a legal anchor of that file's space. Decidable from the
                 record, the schema and THE REGISTER THE SCHEMA NAMES (`kinds.DOC.anchor-template` points at
                 docs/CONFORMANCE.md §7, and since 2026-09-02 `anchor_obliged` asks whether that document
                 actually carries the row — see below). It still does not go looking in `src/`.
                 Checked HERE, at record time, so a malformed batch never reaches the inventory.
  · REFERENCE  — the code-location symbol still exists, the test-ref names a test that is really on disk.
                 Checked ONLY by the C# battery gate, because it must keep holding as the tree changes underneath
                 an already-recorded row. Re-checking it here would be the same rule in two places.
"""
from __future__ import annotations

import json
import pathlib
import re
from typing import Any, NamedTuple

REPO = pathlib.Path(__file__).resolve().parents[2]
SCHEMA_PATH = REPO / "tests" / "version-matrix" / "inventory-schema.json"
CATALOG_PATH = REPO / "docs" / "rearchitecture" / "spec-rule-catalog.json"
INVENTORY_PATH = REPO / "tests" / "version-matrix" / "traceability-inventory.json"
#: The IMPLEMENTOR-DEFINED ELEMENT REGISTER — docs/CONFORMANCE.md §7, whose rows are keyed by the inventory
#: rule-id `DOC-A.1-<n>` because `kinds.DOC.anchor-template` computes that key as the row's own anchor.
REGISTER_PATH = REPO / "docs" / "CONFORMANCE.md"
REGISTER_HEADING = "## 7. Annex A.1"
#: The label in the §7 table's own header row, so the reader can tell a header from a determination.
REGISTER_HEADER_CELL = "A.1 item"

#: The adjudicated fields — the ones a re-run of build_inventory.py must carry forward, and the ones a verdict
#: record may set. Everything else on a row is regenerated from the catalog and is not the reviewer's to write.
#: ⚙ `derivation` is the §1.1 alternative to `test-ref` (owner decision kb/Work PB386, 2026-09-03) — see
#: `Derivation` below.
ADJUDICATED = ("verdict", "code-location", "test-ref", "derivation", "editions", "notes")

#: The adjudicated fields that are SETS OF WITNESSES rather than one statement (kb/Work PB959). A verdict record
#: MERGES into these — the union of what the row carried and what the record names — because a witness is
#: evidence about the rule, and a batch that re-types three of five witnesses has not refuted the other two.
#: A witness leaves a row only by an explicit, reasoned RETIREMENT, which is written into the row's `notes` as
#: `RETIRED_WITNESS_MARK <ref>` so the loss is visible to `audit_witness_loss.py --check` long after the batch
#: file that asked for it is gone. Every other adjudicated field is a single statement the record restates.
#:
#: ⚖ ONE ASYMMETRY, MEASURED FROM THE INVENTORY'S OWN HISTORY: a RE-ADJUDICATION (the record changes the row's
#: verdict) RE-SITES `code-location` instead of merging it. A PARTIAL or NOT-IMPLEMENTED row's code-location names
#: where the DEFECT is; the CONFORMS record that closes it names where the rule is now IMPLEMENTED, and a union
#: would present the defect site as implementation evidence. 1,052 of the 1,224 still-resolving code-locations the
#: history walk found removed were exactly that shape. A `test-ref` is never re-sited: a test that exists and
#: exercises the rule is an observation of it whatever the verdict says (`CODE_LOCATION_RESITES_ON_READJUDICATION`).
WITNESS_FIELDS = ("code-location", "test-ref")
CODE_LOCATION_RESITES_ON_READJUDICATION = True
RETIRED_WITNESS_MARK = "retired-witness:"
_RETIRED_RE = re.compile(re.escape(RETIRED_WITNESS_MARK) + r"\s*(\S+)")


def retired_witnesses(notes: str) -> set[str]:
    """The witnesses a row's `notes` records as deliberately RETIRED — see `WITNESS_FIELDS`."""
    return {m.rstrip(".,;)") for m in _RETIRED_RE.findall(notes or "")}


def resites(field: str, before_verdict: str, after_verdict: str) -> bool:
    """Does a write REPLACE this witness field rather than merge it? Only a re-adjudication's code-location."""
    return (CODE_LOCATION_RESITES_ON_READJUDICATION and field == "code-location"
            and bool(before_verdict) and before_verdict != after_verdict)


class WitnessLoss(NamedTuple):
    """One witness present on a row BEFORE a write and absent AFTER it."""
    rule_id: str
    field: str
    ref: str
    retired: bool   # the AFTER row's notes record it as a deliberate retirement
    resited: bool   # a re-adjudication's code-location, which the write rule REPLACES (see `resites`)

    @property
    def excused(self) -> bool:
        return self.retired or self.resited


def witness_shape_error(field: str, ref: str, schema: "Schema") -> str | None:
    """Why one witness reference is malformed under the schema's SHAPE rules, or None — asked by the writer
    (`record_verdicts.validate`) of every reference a record names, and by the history replay
    (`audit_witness_loss.owed_back`) of every reference it would owe back, so a witness removed BECAUSE it broke a
    shape rule (a bare `docs/CONFORMANCE.md`, the `#7` that PB-era batches carried) is never restored."""
    if field == "code-location":
        if not schema.code_location_re.match(ref):
            return f"code-location '{ref}' is not '<repo-relative-path>[#Symbol]'"
        # ⛔ A LISTED FILE'S FRAGMENT IS AN ANCHOR OR IT IS A FAILURE — and a BARE citation of one is the
        # weakest form of the same defect, resolving on File.Exists alone. Five live rows carried
        # `docs/CONFORMANCE.md#7`, which the battery gate's word search satisfies against the digit 7
        # anywhere in a 790-line document, and three more cited the bare path.
        file, _, fragment = ref.partition("#")
        if (rx := schema.anchored_files.get(file)) is not None and not rx.match(fragment):
            return (f"code-location '{ref}' — '{file}' is an anchored file, so its fragment must match "
                    f"{rx.pattern} ({'no fragment at all' if not fragment else f'got {fragment!r}'})")
        return None
    scheme = ref.split(":", 1)[0]
    if scheme not in schema.test_ref_forms:
        return f"test-ref '{ref}' — unknown form '{scheme}', expected one of {sorted(schema.test_ref_forms)}"
    if ":" not in ref or not ref.split(":", 1)[1].strip():
        return f"test-ref '{ref}' has an empty body after '{scheme}:'"
    return None


def witness_losses(before: dict[str, Any], after: dict[str, Any], schema: "Schema") -> list[WitnessLoss]:
    """Every witness `before` carried that `after` does not — THE one definition of a narrowing (kb/Work PB959).

    Read by the writer (`record_verdicts.py`, at record time) and by the auditor (`audit_witness_loss.py`, over a
    diff or the whole git history), so the two cannot come to disagree about what counts as losing evidence.
    """
    retired = retired_witnesses(after.get("notes", ""))
    out: list[WitnessLoss] = []
    for field in WITNESS_FIELDS:
        sep = schema.code_location_sep if field == "code-location" else schema.test_ref_sep
        kept = set(schema.split(after.get(field, "") or "", sep))
        resited = resites(field, before.get("verdict", ""), after.get("verdict", ""))
        out += [WitnessLoss(before.get("rule-id", ""), field, ref, ref in retired, resited)
                for ref in schema.split(before.get(field, "") or "", sep) if ref not in kept]
    return out

#: A parenthesised or semicolon-introduced clause citation inside a rule's own text — the form Annex A.1 uses to
#: point an implementor-defined documentation obligation at the clause that creates it:
#: "(13.18.40, PICTURE clause, General rule 15; 13.18.60, USAGE clause, General rule 3)".
XREF_CITATION = re.compile(r"[(;]\s*(\d+(?:\.\d+)*)\s*,")

#: ⛔ THE TRANSCRIPTION SPELLS A COBOL OPERAND NAME'S HYPHEN TWO WAYS AND A PREDICATE CANNOT SEE THE DIFFERENCE.
#: 29 catalog rules carry U+2011 NON-BREAKING HYPHEN where every other rule carries ASCII '-': SR-13.18.14.3-12
#: reads "Identifier‑1 shall be described in the file, working‑storage, …". A selector arm written
#: `\bidentifier-1\b` — the obvious spelling, and the one four of these six selectors need — silently matches
#: NOTHING there, and a selector that silently under-selects is the failure mode feedback_measure_the_selectors_-
#: complement names: the drift test stays green and the rows stay unstamped. Folding it HERE, in the one place
#: both engines read the text through, is the fix that makes the next selector immune; putting the alternation in
#: each pattern instead would be the same rule written down N times, which is what CLAUDE.md rule 5 forbids.
#: Only the two unambiguous HYPHEN code points are folded — U+2013 EN DASH is left alone, because the standard
#: uses it both as the MINUS-sign glyph and as a clause-range separator ("13.16–13.18").
HYPHENS = {0x2010: "-", 0x2011: "-"}

#: ⛔ A NOTE IS NOT PART OF THE RULE, SO A PREDICATE NEVER SEES ONE. The standard's Introduction states it in its
#: verbal-forms paragraph (unnumbered, so `cite.py` cannot address it — `--find` reports no numbered clause):
#: "Information marked as 'NOTE' is intended to assist the understanding or use of the document." A NOTE creates
#: no requirement, so it can neither SELECT a rule into a determination nor EXCLUDE one from it. The catalog
#: keeps the NOTE inside the rule's text, and before kb/Work R43 (2026-09-24) the selectors read it: §11.9.5.2
#: GR2 — "If the STANDARD-BINARY phrase is specified, the techniques used … shall be as described for
#: standard-binary arithmetic" — sat outside `standard-binary-only` because its NOTE 1 encourages implementors to
#: support STANDARD-DECIMAL, and that informative sentence tripped the mixed-rule exclusion (kb/Work PB1517).
#: MEASURED when the cut landed: across every selector and all 233 catalog rules that carry a NOTE, cutting the
#: text at the first NOTE changed exactly that one row. The cut runs to the END of the text because the catalog
#: joins a rule's paragraphs with spaces and a NOTE is printed after the requirement it annotates.
NOTE_MARK = re.compile(r"\bNOTE(?:\s+\d+)?\s+(?=[A-Z(])")


def section_matches(prefix: str, section: str) -> bool:
    """Does `section` fall under `prefix`, COMPONENT-WISE?

    ⛔ THIS IS THE ONE RULE THAT MUST NOT BE A RAW `startswith`, AND IT IS WHY THIS FUNCTION EXISTS AT ALL.
    A clause number is a dotted PATH, not a string: §13.18.30 is not inside §13.18.3, and §13.18.40 (PICTURE) is
    not inside §13.18.4 (BACKGROUND-COLOR). A raw prefix test says otherwise. Measured on the A.4.2 screen
    selector, whose clause arm names §13.18.3, §13.18.4, §13.18.6, §13.18.7 and §13.18.9 among others: the raw
    test selects 538 rules where the component-wise test selects 149 — 389 extra, 33 of them already adjudicated
    (32 CONFORMS, 1 DOCUMENTED-NON-SUPPORT), i.e. a silent flip of thirty-two verified rows to non-support.

    Trailing dots in the DATA are accepted and ignored: "13.18.3" and "13.18.3." mean the same path here, so the
    defensive spelling a previous author reached for cannot become load-bearing again.
    """
    p = [c for c in prefix.strip().strip(".").split(".") if c]
    s = [c for c in section.strip().strip(".").split(".") if c]
    return len(p) <= len(s) and s[: len(p)] == p


class Section7Row(NamedTuple):
    """One data row of the docs/CONFORMANCE.md §7 register, by cell."""

    key: str            #: the inventory rule-id the row discharges — `DOC-A.1-<n>`
    element: str        #: the A.1 element the row claims to be about
    determination: str  #: WHAT WE DO — the determination itself
    pinned: str         #: the spec-derived test-ref(s) that pin it, or '—'


class Refusal(NamedTuple):
    """One reason a claimed derivation does not stand — a STABLE CODE plus the sentence a reviewer reads.

    The code exists so the cross-language parity fixture can compare what the two engines refused, not merely
    THAT they refused: two evaluators that reject one row for two different reasons look identical under a
    boolean, and that is precisely the shape kb/Work PB315's disagreement hid behind.
    """

    code: str
    message: str


class DerivationRow(NamedTuple):
    """One data row of the docs/CONFORMANCE.md §8 derivation register, by cell."""

    key: str        #: the derivation anchor fragment — `DRV-<rule-id>`
    arm: str        #: which of the three §1.1 grounds this determination stands on
    names: str      #: the arm's OBJECT — `A.2 item <n>` / the closed set / the indistinguishable rule-id
    argument: str   #: the derivation itself, written for a reader
    signature: str  #: the owner's signature, one of the keys of `derivation.signatures` in the schema


def register_cells(line: str) -> list[str]:
    """A markdown row's content cells, split on UNESCAPED pipes.

    ⚠ Splitting on a bare '|' is wrong here and was wrong once: the item-82 determination writes an absolute
    value as `\\|v\\|`, so a naive split reports that row with two extra cells and mis-places anything appended
    to it.
    """
    parts = [c.replace("\\|", "|").strip() for c in re.split(r"(?<!\\)\|", line.rstrip("\n"))]
    return parts[1:-1]


def register_section(text: str, heading: str, header_cell: str) -> list[list[str]]:
    """Every data row of ONE `docs/CONFORMANCE.md` register table, as raw cell lists, in document order.

    ⛔ THE ONE PARSER, and it lives HERE rather than in `audit_annex_a1.py` where it was written, because four
    readers now need it and a markdown table read four ways is `feedback_one_rule_one_place` waiting to happen:
    the register audit (which CHECKS the rows), `Schema.anchor_obliged` (which asks whether an item has a
    determination at all), `DerivedSelector` (whose `determination-prefix` arm reads what the determination SAYS)
    and `Derivation` (which reads §8). The C# twin is `tests/_shared/ConformanceRegister.cs`; that pair is the
    same two-engines-one-artifact shape as the selector evaluator, and `EveryRowState_IsDerived_NotAsserted`
    catches them drifting.

    ⛔ IT STOPS AT THE NEXT `## ` HEADING, and that is not tidiness. Until §8 existed §7 was the LAST section, so
    reading to end-of-file was indistinguishable from reading the section — and the moment a second register was
    appended, every §8 row would have been served to `register_determinations` as an A.1 determination keyed by
    something that is not an A.1 item. Bounding it is what makes the NEXT section free.
    """
    start = text.find(heading)
    if start < 0:
        # A body that does not carry this section has no rows of it — the same answer the C# reader gives, and
        # the reason it is not an error is that both are driven against FABRICATED bodies carrying one section
        # at a time. A section genuinely lost from the live document is caught by the POPULATION assertions its
        # gates make (`filed.Count > 0`, `Derivation is not null`), never by a parser raising here.
        return []
    body = text[start + len(heading):]
    if (nxt := body.find("\n## ")) >= 0:
        body = body[:nxt]
    rows: list[list[str]] = []
    for line in body.splitlines():
        if not line.startswith("| "):
            continue
        cells = register_cells(line)
        if len(cells) < 2 or cells[0] == header_cell or set(cells[0]) <= set("-: "):
            continue
        rows.append(cells)
    return rows


def _cell(cells: list[str], i: int) -> str:
    return cells[i] if len(cells) > i else ""


def section7_rows(text: str) -> list[Section7Row]:
    """Every data row of the §7 Annex A.1 register, in document order."""
    return [Section7Row(c[0], c[1], _cell(c, 2), _cell(c, 3))
            for c in register_section(text, REGISTER_HEADING, REGISTER_HEADER_CELL)]


def plain(cell: str) -> str:
    """A determination cell with its leading markdown emphasis removed.

    A determination is written for a HUMAN first — `**Not provided.** Each usage holds …` — so a predicate that
    reads the cell must not be reading the bold markers. Stripping them in the one place both engines call is
    what stops the next author's `*Not provided.*` from silently un-selecting a row.
    """
    return cell.lstrip("*_ \t")


_REGISTER_CACHE: dict[str, str] | None = None


def register_determinations(text: str | None = None) -> dict[str, str]:
    """`{row key -> determination cell}` for the §7 register — cached when read from disk.

    An item with SEVERAL determinations (A.1-56 and A.1-92 legitimately have two rows each) keeps the FIRST:
    the question every caller asks is "does this element have a determination, and what does it say", and the
    first row is the one the item's own obligation is discharged by. `audit_annex_a1.py` is the place that cares
    about the second row, and it reads `section7_rows` directly.
    """
    global _REGISTER_CACHE
    if text is not None:
        return {r.key: r.determination for r in reversed(section7_rows(text))}
    if _REGISTER_CACHE is None:
        _REGISTER_CACHE = register_determinations(REGISTER_PATH.read_text(encoding="utf-8"))
    return _REGISTER_CACHE


class DerivedSelector:
    """One `derived-verdicts` entry — a verdict that follows MECHANICALLY from one owner determination.

    ⛔ THE PREDICATE IS DATA AND BOTH ENGINES READ IT. This class and
    `tests/Cobol.Net.Tests.Unit/DerivedVerdictDriftTests.Select` are the two evaluators; neither carries a copy of
    a selector. Until 2026-09-02 the schema's own $comment claimed that already — and it was false: no Python
    consumer of `derived-verdicts` existed, so the PB198 batch was produced by hand and the "two readers" that
    justify writing the predicate as data were one reader and an aspiration.

    THE SHAPE IS `arms`, and the arms are a DISJUNCTION of CONJUNCTIONS. Within an arm every field that is
    present must hold; a rule is selected when ANY arm holds and NO global `excludes-patterns` entry matches its
    text. Four modules of the 2026-09-02 landing needed four different scoping mechanisms and the flat
    requires-sections/requires-pattern pair could express only two of them:
      · `sections`       the rule's OWN clause (§8.8.1.4 is titled for the declined mode — PB198's lesson)
      · `pattern`        the rule's TEXT
      · `sections` + `pattern` together — an AND-gate. `file-name-1` is STATEMENT-LOCAL: in §13.4.5.4 it is the
        file description entry's own subject, so A.4.13's text arm ungated selects 176 rules instead of 12. The
        same gate is what reaches a rule scoped by OPERAND NAME — §12.3.7.3's data-name-1/-2 are the CURSOR and
        CRT STATUS operands and nothing else, and neither a clause arm nor a bare text arm can see them.
      · `xref-sections`  a clause citation inside the rule's text — how an Annex A.1 documentation obligation
        names the clause that creates it. Derived from the clause numbers, so adding a module clause extends the
        A.1 arm for free instead of silently not extending it.
      · `excludes-pattern` a TEXT exclusion scoped to ONE arm — the per-arm twin of the entry-level
        `excludes-patterns`, which drop a rule whatever arm took it. It exists because A.4.10's item-1 arm must
        not stamp the SUPPORTED item 2 (`interface-name`, kb/Work PB285) while its item-3 arm must reach
        GR-9.3.6-L5.2, whose exact-match criteria name "an interface-name" (kb/Work R43 / PB1519): one exclusion
        was right for one arm and wrong for the other, and a global list cannot say that. Negative, so it is not
        in POSITIVE.
      · `kinds` / `excludes-kinds` the rule's KIND, per arm. A general-format diagram (kind FMT) is evidence
        about a CLAUSE, never about one of its formats, so the TEXT axis must not select one: without this,
        FMT-14.9.1.2 and FMT-14.9.11.2 — the ACCEPT and DISPLAY formats, both PARTIAL, both carrying the
        compiler's most-used statement — flip to non-support because their optional format names screen-name-1.
        The CLAUSE axis still takes FMT-13.17.2, whose every format IS screen.
      · `requirement`   the A.1 REQUIREMENT CLASS the standard states for a DOC rule in its own text ("This item
        is optional." / "This item is required." / "conditionally required"), carried on the catalog row by
        `extract_rule_catalog.py`: 164 required · 30 optional · 27 conditionally required · 1 unclassified. It is
        the axis kb/Work PB280 Q1 turns on, and it exists because the licence for that determination is A.1's
        own preamble, which is written PER CLASS.
      · `determination-prefix`  what docs/CONFORMANCE.md §7 SAYS about this element — the register row keyed by
        the rule's own id (which is exactly the anchor `kinds.DOC.anchor-template` computes), matched on its
        leading words with markdown emphasis stripped. ⛔ This is the first arm field that reads an artifact
        outside the catalog, and that is the point: a determination is the DOC row's implementation, so a
        selector about determinations has nowhere else to look. An item with NO §7 row is not selected — nothing
        has been determined about it yet, and a MISSING determination is not a NEGATIVE one
        (`feedback_verdict_evidence_invariant`).
      · `lead-in`       the sentence that INTRODUCES the printed list a rule is an item of — how the standard
        scopes a list's items when the items themselves carry no vocabulary. §9.3.6's exact-match criteria
        (GR-9.3.6-L5.1 … L5.3) say only "the same class and category", "the same … PICTURE … clauses"; what makes
        them exact-match criteria is the sentence ending GR-9.3.6-L3.7, "the following criteria are considered an
        exact match:". A list's lead-in is the catalog rule immediately BEFORE its first item (the catalog keeps
        the standard's order, and a list is its `kind`-`section`-`sublist` triple), so a nested list that
        interrupts an outer one does not change the outer list's lead-in. A plain rule (sublist 1) has none.
        Added for kb/Work R43 / PB1519.

    ⛔ EVERY TEXT AXIS READS THE NORMATIVE TEXT — the rule with its NOTEs cut off (`NOTE_MARK`). See there.
    """

    #: The arm fields that NARROW the catalog. An arm carrying none of them selects all 4,311 rules.
    POSITIVE = ("sections", "pattern", "xref-sections", "kinds", "requirement", "determination-prefix", "lead-in")

    def __init__(self, name: str, raw: dict[str, Any], register: dict[str, str] | None = None) -> None:
        self.name = name
        self._raw = raw
        self.verdict: str = raw["verdict"]
        self.decision: str = raw.get("decision", "")
        self.excludes = [re.compile(p, re.IGNORECASE) for p in raw.get("excludes-patterns", [])]
        #: The §7 register this selector reads for its `determination-prefix` arms. Injectable so a self-test
        #: drives the predicate against a FABRICATED register instead of the live one — the two new axes cannot
        #: be falsified against today's document, where all four "optional + Not provided" cells agree.
        self._register = register
        self.arms: list[dict[str, Any]] = []
        for i, arm in enumerate(raw["arms"]):
            keys = {k for k in arm if not k.startswith("$")}
            if not keys & set(self.POSITIVE):
                # An arm with only NEGATIVE fields selects the whole catalog. Refusing it here is cheap; the
                # alternative is a 4,311-row batch that validates perfectly and is entirely wrong.
                raise SystemExit(f"derived-verdicts.{name} arm[{i}]: no positive field — it would select every rule")
            self.arms.append({
                "sections": list(arm.get("sections", [])),
                "xref-sections": list(arm.get("xref-sections", [])),
                "pattern": re.compile(arm["pattern"], re.IGNORECASE) if "pattern" in arm else None,
                "kinds": set(arm.get("kinds", [])),
                "excludes-kinds": set(arm.get("excludes-kinds", [])),
                "requirement": set(arm.get("requirement", [])),
                "determination-prefix": [p.casefold() for p in arm.get("determination-prefix", [])],
                "lead-in": re.compile(arm["lead-in"], re.IGNORECASE) if "lead-in" in arm else None,
                "excludes-pattern": (re.compile(arm["excludes-pattern"], re.IGNORECASE)
                                     if "excludes-pattern" in arm else None),
            })

    @property
    def register(self) -> dict[str, str]:
        return register_determinations() if self._register is None else self._register

    @staticmethod
    def text_of(rule: dict[str, Any]) -> str:
        """The rule text a predicate is matched against — hyphen-normalised (HYPHENS) and NOTE-free (NOTE_MARK)."""
        text = rule.get("text", "").translate(HYPHENS)
        note = NOTE_MARK.search(text)
        return text[: note.start()] if note else text

    @classmethod
    def lead_ins(cls, rules: list[dict[str, Any]]) -> dict[str, str]:
        """`{rule-id -> the normative text of the sentence introducing the list the rule is an item of}`.

        A list is its `kind`-`section`-`sublist` triple and its lead-in is the rule immediately before its FIRST
        item, so an inner list that interrupts an outer one leaves the outer list's lead-in alone. A plain rule
        (sublist 1) is an item of no list and gets ''. See the class docstring's `lead-in` entry.
        """
        out: dict[str, str] = {}
        first: dict[tuple[str, str, int], str] = {}
        previous = ""
        for r in rules:
            sub = int(r.get("sublist", 1) or 1)
            if sub > 1:
                key = (r.get("kind", ""), r.get("section", ""), sub)
                out[r["id"]] = first.setdefault(key, previous)
            else:
                out[r["id"]] = ""
            previous = cls.text_of(r)
        return out

    def _arm_selects(self, arm: dict[str, Any], rule: dict[str, Any], lead_in: str = "") -> bool:
        kind = rule.get("kind", "")
        if arm["kinds"] and kind not in arm["kinds"]:
            return False
        if kind in arm["excludes-kinds"]:
            return False
        if arm["requirement"] and rule.get("requirement", "") not in arm["requirement"]:
            return False
        if arm["determination-prefix"]:
            said = plain(self.register.get(rule.get("id", ""), "")).casefold()
            # An item with no §7 row yields '', which starts with no prefix — so an undetermined element is
            # never selected, whatever its requirement class.
            if not said or not any(said.startswith(p) for p in arm["determination-prefix"]):
                return False
        if arm["sections"] and not any(section_matches(p, rule.get("section", "")) for p in arm["sections"]):
            return False
        text = self.text_of(rule)
        if arm["xref-sections"]:
            cited = XREF_CITATION.findall(text)
            if not any(section_matches(p, c) for p in arm["xref-sections"] for c in cited):
                return False
        if arm["pattern"] is not None and not arm["pattern"].search(text):
            return False
        if arm["excludes-pattern"] is not None and arm["excludes-pattern"].search(text):
            return False
        if arm["lead-in"] is not None and not (lead_in and arm["lead-in"].search(lead_in)):
            return False
        return True

    def select(self, rules: list[dict[str, Any]]) -> list[str]:
        lead = self.lead_ins(rules) if any(a["lead-in"] is not None for a in self.arms) else {}
        out = []
        for r in rules:
            if any(x.search(self.text_of(r)) for x in self.excludes):
                continue
            if any(self._arm_selects(a, r, lead.get(r["id"], "")) for a in self.arms):
                out.append(r["id"])
        return out


class Derivation:
    """`derivation` in the schema — §1.1's owner-signed ALTERNATIVE to a spec-derived test.

    ⛔ THE OWNER OPENED THIS DOOR AND SET ITS BOUNDS (kb/Work PB386, 2026-09-03); the class is those bounds,
    encoded. A rule that carries NO OBSERVABLE OBLIGATION cannot meet `DESIGN-spec-conformance-review.md` §1(c) —
    not because nobody has written the golden but because any golden would pin an IMPLEMENTATION CHOICE as though
    it were the standard's answer, which is precisely what `test-ref.spec-derived` exists to prevent. Such a row
    may instead close on a DETERMINATION recorded in `docs/CONFORMANCE.md` §8, keyed by the row's own rule-id,
    naming one of three grounds and signed by the owner.

    ⛔ CHECKABLE, NOT A FREE-TEXT ESCAPE HATCH — the note's own second consequence, and the reason this is a
    predicate rather than a convention. The `undefined-A.2` arm is MECHANICAL: Annex A.2 is the standard's own
    list of what it declares undefined, each item ends in the standard's own citation of the rule it is about,
    and `scripts/spec/extract_annex_a2.py` resolves those citations into rule-ids. The other two arms are
    reviewed ARGUMENTS: their shape is checked here (the closed set must be STATED; the indistinguishable rule
    must EXIST and not be this row), and the owner's signature is the record that the argument was read.

    ⚖ IT REPLACES §1(c) AND NOTHING ELSE. `Schema.state_for` charges a kind's anchor and observability costs
    BEFORE it consults a derivation, so kb/Work PB280 Q2 — "no" for a DOC row with nothing in the compiler to
    observe — is untouched by this and cannot be routed around by it.
    """

    def __init__(self, raw: dict[str, Any], register: dict[str, DerivationRow] | None = None,
                 undefined: dict[int, set[str]] | None = None) -> None:
        self._raw = raw
        self.field: str = raw["field"]
        self.anchor_template: str = raw["anchor-template"]
        self.heading: str = raw["register-heading"]
        self.header_cell: str = raw.get("register-header-cell", "Rule")
        #: Every signature the owner has given — one dated decision each, mapped to where it is recorded.
        self.signatures: dict[str, str] = {k: v for k, v in raw["signatures"].items() if not k.startswith("$")}
        self.undefined_list: str = raw["undefined-list"]
        self.arms: dict[str, dict[str, Any]] = {
            name: {"names": re.compile(arm["names-pattern"]), "check": arm["check"]}
            for name, arm in raw["arms"].items() if not name.startswith("$")
        }
        #: Injectable so a self-test drives the predicate against a FABRICATED register and a fabricated A.2
        #: list — the arms cannot be falsified against today's document, where every row is correct.
        self._register = register
        self._undefined = undefined

    def anchor_for(self, row: dict[str, Any]) -> str:
        """The derivation anchor this row would carry — COMPUTED from its own rule-id, never chosen."""
        return self.anchor_template.replace("{rule-id}", row.get("rule-id", ""))

    def key_for(self, row: dict[str, Any]) -> str:
        """The §8 register key for this row — the anchor's fragment, so the two cannot disagree."""
        return self.anchor_for(row).partition("#")[2]

    @property
    def register(self) -> dict[str, DerivationRow]:
        if self._register is None:
            self._register = register_derivations(self.heading, header_cell=self.header_cell)
        return self._register

    @property
    def undefined(self) -> dict[int, set[str]]:
        """`{A.2 item -> the rule-ids it covers}` — read ONCE, from the generated artifact.

        ⛔ NOT a parse of `specs/ISO_COBOL.md` here. `EveryRowState_IsDerived_NotAsserted` recomputes the state
        of all 4,311 rows on every build, so a per-row spec read would put 1.3 MB of I/O on that path; and the
        C# twin would need a second markdown parser, which is the duplication this module exists to prevent.
        `extract_annex_a2.py --check` is what keeps the artifact equal to the standard.
        """
        if self._undefined is None:
            path = REPO / self.undefined_list
            if not path.exists():
                raise SystemExit(f"{self.undefined_list} not found — "
                                 "run: python scripts/spec/extract_annex_a2.py")
            doc = json.loads(path.read_text(encoding="utf-8"))
            self._undefined = {it["item"]: set(it["rule-ids"]) for it in doc["items"]}
        return self._undefined

    def refusals(self, row: dict[str, Any], schema: "Schema") -> list[Refusal]:
        """Why this row's derivation does NOT stand — empty when it does.

        ⛔ ONE PREDICATE, TWO CALLERS, and deliberately so: `record_verdicts.validate` prints these reasons at
        record time and `state_for` merely asks whether the list is empty. Writing the bounds twice is how a
        writer and a gate come to disagree about one rule — which is the whole content of kb/Work PB315.

        ⚙ EVERY REFUSAL CARRIES A STABLE CODE, and the code is what the cross-language parity fixture compares.
        Two engines cannot be held to the same PROSE, and asserting only "some refusal happened" would let the
        two sides refuse a row for different reasons and still look identical — which is exactly the shape
        PB315's disagreement hid behind (both engines derived GAP, for different reasons, for months).
        """
        claimed = (row.get(self.field) or "").strip()
        if not claimed:
            return []                      # no derivation claimed — nothing to refuse
        out: list[Refusal] = []
        anchor = self.anchor_for(row)
        if claimed != anchor:
            out.append(Refusal("not-computed-anchor",
                               f"derivation '{claimed}' is not the anchor computed from this row's rule-id "
                               f"('{anchor}') — it is derived, never chosen"))
        # ⛔ REFUSAL 1 — the row demonstrably HAS an observable obligation. A derivation asserts that no
        # spec-derived test can exist; a spec-derived test on the same row refutes that outright.
        if any(schema.is_spec_derived(r) for r in schema.split(row.get("test-ref", ""), schema.test_ref_sep)):
            out.append(Refusal("has-spec-derived-test",
                               "this row already carries a SPEC-DERIVED test-ref, so it has an observable "
                               "obligation and may not close on a derivation (design doc §1.1 refusal 1)"))
        # ⛔ REFUSAL 2 — a derivation explains why no test can exist, never why a non-resolving verdict is fine.
        if (row.get("verdict") or "") not in schema.resolving:
            out.append(Refusal("verdict-does-not-resolve",
                               f"verdict '{row.get('verdict') or ''}' does not resolve — a derivation stands in "
                               "for the covering test, not for the verdict"))
        # ⛔ REFUSAL 3 — a kind that owes a register determination must have STATED it first. A DOC row asks the
        # implementor to state a choice, and "nothing to observe" about an unstated choice is unfalsifiable
        # (kb/Work PB280 Q2); DOC-A.1-19 is admitted precisely because §7 states its determination.
        if schema.anchor_for(row) is not None and (row.get("rule-id") or "") not in schema.register_items():
            out.append(Refusal("determination-not-stated",
                               f"kind {row.get('kind')} owes a determination in docs/CONFORMANCE.md and none is "
                               f"filed for {row.get('rule-id')} — nothing yet for a derivation to be about"))
        entry = self.register.get(self.key_for(row))
        if entry is None:
            out.append(Refusal("no-register-row",
                               f"docs/CONFORMANCE.md {self.heading.strip('# ')} carries no row keyed "
                               f"'{self.key_for(row)}'"))
            return out
        if entry.signature not in self.signatures:
            out.append(Refusal("bad-signature",
                               f"determination '{entry.key}' is signed '{entry.signature}', not one of "
                               f"{sorted(self.signatures)} — a derivation is the OWNER's, and the signature "
                               f"records which owner decision read it"))
        arm = self.arms.get(entry.arm)
        if arm is None:
            out.append(Refusal("unknown-arm",
                               f"determination '{entry.key}' names arm '{entry.arm}', not one of "
                               f"{sorted(self.arms)}"))
            return out
        m = arm["names"].match(entry.names)
        if m is None:
            out.append(Refusal("names-shape",
                               f"determination '{entry.key}' arm {entry.arm}: 'Names' cell {entry.names!r} does "
                               f"not match {arm['names'].pattern}"))
            return out
        out += self._arm_refusals(arm["check"], m, entry, row, schema)
        return out

    def _arm_refusals(self, check: str, m: re.Match[str], entry: DerivationRow,
                      row: dict[str, Any], schema: "Schema") -> list[Refusal]:
        rid = row.get("rule-id", "")
        if check == "a2-item":
            item = int(m.group(1))
            covered = self.undefined.get(item)
            if covered is None:
                return [Refusal("a2-no-such-item",
                                f"determination '{entry.key}': Annex A.2 has no item {item}")]
            if rid not in covered:
                return [Refusal("a2-does-not-cover",
                                f"determination '{entry.key}': Annex A.2 item {item} does not cover {rid} — it "
                                f"resolves to {sorted(covered) or 'no rule at all'} (design doc §1.1 refusal 4)")]
            return []
        if check == "stated":
            return []           # the names-pattern IS the shape check for a reviewed argument
        if check == "rule-exists":
            named = m.group(1)
            if named == rid:
                return [Refusal("self-indistinguishable",
                                f"determination '{entry.key}': a rule cannot be indistinguishable from itself")]
            if named not in schema.rule_ids:
                return [Refusal("unknown-rule",
                                f"determination '{entry.key}': names '{named}', not a rule in the catalog")]
            return []
        return [Refusal("unknown-check",
                        f"determination '{entry.key}': arm {entry.arm} declares check '{check}', which this "
                        "evaluator does not implement — the schema and the writer disagree")]


_DERIVATION_CACHE: dict[str, DerivationRow] | None = None


def derivation_rows(text: str, heading: str, header_cell: str = "Rule") -> list[DerivationRow]:
    """Every data row of the §8 derivation register, in document order."""
    return [DerivationRow(c[0], c[1], _cell(c, 2), _cell(c, 3), _cell(c, 4))
            for c in register_section(text, heading, header_cell)]


def register_derivations(heading: str, text: str | None = None,
                         header_cell: str = "Rule") -> dict[str, DerivationRow]:
    """`{§8 row key -> row}` — cached when read from disk, like `register_determinations`."""
    global _DERIVATION_CACHE
    if text is not None:
        return {r.key: r for r in reversed(derivation_rows(text, heading, header_cell))}
    if _DERIVATION_CACHE is None:
        _DERIVATION_CACHE = register_derivations(heading, REGISTER_PATH.read_text(encoding="utf-8"),
                                                 header_cell)
    return _DERIVATION_CACHE


class Schema:
    """`tests/version-matrix/inventory-schema.json`, parsed."""

    def __init__(self, raw: dict[str, Any]) -> None:
        self._raw = raw
        self.verdicts: dict[str, dict[str, Any]] = raw["verdicts"]
        self.editions: list[str] = raw["editions"]
        self.code_location_re = re.compile(raw["code-location"]["pattern"])
        self.code_location_sep: str = raw["code-location"]["separator"]
        self.test_ref_sep: str = raw["test-ref"]["separator"]
        self.test_ref_forms: dict[str, dict[str, Any]] = raw["test-ref"]["forms"]
        self.spec_derived_required: bool = raw["test-ref"]["spec-derived-required"]
        self.disqualifying_methods = [re.compile(p) for p in raw["test-ref"]["disqualifying-method-patterns"]]
        #: Per-KIND evidence rules (`kinds` in the schema) — the `$comment` key is documentation, never a kind.
        self.kinds: dict[str, dict[str, Any]] = {
            k: v for k, v in raw.get("kinds", {}).items() if not k.startswith("$")}
        self._impl: dict[str, re.Pattern[str]] = {
            k: re.compile(v["implementation-pattern"]) for k, v in self.kinds.items()
            if v.get("implementation-pattern")}
        #: Files whose '#fragment' is a NAMED ANCHOR SPACE, mapped to the pattern every fragment must match.
        self.anchored_files: dict[str, re.Pattern[str]] = {
            f: re.compile(p) for f, p in raw["code-location"].get("anchored-files", {}).items()}
        #: The DERIVED-VERDICT selectors (`derived-verdicts` in the schema) — the `$comment` key is documentation.
        self.derived: dict[str, DerivedSelector] = {
            name: DerivedSelector(name, entry)
            for name, entry in raw.get("derived-verdicts", {}).items()
            if not name.startswith("$")
        }
        #: §1.1's owner-signed alternative to a spec-derived test (kb/Work PB386). `None` when the schema
        #: declares none — the door is DATA, so removing the object closes it everywhere at once.
        self.derivation: Derivation | None = Derivation(raw["derivation"]) if "derivation" in raw else None
        #: The catalog rule-ids, for the `indistinguishable-consequent` arm. Lazy, because only that arm needs
        #: them and every other consumer of this class would otherwise pay a 4,311-rule parse it never reads.
        self._rule_ids: set[str] | None = None
        #: The §7 register, injectable — see `register_items`.
        self._register_items: set[str] | None = None

    @property
    def rule_ids(self) -> set[str]:
        if self._rule_ids is None:
            self._rule_ids = {r["id"] for r in load_catalog()}
        return self._rule_ids

    def locations(self, row: dict[str, Any]) -> list[str]:
        """The row's `code-location` list, split on the schema's own separator."""
        return self.split(row.get("code-location", "") or "", self.code_location_sep)

    def anchor_for(self, row: dict[str, Any]) -> str | None:
        """The register anchor this row's KIND obliges, COMPUTED from the row's own rule-id — or None.

        ⛔ Written ONCE per language and read by everything that needs it (`state_for` here,
        `record_verdicts.validate`, and `AnchorFor` on the C# side). The anchor is a function of the row, so a
        determination cannot be filed under the wrong item the way kb/Work A11 recorded: the §15.3.3.2
        fractional-seconds determination sat under item 87, whose obligation is FORMATTED-CURRENT-DATE's
        accuracy, because the number was inherited and never re-derived.
        """
        kind = self.kinds.get(row.get("kind") or "")
        if not kind or not kind.get("anchor-template"):
            return None
        return kind["anchor-template"].replace("{rule-id}", row.get("rule-id", ""))

    def anchor_obliged(self, row: dict[str, Any], register: set[str] | None = None) -> bool:
        """Must this row CARRY the anchor `anchor_for` computes — i.e. is there a determination to point at?

        ⛔ `anchor_for` says what the anchor IS (a function of the row); this says whether the row owes it.
        CONFORMS, PARTIAL and DIVERGES each assert something about what the register says, so each owes its own
        §7 row unconditionally.

        ⛔ THE EXEMPTION IS KEYED ON "HAS A §7 ROW", NOT ON THE VERDICT, AND THAT IS A CORRECTION (PB280 Q1,
        2026-09-02). DOCUMENTED-NON-SUPPORT has TWO grounds on a DOC row and they differ exactly here:
          · the item is WITHDRAWN — the conditioning facility is not implemented, so A.1's preamble makes the
            item "not required if the optional or processor-dependent feature is not implemented" and §7 must
            carry NO row (items 84, 85, 173, 86). Nothing to anchor.
          · the element is OPTIONAL AND NOT PROVIDED — §7 DOES carry the row, because the non-provision IS the
            determination and A.1's preamble conditions the documentation duty on provision ("Documentation
            required: If the element is provided by the implementor …"). The row is the evidence, so the anchor
            is owed exactly as for CONFORMS (items 127, 206).
        Keying on the verdict alone answers both cases the same and gets the second one wrong: the row would be
        excused its own determination, and `check_pins` would stop holding its witness in agreement with §7's
        `Pinned by` — silently, at the moment the witness lands. So `anchor-exempt-verdicts` now names the
        verdicts that MAY be exempt and THE REGISTER DECIDES, which is a fact neither the row nor the schema
        carries and which cannot therefore be got wrong by an editor.
        """
        if self.anchor_for(row) is None:
            return False
        kind = self.kinds.get(row.get("kind") or "", {})
        if (row.get("verdict") or "") not in set(kind.get("anchor-exempt-verdicts", [])):
            return True
        reg = self.register_items() if register is None else register
        return (row.get("rule-id") or "") in reg

    def register_items(self) -> set[str]:
        """The rule-ids docs/CONFORMANCE.md §7 carries a determination for.

        Injectable through `_register_items` for the same reason the C# side makes it a Schema FIELD: a
        hermetic case has to be able to drive both arms of `anchor_obliged` against a fabricated register.
        """
        return set(register_determinations()) if self._register_items is None else self._register_items

    def is_observable(self, row: dict[str, Any]) -> bool:
        """Does this row name a site in the compiler a program could observe the determination through?

        The predicate is the row's OWN `code-location` list read through its kind's `implementation-pattern` —
        the greenfield `src/Cobol.Net.*` predicate `audit_annex_a1.py`'s source sweep already applies. A path is
        falsifiable by an independent reader; an opinion is not. A row of a kind that declares no
        implementation-pattern is vacuously observable, so adding a kind never tightens an existing one.
        """
        rx = self._impl.get(row.get("kind") or "")
        if rx is None:
            return True
        anchor = self.anchor_for(row)
        return any(rx.search(loc) for loc in self.locations(row) if loc != anchor)

    @property
    def resolving(self) -> set[str]:
        """The verdicts that can close a GAP — everything else leaves the row open by construction."""
        return {v for v, d in self.verdicts.items() if d["resolves"]}

    def requires(self, verdict: str) -> list[str]:
        return list(self.verdicts[verdict]["requires"])

    def is_spec_derived(self, reference: str) -> bool:
        """Can this ONE test-ref be the basis for closing a row?

        Two ways to fail. The FORM can be inherently differential — a NIST CCVS golden, a characterization
        snapshot — which CLAUDE.md rule 1 makes a regression net rather than authority. Or the form can be
        spec-derived-capable while the specific test is not: an xUnit test named `*_MatchesLegacy` says in its own
        name that its expected value came from the legacy engine.
        """
        scheme, _, body = reference.partition(":")
        form = self.test_ref_forms.get(scheme)
        if form is None or not form.get("spec-derived"):
            return False
        method = body.rsplit(".", 1)[-1].strip()
        return not any(rx.search(method) for rx in self.disqualifying_methods)

    def state_for(self, row: dict[str, Any]) -> str:
        """A row is OK only when its verdict resolves, its required evidence is present, AND a SPEC-DERIVED test
        covers it — or, for a rule with no observable obligation, an owner-signed DERIVATION stands in its place
        (§1.1, kb/Work PB386, 2026-09-03; see `derivation_stands` for why it is consulted LAST).

        This is the definition of DONE from `DESIGN-spec-conformance-review.md` §1, evaluated rather than
        asserted: (a) a located implementation or a recorded non-support decision, (b) a spec-verified verdict,
        (c) a covering spec-derived test. A row that merely says CONFORMS with nothing behind it stays a GAP —
        the point being that the GAP count is the v1.0 completion metric and must never be cheaper to move than
        the work it stands for.

        ⛔ The spec-derived clause is separate from `requires` ON PURPOSE, and it is the whole reason
        CONFORMS-but-untested is expressible: the rule can be verified against the code (a real, recordable
        finding) while no test yet pins it (so the row is not done). Folding the test into `requires` would force
        every such row to be misrecorded as PARTIAL or, worse, closed on whatever test happened to touch the
        function — which on the first Phase-B batch would have closed seven rows on NIST goldens and a
        `*_MatchesLegacy` differential.

        ⛔ A ROW OF A KIND THAT DECLARES EVIDENCE RULES PAYS THOSE TOO, and both are extra COSTS rather than an
        escape: its computed register anchor must be among its locations (a determination filed under the wrong
        item is not evidence for this row), and it must name an implementing site — a DOC row with nothing in the
        compiler to observe stays a GAP, because closing it would widen §1(a)'s definition of DONE and that is
        the owner's to widen (kb/Work PB280 Q2). The spec-derived test clause below then applies unchanged.

        ⚠ Both costs are charged only to a row with a determination to point at — the `anchor_obliged`
        predicate, not `anchor_for`. A DOC row an A.4 module WITHDRAWS (A.1's preamble: the item is not required
        if the optional or processor-dependent feature is not implemented) has no §7 row to anchor and nothing
        implemented to observe; it still owes its WITNESS test, which is the clause that keeps it open. A DOC row
        that is DOCUMENTED-NON-SUPPORT because the OPTIONAL element is not provided (PB280 Q1) is the opposite
        case — §7 states the non-provision, so the row pays both costs like any other, and stays a GAP until a
        witness proves the documented posture is what actually happens. ⛔ Reading `anchor_for` here instead was a real
        defect (kb/Work PB315): the C# twin `DerivedState` had ALWAYS asked `AnchorObliged` and said in its own
        comment that "this side and the Python writer read ONE rule" — they did not, and the disagreement was
        unobservable until a declined DOC row first earned a witness test, at which point three rows could never
        close and `EveryRowState_IsDerived_NotAsserted` went red on a correct batch.
        """
        verdict = row.get("verdict") or ""
        if verdict not in self.resolving:
            return "GAP"
        if not all(row.get(f) for f in self.requires(verdict)):
            return "GAP"
        if self.anchor_obliged(row):
            if self.anchor_for(row) not in self.locations(row):
                return "GAP"
            if not self.is_observable(row):
                return "GAP"
        if not self.spec_derived_required:
            return "OK" if row.get("test-ref") else "GAP"
        refs = self.split(row.get("test-ref", ""), self.test_ref_sep)
        if any(self.is_spec_derived(r) for r in refs):
            return "OK"
        return "OK" if self.derivation_stands(row) else "GAP"

    def derivation_stands(self, row: dict[str, Any]) -> bool:
        """Does this row carry an owner-signed DERIVATION that stands in place of §1(c)'s test?

        ⛔ IT IS CONSULTED HERE AND ONLY HERE — after the verdict, after `requires`, after a kind's anchor and
        observability costs, and exactly where a spec-derived `test-ref` would have been. That ORDER is the
        encoding of how kb/Work PB386 and PB280 Q2 coexist: a DOC row with nothing in the compiler to observe
        still computes GAP, with a derivation or without one, because it never reaches this line. Moving this
        call earlier would silently reverse an owner decision.
        """
        d = self.derivation
        if d is None or not (row.get(d.field) or "").strip():
            return False
        return not d.refusals(row, self)

    def split(self, value: str, sep: str) -> list[str]:
        return [p for p in (p.strip() for p in value.split(sep.strip())) if p]


def load_schema() -> Schema:
    if not SCHEMA_PATH.exists():
        raise SystemExit(f"inventory schema not found: {SCHEMA_PATH}")
    return Schema(json.loads(SCHEMA_PATH.read_text(encoding="utf-8")))


def load_catalog() -> list[dict[str, Any]]:
    if not CATALOG_PATH.exists():
        raise SystemExit(
            f"catalog not found: {CATALOG_PATH}\nRun: python scripts/spec/extract_rule_catalog.py")
    return json.loads(CATALOG_PATH.read_text(encoding="utf-8"))["rules"]


def load_inventory() -> list[dict[str, Any]]:
    if not INVENTORY_PATH.exists():
        raise SystemExit(
            f"inventory not found: {INVENTORY_PATH}\nRun: python scripts/spec/build_inventory.py")
    return json.loads(INVENTORY_PATH.read_text(encoding="utf-8"))


def write_inventory(rows: list[dict[str, Any]]) -> None:
    """Write the inventory ATOMICALLY — a temp file in the same directory, then an os-level replace.

    The inventory is a 1 MB file that accumulates many sessions of adjudicated work. A torn write (an interrupt, a
    full disk, two writers racing) would not merely lose the batch in flight, it would corrupt every verdict
    recorded before it, and the JSON would not necessarily fail to parse — a truncated array can still be a
    perfectly valid, silently shorter one.
    """
    INVENTORY_PATH.parent.mkdir(parents=True, exist_ok=True)
    tmp = INVENTORY_PATH.with_suffix(".json.tmp")
    tmp.write_text(json.dumps(rows, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    tmp.replace(INVENTORY_PATH)
