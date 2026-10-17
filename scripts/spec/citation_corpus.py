#!/usr/bin/env python3
"""The ONE definition of "the files that carry spec citations", shared by the citation audits.

⛔ WHY THIS IS A MODULE AND NOT A COPIED GLOB. There are two citation audits and they own DIFFERENT rules —
`audit_doc_citations.py` checks a QUOTED FRAGMENT against the clause it is filed under, `audit_code_citations.py`
checks the CLAUSE NUMBER against the construct the comment is about — but they must look at the SAME files, or a
citation is covered by one rule and not the other purely by where it happens to live. `audit_doc_citations.py`
already learned this once the hard way: it scanned only `docs/` until PB3's fabricated "GR7 k3" turned up in
THIRTEEN places across five source files (DEVLOG 13557/13606). One glob, one place.
"""
from __future__ import annotations

import functools
import pathlib
import subprocess

REPO = pathlib.Path(__file__).resolve().parents[2]

#: Directories that hold FROZEN evidence — an agent's raw output, recorded as it was said. Repairing a citation
#: inside one would rewrite the record of what was found, which is the opposite of what these files are for.
#: (The findings they contain are actioned through `kb/Work/`, never by editing the transcript.)
FROZEN = (
    "docs/rearchitecture/spec-reconciliation",
    "docs/rearchitecture/evidence",
)

@functools.lru_cache(maxsize=None)
def repository_files() -> frozenset[pathlib.Path]:
    """⛔ THE POPULATION IS THE REPOSITORY, NEVER THE MACHINE. `git ls-files --cached --others
    --exclude-standard`: every tracked file, plus every untracked file the ignore rules do not exclude — so a
    golden written but not yet `git add`ed IS audited, and a build tree, a generated parser, a rendered vault
    is NOT. Anything gitignored is by definition derived from something that is, and is covered through it.

    Two manufactured reds earned this. First the ANTLR build task's copies of every `.g4` under `obj/antlr-lib/`
    (each grammar finding reported twice, the copy frozen at the last build, a clean checkout and a built one
    disagreeing about the gate); that was answered with a hand-kept set of build-output directory names. Then
    `kb/Reference/` — gitignored output of `gen-vault-reference.ps1` — rendered weeks before the citation-repair
    sweep, so battery #42 printed 23 phantom clauses the tracked tree had already shed (kb/Work PB378). The
    directory-name set would have needed a third entry; the ignore rules already knew all three. One mechanism,
    and the next generated tree is excluded the day its `.gitignore` line is written."""
    try:
        out = subprocess.run(["git", "-C", str(REPO), "ls-files", "-z", "--cached", "--others", "--exclude-standard"],
                             check=True, capture_output=True).stdout
    except (OSError, subprocess.CalledProcessError) as e:  # no git, or not a checkout
        raise SystemExit("⛔ citation_corpus: cannot enumerate the repository with git; the audits refuse to "
                         f"guess a population from the disk ({e})") from e
    return frozenset(REPO / p.decode("utf-8") for p in out.split(b"\0") if p)


def _keep(p: pathlib.Path) -> bool:
    if p not in repository_files():
        return False
    rel = p.relative_to(REPO)
    return not any(rel.as_posix().startswith(f) for f in FROZEN)


def prose_files() -> list[pathlib.Path]:
    """Design docs, the process SSOT, and the source doc-comments that carry citations at the same density."""
    out = sorted(REPO.joinpath("docs").rglob("*.md")) + [REPO / "CLAUDE.md"]
    out += sorted(REPO.joinpath("src").rglob("*.cs")) + sorted(REPO.joinpath("src").rglob("*.g4"))
    return [p for p in out if _keep(p)]


def code_files() -> list[pathlib.Path]:
    """The compiler's own C#, where a citation can reach a USER rather than a reader — the population
    `audit_code_citations.py`'s DIAGNOSTIC-STRING checks run over (kb/Work PB838).

    ⛔ IT IS A NARROWER POPULATION THAN `prose_files()`, ON PURPOSE, AND THE SCOPE IS THE WHOLE CHECK. A
    citation in a COMMENT sits beside the derivation that justifies it, and this repository deliberately writes
    a rule block's clause both ways (`§14.9.39 GR29` and `§14.9.39.4 GR29`) — `_alias` exists for that. A
    citation inside a MESSAGE STRING is all the user gets: it is read alone, in a terminal, by someone looking
    the rule up. Measured 2026-09-21: "a rule kind with no ordinal" is 2683 sites tree-wide (`SR 1` with a
    space, `SR-14.9.28.3-2` row ids, "the 1561-1563 SR band") and 5 inside message strings. The narrow
    population is the decidable one."""
    return [p for p in sorted(REPO.joinpath("src").rglob("*.cs")) if _keep(p)]


def declaration_files() -> list[pathlib.Path]:
    """The files whose comments follow the DEFINITION-HEADER convention — "name the construct, then cite its
    clause": the ANTLR grammars (`// MOVE (§14.9.25)` above `moveStatement`) and the golden program headers
    (`*> ISO §14.7.4 + §14.9.2 — ROUNDED MODE on ADD CORRESPONDING`).

    ⛔ C# IS DELIBERATELY NOT HERE, AND IT WAS MEASURED, NOT ASSUMED. Running the construct-vs-clause check over
    `src/**/*.cs` reported 1224 candidates, and the sample was almost entirely correct citations: a doc-comment
    cites a clause to SUPPORT a rule rather than to LABEL a construct, so the construct's name is legitimately
    absent ("as though it were moved to an alphanumeric data item (§14.9.25.4 GR6)" names no MOVE). A check that
    is wrong 95% of the time cannot gate anything, and gating on it would train the reader to skip it.
    C# doc-comments are still covered — by `audit_doc_citations.py`, whose quoted-fragment rule works there, and
    by this file's PHANTOM check, which needs no convention at all."""
    out = sorted(REPO.joinpath("src").rglob("*.g4"))
    out += sorted(REPO.joinpath("conformance-suite").rglob("*.cob"))   # the suite's tests: cases/, wiseowl/cases/
    return [p for p in out if _keep(p)]


def all_files() -> list[pathlib.Path]:
    """Every file that carries a citation — grammars, C#, goldens, design docs, the work register and the vault
    notes. Used by the checks that need no convention at all (does this clause EXIST), which are exactly the
    ones that can be run over prose without a false-positive problem."""
    out = declaration_files() + prose_files()
    out += sorted(REPO.joinpath("tests").rglob("*.cs"))
    out += sorted(REPO.joinpath("kb").rglob("*.md"))
    return [p for p in dict.fromkeys(out) if _keep(p)]
