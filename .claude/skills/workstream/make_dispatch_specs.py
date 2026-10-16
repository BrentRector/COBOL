#!/usr/bin/env python3
"""Render implementer/finisher dispatch specs from ONE template (MANDATORY-PRACTICES.md O1).

    python .claude/skills/workstream/make_dispatch_specs.py <groups.json> [--coord DIR]

groups.json:
{
  "wave": "58", "scratch": "E:\\\\Temp\\\\...\\\\scratchpad", "base": "c54434a8d or later — ...",
  "stop_file": "E:\\\\Temp\\\\...\\\\scratchpad\\\\STOP-w58",   # REQUIRED: this fleet's own stop (kb/Work PB2483)
  "groups": [
    {"letter": "KA", "slug": "w58a", "group": "INTRINSICS BEYOND BINARY64", "notes": "PB999, PB1000", "lead": "PB999",
     "codes": "COBOLNET2400–COBOLNET2402", "root": "...", "files": "`kb/Work/PB999.md`, ...", "body": "...",
     "file_set": ["src/...", ...],   # the files the group changes, a LIST (plan_wave.py writes it); when absent, the
                                      # set computed now from its notes (plan_wave.computed_file_set)
     "pred": "",          # optional: predecessor-branch / resume instruction, rendered into the spec
     "after": "K"}        # optional: SAME-FILE SUCCESSOR of group K — ignored here; wf_rolling_wave.js runs this group
                          # only after K returns and hands it K's branch and report (workstream SKILL §2)
  ]
}
Writes <scratch>\\msg-w<wave>-<letter lower-cased>.txt per group (the file wf_rolling_wave.js opens), RECORDS every
rendered group in the dispatch ledger (<coord>/inflight-groups.json) through plan_wave.record_dispatch, an upsert on
(wave, letter) with the group's notes and file set, and runs check_practices.py over the specs AND the groups file
(exit 1 on a miss).

The ledger has exactly two structured writers, this renderer (every wave, the planner's and a hand-planned one alike)
and `plan_wave.py --release` (an aborted dispatch); it is never written from a scrape of a prompt (kb/Work PB3186: the
retired guard-hook recorder took every note id a brief mentioned and recorded six entries of 303-910 files, which bound
fix-lane worktrees to reference notes and stopped their landings). The planner's file-set partition and the landing
check read it (plan_wave.inflight_file_sets, source 3).

Every spec names TWO stop files (kb/Work PB2483): the owner's global stop (`coord.global_stop()`) and this fleet's own
`stop_file` (`coord.fleet_stop(scratch, scope)`; plan_wave.py writes it). A fleet stopped by its own file never stops
another session's agents.

Every spec's report path is in `coord.reports_dir()`, never under the wave's `scratch` (kb/Work PB2980): the enqueue unit
and the branch survey read that one directory, and a report anywhere else is never landed.
"""
import argparse, json, pathlib, re, subprocess, sys

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[2]
TEMPLATE = HERE / 'templates' / 'dispatch-spec-implementer.md'
sys.path.insert(0, str(REPO / 'scripts' / 'orchestrator'))
sys.path.insert(0, str(REPO / 'scripts' / 'spec'))
import coord  # noqa: E402  (the one definition of the stop files and the reports directory)
import plan_wave  # noqa: E402  (record_dispatch: THE ledger writer; computed_file_set: THE computed file set)
import work  # noqa: E402  (the register: which notes are terminal)

PB = re.compile(r'\bPB\d+\b')


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


def record(cfg, coord_dir, terminal=None, file_set_of=None):
    """Record every group of the groups file `cfg` in the dispatch ledger (plan_wave.record_dispatch, an upsert on
    (wave, letter)) and return the entries written. A group's notes are its `notes` field's ids; its files are its
    `file_set` LIST, or, when a hand-built groups file carries none, `file_set_of(note ids)` (default
    plan_wave.computed_file_set over this tree, on the planner's index policy: THE index of the tree, cached in
    `coord_dir`, kb/Work PB3203). `terminal(note id)` defaults to the register's landed or retired."""
    if terminal is None:
        status = {i.get('id'): i.get('status') for i in work.load()}
        terminal = lambda n: status.get(n) in work.TERMINAL_STATUSES  # noqa: E731
    policy = plan_wave.IndexPolicy.of(coord_dir)
    file_set_of = file_set_of or (lambda ids: plan_wave.computed_file_set(REPO, ids, policy))
    groups = []
    for g in cfg['groups']:
        notes = list(dict.fromkeys(PB.findall(g.get('notes', ''))))
        files = g['file_set'] if isinstance(g.get('file_set'), list) else sorted(file_set_of(notes))
        groups.append({'letter': g['letter'], 'slug': g.get('slug'), 'notes': notes, 'files': files})
    return plan_wave.record_dispatch(coord_dir, str(cfg['wave']), groups, terminal)


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('groups', help='the groups file (groups.json)')
    ap.add_argument('--coord', help=f'coordination directory of the dispatch ledger (default ${coord.ENV} or {coord.DEFAULT})')
    a = ap.parse_args(argv)
    cfg = json.loads(pathlib.Path(a.groups).read_text(encoding='utf-8'))
    tpl = TEMPLATE.read_text(encoding='utf-8')
    scratch = pathlib.Path(cfg['scratch'])
    out = []
    for g in cfg['groups']:
        try:
            body = render(cfg, g, tpl)
        except KeyError as e:
            print(f"{a.groups}: group {g.get('letter')}: no value for {e}")
            return 1
        # Keyed on the FULL letter, lower-cased: the workflow opens msg-w<wave>-<letter.toLowerCase()>.txt, and a
        # multi-character letter (a same-file successor such as "V2") must not collide with another group's file.
        p = scratch / f"msg-w{cfg['wave']}-{g['letter'].lower()}.txt"
        p.write_text(body, encoding='utf-8', newline="\n")
        out.append(str(p))
        print('wrote', p)
    ledger = coord.coord_dir(a.coord)
    for e in record(cfg, ledger):
        print(f"recorded {e['slug']} ({', '.join(e['notes'])}): {len(e['files'])} files in {ledger / plan_wave.DISPATCH_LEDGER}")
    return subprocess.call([sys.executable, str(HERE / 'check_practices.py'), a.groups, *out])


if __name__ == '__main__':
    sys.exit(main())
