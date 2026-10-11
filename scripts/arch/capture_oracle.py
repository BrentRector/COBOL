#!/usr/bin/env python3
"""capture_oracle.py — record the architecture review's behavior-neutrality oracle (kb/Work PB2116;
docs/rearchitecture/DESIGN-architecture-review.md §4 items 1 and 2).

For every compile the file-driven suites perform — the conformance corpus (positive goldens at their edition, every
negative fixture at each edition its `*> reject-at:` header names), the NIST CCVS golden run and its INV-1 continuity
cells, and the version-matrix construct samples at every edition and severity axis their theories compile — it
captures the emitted C# and the diagnostic stream (outcome, compile-time output, every error and warning with its
code, severity, line, column and message). The population and the compiles are the SUITES' OWN: they live in
`tests/Cobol.Net.Tests.Conformance/ArchOracle.cs`, enumerated from the same row sources and compiled through the same
option builders the theories call, and `ArchOracleDriftTests` holds every row source enrolled. This script only builds
and runs `tests/Cobol.Net.ArchOracle` (the process that hosts that code outside xunit) and writes the manifest.

OUTPUT
  TestResults/arch-oracle/<label>/            every case's `<id>.g.cs` and `<id>.diag.txt` (gitignored; diff locally)
  TestResults/arch-oracle/<label>/manifest.json   one line per case: id → [SHA-256 of the C# or null, SHA-256 of the
                                              diagnostics]; <label> is the commit's first 12 hex digits, `-dirty`
                                              appended when the work tree has changes
  --record also writes docs/rearchitecture/evidence/arch-oracle/<commit12>.manifest.json — THE recorded baseline —
  and removes the previous one (git history keeps it), so that directory always holds exactly one manifest. A dirty
  tree is refused: a baseline names a commit.

INPUTS (kb/Work PB2885). A manifest also records `inputs`: the repository paths whose content the capture depends on,
DERIVED, never listed by hand — (a) the host project's MSBuild closure (`code_inputs`: every project its
ProjectReferences reach, the files the toolchain probes for by walking up from each project directory —
Directory.Build.props/.targets, Directory.Packages.props, global.json, NuGet.config — and every existing path an
Include or Import of those files names outside the project directory, such as tests/_shared), and (b) the data the row
sources read, which the host writes to `inputs.txt` from each row source's DataRoots (ArchOracle.DataInputs, bound to
the same members its readers open). `scripts/orchestrator/landing_oracle.py` reads it at every landing (push-main.sh,
exit 7): a landing whose inputs changed after the baseline's commit is refused until it is re-recorded.

DETERMINISM. The same commit captures the same manifest run after run, and on Windows and on Linux but for the
cases whose diagnostic names a COPY text found by a case-insensitive probe (19 NIST continuity cells at the first
baseline): the manifest records its platform, and a wave compares captures of one platform. The C# side writes the repository
root and each scratch directory as `<repo>` / `<scratch>` with forward slashes, normalizes line endings, and masks the
WHEN-COMPILED stamp of a compilation that recorded reading the compile clock; this side scrubs every COBOL* variable
from the environment (a NIST edition override or a directive's environment read would otherwise reach the output),
switches the compiled-program cache OFF (the oracle observes the compiler, never a stored result), and runs from the
repository root (the default COBOL library searches the working directory).

USAGE
  python scripts/arch/capture_oracle.py [--record] [--no-build] [--jobs N]
"""
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import time
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
HOST_PROJECT = REPO / "tests" / "Cobol.Net.ArchOracle" / "Cobol.Net.ArchOracle.csproj"
RESULTS = REPO / "TestResults" / "arch-oracle"
RECORDED = REPO / "docs" / "rearchitecture" / "evidence" / "arch-oracle"
SCHEMA = 1
# The file system decides one thing the compiler reports: a COPY text found by a case-insensitive probe is named as
# probed (`K1FDA.CPY` on Windows, `K1FDA.cpy` on Linux), so a manifest records its platform and a wave compares
# captures of one platform (DESIGN-architecture-review.md §4.1).
PLATFORM = "windows" if sys.platform.startswith("win") else sys.platform


def git(*args: str) -> str:
    return subprocess.run(["git", "-C", str(REPO), *args], check=True, capture_output=True, text=True).stdout


def commit_state() -> tuple[str, bool]:
    """HEAD's sha and whether the work tree differs from it (tracked changes or untracked files) anywhere but
    `.claude/`, the agent harness's own settings, which no compile reads."""
    return (git("rev-parse", "HEAD").strip(),
            git("status", "--porcelain", "--", ".", ":(exclude).claude").strip() != "")


def host_dll(configuration: str) -> Path:
    found = sorted((HOST_PROJECT.parent / "bin" / configuration).glob("*/Cobol.Net.ArchOracle.dll"))
    if len(found) != 1:
        sys.exit(f"capture_oracle: expected exactly one built Cobol.Net.ArchOracle.dll under "
                 f"{HOST_PROJECT.parent / 'bin' / configuration}, found {len(found)} (build it, or drop --no-build)")
    return found[0]


def oracle_environment() -> dict[str, str]:
    env = {k: v for k, v in os.environ.items() if not k.upper().startswith("COBOL")}
    env["COBOLNET_COMPILE_CACHE"] = "off"
    return env


# The files the .NET toolchain discovers by walking UP from a project directory (MSBuild's Directory.Build.* and
# central package management, the SDK's global.json, NuGet's config). Matched case-insensitively, as the tools do.
PROBED = ("directory.build.props", "directory.build.targets", "directory.packages.props", "global.json",
          "nuget.config")
WILDCARD = re.compile(r"[*?]")


def _expand(value: str, this_file: Path, project: Path) -> Path | None:
    """An Include/Import value as a path, with the two directory properties MSBuild authors write; None when it still
    holds an unexpanded property, item transform or function (nothing on disk can be named by it)."""
    v = (value.strip().replace("$(MSBuildThisFileDirectory)", str(this_file.parent) + "/")
         .replace("$(MSBuildProjectDirectory)", str(project.parent)).replace("\\", "/"))
    if not v or "$(" in v or "@(" in v or "%(" in v or "$([" in v:
        return None
    parts = Path(v).parts
    for i, part in enumerate(parts):          # a wildcard names its directory
        if WILDCARD.search(part):
            parts = parts[:i]
            break
    if not parts:
        return None
    p = Path(*parts)
    return (p if p.is_absolute() else this_file.parent / p).resolve()


def code_inputs(host: Path | None = None, repo: Path = REPO) -> list[str]:
    """The host project's MSBuild closure as repository-relative paths (see INPUTS in the module docstring)."""
    import xml.etree.ElementTree as ET
    repo = repo.resolve()
    projects: list[Path] = [(host or HOST_PROJECT).resolve()]
    seen: set[Path] = set()
    found: set[Path] = set()
    while projects:
        project = projects.pop()
        if project in seen or not project.is_file():
            continue
        seen.add(project)
        found.add(project.parent)
        probed = []
        d = project.parent
        while repo in (d, *d.parents):
            probed += [e for e in d.iterdir() if e.is_file() and e.name.lower() in PROBED]
            if d == repo:
                break
            d = d.parent
        found.update(probed)
        for xml_file in [project, *[p for p in probed if p.suffix.lower() in (".props", ".targets")]]:
            for element in ET.parse(xml_file).getroot().iter():
                tag = element.tag.rsplit("}", 1)[-1]
                for value in filter(None, (element.get("Include") or element.get("Project") or "").split(";")):
                    path = _expand(value, xml_file, project)
                    if path is None or repo not in path.parents or not path.exists():
                        continue
                    if tag == "ProjectReference":
                        projects.append(path)
                    else:
                        found.add(path)
    rel = sorted({p.relative_to(repo).as_posix() for p in found})
    # A path inside another input adds nothing: keep the outermost.
    return [p for p in rel if not any(p.startswith(q + "/") for q in rel if q != p)]


def write_manifest(path: Path, commit: str, dirty: bool, rows: list[list[str]], inputs: list[str]) -> None:
    """One case per line, ordinal id order, so two manifests diff line by line."""
    population: dict[str, int] = {}
    for _, pop, _, _ in rows:
        population[pop] = population.get(pop, 0) + 1
    lines = ["{",
             f'  "schema": {SCHEMA},',
             f'  "commit": {json.dumps(commit)},',
             f'  "dirty": {json.dumps(dirty)},',
             f'  "platform": {json.dumps(PLATFORM)},',
             '  "inputs": [' + ", ".join(json.dumps(i) for i in inputs) + "],",
             f'  "cases_total": {len(rows)},',
             '  "population": {' + ", ".join(f"{json.dumps(k)}: {v}" for k, v in sorted(population.items())) + "},",
             '  "cases": {']
    for i, (case_id, _, csharp, diagnostics) in enumerate(rows):
        cs = "null" if csharp == "-" else json.dumps(csharp)
        lines.append(f"    {json.dumps(case_id)}: [{cs}, {json.dumps(diagnostics)}]" + ("," if i < len(rows) - 1 else ""))
    lines += ["  }", "}", ""]
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes("\n".join(lines).encode("utf-8"))


def capture(*, build: bool = True, jobs: int | None = None, configuration: str = "Debug") -> Path:
    """Capture HEAD (plus any work-tree changes); returns the manifest path under TestResults/arch-oracle/."""
    commit, dirty = commit_state()
    label = commit[:12] + ("-dirty" if dirty else "")
    out = RESULTS / label
    if build:
        r = subprocess.run(["dotnet", "build", str(HOST_PROJECT), "-c", configuration, "-nologo", "-v", "quiet"],
                           cwd=REPO)
        if r.returncode != 0:
            sys.exit(f"capture_oracle: building {HOST_PROJECT.relative_to(REPO)} failed (exit {r.returncode})")
    if out.exists():
        shutil.rmtree(out)
    out.mkdir(parents=True)
    command = ["dotnet", str(host_dll(configuration)), "capture", "--out", str(out)]
    if jobs:
        command += ["--jobs", str(jobs)]
    started = time.monotonic()
    r = subprocess.run(command, cwd=REPO, env=oracle_environment())
    elapsed = time.monotonic() - started
    if r.returncode != 0:
        sys.exit(f"capture_oracle: the capture failed (exit {r.returncode}); nothing was recorded")
    rows = [line.split("\t") for line in (out / "cases.tsv").read_text(encoding="utf-8").splitlines() if line]
    if any(len(row) != 4 for row in rows):
        sys.exit("capture_oracle: cases.tsv has a malformed line")
    data_inputs = [line for line in (out / "inputs.txt").read_text(encoding="utf-8").splitlines() if line]
    if not data_inputs:
        sys.exit("capture_oracle: the host wrote no data inputs (inputs.txt); nothing was recorded")
    inputs = code_inputs()
    inputs += [d for d in data_inputs if not any(d == c or d.startswith(c + "/") for c in inputs)]
    manifest = out / "manifest.json"
    write_manifest(manifest, commit, dirty, rows, sorted(inputs))
    print(f"capture_oracle: {len(rows)} cases of {label} in {elapsed:.0f} s -> {manifest.relative_to(REPO).as_posix()}")
    return manifest


def record(manifest: Path) -> Path:
    data = json.loads(manifest.read_text(encoding="utf-8"))
    if data["dirty"]:
        sys.exit("capture_oracle: --record refuses a dirty work tree — a baseline names a commit; commit first")
    target = RECORDED / f"{data['commit'][:12]}.manifest.json"
    for old in RECORDED.glob("*.manifest.json"):
        if old != target:
            old.unlink()
    RECORDED.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(manifest, target)
    print(f"capture_oracle: recorded {target.relative_to(REPO).as_posix()} ({target.stat().st_size:,} bytes)")
    return target


def main() -> int:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--record", action="store_true",
                        help="also write the recorded baseline under docs/rearchitecture/evidence/arch-oracle/")
    parser.add_argument("--no-build", action="store_true", help="run the already-built host")
    parser.add_argument("--jobs", type=int, help="parallel compiles (default: the processor count)")
    args = parser.parse_args()
    manifest = capture(build=not args.no_build, jobs=args.jobs)
    if args.record:
        record(manifest)
    return 0


if __name__ == "__main__":
    sys.exit(main())
