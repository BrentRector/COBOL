## Entry 1981 — 2026-10-12 17:00 PDT — PB3104: every gate row of `enqueue.py land` runs under the shared gate slot

`enqueue.py land` took a gate slot only for its Windows gate row. That row runs build-local, which takes its slot through
`run_gate_legs.py`. The Linux row, `bash scripts/linux-gate.sh --nice` (CI's whole legs in WSL), ran with no slot at
all. On 2026-10-12 the enqueue unit started seven landings at once. Their seven Linux gates ran together while
`gate_slot.py status` showed no slot held, and Linux build times stretched from 49 s to 788 s. The harness's fixed
120 s run budget then failed two tests in five of the seven landings: `RelationPairShapeTests` ran for 8-17 min and
`ActivationResourcesTests` for about 4.5 min, against 7 s and 4 s in the timings store. Four branches whose review was
CLEAN were blocked by load alone.

**Re-probe.** On this branch, before the fix, the Linux row of `GATES_WINDOWS` was bare, and `linux-gate.sh` takes no
slot (it names `gate_slot` only in a comment).

**Fix.** Each gate row is now a `GateRow(host, command, prefix, slot)` with one of two slot values:
- `own`: the command takes the slot itself. build-local does, through `run_gate_legs.py`.
- `wrap`: `run_gates` runs the command under `gate_slot.py run --repo <worktree> --label "enqueue <branch>: <host> gate"`.

The Linux row is `wrap`. `unslotted()` is the single rule. It refuses a bare row, an unknown slot value, an `own` claim
whose script does not hand the gate to the driver, and a driver that no longer acquires a slot. `run_gates` runs no row
while any row is refused. Every row still runs with PB3103's landing class, so the Linux row's ticket is served ahead
of waiting implementer gates. A live probe on the Windows host (`gate_slot.py run -- bash -c 'uname -s'`) took a slot
and ran in WSL inside the slot's Job object.

I chose one slot per row over one slot held across both rows. build-local takes its own slot after its audits, so an
outer slot around it would count against the same cap a second time. Three concurrent landings at cap 3 would then
each hold an outer slot and wait forever for an inner one.

**Drift test.** `enqueue.py --self-test` gains three arms:
1. the wrapped row runs under `gate_slot.py run` with the landing class, and takes its slot before its command starts;
2. every row of `GATES_WINDOWS` and `GATES_LINUX` takes the slot;
3. planted unslotted rows are refused and nothing runs.

`LandingPipelineDriftTests` holds the three arms by name, along with PB3103's landing-class arm. With the bare pre-fix
row planted back, arms 1 and 2 went red.

**Docs.** The module docstring step 3, DESIGN-test-build-ci.md §3.17.2 step 3 and the caller list in `gate_slot.py`
now describe the slotted rows. `units/enqueue.md` step 3 needed no change: the fix made it accurate.

**Not in scope; recorded as leads.** The harness's fixed 120 s run budget, which is a wall-clock limit under load;
PB3022 item 10; and an implementer's own `linux-gate.sh` run under scope `whole`, which still takes no slot.

**Register.** kb/Work PB3104 is landed with `closes_rows: []`: it is process tooling, governed by no ISO rule.

