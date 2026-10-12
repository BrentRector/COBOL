## Entry 1949 — 2026-10-09 11:05 PDT — Train 1049c: the Linux gate builds once and runs its legs in parallel (PB2879); PB2880 filed

Owner, 2026-10-09: "According to Task Manager, the WSL gate is only takes roughtly 10% of the CPU", then "Let's make
this change now: Do it in such a way we lose as little as possibly if we hit the limit". The WSL VM sees 32 CPUs and
62 GB; `scripts/linux-gate.sh` ran its six legs one after another, each test leg with its own build. It now builds the
clone ONCE (`dotnet build Cobol.Net.sln`, 24 s) and runs selftests (beside the build), unit, characterization,
conformance, guard and selftests-built in the background against that build; each leg writes `<leg>.result` and
`<leg>.status`, printed in a fixed order after `wait`, and the one `=== LINUX GATE: … ===` verdict line is unchanged,
so no brief or check changes. `SelfTestDiscoveryDriftTests` accepts the redirected background runner calls (still the
one runner, no self-test named by hand); the first parallel run caught exactly that drift (RED), fixed here. The work
went to the branch as pushed checkpoints (plan, code, drift fix), so a quota cutoff would have lost nothing.

Measured alone: 234 s against 351 s serial, 33 % less. But each leg took about twice as long as alone (unit 169 s
against 84, conformance 151 against 63, guard 132 against 69, selftests-built 155 against 38) while the VM was 80 % idle
at a load average of 80, and the owner still saw ~9 % CPU: the legs contend for something that is not CPU. PB2880 is
filed (analysis) to find that mechanism by measurement. PB2879 landed.

