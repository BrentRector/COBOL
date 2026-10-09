// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Repository;

/// <summary>
/// The assembly-level record every compiled COBOL module carries (docs/rearchitecture/DESIGN-external-repository.md
/// §6.2; kb/Work PB2097): the versions it was compiled against and the registrar that registers its programs into a
/// run unit. The run-time probe reads it to find the module's one registration member,
/// <c>__CobolModule.EnsureRegistered()</c>, without guessing a type name, and refuses a module whose versions differ
/// from the running runtime's (<see cref="RuntimeAbi.Skew"/>).
/// <para>Like every attribute of the repository schema it is <c>sealed</c>, not inherited, takes its identity in one
/// constructor and every other fact as a named property of a primitive or <c>string</c> type, and references no
/// <c>System.Type</c>, so a reader decodes it from metadata without loading the assembly (§6).</para>
/// </summary>
/// <param name="schemaVersion">The repository schema this record follows (§6.6).</param>
/// <param name="runtimeVersion">The WiseOwl COBOL runtime version the module was compiled against
/// (<see cref="RuntimeAbi.Version"/> at its compile).</param>
/// <param name="callAbi">The boundary layout the module was compiled for (<see cref="RuntimeAbi.CallAbi"/> at its
/// compile).</param>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class CobolRepositoryAttribute(int schemaVersion, string runtimeVersion, int callAbi) : Attribute
{
    /// <summary>The schema version the compiler writes today (§6.6). A reader implements exactly one version.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>The repository schema this record follows.</summary>
    public int SchemaVersion { get; } = schemaVersion;

    /// <summary>The runtime version the module was compiled against.</summary>
    public string RuntimeVersion { get; } = runtimeVersion;

    /// <summary>The boundary layout the module was compiled for.</summary>
    public int CallAbi { get; } = callAbi;

    /// <summary>The full CLR name of the module's registrar type (<c>Cobol.&lt;S&gt;.__CobolModule</c>), whose public
    /// static <c>EnsureRegistered()</c> registers the module's programs into the current run unit (§4.5, §11.2).</summary>
    public string? Registrar { get; set; }
}
