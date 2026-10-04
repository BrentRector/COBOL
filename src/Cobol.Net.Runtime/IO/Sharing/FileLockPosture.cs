// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE ONE PLACE THE OPERATING ENVIRONMENT'S SHARE MODE IS DECIDED — ISO §9.1.15's <b>file lock</b>, as a
/// derivation from the connector's sharing mode, rather than as a boolean at four stream sites (kb/Work PB740).
/// </summary>
/// <remarks>
/// <para>§9.1.15 names two audiences for one sharing mode, and they need two different mechanisms:</para>
/// <list type="number">
/// <item><description><b>Other file connectors of this run unit.</b> <i>"Multiple paths of access may exist in
/// the same runtime element, contained elements, separate runtime elements within the same run unit, or runtime
/// elements in different run units."</i> — and for them the gate is stated exactly: <i>"Before access to a
/// shared physical file is allowed through an OPEN statement, the sharing mode and the open mode of that OPEN
/// statement shall be allowed by all other file connectors that are currently associated with the physical
/// file, as described in 9.1.13, I-O status; 14.9.27, OPEN statement; and Table 19"</i>. <see cref="Table19"/>
/// IS that gate. Nothing may add a second, stricter one — which is precisely what the operating environment's
/// share mode had become: two clause-less connectors the arbiter PERMITS to share one file (OPEN INPUT, then
/// OPEN EXTEND) were refused by the host and answered '30', a status no row of Table 19 and no item of
/// §9.1.13.9 can produce.</description></item>
/// <item><description><b>Other run units.</b> <i>"The successful opening of a file establishes a file lock for
/// the applicable sharing rules, thereby preventing other run units from opening that file with incompatible
/// sharing rules."</i> — THAT is the OS share mode's job, and the only one it can do, because a host share mode
/// names no requester: it cannot admit this run unit's second connector while refusing a foreign process.
/// </description></item>
/// </list>
/// <para><b>So the posture is the sharing mode's own, and nothing else.</b> A connector's handle carries the file
/// lock its sharing mode establishes (<see cref="OfSharingMode"/>). That is enough for the first audience too,
/// because every pair of connectors <see cref="Table19"/> admits is a pair whose postures already admit each
/// other's access: a READ ONLY connector admits only input-mode siblings and an ALL OTHER one admits every access,
/// and Table 19 pairs a NO OTHER connector with nobody. <c>FileLockPostureDriftTests</c> proves that over the
/// printed table and over every access a handle may take, which is why no handle is ever rebuilt to admit a
/// sibling. (While the implementor default of a connector with no sharing specification was <i>undetermined</i> a
/// widening was owed, because that connector's table row was unknown; kb/Work PB322 determined it and the
/// widening, with the handle rebuild it needed, was deleted.)</para>
/// <para>⛔ THE IMPLEMENTOR DEFAULT IS NOT DECIDED HERE. §9.1.15: <i>"If no specification is made in either
/// location, the implementor defines the sharing mode in which the file is opened"</i>. This compiler's
/// determination is a function of the open mode (<see cref="FileRegistry.ImplementorDefaultSharing"/>, kb/Work
/// PB322, Annex A.1 items 77 and 131), and it always names one of §9.1.15's three modes, so a clause-less
/// connector reaches this class as READ ONLY (INPUT) or NO OTHER (every other mode) and the file lock it
/// establishes against other run units is the one that mode's rule gives — no separate posture for "undetermined"
/// exists.</para>
/// </remarks>
public static class FileLockPosture
{
    /// <summary>The §9.1.15 file lock a sharing mode establishes against OTHER RUN UNITS — the share mode a
    /// connector's own handle carries when it is alone on the physical file.
    /// <para>The mapping is the three rules read literally. 1) <i>"The sharing with no other mode specifies
    /// exclusive access to a physical file"</i> ⇒ <see cref="FileShare.None"/>. 2) <i>"The sharing with read
    /// only mode restricts concurrent access to a physical file through file connectors other than this one, to
    /// input mode"</i> ⇒ <see cref="FileShare.Read"/>. 3) <i>"The sharing with all other mode allows concurrent
    /// access to a physical file through other file connectors specifying input, I-O, or extend mode"</i> ⇒
    /// <see cref="FileShare.ReadWrite"/>.</para>
    /// <para>⚠ Rule 1 is the half this compiler had INVERTED (kb/Work PB740): a connector that wrote
    /// <c>SHARING WITH NO OTHER</c> was a "sharing participant", and participants were given
    /// <see cref="FileShare.ReadWrite"/> — so writing the most restrictive sharing mode the standard has was
    /// what let a foreign process append to the file while the program held it open, measured across processes,
    /// while a connector that wrote no clause at all refused the same write.</para></summary>
    public static FileShare OfSharingMode(FileSharing sharing) => sharing switch
    {
        FileSharing.NoOther => FileShare.None,        // §9.1.15 1) — exclusive access
        FileSharing.ReadOnly => FileShare.Read,       // §9.1.15 2) — others restricted to input mode
        FileSharing.AllOther => FileShare.ReadWrite,  // §9.1.15 3) — others may specify input, I-O or extend
        _ => throw new ArgumentOutOfRangeException(nameof(sharing), $"{sharing} is not an ISO §9.1.15 sharing mode"),
    };

    /// <summary>The access an open mode needs OF THE PHYSICAL FILE (ISO §9.1.4 open modes): INPUT reads, OUTPUT
    /// and EXTEND write, I-O does both (§14.9.35 GR3 — REWRITE replaces the record a READ retrieved, through the
    /// one connector).
    /// <para>⛔ THIS IS THE OPEN MODE'S FLOOR, NOT NECESSARILY A CONNECTOR'S ANSWER, and the distinction is
    /// load-bearing (kb/Work PB771). An organization whose physical format is rewritten WHOLE has to read the
    /// existing records before it can write them back, so its EXTEND handle genuinely needs
    /// <see cref="FileAccess.ReadWrite"/> where this mapping says <see cref="FileAccess.Write"/>.
    /// <c>FileConnector.HostAccess</c> is where a connector states what it actually takes, and this is its
    /// default: a handle opened for the mode-derived guess would refuse the access it really asks for, which is
    /// kb/Work PB713's '30' by another route.</para></summary>
    public static FileAccess AccessOf(FileOpenMode mode) => mode switch
    {
        FileOpenMode.Input => FileAccess.Read,
        FileOpenMode.Output or FileOpenMode.Extend => FileAccess.Write,
        FileOpenMode.IO => FileAccess.ReadWrite,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), $"{mode} is not an ISO §9.1.4 open mode"),
    };

    /// <summary>Would a handle carrying <paramref name="share"/> admit a handle of this run unit — or of any
    /// other — WRITING to the same physical file? The predicate the sequential write path needs: when it is
    /// true the connector's release must land at the physical end as it stands at that moment and be flushed
    /// whole (§14.9.51.4 GR12/GR19, kb/Work PB739), and when it is false the connector holds the only writable
    /// handle there is and keeps the plain, buffered append it always had.
    /// <para>A bit test rather than <c>HasFlag</c>: <see cref="SequentialConnector"/> asks this once per
    /// released record, which is the sequential WRITE path.</para></summary>
    public static bool AdmitsAnotherWriter(FileShare share) => (share & FileShare.Write) != 0;
}
