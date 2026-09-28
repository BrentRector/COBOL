#!/usr/bin/env python3
"""Render implementer/finisher dispatch specs from ONE template (MANDATORY-PRACTICES.md O1).

    python .claude/skills/workstream/make_dispatch_specs.py <groups.json>

groups.json:
{
  "wave": "58", "scratch": "E:\\\\Temp\\\\...\\\\scratchpad", "base": "c54434a8d or later — ...",
  "groups": [
    {"letter": "KA", "slug": "w58a", "group": "INTRINSICS BEYOND BINARY64", "notes": "PB999, PB1000", "lead": "PB999",
     "codes": "COBOLNET2400–COBOLNET2402", "root": "...", "files": "`kb/Work/PB999.md`, ...", "body": "...",
     "pred": "",          # optional: predecessor-branch / resume instruction, rendered into the spec
     "after": "K"}        # optional: SAME-FILE SUCCESSOR of group K — ignored here; wf_rolling_wave.js runs this group
                          # only after K returns and hands it K's branch and report (workstream SKILL §2)
  ]
}
Writes <scratch>\\msg-w<wave>-<letter lower-cased>.txt per group (the file wf_rolling_wave.js opens) and runs
check_practices.py over the specs AND the groups file (exit 1 on a miss).
"""
import json, pathlib, subprocess, sys

HERE = pathlib.Path(__file__).resolve().parent
TEMPLATE = HERE / 'templates' / 'dispatch-spec-implementer.md'


def main():
    cfg = json.loads(pathlib.Path(sys.argv[1]).read_text(encoding='utf-8'))
    tpl = TEMPLATE.read_text(encoding='utf-8')
    scratch = pathlib.Path(cfg['scratch'])
    out = []
    for g in cfg['groups']:
        pred = g.get('pred', '')
        body = tpl.format(wave=cfg['wave'], base=cfg['base'], S=str(scratch), pred=(pred + '\n') if pred else '',
                          **{k: v for k, v in g.items() if k != 'pred'})
        # Keyed on the FULL letter, lower-cased: the workflow opens msg-w<wave>-<letter.toLowerCase()>.txt, and a
        # multi-character letter (a same-file successor such as "V2") must not collide with another group's file.
        p = scratch / f"msg-w{cfg['wave']}-{g['letter'].lower()}.txt"
        p.write_text(body, encoding='utf-8')
        out.append(str(p))
        print('wrote', p)
    return subprocess.call([sys.executable, str(HERE / 'check_practices.py'), sys.argv[1], *out])


if __name__ == '__main__':
    sys.exit(main())
