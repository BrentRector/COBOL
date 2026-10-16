#!/usr/bin/env python3
"""Extract Annex A.2 — the standard's own UNDEFINED LANGUAGE ELEMENT LIST — into machine-readable data.

    python scripts/spec/extract_annex_a2.py              # regenerate tests/version-matrix/annex-a2-undefined.json
    python scripts/spec/extract_annex_a2.py --check      # exit 1 if the committed artifact differs from the spec
    python scripts/spec/extract_annex_a2.py --self-test  # prove every check here can FAIL

WHY THIS EXISTS. `DESIGN-spec-conformance-review.md` §1.1 (owner decision `kb/Work/PB386`, 2026-09-03) admits an
owner-signed DERIVATION in place of §1(c)'s spec-derived test for a rule that carries no observable obligation,
and the FIRST of its three arms is mechanical: the rule is on Annex A.2's list, which the standard states is the
list of "the COBOL language elements within this Working Draft International Standard that are explicitly
undefined" (`cite.py --check A.2` OK), and §4.4 2) then makes a run unit that reaches such a situation a
CONFORMING run unit. "Mechanically checkable" means the checker must be able to answer *does A.2 item n cover
inventory row r* without a human in the loop — which needs A.2 as DATA.

⛔ WHY A GENERATED ARTIFACT AND NOT A PARSE AT CHECK TIME. Two engines evaluate the derivation rule — this
repository's Python writer (`Schema.state_for`) and its C# battery gate (`DerivedState`) — and they must agree
(`kb/Work/PB315` is the note recording what happens when they do not). Parsing 1.3 MB of spec markdown in BOTH
languages would be a second parser of the same artifact, which is the duplication `feedback_one_rule_one_place`
names; and `EveryRowState_IsDerived_NotAsserted` recomputes the state of all 4,311 rows every build, so a
per-row re-parse would be a per-row 1.3 MB read. So the extraction lives HERE, once, and both engines read the
resolved data. `--check` re-derives and diffs, and `AnnexA2UndefinedListDriftTests` runs it every build, so the
artifact cannot drift from the standard silently — the `AnnexA1RegisterDriftTests` precedent.

⭐ WHAT MAKES THE NEXT A.2 ROW AUTOMATIC. Each A.2 item ends in the standard's own citation of the rule it is
about — `(14.9.5, CANCEL statement, General rule 11)` — so the item→rule mapping is DERIVED from the standard
rather than typed by anyone. Resolving that citation against `spec-rule-catalog.json` (component-wise clause
match + rule kind + ordinal) yields the inventory `rule-id`s the item covers. A future rule the standard declares
undefined therefore needs no edit to any list: it needs one register row naming `A.2 item <n>`.

⚠ TWELVE OF THE 66 ITEMS CITE A CLAUSE WITH NO RULE NUMBER (items 7–11, 15–18, 22, 36, 38 — Cursor, Incompatible
data, Overlapping operands, …). Those resolve to NO rule-ids, deliberately: an item that does not name a rule
cannot be mechanically shown to cover one, and inventing a mapping for it would be the unfalsifiable claim this
whole mechanism exists to refuse. An arm naming such an item is REFUSED, and that is the honest answer.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

#: ⛔ THE CLAUSE-PATH RULE IS IMPORTED, NEVER RE-IMPLEMENTED. §14.9.5 must not select §14.9.50, and the one
#: definition of that lives with the schema. The dependency runs ONE way — `inventory_schema` reads this
#: script's ARTIFACT rather than importing this module, so there is no cycle.
from inventory_schema import section_matches

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC = REPO / "specs" / "ISO_COBOL.md"
CATALOG = REPO / "docs" / "rearchitecture" / "spec-rule-catalog.json"
ARTIFACT = REPO / "tests" / "version-matrix" / "annex-a2-undefined.json"
ARTIFACT_REL = "tests/version-matrix/annex-a2-undefined.json"

A2_ANCHOR = '<a id="section-a-2"></a>'
A3_ANCHOR = '<a id="section-a-3"></a>'

#: An item marker at the start of a line — the transcription writes `41\) READ statement. …`.
ITEM_RX = re.compile(r"^(\d+)\\\)\s", re.M)

#: A rule number inside a citation's rule list: an integer with an optional sub-letter (`6d`, `10c`).
#: ⛔ THE TRAILING `(?![\d.])` IS LOAD-BEARING. Item 32 cites "(14.9.25, MOVE statement, General rules 6d and
#: 14.6.13.2, Incompatible data)" — a rule list whose `and` introduces a second CLAUSE, not a second rule. Without
#: the guard the engine backtracks `\d+` from "14" to "1" and records a phantom General rule 1.
_NUM = r"\d+[a-z]?(?![\d.])"

#: One citation inside an item: `<clause>, <subject>, General rule|rules <n>[, <n> … and <n>]`. An item may carry
#: SEVERAL (item 12 cites 13.4 GR1 and 13.18.63 GR2); `finditer` takes them all.
CITATION_RX = re.compile(
    r"(\d+(?:\.\d+)*)\s*,\s*[^,()]+?,\s*(General|Syntax)\s+rules?\s+"
    r"(" + _NUM + r"(?:\s*(?:,|and)\s*" + _NUM + r")*)")

KIND_OF = {"General": "GR", "Syntax": "SR"}


def annex_a2_text(spec_text: str) -> str:
    """The A.2 region of the spec — from its own anchor to A.3's."""
    try:
        start = spec_text.index(A2_ANCHOR)
        end = spec_text.index(A3_ANCHOR, start)
    except ValueError:
        raise SystemExit(
            f"{SPEC}: the Annex A.2/A.3 anchors are not both present — the transcription, not this script, "
            "is what changed") from None
    return spec_text[start:end]


def parse_items(region: str) -> list[dict]:
    """Annex A.2's numbered items, each with its own citations, in document order."""
    marks = list(ITEM_RX.finditer(region))
    items: list[dict] = []
    for i, m in enumerate(marks):
        body = region[m.end():marks[i + 1].start() if i + 1 < len(marks) else len(region)]
        text = " ".join(body.split())
        citations = []
        for c in CITATION_RX.finditer(text):
            citations.append({
                "clause": c.group(1),
                "kind": KIND_OF[c.group(2)],
                "rules": sorted({int(n) for n in re.findall(r"\d+", c.group(3))}),
            })
        items.append({"item": int(m.group(1)), "text": text, "citations": citations})
    return items


def resolve(items: list[dict], rules: list[dict]) -> list[dict]:
    """Attach to each item the catalog `rule-id`s its OWN citations resolve to.

    A citation resolves to every catalog rule of the cited KIND whose section falls under the cited clause and
    whose ordinal is one of the cited rule numbers, in the block's PRIMARY numbering (`sublist` 1): "General rule
    3" names the third top-level rule, never item 3 of a list printed elsewhere in the block. Until kb/Work PB55
    the ordinal alone was matched, and A.2 items 3, 37 and 41 resolved to six nested-list items numbered 2 or 3
    (`GR-14.9.30.4-L2.3` …) beside the rule they cite — read at the time as "one id per edition variant", which
    the catalog has never had. Several ids per citation stay possible when a citation names several rules or a
    clause with several rule blocks, and membership, not uniqueness, is the question a checker asks of this data.
    """
    by_kind: dict[str, list[dict]] = {}
    for r in rules:
        by_kind.setdefault(r.get("kind", ""), []).append(r)
    out = []
    for it in items:
        ids: set[str] = set()
        for c in it["citations"]:
            for r in by_kind.get(c["kind"], []):
                if (r.get("ordinal") in c["rules"] and r.get("sublist", 1) == 1
                        and section_matches(c["clause"], r.get("section", ""))):
                    ids.add(r["id"])
        out.append({**it, "rule-ids": sorted(ids)})
    return out


COMMENT = [
    "GENERATED — do not hand-edit. `python scripts/spec/extract_annex_a2.py` writes it;",
    "`--check` re-derives it from specs/ISO_COBOL.md and exits 1 on any difference, and",
    "AnnexA2UndefinedListDriftTests runs that check every build.",
    "",
    "Annex A.2 is the standard's own list of the language elements it declares explicitly undefined.",
    "It is the mechanical arm of the DERIVATION evidence kind (DESIGN-spec-conformance-review.md 1.1,",
    "owner decision kb/Work/PB386, 2026-09-03): a docs/CONFORMANCE.md section-8 row may close an inventory",
    "row by naming 'A.2 item <n>', and the checker requires that row's rule-id to be among the ids item n",
    "resolves to. `rule-ids` is that resolution — derived from the item's OWN trailing citation, never typed.",
    "",
    "An item with no citation of a numbered rule resolves to NO rule-ids. That is deliberate: an item that",
    "does not name a rule cannot be shown to cover one, and an arm naming such an item is refused.",
]


def build() -> dict:
    if not SPEC.exists():
        raise SystemExit(
            f"spec not found: {SPEC}\nRun: git submodule update --init --recursive")
    if not CATALOG.exists():
        raise SystemExit(
            f"catalog not found: {CATALOG}\nRun: python scripts/spec/extract_rule_catalog.py")
    items = parse_items(annex_a2_text(SPEC.read_text(encoding="utf-8")))
    rules = json.loads(CATALOG.read_text(encoding="utf-8"))["rules"]
    return {
        "$comment": COMMENT,
        "source": "specs/ISO_COBOL.md#section-a-2",
        "generator": "scripts/spec/extract_annex_a2.py",
        "items": resolve(items, rules),
    }


def render(doc: dict) -> str:
    return json.dumps(doc, indent=1, ensure_ascii=False) + "\n"


def load_artifact() -> dict:
    if not ARTIFACT.exists():
        raise SystemExit(f"{ARTIFACT_REL} not found — run: python scripts/spec/extract_annex_a2.py")
    return json.loads(ARTIFACT.read_text(encoding="utf-8"))


def rule_ids_by_item(doc: dict | None = None) -> dict[int, set[str]]:
    """`{A.2 item number -> the rule-ids it covers}` — the one lookup every consumer wants."""
    return {it["item"]: set(it["rule-ids"]) for it in (doc or load_artifact())["items"]}


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────

FAKE_SPEC = (
    "noise before\n"
    + A2_ANCHOR + "\n### A.2 Undefined language element list\n\n"
    "The following is a list of the COBOL language elements that are explicitly undefined.\n\n"
    "1\\) CANCEL statement. The result is undefined. (14.9.5, CANCEL statement, General rule 11)\n\n"
    "2\\) MOVE statement. The results are undefined. "
    "(14.9.25, MOVE statement, General rules 6d and 14.6.13.2, Incompatible data)\n\n"
    "3\\) Overlapping operands. The situations are undefined. (14.6.10, Overlapping operands)\n\n"
    "4\\) File section. Undefined. (13.4, File section, General rule 1 and 13.18.63, VALUE clause, General rule 2)\n\n"
    + A3_ANCHOR + "\nnoise after\n")

FAKE_CATALOG = [
    {"id": "GR-14.9.5.4-11", "section": "14.9.5.4", "kind": "GR", "ordinal": 11},
    {"id": "GR-14.9.50.4-11", "section": "14.9.50.4", "kind": "GR", "ordinal": 11},
    {"id": "SR-14.9.5.3-11", "section": "14.9.5.3", "kind": "SR", "ordinal": 11},
    {"id": "GR-14.9.25.4-6", "section": "14.9.25.4", "kind": "GR", "ordinal": 6},
    {"id": "GR-14.9.25.4-1", "section": "14.9.25.4", "kind": "GR", "ordinal": 1},
    {"id": "GR-13.4.4-1", "section": "13.4.4", "kind": "GR", "ordinal": 1},
    {"id": "GR-13.18.63.4-2", "section": "13.18.63.4", "kind": "GR", "ordinal": 2},
    # Item 11 of a SECOND list printed in the same block (a peer sub-list, kb/Work PB55): not "General rule 11".
    {"id": "GR-14.9.5.4-L2.11", "section": "14.9.5.4", "kind": "GR", "ordinal": 11, "sublist": 2},
]


def self_test() -> int:
    items = resolve(parse_items(annex_a2_text(FAKE_SPEC)), FAKE_CATALOG)
    by = {it["item"]: it for it in items}
    cases: list[tuple[str, bool, str]] = []

    def check(name: str, ok: bool, detail: str = "") -> None:
        cases.append((name, ok, detail))

    check("every item is parsed", sorted(by) == [1, 2, 3, 4], f"got {sorted(by)}")
    check("an item resolves to the rule its OWN citation names",
          by[1]["rule-ids"] == ["GR-14.9.5.4-11"], f"got {by[1]['rule-ids']}")
    check("§14.9.5 does NOT select §14.9.50 (component-wise, not startswith)",
          "GR-14.9.50.4-11" not in by[1]["rule-ids"])
    check("a General-rule citation does not select a SYNTAX rule of the same ordinal",
          "SR-14.9.5.3-11" not in by[1]["rule-ids"])
    check("'General rule 11' does not select item 11 of a second list in the block (kb/Work PB55)",
          "GR-14.9.5.4-L2.11" not in by[1]["rule-ids"])
    check("⛔ 'General rules 6d and 14.6.13.2' does not invent a phantom General rule 1",
          by[2]["rule-ids"] == ["GR-14.9.25.4-6"], f"got {by[2]['rule-ids']}")
    check("an item citing a clause with NO rule number resolves to nothing",
          by[3]["rule-ids"] == [] and by[3]["citations"] == [], f"got {by[3]}")
    check("an item carrying TWO citations resolves both",
          by[4]["rule-ids"] == ["GR-13.18.63.4-2", "GR-13.4.4-1"], f"got {by[4]['rule-ids']}")

    # ⛔ THE CHECKS MUST BE ABLE TO FAIL. A resolver that returned everything would satisfy every positive
    # assertion above; these drive the negative arm explicitly (feedback_green_gates_arent_evidence).
    broken = resolve(parse_items(annex_a2_text(FAKE_SPEC)), [
        {"id": "GR-14.9.5.4-99", "section": "14.9.5.4", "kind": "GR", "ordinal": 99}])
    check("a catalog with no matching ordinal resolves to nothing (the checker is not vacuous)",
          broken[0]["rule-ids"] == [], f"got {broken[0]['rule-ids']}")
    missing_anchor = "no anchors at all"
    try:
        annex_a2_text(missing_anchor)
        check("a spec with no A.2 anchor is REFUSED", False, "it was accepted")
    except SystemExit:
        check("a spec with no A.2 anchor is REFUSED", True)

    for name, ok, detail in cases:
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + (f"  — {detail}" if not ok and detail else ""))
    bad = sum(1 for _, ok, _ in cases if not ok)
    print(f"\n{len(cases) - bad}/{len(cases)} self-test case(s) passed")
    return 1 if bad else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", action="store_true", help="re-derive and diff; exit 1 on any difference")
    ap.add_argument("--self-test", action="store_true", help="prove every check here can fail")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    if args.self_test:
        return self_test()

    doc = build()
    resolved = sum(1 for it in doc["items"] if it["rule-ids"])
    ids = {rid for it in doc["items"] for rid in it["rule-ids"]}
    summary = (f"Annex A.2 items : {len(doc['items'])}\n"
               f"  citing a rule : {resolved}\n"
               f"  rule-ids      : {len(ids)}")

    if args.check:
        have = ARTIFACT.read_text(encoding="utf-8") if ARTIFACT.exists() else ""
        want = render(doc)
        if have == want:
            print(summary)
            print(f"\n{ARTIFACT_REL} agrees with the spec.")
            return 0
        print(summary)
        print(f"\n⛔ {ARTIFACT_REL} does NOT agree with specs/ISO_COBOL.md.")
        old = {it["item"]: it for it in (json.loads(have)["items"] if have else [])}
        new = {it["item"]: it for it in doc["items"]}
        for n in sorted(set(old) | set(new)):
            if old.get(n) != new.get(n):
                print(f"   item {n}: committed {old.get(n)!r}\n            derived   {new.get(n)!r}")
        print("\n   Run: python scripts/spec/extract_annex_a2.py")
        return 1

    ARTIFACT.parent.mkdir(parents=True, exist_ok=True)
    tmp = ARTIFACT.with_suffix(".json.tmp")
    tmp.write_text(render(doc), encoding="utf-8", newline="\n")
    tmp.replace(ARTIFACT)
    print(summary)
    print(f"\nwrote {ARTIFACT_REL}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
