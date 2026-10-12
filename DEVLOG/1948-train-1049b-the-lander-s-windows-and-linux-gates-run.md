## Entry 1948 — 2026-10-09 10:40 PDT — Train 1049b: the lander's Windows and Linux gates run concurrently (PB2877); PB2878 filed

Owner, 2026-10-09: "Why don't we run the Windows local and the WSL local gates in parallel? I.e. Concurrently?" — and
"I approve this one-change train". The Linux gate (`scripts/linux-gate.sh` under WSL) builds and tests its own clone of
the committed HEAD and shares nothing with the Windows lander gate, which holds no gate slot; the sequence was a
fail-fast choice that saved almost nothing (a red is fixed and both gates re-run anyway) and cost the sum of the two.
Measured on 2026-10-09: alone, the Windows lander gate takes ~4.1 min and the Linux gate ~5.9 min (~10 min in
sequence); fully overlapped on this train they slowed each other by sharing the CPU (7.1 and 7.3 min), so the gates took
7.3 min instead of ~10 (~27 % less). A first, partial overlap on train 1049 had suggested no slowdown. MANDATORY-PRACTICES L10 and step 3b of lander-train-brief.md and lander-brief.md
now start the Linux gate in the background the moment every cluster is committed; both must be GREEN on the same head,
and a commit made to fix either red re-runs both. I8 (the implementer's Linux gate) is unchanged until measured under
the gate-slot cap. `check_practices.py` GREEN. PB2877 landed.

PB2878 filed (analysis, owner: "Should this require a full CI run? I would think not"): `main` requires `ci-gate` on
every commit, and here the practices files, briefs, notes and docs are test inputs, so a docs-only change can still turn
CI red; the design question is a path-classified fast lane for landings confined to paths no test reads, proved by a
drift test and applied identically by the local gates (the CI invariant).

This train's own gates ran concurrently under the new rule.

