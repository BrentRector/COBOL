## Entry 2136 — 2026-10-16 20:38 PDT — PB3315 filed: the gate's rendered-views audit spends 20-28 minutes in gen_ledger.py --check under load

Finisher w1105f measured the gate's pre-slot `audit RENDERED VIEWS` at 1687.4 s and 1210.4 s with about 10 gates on the machine, nearly all of it `rendered_views.py --check` waiting on the git subprocesses of `gen_ledger.py --check`. The first run passed the harness's 30-minute background limit and died before taking a gate slot. No code changed; `kb/Work/PB3315.md` records the evidence, the code sites and the fix shape (measure, then make the check cheap or move it out of the pre-slot path, never a longer timeout).

