#!/usr/bin/env python3
"""Fail when a brief or a rendered dispatch spec drops a MANDATORY practice (templates/MANDATORY-PRACTICES.md).

    python .claude/skills/workstream/check_practices.py              # every brief in templates/
    python .claude/skills/workstream/check_practices.py <spec.txt…>  # rendered implementer specs

A practice that lives only in a scratchpad or a transcript is forgotten by the next session (owner 2026-09-23);
this check is what keeps "automatic" true.
"""
import pathlib, re, sys

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
}
SPEC = BRIEFS['dispatch-spec-implementer.md'] + [r'reports\\w\d+[a-z]-PB\d+-report\.md']


def check(path, pats):
    text = path.read_text(encoding='utf-8')
    return [p for p in pats if not re.search(p if p != POINTER else re.escape(POINTER), text)]


def main():
    bad = 0
    if len(sys.argv) > 1:
        targets = [(pathlib.Path(a), SPEC) for a in sys.argv[1:]]
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
