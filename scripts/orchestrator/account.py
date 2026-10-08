#!/usr/bin/env python3
"""The Claude account a script runs as: the ONE resolver of Claude Code's config dir, and the accounts table.

    python scripts/orchestrator/account.py [--json] [--account NAME | --config-dir DIR] [--repo DIR]
    python scripts/orchestrator/account.py --field config_dir|child_config_dir|name|project_dir|account_uuid|email
    python scripts/orchestrator/account.py --config-dir DIR --seed-plan | --seed-check   # kb/Work PB2480
    python scripts/orchestrator/account.py [--config-dir DIR] --skill-review              # kb/Work PB2598
    python scripts/orchestrator/account.py --self-test

Design: docs/rearchitecture/DESIGN-orchestrator-loop.md section 2.1 (kb/Work PB2478, PB2479). Owner 2026-10-07: the fix
lane runs from TWO Claude accounts at once (the operator on account 2, the Mythos work on account 1), so the account is
a PARAMETER of every coordination tool, never an assumption.

- The CONFIG DIR is `CLAUDE_CONFIG_DIR` when it is set, else `~/.claude`. Claude Code keeps its own files there:
  `settings.json`, `projects/<repo key>/<session>` transcripts, `skills/`, `plugins/`.
- The GLOBAL CONFIG, which names the signed-in account (`oauthAccount.accountUuid`), is `<config dir>/.claude.json`
  when `CLAUDE_CONFIG_DIR` is set and `~/.claude.json` when it is not (measured 2026-10-07 on both accounts).
- The ACCOUNTS TABLE is `model_rules.json` `accounts`: per account its name, config dir (null = the default) and weekly
  reset, plus any `quota` keys that differ from the shared `quota`. A config dir the table does not name is an error,
  never a silent default: a new account is one row in the table.
- LAUNCHING a process as an account (`child_config_dir`): a row that names a config dir runs with `CLAUDE_CONFIG_DIR`
  set to it; the default row runs with the variable UNSET. Setting it to `~/.claude` is NOT the same: Claude Code would
  then look for its global config in `~/.claude/.claude.json` and find no sign-in.
- SEEDING a named account's config dir (`model_rules.json` `accounts.seed`, kb/Work PB2480): `--seed-plan` prints what
  `scripts/account-profile.ps1` copies, junctions and flags; `--seed-check` lists what is missing (tooling_check.py runs
  it at every session start). The default account is the source and needs no seed.
- The weekly /skill-doctor REVIEW (kb/Work PB2598) covers a SKILL SET, not an account: the command reports what each
  skill costs and how often it is used "so you can decide which ones to turn off" (code.claude.com/docs/en/skills), and
  the pruning it drives edits the skills and plugins the seed copies. So `skill_review` answers "was THIS account's
  skill set reviewed within the week?" from every account's stamp: a review under an account whose skill set contains
  this one's covers it, which is how a freshly seeded account inherits its source's review instead of re-asking.
- TELEMETRY is per MACHINE, not per account: every account's settings export to the one sink on 127.0.0.1:4318, which
  writes `~/.claude/telemetry/<UTC day>.jsonl` (`otlp_sink.OUT_DIR`). Each `api_request` event carries the account id
  (`usage_report.ACCOUNT_ATTR`, `user.account_uuid`), and a consumer that needs one account's spend filters on it.
"""
from __future__ import annotations

import argparse
import dataclasses
import datetime
import json
import os
import pathlib
import re
import sys
from typing import Any, Mapping

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import coord  # noqa: E402

ENV = "CLAUDE_CONFIG_DIR"
# The owner's /skill-doctor review: a stamp file in the reviewing account's config dir, touched after the review; its
# mtime is the review's time. It is due again SKILL_REVIEW_DAYS later (kb/Work R49 item 11, PB2598).
SKILL_REVIEW_STAMP = "cobolsharp-skill-doctor.stamp"
SKILL_REVIEW_DAYS = 7


class UnknownAccount(LookupError):
    """The config dir (or the --account name) is not in model_rules.json `accounts`."""


@dataclasses.dataclass(frozen=True)
class Account:
    name: str
    config_dir: pathlib.Path
    explicit: bool               # the row names a config dir: CLAUDE_CONFIG_DIR is set to it (else the default, unset)
    weekly_reset: dict[str, Any]
    quota: dict[str, Any]        # the shared quota with this account's overrides applied

    @property
    def global_config(self) -> pathlib.Path:
        return self.config_dir / ".claude.json" if self.explicit else pathlib.Path.home() / ".claude.json"

    def _oauth(self) -> dict[str, Any]:
        try:
            data = json.loads(self.global_config.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            return {}
        return data.get("oauthAccount") or {} if isinstance(data, dict) else {}

    @property
    def uuid(self) -> str | None:
        """The signed-in account's id from the global config, which telemetry carries as `user.account_uuid`."""
        return self._oauth().get("accountUuid") or None

    @property
    def email(self) -> str | None:
        """The signed-in account's email: what the `meter` unit matches against the usage page it reads."""
        return self._oauth().get("emailAddress") or None

    def project_dir(self, repo: pathlib.Path | str) -> pathlib.Path:
        """Where Claude Code keeps this repository's session transcripts: projects/<path with ':' '\\' '/' as '-'>."""
        return self.config_dir / "projects" / re.sub(r"[:\\/]", "-", str(repo))

    @property
    def child_config_dir(self) -> str:
        """The CLAUDE_CONFIG_DIR a child process must carry to run as this account; "" = the variable must be UNSET."""
        return str(self.config_dir) if self.explicit else ""

    def as_json(self, repo: pathlib.Path | str | None = None) -> dict[str, Any]:
        out = {"name": self.name, "config_dir": str(self.config_dir), "explicit": self.explicit,
               "child_config_dir": self.child_config_dir,
               "global_config": str(self.global_config), "account_uuid": self.uuid, "email": self.email, "weekly_reset": self.weekly_reset}
        if repo is not None:
            out["project_dir"] = str(self.project_dir(repo))
        return out


def default_config_dir() -> pathlib.Path:
    return pathlib.Path.home() / ".claude"


def config_dir(env: Mapping[str, str] | None = None) -> pathlib.Path:
    """`CLAUDE_CONFIG_DIR` or `~/.claude`: the one rule every script that reads Claude's own files uses."""
    v = (os.environ if env is None else env).get(ENV)
    return pathlib.Path(os.path.expanduser(v)) if v else default_config_dir()


def _same(a: pathlib.Path, b: pathlib.Path) -> bool:
    return os.path.normcase(os.path.abspath(a)) == os.path.normcase(os.path.abspath(b))


def _table(rules: dict[str, Any]) -> list[dict[str, Any]]:
    rows = rules.get("accounts", {}).get("list") or []
    if not rows:
        raise UnknownAccount("model_rules.json has no `accounts.list`")
    return rows


def _make(row: dict[str, Any], cdir: pathlib.Path, explicit: bool, rules: dict[str, Any]) -> Account:
    return Account(name=row["name"], config_dir=cdir, explicit=explicit, weekly_reset=row["weekly_reset"],
                   quota={**rules["quota"], **row.get("quota", {})})


def _row_dir(row: dict[str, Any]) -> pathlib.Path:
    return pathlib.Path(os.path.expanduser(row["config_dir"])) if row.get("config_dir") else default_config_dir()


def current(rules: dict[str, Any] | None = None, env: Mapping[str, str] | None = None) -> Account:
    """The account this process runs as: the table row whose config dir is `config_dir(env)`."""
    rules = rules or coord.rules()
    env = os.environ if env is None else env
    cdir = config_dir(env)
    for row in _table(rules):
        if _same(_row_dir(row), cdir):
            return _make(row, _row_dir(row), bool(row.get("config_dir")), rules)
    raise UnknownAccount(f"the Claude config dir {cdir} is not in model_rules.json accounts.list "
                         f"(known: {', '.join(r['name'] for r in _table(rules))}); add a row for it")


def by_name(name: str, rules: dict[str, Any] | None = None) -> Account:
    """A named account from the table (the operator estimating the other account's week)."""
    rules = rules or coord.rules()
    for row in _table(rules):
        if row["name"] == name:
            return _make(row, _row_dir(row), bool(row.get("config_dir")), rules)
    raise UnknownAccount(f"no account named {name!r} in model_rules.json accounts.list "
                         f"(known: {', '.join(r['name'] for r in _table(rules))})")


def resolve(name: str | None = None, rules: dict[str, Any] | None = None,
            env: Mapping[str, str] | None = None) -> Account:
    return by_name(name, rules) if name else current(rules, env)


def default_account(rules: dict[str, Any] | None = None) -> Account:
    """The account of the default config dir: the source every named account is seeded from."""
    rules = rules or coord.rules()
    return current(rules, {})


def main_checkout(repo: pathlib.Path = coord.REPO) -> pathlib.Path:
    """The main working tree of `repo` (a linked worktree's sessions still keep their memory under the main one's key)."""
    import subprocess  # noqa: PLC0415
    r = subprocess.run(["git", "-C", str(repo), "rev-parse", "--path-format=absolute", "--git-common-dir"],
                       capture_output=True, text=True)
    return pathlib.Path(r.stdout.strip()).parent if r.returncode == 0 and r.stdout.strip() else repo


def seed_plan(acct: Account, rules: dict[str, Any], repo: pathlib.Path) -> dict[str, Any]:
    """What `acct`'s config dir must carry, as concrete paths: {copy: [{from, to}], junction: [{link, target}],
    flags: {path, set}, never}. Empty for the default account, which is the source."""
    seed = rules["accounts"]["seed"]
    src = default_account(rules)
    if not acct.explicit:
        return {"account": acct.name, "source": True, "copy": [], "junction": [], "flags": None, "never": seed["never"]}
    key = re.sub(r"[:\\/]", "-", str(main_checkout(repo)))
    return {"account": acct.name, "source": False,
            "copy": [{"from": str(src.config_dir / c), "to": str(acct.config_dir / c)} for c in seed["copy"]],
            "junction": [{"link": str(acct.config_dir / j.format(repo_key=key)),
                          "target": str(src.config_dir / j.format(repo_key=key))} for j in seed["junction"]],
            "flags": {"path": str(acct.global_config), "set": seed["flags"]}, "never": seed["never"]}


def _is_junction_to(link: pathlib.Path, target: pathlib.Path) -> bool:
    is_link = getattr(os.path, "isjunction", lambda p: False)(link) or os.path.islink(link)
    return is_link and _same(pathlib.Path(os.path.realpath(link)), pathlib.Path(os.path.realpath(target)))


def seed_problems(acct: Account, rules: dict[str, Any], repo: pathlib.Path) -> list[str]:
    """What `acct`'s config dir lacks of its seed, or []. Never reads credentials."""
    plan = seed_plan(acct, rules, repo)
    bad = [f"missing {c['to']} (from {c['from']})" for c in plan["copy"] if not pathlib.Path(c["to"]).exists()]
    bad += [f"{j['link']} is not a junction to {j['target']}" for j in plan["junction"]
            if not _is_junction_to(pathlib.Path(j["link"]), pathlib.Path(j["target"]))]
    if plan["flags"]:
        try:
            cfg = json.loads(pathlib.Path(plan["flags"]["path"]).read_text(encoding="utf-8"))
        except (OSError, ValueError):
            cfg = None
        if not isinstance(cfg, dict):
            bad.append(f"{plan['flags']['path']} is missing or unreadable (sign the account in: /login)")
        else:
            bad += [f"{plan['flags']['path']}: {k} is {cfg.get(k)!r}, want {v!r}"
                    for k, v in plan["flags"]["set"].items() if cfg.get(k) != v]
    return bad


def skill_set(acct: Account) -> frozenset[str]:
    """What /skill-doctor reviews for `acct`: the user skills in `<config dir>/skills` and the plugins its user
    `settings.json` enables. The project's own `.claude/skills` are the same for every account and are not part of it."""
    try:
        skills = {f"skill:{p.name}" for p in (acct.config_dir / "skills").iterdir() if p.is_dir()}
    except OSError:
        skills = set()
    try:
        settings = json.loads((acct.config_dir / "settings.json").read_text(encoding="utf-8"))
    except (OSError, ValueError):
        settings = {}
    enabled = settings.get("enabledPlugins") if isinstance(settings, dict) else None
    plugins = {f"plugin:{k}" for k, v in enabled.items() if v} if isinstance(enabled, dict) else set()
    return frozenset(skills | plugins)


@dataclasses.dataclass(frozen=True)
class SkillReview:
    by: str              # the account whose stamp records the review
    stamp: pathlib.Path
    age_days: int


def skill_review(acct: Account, rules: dict[str, Any], now: datetime.datetime | None = None) -> SkillReview | None:
    """The newest /skill-doctor review younger than SKILL_REVIEW_DAYS that covers `acct`'s whole skill set, or None
    when the review is due. A review covers `acct` when the reviewing account's skill set CONTAINS `acct`'s: the owner
    pruned that set, and `acct` runs no skill outside it. The reviewer's set is read as it is now, the same reading an
    account's own stamp has always had. Every account in the table is a candidate (kb/Work PB2598): a freshly seeded
    account whose copied skills were reviewed under its source is not asked again."""
    now = now or datetime.datetime.now()
    mine = skill_set(acct)
    found: list[SkillReview] = []
    for row in _table(rules):
        other = _make(row, _row_dir(row), bool(row.get("config_dir")), rules)
        stamp = other.config_dir / SKILL_REVIEW_STAMP
        try:
            age = (now - datetime.datetime.fromtimestamp(stamp.stat().st_mtime)).days
        except OSError:
            continue
        if age < SKILL_REVIEW_DAYS and mine <= skill_set(other):
            found.append(SkillReview(other.name, stamp, age))
    return min(found, key=lambda r: r.age_days, default=None)


def _self_test() -> int:
    import tempfile
    fails, checked = [], []

    def check(what, got, want):
        checked.append(what)
        if got != want:
            fails.append(f"{what}: got {got!r}, want {want!r}")

    home = pathlib.Path.home()
    rules = coord.rules()
    check("unset -> ~/.claude", config_dir({}), home / ".claude")
    check("set -> that dir", config_dir({ENV: r"C:\x\.claude-acct2"}), pathlib.Path(r"C:\x\.claude-acct2"))
    a1 = current(rules, {})
    check("unset -> the default account", (a1.name, a1.explicit), ("account1", False))
    check("default global config is ~/.claude.json", a1.global_config, home / ".claude.json")
    check("account 1 resets Sunday 03:00", (a1.weekly_reset["weekday"], a1.weekly_reset["hour"]), (6, 3))
    a2 = current(rules, {ENV: str(home / ".claude-acct2")})
    check("acct2 dir -> account2", (a2.name, a2.explicit), ("account2", True))
    check("account 2 resets Saturday 10:00", (a2.weekly_reset["weekday"], a2.weekly_reset["hour"]), (5, 10))
    check("explicit global config is inside the dir", a2.global_config, home / ".claude-acct2" / ".claude.json")
    check("by name", by_name("account2", rules).config_dir, home / ".claude-acct2")
    check("project dir key", a2.project_dir(r"E:\COBOL"), home / ".claude-acct2" / "projects" / "E--COBOL")
    x1 = current(rules, {ENV: str(home / ".claude")})
    check("CLAUDE_CONFIG_DIR=~/.claude is account1, launched with the variable unset", (x1.name, x1.child_config_dir),
          ("account1", ""))
    check("account2 is launched with its dir", a2.child_config_dir, str(home / ".claude-acct2"))
    try:
        current(rules, {ENV: r"Z:\nowhere\.claude-x"})
        check("unknown dir refused", "no error", "UnknownAccount")
    except UnknownAccount:
        check("unknown dir refused", True, True)
    with tempfile.TemporaryDirectory() as t:
        d = pathlib.Path(t)
        (d / ".claude.json").write_text(json.dumps({"oauthAccount": {"accountUuid": "u-1", "emailAddress": "a@b"}}), encoding="utf-8")
        row = {"name": "probe", "config_dir": str(d), "weekly_reset": {"weekday": 0, "hour": 0, "minute": 0, "tz": "UTC"},
               "quota": {"weekly_cap_pct": 50}}
        r2 = dict(rules, accounts={"list": [row]})
        p = current(r2, {ENV: str(d)})
        check("uuid and email read from the global config", (p.uuid, p.email), ("u-1", "a@b"))
        check("per-account quota override", (p.quota["weekly_cap_pct"], p.quota["daily_pct"]),
              (50, rules["quota"]["daily_pct"]))
        (d / ".claude.json").write_text("{not json", encoding="utf-8")
        check("unreadable global config -> no uuid", p.uuid, None)
    _skill_review_cases(check)
    for f in fails:
        print("FAIL:", f)
    print(f"account self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
    return 1 if fails else 0


def _skill_review_cases(check) -> None:
    """kb/Work PB2598, over temporary config dirs: a seeded account inherits its source's review, and only that."""
    import tempfile
    now = datetime.datetime(2026, 10, 7, 23, 47)
    with tempfile.TemporaryDirectory() as t:
        root = pathlib.Path(t)
        src, seeded = root / "src", root / "seeded"

        def give(d: pathlib.Path, skills: list[str], plugins: dict[str, bool]) -> None:
            d.mkdir(parents=True, exist_ok=True)
            for name in skills:
                (d / "skills" / name).mkdir(parents=True, exist_ok=True)
            (d / "settings.json").write_text(json.dumps({"enabledPlugins": plugins}), encoding="utf-8")

        def stamp(d: pathlib.Path, days_ago: float) -> None:
            (d / SKILL_REVIEW_STAMP).touch()
            t0 = (now - datetime.timedelta(days=days_ago)).timestamp()
            os.utime(d / SKILL_REVIEW_STAMP, (t0, t0))

        reset = {"weekday": 0, "hour": 0, "minute": 0, "tz": "UTC"}
        rules = {"quota": {}, "accounts": {"list": [{"name": "src", "config_dir": str(src), "weekly_reset": reset},
                                                    {"name": "seeded", "config_dir": str(seeded), "weekly_reset": reset}]}}
        give(src, ["humanizer", "synced"], {"csharp-lsp@x": True, "github@x": True, "off@x": False})
        give(seeded, ["humanizer", "synced"], {"csharp-lsp@x": True, "github@x": True})
        acct = current(rules, {ENV: str(seeded)})

        def got(who: Account = acct):
            r = skill_review(who, rules, now)
            return r and (r.by, r.age_days)

        check("PB2598: no stamp anywhere -> due", got(), None)
        stamp(src, 5.2)
        check("PB2598: a seeded copy of the set reviewed 5 days ago under its source -> covered", got(), ("src", 5))
        check("PB2598: the source is covered by its own review", got(current(rules, {ENV: str(src)})), ("src", 5))
        stamp(seeded, 1.5)
        check("PB2598: the newest covering review wins", got(), ("seeded", 1))
        (seeded / SKILL_REVIEW_STAMP).unlink()
        give(seeded, ["humanizer", "synced", "extra"], {"csharp-lsp@x": True, "github@x": True})
        check("PB2598: a skill outside the reviewed set -> due", got(), None)
        (seeded / "skills" / "extra").rmdir()
        give(seeded, ["humanizer", "synced"], {"csharp-lsp@x": True, "github@x": True, "off@x": True})
        check("PB2598: a plugin the reviewed set has disabled -> due", got(), None)
        give(seeded, ["humanizer"], {"github@x": True})
        check("PB2598: a subset of the reviewed set is covered", got(), ("src", 5))
        stamp(src, 7.0)
        check("PB2598: a review 7 days old is due again", got(), None)


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--json", action="store_true")
    who = ap.add_mutually_exclusive_group()
    who.add_argument("--account", help="a named account from the table instead of the one CLAUDE_CONFIG_DIR selects")
    who.add_argument("--config-dir", help="the account whose config dir this is (orchestrate.ps1 -ConfigDir)")
    ap.add_argument("--repo", help="also print the repository's transcript dir (project_dir)")
    ap.add_argument("--field", help="print one field of the JSON form, bare (for PowerShell callers)")
    ap.add_argument("--seed-plan", action="store_true", help="print what account-profile.ps1 makes the dir carry")
    ap.add_argument("--seed-check", action="store_true", help="list what the dir lacks of its seed; exit 1 if anything")
    ap.add_argument("--skill-review", action="store_true",
                    help="say which account's /skill-doctor review covers this account's skill set; exit 1 if it is due")
    ap.add_argument("--rules", help="a model_rules.json to read instead of the repository's (a test seam)")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    if a.self_test:
        return _self_test()
    rules = coord.rules(pathlib.Path(a.rules) if a.rules else None)
    try:
        acct = resolve(a.account, rules, env={ENV: a.config_dir} if a.config_dir else None)
    except UnknownAccount as e:
        print(f"account: {e}", file=sys.stderr)
        return 2
    repo = pathlib.Path(a.repo) if a.repo else coord.REPO
    if a.seed_plan:
        print(json.dumps(seed_plan(acct, rules, repo), indent=1))
        return 0
    if a.seed_check:
        bad = seed_problems(acct, rules, repo)
        print("\n".join(bad) if bad else f"{acct.name}: seeded ({acct.config_dir})")
        return 1 if bad else 0
    if a.skill_review:
        r = skill_review(acct, rules)
        print(f"{acct.name}: reviewed {r.age_days} day(s) ago under {r.by} ({r.stamp})" if r
              else f"{acct.name}: the /skill-doctor review is due (no review in {SKILL_REVIEW_DAYS} days covers its skill set)")
        return 0 if r else 1
    out = acct.as_json(a.repo)
    if a.field:
        if a.field not in out:
            print(f"account: no field {a.field!r} (have {', '.join(out)})", file=sys.stderr)
            return 2
        v = out[a.field]
        print("" if v is None else (json.dumps(v) if isinstance(v, (dict, list)) else v))
    elif a.json:
        print(json.dumps(out, indent=1))
    else:
        print(f"{acct.name}: config dir {acct.config_dir}, account {acct.uuid or 'unknown (not signed in?)'}, "
              f"week resets {acct.weekly_reset}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
