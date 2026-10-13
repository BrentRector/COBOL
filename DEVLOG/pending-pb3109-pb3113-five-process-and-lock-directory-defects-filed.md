## PB3109-PB3113: five defects filed from enqueue unit 1647's leads

PB3109 (runtime/io-sharing, MAJOR, wrong answer and silent): on Windows the lock-file host's lock directory goes delete-pending when its last holder closes, and a waiting region-lock hold answers Unavailable; it bounced PR 19 out of the merge queue. Source: w1069c-PB2690-report.md.

PB3110 (orchestrator/gate, MINOR): an implementer's own `linux-gate.sh --nice` (I8) takes no gate slot, the sibling of PB3104's slotted landing gates. Source: w1069m-PB3104-report.md.

PB3111 (orchestrator/devlog, MINOR): an implementer that merges an unlanded notes branch rewrites its pending DEVLOG entry and later conflicts on the stamped one. Source: w1069m-PB3104-report.md.

PB3112 (tests/gate, MAJOR): the Sweep-trait drift check runs 7.2 s on Windows, over the tier 0c cutoff, so it is outside leg 1 and a missing trait goes red only at the Linux gate or in CI. Source: w1069l-PB3103-report.md.

PB3113 (orchestrator/landing, MINOR): `land_lane.pid_alive` trusts a reused pid and the landing lane reads RUNNING. Source: w1069l-PB3103-report.md.
