"""PB1708 counterfactuals over the 20 replayed clusters of trains 65-69 (map dbea12428):

  T0  today's rule (impacted_tests.py as landed), per-file union — reproduces replay3.txt;
  T1  the owner's layer 2 as decided: a registry change selects only the tests that read a CHANGED ENTRY (the methods
      whose owned lines name it) plus the registry's enumerators; every other file as today;
  T2  T1 + the SYMBOL rule for every declaration change and new/deleted file: the tests reaching a method whose owned
      lines name a changed declared symbol (plus the symbol's own entries), instead of 'the whole file plus every
      executed file naming one of its types';
  T3  T2 with a finalizer attributed to the tests that reached its type's constructors (no 'outside every test' whole).

The entry/symbol estimate is taken on the map commit's tree (M), where the replayed clusters have already landed, so a
name is looked up among the sites that exist at M: an over-approximation of what a recording at the cluster's base
would say (never an under-approximation for a symbol that existed at the base).
Also: the under-selection check for today's DiagnosticCatalog/DiagnosticDescriptors rule."""
import pickle, statistics
from collections import defaultdict
from common import *

m, ix = load()
M = m["commit"]
res = pickle.load(open("a1.pkl", "rb"))
REGDIAG = "src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs"
REGFE = "src/Cobol.Net.Frontend/Diagnostics/DiagnosticDescriptors.cs"
REGCR = "src/Cobol.Net.Editions/ConstructRegistry.g.cs"
REGC = "src/Cobol.Net.Editions/Constructs.g.cs"
REG = {REGDIAG, REGFE, REGCR, REGC}
IDENT = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")
STRLIT = re.compile(r'"([a-z0-9][a-z0-9-]{4,})"')

# ---- the owned-line identifier index at M ------------------------------------------------------------------------
files = sorted(f for f in ix.ranges if f.endswith(".cs"))
proc = subprocess.Popen(["git", "cat-file", "--batch"], cwd=WT, stdin=subprocess.PIPE, stdout=subprocess.PIPE)
texts = {}
for f in files:
    proc.stdin.write(f"{M}:{f}\n".encode()); proc.stdin.flush()
    hdr = proc.stdout.readline().decode().split()
    if len(hdr) < 3 or hdr[1] == "missing":
        continue
    body = proc.stdout.read(int(hdr[2])); proc.stdout.read(1)
    texts[f] = body.decode("utf-8", "replace").splitlines()
proc.stdin.close()
ident_ix = defaultdict(set)       # identifier -> entry ids whose owned lines name it
lit_ix = defaultdict(set)         # kebab string literal -> entry ids
qual_ix = defaultdict(set)        # 'Type.Member' qualified text -> entry ids
for f, lines in texts.items():
    owners = defaultdict(list)
    for lo, hi, i in ix.ranges[f]:
        if lo > 0:
            for n in range(lo, min(hi, len(lines)) + 1):
                owners[n].append(i)
    for n, ids in owners.items():
        line = lines[n - 1]
        code = line.split("//", 1)[0]
        for tok in set(IDENT.findall(code)):
            ident_ix[tok].update(ids)
        for lit in STRLIT.findall(code):
            lit_ix[lit].update(ids)
        for q in re.findall(r"\b([A-Z]\w+\.[A-Z]\w+)\b", code):
            qual_ix[q].update(ids)
print(f"index: {len(texts)} files at {M[:12]}, {len(ident_ix)} identifiers", flush=True)


def mask_of(ids):
    mk = 0
    for i in ids:
        mk |= 1 << i
    return mk


def names_mask(names):
    ids = set()
    for nme in names:
        ids |= ident_ix.get(nme, set())
    return mask_of(ids)


def text_at(rev, path):
    return git("show", f"{rev}:{path}").splitlines()


def enclosing(lines, n):
    """The member or type declared at or above line n (1-based)."""
    for k in range(min(n, len(lines)) - 1, -1, -1):
        s = lines[k]
        t = it.TYPE_DECL_RE.search(s)
        if t and it.DECL_RE.match(s):
            return t.group(2)
        d = it.DECL_RE.match(s)
        if d:
            head = re.split(r"\(|=>|=|;|\{", d.group(2), maxsplit=1)[0]
            ids = IDENT.findall(head)
            if ids:
                return ids[-1]
    return None


def declared(line):
    s = line.strip()
    out = set()
    t = it.TYPE_DECL_RE.search(s)
    if t:
        out.add(t.group(2))
        par = re.search(r"\((.*)", s)
        if par and "record" in s:
            out |= {x for x in IDENT.findall(par.group(1)) if x[:1].isupper()}
    a = re.match(r"^\[(\w+)", s)
    if a:
        out |= {a.group(1), a.group(1) + "Attribute"}
    d = it.DECL_RE.match(line)
    if d:
        head = re.split(r"\(|=>|=|;|\{", d.group(2), maxsplit=1)[0]
        ids = IDENT.findall(head)
        if ids:
            out.add(ids[-1])
    return out


def symbol_mask(base, head, path, status, registry):
    """(mask, names) for one changed file under the symbol rule."""
    names = set()
    extra = 0
    if status in ("A", "D"):
        src = text_at(head if status == "A" else base, path)
        names |= set(it.TYPE_DECL_RE_ALL.findall("\n".join(src)))
        extra |= ix.file_ids.get(path, 0) if status == "A" else 0
    else:
        old = text_at(base, path)
        new = text_at(head, path)
        for start, cnt, removed, added in it.hunks(base, path):
            changed = [x for x in removed + added if not it.TRIVIAL_RE.match(x)]
            if not changed:
                continue
            if any(x.strip().startswith("using ") for x in changed):
                extra |= ix.file_ids.get(path, 0)
            for x in changed:
                names |= declared(x)
                if path == REGCR:
                    for lit in re.findall(r'new\("([a-z0-9-]+)"', x):
                        names.add("".join(p[:1].upper() + p[1:] for p in lit.split("-")))
                        extra |= mask_of(lit_ix.get(lit, set()))
            e = enclosing(old, start if cnt else start + 1)
            if e:
                names.add(e)
    mk = names_mask(names) | extra
    if registry:
        q = {REGDIAG: ["DiagnosticCatalog.All"], REGCR: ["ConstructRegistry.Entries", "CompilerDirectiveCatalog.Words"],
             REGFE: [], REGC: []}[path]
        for k in q:
            mk |= mask_of(qual_ix.get(k, set()))
        if path in (REGCR, REGC):
            mk |= mask_of(ident_ix.get("CompilerDirectiveCatalog", set()))
    return mk, names


# ---- finalizer attribution: Finalize is reached by whoever constructed the type ----------------------------------
fin_users = 0
for i in it.bit_ids(ix.ambient & ~ix.reached):
    nm = ix.entries[i][0]
    owner = nm.split(" ")[1].split("::")[0]
    for j, (n2, _) in enumerate(ix.entries):
        if n2.split(" ")[1].split("::")[0] == owner and "::.ctor" in n2 if " " in n2 else False:
            fin_users |= 1 << j

STRUCT = ("a grammar, build or source-generator input",)
print("label | files | T0 | T1 (registry entries) | T2 (+symbol rule) | T3 whole-forcing reasons left")
summary = []
for label, d in res.items():
    base, head = d["base"], d["head"]
    it.HEAD = head
    t0 = t1 = t2 = 0
    whole0, whole3 = [], []
    for s, p, mask, whole, notes, extra in d["rows"]:
        n0 = notes[0] if notes else ""
        t0 |= mask
        whole0 += whole
        struct = [w for w in whole if "grammar, build or source-generator" in w or "non-C# input" in w
                  or "corpus data" in w or "test data" in w]
        whole3 += struct
        if p in REG:
            mk, _ = symbol_mask(base, head, p, s, True)
            t1 |= mk; t2 |= mk
            continue
        t1 |= mask
        if p.endswith(".cs") and p.startswith("src/") and ("file level" in n0 or "never seen" in " ".join(whole)):
            mk, _ = symbol_mask(base, head, p, s, False)
            t2 |= mk
        else:
            t2 |= mask
    # the 'outside every test' check on the narrowed selection, with finalizers attributed to constructors
    orphan = t2 & ix.ambient & ~ix.reached
    if orphan:
        t2 |= fin_users
    c0, c1, c2 = count(ix, t0), count(ix, t1), count(ix, t2)
    summary.append((label, c0, c1, c2, sorted({w.split(":")[0] for w in whole3})))
    print(f"{label} | {len(d['rows'])} | {c0} | {c1} | {c2} | {sorted({w.split(':')[0] for w in whole3})}", flush=True)
pop = len(ix.conf)
for k, name in ((1, "T0"), (2, "T1"), (3, "T2")):
    vals = [r[k] for r in summary]
    print(f"{name}: min {min(vals)} median {statistics.median(vals)} max {max(vals)} of {pop}")
print(f"T2 clusters with no structural whole reason: {sum(1 for r in summary if not r[4])} of {len(summary)}")
pickle.dump(summary, open("a5.pkl", "wb"))

# ---- today's under-selection for descriptor fields -----------------------------------------------------------------
for path, typ in ((REGDIAG, "DiagnosticCatalog"), (REGFE, "DiagnosticDescriptors")):
    cc = [i for i, (n, _) in enumerate(ix.entries) if f"{typ}::.cctor" in n]
    today = count(ix, ix.expand(mask_of(cc)))
    src = texts.get(path) or text_at(M, path)
    fields = re.findall(r"public static readonly DiagnosticDescriptor (\w+)", "\n".join(src))
    reach = []
    for f in fields:
        reach.append((count(ix, mask_of(ident_ix.get(f, set()))), f))
    reach.sort(reverse=True)
    over = [r for r in reach if r[0] > today]
    print(f"{typ}: {len(fields)} fields; a change to any field initializer selects {today} tests today; "
          f"{len(over)} fields are named by methods that more tests reach (max {reach[0]}, median "
          f"{statistics.median(r[0] for r in reach)})")
