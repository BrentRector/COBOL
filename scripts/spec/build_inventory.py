#!/usr/bin/env python3
"""Build / refresh the traceability inventory — the P14 burn-down instrument.

`scripts/session-probe.ps1` already looks for tests/version-matrix/traceability-inventory.json and reports
"invent: N rows · M GAP (v1.0 = zero GAP)". Until now that file did not exist, so the probe said "not built yet"
and the project had no burn-down number at all.

This seeds one inventory row per normative rule from the Phase-A catalog, and is RESUMABLE by design: an existing
row's verdict, code-location, test-ref and notes are PRESERVED across re-runs (the design doc requires that a
rule's verdict persist across sessions — Phase B is many sessions of work). Re-running after the spec submodule
moves adds new rules and reports removed ones; it never silently discards adjudicated work.

    python scripts/spec/build_inventory.py            # create or refresh
    python scripts/spec/build_inventory.py --stats    # report only, write nothing

⛔ THIS SCRIPT SEEDS ROWS; IT NEVER WRITES A VERDICT. Verdicts are recorded by `record_verdicts.py`, which merges
a validated batch file — see there for why hand-editing the inventory is not an option. The two agree because
neither owns the rules: the verdict vocabulary, each verdict's required evidence and the GAP/OK derivation are
DATA, in `tests/version-matrix/inventory-schema.json`, loaded through `inventory_schema.py`.

Row schema (one per rule):
    rule-id / section / kind / ordinal / subject         from the Phase-A catalog (regenerated, authoritative)
    state                                    derived — never written by hand; the GAP count is what session-probe
                                             reports and what v1.0 is defined against
    verdict / code-location / test-ref /     the adjudicated fields, preserved across every rebuild;
      derivation / editions / notes          their vocabulary and requirements live in inventory-schema.json
                                             (`editions` is carried in its one spelling, `Schema.spell_editions`)
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from collections import Counter

from inventory_schema import (ADJUDICATED as CARRIED, CATALOG_PATH as CATALOG,
                              INVENTORY_PATH as INVENTORY, REPO, load_schema, write_inventory)

#: Whitespace and the transcription's markdown escapes are not content — two texts that differ only there are the
#: same rule, and a digest that said otherwise would cry wolf on every re-transcription.
_SQUASH = re.compile(r"\s+")


def rule_digest(text: str) -> str:
    """A short content fingerprint of a rule's catalog text, REGENERATED every rebuild and never carried.

    ⛔ WHY A ROW NEEDS ONE (kb/Work PB698). `prior` is keyed by rule-id and the adjudicated fields are carried
    across a rebuild by that key alone, so the rebuild is only safe while an id names the same RULE it named
    last time. It does not: a sub-list id is POSITIONAL (`…-L<sublist>.<n>`), so when a sub-list above it stops
    existing — as it does whenever a nested list's transcription is re-levelled — every id below SHIFTS DOWN and
    silently inherits a verdict that was adjudicated against different text. PB698's repair moved six such rows
    at once, and nothing in this script could see it: `added`/`removed` compare ids, and `subject` is the rule
    BLOCK's subject, identical for every row of the block. The digest is the missing observation.
    """
    return hashlib.sha1(_SQUASH.sub(" ", text.replace("\\", "")).strip().encode("utf-8")).hexdigest()[:12]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--stats", action="store_true", help="report only; write nothing")
    ap.add_argument("--check", action="store_true",
                    help="report only; exit 1 if a carried verdict landed on CHANGED rule text")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    if not CATALOG.exists():
        sys.exit(f"catalog not found: {CATALOG}\nRun: python scripts/spec/extract_rule_catalog.py")

    schema = load_schema()
    rules = json.loads(CATALOG.read_text(encoding="utf-8"))["rules"]

    prior: dict[str, dict] = {}
    if INVENTORY.exists():
        for row in json.loads(INVENTORY.read_text(encoding="utf-8")):
            prior[row["rule-id"]] = row

    out: list[dict] = []
    respelled: list[str] = []
    unspellable: list[str] = []
    for r in rules:
        row = {
            "rule-id": r["id"], "section": r["section"], "kind": r["kind"], "ordinal": r["ordinal"],
            "subject": r["subject"],
            "rule-digest": rule_digest(r["text"]),
            "verdict": "", "code-location": "", "test-ref": "", "derivation": "", "editions": "", "notes": "",
        }
        if old := prior.get(r["id"]):
            for k in CARRIED:
                if old.get(k):
                    row[k] = old[k]
        # ⚖ A carried `editions` is WRITTEN in its one spelling (`Schema.spell_editions`, kb/Work PB3172), so a
        # rebuild on any tree — a merge that brought in a row recorded before PB3172 — leaves the field in the form
        # the validator accepts. Only the spelling moves; an edition the schema lacks or names twice is a claim
        # to correct through a record_verdicts batch, so it stops the rebuild instead.
        if carried_editions := row["editions"]:
            try:
                row["editions"] = schema.spell_editions(schema.split(carried_editions, schema.editions_sep))
            except ValueError as e:
                unspellable.append(f"{r['id']}: {e}")
            else:
                if row["editions"] != carried_editions:
                    respelled.append(r["id"])
        row["state"] = schema.state_for(row)
        out.append(row)

    added = [r["rule-id"] for r in out if r["rule-id"] not in prior]
    removed = [rid for rid in prior if rid not in {r["rule-id"] for r in out}]
    carried = sum(1 for r in out if any(r[k] for k in CARRIED))

    gaps = sum(1 for r in out if r["state"] == "GAP")
    print(f"rules in catalog : {len(rules)}")
    print(f"inventory rows   : {len(out)}")
    print(f"  GAP            : {gaps}")
    print(f"  resolved       : {len(out) - gaps}")
    if prior:
        print(f"  carried forward: {carried} row(s) with adjudicated work preserved")
        if added:
            print(f"  NEW            : {len(added)} (spec moved — these need verdicts, and editions, which a new row "
                  f"lacks and SpecTraceabilityInventoryDriftTests refuses empty: kb/Work PB3173)")
        if removed:
            print(f"  ⚠ REMOVED      : {len(removed)} rows no longer in the spec — verdicts dropped:")
            for rid in removed[:10]:
                print(f"       {rid}")

    # ⛔ THE CARRY IS ONLY SAFE WHILE AN ID NAMES THE SAME RULE. Compare each carried row's stored digest with the
    # one the catalog yields now: a difference means the verdict was adjudicated against text that is no longer
    # there. `added`/`removed` cannot see this — the id is in both lists — and it is not a theoretical shape:
    # PB698's re-levelling shifted six positional sub-list ids under live verdicts. A row with no stored digest is
    # a pre-guard row and reports nothing (it has nothing to be compared with), which is why this landing states
    # its own migration explicitly instead of relying on the guard it installs.
    drifted = [r["rule-id"] for r in out
               if (p := prior.get(r["rule-id"])) and p.get("rule-digest")
               and p["rule-digest"] != r["rule-digest"] and any(r[k] for k in CARRIED)]
    if drifted:
        print(f"\n⛔ {len(drifted)} CARRIED VERDICT(S) LANDED ON CHANGED RULE TEXT — re-adjudicate each through a")
        print("   record_verdicts batch, or move it to the id that now carries its text (kb/Work PB698):")
        for rid in drifted:
            print(f"       {rid}")

    if respelled:
        print(f"\n  editions respelled: {len(respelled)} carried row(s) written in the one spelling "
              f"(kb/Work PB3172), e.g. {', '.join(respelled[:5])}")
    if unspellable:
        print(f"\n⛔ {len(unspellable)} CARRIED `editions` FIELD(S) NAME AN EDITION THE SCHEMA LACKS, OR ONE TWICE —"
              f" nothing is written; correct each through a record_verdicts batch:")
        for u in unspellable:
            print(f"       {u}")
        return 1

    if args.check:
        return 1 if drifted else 0

    by_clause = Counter(r["section"].split(".")[0] for r in out)
    print("\n  GAP by top-level clause (the P14 work map):")
    for sec, n in sorted(by_clause.items(), key=lambda kv: (0, int(kv[0]), "") if kv[0].isdigit() else (1, 0, kv[0])):
        g = sum(1 for r in out if r["section"].split(".")[0] == sec and r["state"] == "GAP")
        print(f"      §{sec:<3s} {g:5d} / {n}")

    if args.stats:
        return 0

    write_inventory(out)
    print(f"\nwrote {INVENTORY.relative_to(REPO)}  ({INVENTORY.stat().st_size / 1_048_576:.1f} MB)")
    print("session-probe will now report the live GAP count.")

    # kb/Conformance/ is a RENDERED VIEW over this file (kb/Work PB3026, scripts/rendered_views.py): the session probe
    # renders it at every session start and `rendered_views.py conformance-burn-down` renders it on demand, so this
    # writer no longer knows the view exists (one mechanism per job).
    return 0


if __name__ == "__main__":
    sys.exit(main())
