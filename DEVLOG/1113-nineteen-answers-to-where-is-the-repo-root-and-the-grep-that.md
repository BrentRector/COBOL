## Entry 1113 - 2026-07-29 19:05 PDT - Nineteen answers to "where is the repo root", and the grep that found only thirteen

⚠ **Timestamp note.** Entry 1112 above is stamped `21:40 PDT` and §0 said `21:45 PDT`, both on this same day, but
`date "+%Y-%m-%d %H:%M %Z"` returns **19:05 PDT** as I write this. So 1112's stamp is roughly two and a half hours
in the future and this entry, correctly stamped, reads as EARLIER than the entry above it. I have used the real
clock rather than manufacture a later time to preserve the visual ordering, because the rule says stamp from
`date` and a fabricated timestamp is worse than a visibly odd one. Something mis-stamped 1112; worth a glance
before it propagates.

**WHAT I WAS DOING.** Starting the Phase-14 Step-0 inventory work, which needed a new drift test in
`Cobol.Net.Tests.Unit`. Every such test begins by finding the repo root. I went to copy the idiom from a
neighbouring test and found I had a choice of four.

**WHAT WAS THERE.** `RepoRoot()` existed **nineteen times** across five test projects, in **seven** distinct
mechanisms:

| mechanism | sites |
|---|---|
| walk up for `tests/version-matrix` | 6 |
| walk up for `tests/nist` | 4 |
| walk up for `CobolSharp.sln` | 2 |
| walk up for `src/Cobol.Net.Compiler` | 2 |
| walk up for `PROMPT.md` | 1 |
| walk up for `.git` (from `Directory.GetCurrentDirectory()`, not the assembly) | 1 |
| count `".."` hops from `AppDomain.CurrentDomain.BaseDirectory` | 3 |

Not one of them was wrong on the day it was written. They accumulated because writing a six-line walker is
cheaper in the moment than finding the one that already exists — which is precisely the mechanism
`feedback_one_rule_one_place` describes, arriving here as ordinary sediment rather than as a bug.

Every sentinel is a PROXY for the root, and each proxy is a separate way for the answer to drift: rename
`tests/version-matrix` and six tests break while thirteen carry on. The `".."`-counting sites are worse than
that — they encode the OUTPUT DIRECTORY DEPTH, so they break on a TargetFramework change that touches no test.

**THE FIX.** One `TestRepo` in `tests/_shared/TestRepo.cs`, linked into every project under `tests/` by a new
`tests/Directory.Build.props`. Linked rather than referenced as a project so a NEW test project inherits it with
no wiring at all — the point is not to remove nineteen copies once, it is to make the twentieth not worth
writing. Its marker is `CobolSharp.sln` CORROBORATED by `src/` and `tests/` beside it, because a lone `.sln` can
be copied into a build drop and a walker that accepted it would resolve to a tree with no sources and fail with a
file-not-found raised far from the cause. Typed helpers (`Src`, `Tests`, `Docs`, `Scripts`, `Specs`,
`VersionMatrix`, `Nist`) replace the `Path.Combine(RepoRoot(), "tests", "version-matrix", …)` chains, so the call
sites got shorter as well as singular.

`EditionHarness.RepoRoot()` and `CharacterizationCorpus.RepoRoot` were PUBLIC — the de-facto shared ones — and
both are deleted outright with their callers rewritten, not left as forwarders
(`feedback_change_the_dispatch_not_the_callers`).

**⛔ THE PART WORTH REMEMBERING: MY FIRST DRIFT TEST WOULD HAVE BLESSED SIX OF THE NINETEEN.** I wrote the guard
before the sweep finished, keying it on the shape I had seen: `AppContext.BaseDirectory` near `.Parent`. That is
thirteen of the nineteen. The `.git` walker starts from `Directory.GetCurrentDirectory()`, and the three
hop-counters start from `AppDomain.CurrentDomain.BaseDirectory` and never touch `.Parent` at all. My *grep* had
the same blind spot, which is why I reported "sixteen sites" to the owner and the real number is nineteen — a
fan-out agent found the other three by reading the files rather than matching my pattern.

The test now pairs ANY base-directory anchor (`AppContext.BaseDirectory`, `AppDomain.CurrentDomain.BaseDirectory`,
`Directory.GetCurrentDirectory(`) with ANY upward move (`.Parent`, `Directory.GetParent`, `".."`). It is the
COMBINATION that means "finding the root the long way round"; either half alone stays legal. Generalising from
the instances you happened to find is how a guard ends up certifying the cases it never saw.

**⚠ AND A TOOLING TRAP, found by an adversarial verifier and worth carrying.**
`tests/Cobol.Net.Tests.Conformance/DifferentialGolden.cs` contains a NUL byte at offset 4895, so **ripgrep treats
it as binary and SILENTLY SKIPS IT** — a `Grep` for `TestRepo\.` returned 19 hits and omitted that file's use
entirely. `git grep` at least prints `Binary file … matches`. Any sweep over `tests/` that relies on ripgrep alone
has a hole exactly there. The C# drift test does not: it reads with `File.ReadAllText`, so the guard is sound even
though the grep that motivated it was not.

**GATES.** Solution build clean (0 warnings, `TreatWarningsAsErrors`). Unit **953/953** — the 942 baseline plus
exactly the 11 tests added here and in the next entry. Characterization **33/33**. And the FULL Conformance suite
**4137/4137, zero skipped, nothing red**, matching the §0 baseline figure exactly — run because this touched
eleven Conformance files including `EditionHarness`, which every test in that project reaches through, and a
filtered gate would not have covered the blast radius.

