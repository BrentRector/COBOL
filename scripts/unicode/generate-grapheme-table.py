#!/usr/bin/env python3
"""
generate-grapheme-table.py — build WiseOwl COBOL's DERIVED grapheme-cluster property table from Unicode Character
Database data (the data the UAX #29 extended-grapheme-cluster rules read).

    python scripts/unicode/generate-grapheme-table.py [--data data/unicode] [--out src/Cobol.Net.Runtime/Unicode/Segmentation/Data]

Inputs (pinned, committed under data/unicode/, Unicode License — data/unicode/LICENSE-UNICODE.txt; provenance in
data/unicode/SOURCES.md):
  GraphemeBreakProperty.txt   the Grapheme_Cluster_Break property (CR, LF, Control, Extend, ZWJ, Regional_Indicator,
                              Prepend, SpacingMark, L, V, T, LV, LVT; everything else Other)
  emoji-data.txt              Extended_Pictographic (the emoji ZWJ-sequence rule GB11)
  DerivedCoreProperties.txt   Indic_Conjunct_Break (InCB: Consonant, Extend, Linker — the conjunct rule GB9c)

Output: <out>/grapheme-break.bin   ("CNGB", u32 raw length, raw-deflate payload; format documented in
                                   GraphemeBreaker.cs and Unicode/Segmentation/README.md)
        <out>/grapheme-break.manifest.json   versions, input hashes, output hash, statistics (the drift test reads it)

Every code point gets ONE byte: bits 0–3 the Grapheme_Cluster_Break value, bit 4 Extended_Pictographic, bits 5–6 the
InCB value. Runs of equal bytes become ranges; the default byte 0 (Other, not pictographic, InCB=None) is not stored.

⚖ LEGAL: Unicode data only. Nothing here reads or embeds ISO/IEC text.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import struct
import sys
import zlib
from collections import OrderedDict

FORMAT_VERSION = 1
MAGIC = b"CNGB"            # "WiseOwl COBOL Grapheme Break"

GCB = OrderedDict([("Other", 0), ("CR", 1), ("LF", 2), ("Control", 3), ("Extend", 4), ("ZWJ", 5), ("Regional_Indicator", 6),
                   ("Prepend", 7), ("SpacingMark", 8), ("L", 9), ("V", 10), ("T", 11), ("LV", 12), ("LVT", 13)])
INCB = OrderedDict([("None", 0), ("Consonant", 1), ("Extend", 2), ("Linker", 3)])
EXTENDED_PICTOGRAPHIC = 1 << 4
INCB_SHIFT = 5

RANGE_RE = re.compile(r"^([0-9A-Fa-f]{4,6})(?:\.\.([0-9A-Fa-f]{4,6}))?\s*;\s*([^#;]+?)\s*(?:;\s*([^#]+?)\s*)?(?:#.*)?$")


def sha256(path: str) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def read_version(path: str) -> str:
    """The version stated in the file's header — '# GraphemeBreakProperty-17.0.0.txt' /
    '# DerivedCoreProperties-17.0.0.txt' on the first line, or emoji-data.txt's '# Version: 17.0' line (a two-part
    number: the emoji data version equals the Unicode major.minor; '.0' is appended) — never a build-time guess."""
    with open(path, encoding="utf-8") as f:
        head = [f.readline() for _ in range(12)]
    m = re.search(r"-(\d+\.\d+\.\d+)\.txt", head[0])
    if m:
        return m.group(1)
    for line in head:
        m = re.match(r"#\s*Version:\s*(\d+\.\d+)(?:\.(\d+))?", line)
        if m:
            return f"{m.group(1)}.{m.group(2) or '0'}"
    raise SystemExit(f"{path}: no version in the header: {head[0].strip()}")


def parse_property(path: str, prop_filter=None):
    """Lines 'lo..hi ; VALUE' or 'lo..hi ; PROP; VALUE' → [(lo, hi, value)]. With prop_filter, only lines whose second
    field is prop_filter (DerivedCoreProperties: '094D ; InCB; Linker')."""
    out = []
    with open(path, encoding="utf-8") as f:
        for raw in f:
            line = raw.strip()
            if not line or line.startswith("#"):
                continue
            m = RANGE_RE.match(line)
            if not m:
                continue
            lo = int(m.group(1), 16)
            hi = int(m.group(2), 16) if m.group(2) else lo
            f1, f2 = m.group(3).strip(), (m.group(4) or "").strip()
            if prop_filter is None:
                out.append((lo, hi, f1))
            elif f1 == prop_filter:
                out.append((lo, hi, f2))
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--data", default="data/unicode")
    ap.add_argument("--out", default="src/Cobol.Net.Runtime/Unicode/Segmentation/Data")
    args = ap.parse_args()

    p_gcb = os.path.join(args.data, "GraphemeBreakProperty.txt")
    p_emoji = os.path.join(args.data, "emoji-data.txt")
    p_dcp = os.path.join(args.data, "DerivedCoreProperties.txt")
    for p in (p_gcb, p_emoji, p_dcp):
        if not os.path.exists(p):
            raise SystemExit(f"missing input {p} — see data/unicode/SOURCES.md for the pinned URLs")
    versions = {os.path.basename(p): read_version(p) for p in (p_gcb, p_emoji, p_dcp)}
    if len(set(versions.values())) != 1:
        raise SystemExit(f"Unicode version skew between inputs: {versions}")
    version = versions[os.path.basename(p_gcb)]

    props = bytearray(0x110000)
    counts = {k: 0 for k in GCB}
    for lo, hi, value in parse_property(p_gcb):
        if value not in GCB:
            raise SystemExit(f"{p_gcb}: unknown Grapheme_Cluster_Break value {value}")
        for cp in range(lo, hi + 1):
            props[cp] = (props[cp] & ~0x0F) | GCB[value]
        counts[value] += hi - lo + 1
    ep = 0
    for lo, hi, value in parse_property(p_emoji):
        if value != "Extended_Pictographic":
            continue
        for cp in range(lo, hi + 1):
            props[cp] |= EXTENDED_PICTOGRAPHIC
        ep += hi - lo + 1
    incb_counts = {k: 0 for k in INCB}
    for lo, hi, value in parse_property(p_dcp, prop_filter="InCB"):
        if value not in INCB:
            raise SystemExit(f"{p_dcp}: unknown InCB value {value}")
        for cp in range(lo, hi + 1):
            props[cp] = (props[cp] & ~(0x03 << INCB_SHIFT)) | (INCB[value] << INCB_SHIFT)
        incb_counts[value] += hi - lo + 1

    # Runs of equal non-zero bytes → ranges.
    ranges = []
    cp = 0
    while cp < 0x110000:
        v = props[cp]
        if v == 0:
            cp += 1
            continue
        start = cp
        while cp + 1 < 0x110000 and props[cp + 1] == v:
            cp += 1
        ranges.append((start, cp, v))
        cp += 1

    body = bytearray()
    body += struct.pack("<H", FORMAT_VERSION)
    for s in (version, f"UCD {version}: GraphemeBreakProperty.txt + emoji-data.txt (Extended_Pictographic) + DerivedCoreProperties.txt (InCB)"):
        b = s.encode("utf-8")
        body += struct.pack("<B", len(b)) + b
    body += struct.pack("<I", len(ranges))
    for lo, hi, v in ranges:
        body += struct.pack("<IIB", lo, hi, v)
    compressor = zlib.compressobj(9, zlib.DEFLATED, -15)
    payload = compressor.compress(bytes(body)) + compressor.flush()
    blob = MAGIC + struct.pack("<I", len(body)) + payload

    os.makedirs(args.out, exist_ok=True)
    out_bin = os.path.join(args.out, "grapheme-break.bin")
    with open(out_bin, "wb") as f:
        f.write(blob)
    manifest = OrderedDict(
        format=FORMAT_VERSION, unicodeVersion=version,
        generator="scripts/unicode/generate-grapheme-table.py",
        inputs=OrderedDict((os.path.basename(p), sha256(p)) for p in (p_gcb, p_emoji, p_dcp)),
        outputSha256=hashlib.sha256(blob).hexdigest(),
        encoding=OrderedDict(graphemeClusterBreak=GCB, extendedPictographicBit=4, indicConjunctBreak=INCB, indicConjunctBreakShift=INCB_SHIFT),
        stats=OrderedDict(ranges=len(ranges), graphemeClusterBreak=counts, extendedPictographic=ep, indicConjunctBreak=incb_counts,
                          rawBytes=len(body), compressedBytes=len(blob)),
    )
    with open(os.path.join(args.out, "grapheme-break.manifest.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(manifest, f, indent=2)
        f.write("\n")
    print(f"wrote {out_bin}: Unicode {version}, {len(ranges)} ranges, {len(body)} raw / {len(blob)} compressed bytes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
