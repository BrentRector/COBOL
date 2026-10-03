#!/usr/bin/env python3
"""Loop clock: draw Workflow runs as nested arcs, one ring per agent, from their transcripts (kb/Work PB1912).

    python scripts/telemetry/loop_clock.py <workflow transcript dir> [<dir> ...] --out clock.html [--title NAME]

Each ring is one agent. Its arc runs from its first to its last transcript record, on a clock whose full sweep is the
whole run. Stroke weight is the model (heavy Opus, medium Sonnet, thin other); colour is the role read from the label
(impl, land, write, refute, fix, validate, survey). Notches along an arc are one per 40 tool calls, so a long coil is a
long transcript. A red mark is a gate that came back RED (`=== BUILD-LOCAL GATE: RED` or `=== LINUX GATE: RED` in a tool
result). Numbers come from `workflow_metrics.agent_metrics`, the one transcript reader.

Not drawn yet: the orchestrator's own rings, refuter overturns, lander drops, CI reds (they are not in an agent
transcript), and the orchestrator's context size per turn.
"""
import argparse, html, json, math, pathlib, sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from workflow_metrics import agent_metrics, ts  # noqa: E402

SWEEP = 330.0          # degrees of the full run; the gap at the top keeps the start and the end apart
CX = CY = 380
OUTER = 330
NOTCH_CALLS = 40
ROLES = ("impl", "lander", "write", "refute", "fix", "validate", "survey")


def role(label):
    l = (label or "").lower()
    for r in ROLES:
        if l.startswith(r):
            return r
    return "other"


def weight(model):
    m = (model or "").lower()
    return 7 if "opus" in m else 4 if "sonnet" in m else 2


def pt(r, frac):
    a = math.radians(-90 + SWEEP * frac)
    return CX + r * math.cos(a), CY + r * math.sin(a)


def arc(r, f0, f1):
    x0, y0 = pt(r, f0)
    x1, y1 = pt(r, max(f1, f0 + 0.002))
    large = 1 if (f1 - f0) * SWEEP > 180 else 0
    return f"M{x0:.1f},{y0:.1f} A{r},{r} 0 {large} 1 {x1:.1f},{y1:.1f}"


def load(d):
    d = pathlib.Path(d)
    labels = {}
    for line in (d / "journal.jsonl").read_text(encoding="utf-8").splitlines():
        o = json.loads(line)
        if o.get("type") == "started":
            labels[o["agentId"]] = o.get("label")
    rows = []
    for aid, label in labels.items():
        f = d / f"agent-{aid}.jsonl"
        if f.exists():
            m = agent_metrics(f)
            if m.get("first"):
                rows.append({"label": label or aid, **m})
    return rows


def clock(name, rows):
    t0 = min(ts(r["first"]) for r in rows)
    t1 = max(ts(r["last"]) for r in rows)
    span = max((t1 - t0).total_seconds(), 1.0)
    frac = lambda s: (ts(s) - t0).total_seconds() / span
    rows = sorted(rows, key=lambda r: ts(r["first"]))
    step = min(24.0, (OUTER - 70) / max(len(rows), 1))
    out = [f'<svg viewBox="0 0 760 760" role="img" aria-label="Loop clock for {html.escape(name)}">']
    for h in range(0, int(span // 3600) + 2):                      # an hour mark on the outer rim
        f = h * 3600 / span
        if f <= 1:
            x0, y0 = pt(OUTER + 6, f)
            x1, y1 = pt(OUTER + 16, f)
            out.append(f'<line class="hr" x1="{x0:.1f}" y1="{y0:.1f}" x2="{x1:.1f}" y2="{y1:.1f}"/>')
            tx, ty = pt(OUTER + 28, f)
            out.append(f'<text class="lab" x="{tx:.1f}" y="{ty:.1f}" text-anchor="middle">{h}h</text>')
    for i, r in enumerate(rows):
        rad = OUTER - i * step
        f0, f1 = frac(r["first"]), frac(r["last"])
        cls = role(r["label"])
        out.append(f'<path class="track" d="{arc(rad, 0, 1)}"/>')
        out.append(f'<path class="agent {cls}" style="stroke-width:{weight(r["model"])}" d="{arc(rad, f0, f1)}">'
                   f'<title>{html.escape(r["label"])} | {r["turns"]} turns, {r["tool_calls"]} tool calls, '
                   f'{r["tokens"] / 1e6:.1f} M tokens, {r["wall_s"] / 60:.0f} min, {html.escape(r["model"] or "?")}</title></path>')
        for k in range(1, r["tool_calls"] // NOTCH_CALLS + 1):
            f = f0 + (f1 - f0) * (k * NOTCH_CALLS) / max(r["tool_calls"], 1)
            x0, y0 = pt(rad - 4, f)
            x1, y1 = pt(rad + 4, f)
            out.append(f'<line class="notch" x1="{x0:.1f}" y1="{y0:.1f}" x2="{x1:.1f}" y2="{y1:.1f}"/>')
        for s in r["gate_reds"]:
            x, y = pt(rad, frac(s))
            out.append(f'<circle class="red" cx="{x:.1f}" cy="{y:.1f}" r="5"><title>gate RED in {html.escape(r["label"])}</title></circle>')
    reds = sum(len(r["gate_reds"]) for r in rows)
    toks = sum(r["tokens"] for r in rows) / 1e6
    out.append(f'<text class="big" x="{CX}" y="{CY - 14}" text-anchor="middle">{len(rows)} agents</text>')
    out.append(f'<text class="mid" x="{CX}" y="{CY + 10}" text-anchor="middle">{span / 3600:.1f} h, {toks:.0f} M tokens</text>')
    out.append(f'<text class="mid red-t" x="{CX}" y="{CY + 32}" text-anchor="middle">{reds} gate red{"s" if reds != 1 else ""}</text>')
    out.append("</svg>")
    table = ['<div class="scroll"><table><thead><tr><th>agent</th><th>model</th><th class="n">turns</th><th class="n">tool calls</th>'
             '<th class="n">M tokens</th><th class="n">min</th><th class="n">reds</th></tr></thead><tbody>']
    for r in rows:
        table.append(f'<tr><td><i class="sw {role(r["label"])}"></i>{html.escape(r["label"])}</td><td>{html.escape((r["model"] or "?").replace("claude-", ""))}</td>'
                     f'<td class="n">{r["turns"]}</td><td class="n">{r["tool_calls"]}</td><td class="n">{r["tokens"] / 1e6:.1f}</td>'
                     f'<td class="n">{r["wall_s"] / 60:.0f}</td><td class="n">{len(r["gate_reds"]) or ""}</td></tr>')
    table.append("</tbody></table></div>")
    return f'<section><h2>{html.escape(name)}</h2><div class="clock">{"".join(out)}</div>{"".join(table)}</section>'


PAGE = """<title>Fleet Loop Clock</title>
<style>
:root { --bg:#f6f7f4; --fg:#16201c; --dim:#5b6862; --rule:#d9dfda; --track:#e4e9e5; --crit:#c2402f;
  --impl:#1f6f4f; --lander:#2a5db0; --write:#9a6a12; --refute:#7a3f8f; --fix:#b5532a; --validate:#2f7f86; --survey:#6b7a2b; --other:#7b857f; }
@media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) { --bg:#0f1512; --fg:#e4ebe6; --dim:#97a59d; --rule:#2a3530; --track:#1b2420; --crit:#ee7a68;
  --impl:#52bd8b; --lander:#6b9be6; --write:#d9a43c; --refute:#c086d6; --fix:#e08a5e; --validate:#5ec0c8; --survey:#a8b95a; --other:#8d9892; color-scheme: dark } }
:root[data-theme="dark"] { --bg:#0f1512; --fg:#e4ebe6; --dim:#97a59d; --rule:#2a3530; --track:#1b2420; --crit:#ee7a68;
  --impl:#52bd8b; --lander:#6b9be6; --write:#d9a43c; --refute:#c086d6; --fix:#e08a5e; --validate:#5ec0c8; --survey:#a8b95a; --other:#8d9892; color-scheme: dark }
body { background:var(--bg); color:var(--fg); font:15px/1.5 "IBM Plex Sans", system-ui, sans-serif; padding-inline:16px; padding-block:24px; max-width:860px; margin:0 auto }
h1 { font-size:26px; margin:0 0 6px } h2 { font-size:16px; margin:36px 0 8px; letter-spacing:.04em; text-transform:uppercase; color:var(--dim) }
p { max-width:66ch; color:var(--dim); margin:0 0 10px } .key { display:flex; flex-wrap:wrap; gap:6px 16px; font-size:13px; color:var(--dim); margin:10px 0 }
.clock svg { width:100%; height:auto; display:block } .track { fill:none; stroke:var(--track); stroke-width:1 }
.agent { fill:none; stroke-linecap:round } .impl{stroke:var(--impl)} .lander{stroke:var(--lander)} .write{stroke:var(--write)} .refute{stroke:var(--refute)}
.fix{stroke:var(--fix)} .validate{stroke:var(--validate)} .survey{stroke:var(--survey)} .other{stroke:var(--other)}
.notch { stroke:var(--bg); stroke-width:1.5 } .red { fill:var(--crit); stroke:var(--bg); stroke-width:1.5 } .hr { stroke:var(--dim); stroke-width:1 }
.lab { fill:var(--dim); font-size:11px } .big { fill:var(--fg); font-size:22px; font-weight:600 } .mid { fill:var(--dim); font-size:13px } .red-t { fill:var(--crit) }
.scroll { overflow-x:auto } table { border-collapse:collapse; width:100%; font-size:13px; font-variant-numeric:tabular-nums } th { text-align:left; color:var(--dim); font-weight:600; padding:6px 10px; border-bottom:1px solid var(--rule) }
td { padding:5px 10px; border-bottom:1px solid var(--rule); white-space:nowrap } .n { text-align:right } .sw { display:inline-block; width:10px; height:10px; border-radius:2px; margin-right:8px; background:var(--other) }
.sw.impl{background:var(--impl)} .sw.lander{background:var(--lander)} .sw.write{background:var(--write)} .sw.refute{background:var(--refute)} .sw.fix{background:var(--fix)} .sw.validate{background:var(--validate)} .sw.survey{background:var(--survey)}
</style>
<h1>Fleet loop clock</h1>
<p>One ring per agent, drawn on a clock whose full sweep is the run. An arc is the agent from first to last record. Heavy strokes are Opus, medium Sonnet. A notch is 40 tool calls. A red dot is a gate that came back RED. Rings are ordered by start time, outermost first.</p>
<div class="key">{KEY}</div>
{BODY}
"""


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("dirs", nargs="+")
    ap.add_argument("--out", required=True)
    ap.add_argument("--title", action="append", default=[], help="a name per dir, in order")
    a = ap.parse_args()
    body = []
    for i, d in enumerate(a.dirs):
        rows = load(d)
        if not rows:
            sys.exit(f"no agent transcripts in {d}")
        name = a.title[i] if i < len(a.title) else pathlib.Path(d).name
        body.append(clock(name, rows))
    key = "".join(f'<span><i class="sw {r}"></i>{r}</span>' for r in ROLES)
    pathlib.Path(a.out).write_text(PAGE.replace("{KEY}", key).replace("{BODY}", "".join(body)), encoding="utf-8", newline="\n")
    print(f"wrote {a.out}: {len(a.dirs)} run(s)")


if __name__ == "__main__":
    main()
