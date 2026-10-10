#!/usr/bin/env python3
"""r2_collect.py — the ONE owner of the R2 review fleet's checkpoint format: it appends a decision for an agent,
answers an agent's "what is already decided", builds the launch state of a (re)launch, and reads the checkpoint
files FROM DISK to decide each finding, each pair's completeness, each null result and which upheld findings are one
mechanism (kb/Work PB2560; docs/rearchitecture/DESIGN-architecture-review.md §3 R2; the R2 adversarial review's B2,
B3, B5, B6 and the w1034 claim refuter's C2, C3).

WHY. A Workflow script has no filesystem, and its in-memory return is lost with the run; the first R2 batch ran 13
minutes with 8 Opus agents and everything was discarded. Every agent of `wf_r2_review.js` therefore APPENDS one JSON
line per decision the moment it is made, and this script — never the workflow's return — is the authority on what
the batch found. The w1034 refuter then showed that per-AGENT skipping is not enough: the workflow rebuilt its state
from what agents RETURNED, so a relaunch re-read a finisher's files, never re-ran the finisher, left its findings
unverified forever, and re-decided findings whose skeptic chunks had moved; a finder that wrote findings and then
returned nothing (turn cap, API error) orphaned them in a single run. So the state is PER PAIR, FROM DISK, and every
agent asks `--status` what its PAIR (not its own file) has decided.

THE LAUNCH (kb/Work PB2707). A workflow script cannot read a file, and batch 2's relaunch state was a 95,644-byte JSON
line the operator had to transcribe by hand into six Workflow calls. So `--launch` writes the launch itself:
`<batch dir>/launch.js` is the workflow template (`wf_r2_review.js`) with its plan line replaced by the PLAN, built
here from disk, and the operator's whole call is `Workflow({scriptPath: "<batch dir>\\launch.js"})`, whatever the
batch's progress. The plan carries only what is UNDECIDED: per pair with work left, the files no finder has read
(with their lines) and per lens the findings no skeptic has decided; a pair that is finished is not in it, so its
size is O(undecided pairs and findings), never O(all pairs). Before writing it, `--launch` sizes the plan
(`r2_cost.py`: the measured cost of earlier batches against the session's and the week's room left) and REFUSES a
plan that cannot finish, printing the estimate and the split (`--launch --shards ...`, one launch per part); a
refused launch removes any earlier launch.js, so a stale plan is never launched.

THE FILES (all in the batch directory `<inputs dir>/batch-<label>/`, beside its `batch-args.json`; the shard table
`shards.json` is the inputs dir's; `<pair>` is `<shard>--<dimension>`, `<k>` a finder's number, unique per agent):
  review-<pair>--f<k>.jsonl   a finder: {"type": "read", "file"} for each file read WHOLE, {"type": "finding", ...}
                              per finding (the schema below), and {"type": "done"} when its list is finished;
  null-<pair>.jsonl           the null-result examiner: {"type": "null-check", "missed": [ids]} and its findings;
  refute-<pair>--c<j>--<lens>.jsonl   a skeptic: {"type": "verdict", "finding", "lens", "refuted", "why",
                              "corrected"} per finding (lenses `site`, `rule`, `scenario`); a finding is decided under
                              a lens once ANY refute file of the pair holds its verdict for that lens.

THE FINDING SCHEMA (B5; every field required unless marked):
  id            "<pair>--f<k>#<n>" or "<pair>--null#<n>" — the prefix is the checkpoint file's, and an id is never
                reused for a different record (`--append` refuses it)
  kind          finding | lead | defect   (a lead lacks the evidence a finding needs: a performance claim with no
                measurement on the pin, a modern-C# point with no analyzer rule id — both are reported, never filed)
  title, rule, scenario, target          the bar of design §3 R2
  files         repository-relative paths, each must exist in the PIN's tree
  sites         ["path:12-40 (Type.Member)", ...] the exact code sites
  members       ["Ns.Type.Member", ...] what a wave would move or change
  census_ids    the census findings it rests on (R0-nnnn), or []
  design_ref    "§8.x" or "PBnnnn" — the target section of the approved §8, or the open note that already plans it
  wave_kind     extract | unify | move-and-rename | data-ize | delete | modernize | defect-for-fix-lane
  analyzer_rule required when wave_kind is modernize (the analyzer id, §5.5 analyzer-first)
  measurement   required for a performance finding: the measured number on the pin and how it was measured
  severity      Critical | Warning | Suggestion (scale = long-lived, consequence = high)
  harm          for a defect: a non-empty subset of wrong-answer, crashes, silent, rejects-legal-source, under-rejects
  spec_refs     for a defect: [{"clause": "14.9.8.4", "text": "words of that clause"}, ...] — each is run through
                `cite.py`'s check here, so a defect note's `spec_refs:` holds only checked clauses (CLAUDE.md rule 1)
  existing_note "PBnnnn" when the finding is already a note (then it is not filed again), else ""
  owner_question "" or the one question only the owner can answer

DECISIONS.
  * UPHELD when at least two of the three lens skeptics fail to refute it; REFUTED when two refute; UNVERIFIED while
    fewer than two votes agree: never filed. An upheld finding takes the CORRECTIONS of the skeptics that upheld it,
    each lens only for the fields it judged (site: files, sites, members, existing_note; rule: design_ref, wave_kind,
    severity; scenario: measurement, harm, spec_refs), and is validated again afterwards.
  * ONE MECHANISM, ONE NOTE (C3). Upheld findings of the same wave kind whose members overlap (equal, or one a member
    of the other's type) or whose sites overlap (same file, intersecting line ranges) are one mechanism, whichever
    finder, pair or dimension found them; `mechanisms` lists each with every id, and the most specific record (the
    fewest files, then sites) is its primary. `file_census_notes.py --r2` files one note per mechanism.
  * A pair is COMPLETE when the union of its finders' `read` lines equals the shard's files. A pair's null result is
    ACCEPTED only when it is complete and examined (`null-check`).
  * The batch is COMPLETE when every pair is complete, every null is examined and no finding is unverified; until
    then the verdict says LAUNCH NEEDED, and `--launch` writes the launch that resumes it.
  * An INVALID record (a missing field, a file not in the pin's tree, a defect without harm or a checked clause, two
    records under one id) is never filed; leads and suggestions are reported, not filed.

Usage:
    python scripts/arch/r2_collect.py --out <batch dir>                 # decide; writes collected.json
    python scripts/arch/r2_collect.py --out <batch dir> --launch [--shards a,b] [--borrow-days N] [--session-reserve P]
                                                                        # size the plan; write <batch dir>/launch.js
    python scripts/arch/r2_collect.py --status <batch dir> <pair>       # an agent: what its pair has decided
    python scripts/arch/r2_collect.py --append <checkpoint file> '<one JSON object>'   # an agent: one decision
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
from functools import lru_cache
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
LENSES = ("site", "rule", "scenario")
KINDS = ("finding", "lead", "defect")
WAVES = ("extract", "unify", "move-and-rename", "data-ize", "delete", "modernize", "defect-for-fix-lane")
SEVERITIES = ("Critical", "Warning", "Suggestion")
HARMS = ("wrong-answer", "crashes", "silent", "rejects-legal-source", "under-rejects")
sys.path.insert(0, str(REPO / "scripts" / "spec"))
from work import HARM_FLAGS  # noqa: E402  (the register's ONE definition of a harm `work.py next` ranks)
sys.path.insert(0, str(Path(__file__).resolve().parent))
import r2_cost  # noqa: E402  (the ONE estimator of what a launch plan spends)
TEMPLATE = REPO / ".claude" / "skills" / "workstream" / "templates" / "wf_r2_review.js"
# the template's one plan line, which `--launch` replaces with the plan (the line's own comment says so)
PLAN_LINE = re.compile(r"^const A = args\b.*$", re.M)
# `silent` qualifies a harm and is never one by itself: a defect filed with only `silent` sets no flag in HARM_FLAGS and
# is invisible to `work.py next` (batch 1 of 8be230068 filed one, PB2647, and `work.py check` refused it).
RANKED_HARMS = tuple(h for h in HARMS if h.replace("-", "_") in HARM_FLAGS)
REQUIRED = ("id", "kind", "title", "rule", "scenario", "target", "files", "sites", "members", "census_ids",
            "design_ref", "wave_kind", "severity", "existing_note", "owner_question")
# The reference is the LEADING token — a section of the approved §8 or the open note that plans the change; whatever
# follows a non-word character is the finder's qualifier (`§8.3(2)`, `§8.4 (PB2291, PB2333)`, `§8.7 (R4 step 4); …`).
# Batch 1 of 8be230068 lost ten upheld findings to the earlier exact-match form. A non-§8 section (`§5.1 (…)`) or prose
# (`later`) is still INVALID: the finding names no target the approved design or the register holds.
DESIGN_REF = re.compile(r"^(§8(\.\d+)*|PB\d+)(\W.*)?$")
# the fields each lens judges, so a skeptic's correction lands only where its lens looked
LENS_FIELDS = {"site": ("files", "sites", "members", "existing_note"),
               "rule": ("design_ref", "wave_kind", "severity"),
               "scenario": ("measurement", "harm", "spec_refs")}
# review-<pair>--f<k>.jsonl · null-<pair>.jsonl · refute-<pair>--c<j>--<lens>.jsonl
CHECKPOINT = re.compile(r"^(?P<kind>review|null|refute)-(?P<pair>.+?--[a-z][a-z-]*?)"
                        r"(?:--f(?P<k>\d+)|--c(?P<j>\d+)--(?P<lens>site|rule|scenario))?\.jsonl$")
SITE = re.compile(r"^(?P<path>[^\s:]+)(?::(?P<a>\d+)(?:-(?P<b>\d+))?)?")
GLOBAL_PAIR = "global--duplication"   # the ONE whole-codebase clone pass (design §3 R2, B4)
LINE_TYPES = {"review": ("read", "finding", "done"), "null": ("null-check", "finding", "read"), "refute": ("verdict",)}


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


def checkpoint_name(name: str):
    """-> (kind, pair, k, lens) of a checkpoint file name, or None."""
    m = CHECKPOINT.match(name)
    if not m or (m["kind"] == "review" and not m["k"]) or (m["kind"] == "refute" and not m["lens"]) \
            or (m["kind"] == "null" and (m["k"] or m["lens"])):
        return None
    return m["kind"], m["pair"], int(m["k"]) if m["k"] else None, m["lens"]


# ── the citations a defect rests on (CLAUDE.md rule 1: a citation not checked is not a citation) ─────────────────────
@lru_cache(maxsize=1)
def _spec():
    sys.path.insert(0, str(REPO / "scripts" / "spec"))
    import cite  # noqa: E402
    return cite, cite.load()


def spec_ref_problems(refs) -> list[str]:
    if not isinstance(refs, list) or not refs:
        return ["a defect names the ISO clause it breaks: spec_refs [{clause, text}] (cite.py --check)"]
    bad = []
    cite, (lines, cls) = _spec()
    for r in refs:
        if not isinstance(r, dict) or not r.get("clause") or not r.get("text"):
            bad.append("spec_ref %r is not {clause, text}" % (r,))
        elif cite.check(lines, cls, str(r["clause"]), str(r["text"]))[0] != 0:
            bad.append("spec_ref §%s does not contain %r (cite.py --check)" % (r["clause"], r["text"]))
    return bad


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
        if not set(rec.get("harm") or []) <= set(HARMS) or not set(rec.get("harm") or []) & set(RANKED_HARMS):
            bad.append("a defect names its harm from %s, at least one of %s" % (HARMS, RANKED_HARMS))
        bad += spec_ref_problems(rec.get("spec_refs"))
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


# ── reading the disk ─────────────────────────────────────────────────────────────────────────────────────────────
def scan(out: Path):
    """Every checkpoint file of the batch -> {pair: on-disk state} and {finding id: {lens: verdict}}."""
    pairs = defaultdict(lambda: {"read": set(), "findings": {}, "clash": set(), "finders": set(), "done": 0,
                                 "null_checked": False, "corrupt": 0})
    votes = defaultdict(dict)
    for p in sorted(out.glob("*.jsonl")):
        c = checkpoint_name(p.name)
        if not c:
            continue
        kind, name, k, lens = c
        pair = pairs[name]
        if kind == "refute":
            for r in read_jsonl(p):
                if r.get("type") == "verdict" and r.get("lens") == lens:
                    votes[r.get("finding")][lens] = r
                elif r.get("type") == "corrupt":
                    pair["corrupt"] += 1
            continue
        if kind == "review":
            pair["finders"].add(k)
        for r in read_jsonl(p):
            t = r.get("type")
            if t == "read":
                pair["read"].add(r.get("file"))
            elif t == "finding":
                fid, have = r.get("id"), pair["findings"].get(r.get("id"))
                if have is not None and have != r:
                    pair["clash"].add(fid)   # two records under one id: neither is trusted (C2 non-blocking)
                pair["findings"].setdefault(fid, r)
            elif t == "done":
                pair["done"] += 1
            elif t == "null-check":
                pair["null_checked"] = True
            elif t == "corrupt":
                pair["corrupt"] += 1
    return pairs, votes


def pair_state(pair, votes) -> dict:
    """The JSON state of one pair, as `--status` prints it for an agent (the launch takes `plan_entry` instead)."""
    ids = sorted(pair["findings"])
    nxt = defaultdict(int)
    for fid in ids:
        pre, _, n = fid.rpartition("#")
        if n.isdigit():
            nxt[pre] = max(nxt[pre], int(n))
    return {"read": sorted(f for f in pair["read"] if f), "findings": ids,
            "finders": max(pair["finders"], default=0), "done": pair["done"], "null_checked": pair["null_checked"],
            "decided": {lens: [i for i in ids if lens in votes.get(i, {})] for lens in LENSES},
            "next_n": {pre: n + 1 for pre, n in sorted(nxt.items())}}


def batch_files(out: Path):
    sj = out / "shards.json" if (out / "shards.json").exists() else out.parent / "shards.json"
    shards = json.loads(sj.read_text(encoding="utf-8"))
    args = json.loads((out / "batch-args.json").read_text(encoding="utf-8")) if (out / "batch-args.json").exists() else None
    return shards, args


def pin_tracked(commit: str) -> set[str]:
    ls = subprocess.run(["git", "-C", str(REPO), "ls-tree", "-r", "--name-only", commit],
                        capture_output=True, text=True, check=True).stdout
    return set(ls.splitlines())


# ── one mechanism, one note (the w1034 refuter's C3) ─────────────────────────────────────────────────────────────
def site_ranges(rec):
    out = []
    for s in rec.get("sites") or []:
        m = SITE.match(s.strip())
        if m and m["a"]:
            out.append((m["path"], int(m["a"]), int(m["b"] or m["a"])))
    return out


def members_overlap(a, b) -> bool:
    for x in a:
        for y in b:
            if x == y or x.startswith(y + ".") or y.startswith(x + "."):
                return True
    return False


def same_mechanism(a: dict, b: dict) -> str | None:
    """-> why two upheld findings are ONE mechanism (one wave, one note), or None."""
    if a["wave_kind"] != b["wave_kind"]:
        return None
    if members_overlap(a.get("members") or [], b.get("members") or []):
        return "the same wave kind on overlapping members"
    for p, x1, x2 in site_ranges(a):
        for q, y1, y2 in site_ranges(b):
            if p == q and x1 <= y2 and y1 <= x2:
                return "the same wave kind on overlapping sites (%s)" % p
    return None


def mechanisms(upheld: list[dict]) -> list[dict]:
    parent = list(range(len(upheld)))

    def root(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    links = []
    for i in range(len(upheld)):
        for j in range(i + 1, len(upheld)):
            w = same_mechanism(upheld[i], upheld[j])
            if w:
                links.append((i, w))
                parent[root(j)] = root(i)
    groups = defaultdict(list)
    for i in range(len(upheld)):
        groups[root(i)].append(i)
    out = []
    for members in groups.values():
        g = [upheld[i] for i in members]
        prim = min(g, key=lambda r: (len(r["files"]), len(r["sites"]), r["id"]))
        out.append({"primary": prim["id"], "ids": sorted(r["id"] for r in g),
                    "why": sorted({w for i, w in links if i in members})})
    return sorted(out, key=lambda m: m["primary"])


def corrected(rec: dict, stand: dict) -> tuple[dict, list]:
    """Apply the corrections of the skeptics that UPHELD the finding, each only to the fields its lens judged.

    A field a skeptic returns as null is one it did NOT correct (batch 1 of 8be230068: a site-lens skeptic corrected
    `files` and returned `"sites": null`, which overwrote the finding's sites and crashed `mechanisms`), so null never
    replaces a value."""
    rec, applied = dict(rec), []
    for lens, v in sorted(stand.items()):
        c = v.get("corrected")
        if not isinstance(c, dict):
            continue
        for k, val in c.items():
            if val is None:
                continue
            if k in LENS_FIELDS[lens] and rec.get(k) != val:
                applied.append({"lens": lens, "field": k, "was": rec.get(k), "now": val})
                rec[k] = val
    return rec, applied


# ── deciding ─────────────────────────────────────────────────────────────────────────────────────────────────────
def collect(out: Path, tracked: set[str] | None = None):
    shards, args = batch_files(out)
    files_of = {s["id"]: [p for p, _ in s["files"]] for s in shards["shards"]}
    if tracked is None:
        tracked = pin_tracked(shards["commit"])
    pairs, votes = scan(out)
    report = {"pin": shards["commit"], "pairs": {}, "upheld": [], "refuted": [], "unverified": [], "leads": [],
              "invalid": [], "already_tracked": [], "mechanisms": [],
              # a .jsonl the format does not name is never silently skipped: its lines may be findings
              "ignored_files": sorted(p.name for p in out.glob("*.jsonl") if not checkpoint_name(p.name))}
    for name, pair in sorted(pairs.items()):
        shard, dim = name.rsplit("--", 1)
        want = set(files_of.get(shard, []))
        missing = sorted(want - pair["read"])
        counts = defaultdict(int)
        for fid, rec in sorted(pair["findings"].items()):
            rec = dict(rec, pair=name, shard=shard, dimension=dim)
            v = votes.get(fid, {})
            stand = {lens: x for lens, x in v.items() if not x.get("refuted")}
            fall = [x for x in v.values() if x.get("refuted")]
            rec["votes"] = {lens: ("refuted" if x.get("refuted") else "upheld") for lens, x in v.items()}
            bad = ["two different records under the id %s" % fid] if fid in pair["clash"] else problems(rec, tracked)
            if not bad and len(stand) >= 2 and len(fall) < 2:
                rec, rec["corrections_applied"] = corrected(rec, stand)
                bad = ["after the skeptics' corrections: " + b for b in problems(rec, tracked)]
            why = None if bad else demote(rec, dim)
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
            # the one clone pass reads census families, not shard files: it is complete when it says so
            "complete": pair["done"] > 0 if name == GLOBAL_PAIR else not missing,
            "missing": missing, "finders": len(pair["finders"]), "done": pair["done"],
            "findings": len(pair["findings"]), "corrupt_lines": pair["corrupt"], **dict(counts),
            "null_result": null,
            "null_accepted": null and (pair["done"] > 0 if name == GLOBAL_PAIR else not missing and pair["null_checked"])}
    if args:   # a pair the batch asked for that wrote nothing is reported, never silently absent
        if args.get("duplicationPass"):   # the one whole-codebase clone pass is a pair too
            report["pairs"].setdefault(GLOBAL_PAIR, {"shard": "global", "dimension": "duplication", "files": 0,
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
    report["mechanisms"] = mechanisms(report["upheld"])
    report["relaunch"] = relaunch_reasons(report)
    report["complete"] = not report["relaunch"]
    for name in pairs:
        (out / ("%s.json" % name)).write_text(json.dumps(
            {"pair": report["pairs"][name], "findings": [r for b in ("upheld", "refuted", "unverified", "leads", "invalid",
                                                                     "already_tracked")
                                                         for r in report[b] if r["pair"] == name]}, indent=1),
            encoding="utf-8")
    (out / "collected.json").write_text(json.dumps(report, indent=1), encoding="utf-8")
    return report


def relaunch_reasons(rep) -> list[str]:
    out = []
    inc = sorted(n for n, p in rep["pairs"].items() if not p["complete"])
    nulls = sorted(n for n, p in rep["pairs"].items() if p["null_result"] and p["complete"] and not p["null_accepted"])
    if inc:
        out.append("%d pair(s) incomplete or never started" % len(inc))
    if nulls:
        out.append("%d null result(s) not examined" % len(nulls))
    if rep["unverified"]:
        out.append("%d finding(s) unverified" % len(rep["unverified"]))
    if rep["ignored_files"]:
        out.append("%d .jsonl file(s) not named as checkpoints, read by nothing (%s): rename or remove them" % (
            len(rep["ignored_files"]), ", ".join(rep["ignored_files"][:4])))
    return out


def summary(rep) -> str:
    dup = sum(len(m["ids"]) - 1 for m in rep["mechanisms"])
    head = ("COMPLETE" if rep["complete"] else "LAUNCH NEEDED (%s): `--launch`, then Workflow({scriptPath: "
            "<batch dir>\\launch.js})" % "; ".join(rep["relaunch"]))
    return ("%s — pairs %d · upheld %d in %d mechanism(s) (%d merged as duplicates) · refuted %d · unverified %d · "
            "leads %d · invalid %d · already tracked %d" % (
                head, len(rep["pairs"]), len(rep["upheld"]), len(rep["mechanisms"]), dup, len(rep["refuted"]),
                len(rep["unverified"]), len(rep["leads"]), len(rep["invalid"]), len(rep["already_tracked"])))


def plan_entry(slug: str, shard: str | None, dim: str, pair, votes, files=(), lines=None) -> dict | None:
    """One pair's work left, from disk, or None when it has none: the files no finder read (with their lines), the
    findings on disk (their count), the examined null, and per lens the findings no skeptic has decided."""
    ids = sorted(pair["findings"]) if pair else []
    read = pair["read"] if pair else set()
    e = {"slug": slug, "shard": shard, "dim": dim, "finders": max(pair["finders"], default=0) if pair else 0,
         "findings": len(ids), "undecided": {lens: [i for i in ids if lens not in votes.get(i, {})] for lens in LENSES}}
    if slug == GLOBAL_PAIR:   # the clone pass reads census families, not shard files: it is done when it says so
        e["done"] = bool(pair and pair["done"])
        return e if not e["done"] or any(e["undecided"].values()) else None
    e["unread"] = [[f, lines[f]] for f in files if f not in read]
    e["null_checked"] = bool(pair and pair["null_checked"])
    return e if e["unread"] or any(e["undecided"].values()) or not (ids or e["null_checked"]) else None


def plan(out: Path, only: list[str] | None = None) -> dict:
    """The next launch's PLAN, from disk (never from agent returns, the w1034 refuter's C2): the batch's settings, the
    shards its pairs need, and one entry per pair with work left (of `only`'s shards, or the clone pass `global`)."""
    bf = out / "batch-args.json"
    if not bf.exists():
        raise SystemExit("no batch-args.json in %s: write it with r2_inputs.py --batch" % out)
    args = json.loads(bf.read_text(encoding="utf-8"))
    if Path(args["outDir"]).resolve() != out.resolve():
        raise SystemExit("%s names the batch directory %s, not %s: a moved batch is rewritten with r2_inputs.py "
                         "--batch" % (bf, args["outDir"], out))
    return plan_of(args, *scan(out), only)


def plan_of(args: dict, pairs, votes, only: list[str] | None = None) -> dict:
    """The plan of the batch `args` over the on-disk state `pairs`, `votes` (`scan`'s; empty for a batch not yet
    launched, which is how `r2_inputs.py --batch` sizes a new one)."""
    known = [s["id"] for s in args["shards"]] + (["global"] if args.get("duplicationPass") else [])
    unknown = sorted(set(only or ()) - set(known))
    if unknown:
        raise SystemExit("not shards of this batch: %s (its shards: %s)" % (", ".join(unknown), ", ".join(known)))
    shards = [s for s in args["shards"] if only is None or s["id"] in only]
    entries = []
    for s in shards:
        lines = dict(zip(s["files"], s["fileLines"]))
        for d in args["dimensions"]:
            slug = "%s--%s" % (s["id"], d)
            entries.append(plan_entry(slug, s["id"], d, pairs.get(slug), votes, s["files"], lines))
    if args.get("duplicationPass") and (only is None or "global" in only):
        entries.append(plan_entry(GLOBAL_PAIR, None, "duplication", pairs.get(GLOBAL_PAIR), votes))
    work = [e for e in entries if e]
    used = {e["shard"] for e in work}
    return {**{k: args[k] for k in ("batch", "pinnedTree", "pinCommit", "outDir", "inputsDir", "stopFile",
                                    "globalStopFile", "width")},
            "planOf": args["outDir"], "decidedPairs": len(entries) - len(work),
            "shard_order": [s["id"] for s in shards if s["id"] in used],
            "shards": {s["id"]: {"name": s["name"], "lines": s["lines"], "input": s["input"], "files": len(s["files"])}
                       for s in shards if s["id"] in used},
            "pairs": work}


def launch_script(pl: dict) -> str:
    """The workflow template with its plan line replaced by `pl`: the whole launch, one file."""
    body = TEMPLATE.read_text(encoding="utf-8")
    if len(PLAN_LINE.findall(body)) != 1:
        raise SystemExit("%s has no single `const A = args` plan line to replace" % TEMPLATE)
    line = "const A = %s   // the plan of %s, written by r2_collect.py --launch from disk (kb/Work PB2707)" % (
        json.dumps(pl, ensure_ascii=False, separators=(",", ":")), pl["outDir"])
    return PLAN_LINE.sub(lambda _: line, body, count=1)


def launch(out: Path, b: dict, reserve: float = 0.0, only: list[str] | None = None, rl: dict | None = None):
    """Plan the next launch from disk and size it; -> (plan, estimate). Writes `<out>/launch.js` only when the plan
    has work and fits the room left; otherwise removes any earlier one, so a stale plan is never launched."""
    pl = plan(out, only)
    est = r2_cost.size(pl, rl or r2_cost.rules(), b, reserve)
    target = out / "launch.js"
    target.unlink(missing_ok=True)
    if est["fits"] and pl["pairs"]:
        target.write_text(launch_script(pl), encoding="utf-8", newline="\n")
    return pl, est


# ── the agent's two calls ────────────────────────────────────────────────────────────────────────────────────────
def status(out: Path, pair: str) -> dict:
    pairs, votes = scan(out)
    return pair_state(pairs[pair], votes)


def append(path: Path, line: str, tracked: set[str] | None = None) -> str:
    """Validate one decision against the checkpoint format and append it; -> what was done. Raises ValueError with
    the reason on a record the collector would reject, so the agent fixes it while it still has the context."""
    c = checkpoint_name(path.name)
    if not c:
        raise ValueError("%s is not a checkpoint file name (review-<pair>--f<k> · null-<pair> · "
                         "refute-<pair>--c<j>--<lens>)" % path.name)
    kind, pair, k, lens = c
    try:
        rec = json.loads(line)
    except json.JSONDecodeError as e:
        raise ValueError("not one JSON object: %s" % e) from e
    if not isinstance(rec, dict) or rec.get("type") not in LINE_TYPES[kind]:
        raise ValueError("a %s file holds only %s lines" % (kind, "/".join(LINE_TYPES[kind])))
    if rec["type"] == "verdict":
        if rec.get("lens") != lens or not isinstance(rec.get("refuted"), bool) or not rec.get("finding"):
            raise ValueError("a verdict names its finding, lens %r and refuted true|false" % lens)
    if rec["type"] == "finding":
        prefix = "%s--f%d#" % (pair, k) if kind == "review" else "%s--null#" % pair
        if not str(rec.get("id", "")).startswith(prefix) or not str(rec["id"])[len(prefix):].isdigit():
            raise ValueError("a finding in this file has the id %s<n>" % prefix)
        have = scan(path.parent)[0][pair]["findings"].get(rec["id"])
        if have == rec:
            return "ALREADY RECORDED %s" % rec["id"]
        if have is not None:
            raise ValueError("the id %s already holds a different finding: take the next number (--status next_n)"
                             % rec["id"])
        if tracked is None:
            tracked = pin_tracked(batch_files(path.parent)[0]["commit"])
        bad = problems(rec, tracked)
        if bad:
            raise ValueError("the collector would reject it: " + "; ".join(bad))
    with open(path, "a", encoding="utf-8", newline="\n") as fh:
        fh.write(json.dumps(rec, ensure_ascii=False) + "\n")
    return "APPENDED %s" % (rec.get("id") or rec.get("finding") or rec["type"])


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test():
    ok = True

    def arm(name, cond):
        nonlocal ok
        print(("PASS  " if cond else "FAIL  ") + name)
        ok = ok and bool(cond)

    def rec(i, **kw):
        r = {"type": "finding", "id": "s1--architecture--f1#%d" % i, "kind": "finding", "title": "t", "rule": "r",
             "scenario": "s", "target": "x", "files": ["src/A.cs"], "sites": ["src/A.cs:%d-%d (A.F%d)" % (i * 100, i * 100 + 9, i)],
             "members": ["N.A.F%d" % i], "census_ids": [], "design_ref": "§8.3", "wave_kind": "extract",
             "severity": "Warning", "existing_note": "", "owner_question": ""}
        r.update(kw)
        return r

    def write(p: Path, rows, broken=True):
        p.write_text("".join(json.dumps(r) + "\n" for r in rows) + ("{broken\n" if broken else ""), encoding="utf-8")

    def vote(fid, lens, refuted, corrected=None):
        return {"type": "verdict", "finding": fid, "lens": lens, "refuted": refuted, "why": "w", "corrected": corrected}

    def batch_fixture(out: Path, files: dict, dims: list) -> dict:
        """batch-args.json as r2_inputs.py --batch writes it, every file 9 lines."""
        return {"batch": "t", "pinnedTree": "P", "pinCommit": "c" * 40, "outDir": str(out), "inputsDir": str(out.parent),
                "stopFile": "S-r2", "globalStopFile": "S", "width": 8, "dimensions": dims, "duplicationPass": False,
                "shards": [{"id": s, "name": s, "lines": 9 * len(fs), "input": "in-%s.json" % s, "files": fs,
                            "fileLines": [9] * len(fs)} for s, fs in files.items()]}

    def refuses(fn) -> bool:
        try:
            fn()
        except SystemExit:
            return True
        return False

    tracked = {"src/A.cs", "src/B.cs", "src/C.cs"}
    with tempfile.TemporaryDirectory() as tmp:
        out = Path(tmp)
        (out / "shards.json").write_text(json.dumps({"commit": "c", "shards": [
            {"id": "s1", "files": [["src/A.cs", 9], ["src/B.cs", 9]]}, {"id": "s2", "files": [["src/C.cs", 9]]}]}),
            encoding="utf-8")
        (out / "batch-args.json").write_text(json.dumps(batch_fixture(out, {"s1": ["src/A.cs", "src/B.cs"], "s2": ["src/C.cs"]},
                                                                      ["architecture", "performance", "duplication"])),
                                             encoding="utf-8")
        f = "s1--architecture--f1#%d"
        write(out / "review-s1--architecture--f1.jsonl", [
            {"type": "read", "file": "src/A.cs"}, rec(1), rec(2), rec(3), rec(4, files=["src/Nope.cs"]),
            rec(5, wave_kind="modernize"), rec(6, existing_note="PB1"), rec(7, design_ref="later"),
            rec(8, sites=["src/A.cs:5-50 (A.G)"], members=["N.A.G"]),
            rec(9, wave_kind="defect-for-fix-lane", kind="defect", harm=["wrong-answer"],
                spec_refs=[{"clause": "14.9.27.4", "text": "Otherwise, arithmetic-expression-1 is evaluated to produce an algebraic value"}]),
            rec(10), rec(10, title="a different record under the same id")])
        # a second finder found the SAME mechanism as f1#1 (overlapping site, same member): one note, both provenances
        write(out / "review-s1--architecture--f2.jsonl", [
            {"type": "read", "file": "src/B.cs"}, {"type": "done"},
            rec(1, id="s1--architecture--f2#1", files=["src/A.cs", "src/B.cs"], sites=["src/A.cs:95-120 (A.F1)", "src/B.cs:1-9"])])
        write(out / "review-s1--performance--f1.jsonl", [{"type": "read", "file": "src/A.cs"},
                                                         rec(1, id="s1--performance--f1#1")])
        write(out / "review-s2--architecture--f1.jsonl", [{"type": "read", "file": "src/C.cs"}, {"type": "done"}])
        write(out / "null-s2--architecture.jsonl", [{"type": "null-check", "missed": []}])
        f2 = "s1--architecture--f2#1"
        write(out / "refute-s1--architecture--c1--site.jsonl", [
            vote(f % 1, "site", False), vote(f % 2, "site", True), vote(f % 3, "site", False),
            vote(f % 8, "site", False, {"sites": ["src/A.cs:5-50 (A.G)"], "files": ["src/A.cs", "src/B.cs"], "severity": "Critical",
                                       "members": None}),
            vote(f2, "site", False)])
        write(out / "refute-s1--architecture--c1--rule.jsonl", [vote(f % 1, "rule", False), vote(f % 2, "rule", True),
                                                                vote(f % 8, "rule", False), vote(f2, "rule", False)])
        write(out / "refute-s1--architecture--c1--scenario.jsonl", [vote(f % 1, "scenario", True)])
        # a later launch decided f2#1's third lens in ANOTHER chunk's file: it still counts for that lens
        write(out / "refute-s1--architecture--c3--scenario.jsonl", [vote(f2, "scenario", False)], broken=False)
        rep = collect(out, tracked)
        ids = lambda b: sorted(r["id"] for r in rep[b])  # noqa: E731
        arm("two of three skeptics failing to refute UPHOLDS", ids("upheld") == sorted([f % 1, f % 8, f2]))
        arm("two refutations REFUTE", ids("refuted") == [f % 2])
        arm("one vote is UNVERIFIED, never filed", ids("unverified") == [f % 3])
        inv = {r["id"]: r["problems"] for r in rep["invalid"]}
        arm("a file not in the pin's tree and a bad design_ref are INVALID",
            "file not in the pin's tree: src/Nope.cs" in inv.get(f % 4, []) and f % 7 in inv)
        arm("a design_ref is its leading §8.x / PBnnnn token; a qualifier after it is commentary",
            all(DESIGN_REF.match(s) for s in ("§8.3", "§8.3 (2)", "§8.3(2)", "§8.4 (PB2291, PB2333)", "§8.7 (b)",
                                              "§8.7 (R4 step 4); escalating CA1305", "PB948", "PB948 (code half)"))
            and not any(DESIGN_REF.match(s) for s in ("later", "§5.1 (consistent with §8)", "§80", "PB", "8.3")))
        silent_only = rec(11, wave_kind="defect-for-fix-lane", kind="defect", harm=["silent"],
                          spec_refs=[{"clause": "14.9.27.4", "text": "Otherwise, arithmetic-expression-1 is evaluated to produce an algebraic value"}])
        arm("a defect whose only harm is `silent` is INVALID: work.py next ranks only HARM_FLAGS",
            any("at least one of" in p for p in problems(silent_only, tracked))
            and not any("at least one of" in p for p in problems(dict(silent_only, harm=["wrong-answer", "silent"]), tracked)))
        arm("a defect whose clause does not hold its text is INVALID (cite.py's check)",
            any("cite.py --check" in p for p in inv.get(f % 9, [])))
        arm("a defect whose clause holds its text passes the check", spec_ref_problems([{
            "clause": "14.9.8.4", "text": "Otherwise, arithmetic-expression-1 is evaluated to produce an algebraic value"}]) == [])
        arm("two different records under one id are INVALID, never last-wins", any("two different" in p for p in inv.get(f % 10, [])))
        arm("a modernize point with no analyzer rule is a lead", ids("leads") == [f % 5, "s1--performance--f1#1"])
        arm("a performance finding with no measurement is a lead",
            any("measurement" in r["lead_reason"] for r in rep["leads"]))
        arm("an existing note is not filed again", ids("already_tracked") == [f % 6])
        up = {r["id"]: r for r in rep["upheld"]}
        arm("an upholding skeptic's correction is applied to the fields of ITS lens only",
            up[f % 8]["files"] == ["src/A.cs", "src/B.cs"] and up[f % 8]["severity"] == "Warning" and
            [a["field"] for a in up[f % 8]["corrections_applied"]] == ["files"])
        arm("findings of two finders on the same site and member are ONE mechanism, both ids kept",
            {"primary": f % 1, "ids": sorted([f % 1, f2]), "why": ["the same wave kind on overlapping members"]} in rep["mechanisms"])
        arm("a different member at a different site stays its own mechanism",
            {"primary": f % 8, "ids": [f % 8], "why": []} in rep["mechanisms"])
        p = rep["pairs"]
        arm("the union of two finders' reads makes a pair COMPLETE", p["s1--architecture"]["complete"])
        arm("a pair missing a file lists it", p["s1--performance"]["missing"] == ["src/B.cs"])
        arm("a complete, examined null is ACCEPTED", p["s2--architecture"]["null_accepted"])
        arm("a pair the batch asked for that wrote nothing is reported, not absent",
            p["s2--performance"].get("never_started") and not p["s2--performance"]["null_accepted"])
        arm("a shard's duplication lens is a pair like any other", "s2--duplication" in p and "s1--duplication" in p)
        arm("corrupt lines are counted (finders' and skeptics'), never fatal", p["s1--architecture"]["corrupt_lines"] == 5)
        arm("one file per pair is written", (out / "s1--architecture.json").exists())
        arm("an incomplete batch says RELAUNCH, naming why",
            not rep["complete"] and "LAUNCH NEEDED" in summary(rep) and any("unverified" in x for x in rep["relaunch"]))
        (out / "review-s1--architecture.jsonl").write_text(json.dumps(rec(11)) + "\n", encoding="utf-8")
        rep3 = collect(out, tracked)
        arm("a .jsonl the format does not name is reported and holds the batch open, never silently skipped",
            rep3["ignored_files"] == ["review-s1--architecture.jsonl"] and any("not named" in x for x in rep3["relaunch"]))
        (out / "review-s1--architecture.jsonl").unlink()
        rep2 = collect(out, tracked)
        arm("collection is idempotent", json.dumps(rep2, sort_keys=True) == json.dumps(rep, sort_keys=True))
        st = status(out, "s1--architecture")
        arm("--status is the PAIR's, from disk: reads of every finder, every finding, decisions per lens",
            st["read"] == ["src/A.cs", "src/B.cs"] and f2 in st["findings"] and st["finders"] == 2 and
            f2 in st["decided"]["scenario"] and f % 3 not in st["decided"]["rule"] and
            st["next_n"]["s1--architecture--f1"] == 11)
        pl = plan(out)
        by = {e["slug"]: e for e in pl["pairs"]}
        e = by["s1--architecture"]
        arm("the plan is the pair's WORK LEFT, from disk: no read file, the finding count, the undecided per lens",
            e["unread"] == [] and e["finders"] == 2 and e["findings"] == 11 and f % 3 in e["undecided"]["rule"]
            and f2 not in e["undecided"]["scenario"] and f % 1 not in e["undecided"]["site"])
        arm("the plan sends finders to exactly the unread files, with their lines (a never-started pair: all)",
            by["s1--performance"]["unread"] == [["src/B.cs", 9]] and by["s2--performance"]["unread"] == [["src/C.cs", 9]])
        arm("a finished pair (complete, null examined) is NOT in the plan", "s2--architecture" not in by
            and pl["decidedPairs"] == 1 and set(pl["shards"]) == {"s1", "s2"})
        arm("a plan of some shards holds only their pairs; an unknown shard is refused",
            {x["shard"] for x in plan(out, ["s2"])["pairs"]} == {"s2"} and refuses(lambda: plan(out, ["s9"])))
        roomy = {"session_soft_stop_pct": 97, "session_est_pct": 0.0, "headroom_pct": 90.0, "resume_at": None}
        js = out / "launch.js"
        pl, est = launch(out, roomy, 0.0, None, r2_cost.fixture_rules())
        text = js.read_text(encoding="utf-8") if js.exists() else ""
        planned = json.loads(text.split("const A = ", 1)[1].split("   // the plan of", 1)[0]) if text else None
        arm("a launch that fits writes launch.js: the template with its plan line replaced by the plan from disk",
            est["fits"] and planned == pl and not PLAN_LINE.search(text) and "CHECKPOINT PER DECISION" in text
            and text.count("const A = ") == 1)
        pl, est = launch(out, dict(roomy, session_est_pct=96.99), 0.0, None, r2_cost.fixture_rules())
        arm("an over-budget launch is REFUSED with its estimate and split, and the earlier launch.js is removed",
            not est["fits"] and est["parts"] and est["session_points"] > 0 and not js.exists())
        (out / "batch-args.json").write_text(json.dumps(dict(json.loads((out / "batch-args.json").read_text(
            encoding="utf-8")), outDir=str(out / "elsewhere"))), encoding="utf-8")
        arm("the plan of a batch whose args name another directory is refused", refuses(lambda: plan(out)))
        ck = out / "review-s2--code--f1.jsonl"
        good = json.dumps(rec(1, id="s2--code--f1#1", files=["src/C.cs"], sites=["src/C.cs:1-4 (C.F)"]))
        arm("--append writes a valid finding", append(ck, good, tracked).startswith("APPENDED") and
            status(out, "s2--code")["findings"] == ["s2--code--f1#1"])
        arm("--append of the identical line is a no-op", append(ck, good, tracked).startswith("ALREADY") and
            len(read_jsonl(ck)) == 1)
        for name, line, path in (
                ("a different record under a used id", json.dumps(rec(1, id="s2--code--f1#1", title="other")), ck),
                ("an id outside the file's prefix", json.dumps(rec(2, id="s2--code--f9#2")), ck),
                ("a record the collector would reject", json.dumps(rec(3, id="s2--code--f1#3", files=["src/Nope.cs"])), ck),
                ("a verdict of another lens", json.dumps(vote("x", "rule", False)), out / "refute-s2--code--c1--site.jsonl"),
                ("text that is not JSON", "{\"type\": ", ck)):
            try:
                append(path, line, tracked)
                arm("--append refuses " + name, False)
            except ValueError:
                arm("--append refuses " + name, True)
    with tempfile.TemporaryDirectory() as tmp:
        # O(undecided): one open pair, then 40 more pairs decided on disk; the plan and the launch grow by nothing
        roomy = {"session_soft_stop_pct": 97, "session_est_pct": 0.0, "headroom_pct": 90.0, "resume_at": None}
        sizes = []
        for n in (1, 40):
            out = Path(tmp) / ("b%02d" % n)   # equal-length names: the paths add no byte
            out.mkdir()
            files = {"open": ["src/Open.cs"]}
            files.update({"done%d" % i: ["src/Some/Long/Directory/Path/File%d_%d.cs" % (i, j) for j in range(5)]
                          for i in range(n)})
            (out / "batch-args.json").write_text(json.dumps(batch_fixture(out, files, ["code"])), encoding="utf-8")
            for i in range(n):
                write(out / ("review-done%d--code--f1.jsonl" % i), [{"type": "read", "file": x} for x in files["done%d" % i]]
                      + [{"type": "done"}], broken=False)
                write(out / ("null-done%d--code.jsonl" % i), [{"type": "null-check", "missed": []}], broken=False)
            pl, _ = launch(out, roomy, 0.0, None, r2_cost.fixture_rules())
            sizes.append((len(json.dumps(pl)), (out / "launch.js").stat().st_size, pl["decidedPairs"]))
        arm("a relaunch with N decided pairs carries O(undecided) bytes: 40 decided pairs add nothing but a count",
            sizes[0][2] == 1 and sizes[1][2] == 40 and sizes[1][0] - sizes[0][0] <= 1 and sizes[1][1] - sizes[0][1] <= 1)
    print("=== R2 COLLECT SELF-TEST: %s ===" % ("PASS" if ok else "FAIL"))
    return 0 if ok else 1


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", type=Path)
    ap.add_argument("--launch", action="store_true",
                    help="with --out: size the next launch's plan and write <batch dir>/launch.js when it fits")
    ap.add_argument("--shards", help="with --launch: plan only these shards (comma-separated; `global` is the clone "
                                     "pass), the parts a refused launch names")
    r2_cost.add_budget_args(ap)
    ap.add_argument("--status", nargs=2, metavar=("BATCH_DIR", "PAIR"))
    ap.add_argument("--append", nargs=2, metavar=("CHECKPOINT", "JSON"))
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    if a.status:
        print(json.dumps(status(Path(a.status[0]), a.status[1]), indent=1))
        return 0
    if a.append:
        try:
            print(append(Path(a.append[0]), a.append[1]))
        except ValueError as e:
            print("REFUSED: %s" % e)
            return 1
        return 0
    if a.shards and not a.launch:
        ap.error("--shards plans a launch: it needs --launch")
    if not a.out:
        ap.error("--out is required")
    if a.launch:
        only = [s.strip() for s in a.shards.split(",") if s.strip()] if a.shards else None
        pl, est = launch(a.out, r2_cost.budget_from(a), a.session_reserve, only)
        flags = "".join((" --borrow-days %d" % a.borrow_days if a.borrow_days else "",
                         " --session-reserve %g" % a.session_reserve if a.session_reserve else ""))
        print(r2_cost.report(est, "launch of %s%s" % (a.out, " (shards %s)" % a.shards if a.shards else ""),
                             lambda p, i: "python scripts/arch/r2_collect.py --out %s --launch --shards %s%s" % (
                                 a.out, ",".join(p["shards"]), flags)))
        if not pl["pairs"]:
            print("=== R2 LAUNCH: NOTHING TO LAUNCH — every pair of %s is decided (%d); collect it: "
                  "r2_collect.py --out %s ===" % (a.out, pl["decidedPairs"], a.out))
            return 0
        if not est["fits"]:
            print("=== R2 LAUNCH: REFUSED — the plan cannot finish in the room left; launch the parts above, one "
                  "call each (no launch.js written) ===")
            return r2_cost.REFUSED
        js = a.out / "launch.js"
        print("=== R2 LAUNCH: WROTE %s — %d pair(s) with work (%d decided, not in the plan), %d bytes; launch it "
              "with Workflow({scriptPath: \"%s\"}) and no args ===" % (
                  js, len(pl["pairs"]), pl["decidedPairs"], js.stat().st_size, str(js).replace("\\", "\\\\")))
        return 0
    rep = collect(a.out)
    print("=== R2 COLLECT: %s ===" % summary(rep))
    return 0


if __name__ == "__main__":
    sys.exit(main())
