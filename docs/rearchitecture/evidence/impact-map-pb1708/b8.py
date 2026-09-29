"""Do a full unfiltered trx and `dotnet test --list-tests` name the SAME tests, as a multiset of display names?

    python b8.py <Assembly.list.txt> <Assembly.trx> [<Assembly.list.txt> <Assembly.trx> ...] > b8.txt

INPUTS NOT IN THE REPOSITORY: produced on ONE build of this checkout (2026-09-28, the reviser's worktree):
    dotnet build CobolSharp.sln
    dotnet test tests/Cobol.Net.Tests.<A> --no-build --list-tests > <A>.list.txt
    dotnet test tests/Cobol.Net.Tests.<A> --no-build --logger "trx;LogFileName=<A>.trx"
The gate's population check (DESIGN-test-build-ci.md §3.14.4) compares exactly these two multisets, so any
spelling difference between them (escaping, truncation, encoding) would make every gate read NEVER RAN / RAN TWICE.
Reported per assembly: both counts, distinct names, names listed more than once (xunit's `···` truncation), the
multiset difference in each direction, the trx's test DEFINITIONS against the listing, trx outcomes, and names
embedding an absolute path.
"""
import re
import sys
import xml.etree.ElementTree as ET
from collections import Counter

NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
ABS = re.compile(r"[A-Za-z]:\\")

args = sys.argv[1:]
for listing, trx in zip(args[::2], args[1::2]):
    lines = open(listing, encoding="utf-8", errors="strict").read().splitlines()
    try:
        start = next(i for i, l in enumerate(lines) if l.startswith("The following Tests are available:")) + 1
    except StopIteration:
        print(f"{listing}: no test listing found")
        continue
    listed = Counter(l[4:] for l in lines[start:] if l.startswith("    "))
    results = list(ET.parse(trx).getroot().iter(NS + "UnitTestResult"))
    ran = Counter(r.get("testName") for r in results)
    outcomes = Counter(r.get("outcome") for r in results)
    defs = Counter(u.get("name") for u in ET.parse(trx).getroot().iter(NS + "UnitTest"))
    only_list = listed - ran
    only_trx = ran - listed
    dups = {n: c for n, c in listed.items() if c > 1}
    print(f"{trx}")
    print(f"  --list-tests: {sum(listed.values())} names ({len(listed)} distinct; {len(dups)} names listed more than once, "
          f"{sum(dups.values())} rows)")
    print(f"  trx:          {sum(ran.values())} results ({len(ran)} distinct); outcomes {dict(outcomes)}")
    print(f"  multiset: listed-not-in-trx {sum(only_list.values())}, trx-not-listed {sum(only_trx.values())} -> "
          + ("EQUAL" if not only_list and not only_trx else "DIFFERENT"))
    print(f"  the trx's TEST DEFINITIONS (one per discovered case, <UnitTest name>): {sum(defs.values())} -> "
          + ("EQUAL to --list-tests as a multiset" if defs == listed else
             f"DIFFERENT ({sum((listed - defs).values())} listed-only, {sum((defs - listed).values())} definition-only)"))
    for n, c in list(only_list.items())[:5]:
        print(f"    only listed x{c}: {n[:160]}")
    for n, c in list(only_trx.items())[:5]:
        print(f"    only in trx x{c}: {n[:160]}")
    paths = [n for n in listed if ABS.search(n)]
    print(f"  names embedding an absolute path: {sum(listed[n] for n in paths)}"
          + (f", e.g. {paths[0][:170]}" if paths else ""))
    non_ascii = [n for n in listed if any(ord(ch) > 127 for ch in n)]
    print(f"  names with non-ASCII characters: {len(non_ascii)} distinct (all matched: "
          f"{all(listed[n] == ran[n] for n in non_ascii)})")
