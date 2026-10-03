#!/usr/bin/env python3
"""Loop clock: draw an orchestrated mission as one clock of loops, from its transcripts (kb/Work PB1912, experiment 2).

    python scripts/telemetry/loop_clock.py --orchestrator <session.jsonl> <workflow dir> [<workflow dir> ...] --out clock.html

The design follows Scott Ernst's "Loops in the loop" (https://scott-ernst.com/artifacts/loop-clock/view/). LEFT is one
clock. Position around it is MISSION PROGRESS, never wall time: every event (an agent starting or returning, a gate or CI
verdict, a refuter's overturn, a human turn, the end of an orchestrator episode) is sorted by time and takes the next
notch, so an idle night takes no room. From the outside in:

  petals        the human's turns; large and filled is a steering input (text over 40 characters), small and hollow is
                an approval. A turn is a `user` record of the orchestrator transcript with text that is not a system
                reminder, `[SYSTEM`, `<task-notification`, `<command-`, `Base directory` or `[Request interrupted`
                block and is not flagged `isMeta` (the harness's own injections: skill bodies, resumed-session
                prompts, relayed agent messages).
  coil          the orchestrator: the run of its tool calls between two human turns, one loop per 40 calls; bold when
                the episode ran `push-main` or a `git merge` or `git push` (it merged or shipped).
  rings         one per agent, outermost = earliest start; the arc is the agent, wound with its tool-call coil (one
                loop per 40 calls). Stroke weight is the model (heavy Opus, medium Sonnet, thin dashed other); colour is
                the role read from the label.
  marks         a green check is a pass (a gate or CI run that came green, a validator pass, a refuter that upheld
                everything); an orange dot is findings (a SPLIT return or a non-empty `leads` or `defects` list); a red
                curl is a rejection, which leaves the verdict, sweeps back under the work it reviewed and turns
                forward into the fix; consecutive rejections nest.

RIGHT is a grid of small multiples: one mini-clock per agent, its tool-call spiral (one loop per 40 calls) with its own
verdict marks placed at their tool-call index (`tool_ts` of `workflow_metrics.agent_metrics`, the one transcript reader).

Rejections come from: gate RED and CI `failure` events (`agent_metrics().events`); refuter verdicts with `upheld: false`
(the fixer of the same slug is the fix); a validator result whose `status` is not `pass`; a lander return that says
`Dropped from cluster X: PBnnn`.

Not drawn: the orchestrator's context size per turn, per-agent token cost, the wall time spent waiting on gate slots, and
which implementer cluster an agent's ring belongs to (every agent is its own ring). A lander that reads an
implementer's gate log carries that log's verdict lines too, because the reader cannot tell an echo from a run.
"""
import argparse, bisect, html, json, math, pathlib, re, sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from workflow_metrics import agent_metrics, journal, ts  # noqa: E402

SWEEP = 340.0          # degrees of the whole mission; the gap at the top keeps its start and its end apart
C = 400                # clock centre; the viewBox is 800 square
R_RIM, R_HUMAN, R_ORCH, R_FIRST, R_LAST = 392, 368, 340, 318, 120
LOOP_CALLS = 40
STEER_CHARS = 40
NOT_HUMAN = ("<system-reminder>", "[SYSTEM", "<task-notification", "<command-", "Base directory", "[Request interrupted")
SHIPPED = re.compile(r"push-main|git (?:merge|push)")
DROP = re.compile(r"Dropped from cluster (\w+): (PB\d+)")
ROLES = ("impl", "lander", "write", "refute", "fix", "validate", "survey")
KINDS = ("gate", "ci", "refute", "validator", "lander drop")   # the kinds of rejection


def role(label):
    l = (label or "").lower()
    return next((r for r in ROLES if l.startswith(r)), "other")


def weight(model):
    m = (model or "").lower()
    return 4.2 if "opus" in m else 2.6 if "sonnet" in m else 1.3


def short_model(model):
    return re.sub(r"^claude-|-\d+-\d+$", "", model or "?")


# ---------------------------------------------------------------------------------------------------------------- data

def orchestrator(path):
    """(human turns, episodes) of the orchestrator transcript. An episode is the run of its tool calls between two human turns."""
    turns, episodes, seen = [], [], set()
    ep = {"start": None, "end": None, "calls": 0, "shipped": False}

    def close():
        nonlocal ep
        if ep["calls"]:
            episodes.append(ep)
        ep = {"start": None, "end": None, "calls": 0, "shipped": False}

    for line in pathlib.Path(path).read_text(encoding="utf-8").splitlines():
        o = json.loads(line)
        t, m = o.get("timestamp"), o.get("message") or {}
        c = m.get("content")
        if o.get("type") == "user" and t and not o.get("isMeta") and not o.get("isSidechain"):
            texts = [c] if isinstance(c, str) else [p.get("text", "") for p in c or [] if isinstance(p, dict) and p.get("type") == "text"]
            for s in (x.strip() for x in texts):
                if s and not s.startswith(NOT_HUMAN):
                    close()
                    turns.append({"t": ts(t), "chars": len(s), "text": s})
                    ep["start"] = ts(t)
        elif o.get("type") == "assistant" and t:
            for p in c or []:
                if isinstance(p, dict) and p.get("type") == "tool_use" and p.get("id") not in seen:
                    seen.add(p.get("id"))
                    ep["start"] = ep["start"] or ts(t)
                    ep["end"] = ts(t)
                    ep["calls"] += 1
                    ep["shipped"] |= bool(SHIPPED.search(json.dumps(p.get("input") or {})))
    close()
    return turns, episodes


def load_run(d):
    """The agents of one workflow run: label, role, model, calls, tool_ts, events and the journal's return value."""
    labels, results = journal(d)
    rows = []
    for aid, label in labels.items():
        f = pathlib.Path(d) / f"agent-{aid}.jsonl"
        if f.exists():
            m = agent_metrics(f)
            if m["first"]:
                rows.append({**m, "label": label or aid, "role": role(label), "result": results.get(aid), "run": pathlib.Path(d).name})
    return rows


# ------------------------------------------------------------------------------------------------------------ the axis

class Axis:
    """Every event of the mission. Sorted by time, an event's rank is its notch on the clock."""

    def __init__(self):
        self.events = []

    def add(self, t, kind, **kw):
        e = {"t": t, "seq": len(self.events), "kind": kind, **kw}
        self.events.append(e)
        return e

    def rank(self):
        for i, e in enumerate(sorted(self.events, key=lambda e: (e["t"], e["seq"]))):
            e["rank"] = i
        self.n = len(self.events)


def mission(turns, episodes, runs):
    ax = Axis()
    humans = [{**h, "ev": ax.add(h["t"], "human")} for h in turns]
    for e in episodes:
        e["a"] = next((h["ev"] for h in humans if h["t"] == e["start"]), None) or ax.add(e["start"], "episode start")
        e["b"] = ax.add(e["end"], "episode end")
    agents = []
    for rows in runs:
        for r in rows:
            r["a"], r["marks"] = ax.add(ts(r["first"]), "agent start"), []
            agents.append(r)
    by_label = {(r["run"], r["label"]): r for r in agents}
    for r in agents:
        t_end = ts(r["last"])
        for e in r["events"]:
            ev = ax.add(ts(e["t"]), "verdict")
            kind = "ci" if e["kind"] == "ci" else "gate"
            r["marks"].append({"ev": ev, "type": "pass" if e["ok"] else "rej", "kind": kind, "text": e["text"][:140], "t": ts(e["t"])})
        res, slug, name = r["result"], r["label"].split(":", 1)[-1], r["label"]

        def mark(type_, kind, text):
            r["marks"].append({"ev": ax.add(t_end, "verdict"), "type": type_, "kind": kind, "text": text, "t": t_end, "at_end": True})

        if isinstance(res, dict):
            if res.get("status") == "SPLIT" or res.get("leads") or res.get("defects"):
                mark("dot", "findings", f"findings: status {res.get('status')}, {len(res.get('leads') or [])} leads, {len(res.get('defects') or [])} defects")
            if name.startswith("refute:") and "verdicts" in res:
                bad = [v for v in res["verdicts"] if not v.get("upheld")]
                if not bad:
                    mark("pass", "refute", "every verdict upheld")
                fix = by_label.get((r["run"], "fix:" + slug))
                for v in bad:
                    mark("rej", "refute", f"overturned {v.get('rule_id')}: {v.get('kind')}")
                    r["marks"][-1]["fix"] = fix
            if name.startswith("validate:"):
                for v in res.get("results") or []:
                    ok = v.get("status") == "pass"
                    mark("pass" if ok else "rej", "validator", f"{v.get('file')}: {v.get('status')}")
        elif isinstance(res, str) and name.startswith("lander"):
            for cl, pb in DROP.findall(res):
                mark("rej", "lander drop", f"dropped {pb} from cluster {cl}")
        r["b"] = ax.add(t_end, "agent return")
    ax.rank()
    agents.sort(key=lambda r: (r["a"]["rank"], r["a"]["seq"]))
    # a rejection's forward end: the fix agent's start, else the next mark of the same agent, else its return
    for r in agents:
        r["marks"].sort(key=lambda m: m["ev"]["rank"])
        for i, m in enumerate(r["marks"]):
            m["ring"] = m["tgt_ring"] = r
            nxt = r["marks"][i + 1]["ev"]["rank"] if i + 1 < len(r["marks"]) else r["b"]["rank"]
            if m.get("fix"):
                m["tgt_ring"], nxt = m["fix"], m["fix"]["a"]["rank"]
            m["tgt"] = max(nxt, m["ev"]["rank"] + 1.6)
        depth = []
        for m in (m for m in r["marks"] if m["type"] == "rej"):
            depth = [x for x in depth if x > m["ev"]["rank"]]
            m["depth"] = min(len(depth), 3)
            depth.append(m["tgt"])
    return ax, humans, agents


# ---------------------------------------------------------------------------------------------------------- geometry

def ang(rank, n):
    return -90 + SWEEP * rank / max(n - 1, 1)


def pt(r, rank, n):
    a = math.radians(ang(rank, n))
    return C + r * math.cos(a), C + r * math.sin(a)


def f(v):
    return f"{v:.1f}"


def wave(r, r0, r1, loops, n, amp):
    """A sinusoid along the arc from rank r0 to r1 at radius r, one cycle per loop."""
    steps = max(8, int(loops * 18), int((r1 - r0) * 3))
    pts = []
    for i in range(steps + 1):
        s = i / steps
        x, y = pt(r + amp * math.sin(2 * math.pi * loops * s), r0 + (r1 - r0) * s, n)
        pts.append(f"{'M' if i == 0 else 'L'}{f(x)},{f(y)}")
    return "".join(pts)


def curl(r, rank, r_to, rank_to, depth, n):
    """A red curl: leaves the verdict at (r, rank), sweeps back about 1.6 notch widths under the arc, turns forward."""
    back, under = 1.6 + 0.55 * depth, 5 + 3.4 * depth
    p0, p1 = pt(r, rank, n), pt(r_to, rank_to, n)
    c1, c2 = pt(r - under, rank - back * 1.35, n), pt(r_to - under, rank - back * 0.55, n)
    return f"M{f(p0[0])},{f(p0[1])} C{f(c1[0])},{f(c1[1])} {f(c2[0])},{f(c2[1])} {f(p1[0])},{f(p1[1])}"


def check(x, y, extra=""):
    return (f'<g class="ok" transform="translate({f(x)},{f(y)})"><circle r="5.2"/><path d="M-2.8,0.2 L-0.8,2.4 L3,-2.6"/>{extra}</g>')


# --------------------------------------------------------------------------------------------------------------- draw

def main_clock(ax, humans, episodes, agents, counts):
    n = ax.n
    step = min(10.0, (R_FIRST - R_LAST) / max(len(agents) - 1, 1))
    ring = {id(r): R_FIRST - i * step for i, r in enumerate(agents)}
    out = ['<svg class="main" viewBox="0 0 800 800" role="img" aria-label="Loop clock of the mission">']
    for e in ax.events:                                                  # the rim: one notch per event, longer for a return
        long = e["kind"] in ("agent return", "human")
        x0, y0, x1, y1 = *pt(R_RIM - (6 if long else 3), e["rank"], n), *pt(R_RIM + 2, e["rank"], n)
        out.append(f'<line class="rim{" long" if long else ""}" x1="{f(x0)}" y1="{f(y0)}" x2="{f(x1)}" y2="{f(y1)}"/>')
    for h in humans:
        x, y = pt(R_HUMAN, h["ev"]["rank"], n)
        steer = h["chars"] > STEER_CHARS
        rx, ry = (4.6, 11) if steer else (2.8, 5.5)
        out.append(f'<ellipse class="petal {"steer" if steer else "approve"}" cx="{f(x)}" cy="{f(y)}" rx="{rx}" ry="{ry}" '
                   f'transform="rotate({f(ang(h["ev"]["rank"], n) + 90)} {f(x)} {f(y)})"><title>{"steering" if steer else "approval"}: '
                   f'{html.escape(h["text"][:90])}</title></ellipse>')
    out.append(f'<circle class="track" cx="{C}" cy="{C}" r="{R_ORCH}"/>')
    for e in episodes:
        loops = max(1, round(e["calls"] / LOOP_CALLS))
        out.append(f'<path class="orch{" bold" if e["shipped"] else ""}" d="{wave(R_ORCH, e["a"]["rank"], max(e["b"]["rank"], e["a"]["rank"] + 1), loops, n, 4)}">'
                   f'<title>orchestrator: {e["calls"]} tool calls{", merged or shipped" if e["shipped"] else ""}</title></path>')
    for r in agents:
        rad, a, b = ring[id(r)], r["a"]["rank"], r["b"]["rank"]
        loops = max(1, round(r["tool_calls"] / LOOP_CALLS))
        dash = ' stroke-dasharray="5 3"' if weight(r["model"]) < 2 else ""
        out.append(f'<circle class="track" cx="{C}" cy="{C}" r="{f(rad)}"/>')
        out.append(f'<path class="agent {r["role"]}" style="stroke-width:{weight(r["model"])}"{dash} d="{wave(rad, a, max(b, a + 1), loops, n, 2.3)}">'
                   f'<title>{html.escape(r["label"])}: {r["turns"]} turns, {r["tool_calls"]} tool calls, {r["tokens"] / 1e6:.1f} M tokens, '
                   f'{html.escape(short_model(r["model"]))}</title></path>')
        for m in r["marks"]:
            x, y = pt(rad, m["ev"]["rank"], n)
            tip = f'<title>{html.escape(r["label"])}: {html.escape(m["text"])}</title>'
            if m["type"] == "pass":
                out.append(check(x, y, tip))
            elif m["type"] == "dot":
                out.append(f'<circle class="dot" cx="{f(x)}" cy="{f(y)}" r="3.6">{tip}</circle>')
            else:
                out.append(f'<path class="curl" d="{curl(rad, m["ev"]["rank"], ring[id(m["tgt_ring"])], m["tgt"], m["depth"], n)}">{tip}</path>')
    out.append(f'<text class="big" x="{C}" y="{C - 22}" text-anchor="middle">{counts["humans"]} human turns</text>')
    out.append(f'<text class="mid" x="{C}" y="{C - 8}" text-anchor="middle">{counts["iterations"]} iterations, {counts["agents"]} agents</text>')
    out.append(f'<text class="mid red" x="{C}" y="{C + 14}" text-anchor="middle">{counts["rejections"]} rejections</text>')
    out.append('</svg>')
    return "".join(out)


def mini_clock(r):
    """One agent: its tool-call spiral (one loop per 40 calls) and its verdict marks at their tool-call index."""
    calls = max(r["tool_calls"], 1)
    loops = calls / LOOP_CALLS
    dr = min(4.0, 34.0 / max(loops, 1))
    pos = lambda i, off=0.0: ((lambda a, rad: (50 + rad * math.sin(a), 50 - rad * math.cos(a)))(2 * math.pi * i / LOOP_CALLS, 9 + dr * i / LOOP_CALLS + off))
    d = "".join(f"{'M' if i == 0 else 'L'}{f(pos(i)[0])},{f(pos(i)[1])}" for i in range(calls + 1))
    dash = ' stroke-dasharray="3 2"' if weight(r["model"]) < 2 else ""
    out = [f'<svg viewBox="0 0 100 100" role="img" aria-label="{html.escape(r["label"])}"><circle class="track" cx="50" cy="50" r="46"/>',
           f'<path class="agent {r["role"]}" style="stroke-width:{weight(r["model"]) * 0.55:.2f}"{dash} d="{d}"/>']
    stamps = [ts(t) for t in r["tool_ts"]]
    for m in r["marks"]:
        i = calls if m.get("at_end") else bisect.bisect_right(stamps, m["t"])
        x, y = pos(i)
        if m["type"] == "pass":
            out.append(f'<g class="ok" transform="translate({f(x)},{f(y)}) scale(.62)"><circle r="5.2"/><path d="M-2.8,0.2 L-0.8,2.4 L3,-2.6"/></g>')
        elif m["type"] == "dot":
            out.append(f'<circle class="dot" cx="{f(x)}" cy="{f(y)}" r="2.2"/>')
        else:
            a, b, c, e = pos(i, 0), pos(i - 7, -4), pos(i - 3, -4), pos(i + 5, 0)
            out.append(f'<path class="curl" d="M{f(a[0])},{f(a[1])} C{f(b[0])},{f(b[1])} {f(c[0])},{f(c[1])} {f(e[0])},{f(e[1])}"/>')
    out.append('</svg>')
    return f'<figure>{"".join(out)}<figcaption>{html.escape(r["label"])}<br>{r["tool_calls"]} calls, {html.escape(short_model(r["model"]))}</figcaption></figure>'


def counts_of(humans, agents):
    rej = {k: sum(1 for r in agents for m in r["marks"] if m["type"] == "rej" and m["kind"] == k) for k in KINDS}
    return {"humans": len(humans), "steer": sum(h["chars"] > STEER_CHARS for h in humans), "agents": len(agents),
            "iterations": len(agents), "rejections": sum(rej.values()), "by_kind": rej}


PAGE = """<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Loop Clock</title>
<style>
:root { --bg:#f6f7f4; --fg:#16201c; --dim:#556259; --rule:#d9dfda; }
@media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) { --bg:#0f1512; --fg:#e4ebe6; --dim:#97a59d; --rule:#2a3530; color-scheme:dark } }
:root[data-theme="dark"] { --bg:#0f1512; --fg:#e4ebe6; --dim:#97a59d; --rule:#2a3530; color-scheme:dark }
body { background:var(--bg); color:var(--fg); font:15px/1.5 system-ui, "Segoe UI", sans-serif; margin:0; padding:24px 16px }
main { max-width:1180px; margin:0 auto } h1 { font-size:26px; margin:0 0 6px } p { max-width:72ch; color:var(--dim); margin:0 0 12px }
.counts { display:flex; flex-wrap:wrap; gap:6px 22px; margin:14px 0; font-size:14px } .counts b { font-size:20px; font-variant-numeric:tabular-nums }
.counts span { color:var(--dim) }
.key, .panel { --impl:#52bd8b; --lander:#6b9be6; --write:#d9a43c; --refute:#c086d6; --fix:#e08a5e; --validate:#5ec0c8; --survey:#a8b95a; --other:#8d9892 }
.panel { --p-bg:#10161c; --p-fg:#dfe7ee; --p-dim:#8795a3; --p-track:#1d2731; --p-ok:#62c58a; --p-warn:#f0a23c; --p-red:#ee5b4a; --p-human:#d8e2ec;
  --impl:#52bd8b; --lander:#6b9be6; --write:#d9a43c; --refute:#c086d6; --fix:#e08a5e; --validate:#5ec0c8; --survey:#a8b95a; --other:#8d9892;
  background:var(--p-bg); color:var(--p-fg); border-radius:10px; padding:14px; display:grid; grid-template-columns:minmax(0,1.2fr) minmax(0,1fr); gap:18px }
@media (max-width:760px) { .panel { grid-template-columns:minmax(0,1fr) } }
.main { width:100%; height:auto; display:block } .track { fill:none; stroke:var(--p-track); stroke-width:.8 }
.rim { stroke:var(--p-track); stroke-width:1 } .rim.long { stroke:var(--p-dim) }
.petal.steer { fill:var(--p-human); stroke:var(--p-human); stroke-width:1 } .petal.approve { fill:none; stroke:var(--p-human); stroke-width:1.2 }
.orch { fill:none; stroke:var(--p-dim); stroke-width:1.6; stroke-linejoin:round } .orch.bold { stroke:var(--p-fg); stroke-width:3 }
.agent { fill:none; stroke-linecap:round; stroke-linejoin:round } .impl{stroke:var(--impl)} .lander{stroke:var(--lander)} .write{stroke:var(--write)}
.refute{stroke:var(--refute)} .fix{stroke:var(--fix)} .validate{stroke:var(--validate)} .survey{stroke:var(--survey)} .other{stroke:var(--other)}
.ok circle { fill:var(--p-bg); stroke:var(--p-ok); stroke-width:1.2 } .ok path { fill:none; stroke:var(--p-ok); stroke-width:1.8; stroke-linecap:round; stroke-linejoin:round }
.dot { fill:var(--p-warn); stroke:var(--p-bg); stroke-width:1.2 } .curl { fill:none; stroke:var(--p-red); stroke-width:1.9; stroke-linecap:round }
.big { fill:var(--p-fg); font-size:18px; font-weight:600 } .mid { fill:var(--p-dim); font-size:13px } .mid.red { fill:var(--p-red) }
.multiples { display:grid; grid-template-columns:repeat(auto-fill,minmax(104px,1fr)); gap:10px 8px; align-content:start }
figure { margin:0 } figure svg { width:100%; height:auto; display:block } figcaption { font-size:11px; line-height:1.3; color:var(--p-dim); text-align:center; overflow-wrap:anywhere }
.key { display:flex; flex-wrap:wrap; gap:6px 16px; margin-top:14px; font-size:13px; color:var(--dim) } .key span { display:inline-flex; align-items:center; gap:6px }
.key svg { width:22px; height:14px } .key .sw { width:10px; height:10px; border-radius:2px; display:inline-block }
</style></head><body><main>
<h1>Loop clock</h1>
<p>Position around the clock is mission progress: each event (an agent starting or returning, a verdict, a human turn, the end of an orchestrator episode) takes the next notch, so idle time takes no room. From the outside in are the human, the orchestrator, then one ring per agent.</p>
<p>A red curl is a rejection that leaves the verdict, sweeps back under the work it reviewed and turns forward into the fix. The small clocks on the right show each agent's tool-call coil, one loop per 40 calls, with its own verdicts.</p>
<div class="counts">{COUNTS}</div>
<div class="panel"><div>{MAIN}</div><div class="multiples">{MINI}</div></div>
<div class="key">{KEY}</div>
</main></body></html>
"""


def key_html():
    s = '<svg viewBox="0 0 22 14"><{}/></svg>'
    items = [s.format('ellipse class="petal steer" cx="11" cy="7" rx="3" ry="6.5" style="fill:#8795a3;stroke:none"'), "steering turn"]
    items += [s.format('ellipse cx="11" cy="7" rx="2" ry="4" style="fill:none;stroke:#8795a3;stroke-width:1.2"'), "approval"]
    items += [s.format('path d="M1,7 q2,-5 4,0 t4,0 t4,0 t4,0" style="fill:none;stroke:#8795a3;stroke-width:1.6"'), "orchestrator coil (bold: it shipped)"]
    items += [s.format('path d="M1,7 L21,7" style="stroke:#52bd8b;stroke-width:4"'), "Opus", s.format('path d="M1,7 L21,7" style="stroke:#52bd8b;stroke-width:2.6"'), "Sonnet"]
    items += [s.format('path d="M3,7 L8,10 L18,3" style="fill:none;stroke:#62c58a;stroke-width:2"'), "pass",
              s.format('circle cx="11" cy="7" r="3.6" style="fill:#f0a23c"'), "findings",
              s.format('path d="M2,4 C8,13 12,13 20,4" style="fill:none;stroke:#ee5b4a;stroke-width:2"'), "rejection"]
    sw = "".join(f'<span><i class="sw" style="background:var(--{r},#888)"></i>{r}</span>' for r in ROLES)
    return "".join(f"<span>{items[i]} {items[i + 1]}</span>" for i in range(0, len(items), 2)) + sw


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("dirs", nargs="+", help="workflow transcript dirs")
    ap.add_argument("--orchestrator", required=True, help="the orchestrator session transcript (.jsonl)")
    ap.add_argument("--out", required=True)
    a = ap.parse_args()
    runs = [load_run(d) for d in a.dirs]
    if not all(runs):
        sys.exit("a workflow dir has no agent transcripts")
    turns, episodes = orchestrator(a.orchestrator)
    ax, humans, agents = mission(turns, episodes, runs)
    cts = counts_of(humans, agents)
    stat = lambda v, k: f"<div><b>{v}</b> <span>{k}</span></div>"
    counts = (stat(cts["humans"], f"human turns ({cts['steer']} steering)") + stat(cts["iterations"], "iterations (agent returns)")
              + stat(cts["rejections"], "rejections: " + ", ".join(f"{v} {k}" for k, v in cts["by_kind"].items() if v)))
    page = (PAGE.replace("{COUNTS}", counts).replace("{MAIN}", main_clock(ax, humans, episodes, agents, cts))
            .replace("{MINI}", "".join(mini_clock(r) for r in agents)).replace("{KEY}", key_html()))
    pathlib.Path(a.out).write_text(page, encoding="utf-8", newline="\n")
    print(f"wrote {a.out}: {ax.n} events, {cts['humans']} human turns ({cts['steer']} steering), {len(episodes)} orchestrator episodes, "
          f"{cts['agents']} agents, {cts['rejections']} rejections {cts['by_kind']}")


if __name__ == "__main__":
    main()
