#!/usr/bin/env python3
"""T2 (local-model spike): can a local chat model read an implementer REPORT FILE and reproduce the facts the implementer
itself returned as structured fields (status, notes landed, leads)? Ground truth = the workflow journal's result dicts.

    python t2_digest.py <model> [--max N]
Output: scratchpad only (t2-<model>.json); nothing touches the repo.
"""
import glob, json, pathlib, re, sys, time, urllib.request, urllib.error

OUT = pathlib.Path(__file__).resolve().parent
JOURNALS = glob.glob(r"C:/Users/brent/.claude/projects/E--COBOL/5ab816c6-b0b3-41a8-b8b1-b35036522a97/subagents/workflows/*/journal.jsonl")
PB = re.compile(r"PB\d+")
SITE = re.compile(r"[A-Za-z0-9_]+\.(?:cs|g4|py|md)")
SCHEMA = {
    "type": "object",
    "properties": {
        "status": {"type": "string", "enum": ["DONE", "SPLIT", "DISCHARGED", "BLOCKED"]},
        "notes_landed": {"type": "array", "items": {"type": "string"}},
        "leads": {"type": "array", "items": {"type": "string"}},
    },
    "required": ["status", "notes_landed", "leads"],
}
PROMPT = (
    "You read one implementer report for a COBOL compiler fix. Return JSON with exactly these fields:\n"
    "- status: DONE if every note in the group is landed, SPLIT if the implementer stopped at a note boundary with notes left, "
    "DISCHARGED if the notes no longer reproduced, BLOCKED if it could not proceed.\n"
    "- notes_landed: the kb/Work note ids (PB followed by digits) the report says are LANDED or fixed, not the ones left open.\n"
    "- leads: every follow-up issue the report files or names for a later agent (open notes, new defects, things not done), one short string each "
    "that keeps the code site (file name) if the report gives one.\n"
    "Use only facts in the report.\n\nREPORT:\n"
)


def truth():
    seen, rows = set(), []
    for j in JOURNALS:
        for line in open(j, encoding="utf-8"):
            d = json.loads(line)
            r = d.get("result")
            if d.get("type") != "result" or not isinstance(r, dict) or "report" not in r:
                continue
            p = pathlib.Path(r["report"])
            if not p.exists() or str(p) in seen:
                continue
            seen.add(str(p))
            landed = sorted({x for s in r.get("notes_landed", []) for x in PB.findall(s)})
            sites = [m for ld in r.get("leads", []) for m in SITE.findall(ld)[:1]]
            rows.append({"report": str(p), "status": r["status"], "landed": landed, "lead_sites": sites, "n_leads": len(r.get("leads", []))})
    return rows


def chat(model, text):
    body = {"model": model, "messages": [{"role": "user", "content": PROMPT + text[:14000]}], "stream": False,
            "format": SCHEMA, "think": False, "keep_alive": "5m", "options": {"temperature": 0, "num_ctx": 16384}}
    for attempt in (0, 1):
        try:
            r = urllib.request.urlopen(urllib.request.Request("http://localhost:11434/api/chat", json.dumps(body).encode(), {"Content-Type": "application/json"}), timeout=420)
            content = json.loads(r.read())["message"]["content"]
            # muse-glimmer's Ollama build leaks its end-of-turn token into the content ('<|eot|>'); strip it for every model alike.
            return json.loads(content.replace("<|eot|>", "").strip())
        except urllib.error.HTTPError as e:
            msg = e.read()[:200]
            if attempt == 0 and b"think" in msg:
                body.pop("think"); continue
            raise RuntimeError(f"HTTP {e.code} {msg}")
        except json.JSONDecodeError:
            return {"status": "?", "notes_landed": [], "leads": []}


def main():
    model = sys.argv[1]
    mx = int(sys.argv[sys.argv.index("--max") + 1]) if "--max" in sys.argv else 99
    rows = truth()[:mx]
    print(f"{len(rows)} reports with ground truth", flush=True)
    st_ok = 0; f1s = []; recalls = []; per = []; t0 = time.time()
    for row in rows:
        text = pathlib.Path(row["report"]).read_text(encoding="utf-8", errors="replace")
        t1 = time.time()
        try:
            out = chat(model, text)
        except Exception as e:
            per.append({"report": row["report"], "error": str(e)[:200]}); print("ERR", str(e)[:120], flush=True); continue
        got = set(PB.findall(" ".join(out.get("notes_landed", []))))
        exp = set(row["landed"])
        tp = len(got & exp)
        p = tp / len(got) if got else (1.0 if not exp else 0.0)
        r = tp / len(exp) if exp else (1.0 if not got else 0.0)
        f1s.append(0 if p + r == 0 else 2 * p * r / (p + r))
        st_ok += out.get("status") == row["status"]
        joined = " ".join(out.get("leads", []))
        if row["lead_sites"]:
            recalls.append(sum(s in joined for s in row["lead_sites"]) / len(row["lead_sites"]))
        per.append({"report": row["report"], "status": [row["status"], out.get("status")], "landed": [sorted(exp), sorted(got)],
                    "n_leads": [row["n_leads"], len(out.get("leads", []))], "sec": round(time.time() - t1, 1)})
    n = len(f1s)
    res = {"model": model, "reports": len(rows), "scored": n, "status_exact": round(st_ok / max(n, 1), 3),
           "landed_f1": round(sum(f1s) / max(n, 1), 3), "lead_site_recall": round(sum(recalls) / max(len(recalls), 1), 3),
           "sec_per_report": round((time.time() - t0) / max(len(rows), 1), 1)}
    (OUT / f"t2-{model.replace(':', '_')}.json").write_text(json.dumps({"summary": res, "per": per}, indent=1), encoding="utf-8")
    print(json.dumps(res))


if __name__ == "__main__":
    main()
