// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The sequential file-I/O emitter (P7 Step 9j — a real collaborator over the per-unit
/// <see cref="EmitContext"/>): registration (incl. LINAGE/SHARING), OPEN/CLOSE/UNLOCK/WRITE/READ/REWRITE, and
/// the file-I/O common services every I/O verb shares — the post-status USE-declarative hook, the FILE STATUS
/// store (incl. the §12.4.5.8.4 GR1 NOTE 1 inherited-GLOBAL routing via <see cref="CallUnitState"/>), the
/// record-area image splice, and the RETRY/lock renders KeyedIo consumes.</summary>
internal sealed class SequentialIoEmitter(EmitContext ctx, NumericRenderer num, ReferenceResolver refs,
    DispatchState dispatch, EcState ecState, CallUnitState callState, KeyedIoEmitter keyedIo,
    ArithmeticEmitter arith, EcEmitter ec, MoveEmitter move)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the AT END / AT EOP /
    /// NOT-ON phrase bodies nest arbitrary statement lists, a cyclic edge no ctor order can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>The declarative hook after a verb's FILE STATUS store (GR6 — after the standard status routine,
    /// BEFORE the statement's phrase branches). A statement with ENABLED EC-I-O checking for this file (>>TURN,
    /// ISO §7.3.25) calls the EC-aware <c>__IoCheckEc</c> variant instead — same F1 behavior plus the §9.1.13.1
    /// status→EC raise, F3 selection and fatal default, returning a RESUME transfer pc when a declarative's
    /// RESUME redirected control (§14.9.33). A no-op for a declarative-free, checking-off program.
    /// <para><paramref name="notNormalLabel"/> is the ENCLOSING STATEMENT'S end label, supplied only by the
    /// SORT/MERGE implicit transfers (§14.9.40.4 GR12/GR15, §14.9.24.4 GR7/GR12): there the I-O the declarative
    /// was invoked from is not a statement of the program at all, so §14.9.33.4 GR2 a) 1's "the applicable
    /// statement is the one in which the exception condition was raised" is the SORT/MERGE itself, and a
    /// declarative that ends in RESUME AT NEXT STATEMENT has to leave the whole statement rather than fall into
    /// its next implicit transfer. For SORT §14.9.40.4 GR17 says the same thing outright — "If a USE procedure
    /// invoked while a format 1 SORT statement is active does not complete normally, the SORT statement is
    /// terminated" — and §14.6.13.1.2 #1 is what makes a RESUME a completion that is not normal. Returns whether
    /// that arm was emitted, so the caller emits the label only when something jumps to it.</para>
    /// <para>The hook does NOT tell the selector which verb raised: §14.9.49.4 GR6's tiers are the same for
    /// every I-O statement and at every edition (the determination is on <see cref="UseTierEmitter"/> —
    /// kb/Work PB344), and the only per-statement inputs are the phrases it wrote, which are the flags
    /// below.</para></summary>
    /// <para>⛔ THE RESULT CARRIES THE NOT-PHRASE GATE (kb/Work PB1120). A raised-and-handled nonfatal condition on a
    /// SUCCESSFUL statement (EC-I-O-WARNING on a '0x' status) runs the WHEN / USE and then "the imperative-statement in
    /// [the NOT] phrase is not executed" (§14.6.13.1.4 2)/3); §9.1.14 item 2 gives the NOT INVALID KEY phrase to a
    /// successful completion that nothing interrupted). <see cref="UseHookResult.NotPhraseGate"/> is the C#
    /// condition a statement's NOT phrase must also satisfy — null where no EC hook can interrupt it.
    /// <paramref name="successArm"/> is a call from a statement's SUCCESS branch (a sequential READ, whose failure
    /// branch owns the plain hook): only the EC bridge can raise there, so nothing is emitted without an EC-I-O
    /// mask.</para></summary>
    public UseHookResult EmitUseHook(FileModel file, bool atEndHandled = false, bool invalidKeyHandled = false,
        bool onExceptionHandled = false, string? notNormalLabel = null, bool verbDisposes = false,
        string? useCompletedVar = null, bool successArm = false)
    {
        var w = ctx.Writer;
        if (successArm && ec.IoMaskFor(file) == 0) return default;
        // verbDisposes / useCompletedVar — the SORT/MERGE implicit transfers only (kb/Work PB993). verbDisposes
        // tells __IoCheckEc that the VERB's rules dispose of a fatal status (§14.6.13.1.3 2) precedes 5)/7)), so
        // it must not end the run unit; useCompletedVar, where the verb's rule turns on it, declares a bool that is
        // true when an applicable procedure ran and completed normally (§14.9.24.4 GR7 a), GR12 a)/b)). A
        // not-normal completion never reaches the declaration: the notNormalLabel jump and the RESUME transfer
        // above it have already left.
        void Completed(string expr)
        {
            if (useCompletedVar is not null) w.Line($"bool {useCompletedVar} = {expr};   // an applicable USE procedure ran and completed normally");
        }
        // §14.6.13.1.2 #1: a declarative that executes a RESUME does not complete normally. The ≥ 0 arm above
        // each of these already leaves the statement (a transfer of control), so the arm this adds is the
        // RESUME AT NEXT STATEMENT one, which otherwise falls through into the rest of the enclosing statement.
        void NotNormal(int id)
        {
            if (notNormalLabel is null) return;
            w.Line($"if (DispatchResult.TerminatesSortMerge(__ior{id})) goto {notNormalLabel};"
                + "   // the USE procedure did not complete normally (§14.6.13.1.2 #1) — the statement is terminated (§14.9.40.4 GR17; §14.9.33.4 GR2 a) 1.)");
        }
        if (ec.IoMaskFor(file) is not 0 and var mask)
        {
            int id = ctx.Names.NextEc();
            // The literals travel whenever ANY pair for this file carries WITH LOCATION; __locMask decides
            // PER RAISED NAME inside __IoCheckEc (§15.32.3 r1 per-condition — kb/Work R06).
            int locMask = ec.IoLocMaskFor(file);
            var (stmt, loc) = locMask != 0
                ? (CsLiteral(ecState.Info!.StatementName), CsLiteral(ecState.Info!.Location))
                : ("null", "null");
            w.Line($"int __ior{id} = __IoCheckEc({FileKeyExpr(file)}, {(atEndHandled ? "true" : "false")}, "
                + $"{(invalidKeyHandled ? "true" : "false")}, {(onExceptionHandled ? "true" : "false")}, {mask}, {locMask}, {stmt}, {loc}"
                + $"{(verbDisposes ? ", __verbRule: true" : "")});");
            w.Line(dispatch.ResumeTransfer($"__ior{id}"));
            NotNormal(id);
            Completed($"__ior{id} == DispatchResult.Normal || __ior{id} == DispatchResult.HandledNonfatal");
            return new UseHookResult(notNormalLabel is not null, $"__ior{id} == DispatchResult.Normal");
        }
        if (!dispatch.UseDecls) { Completed("false"); return default; }
        // An ON EXCEPTION phrase is the statement's own handler for EVERY unsuccessful family (§14.9.10.4
        // GR20c) — no declarative runs, and the plain path has no EC to raise, so the hook is a no-op.
        if (onExceptionHandled) { Completed("false"); return default; }
        if (ecState.Active)
        {
            // The EC-model __IoCheck returns the declarative's RESUME action — consumed exactly like the
            // __IoCheckEc arm (the old void call discarded it, swallowing RESUME AT — kb/Work PB141).
            int id = ctx.Names.NextEc();
            w.Line($"int __ior{id} = __IoCheck({FileKeyExpr(file)}, {(atEndHandled ? "true" : "false")}, {(invalidKeyHandled ? "true" : "false")});");
            w.Line(dispatch.ResumeTransfer($"__ior{id}"));
            NotNormal(id);
            Completed($"__ior{id} == DispatchResult.Normal");
            return new UseHookResult(notNormalLabel is not null, null);
        }
        // The non-EC form cannot report a resume action, and it does not need to: RESUME is a §14.9.33 statement
        // of the EC model, so a group without it has no declarative that can end in one. It answers only whether
        // a procedure ran (and, returning, completed normally) — discarded except by the SORT/MERGE transfers.
        string call = $"__IoCheck({FileKeyExpr(file)}, {(atEndHandled ? "true" : "false")}, {(invalidKeyHandled ? "true" : "false")})";
        if (useCompletedVar is not null) Completed(call);
        else w.Line($"{call};");
        return default;
    }

    /// <summary>A NOT phrase's body, behind <see cref="UseHookResult.NotPhraseGate"/> when the statement's hook can
    /// interrupt a successful completion (kb/Work PB1120) — else the bare list, so a build with no enabled EC-I-O
    /// name emits byte-identical text.</summary>
    internal void EmitNotPhrase(string? gate, IReadOnlyList<BoundStatement> body)
    {
        if (gate is null) { Statements.EmitStatementList(body); return; }
        using (ctx.Writer.Block($"if ({gate})")) Statements.EmitStatementList(body);
    }



    /// <summary>What <see cref="EmitUseHook"/> tells its statement: whether the hook can leave the statement by a
    /// transfer the USE-procedure's completion rules turn on (<see cref="Terminable"/>, the SORT/MERGE
    /// implicit transfers), and the C# condition the statement's NOT phrase must also satisfy
    /// (<see cref="NotPhraseGate"/>, kb/Work PB1120 — null when no EC-I-O hook can interrupt a success).</summary>
    internal readonly record struct UseHookResult(bool Terminable, string? NotPhraseGate);

    /// <summary>Emit the file registry init + one <c>Register</c> per SELECTed sequential file (at <c>Main</c> start).
    /// The ASSIGN target becomes a host path at run time (the runtime's <c>ResolveHostPath</c>); the record width is the
    /// FD record-area image width. A non-sequential file is skipped here (its verbs emit loud guards).</summary>
    public void EmitFileRegistration(CodeWriter w)
    {
        foreach (var file in ctx.Data.Files)
        {
            if (file.IsSortMerge) continue;   // an SD is the in-memory sort store (ISO §13.4.6) — never a host file
            // ⛔ THE RECORD-LESS ARM IS DECIDED ONCE, ABOVE THE ORGANIZATION SPLIT (kb/Work PB345). It used to be
            // asked TWICE and answered differently — here it registered only a REPORT file, and
            // KeyedIoEmitter.EmitRegistration returned outright — so a legal record-description-less FD
            // (ISO §13.4.5.3 SR3) produced NO file connector on either arm and its first I-O verb aborted the run
            // unit. Since PB345 every such FD carries §14.9.30.4 GR6's implied record description, synthesized at
            // bind time (DataBinder.MaterializeImpliedRecord), so exactly TWO shapes reach here with no record:
            //   • a REPORT FILE, which legally has none (ISO §9.1.22 / §13.18.46 / §13.4.5.3 SR8) and MUST still
            //     register, or its OPEN falls through to the keyed registries and the report engine's writes go
            //     into a void (the silent-OPEN-no-op hazard, COBOLNET_REPORT_WRITER_DESIGN §7); its record width
            //     is the widest hosted report's line width;
            //   • a SELECT with NO file description entry at all, which §13.4.5.4 GR1 leaves connector-less and
            //     the front end has already diagnosed — there is nothing to register.
            if (file.Records.Count == 0)
            {
                if (file.IsReportFile)
                {
                    int width = Math.Max(1, ctx.Data.Reports
                        .Where(r => ReferenceEquals(r.File, file))
                        .Select(r => r.LineWidth).DefaultIfEmpty(1).Max());
                    // The width is the widest RD's LINE WIDTH, not a record length: the connector is registered as a
                    // file with NO record description, so no rule comparing its bytes with a record length (the
                    // layout notice) applies to it (kb/Work PB677).
                    w.Line($"{RuntimeApi.FileRegisterReport(FileKeyExpr(file), CsLiteral(file.AssignTarget), $"{width}", file.Optional ? "true" : "false", ctx.Data.Edition.DialectLevel, CsLiteral(file.SelectName))};");
                }
                continue;
            }
            if (!file.IsSequential) { keyedIo.EmitRegistration(w, file); EmitSharingRegistration(w, file); continue; }   // relative/indexed connectors
            bool lineSeq = file.Organization == FileOrganization.LineSequential;
            // A variable-length file registers its record-size bounds (ISO §13.18.43 GR9/GR10) — the connector
            // length-frames its records and enforces the GR14 '44' boundary checks.
            // A variable-length record (D-FRA; kb/Work PB981) implies Format 2 (§13.18.43.4 GR5) and reaches past
            // the character area, so the connector learns the record descriptions' own maximum (GR14/GR15's
            // truncation bound) separately from the area width it pads to.
            string vary = file.RecordSizeVaries ? $", {file.VaryMin}, {file.VaryMax}" : "";
            int recordMax = file.RecordMax > file.RecordWidth ? file.RecordMax : 0;
            w.Line($"{RuntimeApi.FileRegister(FileKeyExpr(file), CsLiteral(file.AssignTarget), $"{file.RecordWidth}", lineSeq ? "true" : "false", file.Optional ? "true" : "false", ctx.Data.Edition.DialectLevel, vary, CsLiteral(file.SelectName), recordMax)};");
            EmitAreaRegistrations(w, file);
            EmitSharingRegistration(w, file);
        }
    }

    /// <summary>⛔ THE EXECUTING RUNTIME ELEMENT'S OWN FILE-STATEMENT OPERANDS — the <c>assign, assignDynamic,
    /// page</c> triple every emitted OPEN (and the SORT/MERGE implicit opens) passes, rendered HERE, inside the
    /// element that runs the statement (kb/Work PB673).
    /// <para><b>The rule.</b> ISO §12.4.5.3 GR3 makes the association an act of the STATEMENT — <i>"The
    /// association occurs at the time of execution of an OPEN, SORT, or MERGE statement that referenced
    /// file-name-1"</i> — and both arms name the element that performs it: a) <i>"identified by the specification
    /// of device-name-1 or the value of literal-1 in the source unit that specifies the OPEN, SORT, or MERGE
    /// statement"</i>, b) <i>"identified by the content of the data item referenced by data-name-1 in the runtime
    /// element that executes the OPEN, SORT, or MERGE statement"</i>. §13.18.34 GR6 b) 1 does the same for the
    /// LINAGE operands at the completion of an OPEN OUTPUT.</para>
    /// <para><b>Why it cannot be installed on the connector.</b> A file connector is not per-element and not
    /// per-activation: an EXTERNAL one is a single object per run unit shared by every describing element
    /// (§13.18.22.4 GR4 a, whose file control entries §12.4.5.3 GR1 b requires only to be CONSISTENT — unlike
    /// GR1 i's FILE STATUS, which must name the same external item), and a RECURSIVE unit's internal one is
    /// unit-scoped last-used state across activations (§8.6.4 / §14.6.2.3.3, kb/Work PB168). A connector-held
    /// closure therefore answered with whichever element/activation installed it LAST, which is the executing one
    /// only by accident. Rendering the operands at the statement makes the right answer structural: the emitting
    /// unit IS the executing element, and the emitted expression reads the LIVE activation's storage.</para>
    /// <para>An unresolvable USING operand falls back to the static specification — <c>COBOLNET1810</c>/
    /// <c>COBOLNET1811</c> (§12.4.5.2 SR7) have already rejected the program, so this only keeps emission
    /// total.</para></summary>
    internal string ExecutingElementArgs(FileModel file)
    {
        var (assign, dynamic) = ExecutingAssignment(file);
        return RuntimeApi.ExecutingElementArgs(assign, dynamic, LinageArg(file));
    }

    /// <summary>⛔ THE ONE RENDERING OF THE EXECUTING ELEMENT'S ASSIGN SPECIFICATION (§12.4.5.3 GR3 a)/b)): the
    /// content of data-name-1 when the file control entry writes the USING phrase, else the literal/device value.
    /// Shared by the OPEN entries (<see cref="ExecutingElementArgs"/>) and by the SORT/MERGE statement's association
    /// of its OWN sort-merge file (<see cref="SortEmitter"/>, kb/Work PB1097), so the two cannot render the
    /// operand differently.</summary>
    internal (string Assign, bool Dynamic) ExecutingAssignment(FileModel file)
    {
        if (file.AssignUsingItem is { } item && refs.ResolveItem(item) is { } p)
            // OperandText.FieldImage is THE operand-to-character-image renderer for every shape — an elementary
            // alphanumeric leaf reads its carrier, an alphanumeric group its generated AsImage(). No second arm.
            return (OperandText.FieldImage(p), true);
        return (CsLiteral(file.AssignTarget), false);
    }

    /// <summary>The executing element's LINAGE operand values (ISO §13.18.34 GR6) as a <c>LinagePage</c>, or
    /// <c>null</c> for an FD with no LINAGE clause. ONE rendering for both operand forms — GR6 a) literals fold to
    /// constants, GR6 b) data-names render this element's field reads, evaluated by the runtime at the three GR6 b)
    /// times (OPEN OUTPUT completion, WRITE ADVANCING PAGE, page-overflow WRITE). Passed by every OPEN and every
    /// sequential WRITE entry, which is what makes the OO path (a class's FD) carry LINAGE for free: the statement
    /// supplies it, so there is no per-registration install for a second emit path to forget.</summary>
    internal string LinageArg(FileModel file) =>
        file.Linage is { } lin
            ? RuntimeApi.LinagePageExpr(LinageOpExpr(lin.Body, absent: "0"),
                LinageOpExpr(lin.Footing, absent: "null"),           // GR1 — an ABSENT phrase, not the value 0
                LinageOpExpr(lin.Top, absent: "0"), LinageOpExpr(lin.Bottom, absent: "0"))
            : "null";

    /// <summary>⛔ §14.9.30.4 GR15's RECORD-AREA CATEGORY, told to the connector (kb/Work PB327): "If the
    /// record-area associated with file-name-1 is specified implicitly or explicitly as ALPHANUMERIC, a trailing
    /// space is defined to be the alphanumeric space character. If the record-area associated with file-name-1 is
    /// specified implicitly or explicitly as NATIONAL, a trailing space is defined to be the national space
    /// character." The connector pads a short record to the record width, and it is the only place that knows how
    /// long the physical record was — so it, not the emitter, must know which space to use. The area is national
    /// exactly when its OPERAND category is (an elementary national record, or a GROUP-USAGE NATIONAL group,
    /// §13.18.29.4 GR2b) — THE ONE category reader, never a re-derivation from the leaves. Emitted only for a
    /// national area, so every other program's registration is unchanged byte for byte.
    /// <para>⛔ ANY record description of the FD, not merely the WIDEST one (kb/Work PB329). §13.18.33.4 GR3 —
    /// "Multiple level 1 entries subordinate to a FD or SD entry represent implicit redefinitions of the same
    /// area" (§9.1.2's NOTE says the same) — makes every record description of an FD describe the SAME record
    /// area, so an area one of them declares national IS "specified … as national"; and §14.9.51.4 GR21/GR22 —
    /// the trailing-space rule <c>TrimRecordEnd</c>
    /// implements — key on <i>record-name-1</i>, so a WRITE naming the national record must shed national
    /// spaces whatever the sibling descriptions say. Keying on <see cref="FileModel.AreaRecord"/> alone read the
    /// category off whichever description happened to be widest: <c>01 L-REC PIC N(4). 01 L-BYTES PIC X(8).</c>
    /// (the shape of golden <c>2002/pb327_national_line_sequential_fill</c>) registered NOTHING, so that file's
    /// line-sequential WRITE shed one 0x20 with <c>string.TrimEnd</c> and left a SEVEN-byte line ending in half
    /// a national position — the exact trap PB327's own header describes — and the READ then re-padded it
    /// alphanumerically back to the same eight bytes, so the defect was invisible in the golden's output.
    /// It became visible the moment §14.9.51.4 GR23's character-set test needed the same flag.</para>
    /// <para>⚠ RESIDUAL, and deliberately not modelled here: the flag is per-CONNECTOR while GR21/GR22 are per
    /// record-name-1, so an FD carrying BOTH a national and an alphanumeric record description answers national
    /// for both. Distinguishing them means carrying the category on the statement; no corpus program writes the
    /// alphanumeric sibling of a national area, and inventing a second national axis to say so would be the
    /// two-mechanism anti-pattern.</para></summary>
    internal static void EmitNationalAreaRegistration(CodeWriter w, FileModel file)
    {
        if (file.Records.Any(r => r.OperandPic is { Category: PicCategory.National }))
            w.Line($"{RuntimeApi.FileRegisterNationalArea(FileKeyExpr(file))};");
    }

    /// <summary>⛔ THE CONNECTOR PROPERTIES a file description declares ABOUT ITS RECORD AREA AND ITS MEDIUM,
    /// emitted immediately after the registration for every organization alike — ONE call site per registration
    /// so the next such property is a line HERE and not three edits at the registrations (kb/Work PB793).</summary>
    internal static void EmitAreaRegistrations(CodeWriter w, FileModel file)
    {
        EmitNationalAreaRegistration(w, file);   // §14.9.30.4 GR15 (kb/Work PB327)
        // §14.9.51.4 GR21 / GR22 (kb/Work PB1191): a RECORD clause WITH the DEPENDING phrase keeps a line sequential
        // record at data-name-1's length — the connector's trailing-space strip is off for it.
        if (file.Varying is { DependingName: not null })
            w.Line($"{RuntimeApi.FileRegisterRecordLengthFromDepending(FileKeyExpr(file))};");
        EmitCodeSetRegistration(w, file);        // §13.18.13.4 GR2/GR6 (kb/Work PB793)
    }

    /// <summary>§13.18.13.4 GR2's on-medium coded character set, told to the connector: the GR7 c / GR7 i
    /// correspondence the CODE-SET clause's alphabet defines, as a char[] literal indexed by medium code unit — one
    /// entry per character of the set (<c>CodedCharacterSet.MediumCorrespondence</c>). Emitted ONLY when the set
    /// is not the whole-channel identity (§13.18.13.4 GR7's default and NATIVE / UTF-16 need no conversion at
    /// all), so every other program's registration is unchanged byte for byte.</summary>
    private static void EmitCodeSetRegistration(CodeWriter w, FileModel file)
    {
        if (file.CodeSetCorrespondence is not { } map) return;
        string literal = "new char[] { " + string.Join(", ", map.Select(c => $"(char){(int)c}")) + " }";
        w.Line($"{RuntimeApi.FileRegisterCodeSet(FileKeyExpr(file), literal)};");
    }

    /// <summary>Emit the RECORD-LOCKING registration for a file that declares a SHARING and/or LOCK MODE clause
    /// (Phase 4d M2-FILE-1) — it routes the file's READs through record-lock governance (§9.1.16) and records the
    /// SHARING clause's mode. Files with neither clause emit nothing: they have no LOCK MODE, so §12.4.5.9 GR1
    /// sets no record locks for them. They are NOT outside the sharing arbitration — every OPEN is arbitrated
    /// against ISO §14.9.27.4 Table 19 by <c>FileRegistry.SharedOpenAttempt</c> whether this call is emitted or
    /// not (kb/Work PB321).
    /// <para>⛔ A LOCK-MODE-ONLY FILE HAS NO SHARING MODE OF ITS OWN, so it registers <c>null</c> — the
    /// UNDETERMINED implementor default of §9.1.15 (<i>"If no specification is made in either location, the
    /// implementor defines the sharing mode in which the file is opened"</i>; a LOCK MODE clause is not such a
    /// specification). It used to register ALL OTHER, which answered kb/Work PB322's owner determination here, in
    /// the emitter, for one file shape only — while a clause-less file's default stayed undecided.</para></summary>
    private void EmitSharingRegistration(CodeWriter w, FileModel file)
    {
        if (file.Sharing == SharingMode.None && file.LockMode is null) return;
        string sharing = file.Sharing switch
        {
            SharingMode.NoOther => "FileSharing.NoOther",
            SharingMode.ReadOnly => "FileSharing.ReadOnly",
            SharingMode.AllOther => "FileSharing.AllOther",
            // SharingMode.None (LOCK-MODE-only): the undetermined implementor default — §9.1.15 / kb/Work PB322.
            _ => "(FileSharing?)null",
        };
        string lockMode = (file.LockMode?.Kind ?? LockKind.None) switch
        {
            LockKind.Manual => "FileLockMode.Manual",
            LockKind.Automatic => "FileLockMode.Automatic",
            _ => "FileLockMode.None",
        };
        bool multiple = file.LockMode?.Multiple ?? false;
        w.Line($"{RuntimeApi.FileRegisterSharing(FileKeyExpr(file), $"{sharing}, {lockMode}, {(multiple ? "true" : "false")}")};");
    }

    /// <summary>The C# <c>int</c> expression for one LINAGE clause operand (ISO §13.18.34 GR6): the fixed literal
    /// (GR6 a), or the data item's current value (GR6 b — scale 0 by SR2's elementary-unsigned-integer rule, with
    /// a defensive rescale). A declared data-name that does not resolve to storage fails loud (§1.4).
    /// <para><paramref name="absent"/> is what an OMITTED phrase renders, and it is the caller's decision because
    /// the standard gives the two kinds of omission different meanings (kb/Work PB525): §13.18.34.4 GR1 says the
    /// TOP and BOTTOM values ARE zero when their phrases are absent, so <c>"0"</c> is their value; an absent
    /// FOOTING phrase has no value at all — GR1's <i>"If the FOOTING phrase is not specified, no end-of-page
    /// condition independent of the page overflow condition exists"</i> — so it renders <c>"null"</c> and GR6
    /// b) 2's value rule applies to every footing start that IS specified, zero included.</para></summary>
    private string LinageOpExpr(LinageOperand? op, string absent)
    {
        if (op is null) return absent;
        if (op.Literal is { } lit) return lit.ToString();
        if (op.Item is { } item && refs.ResolveItem(item) is { } p)
            // The integer landing (a P-scaled or unsigned-wide item by VALUE), then the ONE saturating narrowing
            // (kb/Work PB1033) — a bare `(int)` wrapped a 10-digit LINAGE value back into a small page.
            return RuntimeApi.HostInt32(NumericRenderer.Align(num.FieldNum(p), 0));
        return LoudValue("int", $"LINAGE operand '{op.DataName}' is not resolvable to storage (ISO §13.18.34.3 SR2)");
    }

    public void EmitOpen(BoundOpen o)
    {
        var w = ctx.Writer;
        foreach (var (file, mode, sharing, retry, tape, unsupported) in o.Files)
        {
            if (unsupported is { } u) { w.Line(LoudStmt(u)); continue; }
            // ⛔ PER FILE, NOT PER STATEMENT. §14.9.27.4 GR20 makes a multi-group OPEN equal to one separate
            // OPEN statement per file-name, "each hav[ing] the same open mode specification, the
            // sharing-phrase, retry-phrase, and REWIND phrase as specified in the OPEN statement" — i.e. the
            // phrases of ITS OWN group (§14.9.27.2 nests both inside the repeated group). A file whose group
            // carries neither phrase keeps the direct entry points, because the phrase-bearing facade REGISTERS
            // an unregistered connector's record-locking posture (§9.1.15's undetermined implementor default)
            // for the rest of the run, and doing that for a phrase-less file would contradict GR23's "file
            // sharing is completely specified in the file control entry" (kb/Work PB316). Both arms are
            // arbitrated against §14.9.27.4 Table 19 either way — that is not what distinguishes them
            // (kb/Work PB321).
            bool shared = sharing is not null || retry is not null;
            if (shared)
            {
                string modeEnum = RuntimeApi.FileOpenModeExpr(mode);
                var (retryKind, retryAmount) = RenderRetry(retry);
                string shHas = sharing is not null ? "true" : "false";
                string shVal = sharing is { } sm ? RuntimeSharing(sm) : "FileSharing.AllOther";
                // ⛔ THE TAPE PHRASE TRAVELS ON BOTH ARMS. SHARING/RETRY and the per-file-name tape phrase are
                // independent phrases of one general format (§14.9.27.2), so an OPEN may carry both; a phrase
                // honoured on only the plain arm would report '07' for `OPEN INPUT F WITH NO REWIND` and '00'
                // for the same statement once a SHARING phrase was added — the two-arm dispatch this whole
                // item is an instance of (kb/Work PB317), and the same hole swallowed REVERSED (kb/Work PB668).
                w.Line($"{RuntimeApi.FileOpenShared(FileKeyExpr(file), $"{modeEnum}, {shHas}, {shVal}, {retryKind}, {retryAmount}, {RuntimeApi.OpenTapePhraseExpr(tape)}, {ExecutingElementArgs(file)}")};");
            }
            else
            {
                w.Line($"{RuntimeApi.FileOpen(FileKeyExpr(file), mode, tape, ExecutingElementArgs(file))};");
            }
            EmitStoreFileStatus(file);
            EmitUseHook(file);   // a failed OPEN reaches a mode-scoped USE via the being-opened mode (GR6b)
        }
    }

    /// <summary>⛔ THE ONE rendering of a bound SHARING mode as the runtime <c>FileSharing</c> enum member
    /// (Phase 4d). <c>internal</c> rather than private because the SORT/MERGE implicit opens name a sharing mode
    /// too — §14.9.40.4 GR12 a)/GR15 a) and §14.9.24.4 GR7 a)/GR12 a) render "as if an OPEN statement with the
    /// … SHARING WITH READ ONLY / NO OTHER phrase" — and a second copy of this switch is a second place a new
    /// <see cref="SharingMode"/> member could be forgotten (kb/Work PB714).</summary>
    internal static string RuntimeSharing(SharingMode m) => m switch
    {
        SharingMode.NoOther => "FileSharing.NoOther",
        SharingMode.ReadOnly => "FileSharing.ReadOnly",
        _ => "FileSharing.AllOther",
    };

    /// <summary>Render a bound RETRY phrase (ISO §14.7.9) to the runtime <c>(FileRetryKind, int amount)</c> pair.
    /// FOREVER carries no expression. The two that do are governed by DIFFERENT rounding rules and must keep
    /// separate renderings — this is one clause with two arms, not one helper serving both:
    /// <list type="bullet">
    /// <item>TIMES — §14.7.9.3 GR1: "if arithmetic-expression-1 does not evaluate to an integer, the value …
    /// is rounded up to the next whole number", so <c>RETRY 1.5 TIMES</c> is TWO re-attempts. That round-up is
    /// the shared rule <see cref="NumericRenderer.AlignRoundedUp"/> owns (ALLOCATE §14.9.3.4 GR1 is its only
    /// sibling).</item>
    /// <item>SECONDS — §14.7.9.3 GR2 instead stores the timeout period through an implicit COMPUTE WITHOUT the
    /// ROUNDED phrase into a 9(n)V9(m) temporary, i.e. TRUNCATION at the implementor's m. WiseOwl COBOL's
    /// determination is n = 1, m = 0 with a maximum meaningful value of 0 (A.1 item 166, docs/CONFORMANCE.md
    /// §7), so the amount is clamped to a zero-length period at the runtime and is inert — but it is still
    /// PASSED, because it is the GR4a screen input, and it is still rendered by the truncating
    /// <see cref="NumericRenderer.Align"/>, because merging the two arms would re-merge the two rules.</item>
    /// </list></summary>
    public (string Kind, string Amount) RenderRetry(RetrySpec? retry) => retry switch
    {
        null => ("FileRetryKind.None", "0"),
        { Kind: RetryKind.Forever } => ("FileRetryKind.Forever", "0"),
        { Kind: RetryKind.Seconds } => ("FileRetryKind.Seconds", RetrySeconds(retry.Amount)),
        _ => ("FileRetryKind.Times", RetryTimes(retry.Amount)),
    };

    /// <summary>§14.7.9.3 GR1 — the n-TIMES count, ROUNDED UP to the next whole number, as a <c>long</c> through the
    /// saturating <c>RuntimeApi.HostInt64</c> (kb/Work PB1033: an <c>(int)</c> cast made 4294967297 ONE re-attempt, turned
    /// [2^31, 2^32) negative — which GR4 a) then screened as "negative" — and was CS0221 on a literal).</summary>
    private string RetryTimes(BoundExpr? amount) =>
        amount is null ? "0"
        : RuntimeApi.HostInt64(NumericRenderer.AlignRoundedUp(num.Render(amount, ReceiverContext.None)));

    /// <summary>§14.7.9.3 GR2 — the timeout period, truncated to the implementor's m (= 0).</summary>
    private string RetrySeconds(BoundExpr? amount) =>
        amount is null ? "0"
        : RuntimeApi.HostInt64(NumericRenderer.Align(num.Render(amount, ReceiverContext.None), 0));

    /// <summary>Map a bound lock-RETENTION phrase (§14.9.30.2 bracket 2) to the runtime <c>FileRecordLock</c>
    /// enum member (Phase 4d). IGNORING LOCK is NOT here — it is bracket 1 and travels as its own bool argument
    /// (kb/Work PB331), because §5.2.6.1 lets a READ select from both brackets at once.</summary>
    public static string RuntimeRecordLock(BoundRecordLock l) => l switch
    {
        BoundRecordLock.WithLock => "FileRecordLock.WithLock",
        BoundRecordLock.WithNoLock => "FileRecordLock.WithNoLock",
        _ => "FileRecordLock.None",
    };

    // ⛔ THERE IS NO COMPILE-TIME LOCK-GOVERNANCE PREDICATE, AND THERE MUST NEVER BE ONE AGAIN (kb/Work PB683).
    // Whether a connector is open FOR FILE SHARING is a RUN-TIME fact: ISO §9.1.15 — "The SHARING phrase on an
    // OPEN statement overrides the SHARING clause in the file control entry for establishing the sharing mode"
    // — so `OPEN INPUT SHARING WITH READ ONLY F` makes F a sharing participant whatever its SELECT says, and no
    // property of the file control entry or of the READ/WRITE/REWRITE/DELETE statement can see it. The emitter
    // used to guess it from (SHARING clause | LOCK MODE clause | the statement's own lock/RETRY/IGNORING
    // phrases) and routed every unphrased verb on such a connector to the UNGOVERNED entry, which reads a record
    // another connector has locked with '00' where §14.9.30.4 GR9/GR10 b) require the record operation conflict
    // status '51' — and writing `RETRY 0 TIMES`, a behavioural no-op by §14.7.9.3 GR4 a), flipped the answer.
    // Every record verb now renders its GOVERNED runtime entry unconditionally; FileRegistry's
    // `_connectorShares` probe is the ONE place governance is decided, one layer below, where the OPEN's own
    // phrase has already been recorded. The ungoverned entries no longer exist to route to.

    public void EmitClose(BoundClose c)
    {
        var w = ctx.Writer;
        // §14.9.6.4 GR10: a multi-file CLOSE is a series of IMPLICIT CLOSE statements, one per file-name-1, and a
        // RESUME NEXT STATEMENT out of one "resumes at the next implicit CLOSE statement, if any" — so each
        // iteration below is one whole statement, and anything it raises is raised at its END (kb/Work PB893).
        foreach (var (file, kind) in c.Files)
        {
            // §14.9.6.4 GR5: closing a report file while any associated report is ACTIVE (INITIATEd, not
            // TERMINATEd): "the CLOSE statement for that file is completed and the EC-REPORT-NOT-TERMINATED
            // exception condition is set to exist" (nonfatal; kb/Work PB141). COMPLETED FIRST: the active state
            // is read before the close, the close and its GR4 status update run, and only then is the condition
            // raised — so a RESUME AT procedure-name (§14.9.33.4 GR3) leaves the file closed, a declarative sees
            // the updated status, and a RESUME NEXT STATEMENT falls straight to the next implicit CLOSE. Raising
            // it before the close (the pre-PB893 shape) let a RESUME AT transfer out with the file still open.
            // The checking state is the statement's >>TURN (bind-time, BoundClose).
            string? active = null;
            if (c.ReportNotTerminatedCheck
                && ctx.Data.VisibleReports.Where(r => ReferenceEquals(r.File, file)).ToList() is { Count: > 0 } reports)
            {
                active = $"__rptAct{ctx.Names.NextEc()}";
                w.Line($"bool {active} = {string.Join(" || ", reports.Select(r => $"{RuntimeApi.ReportEngine(r.CsIndex, ctx.Data.ReportDepth(r))}.IsActive"))};   // read BEFORE the close (§14.9.6.4 GR5)");
            }
            w.Line($"{RuntimeApi.FileClose(FileKeyExpr(file), kind)};");
            EmitStoreFileStatus(file);
            EmitUseHook(file);
            if (active is not null)
                using (w.Block($"if ({active})"))
                {
                    // Nonfatal (Table 13) — -1/-2/-3 continue to the next implicit CLOSE (GR10).
                    ec.EmitConditionRaise("EC-REPORT-NOT-TERMINATED", "§14.9.6.4 GR5 — after the CLOSE completed");
                }
        }
    }

    /// <summary>UNLOCK file [RECORD[S]] (ISO §14.9.47, COBOL-2002): release the connector's record locks and set
    /// the I-O status (00, or 42 if not open). The two hooks let a USE declarative see the status like any I-O.</summary>
    public void EmitUnlock(BoundUnlock ul)
    {
        var w = ctx.Writer;
        w.Line($"{RuntimeApi.FileUnlock(FileKeyExpr(ul.File), ul.Records ? "true" : "false")};");
        EmitStoreFileStatus(ul.File);
        EmitUseHook(ul.File);
    }

    /// <summary>⛔ THE ONE RENDERER OF ISO §9.1.14's transfer-of-control contract, over a captured status local —
    /// shared by every I-O verb that can carry the phrase, sequential arm and keyed arm alike (this class is the
    /// home of the file-I/O common services KeyedIo consumes; a second copy there was how the sequential WRITE
    /// came to have no branch at all — kb/Work PB691). The INVALID KEY imperative runs on the <c>'2x'</c> family
    /// ONLY (§9.1.13.5 — 3x/4x route to exception processing, never this branch); the NOT INVALID KEY imperative
    /// ONLY on successful completion (<c>'0x'</c>, §9.1.14 final rule item 2). On a sequential-organization file
    /// no '2x' status is reachable at all (§9.1.13.5 items 1–4 all name a relative or indexed file), so the
    /// INVALID arm renders as a branch that provably never fires — dead, never silently rerouted.</summary>
    public void EmitInvalid(string st, KeyedInvalidKey? ik, string? notGate = null)
    {
        if (ik is null) return;
        var w = ctx.Writer;
        string success = notGate is null ? IoStatusClass.Successful(st) : $"{IoStatusClass.Successful(st)} && {notGate}";
        if (ik.Invalid is { } inv)
        {
            using (w.Block($"if ({IoStatusClass.InvalidKey(st)})")) Statements.EmitStatementList(inv);
            if (ik.NotInvalid is { } not)
                using (w.Block($"else if ({success})")) Statements.EmitStatementList(not);
        }
        else if (ik.NotInvalid is { } not)
            using (w.Block($"if ({success})")) Statements.EmitStatementList(not);
    }

    /// <summary>WRITE record [FROM x] [ADVANCING …] (ISO §14.9.51): a FROM operand first MOVEs into the record area,
    /// then the record's character image is written (plain, or with print-control advancing).</summary>
    public void EmitWrite(BoundWrite wr)
    {
        var w = ctx.Writer;
        if (wr.Unsupported is { } u) { w.Line(LoudStmt(u)); return; }
        if (wr.FromMove is { } fromMove) move.Emit(fromMove);   // §14.9.51.4 GR5 a) — the BOUND implicit MOVE
        string name = FileKeyExpr(wr.File);
        string image = OperandText.RecordAreaImage(wr.Record);   // THE ONE record-area channel (kb/Work PB327)
        // ⛔ ONE CALL FOR EVERY WRITE SHAPE (kb/Work PB683). §9.1.16/§14.9.51 GR10-GR11 (P10 Step 8): the governed
        // WRITE — single locking releases the connector's prior lock, WITH LOCK locks the record written — and
        // both are ALL FILES rules, so the ADVANCING phrases are the statement's PRESENTATION shape and travel
        // as an argument, never as a choice of runtime entry. This used to be a three-arm dispatch whose two
        // print-control arms rendered `WriteAdvancing`/`WriteBeforeAndAfter`, which have no lock or RETRY
        // parameter, so `WRITE R AFTER ADVANCING 1 LINE WITH LOCK RETRY 5 TIMES` — one legal statement of
        // §14.9.51.2's Format 1, which prints the ADVANCING phrase, the retry-phrase and the WITH LOCK bracket
        // together — silently dropped both phrases. UNCONDITIONAL: the decision is made where the OPEN's own
        // SHARING phrase is visible (§9.1.15), and the runtime body governs every connector (kb/Work PB669).
        // Status lands on the connector either way.
        var (retryKind, retryAmount) = RenderRetry(wr.Retry);
        string lenArg = VaryingLengthArg(wr.File) ?? "-1";
        // §14.9.51.4 GR4 — the released record is also available as a record of the other files of a SAME RECORD AREA
        // clause (kb/Work PB1195): held in a local so the statement and the store after it see the SAME record.
        string? released = BeginReleasedRecord(wr.File, wr.Record, ref image);
        w.Line($"{RuntimeApi.FileWriteShared(name, image, lenArg, RuntimeRecordLock(wr.Lock), retryKind, retryAmount, LinageArg(wr.File), AdvanceArg(wr), OperandText.RecordAreaExtents(wr.Record))};");
        EndReleasedRecord(wr.File, wr.Record, released, lenArg, RuntimeApi.FileStatus(name));
        // The §9.1.14 status SNAPSHOT for a --permissive INVALID KEY phrase (kb/Work PB691). Taken HERE, before
        // the FILE STATUS store and the USE hook, for the same reason the end-of-page flag is read in the `if`
        // header below: a declarative or a phrase body may operate on this same connector and move its status.
        // It reads the CONNECTOR rather than capturing the entry's return value — that is exactly what §9.1.14
        // means by "the I-O status of the file connector associated with the statement", and it keeps the
        // snapshot independent of which runtime entry the write rendered. Emitted ONLY when the forbidden phrase
        // is present, so the legal Format-1 WRITE renders byte-for-byte as before.
        // ⛔ THE END-OF-PAGE PHRASES NEED THE SAME SNAPSHOT, AND FOR A STRONGER REASON (kb/Work PB526): they are
        // guarded on SUCCESSFUL COMPLETION. §14.9.51.4 GR27 — "When an end-of-page condition occurs, the WRITE
        // statement is successful and then the following actions take place" — and GR28 — "If, during the
        // SUCCESSFUL EXECUTION of a WRITE statement with the NOT END-OF-PAGE phrase, the end-of-page condition
        // does not occur, then ... control is transferred to imperative-statement-2" — so an UNSUCCESSFUL write
        // runs neither phrase. Without the guard the NOT arm runs on every unsuccessful write of a LINAGE file:
        // a '48' write to a closed file, a '71' bad line-sequential character, and — the case that made it
        // observable — the '90' LINAGE value-rule violation, on which the connector's per-write end-of-page flag
        // is not even the failed write's own.
        string? wst = null;
        if (wr.InvalidKey is not null || wr.AtEop is not null || wr.NotAtEop is not null)
        {
            wst = $"__wst{ctx.Names.NextKeyedSeq()}";
            w.Line($"var {wst} = {RuntimeApi.FileStatus(name)};");
        }
        EmitStoreFileStatus(wr.File);
        // invalidKeyHandled stays FALSE even with the phrase present: §9.1.14 item 2 suppresses exception
        // processing only "if the invalid key condition exists", and on a sequential organization it never can
        // (§9.1.13.5 items 1–4 all name a relative or indexed file), so every unsuccessful status here is a
        // §9.1.14 final-rule item 1 completion that the declarative must still see.
        // The END-OF-PAGE phrase travels as the hook's condition-phrase flag: §14.9.51.4 GR27 c)/d) run the
        // exception-checking PERFORM's WHEN or the USE declarative for GR27 a)'s EC-I-O-EOP / -OVERFLOW only
        // "If the END-OF-PAGE phrase is not specified" (kb/Work PB854). NOT END-OF-PAGE alone does not count —
        // it is GR28's arm for the ABSENCE of the condition.
        var hook = EmitUseHook(wr.File, atEndHandled: wr.AtEop is not null);
        // END-OF-PAGE branches (ISO §14.9.51 GR27b/GR28): an end-of-page WRITE is SUCCESSFUL — the branch runs
        // after the status store and the hook (which sets GR27 a)'s condition and, with the phrase present,
        // dispatches nothing). The flag is read in the
        // `if` HEADER before either body runs: a branch body may WRITE the same file again (SQ208M's footing loop
        // inside the AT phrase), which clobbers the connector's per-write flag. The `__wst[0] == '0'` guard is
        // GR27/GR28's "successful" — see the snapshot above.
        if (wr.AtEop is not null || wr.NotAtEop is not null)
        {
            using (w.Block($"if ({IoStatusClass.Successful(wst!)} && {RuntimeApi.FileEndOfPage(name)})"))
            {
                if (wr.AtEop is { } at) Statements.EmitStatementList(at);
            }
            if (wr.NotAtEop is { } not)
                using (w.Block($"else if ({IoStatusClass.Successful(wst!)})"))
                    EmitNotPhrase(hook.NotPhraseGate, not);
        }
        // The forbidden-but-tolerated INVALID KEY pair, through THE ONE §9.1.14 renderer the keyed arm uses.
        // Last, after the END-OF-PAGE branches: both are end-of-statement phrase transfers, and GR27b's EOP
        // imperative belongs to the WRITE's own general rules while §9.1.14 is the outer transfer contract.
        if (wst is not null) EmitInvalid(wst, wr.InvalidKey, hook.NotPhraseGate);
    }

    /// <summary>The ADVANCING phrase of ONE WRITE statement as the runtime's <c>WriteAdvance</c> descriptor
    /// (ISO §14.9.51.2 Format 1 — the print-control bracket). TWO shapes, one argument: no phrase; or the one
    /// phrase, whose <c>-1</c> line count is ADVANCING PAGE and whose kind is the §14.9.51.4 GR25 e)/f)
    /// PLACEMENT the binder already decided. ⛔ There was a third arm for COBOL-2023's combined form carrying a
    /// SECOND amount; §14.9.51.2 prints one operand and GR25 a) defines one advance, so the combination is a
    /// placement (present, then advance) and renders through the Before arm (kb/Work PB712).</summary>
    private string AdvanceArg(BoundWrite wr)
    {
        if (wr.Advancing is { } adv)
            return $"new WriteAdvance(WriteAdvanceKind.{(adv.Before ? "Before" : "After")}, "
                + $"{(adv.Page ? "-1" : LinesExpr(adv.Lines!))})";
        return "WriteAdvance.None";
    }

    /// <summary>Render the governed sequential-organization READ call (§14.9.30.4 GR9–GR12 over the ordinal lock
    /// identity, plus the GR22 skip-scan) as the plain read's BOOL. The runtime entry is the ONE governed
    /// Format-1 read shared with the keyed emitter, so <c>previous</c> is passed explicitly — taken from the
    /// <see cref="BoundRead.Kind"/> §14.9.30.4 GR19 carries (kb/Work PB334).</summary>
    private string EmitReadSharedCall(BoundRead rd, string name, string tmp)
    {
        var (retryKind, retryAmount) = RenderRetry(rd.Retry);
        return RuntimeApi.FileReadSharedOk(name, rd.Kind == ReadKind.Previous ? "true" : "false", RuntimeRecordLock(rd.Lock),
            rd.AdvancingOnLock ? "true" : "false", rd.IgnoringLock ? "true" : "false",
            retryKind, retryAmount, tmp);
    }

    /// <summary>The resolved <c>RECORD VARYING … DEPENDING ON</c> item of <paramref name="file"/> (ISO §13.18.43
    /// data-name-1), or null when the phrase is absent or the name did not resolve. ⛔ THE ONE resolution: GR13 a)
    /// (the length WRITTEN), GR15 (the length READ BACK) and GR16 a) (the INTO sending size) are three rules over
    /// the SAME data item, and they were re-spelling the same four-part pattern independently.</summary>
    public Place? VaryingDepending(FileModel file) =>
        file is { Varying.DependingName: not null, VaryingDependingItem: { } d } ? refs.ResolveItem(d) : null;

    /// <summary>The record-length argument for a WRITE/REWRITE on a RECORD VARYING … DEPENDING file (ISO
    /// §13.18.43 GR13a — the DEPENDING item's content names the record length), or null when the statement
    /// writes the record's own size (GR13b/c — on a varying file the runtime takes the image's length; on a
    /// fixed file it pads to the record width).</summary>
    public string? VaryingLengthArg(FileModel file) =>
        VaryingDepending(file) is { } dep ? RuntimeApi.HostInt32(PlaceRenderer.CountRead(dep)) : null;

    /// <summary>The record-length argument of a sequential-organization REWRITE — §13.18.43.4 GR13's number of
    /// bytes in the record, which §14.9.35.4 GR16 (record sequential: '44' unless it EQUALS the replaced record's)
    /// and GR17 b)/c) (line sequential: '44' when greater, space-filled when less) compare. The DEPENDING item's
    /// content when the file has one (GR13 a)); on a FIXED-length file the byte size of record-name-1 itself
    /// (GR13 b)), a compile-time constant in the same byte basis that sizes the file's records
    /// (<see cref="FileModel.RecordWidth"/> is the widest of these) — the runtime's −1 on a fixed file means the
    /// WRITE's whole-record-area rule, which is exactly what a shorter record-name-1 must NOT get here (kb/Work
    /// PB1168), and an image's character count is not a byte count for every record shape (a pointer-class
    /// record sends no character image); and −1 on a variable-length file without DEPENDING, whose image is the
    /// record at its current length (GR13 b)/c)).
    /// <para>⛔ A VARIABLE-LENGTH record-name-1 in a FIXED-length file (D-FRA (vi), <see cref="FileModel.FixedFormRecords"/>)
    /// is the size it occupies in the fixed form — every member at its maximum, <see cref="FileModel.MaxRecordSize"/>
    /// (the sum §13.18.43.4 GR8 b) states) — never <see cref="RecordLayout.PhysicalWidth"/>, which is only the record's FIXED run:
    /// a REWRITE of a record that exactly fills the file's integer-1 bytes was refused '44' as "3 bytes".</para></summary>
    private static string RewriteLengthArg(BoundRewrite rw, string? dependingArg) =>
        dependingArg ?? (rw.File.RecordSizeVaries ? "-1"
            : (FileModel.IsVariableLengthRecord(rw.Record.Item) ? FileModel.MaxRecordSize(rw.Record.Item)
                : RecordLayout.PhysicalWidth(rw.Record.Item)).ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>After a SUCCESSFUL read of a RECORD VARYING … DEPENDING file, store the just-read record's length
    /// into the DEPENDING item (ISO §13.18.43 GR15; GR12 — an unsuccessful READ leaves it unchanged, so the call
    /// site sits inside the success branch).</summary>
    public void EmitReadLengthStore(FileModel file)
    {
        if (VaryingDepending(file) is not { } dep) return;
        arith.StoreArith(dep, new NumX(RuntimeApi.FileLastReadLength(FileKeyExpr(file)), 0), CobolRounding.Truncation);
    }

    /// <summary>READ file [INTO x] [AT END …][NOT AT END …] (ISO §14.9.30): on success the record image is
    /// distributed into the FD record area (and, with INTO, MOVEd to the target); the AT END / NOT AT END imperative
    /// branches on the at-end condition. After an UNSUCCESSFUL read the record area's content is spec-UNDEFINED
    /// (§14.9.30 GR18 "unless otherwise specified…"); WiseOwl COBOL's documented refinement is that the area is
    /// UNCHANGED — the store sits in the success branch only — extending the spec's own rule for every other
    /// unsuccessful I-O verb (REWRITE GR14 / WRITE GR15 / DELETE GR8 / START GR2 all say "unaffected"). The
    /// legacy's LOW-VALUE fill there was a byte-engine artifact (ST146A's golden is re-baselined over it,
    /// DEVLOG 570).
    /// <para>⚠ THE ONE EXCEPTION is §9.1.13.6 item 4 b)'s '34' (kb/Work PB1513): the READ is unsuccessful BECAUSE of what
    /// the landed record's OCCURS DEPENDING ON item says, so the verdict is known only after the record has landed in
    /// the area (<see cref="OdoRecordExceedsMaximum"/>) and the area then HOLDS that record. Its content after an
    /// unsuccessful READ is undefined anyway (§14.9.30.4 GR18), so this is within the standard, not a divergence; the
    /// success-only steps — the DEPENDING length store, INTO, NOT AT END — still follow the verdict. The keyed READ
    /// (<c>KeyedIoEmitter.EmitRead</c>) and the SORT/MERGE USING transfer have the same shape.</para></summary>
    public void EmitRead(BoundRead rd)
    {
        var w = ctx.Writer;
        if (rd.Unsupported is { } u) { w.Line(LoudStmt(u)); return; }
        string name = FileKeyExpr(rd.File);
        string tmp = $"__rd{ctx.Names.NextRead()}";
        // The read record is made available in the WHOLE record area — store through the LARGEST record's view
        // (ReferenceResolver.RecordArea, ISO §13.18.33.4 GR3); a shorter Records[0] window would truncate the
        // splice (ST111A).
        Place? area = refs.RecordArea(rd.File);
        // §9.1.16 record locking on the sequential organization (P10 Step 8): EVERY READ routes through the
        // governed runtime entry — the next ordinal's pre-read conflict check (§14.9.30 GR9, FPI unchanged on a
        // 51 per GR10a), the GR11 lock discipline, and the GR22 ADVANCING ON LOCK skip-scan. Unconditional
        // (kb/Work PB683): only the runtime can see an OPEN statement's own SHARING phrase (§9.1.15), and it
        // governs the retrieval for EVERY connector — §9.1.16 makes a locked record inaccessible to another file
        // connector whatever that connector's own LOCK MODE clause says (kb/Work PB669).
        // §14.9.30.4 GR19's read kind rides INSIDE that one call as the direction of the retrieval; GR21's
        // sequential-file rules b)/c) then select the record NUMBER from it (kb/Work PB334). With one call shape
        // there is no longer a second place to drop it — which is how `READ … PREVIOUS` became a forward read.
        string readCall = EmitReadSharedCall(rd, name, tmp);
        // §9.1.13.6 item 4 b) (kb/Work PB1513): a record that LANDS with an OCCURS DEPENDING ON item making it too long
        // makes the READ unsuccessful — '34' — so the verdict on the READ is known only after the area store, and
        // every success-only step (length store, INTO, NOT AT END) follows the verdict, not the physical retrieval.
        string cond = readCall;
        bool stored = false;
        if (OdoRecordExceedsMaximum(rd.File) is { } tooLong)
        {
            cond = $"__rok{ctx.Names.NextRead()}";
            w.Line($"bool {cond} = {readCall};");
            using (w.Block($"if ({cond})"))
            {
                EmitRecordAreaStore(rd.File, area, tmp, RuntimeApi.FileCurrentRecord(name), RuntimeApi.FileCurrentRecordExtents(name));
                using (w.Block($"if ({tooLong})"))
                {
                    w.Line($"{RuntimeApi.FileReadExceedsRecordMaximum(name)};   // §9.1.13.6 item 4 b) — the READ is unsuccessful");
                    w.Line($"{cond} = false;");
                }
            }
            stored = true;
        }
        using (w.Block($"if ({cond})"))
        {
            if (!stored) EmitRecordAreaStore(rd.File, area, tmp, RuntimeApi.FileCurrentRecord(name), RuntimeApi.FileCurrentRecordExtents(name));
            EmitReadLengthStore(rd.File);   // §13.18.43 GR15 — the just-read length into DEPENDING
            EmitStoreFileStatus(rd.File);
            // ⛔ A SUCCESSFUL READ RUNS THE EC HOOK TOO (kb/Work PB1382): a '0x' status (04, 06, 09 …) is
            // EC-I-O-WARNING (§9.1.13.1) and, with it enabled (§7.3.25.4 GR4), a WHEN / USE handles it and the NOT
            // phrase below is not executed (§14.6.13.1.4 2)/3, kb/Work PB1120). OPEN and the keyed verbs already did.
            var hook = EmitUseHook(rd.File, atEndHandled: rd.AtEnd is not null, successArm: true);
            // READ … INTO is READ then MOVE THE CURRENT RECORD to the target (ISO §14.9.30.4 GR4 b)) — the
            // move is BOUND (kb/Work PB348), its sender the record sliced to the §13.18.43.4 GR16 byte count
            // through the ONE builder the keyed READ and the sort RETURN also use (kb/Work PB339).
            if (rd.IntoMove is { } intoMove) move.Emit(intoMove);
            if (rd.NotAtEnd is { } not) EmitNotPhrase(hook.NotPhraseGate, not);
            // §14.9.30.4 GR13c — on a successful READ "control is transferred to the end of the READ statement,
            // or, if the NOT AT END phrase or NOT INVALID KEY phrase is specified, to imperative-statement-2".
            // The phrase is never conforming source on this arm (Format 1 has no INVALID KEY bracket — the
            // binder reports COBOLNET1720), so this renders only under --permissive, where the bind stands and
            // the block has to MEAN something rather than vanish. There is deliberately NO invalid-key arm: a
            // sequential-organization READ raises no '2x' status (§9.1.13.5), so the condition cannot exist.
            if (rd.InvalidKey?.NotInvalid is { } notInvalid) EmitNotPhrase(hook.NotPhraseGate, notInvalid);
        }
        using (w.Block("else"))
        {
            EmitStoreFileStatus(rd.File);
            EmitUseHook(rd.File, atEndHandled: rd.AtEnd is not null);
            // The AT END imperative runs ONLY for the at end status class (§14.9.30.4 GR24 c) — "If, during the
            // execution of the READ statement, the at end condition exists"); every other unsuccessful status,
            // GR21's '46' included (a LOGIC ERROR, §9.1.13.7 6)), is §14.9.30.4 GR13's "neither an at end nor an invalid
            // key condition" and reaches a USE declarative through §9.1.12 instead. The determination is on
            // IoStatusClass (kb/Work PB810).
            if (rd.AtEnd is { } at)
                using (w.Block($"if ({IoStatusClass.AtEnd(RuntimeApi.FileStatus(name))})"))
                    Statements.EmitStatementList(at);
        }
    }

    public void EmitRewrite(BoundRewrite rw)
    {
        var w = ctx.Writer;
        if (rw.Unsupported is { } u) { w.Line(LoudStmt(u)); return; }
        if (rw.FromMove is { } fromMove) move.Emit(fromMove);   // §14.9.35.4 GR7 a) — the BOUND implicit MOVE
        string image = OperandText.RecordAreaImage(rw.Record);   // THE ONE record-area channel (kb/Work PB327)
        // §9.1.16/§14.9.35 GR11-GR12 (P10 Step 8): EVERY sequential REWRITE routes through the governed runtime
        // entry — the pre-operation conflict check on the last-read record (51 leaves the record unrewritten)
        // and the GR12 lock discipline. Unconditional (kb/Work PB683), and the runtime body governs every
        // connector, opted in or not (kb/Work PB669). The status lands on the connector either way.
        var (retryKind, retryAmount) = RenderRetry(rw.Retry);
        string rwLenArg = RewriteLengthArg(rw, VaryingLengthArg(rw.File));
        // §14.9.35.4 GR6 — the released record is also available as a record of the other files of a SAME RECORD AREA
        // clause: held in a local so the statement and the store after it see the SAME record (kb/Work PB1195).
        string? released = BeginReleasedRecord(rw.File, rw.Record, ref image);
        w.Line($"{RuntimeApi.FileRewriteShared(FileKeyExpr(rw.File), image, rwLenArg, RuntimeRecordLock(rw.Lock), retryKind, retryAmount, OperandText.RecordAreaExtents(rw.Record))};");
        EndReleasedRecord(rw.File, rw.Record, released, rwLenArg, RuntimeApi.FileStatus(FileKeyExpr(rw.File)));
        // The §9.1.14 status snapshot for a --permissive INVALID KEY phrase, taken before the status store and
        // the USE hook — the WRITE arm above carries the full reasoning (kb/Work PB691).
        string? rst = null;
        if (rw.InvalidKey is not null)
        {
            rst = $"__rwst{ctx.Names.NextKeyedSeq()}";
            w.Line($"var {rst} = {RuntimeApi.FileStatus(FileKeyExpr(rw.File))};");
        }
        EmitStoreFileStatus(rw.File);
        var hook = EmitUseHook(rw.File);   // invalidKeyHandled stays false: no '2x' status is reachable here (§9.1.13.5)
        if (rst is not null) EmitInvalid(rst, rw.InvalidKey, hook.NotPhraseGate);
    }

    /// <summary>⛔ THE ONE RECORD-AREA STORE of a READ / RETURN — "the record is made available in the record
    /// area" (ISO §14.9.30.4 / §14.9.34.4 GR3), and the area is EVERY record of the file (§13.18.33.4 GR3) and of
    /// every file sharing it (§12.4.6.4.4 GR2). Its two halves are determination D-FRA (docs/CONFORMANCE.md §3;
    /// kb/Work PB981): the CHARACTER half — <paramref name="area"/>, the largest character-window record, whose
    /// view spans the shared backing — takes the area image <paramref name="areaImage"/>; each OUT-OF-LINE
    /// record (<see cref="FileModel.OutOfLineRecords"/>, or the area itself when every record is out of line)
    /// takes the CURRENT RECORD <paramref name="currentRecord"/> at its own length, with the extent table it was
    /// sent with (<paramref name="currentExtents"/> — D-FRA (v), kb/Work PB1053), through
    /// <see cref="EmitOutOfLineInto"/>. Every READ organization and the sort RETURN route here, so no transfer
    /// can reach one half of the area and not the other.</summary>
    public void EmitRecordAreaStore(FileModel file, Place? area, string areaImage, string currentRecord,
        string currentExtents)
    {
        // D-FRA (vi): a file of FIXED-length records holds a variable-length record in its fixed form, so the record
        // this READ just retrieved is decomposed at the members' maximum widths and each drops its space padding.
        bool fixedForm = file.FixedFormRecords;
        if (area is not null)
        {
            if (FileModel.IsOutOfLineRecord(area.Item))
            {
                EmitOutOfLineInto(area, currentRecord, currentExtents, fixedForm);
                // A file whose every record is out of line has no character window of its own, but a file of its
                // SAME RECORD AREA clause may: the shared character area holds the record too (§12.4.6.4.4 GR2;
                // the READ twin of the released-record rule, kb/Work PB1195). It takes the CURRENT RECORD, not the
                // area image: an out-of-line file's area image is cut to its own character width, which is not the record.
                if (AreaCharacterViewFor(file, area.Item) is { } peerChars) EmitImageInto(peerChars, currentRecord);
            }
            else EmitImageInto(area, areaImage);
        }
        foreach (var record in file.OutOfLineRecords)
            if (refs.ResolveItem(record) is { } place)
                EmitOutOfLineInto(place, currentRecord, currentExtents, fixedForm);
    }

    /// <summary>Make the current record available in ONE out-of-line record (D-FRA; kb/Work PB981) — the
    /// receiving inverse of <see cref="OperandText.RecordAreaImage"/>'s out-of-line arms, arm for arm: a
    /// variable-length group decomposes the contiguous record (§8.5.1.11.2) through its generated
    /// <c>FromContiguousImage</c>; a dynamic-length record takes the record as its new content (§8.5.1.10.4 —
    /// "the new value becomes the content of the item", truncated on the right at its maximum; a NATIONAL one
    /// decodes the record's byte pairs first, the inverse of its NatBytes image); a pointer-class record has no
    /// character image, so the record does not reach it and its value is unchanged.</summary>
    private void EmitOutOfLineInto(Place record, string currentRecord, string currentExtents, bool fixedForm = false)
    {
        var w = ctx.Writer;
        var item = record.Item;
        if (item.IsGroup)
        {
            w.Line(PlaceRenderer.WriteVarGroupContiguous(record, currentRecord, currentExtents,
                $"record area '{item.CobolName}' read", fixedForm));
            return;
        }
        if (item.IsDynamicLength)
        {
            string content = item.Pic?.Category is PicCategory.National
                ? RuntimeApi.NatReadWindow(currentRecord, "0", $"{currentRecord}.Length / 2")
                : currentRecord;
            // D-FRA (vi): the fixed form of a dynamic-length record fills its whole field with the content and spaces;
            // the spaces are padding, never data (the group arm drops them member by member, in Decompose).
            if (fixedForm) content = $"({content}).TrimEnd(' ')";
            w.Line(PlaceRenderer.Write(record, ReceivingStore.Characters(item, content, "0")));   // the ONE elementary character store (PB871)
            return;
        }
        // A pointer-class record: no character image (A.1 items 210/216) — nothing of the record lands in it.
        w.Line($"// '{item.CobolName}' — pointer-class record: no character image, the READ leaves its value unchanged (D-FRA)");
    }

    /// <summary>⛔ THE C# CONDITION "the record the READ just landed is made too long by its OCCURS DEPENDING ON item",
    /// or null when the rule has nothing to ask (kb/Work PB1513). §9.1.13.6 item 4 b): <i>"A READ statement is
    /// unsuccessfully executed because the records are variable in length because of an OCCURS DEPENDING ON clause
    /// in the record in the associated record description entry and the associated DEPENDING ON item contains a
    /// value that makes the number of bytes in the record exceed the value specified or implied by the file
    /// description entry"</i> — '34'. The record's byte size at a count <c>n</c> is §13.18.43.4 GR13 c)'s: the fixed
    /// portion plus the table at <c>n</c> occurrences (the very arithmetic <see cref="OdoModel.WrapGroup"/> sizes a
    /// group operand by, bit-granular when the subtree holds a USAGE BIT leaf), and "the value specified or
    /// implied by the file description entry" is the file's maximum record size (<see cref="FileModel.VaryMax"/>
    /// for a variable-length file, else <see cref="FileModel.RecordWidth"/> — which an explicit
    /// <c>RECORD CONTAINS</c> raises above the largest description). The test is therefore a compile-time constant
    /// LIMIT on the count, the largest <c>n</c> whose size still fits, so the emitted code is one comparison of the
    /// DEPENDING ON item against a literal.
    /// <para>⚠ DETERMINATION (docs/CONFORMANCE.md D-ODO2): the rule speaks of "THE record in the associated record
    /// description entry" — one description. A file description with several record descriptions (implicit
    /// redefinitions of one area, §13.18.33.4 GR3) has no one description the READ is associated with, and
    /// evaluating a description's count over bytes that belong to another record type would reject every legal
    /// record of the other types, so the rule is asked of a file with exactly ONE record description. The
    /// DEPENDING ON item may lie in the record or outside it; it is read AFTER the record has landed, which is the
    /// only moment a count carried in the record has a value.</para></summary>
    internal string? OdoRecordExceedsMaximum(FileModel file)
    {
        if (file.Records.Count != 1) return null;
        var record = file.Records[0];
        if (OdoModel.TableUnder(record) is not { OccursSpec: { } spec } table || spec.Depending is not { } depItem
            || refs.ResolveItem(depItem) is not { } dep)
            return null;
        int max = table.Occurs ?? spec.Max;
        bool bits = record.HasBitDescendant && BitLayout.StartBitOf(record, table) >= 0;
        int elem = bits ? BitLayout.StrideBits(table) : RecordLayout.PhysicalOccurrenceWidth(table);
        long fixedUnits = bits ? BitLayout.StartBitOf(record, table) : RecordLayout.PhysicalWidth(record) - (long)elem * max;
        // The record's size in BYTES at n occurrences (GR13 c) — bits round up to the byte the last one ends in (GR4).
        long BytesAt(long n) => bits
            ? BitLayout.Characters((int)(fixedUnits + (n == 0 ? 0 : (long)elem * (n - 1) + BitLayout.WidthBits(table))))
            : fixedUnits + n * elem;
        long fdMax = file.RecordSizeVaries ? file.VaryMax : file.RecordWidth;
        long limit = max;
        for (int guard = 0; BytesAt(limit + 1) <= fdMax && guard < (1 << 22); guard++) limit++;
        while (limit >= 0 && BytesAt(limit) > fdMax) limit--;
        return $"{PlaceRenderer.CountRead(dep)} > {limit}L";
    }

    /// <summary>Land the record an IMPLICIT READ (the SORT/MERGE USING transfer) just retrieved into the file's record
    /// area, through the same store a READ statement uses — what a file whose record description is judged by its own
    /// content (<see cref="OdoRecordExceedsMaximum"/>) needs before the judgment can be made.</summary>
    internal void EmitImplicitReadLanding(FileModel file, string areaImage)
    {
        string name = FileKeyExpr(file);
        EmitRecordAreaStore(file, refs.RecordArea(file), areaImage, RuntimeApi.FileCurrentRecord(name),
            RuntimeApi.FileCurrentRecordExtents(name));
    }

    /// <summary>⛔ THE CHARACTER-WINDOW RECORD a released OUT-OF-LINE record must reach for the shared character area to
    /// hold it, or null when none is needed: the released record is itself a character-window record (it IS the
    /// shared backing — the other character-window records of the area see it for free), or no file of the area
    /// has a character-window record. Among the area files' views it is the WIDEST, so the store spans the whole
    /// shared backing (§13.18.33.4 GR3: every record of the area redefines the same storage).</summary>
    private Place? AreaCharacterViewFor(FileModel file, DataItem released)
    {
        if (!FileModel.IsOutOfLineRecord(released)) return null;
        Place? widest = null;
        int width = -1;
        foreach (var f in file.AreaFiles)
            if (refs.RecordArea(f) is { } view && !FileModel.IsOutOfLineRecord(view.Item)
                && RecordLayout.PhysicalWidth(view.Item) > width)
                (widest, width) = (view, RecordLayout.PhysicalWidth(view.Item));
        return widest;
    }

    /// <summary>⛔ DOES THE RECORD A STATEMENT JUST RELEASED HAVE ANYWHERE TO BE "ALSO AVAILABLE"? (§14.9.51.4 GR4
    /// for WRITE, §14.9.35.4 GR6 for REWRITE, §14.9.32.4 GR3 for RELEASE — one rule, three statements; kb/Work
    /// PB1195.) The rule is the SAME RECORD AREA clause's: <i>"The logical record is also available as a record of
    /// other file-names referenced in the same SAME RECORD AREA clause ... as well as the file associated with
    /// record-name-1"</i>. Character-window records share one backing, so among them it holds for free; it needs
    /// an emitted store exactly where an OUT-OF-LINE record (D-FRA, <see cref="FileModel.IsOutOfLineRecord"/>) is on
    /// either side — the released record is out of line (the shared character area must be told), or some other
    /// record of the area is (<see cref="FileModel.OutOfLineRecordsOfArea"/>). Decided at compile time, so a
    /// program with no such pair renders byte-for-byte as before.</summary>
    internal bool ReleasedRecordNeedsAlsoAvailable(FileModel file, Place released) =>
        file.SameRecordAreaPeers.Count > 0
        && (AreaCharacterViewFor(file, released.Item) is not null
            || file.OutOfLineRecordsOfArea(released.Item).Any());

    /// <summary>The first half of a released record's "also available" rule (see
    /// <see cref="ReleasedRecordNeedsAlsoAvailable"/>): when it applies, declares the local that holds the record
    /// image the statement sends and rewrites <paramref name="image"/> to that local, so the statement and the store
    /// after it (<see cref="EndReleasedRecord"/>) use one evaluation. Returns the local's name, or null when the rule
    /// has nowhere to store and the statement renders exactly as before.</summary>
    internal string? BeginReleasedRecord(FileModel file, Place record, ref string image)
    {
        if (!ReleasedRecordNeedsAlsoAvailable(file, record)) return null;
        string local = $"__rel{ctx.Names.NextKeyedSeq()}";
        ctx.Writer.Line($"string {local} = {image};");
        image = local;
        return local;
    }

    /// <summary>The second half: after the statement, on a SUCCESSFUL completion only (§14.9.35.4 GR6 "a successful
    /// execution"; the status is the connector's, read as the statement's own <paramref name="statusExpr"/>), store
    /// the released record — at the length GR13 gives it (<paramref name="lengthArg"/>) — into the other records of
    /// the area. A null <paramref name="released"/> (the rule has nothing to do) emits nothing.</summary>
    internal void EndReleasedRecord(FileModel file, Place record, string? released, string lengthArg, string statusExpr)
    {
        if (released is null) return;
        using (ctx.Writer.Block($"if ({IoStatusClass.Successful(statusExpr)})"))
            EmitReleasedRecordAlsoAvailable(file, record, RuntimeApi.FileReleasedRecord(released, lengthArg),
                OperandText.RecordAreaExtents(record));
    }

    /// <summary>⛔ THE ONE "ALSO AVAILABLE" STORE of a released record — the WRITE / REWRITE / RELEASE twin of
    /// <see cref="EmitRecordAreaStore"/>, and built from the same two halves so a record released and a record read
    /// reach the same records of the area: the CHARACTER half (<see cref="AreaCharacterViewFor"/>) through
    /// <see cref="EmitImageInto"/>, each OTHER out-of-line record of the area through <see cref="EmitOutOfLineInto"/>.
    /// Emitted only after a SUCCESSFUL statement (§14.9.35.4 GR6 says "a successful execution"). The released
    /// record is the statement's own — <paramref name="releasedRecord"/> is a C# string expression of it at its
    /// released length (<see cref="RuntimeApi.FileReleasedRecord"/>) and <paramref name="extents"/> its extent table
    /// expression or null — never re-read from the record area after the statement.</summary>
    internal void EmitReleasedRecordAlsoAvailable(FileModel file, Place released, string releasedRecord, string? extents)
    {
        if (AreaCharacterViewFor(file, released.Item) is { } chars) EmitImageInto(chars, releasedRecord);
        foreach (var record in file.OutOfLineRecordsOfArea(released.Item))
            if (refs.ResolveItem(record) is { } place)
                EmitOutOfLineInto(place, releasedRecord, extents ?? "null");
    }

    /// <summary>Store a read record image into the FD record area: a character-image group distributes via FromImage;
    /// an elementary record decodes the image into its own representation through
    /// <see cref="ReceivingStore.StorageArea"/> (kb/Work PB1556).</summary>
    public void EmitImageInto(Place record, string imageExpr)
    {
        var w = ctx.Writer;
        // A character-image group record distributes the read image into its typed leaves via the generated FromImage.
        // A Tier-B view record (a multi-01 FD whose shared area is a synthesized REDEFINES) has no struct to call
        // FromImage on — its Read() is a string window — so splice the padded image into its backing via Write, as for
        // an elementary record. (Mirrors EmitGroupImage's RedefViewPlace handling.)
        if (record.Item.IsGroup)
        {
            // A STORAGE-boundary write: the WHOLE record image, whatever the record area's storage shape (a
            // record struct, a Tier-B view over a shared area, an occurs-depending record — kb/Work PB80) — the
            // ONE full-image store. A mixed-usage record area distributes through the generated FromImage (its
            // BINARY/PACKED/COMP-5/float leaves decode their byte slices — COBOLNET_DESIGN §14.4/§8.2); a
            // record that is variable-length or pointer/object-leafed is the loud Tier-C island inside the helper (§1.4; kb/Work PB164 + R40).
            // ⛔ THE FIT WIDTH IS THE RECORD'S BYTE EXTENT (kb/Work PB327) — §14.9.30.4 GR14/GR15 measure a
            // record in BYTES, and FromImage distributes a byte image. ByteWidth IS ImageWidth for every leaf
            // kind but NATIONAL, so this is byte-identical for national-free records.
            w.Line(PlaceRenderer.WriteFullGroupImage(record, RuntimeApi.StrStore(imageExpr, $"{record.Item.ByteWidth}"),
                $"record area '{record.Item.CobolName}' read"));
            return;
        }
        // ⛔ AN ELEMENTARY RECORD AREA DECODES ITS STORAGE BYTES (kb/Work PB1556) — the receiving inverse of
        // OperandText.RecordAreaImage, arm for arm, through THE ONE elementary storage-area store: a record
        // description entry may be a single elementary item of ANY usage, and §14.9.30.4 GR13 c) / §14.9.34.4
        // GR3 make the record "available in the record area" — its bytes, whatever the item's representation.
        // The area is fitted to the record's BYTE extent (§14.9.30.4 GR14/GR15 measure a record in bytes).
        // ⛔ A NATIONAL record area is NOT fitted first (kb/Work PB327): NatReadWindow fills a position the image
        // is too short to hold with the NATIONAL space, which is precisely §14.9.30.4 GR15's "If the record-area
        // associated with file-name-1 is specified implicitly or explicitly as national, a trailing space is
        // defined to be the national space character" — a byte-level space pad would manufacture U+2020 instead.
        var item = record.Item;
        string area = item.Pic is { Usage: Usage.National } ? imageExpr : RuntimeApi.StrStore(imageExpr, $"{item.ByteWidth}");
        w.Line(ReceivingStore.StorageArea(record, area, ReceivingStore.StoragePositions(record)));
    }

    /// <summary>After an I/O verb, store the file's two-character I-O status into its FILE STATUS item (ISO §9.1.13),
    /// when the SELECT declared one.</summary>
    public void EmitStoreFileStatus(FileModel file)
    {
        // ISO §12.4.5.8 / §9.1.13.1 — the two-character status is stored into the FILE STATUS item as part of
        // the I/O statement's execution, BEFORE any exception processing.
        if (file.FileStatusName is null) return;   // no FILE STATUS clause — nothing to store
        // An INHERITED GLOBAL file stores into the OWNER's status item through the __outer chain
        // (§12.4.5.8.4 GR1 NOTE 1 — the item is updated by contained-program references to the global
        // file-name even though it is a LOCAL name of the owner; map built per unit in ProgramEmitter.EmitProgramClass).
        Place? place = callState.InheritedStatusPlace.TryGetValue(file, out var inherited)
            ? inherited
            : file.FileStatusItem is { } own ? refs.ResolveItem(own) : null;
        if (file.FileStatusItem is not { } item || place is null)
        {
            // §1.4 loud-guard doctrine: a DECLARED FILE STATUS name that did not resolve is never silent.
            ctx.Writer.Line(LoudStmt($"FILE STATUS item '{file.FileStatusName}' is not resolvable to storage (ISO §12.4.5.8)"));
            return;
        }
        string status = RuntimeApi.StrStore(RuntimeApi.FileStatus(FileKeyExpr(file)), $"{item.Pic?.Length ?? item.ImageWidth}");
        if (item.IsGroup && place is not RedefViewPlace)
        {
            // Same image-capability rule as every other group receiver (COBOLNET_DESIGN §14.4): a mixed-usage
            // status group distributes via FromImage; only a VARIABLE-LENGTH group or a group with a pointer/object-class leaf stays loud (§1.4; kb/Work PB164 + R40).
            if (!item.IsImageCapable)
            {
                ctx.Writer.Line(LoudStmt(TierCIsland.Reason(item, "FILE STATUS into group")));
                return;
            }
            // A GROUP status item fills without conversion through the image facility (§14.9.25.4 GR4 — the
            // CCVS shape `01 SQ-FS2-STATUS. 03 KEY-1 PIC X. 03 KEY-2 PIC X.`); a struct field cannot take the
            // raw string write.
            ctx.Writer.Line(PlaceRenderer.WriteFullGroupImage(place, status, "FILE STATUS group"));   // the ONE full-image store (kb/Work PB80)
            return;
        }
        ctx.Writer.Line(PlaceRenderer.Write(place, status));
    }

    /// <summary>The C# <c>int</c> expression for an ADVANCING line count (a literal or a numeric data-name), narrowed
    /// only through the saturating <c>RuntimeApi.HostInt32</c> / <c>HostInt32Literal</c> (kb/Work PB1033): a cast wrapped
    /// a 10-digit count, and a literal past <c>int</c> was CS0221. A count past the carrier advances
    /// 2,147,483,647 lines — docs/CONFORMANCE.md §3 "Integer operands and host carriers".</summary>
    private string LinesExpr(BoundOperand lines) => lines switch
    {
        BoundNumericLiteral n => RuntimeApi.HostInt32Literal(n.Text),
        BoundOperandError e => LoudValue("int", e.Feature),
        // An integer identifier (§14.9.47.3 SR — identifier-2 an integer item) reads by VALUE through the ONE integer
        // landing (kb/Work PB86's sweep: the raw read mis-counted a P-scaled item; the `_ => "1"` default was a swallow).
        _ => RuntimeApi.HostInt32(NumericRenderer.Align(num.AsNum(lines, ReceiverContext.None), 0)),
    };
}
