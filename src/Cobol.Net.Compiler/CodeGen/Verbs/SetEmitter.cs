// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

/// <summary>The SET-family emitter (P7 Step 9i — a real collaborator over the per-unit
/// <see cref="EmitContext"/>): SET … TO / UP-DOWN BY / pointer F4 senders / OCCURS-DYNAMIC capacity /
/// condition-names TO TRUE, plus the ONE SET-target store/augment pair PERFORM VARYING and SEARCH ride.</summary>
internal sealed class SetEmitter(EmitContext ctx, NumericRenderer num, ArithmeticEmitter arith, PtrEmitter ptr, MoveEmitter move,
    ReceiverBracketEmitter brackets)
{
    /// <summary>The DATA DIVISION emitter of THIS unit, built once on first use — the SET lane reaches it for
    /// exactly one thing: <see cref="DataEmitter.ValueImageOf"/>, the ONE §13.18.63 VALUE recipe §14.9.39.4 GR6
    /// places a level-88 literal by (kb/Work PB560). Lazy and cached for the same reason the report lane's twin
    /// is (<see cref="ReportWriterEmitter"/>): a DataEmitter builds a PhysicalModel and its four collaborators,
    /// and a program with no <c>SET condition-name</c> must not pay for one.</summary>
    private DataEmitter? _data;
    private DataEmitter Data => _data ??= new DataEmitter(ctx);

    /// <summary><c>SET … TO value</c> (ISO §14.9.39 Format 1): the sender is evaluated ONCE through THE ONE SET
    /// amount landing (GR2 — "the value of the sending operand is determined once"), then each receiver takes it
    /// by kind: an index-name or index data item receives it unchanged (GR2a/GR2b — in the §3.5 model an index IS
    /// its 1-based occurrence number, so cross-table conversion is the identity); a numeric data item receives the
    /// occurrence number through its own PICTURE store (GR2c).
    /// <para>⛔ THE LANDING IS THE GUARD (kb/Work PB459). GR2 a) 1. a — "If the value of arithmetic-expression-1
    /// does not result in an integer" — and GR2 a) 1. b — "outside the limit specified in General rule 2 of
    /// 13.18.38" — each state the same three consequents (the condition set to exist · the SET unsuccessful · the
    /// receiving operand unchanged), and the former <c>long __set = (long)(Align(…, 0))</c> could satisfy NEITHER:
    /// the cast TRUNCATED the fraction the first tests for and WRAPPED the magnitude the second rejects. The whole
    /// receiver loop now sits inside the landing's success leg, so "unsuccessful" leaves EVERY receiver unchanged
    /// — the rule is stated over the statement, not over the receiver that happened to be reached first.</para>
    /// <para>The guard rides every receiver kind and that is exactly right: §14.9.39.3 SR3 forbids
    /// arithmetic-expression-1 with an index-data-item receiver and SR4 requires index-name-2 with a numeric
    /// receiver, so an arithmetic-expression sender implies EVERY receiver is an index-name-1 — GR2 a)'s own
    /// population. With an index / index-data-item sender the value is a <c>long</c> occurrence number, integral
    /// and inside the implementor range by construction, so the landing is a no-op on the GR2 b) / GR2 c) arms
    /// rather than a rule applied where the standard states none.</para></summary>
    public void EmitSetTo(BoundSetTo s)
    {
        var w = ctx.Writer;
        string guard = LandAmount(s.Value, SetAmountRule.IndexTo, "SET … TO", out string tmp, "set");
        // GR2: each receiver is identified as it is changed, so an object-property receiver's accessors go around ITS store
        // (kb/Work PB2078).
        using (w.Block($"if ({guard})"))
            foreach (var t in s.Targets) brackets.Receive(t, () => StoreSetTarget(t, new NumX(tmp, 0)));
    }

    /// <summary>⛔ THE ONE SET-FAMILY AMOUNT LANDING at the emitter — the two-lane render that keeps an amount's
    /// FRACTION and its FULL MAGNITUDE alive all the way into <see cref="CobolIndex"/>, where §14.9.39.4's
    /// integrality / range / sign guards are written once (kb/Work PB459). Renders the amount temp and returns the
    /// C# condition text that is TRUE when the landing succeeded; the caller puts the statement's effect inside
    /// <c>if (…)</c> so the unsuccessful leg leaves every receiving operand unchanged.
    /// <para>The shape is <see cref="PtrEmitter.EmitSetPointerUpDown"/>'s, by construction: a NATIVE-FLOAT amount
    /// keeps its <c>double</c> so the integrality test runs on the double (kb/Work PB151 — an emitter-side
    /// <c>(long)(double)</c> truncation bypasses the raise entirely), and every other amount arrives as its EXACT
    /// scaled <c>Int128</c> with its scale, so the divisibility test IS the integrality test and a magnitude past
    /// the 64-bit index carrier is a value the runtime can see rather than a wrap it cannot.</para>
    /// <para><paramref name="valueVar"/> is declared by the <c>out long</c> in the returned condition, so it is in
    /// scope for the whole enclosing block — the landed, guaranteed-integral occurrence number.</para></summary>
    /// <param name="x">The rendered amount, BEFORE any narrowing. Funnelled through <c>Landed</c>/<c>DeU</c> here,
    /// so an SDIDI intermediate (STANDARD-DECIMAL, or a native <c>**</c> — kb/Work PB84) and an unsigned-wide read
    /// (kb/Work R10) each reach the exact <c>Int128</c> lane in ONE place rather than at every caller.</param>
    /// <param name="prefix">The generated temp's name stem, so the emitted C# still reads as the statement it came
    /// from (<c>__set…</c> / <c>__cap…</c> / <c>__pv…</c>).</param>
    public string LandAmount(NumX x, SetAmountRule rule, string detail, out string valueVar, string prefix)
    {
        x = num.Landed(NumericRenderer.DeU(x), ReceiverContext.None);
        int id = ctx.Names.NextSet();
        valueVar = $"__{prefix}{id}";
        string raw = $"__amt{id}";
        if (x.Real)
        {
            ctx.Writer.Line($"double {raw} = ({x.Expr});");
            return RuntimeApi.IndexTryAmountReal(raw, rule, detail, valueVar);
        }
        ctx.Writer.Line($"Int128 {raw} = (Int128)({x.Expr});");
        return RuntimeApi.IndexTryAmount(raw, $"{x.Scale}", rule, detail, valueVar);
    }

    /// <inheritdoc cref="LandAmount(NumX,SetAmountRule,string,out string,string)"/>
    private string LandAmount(BoundExpr amount, SetAmountRule rule, string detail, out string valueVar, string prefix) =>
        LandAmount(num.Render(amount, ReceiverContext.None), rule, detail, out valueVar, prefix);

    /// <summary><c>SET { ADDRESS OF data-name-1 | identifier-5 } … TO identifier-6</c> (ISO §14.9.39 Format 7):
    /// ONE loop over the printed receiving list, each operand taking its own general rule.
    /// <para>⛔ GR12 AND GR13 ARE ONE LOOP, BECAUSE THE STANDARD WRITES THEM AS ONE (kb/Work PB450). GR12 — "the
    /// address identified by identifier-6 is stored in EACH data item referenced by identifier-5 IN THE ORDER
    /// SPECIFIED" — and GR13 — the same sentence for the based data-name-1 spelling — differ only in what a
    /// receiver IS, which is why <see cref="BoundPointerReceiver"/> carries the kind per operand and the two
    /// spellings may be mixed in one statement. A separate node and a separate emitter used to hold the GR13
    /// half at arity one, and a mixed statement could not be expressed at all.</para></summary>
    public void EmitSetPointer(BoundSetPointer s)
    {
        // The sender is ONE operand OUTSIDE the printed repetition, so it is evaluated ONCE (kb/Work PB394) —
        // which also makes `SET P1 P2 TO ADDRESS OF R` and a receiver that names the sender's own item agree
        // with the rule. A TO NULL sender is the constant and needs no local.
        string src = s.ToNull ? "ManagedPointer.Null"
            : ctx.SendOnce(s.Address is { } a ? ptr.AddressOfText(a)   // §8.4.3.11 data-address-identifier sender
                                              : PlaceRenderer.Read(s.Source!), s.Receivers.Count, "setPtr");
        foreach (var r in s.Receivers)
            if (r.Based is { } based) ptr.EmitSetAddressOfBased(based, src);       // GR13 — data-name-1
            else ctx.Writer.Line(PlaceRenderer.Write(r.Pointer!, src)              // GR12 — identifier-5
                                 + "   // SET pointer (ISO §14.9.39.4 Format 7 GR12)");
    }

    /// <summary><c>SET LOCALE … TO …</c> (ISO §14.9.39 Format 11; kb/Work PB64 T1): one call on the run unit's ONE
    /// <c>LocaleState</c> per (first operand, source) pair — GR22 (the user default), GR23a/b/c (the categories from a
    /// locale-name / a saved locale / the defaults); GR24 / GR21 are the runtime's EC-LOCALE-MISSING / -INVALID-PTR.</summary>
    public void EmitSetLocale(BoundSetLocale s)
    {
        string state = "RunUnit.Current.Locale";
        string cats = s.Categories == LocaleCategorySet.All ? "LocaleCategorySet.All"
            : string.Join(" | ", Enum.GetValues<LocaleCategorySet>().Where(c => c is not (LocaleCategorySet.None or LocaleCategorySet.All) && s.Categories.HasFlag(c)).Select(c => $"LocaleCategorySet.{c}"));
        string call = (s.SetsUserDefault, s.Source) switch
        {
            (true, LocaleSetSource.LocaleName) => $"{state}.SetUserDefaultFromLocale({CsLiteral(s.Locale!.External)})",
            (true, _) => $"{state}.SetUserDefaultFromSaved({PlaceRenderer.Read(s.SavedPointer!)})",
            (false, LocaleSetSource.LocaleName) => $"{state}.SetFromLocale({cats}, {CsLiteral(s.Locale!.External)})",
            (false, LocaleSetSource.SavedPointer) => $"{state}.SetFromSaved({cats}, {PlaceRenderer.Read(s.SavedPointer!)})",
            (false, LocaleSetSource.UserDefault) => $"{state}.SetFromUserDefault({cats})",
            _ => $"{state}.SetFromSystemDefault({cats})",
        };
        ctx.Writer.Line($"{call};   // SET LOCALE (ISO §14.9.39 Format 11{(s.SetsUserDefault ? ", GR22" : ", GR23")})");
    }

    /// <summary><c>SET identifier TO LOCALE {LC_ALL | USER-DEFAULT}</c> (ISO §14.9.39 Format 12; GR26/GR27): a saved-locale
    /// handle into the data-pointer.</summary>
    public void EmitSaveLocale(BoundSaveLocale s) =>
        ctx.Writer.Line(PlaceRenderer.Write(s.Target, $"RunUnit.Current.Locale.Save(userDefault: {(s.UserDefault ? "true" : "false")})")
            + $"   // SET … TO LOCALE {(s.UserDefault ? "USER-DEFAULT" : "LC_ALL")} (ISO §14.9.39 Format 12)");

    private static string CsLiteral(string s) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(s, quote: true);

    /// <summary><c>SET program-pointer… TO {NULL | program-pointer}</c> (ISO §14.9.39 Format 9; P10 Step 7):
    /// a straight carrier copy — the Format-4 data-pointer twin over <c>ProgramPointer</c>.</summary>
    public void EmitSetProgramPointer(BoundSetProgramPointer s)
    {
        string src = s.ToNull ? "ProgramPointer.Null"
            : ctx.SendOnce(PlaceRenderer.Read(s.Source!), s.Targets.Count, "setPp");   // §14.9.39.4 GR18 (PB394)
        foreach (var t in s.Targets)
            ctx.Writer.Line(PlaceRenderer.Write(t, src) + "   // SET program-pointer (ISO §14.9.39 Format 9)");
    }

    /// <summary><c>SET function-pointer… TO {NULL | function-pointer}</c> (ISO §14.9.39.2 Format 8; GR14 — the
    /// address is stored in each receiver in the order specified): the Format-9 twin, a carrier copy. The GR14
    /// EC-FUNCTION-PTR-INVALID screen belongs to the ADDRESS OF FUNCTION sender, not here — §14.9.39.3 SR20 has
    /// already proven the two prototypes' signatures equal at bind and §13.18.60.4 GR26 makes the sender's
    /// content NULL-or-same-signature by construction. kb/Work PB452.</summary>
    public void EmitSetFunctionPointer(BoundSetFunctionPointer s)
    {
        string src = s.ToNull ? "FunctionPointer.Null"
            : ctx.SendOnce(PlaceRenderer.Read(s.Source!), s.Targets.Count, "setFp");   // §14.9.39.4 GR14 (PB394)
        foreach (var t in s.Targets)
            ctx.Writer.Line(PlaceRenderer.Write(t, src) + "   // SET function-pointer (ISO §14.9.39.2 Format 8)");
    }

    /// <summary><c>SET index-name… {UP|DOWN} BY amount</c> (ISO §14.9.39 Format 2): the amount is evaluated ONCE
    /// through THE ONE landing (GR3 — "If arithmetic-expression-2 does not evaluate to an integer" ⇒
    /// EC-BOUND-SUBSCRIPT, the SET unsuccessful, the receiving operand unchanged), then each index is adjusted by
    /// it through the GUARDED augment (GR4 a) — a result outside the §13.18.38.4 GR2 implementor range is
    /// EC-RANGE-INDEX with the index unchanged). kb/Work PB459: both guards were absent, so `SET IX UP BY 1.5`
    /// moved IX by 1 and `UP BY 9223372036854775800` twice left IX holding a NEGATIVE occurrence number.</summary>
    public void EmitSetUpDown(BoundSetUpDown s)
    {
        var w = ctx.Writer;
        string verb = s.Down ? "SET … DOWN BY" : "SET … UP BY";
        string guard = LandAmount(s.Amount, SetAmountRule.IndexBy, verb, out string tmp, "set");
        using (w.Block($"if ({guard})"))
            foreach (var t in s.Targets) AugmentSetTarget(t, s.Down, new NumX(tmp, 0), verb);
    }

    /// <summary>SET Format 14 (ISO §14.9.39 GR29; OCCURS DYNAMIC, data-model D9): the amount is evaluated ONCE,
    /// then the owning table's current capacity is set / raised / lowered through the runtime — new occurrences
    /// seeded (§8.5.1.9.5), clamped to the minimum, and EC-FLOW-SEARCH raised if a SEARCH of the same table is
    /// active (GR31). The register carries no storage; the operation is on the <c>CobolDynTable&lt;T&gt;</c> itself.</summary>
    public void EmitSetCapacity(BoundSetCapacity s)
    {
        var w = ctx.Writer;
        string call = s.Kind switch
        {
            SetCapacityKind.To => "SetCapacity",
            SetCapacityKind.UpBy => "CapacityUpBy",
            _ => "CapacityDownBy",
        };
        // GR29 — "If arithmetic-expression-4 does not evaluate to a NONNEGATIVE integer, the EC-BOUND-SUBSCRIPT
        // exception condition is set to exist and the execution of the SET statement is unsuccessful." The test is
        // on the AMOUNT, in all three kinds (GR30 forms the new capacity from it afterwards), and GR30's
        // minimum-capacity clamp CANNOT stand in for it: the clamp is the rule for a legal new capacity below the
        // OCCURS minimum, and GR29 rejects the operand BEFORE any new capacity is computed. kb/Work PB459
        // measured both holes — `SET CAP TO A` (A = −3) DESTROYED five live occurrences, `SET CAP TO B` (B = 3.5)
        // shrank five to three.
        string guard = LandAmount(s.Amount, SetAmountRule.Capacity,
            $"SET capacity-register {SetCapacityKinds.Text(s.Kind)}", out string tmp, "cap");
        using (w.Block($"if ({guard})"))
            w.Line($"{PlaceRenderer.RenderPath(s.Table, AccessDir.Sending)}.{call}({tmp});");
    }

    /// <summary>SET [SIZE OF] data-name TO n (ISO §14.9.39 Format 16, COBOL-2023): evaluate the amount ONCE, then
    /// resize the dynamic-length item's native string in place — <c>CobolDynString.SetSize</c> space-fills grown
    /// positions (GR39), drops trailing ones on shrink, clamps above the LIMIT and floors a negative to 0
    /// (GR37/GR38), and raises the nonfatal EC-STORAGE-NOT-AVAIL on the clamp/negative legs through the engine's
    /// ambient (EC-STORAGE-NOT-AVAIL → StorageNotAvailChecking) pair — the gate this statement's EC wrapper set —
    /// so the raise also runs the §14.6.13.1.4 #3 declarative selection (kb/Work PB367b).</summary>
    public void EmitSetSize(BoundSetSize s)
    {
        // Evaluate arithmetic-expression-5 at FULL precision (a double) — the GR37 sign test must precede the
        // GR38 clamp and the toward-zero truncation, so a fractional negative in (−1,0) still raises. Mirrors the
        // CONTINUE AFTER interval render (StatementEmitter.Visit(BoundContinueAfter)); the runtime does the
        // truncation, not a (long) cast here that would lose the sign of a (−1,0) value.
        string amt = $"__sz{ctx.Names.NextSet()}";
        ctx.Writer.Line($"double {amt} = {NumericRenderer.Real(num.Render(s.Amount, ReceiverContext.None))};");
        ctx.Writer.Line(PlaceRenderer.Write(s.Target,
            RuntimeApi.DynSetSize(PlaceRenderer.Read(s.Target), amt, s.MaxSize.ToString())));
    }

    /// <summary>SET CONTENT OF identifier-14 … TO … (ISO §14.9.39.2 Format 15, numeric-content; kb/Work PB452) —
    /// one store per receiver, each with the content the binder computed for THAT receiver's data description.
    /// <list type="bullet">
    /// <item>GR32/GR36 (FARTHEST-FROM-ZERO / NEAREST-TO-ZERO): the extreme is a numeric literal, stored through
    /// the ONE MOVE path — so conversion, truncation and an unsigned receiver's sign drop are the ordinary store
    /// rules and not a second copy of them. The <see cref="BoundMove"/> was BUILT BY THE BINDER (kb/Work PB348);
    /// this only renders it.</item>
    /// <item>GR33/GR34/GR35 (FLOAT-INFINITY / FLOAT-NOT-A-NUMBER[-SIGNALING]): a bit-exact write of the carrier.
    /// No numeric store path can carry an infinity or a NaN — CobolNum's store rules are stated over algebraic
    /// values — and §14.9.39.3 SR32 has already confined the receiver to a STANDARD floating-point usage, so the
    /// carrier IS the ISO/IEC 60559:2020 basic interchange format GR33-GR35 name. A WINDOWED receiver (Tier-B /
    /// image-stored) takes its IEEE window bytes, the same shape MoveEmitter's and InitializeEmitter's
    /// float-receiver arms use, so all three deposit identical bytes into the same window.</item></list></summary>
    public void EmitSetContent(BoundSetContent s)
    {
        foreach (var st in s.Stores)
        {
            if (st.Ieee is { } which)
            {
                bool single = st.Target.Item.Pic!.IsSingle;
                string ieee = RuntimeApi.FloatFromBits(IeeeSpecials.Bits(which, single, st.NegativeSign), single);
                ctx.Writer.Line(PlaceRenderer.Write(st.Target, st.Target.Item.StoreAsImage
                    ? RuntimeApi.NumFormatImageFloat(ieee, st.Target.Item.ProfileName, st.Target.Item.Pic!.IsSingle)
                    : ieee));
                continue;
            }
            move.Emit(st.Store!);
        }
    }

    /// <summary>THE store into a SET-style target (shared by SET TO and PERFORM VARYING initialization): an
    /// index-name field or index data item takes the integer value UNCHANGED (§14.9.39 GR2a/2b — an index IS its
    /// occurrence number); a numeric data item takes it through its own PICTURE store (GR2c).
    /// <para>⛔ PRECONDITION — <paramref name="value"/> SHALL ALREADY HAVE COME THROUGH <see cref="LandAmount"/>
    /// when the target is an index (kb/Work PB459): the integrality (§14.9.39.4 GR2 a) 1. a) and implementor-range
    /// (GR2 a) 1. b / §13.18.38.4 GR2) guards are stated over the value BEFORE it reaches the <c>long</c> carrier,
    /// so they cannot be applied here — by the time this renders, a fraction and a 64-bit overflow are already
    /// gone. Both of this method's callers land first (<see cref="EmitSetTo"/>, and
    /// <c>ControlFlowEmitter.InitVaryingTarget</c> for PERFORM VARYING's FROM, which §13.18.38.4 GR2 names
    /// alongside SET); <c>SetIndexStoreLandingTests</c> is the drift test that keeps that true for the next
    /// caller. The landing hands back a range-checked <c>long</c>, so an index target stores it AS IS — no
    /// narrowing, and never a C# cast (kb/Work PB1033: a cast here would wrap anything that bypassed the
    /// landing; without one, a caller that did bypass it fails to compile instead).</para></summary>
    public void StoreSetTarget(BoundSetTarget t, NumX value)
    {
        switch (t)
        {
            case SetIndexTarget ix:
                ctx.Writer.Line($"{ix.IndexField} = {NumericRenderer.Align(value, 0)};");
                break;
            case SetPlaceTarget { Place: var p } when p.Item.Pic is { Usage: Usage.Index }:
                // A WINDOWED index data item (Tier-B / image-stored — the Step D arm-1 dissolution) stores
                // its 8 occurrence-number bytes (the R40 pin); a native one takes the raw long unchanged
                // (§14.9.39 GR2b). The raw arm against a window was CS1503 in generated code.
                ctx.Writer.Line(PlaceRenderer.Write(p, p.Item.StoreAsImage
                    ? RuntimeApi.NumFormatImage(NumericRenderer.Align(value, 0), p.Item.ProfileName)
                    : NumericRenderer.Align(value, 0)));
                break;
            case SetPlaceTarget { Place: var p }:
                arith.StoreArith(p, value, CobolRounding.Truncation);
                break;
        }
    }

    /// <summary>THE augment of a SET-style target by ±amount (shared by SET UP/DOWN BY, PERFORM VARYING and
    /// SEARCH's GR8b varied index): index-name / index data item → THE GUARDED occurrence-number augment; a
    /// numeric data item → an in-place add through its PICTURE store (legal as a VARYING induction variable,
    /// §14.9.28 GR13). ⛔ A plain <c>SET</c> UP/DOWN on a numeric item is invalid COBOL and is now REFUSED AT
    /// BIND (§14.9.39.2 Format 2's receiving operand is index-name-3; COBOLNET2112, kb/Work PB449) — this comment
    /// used to say "the edition validator will diagnose it", and no validator did: the statement compiled and
    /// ran. The numeric arm survives because PERFORM VARYING and SEARCH reach this same augment.
    /// <para>⛔ THE INDEX ARMS ARE GUARDED (kb/Work PB459). §14.9.39.4 GR4 a) and §13.18.38.4 GR2 — which names
    /// PERFORM VARYING and SEARCH beside SET as the three statements that may modify an index — make a result
    /// "outside the range of the values allowed by the implementor" the EC-RANGE-INDEX case with the receiving
    /// operand unchanged. The former <c>{ix} += (long)(…)</c> formed the sum IN the carrier, so the boundary was a
    /// silent wrap: measured, <c>SET IDX UP BY 9223372036854775800</c> then <c>UP BY 100</c> left IDX holding a
    /// NEGATIVE occurrence number and the program carried on. <see cref="CobolIndex.Augment"/> forms it in
    /// <c>Int128</c>, where the boundary is a value the runtime can see.</para>
    /// <para>The amount stays <c>Int128</c> into the runtime for the same reason: a PERFORM VARYING BY operand is
    /// an integer data item (§14.9.28.3 SR4 a/c — so no integrality test belongs here) but may be up to 31 digits,
    /// which a <c>long</c> narrowing would wrap before the guard could see it.</para>
    /// <para>The numeric-data-item arm is deliberately NOT guarded: §14.9.28.4 GR13's induction variable is a
    /// PICTURE store with its own §14.7 size rules, and no index exists to be out of range.</para></summary>
    /// <param name="detail">The statement this augment belongs to, for the EC-RANGE-INDEX detail — every caller
    /// names its own verb so the diagnostic reads as the user's statement, not as "SET".</param>
    public void AugmentSetTarget(BoundSetTarget t, bool down, NumX amount, string detail)
    {
        string op = down ? "-" : "+";
        // Int128, not long: see the remarks — a 31-digit BY operand must reach the guard unwrapped.
        string by = $"(Int128)({NumericRenderer.Align(amount, 0)})";
        switch (t)
        {
            case SetIndexTarget ix:
                ctx.Writer.Line($"{ix.IndexField} = {RuntimeApi.IndexAugment(ix.IndexField, by, down, detail)};");
                break;
            case SetPlaceTarget { Place: var p } when p.Item.Pic is { Usage: Usage.Index }:
                // The windowed twin of the StoreSetTarget arm (Step D): decode → augment → re-encode.
                // sending: false — an INDEX data item's class and category are INDEX, not numeric
                // (§13.18.60.4 GR10), so §14.6.13.2 rule 2 ("a numeric sending item") is not about it.
                string cur = p.Item.StoreAsImage
                    ? RuntimeApi.NumParseImage(PlaceRenderer.Read(p), p.Item.ProfileName, sending: false)
                    : PlaceRenderer.Read(p);
                string augmented = RuntimeApi.IndexAugment($"(long)({cur})", by, down, detail);
                ctx.Writer.Line(PlaceRenderer.Write(p, p.Item.StoreAsImage
                    ? RuntimeApi.NumFormatImage(augmented, p.Item.ProfileName)
                    : augmented));
                break;
            case SetPlaceTarget { Place: var p }:
                arith.StoreArith(p, num.Combine(num.FieldNum(p), op, amount, ReceiverContext.None), CobolRounding.Truncation);
                break;
        }
    }

    public void EmitSet(BoundSetConditions set)
    {
        foreach (var (parent, cond, toTrue) in set.Sets)
        {
            // ⛔ THE ONLY DIFFERENCE BETWEEN THE TWO ARMS (kb/Work PB555). §14.9.39.4 GR6 stores "the literal in
            // the VALUE clause … If more than one literal is specified in the VALUE clause, the conditional
            // variable is set to the value of the FIRST literal that appears in the VALUE clause"; GR7 stores
            // "the literal in the FALSE phrase of the VALUE clause" — §13.18.63.4 GR20's literal-4. Everything
            // after this line is the ONE store both rules describe in the same words, so the FALSE arm inherits
            // the figurative fill, the group-image splice and the category funnel without a second copy. The
            // binder has already refused a FALSE arm with no phrase (§14.9.39.3 SR7, COBOLNET2049).
            // ⛔ THE ARM IS THE GROUP'S, NOT THE STATEMENT'S (kb/Work PB450): §14.9.39.2 Format 4's outer `…`
            // repeats the whole `{ condition-name-1 } … TO { TRUE | FALSE }` unit, so one statement may store
            // TRUE into one group and FALSE into the next.
            string low = toTrue ? cond.Values[0].Low : cond.FalseValue!;
            // ⛔ THE ONE §13.18.63 VALUE RECIPE — DataEmitter.ValueImageOf → ValueInitializer.InitializerFrom,
            // the SAME method that renders the conditional variable's OWN VALUE clause and the report section's
            // format-4 operand (kb/Work PB506). §14.9.39.4 GR6 does not describe a store of its own: it says the
            // literal "is placed in the conditional variable according to the rules for the VALUE clause", so
            // there is exactly one recipe and this statement is one of its callers (kb/Work PB560).
            // <para>What the private recipe deleted here could not answer, each MEASURED on its own probe:
            // a NUMERIC-EDITED variable took the raw literal text (`88 E-TEN VALUE 10` over `PIC ZZ9.99` stored
            // `10    `, not ` 10.00` — §13.18.63.3 SR6 converts it "according to the rules for the MOVE
            // statement"); a FLOAT variable took `UnscaledAtScale(…, 0)`, dropping the fraction (`VALUE 0.5`
            // stored 0 — §13.18.63.4 GR17/GR1's "approximation of the arithmetic value"); BLANK WHEN ZERO was
            // ignored (`88 B-ZERO VALUE 0` over `PIC ZZ9 BLANK WHEN ZERO` stored `0  `, not spaces —
            // §13.18.63.3 SR8 NOTE 2); a PICTURE format-2 (LOCALE) variable had no runtime compose at all
            // (§13.18.40.5 editing rule 11 — the locale is the one current AT THE TIME of editing, so no
            // compile-time image exists); and a WHOLE-GROUP-
            // ALIASED numeric variable (StoreAsImage) handed a `long` to a `string` field — `SET PK-SEVEN TO
            // TRUE` over `05 PK PIC 9(4) COMP-3` under a REDEFINES failed the whole compilation with CS1503.
            // Every one of the five is a branch InitializerFrom already had, because the VALUE clause needs it.
            // WHICH ARM DID I FIX? The STORE. The TEST arm (ConditionRenderer.StringMembershipExpr /
            // FloatMembershipValue) already called this recipe's shared pieces, which is why a SET-then-IF
            // round-trip returned FALSE; both arms now read §13.18.63's rules from the same code.</para>
            string rhs = Data.ValueImageOf(parent.Item, low);
            // An ORDINARY (alphanumeric) group has neither a PICTURE nor an as-if one (DataItem.OperandPic, the
            // ONE category reader, is null for exactly that shape); it is the receiver that takes its value
            // through the group-image store below rather than as a record-struct assignment. The VALUE clause's
            // own rule for that subject — §13.18.63.3 SR4, "Alphanumeric literals in the VALUE clause of an
            // alphanumeric group item shall not exceed the size of the group item" — is how InitializerFrom
            // described it just now.
            bool imageGroup = parent.Item.OperandPic is null && parent.Item.IsGroup;
            // ⛔ An ORDINARY group receiver takes the value through THE ONE GROUP-IMAGE STORE, not a bare
            // assignment: PlaceRenderer.Write's MemberPlace arm assigns the record struct, so handing it a string
            // is the CS0029 above. §14.9.25.4 GR4 — a group receiver is "filled without consideration for the
            // individual elementary or group items" — and §14.9.39.4 GR6's OCCURS-dependent length is exactly the
            // §13.18.38 GR8 splice WriteGroupImage already carries. A bit / national group is NOT an image group:
            // its as-if value is boolean positions / national positions, which Write routes to FromBits / FromNat.
            ctx.Writer.Line(imageGroup
                ? PlaceRenderer.WriteGroupImage(parent, rhs, $"SET condition '{cond.Name}' TO {(toTrue ? "TRUE" : "FALSE")}")
                : PlaceRenderer.Write(parent, rhs));
        }
    }

    // ── File I/O (ISO §14.9; COBOLNET_DESIGN §8) ─────────────────────────────────────────────────────────────

}
