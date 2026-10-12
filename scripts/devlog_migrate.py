#!/usr/bin/env python3
"""devlog_migrate.py — the ONE-TIME split of the monolithic DEVLOG.md into DEVLOG/, one file per entry (kb/Work PB3025;
DESIGN-test-build-ci.md section 3.17.4). It runs in the branch that makes the change, and AGAIN in the landing after
its final rebase onto main (main's DEVLOG.md has gained entries since: take main's DEVLOG.md in the modify/delete
conflict and run this; it re-splits the whole file); that landing deletes this script (CLAUDE.md rule 4: no tool is
kept for a job that no longer exists; git history keeps it).

    python scripts/devlog_migrate.py            # split DEVLOG.md, prove it, write DEVLOG/, git rm DEVLOG.md

It reads the working tree's DEVLOG.md (stored `-text`, so its bytes are the committed ones: mixed CRLF and LF),
normalizes the line
endings to LF (kb/Work PB3029: one line-ending format), and splits it at every line that starts `## Entry`: what
precedes the first is the front matter (written as DEVLOG/README.md only when that file does not exist yet, so a
re-run keeps the README as edited), and each entry block (its header line through the line before the next header)
is one file, `NNNN[x]-<slug>.md`, named by devlog.py's own rule. A kept README's history sentence, `(entries
001–NNNN, split from …`, is rewritten to the range this run split (README_RANGE), so the README never names a range
older than the history it introduces (kb/Work PB3025, review R1-2); a README without that sentence is refused. A number the monolith used twice gets a letter in the
FILE NAME only (the first occurrence the highest letter), so the render order is the monolith's order.

THE PROOF, before anything is written to the tree: `devlog.render` over the split (with the monolith's own front
matter) must equal the normalized monolith byte for byte, every file must pass `devlog.entry_problems`, every name must
be unique, and the number of entry files must equal the number of `## Entry` lines. Any failure exits 1 and writes
nothing. After writing (every earlier entry file is removed first; README.md and a pending entry are kept), the entry
files as written must render the monolith's entries byte for byte again.
"""
from __future__ import annotations

import collections
import hashlib
import pathlib
import re
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import devlog  # noqa: E402

#: The README's history sentence: the range of entries the migration split, which every run rewrites.
README_RANGE = re.compile(r"\(entries (?P<lo>\d+)–(?P<hi>\d+), split from ")


def split(text: str) -> tuple[str, list[str]]:
    """(the front matter, the entry blocks in file order) of an LF text."""
    lines = text.split("\n")
    starts = [i for i, ln in enumerate(lines) if ln.startswith("## Entry")]
    pre = "\n".join(lines[:starts[0]]) + "\n"
    blocks = []
    for k, s in enumerate(starts):
        e = starts[k + 1] if k + 1 < len(starts) else len(lines)
        blocks.append("\n".join(lines[s:e]) + ("\n" if e < len(lines) else ""))
    return pre, blocks


def names(blocks: list[str]) -> list[str]:
    keys = []
    for b in blocks:
        m = devlog.HEADER.match(devlog.first_line(b))
        if not m:
            raise SystemExit(f"⛔ not an entry header: {devlog.first_line(b)[:80]!r}")
        keys.append((int(m.group("num")), m.group("sfx")))
    count = collections.Counter(keys)
    seen: collections.Counter = collections.Counter()
    out = []
    for (num, sfx), b in zip(keys, blocks):
        letter = sfx
        if count[(num, sfx)] > 1:
            if sfx:
                raise SystemExit(f"⛔ Entry {num}{sfx} is used twice WITH a letter: no file-name letter can order it")
            letter = chr(ord("a") + count[(num, sfx)] - 1 - seen[(num, sfx)])
            seen[(num, sfx)] += 1
        out.append(devlog.entry_name(num, letter, devlog.title_of(devlog.first_line(b))))
    return out


def readme_with_range(readme: str, file_names: list[str]) -> str | None:
    """The README with its history sentence naming the split's lowest and highest entry numbers; None when the README
    has no such sentence (the caller refuses)."""
    nums = [devlog.key_of(n)[0] for n in file_names]
    if not README_RANGE.search(readme):
        return None
    return README_RANGE.sub(f"(entries {min(nums):03d}–{max(nums)}, split from ", readme, count=1)


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    repo = HERE.parent
    raw = (repo / devlog.MONOLITH).read_bytes()
    text = raw.decode("utf-8").replace("\r\n", "\n")
    pre, blocks = split(text)
    file_names = names(blocks)
    files = {devlog.README: pre, **dict(zip(file_names, blocks))}
    problems = [p for n, b in zip(file_names, blocks) for p in devlog.entry_problems(n, b)]
    if len(set(file_names)) != len(file_names):
        problems.append(f"{len(file_names) - len(set(file_names))} file names collide")
    if len(blocks) != sum(1 for ln in text.split("\n") if ln.startswith("## Entry")):
        problems.append("the entry count is not the number of `## Entry` lines")
    rendered = devlog.render(files)
    if rendered != text:
        at = next((i for i, (a, b) in enumerate(zip(rendered, text)) if a != b), min(len(rendered), len(text)))
        problems.append(f"the render differs from the monolith at character {at}: {rendered[at:at + 60]!r} vs "
                        f"{text[at:at + 60]!r}")
    print(f"monolith : {len(raw):,} bytes as committed ({raw.count(b'\r\n'):,} CRLF, "
          f"{raw.count(b'\n') - raw.count(b'\r\n'):,} LF); {len(text.encode('utf-8')):,} bytes normalized to LF")
    print(f"           sha256 {hashlib.sha256(text.encode('utf-8')).hexdigest()} (normalized)")
    print(f"split    : README.md + {len(blocks):,} entry files "
          f"({sum(1 for n in file_names if devlog.key_of(n)[1]):,} carry a letter)")
    print(f"render   : sha256 {hashlib.sha256(rendered.encode('utf-8')).hexdigest()}, "
          f"{'IDENTICAL' if rendered == text else 'DIFFERENT'} to the normalized monolith")
    if problems:
        for p in problems[:40]:
            print(f"⛔ {p}")
        print(f"=== DEVLOG MIGRATION: REFUSED ({len(problems)} problem(s)); nothing written ===")
        return 1
    out = repo / devlog.DIR
    kept = out / devlog.README
    readme = None
    if kept.exists():
        readme = readme_with_range(kept.read_bytes().decode("utf-8"), file_names)
        if readme is None:
            print(f"=== DEVLOG MIGRATION: REFUSED ({devlog.DIR}/{devlog.README} has no `(entries 001–NNNN, split from` "
                  "sentence to bring up to date); nothing written ===")
            return 1
    out.mkdir(exist_ok=True)
    for old in out.iterdir():
        if devlog.key_of(old.name):
            old.unlink()
    for n, t in files.items():
        if n != devlog.README or readme is None:
            (out / n).write_bytes(t.encode("utf-8"))
    if readme is not None:
        kept.write_bytes(readme.encode("utf-8"))
        print(f"readme   : {README_RANGE.search(readme).group(0).strip()} (the history sentence brought up to date)")
    if devlog.render(devlog.worktree_files(repo), since=0) != text[len(pre):]:
        print("=== DEVLOG MIGRATION: REFUSED (the files as written do not render the monolith) ===")
        return 1
    subprocess.run(["git", "-C", str(repo), "rm", "-q", devlog.MONOLITH], check=True)
    subprocess.run(["git", "-C", str(repo), "add", "--", devlog.DIR], check=True)
    print(f"=== DEVLOG MIGRATION: DONE ({len(blocks):,} entries; the written files re-render byte-identical) ===")
    return 0


if __name__ == "__main__":
    sys.exit(main())
