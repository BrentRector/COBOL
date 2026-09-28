"""Combine validator + refuter results into final verdicts and append them to the learnings ledger."""
import collections, json, pathlib, re, sys
sys.stdout.reconfigure(encoding="utf-8")
V = pathlib.Path(__file__).parent
J = pathlib.Path(r"C:\Users\brent\.claude\projects\E--COBOL\73cac64c-391d-42c0-bcdf-c880e3e10305\subagents\workflows\wf_cf692111-b7b\journal.jsonl")
L = pathlib.Path(r"E:\COBOL\docs\rearchitecture\evidence\fleet-optimization\learnings-ledger")
labels, results = {}, {}
for line in J.read_text(encoding="utf-8").splitlines():
    o = json.loads(line)
    if o.get("type") == "started":
        labels[o["agentId"]] = o["label"]
    elif o.get("type") == "result":
        results[o["agentId"]] = o["result"]
val, ref = {}, {}
for aid, lab in labels.items():
    r = results.get(aid)
    if not isinstance(r, dict):
        continue
    job = lab.split("-", 1)[1]
    (val if lab.startswith("validate-") else ref)[job] = r.get("items", [])

themes = {t["slug"][:20]: t["title"] for t in json.loads((V / "themes.json").read_text(encoding="utf-8"))}
norm = lambda s: re.sub(r"\W+", " ", s.lower()).strip()
final, n = [], 0
for job in sorted(val):
    refs = {norm(i["title"]): i for i in ref.get(job, [])}
    for it in val[job]:
        n += 1
        cid = f"C{n}"
        verdict, refuter = it["verdict"], None
        if verdict == "VETTED":
            rf = refs.get(norm(it["title"]))
            if rf is None:  # fuzzy: prefix match
                rf = next((v for k, v in refs.items() if k[:40] == norm(it["title"])[:40]), None)
            refuter = rf
            if rf is None:
                verdict = "UNPROVEN"
            elif not rf.get("upheld"):
                verdict = rf.get("verdict", "UNPROVEN")
        theme = themes.get(job.rsplit("-", 1)[0][:20], themes.get(job[:20], job))
        final.append({"id": cid, "job": job, "theme": theme, "title": it["title"], "validator": it,
                      "refuter": refuter, "verdict": verdict})

c = collections.Counter(f["verdict"] for f in final)
print("candidates", len(final), dict(c))
print("validator VETTED", sum(f["validator"]["verdict"] == "VETTED" for f in final),
      "| overturned by refuter", sum(f["validator"]["verdict"] == "VETTED" and f["verdict"] != "VETTED" for f in final))
bytheme = collections.defaultdict(collections.Counter)
for f in final:
    bytheme[f["theme"]][f["verdict"]] += 1
for t, cc in bytheme.items():
    print(f"  {t[:45]:45} {dict(cc)}")
print("\nVETTED:")
for f in final:
    if f["verdict"] == "VETTED":
        print(" ", f["id"], f["title"][:110])
print("\nREFUTED:")
for f in final:
    if f["verdict"] == "REFUTED":
        why = (f["refuter"] or {}).get("reason") if f["refuter"] and not f["refuter"].get("upheld") else f["validator"].get("contradiction")
        print(" ", f["id"], f["title"][:80], "|", (why or "")[:160].replace("\n", " "))
(V / "final.json").write_text(json.dumps(final, indent=1, ensure_ascii=False), encoding="utf-8")

if "--write-ledger" in sys.argv:
    with open(L / "candidates.jsonl", "a", encoding="utf-8", newline="\n") as fc, \
         open(L / "verdicts.jsonl", "a", encoding="utf-8", newline="\n") as fv:
        for f in final:
            srcs = f["validator"].get("sources", [])
            entries = sorted({int(x) for s in srcs for x in re.findall(r"Entr(?:y|ies)\s+(\d{3,4})", s)})
            fc.write(json.dumps({"id": f["id"], "run": "consolidation-1", "theme": f["theme"], "title": f["title"],
                                 "source_entries": entries, "sources": srcs}, ensure_ascii=False) + "\n")
            fv.write(json.dumps({"id": f["id"], "run": "validation-1 (wf_cf692111-b7b)", "date": "2026-09-28",
                                 "verdict": f["verdict"], "validator": f["validator"], "refuter": f["refuter"]},
                                ensure_ascii=False) + "\n")
    print("ledger written")
