#!/usr/bin/env python3
"""Per-agent metrics for one Workflow run, measured from its transcript directory — the raw data of a fleet experiment.

    python scripts/telemetry/workflow_metrics.py <workflow transcript dir> [--out metrics.json]

The transcript dir is the one the Workflow tool prints ("Transcript dir: …\\subagents\\workflows\\wf_<id>"). For each
agent it reads `journal.jsonl` (label, return value) and `agent-<id>.jsonl` (the model calls) and reports:

  turns        distinct assistant messages (one model call each)
  tool_calls   tool_use blocks
  tokens       input + output + cache-read + cache-write, summed over calls (what the quadratic cost law counts)
  fresh        input + cache-write + output (the non-cached part)
  wall_s       first to last transcript timestamp
  result       the agent's structured return value, verbatim

Written for the fleet-optimization evidence record (docs/rearchitecture/evidence/fleet-optimization/): every experiment
stores this JSON beside its write-up, so a later analysis never depends on transcripts that may be pruned. It reads
defensively: a line that does not parse is counted in `unparsed`, never silently dropped.
"""
import argparse, datetime, json, pathlib, sys


def ts(s):
    return datetime.datetime.fromisoformat(s.replace("Z", "+00:00"))


def agent_metrics(path):
    turns = tools = unparsed = 0
    tok = {"input": 0, "output": 0, "cache_read": 0, "cache_write": 0}
    stamps, seen = [], set()
    for line in path.read_text(encoding="utf-8").splitlines():
        try:
            o = json.loads(line)
        except Exception:
            unparsed += 1
            continue
        if o.get("timestamp"):
            stamps.append(o["timestamp"])
        if o.get("type") != "assistant":
            continue
        m = o.get("message") or {}
        mid = m.get("id")
        if mid and mid not in seen:
            seen.add(mid)
            turns += 1
            u = m.get("usage") or {}
            tok["input"] += u.get("input_tokens", 0)
            tok["output"] += u.get("output_tokens", 0)
            tok["cache_read"] += u.get("cache_read_input_tokens", 0)
            tok["cache_write"] += u.get("cache_creation_input_tokens", 0)
        tools += sum(1 for p in m.get("content") or [] if isinstance(p, dict) and p.get("type") == "tool_use")
    wall = (ts(stamps[-1]) - ts(stamps[0])).total_seconds() if len(stamps) > 1 else 0.0
    return {"turns": turns, "tool_calls": tools, "tokens": sum(tok.values()),
            "fresh": tok["input"] + tok["cache_write"] + tok["output"], "token_parts": tok,
            "wall_s": round(wall, 1), "first": stamps[0] if stamps else None, "unparsed": unparsed}


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("dir")
    ap.add_argument("--out")
    a = ap.parse_args()
    d = pathlib.Path(a.dir)
    labels, results = {}, {}
    for line in (d / "journal.jsonl").read_text(encoding="utf-8").splitlines():
        o = json.loads(line)
        if o.get("type") == "started":
            labels[o["agentId"]] = o.get("label")
        elif o.get("type") == "result":
            results[o["agentId"]] = o.get("result")
    rows = []
    for aid, label in labels.items():
        f = d / f"agent-{aid}.jsonl"
        m = agent_metrics(f) if f.exists() else {"missing_transcript": True}
        rows.append({"agent": aid, "label": label, **m, "returned": aid in results, "result": results.get(aid)})
    rows.sort(key=lambda r: r["label"] or "")
    out = {"workflow_dir": d.name, "agents": len(rows), "returned": sum(r["returned"] for r in rows), "rows": rows}
    text = json.dumps(out, indent=1, ensure_ascii=False)
    if a.out:
        pathlib.Path(a.out).write_text(text, encoding="utf-8")
        print(f"wrote {a.out}: {out['agents']} agents, {out['returned']} returned")
    else:
        sys.stdout.reconfigure(encoding="utf-8")
        print(text)


if __name__ == "__main__":
    main()
