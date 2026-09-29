"""The static-state SHAPES in src/Cobol.Net.* (design review finding 1): how many places establish static state in a
form the one-time-initialization frame must cover. A textual scan (comments and strings are not stripped, so it is
an upper bound per form); the drift test that replaces it (`StaticStateDriftTests`) uses Roslyn symbols."""
import collections, re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
MODS = r"(?:(?:public|private|internal|protected|new|unsafe|volatile)\s+)*"
forms = {
    "static readonly field (initializer or static constructor)": re.compile(rf"^\s*{MODS}static\s+{MODS}readonly\s"),
    "MUTABLE static field (non-readonly, non-const)": re.compile(
        rf"^\s*{MODS}static\s+{MODS}(?!readonly\b|class\b|partial\b|record\b|abstract\b|override\b|implicit\b|explicit\b"
        rf"|async\b|void\b|extern\b|operator\b|bool\s+Try)[\w.<>\[\],? ]+?\s+\w+\s*(?:=(?!>)|;)"),
    "static Lazy<T>": re.compile(r"\bstatic\b[^;(]*\bLazy<"),
    "ConcurrentDictionary GetOrAdd/AddOrUpdate call": re.compile(r"\.(?:GetOrAdd|AddOrUpdate)\s*\("),
    "LazyInitializer": re.compile(r"\bLazyInitializer\."),
    "static property 'field ??=' (the accessor shape)": re.compile(r"\bstatic\b[^;]*=>\s*field\s*\?\?="),
    "[ThreadStatic]": re.compile(r"\[ThreadStatic\]"),
}
counts = collections.defaultdict(collections.Counter)
for f in sorted(ROOT.glob("src/Cobol.Net.*/**/*.cs")):
    if "Generated" in f.parts or "obj" in f.parts or "bin" in f.parts:
        continue
    asm = f.relative_to(ROOT).parts[1]
    for line in f.read_text(encoding="utf-8", errors="replace").splitlines():
        for k, rx in forms.items():
            if rx.search(line):
                counts[k][asm] += 1
for k in forms:
    c = counts[k]
    print(f"{sum(c.values()):5d}  {k}: " + ", ".join(f"{a} {n}" for a, n in c.most_common()))
