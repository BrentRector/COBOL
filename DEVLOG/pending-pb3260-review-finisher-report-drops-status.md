## PB3260 filed: the review finisher's report rewrite drops the Status line, and the wave reads a DONE as SPLIT

In wave 1093, group D's review finisher (PB651) answered both round-1 findings, gated GREEN at `dafae64ea` and
returned DONE. Its rewritten report had no `**Status:**` line, though. The rolling wave's finished test
(`plan_wave.py --finished`, kb/Work PB3181) read it as NOT FINISHED, so the wave recorded the return as SPLIT and
dispatched no re-review. The cause is the review-finisher prompt (`wf_rolling_wave.js#fixFindings`): it lists the
header as worktree, branch, base and HEAD and leaves out Status. Filed as kb/Work PB3260 (MINOR, tooling). The fix
names the Status line and has every DONE agent run the finished test on its own report before returning. The wave
unit put the header line back from the structured return, so the branch goes to review instead of a finisher. No
code changed in this entry.
