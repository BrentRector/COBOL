#!/usr/bin/env python3
"""file_census_notes.py — files the architecture census's findings as kb/Work notes (kb/Work PB2119, the Delete program).

    python scripts/arch/file_census_notes.py <findings.json> --ids PB2155-PB2214[,PB2215-PB2274] [--dry-run] [--notes-dir DIR]
    python scripts/arch/file_census_notes.py --self-test

One note = one mechanism a single Delete / Unify / Move wave can take. The grouping is fixed by kind:
  dead-artifact       one note per (class, directory)
  unreachable-type /
  unreachable-family  one note per primary src file (families whose members live in the same file merge)
  clone-family        one note per family
  folder-namespace    one note per disagreement
Never filed: god-class (R1 owns the decomposition targets, kb/Work PB2118) and test-only-family / test-only-type
("only tests use it" is a judgment, reported as NEEDS-OPUS). Unknown kinds are reported, never guessed.

Idempotent: a finding whose id already appears in any note's `census_ids:` line is skipped, so the next census
files only what is new. Every number and site in a note comes from the findings file; nothing is measured here.
"""
import argparse
import json
import os
import re
import sys
import tempfile
from collections import OrderedDict

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
NOTES_DIR = os.path.join(REPO, "kb", "Work")
RECORD_DIR = "docs/rearchitecture/evidence/arch-census"

FRONTMATTER = """---
title: "{title}"
id: {nid}
kind: analysis
status: open
severity: MINOR
area: architecture
wrong_answer: false
crashes: false
silent: false
rejects_legal_source: false
under_rejects: false
process_only: true
blocked: false
blocked_by: []
cluster: ["PB2119"]
spec_refs: []
inventory_rows: []
closes_rows: []
closes_rows_reason: "behavior-neutral restructuring (Delete program PB2119, owner R69 §3); no inventory row"
tags: [cobolsharp, work, analysis]
---
"""

NOT_FILED = {"god-class": "decomposition targets come from R1 (kb/Work PB2118)",
             "test-only-family": "NEEDS-OPUS: 'only tests use it' is a judgment",
             "test-only-type": "NEEDS-OPUS: 'only tests use it' is a judgment"}
WAVE_OF_KIND = {"dead-artifact": "delete", "unreachable-type": "delete", "unreachable-family": "delete",
                "clone-family": "unify", "folder-namespace": "move-rename"}
PREFIX = {"delete": "Delete:", "unify": "Unify:", "move-rename": "Move/rename:"}


def fix_text(s):
    """The findings file carries UTF-8 text that was decoded as cp1252 once; undo that when it round-trips."""
    if not isinstance(s, str):
        return s
    try:
        return s.encode("cp1252").decode("utf-8")
    except (UnicodeEncodeError, UnicodeDecodeError):
        return s


def parse_ranges(spec):
    ids = []
    for part in spec.split(","):
        m = re.fullmatch(r"PB(\d+)-PB(\d+)", part.strip())
        if not m:
            raise SystemExit("bad --ids range: " + part)
        ids.extend("PB%d" % n for n in range(int(m.group(1)), int(m.group(2)) + 1))
    return ids


def filed_census_ids(notes_dir):
    seen = set()
    for name in os.listdir(notes_dir):
        if not name.endswith(".md"):
            continue
        with open(os.path.join(notes_dir, name), encoding="utf-8") as fh:
            for line in fh:
                if line.startswith("census_ids:"):
                    seen.update(x.strip() for x in line[len("census_ids:"):].split(",") if x.strip())
    return seen


def member_file(member):
    m = re.search(r" @ (.+?):(\d+)$", member)
    return m.group(1) if m else None


def short_type(name):
    return name.rsplit(".", 1)[-1] if name else name


def group_findings(findings):
    """-> ordered list of groups: dict(kind, wave, findings, key). Deterministic: ordered by first finding id."""
    groups = OrderedDict()
    for f in findings:
        k = f["kind"]
        if k == "dead-artifact":
            site = f["site"]
            key = ("dead-artifact", f.get("class", ""), os.path.dirname(site.split(":")[0]))
        elif k in ("unreachable-type", "unreachable-family"):
            if k == "unreachable-type":
                files = tuple(sorted(f["site"]["files"]))
            else:
                files = tuple(sorted({member_file(m) for m in f["site"]["members"]} - {None}))
            if len(files) != 1:
                key = ("unreachable-multi", f["id"])
            else:
                key = ("unreachable", files[0])
        else:
            key = (k, f["id"])
        groups.setdefault(key, []).append(f)
    return [{"key": k, "findings": v} for k, v in groups.items()]


def render(group, nid, record_path, short_sha):
    fs = group["findings"]
    k = fs[0]["kind"]
    key = group["key"]
    ids = [f["id"] for f in fs]
    wave = WAVE_OF_KIND[k]
    sites, measured, targets = [], [], []
    needs_opus = [f["id"] for f in fs if "design" in fix_text(f.get("scenario", "")).lower()]

    if key[0] == "dead-artifact":
        d = key[2] or "(repository root)"
        what = "%d dead %s artifact(s) in %s" % (len(fs), key[1], d)
        for f in fs:
            sites.append("- `%s` (%s)" % (f["site"], fix_text(f["scenario"])))
    elif key[0] == "unreachable":
        n = sum(f["measure"]["members"] for f in fs)
        types = []
        for f in fs:
            t = f["site"].get("family") or f["site"].get("type")
            if t and t not in types:
                types.append(t)
        what = "%s — %d unreferenced members of %s" % (key[1], n, ", ".join(short_type(t) for t in types))
        for f in fs:
            fam = f["site"].get("family") or f["site"].get("type")
            if "members" in f["site"]:
                for m in f["site"]["members"]:
                    sites.append("- %s: `%s`" % (fam, m))
            else:
                sites.append("- type %s (%s; %d lines, %d members)" % (fam, ", ".join(f["site"]["files"]),
                                                                     f["measure"]["lines"], f["measure"]["members"]))
    elif k == "unreachable-family":
        f = fs[0]
        what = "%s — %d unreferenced members" % (f["site"]["family"], f["measure"]["members"])
        sites.extend("- `%s`" % m for m in f["site"]["members"])
    elif k == "clone-family":
        f = fs[0]
        names = [re.search(r"\((.+)\)$", s).group(1) for s in f["site"] if re.search(r"\((.+)\)$", s)]
        mm = f["measure"]
        what = "%s — %d copies, %d tokens" % (" and ".join(names), mm["copies"], mm["tokens"])
        sites.extend("- `%s`" % s for s in f["site"])
    elif k == "folder-namespace":
        f = fs[0]
        dis = f["measure"]["disagreements"]
        left, right = dis[0].split(" != ")
        what = "%s — namespace %s != folder %s" % (f["site"], left, right)
        sites.append("- folder `%s`" % f["site"])
        sites.extend("- disagreement: `%s`" % x for x in dis)
    else:
        raise SystemExit("unhandled kind " + k)

    for f in fs:
        rule = fix_text(f.get("rule", ""))
        mm = json.dumps(f["measure"], sort_keys=True) if "measure" in f else "none recorded"
        measured.append("%s: rule \"%s\"; measure %s" % (f["id"], rule, mm))
        t = fix_text(f.get("target", ""))
        if t not in targets:
            targets.append(t)

    # the model that may take the wave
    claims_design = [f["id"] for f in fs if f["kind"] == "clone-family"]
    if needs_opus or claims_design:
        who = ("NEEDS-OPUS: the finding's scenario or target names a design claim (%s); a Sonnet wave may not decide "
               "it" % ", ".join(needs_opus or claims_design))
    else:
        who = "Sonnet (census-measured, mechanical)"

    title = "%s — %s %s (census %s at %s)" % (nid, PREFIX[wave], what, ", ".join(ids), short_sha)
    body = []
    body.append(FRONTMATTER.format(title=title.replace('"', "'"), nid=nid).rstrip("\n"))
    body.append("")
    body.append("census_ids: " + ", ".join(ids))
    body.append("")
    body.append("**Wave kind:** %s. **Model:** %s." % (wave, who))
    body.append("")
    body.append("**Sites:**")
    body.extend(sites)
    body.append("")
    body.append("**Measured how:** the R0 census record `%s` (exclusion rules and walkMisses live there). %s"
                % (record_path, " | ".join(measured)))
    body.append("")
    body.append("**The wave's contract:** every caller changes in the same change (CLAUDE.md rule 4: no alias, shim or "
                "forwarder); the drift test that pinned the thing goes with it, or a new boundary gets one; the §4 oracle "
                "(`python scripts/arch/compare_oracle.py`) proves neutrality; a deletion that changes behavior is a "
                "`kind: defect` lead for the fix lane, never a deletion (PB2119).")
    body.append("")
    body.append("**Target:** " + " | ".join(targets))
    body.append("")
    return "\n".join(body), title


def file_notes(findings_path, id_pool, notes_dir, dry_run):
    with open(findings_path, encoding="utf-8") as fh:
        findings = json.load(fh)["findings"]
    base = os.path.basename(findings_path)
    sha = base.split(".")[0]
    record_path = "%s/%s.json" % (RECORD_DIR, sha)
    already = filed_census_ids(notes_dir)
    skipped, todo = {}, []
    for f in findings:
        if f["kind"] in NOT_FILED:
            skipped.setdefault(f["kind"], []).append(f["id"])
        elif f["kind"] not in WAVE_OF_KIND:
            skipped.setdefault("UNKNOWN:" + f["kind"], []).append(f["id"])
        elif f["id"] in already:
            skipped.setdefault("already-filed", []).append(f["id"])
        else:
            todo.append(f)
    groups = group_findings(todo)
    if len(groups) > len(id_pool):
        raise SystemExit("need %d ids, only %d allocated" % (len(groups), len(id_pool)))
    written = []
    for g, nid in zip(groups, id_pool):
        text, title = render(g, nid, record_path, sha[:9])
        path = os.path.join(notes_dir, nid + ".md")
        if os.path.exists(path):
            raise SystemExit("refusing to overwrite " + path)
        if not dry_run:
            with open(path, "w", encoding="utf-8", newline="\r\n") as fh:
                fh.write(text)
        written.append((nid, g["findings"][0]["kind"], title))
    return written, skipped, id_pool[len(groups):]


def self_test():
    fixture = {"findings": [
        {"kind": "dead-artifact", "wave": "delete", "class": "root-file", "site": "x.txt", "rule": "r",
         "scenario": "no live caller names x.txt", "target": "delete it", "id": "R0-9001"},
        {"kind": "folder-namespace", "wave": "move-rename", "site": "src/A/B", "rule": "r", "scenario": "s",
         "measure": {"disagreements": ["N.A != N.A.B"]}, "target": "t", "id": "R0-9002"},
        {"kind": "god-class", "wave": "extract", "site": {"type": "T", "files": []}, "rule": "r", "scenario": "s",
         "target": "t", "id": "R0-9003"}]}
    with tempfile.TemporaryDirectory() as tmp:
        fp = os.path.join(tmp, "abcdef0123456.findings.json")
        with open(fp, "w", encoding="utf-8") as fh:
            json.dump(fixture, fh)
        nd = os.path.join(tmp, "Work")
        os.mkdir(nd)
        pool = ["PB9001", "PB9002", "PB9003"]
        w1, s1, tail = file_notes(fp, pool, nd, False)
        assert len(w1) == 2 and tail == ["PB9003"] and "god-class" in s1, (w1, s1)
        w2, s2, _ = file_notes(fp, pool[2:], nd, False)
        assert not w2 and len(s2["already-filed"]) == 2, (w2, s2)
        assert sorted(os.listdir(nd)) == ["PB9001.md", "PB9002.md"]
        with open(os.path.join(nd, "PB9001.md"), encoding="utf-8", newline="") as fh:
            text = fh.read().replace("\r\n", "\n")
        expect_tail = FRONTMATTER.format(title="PB9001 — Delete: 1 dead root-file artifact(s) in (repository root) "
                                               "(census R0-9001 at abcdef012)", nid="PB9001")
        assert text.startswith(expect_tail), text[:900]
        assert "census_ids: R0-9001" in text and "**Target:** delete it" in text
    print("self-test OK")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("findings", nargs="?")
    ap.add_argument("--ids")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--notes-dir", default=NOTES_DIR)
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    if not a.findings or not a.ids:
        ap.error("findings file and --ids are required")
    written, skipped, tail = file_notes(a.findings, parse_ranges(a.ids), a.notes_dir, a.dry_run)
    counts = {}
    for _, kind, _t in written:
        counts[kind] = counts.get(kind, 0) + 1
    for nid, kind, title in written:
        print("%s%s  %s" % ("(dry) " if a.dry_run else "", nid, title))
    print("notes: %d  by first-finding kind: %s" % (len(written), json.dumps(counts, sort_keys=True)))
    for k, v in sorted(skipped.items()):
        print("not filed [%s]: %d (%s)" % (k, len(v), NOT_FILED.get(k, "")))
    print("unused ids: %s" % (("%s..%s (%d)" % (tail[0], tail[-1], len(tail))) if tail else "none"))


if __name__ == "__main__":
    sys.exit(main())
