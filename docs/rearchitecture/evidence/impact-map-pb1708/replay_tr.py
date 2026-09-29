import re, subprocess
import impacted_tests as it


def translate(WT, M, base, path, h):
    """Re-express hunk h (old side at `base`) in the map commit M's line numbers."""
    if base == M:
        return h
    start, count, removed, added = h
    out = subprocess.run(["git", "diff", "-U0", "--no-color", "--no-renames", base, M, "--", path], cwd=WT,
                         capture_output=True, text=True, encoding="utf-8").stdout
    shift = []
    for line in out.splitlines():
        mm = re.match(r"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@", line)
        if mm:
            shift.append((int(mm.group(1)), int(mm.group(2) if mm.group(2) is not None else 1),
                          int(mm.group(3)), int(mm.group(4) if mm.group(4) is not None else 1)))

    def one(n):
        off = 0
        for os_, oc, ns, nc in shift:
            if oc and os_ <= n < os_ + oc:
                return (ns, max(ns + nc - 1, ns))
            if (oc and n >= os_ + oc) or (not oc and n > os_):
                off += nc - oc
        return (n + off, n + off)

    if count == 0:
        lo, hi = one(start)
        return (lo, 0, removed, added) if lo == hi else (lo, hi - lo + 1, ["x"] * (hi - lo + 1), added)
    los, his = zip(*(one(n) for n in range(start, start + count)))
    lo, hi = min(los), max(his)
    trivial = all(it.TRIVIAL_RE.match(r) for r in removed)
    texts = removed if (hi - lo + 1) == count else ([""] if trivial else ["x"]) * (hi - lo + 1)
    return (lo, hi - lo + 1, texts, added)
