#!/usr/bin/env python3
"""Emit a self-contained PDF-vs-markdown reconciliation work unit for one printed page.

The markdown is an OCR transcription of the canonical PDF. Four bugs in the rule-catalog extractor traced back to
transcription artefacts (page furniture inside rules, a heading demoted to bold, split sentences), and the owner
directed a systematic reconciliation: compare the PDF against the markdown page by page and repair the markdown.

This produces the unit of that work. For page N it writes:

    <out>/page-<N>.png        the page rendered at --dpi (default 200; 300 for dense/diagram pages)
    <out>/page-<N>.md         the markdown slice between anchor page-N and page-N+1
    <out>/page-<N>.json       {page, png, md, md_lines, has_figure, anchors}

An agent then READS the png, READS the md, and reports discrepancies. Everything it needs is on disk, so units
fan out cleanly and no agent has to re-derive the mapping (feedback_agent_dispatch: each agent its OWN input).

    python scripts/spec/page_workunit.py 95 688 --out E:/Temp/specrecon
    python scripts/spec/page_workunit.py --range 100 120 --out E:/Temp/specrecon
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC = REPO / "specs" / "ISO_COBOL.md"
RENDER = REPO / "scripts" / "render-spec-page.py"


def slice_for(lines: list[str], page: int) -> tuple[list[str], int]:
    """Markdown between <a id="page-N"></a> and the next page anchor. Returns (slice, start-line-number)."""
    start = end = None
    open_re = re.compile(rf'^<a id="page-{page}"></a>\s*$')
    next_re = re.compile(r'^<a id="page-(\d+)"></a>\s*$')
    for i, line in enumerate(lines):
        if start is None:
            if open_re.match(line):
                start = i
            continue
        if (m := next_re.match(line)) and int(m.group(1)) != page:
            end = i
            break
    if start is None:
        return [], 0
    return lines[start:end if end is not None else len(lines)], start + 1


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("pages", nargs="*", type=int)
    ap.add_argument("--range", nargs=2, type=int, metavar=("FIRST", "LAST"))
    ap.add_argument("--out", required=True)
    ap.add_argument("--dpi", type=int, default=200)
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    pages = list(args.pages)
    if args.range:
        pages += list(range(args.range[0], args.range[1] + 1))
    if not pages:
        sys.exit("no pages given")

    out = pathlib.Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    lines = SPEC.read_text(encoding="utf-8", errors="replace").splitlines()

    # Render in ONE call — PyMuPDF opens the 9 MB PDF per invocation, so batching matters at 1,261 pages.
    subprocess.run([sys.executable, str(RENDER), *[str(p) for p in pages],
                    "--dpi", str(args.dpi), "--out", str(out)],
                   check=True, cwd=str(REPO))

    made = []
    for p in pages:
        sl, at = slice_for(lines, p)
        md = out / f"page-{p}.md"
        md.write_text("\n".join(sl) + "\n", encoding="utf-8", newline="\n")
        # render-spec-page names its output spec_p<N>.png
        png = out / f"spec_p{p}.png"
        meta = {
            "page": p,
            "png": str(png),
            "md": str(md),
            "md_source_line": at,
            "md_lines": len(sl),
            "has_figure": any("Figure" in x or "figure notes" in x.lower() for x in sl),
            "anchors": [x for x in sl if x.startswith("<a id=")],
        }
        (out / f"page-{p}.json").write_text(json.dumps(meta, indent=1), encoding="utf-8", newline="\n")
        made.append(meta)
        if not sl:
            print(f"  ⚠ page {p}: NO markdown slice — anchor page-{p} not found (a missing anchor is itself a finding)")

    print(f"wrote {len(made)} work unit(s) to {out}")
    figs = sum(1 for m in made if m["has_figure"])
    empty = sum(1 for m in made if m["md_lines"] == 0)
    print(f"  {figs} with figure content · {empty} with an empty/missing slice")
    return 0


if __name__ == "__main__":
    sys.exit(main())
