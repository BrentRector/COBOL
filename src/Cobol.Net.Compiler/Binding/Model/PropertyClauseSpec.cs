// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>The PROPERTY clause of one data description entry, as written (ISO §13.18.42.2:
/// <c>PROPERTY [ WITH NO { GET | SET } ] [ IS FINAL ]</c>).</summary>
/// <param name="NoGet">WITH NO GET — §13.18.42.4 GR1: "If the GET phrase is not specified, the PROPERTY clause causes
/// a method to be defined", so NO GET suppresses the GET accessor.</param>
/// <param name="NoSet">WITH NO SET — §13.18.42.4 GR2's counterpart for the SET accessor; §13.18.42.3 SR5 REQUIRES it
/// for an item in or under a CONSTANT RECORD.</param>
/// <param name="IsFinal">IS FINAL — the accessors may not be overridden.</param>
public sealed record PropertyClauseSpec(bool NoGet, bool NoSet, bool IsFinal);
