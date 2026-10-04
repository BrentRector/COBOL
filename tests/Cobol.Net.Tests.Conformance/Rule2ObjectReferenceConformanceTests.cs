// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1497 + PB1112 — ISO §9.3.8.2.3 rule 2 (the object-reference arms a)–d)) over the IMPLEMENTS relation, one
/// row per alternative of the USAGE OBJECT REFERENCE general format (§13.18.60.2). Each rule quotes its clause; every
/// citation was run through <c>cite.py --check 9.3.8.2.3</c> (rule 2 a) "universal object reference", b) "the same
/// interface-name", c) "the same object-class-name, and the presence or absence of the FACTORY and ONLY phrases is the
/// same in both interfaces", d) "ACTIVE-CLASS phrase, and the presence or absence of the FACTORY phrase is the same in
/// both interfaces"). The class implements the interface with the SAME formal description (conforms) or a DIFFERENT one
/// (COBOLNET0841). The ACTIVE-CLASS rows are the ones PB1497 made reachable: an interface method prototype may carry the
/// phrase (§13.18.60.3 SR16; §10.6.1 NOTE), and the containing class is no part of the description.
/// </summary>
public sealed class Rule2ObjectReferenceConformanceTests
{
    private static string Source(string prototypeFormal, string implementationFormal) => $"""
        IDENTIFICATION DIVISION.
        INTERFACE-ID. IJ2.
        PROCEDURE DIVISION.
        END INTERFACE IJ2.

        IDENTIFICATION DIVISION.
        INTERFACE-ID. IK2.
        PROCEDURE DIVISION.
        END INTERFACE IK2.

        IDENTIFICATION DIVISION.
        CLASS-ID. CX2 INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        IDENTIFICATION DIVISION.
        OBJECT.
        PROCEDURE DIVISION.
        END OBJECT.
        END CLASS CX2.

        IDENTIFICATION DIVISION.
        INTERFACE-ID. IP2.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS CX2
            INTERFACE IJ2
            INTERFACE IK2.
        PROCEDURE DIVISION.
        METHOD-ID. M.
        DATA DIVISION.
        LINKAGE SECTION.
        01 A USAGE OBJECT REFERENCE {prototypeFormal}.
        PROCEDURE DIVISION USING A.
        END METHOD M.
        END INTERFACE IP2.

        IDENTIFICATION DIVISION.
        CLASS-ID. CI2 INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE
            CLASS CX2
            INTERFACE IJ2
            INTERFACE IK2
            INTERFACE IP2.
        IDENTIFICATION DIVISION.
        OBJECT. IMPLEMENTS IP2.
        PROCEDURE DIVISION.
        METHOD-ID. M.
        DATA DIVISION.
        LINKAGE SECTION.
        01 A USAGE OBJECT REFERENCE {implementationFormal}.
        PROCEDURE DIVISION USING A.
        END METHOD M.
        END OBJECT.
        END CLASS CI2.
        """;

    [Theory]
    // a) universal ⇔ universal.
    [InlineData("", "", true)]
    [InlineData("", "IJ2", false)]
    [InlineData("", "CX2", false)]
    [InlineData("IJ2", "", false)]
    // b) the same interface-name.
    [InlineData("IJ2", "IJ2", true)]
    [InlineData("IJ2", "IK2", false)]
    // c) the same object-class-name, with the same FACTORY and ONLY presence.
    [InlineData("CX2", "CX2", true)]
    [InlineData("CX2 ONLY", "CX2 ONLY", true)]
    [InlineData("FACTORY OF CX2 ONLY", "FACTORY OF CX2 ONLY", true)]
    [InlineData("CX2 ONLY", "CX2", false)]
    [InlineData("CX2", "CX2 ONLY", false)]
    [InlineData("FACTORY OF CX2", "CX2", false)]
    [InlineData("CX2", "FACTORY OF CX2", false)]
    // d) ACTIVE-CLASS on both sides with the same FACTORY presence — written in an interface prototype (no class) and
    //    in the implementing class (CI2): the containing class is no part of the description.
    [InlineData("ACTIVE-CLASS", "ACTIVE-CLASS", true)]
    [InlineData("FACTORY OF ACTIVE-CLASS", "FACTORY OF ACTIVE-CLASS", true)]
    [InlineData("FACTORY OF ACTIVE-CLASS", "ACTIVE-CLASS", false)]
    [InlineData("ACTIVE-CLASS", "FACTORY OF ACTIVE-CLASS", false)]
    [InlineData("ACTIVE-CLASS", "CX2", false)]
    [InlineData("CX2", "ACTIVE-CLASS", false)]
    [InlineData("ACTIVE-CLASS", "", false)]
    public void ImplementsRule2_ObjectReferenceArms(string prototypeFormal, string implementationFormal, bool conforms)
    {
        var (_, errors, _) = EditionHarness.CompileFull(
            Source(prototypeFormal, implementationFormal).Replace("\r\n", "\n"), 2023);
        if (conforms)
            Assert.Empty(errors);   // not merely "no 0841": the conforming pair compiles clean
        else
            EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0841");
    }
}
