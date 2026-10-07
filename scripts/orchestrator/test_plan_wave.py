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
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import coord  # noqa: E402
import plan_wave as pw  # noqa: E402

sys.path.insert(0, str(coord.REPO / ".claude" / "skills" / "workstream"))
import check_practices  # noqa: E402

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

# 7. the group JSON renders through the real template and passes check_practices' same-file rule
tpl = (coord.REPO / ".claude/skills/workstream/templates/dispatch-spec-implementer.md").read_text(encoding="utf-8")
gjson = [pw.as_group_json(g, "77", notes, "COBOLNET0001-COBOLNET0003") for g in p["groups"]]
try:
    for g in gjson:
        tpl.format(wave="77", base="abc", S="X", pred=g.get("pred", ""), **{k: v for k, v in g.items() if k != "pred"})
    rendered = True
except (KeyError, IndexError) as e:
    rendered = f"template placeholder missing: {e}"
check("renders through the template", rendered, True)
check("lead is the note with most rows", next(j["lead"] for j in gjson if j["letter"] == G[("PB3", "PB6", "PB7", "PB12")].letter), "PB6")
gfile = TMP / "groups.json"
gfile.write_text(__import__("json").dumps({"wave": "77", "scratch": "X", "base": "abc", "groups": gjson}), encoding="utf-8")
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
        tpl.format(wave="78", base="abc", S="X", pred=g.get("pred", ""), **{k: v for k, v in g.items() if k != "pred"})
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

for f in fails:
    print("FAIL:", f)
print(f"plan_wave self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
sys.exit(1 if fails else 0)
