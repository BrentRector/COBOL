#!/usr/bin/env python3
"""FIX CLUSTERS — group the open kb/Work defects by the SOURCE FILES their code sites name, so one implementer fixes
every defect in one file (or one small set of related files) in ONE pass: one orientation, one build, one gate.

    python scripts/spec/fix_clusters.py                 # ranked clusters, human-readable
    python scripts/spec/fix_clusters.py --json out.json # the same, machine-readable (the wave builder's input)
    python scripts/spec/fix_clusters.py --max 5         # cluster size cap (default 5)

⛔ This is a GENERATED VIEW of `kb/Work/`, never a work list of its own (CLAUDE.md rule 8): it is recomputed from the
notes on every run and writes nothing into the tree.

HOW (owner 2026-09-27: "group fixes so all fixes in one source file, or one small set of related code, all get fixed
in one pass"):
1. Each open defect note (status open, kind defect, not blocked, not process-only) is read for CODE SITES: explicit
   paths (`src/…/X.cs`), `Type.Member` references and bare type names; each is resolved against the real `*.cs`
   files under `src/Cobol.Net.*` (a partial class `DataBinder.Switches.cs` is its own file; `DataBinder` alone means
   the main file). Legacy `src/CobolSharp.*` is excluded (a differential oracle, never fixed).
2. A file's WEIGHT for a note is how often the note names it; the note's PRIMARY file is its heaviest, ties broken
   toward the file FEWER notes name (the more specific site).
3. Notes sharing a primary file form a cluster; a cluster over --max splits into chunks ranked by harm, and a small
   cluster ABSORBS notes whose primary file differs but which name the cluster's file as a secondary site, up to
   the cap (the "small set of related code").
4. Clusters rank by total harm (wrong answer 8, crash 4, rejects legal 2, under-rejects 1, summed), so the fill
   order stays harm-first while each dispatch carries every co-located defect.
Notes with no resolvable code site are listed separately: they need a site found before they can be grouped.
"""
import argparse, collections, json, pathlib, re, sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
WORK = ROOT / "kb" / "Work"
HARM = {"wrong_answer": 8, "crashes": 4, "rejects_legal_source": 2, "under_rejects": 1}


def frontmatter(text):
    m = re.match(r"---\n(.*?)\n---", text, re.S)
    out = {}
    for line in (m.group(1).split("\n") if m else []):
        if ":" in line and not line.startswith((" ", "\t")):
            k, v = line.split(":", 1)
            out[k.strip()] = v.strip()
    return out


def source_index():
    """{type-or-stem: [relative path]} over the greenfield sources; partial files key on their full stem too."""
    idx = collections.defaultdict(list)
    for p in [*(ROOT / "src").rglob("*.cs"), *(ROOT / "src").rglob("*.g4")]:   # grammar files are code sites too
        rel = p.relative_to(ROOT).as_posix()
        if not rel.startswith("src/Cobol.Net.") or "/obj/" in rel or "/bin/" in rel or "/Generated/" in rel:
            continue
        stem = p.stem                      # DataBinder.Switches
        idx[stem].append(rel)
        base = stem.split(".")[0]          # DataBinder
        if stem == base:
            idx[base].insert(0, rel)       # the main file wins for a bare type name
        else:
            idx.setdefault(base, []).append(rel)
    return idx


def sites(text, idx):
    """Counter of resolved source files a note names."""
    c = collections.Counter()
    for m in re.finditer(r"src/Cobol\.Net\.[\w./-]+?\.(?:cs|g4)", text):
        c[m.group(0)] += 3                                   # an explicit path is the strongest signal
    # A bare file name — `ReferenceResolver.cs#ResolveImplCore`, `CobolLexer.g4#NAME_BODY`, `DataBinder.Constants.cs`
    # — the form notes' "**Code site.**" prose uses. Missing it left 15 notes that NAME their site unclusterable
    # (measured 2026-09-27 by the site-locator pass).
    for m in re.finditer(r"(?<![\w/.])([A-Z]\w+(?:\.[A-Z]\w+)*)\.(cs|g4)\b", text):
        stem = m.group(1)
        hit = [r for r in idx.get(stem, []) if r.endswith(f"/{stem}.{m.group(2)}")]
        if hit:
            c[hit[0]] += 3
    for m in re.finditer(r"\b([A-Z][A-Za-z0-9]+)\.([A-Z][A-Za-z0-9]+)\b", text):
        partial = f"{m.group(1)}.{m.group(2)}"
        if partial in idx and len(idx[partial]) == 1 and idx[partial][0].endswith(f"/{partial}.cs"):
            c[idx[partial][0]] += 2                          # DataBinder.Switches → that partial file
        elif m.group(1) in idx:
            c[idx[m.group(1)][0]] += 2                       # Type.Member → the type's main file
    for m in re.finditer(r"\b([A-Z][a-z]+(?:[A-Z][a-z0-9]+)+)\b", text):
        name = m.group(1)
        if name in idx and len(name) > 6:
            c[idx[name][0]] += 1
    return c


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--max", type=int, default=5)
    ap.add_argument("--json")
    ap.add_argument("--top", type=int, default=40)
    a = ap.parse_args()
    idx = source_index()
    notes = {}
    for p in sorted(WORK.glob("PB*.md")):
        t = p.read_text(encoding="utf-8", errors="replace").replace("\r\n", "\n")
        f = frontmatter(t)
        if f.get("status") != "open" or f.get("kind") != "defect":
            continue
        if f.get("blocked") == "true" or f.get("process_only") == "true":
            continue
        harm = sum(w for k, w in HARM.items() if f.get(k) == "true")
        notes[f["id"]] = {"id": f["id"], "area": f.get("area", ""), "harm": harm, "title": f.get("title", "")[:140],
                          "sites": sites(t, idx)}
    popularity = collections.Counter(fl for n in notes.values() for fl in n["sites"])
    unsited = []
    for n in notes.values():
        if not n["sites"]:
            n["primary"] = None
            unsited.append(n)
            continue
        n["primary"] = max(n["sites"], key=lambda fl: (n["sites"][fl], -popularity[fl], fl))
    by_primary = collections.defaultdict(list)
    for n in notes.values():
        if n["primary"]:
            by_primary[n["primary"]].append(n)
    clusters = []
    for fl, ns in by_primary.items():
        ns.sort(key=lambda n: -n["harm"])
        for i in range(0, len(ns), a.max):
            clusters.append({"file": fl, "notes": ns[i:i + a.max]})
    # absorb: a small cluster takes notes that name its file as a SECONDARY site, from clusters of size 1
    singles = {c["notes"][0]["id"]: c for c in clusters if len(c["notes"]) == 1}
    for c in sorted(clusters, key=lambda c: -len(c["notes"])):
        if len(c["notes"]) >= a.max or len(c["notes"]) == 1 and c["notes"][0]["id"] not in singles:
            continue
        for nid, sc in list(singles.items()):
            if len(c["notes"]) >= a.max:
                break
            n = sc["notes"][0]
            if sc is c or c["file"] not in n["sites"]:
                continue
            c["notes"].append(n)
            sc["notes"] = []
            del singles[nid]
    clusters = [c for c in clusters if c["notes"]]
    for c in clusters:
        c["harm"] = sum(n["harm"] for n in c["notes"])
        c["files"] = sorted({fl for n in c["notes"] for fl in n["sites"] if n["sites"][fl] >= 2} | {c["file"]})
    clusters.sort(key=lambda c: (-c["harm"], -len(c["notes"])))
    sizes = collections.Counter(len(c["notes"]) for c in clusters)
    print(f"{len(notes)} open actionable defects -> {len(clusters)} clusters (cap {a.max}); "
          f"sizes {dict(sorted(sizes.items()))}; {len(unsited)} with no resolvable code site")
    for c in clusters[:a.top]:
        ids = ", ".join(f"{n['id']}({n['harm']})" for n in c["notes"])
        print(f"  harm {c['harm']:3} | {c['file']} | {ids}")
    if a.json:
        out = [{"file": c["file"], "harm": c["harm"], "files": c["files"],
                "notes": [{k: n[k] for k in ("id", "area", "harm", "title")} for n in c["notes"]]} for c in clusters]
        pathlib.Path(a.json).write_text(json.dumps({"clusters": out, "unsited": [n["id"] for n in unsited]}, indent=1),
                                        encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
