#!/usr/bin/env python3
"""Per-agent metrics for one Workflow run, measured from its transcript directory — the raw data of a fleet experiment.

    python scripts/telemetry/workflow_metrics.py <workflow transcript dir> [--out metrics.json]

The transcript dir is the one the Workflow tool prints ("Transcript dir: …\\subagents\\workflows\\wf_<id>"). For each
agent it reads `journal.jsonl` (label, return value) and `agent-<id>.jsonl` (the model calls) and reports:

  turns        distinct assistant messages (one model call each)
  tool_calls   tool_use blocks
  tool_ts      the timestamp of each tool_use block, in order (the loop clock places a verdict at its tool-call index)
  events       verdicts found in tool RESULTS (never in the brief's text), de-duplicated by the matched line (a line that is a prefix of a known one is the same verdict):
               {t, kind, ok, text} with kind "build-local" or "linux" (`=== BUILD-LOCAL GATE: GREEN|RED...`, ok =
               GREEN) or "ci" (`push-main: run <id> concluded '<conclusion>'` and `push-main: ✅ landed`, ok = the
               run succeeded). A verdict line quoted inside backticks in prose is skipped; a plain echo of an
               earlier run's line (a lander reading an implementer's log) still counts, once per distinct line.
  tokens       input + output + cache-read + cache-write, summed over calls (what the quadratic cost law counts)
  fresh        input + cache-write + output (the non-cached part)
  wall_s       first to last transcript timestamp
  result       the agent's structured return value, verbatim

`journal(dir)` is the one reader of `journal.jsonl` (labels and return values); the loop clock uses it too.

Written for the fleet-optimization evidence record (docs/rearchitecture/evidence/fleet-optimization/): every experiment
stores this JSON beside its write-up, so a later analysis never depends on transcripts that may be pruned. It reads
defensively: a line that does not parse is counted in `unparsed`, never silently dropped.
"""
import argparse, datetime, json, pathlib, re, sys


def ts(s):
    return datetime.datetime.fromisoformat(s.replace("Z", "+00:00"))


# A verdict line starts at "===" or "push-main:" and runs to the end of the line. The (?<![`\w]) guard drops a verdict
# quoted inside backticks in prose.
GATE = re.compile(r"(?<![`\w])=== (BUILD-LOCAL|LINUX) GATE: (GREEN|RED)[^\n]*")
CI = re.compile(r"(?<![`\w])push-main: (?:run \d+ concluded '(\w+)'|✅ landed)[^\n]*")


def result_text(part):
    c = part.get("content")
    if isinstance(c, list):
        c = "\n".join(x.get("text", "") for x in c if isinstance(x, dict))
    return c if isinstance(c, str) else ""


def verdicts(text):
    """(kind, ok, matched line) for every gate or CI verdict line in one tool result."""
    for m in GATE.finditer(text):
        yield ("build-local" if m.group(1) == "BUILD-LOCAL" else "linux"), m.group(2) == "GREEN", m.group(0).strip()
    for m in CI.finditer(text):
        yield "ci", m.group(1) in (None, "success"), m.group(0).strip()


def agent_metrics(path):
    turns = unparsed = 0
    tok = {"input": 0, "output": 0, "cache_read": 0, "cache_write": 0}
    stamps, seen, events, model = [], set(), [], None
    tool_ts = []
    for line in path.read_text(encoding="utf-8").splitlines():
        try:
            o = json.loads(line)
        except Exception:
            unparsed += 1
            continue
        if o.get("timestamp"):
            stamps.append(o["timestamp"])
        content = (o.get("message") or {}).get("content")
        if o.get("type") == "user" and o.get("timestamp") and isinstance(content, list):
            for p in content:
                if isinstance(p, dict) and p.get("type") == "tool_result":
                    for kind, ok, text in verdicts(result_text(p)):
                        # one run's line reaches the transcript more than once, often cut short at a different
                        # column each time: a line that is a prefix of a known one is the same verdict
                        same = next((e for e in events if e["kind"] == kind and (e["text"].startswith(text) or text.startswith(e["text"]))), None)
                        if same is None:
                            events.append({"t": o["timestamp"], "kind": kind, "ok": ok, "text": text})
                        elif len(text) > len(same["text"]):
                            same["text"] = text
        if o.get("type") != "assistant":
            continue
        m = o.get("message") or {}
        model = model or m.get("model")
        mid = m.get("id")
        if mid and mid not in seen:
            seen.add(mid)
            turns += 1
            u = m.get("usage") or {}
            tok["input"] += u.get("input_tokens", 0)
            tok["output"] += u.get("output_tokens", 0)
            tok["cache_read"] += u.get("cache_read_input_tokens", 0)
            tok["cache_write"] += u.get("cache_creation_input_tokens", 0)
        for p in content or []:
            if isinstance(p, dict) and p.get("type") == "tool_use":
                tool_ts.append(o.get("timestamp"))
    wall = (ts(stamps[-1]) - ts(stamps[0])).total_seconds() if len(stamps) > 1 else 0.0
    return {"turns": turns, "tool_calls": len(tool_ts), "tool_ts": tool_ts, "tokens": sum(tok.values()),
            "fresh": tok["input"] + tok["cache_write"] + tok["output"], "token_parts": tok,
            "wall_s": round(wall, 1), "first": stamps[0] if stamps else None, "last": stamps[-1] if stamps else None,
            "model": model, "events": events, "unparsed": unparsed}


def journal(d):
    """(labels, results) of one workflow run, each keyed by agent id, from its journal.jsonl."""
    labels, results = {}, {}
    for line in (pathlib.Path(d) / "journal.jsonl").read_text(encoding="utf-8").splitlines():
        o = json.loads(line)
        if o.get("type") == "started":
            labels[o["agentId"]] = o.get("label")
        elif o.get("type") == "result":
            results[o["agentId"]] = o.get("result")
    return labels, results


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("dir")
    ap.add_argument("--out")
    a = ap.parse_args()
    d = pathlib.Path(a.dir)
    labels, results = journal(d)
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
