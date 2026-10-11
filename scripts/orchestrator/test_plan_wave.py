#!/usr/bin/env python3
"""Self-test for plan_wave.py on fabricated notes, clusters and reports: work awaiting landing is never re-planned,
finishers come first and name their predecessor, clusters rank by rows claimed, a second group on a file is a
same-file successor, the model routing rules, the budget fill (with landers and successors), and that the groups
render through the real dispatch-spec template and pass check_practices.py's same-file rule; that the fix lane's whole
plan equals the golden the pre-campaign planner wrote; and the campaign lane (--cluster, kb/Work PB2120): cluster
selection whatever the harm flags, the blocked_by order, site clustering per depth, the `after:` derivation, the
waits (blockers in two chains, the owner, a frontier model), and work.py check's topology rules.
Run: python scripts/orchestrator/test_plan_wave.py   (no build, no git; the campaign section needs the
tools/claude-skills submodule, and its absence is a FAIL)."""
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
        f"wrong_answer: true\n{inv}\n---\n\n# {nid}\n\n{body}\n", encoding="utf-8")


def report(name, first_line, header):
    (REPORTS / name).write_text(f"{first_line}\n{header}\n\n## Reproduced?\nPB9999 is mentioned only below the header.\n",
                                encoding="utf-8")


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
one = pw.group_cost(G[("PB3", "PB6", "PB7", "PB12")].model, 4, RULES) + pw.lander_cost(RULES)
p2 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=one + 0.01)
check("tight budget: first group + its lander", ([g.notes for g in p2["groups"]], p2["trains"]), ([["PB3", "PB6", "PB7", "PB12"]], 1))
check("points include the lander", round(p2["points"], 6), round(one, 6))
small = pw.group_cost("sonnet", 1, RULES) + pw.lander_cost(RULES)
p3 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=small + 0.01)
check("a group that does not fit is skipped, a smaller later one taken", [g.notes for g in p3["groups"]], [["PB4"]])
check("successor never without predecessor", any(g.after for g in p3["groups"]), False)
p4 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100,
             max_groups=2)
check("max groups", len(p4["groups"]), 2)
check("trains", p["trains"], -(-len(p["groups"]) // RULES["wave"]["train_size"]))

# 6b. A SPLIT REPORT ON AN UNLANDED BRANCH IS A FINISHER, NOT "AWAITING LANDING" (kb/Work PB2575): a `land` unit lands
#     only DONE branches, so holding it stranded the work (waves 1020 D and 1032 A). Its pred cherry-picks the branch.
#     A DONE report that names a note NOT started still waits for its landing. Own reports, so check 8's golden holds.
SPLIT_REPORTS = TMP / "reports-split"
SPLIT_REPORTS.mkdir()
(SPLIT_REPORTS / "w1020d-PB5-report.md").write_text(
    "SPLIT\n# PB5 group\n**Status:** SPLIT at the turn cap · **worktree:** `E:\\COBOL\\.claude\\worktrees\\wf_ggg-4` · "
    "**branch:** `worktree-wf_ggg-4` · **HEAD:** `3333333cc`\n\n## Reproduced?\n", encoding="utf-8")
(SPLIT_REPORTS / "w1020e-PB7-report.md").write_text(
    "DONE\n# PB7 group (PB12 NOT started)\n**branch:** `worktree-wf_hhh-5` · **HEAD:** `4444444dd`\n\n## Reproduced?\n",
    encoding="utf-8")
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

# 6c. A NOTE AN UNREPORTED DISPATCH HOLDS IS NOT PLANNED AGAIN, IN EITHER LANE (kb/Work PB2806): the fix lane read no
#     ledger, and wave 1044 re-planned seven of the eight groups another session's recorded, unlaunched wave 1045 held.
#     A report written since the dispatch answers it (steps 1 and 2 govern), a terminal note is never held, and a hand
#     entry expires after HAND_TTL_SECONDS.
HD = TMP / "held"
HD_REPORTS = HD / "reports"
HD_REPORTS.mkdir(parents=True)
pw.record_dispatch(HD, "1045", [pw.Group(kind="cluster", notes=["PB5", "PB2"], file="", files=["src/X.cs"], letter="a"),
                                pw.Group(kind="cluster", notes=["PB8"], file="", files=["src/Y.cs"], letter="b"),
                                pw.Group(kind="cluster", notes=["PB9"], file="", files=["src/Z.cs"], letter="c")],
                   lambda i: False)
HD_AT = {g["letter"]: g["at"] for g in coord.read_json(HD / pw.DISPATCH_LEDGER, {})["groups"]}
for name, nid, when in (("w1045b-PB8-report.md", "PB8", HD_AT["b"] + 60), ("w1031c-PB9-report.md", "PB9", HD_AT["c"] - 3600)):
    (HD_REPORTS / name).write_text(f"DONE\n# {nid} group\n**branch:** `worktree-wf_held-{nid}` · **HEAD:** `5555555ee`\n\n"
                                   "## Reproduced?\n", encoding="utf-8")
    __import__("os").utime(HD_REPORTS / name, (when, when))
LEDGER_DOC = coord.read_json(HD / pw.DISPATCH_LEDGER, {})
LEDGER_DOC["groups"].append({"wave": "hand", "letter": "", "notes": ["PB10"], "files": ["src/W.cs"], "hand": True,
                             "at": HD_AT["a"] - pw.HAND_TTL_SECONDS - 60})
coord.write_json(HD / pw.DISPATCH_LEDGER, LEDGER_DOC)
check("held: unanswered groups only, terminal notes and expired hand entries never",
      pw.held_by_dispatch(HD, pw.load_reports(HD_REPORTS), lambda i: i == "PB2", now=HD_AT["a"] + 120),
      {"PB5": "dispatch w1045a", "PB9": "dispatch w1045c"})
p6 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100,
             held={"PB5": "dispatch w1045a"})
check("a held note is planned in no group and waits naming its dispatch",
      (any("PB5" in g.notes for g in p6["groups"]), "w1045a" in p6["waiting"].get("PB5", "")), (False, True))
check("its cluster's other notes are still planned", any("PB2" in g.notes for g in p6["groups"]), True)

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
spec_file.write_text(specs[0] if specs else "", encoding="utf-8")
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
                                          "groups": gjson}), encoding="utf-8")
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
            "awaiting_landing": pl["awaiting_landing"], "points": round(pl["points"], 9), "trains": pl["trains"],
            "unlanded_without_note": pl["unlanded_without_note"]}


JSON = __import__("json")
fp = {"full": fingerprint(p, {j["letter"]: j for j in gjson}), "tight": fingerprint(p2, {}),
      "small": fingerprint(p3, {}), "max2": fingerprint(p4, {})}
# the fixture's temporary directory is the one varying input: it is spelled <TMP> in the golden, and every path
# separator as `/`, so the same golden holds on Windows and Linux
fp_text = (JSON.dumps(fp, indent=1, ensure_ascii=False).replace(JSON.dumps(str(TMP))[1:-1], "<TMP>")
           .replace("\\\\", "/") + "\n")
if "--write-golden" in sys.argv:
    GOLDEN.write_text(fp_text, encoding="utf-8")
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
    (TMP / "campaign" / rel).write_text("// fixture\n", encoding="utf-8")


def cnote(nid, cluster=("PB901",), blocked_by=(), body="", status="open", kind="defect"):
    bb = ", ".join(blocked_by)
    (CW / f"{nid}.md").write_text(
        f'---\ntitle: "{nid} — campaign step"\nid: {nid}\nkind: {kind}\nstatus: {status}\narea: build/ci\n'
        f"wrong_answer: false\nprocess_only: true\nblocked: {'true' if blocked_by and status != 'landed' else 'false'}\n"
        f"blocked_by: [{bb}]\ncluster: [{', '.join(cluster)}]\n---\n\n# {nid}\n\n{body}\n", encoding="utf-8")


cnote("PB901", body="Sever `src/Lib/Alpha.cs` from the old engine.")
cnote("PB902", body="Rewrite scripts/gamma.py's legacy legs.")
cnote("PB903", blocked_by=("PB901",), body="Then delete `src/Lib/Beta.cs`.")
cnote("PB904", blocked_by=("PB901", "PB902"), body="Then rename tests/Delta.Tests/DeltaTests.cs.")
cnote("PB905", blocked_by=("PB903",), body="Then sweep the docs; no code site named.")
cnote("PB906", body="Author = Mythos 5.1, only after the owner's explicit\napproval for this dispatch.", kind="analysis")
cnote("PB907", cluster=("OTHER",), body="Another campaign: `src/Lib/Alpha.cs`.")
cnote("PB908", status="landed", body="Landed already: `src/Lib/Alpha.cs`.")
cnote("PB909", status="owner", body="Waits for the owner.")
cnote("PB910", cluster=("PB901", "OTHER"), body="In two clusters: `scripts/gamma.py`.")
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
    copen, chalf, cdeps, cwait = pw.campaign_clusters(view, cnotes, ctexts, RULES, fc, TMP / "campaign")
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
    cfile.write_text(JSON.dumps({"wave": "78", "scratch": "X", "base": "abc", "groups": cg}), encoding="utf-8")
    check("campaign groups pass check_practices' successor rule", check_practices.same_file_without_successor(cfile), [])
    for g in cg:
        mds.render(dict(CFG77, wave="78"), g)
    only_b = pw.group_cost("opus", 1, RULES) + pw.lander_cost(RULES)
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
             budget_points=100, deps={"PB13": ["PB5"]}, busy={"worktree-train-a": {F_BIND, "src/Other.cs"}})
check("a colliding group waits, the collision named",
      "file-set collision with worktree-train-a (" + F_BIND + ")" in bp["waiting"].get("PB5", ""), True)
check("its successor waits for it", "PB5 is not planned" in bp["waiting"].get("PB13", ""), True)
check("a group that meets nothing runs", [g.notes for g in bp["groups"]], [["PB9"]])
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

# 9e. the group's file set (PB2118 Draft 8, the seventh refuter's J2 and J3): every site a note names WHATEVER its
#     score (Draft 7 kept fix_clusters' >= 2 and dropped a bare type name: "GammaState's" never added GammaState.cs),
#     plus the callers the member index computes for the note (an injected function here; member_index.py's own
#     self-test drives the computation); and a HAND dispatch is in flight from the moment the guard admits it, not
#     from its first edit (record_hand_dispatch), until its notes land or its entry expires.
if fc:
    C2 = TMP / "campaign2"
    for rel in ("src/Lib/Alpha.cs", "src/Lib/GammaState.cs", "src/Lib/Beta.cs", "src/Lib/Caller.cs"):
        (C2 / rel).parent.mkdir(parents=True, exist_ok=True)
        (C2 / rel).write_text("// fixture" + NL, encoding="utf-8")
    W2 = C2 / "kb" / "Work"
    W2.mkdir(parents=True)
    for nid, body in (("PB950", "Fold GammaState's flags into `src/Lib/Alpha.cs`."),
                      ("PB951", "Move `src/Lib/Beta.cs`'s members (callers computed).")):
        (W2 / f"{nid}.md").write_text(
            f'---{NL}title: "{nid} — step"{NL}id: {nid}{NL}kind: analysis{NL}status: open{NL}area: architecture{NL}'
            f"wrong_answer: false{NL}process_only: true{NL}blocked: false{NL}blocked_by: []{NL}cluster: [PB950]{NL}---"
            f"{NL}{NL}# {nid}{NL}{NL}{body}{NL}", encoding="utf-8")
    items2 = [dict(work.parse_frontmatter(q.read_text(encoding="utf-8")), _file=q.name) for q in sorted(W2.glob("*.md"))]
    view2 = work.cluster_order(items2, "PB950")
    notes2 = pw.load_notes(W2, {"wrong_answer": 8})
    texts2 = {i: (W2 / f"{i}.md").read_text(encoding="utf-8") for i in notes2}
    o2, _, _, _ = pw.campaign_clusters(view2, notes2, texts2, RULES, fc, C2,
                                       callers=lambda text: {"src/Lib/Caller.cs"} if "PB951" in text else set())
    files2 = {n["id"]: c["files"] for c in o2 for n in c["notes"]}
    check("a bare type name (score 1) is in its group's file set", "src/Lib/GammaState.cs" in files2["PB950"], True)
    check("a note's computed callers are in its group's file set", "src/Lib/Caller.cs" in files2["PB951"], True)
    check("callers never steer the clustering (the primary file is a named site)",
          sorted(c["file"] for c in o2), ["src/Lib/Alpha.cs", "src/Lib/Beta.cs"])
    check("the collision check sees a caller", pw.file_set_collisions(files2["PB951"], {"train t": {"src/Lib/Caller.cs"}}),
          ["train t (src/Lib/Caller.cs)"])
HL = TMP / "coord-hand"
HL.mkdir()
pw.record_hand_dispatch(HL, "general-purpose: R1 author", ["PB960"], {"src/Hand.cs"}, terminal=lambda i: False, now=1000.0)
hand = pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: False, with_git=False, now=1000.0 + 60)
check("a hand dispatch is in flight before its agent edits a file", {f for fs in hand.values() for f in fs},
      {"src/Hand.cs"})
check("a hand entry expires after HAND_TTL_SECONDS",
      pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: False, with_git=False,
                            now=1000.0 + pw.HAND_TTL_SECONDS + 1), {})
check("a hand entry whose notes landed is not in flight",
      pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: True, with_git=False, now=1000.0 + 60), {})
# 9f. (PB2118 Draft 9) the landing check orders two branches by dispatch time: the in-flight enumeration reports each
#     dispatch entry's notes and time through `meta`; and the planner never plans on a stale member index
META: dict = {}
pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: False, with_git=False, now=1000.0 + 60, meta=META)
check("meta carries a dispatch entry's notes and time", [(m["notes"], m["at"]) for m in META.values()],
      [(["PB960"], 1000.0)])
STALE = TMP / "stale-repo"
(STALE / "src").mkdir(parents=True)
(STALE / "src" / "A.cs").write_text("// a" + NL, encoding="utf-8")
for args in (["init", "-q"], ["add", "-A"], ["-c", "user.name=t", "-c", "user.email=t@t", "commit", "-qm", "c"]):
    subprocess.run(["git", *args], cwd=STALE, check=True, capture_output=True)
try:
    pw.member_callers(STALE, TMP / "no-cache", regenerate=False)
    check("the planner refuses a tree no member index describes", "planned", "refused")
except SystemExit:
    check("the planner refuses a tree no member index describes", "refused", "refused")

# 9g. (PB2118 Draft 10, the ninth refuter) every changed-file read passes --no-renames (the fake git above refuses a
#     diff without it, so 9d already holds the enumeration to it); one wave's groups are recorded strictly apart in
#     letter order, so no two tie (N4); and an aborted dispatch is released by wave, group or note, never by a clock (N5)
RL = TMP / "release"
RL.mkdir()
pw.record_dispatch(RL, "1050", [pw.Group(kind="cluster", notes=["PB80"], file="", files=["src/X.cs"], letter="a"),
                                pw.Group(kind="cluster", notes=["PB81"], file="", files=["src/Y.cs"], letter="b"),
                                pw.Group(kind="cluster", notes=["PB82"], file="", files=["src/Z.cs"], letter="c")],
                   terminal=lambda i: False)
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
    "", "## 13. Next", ""]), encoding="utf-8")
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
    (SR / rel).write_text(text, encoding="utf-8")
# a build output and a stray file name the same type: the slice set reads the COMMITTED tree (PB1957, 2026-10-07)
(SR / "src" / "B" / "Generated").mkdir(parents=True)
(SR / "src" / "B" / "Generated" / "SessionParser.cs").write_text("class Session { }", encoding="utf-8")
subprocess.run(["git", "-C", str(SR), "init", "-q"], check=True)
subprocess.run(["git", "-C", str(SR), "add", "-A", "--", ":!src/B/Generated"], check=True)
(SR / "src" / "B" / "Stray.cs").write_text("class GroupRepo { }", encoding="utf-8")
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
    (tdir / "DEVLOG.md").write_text("## Entry 2 — Train 1036 (wave 1032: D, E)" + NL + "## Entry 1 — wave 1034 dispatched" + NL,
                                     encoding="utf-8")
    check("next_wave counts a train label above every wave number", pw.next_wave(tdir, []), 1037)
    # PB2806: a wave recorded in the dispatch ledger with no report yet (another session's) is a taken number too
    pw.record_dispatch(tdir, "1045", [pw.Group(kind="cluster", notes=["PB5"], file="", files=["src/X.cs"], letter="a")],
                       lambda i: False)
    check("next_wave counts a dispatched wave with no report", pw.next_wave(tdir, [], tdir), 1046)

for f in fails:
    print("FAIL:", f)
print(f"plan_wave self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
sys.exit(1 if fails else 0)
