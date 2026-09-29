"""Classify `dotnet-stack report` samples of the b2 probe: where is each compiling thread?

    python classify_stacks.py <sample.txt> ...

INPUT NOT IN THE REPOSITORY: stack samples taken with `dotnet-stack report -p <ScalingProbe pid>` while Program.cs
runs (dotnet-stack is a NuGet tool: `dotnet tool install dotnet-stack --tool-path <dir>`). The committed samples
(`stacks-12threads-*.txt`, `stacks-1thread.txt`) were taken on the shared host on 2026-09-28.

A thread counts when its stack contains `CompilerDriver.Compile`. It is classified by its frames, innermost wins:
lexer start-state work (ComputeStartState / AddDFAState: the default mode's start state is recomputed per token,
see §3.14.5 of DESIGN-test-build-ci.md), other lexing, parsing, binding, anything else.
"""
import re
import sys
from collections import Counter

CLASSES = [
    ("lexer: start-state closure / AddDFAState", re.compile(r"LexerATNSimulator\.(ComputeStartState|AddDFAState)")),
    ("lexer: other", re.compile(r"Lexer\.NextToken|LexerATNSimulator")),
    ("parser", re.compile(r"ParserATNSimulator|CobolParserCore|Antlr4\.Runtime\.Parser")),
    ("binder", re.compile(r"CobolNet\.Binding\.")),
]


def threads(text):
    block = []
    for line in text.splitlines():
        if line.startswith("Thread"):
            if block:
                yield block
            block = []
        else:
            block.append(line)
    if block:
        yield block


def main(paths):
    tally = Counter()
    blocked = 0
    n = 0
    for p in paths:
        for t in threads(open(p, encoding="utf-8", errors="replace").read()):
            s = "\n".join(t)
            if "CompilerDriver.Compile" not in s:
                continue
            n += 1
            if "Monitor.Enter_Slowpath" in s:
                blocked += 1
            tally[next((name for name, rx in CLASSES if rx.search(s)), "other")] += 1
    print(f"{len(paths)} sample file(s), {n} compiling thread stacks; {blocked} blocked in Monitor.Enter")
    for name, c in tally.most_common():
        print(f"  {c:4d}  {100 * c / max(n, 1):5.1f}%  {name}")


if __name__ == "__main__":
    main(sys.argv[1:])
