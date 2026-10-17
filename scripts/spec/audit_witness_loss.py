#!/usr/bin/env python3
"""Watch the axis nothing else watches: a traceability-inventory row LOSING a witness (kb/Work PB959).

    python scripts/spec/audit_witness_loss.py --check [--base REF]      # the gate: working tree vs merge-base
    python scripts/spec/audit_witness_loss.py --history [--since REV]   # every loss in the inventory's git history
    python scripts/spec/audit_witness_loss.py --history --restore-batch OUT.json   # ...and a batch owing them back
    python scripts/spec/audit_witness_loss.py --self-test               # prove the check FAILS on a planted loss

⛔ WHY THIS EXISTS. `code-location` and `test-ref` are the EVIDENCE a row's verdict stands on, and the inventory is
the instrument that defines "done" for this project. Until PB959, `record_verdicts.py` assigned both fields by
blind overwrite, so a batch that re-typed two of a row's five witnesses silently deleted the other three, and one
that omitted the field emptied it. `SpecTraceabilityInventoryDriftTests.EveryTestRef_ResolvesToARealTest` checks
that each SURVIVING reference RESOLVES — deliberately not how many there are — so a subtraction of perfectly valid
evidence left every gate green and every derived artifact (the burn-down, the owner's ledger) unchanged. A row
whose witnesses were removed still reads CONFORMS; it simply no longer has the evidence that justified it.

WHAT COUNTS AS A LOSS is defined ONCE, in `inventory_schema.witness_losses`, and the writer asks the same function
at record time — so this auditor and `record_verdicts.py` cannot come to disagree about it. Three losses are
EXCUSED, and each is printed: a RETIREMENT (the writer records `retired-witness: <ref> (<reason>)` in the row's
`notes` when a batch retires a witness by name), a RE-SITE (a record that changes the verdict restates the
row's `code-location` — see `inventory_schema.WITNESS_FIELDS` for why), and a RE-SPELLING (the test's file was
MOVED and the row cites it under its new name: git's rename record across the change says which file became
which, `respellings`, so a move needs no map of its own; counted, not listed). Every other loss is RED.

WHAT IS NOT A WITNESS LOSS: a ROW that disappears. Rows are regenerated from the rule catalog by
`build_inventory.py`, which owns a row's existence; such rows are counted and printed, never gated.

WHETHER A RESTORED REFERENCE STILL RESOLVES is NOT decided here. That is the C# battery gate's job (the division
of labour in `inventory_schema.py`); a `--restore-batch` is applied through `record_verdicts.py` and then gated
by `SpecTraceabilityInventoryDriftTests`, which names every restored reference that points at nothing.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import subprocess
import sys
from collections import Counter
from typing import Any

from inventory_schema import (INVENTORY_PATH, REPO, SCHEMA_PATH, Schema, WitnessLoss, load_inventory, load_schema,
                              witness_losses, witness_shape_error)

INVENTORY_REL = INVENTORY_PATH.relative_to(REPO).as_posix()
SCHEMA_REL = SCHEMA_PATH.relative_to(REPO).as_posix()


def _git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=REPO, check=True, capture_output=True, text=True,
                          encoding="utf-8").stdout


def _sep(field: str, schema) -> str:
    return schema.code_location_sep if field == "code-location" else schema.test_ref_sep


def rows_at(rev: str) -> list[dict[str, Any]] | None:
    """The inventory as committed at `rev`, or None when the file does not exist there."""
    r = subprocess.run(["git", "show", f"{rev}:{INVENTORY_REL}"], cwd=REPO, capture_output=True)
    return json.loads(r.stdout.decode("utf-8")) if r.returncode == 0 else None


def schema_at(rev: str) -> Schema | None:
    """The inventory schema as committed at `rev` (its test-ref forms name the files of THAT tree), or None."""
    r = subprocess.run(["git", "show", f"{rev}:{SCHEMA_REL}"], cwd=REPO, capture_output=True)
    return Schema(json.loads(r.stdout.decode("utf-8"))) if r.returncode == 0 else None


def respelled_from_name_status(out: str, before: Schema, after: Schema) -> dict[str, str]:
    """`git diff -M --name-status -z` output → {before test-ref: after test-ref} for every RENAMED file that a
    file-backed form names on both sides, each side spelled by its own tree's schema (`Schema.ref_for_path`)."""
    fields = out.split("\0")
    found: dict[str, str] = {}
    i = 0
    while i < len(fields) and fields[i]:
        status = fields[i]
        if status[:1] in ("R", "C"):
            old, new = fields[i + 1], fields[i + 2]
            i += 3
            if status[0] == "R" and (was := before.ref_for_path(old)) and (now := after.ref_for_path(new)):
                found[was] = now
        else:
            i += 2
    return found


def respellings(before_rev: str, after_rev: str | None, before: Schema, after: Schema) -> dict[str, str]:
    """{before test-ref: after test-ref} for every witness file git records as RENAMED from `before_rev` to
    `after_rev` (None: the working tree, whose renamed files must be tracked). Git's rename record IS the identity
    of a moved file, so a move (the suite's S2 move, a test re-filed under another clause folder) needs no
    hand-kept map, and the history walk reads the same record of every commit long after the move landed."""
    roots = sorted(set(before.ref_roots) | set(after.ref_roots))
    out = _git("-c", "diff.renameLimit=0", "diff", "-M", "--name-status", "-z", before_rev,
               *([after_rev] if after_rev else []), "--", *roots)
    return respelled_from_name_status(out, before, after)


def compare(before: list[dict[str, Any]], after: list[dict[str, Any]], schema,
            respelled: dict[str, str] | None = None) -> tuple[list[WitnessLoss], list[str]]:
    """(every witness lost by a row present on both sides, the rule-ids of witnessed rows that vanished)."""
    by_after = {r["rule-id"]: r for r in after}
    losses: list[WitnessLoss] = []
    vanished: list[str] = []
    for row in before:
        now = by_after.get(row["rule-id"])
        if now is None:
            if row.get("code-location") or row.get("test-ref"):
                vanished.append(row["rule-id"])
            continue
        losses += witness_losses(row, now, schema, respelled)
    return losses, vanished


def compare_revisions(before: list[dict[str, Any]], after: list[dict[str, Any]], before_rev: str,
                      after_rev: str | None, schema) -> tuple[list[WitnessLoss], list[str]]:
    """`compare`, with the re-spellings of the change from `before_rev` to `after_rev` (None: the working tree).
    Git's rename record is read only when a file-backed test-ref was lost, so a change that moved nothing pays
    nothing for it."""
    losses, vanished = compare(before, after, schema)
    before_schema = schema_at(before_rev) or schema
    if any(not l.excused and l.field == "test-ref" and before_schema.ref_path(l.ref) for l in losses):
        after_schema = (schema_at(after_rev) or schema) if after_rev else schema
        losses, vanished = compare(before, after, schema,
                                   respellings(before_rev, after_rev, before_schema, after_schema))
    return losses, vanished


def default_base() -> str:
    for ref in ("origin/main", "main"):
        try:
            return _git("merge-base", "HEAD", ref).strip()
        except subprocess.CalledProcessError:
            continue
    raise SystemExit("⛔ no base: neither origin/main nor main is a ref here — pass --base REF")


def check(base: str, schema) -> int:
    before = rows_at(base)
    if before is None:
        print(f"⛔ {INVENTORY_REL} does not exist at {base} — nothing to compare against")
        return 2
    losses, vanished = compare_revisions(before, load_inventory(), base, None, schema)
    unexcused = [l for l in losses if not l.excused]
    retired = [l for l in losses if l.retired]
    resited = [l for l in losses if l.resited and not l.retired]
    respelled = [l for l in losses if l.respelled and not (l.retired or l.resited)]
    for l in retired:
        print(f"  ⓘ RETIRED  {l.rule_id} {l.field}: {l.ref}")
    for l in resited:
        print(f"  ⓘ RE-SITED {l.rule_id} {l.field}: {l.ref}  (the verdict changed — the record restates it)")
    for l in unexcused:
        print(f"  ⛔ LOST     {l.rule_id} {l.field}: {l.ref}")
    if respelled:
        print(f"  ⓘ RE-SPELLED {len(respelled)} test-ref(s) on {len({l.rule_id for l in respelled})} row(s): the "
              "same files, moved (git's rename record), cited under their new names")
    if vanished:
        print(f"  ⓘ {len(vanished)} witnessed row(s) no longer in the inventory (catalog-owned, not gated): "
              + ", ".join(vanished[:10]) + (" …" if len(vanished) > 10 else ""))
    print(f"=== WITNESS LOSS vs {base[:12]}: {len(unexcused)} unexcused, {len(retired)} retired, "
          f"{len(resited)} re-sited, {len(respelled)} re-spelled — {'RED' if unexcused else 'GREEN'} ===")
    return 1 if unexcused else 0


def history(since: str | None, schema) -> tuple[list[tuple[str, str, WitnessLoss]], Counter[str]]:
    """Walk every commit that touched the inventory (oldest first) and diff it against its first parent.

    Each loss comes back as (commit label, the row's verdict AFTER that commit, the loss): the verdict is what
    decides whether a code-location loss is still owed back (`owed_back`).
    """
    rng = [f"{since}..HEAD"] if since else []
    shas = _git("log", "--reverse", "--first-parent", "--format=%H %ad %s", "--date=short", *rng, "--",
                INVENTORY_REL).splitlines()
    found: list[tuple[str, str, WitnessLoss]] = []
    vanished: Counter[str] = Counter()
    for line in shas:
        sha, date, *subject = line.split(" ", 2)
        after = rows_at(sha)
        before = rows_at(f"{sha}^")
        if after is None or before is None:
            continue
        losses, gone = compare_revisions(before, after, f"{sha}^", sha, schema)
        verdict_after = {r["rule-id"]: r.get("verdict", "") for r in after}
        label = f"{sha[:9]} {date} {(subject[0] if subject else '')[:70]}"
        found += [(label, verdict_after[l.rule_id], l) for l in losses]
        vanished[label] += len(gone)
    return found, vanished


def owed_back(found: list[tuple[str, str, WitnessLoss]], current: dict[str, dict[str, Any]],
              schema) -> dict[str, dict[str, set[str]]]:
    """The witnesses history removed that PB959's write rule would have KEPT, and that the row still lacks.

    That is the history REPLAYED under the current rule, never a blanket undo:
      · a retirement or a re-site was excused when it happened and stays excused;
      · a silently lost TEST-REF is owed back — a test that exercises the rule observes it whatever the verdict;
      · a silently lost CODE-LOCATION is owed back only while the row still carries the verdict it was lost
        under: a later re-adjudication would have re-sited it anyway;
      · a removal the SCHEMA compelled is not owed: a reference that breaks a shape rule
        (`witness_shape_error`), and a test-ref on a row now closed on a §1.1 derivation, which may carry none.
    Whether an owed reference still RESOLVES is the battery gate's question, not this one's.
    """
    owed: dict[str, dict[str, set[str]]] = {}
    for _, verdict_then, l in found:
        row = current.get(l.rule_id)
        if row is None or l.excused or witness_shape_error(l.field, l.ref, schema):
            continue   # a witness removed because it broke a SHAPE rule was a correction, not a loss
        if l.field == "code-location" and row.get("verdict", "") != verdict_then:
            continue
        if l.field == "test-ref" and (row.get("derivation") or "").strip():
            continue   # a row closed on a §1.1 DERIVATION may carry no spec-derived test (refusal 1): compelled
        if l.ref not in schema.split(row.get(l.field, "") or "", _sep(l.field, schema)):
            owed.setdefault(l.rule_id, {}).setdefault(l.field, set()).add(l.ref)
    return owed


def run_history(args, schema) -> int:
    found, vanished = history(args.since, schema)
    current = {r["rule-id"]: r for r in load_inventory()}
    per_commit: Counter[str] = Counter(label for label, _, _ in found)
    owed = owed_back(found, current, schema)
    kinds = Counter(("retired" if l.retired else "re-sited" if l.resited else "re-spelled" if l.respelled
                     else "lost", l.field) for *_, l in found)
    print(f"commits that removed a witness : {len(per_commit)}")
    for label, n in per_commit.most_common():
        print(f"   {n:5d}  {label}")
    print(f"witness removals in history    : {len(found)}")
    for (kind, field), n in sorted(kinds.items()):
        print(f"   {n:5d}  {kind:<9s} {field}")
    by_field = {f: sum(len(o.get(f, ())) for o in owed.values()) for f in ("code-location", "test-ref")}
    print(f"rows OWED a witness back       : {len(owed)} ({sum(by_field.values())} reference(s): "
          + ", ".join(f"{f} {n}" for f, n in by_field.items()) + ")")
    if sum(vanished.values()):
        print(f"witnessed rows that vanished   : {sum(vanished.values())} (catalog-owned, not counted above)")
    if args.restore_batch:
        # WITNESS-ONLY records (`record_verdicts.is_witness_only`): the owed witnesses and nothing else, which the
        # writer MERGES into what the row carries and which restate no verdict, editions or notes — so the batch
        # is safe to re-apply on a merged tree, in any order relative to another train's re-adjudication.
        records = []
        for rid in sorted(owed):
            rec = {"rule-id": rid}
            for field, refs in owed[rid].items():
                rec[field] = _sep(field, schema).join(sorted(refs))
            records.append(rec)
        pathlib.Path(args.restore_batch).write_text(
            json.dumps({"batch": "PB959-restore-owed-witnesses", "records": records}, indent=1,
                       ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
        print(f"wrote {len(records)} restore record(s) → {args.restore_batch}")
    return 0


def self_test(schema) -> int:
    """Prove each arm FIRES on a planted defect, and stays silent on the shapes that are not one."""
    rows = load_inventory()
    row = next(r for r in rows if len(schema.split(r.get("test-ref", ""), schema.test_ref_sep)) >= 3
               and len(schema.split(r.get("code-location", ""), schema.code_location_sep)) >= 2
               and r.get("verdict"))
    refs = schema.split(row["test-ref"], schema.test_ref_sep)
    locs = schema.split(row["code-location"], schema.code_location_sep)
    fails = 0

    def verify(name: str, ok: bool, detail: str = "") -> None:
        nonlocal fails
        fails += not ok
        print(f"  {'PASS' if ok else 'FAIL'}  {name}{detail}")

    def expect(name: str, got: list[WitnessLoss], unexcused: int, excused: int) -> None:
        u, e = sum(not l.excused for l in got), sum(l.excused for l in got)
        verify(name, (u, e) == (unexcused, excused),
               f": {u} unexcused, {e} excused (want {unexcused}, {excused})")

    narrowed = {**row, "test-ref": refs[-1], "code-location": locs[0]}
    expect("planted overwrite (PB959's shape)", witness_losses(row, narrowed, schema),
           len(refs) - 1 + len(locs) - 1, 0)
    expect("field omitted → emptied", witness_losses(row, {**row, "test-ref": ""}, schema), len(refs), 0)
    retiring = {**row, "test-ref": schema.test_ref_sep.join(refs[1:]),
                "notes": (row.get("notes", "") + f" retired-witness: {refs[0]} (self-test)").strip()}
    expect("explicit retirement is excused", witness_losses(row, retiring, schema), 0, 1)
    resite = {**narrowed, "verdict": "PARTIAL" if row["verdict"] != "PARTIAL" else "CONFORMS"}
    expect("a re-adjudication re-sites code-location, never test-ref", witness_losses(row, resite, schema),
           len(refs) - 1, len(locs) - 1)
    widened = {**row, "test-ref": row["test-ref"] + schema.test_ref_sep + "conformance:cases/0-SELF-TEST/self_test_extra"}
    expect("a widening is not a loss", witness_losses(row, widened, schema), 0, 0)
    reordered = {**row, "test-ref": schema.test_ref_sep.join(reversed(refs))}
    expect("a reordering is not a loss", witness_losses(row, reordered, schema), 0, 0)
    losses, vanished = compare([row], [], schema)
    verify("a vanished row is reported apart, never as a witness loss",
           not losses and vanished == [row["rule-id"]])

    # A RE-SPELLING (kb/Work PB3177): the same test file, moved, cited under its new name.
    moved_to = "conformance:cases/0-SELF-TEST/self_test_moved"
    moved = {**row, "test-ref": schema.test_ref_sep.join([moved_to, *refs[1:]])}
    expect("a re-spelled witness (its file moved, the row cites the new name) is excused",
           witness_losses(row, moved, schema, {refs[0]: moved_to}), 0, 1)
    expect("a re-spelling the row does not cite is still a loss",
           witness_losses(row, {**row, "test-ref": schema.test_ref_sep.join(refs[1:])}, schema,
                          {refs[0]: moved_to}), 1, 0)
    expect("a re-spelling never excuses a code-location",
           witness_losses(row, {**row, "code-location": schema.code_location_sep.join(locs[1:])}, schema,
                          {locs[0]: locs[1]}), 1, 0)
    owed = owed_back([("t", row["verdict"], l) for l in witness_losses(row, moved, schema, {refs[0]: moved_to})],
                     {row["rule-id"]: moved}, schema).get(row["rule-id"], {})
    verify("history replay owes back no re-spelled witness", not owed, f": owed {owed}")
    raw = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
    raw["test-ref"]["forms"]["conformance"]["path-template"] = "tests/conformance/{0}.cob"   # the tree before S2
    old = Schema(raw)
    z = "\0".join(["R087", "tests/conformance/85/foo.cob", "conformance-suite/cases/7.2.3-COPY/foo_85.cob",
                   "R100", "tests/conformance/85/lib.cpy", "conformance-suite/cases/7.2.3-COPY/lib.cpy",
                   "M", "tests/nist/programs/NC101A.cob", "D", "tests/conformance/85/gone.cob",
                   "C075", "tests/nist/programs/NC102A.cob", "tests/nist/programs/NC102B.cob", ""])
    got = respelled_from_name_status(z, old, schema)
    verify("git's rename record re-spells each renamed witness file, each side by its own tree's schema (a "
           "modified, deleted or copied file, or one no form names, re-spells nothing)",
           got == {"conformance:85/foo": "conformance:cases/7.2.3-COPY/foo_85"}, f": {got}")
    verify("a file-backed form's ref and its path are inverses; an xUnit form names no file",
           schema.ref_for_path(schema.ref_path(moved_to) or "") == moved_to
           and schema.ref_path("unit:SomeTests.SomeMethod") is None
           and "conformance-suite" in schema.ref_roots, f": {schema.ref_roots}")

    rid = row["rule-id"]
    owed = owed_back([("t", row["verdict"], l) for l in witness_losses(row, narrowed, schema)],
                     {rid: narrowed}, schema).get(rid, {})
    verify("history replay owes back every silently lost witness",
           owed.get("test-ref") == set(refs[:-1]) and owed.get("code-location") == set(locs[1:]))
    owed = owed_back([("t", resite["verdict"], l) for l in witness_losses(row, resite, schema)],
                     {rid: resite}, schema).get(rid, {})
    verify("history replay leaves a re-site re-sited", owed.get("test-ref") == set(refs[:-1])
           and "code-location" not in owed)
    later = {**narrowed, "verdict": resite["verdict"]}
    owed = owed_back([("t", row["verdict"], l) for l in witness_losses(row, narrowed, schema)],
                     {rid: later}, schema).get(rid, {})
    verify("a code-location lost under a verdict since re-adjudicated is not owed",
           "code-location" not in owed and owed.get("test-ref") == set(refs[:-1]))

    # THE WRITER, asked the same questions: `record_verdicts.merged` is the write rule, so each planted batch
    # shape must come out of it with exactly the losses the auditor above would excuse — and no others.
    from record_verdicts import merged
    stmt = {k: row[k] for k in ("rule-id", "verdict", "editions", "notes") if row.get(k)}

    def written(rec: dict[str, str]) -> dict[str, Any]:
        return {**row, **merged(row, rec, schema)}

    expect("writer: a batch re-typing ONE witness per field (PB959's batch) loses nothing",
           witness_losses(row, written({**stmt, "test-ref": refs[-1], "code-location": locs[0]}), schema), 0, 0)
    expect("writer: a batch OMITTING both witness fields loses nothing",
           witness_losses(row, written(stmt), schema), 0, 0)
    got = written({**stmt, "retire-witnesses": refs[0], "retire-reason": "self-test"})
    expect("writer: a named retirement is the only loss, and it is marked", witness_losses(row, got, schema), 0, 1)
    new_verdict = "PARTIAL" if row["verdict"] != "PARTIAL" else "CONFORMS"
    got = written({**stmt, "verdict": new_verdict, "code-location": locs[0]})
    expect("writer: a re-adjudication naming a site re-sites code-location only",
           witness_losses(row, got, schema), 0, len(locs) - 1)
    got = written({**stmt, "verdict": new_verdict})
    expect("writer: a re-adjudication naming NO site keeps the row's", witness_losses(row, got, schema), 0, 0)
    got = written({"rule-id": row["rule-id"], "test-ref": "conformance:cases/0-SELF-TEST/self_test_extra",
                   "code-location": locs[0]})
    verify("writer: a witness-only record adds its witness and restates nothing",
           not witness_losses(row, got, schema) and "conformance:cases/0-SELF-TEST/self_test_extra" in got["test-ref"]
           and all(got.get(k) == row.get(k) for k in ("verdict", "editions", "notes", "derivation")))
    print(f"=== SELF-TEST: {'GREEN' if not fails else f'RED ({fails})'} ===")
    return 1 if fails else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--check", action="store_true",
                      help="gate the working tree against --base (default: the merge-base with main)")
    mode.add_argument("--history", action="store_true", help="report every witness removal in git history")
    mode.add_argument("--self-test", action="store_true", help="prove the check fires on a planted loss")
    ap.add_argument("--base", help="the revision --check compares against")
    ap.add_argument("--since", help="--history: only commits after this revision")
    ap.add_argument("--restore-batch", help="--history: write a record_verdicts batch of every witness owed back")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    schema = load_schema()
    if args.self_test:
        return self_test(schema)
    if args.history:
        return run_history(args, schema)
    return check(args.base or default_base(), schema)


if __name__ == "__main__":
    sys.exit(main())
