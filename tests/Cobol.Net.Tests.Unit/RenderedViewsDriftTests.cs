// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE TREE HOLDS SOURCES, VIEWS ARE RENDERED (kb/Work PB3026; DESIGN-test-build-ci.md §3.17.4). Every generated
/// index is a row of <c>VIEWS</c> in <c>scripts/rendered_views.py</c>, and none may be tracked: a committed file that every
/// landing touching its inputs regenerates (<c>docs/DRIFT_RULES.md</c>'s count line, <c>docs/DIAGNOSTICS.md</c>) collides
/// when two pull requests are queued, and one nobody regenerates goes stale (the burn-down was STALE in the probe for
/// days). So <c>git ls-files</c> ∩ the registered outputs is empty, every in-tree output is gitignored (a render must not
/// dirty the checkout), and no tracked <c>docs/</c> or <c>kb/</c> markdown declares itself generated without a row. The
/// rule lives ONCE, in the script (<c>--tree</c>); this test asks it, so the gate's <c>RENDERED VIEWS</c> audit, CI's
/// <c>audits</c> job and this test cannot disagree. Each view's own render check is its generator's <c>--check</c> (or the
/// test the registry names for a C# renderer).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class RenderedViewsDriftTests
{
    [Fact]
    public void NoRenderedViewIsTracked_EveryOneIsIgnored_AndNoGeneratedFileIsUnregistered()
    {
        string script = TestRepo.Scripts("rendered_views.py");
        Assert.True(File.Exists(script), $"the registry of rendered views is missing: {script}");

        ProcessObservation r = PythonInstrument.Run(script, "--tree");
        Assert.True(r.ExitCode == 0,
            "a rendered view is tracked, not gitignored, or a tracked doc declares itself generated without a row in "
            + $"scripts/rendered_views.py VIEWS:\n{r.Stdout}\n{r.Stderr}");
    }
}
