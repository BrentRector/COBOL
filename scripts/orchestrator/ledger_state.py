#!/usr/bin/env python3
"""Is a publish of the owner's conformance-ledger artifact owed? (docs/rearchitecture/DESIGN-orchestrator-loop.md section 14)

  ledger_state.py owed              prints `owed <stamp>` or `current <stamp>`; --json for a machine reading
  ledger_state.py mark-published    records that the page for the current stamp is live (the attended session runs this
                                    right after its Artifact publish)

The stamp is the page's own: the last commit that touched an input the page reports (gen_ledger.STAMP_PATHS, imported, so
the two can never disagree about what makes the page stale). A headless unit cannot publish (it has no Artifact tool:
probed 2026-10-04), so it only renders `<coord>/ledger.html`; the supervisor announces `owed` after every unit, and the
attended session publishes (owner 2026-10-04: "publish ledger each time") and marks it.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent / "spec"))
import coord  # noqa: E402
import gen_ledger  # noqa: E402

MARKER = "ledger-published.json"


def stamp(repo: pathlib.Path) -> str:
    r = subprocess.run(["git", "log", "-1", "--format=%h", "--", *gen_ledger.STAMP_PATHS], cwd=repo,
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    return r.stdout.strip() if r.returncode == 0 else ""


def state(repo: pathlib.Path, cdir: pathlib.Path) -> dict:
    now = stamp(repo)
    published = coord.read_json(cdir / MARKER, {}).get("stamp", "")
    return {"state": "current" if now and now == published else "owed", "stamp": now, "published_stamp": published}


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("cmd", choices=("owed", "mark-published"))
    ap.add_argument("--repo", default=str(coord.REPO))
    ap.add_argument("--coord")
    ap.add_argument("--json", action="store_true")
    a = ap.parse_args(argv)
    repo, cdir = pathlib.Path(a.repo), coord.coord_dir(a.coord)
    if a.cmd == "mark-published":
        s = stamp(repo)
        if not s:
            print("no stamp: not a repository with the ledger's inputs", file=sys.stderr)
            return 2
        coord.write_json(cdir / MARKER, {"stamp": s, "published_at": dt.datetime.now(dt.timezone.utc).astimezone().isoformat()})
        print(f"marked published: {s}")
        return 0
    st = state(repo, cdir)
    print(json.dumps(st) if a.json else f"{st['state']} {st['stamp']} (last published {st['published_stamp'] or 'never'})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
