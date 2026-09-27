#!/usr/bin/env python3
"""orient.py — ONE-CALL ORIENTATION for a set of source files: what an implementer would otherwise re-derive, every
wave, by reading and grepping.

    python scripts/spec/orient.py src/Cobol.Net.Runtime/IO/ReportWriter.cs
    python scripts/spec/orient.py <file> <file> … [--landed 6] [--commits 6] [--tests 8]

Owner, 2026-09-27: "Isn't there over many waves, more of the same orientation? Can we reduce the repeated work?"
Measured (waves 45–57, 81 implementer/finisher transcripts): reading and searching the codebase was 46 % of their
tokens, and each agent re-surveyed the same subsystems from scratch. `where.py` answers "which files implement this
clause"; this answers the next question, "what do I need to know about THESE files", and it carries forward what
earlier implementers LEARNED, which no grep recovers:

  1. OUTLINE — the file's types and members with line numbers (read the right 80 lines, not the whole file).
  2. CLAUSES — the § citations in the file, by count (the rules the file owns; cite.py them before relying on one).
  3. TESTS — the test files that name the file's types, by hit count (the gate filter to start from).
  4. LEARNED — the Landing / Fix sections of LANDED kb/Work notes that named the file: entry points, the helper that
     owns a rule, the trap that bit the last fixer. Newest first. This is the accumulated orientation.
  5. OPEN — the open notes that name the file (the rest of its cluster; see fix_clusters.py).
  6. HISTORY — the last commits that touched the file.

It writes nothing: every section is derived from the tree, kb/Work and git on each run, so it can never go stale and
there is no second document to maintain (CLAUDE.md rules 6 and 8).
"""
import argparse, collections, pathlib, re, subprocess, sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
WORK = ROOT / "kb" / "Work"

DECL = re.compile(r"^\s*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|override|"
                  r"virtual|async|unsafe|new|required|file)\s+)*(class|record struct|record|struct|interface|enum|"
                  r"[\w<>\[\],.? ]+?)\s+([A-Z]\w*)\s*(?:<[^>]*>)?\s*(\(|:|\{|$|where\b|=>)")
CITE = re.compile(r"§\s?(\d+(?:\.\d+)+)(?:\s+(?:SR|GR)\s?\d+)?")


def frontmatter(text):
    m = re.match(r"---\n(.*?)\n---", text, re.S)
    out = {}
    for line in (m.group(1).split("\n") if m else []):
        if ":" in line and not line.startswith((" ", "\t")):
            k, v = line.split(":", 1)
            out[k.strip()] = v.strip()
    return out


def outline(lines, limit=60):
    out = []
    for i, line in enumerate(lines, 1):
        s = line.strip()
        if s.startswith(("//", "*", "using ", "namespace ", "return ", "if ", "else", "var ", "throw ")):
            continue
        m = DECL.match(line)
        if not m or m.group(2) in ("if", "for", "foreach", "while", "switch", "catch", "using", "return"):
            continue
        kind = m.group(1).strip()
        if kind in ("class", "record", "record struct", "struct", "interface", "enum"):
            out.append(f"  {i:5}  {kind} {m.group(2)}")
        elif m.group(3) == "(" and len(line) - len(line.lstrip()) <= 8:
            out.append(f"  {i:5}    {m.group(2)}(")
    if len(out) > limit:
        out = out[:limit] + [f"  … {len(out) - limit} more members"]
    return out


def section(text, heads=("## Landing", "## Fix", "**Fix.**", "## Retired")):
    """The first landing/fix paragraph block of a note, trimmed."""
    for h in heads:
        i = text.find(h)
        if i >= 0:
            block = text[i:].split("\n\n## ", 1)[0]
            return [l for l in block.split("\n")[:9] if l.strip()]
    return []


def section_text(body):
    return "\n".join(section(body))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("files", nargs="+")
    ap.add_argument("--landed", type=int, default=6)
    ap.add_argument("--commits", type=int, default=6)
    ap.add_argument("--tests", type=int, default=8)
    a = ap.parse_args()
    notes = []
    for p in WORK.glob("PB*.md"):
        t = p.read_text(encoding="utf-8", errors="replace").replace("\r\n", "\n")
        notes.append((frontmatter(t), t))
    test_files = [p for p in (ROOT / "tests").rglob("*.cs")
                  if "/bin/" not in p.as_posix() and "/obj/" not in p.as_posix()]
    test_text = {p: p.read_text(encoding="utf-8", errors="replace") for p in test_files}

    for f in a.files:
        path = (ROOT / f).resolve()
        rel = path.relative_to(ROOT).as_posix()
        if not path.exists():
            print(f"== {rel}: not found"); continue
        lines = path.read_text(encoding="utf-8", errors="replace").split("\n")
        stem = path.stem
        base = stem.split(".")[0]
        print(f"\n{'=' * 100}\n== {rel}  ({len(lines)} lines)")
        print("-- OUTLINE"); print("\n".join(outline(lines)))
        cites = collections.Counter(m.group(1) for l in lines for m in CITE.finditer(l))
        print("-- CLAUSES (§ citations in the file): " + ", ".join(f"§{c}×{n}" for c, n in cites.most_common(14)))
        hits = sorted(((t.count(base), p) for p, t in test_text.items() if base in t), reverse=True)[:a.tests]
        print("-- TESTS naming " + base + ": " + ", ".join(f"{p.stem}({n})" for n, p in hits))
        names = (rel, path.name, stem, base)
        landed, open_ = [], []
        for fm, t in notes:
            if not any(n in t for n in names if len(n) > 6):
                continue
            (landed if fm.get("status") in ("landed", "retired") else open_ if fm.get("status") == "open" else []) \
                .append((int(re.sub(r"\D", "", fm.get("id", "")) or 0), fm, t))   # ids like PB367b
        print(f"-- LEARNED (landed notes naming the file, newest first; {len(landed)} total)")
        for _, fm, t in sorted(landed, key=lambda x: x[0], reverse=True)[:a.landed]:
            print(f"   {fm.get('id')}: {fm.get('title', '')[:150]}")
            body = t.split("\n---\n", 1)[-1]
            # What the note says ABOUT THIS FILE: its sentences naming the file or its type, landing sections first.
            said = [s.strip() for s in re.split(r"(?<=[.;])\s+|\n", section_text(body) + "\n" + body)
                    if any(n in s for n in (path.name, stem, base)) and len(s.strip()) > 30]
            seen = set()
            for s in said:
                if s in seen:
                    continue
                seen.add(s)
                print("      · " + s[:200])
                if len(seen) >= 4:
                    break
        print(f"-- OPEN notes naming the file ({len(open_)}): "
              + ", ".join(f"{fm.get('id')}" for _, fm, _ in sorted(open_, key=lambda x: x[0]))[:600])
        log = subprocess.run(["git", "-C", str(ROOT), "log", f"-{a.commits}", "--format=%h %ad %s", "--date=short",
                              "--", rel], capture_output=True, text=True, encoding="utf-8").stdout.strip()
        print("-- HISTORY\n" + "\n".join("   " + l[:150] for l in log.split("\n") if l))
    return 0


if __name__ == "__main__":
    sys.exit(main())
