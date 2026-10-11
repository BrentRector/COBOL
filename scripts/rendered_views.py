#!/usr/bin/env python3
"""rendered_views.py — THE REGISTRY OF RENDERED VIEWS: every generated index the tree does NOT hold (kb/Work PB3026).

    python scripts/rendered_views.py                    # render every light view to its path (gitignored, or outside the tree)
    python scripts/rendered_views.py drift-rules        # render the named view(s) only (the only way to render a heavy one)
    python scripts/rendered_views.py --check            # CI's `audits` job and the gate's audits: every view renders,
                                                        #   none is tracked, every in-tree one is gitignored
    python scripts/rendered_views.py --list             # name · output · generator
    python scripts/rendered_views.py --self-test

⛔ THE RULE (DESIGN-test-build-ci.md §3.17.4; owner decisions 2026-10-11 via kb/Work PB3015): THE TREE HOLDS SOURCES,
VIEWS ARE RENDERED. A committed file that every landing touching its inputs REGENERATES is a guaranteed collision once
landings are queued pull requests: two PRs that each add a drift test both rewrite `docs/DRIFT_RULES.md`'s count line,
and the second is bounced. A generated file nobody regenerates is the opposite failure: the probe printed
"kb/Conformance burn-down is STALE" for days. A view that is rendered on demand can be neither — there is nothing to
commit and nothing to go stale — so each is gitignored and its generator renders it when asked.

What is a VIEW here, and what is not: a view is DERIVED from tracked sources by a script that needs nothing outside the
tree (or the coordination directory), so it can be rendered on demand — the drift-rule index, the conformance
burn-down, the diagnostics table, the ledger page. These stay TRACKED, on purpose, and are not views: a SOURCE file
the build compiles (`*.g.cs`, `*.Table.cs`: a clone must build with `dotnet build` and no pre-step; each is pinned by
its own drift test), DATA extracted from the licensed standard (`spec-rule-catalog.json`, the reserved-word tables:
the generator needs the private PDF, which CI does not have), and a view DEFINITION (`kb/Work.base` is an Obsidian
query over the notes, not generated content).

ONE REGISTRY, ONE RULE (rule 4: no hand list per consumer). `VIEWS` below is the only place a view is named. The
gate's audits, CI's `audits` job and `RenderedViewsDriftTests` (Unit) all call `--check`, which enforces, per view:
  (a) NOT TRACKED — `git ls-files` finds nothing at its output (the drift test the design names: tracked ∩ outputs = ∅);
  (b) GITIGNORED when its output is inside the tree — otherwise a render leaves the checkout dirty, and a dirty main
      checkout sends the orchestrator loop to `resume`;
  (c) NOTHING UNREGISTERED — no tracked `docs/**.md` or `kb/**.md` declares itself generated in its first lines (the sweep
      for the next case: a new generated index added by copying the old habit is refused until it has a row);
  (d) RENDERS — the generator's own `--check` renders it in memory and exits non-zero when its sources no longer
      produce a view (a drift test deleted its `<summary>`, an inventory row lost a field, the plan cannot be read).
A view rendered by a C# test (it reads the compiled catalogue) names that test instead, and the test is its (d).

ABSENT IS NEVER STALE: a view is not compared with a copy on disk, because nothing is promised about a copy. The
session probe renders the burn-down itself, so the copy a session reads is the one it just made.
"""
from __future__ import annotations

import argparse
import dataclasses
import pathlib
import re
import subprocess
import sys
import tempfile

ROOT = pathlib.Path(__file__).resolve().parents[1]


@dataclasses.dataclass(frozen=True)
class View:
    """One rendered view.

    ``out`` is the repo-relative output (a file, or a directory when it ends in ``/``), or ``None`` when the generator
    renders OUTSIDE the tree on its own (the ledger lives in the coordination directory). ``render`` is the command
    that writes it (``{out}`` is replaced by the path); ``check`` is the generator's own render-without-writing check,
    or ``None`` when a test is the check (``checked_by`` names it). ``heavy`` marks a render that needs a build or an
    external tool (the diagnostics table is rendered by a C# test; the grammar diagrams need java): a bare
    ``rendered_views.py`` skips those, and naming one renders it."""
    name: str
    out: str | None
    render: tuple[str, ...]
    check: tuple[str, ...] | None
    checked_by: str = ""
    heavy: bool = False


def _py(script: str, *args: str) -> tuple[str, ...]:
    return (sys.executable, script, *args)


#: ⛔ THE ONE REGISTRY. A new generated index is ONE row here, a `.gitignore` entry for its output, and a `--check`
#: on its generator; `RenderedViewsDriftTests` then holds it to the rule with nothing else to remember.
VIEWS: tuple[View, ...] = (
    View("drift-rules", "docs/DRIFT_RULES.md",
         _py("scripts/spec/drift_rules.py", "--out", "{out}"),
         _py("scripts/spec/drift_rules.py", "--check")),
    View("conformance-burn-down", "kb/Conformance/",
         _py("scripts/spec/gen_conformance_notes.py", "--out", "{out}"),
         _py("scripts/spec/gen_conformance_notes.py", "--check")),
    View("diagnostics", "docs/DIAGNOSTICS.md",
         ("pwsh", "-NoProfile", "-File", "scripts/gen-diagnostics-doc.ps1"),
         None, checked_by="DiagnosticRegistryDriftTests.DiagnosticsDoc_RendersTheWholeCatalogue", heavy=True),
    View("vault-reference", "kb/Reference/",
         ("pwsh", "-NoProfile", "-File", "scripts/gen-vault-reference.ps1"),
         None, checked_by="VaultReferenceGeneratorDriftTests"),
    View("grammar-diagrams", "kb/Grammar/",
         ("pwsh", "-NoProfile", "-File", "scripts/gen-grammar-diagrams.ps1"),
         None, checked_by="GrammarDiagramGeneratorDriftTests", heavy=True),
    View("ledger", None,
         _py("scripts/spec/gen_ledger.py"),
         _py("scripts/spec/gen_ledger.py", "--check")),
)


def is_view_output(path: str) -> bool:
    """Whether ``path`` is a registered view's output (or lies under a directory view): a path a note may NAME and a
    reader may OPEN although the tree does not hold it, because it is rendered on demand (``work.py check`` asks)."""
    return any(v.out is not None and (path == v.out or (v.out.endswith("/") and path.startswith(v.out)))
               for v in VIEWS)


def _git(root: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *args], cwd=root, capture_output=True, text=True, encoding="utf-8")


def _ls_files(root: pathlib.Path, *args: str, sep: str | None = None) -> list[str]:
    """``git ls-files``, or an error: a failed listing is not an empty one (a rule that finds nothing tracked because git
    could not run would pass for the wrong reason)."""
    r = _git(root, "ls-files", *args)
    if r.returncode != 0:
        raise RuntimeError(f"git ls-files {' '.join(args)} failed in {root}: {r.stderr.strip()}")
    return r.stdout.split(sep)


def tree_problems(views, root: pathlib.Path = ROOT) -> list[str]:
    """Rules (a) and (b), on the tree at ``root`` — read from git, not from the file system, so a tracked output is
    found whether or not it exists in the working tree."""
    problems = []
    for v in views:
        if v.out is None:
            continue
        tracked = _ls_files(root, "--", v.out)
        if tracked:
            shown = ", ".join(tracked[:3]) + (f" … ({len(tracked)} files)" if len(tracked) > 3 else "")
            problems.append(f"{v.name}: its output is TRACKED ({shown}) — a view is rendered, never committed: "
                            f"`git rm --cached` it")
        probe = v.out + "x.md" if v.out.endswith("/") else v.out
        if _git(root, "check-ignore", "-q", "--", probe).returncode != 0:
            problems.append(f"{v.name}: {v.out} is not gitignored — rendering it would dirty the checkout; add it "
                            f"to .gitignore")
    return problems


#: A tracked doc or note that calls itself generated and not for editing, in its first lines: "**Generated** from … — do not
#: edit by hand", "GENERATED by … — do not edit by hand", "Generated — do not hand-edit".
GENERATED_MARKER = re.compile(r"(?i)\b(generated|rendered view)\b.*\b(do not|don't|never)\b.{0,24}\bedit")
MARKER_LINES = 12
SWEPT = ("docs", "kb")
#: Not content that could be a view: the work register's notes (they quote the phrase when they are ABOUT generated files)
#: and the frozen evidence records ("Recorded by …": a measurement, not an index).
NOT_VIEWS = ("kb/Work/", "docs/rearchitecture/evidence/")


def unregistered_problems(views, root: pathlib.Path = ROOT) -> list[str]:
    """Rule (c), the sweep for the NEXT case: a tracked doc under ``docs/`` or ``kb/`` that declares itself generated is
    a view nobody registered (it would collide in the merge queue and go stale exactly as the registered ones did), so a
    new generated index cannot be added by copying the old habit. The fix is a row in ``VIEWS``, never an exemption."""
    out = []
    listing = _ls_files(root, "-z", "--", *SWEPT, sep="\0")
    for rel in sorted(f for f in listing if f.endswith(".md") and not f.startswith(NOT_VIEWS)):
        try:
            with open(root / rel, encoding="utf-8", errors="replace") as fh:
                head = [next(fh, "") for _ in range(MARKER_LINES)]
        except OSError:
            continue
        hit = next((l.strip() for l in head if GENERATED_MARKER.search(l)), None)
        if hit:
            out.append(f"{rel}: a tracked file that declares itself generated ({hit[:90]!r}) — render it as a view: add a "
                       f"row to VIEWS in scripts/rendered_views.py, gitignore its output and `git rm --cached` it")
    return out


def render_problems(views, root: pathlib.Path = ROOT) -> list[str]:
    """Rule (d): each generator's own ``--check`` renders in memory and must exit 0."""
    problems = []
    for v in views:
        if v.check is None:
            continue
        r = subprocess.run(list(v.check), cwd=root, capture_output=True, text=True, encoding="utf-8")
        if r.returncode != 0:
            body = (r.stdout + r.stderr).strip().splitlines()
            problems.append(f"{v.name}: does not render ({' '.join(v.check[1:])} exit {r.returncode})"
                            + "".join(f"\n      {line}" for line in body[-8:]))
    return problems


def render_all(views, root: pathlib.Path = ROOT) -> int:
    rc = 0
    for v in views:
        cmd = [a.replace("{out}", v.out or "") for a in v.render]
        print(f"· {v.name}: {' '.join(cmd[1:] if cmd[0] == sys.executable else cmd)}", flush=True)
        r = subprocess.run(cmd, cwd=root)
        rc = rc or r.returncode
    return rc


def select(names: list[str]):
    by_name = {v.name: v for v in VIEWS}
    unknown = [n for n in names if n not in by_name]
    if unknown:
        sys.exit(f"unknown view(s): {', '.join(unknown)} — known: {', '.join(by_name)}")
    return [by_name[n] for n in names] or list(VIEWS)


def to_render(names: list[str]):
    """The named views, or every view that renders without a build or an external tool."""
    return select(names) if names else [v for v in VIEWS if not v.heavy]


def self_test() -> int:
    """The tree rules on a planted repository: a tracked output, an un-ignored output and a clean one; and the
    registry itself — unique names, every in-tree output reachable by a rule."""
    results: list[tuple[str, bool, str]] = []

    def check(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))

    def git(root, *a):
        r = _git(root, *a)
        if r.returncode != 0:
            raise RuntimeError(f"git {' '.join(a)}: {r.stderr}")

    with tempfile.TemporaryDirectory() as td:
        root = pathlib.Path(td)
        git(root, "init", "-q")
        (root / "docs").mkdir()
        (root / "kb" / "Conformance").mkdir(parents=True)
        (root / ".gitignore").write_text("kb/Conformance/\ndocs/OK.md\n", encoding="utf-8")
        (root / "docs" / "TRACKED.md").write_text("x", encoding="utf-8")
        (root / "kb" / "Conformance" / "a.md").write_text("x", encoding="utf-8")
        git(root, "add", "-f", "docs/TRACKED.md", "kb/Conformance/a.md")
        views = (View("ok", "docs/OK.md", ("x",), None),
                 View("tracked", "docs/TRACKED.md", ("x",), None),
                 View("dir-tracked", "kb/Conformance/", ("x",), None),
                 View("unignored", "docs/UNIGNORED.md", ("x",), None),
                 View("outside", None, ("x",), None))
        found = tree_problems(views, root)
        text = "\n".join(found)
        check("a clean view raises nothing", not any(p.startswith("ok:") for p in found), text)
        check("a tracked file output is named", any(p.startswith("tracked:") and "TRACKED" in p for p in found), text)
        check("a tracked file under a directory output is named",
              any(p.startswith("dir-tracked:") and "kb/Conformance/a.md" in p for p in found), text)
        check("an un-ignored output is named", any(p.startswith("unignored:") and "gitignore" in p for p in found), text)
        check("an output outside the tree is never an in-tree finding", not any(p.startswith("outside:") for p in found), text)

        (root / "kb" / "Work").mkdir()
        (root / "docs" / "GEN.md").write_text("# T\n\n> **Generated** from X - do not edit by hand.\n", encoding="utf-8")
        (root / "docs" / "HAND.md").write_text("# T\n\nAn index someone wrote; see the generated ones.\n", encoding="utf-8")
        (root / "kb" / "Work" / "PB1.md").write_text("Generated indexes - do not hand-edit them.\n", encoding="utf-8")
        git(root, "add", "docs/GEN.md", "docs/HAND.md", "kb/Work/PB1.md")
        swept = unregistered_problems(views, root)
        text = "\n".join(swept)
        check("an unregistered tracked doc that declares itself generated is named",
              len(swept) == 1 and swept[0].startswith("docs/GEN.md:"), text)

        bad = (View("fails", None, ("x",), (sys.executable, "-c", "import sys; print('boom'); sys.exit(3)")),
               View("passes", None, ("x",), (sys.executable, "-c", "pass")),
               View("by-test", None, ("x",), None, checked_by="SomeTest"))
        found = render_problems(bad, root)
        check("a generator whose --check fails is named with its output",
              len(found) == 1 and found[0].startswith("fails:") and "boom" in found[0], "\n".join(found))

    names = [v.name for v in VIEWS]
    check("view names are unique", len(set(names)) == len(names), str(names))
    check("every view is checked by its generator or names the test that is", all(v.check or v.checked_by for v in VIEWS))

    failed = [r for r in results if not r[1]]
    for name, ok, detail in results:
        print(f"  {'ok  ' if ok else 'FAIL'} {name}" + (f"\n         {detail}" if detail and not ok else ""))
    print(f"rendered_views self-test: {len(results) - len(failed)}/{len(results)}")
    return 1 if failed else 0


def main(argv: list[str]) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("names", nargs="*", help="views to render or check (default: all)")
    ap.add_argument("--check", action="store_true",
                    help="exit non-zero when a view is tracked, not gitignored, or no longer renders")
    ap.add_argument("--tree", action="store_true",
                    help="--check without rendering: only the tree rules (tracked, gitignored, unregistered generated "
                         "files), which is what RenderedViewsDriftTests asks")
    ap.add_argument("--list", action="store_true", help="print each view: name, output, generator")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)

    if a.self_test:
        return self_test()
    views = select(a.names)
    if a.list:
        for v in views:
            print(f"{v.name:24} {v.out or '(outside the tree: the generator\'s own default)':32} "
                  f"{' '.join(v.render[1:3]) if v.render[0] == sys.executable else v.render[-1]}")
        return 0
    if a.check or a.tree:
        problems = tree_problems(views) + unregistered_problems(views) + ([] if a.tree else render_problems(views))
        if problems:
            print(f"⛔ {len(problems)} RENDERED-VIEW PROBLEM(S) (scripts/rendered_views.py; DESIGN-test-build-ci.md §3.17.4):")
            for p in problems:
                print(f"   {p}")
            return 1
        print(f"rendered views OK — {len(views)} views, none tracked, every in-tree one gitignored, nothing unregistered"
              + ("" if a.tree else ", every one renders"))
        return 0
    return render_all(to_render(a.names))


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
