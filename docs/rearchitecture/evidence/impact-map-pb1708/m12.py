"""PB1719 (PB1708 pivot M12) acceptance: the in-assembly leg filter on the REAL test hosts.

    python m12.py <work dir>          (from this directory, on a built tree: dotnet build CobolSharp.sln -c Debug)

Measures, on the three gated assemblies of THIS build (DESIGN-test-build-ci.md section 3.14.9, row M12):
  A. no handshake: every assembly's trx TEST DEFINITIONS equal its `--list-tests` as a multiset, every case passed;
  B. a full handshake: a plan from scripts/gate_plan.py (timings from A's trx), then each assembly's legs; each leg's
     definitions are exactly the cases gate_plan.leg_of gives that leg, the legs' union equals `--list-tests`, and
     each leg host wrote its identity record (digest, leg, MVID, hashes, the keys it runs = its trx's keys);
  C. every partial or stale handshake (one variable, two, a missing file, a digest mismatch, a bad leg) on a real
     host: every case Failed with the refusal message, a `Failed!` verdict line, exit 1.
Each `dotnet test` gets an environment with every COBOLNET_GATE_* variable scrubbed, then only what the arm sets.
"""
from __future__ import annotations

import hashlib
import json
import os
import subprocess
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
WT = HERE.parents[3]
sys.path.insert(0, str(WT / "scripts"))
import gate_plan  # noqa: E402

ASSEMBLIES = ["Characterization", "Unit", "Conformance"]
NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
GATE_VARS = ("COBOLNET_GATE_PLAN", "COBOLNET_GATE_LEG", "COBOLNET_GATE_PLAN_SHA256")
REFUSAL = "GATE ENVIRONMENT INCOMPLETE: "


def project(asm: str) -> str:
    return str(WT / "tests" / f"Cobol.Net.Tests.{asm}")


def env_with(**gate: str) -> dict[str, str]:
    env = {k: v for k, v in os.environ.items() if k not in GATE_VARS}
    env.update(gate)
    return env


def dotnet_test(asm: str, out: Path, name: str, env: dict[str, str]) -> tuple[int, str]:
    out.mkdir(parents=True, exist_ok=True)
    p = subprocess.run(["dotnet", "test", project(asm), "--no-build", "--logger", f"trx;LogFileName={name}.trx",
                        "--results-directory", str(out)], cwd=WT, env=env, capture_output=True, text=True,
                       encoding="utf-8", errors="replace")
    verdict = next((ln.strip() for ln in p.stdout.splitlines() if ln.strip().startswith(("Passed!", "Failed!"))),
                   "(no verdict line)")
    return p.returncode, verdict


def trx(path: Path) -> tuple[list[str], Counter, list[str]]:
    """(the test definitions' names, the result outcomes, the result messages)."""
    root = ET.parse(path).getroot()
    defs = [u.get("name") or "" for u in root.iter(NS + "UnitTest")]
    outcomes = Counter(r.get("outcome") for r in root.iter(NS + "UnitTestResult"))
    msgs = [m.text or "" for m in root.iter(NS + "Message")]
    return defs, outcomes, msgs


def main() -> int:
    work = Path(sys.argv[1]).resolve()
    work.mkdir(parents=True, exist_ok=True)
    head = subprocess.run(["git", "rev-parse", "--short=12", "HEAD"], cwd=WT, capture_output=True,
                          text=True).stdout.strip()
    base = subprocess.run(["git", "merge-base", "HEAD", "origin/main"], cwd=WT, capture_output=True,
                          text=True).stdout.strip()
    print(f"M12 acceptance on {head} (base {base[:12]})")
    ok = True

    def check(label: str, good: bool, detail: str = "") -> None:
        nonlocal ok
        ok &= good
        print(f"  {'PASS' if good else 'FAIL'}  {label}" + (f" — {detail}" if detail else ""))

    listings: dict[str, list[str]] = {}
    for asm in ASSEMBLIES:
        p = subprocess.run(["dotnet", "test", project(asm), "--no-build", "--list-tests"], cwd=WT, env=env_with(),
                           capture_output=True, text=True, encoding="utf-8", errors="replace")
        f = work / f"{asm}.list.txt"
        f.write_text(p.stdout, encoding="utf-8")
        listings[asm] = gate_plan.read_listing(f)
        print(f"{asm}: --list-tests discovered {len(listings[asm])} cases")

    print("A. no handshake")
    whole = work / "A-whole"
    for asm in ASSEMBLIES:
        rc, verdict = dotnet_test(asm, whole, asm, env_with())
        defs, outcomes, _ = trx(whole / f"{asm}.trx")
        check(f"{asm}: exit {rc}; {verdict}", rc == 0 and verdict.startswith("Passed!"))
        check(f"{asm}: definitions == --list-tests as a multiset ({len(defs)} vs {len(listings[asm])})",
              Counter(defs) == Counter(listings[asm]))
        print(f"      outcomes {dict(outcomes)}")

    print("B. a full handshake")
    plan_file = work / "B-legs" / "plan.json"
    plan_file.parent.mkdir(parents=True, exist_ok=True)
    cmd = [sys.executable, str(WT / "scripts" / "gate_plan.py"), "--out", str(plan_file), "--base", base,
           "--previous-run", str(whole)] + [a for asm in ASSEMBLIES for a in ("--list", f"{asm}={work / f'{asm}.list.txt'}")]
    gp = subprocess.run(cmd, cwd=WT, capture_output=True, text=True, encoding="utf-8")
    print("  " + "\n  ".join(gp.stderr.strip().splitlines()))
    digest = gp.stdout.strip().splitlines()[-1]
    check("the handshake digest is the SHA-256 of the plan's bytes",
          digest == hashlib.sha256(plan_file.read_bytes()).hexdigest())
    plan = json.loads(plan_file.read_text(encoding="utf-8"))
    for asm in ASSEMBLIES:
        legs = plan["assemblies"][asm]["legs"]
        ran: Counter = Counter()
        for leg in legs:
            rc, verdict = dotnet_test(asm, plan_file.parent, f"{asm}-leg{leg}",
                                      env_with(COBOLNET_GATE_PLAN=str(plan_file), COBOLNET_GATE_LEG=str(leg),
                                               COBOLNET_GATE_PLAN_SHA256=digest))
            defs, outcomes, _ = trx(plan_file.parent / f"{asm}-leg{leg}.trx")
            expected = Counter(d for d in listings[asm] if gate_plan.leg_of(plan, asm, d) == leg)
            check(f"{asm} leg {leg}: exit {rc}; {verdict}", rc == 0 and verdict.startswith("Passed!"))
            check(f"{asm} leg {leg}: definitions == the plan's leg-{leg} cases ({len(defs)} vs "
                  f"{sum(expected.values())})", Counter(defs) == expected)
            ran += Counter(defs)
            rec_path = plan_file.parent / f"leg-{leg}-{asm}.json"
            rec = json.loads(rec_path.read_text(encoding="utf-8")) if rec_path.exists() else {}
            check(f"{asm} leg {leg}: identity record {rec_path.name}",
                  rec.get("plan_sha256") == digest and rec.get("leg") == leg and rec.get("assembly") == asm
                  and rec.get("plan_content_sha256") == plan["sha256"] and bool(rec.get("test_assembly", {}).get("mvid"))
                  and len(rec.get("product_assemblies", {})) > 0 and rec.get("received") == len(listings[asm])
                  and Counter(rec.get("runs", [])) == Counter(gate_plan.name_key(d) for d in defs),
                  f"received {rec.get('received')}, runs {len(rec.get('runs', []))}, "
                  f"products {sorted(rec.get('product_assemblies', {}))}")
        check(f"{asm}: legs {legs} — the union of the legs' definitions == --list-tests",
              ran == Counter(listings[asm]))

    print("C. every partial or stale handshake refuses (Characterization's real host)")
    stale = work / "C-stale.json"
    stale.write_bytes(plan_file.read_bytes() + b" ")
    arms = {
        "plan only": env_with(COBOLNET_GATE_PLAN=str(plan_file)),
        "leg only": env_with(COBOLNET_GATE_LEG="1"),
        "digest only": env_with(COBOLNET_GATE_PLAN_SHA256=digest),
        "plan + leg": env_with(COBOLNET_GATE_PLAN=str(plan_file), COBOLNET_GATE_LEG="1"),
        "leg + digest": env_with(COBOLNET_GATE_LEG="2", COBOLNET_GATE_PLAN_SHA256=digest),
        "missing file": env_with(COBOLNET_GATE_PLAN=str(work / "absent.json"), COBOLNET_GATE_LEG="1",
                                 COBOLNET_GATE_PLAN_SHA256=digest),
        "digest mismatch (a stale plan)": env_with(COBOLNET_GATE_PLAN=str(stale), COBOLNET_GATE_LEG="1",
                                                   COBOLNET_GATE_PLAN_SHA256=digest),
        "bad leg": env_with(COBOLNET_GATE_PLAN=str(plan_file), COBOLNET_GATE_LEG="3",
                            COBOLNET_GATE_PLAN_SHA256=digest),
    }
    for i, (arm, env) in enumerate(arms.items()):
        rc, verdict = dotnet_test("Characterization", work / "C-refused", f"arm{i}", env)
        defs, outcomes, msgs = trx(work / "C-refused" / f"arm{i}.trx")
        refused = sum(1 for m in msgs if REFUSAL in m)
        check(f"{arm}: exit {rc}; {verdict}; outcomes {dict(outcomes)}; {refused} refusal messages",
              rc == 1 and verdict.startswith("Failed!") and set(outcomes) == {"Failed"} and refused == sum(outcomes.values()))
        print(f"      {next((m.strip().splitlines()[0] for m in msgs if REFUSAL in m), '(no message)')[:220]}")

    print("=== M12 ACCEPTANCE: " + ("GREEN" if ok else "RED") + " ===")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
