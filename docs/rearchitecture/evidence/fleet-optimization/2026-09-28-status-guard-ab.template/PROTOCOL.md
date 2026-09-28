# Fleet engineering protocol

You are one agent in a fleet of engineers working on this repository. Other agents may take over your work at any
moment: a session limit, an API error or an orchestrator decision can stop you mid-step, and a fresh agent then
continues from what you left on disk. Everything below exists so that hand-over is cheap and safe.

## 1. Scope

1.1 Work only on the task you were given. If you find an unrelated problem, note it in your final report as a lead
    (file, line, one sentence); do not fix it.
1.2 Do not change a test to make it pass. A failing test is evidence about the code. If you believe a test is wrong,
    say why in your report and leave it failing.
1.3 Do not add dependencies. The standard library is enough for everything here.
1.4 Keep each change as small as the root cause allows, but fix the root cause, not the symptom.

## 2. Reading the code

2.1 Read the failing test first, then the code it exercises. Reproduce the failure before changing anything.
2.2 Prefer reading the exact function over reading whole files.
2.3 When two tests fail for what looks like one reason, confirm it by fixing one and re-running both.

## 3. Making changes

3.1 One bug per change. Do not fold two unrelated fixes into one edit.
3.2 After each change, run the whole suite (`python -m unittest -q`), not only the test you were fixing.
3.3 If a change makes another test fail, stop and understand why before continuing.
3.4 Match the surrounding style: names, quoting, spacing, docstring form.

## 4. Commits (checkpoints)

4.1 Commit after every fix, with `git add -A` then `git commit -m "WIP checkpoint: <what changed and why>"`.
4.2 Never amend or rewrite a commit; history is how the next agent learns what you did.
4.3 Never commit a failing suite unless the commit message says which test still fails and why.
4.4 Commit messages name the function changed and the behavior before and after.

## 5. The handoff file

5.1 STATUS.md at the repository root is the handoff. It is listed in .gitignore and never committed.
5.2 Its first line is `STATUS-AT: <full sha of HEAD, from git rev-parse HEAD>`: the commit it describes.
5.3 After EVERY commit, rewrite STATUS.md so it describes that commit: DONE (what is committed, one line per fix),
    NEXT (the exact next step), BLOCKED (anything you cannot resolve), GATE (the last gate verdict line).
5.4 A handoff that does not describe HEAD is worse than none: the next agent will trust it.

## 6. The gate

6.1 The gate is `python gate.py`. It prints one verdict line, `GATE: GREEN` or `GATE: RED (<reasons>)`.
6.2 When the suite passes, run the gate. A RED gate is part of the task: fix each reason, one commit per reason
    class, and run the gate again until it is GREEN.
6.3 Read the verdict line itself. Never infer a verdict from an exit code or from silence.
6.4 The house rules the gate checks are in CONTRIBUTING.md.

## 7. Communication

7.1 Your final message is a report of at most ten lines: what you fixed (one line each), the final gate line, and
    any leads (rule 1.1).
7.2 Do not ask questions; there is no one to answer. Decide, and say what you decided in the report.
7.3 Do not describe work you did not do. If something is unverified, say so.

## 8. Tools

8.1 Use git, python and file reads/edits. Do not install anything.
8.2 Do not run commands that modify files outside this repository.
8.3 Prefer one command per step; do not chain a verdict command with the next step.

## 9. When you are stopped

9.1 If you are told to stop, commit what is working, rewrite STATUS.md with the exact NEXT step, and report.
9.2 A fresh agent resuming your work reads STATUS.md and only the commits after its stamp.
