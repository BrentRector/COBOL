#!/usr/bin/env python3
"""T3 (local-model spike): lead-to-note matching, measured as 'does the nearest-note search reproduce the registrar's grouping?'

Ground truth: a kb/Work note's own `cluster` frontmatter (the registrar grouped notes by root cause). For every note with a
non-empty cluster, rank all OTHER notes by similarity of title text; a hit means a clustered note is in the top k.
Methods: bm25, embeddinggemma, qwen3-embedding:4b, rrf of bm25 with each. Output: scratchpad only.
"""
import json, pathlib, re, sys, time
import numpy as np
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from t1_retrieval import BM25, embed, REPO, OUT

KS = (1, 3, 5, 10)


def load():
    out = []
    for p in sorted((REPO / "kb" / "Work").glob("PB*.md")):
        t = p.read_text(encoding="utf-8")
        m = re.match(r"---\n(.*?)\n---", t, re.S)
        if not m:
            continue
        fm = m.group(1)
        title = re.search(r'^title:\s*"(.*)"\s*$', fm, re.M)
        cl = re.search(r"^cluster:\s*\[(.*?)\]", fm, re.M)
        if not title:
            continue
        ids = [x.strip().strip('"') for x in (cl.group(1).split(",") if cl else []) if x.strip().strip('"')]
        out.append((p.stem, re.sub(r"^PB\d+\s*[—-]\s*", "", title.group(1))[:500], ids))
    return out


def rank_of(s):
    r = np.empty(len(s)); r[np.argsort(-s)] = np.arange(len(s)); return r


def main():
    notes = load(); n = len(notes)
    idx = {nid: i for i, (nid, _, _) in enumerate(notes)}
    texts = [t for _, t, _ in notes]
    truth = [{idx[c] for c in cl if c in idx and c != nid} for nid, _, cl in notes]
    eval_ids = [i for i in range(n) if truth[i]]
    print(f"{n} notes, {len(eval_ids)} with a usable cluster", flush=True)
    bm = BM25(texts)
    S = {}
    S["bm25"] = np.array([bm.scores(t) for t in texts])
    for model, fmt in (("embeddinggemma", lambda t: f"task: clustering | query: {t}"), ("qwen3-embedding:4b", lambda t: t)):
        cache = OUT / f"t3-emb-{model.replace(':', '_')}.npy"
        if cache.exists() and len(np.load(cache)) == n:
            E = np.load(cache)
        else:
            E = embed(model, [fmt(t) for t in texts]); np.save(cache, E)
        S[model] = E @ E.T
        S["rrf:" + model] = None
    for model in ("embeddinggemma", "qwen3-embedding:4b"):
        R = np.zeros((n, n))
        for i in range(n):
            R[i] = 1 / (60 + rank_of(S["bm25"][i])) + 1 / (60 + rank_of(S[model][i]))
        S["rrf:" + model] = R
    res = {}
    for name, M in S.items():
        hit = {k: 0 for k in KS}; prec = {k: 0.0 for k in KS}
        for i in eval_ids:
            s = M[i].copy(); s[i] = -1e9
            order = np.argsort(-s)
            for k in KS:
                got = [j for j in order[:k] if j in truth[i]]
                if got: hit[k] += 1
                prec[k] += len(got) / k
        m = len(eval_ids)
        res[name] = {"any_hit@k": {k: round(hit[k] / m, 3) for k in KS}, "precision@k": {k: round(prec[k] / m, 3) for k in KS}}
        print(name, json.dumps(res[name]), flush=True)
    (OUT / "t3-neighbors.json").write_text(json.dumps({"notes": n, "evaluated": len(eval_ids), "results": res}, indent=1), encoding="utf-8")


if __name__ == "__main__":
    main()
