#!/usr/bin/env python3
"""Normalize a Phase-B batch file into the exact shape `record_verdicts.py` accepts — reporting every change.

    python scripts/spec/normalize_batch.py scratchpad/phase-b/b5/out-*.json --in-place

⛔ WHY THIS EXISTS, AND WHY IT REPORTS. `record_verdicts.py` validates ALL-OR-NOTHING and accepts only
the adjudicated fields (`inventory_schema.ADJUDICATED`). A batch that carries anything else is rejected whole —
correctly, because a partially-applied batch is the worst outcome available (the rows that DID land look exactly
like reviewed work). But the fix must not be "delete the offending fields": an agent's `evidence` is the reason
its verdict is believable, and dropping it silently would convert a reviewed verdict into an unsourced one.

So:
  · `evidence` is FOLDED INTO `notes`, never discarded.
  · A `test-ref` that does not parse is REMOVED and the reason appended to `notes` — a non-resolving ref cannot
    close a row anyway (the C# gate `SpecTraceabilityInventoryDriftTests` enforces that), so keeping a malformed
    one only makes the batch un-mergeable. The obvious `path/To/Class.cs#Method` shape is REWRITTEN to the
    canonical `unit:` / `conformance-test:` form where the directory says which it is; anything else is dropped.
  · `editions` is RESPELLED in its one form (`Schema.spell_editions`: ascending, comma-separated, no spaces;
    kb/Work PB3172); an edition the schema lacks or names twice is left as written for the validator to refuse.
  · Every change is printed. A silent normalizer would be a second place verdicts can change without a reader.
"""
from __future__ import annotations

import argparse
import glob
import json
import pathlib
import re
import sys

#: ⛔ IMPORTED, NOT RETYPED. This set is the SAME rule `record_verdicts` enforces, and it had a second copy here
#: until 2026-09-03 — at which point adding the `derivation` field (kb/Work PB386) would have made this
#: normalizer reject, whole, every batch the writer accepts. One rule, one place.
from inventory_schema import ADJUDICATED, Schema, load_schema

ALLOWED = set(ADJUDICATED)

#: `tests/<project>/<...>/<Class>.cs#<Method>` -> the canonical form, when the project names the form.
CS_REF = re.compile(r"^tests/(?P<proj>[A-Za-z0-9_.]+)/(?:.*/)?(?P<cls>[A-Za-z0-9_]+)\.cs#(?P<m>[A-Za-z0-9_]+)$")
PROJECT_FORM = {
    "Cobol.Net.Tests.Unit": "unit",
    "Cobol.Net.Tests.Conformance": "conformance-test",
}
#: A repository path a file-backed form names (`conformance-suite/cases/<folder>/<id>.cob` -> `conformance:cases/
#: <folder>/<id>`, the SPEC-DERIVED suite form, the one that can actually CLOSE a row) is spelled by the schema's own
#: inverse of its path templates (`Schema.ref_for_path`), never by a pattern of this file's: dropping such a path
#: would remove a legitimate closing reference and leave the row open for a reason that had nothing to do with the
#: compiler, and a copy of the template here is how the corpus's old spelling outlived its tree (kb/Work PB3177).


def normalize_ref(ref: str, schema: Schema) -> tuple[str | None, str | None]:
    """(canonical-ref, why-dropped). Returns (ref, None) unchanged when already canonical.

    ⚠ AGENTS APPEND COMMENTARY TO REFS — "…#Median_Even… (CORROBORATION ONLY — does not close this row)". A
    canonical ref never contains a space, so everything from the first space on is commentary: it is split off
    and preserved in `notes` rather than making the whole ref unparseable and losing the reference with it.
    """
    ref = ref.strip()
    if not ref:
        return None, None
    head, _, tail = ref.partition(" ")
    commentary = tail.strip()

    def keep(canon: str) -> tuple[str, str | None]:
        return canon, (f"ref note: {commentary}" if commentary else None)

    if head.partition(":")[0] in schema.test_ref_forms:
        return keep(head)
    norm = head.replace("\\", "/")
    m = CS_REF.match(norm)
    if m and m.group("proj") in PROJECT_FORM:
        return keep(f"{PROJECT_FORM[m.group('proj')]}:{m.group('cls')}.{m.group('m')}")
    if (canon := schema.ref_for_path(norm if norm.endswith(".cob") else norm + ".cob")) is not None:
        return keep(canon)
    return None, f"test-ref {ref!r} did not parse and was removed by normalize_batch"


def normalize(doc: dict, name: str, log: list[str], schema: Schema) -> dict:
    out = []
    for i, rec in enumerate(doc.get("records", [])):
        rec = dict(rec)
        notes = (rec.get("notes") or "").strip()

        ev = rec.pop("evidence", None)
        if ev:
            ev = str(ev).strip()
            notes = f"{notes} EVIDENCE: {ev}".strip() if notes else f"EVIDENCE: {ev}"
            log.append(f"  {name}[{i}] {rec.get('rule-id','?')}: folded `evidence` into notes")

        refs, dropped = [], []
        prev_class: str | None = None            # for a bare `#Method` continuation — see below
        for part in (rec.get("test-ref") or "").split(";"):
            # ⚠ A BARE `#Method` INHERITS THE PRECEDING REF'S CLASS. Agents write
            # `tests/.../XTests.cs#A; #B` meaning two methods of ONE class, and dropping the second loses a
            # real reference. The inference is safe because it is CHECKED: the battery gate
            # SpecTraceabilityInventoryDriftTests resolves every test-ref against the working tree, so a wrong
            # guess fails loudly instead of closing a row on a test that does not exist.
            stripped = part.strip()
            if stripped.startswith("#") and prev_class:
                part = f"{prev_class}.{stripped[1:].split()[0]}"
                log.append(f"  {name}[{i}] {rec.get('rule-id','?')}: bare {stripped!r} -> {part!r} "
                           f"(class inherited from the preceding ref)")
            canon, why = normalize_ref(part, schema)
            if canon and ":" in canon:
                prev_class = canon.rsplit(".", 1)[0]
            if canon:
                if canon != part.strip():
                    log.append(f"  {name}[{i}] {rec.get('rule-id','?')}: test-ref {part.strip()!r} -> {canon!r}")
                refs.append(canon)
            elif why:
                dropped.append(why)
                log.append(f"  {name}[{i}] {rec.get('rule-id','?')}: {why}")
        rec["test-ref"] = "; ".join(refs)
        if dropped:
            notes = f"{notes} [{'; '.join(dropped)}]".strip()

        # A canonical code-location is `path[#Symbol]`, joined with "; " — it never contains a space, so the
        # same commentary-stripping rule applies as for test-ref ("…#IntrinsicCatalog (the MAX row)").
        locs, loc_notes = [], []
        for part in (rec.get("code-location") or "").split(";"):
            part = part.strip()
            if not part:
                continue
            head, _, tail = part.partition(" ")
            if tail.strip():
                loc_notes.append(f"code-location note: {tail.strip()}")
                log.append(f"  {name}[{i}] {rec.get('rule-id','?')}: stripped commentary from code-location "
                           f"{part!r} -> {head!r}")
            # ⚠ `Foo.cs#Foo.Bar` -> `Foo.cs#Bar`. Agents qualify a member with its class, but the FILE already
            # names the class and the battery gate resolves the symbol by searching that file — so the
            # qualified form is never found and the row fails EveryCodeLocation_ResolvesInTheTree. Only the
            # redundant leading class is removed, and only when it matches the file's own name.
            path, sep, sym = head.partition("#")
            if sep and "." in sym:
                stem = pathlib.PurePosixPath(path.replace("\\", "/")).stem
                if sym.startswith(stem + "."):
                    fixed = f"{path}#{sym[len(stem) + 1:]}"
                    log.append(f"  {name}[{i}] {rec.get('rule-id','?')}: code-location {head!r} -> {fixed!r} "
                               f"(the file already names the class)")
                    head = fixed
            locs.append(head)
        if locs:
            rec["code-location"] = "; ".join(locs)
        if loc_notes:
            notes = f"{notes} [{'; '.join(loc_notes)}]".strip()

        # `editions` in its ONE spelling (kb/Work PB3172), which record_verdicts now refuses any other: agents
        # write "85, 2002, 2014, 2023", an out-of-order list, or a JSON list. Spelled by the schema's own writer;
        # an edition the schema lacks or names twice is left as written, for the validator to name.
        if ed := rec.get("editions"):
            names = [str(e) for e in ed] if isinstance(ed, list) else schema.split(str(ed), schema.editions_sep)
            try:
                spelled = schema.spell_editions(names)
            except ValueError as e:
                log.append(f"  {name}[{i}] {rec.get('rule-id','?')}: editions {ed!r} left as written ({e})")
            else:
                if spelled != ed:
                    log.append(f"  {name}[{i}] {rec.get('rule-id','?')}: editions {ed!r} -> {spelled!r}")
                    rec["editions"] = spelled

        rec["notes"] = notes
        for k in list(rec):
            if k != "rule-id" and k not in ALLOWED:
                log.append(f"  {name}[{i}] {rec.get('rule-id','?')}: dropped unknown field {k!r}")
                rec.pop(k)
        out.append(rec)
    return {"batch": doc.get("batch") or doc.get("subject") or name, "records": out}


def main(argv: list[str]) -> int:
    for s in (sys.stdout, sys.stderr):
        try:
            s.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("files", nargs="+")
    ap.add_argument("--in-place", action="store_true")
    a = ap.parse_args(argv)

    paths = [pathlib.Path(p) for pat in a.files for p in glob.glob(pat)]
    if not paths:
        print("!! no files matched", file=sys.stderr)
        return 2
    log: list[str] = []
    total = 0
    schema = load_schema()
    for p in sorted(paths):
        doc = json.loads(p.read_text(encoding="utf-8"))
        fixed = normalize(doc, p.name, log, schema)
        total += len(fixed["records"])
        if a.in_place:
            p.write_text(json.dumps(fixed, indent=1, ensure_ascii=False), encoding="utf-8", newline="\n")
    print(f"{len(paths)} file(s), {total} record(s); {len(log)} change(s)"
          + (" — WRITTEN" if a.in_place else " — dry run, nothing written"))
    for line in log:
        print(line)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
