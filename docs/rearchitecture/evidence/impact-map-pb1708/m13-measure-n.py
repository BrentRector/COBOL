"""PB1720: measure N — the lander's whole-Conformance leg with N concurrent implementer gates (builds included).

For each configuration: touch one compiler source in each of the N measurement worktrees (so each implementer gate
really rebuilds the compiler and its dependents), then start the N implementer gates (BelowNormal, their own
worktrees, COBOLNET_GATE_SLOTS=3 so none queues; every gate COLD, COBOLNET_COMPILE_CACHE=off, as after a compiler change) and the lander gate (Normal) at the same moment. The lander's
Conformance wall comes from its verdict.json. Results go to pb1720/results.json.
"""
import json, os, subprocess, sys, time
from pathlib import Path

SCR = Path(r'E:\Temp\claude\E--COBOL\73cac64c-391d-42c0-bcdf-c880e3e10305\scratchpad\w72h')
LANDER = Path(r'E:\COBOL\.claude\worktrees\wf_10d5c11f-c83-2')
IMPL = [SCR / 'm1', SCR / 'm2', SCR / 'm3']
OUT = SCR / 'pb1720'
OUT.mkdir(exist_ok=True)
TOUCH = Path('src/Cobol.Net.Compiler/Binding/DataBinder.cs')


def host_load():
    ps = subprocess.run(['pwsh', '-NoProfile', '-Command',
                         "(Get-Process dotnet,testhost,MSBuild,VBCSCompiler -ErrorAction SilentlyContinue | Measure-Object).Count;"
                         "(Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average).Average"],
                        capture_output=True, text=True)
    return ps.stdout.split()


def newest_run(wt: Path) -> dict:
    runs = sorted((wt / 'TestResults' / 'build-local').iterdir())
    return json.loads((runs[-1] / 'verdict.json').read_text(encoding='utf-8'))


def one(label: str, n: int) -> dict:
    load_before = host_load()
    procs = []
    for wt in IMPL[:n]:
        f = wt / TOUCH
        os.utime(f, None)
        env = {**os.environ, 'COBOLNET_GATE_SLOTS': '3', 'COBOLNET_COMPILE_CACHE': 'off'}
        log = open(OUT / f'{label}-{wt.name}.log', 'w', encoding='utf-8')
        procs.append((wt, subprocess.Popen([sys.executable, 'scripts/run_gate_legs.py', '--mode', 'implementer'],
                                           cwd=wt, env=env, stdout=log, stderr=subprocess.STDOUT,
                                           creationflags=subprocess.BELOW_NORMAL_PRIORITY_CLASS), log))
    t0 = time.monotonic()
    llog = open(OUT / f'{label}-lander.log', 'w', encoding='utf-8')
    lander = subprocess.run([sys.executable, 'scripts/run_gate_legs.py', '--mode', 'lander'], cwd=LANDER,
                            env={**os.environ, 'COBOLNET_COMPILE_CACHE': 'off'}, stdout=llog, stderr=subprocess.STDOUT)
    lander_wall = time.monotonic() - t0
    llog.close()
    impl = []
    for wt, p, log in procs:
        p.wait()
        log.close()
        v = newest_run(wt)
        impl.append({'worktree': wt.name, 'rc': p.returncode, 'verdict': v['verdict'], 'timings': v['timings']})
    v = newest_run(LANDER)
    conf = next(r for r in v['runs'] if r['asm'] == 'Conformance')
    res = {'label': label, 'n': n, 'load_before': load_before, 'lander_rc': lander.returncode,
           'lander_verdict': v['verdict'], 'lander_conformance_s': conf['wall_s'], 'lander_timings': v['timings'],
           'lander_process_wall_s': round(lander_wall, 1), 'implementers': impl}
    print(json.dumps(res), flush=True)
    return res


results = []
for label, n in [('quiet-a', 0), ('n1', 1), ('n2', 2), ('n3', 3), ('quiet-b', 0)]:
    results.append(one(label, n))
    (OUT / 'results.json').write_text(json.dumps(results, indent=1), encoding='utf-8')
quiet = (results[0]['lander_conformance_s'] + results[-1]['lander_conformance_s']) / 2
for r in results:
    r['ratio_to_quiet'] = round(r['lander_conformance_s'] / quiet, 3)
(OUT / 'results.json').write_text(json.dumps({'quiet_mean_s': quiet, 'runs': results}, indent=1), encoding='utf-8')
print('=== PB1720 MEASUREMENT DONE: quiet', round(quiet, 1), 's; ratios',
      [(r['label'], r['ratio_to_quiet']) for r in results], '===', flush=True)
