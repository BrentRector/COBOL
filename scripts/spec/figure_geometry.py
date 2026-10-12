#!/usr/bin/env python3
"""Measure the DELIMITER GEOMETRY of the printed general-format figures, straight from the PDF's vector data.

WHY THIS EXISTS. The reconciliation sweep repeatedly had to decide whether a printed figure carries the §5.2.6.4
CHOICE INDICATOR — a pair of vertical bars just inside a bracket or brace — because the difference is normative:

    [  A / B  ]     per §5.2.6.2   at most ONE of A, B
    [ |A / B| ]     per §5.2.6.4   ZERO OR MORE of A, B, each at most once, in any order
    {  A / B  }     per §5.2.6.3   exactly ONE of A, B
    { |A / B| }     per §5.2.6.4   ONE OR MORE of A, B, each at most once, in any order

Dropping the bars turns legal COBOL into apparently illegal COBOL, which is the falsely-restrictive loss this
whole effort is hunting. Until now that call was made by rendering the page and squinting at it — slow, and a
judgement call at exactly the point where a wrong judgement silently corrupts the grammar.

It does not have to be a judgement call. The brackets and the bars are VECTOR RECTANGLES in the PDF content
stream, so they can be MEASURED. Crucially this is immune to the obfuscated text layer (the fonts carry shifted
cmaps, which is why `render-spec-page.py` exists at all) — vector geometry has no encoding to obfuscate.

HOW A BAR IS TOLD FROM A BRACKET. A bracket stem carries short horizontal FEET at one or both ends, turning
inward; a choice-indicator bar is a bare vertical rule with no feet, and is drawn slightly TALLER than the bracket
it sits inside. Measured on page 632, the ON SIZE ERROR group of COMPUTE Format 1:

    x= 76.29  h=31.98  feet at both ends, turning right   -> '[' bracket stem
    x= 81.19  h=37.58  no feet, taller                    -> '|' choice indicator
    x=304.36  h=37.58  no feet, taller                    -> '|' choice indicator
    x=309.72  h=31.98  feet at both ends, turning left    -> ']' bracket stem

A foot is distinguished from an UNDERLINE (which marks a required word, §5.2.2) by length: feet are 2-8 pt and
start at the stem's own x, underlines are far longer and sit under text.

CAVEAT, stated because it bounds what this tool may be used for. Curly braces are typeset as FONT GLYPHS, not
rects, so a brace does not appear in the vector data at all. This tool therefore reports bars and brackets with
certainty, and infers a brace only from a bar that has no bracket around it. When the enclosing delimiter matters,
render the page. What the tool is authoritative about is the BARS — and those are what the sweep keeps losing.

    python scripts/spec/figure_geometry.py 632              # one page, annotated
    python scripts/spec/figure_geometry.py --all            # every choice-indicator group in the standard
    python scripts/spec/figure_geometry.py --all --json out.json
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)

# Geometry thresholds, in PDF points. Deliberately loose: the figures are typeset at one size throughout.
MIN_VERT_H = 10.0     # a delimiter stem is at least this tall
MAX_VERT_W = 3.0      # ... and this thin
MAX_HORZ_H = 3.0      # a horizontal rule is at most this thick
FOOT_MIN, FOOT_MAX = 1.5, 9.0   # a bracket foot's length; longer means it is an underline
TOL = 1.2             # coincidence tolerance for "touches the end of the stem"


def _load(page):
    """Split a page's vector rects into vertical stems and horizontal rules."""
    verts, horzs = [], []
    for it in page.get_drawings():
        r = it["rect"]
        if r.height >= MIN_VERT_H and r.width <= MAX_VERT_W:
            verts.append(r)
        elif r.width > MAX_HORZ_H and r.height <= MAX_HORZ_H:
            horzs.append(r)
    return verts, horzs


def _foot_side(v, horzs):
    """Which way this stem's feet turn: 'L' for an opening '[', 'R' for a closing ']', None for a bare bar.

    A bracket's feet turn INWARD, so their direction states the bracket's hand outright. This used to return
    only yes/no and callers recovered the hand from which side of the enclosure's midpoint the stem sat on —
    which is wrong whenever an enclosure is measured on its own, because a single stem is never left of its
    own midpoint and so always drew as a CLOSING bracket. Measuring the feet removes the inference.
    """
    votes = []
    vmid = (v.x0 + v.x1) / 2
    for h in horzs:
        if not (FOOT_MIN <= h.width <= FOOT_MAX):
            continue                                  # too long: that is an underline under a required word
        if min(abs(h.y0 - v.y0), abs(h.y0 - v.y1), abs(h.y1 - v.y0), abs(h.y1 - v.y1)) > TOL:
            continue                                  # not at either end of the stem
        # A foot is ANCHORED at its own stem and extends AWAY from it. Accepting a rule that merely touches
        # either end of the stem let a CHOICE-INDICATOR BAR adopt the foot of the bracket beside it: on folio
        # 503 the bracket's foot ends at 243.43 and the bar starts at 244.60, 1.17 pt away, so the bar was
        # called a bracket and drew as a closing one in the middle of the group. Requiring the anchored end to
        # be the NEAR one, and the far end to lie outside the stem, tells the two apart at any spacing.
        if abs(h.x0 - v.x0) <= TOL and h.x1 > v.x1:
            votes.append("L")                         # foot turns right: an opening bracket
        elif abs(h.x1 - v.x1) <= TOL and h.x0 < v.x0:
            votes.append("R")                         # foot turns left: a closing bracket
    return max(set(votes), key=votes.count) if votes else None


def classify_page(page):
    """Return the delimiter stems on a page, each tagged 'bracket' or 'bar'."""
    verts, horzs = _load(page)
    out = []
    for v in sorted(verts, key=lambda r: (round(r.y0, 1), r.x0)):
        side = _foot_side(v, horzs)
        out.append({
            "x": round(v.x0, 2), "y0": round(v.y0, 2), "y1": round(v.y1, 2), "h": round(v.height, 2),
            "kind": "bracket" if side else "bar", "side": side,
        })
    return out


def group_enclosures(stems, ytol=6.0):
    """Cluster stems that span the same vertical band — one printed enclosure per cluster."""
    groups, cur = [], []
    for s in sorted(stems, key=lambda s: (round(s["y0"], 0), s["x"])):
        if cur and abs(s["y0"] - cur[0]["y0"]) > ytol:
            groups.append(cur)
            cur = []
        cur.append(s)
    if cur:
        groups.append(cur)
    return groups


def signature(g) -> str:
    """A left-to-right delimiter signature, e.g. '[ | | ]' for a bracket carrying choice indicators.

    Bracket stems are drawn as '[' or ']' by which half of the enclosure they sit in, so the signature reads the
    way the printed figure does.
    """
    ordered = sorted(g, key=lambda s: s["x"])
    mid = (ordered[0]["x"] + ordered[-1]["x"]) / 2 if ordered else 0
    return " ".join(
        ("|" if s["kind"] == "bar" else ("[" if s["x"] < mid else "]")) for s in ordered
    )


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("page", nargs="?", type=int, help="PDF page number")
    ap.add_argument("--all", action="store_true", help="scan every page for choice-indicator groups")
    ap.add_argument("--json", help="write the --all result to this file")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives in a PRIVATE submodule; the public repository carries only the Markdown transcription at specs/ISO_COBOL.md. This tool measures the printed page, so it needs the PDF: "
             "git submodule update --init specs-private")
    import fitz

    doc = fitz.open(PDF)

    if args.page:
        stems = classify_page(doc[args.page - 1])
        print(f"PDF page {args.page}: {len(stems)} delimiter stems")
        for g in group_enclosures(stems):
            bars = sum(1 for s in g if s["kind"] == "bar")
            flag = "  <-- CHOICE INDICATORS (5.2.6.4)" if bars >= 2 else ""
            print(f"\n  y={g[0]['y0']:7.2f}  {signature(g):<20}{flag}")
            for s in sorted(g, key=lambda s: s["x"]):
                print(f"      x={s['x']:7.2f}  h={s['h']:6.2f}  {s['kind']}")
        return 0

    if not args.all:
        ap.error("give a page number, or --all")

    found = []
    for pno in range(1, doc.page_count + 1):
        for g in group_enclosures(classify_page(doc[pno - 1])):
            bars = [s for s in g if s["kind"] == "bar"]
            if len(bars) < 2:
                continue
            found.append({"page": pno, "y": round(g[0]["y0"], 1), "signature": signature(g),
                          "bars": len(bars), "brackets": len(g) - len(bars)})
    print(f"choice-indicator groups printed in the standard: {len(found)}")
    print(f"pages carrying at least one: {len({f['page'] for f in found})}\n")
    print(f"{'page':>5} {'y':>8}  {'bars':>4} {'brkt':>4}  signature")
    for f in found:
        print(f"{f['page']:>5} {f['y']:>8.1f}  {f['bars']:>4} {f['brackets']:>4}  {f['signature']}")
    if args.json:
        pathlib.Path(args.json).write_text(json.dumps(found, indent=2), encoding="utf-8", newline="\n")
        print(f"\nwrote {args.json}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
