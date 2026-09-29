// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Gate;
using Xunit;

namespace CobolNet.Tests.Characterization;

/// <summary>
/// ⛔ THIS ASSEMBLY RUNS UNDER THE GATE'S LEG FILTER, AND ITS NAMES ARE PLANNABLE (kb/Work PB1719;
/// DESIGN-test-build-ci.md section 3.14.4 arms (4) and (5)): it names GateTestFramework as its xunit framework, and no
/// discovered case carries the repository root. The Unit assembly's GateLegDriftTests holds the other arms.
/// </summary>
public sealed class GateLegDriftTests
{
    [Fact]
    public void Arm4_ThisAssembly_NamesTheGateFramework() =>
        GateLegAudit.AssertNamesTheGateFramework(typeof(GateLegDriftTests).Assembly);

    [Fact]
    public void Arm5_NoDiscoveredCase_CarriesTheRepositoryRoot() =>
        GateLegAudit.AssertNoCaseCarriesTheRoot(typeof(GateLegDriftTests).Assembly);
}
