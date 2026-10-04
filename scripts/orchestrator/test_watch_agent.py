#!/usr/bin/env python3
"""Self-test for watch_agent.py: one count per model call (not per content block), the context size of the latest
call, tool summaries cut at 120 characters and text at 200, a partial last line held until its newline arrives, and
a file that does not exist yet.
Run: python scripts/orchestrator/test_watch_agent.py   (no submodule, no build)."""
import json
import pathlib
import subprocess
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import watch_agent as w  # noqa: E402

fails, checked = [], []


def check(name, got, want):
    checked.append(name)
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def asst(mid, blocks, cr=1000, cw=10, inp=2, ts="2026-10-04T18:34:44.623Z"):
    return json.dumps({"type": "assistant", "timestamp": ts, "message": {
        "id": mid, "content": blocks,
        "usage": {"input_tokens": inp, "cache_read_input_tokens": cr, "cache_creation_input_tokens": cw}}})


T = pathlib.Path(tempfile.mkdtemp(prefix="watch-test-")) / "agent-a1.jsonl"
st = w.State()
check("missing file renders nothing", list(w.feed(T, st)), [])

lines = [
    json.dumps({"type": "user", "message": {"content": "the brief"}}),
    asst("m1", [{"type": "thinking", "thinking": "hidden"}], cr=0, cw=54065),
    asst("m1", [{"type": "tool_use", "name": "Read", "input": {"file_path": "E:\\x\\msg.txt"}}], cr=0, cw=54065),
    asst("m2", [{"type": "text", "text": "word " * 100}], cr=172469, cw=264),
    asst("m3", [{"type": "tool_use", "name": "Bash", "input": {"command": "x" * 300, "description": "d"}}], cr=200000),
]
T.write_text("\n".join(lines[:4]) + "\n" + lines[4][:40], encoding="utf-8")   # the last line is still being written
out = list(w.feed(T, st))
check("one line per rendered block", len(out), 2)
check("tool line", out[0].split()[1:5], ["#1", "ctx", "54k", "Read"])
check("tool summary is the file path", out[0].endswith("E:\\x\\msg.txt"), True)
check("calls counted per message id", st.calls, 2)
check("context of the latest call", st.context, 172471 + 264)
check("text cut at 200", len(out[1].split("text", 1)[1].strip()), 200)
check("partial line held", st.buf.startswith(lines[4][:40].encode()), True)

with open(T, "a", encoding="utf-8") as f:
    f.write(lines[4][40:] + "\n")
out = list(w.feed(T, st))
check("completed line rendered once", len(out), 1)
check("tool summary cut at 120", len(out[0].split("Bash", 1)[1].strip()), 120)
check("third call", st.calls, 3)
check("nothing new renders nothing", list(w.feed(T, st)), [])
with open(T, "a", encoding="utf-8") as f:
    f.write("{not json\n")
check("a corrupt line is skipped", list(w.feed(T, st)), [])

r = subprocess.run([sys.executable, str(HERE / "watch_agent.py"), str(T), "--once"], capture_output=True, text=True,
                   encoding="utf-8")
check("CLI --once exit", r.returncode, 0)
check("CLI --once lines", len(r.stdout.splitlines()), 3)

for f in fails:
    print("FAIL:", f)
print(f"watch_agent self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
sys.exit(1 if fails else 0)
