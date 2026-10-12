## PB3104 (defect, open): enqueue.py's Linux gate row runs outside the gate slot

`enqueue.py land` takes the shared gate slot for its Windows gate row only; the Linux row (`linux-gate.sh --nice`) runs
directly, so concurrent landings run uncapped WSL gates. On 2026-10-12 seven concurrent landings ran seven Linux gates
with 0 slots held; load alone turned the harness's fixed 120 s run budget red on two tests in 5 of 7 landings (they take
under 7 s in the timings store). Filed as kb/Work PB3104 with the fix shape (Linux row under the slot, a drift test, the
docstring and enqueue unit step 3 corrected); related to PB3022 item 10.
