// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.CodeGen;

/// <summary>⛔ THE ONE TABLE FROM AN I-O STATUS VALUE TO THE CONDITION IT EXPRESSES, as the C# test the emitted
/// branch reads (kb/Work PB810). Every I-O verb's phrase dispatch — AT END, INVALID KEY, the NOT phrases, the
/// END-OF-PAGE success guard, ON EXCEPTION — and both generated after-verb hooks (<c>__IoCheck</c>,
/// <c>__IoCheckEc</c>) ask one of these three questions of a status, and they ask it HERE, so no site can answer
/// it from a paraphrase. <c>IoStatusClassDriftTests</c> forbids a first-character status comparison anywhere else
/// under <c>CodeGen</c>.
/// <para><b>The table is §9.1.13.1's own.</b> <i>"I-O status expresses one of the following conditions upon
/// completion of the input-output operation"</i> and §9.1.13.2 through §9.1.13.11 then give each condition its
/// class of values. Three of those classes drive a phrase:</para>
/// <list type="bullet">
/// <item><b>Successful completion</b> — the <c>'0x'</c> class (§9.1.13.2, and §9.1.13.3's implementor-defined
///   <c>'0x'</c>): the NOT AT END / NOT INVALID KEY / NOT END-OF-PAGE / NOT ON EXCEPTION imperatives.</item>
/// <item><b>At end</b> — <i>"A sequential READ statement was unsuccessfully executed as a result of an at end
///   condition"</i> (§9.1.13.1), whose values are §9.1.13.4's '10' and '14': the AT END imperative
///   (§14.9.30.4 GR24 c)).</item>
/// <item><b>Invalid key</b> — §9.1.13.5's <c>'2x'</c> values: the INVALID KEY imperative (§9.1.14).</item>
/// </list>
/// <para>⛔ <b>EVERY OTHER UNSUCCESSFUL STATUS TAKES NO PHRASE, and that is a rule, not a default.</b> The
/// determination of kb/Work PB810: §14.9.30.4 GR21 sets '46' for a sequential READ whose previous READ or START
/// was unsuccessful and says execution <i>"proceeds as indicated in General rule 24"</i>, but GR24's actions are
/// conditioned on its own first words — <i>"If, during the execution of the READ statement, the at end condition
/// exists"</i> — and '46' is not the at end condition: §9.1.13.7 6) makes it a LOGIC ERROR, which §9.1.13.1
/// defines as <i>"unsuccessfully executed as a result of an improper sequence of input-output operations"</i>.
/// The rule that therefore governs is §14.9.30.4 GR13: <i>"If neither an at end nor an invalid key condition
/// occurs during the execution of a READ statement, the AT END phrase or the INVALID KEY phrase is ignored"</i>,
/// and GR13 b) sends control through §9.1.12's input-output exception processing — a USE declarative, then the
/// end of the READ. The surveyed implementations agree: GnuCOBOL's generated AT END test is the EC-I-O-AT-END
/// exception code, and IBM Enterprise COBOL documents the AT END phrase for the at end condition only.</para></summary>
internal static class IoStatusClass
{
    /// <summary>Successful completion — the <c>'0x'</c> class (§9.1.13.2 / §9.1.13.3).</summary>
    public static string Successful(string status) => $"{status}[0] == '0'";

    /// <summary>Successful completion with a status that is NOT <c>'00'</c> — §9.1.13.1: <i>"The exception-name for
    /// successful completion with an I-O status value that is not '00' is EC-I-O-WARNING"</i>. The one test that
    /// decides whether a successful as-if READ or WRITE of a SORT/MERGE transfer owes its EC hook (kb/Work PB749).</summary>
    public static string Warning(string status) => $"({status}[0] == '0' && {status} != \"00\")";

    /// <summary>Any unsuccessful completion — every class but <c>'0x'</c> (the ON EXCEPTION arm).</summary>
    public static string Unsuccessful(string status) => $"{status}[0] != '0'";

    /// <summary>The at end condition — §9.1.13.4's '10' and '14'.</summary>
    public static string AtEnd(string status) => $"{status}[0] == '1'";

    /// <summary>The invalid key condition — §9.1.13.5's <c>'2x'</c> values.</summary>
    public static string InvalidKey(string status) => $"{status}[0] == '2'";

    /// <summary>An attempt to WRITE outside the externally defined boundaries of the file — the '34' of
    /// §14.9.51.4 GR20 (sequential organization: "the I-O status value of the write file connector is set to '34'") and
    /// the '24' of §14.9.51.4 GR33 b) (relative or indexed organization: "the I-O status associated with the write file
    /// connector is set to '24'"). The one condition SORT GR15 / MERGE GR12's implicit WRITE loop terminates on
    /// (kb/Work PB837). Whole-value, not a class test: '2x' and '3x' each hold values that are not this.</summary>
    public static string WriteBoundary(string status) => $"({status} == \"24\" || {status} == \"34\")";

    /// <summary>A FATAL exception condition — §9.1.13.1: "Certain classes of I-O status values indicate fatal
    /// exception conditions. These are: any that begin with the digit 3, 4, or 7, and any that begin with the digit
    /// 9 that the implementor defines as fatal." The class lives in the RUNTIME (<c>ExceptionCatalog
    /// .IsFatalIoStatus</c>, which the generated <c>__IoCheckEc</c> already asks), so this renders a call to it
    /// rather than a second copy of the digit set. §9.1.13.1 makes fatality a property of the STATUS VALUE, not of
    /// exception checking — the SORT/MERGE implicit-transfer dispositions ask it with checking off (kb/Work PB993).</summary>
    public static string Fatal(string status) => $"CobolNet.Runtime.Exceptions.ExceptionCatalog.IsFatalIoStatus({status})";
}
