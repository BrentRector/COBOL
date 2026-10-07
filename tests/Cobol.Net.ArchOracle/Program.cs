// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// The architecture review's behavior-neutrality oracle (kb/Work PB2116): every case and its compile live in
// CobolNet.Tests.Conformance.ArchOracle; this process only runs it outside xunit. scripts/arch/capture_oracle.py is
// the caller.
return CobolNet.Tests.Conformance.ArchOracle.Run(args, Console.Out, Console.Error);
