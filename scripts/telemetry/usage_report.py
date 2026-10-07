#!/usr/bin/env python3
"""Summarize the Claude Code telemetry otlp_sink.py recorded: tokens and cost per agent / skill / model.

    python scripts/telemetry/usage_report.py                  # today (UTC)
    python scripts/telemetry/usage_report.py 2026-09-25 ...   # named days
    python scripts/telemetry/usage_report.py --all
    python scripts/telemetry/usage_report.py --account-uuid <id> [day ...]   # one Claude account only

The sink is per MACHINE: every Claude account on it exports here, and each event names its account in
`user.account_uuid` (`ACCOUNT_ATTR`; `python scripts/orchestrator/account.py --field account_uuid` prints the running
account's). The header line counts the events per account, so a report that mixes accounts says so.

Reads the `api_request` log events (one per model call: model, input/output/cache tokens, cost) and groups them by the
identifying attributes Claude Code attaches (agent name, else query source; skill when present). Attribute names are
read defensively — an event that lacks one is grouped under "-", never dropped, and the event total is printed so a
parse that silently matched nothing is visible.
"""
import collections
import datetime
import json
import pathlib
import sys

from otlp_sink import OUT_DIR as DIR  # the sink writes it; one definition of the location

ACCOUNT_ATTR = "user.account_uuid"
TOKENS = ("input_tokens", "output_tokens", "cache_read_tokens", "cache_creation_tokens")


def attrs(lst):
    out = {}
    for a in lst or []:
        v = a.get("value") or {}
        for k in ("stringValue", "intValue", "doubleValue", "boolValue"):
            if k in v:
                out[a.get("key")] = v[k]
                break
    return out


def num(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return 0.0


def events(files):
    for f in files:
        for line in f.read_text(encoding="utf-8").splitlines():
            try:
                rec = json.loads(line)
            except ValueError:
                continue
            for rl in (rec.get("body") or {}).get("resourceLogs", []):
                for sl in rl.get("scopeLogs", []):
                    for lr in sl.get("logRecords", []):
                        a = attrs(lr.get("attributes"))
                        name = str(a.get("event.name") or (lr.get("body") or {}).get("stringValue") or "")
                        if name.endswith("api_request"):
                            yield a


def main(argv):
    only = None
    if "--account-uuid" in argv:
        i = argv.index("--account-uuid")
        if i + 1 >= len(argv):
            print("--account-uuid needs a value")
            return 2
        only, argv = argv[i + 1], argv[:i] + argv[i + 2:]
    if "--all" in argv:
        files = sorted(DIR.glob("*.jsonl"))
    else:
        days = [d for d in argv if not d.startswith("-")] or [f"{datetime.datetime.now(datetime.timezone.utc):%Y-%m-%d}"]
        files = [DIR / f"{d}.jsonl" for d in days if (DIR / f"{d}.jsonl").exists()]
    if not files:
        print(f"no telemetry under {DIR} for {argv or 'today'} — is otlp_sink.py running and telemetry enabled?")
        return 1
    groups = collections.defaultdict(lambda: collections.Counter())
    n = 0
    per_account = collections.Counter()
    for a in events(files):
        acct = str(a.get(ACCOUNT_ATTR) or "-")
        per_account[acct] += 1
        if only is not None and acct != only:
            continue
        n += 1
        who = a.get("agent.name") or a.get("agent_type") or a.get("query_source") or "-"
        key = (str(who), str(a.get("skill.name") or "-"), str(a.get("model") or "-"))
        c = groups[key]
        c["calls"] += 1
        for t in TOKENS:
            c[t] += num(a.get(t))
        c["cost_usd"] += num(a.get("cost_usd"))
    print(f"{n} api_request events in {len(files)} file(s)"
          + (f" for account {only}" if only is not None else "")
          + f"; per account: {', '.join(f'{k[:8]} {v}' for k, v in per_account.most_common())}")
    rows = sorted(groups.items(), key=lambda kv: -(kv[1]["cache_read_tokens"] + kv[1]["input_tokens"]))
    print(f"{'agent/source':32} {'skill':20} {'model':24} {'calls':>6} {'in':>10} {'out':>9} {'cache-rd':>12} "
          f"{'cache-wr':>11} {'usd':>9}")
    for (who, skill, model), c in rows:
        print(f"{who[:32]:32} {skill[:20]:20} {model[:24]:24} {int(c['calls']):6} {int(c['input_tokens']):10} "
              f"{int(c['output_tokens']):9} {int(c['cache_read_tokens']):12} {int(c['cache_creation_tokens']):11} "
              f"{c['cost_usd']:9.2f}")
    return 0 if n else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
