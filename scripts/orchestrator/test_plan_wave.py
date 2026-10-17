#!/usr/bin/env python3
"""Self-test for plan_wave.py on fabricated notes, clusters and reports: work awaiting landing is never re-planned,
finishers come first and name their predecessor, clusters rank by rows claimed, a second group on a file is a
same-file successor, the model routing rules, the budget fill (with landings and successors), and that the groups
render through the real dispatch-spec template and pass check_practices.py's same-file rule; that the fix lane's whole
plan equals the golden the pre-campaign planner wrote; and the campaign lane (--cluster, kb/Work PB2120): cluster
selection whatever the harm flags, the blocked_by order, site clustering per depth, the `after:` derivation, the
waits (blockers in two chains, the owner, a frontier model), and work.py check's topology rules.
Run: python scripts/orchestrator/test_plan_wave.py   (no build, no git; the campaign section needs the
tools/claude-skills submodule, and its absence is a FAIL)."""
import dataclasses
import os
import pathlib
import re
import subprocess
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import coord  # noqa: E402
import plan_wave as pw  # noqa: E402

sys.path.insert(0, str(coord.REPO / ".claude" / "skills" / "workstream"))
import check_practices  # noqa: E402
import make_dispatch_specs as mds  # noqa: E402

RULES = coord.rules()
TMP = pathlib.Path(tempfile.mkdtemp(prefix="plan-wave-test-"))
WORK = TMP / "Work"
WORK.mkdir()
REPORTS = TMP / "reports"
REPORTS.mkdir()
fails, checked = [], []


def check(name, got, want):
    checked.append(name)
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def note(nid, status="open", area="runtime", rows=0, body="", wrapped=False):
    rl = ", ".join(f'"SR-{nid}-{k}"' for k in range(rows))
    inv = f"inventory_rows: [{rl}]" if not wrapped else f"inventory_rows: [{rl},\n  \"SR-{nid}-x\"]"
    (WORK / f"{nid}.md").write_text(
        f'---\ntitle: "{nid} — a defect in {area}"\nid: {nid}\nkind: defect\nstatus: {status}\narea: {area}\n'
        f"wrong_answer: true\n{inv}\n---\n\n# {nid}\n\n{body}\n", encoding="utf-8", newline="\n")


def report(name, first_line, header):
    (REPORTS / name).write_text(f"{first_line}\n{header}\n\n## Reproduced?\nPB9999 is mentioned only below the header.\n",
                                encoding="utf-8", newline="\n")


# Notes. PB1/PB2 sit on an unlanded branch; PB3 was split by a landed predecessor; PB4 is half; PB5..PB12 open.
for n in ("PB1", "PB2", "PB3"):
    note(n, rows=2)
note("PB4", status="half", rows=1)
note("PB5", rows=1)
note("PB6", rows=9, wrapped=True)       # a wrapped list: the one frontmatter reader must still count it (9 + 1)
note("PB7", rows=3)
note("PB8", area="oo/classes", rows=1)
note("PB9", rows=1, body="The design question is open: which seam owns this.")
note("PB10", rows=1)
note("PB11", rows=1)
note("PB12", rows=4)
note("PB13", rows=0)
note("PB14", rows=0)
note("PB20", status="landed", rows=5)

report("w1014a-PB1-report.md", "DONE", "# PB1 group\n**branch:** `worktree-wf_aaa-1` · **HEAD:** `1111111aa`")
report("w1014b-PB3-report.md", "SPLIT",
       "# PB3 group (PB3 NOT started)\n**worktree:** `E:\\COBOL\\.claude\\worktrees\\wf_bbb-2` · **branch:** "
       "`worktree-wf_bbb-2` · **HEAD:** `2222222bb`")
report("w1012c-PB10-report.md", "DONE", "# PB10 first attempt\n**branch:** `worktree-wf_ccc-1`")
report("w1013c-PB10-report.md", "**Status:** DONE · PB10 partial", "# PB10 second attempt\n**branch:** `worktree-wf_ccc-2`")

CLASS = {"worktree-wf_aaa-1": "UNLANDED", "worktree-wf_bbb-2": "LANDED", "worktree-wf_ccc-1": "ABSENT",
         "worktree-wf_ccc-2": "MERGED"}
F_BIND, F_EMIT, F_G4, F_OO = ("src/X/Binder.cs", "src/X/Emitter.cs", "src/F/Grammar/Core.g4", "src/X/Oo/Table.cs")


def cl(file, *ids, harm=1):
    return {"file": file, "harm": harm, "files": [file], "notes": [{"id": i, "area": "", "harm": 1, "title": ""} for i in ids]}


CLUSTERS = [cl(F_BIND, "PB1", "PB2", "PB5"), cl(F_EMIT, "PB6", "PB7", "PB3"), cl(F_EMIT, "PB12"),
            cl(F_OO, "PB8"), cl(F_G4, "PB11"), cl("src/X/Misc.cs", "PB9"), cl("src/X/Other.cs", "PB10"),
            cl("src/X/Zero.cs", "PB13"), cl(F_BIND, "PB14")]
HALF = [cl("src/X/Half.cs", "PB4")]
UNLANDED = {"worktree-wf_aaa-1": ["PB1"], "worktree-wf_ddd-9": ["PB13"], "worktree-wf_eee-1": [],
            "worktree-wf_fff-3": ["PB4"]}  # a killed wave left WIP for the half note PB4

notes = pw.load_notes(WORK, {"wrong_answer": 8})
reports = pw.load_reports(REPORTS)
check("wrapped inventory_rows counted", len(notes["PB6"].rows), 10)
check("report header ids only", sorted(r.lead for r in reports if "PB9999" in r.ids), [])
r3 = next(r for r in reports if r.lead == "PB3")
check("report parse", (r3.status, r3.branch, r3.head, r3.not_started, r3.worktree),
      ("SPLIT", "worktree-wf_bbb-2", "2222222bb", True, r"E:\COBOL\.claude\worktrees\wf_bbb-2"))
check("status from **Status:** line", next(r for r in reports if r.slug == "w1013c").status, "DONE")

p = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100)
G = {tuple(g.notes): g for g in p["groups"]}
if "-v" in sys.argv:
    for g in p["groups"]:
        print(g.letter, g.after, g.kind, g.model, g.notes, g.file)
order = [g.notes for g in p["groups"]]

# 1. work on an unlanded branch with a report is not planned again; a branch naming no open note is surfaced
check("awaiting landing", p["awaiting_landing"], {"worktree-wf_aaa-1": ["PB1"]})
check("PB1 not planned", any("PB1" in g.notes for g in p["groups"]), False)
check("unlanded branch without note", p["unlanded_without_note"], ["worktree-wf_eee-1"])

# 2. finishers first: PB3 (split, landed predecessor) absorbs the notes of BOTH clusters on its file (cap 5), then
#    the half PB4, then the branch-only PB13
FIN = ("PB3", "PB6", "PB7", "PB12")
check("finisher order", order[:3], [list(FIN), ["PB4"], ["PB13"]])
check("finisher pred names report and branch",
      all(s in G[FIN].pred for s in ("w1014b-PB3-report.md", "worktree-wf_bbb-2", "LANDED")), True)
check("half pred", "status: half" in G[("PB4",)].pred, True)
check("half pred names its unlanded branch", "worktree-wf_fff-3" in G[("PB4",)].pred, True)
check("half note planned once", sum("PB4" in g.notes for g in p["groups"]), 1)
check("branch-only pred", "worktree-wf_ddd-9" in G[("PB13",)].pred, True)

# 3. clusters by rows claimed: PB2+PB5 (2+1) first, the 1-row ones next, PB14 (0 rows) last
check("cluster order", (order[3], order[-1]), (["PB2", "PB5"], ["PB14"]))
# 4. a second cluster on a file is that file's same-file successor
g14, g25 = G[("PB14",)], G[("PB2", "PB5")]
check("successor", (g14.after, g14.letter), (g25.letter, g25.letter + "2"))

# 5. routing
check("default sonnet", G[("PB2", "PB5")].model, "sonnet")
check("oo file -> opus", G[("PB8",)].model, "opus")
check(".g4 -> opus", G[("PB11",)].model, "opus")
check("open design question -> opus", G[("PB9",)].model, "opus")
check("failed twice -> opus", (G[("PB10",)].model, "2 times" in G[("PB10",)].why_model), ("opus", True))
check("never fable", {g.model for g in p["groups"]} <= set(RULES["routing"]["allowed"]), True)

# 6. budget fill: a tight budget takes the first finisher only; its successor's predecessor rule holds
one = pw.group_cost(G[("PB3", "PB6", "PB7", "PB12")].model, 4, RULES) + pw.landing_cost(RULES)
p2 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=one + 0.01)
check("tight budget: first group + its landing", [g.notes for g in p2["groups"]], [["PB3", "PB6", "PB7", "PB12"]])
check("points include the landing", round(p2["points"], 6), round(one, 6))
small = pw.group_cost("sonnet", 1, RULES) + pw.landing_cost(RULES)
p3 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=small + 0.01)
check("a group that does not fit is skipped, a smaller later one taken", [g.notes for g in p3["groups"]], [["PB4"]])
check("successor never without predecessor", any(g.after for g in p3["groups"]), False)
p4 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100,
             max_groups=2)
check("max groups", len(p4["groups"]), 2)
check("no train: the plan carries no train count (kb/Work PB3023)", "trains" in p, False)

# 6b. A SPLIT REPORT ON AN UNLANDED BRANCH IS A FINISHER, NOT "AWAITING LANDING" (kb/Work PB2575): a `land` unit lands
#     only DONE branches, so holding it stranded the work (waves 1020 D and 1032 A). Its pred cherry-picks the branch.
#     A DONE report that names a note NOT started still waits for its landing. Own reports, so check 8's golden holds.
SPLIT_REPORTS = TMP / "reports-split"
SPLIT_REPORTS.mkdir()
(SPLIT_REPORTS / "w1020d-PB5-report.md").write_text(
    "SPLIT\n# PB5 group\n**Status:** SPLIT at the turn cap · **worktree:** `E:\\COBOL\\.claude\\worktrees\\wf_ggg-4` · "
    "**branch:** `worktree-wf_ggg-4` · **HEAD:** `3333333cc`\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
(SPLIT_REPORTS / "w1020e-PB7-report.md").write_text(
    "DONE\n# PB7 group (PB12 NOT started)\n**branch:** `worktree-wf_hhh-5` · **HEAD:** `4444444dd`\n\n## Reproduced?\n",
    encoding="utf-8", newline="\n")
SPLIT_CLASS = {"worktree-wf_ggg-4": "UNLANDED", "worktree-wf_hhh-5": "UNLANDED"}
p5 = pw.plan(notes, CLUSTERS, HALF, pw.load_reports(SPLIT_REPORTS), lambda b: SPLIT_CLASS.get(b, "ABSENT"), {}, RULES,
             budget_points=100)
g5 = next((g for g in p5["groups"] if "PB5" in g.notes), None)
check("split on an unlanded branch is a finisher", (g5.kind if g5 else None, "PB5" in str(p5["awaiting_landing"])),
      ("finisher", False))
check("its pred cherry-picks the unlanded branch",
      bool(g5) and all(s in g5.pred for s in ("worktree-wf_ggg-4", "UNLANDED", "cherry-pick")), True)
check("done with a note not started still awaits landing", p5["awaiting_landing"].get("worktree-wf_hhh-5"),
      ["PB7", "PB12"])
# 6b'. A DONE BRANCH WHOSE REVIEW STAMPED FINDINGS IS A FINISHER (kb/Work PB3027): the land unit never fixes a review
#      finding, so the finisher's pred names the findings file and the answer-by-id rule. A CLEAN stamp still awaits
#      landing; a placeholder `Review:` line stamps nothing.
RV_REPORTS = TMP / "reports-review"
RV_REPORTS.mkdir()
(RV_REPORTS / "w1062e-PB5-report.md").write_text(
    "# PB5 report\n**Status:** DONE · **branch:** `worktree-wf_rv-1` · **HEAD:** `6666666ff`\n"
    "**Review:** FINDINGS 2 w1062e-review-r1 6666666ff E:/s/review-w1062-e.jsonl\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
(RV_REPORTS / "w1062f-PB7-report.md").write_text(
    "# PB7 report\n**Status:** DONE · **branch:** `worktree-wf_rv-2` · **HEAD:** `7777777aa`\n"
    "**Review:** CLEAN w1062f-review-r1 7777777aa\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
rv_reports = pw.load_reports(RV_REPORTS)
check("the review stamps parse", [(r.review.verdict, r.review.findings) if r.review else None for r in rv_reports],
      [("FINDINGS", 2), ("CLEAN", 0)])
p5r = pw.plan(notes, CLUSTERS, HALF, rv_reports, lambda b: "UNLANDED", {}, RULES, budget_points=100)
g5r = next((g for g in p5r["groups"] if "PB5" in g.notes), None)
check("a FINDINGS stamp is a finisher whose pred names the findings file and the answer rule",
      (g5r.kind if g5r else None, bool(g5r) and all(s in g5r.pred for s in ("review-w1062-e.jsonl", "no_change_needed",
                                                                              "git merge worktree-wf_rv-1"))),
      ("finisher", True))
check("a CLEAN stamp awaits landing", p5r["awaiting_landing"].get("worktree-wf_rv-2"), ["PB7"])
check("a placeholder Review line stamps nothing", pw.parse_review("**Review:** stamped by the branch reviewer"), None)
# 6b''. THE BRANCH IS THE HEADER'S FIELD, NEVER A WORD OF THE REVIEW STAMP (kb/Work PB3077): review_findings.py
#       stamps the review one line under the Status line, ABOVE the Branch field of an O11 header, and its reviewer
#       label `land-review-<branch>-r1` carries the branch name with a suffix. The header's first `worktree-…` word
#       was that label: a branch that does not exist, ABSENT, so every reviewed branch read as landed, and wave
#       1064's review finishers (2026-10-11) were first told to merge `worktree-wf_aba5c7ac-b61-1-r1`.
(RV_REPORTS / "w1062g-PB5-report.md").write_text(
    "Status: DONE\n**Review:** FINDINGS 2 land-review-worktree-wf_rv-3-r1 8888888bb E:/s/review-worktree-wf_rv-3.jsonl\n"
    "Worktree: E:\\COBOL\\.claude\\worktrees\\wf_rv-3\nBranch: worktree-wf_rv-3\nHEAD: 8888888bb\nBase: 1111111aa\n"
    "# PB5 report\n\n## What landed\n", encoding="utf-8", newline="\n")
(RV_REPORTS / "w1062h-PB7-report.md").write_text(
    "# PB7 report\n**Status:** DONE · **branch:** `worktree-wf_rv-4` · **HEAD:** `9999999cc`\n"
    "**Review:** FINDINGS 1 land-review-worktree-wf_rv-4-r1 9999999cc E:/s/review-worktree-wf_rv-4.jsonl\n\n## Reproduced?\n",
    encoding="utf-8", newline="\n")
(RV_REPORTS / "w1062i-PB12-report.md").write_text(
    "Status: DONE\n**Review:** FINDINGS 1 land-review-worktree-wf_rv-5-r1 aaaaaaa11 E:/s/r.jsonl\n"
    "# PB12 report — the work of branch worktree-wf_rv-5 (a header with no Branch field)\n\n## X\n", encoding="utf-8", newline="\n")
rv2 = {r.slug: r for r in pw.load_reports(RV_REPORTS)}
check("an O11 header's Branch field wins over the review stamp's reviewer label above it",
      (rv2["w1062g"].branch, rv2["w1062g"].worktree, rv2["w1062g"].head),
      ("worktree-wf_rv-3", "E:\\COBOL\\.claude\\worktrees\\wf_rv-3", "8888888bb"))
check("a template header's **branch:** field wins over the label below it", rv2["w1062h"].branch, "worktree-wf_rv-4")
check("with no field, the first branch word OUTSIDE the stamp is the branch", rv2["w1062i"].branch, "worktree-wf_rv-5")
check("the stamp still parses beside the field", (rv2["w1062g"].review.verdict, rv2["w1062g"].review.findings),
      ("FINDINGS", 2))
# The field is read at a FIELD POSITION only (PB3077 review round 1, R1-1): header prose says "this branch: every …"
# in real reports (w1043x-PB2806, w1055z-PB2426), and an unanchored `branch:` rule took `every` as the branch — ABSENT,
# the same harm — in a header with no field or with that prose above the field.
(RV_REPORTS / "w1062j-PB5-report.md").write_text(
    "Status: DONE\n**Review:** FINDINGS 1 land-review-worktree-wf_rv-6-r1 bbbbbbb22 E:/s/r.jsonl\n"
    "# PB5 report — the work of branch worktree-wf_rv-6; on this branch: every file is LF\n\n## X\n", encoding="utf-8", newline="\n")
(RV_REPORTS / "w1062k-PB7-report.md").write_text(
    "Status: DONE\n# PB7 report — on this branch: the reader takes the field\nBranch: worktree-wf_rv-7\nHEAD: ccccccc33\n"
    "\n## X\n", encoding="utf-8", newline="\n")
rv3 = {r.slug: r for r in pw.load_reports(RV_REPORTS)}
check("mid-prose `branch:` is not a field: with no field the branch word wins, never the prose", rv3["w1062j"].branch,
      "worktree-wf_rv-6")
check("mid-prose `branch:` above the Branch field does not pre-empt it", rv3["w1062k"].branch, "worktree-wf_rv-7")

# 6b'''. THE DRAIN AND THE UNSEEN LEAD (kb/Work PB3150). Wave 1075's drain plan (`--max-groups 2`) put the SPLIT of
#        w1074a first, whose finisher would have started PB3101 (never started) and cherry-picked `…-e99-1` again
#        though `…-e99-4` merged it, and never planned license-product-name's review finisher: its lead PB3146 is
#        process_only and existed only on its branch. --drain plans the finishers of STARTED work only; a FINDINGS
#        report whose lead the fix lane cannot see gets its finisher in every mode, decided by the same rule as an
#        open note (its newest report naming it, on any branch), and a DONE branch that merely contains it is named in
#        its pred, never a reason to drop it (review R1-1, R1-2).
DR = TMP / "reports-drain"
DR.mkdir()
for name, first, hdr in (
        ("w1074a-PB5-report.md", "SPLIT", "# PB5 group (PB12 not started)\n**branch:** `worktree-wf_dr-1` · **HEAD:** `1010101aa`"),
        ("w1074b-PB7-report.md", "Status: DONE", "**Review:** FINDINGS 1 w1074b-review-r1 2020202bb E:/s/review-w1074-b.jsonl\n"
                                                 "Branch: worktree-wf_dr-2\nHEAD: 2020202bb"),
        ("w1074c-PB6-report.md", "SPLIT", "# PB6 group\n**branch:** `worktree-wf_dr-3` · **HEAD:** `3030303cc`"),
        ("w1074d-PB10-report.md", "SPLIT", "# PB10 group\n**branch:** `worktree-wf_dr-4` · **HEAD:** `4040404dd`"),
        ("w1074z-PB77-report.md", "Status: DONE", "**Review:** FINDINGS 1 enqueue-review-owner-task-r1 5050505ee "
                                                  "E:/s/review-owner-task.jsonl\nBranch: owner-task\nHEAD: 5050505ee"),
        ("w1074y-PB79-report.md", "Status: DONE", "**Review:** FINDINGS 1 enqueue-review-r1 6060606ff E:/s/r79.jsonl\n"
                                                  "Branch: worktree-wf_dr-5\nHEAD: 6060606ff")):
    (DR / name).write_text(f"{first}\n{hdr}\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
DR_CLASS = {"worktree-wf_dr-1": "UNLANDED", "worktree-wf_dr-2": "UNLANDED", "worktree-wf_dr-3": "UNLANDED",
            "worktree-wf_dr-4": "LANDED", "owner-task": "UNLANDED", "worktree-wf_dr-5": "UNLANDED",
            "worktree-wf_dr-6": "UNLANDED", "worktree-wf_dr-7": "UNLANDED"}
DR_CARRIER = {"worktree-wf_dr-1": "worktree-wf_dr-2", "worktree-wf_dr-5": "worktree-wf_dr-2",
              "worktree-wf_dr-6": "worktree-wf_dr-2"}
dr_reports = pw.load_reports(DR)


class _Shown:
    def __init__(self, rc, out=""):
        self.returncode, self.stdout = rc, out


def _show(*args, cwd=None):
    if args[:2] == ("show", "owner-task:kb/Work/PB77.md"):
        return _Shown(0, '---\ntitle: "PB77 — an owner task"\nid: PB77\nkind: defect\nstatus: landed\narea: process\n'
                         "process_only: true\n---\n\n# PB77\n")
    return _Shown(128)


dr_notes = dict(notes)
dr_notes.update(pw.branch_lead_notes(dr_reports, dr_notes, {"wrong_answer": 8}, _show))
check("an unseen lead is read from its own branch, and only there", sorted(set(dr_notes) - set(notes)), ["PB77"])
dr_notes["PB79"] = pw.Note("PB79", "open", "process", "PB79 — carried", [], "", 0)
# unlanded_branch_notes is unfiltered (every id the subjects name); plan() keeps its leads (review R2-1)
DR_UNLANDED = {"owner-task": ["PB77"], "worktree-wf_dr-6": ["PB11"], "worktree-wf_dr-7": ["PB9"]}


def dr_plan(drain):
    return pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                   budget_points=100, drain=drain, carrier=DR_CARRIER.get)


pd = dr_plan(True)
check("drain: only the finishers of started work", sorted(sorted(g.notes) for g in pd["groups"]),
      [["PB6"], ["PB7"], ["PB77"], ["PB79"], ["PB9"]])
check("drain: every group is a finisher whose pred says DRAIN",
      all(g.kind == "finisher" and "DRAIN" in g.pred for g in pd["groups"]), True)
check("drain: a SPLIT branch another DONE branch merged waits, with its carrier named",
      {i: "worktree-wf_dr-2" in pd["waiting"].get(i, "") for i in ("PB5", "PB12", "PB11")},
      {"PB5": True, "PB12": True, "PB11": True})
check("drain: a SPLIT whose branch landed left only unstarted work", "never started" in pd["waiting"].get("PB10", ""), True)
check("drain: the clusters' notes wait, counted", ("PB2" in pd["drain_not_started"], "PB4" in pd["drain_not_started"]),
      (True, True))
check("an unseen lead's branch with a finisher is no judgment row", pd["unlanded_without_note"], [])
# kb/Work PB3311 hole 1: a FINDINGS report's branch with a live owner of its OWN (prune_worktrees.owned: a resumed
# branch whose tip moved past the reviewed head, a live agent's worktree, a dispatch out) is being finished: no second
# finisher, in either mode, for an open note (PB7, branch wf_dr-2) and an unseen lead (PB79, wf_dr-5) alike, with the
# reason named. The same plan with neither branch owned (nobody working on it: the stamp covers the tip) plans both.
for drain in (False, True):
    for owned, planned in (({"worktree-wf_dr-2", "worktree-wf_dr-5"}, False), (set(), True)):
        pf1 = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                      budget_points=100, drain=drain, carrier=DR_CARRIER.get, owned=owned)
        check(f"{'drain' if drain else 'normal'}: a FINDINGS branch {'not ' if planned else ''}owned "
              f"{'plans' if planned else 'waits, no'} finisher (PB3311)",
              [(any(i in g.notes for g in pf1["groups"]), "IN FLIGHT" in pf1["waiting"].get(i, ""))
               for i in ("PB7", "PB79")], [(planned, not planned)] * 2)
        if not planned:
            check(f"{'drain' if drain else 'normal'}: the waiting reason names the branch and the answers in progress",
                  all(w in pf1["waiting"].get("PB7", "") for w in ("worktree-wf_dr-2", "being answered")), True)
    # a SPLIT report's branch IN FLIGHT is a different arm (kb/Work PB2974): untouched by this rule
    pf2 = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                  budget_points=100, drain=False, carrier=DR_CARRIER.get, owned={"worktree-wf_dr-3"})
    check("a SPLIT report's branch IN FLIGHT is the SPLIT arm's (PB2974), not this rule's",
          any("PB6" in g.notes for g in pf2["groups"]), True)
# PB3311 review R1-1: the survey reads a FINDINGS branch IN FLIGHT ALSO when a carrier merely contains it
# (prune_worktrees.contained: it lands through that branch), and that says nothing about who answers ITS findings. The
# survey as plan_wave.main reads it, end to end: wf_dr-5 (PB79's FINDINGS branch, DECISION by lifecycle) is an ancestor
# of wf_dr-2 (PB7's branch, the carrier). With the carrier locked by a live agent (IN FLIGHT, owned) PB7 waits and the
# carried PB79 still gets its R1-2 finisher, its pred naming the carrier; with the carrier finished (WAITING TO LAND)
# both are planned. Before the fix the bare state made PB79 wait on an answer nobody was writing.
import prune_worktrees  # noqa: E402
_dr_tips = {"worktree-wf_dr-2": "2020202bb", "worktree-wf_dr-5": "6060606ff"}
_dr_rep = {r.lead: r for r in prune_worktrees.report_index(DR, lambda _i: False)}
_git_saved = prune_worktrees.git
prune_worktrees.git = lambda *a, **_k: _Shown(0 if a == ("merge-base", "--is-ancestor", "6060606ff", "2020202bb") else 1)
try:
    for carrier_state, pb7_planned in (("IN FLIGHT", False), ("WAITING TO LAND", True)):
        rows = [prune_worktrees.Row(b, "UNLANDED", [], [], 1, None, None, 0, 999.0, "", report=_dr_rep[lead], state=st,
                                    tip=_dr_tips[b])
                for b, lead, st in (("worktree-wf_dr-2", "PB7", carrier_state), ("worktree-wf_dr-5", "PB79", "DECISION"))]
        prune_worktrees.contained(rows)
        survey_owned = prune_worktrees.owned(rows)
        pr = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                     budget_points=100, carrier=DR_CARRIER.get, owned=survey_owned)
        g79 = next((g for g in pr["groups"] if g.notes == ["PB79"]), None)
        check(f"R1-1: a FINDINGS branch contained in a {carrier_state} carrier reads IN FLIGHT, is not owned, and gets "
              f"its finisher naming the carrier; the carrier's own note {'is planned' if pb7_planned else 'waits'}",
              (rows[1].state, rows[1].carrier, "worktree-wf_dr-5" in survey_owned, "PB79" in pr["waiting"],
               bool(g79) and "contained in worktree-wf_dr-2" in g79.pred,
               any("PB7" in g.notes for g in pr["groups"]), "being answered" in pr["waiting"].get("PB7", "")),
              ("IN FLIGHT", "worktree-wf_dr-2", False, False, True, pb7_planned, not pb7_planned))
finally:
    prune_worktrees.git = _git_saved
pn = dr_plan(False)
check("every mode: an unseen lead's FINDINGS gets its finisher", any(g.notes == ["PB77"] for g in pn["groups"]), True)
# R1-2: PB79's branch is contained in worktree-wf_dr-2, PB7's DONE branch, whose report never answers PB79's findings:
# the carried FINDINGS branch still gets its finisher (the drop was silent: no group, no waiting entry, no judgment row),
# which merges PB79's own branch only and is told the carrier may land its commits first.
for mode, pm in (("every mode", pn), ("drain", pd)):
    g79 = next((g for g in pm["groups"] if g.notes == ["PB79"]), None)
    check(f"{mode}: a carried unseen-lead FINDINGS gets its finisher, its pred naming the carrier (R1-2)",
          bool(g79) and ("git merge worktree-wf_dr-5" in g79.pred, "contained in worktree-wf_dr-2" in g79.pred,
                         "never worktree-wf_dr-2" in g79.pred, "review_findings.py show E:/s/r79.jsonl" in g79.pred),
          (True, True, True, True))
g5n = next((g for g in pn["groups"] if "PB5" in g.notes), None)
check("every mode: a carried SPLIT's finisher merges its carrier, never cherry-picks it",
      bool(g5n) and "git merge worktree-wf_dr-2" in g5n.pred and "cherry-pick its unlanded" not in g5n.pred, True)
check("without --drain nothing waits on the drain", (pn["drain_not_started"], any("DRAIN" in g.pred for g in pn["groups"])),
      ([], False))
# R1-1: the review finisher of owner-task's FINDINGS works on its OWN branch, and its later report about the same
# unseen lead supersedes the FINDINGS report, as for an open note: a SPLIT is resumed from its own branch (never the
# findings answered again from scratch, its branch never a judgment row); an unreviewed DONE awaits landing; a newer
# FINDINGS stamp on the finisher's branch gets the next finisher from that report.
SU = TMP / "reports-successor"
SU.mkdir()
(SU / "w1074z-PB77-report.md").write_text(
    "Status: DONE\n**Review:** FINDINGS 1 enqueue-review-owner-task-r1 5050505ee E:/s/review-owner-task.jsonl\n"
    "Branch: owner-task\nHEAD: 5050505ee\n\n## X\n", encoding="utf-8", newline="\n")
SU_CLASS = {"owner-task": "UNLANDED", "worktree-wf_f-1": "UNLANDED"}
for successor, carried, want in (
        ("Status: SPLIT\nBranch: worktree-wf_f-1\nHEAD: 7070707aa", {}, "SPLIT"),
        ("Status: DONE\nBranch: worktree-wf_f-1\nHEAD: 7070707aa", {"owner-task": "worktree-wf_f-1"}, "DONE"),
        ("Status: DONE\n**Review:** FINDINGS 2 enqueue-review-wf_f-1-r1 7070707aa E:/s/review-wf_f-1.jsonl\n"
         "Branch: worktree-wf_f-1\nHEAD: 7070707aa", {"owner-task": "worktree-wf_f-1"}, "FINDINGS")):
    (SU / "w1075a-PB77-report.md").write_text(successor + "\n\n## X\n", encoding="utf-8", newline="\n")
    for drain in (False, True):
        ps = pw.plan(dr_notes, CLUSTERS, HALF, pw.load_reports(SU), lambda b: SU_CLASS.get(b, "ABSENT"),
                     {"owner-task": ["PB77"], "worktree-wf_f-1": ["PB77"]}, RULES, budget_points=100, drain=drain,
                     carrier=carried.get)
        g77 = [g for g in ps["groups"] if "PB77" in g.notes]
        mode = f"{want} successor{' (drain)' if drain else ''}"
        if want == "SPLIT":
            check(f"{mode}: the finisher resumes the successor's branch, never the stale FINDINGS report (R1-1)",
                  [("w1075a" in g.pred, "worktree-wf_f-1" in g.pred, "review-owner-task.jsonl" in g.pred) for g in g77],
                  [(True, True, False)])
            # R2-1: the superseded FINDINGS branch names the unseen lead, which a newer report covers: skipped, as an
            # open note's stale branch is, never a judgment row (unlanded_branch_notes once kept only open notes)
            check(f"{mode}: neither the successor's nor the superseded FINDINGS branch is a judgment row (R2-1)",
                  [b for b in ("worktree-wf_f-1", "owner-task") if b in ps["unlanded_without_note"]], [])
        elif want == "DONE":
            check(f"{mode}: an unreviewed successor awaits landing, no finisher (R1-1)",
                  (g77, ps["awaiting_landing"].get("worktree-wf_f-1")), ([], ["PB77"]))
        else:
            check(f"{mode}: the successor's own FINDINGS gets the next finisher (R1-1)",
                  [("review-wf_f-1.jsonl" in g.pred, "git merge worktree-wf_f-1" in g.pred) for g in g77], [(True, True)])

# 6c. A NOTE AN UNREPORTED DISPATCH HOLDS IS NOT PLANNED AGAIN, IN EITHER LANE (kb/Work PB2806): the fix lane read no
#     ledger, and wave 1044 re-planned seven of the eight groups another session's recorded, unlaunched wave 1045 held.
#     A report written since the dispatch answers it (steps 1 and 2 govern), a terminal note is never held, and a
#     retired hand entry (kb/Work PB3186) is no dispatch, however fresh.
HD = TMP / "held"
HD_REPORTS = HD / "reports"
HD_REPORTS.mkdir(parents=True)
pw.record_dispatch(HD, "1045", [{"letter": "a", "notes": ["PB5", "PB2"], "files": ["src/X.cs"]},
                                {"letter": "b", "notes": ["PB8"], "files": ["src/Y.cs"]},
                                {"letter": "c", "notes": ["PB9"], "files": ["src/Z.cs"]}], lambda i: False)
HD_AT = {g["letter"]: g["at"] for g in coord.read_json(HD / pw.DISPATCH_LEDGER, {})["groups"]}
for name, nid, when in (("w1045b-PB8-report.md", "PB8", HD_AT["b"] + 60), ("w1031c-PB9-report.md", "PB9", HD_AT["c"] - 3600)):
    (HD_REPORTS / name).write_text(f"DONE\n# {nid} group\n**branch:** `worktree-wf_held-{nid}` · **HEAD:** `5555555ee`\n\n"
                                   "## Reproduced?\n", encoding="utf-8", newline="\n")
    __import__("os").utime(HD_REPORTS / name, (when, when))
LEDGER_DOC = coord.read_json(HD / pw.DISPATCH_LEDGER, {})
LEDGER_DOC["groups"].append({"wave": "hand", "letter": "", "notes": ["PB10"], "files": ["src/W.cs"], "hand": True,
                             "at": HD_AT["a"]})
coord.write_json(HD / pw.DISPATCH_LEDGER, LEDGER_DOC)
check("held: unanswered groups only, terminal notes and retired hand entries never",
      pw.held_by_dispatch(HD, pw.load_reports(HD_REPORTS), lambda i: i == "PB2"),
      {"PB5": "dispatch w1045a", "PB9": "dispatch w1045c"})
p6 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100,
             held={"PB5": "dispatch w1045a"})
check("a held note is planned in no group and waits naming its dispatch",
      (any("PB5" in g.notes for g in p6["groups"]), "w1045a" in p6["waiting"].get("PB5", "")), (False, True))
check("its cluster's other notes are still planned", any("PB2" in g.notes for g in p6["groups"]), True)
# R2-1 (kb/Work PB3150): an UNSEEN lead (PB77, process_only, its FINDINGS on owner-task) is held exactly as an open note
# is. Wave 1075 dispatched its review finisher, which has not reported: the next plan, normal or --drain, must not
# plan a second finisher on the same findings (the hold once read `open_ids`, so PB77 was planned again).
for drain in (False, True):
    ph = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                 budget_points=100, drain=drain, carrier=DR_CARRIER.get, held={"PB77": "dispatch w1075a", "PB7": "dispatch w1075b"})
    mode = "drain" if drain else "normal"
    check(f"{mode}: a held unseen lead and a held open note both wait naming their dispatch, neither planned (R2-1)",
          [(any(i in g.notes for g in ph["groups"]), f"held by dispatch {w}" in ph["waiting"].get(i, ""))
           for i, w in (("PB77", "w1075a"), ("PB7", "w1075b"))], [(False, True), (False, True)])
    check(f"{mode}: the held unseen lead's branch is no judgment row (R2-1)", "owner-task" in ph["unlanded_without_note"],
          False)
# The frontier-model hold reads the leads too: an unseen lead whose body needs Mythos under the owner's approval waits.
fm_notes = dict(dr_notes)
fm_notes["PB77"] = dataclasses.replace(dr_notes["PB77"], body="This needs Mythos, under the owner's per-dispatch approval.")
pf = pw.plan(fm_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
             budget_points=100, carrier=DR_CARRIER.get)
check("an unseen lead that needs the owner's frontier-model approval waits (R2-1)",
      (any("PB77" in g.notes for g in pf["groups"]), "owner's approval" in pf["waiting"].get("PB77", "")), (False, True))
# unlanded_branch_notes is unfiltered: an id that is no lead (a landed note, a typo) leaves its branch a judgment row
pj = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"),
             {**DR_UNLANDED, "worktree-wf_dr-8": ["PB4242"]}, RULES, budget_points=100, carrier=DR_CARRIER.get)
check("a branch naming no lead is a judgment row, its non-lead ids never planned",
      ("worktree-wf_dr-8" in pj["unlanded_without_note"], any("PB4242" in g.notes for g in pj["groups"])), (True, False))
# ...and unlanded_branch_notes itself keeps every id the subjects name: it once kept only the open notes, so a
# superseded FINDINGS branch naming only its unseen lead (owner-task: PB77) reached plan() empty, a judgment row.
_saved = (prune_worktrees.main_names, prune_worktrees.classify, prune_worktrees.git)
_subjects = {"owner-task": "PB77: an owner task\nPB77: answer review R1-1", "worktree-wf_x": "PB2, PB11: fix"}
prune_worktrees.main_names = lambda: set()
prune_worktrees.classify = lambda b, _names: ("UNLANDED",)
prune_worktrees.git = lambda *a, **_k: _Shown(0, "main\n" + "\n".join(_subjects) if a[0] == "branch"
                                              else _subjects[a[-1].split("..")[1]])
try:
    check("unlanded_branch_notes names every id, an unseen lead's too (R2-1)", pw.unlanded_branch_notes(),
          {"owner-task": ["PB77"], "worktree-wf_x": ["PB2", "PB11"]})
finally:
    prune_worktrees.main_names, prune_worktrees.classify, prune_worktrees.git = _saved

# 7. the group JSON renders through the real template and passes check_practices' same-file rule
gjson = [pw.as_group_json(g, "77", notes, "COBOLNET0001-COBOLNET0003") for g in p["groups"]]
CFG77 = {"wave": "77", "base": "abc", "scratch": str(TMP), "stop_file": str(coord.fleet_stop(TMP, "w77"))}
try:
    specs = [mds.render(CFG77, g) for g in gjson]
    rendered = True
except (KeyError, IndexError) as e:
    specs, rendered = [], f"template placeholder missing: {e}"
check("renders through the template", rendered, True)
# 7b. THE STOP IS SCOPED (kb/Work PB2483): every spec names the owner's global stop and this fleet's own, and passes
#     check_practices' rendered-spec rule; a groups file without a fleet stop renders nothing
spec_file = TMP / "msg-w77-x.txt"
spec_file.write_text(specs[0] if specs else "", encoding="utf-8", newline="\n")
check("a rendered spec names the global stop", str(coord.global_stop()) in (specs or [""])[0], True)
check("a rendered spec names the fleet's own stop", str(coord.fleet_stop(TMP, "w77")) in (specs or [""])[0], True)
check("a rendered spec passes check_practices", [m for m in check_practices.check(spec_file, check_practices.SPEC)
                                                 if "report" not in m], [])
# 7c. THE ONE REPORTS DIRECTORY (kb/Work PB2980): the report path is coord.reports_dir() whatever the wave's scratch
#     (here TMP), and check_practices refuses a spec that writes its report under the scratch instead
report_line = next((l for l in (specs or [""])[0].splitlines() if l.startswith("Report:")), "")
check("a rendered spec's report is in coord.reports_dir(), not the wave's scratch",
      (str(coord.reports_dir()) in report_line, str(TMP) in report_line), (True, False))
check("check_practices accepts the rendered report path",
      bool(re.search(check_practices.RENDERED_REPORT, report_line)), True)
check("check_practices refuses a report under the wave's scratch",
      bool(re.search(check_practices.RENDERED_REPORT, f"Report: {TMP}\\reports\\w77a-PB1-report.md")), False)
try:
    mds.render({k: v for k, v in CFG77.items() if k != "stop_file"}, gjson[0])
    check("no stop_file, no spec", "rendered", "KeyError")
except KeyError:
    check("no stop_file, no spec", True, True)
check("a fleet stop is STOP-<scope>", coord.fleet_stop(pathlib.Path("S"), "loop").name, "STOP-loop")
try:
    coord.fleet_stop(TMP, "../x")
    check("a scope with a path in it is refused", "accepted", "ValueError")
except ValueError:
    check("a scope with a path in it is refused", True, True)
check("lead is the note with most rows", next(j["lead"] for j in gjson if j["letter"] == G[("PB3", "PB6", "PB7", "PB12")].letter), "PB6")
gfile = TMP / "groups.json"
gfile.write_text(__import__("json").dumps({"wave": "77", "scratch": "X", "base": "abc", "stop_file": CFG77["stop_file"],
                                          "groups": gjson}), encoding="utf-8", newline="\n")
check("check_practices O2 same-file rule", check_practices.same_file_without_successor(gfile), [])

# 8. THE FIX LANE WITHOUT --cluster IS UNCHANGED (kb/Work PB2120): the whole plan of this fixture, every group's
#    rendered JSON included, equals the golden that the planner wrote BEFORE the campaign lane existed (committed
#    alone in the PB2120 checkpoint before plan_wave.py changed). `--write-golden` rewrites it; only a deliberate
#    change to the fix lane does that, and says so in its commit.
GOLDEN = HERE / "testdata" / "plan_wave_default.json"


def fingerprint(pl, letter_json):
    return {"groups": [{"letter": g.letter, "after": g.after, "kind": g.kind, "model": g.model, "why": g.why_model,
                        "notes": g.notes, "file": g.file, "files": g.files, "pred": g.pred, "cost": round(g.cost, 9),
                        "json": letter_json.get(g.letter)} for g in pl["groups"]],
            "skipped": [[g.letter, g.after, g.notes] for g in pl["skipped"]],
            "awaiting_landing": pl["awaiting_landing"], "points": round(pl["points"], 9),
            "unlanded_without_note": pl["unlanded_without_note"]}


JSON = __import__("json")
fp = {"full": fingerprint(p, {j["letter"]: j for j in gjson}), "tight": fingerprint(p2, {}),
      "small": fingerprint(p3, {}), "max2": fingerprint(p4, {})}
# the fixture's temporary directory is the one varying input: it is spelled <TMP> in the golden, and every path
# separator as `/`, so the same golden holds on Windows and Linux
fp_text = (JSON.dumps(fp, indent=1, ensure_ascii=False).replace(JSON.dumps(str(TMP))[1:-1], "<TMP>")
           .replace("\\\\", "/") + "\n")
if "--write-golden" in sys.argv:
    GOLDEN.write_text(fp_text, encoding="utf-8", newline="\n")
    print(f"wrote {GOLDEN}")
check("default plan equals the pre-campaign golden", fp_text == GOLDEN.read_text(encoding="utf-8"), True)

# 9. A CAMPAIGN (--cluster, kb/Work PB2120): the open notes naming the cluster, harm flags or not, in blocked_by order;
#    clustered by the sites they name anywhere in the tree; a dependent runs after its blocker's group (`after:`); a
#    dependent whose blockers sit in two chains, a note held by the owner, and a frontier-model note wait.
sys.path.insert(0, str(coord.REPO / "scripts" / "spec"))
import work  # noqa: E402

CW = TMP / "campaign" / "kb" / "Work"
CW.mkdir(parents=True)
for rel in ("src/Lib/Alpha.cs", "src/Lib/Beta.cs", "scripts/gamma.py", "tests/Delta.Tests/DeltaTests.cs"):
    (TMP / "campaign" / rel).parent.mkdir(parents=True, exist_ok=True)
    (TMP / "campaign" / rel).write_text("// fixture\n", encoding="utf-8", newline="\n")


def cnote(nid, cluster=("PB901",), blocked_by=(), body="", status="open", kind="defect"):
    bb = ", ".join(blocked_by)
    (CW / f"{nid}.md").write_text(
        f'---\ntitle: "{nid} — campaign step"\nid: {nid}\nkind: {kind}\nstatus: {status}\narea: build/ci\n'
        f"wrong_answer: false\nprocess_only: true\nblocked: {'true' if blocked_by and status != 'landed' else 'false'}\n"
        f"blocked_by: [{bb}]\ncluster: [{', '.join(cluster)}]\n---\n\n# {nid}\n\n{body}\n", encoding="utf-8", newline="\n")


cnote("PB901", body="**Sites:**\n- `src/Lib/Alpha.cs`\n\nSever it from the old engine.")
cnote("PB902", body="**Code site.** scripts/gamma.py (its legacy legs)\n\nRewrite them.")
cnote("PB903", blocked_by=("PB901",), body="### Code site\n\n`src/Lib/Beta.cs:12-40`\n\n### Fix\n\nThen delete it.")
cnote("PB904", blocked_by=("PB901", "PB902"), body="- **Code site:** tests/Delta.Tests/DeltaTests.cs#Rename\n\nThen rename.")
# PB905 names no site in a site block: what its prose discusses, and its Gate: line, declare nothing (kb/Work PB3242)
cnote("PB905", blocked_by=("PB903",), body="Then sweep the docs; it discusses `src/Lib/Alpha.cs` and scripts/gamma.py.\n\n"
                                           "**Gate:** `python scripts/gamma.py --check`")
cnote("PB906", body="Author = Mythos 5.1, only after the owner's explicit\napproval for this dispatch.", kind="analysis")
cnote("PB907", cluster=("OTHER",), body="**Sites:**\n- `src/Lib/Alpha.cs`")
cnote("PB908", status="landed", body="**Sites:**\n- `src/Lib/Alpha.cs`")
cnote("PB909", status="owner", body="Waits for the owner.")
cnote("PB910", cluster=("PB901", "OTHER"), body="**Sites:**\n- `scripts/gamma.py#main` (main)")
citems = [dict(work.parse_frontmatter(p.read_text(encoding="utf-8")), _file=p.name) for p in sorted(CW.glob("*.md"))]
view = work.cluster_order(citems, "PB901")
check("cluster members: open, any kind, harm flags or not, in blocked_by order",
      [(r["id"], r["depth"], r["ready"]) for r in view["notes"]],
      [("PB901", 0, True), ("PB902", 0, True), ("PB906", 0, True), ("PB909", 0, False), ("PB910", 0, True), ("PB903", 1, False),
       ("PB904", 1, False), ("PB905", 2, False)])
check("a waiting note names what it waits on", {r["id"]: r["waiting_on"] for r in view["notes"] if not r["ready"]},
      {"PB909": ["status: owner"], "PB903": ["PB901"], "PB904": ["PB901", "PB902"], "PB905": ["PB903"]})
check("named counts the landed member too", view["named"], 9)
check("a landed cluster has no open note", work.cluster_order(citems, "PB908")["notes"], [])

try:
    fc = pw.load_fix_clusters()
except SystemExit as e:  # the campaign clusters by fix_clusters.py's real index: a missing submodule is a FAIL, not a skip
    fc = None
    check("tools/claude-skills submodule present (git submodule update --init tools/claude-skills)", str(e), "")
if fc:
    cnotes = pw.load_notes(CW, {"wrong_answer": 8})
    ctexts = {i: (CW / f"{i}.md").read_text(encoding="utf-8") for i in cnotes}
    # THE declared set (declared_sets) on a policy that refuses to build an index: no note here names a member, so the
    # index is never read (a fix-lane or sites-only note declares its sites without one)
    CDECL = pw.declared_sets(TMP / "campaign", pw.IndexPolicy(TMP / "no-cache", regenerate=False))
    copen, chalf, cdeps, cwait = pw.campaign_clusters(view, cnotes, ctexts, RULES, fc, TMP / "campaign", CDECL)
    # PB904 waits on PB901 (Alpha.cs) and PB902 (gamma.py): their two clusters merge, so PB904 can follow ONE chain
    check("campaign clusters by declared sites, one depth at a time; a dependent's blocker clusters merge",
          [(c["depth"], c["file"], [n["id"] for n in c["notes"]]) for c in copen],
          [(0, "scripts/gamma.py", ["PB902", "PB910", "PB901"]), (0, "", ["PB906"]),
           (1, "src/Lib/Beta.cs", ["PB903"]), (1, "tests/Delta.Tests/DeltaTests.cs", ["PB904"]), (2, "", ["PB905"])])
    check("the merged cluster orients on both files", copen[0]["files"], ["scripts/gamma.py", "src/Lib/Alpha.cs"])
    check("deps are the open in-cluster blockers", cdeps, {"PB903": ["PB901"], "PB904": ["PB901", "PB902"], "PB905": ["PB903"]})
    check("held by the owner: waiting before planning", cwait, {"PB909": "waiting on status: owner"})
    cp = pw.plan(cnotes, copen, chalf, [], lambda b: "ABSENT", {}, RULES, budget_points=100, deps=cdeps)
    CG = {tuple(g.notes): g for g in cp["groups"]}
    check("campaign groups and their successor chain",
          [(g.letter, g.after, g.notes, g.blocked_by) for g in cp["groups"]],
          [("A", "", ["PB902", "PB910", "PB901"], []), ("A2", "A", ["PB903"], ["PB901"]),
           ("A3", "A2", ["PB904"], ["PB901", "PB902"]), ("A4", "A3", ["PB905"], ["PB903"])])
    check("a frontier-model note waits for the owner", sorted(cp["waiting"]), ["PB906"])
    check("the frontier-model reason", "approval" in cp["waiting"].get("PB906", ""), True)
    check("the dependency is in the rendered body",
          ("PB903",) in CG and "DEPENDENCY SUCCESSOR" in pw.body_of(CG[("PB903",)], cnotes), True)
    check("a fix-lane body never says it", any("DEPENDENCY SUCCESSOR" in pw.body_of(g, notes) for g in p["groups"]), False)
    cg = [pw.as_group_json(g, "78", cnotes, "COBOLNET0001-COBOLNET0003") for g in cp["groups"]]
    cfile = TMP / "campaign-groups.json"
    cfile.write_text(JSON.dumps({"wave": "78", "scratch": "X", "base": "abc", "groups": cg}), encoding="utf-8", newline="\n")
    check("campaign groups pass check_practices' successor rule", check_practices.same_file_without_successor(cfile), [])
    for g in cg:
        mds.render(dict(CFG77, wave="78"), g)
    only_b = pw.group_cost("opus", 1, RULES) + pw.landing_cost(RULES)
    cp2 = pw.plan(cnotes, copen, chalf, [], lambda b: "ABSENT", {}, RULES, budget_points=only_b * 1.5, deps=cdeps)
    check("a dependent is never chosen without its blocker's group",
          all(g.after in {c.letter for c in cp2["groups"]} for g in cp2["groups"] if g.after), True)

# 9b. plan() alone: a dependent whose blockers sit in two chains (they did not merge: over the cap) waits, and so does
#     one whose blocker is planned in no group; a dependent on one blocker becomes its chain's successor
dep = cl("src/X/Dep.cs", "PB11")
dep["depth"] = 1
lone = cl("src/X/Lone.cs", "PB10")
lone["depth"] = 1
after5 = cl("src/X/After.cs", "PB13")
after5["depth"] = 1
dp = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9"), dep, lone, after5], [], [], lambda b: "ABSENT", {},
             RULES, budget_points=100, deps={"PB11": ["PB5", "PB9"], "PB10": ["PB99"], "PB13": ["PB5"]})
check("two chains wait", "different successor chains" in dp["waiting"].get("PB11", ""), True)
check("an unplanned blocker waits", "PB99 is not planned" in dp["waiting"].get("PB10", ""), True)
check("one blocker: its chain's successor", [(g.letter, g.after, g.notes) for g in dp["groups"]],
      [("A", "", ["PB5"]), ("B", "", ["PB9"]), ("A2", "A", ["PB13"])])
# a finisher never absorbs a dependent cluster on its file: the dependent's blocker is planned after the finisher,
# so absorbing it would hold the finisher itself back
dep12 = cl(F_EMIT, "PB12")
dep12["depth"] = 1
fp_ = pw.plan(notes, [cl(F_BIND, "PB5"), cl(F_EMIT, "PB3"), dep12], [], [r3], lambda b: "LANDED", {}, RULES,
              budget_points=100, deps={"PB12": ["PB5"]})
check("a finisher keeps its own notes and runs", [(g.kind, g.notes) for g in fp_["groups"]][:1], [("finisher", ["PB3"])])

# 9c. the file-set partition (owner, kb/Work PB2118 question 5, 2026-10-07): a campaign group whose files meet an
#     in-flight train's file set waits, with the collision named, and so does every group that follows it; a group
#     that meets nothing runs. Planted collision first: without `busy` the same groups all run.
free = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9"), after5], [], [], lambda b: "ABSENT", {}, RULES,
               budget_points=100, deps={"PB13": ["PB5"]})
check("with nothing in flight all three run", [g.notes for g in free["groups"]], [["PB5"], ["PB9"], ["PB13"]])
bp = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9"), after5], [], [], lambda b: "ABSENT", {}, RULES,
             budget_points=100, deps={"PB13": ["PB5"]},
             busy={"worktree-train-a": pw.InFlight({F_BIND, "src/Other.cs"}, "worktree-train-a")})
check("a group colliding with RUNNING work waits, the collision named",
      "file-set collision with running work worktree-train-a [RUNNING] (" + F_BIND + ")" in bp["waiting"].get("PB5", ""),
      True)
check("its successor waits for it", "PB5 is not planned" in bp["waiting"].get("PB13", ""), True)
check("a group that meets nothing runs", [g.notes for g in bp["groups"]], [["PB9"]])
# PB2807: ONE rule for both lanes. The fix lane passes `busy` too (no deps): running work defers a group, a FINISHED
# branch (a DONE report, no review findings) makes it that branch's CONTINUATION (merge it first, no `after:`), and the
# group's own work never collides (the branch its pred continues; a dispatch of its own notes).
FIN = pw.Report(TMP / "w1090c-PB66-report.md", 1090, "w1090c", "PB66", "DONE", ["PB66"], False, "worktree-done",
                "E:/w/done", "abc1234")
fl = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9"), cl("src/X/Other.cs", "PB11")], [], [],
             lambda b: "ABSENT", {}, RULES, budget_points=100,
             busy={"worktree done (worktree-done)": pw.InFlight({F_BIND}, "worktree-done", finished=FIN),
                   "dispatched w1091a (PB9)": pw.InFlight({"src/X/Misc.cs"}, notes={"PB9"}),
                   "dispatched w1091b (PB40)": pw.InFlight({"src/X/Other.cs"}, notes={"PB40"})})
FG = {tuple(g.notes): g for g in fl["groups"]}
check("fix lane: a group meeting only a FINISHED branch is planned as its continuation, no after:",
      (("PB5",) in FG, FG.get(("PB5",)) and FG[("PB5",)].after, FG.get(("PB5",)) and FG[("PB5",)].continues),
      (True, "", ["worktree-done"]))
check("... its pred says merge that branch first and names the shared file",
      all(t in FG[("PB5",)].pred for t in ("CONTINUATION", "git merge", "worktree-done", "w1090c", F_BIND)), True)
check("fix lane: a dispatch of the group's OWN notes never defers it (the hold already decided them)",
      ("PB9",) in FG, True)
check("fix lane: a group meeting another dispatch's RUNNING group waits, naming it",
      ("PB11" in fl["waiting"], "running work dispatched w1091b" in fl["waiting"].get("PB11", "")), (True, True))
fin_own = pw.plan(notes, [cl(F_EMIT, "PB3")], [], [r3], lambda b: "UNLANDED", {}, RULES, budget_points=100,
                  busy={"worktree pred (" + str(r3.branch) + ")": pw.InFlight({F_EMIT}, r3.branch)})
check("a finisher never collides with the branch its own pred continues (its predecessor's worktree)",
      [(g.kind, g.notes) for g in fin_own["groups"]][:1], [("finisher", ["PB3"])])
CI_META = {"worktree a (worktree-done)": {"branch": "worktree-done", "rev": "x"},
           "worktree b (worktree-split)": {"branch": "worktree-split", "rev": "y"},
           "worktree c (worktree-findings)": {"branch": "worktree-findings", "rev": "z"},
           "worktree d (worktree-live)": {"branch": "worktree-live", "rev": "v"},
           "branch worktree-landed": {"branch": "worktree-landed", "rev": "worktree-landed"},
           "dispatched w1092a (PB67)": {"notes": ["PB67"], "at": 1.0, "slug": "w1092a"}}
SPL = dataclasses.replace(FIN, path=TMP / "w1090d-PB68-report.md", slug="w1090d", lead="PB68", status="SPLIT",
                          ids=["PB68"], branch="worktree-split")
FND = dataclasses.replace(FIN, path=TMP / "w1090e-PB69-report.md", slug="w1090e", lead="PB69", ids=["PB69"],
                          branch="worktree-findings", review=pw.Review("FINDINGS", 2, "r", "abc1234", "f.jsonl"))
# the survey's lifecycle states (prune_worktrees.lifecycle) decide; a branch the survey does not name is running
CI_STATES = {"worktree-done": "WAITING TO LAND", "worktree-split": "DECISION", "worktree-findings": "DECISION",
             "worktree-live": "IN FLIGHT", "worktree-landed": "LANDED"}
ci = pw.classify_inflight({o: {"src/A.cs"} for o in CI_META} | {"the open external-repository slices": {"src/S.cs"}},
                          CI_META, [FIN, SPL, FND], CI_STATES, {"worktree-live": ["PB70"]})
check("classify_inflight: WAITING TO LAND continues; DECISION, IN FLIGHT, a dispatch (answered ones never reach it: "
      "inflight_file_sets drops them, PB2795) and an unnamed owner run; LANDED is gone",
      {o: (x.finished.slug if x.finished else x.state) for o, x in ci.items()},
      {"worktree a (worktree-done)": "w1090c", "worktree b (worktree-split)": "DECISION",
       "worktree c (worktree-findings)": "DECISION", "worktree d (worktree-live)": "IN FLIGHT",
       "dispatched w1092a (PB67)": "DISPATCHED, NO REPORT YET", "the open external-repository slices": "RUNNING"})
check("classify_inflight: a branch's notes are its newest report's ids and its commit subjects' ids",
      (ci["worktree b (worktree-split)"].notes, ci["worktree d (worktree-live)"].notes), ({"PB68"}, {"PB70"}))
own = pw.plan(notes, [cl("src/X/Misc.cs", "PB9")], [], [], lambda b: "ABSENT", {}, RULES, budget_points=100,
              busy={"worktree e (worktree-old)": pw.InFlight({"src/X/Misc.cs"}, "worktree-old", {"PB9"}, state="DECISION")})
check("a branch working on the group's OWN note (an earlier attempt) never defers it",
      [g.notes for g in own["groups"]], [["PB9"]])
check("the one collision rule, path separators folded",
      pw.file_set_collisions(["src" + chr(92) + "A.cs"], {"t": {"src/A.cs"}, "u": {"src/B.cs"}}), ["t (src/A.cs)"])

# 9d. the in-flight set reads sources a branch CLASSIFICATION cannot drop (PB2118 Draft 7, the sixth refuter's
#     partition lead): a dispatched worktree with no commit (classifies MERGED) whose agent has edited a file, an
#     unlanded branch that only edits existing files (classifies LANDED), and a dispatched group with no worktree yet
#     are all in flight; a branch landed by content and a dispatched group whose notes are all landed are not.
#     A fake git (prune_worktrees.git's shape) stands in for the repository, so no git runs.
NL = chr(10)


class _R:
    def __init__(self, out="", rc=0):
        self.stdout, self.returncode = out, rc


def fake_git(*args, cwd=None):
    key = (str(cwd).replace(chr(92), "/"), args)
    table = {
        ("/r", ("worktree", "list", "--porcelain")): _R(NL.join([
            "worktree /r", "HEAD aaa", "branch refs/heads/main", "",
            "worktree /w/fresh", "HEAD aaa", "branch refs/heads/worktree-fresh", "",
            "worktree /w/edits", "HEAD bbb", "branch refs/heads/worktree-edits", ""])),
        ("/w/fresh", ("diff", "--name-only", "--no-renames", "origin/main...HEAD")): _R(""),
        ("/w/fresh", ("diff", "--name-only", "--no-renames", "HEAD")): _R("src/Fresh.cs" + NL),
        ("/w/fresh", ("ls-files", "--others", "--exclude-standard")): _R("STATUS.md" + NL),
        ("/w/edits", ("diff", "--name-only", "--no-renames", "origin/main...HEAD")): _R("src/Existing.cs" + NL),
        ("/w/edits", ("diff", "--name-only", "--no-renames", "origin/main", "HEAD")): _R("src/Existing.cs" + NL),
        ("/w/edits", ("diff", "--name-only", "--no-renames", "HEAD")): _R(""),
        ("/w/edits", ("ls-files", "--others", "--exclude-standard")): _R(""),
        ("/r", ("branch", "--format=%(refname:short)")): _R(NL.join(
            ["main", "worktree-fresh", "worktree-edits", "squashed", "merged"])),
        ("/r", ("merge-base", "--is-ancestor", "squashed", "origin/main")): _R("", 1),
        ("/r", ("merge-base", "--is-ancestor", "merged", "origin/main")): _R("", 0),
        ("/r", ("diff", "--name-only", "--no-renames", "origin/main...squashed")): _R("src/Landed.cs" + NL),
        ("/r", ("diff", "--name-only", "--no-renames", "origin/main", "squashed")): _R(""),
    }
    if key not in table:
        raise AssertionError(f"unexpected git call {key}")
    return table[key]


LEDGER = TMP / "coord"
LEDGER.mkdir()
coord.write_json(LEDGER / pw.DISPATCH_LEDGER, {"groups": [
    {"wave": "1040", "letter": "a", "notes": ["PB70"], "files": ["src/Dispatched.cs"]},
    {"wave": "1039", "letter": "b", "notes": ["PB71"], "files": ["src/Done.cs"]}]})
fly = pw.inflight_file_sets(git=fake_git, repo=pathlib.Path("/r"), coord_dir=LEDGER,
                            terminal=lambda i: i == "PB71")
flat = {f for fs in fly.values() for f in fs}
check("a dispatched worktree with no commit is in flight (its uncommitted edit)", "src/Fresh.cs" in flat, True)
check("an unlanded branch that only edits existing files is in flight", "src/Existing.cs" in flat, True)
check("a dispatched group with no worktree yet is in flight", "src/Dispatched.cs" in flat, True)
check("a branch landed by content is not", "src/Landed.cs" in flat, False)
check("a dispatched group whose notes all landed is not", "src/Done.cs" in flat, False)
check("STATUS.md is never a product file", "STATUS.md" in flat, False)
check("the planner's own checkout is not in flight", any("main" in k for k in fly), False)
check("the collision names the dispatched owner", pw.file_set_collisions(["src/Dispatched.cs"], fly),
      ["dispatched w1040a (PB70) (src/Dispatched.cs)"])

# 9e. the group's file set (kb/Work PB3242; PB2118 Draft 8, the seventh refuter's J2): its notes' DECLARED sets, the
#     structured sites (a bare type name in prose, "GammaState's", declares nothing) plus the callers the member index
#     computes for the note (an injected function here; member_index.py's own self-test drives the computation), plus
#     the primary file; and the dispatch ledger has STRUCTURED writers only (kb/Work PB3186):
#     record_dispatch upserts on (wave, letter) and drops the retired hand entries, and make_dispatch_specs.py records
#     every group it renders with its `file_set` list (or the set computed from its notes when a hand-built groups
#     file carries none).
if fc:
    C2 = TMP / "campaign2"
    for rel in ("src/Lib/Alpha.cs", "src/Lib/GammaState.cs", "src/Lib/Beta.cs", "src/Lib/Caller.cs"):
        (C2 / rel).parent.mkdir(parents=True, exist_ok=True)
        (C2 / rel).write_text("// fixture" + NL, encoding="utf-8", newline="\n")
    W2 = C2 / "kb" / "Work"
    W2.mkdir(parents=True)
    for nid, body in (("PB950", f"**Sites:**{NL}- `src/Lib/Alpha.cs`{NL}{NL}Fold GammaState's flags into it."),
                      ("PB951", f"**Sites:**{NL}- `src/Lib/Beta.cs`{NL}{NL}Move its members (callers computed).")):
        (W2 / f"{nid}.md").write_text(
            f'---{NL}title: "{nid} — step"{NL}id: {nid}{NL}kind: analysis{NL}status: open{NL}area: architecture{NL}'
            f"wrong_answer: false{NL}process_only: true{NL}blocked: false{NL}blocked_by: []{NL}cluster: [PB950]{NL}---"
            f"{NL}{NL}# {nid}{NL}{NL}{body}{NL}", encoding="utf-8", newline="\n")
    items2 = [dict(work.parse_frontmatter(q.read_text(encoding="utf-8")), _file=q.name) for q in sorted(W2.glob("*.md"))]
    view2 = work.cluster_order(items2, "PB950")
    notes2 = pw.load_notes(W2, {"wrong_answer": 8})
    texts2 = {i: (W2 / f"{i}.md").read_text(encoding="utf-8") for i in notes2}
    D2 = pw.declared_sets(C2, pw.IndexPolicy(TMP / "no-cache", regenerate=False))
    o2, _, _, _ = pw.campaign_clusters(view2, notes2, texts2, RULES, fc, C2,
                                       lambda text: D2(text) | ({"src/Lib/Caller.cs"} if "PB951" in text else set()))
    files2 = {n["id"]: c["files"] for c in o2 for n in c["notes"]}
    check("a bare type name in prose declares nothing (PB3242)", "src/Lib/GammaState.cs" in files2["PB950"], False)
    check("a note's computed callers are in its group's file set", "src/Lib/Caller.cs" in files2["PB951"], True)
    check("callers never steer the clustering (the primary file is a named site)",
          sorted(c["file"] for c in o2), ["src/Lib/Alpha.cs", "src/Lib/Beta.cs"])
    check("the collision check sees a caller", pw.file_set_collisions(files2["PB951"], {"train t": {"src/Lib/Caller.cs"}}),
          ["train t (src/Lib/Caller.cs)"])
HL = TMP / "coord-hand"
HL.mkdir()
coord.write_json(HL / pw.DISPATCH_LEDGER, {"groups": [
    {"wave": "hand", "letter": "", "label": "cobol-clerk: x", "hand": True, "notes": ["PB961"], "files": ["src/Junk.cs"],
     "at": 1.0},
    {"wave": "1060", "letter": "A", "notes": ["PB962"], "files": ["src/Old.cs"], "at": 2.0}]})
check("a retired hand entry is no dispatch: not in flight, not held, no wave number",
      ({f for fs in pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: False, with_git=False).values() for f in fs},
       pw.held_by_dispatch(HL, [], lambda i: False), pw.next_wave(TMP / "no-devlog", [], HL)),
      ({"src/Old.cs"}, {"PB962": "dispatch w1060a"}, 1061))
first = pw.record_dispatch(HL, "1061", [{"letter": "A", "notes": ["PB960"], "files": ["src" + chr(92) + "Hand.cs"]},
                                        {"letter": "B", "notes": ["PB963"], "files": ["src/B.cs"]}], lambda i: False)
again = pw.record_dispatch(HL, "1061", [{"letter": "a", "notes": ["PB960"], "files": ["src/Hand.cs", "src/More.cs"]}],
                           lambda i: False)
LG = coord.read_json(HL / pw.DISPATCH_LEDGER, {})["groups"]
check("record_dispatch drops the retired hand entry and keeps the other waves", [(g["wave"], g["letter"]) for g in LG],
      [("1060", "A"), ("1061", "B"), ("1061", "a")])
check("record_dispatch UPSERTS on (wave, letter): the re-recorded group replaces its entry, its files exact, POSIX",
      [g["files"] for g in LG if g["wave"] == "1061" and g["letter"].upper() == "A"], [["src/Hand.cs", "src/More.cs"]])
check("each entry carries its report slug", [g["slug"] for g in first + again], ["w1061a", "w1061b", "w1061a"])
check("a recorded group is in flight before its agent edits a file, until its notes land",
      ({f for fs in pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: False, with_git=False).values() for f in fs}
       >= {"src/Hand.cs", "src/More.cs"},
       pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: True, with_git=False)), (True, {}))
HL_REPORTS = TMP / "reports-hl"
HL_REPORTS.mkdir()


def hl_report(name, status, when=None, sub=""):
    """A real report file (the answered rule reads its mtime), `when` seconds since the epoch or now."""
    path = HL_REPORTS / sub / name
    path.parent.mkdir(exist_ok=True)
    path.write_text(f"Status: {status}\n# {name}\n**branch:** `worktree-wf_hl`\n\n## X\n", encoding="utf-8", newline="\n")
    if when is not None:
        os.utime(path, (when, when))
    return pw.parse_report(path)


HL_AT = {g["letter"]: g["at"] for g in LG}
check("an ANSWERED group (its report exists, SPLIT residue included) holds no declared file, for every reader (PB2795)",
      {f for fs in pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: False, with_git=False,
                                         reports=[hl_report("w1061b-PB963-report.md", "SPLIT", HL_AT["B"] + 60)]).values()
       for f in fs} & {"src/B.cs", "src/Hand.cs"}, {"src/Hand.cs"})
# kb/Work PB3311 hole 2 (with PB3152's second-predicate witness): ONE predicate (`answers`) says "a dispatch returned"
# for the notes' hold AND the group's file set: the group's OWN report (its slug), written at or after the dispatch.
# A reviewer's stamp on ANOTHER dispatch's report names the same note and bumps its mtime past the dispatch, and a
# header that merely mentions the note says nothing about it: neither answers (the dispatch is the review finisher
# `w1101ar1` of PB3198, whose original report `w1101a` the branch reviewer re-stamped after the dispatch).
RV = TMP / "coord-review"
RV.mkdir()
pw.record_dispatch(RV, "1101", [{"letter": "AR1", "notes": ["PB3198"], "files": ["src/Review.cs"]}], lambda i: False)
RV_AT = coord.read_json(RV / pw.DISPATCH_LEDGER, {})["groups"][0]["at"]


def rv_state(*reports):
    held = pw.held_by_dispatch(RV, list(reports), lambda i: False)
    files = {f for fs in pw.inflight_file_sets(coord_dir=RV, terminal=lambda i: False, with_git=False,
                                               reports=list(reports)).values() for f in fs}
    return held, files


STAMPED = hl_report("w1101a-PB3198-report.md", "DONE", RV_AT + 600)           # the reviewed report, stamped after
MENTION = hl_report("w1102z-PB3203-report.md", "DONE", RV_AT + 600)           # its header names PB3198 below
MENTION.ids.append("PB3198")
EARLIER = hl_report("w1101ar1-PB3198-report.md", "DONE", RV_AT - 3600, "earlier")      # an earlier dispatch's, reused slug
RETURNED = hl_report("w1101ar1-PB3198-report.md", "DONE", RV_AT + 900)        # the review finisher's own report
HELD = ({"PB3198": "dispatch w1101ar1"}, {"src/Review.cs"})
check("a reviewer's stamp on another dispatch's report, a header that mentions the note, and an earlier dispatch's "
      "report of the slug answer nothing: held and in flight by both readers (PB3311)",
      (rv_state(STAMPED), rv_state(MENTION), rv_state(EARLIER)), (HELD, HELD, HELD))
check("the group's own report written since the dispatch answers it for both readers (PB3311)",
      rv_state(STAMPED, RETURNED), ({}, set()))
check("a report whose file is gone answers nothing (the safe side of a double dispatch)",
      rv_state(dataclasses.replace(RETURNED, path=HL_REPORTS / "gone-PB3198-report.md")), HELD)
pw.record_dispatch(HL, "1062", [], lambda i: i == "PB962")
check("an entry whose notes all landed is dropped at the next record",
      [g["wave"] for g in coord.read_json(HL / pw.DISPATCH_LEDGER, {})["groups"]], ["1061", "1061"])
MDS = TMP / "coord-mds"
MDS.mkdir()
MCFG = {"wave": "1070", "groups": [{"letter": "A", "slug": "w1070a", "notes": "PB970, PB971", "file_set": ["src/M.cs"]},
                                   {"letter": "B", "slug": "w1070b", "notes": "PB972"}]}
mds.record(MCFG, MDS, terminal=lambda i: False, file_set_of=lambda ids: {f"src/{i}.cs" for i in ids})
check("make_dispatch_specs records every rendered group: its file_set list, else the set computed from its notes",
      [(g["slug"], g["notes"], g["files"]) for g in coord.read_json(MDS / pw.DISPATCH_LEDGER, {})["groups"]],
      [("w1070a", ["PB970", "PB971"], ["src/M.cs"]), ("w1070b", ["PB972"], ["src/PB972.cs"])])
check("the planner's groups file carries the file set as a LIST", gjson[0]["file_set"], sorted(p["groups"][0].files))
# 9f. (PB2118 Draft 9) the landing check orders two branches by dispatch time: the in-flight enumeration reports each
#     dispatch entry's notes and time through `meta`; and the planner never plans on a stale member index
META: dict = {}
pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: i != "PB960", with_git=False, meta=META)
check("meta carries a dispatch entry's notes, time and slug",
      [(m["notes"], m["at"], m["slug"]) for m in META.values()], [(["PB960"], again[0]["at"], "w1061a")])
STALE = TMP / "stale-repo"
(STALE / "src").mkdir(parents=True)
(STALE / "src" / "A.cs").write_text("// a" + NL, encoding="utf-8", newline="\n")
for args in (["init", "-q"], ["add", "-A"], ["-c", "user.name=t", "-c", "user.email=t@t", "commit", "-qm", "c"]):
    subprocess.run(["git", *args], cwd=STALE, check=True, capture_output=True)
try:
    pw.declared_sets(STALE, pw.IndexPolicy(TMP / "no-cache", regenerate=False))("**Moves or changes:** `N.T.M`")
    check("the planner refuses a tree no member index describes", "planned", "refused")
except SystemExit:
    check("the planner refuses a tree no member index describes", "refused", "refused")

# 9g. (PB2118 Draft 10, the ninth refuter) every changed-file read passes --no-renames (the fake git above refuses a
#     diff without it, so 9d already holds the enumeration to it); one wave's groups are recorded strictly apart in
#     letter order, so no two tie (N4); and an aborted dispatch is released by wave, group or note, never by a clock (N5)
RL = TMP / "release"
RL.mkdir()
pw.record_dispatch(RL, "1050", [{"letter": "a", "notes": ["PB80"], "files": ["src/X.cs"]},
                                {"letter": "b", "notes": ["PB81"], "files": ["src/Y.cs"]},
                                {"letter": "c", "notes": ["PB82"], "files": ["src/Z.cs"]}], terminal=lambda i: False)
ATS = [g["at"] for g in coord.read_json(RL / pw.DISPATCH_LEDGER, {})["groups"]]
check("one wave's groups never tie and keep letter order", ATS == sorted(ATS) and len(set(ATS)) == 3, True)
check("release by group", [g["letter"] for g in pw.release_dispatch(RL, "1050b")], ["b"])
check("release by note", [g["letter"] for g in pw.release_dispatch(RL, "PB82")], ["c"])
check("release by wave", [g["letter"] for g in pw.release_dispatch(RL, "1050")], ["a"])
check("nothing left to release", pw.release_dispatch(RL, "1050"), [])
check("changed_files passes --no-renames",
      pw.changed_files(lambda *a, **k: _R(NL.join(a) + NL) if "--no-renames" in a else _R(""), "x...y"),
      {"diff", "--name-only", "--no-renames", "x...y"})

# 10. work.py check holds blocked to blocked_by: a stale flag, an unknown blocker, a cycle
TOPO = [{"_file": "T1.md", "id": "T1", "status": "open", "blocked": True, "blocked_by": ["T9"], "cluster": []},
        {"_file": "T9.md", "id": "T9", "status": "landed", "blocked": False, "blocked_by": [], "cluster": []},
        {"_file": "T2.md", "id": "T2", "status": "open", "blocked": True, "blocked_by": ["nobody"], "cluster": []},
        {"_file": "T3.md", "id": "T3", "status": "open", "blocked": True, "blocked_by": ["T4"], "cluster": ["T3"]},
        {"_file": "T4.md", "id": "T4", "status": "open", "blocked": True, "blocked_by": ["T3"], "cluster": "T3"},
        {"_file": "T5.md", "id": "T5", "status": "open", "blocked": False, "blocked_by": ["T3"], "cluster": []},
        {"_file": "T6.md", "id": "T6", "status": "open", "blocked": True, "blocked_by": ["T7"], "cluster": ["C"]},
        {"_file": "T7.md", "id": "T7", "status": "open", "blocked": True, "blocked_by": ["T6"], "cluster": ["C"]}]
probs = work.topology_problems(TOPO)
check("stale blocked flag", any(s.startswith("T1.md: blocked is True") for s in probs), True)
check("unknown blocker", any(s.startswith("T2.md: blocked_by names 'nobody'") for s in probs), True)
check("cycle", any("cycle among open notes: T3 -> T4 -> T3" in s for s in probs), True)
check("cluster must be a list", any(s.startswith("T4.md: cluster must be a list") for s in probs), True)
check("an open blocker needs the flag", any(s.startswith("T5.md: blocked is False") for s in probs), True)
check("a second cycle", any("cycle among open notes: T6 -> T7 -> T6" in s for s in probs), True)
check("exactly those six", len(probs), 6)
check("a cycle never reads as ready", [(r["id"], r["depth"], r["ready"]) for r in work.cluster_order(TOPO, "C")["notes"]],
      [("T6", None, False), ("T7", None, False)])
check("a scalar cluster is no member (no substring match)", [r["id"] for r in work.cluster_order(TOPO, "T3")["notes"]],
      ["T3"])

# 11. the slices' file set reads ALL of DESIGN-external-repository §12 (PB2118 Draft 7, the sixth refuter's H4): the
#     Callers column, every name in a span (`X.Y : Z`), a member mapped to the file that declares it, tests/ and docs/
#     paths; a parameter list and a one-word name are not sites. A synthetic repository first, then the refuter's
#     four plants against the real register, each of which must FAIL the register's slice rule.
SR = TMP / "slicerepo"
(SR / "docs" / "rearchitecture").mkdir(parents=True)
(SR / "src" / "B").mkdir(parents=True)
(SR / "tests" / "T").mkdir(parents=True)
(SR / "docs" / "rearchitecture" / "DESIGN-external-repository.md").write_text(NL.join([
    "## 12. Changes", "", "| Today | Change | Callers updated in the same change |", "|---|---|---|",
    "| `Session.Repo : GroupRepo` | `: Resolver` | `DriverX.Bind`, `CheckPairs` |",
    "| `Rec(Kind, Name)` | `+ Target` | `Target` |",
    "| `OldThing` | **landed (kb/Work PB1):** done | `LandedCaller` |",
    "| tests (`tests/T/HostTests.cs`) | rewritten | `docs/NOTES.md` |",
    "", "## 13. Next", ""]), encoding="utf-8", newline="\n")
for rel, text in {"src/B/Session.cs": "public sealed class Session { public GroupRepo Repo { get; } }",
                  "src/B/GroupRepo.cs": "internal sealed class GroupRepo { }",
                  "src/B/DriverX.cs": "public static class DriverX { public static void Bind() { } }",
                  "src/B/Checks.cs": NL.join(["static class Checks", "{", "    private static void CheckPairs(int a) { }", "}"]),
                  "src/B/Rec.cs": "public sealed record Rec(int K);",
                  "src/B/Kind.cs": "public enum Kind { A }",
                  "src/B/Target.cs": NL.join(["class Holder", "{", "    public int Target { get; set; }", "}"]),
                  "src/B/LandedCaller.cs": "class LandedCaller { }",
                  "tests/T/HostTests.cs": "public class HostTests { }",
                  "docs/NOTES.md": "notes"}.items():
    (SR / rel).write_text(text, encoding="utf-8", newline="\n")
# a build output and a stray file name the same type: the slice set reads the COMMITTED tree (PB1957, 2026-10-07)
(SR / "src" / "B" / "Generated").mkdir(parents=True)
(SR / "src" / "B" / "Generated" / "SessionParser.cs").write_text("class Session { }", encoding="utf-8", newline="\n")
subprocess.run(["git", "-C", str(SR), "init", "-q"], check=True)
subprocess.run(["git", "-C", str(SR), "add", "-A", "--", ":!src/B/Generated"], check=True)
(SR / "src" / "B" / "Stray.cs").write_text("class GroupRepo { }", encoding="utf-8", newline="\n")
um = []
sset = work.slice_file_set(SR, unmapped=um)
check("an untracked file declaring a slice name is not in the slice set", "src/B/Stray.cs" in sset, False)
check("a build output declaring a slice name is not in the slice set", "src/B/Generated/SessionParser.cs" in sset, False)
check("the Today column's first name", "src/B/Session.cs" in sset, True)
check("the second name of a `X.Y : Z` span", "src/B/GroupRepo.cs" in sset, True)
check("the Callers column (a type)", "src/B/DriverX.cs" in sset, True)
check("the Callers column (a member, mapped to its declaring file)", "src/B/Checks.cs" in sset, True)
check("a tests/ path and a docs/ path", {"tests/T/HostTests.cs", "docs/NOTES.md"} <= sset, True)
check("a parameter list names no site", "src/B/Kind.cs" in sset, False)
check("a one-word member name is prose, reported", ("src/B/Target.cs" in sset, any(u.startswith("Target") for u in um)),
      (False, True))
check("a landed row is history", "src/B/LandedCaller.cs" in sset, False)

REAL = work.load()
REAL_SET = work.slice_file_set()


def r3_plant(pid, site):
    body = (NL + "**Wave kind:** extract. **Model:** Opus." + NL + NL + "**Sites:**" + NL + "- `" + site + "`" + NL + NL
            + "**Blocked by:** PB2118, PB2417." + NL)
    return {"_file": pid + ".md", "id": pid, "title": pid + " — Extract: planted", "status": "open", "blocked": True,
            "blocked_by": ["PB2118", "PB2417"], "cluster": ["PB2119"], "_body": body}


for pid, site in (("PB9991", "src/Cobol.Net.Compiler/Binding/BindSession.cs"),
                  ("PB9992", "src/Cobol.Net.Compiler/Binding/GroupRepository.cs"),
                  ("PB9993", "src/Cobol.Net.Compiler/Oo/OoConformance.cs"),
                  ("PB9994", "tests/Cobol.Net.Tests.Conformance/NonCobolActivatorReturnTests.cs")):
    hit = [m for m in work.r3_program_problems(REAL + [r3_plant(pid, site)], REAL_SET) if m.startswith(pid)]
    check(f"a planted R3 note on {site.rsplit('/', 1)[-1]} must wait for an open slice", bool(hit), True)

# PB2197 (the R1 landing's CI red): the existence rule reads the COMMITTED tree, so a build output that exists in a built
# checkout and not in CI's never decides it; a note naming a generated parser file must name the grammar it comes from.
GEN = "src/Cobol.Net.Frontend/Generated/CobolParserCore.cs"
G4 = "src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4"
gen_only = [m for m in work.r3_program_problems(REAL + [r3_plant("PB9995", GEN)], REAL_SET) if m.startswith("PB9995")]
check("a planted R3 note naming only a generated file is refused (build output, no grammar)",
      any("build output" in m for m in gen_only), True)
check("a generated file is never reported missing, built checkout or not", any("does not exist" in m for m in gen_only), False)
with_g4 = r3_plant("PB9996", GEN)
with_g4["_body"] += "- `" + G4 + "`" + NL
check("a planted R3 note naming the generated file AND its grammar passes the build-output rule",
      any("build output" in m for m in work.r3_program_problems(REAL + [with_g4], REAL_SET) if m.startswith("PB9996")), False)
check("an untracked path is missing even if a local file has that name",
      work.named_missing_paths("`src/Cobol.Net.Frontend/obj/project.assets.json`"), ["src/Cobol.Net.Frontend/obj/project.assets.json"])

# PB2562: a train label is a taken wave number, because a wave's first train is labelled with the wave number.
with tempfile.TemporaryDirectory() as td:
    tdir = pathlib.Path(td)
    (tdir / "DEVLOG").mkdir()                                    # one file per entry (kb/Work PB3025)
    (tdir / "DEVLOG" / "0002-train-1036.md").write_text("## Entry 2 — Train 1036 (wave 1032: D, E)" + NL, encoding="utf-8", newline="\n")
    (tdir / "DEVLOG" / "0001-wave-1034.md").write_text("## Entry 1 — wave 1034 dispatched" + NL, encoding="utf-8", newline="\n")
    check("next_wave counts a train label above every wave number", pw.next_wave(tdir, []), 1037)
    # PB2806: a wave recorded in the dispatch ledger with no report yet (another session's) is a taken number too
    pw.record_dispatch(tdir, "1045", [{"letter": "a", "notes": ["PB5"], "files": ["src/X.cs"]}], lambda i: False)
    check("next_wave counts a dispatched wave with no report", pw.next_wave(tdir, [], tdir), 1046)

# PB3181: THE ONE "FINISHED REPORT" PREDICATE (Report.finished) and its `--finished` arm, which the rolling wave asks
# before it reviews a return; the header's HEAD and BASE fields are read field-first.
with tempfile.TemporaryDirectory() as td:
    fdir = pathlib.Path(td)
    full = "0123456789abcdef0123456789abcdef01234567"
    for name, text in (("w9f1a-PB1-report.md", "Status: DONE\nBranch: b\nHEAD: 1111111\nBase: 2222222\n"),
                       ("w9f1b-PB1-report.md", "**Status:** DISCHARGED · **branch:** `b` · **base:** `2222222` · "
                                               "**HEAD:** `3333333`\n"),
                       ("w9f1c-PB1-report.md", f"DISCHARGED\nBase {full}  Head {full[:9]}\n"),
                       ("w9f1d-PB1-report.md", "Status: DISCHARGED\nBase: 2222222\n"),
                       ("w9f1e-PB1-report.md", "Status: SPLIT\nHEAD: 1111111\n"),
                       ("w9f1f-PB1-report.md", "\n**Review:** CLEAN r1 1111111\n# a report with no status line\n"
                                               "Branch: b · head 1111111\n"),
                       ("w9f1g-PB1-report.md", "Status: DONE\nBranch: b · base 2222222 (reviewed head 4444444 merged in)"
                                               " · head 5555555\n")):
        (fdir / name).write_text(text + "\n## Rule\nHEAD: 9999999 below the header binds nothing\n", encoding="utf-8",
                                 newline="\n")
    got = {n[4]: (r.status, r.head, r.base, r.finished) for n in sorted(p.name for p in fdir.iterdir())
           if (r := pw.parse_report(fdir / n))}
    check("Report.finished: DONE, or DISCHARGED with a HEAD past its base; never SPLIT, no status, head == base or none",
          {k: v[3] for k, v in got.items()},
          {"a": True, "b": True, "c": False, "d": False, "e": False, "f": False, "g": True})
    check("the template's base and HEAD fields are read", got["b"][1:3], ("3333333", "2222222"))
    check("a field-position head beats a head named in prose before it (w1080f's header)", got["g"][1], "5555555")
    check("an abbreviated head names its full base: one commit (same_commit)", pw.same_commit(full[:9], full), True)
    for letter, want in (("a", 0), ("b", 0), ("c", 1), ("d", 1), ("f", 1)):
        check(f"--finished w9f1{letter}: exit {want}", pw.main(["--finished", str(fdir / f"w9f1{letter}-PB1-report.md")]),
              want)
    check("--finished on a missing report: exit 2", pw.main(["--finished", str(fdir / "w9f1z-PB1-report.md")]), 2)
    (fdir / "notes.md").write_text("Status: DONE\n", encoding="utf-8", newline="\n")
    check("--finished on a name that is not a fix-lane report: exit 2", pw.main(["--finished", str(fdir / "notes.md")]), 2)
    check("status_line finds the bare word and its index; None without one",
          (pw.status_line(["# title", "DONE"]), pw.status_line(["# title", "Branch: b"])), ((1, "DONE"), None))

# 10. A NOTE'S DECLARED FILE SET IS ITS STRUCTURED SITES, READ BY ONE READER (kb/Work PB3242; DESIGN-architecture-review
#     §8.7). The planner once declared every path a note's prose mentioned (fix_clusters' scored reading), so the R2
#     and PB3160 campaigns planned zero groups: 57 of PB1754's 63 ready notes DISCUSS plan_wave.py and landing_check.py,
#     and every PB3160 slice's Gate: line names cite.py.
DR = TMP / "declared"
for rel in ("src/Lib/Alpha.cs", "src/Lib/Beta.cs", "tests/Delta.Tests/DeltaTests.cs", "scripts/gamma.py",
            "scripts/spec/cite.py", "src/Model/DataItem.cs"):
    (DR / rel).parent.mkdir(parents=True, exist_ok=True)
    (DR / rel).write_text("// fixture" + NL, encoding="utf-8", newline="\n")
DDECL = pw.declared_sets(DR, pw.IndexPolicy(TMP / "no-cache", regenerate=False))
sys.path.insert(0, str(coord.REPO / "scripts" / "arch"))
import member_index  # noqa: E402
GATE_ONLY = (f"**Sites:**{NL}- `scripts/gamma.py`{NL}{NL}Probe it with `python scripts/spec/cite.py --check 14.9 \"x\"`."
             f"{NL}{NL}**Gate:** `python scripts/spec/cite.py --check 14.9 \"x\"` and `pwsh scripts/build-local.ps1`{NL}")
check("(1) a note whose only mention of cite.py is a Gate: line and prose declares no cite.py",
      ("scripts/spec/cite.py" in DDECL(GATE_ONLY), sorted(DDECL(GATE_ONLY))), (False, ["scripts/gamma.py"]))
EVERY_SHAPE = (f"**Sites:**{NL}- `src/Lib/Alpha.cs`{NL}- `src/Lib/Beta.cs#Run` (Run){NL}"
               f"- `tests/Delta.Tests/DeltaTests.cs:12-40`{NL}- scripts/gamma.py (main){NL}"
               f"- `M:N.OoBinder.Err(N.Model.DataItem)~System.String @ src/.../OoBinder.cs:1783`{NL}{NL}"
               f"**Fix.** It also touches `src/Model/DataItem.cs` in prose.{NL}")
SITES = ("scripts/gamma.py", "src/Lib/Alpha.cs", "src/Lib/Beta.cs", "tests/Delta.Tests/DeltaTests.cs")
check("(2) a Sites path, a file#member, a file:line and a scripts/ path each declare their file; a census member id's "
      "parameter type (PB2423) and a path in prose declare nothing", work.note_sites(EVERY_SHAPE).paths, SITES)
check("(2) the census member id is a name, its return type dropped",
      work.note_sites(EVERY_SHAPE).names, ("N.OoBinder.Err(N.Model.DataItem)",))
for shape, text in (("a bold paragraph label", f"**Code site.** src/Lib/Alpha.cs#Run (the arm){NL}{NL}**Fix.** x"),
                    ("a heading section", f"## Code sites{NL}{NL}`src/Lib/Alpha.cs:7`, and more:{NL}{NL}"
                                          f"### Detail{NL}`src/Lib/Beta.cs`{NL}## Fix{NL}`scripts/gamma.py`"),
                    ("a list-item label", f"- **Code site:** src/Lib/Alpha.cs#Run{NL}- **Fix:** scripts/gamma.py"),
                    ("a label alone over a loose list", f"**Sites:**{NL}{NL}- `src/Lib/Alpha.cs`{NL}{NL}Prose: "
                                                        f"scripts/gamma.py")):
    want = ("src/Lib/Alpha.cs", "src/Lib/Beta.cs") if shape == "a heading section" else ("src/Lib/Alpha.cs",)
    check(f"(2) the site block as {shape} ends where the block ends", work.note_sites(text).paths, want)
check("(2) a placeholder, a glob and a bare root are no sites",
      work.note_sites(f"**Sites:**{NL}- `src/<Project>/X.cs`{NL}- `tests/**/char_*.txt`{NL}- `src/`{NL}").paths, ())
check("(2) the roots are model_rules.json's campaign roots, never a literal",
      work.campaign_domain().roots, tuple(RULES["campaign"]["src"]))
EMPTY_INDEX = {"commit": "c", "uses": {}, "types": {}, "declared": {}, "emitted": {}, "_simple": {}}
fs_mi = member_index.note_file_set(EVERY_SHAPE, DR, index=lambda: EMPTY_INDEX, drifted=lambda: {})
check("(3) the member index's declared set of a note with a name: its sites (the name unknown to this index)",
      (tuple(fs_mi["sites"]), tuple(fs_mi["files"]), fs_mi["unknown"]), (SITES, SITES, ["N.OoBinder.Err(N.Model.DataItem)"]))
SITES_ONLY = EVERY_SHAPE.replace(f"- `M:N.OoBinder.Err(N.Model.DataItem)~System.String @ src/.../OoBinder.cs:1783`{NL}", "")
check("(3) the planner, the member index and the register read the same sites for the same note",
      (tuple(sorted(DDECL(SITES_ONLY))), tuple(member_index.note_file_set(SITES_ONLY, DR)["sites"]),
       work.note_sites(SITES_ONLY).paths), (SITES, SITES, SITES))
if fc:
    CD = TMP / "declared-campaign" / "kb" / "Work"
    CD.mkdir(parents=True)
    (CD / "PB990.md").write_text(f"---{NL}id: PB990{NL}status: open{NL}cluster: [PB990]{NL}---{NL}{NL}{SITES_ONLY}",
                                 encoding="utf-8", newline="\n")
    citems = [dict(work.parse_frontmatter((CD / "PB990.md").read_text(encoding="utf-8")), _file="PB990.md")]
    cn = pw.load_notes(CD, {"wrong_answer": 8})
    o3, _, _, _ = pw.campaign_clusters(work.cluster_order(citems, "PB990"), cn,
                                       {"PB990": (CD / "PB990.md").read_text(encoding="utf-8")}, RULES, fc, DR, DDECL)
    check("(3) the planner's campaign group: its primary file is a declared site, its files exactly the declared set",
          (o3[0]["file"] in SITES, tuple(o3[0]["files"])), (True, SITES))
FL = TMP / "declared-fix" / "kb" / "Work"
FL.mkdir(parents=True)
(FL / "PB991.md").write_text(f"---{NL}id: PB991{NL}---{NL}{NL}{GATE_ONLY}", encoding="utf-8", newline="\n")
FLC = [{"file": "src/Lib/Alpha.cs", "files": ["src/Lib/Alpha.cs", "scripts/spec/cite.py"], "notes": [{"id": "PB991"}]}]
pw.declare_fix_lane(FLC, DDECL, FL)
check("a fix-lane cluster's collision set is its notes' declared sets plus its primary file, never fix_clusters' "
      "prose sites", FLC[0]["files"], ["scripts/gamma.py", "src/Lib/Alpha.cs"])
# Branch review R1-2: an R2 defect routed to the fix lane (its wave kind work.FIX_LANE_WAVE, with r2_members) is a
# fix-lane note: the fix lane's plan declares its sites without reading the member index, so it never regenerates
# one (DDECL's policy has no cache and refuses to regenerate: a read would raise StaleIndex).
(FL / "PB992.md").write_text(f"---{NL}id: PB992{NL}---{NL}{NL}r2_members: N.Beta.Run, N.Alpha.Go{NL}{NL}"
                             f"**Wave kind:** {work.FIX_LANE_WAVE}. **Model:** the fix lane's routing.{NL}{NL}"
                             f"**Sites:**{NL}- `src/Lib/Beta.cs:10-20` (Beta.Run){NL}", encoding="utf-8", newline="\n")
FLD = [{"file": "src/Lib/Beta.cs", "files": ["src/Lib/Beta.cs"], "notes": [{"id": "PB992"}]}]
try:
    pw.declare_fix_lane(FLD, DDECL, FL)
    check("R1-2: an R2 fix-lane defect with r2_members declares its sites and reads no index", FLD[0]["files"],
          ["src/Lib/Beta.cs"])
except Exception as e:   # noqa: BLE001 — any read of the index (StaleIndex, a git call on a fixture) is the red
    check("R1-2: an R2 fix-lane defect with r2_members declares its sites and reads no index",
          f"read the index: {type(e).__name__}: {e}",
          ["src/Lib/Beta.cs"])
sys.path.insert(0, str(coord.REPO / "scripts" / "arch"))
import r2_collect  # noqa: E402
import file_census_notes  # noqa: E402
check("R1-2: the fix lane's wave kind is one of the R2 collector's wave kinds (one constant, work.FIX_LANE_WAVE)",
      work.FIX_LANE_WAVE in r2_collect.WAVES, True)
# Branch review R1-1: the line file_census_notes writes naming the files ONE STEP edits is a site block, so a step file
# outside the census finding's Sites (PB2300's IntrinsicCatalog.cs) is declared; the reader reads the writer's line.
STEP = f"**Sites:**{NL}- `src/Lib/Alpha.cs`{NL}{NL}{file_census_notes.step_files_line(['src/Lib/Alpha.cs', 'src/Lib/Beta.cs'])}{NL}"
check("R1-1: the step-files line file_census_notes writes declares its files (reader = planner)",
      (work.note_sites(STEP).paths, tuple(sorted(DDECL(STEP)))), (("src/Lib/Alpha.cs", "src/Lib/Beta.cs"),) * 2)


def note_path_scanners(sources: dict[str, str]) -> list[str]:
    """(4) the modules that scan text for paths with a regex over the campaign roots (`(?:src|tests…)/`) or call
    fix_clusters' prose scorer (`.sites(`): only the register's reader (scripts/spec/work.py) may read a note's paths.
    The other regex shapes read documents that are not notes: drift_rules.py a C# drift test's string literals,
    audit_evidence_supersession.py the evidence records. A test file's fixtures are no reader."""
    alts = "|".join(sorted({re.escape(x) for r in RULES["campaign"]["src"] for x in (r, re.escape(r))}))
    shape = re.compile(r"\((?:\?:)?(?:" + alts + r")\||\.sites\(")
    exempt = {"scripts/spec/work.py", "scripts/spec/drift_rules.py", "scripts/spec/audit_evidence_supersession.py"}
    return sorted(p for p, t in sources.items()
                  if p not in exempt and not pathlib.PurePosixPath(p).name.startswith("test_") and shape.search(t))


TREE = {p.relative_to(coord.REPO).as_posix(): p.read_text(encoding="utf-8", errors="replace")
        for root in ("scripts", ".claude/skills") for p in (coord.REPO / root).rglob("*.py")}
check("(4) no module outside the reader scans a note's text for paths", note_path_scanners(TREE), [])
PLANT = "SITE_PATH = re.compile(r\"`((?" + ":src|tests)/[^`]+)\")"
check("(4) the scan fails on a planted copy of the old whole-text SITE_PATH and on a prose-scorer call",
      note_path_scanners({"scripts/arch/planted.py": PLANT, "scripts/orchestrator/p2.py": "fc" + ".sites(text, idx, exts)",
                          "scripts/orchestrator/fine.py": "x = 1"}),
      ["scripts/arch/planted.py", "scripts/orchestrator/p2.py"])

for f in fails:
    print("FAIL:", f)
print(f"plan_wave self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
sys.exit(1 if fails else 0)
