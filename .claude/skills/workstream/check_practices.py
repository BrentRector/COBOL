#!/usr/bin/env python3
"""Fail when a brief or a rendered dispatch spec drops a MANDATORY practice (templates/MANDATORY-PRACTICES.md).

    python .claude/skills/workstream/check_practices.py              # every brief in templates/
    python .claude/skills/workstream/check_practices.py <spec.txt…>  # rendered implementer specs
    python .claude/skills/workstream/check_practices.py <groups.json> <spec.txt…>  # + the same-file successor rule (O2)

A practice that lives only in a scratchpad or a transcript is forgotten by the next session (owner 2026-09-23);
this check is what keeps "automatic" true.
"""
import json, pathlib, re, sys

HERE = pathlib.Path(__file__).resolve().parent
T = HERE / 'templates'
POINTER = 'MANDATORY-PRACTICES.md'

# role → (file, required patterns). Every brief must point at the practices file; the patterns are the practices
# that are cheapest to lose silently.
BRIEFS = {
    'fix-lane-implementer-brief.md': [r'claude-skills', POINTER, r'BelowNormal', r'whole Conformance'],
    'implementer-brief.md': [r'claude-skills', POINTER, r'BelowNormal', r'whole Conformance'],
    'lander-train-brief.md': [r'claude-skills', POINTER, r'STOP', r'tail -n \+1 -f', r'(?i)pipelin', r'push-main', r'REVIEW THE TRAIN'],
    'lander-brief.md': [r'claude-skills', POINTER, r'push-main'],
    'golden-lander-brief.md': [r'claude-skills', POINTER, r'push-main'],
    'registrar-brief.md': [r'claude-skills', POINTER, r'code site'],
    'wf_lane3_adjudicate.js': [r'claude-skills', r'args\.stopFile', r'GRACEFUL STOP', r'CHECKPOINT PER RULE', r"model: 'opus'", r"agentType: 'cobol-adjudicator'", r"agentType: 'cobol-refuter'"],
    'wf_lane3_refute.js': [r'claude-skills', r'args\.stopFile', r'GRACEFUL STOP', r"model: 'opus'", r"agentType: 'cobol-refuter'"],
    'dispatch-spec-implementer.md': [r'claude-skills', r'BelowNormal', r'NEVER run the whole Conformance', r'\\STOP',
                                     r'tail -n \+1 -f', r'where\.py', r'orient\.py', r'semgrep/verify\.py', r'cite\.py --check',
                                     r'Turn cap 220', r'code site', r'RUN BY NAME', r'drift_rules\.py'],
    # O2: the standard fix-lane dispatch — rolling pool, same-file successors, the graceful STOP, and the explicit
    # final StructuredOutput reminder (three agents in waves 65-67 ended without it and stranded finished branches).
    'wf_rolling_wave.js': [r'STOP', r"agentType: 'cobol-implementer'", r"agentType: 'cobol-lander'", r'StructuredOutput',
                           r'g\.after', r'held\[', r'push-main\.sh'],
}
# The group slug is w<wave><letter>, optionally followed by a successor ordinal (w68v2 = the second same-file
# cluster after group V), so the report path stays wave-and-group prefixed.
SPEC = BRIEFS['dispatch-spec-implementer.md'] + [r'reports\\w\d+[a-z]\d*-PB\d+-report\.md']


def check(path, pats):
    text = path.read_text(encoding='utf-8')
    return [p for p in pats if not re.search(p if p != POINTER else re.escape(POINTER), text)]


# P1: a mechanical role takes its model from its own frontmatter (Sonnet). A per-call `model` on its agent() call
# OVERRIDES that, which is how "model: 'opus' on every agent" silently put clerks on Opus (owner 2026-09-27).
MECHANICAL = re.compile(r"agentType:\s*'cobol-(?:clerk|locator)'")
CALL_MODEL = re.compile(r"\bmodel:\s*'")


def mechanical_model_overrides():
    """Every workflow template line (or agent() option object) that names a mechanical role AND passes a model."""
    found = []
    for js in [*T.glob('*.js'), *(HERE.parents[1] / 'workflows').glob('*.js')]:
        for i, line in enumerate(js.read_text(encoding='utf-8').splitlines(), 1):
            if MECHANICAL.search(line) and CALL_MODEL.search(line):
                found.append(f'{js.name}:{i}')
    return found


# O2: a file with more open notes than the cluster cap is split into same-file SUCCESSOR groups (`after:`), so the
# file's context is gathered once. Two groups whose `root` names the same primary src/ file with neither reachable
# from the other through `after:` would each re-orient on it (owner 2026-09-27: "minimize repetitive context gathering").
PRIMARY = re.compile(r'src/[\w./-]+\.(?:cs|g4)')


def same_file_without_successor(groups_path):
    groups = json.loads(groups_path.read_text(encoding='utf-8'))['groups']
    after = {g['letter']: g.get('after') for g in groups}

    def chain(letter):
        seen = []
        while letter and letter not in seen:
            seen.append(letter)
            letter = after.get(letter)
        return seen

    by_file = {}
    for g in groups:
        m = PRIMARY.search(g.get('root', ''))
        if m:
            by_file.setdefault(m.group(0), []).append(g['letter'])
    found = []
    for f, letters in by_file.items():
        for i, a in enumerate(letters):
            for b in letters[i + 1:]:
                if a not in chain(b) and b not in chain(a):
                    found.append(f'{a}+{b} on {f}')
    return found


def main():
    bad = 0
    over = mechanical_model_overrides()
    if over:
        bad += 1
        print(f'FAIL     mechanical role given a per-call model (P1 — it overrides its Sonnet frontmatter): {over}')
    groups_files = [pathlib.Path(a) for a in sys.argv[1:] if a.endswith('.json')]
    for gp in groups_files:
        clash = same_file_without_successor(gp)
        if clash:
            bad += 1
            print(f'FAIL     {gp.name}: groups share a primary file without a same-file successor (`after:`, O2): {clash}')
        else:
            print(f'ok       {gp.name}')
    specs = [a for a in sys.argv[1:] if not a.endswith('.json')]
    if len(sys.argv) > 1:
        targets = [(pathlib.Path(a), SPEC) for a in specs]
    else:
        targets = [(T / f, pats) for f, pats in BRIEFS.items()]
    for path, pats in targets:
        if not path.exists():
            print(f'MISSING  {path}'); bad += 1; continue
        miss = check(path, pats)
        if miss:
            bad += 1
            print(f'FAIL     {path.name}: missing {miss}')
        else:
            print(f'ok       {path.name}')
    print('=== PRACTICES CHECK: ' + ('GREEN' if not bad else f'RED ({bad})') + ' ===')
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main())
