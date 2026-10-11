#!/usr/bin/env python3
"""Render implementer/finisher dispatch specs from ONE template (MANDATORY-PRACTICES.md O1).

    python .claude/skills/workstream/make_dispatch_specs.py <groups.json>

groups.json:
{
  "wave": "58", "scratch": "E:\\\\Temp\\\\...\\\\scratchpad", "base": "c54434a8d or later — ...",
  "stop_file": "E:\\\\Temp\\\\...\\\\scratchpad\\\\STOP-w58",   # REQUIRED: this fleet's own stop (kb/Work PB2483)
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

Every spec names TWO stop files (kb/Work PB2483): the owner's global stop (`coord.global_stop()`) and this fleet's own
`stop_file` (`coord.fleet_stop(scratch, scope)`; plan_wave.py writes it). A fleet stopped by its own file never stops
another session's agents.

Every spec's report path is in `coord.reports_dir()`, never under the wave's `scratch` (kb/Work PB2980): the land unit
and the branch survey read that one directory, and a report anywhere else is never landed.
"""
import json, pathlib, subprocess, sys

HERE = pathlib.Path(__file__).resolve().parent
TEMPLATE = HERE / 'templates' / 'dispatch-spec-implementer.md'
sys.path.insert(0, str(HERE.parents[2] / 'scripts' / 'orchestrator'))
import coord  # noqa: E402  (the one definition of the stop files and the reports directory)


def render(cfg, g, tpl=None):
    """One group's spec: the template filled from the groups file `cfg` and the group `g` (the one render; the planner's
    self-test calls it too). Raises KeyError naming a placeholder the inputs do not fill."""
    tpl = tpl if tpl is not None else TEMPLATE.read_text(encoding='utf-8')
    if not cfg.get('stop_file'):
        raise KeyError("stop_file (this fleet's own STOP-<scope>, kb/Work PB2483; plan_wave.py writes it)")
    pred = g.get('pred', '')
    return tpl.format(wave=cfg['wave'], base=cfg['base'], S=str(cfg['scratch']), pred=(pred + '\n') if pred else '',
                      global_stop=str(coord.global_stop()), stop_file=cfg['stop_file'], reports=str(coord.reports_dir()),
                      **{k: v for k, v in g.items() if k != 'pred'})


def main():
    cfg = json.loads(pathlib.Path(sys.argv[1]).read_text(encoding='utf-8'))
    tpl = TEMPLATE.read_text(encoding='utf-8')
    scratch = pathlib.Path(cfg['scratch'])
    out = []
    for g in cfg['groups']:
        try:
            body = render(cfg, g, tpl)
        except KeyError as e:
            print(f"{sys.argv[1]}: group {g.get('letter')}: no value for {e}")
            return 1
        # Keyed on the FULL letter, lower-cased: the workflow opens msg-w<wave>-<letter.toLowerCase()>.txt, and a
        # multi-character letter (a same-file successor such as "V2") must not collide with another group's file.
        p = scratch / f"msg-w{cfg['wave']}-{g['letter'].lower()}.txt"
        p.write_text(body, encoding='utf-8')
        out.append(str(p))
        print('wrote', p)
    return subprocess.call([sys.executable, str(HERE / 'check_practices.py'), sys.argv[1], *out])


if __name__ == '__main__':
    sys.exit(main())
