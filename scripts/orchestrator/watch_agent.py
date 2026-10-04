#!/usr/bin/env python3
"""Render a Workflow subagent transcript as readable lines, following it while the agent works. Read-only; no tokens.

    python scripts/orchestrator/watch_agent.py <agent-*.jsonl> [--once]

Each model call prints once (a call's content blocks share one message id): the local time, the running call count,
the latest context size (input + cache read + cache write of that call), then each tool use as its name and a
120-character summary of its input, and assistant text as its first 200 characters. A file still being written is
read from the last complete line; a partial last line waits for its newline. --once renders what is there and exits.

Design: docs/rearchitecture/DESIGN-orchestrator-loop.md section 13 (watching agents).
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import sys
import time
from typing import Iterator

TOOL_KEYS = ("command", "file_path", "pattern", "path", "description", "skill", "query", "prompt", "url")


class State:
    def __init__(self) -> None:
        self.calls = 0
        self.context = 0
        self.seen: set[str] = set()
        self.buf = b""
        self.pos = 0


def summarize(inp: object, width: int = 120) -> str:
    if isinstance(inp, dict):
        lead = next((str(inp[k]) for k in TOOL_KEYS if k in inp and inp[k]), "")
        text = lead or json.dumps(inp, ensure_ascii=False)
    else:
        text = str(inp)
    text = " ".join(text.split())
    return text if len(text) <= width else text[:width - 1] + "…"


def local_time(ts: str | None) -> str:
    if not ts:
        return "--:--:--"
    try:
        return dt.datetime.fromisoformat(ts.replace("Z", "+00:00")).astimezone().strftime("%H:%M:%S")
    except ValueError:
        return ts[:8]


def render(line: str, st: State) -> list[str]:
    try:
        o = json.loads(line)
    except ValueError:
        return []
    if o.get("type") != "assistant":
        return []
    msg = o.get("message") or {}
    out = []
    mid = str(msg.get("id") or o.get("uuid") or "")
    u = msg.get("usage") or {}
    if mid not in st.seen:
        st.seen.add(mid)
        st.calls += 1
        st.context = sum(int(u.get(k) or 0) for k in ("input_tokens", "cache_read_input_tokens",
                                                      "cache_creation_input_tokens"))
    head = f"{local_time(o.get('timestamp'))}  #{st.calls:<4} ctx {st.context // 1000:>4}k"
    for c in msg.get("content") or []:
        if not isinstance(c, dict):
            continue
        if c.get("type") == "tool_use":
            out.append(f"{head}  {c.get('name', '?'):<10} {summarize(c.get('input'))}")
        elif c.get("type") == "text" and (c.get("text") or "").strip():
            out.append(f"{head}  {'text':<10} {summarize(c.get('text'), 200)}")
    return out


def feed(path: pathlib.Path, st: State) -> Iterator[str]:
    """Render every COMPLETE line appended since the last call; a partial last line stays buffered."""
    try:
        with open(path, "rb") as f:
            f.seek(st.pos)
            chunk = f.read()
    except FileNotFoundError:
        return
    st.pos += len(chunk)
    st.buf += chunk
    *lines, st.buf = st.buf.split(b"\n")
    for raw in lines:
        yield from render(raw.decode("utf-8", errors="replace"), st)


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("transcript")
    ap.add_argument("--once", action="store_true", help="render what is there and exit")
    ap.add_argument("--poll", type=float, default=0.5)
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    path, st = pathlib.Path(a.transcript), State()
    try:
        while True:
            for text in feed(path, st):
                print(text, flush=True)
            if a.once:
                return 0
            time.sleep(a.poll)
    except KeyboardInterrupt:
        return 0


if __name__ == "__main__":
    sys.exit(main())
