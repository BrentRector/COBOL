#!/usr/bin/env python3
"""Insert a new DEVLOG entry at the TOP, in bytes, without touching the other 36,000 lines.

    python scripts/prepend-devlog.py <entry.md> [<entry.md> ...]

Entries are inserted in the order given, so the LAST file named ends up nearest the top — pass them
oldest-first and the descending order comes out right.

⛔ WHY THIS EXISTS AND WHY IT WORKS IN BYTES. `DEVLOG.md` is CRLF and unnormalized. Any tool that reads it as text
and writes it back — `sed -i`, a naive Python `read_text`/`write_text`, most editors — re-encodes every line
ending and produces a 36,000-line diff around a twenty-line addition, which destroys the reviewability of the one
change that matters and makes `git blame` useless for the entry. So this reads bytes, splices bytes, and writes
bytes; the only lines in the diff are the ones added.

⛔ AND READ THE DIFFSTAT, NOT THE EXIT CODE. A successful run adds exactly the entry's own line count. Anything
else means the file was re-encoded, and the exit code will not tell you.
"""
from __future__ import annotations

import pathlib
import subprocess
import sys

DEVLOG = pathlib.Path(__file__).resolve().parents[1] / "DEVLOG.md"
#: The entry blocks start immediately after the ordering note, so the first `## Entry` IS the insertion point. The
#: file is unnormalized: a heading may follow an LF line end, so the anchor is the LF alone (`\r\n## Entry` skipped an
#: LF-preceded newest entry and inserted the w1034 entry BELOW 1927, 2026-10-07).
ANCHOR = b"\n## Entry "


def main(argv: list[str]) -> int:
    if not argv:
        sys.exit(__doc__)

    body = DEVLOG.read_bytes()
    at = body.find(ANCHOR)
    if at < 0:
        sys.exit("no '## Entry' heading found — DEVLOG.md is not in the expected shape")
    at += 1  # past the line end: keep the blank line that separates the ordering note from the first entry

    added = 0
    for name in argv:
        text = pathlib.Path(name).read_bytes()
        if not text.startswith(b"## Entry"):
            sys.exit(f"{name}: an entry must start with '## Entry NNN - YYYY-MM-DD HH:MM TZ - Title'")
        # Normalize the incoming entry to CRLF so it matches the file it is joining, then leave a blank line
        # after it. Doing this per-entry is what lets several be inserted in one pass.
        chunk = text.replace(b"\r\n", b"\n").replace(b"\n", b"\r\n").rstrip(b"\r\n") + b"\r\n\r\n"
        body = body[:at] + chunk + body[at:]
        at += len(chunk)
        added += chunk.count(b"\r\n")
        print(f"inserted {name} ({chunk.count(b'\r\n')} lines)")

    DEVLOG.write_bytes(body)
    print(f"\nexpected diffstat: +{added} / -0 on DEVLOG.md")
    diff = subprocess.run(["git", "diff", "--numstat", "--", "DEVLOG.md"],
                          cwd=DEVLOG.parent, capture_output=True, text=True).stdout.strip()
    print(f"actual  diffstat: {diff or '(not a git checkout)'}")
    if diff:
        ins, dele, _ = diff.split(None, 2)
        if dele != "0":
            print(f"\n⛔ {dele} line(s) DELETED — the file was re-encoded. Revert and investigate.")
            return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
