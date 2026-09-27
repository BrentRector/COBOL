#!/usr/bin/env python3
"""test_leg_report.py — THE ONE RULE for what a gate prints from a `dotnet test` leg (kb/Work PB1573).

    python scripts/test_leg_report.py --name unit --log <file> --rc <exit code>   # print the report, gate on it
    python scripts/test_leg_report.py --self-test                                 # prove a failure's text arrives

⛔ A GATE MAY TRIM A PASSING LEG. IT MAY NEVER TRIM A FAILING ONE. `build-local.ps1` used to keep only the lines of
a leg's output matching `^(Passed!|Failed!)|error|\\[FAIL\\]`, and only the last 20 of those. vstest prints a failing
test as a block —

      Failed CobolNet.Tests.Unit.SomeTests.SomeFact [21 s]
      Error Message:
       System.UnauthorizedAccessException : Access to the path '…' is denied.
      Stack Trace:
         at …

— and the filter kept the `Failed` header and the literal `Error Message:` label (PowerShell's `-match` is
case-insensitive, so `error` matched the label) while DROPPING the message under it and every stack frame. Every red
arrived as a test name over an empty message: the PB1564 grammar-diagram red and a pb64t5 red under load were both
reported with no cause, and the verdict-evidence rule (a red is attributed by name AND cause) could not be met from
the gate's own log. `build-local.sh` carried the same filter case-SENSITIVELY, so it dropped the label as well, and
`guard-fast.sh` printed the last six matching lines of the legacy unit and integration legs — in CI, where the full
log in `$TMPDIR` dies with the runner.

The rule, written here once for every caller:

  GREEN  exit 0 AND a `Passed!` verdict line → print the verdict line and where the full log is. Trimming is safe:
         nothing failed, and the verdict carries the counts.
  RED    anything else — a non-zero exit, a `Failed!` verdict, or NO verdict line at all (a filter that matched
         nothing: vstest exits 0 on an empty run) → print the leg's COMPLETE output, untrimmed and unfiltered,
         between two banner lines, then the verdict (or the no-verdict reason). A red's evidence is the whole log;
         any selection over it is a guess about which lines matter, and the guess was wrong.

The log file is always kept, so a GREEN leg's detail is still one `cat` away.

Exit codes: 0 GREEN · 1 RED · 2 the reporter itself could not read the log (a gate treats that as RED too — a
reporter that could not look is not a green, feedback_green_gates_arent_evidence).

`--self-test` drives the rule against a REAL vstest failure transcript (the one golden lane #2's lander captured,
DEVLOG 1700, with a planted token for its message) and proves, arm by arm, that the message and the stack frames
reach the printed report — and, as the calibration that makes the proof mean something, that BOTH retired filters
drop that same message. `tests/Cobol.Net.Tests.Unit/TestLegReportDriftTests.cs` runs it in every Unit leg and
asserts every gate script routes its legs through this file.
"""
from __future__ import annotations

import argparse
import os
import pathlib
import re
import subprocess
import sys
import tempfile

#: vstest's per-assembly summary line. The same shape `filter_population.py` and `battery.sh` key on.
VERDICT = re.compile(r"^(Passed!|Failed!)")


def verdict_of(lines: list[str]) -> str | None:
    """The LAST verdict line — one `dotnet test` invocation over one project prints exactly one."""
    found = [line for line in lines if VERDICT.match(line)]
    return found[-1] if found else None


def render(name: str, lines: list[str], rc: int, log: str) -> tuple[list[str], bool]:
    """(the lines a gate prints for this leg, whether the leg is RED). The ONE implementation — the CLI and the
    self-test both call it, so the self-test cannot pass against a rule the gate no longer applies."""
    verdict = verdict_of(lines)
    red = rc != 0 or verdict is None or verdict.startswith("Failed!")
    out: list[str] = []
    if red:
        out.append(f"----- {name}: RED (exit {rc}) — the leg's COMPLETE output, untrimmed ({len(lines)} lines; "
                   f"also kept at {log}) -----")
        out.extend(lines)
        out.append(f"----- {name}: end of the leg's output -----")
    if verdict is None:
        out.append(f"{name}: NO VERDICT LINE — the filter matched no test (a run must assert its population)")
    else:
        out.append(verdict)
        if not red:
            out.append(f"{name}: full log at {log}")
    return out, red


def read_log(path: pathlib.Path) -> list[str]:
    # ⚠ `backslashreplace`, never `replace` or `ignore`: a byte the console wrote in another code page is still
    # PRINTED (escaped), so no part of a failure's text is dropped on the way to the reader.
    return path.read_bytes().decode("utf-8", errors="backslashreplace").splitlines()


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────
#: A planted token, standing in for the assertion message a real red carries. Nothing else in the transcript
#: contains it, so "the token reached the report" is exactly "the failure's message reached the report".
PLANT = "PB1573-PLANTED-FAILURE-MESSAGE-7f3c"
FRAME = "at CobolNet.Tests.Unit.ConflictMarkerDriftTests.Sweep"

#: The real transcript of a failing Unit leg (golden lane #2 batch 3, DEVLOG 1700), message replaced by PLANT.
FAILED_TRANSCRIPT = f"""\
Test run for E:\\COBOL\\tests\\Cobol.Net.Tests.Unit\\bin\\Debug\\net10.0\\Cobol.Net.Tests.Unit.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:40.84]     CobolNet.Tests.Unit.ConflictMarkerDriftTests.TheSweepActuallyReadsTheTrackedTree [FAIL]
  Failed CobolNet.Tests.Unit.ConflictMarkerDriftTests.TheSweepActuallyReadsTheTrackedTree [21 s]
  Error Message:
   System.UnauthorizedAccessException : {PLANT}
  Stack Trace:
     at Microsoft.Win32.SafeHandles.SafeFileHandle.CreateFile(String fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options)
   at System.IO.File.OpenRead(String path)
   {FRAME}(String root, IEnumerable`1 relativePaths) in E:\\COBOL\\tests\\Cobol.Net.Tests.Unit\\ConflictMarkerDriftTests.cs:line 232
   at CobolNet.Tests.Unit.ConflictMarkerDriftTests.TheSweepActuallyReadsTheTrackedTree() in E:\\COBOL\\tests\\Cobol.Net.Tests.Unit\\ConflictMarkerDriftTests.cs:line 124

Failed!  - Failed:     1, Passed: 29189, Skipped:     0, Total: 29190, Duration: 2 m 47 s - Cobol.Net.Tests.Unit.dll (net10.0)
""".splitlines()

PASSED_TRANSCRIPT = """\
Test run for E:\\COBOL\\tests\\Cobol.Net.Tests.Unit\\bin\\Debug\\net10.0\\Cobol.Net.Tests.Unit.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
Passed!  - Failed:     0, Passed: 29190, Skipped:     0, Total: 29190, Duration: 2 m 41 s - Cobol.Net.Tests.Unit.dll (net10.0)
""".splitlines()

#: A filter that matched nothing: vstest prints no verdict line and EXITS 0.
EMPTY_TRANSCRIPT = """\
Test run for E:\\COBOL\\tests\\Cobol.Net.Tests.Conformance\\bin\\Debug\\net10.0\\Cobol.Net.Tests.Conformance.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
No test matches the given testcase filter `FullyQualifiedName~NoSuchTest` in E:\\COBOL\\tests\\Cobol.Net.Tests.Conformance\\bin\\Debug\\net10.0\\Cobol.Net.Tests.Conformance.dll
""".splitlines()


def retired_ps1_filter(lines: list[str]) -> list[str]:
    """build-local.ps1's filter until PB1573 — PowerShell `-match` is case-INSENSITIVE, then `-Last 20`."""
    return [l for l in lines if re.search(r"^(Passed!|Failed!)|error|\[FAIL\]", l, re.IGNORECASE)][-20:]


def retired_sh_filter(lines: list[str]) -> list[str]:
    """build-local.sh's twin — `grep -E` is case-SENSITIVE, then `tail -20`."""
    return [l for l in lines if re.search(r"^(Passed!|Failed!)|error|\[FAIL\]", l)][-20:]


def self_test() -> int:
    failures: list[str] = []

    def case(label: str, ok: bool, detail: str = "") -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f" — {detail}" if detail and not ok else ""))
        if not ok:
            failures.append(label)

    print("=== test_leg_report --self-test ===")
    # Calibration FIRST: the planted message is exactly what the retired filters lost. If either kept it, this
    # self-test could not tell the defect from the fix and would prove nothing.
    case("calibration: the retired build-local.ps1 filter DROPS the planted message",
         not any(PLANT in l for l in retired_ps1_filter(FAILED_TRANSCRIPT)))
    case("calibration: the retired build-local.sh filter DROPS the planted message",
         not any(PLANT in l for l in retired_sh_filter(FAILED_TRANSCRIPT)))

    out, red = render("unit", FAILED_TRANSCRIPT, 1, "unit.log")
    case("fires RED on a failing leg", red)
    case("fires MESSAGE: the failing test's message reaches the report", any(PLANT in l for l in out))
    case("fires STACK: the failing test's stack frames reach the report", any(FRAME in l for l in out))
    case("fires WHOLE: every line of a red leg is printed, in order",
         out[1:1 + len(FAILED_TRANSCRIPT)] == FAILED_TRANSCRIPT)

    out, red = render("unit", FAILED_TRANSCRIPT, 0, "unit.log")
    case("fires RED on a Failed! verdict even at exit 0", red and any(PLANT in l for l in out))

    out, red = render("conformance", EMPTY_TRANSCRIPT, 0, "conformance.log")
    case("fires NO-VERDICT: an empty filtered run is RED, with its whole output",
         red and any("NO VERDICT LINE" in l for l in out) and any("No test matches" in l for l in out))

    out, red = render("unit", PASSED_TRANSCRIPT, 0, "unit.log")
    case("silent on GREEN: a passing leg prints its verdict and the log path, nothing else",
         not red and out == [PASSED_TRANSCRIPT[-1], "unit: full log at unit.log"])

    out, red = render("unit", PASSED_TRANSCRIPT, 3, "unit.log")
    case("fires RED on a non-zero exit behind a Passed! verdict (a crashed host after the summary)",
         red and len(out) == len(PASSED_TRANSCRIPT) + 3)

    # The CLI arm the gates actually call: the exit code IS the verdict.
    with tempfile.TemporaryDirectory() as tmp:
        log = pathlib.Path(tmp, "unit.log")
        log.write_text("\n".join(FAILED_TRANSCRIPT) + "\n", encoding="utf-8")
        r = subprocess.run([sys.executable, __file__, "--name", "unit", "--log", str(log), "--rc", "1"],
                           capture_output=True, text=True, encoding="utf-8", errors="backslashreplace")
        case("fires CLI: a red log exits 1 and prints the planted message",
             r.returncode == 1 and PLANT in r.stdout, f"rc={r.returncode}\n{r.stdout}{r.stderr}")
        log.write_text("\n".join(PASSED_TRANSCRIPT) + "\n", encoding="utf-8")
        r = subprocess.run([sys.executable, __file__, "--name", "unit", "--log", str(log), "--rc", "0"],
                           capture_output=True, text=True, encoding="utf-8", errors="backslashreplace")
        case("silent CLI: a green log exits 0", r.returncode == 0, f"rc={r.returncode}\n{r.stdout}{r.stderr}")
        r = subprocess.run([sys.executable, __file__, "--name", "unit", "--log", str(pathlib.Path(tmp, "absent.log")),
                            "--rc", "0"], capture_output=True, text=True, encoding="utf-8", errors="backslashreplace")
        case("fires UNREADABLE: a log the reporter cannot read exits 2, never 0", r.returncode == 2,
             f"rc={r.returncode}\n{r.stdout}{r.stderr}")

    print(f"SELF-TEST: {'PASS' if not failures else 'FAIL — ' + '; '.join(failures)}")
    return 0 if not failures else 1


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001 — a stream without reconfigure keeps its encoding; the report still prints
        pass
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true", help="prove a failing leg's message reaches the report")
    ap.add_argument("--name", help="the leg's name, as the gate's verdict lines call it")
    ap.add_argument("--log", help="the file holding the leg's complete `dotnet test` output")
    ap.add_argument("--rc", type=int, help="the leg's `dotnet test` exit code")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if args.name is None or args.log is None or args.rc is None:
        ap.error("--name, --log and --rc are all required (or --self-test)")
    path = pathlib.Path(args.log)
    try:
        lines = read_log(path)
    except OSError as e:
        print(f"{args.name}: ⛔ the leg's log could not be read ({path}: {e}) — the leg is UNMEASURED, and an "
              f"unmeasured leg is RED")
        return 2
    out, red = render(args.name, lines, args.rc, os.fspath(path))
    for line in out:
        print(line)
    return 1 if red else 0


if __name__ == "__main__":
    sys.exit(main())
