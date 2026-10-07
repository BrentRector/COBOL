#!/usr/bin/env python3
"""Is a publish of the owner's ledger artifact owed, and WHERE does this account publish it?
(docs/rearchitecture/DESIGN-orchestrator-loop.md section 14)

  ledger_state.py owed [--json]                 prints `owed <stamp> ...` or `current <stamp> ...`
  ledger_state.py mark-published [--url URL]    records that this account's artifact shows the current stamp (the
                                                attended session runs it right after its Artifact publish; --url is
                                                required the first time an account publishes, and replaces the URL)
  ledger_state.py url                           prints where this account publishes, or how to start (gen_ledger.py
                                                prints the same line after a render)
  --account NAME                                another account of model_rules.json than the one CLAUDE_CONFIG_DIR selects

The stamp is the page's own: the last commit that touched an input the page reports (gen_ledger.STAMP_PATHS, imported, so
the two can never disagree about what makes the page stale). A headless unit cannot publish (it has no Artifact tool:
probed 2026-10-04), so it only renders `<coord>/ledger.html`; the supervisor announces `owed` after every unit, and the
attended session publishes (owner 2026-10-04: "publish ledger each time") and marks it.

PER ACCOUNT (kb/Work PB2481): an artifact belongs to the claude.ai account that published it, and another account cannot
update it, so `<coord>/ledger-published.json` is `{"accounts": {"<name>": {"url", "stamp", "published_at"}}}`. A record
written before 2026-10-07 (top-level `stamp`, no account, no URL) belongs to no account: it is ignored, and the next
`mark-published --url` rewrites the file in the keyed shape.
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
import account  # noqa: E402
import coord  # noqa: E402
import gen_ledger  # noqa: E402

MARKER = "ledger-published.json"


class NoUrl(LookupError):
    """The account has never published the ledger and no --url names its artifact."""


def stamp(repo: pathlib.Path) -> str:
    r = subprocess.run(["git", "log", "-1", "--format=%h", "--", *gen_ledger.STAMP_PATHS], cwd=repo,
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    return r.stdout.strip() if r.returncode == 0 else ""


def published(cdir: pathlib.Path, acct_name: str) -> dict:
    """This account's record: {url, stamp, published_at}, or {} when it has never published."""
    return (coord.read_json(cdir / MARKER, {}).get("accounts") or {}).get(acct_name) or {}


def state(repo: pathlib.Path, cdir: pathlib.Path, acct_name: str) -> dict:
    now, rec = stamp(repo), published(cdir, acct_name)
    return {"state": "current" if now and now == rec.get("stamp") else "owed", "stamp": now,
            "published_stamp": rec.get("stamp", ""), "account": acct_name, "url": rec.get("url")}


def mark(cdir: pathlib.Path, acct_name: str, s: str, url: str | None, now: dt.datetime) -> dict:
    """Record that `acct_name`'s artifact (at `url`, or at the URL already recorded) shows stamp `s`."""
    data = coord.read_json(cdir / MARKER, {})
    accounts = dict(data.get("accounts") or {})
    url = url or (accounts.get(acct_name) or {}).get("url")
    if not url:
        raise NoUrl(f"account {acct_name} has no ledger artifact on record: publish a NEW artifact (another account's "
                    f"cannot be updated) and pass its URL: ledger_state.py mark-published --url <url>")
    accounts[acct_name] = {"url": url, "stamp": s, "published_at": now.isoformat()}
    coord.write_json(cdir / MARKER, {"accounts": accounts})
    return accounts[acct_name]


def publish_hint(cdir: pathlib.Path, acct_name: str) -> str:
    """The one line that says where this account publishes the rendered page."""
    url = published(cdir, acct_name).get("url")
    if url:
        return f"publish: Artifact tool, action publish, url {url} ({acct_name}'s artifact) — same url, no icon on redeploy"
    return (f"publish: no ledger artifact yet for {acct_name}: publish a NEW artifact (another account's cannot be updated), "
            f"then python scripts/orchestrator/ledger_state.py mark-published --url <its url>")


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("cmd", choices=("owed", "mark-published", "url"))
    ap.add_argument("--repo", default=str(coord.REPO))
    ap.add_argument("--coord")
    ap.add_argument("--account", help="a named account (default: the one CLAUDE_CONFIG_DIR selects)")
    ap.add_argument("--url", help="mark-published: the artifact this account published to")
    ap.add_argument("--json", action="store_true")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    repo, cdir = pathlib.Path(a.repo), coord.coord_dir(a.coord)
    try:
        name = account.resolve(a.account).name
    except account.UnknownAccount as e:
        print(f"ledger_state: {e}", file=sys.stderr)
        return 2
    if a.cmd == "url":
        print(publish_hint(cdir, name))
        return 0
    if a.cmd == "mark-published":
        s = stamp(repo)
        if not s:
            print("no stamp: not a repository with the ledger's inputs", file=sys.stderr)
            return 2
        try:
            rec = mark(cdir, name, s, a.url, dt.datetime.now(dt.timezone.utc).astimezone())
        except NoUrl as e:
            print(str(e), file=sys.stderr)
            return 2
        print(f"marked published: {s} for {name} at {rec['url']}")
        return 0
    st = state(repo, cdir, name)
    print(json.dumps(st) if a.json else
          f"{st['state']} {st['stamp']} (last published {st['published_stamp'] or 'never'} by {name}"
          f"{' to ' + st['url'] if st['url'] else ', no artifact yet for this account'})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
