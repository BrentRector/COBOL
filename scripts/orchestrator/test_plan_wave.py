#!/usr/bin/env python3
"""Self-test for plan_wave.py on fabricated notes, clusters and reports: work awaiting landing is never re-planned,
finishers come first and name their predecessor, clusters rank by rows claimed, a second group on a file is a
same-file successor, the model routing rules, the budget fill (with landers and successors), and that the groups
render through the real dispatch-spec template and pass check_practices.py's same-file rule.
Run: python scripts/orchestrator/test_plan_wave.py   (no submodule, no build, no git)."""
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

for f in fails:
    print("FAIL:", f)
print(f"plan_wave self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
sys.exit(1 if fails else 0)
