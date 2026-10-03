#!/usr/bin/env python3
"""T1 (local-model spike): does embedding retrieval find the governing ISO clause of a kb/Work note
better than a lexical BM25 baseline over the same clause chunks?

Ground truth = each note's own `spec_refs` frontmatter. Output only: scratchpad results; nothing is written into the repo.

    python t1_retrieval.py <model> [--limit N]      model: bm25 | qwen3-embedding:4b | embeddinggemma
"""
import json, math, pathlib, re, sys, time, urllib.request
import numpy as np

REPO = pathlib.Path(r"E:\COBOL")
OUT = pathlib.Path(__file__).resolve().parent
SPEC = REPO / "specs" / "ISO_COBOL.md"
HEAD = re.compile(r"^#{2,6} ((?:\d+|[A-Z])(?:\.\d+)+|\d+) (.*)$")
MAXCH = 2400
KS = (1, 3, 5, 10, 20)


def chunks():
    cur_id, cur_title, buf, out = None, "", [], []
    def flush():
        if cur_id and buf:
            text = "\n".join(buf).strip()
            for i in range(0, max(len(text), 1), MAXCH):
                out.append((cur_id, cur_title, text[i:i + MAXCH]))
    for line in SPEC.read_text(encoding="utf-8").splitlines():
        m = HEAD.match(line)
        if m:
            flush(); cur_id, cur_title, buf = m.group(1), m.group(2), []
        elif not line.startswith("<a id="):
            buf.append(line)
    flush()
    return out


def notes():
    out = []
    for p in sorted((REPO / "kb" / "Work").glob("PB*.md")):
        t = p.read_text(encoding="utf-8")
        m = re.match(r"---\n(.*?)\n---", t, re.S)
        if not m:
            continue
        fm = m.group(1)
        refs = re.search(r"^spec_refs:\s*\[(.*?)\]", fm, re.M)
        title = re.search(r'^title:\s*"(.*)"\s*$', fm, re.M)
        if not refs or not title:
            continue
        ids = [x.strip().strip('"') for x in refs.group(1).split(",") if x.strip().strip('"')]
        ids = [re.sub(r"[^\d.A-Z]", "", i) for i in ids]
        ids = [i for i in ids if i]
        if ids:
            q = re.sub(r"^PB\d+\s*[—-]\s*", "", title.group(1))[:500]
            out.append((p.stem, q, ids))
    return out


def related(a, b):
    return a == b or a.startswith(b + ".") or b.startswith(a + ".")


def tok(s):
    return re.findall(r"[a-z0-9][a-z0-9\-]+", s.lower())


class BM25:
    def __init__(self, docs, k1=1.5, b=0.75):
        self.tf = [{} for _ in docs]; self.dl = []; df = {}
        for i, d in enumerate(docs):
            ts = tok(d); self.dl.append(len(ts))
            for t in ts:
                self.tf[i][t] = self.tf[i].get(t, 0) + 1
            for t in set(ts):
                df[t] = df.get(t, 0) + 1
        n = len(docs); self.avg = sum(self.dl) / n; self.k1, self.b = k1, b
        self.idf = {t: math.log(1 + (n - c + .5) / (c + .5)) for t, c in df.items()}
    def scores(self, q):
        s = np.zeros(len(self.tf))
        for t in set(tok(q)):
            if t not in self.idf: continue
            for i, tf in enumerate(self.tf):
                f = tf.get(t)
                if f:
                    s[i] += self.idf[t] * f * (self.k1 + 1) / (f + self.k1 * (1 - self.b + self.b * self.dl[i] / self.avg))
        return s


def _post(model, batch, keep):
    body = json.dumps({"model": model, "input": batch, "keep_alive": keep, "truncate": True}).encode()
    r = urllib.request.urlopen(urllib.request.Request("http://localhost:11434/api/embed", body, {"Content-Type": "application/json"}), timeout=600)
    return json.loads(r.read())["embeddings"]


def embed(model, texts, keep="2m"):
    out = []
    for i in range(0, len(texts), 16):
        batch = [t if t.strip() else "(empty)" for t in texts[i:i + 16]]
        try:
            out += _post(model, batch, keep)
        except urllib.error.HTTPError as e:
            print("HTTP", e.code, e.read()[:300], "-> retrying one by one", flush=True)
            for t in batch:
                try:
                    out += _post(model, [t], keep)
                except urllib.error.HTTPError as e2:
                    print("  single failed", e2.code, e2.read()[:200], len(t), flush=True)
                    out += _post(model, [t[:600]], keep)
    a = np.array(out, dtype=np.float32)
    return a / np.linalg.norm(a, axis=1, keepdims=True)


def fmt_doc(model, cid, title, text):
    if model.startswith("embeddinggemma"):
        return f"title: {cid} {title} | text: {text}"
    return f"{cid} {title}\n{text}"


def fmt_query(model, q):
    if model.startswith("embeddinggemma"):
        return f"task: search result | query: {q}"
    return ("Instruct: Given a description of a COBOL compiler defect, retrieve the ISO/IEC 1989 clause that governs it\nQuery: " + q)


def main():
    model = sys.argv[1]
    limit = int(sys.argv[sys.argv.index("--limit") + 1]) if "--limit" in sys.argv else 0
    ch = chunks(); ns = notes()
    if limit: ns = ns[:limit]
    print(f"{len(ch)} chunks, {len(ns)} notes with spec_refs", flush=True)
    t0 = time.time()
    if model == "bm25":
        bm = BM25([f"{c} {t} {x}" for c, t, x in ch])
        score = lambda q: bm.scores(q)
    elif model.startswith("rrf:"):
        # reciprocal rank fusion of BM25 and one embedding model (cache already built by a plain run of that model)
        base = model[4:]
        bm = BM25([f"{c} {t} {x}" for c, t, x in ch])
        D = np.load(OUT / f"emb-{base.replace(':', '_')}.npy")
        Q = embed(base, [fmt_query(base, q) for _, q, _ in ns])
        qi_of = {q: i for i, (_, q, _) in enumerate(ns)}
        def rank_of(s):
            r = np.empty(len(s)); r[np.argsort(-s)] = np.arange(len(s)); return r
        def score(q):
            return 1.0 / (60 + rank_of(bm.scores(q))) + 1.0 / (60 + rank_of(D @ Q[qi_of[q]]))
    else:
        cache = OUT / f"emb-{model.replace(':', '_')}.npy"
        if cache.exists() and len(np.load(cache)) == len(ch):
            D = np.load(cache)
        else:
            D = embed(model, [fmt_doc(model, *c) for c in ch]); np.save(cache, D)
        Q = embed(model, [fmt_query(model, q) for _, q, _ in ns])
        score = None
    t_index = time.time() - t0
    hit = {k: 0 for k in KS}; cov = {k: 0.0 for k in KS}; rr = 0.0; per = []
    for qi, (nid, q, ids) in enumerate(ns):
        s = score(q) if score else D @ Q[qi]
        order = np.argsort(-s)
        first = None; ranked = []
        for pos, ci in enumerate(order[:max(KS)]):
            ranked.append(ch[ci][0])
        for k in KS:
            top = ranked[:k]
            got = [e for e in ids if any(related(e, r) for r in top)]
            if got: hit[k] += 1
            cov[k] += len(got) / len(ids)
        for pos, r in enumerate(ranked):
            if any(related(e, r) for e in ids):
                rr += 1 / (pos + 1); break
        per.append({"note": nid, "expected": ids, "top5": ranked[:5]})
    n = len(ns)
    res = {"model": model, "notes": n, "chunks": len(ch), "index_s": round(t_index, 1), "total_s": round(time.time() - t0, 1),
           "any_hit@k": {k: round(hit[k] / n, 3) for k in KS}, "mean_coverage@k": {k: round(cov[k] / n, 3) for k in KS}, "mrr": round(rr / n, 3)}
    (OUT / f"t1-{model.replace(':', '_')}.json").write_text(json.dumps({"summary": res, "per_note": per}, indent=1), encoding="utf-8")
    print(json.dumps(res))


if __name__ == "__main__":
    main()
