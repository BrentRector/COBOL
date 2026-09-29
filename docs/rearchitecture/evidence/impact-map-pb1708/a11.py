"""PB1710 diagnosis (design review): which tests started a child process that wrote no hits file, and why.

Reads the RAW per-test contexts of a recording (`record_impact_map.py --work DIR --keep` leaves them under
`DIR/rec-<sha>-<n>/raw/contexts/*.jsonl`); pass one or more such `rec-*` directories.

Finding (dbea12428): all 172 missing children belong to 35 Unit tests, and every one of them launches a NATIVE or
non-.NET executable — `icacls` (FileAuthorityPresenceTests), `cmd.exe` / `/bin/sh` (ProcessObservationDriftTests),
`python` and `pwsh` (the script-driving drift tests). Such a child loads no instrumented assembly, so it can execute
no instrumented code and correctly writes no hits file. None is a lost write, none is in Conformance, and the retry
the first design proposed would change nothing."""
import collections, json, sys
from pathlib import Path

for d in map(Path, sys.argv[1:]):
    tot = collections.Counter(); miss = collections.Counter(); rows = collections.Counter(); allrows = collections.Counter()
    for f in (d / "raw" / "contexts").glob("*.jsonl"):
        asm = f.name.split(".")[3]
        for line in open(f, encoding="utf-8"):
            r = json.loads(line)
            if r.get("children"):
                k = (asm, r["name"])
                tot[k] += r["children"]; miss[k] += r.get("missing_children", 0); allrows[k] += 1
                if r.get("missing_children"):
                    rows[k] += 1
    by_asm = collections.Counter()
    for (asm, _), v in miss.items():
        by_asm[asm] += v
    print(f"== {d.name}: children started {sum(tot.values())}, without a hits file {sum(miss.values())}, "
          f"by assembly {dict(by_asm)}, tests {sum(1 for v in miss.values() if v)}")
    for k, v in miss.most_common():
        if v:
            print(f"  {v:3d} of {tot[k]:3d} children  ({rows[k]} of {allrows[k]} rows)  {k[0]}  {k[1]}")
