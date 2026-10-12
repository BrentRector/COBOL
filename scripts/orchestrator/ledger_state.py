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
the two can never disagree about what makes the page stale), read on MAIN (`--main-ref`, default `origin/main`, fetched
first), never on the checkout's HEAD (kb/Work PB2987): every landing is pushed from a lander's own worktree, so the main
checkout the supervisor passes as `--repo` stays at whatever train someone last fast-forwarded it to, and a stamp read
there said `current` for a train that had landed. The rendered page is still the checkout's tree, so `mark-published`
refuses unless the checkout's stamp IS main's (the page published describes main), and `owed` names a stale checkout so
the publisher fast-forwards it before rendering. A headless unit cannot publish (it has no Artifact tool:
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


MAIN_REF = "origin/main"


class NoMain(LookupError):
    """The main ref names no commit that touches a ledger input (a wrong --main-ref, or not this repository)."""


def _git(repo: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *args], cwd=repo, capture_output=True, text=True, encoding="utf-8", errors="replace")


def stamp(repo: pathlib.Path, rev: str = "HEAD") -> str:
    """The last commit at or before `rev` that touched a ledger input, abbreviated; "" when none or `rev` is unknown."""
    r = _git(repo, "log", "-1", "--format=%h", rev, "--", *gen_ledger.STAMP_PATHS)
    return r.stdout.strip() if r.returncode == 0 else ""


def refresh(repo: pathlib.Path, ref: str) -> str:
    """Fetch the remote branch `ref` names (`<remote>/<branch>`) so its stamp is main's now, not at the last fetch.
    Returns "" on success, else the reason; a ref naming no configured remote is a local ref and needs no fetch."""
    remote, sep, branch = ref.partition("/")
    if not sep or remote not in _git(repo, "remote").stdout.split():
        return ""
    r = _git(repo, "fetch", "--quiet", remote, branch)
    return "" if r.returncode == 0 else (r.stderr.strip().splitlines() or [f"git fetch exit {r.returncode}"])[-1]


def published(cdir: pathlib.Path, acct_name: str) -> dict:
    """This account's record: {url, stamp, published_at}, or {} when it has never published."""
    return (coord.read_json(cdir / MARKER, {}).get("accounts") or {}).get(acct_name) or {}


def state(repo: pathlib.Path, cdir: pathlib.Path, acct_name: str, ref: str = MAIN_REF, fetch: bool = True) -> dict:
    """Owed or current against MAIN's stamp, plus the checkout's own stamp (the tree gen_ledger.py renders)."""
    fetch_error = refresh(repo, ref) if fetch else ""
    now = stamp(repo, ref)
    if not now:
        raise NoMain(f"{ref} names no commit touching a ledger input in {repo}"
                     + (f" (fetch failed: {fetch_error})" if fetch_error else ""))
    rec, checkout = published(cdir, acct_name), stamp(repo)
    return {"state": "current" if now == rec.get("stamp") else "owed", "stamp": now, "main_ref": ref,
            "checkout_stamp": checkout, "checkout_current": checkout == now, "fetch_error": fetch_error,
            "published_stamp": rec.get("stamp", ""), "account": acct_name, "url": rec.get("url")}


def stale_checkout_line(repo: pathlib.Path, st: dict) -> str:
    """What the publisher must do first when the checkout renders another page than main's; "" when it is main's."""
    if st["checkout_current"]:
        return ""
    return (f"the checkout {repo} renders the ledger at {st['checkout_stamp'] or 'nothing'}, not {st['main_ref']}'s "
            f"{st['stamp']}: fast-forward it first (git -C {repo} merge --ff-only {st['main_ref']}), then render")


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
    ap.add_argument("--main-ref", default=MAIN_REF, help="the ref whose stamp is the truth (default origin/main, fetched)")
    ap.add_argument("--no-fetch", action="store_true", help="read --main-ref as it stands, without fetching it")
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
    try:
        st = state(repo, cdir, name, a.main_ref, fetch=not a.no_fetch)
    except NoMain as e:
        print(f"ledger_state: {e}", file=sys.stderr)
        return 2
    if a.cmd == "mark-published":
        # The page was rendered from the checkout: marking main's stamp published while the checkout is behind (or
        # ahead) would record a page that does not describe main as current (kb/Work PB2987).
        if not st["checkout_current"]:
            print(f"ledger_state: not marked: {stale_checkout_line(repo, st)}, publish, and mark again", file=sys.stderr)
            return 2
        try:
            rec = mark(cdir, name, st["stamp"], a.url, dt.datetime.now(dt.timezone.utc).astimezone())
        except NoUrl as e:
            print(str(e), file=sys.stderr)
            return 2
        print(f"marked published: {st['stamp']} for {name} at {rec['url']}")
        return 0
    stale = stale_checkout_line(repo, st)
    print(json.dumps(st) if a.json else
          f"{st['state']} {st['stamp']} (last published {st['published_stamp'] or 'never'} by {name}"
          f"{' to ' + st['url'] if st['url'] else ', no artifact yet for this account'}"
          f"{'; ' + stale if stale else ''}{'; fetch failed: ' + st['fetch_error'] if st['fetch_error'] else ''})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
