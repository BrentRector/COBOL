"""The ONE-TIME-INITIALIZATION hole (design review finding 1): code a static initializer CALLS runs once per test
process, in whichever test got there first, and no later reader of the built value records it.

For every src/ type with a static constructor, list the NON-constructor methods of the SAME type whose executing
tests are a subset of the static constructor's executing tests, while some other member of the type is reached by
more tests (the readers of the built value). That is a LOWER BOUND on the hole: builders that live in another type
(a captured registry accessor, a Lazy factory in a different class) are not counted here."""
from common import *

m, ix = load()


def tests_of(i):
    return testset(ix, 1 << i)


by_type = {}
for i, (name, segs) in enumerate(ix.entries):
    if " " not in name or "::" not in name:
        continue
    t = name.split(" ", 1)[1].split("::")[0]
    f = sorted({s[0] for s in segs})[0] if segs else ""
    by_type.setdefault(t, []).append((i, name, f))

rows = []
for t, members in by_type.items():
    cctor = [i for i, n, f in members if "::.cctor" in n]
    if not cctor or not any(f.startswith("src/") for _, _, f in members):
        continue
    ct = tests_of(cctor[0])
    readers = max((len(tests_of(i)) for i, n, _ in members if "::.cctor" not in n), default=0)
    builders = []
    for i, n, f in members:
        if "::.cctor" in n or not f.startswith("src/"):
            continue
        tt = tests_of(i)
        if tt and tt <= ct and len(tt) < readers:
            builders.append((n.split("::")[-1].split("(")[0], len(tt)))
    if builders:
        rows.append((readers, len(ct), t, builders))

print(f"{len(rows)} types whose static initializer calls methods charged only to the initializing test(s)")
for readers, c, t, b in sorted(rows, reverse=True):
    names = ", ".join(f"{n} ({k})" for n, k in b[:6]) + (f", +{len(b) - 6} more" if len(b) > 6 else "")
    print(f"{readers:5d} readers  {c:4d} init  {t}: {names}")
