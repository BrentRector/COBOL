// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Binding;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE BIND-TIME RENDERER NAMES A FIELD AND LETS C# OVERLOAD RESOLUTION SUPPLY THE CONVERSION — SO THE SET OF
/// CARRIERS IT MAY NAME IS THE RUNTIME METHOD'S OVERLOAD SET, AND NOTHING ELSE (kb/Work PB201, PB1117).
/// </summary>
/// <remarks>
/// <para>
/// <c>ReferenceResolver.PositionRead</c> emits <c>CobolTable.Occ(&lt;field&gt;, _P_n)</c> for a NUMERIC subscript,
/// <c>CobolString.RefModPosition(&lt;field&gt;, _P_n)</c> for a NUMERIC reference-modification bound (the item's own
/// profile carries its scale, its sign and the byte form §14.6.13.2 rule 2's check reads — kb/Work PB1117), and
/// the profile-less <c>CobolTable.Occ(&lt;field&gt;)</c> only for a NON-numeric <c>--permissive</c> operand. That is a
/// deliberate design: the post-bind whole-group analysis has not yet chosen the field's storage form when the
/// text is produced, so ONE text has to serve a <c>long</c> field and the <c>string</c> image it may become, and
/// the C# compiler picks the conversion later. The bet is only good for carriers the method DECLARES a parameter
/// for. It did not hold: <c>DataItem.ElementType</c> also produces <c>double</c>/<c>float</c> (a COMP-1/COMP-2
/// leaf), <c>Int128</c>/<c>ulong</c>/<c>UInt128</c> (the &gt;18-digit and unsigned-binary tiers),
/// <c>ManagedPointer</c>, <c>ProgramPointer</c>, an object-reference type and a group's per-program
/// <c>record struct</c> — and every one of those emitted C# that did not compile (CS1503, or CS0103 for a
/// class-tier BASED group whose name is not a field at all).
/// </para>
/// <para>
/// ⚠ This is the drift guard the fix is paired with, not a restatement of it. The fix reads two lists; adding an
/// overload without widening them leaves the fast path routing a carrier it could now render, and widening them
/// without the overload puts the CS1503 back. Both directions fail here. The two-arm assertion
/// (<c>two_arm_dispatch</c>) holds <c>CobolTable.Occ</c> and <c>CobolString.RefModPosition</c> to the SAME
/// numeric carrier set, because <c>PositionRead</c> screens both arms with ONE list.
/// </para>
/// <para>
/// PROVEN TO FAIL, both directions, before being trusted: adding <c>"double"</c> to the numeric list made the
/// first assertion red naming "double" as the extra carrier, and deleting a <c>RefModPosition</c> overload made
/// the two-arm one red.
/// </para>
/// </remarks>
public sealed class PositionCarrierOverloadDriftTests
{
    /// <summary>The <c>DataItem.ElementType</c> spelling of a CLR type — the C# keyword where one exists, the
    /// type name otherwise. Written HERE rather than reused from the compiler so the test is an independent
    /// second opinion about the spelling, not an echo of it.</summary>
    private static string Spelling(Type t) => t == typeof(long) ? "long"
        : t == typeof(ulong) ? "ulong"
        : t == typeof(string) ? "string"
        : t == typeof(double) ? "double"
        : t == typeof(float) ? "float"
        : t == typeof(decimal) ? "decimal"
        : t == typeof(object) ? "object"
        : t.Name;

    /// <summary>The first-parameter spellings of every public static one-argument overload of
    /// <paramref name="name"/> on <paramref name="host"/>.</summary>
    private static SortedSet<string> OneArgumentCarriers(Type host, string name) =>
        new(host.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == name && m.GetParameters().Length == 1)
                .Select(m => Spelling(m.GetParameters()[0].ParameterType)));

    /// <summary>The first-parameter spellings of every public static overload of <paramref name="name"/> whose
    /// SECOND parameter is an <c>in NumProfile</c> — the numeric position read's arity.</summary>
    private static SortedSet<string> ProfileArityCarriers(Type host, string name) =>
        new(host.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == name && m.GetParameters() is { Length: 2 } ps
                            && (ps[1].ParameterType.IsByRef ? ps[1].ParameterType.GetElementType() : ps[1].ParameterType)
                               == typeof(NumProfile))
                .Select(m => Spelling(m.GetParameters()[0].ParameterType)));

    /// <summary>A NUMERIC position operand renders <c>CobolTable.Occ(path, _P_n)</c>, so the numeric carrier list
    /// is exactly the profile-arity overload set.</summary>
    [Fact]
    public void NumericCarriers_AreExactlyTheProfileArityOccOverloads()
    {
        var declared = new SortedSet<string>(ReferenceResolver.NumericPositionCarriers);
        var actual = ProfileArityCarriers(typeof(CobolTable), nameof(CobolTable.Occ));
        Assert.True(declared.SetEquals(actual),
            $"ReferenceResolver.NumericPositionCarriers = [{string.Join(", ", declared)}] but "
            + $"CobolTable.Occ(x, in NumProfile) accepts [{string.Join(", ", actual)}]. A carrier declared here with "
            + "no overload puts the CS1503 back (kb/Work PB201); an overload with no entry here leaves the fast path "
            + "routing work it could now render.");
    }

    /// <summary>A NON-numeric (<c>--permissive</c>) position operand renders <c>CobolTable.Occ(path)</c>, so its
    /// carrier list is exactly the one-argument overload set.</summary>
    [Fact]
    public void NonNumericCarriers_AreExactlyTheOneArgumentOccOverloads()
    {
        var declared = new SortedSet<string>(ReferenceResolver.NonNumericPositionCarriers);
        var actual = OneArgumentCarriers(typeof(CobolTable), nameof(CobolTable.Occ));
        Assert.True(declared.SetEquals(actual),
            $"ReferenceResolver.NonNumericPositionCarriers = [{string.Join(", ", declared)}] but "
            + $"CobolTable.Occ(x) accepts [{string.Join(", ", actual)}].");
    }

    /// <summary>⛔ THE PROFILE-LESS ARITY IS NOT A NUMERIC READ (kb/Work PB1117). A numeric item read without its
    /// profile cannot apply §14.6.13.2 rule 2 to a character image and cannot see its sign or scale, which is
    /// exactly how EC-DATA-INCOMPATIBLE became unreachable on the item-identification lane. So no one-argument
    /// overload may accept a carrier only a NUMERIC item has (the wide and unsigned-binary tiers), and no
    /// profile-less scale arity may come back.</summary>
    [Fact]
    public void NoProfileLessNumericArity()
    {
        var one = OneArgumentCarriers(typeof(CobolTable), nameof(CobolTable.Occ));
        foreach (string numericOnly in new[] { "Int128", "ulong", "UInt128" })
            Assert.DoesNotContain(numericOnly, one);
        foreach (var host in new[] { typeof(CobolTable), typeof(CobolString) })
            Assert.DoesNotContain(host.GetMethods(BindingFlags.Public | BindingFlags.Static), m =>
                m.Name is nameof(CobolTable.Occ) or nameof(CobolString.RefModPosition)
                && m.GetParameters() is { Length: 2 } ps && ps[1].ParameterType == typeof(int));
    }

    /// <summary>⛔ NO FLOAT CARRIER AT EITHER ARITY, AND THIS IS A CONFORMANCE RULE, NOT A CAPABILITY GAP. A
    /// <c>double</c>/<c>float</c> operand can be fractional, and §8.4.2.3.4 GR1b sets EC-BOUND-SUBSCRIPT when the
    /// expression "does not result in an integer" — a test <c>Occ</c> performs over an unscaled/profile pair a
    /// float has no equivalent of. So a float position operand must keep routing to the D18 §15.4 temp, where the
    /// rule is applied exactly once, to the result. An <c>Occ(double)</c> overload added for convenience would
    /// silently truncate <c>E(FUNCTION SQRT(2))</c> to occurrence 1 and raise nothing; this test is what makes that
    /// a deliberate decision to reverse rather than an easy one to slip in.</summary>
    [Fact]
    public void NoFloatCarrier_AtEitherArity()
    {
        foreach (var set in new[] { OneArgumentCarriers(typeof(CobolTable), nameof(CobolTable.Occ)),
                                    ProfileArityCarriers(typeof(CobolTable), nameof(CobolTable.Occ)),
                                    ProfileArityCarriers(typeof(CobolString), nameof(CobolString.RefModPosition)) })
        {
            Assert.DoesNotContain("double", set);
            Assert.DoesNotContain("float", set);
        }
        Assert.DoesNotContain("double", ReferenceResolver.NumericPositionCarriers);
        Assert.DoesNotContain("double", ReferenceResolver.NonNumericPositionCarriers);
    }

    /// <summary>⛔ THE TWO-ARM ASSERTION. <c>PositionRead</c> asks ONE question — "may the fast path name this
    /// numeric carrier" — and then emits <c>CobolTable.Occ</c> for a subscript or <c>CobolString.RefModPosition</c>
    /// for a reference-modification bound. A single admission list is only correct while the two methods admit the
    /// same carriers.</summary>
    [Fact]
    public void SubscriptAndRefModArms_AdmitTheSameNumericCarriers()
    {
        var occ = ProfileArityCarriers(typeof(CobolTable), nameof(CobolTable.Occ));
        var refmod = ProfileArityCarriers(typeof(CobolString), nameof(CobolString.RefModPosition));
        Assert.True(occ.SetEquals(refmod),
            $"CobolTable.Occ(x, in NumProfile) accepts [{string.Join(", ", occ)}] but "
            + $"CobolString.RefModPosition(x, in NumProfile) accepts [{string.Join(", ", refmod)}]. "
            + "ReferenceResolver.PositionRead screens both arms with ONE list, so they must not diverge.");
    }
}
