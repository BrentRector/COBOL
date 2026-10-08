// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Linq;
using System.Reflection;
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// DESIGN-codegen-backend.md §6 R5 — the backend-neutrality invariant (G4). A <see cref="Place"/> is a STRUCTURAL
/// value: it carries an <see cref="AccessPath"/>, resolved <see cref="DataItem"/>s, and (until PHASE 15's D10)
/// transitional index/offset STRINGS — but NEVER a C# render method. The C# read/write text lives ONLY in
/// <c>CodeGen.PlaceRenderer</c>, so a future CIL backend can consume the same bound tree. This test keeps the
/// invariant enforced even without a live CIL backend: no <see cref="Place"/> subtype, <see cref="AccessPath"/>, or
/// <see cref="AccessSegment"/> may DECLARE a string-returning render method. (String PROPERTIES — the D10-transitional
/// subscript/ref-mod/offset carriers — are permitted; the ban is on render METHODS.)
/// </summary>
public sealed class PlaceNeutralityTests
{
    [Fact]
    public void PlaceHierarchyDeclaresNoStringReturningRenderMethod()
    {
        var offenders = typeof(Place).Assembly.GetTypes()
            .Where(t => typeof(Place).IsAssignableFrom(t)
                || typeof(AccessSegment).IsAssignableFrom(t)
                || t == typeof(AccessPath))
            .SelectMany(t => t
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.ReturnType == typeof(string)
                    && !m.IsSpecialName                  // property getters (get_Start, get_Offset, …) are structural data, allowed
                    && m.Name != nameof(ToString)        // record/object ToString
                    && m.Name != "PrintMembers")         // record-generated
                .Select(m => $"{t.Name}.{m.Name}()"))
            .OrderBy(s => s)
            .ToList();

        Assert.True(offenders.Count == 0,
            "G4 neutrality (R5): the Place hierarchy must expose NO string-returning render method — C# text belongs "
            + "in CodeGen.PlaceRenderer. Offenders: " + string.Join(", ", offenders));
    }
}
