#!/usr/bin/env python3
"""Assemble the spec-reconciliation ledger from whatever agent files exist on disk.

Every compare and verify agent writes its own JSON file under
docs/rearchitecture/spec-reconciliation/ BEFORE returning, so the work survives a rate limit, a session limit, or
a stopped workflow. This merges those files into a ledger and a human-readable report.

Safe to run MID-RUN: it reports partial coverage rather than failing, so progress is visible while agents are still
working, and nothing has to be re-derived after an interruption.

    python scripts/spec/merge_reconciliation.py                     # merge + report
    python scripts/spec/merge_reconciliation.py --expect 173        # also report sweep coverage vs a target
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys
from collections import Counter

REPO = pathlib.Path(__file__).resolve().parents[2]
DIR = REPO / "docs" / "rearchitecture" / "spec-reconciliation"
# The 455 per-agent files live in a subfolder: they are bulk data, not vault content, and having them at
# the top level buried LEDGER/REPORT/REPAIR-PLAN — the three things a human actually opens — and forced an
# Obsidian exclusion that hid the report too.
AGENTS = DIR / "agents"
LEDGER = DIR / "LEDGER.json"
REPORT = DIR / "REPORT.md"

SEV_ORDER = {"normative": 0, "structural": 1, "cosmetic": 2}


def load(pattern: str) -> list[tuple[pathlib.Path, dict]]:
    out = []
    for p in sorted(list(AGENTS.glob(pattern)) + list(DIR.glob(pattern))):
        try:
            out.append((p, json.loads(p.read_text(encoding="utf-8"))))
        except Exception as exc:  # noqa: BLE001 - a malformed agent file must not lose the others
            print(f"  ! unreadable, skipped: {p.name} ({exc})")
    return out


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--expect", type=int, default=0, help="expected page count, to report coverage")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    if not DIR.exists():
        sys.exit(f"nothing to merge: {DIR} does not exist")

    compares = load("compare-*.json")
    verifies = load("verify-*.json")

    pages_swept: set[int] = set()
    claims: list[dict] = []
    for _, doc in compares:
        pages_swept.update(doc.get("pages_checked") or doc.get("batch") or [])
        for f in doc.get("findings") or []:
            claims.append(f)

    # Match each claim to its verdict. Keying on (page, kind) ALONE is wrong: a page can carry two findings of the
    # same kind (p286 has two incorrect-figure claims), and a single-slot index silently attaches one verdict to
    # the wrong claim while leaving the other unverified. Verify files carry the CLAIM, so match on its text
    # first, then fall back to consuming same-(page, kind) verdicts in filename order.
    by_claim: dict[tuple, dict] = {}
    pool: dict[tuple, list[dict]] = {}
    for _, doc in verifies:
        key = (doc.get("page"), doc.get("kind"))
        claim = doc.get("claim") or {}
        if pdf := (claim.get("pdf_says") or "").strip()[:120]:
            by_claim[(doc.get("page"), pdf)] = doc.get("verdict") or {}
        pool.setdefault(key, []).append(doc.get("verdict") or {})

    rows = []
    for c in claims:
        v = by_claim.get((c.get("page"), (c.get("pdf_says") or "").strip()[:120]))
        if v is None:
            bucket = pool.get((c.get("page"), c.get("kind")))
            v = bucket.pop(0) if bucket else None
        rows.append({
            **c,
            "verified": v is not None,
            "real": (v or {}).get("real"),
            "verifier": (v or {}).get("corrected_description") or (v or {}).get("reasoning", ""),
        })
    rows.sort(key=lambda r: (SEV_ORDER.get(r.get("severity"), 9), r.get("page", 0)))

    confirmed = [r for r in rows if r["real"] is True]
    refuted = [r for r in rows if r["real"] is False]
    unverified = [r for r in rows if not r["verified"]]

    print(f"agent files      : {len(compares)} compare · {len(verifies)} verify")
    print(f"pages swept      : {len(pages_swept)}" + (f" of {args.expect} expected" if args.expect else ""))
    if args.expect and len(pages_swept) < args.expect:
        print(f"  ⚠ INCOMPLETE — {args.expect - len(pages_swept)} page(s) not yet swept; re-run the workflow for those")
    print(f"claims           : {len(rows)}")
    print(f"  confirmed      : {len(confirmed)}  {dict(Counter(r['severity'] for r in confirmed))}")
    print(f"  refuted        : {len(refuted)}")
    print(f"  unverified     : {len(unverified)}" + ("  ⚠ verdicts missing — do not act on these yet" if unverified else ""))

    LEDGER.write_text(json.dumps({
        "pages_swept": sorted(pages_swept),
        "compare_files": len(compares), "verify_files": len(verifies),
        "confirmed": confirmed, "refuted": refuted, "unverified": unverified,
    }, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")

    lines = ["# Spec reconciliation ledger (generated)", "",
             "> Merged from the per-agent files in this directory by `scripts/spec/merge_reconciliation.py`.",
             "> Each agent writes its own file before returning, so an interrupted run loses nothing.", "",
             f"**{len(pages_swept)} pages swept · {len(confirmed)} confirmed · {len(refuted)} refuted · "
             f"{len(unverified)} unverified**", ""]
    if confirmed:
        lines += ["## Confirmed — repair these", ""]
        for r in confirmed:
            lines += [f"### p{r['page']} · {r['kind']} · **{r['severity']}**", "",
                      f"- **PDF:** {r.get('pdf_says','')}",
                      f"- **Markdown:** {r.get('markdown_says','')}",
                      f"- **Why:** {r.get('why_it_matters','')}",
                      f"- **Repair:** {r.get('suggested_repair','—')}",
                      f"- **Verifier:** {r.get('verifier','')}", ""]
    if unverified:
        lines += ["## Unverified claims — NOT yet actionable", ""]
        for r in unverified:
            lines += [f"- p{r['page']} · {r['kind']} · {r['severity']} — {r.get('why_it_matters','')[:200]}"]
        lines.append("")
    if refuted:
        lines += ["## Refuted — deliberately NOT repaired", ""]
        for r in refuted:
            lines += [f"- p{r['page']} · {r['kind']} — {r.get('verifier','')[:240]}"]
        lines.append("")
    REPORT.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")

    print(f"\nwrote {LEDGER.relative_to(REPO)} and {REPORT.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
