#!/usr/bin/env python3
"""train_measure.py — the gating policy's measurement, one record per lander train (kb/Work PB2515).

    python scripts/orchestrator/train_measure.py record --train 1036 --gating batched --started <ISO> --pushed <ISO> \
        --clusters 5 --landed 4 --ejected 1 --population-runs 2 --first-run red --attribution-min 14 \
        --interaction no --ci green [--fixed-in-train 0] [--note TEXT] [--replace]
    python scripts/orchestrator/train_measure.py summary [--since <ISO>]
    python scripts/orchestrator/train_measure.py --self-test

WHY. Batched gating is the gate policy (kb/Work PB2515; the owner's decision 2026-10-10, "Yes, permanently", after
a measured trial from 2026-10-07): an implementer gates leg 1 only, and the lander's whole-population train gate is
the population check for every cluster. It was decided on what it measured against the per-commit whole population
(kb/Work PB1708) — changes landed per hour, whole-population runs per train, red trains and their attribution time,
finishers created by ejection, and CI reds, which must stay ZERO (the CI invariant, kb/Work PB1957) — and every train
is still recorded, so a policy that stops paying shows in the numbers. Each lander records its train once it has
pushed (or given up), `--gating batched` while the implementer scope is `leg1` (the default) and `--gating whole`
while the owner has set it to `whole`. `summary` computes the comparison PB2515 quotes.

The records are one JSON object per line in `<coord>/train-measurements.jsonl`, outside every git worktree
(scripts/orchestrator/coord.py), so no branch switch or worktree removal loses one.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import pathlib
import shutil
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import coord  # noqa: E402

FILE = "train-measurements.jsonl"
GATINGS = ("batched", "whole")
SCHEMA = 1


def _when(text: str) -> dt.datetime:
    t = dt.datetime.fromisoformat(text.strip())
    if t.tzinfo is None:
        raise ValueError(f"{text!r} has no UTC offset (write e.g. 2026-10-08T09:15:00-07:00)")
    return t


def make_record(a: argparse.Namespace) -> dict:
    """One train, validated: every count consistent with the others, a red first run carries its attribution time."""
    started, pushed = _when(a.started), _when(a.pushed)
    if pushed <= started:
        raise ValueError(f"--pushed {a.pushed} is not after --started {a.started}")
    counts = {"clusters": a.clusters, "landed": a.landed, "ejected": a.ejected, "fixed_in_train": a.fixed_in_train,
              "population_runs": a.population_runs}
    if any(v < 0 for v in counts.values()) or a.clusters < 1 or a.population_runs < 1:
        raise ValueError(f"counts must be non-negative, with at least one cluster and one population run: {counts}")
    if a.landed + a.ejected > a.clusters:
        raise ValueError(f"landed {a.landed} + ejected {a.ejected} exceeds the train's {a.clusters} clusters")
    if a.first_run == "red" and a.attribution_min is None:
        raise ValueError("a red first run needs --attribution-min (minutes from the red verdict to its attribution)")
    if a.first_run == "green" and (a.attribution_min or a.interaction == "yes"):
        raise ValueError("a green first run has no attribution time and no interaction red")
    if a.landed and a.ci == "none":
        raise ValueError("a train that landed clusters went through CI: --ci green or red")
    return {"schema": SCHEMA, "train": a.train, "gating": a.gating, "started": started.isoformat(),
            "pushed": pushed.isoformat(), **counts, "first_run": a.first_run,
            "attribution_min": a.attribution_min, "interaction": a.interaction == "yes", "ci": a.ci,
            "note": a.note or ""}


def read_all(path: pathlib.Path) -> list[dict]:
    if not path.exists():
        return []
    out = []
    for n, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if line.strip():
            try:
                out.append(json.loads(line))
            except ValueError as e:
                raise ValueError(f"{path}:{n} is not JSON ({e})") from None
    return out


def record(path: pathlib.Path, rec: dict, replace: bool) -> None:
    existing = read_all(path)
    if any(r.get("train") == rec["train"] for r in existing):
        if not replace:
            raise ValueError(f"train {rec['train']} is already recorded in {path}; pass --replace to rewrite it")
        existing = [r for r in existing if r.get("train") != rec["train"]]
    lines = [json.dumps(r, sort_keys=True) for r in [*existing, rec]]
    tmp = path.with_name(f"{path.name}.{os.getpid()}.tmp")
    tmp.write_text("\n".join(lines) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def summarize(records: list[dict], since: dt.datetime | None = None) -> list[str]:
    """Per gating mode: changes landed per hour (landed clusters over the wall-clock window from the first train's
    start to the last train's push), whole-population runs per train, red first runs with their mean attribution time,
    interaction reds, finishers created by ejection, and CI reds."""
    lines = []
    for gating in GATINGS:
        rs = [r for r in records if r["gating"] == gating and (since is None or _when(r["started"]) >= since)]
        if not rs:
            lines.append(f"{gating}: no trains recorded")
            continue
        window_h = (max(_when(r["pushed"]) for r in rs) - min(_when(r["started"]) for r in rs)).total_seconds() / 3600
        landed = sum(r["landed"] for r in rs)
        reds = [r for r in rs if r["first_run"] == "red"]
        attribution = (f", mean attribution {sum(r['attribution_min'] for r in reds) / len(reds):.1f} min"
                       if reds else "")
        lines.append(
            f"{gating}: {len(rs)} train(s), {landed} change(s) landed in {window_h:.1f} h = {landed / window_h:.2f}/h; "
            f"whole-population runs per train {sum(r['population_runs'] for r in rs) / len(rs):.1f}; "
            f"red first runs {len(reds)}{attribution}, interaction reds {sum(r['interaction'] for r in rs)}; "
            f"finishers created by ejection {sum(r['ejected'] for r in rs)}; "
            f"fixed in the train {sum(r['fixed_in_train'] for r in rs)}; "
            f"CI reds {sum(r['ci'] == 'red' for r in rs)} (must be 0)")
    return lines


def parser() -> argparse.ArgumentParser:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true", help="prove the validation, the record file and the summary")
    ap.add_argument("--coord", help=f"the coordination directory (default: ${coord.ENV}, else {coord.DEFAULT})")
    sub = ap.add_subparsers(dest="action")
    r = sub.add_parser("record", help="append one train's measurement")
    r.add_argument("--train", required=True, help="the train label (e.g. 1036, 1036b)")
    r.add_argument("--gating", required=True, choices=GATINGS,
                   help="batched: its implementers gated leg 1 only (scope leg1, the default); whole: the per-commit "
                        "whole population (scope whole)")
    r.add_argument("--started", required=True, help="ISO time with offset: the lander was dispatched")
    r.add_argument("--pushed", required=True, help="ISO time with offset: push-main finished (or the lander gave up)")
    r.add_argument("--clusters", type=int, required=True, help="clusters the train was dispatched with")
    r.add_argument("--landed", type=int, required=True, help="clusters it landed")
    r.add_argument("--ejected", type=int, required=True, help="clusters ejected to a finisher")
    r.add_argument("--fixed-in-train", type=int, default=0, help="reds the lander fixed inside the train")
    r.add_argument("--population-runs", type=int, required=True,
                   help="whole-population gate runs the lander made (first run plus every re-run)")
    r.add_argument("--first-run", required=True, choices=("green", "red"), help="the train's first whole-population run")
    r.add_argument("--attribution-min", type=float, help="red first run: minutes from its verdict to its attribution")
    r.add_argument("--interaction", choices=("yes", "no"), default="no",
                   help="a red no single cluster reproduced alone (bisected)")
    r.add_argument("--ci", required=True, choices=("green", "red", "none"), help="CI's verdict on the pushed head")
    r.add_argument("--note", help="anything the numbers do not say")
    r.add_argument("--replace", action="store_true", help="rewrite an already recorded train")
    s = sub.add_parser("summary", help="the trial against the baseline")
    s.add_argument("--since", help="only trains started at or after this ISO time")
    return ap


#: The self-test's arms, by name (TrainMeasureDriftTests asserts each one ran and passed).
ARMS = ("refuses an inconsistent record", "refuses a train recorded twice and rewrites it with --replace",
        "summarizes changes landed per hour, population runs, red trains, ejections and CI reds per gating mode",
        "--since narrows the summary")


def self_test() -> int:
    d = pathlib.Path(tempfile.mkdtemp(prefix="train-measure-"))
    try:
        fails = _self_test(d / FILE)
    finally:
        shutil.rmtree(d, ignore_errors=True)
    for name in ARMS:
        bad = [f for f in fails if f.startswith(name)]
        print(f"  {'FAIL' if bad else 'PASS'}  {name}" + "".join(f"\n        {f}" for f in bad))
    print(f"=== train_measure SELF-TEST: {'PASS' if not fails else 'FAIL'} ({len(ARMS)} arms) ===")
    return 1 if fails else 0


def _self_test(path: pathlib.Path) -> list[str]:
    """The arms, each failure prefixed with its arm's name."""
    fails: list[str] = []
    refuse, twice, summary, since_arm = ARMS

    def args(*argv: str) -> argparse.Namespace:
        return parser().parse_args(["record", *argv])

    base = ["--train", "T1", "--gating", "batched", "--started", "2026-10-08T09:00:00-07:00",
            "--pushed", "2026-10-08T11:00:00-07:00", "--clusters", "5", "--landed", "4", "--ejected", "1",
            "--population-runs", "2", "--first-run", "red", "--attribution-min", "12", "--interaction", "no",
            "--ci", "green"]

    def refused(name: str, *override: str) -> None:
        argv = list(base)
        for k, v in zip(override[::2], override[1::2]):
            argv[argv.index(k) + 1] = v
        try:
            make_record(args(*argv))
            fails.append(f"{refuse}: accepted {name}")
        except ValueError:
            pass

    refused("pushed before started", "--pushed", "2026-10-08T08:00:00-07:00")
    refused("a time with no UTC offset", "--started", "2026-10-08T09:00:00")
    refused("landed + ejected above the clusters", "--landed", "5")
    refused("no population run", "--population-runs", "0")
    refused("a green first run with an attribution time", "--first-run", "green")
    try:
        make_record(args(*[a for a in base if a not in ("--attribution-min", "12")]))
        fails.append(f"{refuse}: accepted a red first run with no attribution time")
    except ValueError:
        pass

    record(path, make_record(args(*base)), replace=False)
    try:
        record(path, make_record(args(*base)), replace=False)
        fails.append(f"{twice}: accepted the same train twice")
    except ValueError:
        pass
    green = list(base)
    for k, v in (("--train", "T2"), ("--first-run", "green"), ("--landed", "5"), ("--ejected", "0"),
                 ("--population-runs", "1"), ("--started", "2026-10-08T11:00:00-07:00"),
                 ("--pushed", "2026-10-08T13:00:00-07:00")):
        green[green.index(k) + 1] = v
    green = [a for a in green if a not in ("--attribution-min", "12")]
    record(path, make_record(args(*green)), replace=False)
    whole = list(base)
    for k, v in (("--train", "B1"), ("--gating", "whole"), ("--population-runs", "7"), ("--ci", "red")):
        whole[whole.index(k) + 1] = v
    record(path, make_record(args(*whole)), replace=False)
    record(path, make_record(args(*whole[:-2], "--ci", "red", "--note", "rewritten")), replace=True)
    recs = read_all(path)
    if [r["train"] for r in recs] != ["T1", "T2", "B1"] or recs[-1]["note"] != "rewritten":
        fails.append(f"{twice}: the record file: {[(r['train'], r['note']) for r in recs]}")
    lines = summarize(recs)
    want = ["batched: 2 train(s), 9 change(s) landed in 4.0 h = 2.25/h; whole-population runs per train 1.5; "
            "red first runs 1, mean attribution 12.0 min, interaction reds 0; finishers created by ejection 1; "
            "fixed in the train 0; CI reds 0 (must be 0)",
            "whole: 1 train(s), 4 change(s) landed in 2.0 h = 2.00/h; whole-population runs per train 7.0; "
            "red first runs 1, mean attribution 12.0 min, interaction reds 0; finishers created by ejection 1; "
            "fixed in the train 0; CI reds 1 (must be 0)"]
    if lines != want:
        fails.append(f"{summary}:\n  got  {lines}\n  want {want}")
    since = summarize(recs, _when("2026-10-08T10:00:00-07:00"))
    if not since[0].startswith("batched: 1 train(s), 5 change(s)"):
        fails.append(f"{since_arm}: {since}")
    return fails


def main(argv: list[str]) -> int:
    a = parser().parse_args(argv)
    if a.self_test:
        return self_test()
    try:
        if a.action == "record":
            path = coord.coord_dir(a.coord) / FILE
            rec = make_record(a)
            record(path, rec, a.replace)
            print(f"train-measure: recorded train {rec['train']} ({rec['gating']}) in {path}")
            return 0
        if a.action == "summary":
            path = coord.coord_path(a.coord) / FILE
            for line in summarize(read_all(path), _when(a.since) if a.since else None):
                print(line)
            return 0
    except ValueError as e:
        print(f"train_measure.py: {e}", file=sys.stderr)
        return 2
    parser().print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
