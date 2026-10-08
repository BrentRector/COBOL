#!/usr/bin/env python3
"""r2_collect.py — reads the R2 review fleet's checkpoint files FROM DISK and decides each finding, each pair's
completeness and each null result (kb/Work PB2560; docs/rearchitecture/DESIGN-architecture-review.md §3 R2; the R2
adversarial review's B2, B3, B5, B6).

WHY. A Workflow script has no filesystem, and its in-memory return is lost with the run; the first R2 batch ran 13
minutes with 8 Opus agents and everything was discarded. Every agent of `scripts/arch/wf_r2_review.js` therefore
APPENDS one JSON line per decision the moment it is made, and this script — not the workflow's return — is the
authority on what the batch found. It runs any time (mid-batch, after a stop, after a relaunch) and is idempotent.

THE FILES (all under the batch's directory `<inputs dir>/batch-<label>/`, beside its `batch-args.json`; the shard
table `shards.json` is the inputs dir's; `<pair>` is `<shard>--<dimension>`, `<k>` a finder's number):
  review-<pair>--f<k>.jsonl   a finder: {"type": "read", "file"} for each file read WHOLE, {"type": "finding", ...}
                              per finding (the schema below), and {"type": "done"} when it finished its files;
  null-<pair>.jsonl           the null-result examiner of a pair whose finders found nothing:
                              {"type": "null-check", "missed": [finding ids it then wrote as findings]} — its
                              findings are ordinary `finding` lines in the same file;
  refute-<pair>--c<j>--<lens>.jsonl   a skeptic: {"type": "verdict", "finding", "lens", "refuted", "why",
                              "corrected"} per finding of chunk j (chunks of 4, lenses `site`, `rule`, `scenario`).

THE FINDING SCHEMA (B5; every field required unless marked):
  id            "<pair>--f<k>#<n>" (unique; the skeptics and the note's `r2_ids:` line use it)
  kind          finding | lead | defect   (a lead lacks the evidence a finding needs: a performance claim with no
                measurement on the pin, a modern-C# point with no analyzer rule id — both are reported, never filed)
  title, rule, scenario, target          the bar of design §3 R2
  files         repository-relative paths, each must exist in the PIN's tree
  sites         ["path:12-40 (Type.Member)", ...] the exact code sites
  members       ["Ns.Type.Member", ...] what a wave would move or change (rendered as `**Moves or changes:**`, which
                the member index turns into the wave's computed file set)
  census_ids    the census findings it rests on (R0-nnnn), or []
  design_ref    "§8.x" or "PBnnnn" — the target section of the approved §8, or the open note that already plans it
  wave_kind     extract | unify | move-and-rename | data-ize | delete | modernize | defect-for-fix-lane
  analyzer_rule required when wave_kind is modernize (the analyzer id, §5.5 analyzer-first)
  measurement   required for a performance finding: the measured number on the pin and how it was measured
  severity      Critical | Warning | Suggestion (scale = long-lived, consequence = high)
  harm          for a defect: a subset of wrong-answer, crashes, silent, rejects-legal-source, under-rejects
  existing_note "PBnnnn" when the finding is already a note (then it is not filed again), else ""
  owner_question "" or the one question only the owner can answer

DECISIONS.
  * UPHELD when at least two of the three lens skeptics fail to refute it; REFUTED when two refute; UNVERIFIED while
    fewer than two votes agree (a stopped batch): never filed.
  * A pair is COMPLETE when the union of its finders' `read` lines equals the shard's files; otherwise its MISSING
    files are listed and the workflow's next launch sends a finder to exactly them. A pair's null result is ACCEPTED
    only when it is complete and its null examiner wrote a `null-check` line.
  * An INVALID record (a missing field, a file not in the pin's tree, a modernize without an analyzer rule, a
    performance finding without a measurement) is never filed; leads and suggestions are reported, not filed.

Writes `<out>/collected.json` and `<out>/<pair>.json` (one per pair); prints a summary.

Usage:
    python scripts/arch/r2_collect.py --out <inputs dir>/batch-<label>
    python scripts/arch/r2_collect.py --self-test
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import tempfile
from collections import defaultdict
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
LENSES = ("site", "rule", "scenario")
KINDS = ("finding", "lead", "defect")
WAVES = ("extract", "unify", "move-and-rename", "data-ize", "delete", "modernize", "defect-for-fix-lane")
SEVERITIES = ("Critical", "Warning", "Suggestion")
HARMS = ("wrong-answer", "crashes", "silent", "rejects-legal-source", "under-rejects")
REQUIRED = ("id", "kind", "title", "rule", "scenario", "target", "files", "sites", "members", "census_ids",
            "design_ref", "wave_kind", "severity", "existing_note", "owner_question")
DESIGN_REF = re.compile(r"^(§8(\.\d+)*( \(\d+\))?|PB\d+)$")


def read_jsonl(p: Path):
    out = []
    for line in p.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line:
            continue
        try:
            out.append(json.loads(line))
        except json.JSONDecodeError:
            out.append({"type": "corrupt", "text": line[:200]})
    return out


def problems(rec: dict, tracked: set[str]) -> list[str]:
    bad = ["missing field %s" % k for k in REQUIRED if k not in rec]
    if bad:
        return bad
    if rec["kind"] not in KINDS:
        bad.append("kind %r" % rec["kind"])
    if rec["wave_kind"] not in WAVES:
        bad.append("wave_kind %r" % rec["wave_kind"])
    if rec["severity"] not in SEVERITIES:
        bad.append("severity %r" % rec["severity"])
    if not rec["files"]:
        bad.append("no files")
    bad += ["file not in the pin's tree: %s" % f for f in rec["files"] if f not in tracked]
    if not DESIGN_REF.match(rec["design_ref"] or ""):
        bad.append("design_ref %r is neither §8.x nor PBnnnn" % rec["design_ref"])
    if rec["kind"] == "defect" or rec["wave_kind"] == "defect-for-fix-lane":
        if not set(rec.get("harm") or []) <= set(HARMS) or not rec.get("harm"):
            bad.append("a defect names its harm from %s" % (HARMS,))
    return bad


def demote(rec: dict, dimension: str) -> str | None:
    """-> why a record is a LEAD (reported, not filed), or None."""
    if rec.get("kind") == "lead":
        return "the reviewer marked it a lead"
    if rec.get("wave_kind") == "modernize" and not rec.get("analyzer_rule"):
        return "a modern-C# point with no analyzer rule id is a suggestion (§5.5 analyzer-first; N2)"
    if dimension == "performance" and rec.get("kind") == "finding" and not rec.get("measurement"):
        return "a performance claim with no measurement on the pin is a lead (§5.4; N3)"
    if rec.get("severity") == "Suggestion":
        return "severity Suggestion"
    return None


def collect(out: Path, tracked: set[str] | None = None):
    sj = out / "shards.json" if (out / "shards.json").exists() else out.parent / "shards.json"
    shards = json.loads(sj.read_text(encoding="utf-8"))
    files_of = {s["id"]: [p for p, _ in s["files"]] for s in shards["shards"]}
    if tracked is None:
        ls = subprocess.run(["git", "-C", str(REPO), "ls-tree", "-r", "--name-only", shards["commit"]],
                            capture_output=True, text=True, check=True).stdout
        tracked = set(ls.splitlines())
    args = json.loads((out / "batch-args.json").read_text(encoding="utf-8")) if (out / "batch-args.json").exists() else None
    pairs = defaultdict(lambda: {"read": set(), "findings": {}, "finders": 0, "done": 0, "null_checked": False,
                                 "corrupt": 0})
    for p in sorted(out.glob("review-*.jsonl")) + sorted(out.glob("null-*.jsonl")):
        m = re.match(r"^(?:review|null)-(.+?--[a-z-]+?)(?:--f\d+)?\.jsonl$", p.name)
        if not m:
            continue
        pair = pairs[m.group(1)]
        if p.name.startswith("review-"):
            pair["finders"] += 1
        for r in read_jsonl(p):
            t = r.get("type")
            if t == "read":
                pair["read"].add(r.get("file"))
            elif t == "finding":
                pair["findings"][r.get("id")] = r
            elif t == "done":
                pair["done"] += 1
            elif t == "null-check":
                pair["null_checked"] = True
            elif t == "corrupt":
                pair["corrupt"] += 1
    votes = defaultdict(dict)
    for p in sorted(out.glob("refute-*.jsonl")):
        for r in read_jsonl(p):
            if r.get("type") == "verdict" and r.get("lens") in LENSES:
                votes[r.get("finding")][r["lens"]] = r
    report = {"pin": shards["commit"], "pairs": {}, "upheld": [], "refuted": [], "unverified": [], "leads": [],
              "invalid": [], "already_tracked": []}
    for name, pair in sorted(pairs.items()):
        shard, dim = name.rsplit("--", 1)
        want = set(files_of.get(shard, []))
        missing = sorted(want - pair["read"])
        counts = defaultdict(int)
        for fid, rec in pair["findings"].items():
            rec = dict(rec, pair=name, shard=shard, dimension=dim)
            bad = problems(rec, tracked)
            why = None if bad else demote(rec, dim)
            v = votes.get(fid, {})
            stand = [x for x in v.values() if not x.get("refuted")]
            fall = [x for x in v.values() if x.get("refuted")]
            rec["votes"] = {lens: ("refuted" if x.get("refuted") else "upheld") for lens, x in v.items()}
            rec["corrections"] = [x.get("corrected") for x in stand if x.get("corrected")]
            if bad:
                bucket, rec["problems"] = "invalid", bad
            elif rec.get("existing_note"):
                bucket = "already_tracked"
            elif why:
                bucket, rec["lead_reason"] = "leads", why
            elif len(fall) >= 2:
                bucket = "refuted"
            elif len(stand) >= 2:
                bucket = "upheld"
            else:
                bucket = "unverified"
            report[bucket].append(rec)
            counts[bucket] += 1
        null = not pair["findings"]
        report["pairs"][name] = {
            "shard": shard, "dimension": dim, "files": len(want), "read": len(pair["read"] & want),
            "complete": not missing, "missing": missing, "finders": pair["finders"], "done": pair["done"],
            "findings": len(pair["findings"]), "corrupt_lines": pair["corrupt"], **dict(counts),
            "null_result": null, "null_accepted": null and not missing and pair["null_checked"]}
        (out / ("%s.json" % name)).write_text(json.dumps(
            {"pair": report["pairs"][name], "findings": [r for b in ("upheld", "refuted", "unverified", "leads", "invalid",
                                                                     "already_tracked")
                                                         for r in report[b] if r["pair"] == name]}, indent=1),
            encoding="utf-8")
    if args:   # a pair the batch asked for that wrote nothing is reported, never silently absent
        if args.get("duplicationPass"):   # the one whole-codebase clone pass is a pair too
            report["pairs"].setdefault("global--duplication", {"shard": "global", "dimension": "duplication", "files": 0,
                                                               "read": 0, "complete": False, "missing": [], "finders": 0,
                                                               "findings": 0, "null_result": True, "null_accepted": False,
                                                               "never_started": True})
        for s in args.get("shards", []):
            for d in args.get("dimensions", []):   # duplication too: each shard has its own duplication lens
                name = "%s--%s" % (s["id"], d)
                report["pairs"].setdefault(name, {"shard": s["id"], "dimension": d, "files": len(s["files"]), "read": 0,
                                                  "complete": False, "missing": s["files"], "finders": 0,
                                                  "findings": 0, "null_result": True, "null_accepted": False,
                                                  "never_started": True})
    (out / "collected.json").write_text(json.dumps(report, indent=1), encoding="utf-8")
    return report


def summary(rep) -> str:
    inc = [n for n, p in rep["pairs"].items() if not p["complete"]]
    nulls = [n for n, p in rep["pairs"].items() if p["null_result"] and not p["null_accepted"]]
    return ("pairs %d (incomplete %d, unexamined nulls %d) · upheld %d · refuted %d · unverified %d · leads %d · "
            "invalid %d · already tracked %d" % (len(rep["pairs"]), len(inc), len(nulls), len(rep["upheld"]),
                                                 len(rep["refuted"]), len(rep["unverified"]), len(rep["leads"]),
                                                 len(rep["invalid"]), len(rep["already_tracked"])))


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test():
    ok = True

    def arm(name, cond):
        nonlocal ok
        print(("PASS  " if cond else "FAIL  ") + name)
        ok = ok and cond

    def rec(i, **kw):
        r = {"type": "finding", "id": "s1--architecture--f1#%d" % i, "kind": "finding", "title": "t", "rule": "r",
             "scenario": "s", "target": "x", "files": ["src/A.cs"], "sites": ["src/A.cs:1-9 (A.F)"], "members": ["N.A.F"],
             "census_ids": [], "design_ref": "§8.3", "wave_kind": "extract", "severity": "Warning",
             "existing_note": "", "owner_question": ""}
        r.update(kw)
        return r

    def write(p: Path, rows):
        p.write_text("".join(json.dumps(r) + "\n" for r in rows) + "{broken\n", encoding="utf-8")

    def vote(fid, lens, refuted):
        return {"type": "verdict", "finding": fid, "lens": lens, "refuted": refuted, "why": "w", "corrected": None}

    with tempfile.TemporaryDirectory() as tmp:
        out = Path(tmp)
        (out / "shards.json").write_text(json.dumps({"commit": "c", "shards": [
            {"id": "s1", "files": [["src/A.cs", 9], ["src/B.cs", 9]]}, {"id": "s2", "files": [["src/C.cs", 9]]}]}),
            encoding="utf-8")
        (out / "batch-args.json").write_text(json.dumps({"shards": [{"id": "s1", "files": ["src/A.cs", "src/B.cs"]},
                                                                    {"id": "s2", "files": ["src/C.cs"]}],
                                                         "dimensions": ["architecture", "performance", "duplication"]}),
                                             encoding="utf-8")
        write(out / "review-s1--architecture--f1.jsonl", [
            {"type": "read", "file": "src/A.cs"}, rec(1), rec(2), rec(3), rec(4, files=["src/Nope.cs"]),
            rec(5, wave_kind="modernize"), rec(6, existing_note="PB1"), rec(7, design_ref="later")])
        write(out / "review-s1--architecture--f2.jsonl", [{"type": "read", "file": "src/B.cs"}, {"type": "done"}])
        write(out / "review-s1--performance--f1.jsonl", [{"type": "read", "file": "src/A.cs"},
                                                         rec(1, id="s1--performance--f1#1")])
        write(out / "review-s2--architecture--f1.jsonl", [{"type": "read", "file": "src/C.cs"}, {"type": "done"}])
        write(out / "null-s2--architecture.jsonl", [{"type": "null-check", "missed": []}])
        f = "s1--architecture--f1#%d"
        write(out / "refute-s1--architecture--c1--site.jsonl", [vote(f % 1, "site", False), vote(f % 2, "site", True),
                                                                vote(f % 3, "site", False)])
        write(out / "refute-s1--architecture--c1--rule.jsonl", [vote(f % 1, "rule", False), vote(f % 2, "rule", True)])
        write(out / "refute-s1--architecture--c1--scenario.jsonl", [vote(f % 1, "scenario", True)])
        rep = collect(out, {"src/A.cs", "src/B.cs", "src/C.cs"})
        ids = lambda b: sorted(r["id"] for r in rep[b])  # noqa: E731
        arm("two of three skeptics failing to refute UPHOLDS", ids("upheld") == [f % 1])
        arm("two refutations REFUTE", ids("refuted") == [f % 2])
        arm("one vote is UNVERIFIED, never filed", ids("unverified") == [f % 3])
        arm("a file not in the pin's tree and a bad design_ref are INVALID",
            ids("invalid") == [f % 4, f % 7] and "file not in the pin's tree: src/Nope.cs" in rep["invalid"][0]["problems"])
        arm("a modernize point with no analyzer rule is a lead", ids("leads") == [f % 5, "s1--performance--f1#1"])
        arm("a performance finding with no measurement is a lead",
            any("measurement" in r["lead_reason"] for r in rep["leads"]))
        arm("an existing note is not filed again", ids("already_tracked") == [f % 6])
        p = rep["pairs"]
        arm("the union of two finders' reads makes a pair COMPLETE", p["s1--architecture"]["complete"])
        arm("a pair missing a file lists it", p["s1--performance"]["missing"] == ["src/B.cs"])
        arm("a complete, examined null is ACCEPTED", p["s2--architecture"]["null_accepted"])
        arm("a pair the batch asked for that wrote nothing is reported, not absent",
            p["s2--performance"].get("never_started") and not p["s2--performance"]["null_accepted"])
        arm("a shard's duplication lens is a pair like any other", "s2--duplication" in p and "s1--duplication" in p)
        arm("corrupt lines are counted, never fatal", p["s1--architecture"]["corrupt_lines"] == 2)
        arm("one file per pair is written", (out / "s1--architecture.json").exists())
        rep2 = collect(out, {"src/A.cs", "src/B.cs", "src/C.cs"})
        arm("collection is idempotent", json.dumps(rep2, sort_keys=True) == json.dumps(rep, sort_keys=True))
    print("=== R2 COLLECT SELF-TEST: %s ===" % ("PASS" if ok else "FAIL"))
    return 0 if ok else 1


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", type=Path)
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    if not a.out:
        ap.error("--out is required")
    rep = collect(a.out)
    print("=== R2 COLLECT: %s ===" % summary(rep))
    return 0


if __name__ == "__main__":
    sys.exit(main())
