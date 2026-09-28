"""Stamp A/B at scale — scoring and statistics.

    python analyze.py <run transcript dir> <truth.json> <out.json>

truth.json: {"<scenario id>": {"branch": ..., "current": [], "stale": [full shas of uncovered commits]}}
Correct = the agent's uncovered-commit set equals the truth set (7-char prefix match; CURRENT truth is empty).
Statistics are PAIRED BY SCENARIO: each scenario's arm mean is one observation, so replicates inside a scenario do
not inflate n. Wilcoxon signed-rank (exact) on the per-scenario differences; 95 % bootstrap CI (10 000 resamples of
scenarios) on the ratio mean(NEW)/mean(OLD); Fisher's exact test on accuracy.
"""
import json, pathlib, re, sys, random, statistics as st
sys.path.insert(0, r"E:\COBOL\scripts\telemetry")
from workflow_metrics import agent_metrics
from scipy import stats

run_dir, truth_path, out_path = map(pathlib.Path, sys.argv[1:4])
truth = json.loads(truth_path.read_text(encoding="utf-8"))
labels, results = {}, {}
for line in (run_dir / "journal.jsonl").read_text(encoding="utf-8").splitlines():
    o = json.loads(line)
    if o.get("type") == "started": labels[o["agentId"]] = o["label"]
    if o.get("type") == "result": results[o["agentId"]] = o["result"]

LBL = re.compile(r"^ab2-(?P<sid>.+)-(?P<variant>current|stale)-(?P<arm>OLD|NEW)-(?P<rep>\d+)$")
by_suffix = {sid[-6:]: sid for sid in truth}
rows = []
for aid, lab in labels.items():
    m = LBL.match(lab or "")
    if not m: continue
    sid = by_suffix[m["sid"]]
    r = results.get(aid)
    met = agent_metrics(run_dir / f"agent-{aid}.jsonl")
    want = {s[:7] for s in truth[sid][m["variant"]]}
    got = set()
    if r:
        for c in r.get("uncovered_commits", []):
            h = re.search(r"[0-9a-f]{7,40}", c.get("sha", ""))
            if h: got.add(h.group(0)[:7])
    rows.append({"agent": aid, "label": lab, "scenario": sid, "variant": m["variant"], "arm": m["arm"], "rep": int(m["rep"]),
                 "returned": r is not None, "correct": r is not None and got == want, "missed": sorted(want - got),
                 "extra": sorted(got - want), **{k: met[k] for k in ("turns", "tool_calls", "tokens", "fresh", "wall_s")},
                 "commits_read": (r or {}).get("commits_read"), "result": r})


def paired(sub, key):
    keys = sorted({(x["scenario"], x["variant"]) for x in sub})
    old, new = [], []
    for k in keys:
        o = [x[key] for x in sub if (x["scenario"], x["variant"]) == k and x["arm"] == "OLD" and x["returned"]]
        n = [x[key] for x in sub if (x["scenario"], x["variant"]) == k and x["arm"] == "NEW" and x["returned"]]
        if o and n: old.append(st.mean(o)); new.append(st.mean(n))
    if len(old) < 2: return None
    diffs = [b - a for a, b in zip(old, new)]
    w = stats.wilcoxon(new, old, method="exact" if len(old) <= 25 else "auto") if any(diffs) else None
    rnd = random.Random(20260928); idx = range(len(old)); boots = []
    for _ in range(10000):
        s = [rnd.choice(idx) for _ in idx]
        so = sum(old[i] for i in s)
        if so: boots.append(sum(new[i] for i in s) / so)
    boots.sort()
    return {"pairs": len(old), "old_mean": round(st.mean(old), 2), "new_mean": round(st.mean(new), 2),
            "ratio": round(st.mean(new) / st.mean(old), 3), "ci95": [round(boots[250], 3), round(boots[9749], 3)],
            "new_lower_in": sum(d < 0 for d in diffs), "wilcoxon_p": None if w is None else float(f"{w.pvalue:.3g}")}


def accuracy(sub):
    out = {}
    for arm in ("OLD", "NEW"):
        a = [x for x in sub if x["arm"] == arm]
        out[arm] = f'{sum(x["correct"] for x in a)}/{len(a)}'
    o = [x for x in sub if x["arm"] == "OLD"]; n = [x for x in sub if x["arm"] == "NEW"]
    table = [[sum(x["correct"] for x in n), sum(not x["correct"] for x in n)], [sum(x["correct"] for x in o), sum(not x["correct"] for x in o)]]
    out["fisher_p"] = float(f"{stats.fisher_exact(table).pvalue:.3g}")
    return out


summary = {}
for name, sub in (("ALL", rows), ("STALE", [x for x in rows if x["variant"] == "stale"]), ("CURRENT", [x for x in rows if x["variant"] == "current"])):
    summary[name] = {"agents": len(sub), "accuracy": accuracy(sub),
                     **{k: paired(sub, k) for k in ("turns", "tool_calls", "tokens", "wall_s")}}
out = {"summary": summary, "rows": rows}
out_path.write_text(json.dumps(out, indent=1, ensure_ascii=False), encoding="utf-8")
sys.stdout.reconfigure(encoding="utf-8")
print(json.dumps(summary, indent=1))
