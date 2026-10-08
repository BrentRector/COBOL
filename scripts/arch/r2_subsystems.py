#!/usr/bin/env python3
"""r2_subsystems.py — the R2 review fleet's subsystem table: each subsystem a COMPUTED file set, the sets a PARTITION of
the reviewed code, and each set cut into shards a reviewer can read whole (kb/Work PB2558;
docs/rearchitecture/DESIGN-architecture-review.md §3 R2; the R2 adversarial review's B1 and B2).

WHY. The fleet's first brief named sixteen subsystems as prose ("data binding", "reference resolution", ...) and
nothing mapped a name to files: `ReferenceResolver.cs` sat in two of them, the runtime's collation, Unicode and
globalization code (about 6,000 lines) and the source generator sat in none, and the binding family (about 76,000
lines) was one reviewer's whole share, so most of it would never have been read and no record would have said so.
The same lesson as §8.7: a file set is computed, never listed.

WHAT.
  * DOMAIN — the files under review: every tracked authored code file (C#, grammar, project, script, CI and agent
    tooling); EXCLUDED names each kind left out and why (generated files, data, the public-skills submodule).
  * TABLE — subsystem key → name → kb/Work `area` (what a defect the fleet finds there is filed under) → `include`
    and `exclude` globs (`**` any folders, `*` within one name, `{a,b}`
    alternatives). A domain file belongs to a subsystem when it matches one of its includes and none of its excludes.
  * THE PARTITION CHECK — every domain file belongs to EXACTLY one subsystem: a file in none is a HOLE, a file in two
    an OVERLAP, and either fails the run (exit 1). The drift test runs it on the committed tree, so a new folder no
    subsystem owns is red until the table owns it.
  * SHARDS — each subsystem's files, sorted by path, packed greedily into shards of at most `--max-lines` physical
    lines (default 6,000), balanced (the fewest shards the cap allows, each about the subsystem's lines divided by
    that count, with 15 % slack); a single file larger than the cap is its own shard. Lines are the blob's physical lines at
    the pinned commit: the census `lines` column is per TYPE and a partial type's files fall in different shards, so
    a file-level size is the only one that adds up. Each shard carries the census god-class rows whose files it holds.
  * THE DESIGN RENDER — design §3 R2's subsystem table is rendered from TABLE (`--render-design`) between the markers
    `<!-- r2-subsystems:begin -->` and `<!-- r2-subsystems:end -->`; `--check` fails when the document differs.

Usage:
    python scripts/arch/r2_subsystems.py --check                       # partition + design text, committed tree (HEAD)
    python scripts/arch/r2_subsystems.py --commit <sha> --out <dir>    # shards.json + one file list per shard
    python scripts/arch/r2_subsystems.py --render-design               # print the design table
    python scripts/arch/r2_subsystems.py --write-design                # rewrite the design block from TABLE
    python scripts/arch/r2_subsystems.py --self-test

The last line printed is the verdict: `=== R2 SUBSYSTEMS: ... ===`.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
DESIGN = Path("docs/rearchitecture/DESIGN-architecture-review.md")
CENSUS_DIR = Path("docs/rearchitecture/evidence/arch-census")
BEGIN, END = "<!-- r2-subsystems:begin -->", "<!-- r2-subsystems:end -->"
MAX_LINES = 6000

# ── THE DOMAIN: what the review reads ────────────────────────────────────────────────────────────────────────────
DOMAIN = [
    "src/**/*.{cs,g4,ps1,csproj}",
    "tests/**/*.{cs,csproj,props,py,ps1,sh}",
    "scripts/**/*.{py,ps1,sh,js,mjs,cs,yml}",
    "tools/**/*.{cs,csproj,targets,sh,py,ps1}",
    ".github/**/*.yml",
    ".claude/**/*.{py,js,ps1}",
    "Directory.*.props",
    "Cobol.Net.sln",
]
# Each exclusion with its reason; a domain file matching one is reported as excluded, never as a hole.
EXCLUDED = [
    ("**/*.{g.cs,g.i.cs,Designer.cs}", "generated, by the census rule `generated`; reviewed through its generator"),
    ("**/Generated/**", "a build output, the ANTLR parser, never committed"),
    ("**/obj/**", "a build output"),
    ("tools/claude-skills/**", "the pinned public-skills submodule, BrentRector/claude-skills, reviewed in its own repository"),
]

# ── THE TABLE ────────────────────────────────────────────────────────────────────────────────────────────────────
C = "src/Cobol.Net.Compiler"
B = C + "/Binding"
F = "src/Cobol.Net.Frontend"
R = "src/Cobol.Net.Runtime"
TABLE = [
    {"key": "preprocessor", "area": "frontend/preprocessor", "name": "preprocessor", "include": [F + "/Preprocessor/**"]},
    {"key": "lexer-grammar", "area": "frontend/grammar", "name": "lexer and grammar", "include": [F + "/Grammar/**", F + "/*.ps1"]},
    {"key": "parser-tree", "area": "frontend/parser", "name": "parser drivers and the syntax tree",
     "include": [F + "/{Parsing,Cst,Expressions,Pipeline,Common}/**", F + "/*.csproj"]},
    {"key": "data-binding", "area": "binding/data-division", "name": "data binding",
     "include": [B + "/*.cs", B + "/Model/**", B + "/Passes/**"],
     "exclude": [B + "/ReferenceResolver*.cs", B + "/RefResolution.cs", B + "/QualifiedNameClasses.cs",
                 B + "/EcNameResolution.cs", B + "/ReportGroupResolution.cs", B + "/Oo*.cs", B + "/Prototype*.cs"]},
    {"key": "procedure-binding", "area": "binding", "name": "procedure binding", "include": [B + "/Procedure/**"]},
    {"key": "reference-resolution", "area": "binding", "name": "reference resolution",
     "include": [B + "/ReferenceResolver*.cs", B + "/RefResolution.cs", B + "/QualifiedNameClasses.cs",
                 B + "/EcNameResolution.cs", B + "/ReportGroupResolution.cs"]},
    {"key": "validation", "area": "binding/validation", "name": "validation and edition gating", "include": [C + "/Validation/**", B + "/Validation/**"]},
    {"key": "bound-tree", "area": "binding", "name": "lowering and the bound tree",
     "include": [B + "/Bound/**", "src/Cobol.Net.Compiler.SourceGen/**"]},
    {"key": "codegen", "area": "codegen", "name": "Roslyn code generation", "include": [C + "/CodeGen/**", C + "/*.{cs,csproj}"]},
    {"key": "oo", "area": "binder/oo", "name": "OO", "include": [C + "/Oo/**", B + "/Oo*.cs", B + "/Prototype*.cs"]},
    {"key": "runtime-values", "area": "runtime/values", "name": "runtime values and numerics",
     "include": [R + "/{Values,Intrinsics,Verbs}/**", R + "/*.{cs,csproj}"]},
    {"key": "runtime-text", "area": "runtime/collation", "name": "runtime collation, Unicode and globalization",
     "include": [R + "/{Collation,Unicode,Globalization}/**"]},
    {"key": "runtime-io", "area": "runtime/io", "name": "runtime I/O", "include": [R + "/IO/**"]},
    {"key": "runtime-control", "area": "runtime/control", "name": "runtime control and exceptions", "include": [R + "/{Control,Exceptions}/**"]},
    {"key": "editions-diagnostics", "area": "editions", "name": "editions and diagnostics",
     "include": ["src/Cobol.Net.Editions/**", F + "/Diagnostics/**"]},
    {"key": "cli", "area": "cli", "name": "CLI", "include": ["src/Cobol.Net.Cli/**"]},
    {"key": "tests-scripts-ci", "area": "process/tooling", "name": "tests, scripts and CI",
     "include": ["tests/**", "scripts/**", "tools/**", ".github/**", ".claude/**", "Directory.*.props", "Cobol.Net.sln"]},
]


# ── globs ────────────────────────────────────────────────────────────────────────────────────────────────────────
def glob_re(glob: str) -> re.Pattern:
    """`**/` any folders (zero or more), `**` at the end anything, `*` within one name, `{a,b}` alternatives."""
    out, i = [], 0
    while i < len(glob):
        c = glob[i]
        if glob.startswith("**/", i):
            out.append("(?:[^/]+/)*")
            i += 3
        elif glob.startswith("**", i):
            out.append(".*")
            i += 2
        elif c == "*":
            out.append("[^/]*")
            i += 1
        elif c == "{":
            j = glob.index("}", i)
            out.append("(?:" + "|".join(re.escape(a) for a in glob[i + 1:j].split(",")) + ")")
            i = j + 1
        else:
            out.append(re.escape(c))
            i += 1
    return re.compile("".join(out) + r"\Z")


def matches(path: str, globs) -> bool:
    return any(glob_re(g).match(path) for g in globs)


def domain(paths, domain_globs=DOMAIN, excluded=EXCLUDED):
    """-> (the domain files, {excluded file: reason})."""
    keep, skipped = [], {}
    for p in sorted(paths):
        if not matches(p, domain_globs):
            continue
        why = next((r for g, r in excluded if glob_re(g).match(p)), None)
        if why:
            skipped[p] = why
        else:
            keep.append(p)
    return keep, skipped


def partition(files, table=TABLE):
    """-> ({key: [files]}, holes, {file: [keys]} overlaps)."""
    sets = {s["key"]: [] for s in table}
    holes, overlaps = [], {}
    for p in files:
        owners = [s["key"] for s in table if matches(p, s["include"]) and not matches(p, s.get("exclude", []))]
        if not owners:
            holes.append(p)
        elif len(owners) > 1:
            overlaps[p] = owners
        else:
            sets[owners[0]].append(p)
    return sets, holes, overlaps


def shard(sets, lines, max_lines=MAX_LINES, table=TABLE):
    """-> list of shards, each {id, subsystem, name, files: [[path, lines]], lines}; deterministic for one tree."""
    names = {s["key"]: s["name"] for s in table}
    out = []
    for key, files in sets.items():
        total = sum(lines[p] for p in files)
        want = max(1, -(-total // max_lines))            # the fewest shards the cap allows
        cap = min(max_lines, -(-total * 115 // (100 * want)))   # balanced: about total/want each, never over the cap
        packs, cur, n = [], [], 0
        for p in sorted(files):
            k = lines[p]
            if cur and n + k > cap:
                packs.append(cur)
                cur, n = [], 0
            cur.append(p)
            n += k
        if cur:
            packs.append(cur)
        for i, pack in enumerate(packs, 1):
            out.append({"id": key if len(packs) == 1 else "%s-%d" % (key, i), "subsystem": key, "name": names[key],
                        "files": [[p, lines[p]] for p in pack], "lines": sum(lines[p] for p in pack)})
    return out


# ── the tree ─────────────────────────────────────────────────────────────────────────────────────────────────────
def tree_lines(commit: str, repo: Path = REPO):
    """-> {path: physical line count} for every blob in the commit's tree (submodules are commits, not blobs)."""
    ls = subprocess.run(["git", "-C", str(repo), "ls-tree", "-r", "-z", commit], capture_output=True, check=True).stdout
    blobs = {}
    for rec in ls.decode("utf-8").split("\0"):
        if not rec:
            continue
        meta, path = rec.split("\t", 1)
        _, typ, oid = meta.split()
        if typ == "blob":
            blobs[path] = oid
    out = {}
    if not blobs:
        return out
    proc = subprocess.run(["git", "-C", str(repo), "cat-file", "--batch"], input="\n".join(blobs.values()).encode() + b"\n",
                          capture_output=True, check=True).stdout
    pos, by_oid = 0, {}
    while pos < len(proc):
        nl = proc.index(b"\n", pos)
        oid, _, size = proc[pos:nl].decode().split()
        body = proc[nl + 1:nl + 1 + int(size)]
        by_oid[oid] = body.count(b"\n") + (1 if body and not body.endswith(b"\n") else 0)
        pos = nl + 1 + int(size) + 1
    return {p: by_oid[o] for p, o in blobs.items()}


def census_god_classes(commit: str, repo: Path = REPO):
    """-> [{type, lines, members, files}] from the census findings of `commit` (empty when there is no record)."""
    full = subprocess.run(["git", "-C", str(repo), "rev-parse", commit], capture_output=True, text=True, check=True).stdout.strip()
    fp = repo / CENSUS_DIR / (full + ".findings.json")
    if not fp.exists():
        return [], None
    doc = json.loads(fp.read_text(encoding="utf-8"))
    rows = [{"id": f["id"], "type": f["site"]["type"], "lines": f["measure"]["lines"],
             "members": f["measure"]["members"], "files": f["site"]["files"]}
            for f in doc["findings"] if f["kind"] == "god-class"]
    return rows, str(CENSUS_DIR / (full + ".json")).replace("\\", "/")


def measure(commit: str, max_lines=MAX_LINES, repo: Path = REPO):
    lines = tree_lines(commit, repo)
    files, skipped = domain(lines)
    sets, holes, overlaps = partition(files)
    shards = shard(sets, lines, max_lines)
    gods, census = census_god_classes(commit, repo)
    for s in shards:
        mine = {p for p, _ in s["files"]}
        s["god_classes"] = [{"id": g["id"], "type": g["type"], "lines": g["lines"],
                             "files_here": sorted(set(g["files"]) & mine), "files_total": len(g["files"])}
                            for g in gods if set(g["files"]) & mine]
    return {"commit": subprocess.run(["git", "-C", str(repo), "rev-parse", commit], capture_output=True, text=True,
                                     check=True).stdout.strip(),
            "census": census, "max_lines": max_lines, "domain_files": len(files), "excluded": len(skipped),
            "holes": holes, "overlaps": overlaps,
            "subsystems": {k: {"name": next(s["name"] for s in TABLE if s["key"] == k), "files": len(v),
                               "lines": sum(lines[p] for p in v),
                               "shards": [s["id"] for s in shards if s["subsystem"] == k]} for k, v in sets.items()},
            "shards": shards}


# ── the design text ──────────────────────────────────────────────────────────────────────────────────────────────
def render_design(table=TABLE):
    rows = ["| Subsystem (`key`) | Files: include | except |", "|---|---|---|"]
    for s in table:
        rows.append("| %s (`%s`) | %s | %s |" % (s["name"], s["key"], " ".join("`%s`" % g for g in s["include"]),
                                                " ".join("`%s`" % g for g in s.get("exclude", [])) or "—"))
    rows.append("")
    rows.append("Under review: " + ", ".join("`%s`" % g for g in DOMAIN) + ". Left out: " +
                "; ".join("`%s` (%s)" % (g, r) for g, r in EXCLUDED) + ".")
    return "\n".join(rows)


def design_block(text: str):
    if BEGIN not in text or END not in text:
        return None
    return text.split(BEGIN, 1)[1].split(END, 1)[0].strip("\n")


def check_design(repo: Path = REPO):
    text = (repo / DESIGN).read_text(encoding="utf-8").replace("\r\n", "\n")
    have = design_block(text)
    return have is not None and have == render_design()


def write_design(repo: Path = REPO):
    p = repo / DESIGN
    raw = p.read_bytes().decode("utf-8")
    crlf = "\r\n" in raw
    text = raw.replace("\r\n", "\n")
    if BEGIN not in text:
        raise SystemExit("the design has no %s marker" % BEGIN)
    head, rest = text.split(BEGIN, 1)
    _, tail = rest.split(END, 1)
    text = head + BEGIN + "\n" + render_design() + "\n" + END + tail
    p.write_bytes((text.replace("\n", "\r\n") if crlf else text).encode("utf-8"))


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test():
    ok = True

    def arm(name, cond):
        nonlocal ok
        print(("PASS  " if cond else "FAIL  ") + name)
        ok = ok and cond

    arm("`**/` matches zero folders", bool(glob_re("a/**/*.cs").match("a/x.cs")))
    arm("`*` stays inside one name", not glob_re("a/*.cs").match("a/b/x.cs"))
    arm("`{a,b}` alternatives", bool(glob_re("a/{X,Y}/**").match("a/Y/z.cs")) and not glob_re("a/{X,Y}/**").match("a/Z/z.cs"))
    t = [{"key": "one", "name": "one", "include": ["s/A/**"], "exclude": ["s/A/Ref*.cs"]},
         {"key": "two", "name": "two", "include": ["s/A/Ref*.cs", "s/B/**"]},
         {"key": "three", "name": "three", "include": ["s/B/x.cs"]}]
    sets, holes, over = partition(["s/A/a.cs", "s/A/RefX.cs", "s/B/y.cs", "s/C/z.cs", "s/B/x.cs"], t)
    arm("a file no subsystem owns is a HOLE", holes == ["s/C/z.cs"])
    arm("a file two subsystems own is an OVERLAP", over == {"s/B/x.cs": ["two", "three"]})
    arm("an exclude hands a file to the other subsystem", sets["one"] == ["s/A/a.cs"] and sets["two"] == ["s/A/RefX.cs", "s/B/y.cs"])
    sub = EXCLUDED[-1][0].replace("**", "planted.py")   # built, so the fleet-tooling check sees no submodule path
    keep, skipped = domain(["src/a.cs", "src/Gen.g.cs", "src/Generated/P.cs", "src/a.bin", sub],
                           ["src/**/*.cs", "tools/**/*.py"])
    arm("generated and submodule files are EXCLUDED with a reason, never holes",
        keep == ["src/a.cs"] and set(skipped) == {"src/Gen.g.cs", "src/Generated/P.cs", sub})
    sh = shard({"k": ["a", "b", "c", "d"]}, {"a": 4000, "b": 3000, "c": 9000, "d": 10}, 6000,
               [{"key": "k", "name": "k"}])
    arm("shards pack to the line cap; an oversize file is its own shard",
        [[p for p, _ in s["files"]] for s in sh] == [["a"], ["b"], ["c"], ["d"]] and [s["id"] for s in sh] == ["k-1", "k-2", "k-3", "k-4"])
    shb = shard({"k": ["a", "b", "c", "d"]}, {"a": 2000, "b": 2000, "c": 2000, "d": 1000}, 6000, [{"key": "k", "name": "k"}])
    arm("shards are balanced, not cap-then-remainder", [s["lines"] for s in shb] == [4000, 3000])
    sh1 = shard({"k": ["a", "b"]}, {"a": 10, "b": 20}, 6000, [{"key": "k", "name": "k"}])
    arm("a subsystem under the cap is one shard named by its key", [s["id"] for s in sh1] == ["k"] and sh1[0]["lines"] == 30)
    arm("the design render names every subsystem", all("`%s`" % s["key"] in render_design() for s in TABLE))
    arm("a document without the markers fails the design check", design_block("no markers") is None)
    with tempfile.TemporaryDirectory() as tmp:
        subprocess.run(["git", "-C", tmp, "init", "-q"], check=True)
        Path(tmp, "a.txt").write_bytes(b"1\n2\n3")
        Path(tmp, "b.txt").write_bytes(b"")
        subprocess.run(["git", "-C", tmp, "add", "-A"], check=True)
        subprocess.run(["git", "-C", tmp, "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-qm", "t"], check=True)
        arm("physical lines are read from the commit's blobs", tree_lines("HEAD", Path(tmp)) == {"a.txt": 3, "b.txt": 0})
    print("=== R2 SUBSYSTEMS SELF-TEST: %s ===" % ("PASS" if ok else "FAIL"))
    return 0 if ok else 1


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--check", action="store_true")
    g.add_argument("--out", type=Path)
    g.add_argument("--render-design", action="store_true")
    g.add_argument("--write-design", action="store_true")
    g.add_argument("--self-test", action="store_true")
    ap.add_argument("--commit", default="HEAD")
    ap.add_argument("--max-lines", type=int, default=MAX_LINES)
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    if a.render_design:
        print(render_design())
        return 0
    if a.write_design:
        write_design()
        print("=== R2 SUBSYSTEMS: design block rewritten ===")
        return 0
    m = measure(a.commit, a.max_lines)
    for k, v in m["subsystems"].items():
        print("%-22s %5d files %7d lines  %d shard(s)" % (k, v["files"], v["lines"], len(v["shards"])))
    for p in m["holes"]:
        print("HOLE     " + p)
    for p, ks in m["overlaps"].items():
        print("OVERLAP  %s  %s" % (p, ", ".join(ks)))
    bad = bool(m["holes"] or m["overlaps"])
    if a.check:
        design_ok = check_design()
        if not design_ok:
            print("DESIGN   %s's block between %s and %s differs from the table: run --write-design" % (DESIGN, BEGIN, END))
        print("=== R2 SUBSYSTEMS: %s (%d domain files, %d holes, %d overlaps, design %s) ===" % (
            "FAIL" if bad or not design_ok else "PASS", m["domain_files"], len(m["holes"]), len(m["overlaps"]),
            "current" if design_ok else "STALE"))
        return 1 if bad or not design_ok else 0
    if bad:
        print("=== R2 SUBSYSTEMS: FAIL (not a partition; no shards written) ===")
        return 1
    a.out.mkdir(parents=True, exist_ok=True)
    (a.out / "shards.json").write_text(json.dumps(m, indent=1), encoding="utf-8")
    for s in m["shards"]:
        (a.out / ("files-%s.txt" % s["id"])).write_text("".join(p + "\n" for p, _ in s["files"]), encoding="utf-8")
    print("=== R2 SUBSYSTEMS: WROTE %d shards over %d files to %s ===" % (len(m["shards"]), m["domain_files"], a.out))
    return 0


if __name__ == "__main__":
    sys.exit(main())
