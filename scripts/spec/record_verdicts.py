#!/usr/bin/env python3
"""Record a batch of Phase-B conformance verdicts into the traceability inventory.

THE PROBLEM THIS SOLVES. Phase A left a 3,790-rule catalog and an inventory row per rule, and then there was no
way to write a verdict into one. `build_inventory.py` only ever PRESERVES the adjudicated fields across a rebuild;
it never sets them. The alternative — reviewers hand-editing a 1 MB JSON array — loses a batch the first time two
of them overlap, and offers no point at which anything is checked.

So verdicts are never written by hand. A reviewer (or a Phase-B agent) emits a BATCH FILE; this merges it.

    python scripts/spec/record_verdicts.py <batch.json> [<batch.json> ...]
    python scripts/spec/record_verdicts.py --dry-run <batch.json>     # validate + report, write nothing

A batch file is a JSON list of records, or an object with a "records" list (and optionally a "batch" label):

    {"batch": "phase-b-15.7-abs",
     "records": [
       {"rule-id": "AR-15.7-1",
        "verdict": "CONFORMS",
        "code-location": "src/Cobol.Net.Compiler/Binder/Intrinsics/IntrinsicSignatures.cs#Abs",
        "test-ref": "conformance:2023/intrinsic_abs; unit:IntrinsicArgumentRuleTests.Abs_RejectsAlphanumeric",
        "editions": "85,2002,2014,2023",
        "notes": ""}]}

WITNESSES MERGE; STATEMENTS ARE RESTATED (kb/Work PB959). `code-location` and `test-ref` are SETS of witnesses
(`inventory_schema.WITNESS_FIELDS`): a record's values are UNIONED with the row's, in the row's order, so a batch
that names one new witness adds it and a batch that omits the field keeps what the row had. Every other field —
`verdict`, `editions`, `notes`, `derivation` — is one statement, and the record restates it. Until PB959 both
witness fields were overwritten too, and a batch that re-typed two of five witnesses deleted the other three with
nothing anywhere able to see it. A witness now leaves a row only by an explicit RETIREMENT:

       {"rule-id": "...", ..., "retire-witnesses": "<ref>; <ref>", "retire-reason": "<why each no longer witnesses>"}

Each retired ref must be on the row today and must not be re-named by the same record; the writer drops it and
appends `retired-witness: <ref> (<reason>)` to the row's `notes`, which is what lets
`audit_witness_loss.py --check` tell a deliberate retirement from a silent loss after the batch file is gone. A
retirement is printed apart from `rows changed`, and a write that would lose a witness any OTHER way is refused.

A WITNESS-ONLY record — `{"rule-id": "...", "test-ref": "..."}`, with no verdict and no statement field — adds
witnesses to a row that is already adjudicated and restates nothing, so it is safe to re-apply on a merged tree
after another batch has re-adjudicated the same row (a full record there would silently revert it).

ALL-OR-NOTHING. Every record in every named batch is validated before ANY of them is merged, and the merge itself
writes through a temp file and an atomic replace. A batch that is half-right leaves the inventory untouched rather
than half-adjudicated — a partially applied batch is the worst outcome available here, because the rows that DID
land look exactly like reviewed work and nothing afterwards can tell them from it.

WHAT IS CHECKED HERE, AND WHAT IS NOT. This validates SHAPE only: the rule-id is real, the verdict is in the
vocabulary, the fields that verdict requires are present, the editions are legal, the syntax of each reference
parses, the register anchor the row's KIND computes is present, and no fragment on an `anchored-files` path is
outside that file's anchor space. A record may also carry a `derivation` — §1.1's owner-signed alternative to a
spec-derived test, for a rule with no observable obligation (kb/Work PB386) — and every one of the six bounds the
owner set on it is checked HERE, by the same predicate `Schema.state_for` asks. All of that is decidable from the
record, the schema, and the registers the schema names.
Whether a `code-location` symbol still exists and whether a `test-ref` names a test that is really on disk
is checked by the C# battery gate `SpecTraceabilityInventoryDriftTests` — deliberately NOT here. That check has to
keep holding as the tree changes under an already-recorded row, so it belongs to something that runs every build,
and implementing it in both places would be exactly the duplication CLAUDE.md rule 5 forbids.
See `scripts/spec/inventory_schema.py` for the full division of labour.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys
from collections import Counter

from inventory_schema import (ADJUDICATED, RETIRED_WITNESS_MARK, WITNESS_FIELDS, load_catalog, load_inventory,
                              load_schema, resites, witness_losses, witness_shape_error, write_inventory)

#: The record-level CONTROL fields: they steer the write and are never stored on the row as themselves.
RETIRE, RETIRE_REASON = "retire-witnesses", "retire-reason"
CONTROL = (RETIRE, RETIRE_REASON)

# ⛔ THE PRINTED GATE CARRIES ITS OWN POPULATION GUARD (kb/Work PB751). This constant is never executed here —
# it is an instruction handed to a HUMAN — but the hazard travels with the text: vstest answers a filter that
# matches nothing with a PASSING run of zero tests, so if `SpecTraceabilityInventoryDriftTests` is renamed or
# partitioned, the operator who pastes this line sees a green that proves nothing about the rows just recorded.
# The guard is the same one `build-local.{ps1,sh}`, the two regenerators and the CI legs call — one rule, one
# place — so the two lines below fail loudly instead. The run itself is SCRUBBED (kb/Work PB1718): a gate leg
# handshake or a VSTest*/RunSettingsFilePath variable left in the operator's shell would narrow it further.
GATE_FILTER = "FullyQualifiedName~SpecTraceabilityInventory"
GATE_PROJECT = "tests/Cobol.Net.Tests.Unit"
GATE = (f'python scripts/filter_population.py --filter "{GATE_FILTER}" --filtered {GATE_PROJECT} --allow-build\n'
        f'    python scripts/test_population.py scrubbed dotnet test {GATE_PROJECT} --filter "{GATE_FILTER}"')
INVENTORY_REL = "tests/version-matrix/traceability-inventory.json"


def read_batch(path: pathlib.Path) -> list[dict]:
    raw = json.loads(path.read_text(encoding="utf-8"))
    if isinstance(raw, dict):
        raw = raw.get("records", [])
    if not isinstance(raw, list):
        raise SystemExit(f"{path}: expected a list of records, or an object with a 'records' list")
    return raw


def _sep(field: str, schema) -> str:
    return schema.code_location_sep if field == "code-location" else schema.test_ref_sep


def is_witness_only(rec: dict) -> bool:
    """A record that ADDS (or retires) witnesses on an already-adjudicated row and restates no statement.

    The shape a golden round and a history restore both need: "this test also witnesses that rule" is not a
    re-adjudication, and forcing it to restate the verdict, editions and notes is how a batch applied after
    another train's re-adjudication would silently REVERT it (the lander re-applies batches on a merged tree).
    """
    return not any(f in rec for f in ADJUDICATED if f not in WITNESS_FIELDS)


def merged(row: dict, rec: dict, schema) -> dict[str, str]:
    """The adjudicated fields `row` carries after `rec` is applied — THE write rule, read by `validate` too.

    Witness fields: the row's witnesses minus the record's retirements, then the record's new ones, deduplicated in
    first-seen order — except that a record which CHANGES the verdict and SUPPLIES a code-location re-sites that
    field (`inventory_schema.resites`: the defect site a PARTIAL row names is not implementation evidence for the
    CONFORMS record that closes it). A record that omits a witness field keeps what the row had. Statement fields:
    the record's value (absent = empty, as before PB959). A retirement is appended to `notes` as
    `retired-witness: <ref> (<reason>)` — the durable mark `audit_witness_loss.py` reads.

    A WITNESS-ONLY record (`is_witness_only`: no verdict and no statement field) restates nothing: every statement
    field keeps the row's value, so the record can be applied on any tree, in any order, any number of times.
    """
    retire = set(schema.split(rec.get(RETIRE, ""), schema.test_ref_sep))
    source = row if is_witness_only(rec) else rec
    out = {f: source.get(f, "") or "" for f in ADJUDICATED if f not in WITNESS_FIELDS}
    for field in WITNESS_FIELDS:
        sep = _sep(field, schema)
        given = schema.split(rec.get(field, ""), sep)
        kept = [] if given and resites(field, row.get("verdict", "") or "", out["verdict"]) else \
            schema.split(row.get(field, "") or "", sep)
        refs = [r for r in kept if r not in retire] + given
        out[field] = sep.join(dict.fromkeys(refs))
    if retire:
        reason = rec.get(RETIRE_REASON, "").strip()
        marks = " ".join(f"{RETIRED_WITNESS_MARK} {r} ({reason})" for r in sorted(retire))
        out["notes"] = f"{out['notes']} {marks}".strip()
    return out


def validate(records: list[tuple[pathlib.Path, int, dict]], schema, catalog: dict[str, str],
             by_id: dict[str, dict]) -> list[str]:
    """Every shape violation across the whole set — reported together, never one at a time.

    Stopping at the first bad record would make a reviewer re-run this once per mistake, and a batch produced by a
    fan-out of agents tends to carry the SAME mistake in many rows: seeing all of them at once is what turns a
    twenty-round correction loop into one.

    `catalog` maps a rule-id to its catalog KIND, because two of the shape rules are per-kind: the computed
    register anchor a `kinds` entry obliges, and the anchor space a listed file's fragments must belong to. A
    batch record carries no `kind` of its own — the catalog owns it — so the lookup happens here rather than the
    record being trusted to restate it.
    """
    bad: list[str] = []
    seen: dict[str, str] = {}

    for path, i, rec in records:
        where = f"{path.name}[{i}]"
        rid = rec.get("rule-id", "")
        if not rid:
            bad.append(f"{where}: no rule-id")
            continue
        if rid not in catalog:
            bad.append(f"{where}: rule-id '{rid}' is not in the catalog")
        if rid in seen and seen[rid] != where:
            bad.append(f"{where}: rule-id '{rid}' recorded twice in this run (also {seen[rid]})")
        seen.setdefault(rid, where)

        if unknown := set(rec) - {"rule-id", *ADJUDICATED, *CONTROL}:
            bad.append(f"{where}: unknown field(s) {sorted(unknown)} — a record may set only {list(ADJUDICATED)}"
                       f" (plus the control fields {list(CONTROL)})")

        # ⛔ TYPE-CHECK BEFORE TOUCHING A FIELD. A batch file is UNTRUSTED input — it is written by an agent, or
        # by hand — and the first real batch to get this wrong supplied `editions` as a JSON LIST, which made the
        # validator itself throw an AttributeError. A validator that crashes on malformed input tells the author
        # nothing about what to fix and reports no other violation in the file; it must FAIL THE RECORD, not the
        # run. Checked here so every field access below is safe.
        if mistyped := sorted(f for f in (*ADJUDICATED, *CONTROL) if f in rec and not isinstance(rec[f], str)):
            for f in mistyped:
                bad.append(f"{where}: '{f}' is {type(rec[f]).__name__}, not a string"
                           + (f" — write it as \"{','.join(map(str, rec[f]))}\"" if isinstance(rec[f], list) else ""))
            continue

        row = by_id.get(rid, {})
        verdict = rec.get("verdict", "")
        if is_witness_only(rec):
            if not (rec.get("code-location") or rec.get("test-ref") or rec.get(RETIRE)):
                bad.append(f"{where}: no verdict and no witness — the record says nothing")
                continue
            verdict = row.get("verdict") or ""
            if not verdict:
                bad.append(f"{where}: a witness-only record (no verdict, no statement field) needs a row that "
                           f"is already adjudicated — '{rid}' has no verdict")
                continue
        elif not verdict:
            bad.append(f"{where}: no verdict")
            continue
        if verdict not in schema.verdicts:
            bad.append(f"{where}: verdict '{verdict}' is not in the vocabulary {sorted(schema.verdicts)}")
            continue

        # ⚖ The EVIDENCE rules below are asked of the row AS IT WILL BE WRITTEN (`merged`), not of the record: a
        # record that names only a new test-ref still leaves the row's code-locations in place (kb/Work PB959),
        # so requiring the record to restate them would be requiring the very re-typing that used to lose them.
        # The per-reference SYNTAX checks further down stay on the record's own values — those are its claims.
        after = merged(row, rec, schema)
        retire = schema.split(rec.get(RETIRE, ""), schema.test_ref_sep)
        if retire and not rec.get(RETIRE_REASON, "").strip():
            bad.append(f"{where}: '{RETIRE}' needs a '{RETIRE_REASON}' — a retired witness is evidence removed, "
                       f"and the row's notes must say why")
        if rec.get(RETIRE_REASON) and not retire:
            bad.append(f"{where}: '{RETIRE_REASON}' without '{RETIRE}' retires nothing")
        carried = {f: schema.split(row.get(f, "") or "", _sep(f, schema)) for f in WITNESS_FIELDS}
        for ref in retire:
            if not any(ref in refs for refs in carried.values()):
                bad.append(f"{where}: retire-witnesses '{ref}' is not on the row — a retirement of nothing is a "
                           f"typo that would read as work done")
            if any(ref in schema.split(rec.get(f, ""), _sep(f, schema)) for f in WITNESS_FIELDS):
                bad.append(f"{where}: '{ref}' is both retired and named as a witness by the same record")

        for field in schema.requires(verdict):
            if not after.get(field):
                bad.append(f"{where}: verdict {verdict} requires a non-empty '{field}'")

        if ed := rec.get("editions", ""):
            if illegal := [e for e in schema.split(ed, ",") if e not in schema.editions]:
                bad.append(f"{where}: editions {illegal} not in {schema.editions}")

        # Each reference the record NAMES is shape-checked by the one predicate the history replay also asks
        # (`witness_shape_error`); the row's already-carried references are the battery gate's to re-check.
        for field in WITNESS_FIELDS:
            for ref in schema.split(rec.get(field, ""), _sep(field, schema)):
                if err := witness_shape_error(field, ref, schema):
                    bad.append(f"{where}: {err}")

        # The row's KIND may oblige a COMPUTED register anchor (`kinds` in the schema). It is derived from the
        # rule-id rather than typed, so a mis-filed determination cannot be spelled — kb/Work A11's failure mode.
        # ⚠ Only a verdict that CLAIMS a determination owes it: `anchor_obliged` exempts the verdicts the kind
        # names in `anchor-exempt-verdicts` (a declined facility withdraws the A.1 item, so there is no §7 row).
        probe = {"rule-id": rid, "kind": catalog.get(rid, ""), "verdict": verdict,
                 "code-location": after["code-location"], "test-ref": after["test-ref"],
                 "derivation": after["derivation"]}
        anchor = schema.anchor_for(probe)
        if schema.anchor_obliged(probe) and anchor not in schema.locations(probe):
            bad.append(f"{where}: kind {catalog[rid]} requires the register anchor '{anchor}' among its "
                       f"code-location(s) — it is computed from the rule-id, never chosen")

        # ⛔ A DERIVATION IS THE OWNER'S ALTERNATIVE TO §1(c)'s TEST, AND IT IS CHECKED, NOT TRUSTED.
        # kb/Work PB386 (2026-09-03) admits an owner-signed determination in docs/CONFORMANCE.md §8 in place of
        # a spec-derived test-ref, for a rule that carries no observable obligation — and its second stated cost
        # is that the determination be CHECKABLE, "or the GAP metric becomes cheaper to move than the work it
        # stands for". The bounds live in ONE predicate (`Derivation.refusals`) that `Schema.state_for` also
        # asks, so the writer and the drift gate cannot come to read one rule differently (kb/Work PB315).
        if schema.derivation is not None:
            for refusal in schema.derivation.refusals(probe, schema):
                bad.append(f"{where}: [{refusal.code}] {refusal.message}")

    return bad


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("batches", nargs="+", type=pathlib.Path, help="one or more verdict batch files")
    ap.add_argument("--dry-run", action="store_true", help="validate and report; write nothing")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    schema = load_schema()
    rows = load_inventory()
    by_id = {r["rule-id"]: r for r in rows}
    catalog = {r["id"]: r.get("kind", "") for r in load_catalog()}

    records: list[tuple[pathlib.Path, int, dict]] = []
    for path in args.batches:
        if not path.exists():
            sys.exit(f"batch not found: {path}")
        records += [(path, i, rec) for i, rec in enumerate(read_batch(path))]

    if not records:
        print("no records in the named batch file(s) — nothing to do")
        return 0

    if bad := validate(records, schema, catalog, by_id):
        print(f"⛔ {len(bad)} shape violation(s) — NOTHING was written:\n")
        for b in bad:
            print(f"   {b}")
        return 1

    gap_before = sum(1 for r in rows if r["state"] == "GAP")
    changed, rewritten = 0, []
    verdicts: Counter[str] = Counter()
    closed: Counter[str] = Counter()
    untested: list[str] = []
    derived_closures: list[str] = []
    added: Counter[str] = Counter()
    retired, resited, lost = [], [], []
    for _, _, rec in records:
        row = by_id[rec["rule-id"]]
        had = row.get("verdict") or ""
        before = dict(row)
        row.update(merged(row, rec, schema))
        row["state"] = schema.state_for(row)
        verdict = row["verdict"]   # a witness-only record carries none of its own
        if had and had != verdict:
            rewritten.append(f"{rec['rule-id']}: {had} → {verdict}")
        if any(row.get(k, "") != before.get(k, "") for k in ADJUDICATED):
            changed += 1
        # ⛔ THE WITNESS COUNT IS WATCHED, NOT ONLY THE FIELDS (kb/Work PB959): a narrowing and a widening both
        # increment `changed`, so it cannot be the number anyone reads to learn that evidence left a row.
        for loss in witness_losses(before, row, schema):
            (retired if loss.retired else resited if loss.resited else lost).append(loss)
        for f in WITNESS_FIELDS:
            added[f] += len(set(schema.split(row.get(f, ""), _sep(f, schema)))
                            - set(schema.split(before.get(f, "") or "", _sep(f, schema))))
        verdicts[verdict] += 1
        if row["state"] == "OK":
            closed[verdict] += 1
            if row.get("derivation"):
                derived_closures.append(rec["rule-id"])
        elif verdict in schema.resolving:
            untested.append(rec["rule-id"])

    gap_after = sum(1 for r in rows if r["state"] == "GAP")

    # The merge rule makes an unexcused loss impossible by construction; this is the check that it stays so. A
    # write that would lose evidence any other way is refused whole, like a shape violation.
    if lost:
        print(f"⛔ {len(lost)} witness(es) would be LOST without a retirement — NOTHING was written:\n")
        for l in lost:
            print(f"   ⚠ NARROWED {l.rule_id} {l.field}: {l.ref}")
        return 1

    print(f"records        : {len(records)} across {len(args.batches)} batch file(s)")
    print(f"rows changed   : {changed}")
    print(f"witnesses      : +{added['test-ref']} test-ref, +{added['code-location']} code-location added; "
          f"{len(retired)} retired, {len(resited)} re-sited")
    for l in retired:
        print(f"    ⚠ RETIRED  {l.rule_id} {l.field}: {l.ref}")
    for l in resited:
        print(f"    ⓘ RE-SITED {l.rule_id} {l.field}: {l.ref}  (verdict changed; the record restates the site)")
    for v, n in sorted(verdicts.items(), key=lambda kv: -kv[1]):
        # Report what ACTUALLY closed, per verdict — not what the verdict is nominally capable of closing. A
        # resolving verdict still needs a SPEC-DERIVED covering test, so "CONFORMS n" and "n rows closed" are
        # different numbers, and printing the first as if it were the second is how a burn-down gets overstated.
        note = ""
        if v in schema.resolving:
            note = f" — {closed[v]} closed the GAP, {n - closed[v]} still test-needed"
        print(f"    {v:<24s} {n:5d}{note}")
    if untested:
        print(f"\n  ⓘ {len(untested)} CONFORMS-but-untested row(s) — verdict recorded, row still open until a"
              f" SPEC-DERIVED test covers it (a NIST golden or a *_MatchesLegacy differential does not):")
        for rid in untested[:10]:
            print(f"       {rid}")
    if derived_closures:
        # ⛔ REPORTED SEPARATELY BECAUSE IT IS A DIFFERENT KIND OF CLOSURE. These rows close on an owner-signed
        # DERIVATION rather than on a test (kb/Work PB386), so a reader comparing the GAP delta against the
        # goldens that landed must be able to see which rows moved without one.
        print(f"\n  ⓘ {len(derived_closures)} row(s) closed on an owner-signed DERIVATION rather than a test "
              f"(docs/CONFORMANCE.md {schema.derivation.heading.strip('# ')}):")
        for rid in derived_closures[:10]:
            print(f"       {rid}")
    if rewritten:
        print(f"\n  ⚠ {len(rewritten)} row(s) RE-ADJUDICATED — a prior verdict was replaced:")
        for r in rewritten[:10]:
            print(f"       {r}")
    print(f"\nGAP            : {gap_before} → {gap_after}  ({gap_before - gap_after:+d} closed of {len(rows)})")

    if args.dry_run:
        print("\n--dry-run: nothing written.")
        return 0

    write_inventory(rows)
    print(f"\nwrote {INVENTORY_REL}")
    print(f"⛔ NOW RUN THE GATE — it is what checks that these references actually resolve:\n    {GATE}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
