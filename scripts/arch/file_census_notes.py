#!/usr/bin/env python3
"""file_census_notes.py — files the architecture census's findings as kb/Work notes (kb/Work PB2119, the Delete program).

    python scripts/arch/file_census_notes.py <findings.json> --ids PB2155-PB2214[,PB2215-PB2274] [--targets T.json]
                                             [--rerender] [--dry-run] [--notes-dir DIR]
    python scripts/arch/file_census_notes.py --r2 <batch dir>/collected.json [--ids PBa-PBb] [--dry-run]
    python scripts/arch/file_census_notes.py --self-test

R2 (kb/Work PB2560; DESIGN-architecture-review §3 R2): `--r2` files each UPHELD finding of a review batch (decided by
scripts/arch/r2_collect.py from the fleet's JSON lines) as one note of cluster PB1754 — `kind: analysis` for a
restructuring finding, `kind: defect` for a defect the fleet hands the fix lane — with its sites as backticked paths
(so `work.py note_sites`, `fix_clusters.py` and the planner resolve them), its members as the `**Moves or changes:**`
line (the member index computes the wave's callers from it), its census findings as `census_refs:` (never
`census_ids:`, which is the census filer's own key), and its finding id on `r2_ids:` (the idempotence key). Ids come
from `scripts/orchestrator/alloc.py` unless --ids is given. A note naming a path the committed tree lacks is refused.

One note = one mechanism a single Delete / Unify / Move / Extract wave can take. The grouping is fixed by kind:
  dead-artifact       one note per (class, directory)
  unreachable-type /
  unreachable-family  one note per primary src file (families whose members live in the same file merge)
  clone-family        one note per family
  folder-namespace    one note per disagreement
  god-class           filed ONLY with --targets, the design record of DESIGN-architecture-review §8.3 (R1, kb/Work
                      PB2118), kept in the repository at docs/rearchitecture/evidence/arch-census/targets/<sha>.json
                      beside the census record it refines; a god-class finding with no record there is refused,
                      never guessed. A record with `steps` is a designed extraction ORDER: the finding's note is
                      step 1 and every later step is its own note (one note = one wave, because plan_wave.py groups
                      notes), blocked_by the step before it. A note claims ONLY its own step: the class note's target,
                      seam and edited files are step 1's (its Sites stay the census finding's, and the finding's whole
                      target list stays behind its census_ids back-link), and it names the later steps by note id
                      only, so no extraction is claimed twice.
                      A record's `removes` (or a step's) names the §8.1 tolerated rows that note removes. A step's
                      `moves` names what it moves or changes (`Ns.Type.Member`, or a type): the note renders it as
                      its `**Moves or changes:**` line, from which scripts/arch/member_index.py computes the callers
                      the wave edits (PB2118 Draft 8, J2); a step without it falls back to every type its Sites
                      declare. Every path a step's `files` or a designed record's `sites` names must exist (or be
                      marked `(new)`), or nothing is filed.
The --targets file also carries `designed` records: waves §8 designs that no census finding names (each tolerated
row's removal in §8.1's edges file, a grammar regroup, a re-model). Each is one note keyed by its `key`.

Targets file (schema 2):
  {"schema": 2, "census": "<sha>", "source": "...",
   "targets": {"R0-nnnn": {"targets": [...], "seam": "...", "order": "A|B|C|C+B", "order_text": "...",
                           "blocked_by": [...], "note": "...", "removes": [[from, to, uses], ...],
                           "steps": [{"target": "...", "seam": "...", "files": [...], "moves": [...], "blocked_by": [...],
                                      "note": "...", "removes": [...]}, ...]}},
   "designed": [{"key": "T-...", "wave": "extract|move-rename|grammar|re-model|measure", "what": "...", "sites": [...],
                 "removes": [[from, to, uses], ...], "targets": [...], "seam": "...", "order": "...",
                 "order_text": "...", "blocked_by": [...], "note": "...", "model": "...", "claims": [{"file": "...", "kinds": [...]}]}]}
  A blocked_by entry is a note id (PBnnnn, Rnn), a step reference (`R0-nnnn` = step 1, `R0-nnnn#k`, `R0-nnnn#last`)
  or a designed key; references resolve to note ids when the notes are rendered. A designed record's `claims` names
  (file, finding kinds) it already covers (BoundTree.cs's family split claims the §8.2 rule 2-3 findings that kb/Work
  PB2351 makes the census report); a finding of a claimed kind on a claimed file is skipped (`claimed-by-design`).

Never filed: test-only-family / test-only-type ("only tests use it" is a judgment, reported as NEEDS-OPUS), and
god-class without --targets. Unknown kinds are reported, never guessed.

Idempotent: a finding whose id already appears in any note's `census_ids:` line, and a step or designed record whose
key appears in any note's `design_keys:` line, is skipped, so the next run files only what is new. --rerender
rewrites every OPEN note the targets file renders (god-class steps and designed records) whose text differs from what
the file renders now, so the notes and the file never disagree (a note is never hand-edited; its record is). Every
number and site in a note comes from the findings file or the targets file; nothing is measured here.
"""
import argparse
import json
import os
import re
import sys
import pathlib
import subprocess
import tempfile
from collections import OrderedDict

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(REPO, "scripts", "spec"))
import work  # noqa: E402 — named_missing_paths: the one rule that a path a note names exists
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
blocked: {blocked}
blocked_by: {blocked_by}
cluster: ["PB2119"]
spec_refs: []
inventory_rows: []
closes_rows: []
closes_rows_reason: "{reason}"
tags: [cobolsharp, work, analysis]
---
"""

REASON = {"extract": "behavior-neutral restructuring (an Extract wave of the PB2119 program; owner R69 §2-§3); "
                      "no inventory row"}
DEFAULT_REASON = "behavior-neutral restructuring (Delete program PB2119, owner R69 §3); no inventory row"
NOT_FILED = {"god-class": "decomposition targets come from R1 (kb/Work PB2118): pass --targets",
             "test-only-family": "NEEDS-OPUS: 'only tests use it' is a judgment",
             "test-only-type": "NEEDS-OPUS: 'only tests use it' is a judgment",
             "claimed-by-design": "a designed record of the --targets file already covers this finding's file"}
WAVE_OF_KIND = {"dead-artifact": "delete", "unreachable-type": "delete", "unreachable-family": "delete",
                "clone-family": "unify", "folder-namespace": "move-rename", "god-class": "extract"}
PREFIX = {"delete": "Delete:", "unify": "Unify:", "move-rename": "Move/rename:", "extract": "Extract:",
          "grammar": "Grammar:", "re-model": "Re-model:", "measure": "Measure:"}
EXTRACT_MODEL = "Opus (an extract wave: kb/Work R69 §4; `model_rules.json` routes area architecture to Opus)"


def frontmatter(title, nid, blocked_by=(), reason=DEFAULT_REASON):
    return FRONTMATTER.format(title=title, nid=nid, blocked="true" if blocked_by else "false",
                              blocked_by=json.dumps(list(blocked_by)), reason=reason)


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


def scan_notes(notes_dir):
    """-> ({census id: note id}, {design key: note id}, {note id: status})."""
    census, design, status = {}, {}, {}
    for name in os.listdir(notes_dir):
        if not name.endswith(".md"):
            continue
        nid = name[:-3]
        with open(os.path.join(notes_dir, name), encoding="utf-8") as fh:
            for line in fh:
                if line.startswith("census_ids:"):
                    for x in line[len("census_ids:"):].split(","):
                        if x.strip():
                            census.setdefault(x.strip(), nid)
                elif line.startswith("design_keys:"):
                    for x in line[len("design_keys:"):].split(","):
                        if x.strip():
                            design.setdefault(x.strip(), nid)
                elif line.startswith("status:") and nid not in status:
                    status[nid] = line[len("status:"):].strip()
    return census, design, status


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


def site_files(f):
    s = f.get("site")
    if isinstance(s, str):
        return {s.split(":")[0]}
    if isinstance(s, dict):
        return set(s.get("files", [])) | {member_file(m) for m in s.get("members", [])} - {None}
    if isinstance(s, list):
        return {x.split(":")[0].split(" ")[0] for x in s}
    return set()


class Design:
    """The --targets file plus the note ids its references resolve to."""

    def __init__(self, tf, census_ids=None, design_ids=None):
        self.targets = tf.get("targets", {})
        self.designed = tf.get("designed", [])
        self.census_ids = dict(census_ids or {})   # R0 id -> note id (step 1)
        self.design_ids = dict(design_ids or {})   # design key -> note id
        self.claims = {(c["file"], k) for d in self.designed for c in d.get("claims", []) for k in c["kinds"]}

    def step_count(self, rid):
        return max(1, len(self.targets[rid].get("steps", [])))

    def new_keys(self):
        """Every step (k >= 2) and designed key, in filing order: the targets in census order, then `designed`."""
        keys = []
        for rid, t in self.targets.items():
            keys += ["%s#%d" % (rid, k) for k in range(2, len(t.get("steps", [])) + 1)]
        keys += [d["key"] for d in self.designed]
        return keys

    def resolve(self, ref):
        if re.fullmatch(r"(PB|R)\d+", ref):
            return ref
        m = re.fullmatch(r"(R0-\d+)(?:#(\d+|last))?", ref)
        if m:
            rid, k = m.group(1), m.group(2) or "1"
            if rid not in self.targets:
                raise SystemExit("blocked_by names %s, which has no targets record" % ref)
            k = self.step_count(rid) if k == "last" else int(k)
            nid = self.census_ids.get(rid) if k == 1 else self.design_ids.get("%s#%d" % (rid, k))
        else:
            nid = self.design_ids.get(ref)
        if nid is None:
            raise SystemExit("blocked_by names %s, which resolves to no filed or allocated note" % ref)
        return nid

    def resolve_all(self, refs):
        out = []
        for r in refs:
            nid = self.resolve(r)
            if nid not in out:
                out.append(nid)
        return out


def blocked_sentence(ids):
    tail = []
    if "PB2118" in ids:
        tail.append("PB2118: §8, the target this wave executes, approved by the owner 2026-10-07")
    if "R69" in ids:
        tail.append("R69 §1: Cut 3 is the v1.0 cut, a release decision on the public runtime namespaces (PB2349); "
                    "R69 leaves this list at v1.0")
    return ", ".join(ids) + (" (" + "; ".join(tail) + ")" if tail else "")


def contract(wave):
    return ("**The wave's contract:** every caller changes in the same change (CLAUDE.md rule 4: no alias, shim or "
            "forwarder); the drift test that pinned the thing goes with it, or a new boundary gets one; the §4 oracle "
            "(`python scripts/arch/compare_oracle.py`) proves neutrality; a %s that changes behavior is a "
            "`kind: defect` lead for the fix lane, never part of the wave (PB2119)." % (
                "deletion" if wave == "delete" else "change"))


def render(group, nid, record_path, short_sha, design=None):
    fs = group["findings"]
    k = fs[0]["kind"]
    key = group["key"]
    ids = [f["id"] for f in fs]
    wave = WAVE_OF_KIND[k]
    sites, measured, targets = [], [], []
    needs_opus = [f["id"] for f in fs if "design" in fix_text(f.get("scenario", "")).lower()]
    t = None

    if key[0] == "dead-artifact":
        d = key[2] or "(repository root)"
        what = "%d dead %s artifact(s) in %s" % (len(fs), key[1], d)
        for f in fs:
            sites.append("- `%s` (%s)" % (f["site"], fix_text(f["scenario"])))
    elif key[0] == "unreachable":
        n = sum(f["measure"]["members"] for f in fs)
        types = []
        for f in fs:
            ty = f["site"].get("family") or f["site"].get("type")
            if ty and ty not in types:
                types.append(ty)
        what = "%s — %d unreferenced members of %s" % (key[1], n, ", ".join(short_type(x) for x in types))
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
    elif k == "god-class":
        f = fs[0]
        t = design.targets.get(f["id"]) if design else None
        if t is None:
            raise SystemExit("no --targets record for god-class finding %s (%s): R1 names the targets, never this "
                             "script" % (f["id"], f["site"]["type"]))
        mm = f["measure"]
        what = "%s — %d lines, %d members, %d file(s) into %d target type(s)" % (
            short_type(f["site"]["type"].replace("+", ".")), mm["lines"], mm["members"], len(f["site"]["files"]),
            len(t["targets"]))
        if t.get("steps"):
            what += "; step 1 of %d: %s" % (len(t["steps"]), t["steps"][0]["target"])
        sites.append("- type `%s`" % f["site"]["type"])
        sites.extend("- `%s`" % x for x in f["site"]["files"])
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
        tg = fix_text(f.get("target", ""))
        if tg not in targets:
            targets.append(tg)

    # the model that may take the wave
    claims_design = [f["id"] for f in fs if f["kind"] == "clone-family"]
    if wave == "extract":
        who = EXTRACT_MODEL
    elif needs_opus or claims_design:
        who = ("NEEDS-OPUS: the finding's scenario or target names a design claim (%s); a Sonnet wave may not decide "
               "it" % ", ".join(needs_opus or claims_design))
    else:
        who = "Sonnet (census-measured, mechanical)"

    title = "%s — %s %s (census %s at %s)" % (nid, PREFIX[wave], what, ", ".join(ids), short_sha)
    body = []
    steps = (t or {}).get("steps") or []
    bb = design.resolve_all(t["blocked_by"] + steps[0].get("blocked_by", []) if steps else t["blocked_by"]) if t else []
    body.append(frontmatter(title.replace('"', "'"), nid, bb, REASON.get(wave, DEFAULT_REASON)).rstrip("\n"))
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
    body.append(contract(wave))
    body.append("")
    if t:
        if steps:
            chain = [nid] + [design.resolve("%s#%d" % (fs[0]["id"], j)) for j in range(2, len(steps) + 1)]
            body.append("**This note's target (DESIGN-architecture-review §8.3), step 1 of %d:** %s." % (
                len(steps), steps[0]["target"]))
            body.append("")
            if steps[0].get("files"):
                body.append("**The files this step edits** (the Sites above are the census finding's, the whole "
                            "class): " + ", ".join("`%s`" % x for x in steps[0]["files"]) + ".")
                body.append("")
            body.extend(file_set_lines(steps[0].get("moves")))
            body.append("**It claims only that step** (one note = one wave; `plan_wave.py` groups notes). Steps 2–%d are "
                        "their own notes, each blocked by the one before it, and each claims its own target: %s." % (
                            len(steps), ", ".join("%d %s" % (j + 1, chain[j]) for j in range(1, len(steps)))))
            body.append("")
        else:
            body.append("**Target types (DESIGN-architecture-review §8.3):** " + "; ".join(t["targets"]) + ".")
            body.append("")
        if t.get("removes"):
            body.append(removes_line(t["removes"]))
            body.append("")
        body.append("**Seam:** " + (steps[0].get("seam") or t["seam"] if steps else t["seam"]) + ".")
        body.append("")
        body.append("**Extraction order:** " + t["order_text"] + (" — " + t["note"] if t.get("note") else "") + ".")
        body.append("")
        body.append("**Blocked by:** " + blocked_sentence(bb) + ". The wave proves the §4 contract and adds the drift "
                    "test that keeps its new boundary true.")
        body.append("")
    else:
        body.append("**Target:** " + " | ".join(targets))
        body.append("")
    return "\n".join(body), title


def render_step(rid, k, nid, finding, design, record_path, short_sha):
    """Step k >= 2 of a designed extraction order: its own note, blocked by step k-1."""
    t = design.targets[rid]
    steps = t["steps"]
    s = steps[k - 1]
    ty = finding["site"]["type"]
    files = s.get("files") or finding["site"]["files"]
    prev = design.resolve("%s#%d" % (rid, k - 1)) if k > 2 else design.resolve(rid)
    bb = design.resolve_all([prev] + s.get("blocked_by", []))
    what = "%s step %d of %d — %s" % (short_type(ty.replace("+", ".")), k, len(steps), s["target"])
    title = "%s — Extract: %s (census %s at %s)" % (nid, what, rid, short_sha)
    body = [frontmatter(title.replace('"', "'"), nid, bb, REASON["extract"]).rstrip("\n"), "",
            "design_keys: %s#%d" % (rid, k), "",
            "**Wave kind:** extract (step %d of %d of `%s`, census %s). **Model:** %s." % (k, len(steps), ty, rid,
                                                                                          EXTRACT_MODEL), "",
            "**Sites:**", "- type `%s`" % ty]
    body += ["- `%s`" % x for x in files]
    body += ["", "**Measured how:** the R0 census record `%s` (finding %s; its measures are on step 1, %s)."
             % (record_path, rid, design.resolve(rid)), "", contract("extract"), "",
             "**This step's target (DESIGN-architecture-review §8.3):** " + s["target"] + ".", ""]
    body += file_set_lines(s.get("moves"))
    body += ["**Seam:** " + (s.get("seam") or t["seam"]) + ".", ""]
    if s.get("removes"):
        body += [removes_line(s["removes"]), ""]
    body += ["**Extraction order:** step %d of %d; the step before it is %s%s." % (
                 k, len(steps), prev, (" — " + s["note"]) if s.get("note") else ""), "",
             "**Blocked by:** " + blocked_sentence(bb) + ". The wave proves the §4 contract and adds the drift test "
             "that keeps its new boundary true.", ""]
    return "\n".join(body), title


def file_set_lines(moves):
    """The note's computed file set (PB2118 Draft 8, J2): its `Moves or changes` names, when the record gives them,
    and the sentence that says the callers are computed, never listed."""
    out = []
    if moves:
        out += ["**Moves or changes:** " + ", ".join("`%s`" % m for m in moves) + ".", ""]
    out += ["**Its file set is computed, never listed** (DESIGN-architecture-review §8.7): the files this note names, "
            "plus every file `scripts/arch/member_index.py` finds using or emitting what it moves or changes (" +
            ("the names above and every member of their types its target names" if moves
             else "every type its Sites declare") + "), on the index of the current tree; "
            "`plan_wave.py --cluster` admits the wave on that set, the landing check (`landing_check.py`, run by "
            "push-main.sh) stops a landing that changed a file outside it, and `python scripts/arch/member_index.py "
            "--note <id>` prints it.", ""]
    return out


def missing_design_paths(design, root):
    """The paths the targets file names (steps' `files`, designed records' `sites`) that do not exist under `root`."""
    spans = ["`%s`" % f for t in design.targets.values() for s in t.get("steps") or [] for f in s.get("files") or []]
    spans += [x for d in design.designed for x in d.get("sites") or []]
    return work.named_missing_paths(" ".join(spans), pathlib.Path(root))


def removes_line(removes):
    return ("**Removes (DESIGN-architecture-review §8.1, `tolerated`, census uses at the R0 record):** " +
            "; ".join("`%s → %s` %s" % (a, b, u) for a, b, u in removes) +
            ". The row's `removedBy` names this note; the row is deleted when both observers read zero.")


def render_designed(d, nid, design):
    wave = d["wave"]
    bb = design.resolve_all(d["blocked_by"])
    title = "%s — %s %s" % (nid, PREFIX[wave], d["what"])
    body = [frontmatter(title.replace('"', "'"), nid, bb, d.get("reason") or REASON["extract"]).rstrip("\n"), "",
            "design_keys: " + d["key"], "",
            "**Wave kind:** %s. **Model:** %s." % (wave, d.get("model") or EXTRACT_MODEL), "",
            "**Sites:**"]
    body += ["- " + x for x in d["sites"]]
    body.append("")
    body += file_set_lines(d.get("moves"))
    if d.get("removes"):
        body.append(removes_line(d["removes"]))
        body.append("")
    body += [contract(wave), "",
             "**Target (DESIGN-architecture-review §8):** " + "; ".join(d["targets"]) + ".", "",
             "**Seam:** " + d["seam"] + ".", "",
             "**Extraction order:** " + d["order_text"] + ((" — " + d["note"]) if d.get("note") else "") + ".", "",
             "**Blocked by:** " + blocked_sentence(bb) + ".", ""]
    return "\n".join(body), title


def file_notes(findings_path, id_pool, notes_dir, dry_run, targets=None, rerender=False, path_root=REPO):
    """-> (written, skipped, unused ids, rewritten). `targets` is the --targets file's parsed JSON (or None);
    `path_root` is where the targets file's paths must exist (the repository)."""
    with open(findings_path, encoding="utf-8") as fh:
        findings = json.load(fh)["findings"]
    base = os.path.basename(findings_path)
    sha = base.split(".")[0]
    record_path = "%s/%s.json" % (RECORD_DIR, sha)
    census, dkeys, status = scan_notes(notes_dir)
    design = Design(targets, census, dkeys) if targets is not None else None
    if design and (missing := missing_design_paths(design, path_root)):
        raise SystemExit("the targets file names paths that do not exist: " + ", ".join(missing))
    by_id = {f["id"]: f for f in findings}
    skipped, todo = {}, []
    for f in findings:
        if f["kind"] in NOT_FILED and not (f["kind"] == "god-class" and targets is not None):
            skipped.setdefault(f["kind"], []).append(f["id"])
        elif f["kind"] not in WAVE_OF_KIND:
            skipped.setdefault("UNKNOWN:" + f["kind"], []).append(f["id"])
        elif f["id"] in census:
            skipped.setdefault("already-filed", []).append(f["id"])
        elif design and any((p, f["kind"]) in design.claims for p in site_files(f)):
            skipped.setdefault("claimed-by-design", []).append(f["id"])
        else:
            todo.append(f)
    groups = group_findings(todo)
    new_keys = [k for k in design.new_keys() if k not in dkeys] if design else []
    if len(groups) + len(new_keys) > len(id_pool):
        raise SystemExit("need %d ids, only %d allocated" % (len(groups) + len(new_keys), len(id_pool)))
    # allocate every id first, so a reference to a note filed later in this run resolves
    gids = id_pool[:len(groups)]
    kids = id_pool[len(groups):len(groups) + len(new_keys)]
    if design:
        for g, nid in zip(groups, gids):
            if g["findings"][0]["kind"] == "god-class":
                design.census_ids[g["findings"][0]["id"]] = nid
        design.design_ids.update(zip(new_keys, kids))
    out = []
    for g, nid in zip(groups, gids):
        out.append((nid, g["findings"][0]["kind"]) + render(g, nid, record_path, sha[:9], design))
    designed = {d["key"]: d for d in design.designed} if design else {}
    for key, nid in zip(new_keys, kids):
        if key in designed:
            out.append((nid, "designed") + render_designed(designed[key], nid, design))
        else:
            rid, k = key.split("#")
            out.append((nid, "god-class-step") + render_step(rid, int(k), nid, by_id[rid], design, record_path,
                                                             sha[:9]))
    written = []
    for nid, kind, text, title in out:
        path = os.path.join(notes_dir, nid + ".md")
        if os.path.exists(path):
            raise SystemExit("refusing to overwrite " + path)
        if not dry_run:
            with open(path, "w", encoding="utf-8", newline="\r\n") as fh:
                fh.write(text)
        written.append((nid, kind, title))
    rewritten = []
    if rerender and design:
        mine = [(nid, "god") for rid, nid in census.items() if rid in design.targets] + \
               [(nid, key) for key, nid in dkeys.items()]
        for nid, key in mine:
            if status.get(nid) != "open":
                continue
            if key == "god":
                rid = next(r for r, n in census.items() if n == nid)
                text, _ = render({"key": ("god-class", rid), "findings": [by_id[rid]]}, nid, record_path, sha[:9],
                                 design)
            elif key in designed:
                text, _ = render_designed(designed[key], nid, design)
            elif "#" in key and key.split("#")[0] in design.targets:
                rid, k = key.split("#")
                text, _ = render_step(rid, int(k), nid, by_id[rid], design, record_path, sha[:9])
            else:
                continue
            path = os.path.join(notes_dir, nid + ".md")
            with open(path, encoding="utf-8", newline="") as fh:
                have = fh.read().replace("\r\n", "\n")
            if have.rstrip("\n") != text.rstrip("\n"):
                if not dry_run:
                    with open(path, "w", encoding="utf-8", newline="\r\n") as fh:
                        fh.write(text)
                rewritten.append(nid)
    return written, skipped, id_pool[len(groups) + len(new_keys):], sorted(rewritten)


# ── R2: the review fleet's upheld findings (kb/Work PB2560; DESIGN-architecture-review §3 R2; the R2 review's B5) ──
R2_PREFIX = {"extract": "Extract:", "unify": "Unify:", "move-and-rename": "Move/rename:", "data-ize": "Data-ize:",
             "delete": "Delete:", "modernize": "Modernize:", "defect-for-fix-lane": "Defect:"}
R2_MODEL = {"extract": EXTRACT_MODEL, "unify": EXTRACT_MODEL, "data-ize": EXTRACT_MODEL,
            "move-and-rename": "Sonnet (a rewriter-driven wave, R69 §4); a judgment it meets returns NEEDS-OPUS",
            "delete": "Sonnet (a measured deletion, R69 §4); a judgment it meets returns NEEDS-OPUS",
            "modernize": "Sonnet (one analyzer rule with its code fix, §5.5; R69 §4)",
            "defect-for-fix-lane": "the fix lane's routing (`model_rules.json`)"}
R2_SEVERITY = {"Critical": "MAJOR", "Warning": "MINOR"}
R2_HARM = {"wrong-answer": "wrong_answer", "crashes": "crashes", "silent": "silent",
           "rejects-legal-source": "rejects_legal_source", "under-rejects": "under_rejects"}
R2_FRONTMATTER = """---
title: "{title}"
id: {nid}
kind: {kind}
status: open
severity: {severity}
area: {area}
wrong_answer: {wrong_answer}
crashes: {crashes}
silent: {silent}
rejects_legal_source: {rejects_legal_source}
under_rejects: {under_rejects}
process_only: {process_only}
blocked: false
blocked_by: []
cluster: ["PB1754"]
spec_refs: []
inventory_rows: []
closes_rows: []
closes_rows_reason: "{reason}"
tags: [cobolsharp, work, {kind}, r2]
---
"""


def scan_r2(notes_dir):
    """-> {R2 finding id: note id} from every note's `r2_ids:` line (the idempotence key of an R2 filing)."""
    out = {}
    for name in os.listdir(notes_dir):
        if name.endswith(".md"):
            with open(os.path.join(notes_dir, name), encoding="utf-8") as fh:
                for line in fh:
                    if line.startswith("r2_ids:"):
                        for x in line[len("r2_ids:"):].split(","):
                            if x.strip():
                                out.setdefault(x.strip(), name[:-3])
    return out


def render_r2(rec, nid, pin, areas):
    wave = rec["wave_kind"]
    defect = wave == "defect-for-fix-lane" or rec["kind"] == "defect"
    harm = {v: "true" if k in (rec.get("harm") or []) else "false" for k, v in R2_HARM.items()}
    title = "%s — %s %s (R2 %s at %s)" % (nid, R2_PREFIX[wave], rec["title"].rstrip("."), rec["pair"], pin[:9])
    fm = R2_FRONTMATTER.format(
        title=title.replace('"', "'"), nid=nid, kind="defect" if defect else "analysis",
        severity=R2_SEVERITY.get(rec["severity"], "MINOR"),
        area=areas.get(re.sub(r"-[0-9]+$", "", rec["shard"]), "architecture") if defect else "architecture",
        process_only="false" if defect else "true",
        reason=("open; an R2 review finding for the fix lane (PB1754): the inventory row it touches is decided when it "
                "is fixed") if defect else
               ("behavior-neutral restructuring (an R2 finding of the PB1754 review, DESIGN-architecture-review §3 R2); "
                "no inventory row"), **harm)
    body = [fm.rstrip("\n"), "", "r2_ids: " + rec["id"]]
    if rec.get("census_ids"):
        body.append("census_refs: " + ", ".join(rec["census_ids"]))
    body += ["", "**Wave kind:** %s. **Model:** %s." % (wave, R2_MODEL[wave]), "", "**Sites:**"]
    named = set()
    for s in rec["sites"]:
        m = re.match(r"^(\S+?)(:[\d,-]+)?(?:\s+\((.+)\))?$", s.strip())
        path, lines, sym = (m.group(1), m.group(2) or "", m.group(3)) if m else (s, "", None)
        named.add(path)
        body.append("- `%s%s`%s" % (path, lines, " (%s)" % sym if sym else ""))
    body += ["- `%s`" % f for f in rec["files"] if f not in named]
    body.append("")
    if not defect:
        body += file_set_lines(rec.get("members"))
    body += ["**Rule it breaks:** " + rec["rule"], "", "**Scenario:** " + rec["scenario"], ""]
    measured = "R2 batch finding `%s` (pair %s), read in the pinned tree at %s; skeptics %s." % (
        rec["id"], rec["pair"], pin[:12], ", ".join("%s %s" % (k, v) for k, v in sorted(rec.get("votes", {}).items())))
    if rec.get("measurement"):
        measured += " Measurement: " + rec["measurement"]
    if rec.get("analyzer_rule"):
        measured += " Analyzer rule: `%s`." % rec["analyzer_rule"]
    body += ["**Measured how:** " + measured, ""]
    if rec.get("corrections"):
        body += ["**Skeptics' corrections:** " + " | ".join(json.dumps(c, ensure_ascii=False) for c in rec["corrections"]), ""]
    body += ["**Target:** " + rec["target"], "", "**Design reference:** " + rec["design_ref"] + ".", ""]
    if rec.get("owner_question"):
        body += ["**Owner question:** " + rec["owner_question"], ""]
    if not defect:
        body += [contract("delete" if wave == "delete" else "extract"), ""]
    return "\n".join(body), title


def alloc_ids(n):
    """`n` kb/Work ids from the ONE locked allocator (scripts/orchestrator/alloc.py)."""
    if n == 0:
        return []
    out = subprocess.run([sys.executable, os.path.join(REPO, "scripts", "orchestrator", "alloc.py"), "pb", str(n)],
                         capture_output=True, text=True, check=True).stdout.strip().splitlines()[-1]
    m = re.fullmatch(r"PB(\d+)(?:-PB(\d+))?", out.strip())
    if not m:
        raise SystemExit("alloc.py answered %r" % out)
    a, b = int(m.group(1)), int(m.group(2) or m.group(1))
    if b - a + 1 != n:
        raise SystemExit("alloc.py reserved %s for %d notes" % (out, n))
    return ["PB%d" % k for k in range(a, b + 1)]


def file_r2(collected_path, id_pool, notes_dir, dry_run, path_root=REPO, areas=None):
    """-> (written, skipped {reason: [finding ids]}, unused ids). Only UPHELD findings are filed; a finding already
    filed (its id on an `r2_ids:` line) is skipped; a note naming a path the committed tree lacks is refused."""
    with open(collected_path, encoding="utf-8") as fh:
        rep = json.load(fh)
    if areas is None:
        sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
        import r2_subsystems
        areas = {s["key"]: s["area"] for s in r2_subsystems.TABLE}
    done = scan_r2(notes_dir)
    skipped = {}
    for b in ("refuted", "unverified", "leads", "invalid", "already_tracked"):
        if rep.get(b):
            skipped["not upheld: " + b] = [r["id"] for r in rep[b]]
    tracked = work.tracked_paths(pathlib.Path(path_root))
    todo = []
    for r in rep.get("upheld", []):
        named = set(r["files"]) | {x.strip().split(" ")[0].split(":")[0] for x in r["sites"]}
        missing = sorted(p for p in named if p not in tracked and not work.is_build_output(p))
        if r["id"] in done:
            skipped.setdefault("already-filed", []).append(r["id"])
        elif missing:   # the pin's path is gone from the committed tree: a stale finding, re-reviewed, never filed
            skipped.setdefault("stale-path", []).append("%s (%s)" % (r["id"], ", ".join(missing)))
        else:
            todo.append(r)
    if id_pool is None:   # ids go only to the notes written: a stale or filed finding takes none
        id_pool = ["PB(new%d)" % (i + 1) for i in range(len(todo))] if dry_run else alloc_ids(len(todo))
    if len(todo) > len(id_pool):
        raise SystemExit("need %d ids, only %d allocated" % (len(todo), len(id_pool)))
    written = []
    for r, nid in zip(todo, id_pool):
        text, title = render_r2(r, nid, rep["pin"], areas)
        path = os.path.join(notes_dir, nid + ".md")
        if os.path.exists(path):
            raise SystemExit("refusing to overwrite " + path)
        if not dry_run:
            with open(path, "w", encoding="utf-8", newline="\r\n") as fh:
                fh.write(text)
        written.append((nid, r["wave_kind"], title))
    return written, skipped, id_pool[len(written):]


def self_test_r2():
    def rec(i, **kw):
        r = {"id": "s1--architecture--f1#%d" % i, "pair": "s1--architecture", "shard": "s1", "dimension": "architecture",
             "kind": "finding", "title": "split T", "rule": "§5.1 single responsibility", "scenario": "two reasons",
             "target": "T and U", "files": ["src/T.cs"], "sites": ["src/T.cs:10-90 (N.T.F)"], "members": ["N.T.F"],
             "census_ids": ["R0-0001"], "design_ref": "§8.3", "wave_kind": "extract", "severity": "Warning",
             "existing_note": "", "owner_question": "", "votes": {"rule": "upheld", "scenario": "upheld", "site": "refuted"},
             "corrections": []}
        r.update(kw)
        return r
    with tempfile.TemporaryDirectory() as tmp:
        os.makedirs(os.path.join(tmp, "src"))
        for f in ("T.cs", "D.cs"):
            open(os.path.join(tmp, "src", f), "w").close()
        stage(tmp)
        nd = os.path.join(tmp, "Work")
        os.mkdir(nd)
        cp = os.path.join(tmp, "collected.json")
        with open(cp, "w", encoding="utf-8") as fh:
            json.dump({"pin": "abcdef0123456789", "upheld": [
                rec(1), rec(2, wave_kind="defect-for-fix-lane", kind="defect", harm=["wrong-answer"], files=["src/D.cs"],
                           sites=["src/D.cs:3"], severity="Critical"),
                rec(3, files=["src/Gone.cs"], sites=["src/Gone.cs:1"])],
                "refuted": [rec(9)], "leads": [rec(8)]}, fh)
        areas = {"s1": "binding"}
        w, s, tail = file_r2(cp, ["PB9101", "PB9102", "PB9103"], nd, False, tmp, areas)
        assert [x[0] for x in w] == ["PB9101", "PB9102"] and tail == ["PB9103"], (w, tail)
        assert s["stale-path"][0].startswith("s1--architecture--f1#3") and s["not upheld: refuted"] and s["not upheld: leads"], s
        with open(os.path.join(nd, "PB9101.md"), encoding="utf-8") as fh:
            a = fh.read()
        assert "kind: analysis" in a and 'cluster: ["PB1754"]' in a and "r2_ids: s1--architecture--f1#1" in a, a
        assert "census_refs: R0-0001" in a and "census_ids:" not in a, a   # never claims a census finding's filing key
        assert "- `src/T.cs:10-90` (N.T.F)" in a and "**Moves or changes:** `N.T.F`." in a and "Extract:" in a, a
        assert "severity: MINOR" in a and "area: architecture" in a and "skeptics rule upheld, scenario upheld, site refuted" in a
        with open(os.path.join(nd, "PB9102.md"), encoding="utf-8") as fh:
            d = fh.read()
        assert "kind: defect" in d and "wrong_answer: true" in d and "area: binding" in d and "severity: MAJOR" in d, d
        assert "process_only: false" in d and "**Moves or changes" not in d, d
        parsed = work.parse_frontmatter(a)
        assert parsed and parsed["kind"] == "analysis" and parsed["status"] == "open", parsed
        w2, s2, _ = file_r2(cp, ["PB9104", "PB9105"], nd, False, tmp, areas)
        assert not w2 and s2["already-filed"] == ["s1--architecture--f1#1", "s1--architecture--f1#2"], (w2, s2)
    print("self-test R2 OK")


def stage(root):
    """The self-test's scratch tree as a git repository with every file staged: `work.named_missing_paths` reads the
    git index, so a planted file exists for it only once staged."""
    if not os.path.isdir(os.path.join(root, ".git")):
        subprocess.run(["git", "-C", root, "init", "-q"], check=True)
    subprocess.run(["git", "-C", root, "add", "-A"], check=True)


def self_test():
    fixture = {"findings": [
        {"kind": "dead-artifact", "wave": "delete", "class": "root-file", "site": "x.txt", "rule": "r",
         "scenario": "no live caller names x.txt", "target": "delete it", "id": "R0-9001"},
        {"kind": "folder-namespace", "wave": "move-rename", "site": "src/A/B", "rule": "r", "scenario": "s",
         "measure": {"disagreements": ["N.A != N.A.B"]}, "target": "t", "id": "R0-9002"},
        {"kind": "god-class", "wave": "extract", "site": {"type": "N.T", "files": ["src/T.cs"]}, "rule": "r",
         "scenario": "s", "measure": {"lines": 900, "members": 40}, "target": "t", "id": "R0-9003"},
        {"kind": "god-class", "wave": "extract", "site": {"type": "N.U", "files": ["src/U.cs", "src/U.X.cs"]},
         "rule": "r", "scenario": "s", "measure": {"lines": 800, "members": 30}, "target": "t", "id": "R0-9004"},
        {"kind": "dead-artifact", "wave": "delete", "class": "src-file", "site": "src/Claimed.cs", "rule": "r",
         "scenario": "s", "target": "delete it", "id": "R0-9005"}]}
    with tempfile.TemporaryDirectory() as tmp:
        fp = os.path.join(tmp, "abcdef0123456.findings.json")
        with open(fp, "w", encoding="utf-8") as fh:
            json.dump(fixture, fh)
        nd = os.path.join(tmp, "Work")
        os.mkdir(nd)
        pool = ["PB9001", "PB9002", "PB9003", "PB9004"]
        w1, s1, tail, _ = file_notes(fp, pool, nd, False)
        assert len(w1) == 3 and tail == ["PB9004"] and "god-class" in s1, (w1, s1)
        w2, s2, _, _ = file_notes(fp, pool[3:], nd, False)
        assert not w2 and len(s2["already-filed"]) == 3, (w2, s2)
        assert sorted(os.listdir(nd)) == ["PB9001.md", "PB9002.md", "PB9003.md"]
        with open(os.path.join(nd, "PB9001.md"), encoding="utf-8", newline="") as fh:
            text = fh.read().replace("\r\n", "\n")
        expect_tail = frontmatter("PB9001 — Delete: 1 dead root-file artifact(s) in (repository root) "
                                  "(census R0-9001 at abcdef012)", "PB9001")
        assert text.startswith(expect_tail), text[:900]
        assert "census_ids: R0-9001" in text and "**Target:** delete it" in text

        # god-class: filed only with --targets, refused without a record, idempotent on census_ids
        gt = {"schema": 2, "targets": {
            "R0-9003": {"targets": ["T1", "T2"], "seam": "S", "order": "C+B", "order_text": "C then B",
                        "blocked_by": ["PB9100", "R69"], "note": ""},
            "R0-9004": {"targets": ["V1", "V2", "V3"], "seam": "S0", "order": "B", "order_text": "B",
                        "blocked_by": ["PB9100", "R69"], "note": "", "removes": [["N.C", "N.D", 4]],
                        "steps": [{"target": "V1", "seam": "S1", "files": ["src/U.cs"]},
                                  {"target": "V2", "files": ["src/U.X.cs"], "blocked_by": ["T-one"]},
                                  {"target": "V3", "blocked_by": ["PB9101"], "note": "last",
                                   "removes": [["N.E", "N.F", 1]]}]}},
            "designed": [{"key": "T-one", "wave": "move-rename", "what": "move W to the model", "sites": ["`src/W.cs:3`"],
                          "removes": [["N.A", "N.B", 2]], "targets": ["W in N.Model"], "seam": "W", "order": "B",
                          "order_text": "B", "blocked_by": ["R0-9003", "R69"], "model": "Sonnet",
                          "claims": [{"file": "src/Claimed2.cs", "kinds": ["dead-artifact"]}]},
                         {"key": "T-two", "wave": "grammar", "what": "regroup", "sites": ["`g.g4`"],
                          "targets": ["G"], "seam": "G", "order": "A", "order_text": "A",
                          "blocked_by": ["R0-9004#last"]}]}
        try:
            file_notes(fp, ["PB9003", "PB9004"], nd, True, {"targets": {}})
            raise AssertionError("a god-class finding without a targets record was filed")
        except SystemExit as e:
            assert "R0-9004" in str(e) or "R0-9003" in str(e), e
        fixture["findings"].append({"kind": "dead-artifact", "wave": "delete", "class": "src-file",
                                    "site": "src/Claimed2.cs", "rule": "r", "scenario": "s", "target": "t",
                                    "id": "R0-9006"})
        with open(fp, "w", encoding="utf-8") as fh:
            json.dump(fixture, fh)
        for rel in ("src/U.cs", "src/U.X.cs", "src/W.cs"):   # the targets file's paths exist, or nothing files
            os.makedirs(os.path.join(tmp, "src"), exist_ok=True)
            open(os.path.join(tmp, rel), "w").close()
        os.remove(os.path.join(tmp, "src/W.cs"))
        stage(tmp)   # the existence rule reads the committed tree (work.tracked_paths), never the filesystem
        try:
            file_notes(fp, ["PB9004"], nd, True, gt, path_root=tmp)
            raise AssertionError("a targets file naming a missing path filed")
        except SystemExit as e:
            assert "src/W.cs" in str(e), e
        open(os.path.join(tmp, "src/W.cs"), "w").close()
        stage(tmp)
        pool3 = ["PB9004", "PB9005", "PB9006", "PB9007", "PB9008", "PB9009", "PB9010"]
        w3, s3, tail3, r3 = file_notes(fp, pool3, nd, False, gt, path_root=tmp)
        assert [x[0] for x in w3] == pool3[:6], w3
        assert [x[1] for x in w3] == ["god-class", "god-class", "god-class-step", "god-class-step", "designed",
                                      "designed"], w3
        assert tail3 == ["PB9010"] and s3["claimed-by-design"] == ["R0-9006"] and not r3, (tail3, s3, r3)

        def read(n):
            with open(os.path.join(nd, n + ".md"), encoding="utf-8", newline="") as fh:
                return fh.read().replace("\r\n", "\n")
        g = read("PB9004")
        assert 'blocked: true\nblocked_by: ["PB9100", "R69"]' in g, g[:900]
        assert "**Seam:** S." in g and "T1; T2" in g and "census_ids: R0-9003" in g and "Extract:" in g
        assert "**Blocked by:** PB9100, R69 (" in g          # body and frontmatter come from one list
        u = read("PB9005")
        assert "step 1 of 3: V1" in u and "**Seam:** S1." in u and "3 PB9007" in u, u
        assert "V2" not in u and "V3" not in u, u      # a class note never claims a later step's target (E6)
        assert "- `src/U.X.cs`" in u and "The files this step edits" in u and "`N.C → N.D` 4" in u, u
        assert "**Its file set is computed, never listed**" in u and "every type its Sites declare" in u, u
        assert "**Removes" in read("PB9007") and "`N.E → N.F` 1" in read("PB9007")
        s2_ = read("PB9006")
        assert 'blocked_by: ["PB9005", "PB9008"]' in s2_ and "design_keys: R0-9004#2" in s2_, s2_
        assert "- `src/U.X.cs`" in s2_ and "- `src/U.cs`" not in s2_ and "**Seam:** S0." in s2_, s2_
        assert 'blocked_by: ["PB9006", "PB9101"]' in read("PB9007")
        t1 = read("PB9008")
        assert 'blocked_by: ["PB9004", "R69"]' in t1 and "`N.A → N.B` 2" in t1 and "Move/rename:" in t1, t1
        assert 'blocked_by: ["PB9007"]' in read("PB9009")   # R0-9004#last
        # a second run files nothing, and --rerender rewrites only a note whose record changed
        w4, s4, _, r4 = file_notes(fp, ["PB9011"], nd, False, gt, rerender=True, path_root=tmp)
        assert not w4 and not r4 and len(s4["already-filed"]) == 5, (w4, s4, r4)
        gt["designed"][0]["seam"] = "W2"
        gt["targets"]["R0-9003"]["blocked_by"] = ["PB9100"]
        _, _, _, r5 = file_notes(fp, ["PB9011"], nd, False, gt, rerender=True, path_root=tmp)
        assert r5 == ["PB9004", "PB9008"], r5
        assert "**Seam:** W2." in read("PB9008") and 'blocked_by: ["PB9100"]' in read("PB9004")
        # a reference that resolves to nothing is refused
        gt["designed"][1]["blocked_by"] = ["T-missing"]
        try:
            file_notes(fp, ["PB9011"], nd, True, gt, rerender=True, path_root=tmp)
            raise AssertionError("an unresolved reference rendered")
        except SystemExit as e:
            assert "T-missing" in str(e), e
    print("self-test OK")
    self_test_r2()


def main():
    sys.stdout.reconfigure(encoding="utf-8")   # titles carry §, → and em dashes; a cp1252 console cannot print them
    ap = argparse.ArgumentParser()
    ap.add_argument("findings", nargs="?")
    ap.add_argument("--ids")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--notes-dir", default=NOTES_DIR)
    ap.add_argument("--targets", help="the design record of DESIGN-architecture-review §8 (schema 2, see the module "
                                      "doc), docs/rearchitecture/evidence/arch-census/targets/<sha>.json")
    ap.add_argument("--rerender", action="store_true", help="with --targets: rewrite every open note the targets file "
                                                            "renders whose text differs from it")
    ap.add_argument("--r2", metavar="COLLECTED", help="file the UPHELD findings of an R2 batch (r2_collect.py's "
                                                      "collected.json) as PB1754 notes; ids from --ids, else alloc.py")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    if a.r2:
        pool = parse_ranges(a.ids) if a.ids else None   # no --ids: exactly as many as needed, from alloc.py
        written, skipped, tail = file_r2(a.r2, pool, a.notes_dir, a.dry_run)
        for nid, wave, title in written:
            print("%s%s  %s" % ("(dry) " if a.dry_run else "", nid, title))
        print("notes: %d" % len(written))
        for k, v in sorted(skipped.items()):
            print("not filed [%s]: %d (%s)" % (k, len(v), ", ".join(v[:6]) + (" ..." if len(v) > 6 else "")))
        print("unused ids: %s" % (", ".join(tail) if tail else "none"))
        return 0
    if not a.findings or not a.ids:
        ap.error("findings file and --ids are required")
    targets = None
    if a.targets:
        with open(a.targets, encoding="utf-8") as fh:
            targets = json.load(fh)
    written, skipped, tail, rewritten = file_notes(a.findings, parse_ranges(a.ids), a.notes_dir, a.dry_run, targets,
                                                   a.rerender)
    counts = {}
    for _, kind, _t in written:
        counts[kind] = counts.get(kind, 0) + 1
    for nid, kind, title in written:
        print("%s%s  %s" % ("(dry) " if a.dry_run else "", nid, title))
    print("notes: %d  by kind: %s" % (len(written), json.dumps(counts, sort_keys=True)))
    if a.rerender:
        print("re-rendered: %d%s" % (len(rewritten), (" (" + ", ".join(rewritten) + ")") if rewritten else ""))
    for k, v in sorted(skipped.items()):
        print("not filed [%s]: %d (%s)" % (k, len(v), NOT_FILED.get(k, "")))
    print("unused ids: %s" % (("%s..%s (%d)" % (tail[0], tail[-1], len(tail))) if tail else "none"))


if __name__ == "__main__":
    sys.exit(main())
