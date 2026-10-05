// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The data-pointer statement slice (Phase-4b increment 2 — ISO §14.9.39 Formats 7/10, §14.9.3 ALLOCATE,
/// §14.9.15 FREE; the PHASE4_RECONCILIATION "M2-DATA-5 / M2-PROC-5 — increment 2" design). All semantic
/// resolution happens here once: receiver/sender category checks (the 0869 pointer band), the BASED-receiver
/// rule (SR18), edition gates via the registry (binder-side — a grammar predicate would fall through to the
/// OTHER SET alternatives and mis-diagnose), and the staged-loud residue (qualified/subscripted ADDRESS OF
/// operands).
/// P7 Step 10g: a real collaborator over <see cref="BinderContext"/> — the tri-state
/// <see cref="TryBindSetUpDown"/> contract (null = fall through to the index path · BoundNop = error
/// consumed · node = bound) and the non-consuming first-target peek move VERBATIM.
/// <para>⛔ NO NAME LOOKUP LIVES HERE. Every operand resolves through <see cref="ReferenceResolver"/> — a place
/// via <c>ExpressionBinder.ResolveSending</c>/<c>ResolveReceiving</c> by its role (kb/Work PB881), a declaration
/// via <c>DeclarationOf</c> — so the scoping rules are the resolver's in ONE
/// place. The raw <c>ctx.Data.ByName</c> lookup <c>PtrResolveBased</c> used to carry (the documented SymbolTable
/// bypass, moved verbatim by P7 Step 10g and flagged as a behavior-sensitive follow-up) is GONE: it answered a
/// scoped question from the unit-wide map, so inside a method definition the §14.9.39.3 SR18 / §14.9.3.3 SR1
/// based-item verdict was wrong in both directions (kb/Work PB467).</para></summary>
internal sealed class PtrBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>The one §8.4.3.11.3 operand screen of every data-address-identifier surface (kb/Work PB1407).</summary>
    private readonly AddressOfOperandScreen _addressScreen = new(ctx);

    /// <summary>⛔ THE ONE BINDER FOR THE WHOLE PRINTED SET FORMAT 7 (ISO §14.9.39.2; kb/Work PB450).
    /// <para>The rendered figure (PDF p760 / folio 730) is
    /// <c>SET { ADDRESS OF data-name-1 | identifier-5 } … TO identifier-6</c>: the brace is a plain required
    /// choice, the <c>…</c> is OUTSIDE it, so the statement carries a LIST of receiving operands, each
    /// independently either spelling and mixable in one statement, over ONE sender. §14.9.39.4 GR12 ("the
    /// address identified by identifier-6 is stored in each data item referenced by identifier-5 IN THE ORDER
    /// SPECIFIED") and GR13 (the same sentence for data-name-1) are then ONE loop over that list, each operand
    /// taking its own rule — which is exactly how the standard writes them.</para>
    /// <para>Before this the grammar held two fixed productions split on the SENDER's spelling, each with arity
    /// one, and this method branched on <c>GetChild(1)</c>. Four cells of the printed cross-product could not be
    /// written at all, so SR17 and SR18 were unreachable on every one of them.</para>
    /// <para>The sender is bound ONCE, before any receiver is resolved, which is GR12/GR13's "the address
    /// identified by identifier-6" read once — a receiver may name the sender's own item.</para></summary>
    public BoundStatement BindSetAddress(Core.SetAddressStatementContext sa)
    {
        // SET ADDRESS OF (§14.9.39 Format 7) is a COBOL-2002 introduction; the edition gate lives in the
        // post-bind VersionConformancePass (PHASE-03 Step 14b) as a parse-tree override on this rule.
        var send = sa.setAddressSender();
        Place? source = null;
        BoundAddressOf? address = null;
        bool toNull = send.predefinedNull() is not null;
        if (send.dataAddressIdentifier() is { } dai)
        {
            // identifier-6 as a §8.4.3.11 data-address-identifier — §8.4.3.11.4 GR1: "Data-address-identifier
            // creates a unique data item of class pointer and category data-pointer", which is precisely what
            // SR17's second sentence ("Identifier-6 shall be of category data-pointer") demands of it.
            if (BindDataAddress(dai) is not { } addr) return BoundRejected.Reported(ctx.Edition);
            address = addr;
        }
        else if (!toNull)
        {
            // SR19 first sentence's "shall reference a data-pointer" — identifier-6 as a plain pointer item.
            if (PtrResolvePointer(send.dataReference(), "identifier-6, the sending operand of SET Format 7 "
                                                     + "(ISO §14.9.39.2; §14.9.39.3 SR17)", receiving: false) is not { } src)
                return BoundRejected.Reported(ctx.Edition);
            source = src;
        }
        // ⛔ IDENTIFIER-6'S RESTRICTION, DERIVED ONCE. §14.9.39.3 SR19's three sentences all compare a RECEIVER's
        // restriction against identifier-6's, and identifier-6 has one whichever spelling it takes: a pointer
        // item's own declared `TO type-name-1` (§13.18.60.4 GR23), or, for a data-address-identifier, the one
        // §8.4.3.11.4 GR2 gives it ("If identifier-1 is a strongly-typed group item or a restricted
        // data-pointer, the data-address-identifier is a restricted data-pointer that is restricted to the type
        // of identifier-1"). It is a property of the SENDER, which is ONE operand outside the printed
        // repetition, so deriving it per receiver would be the same rule written twice. ⚠ `default` for TO
        // NULL, which SR19's own words admit ("shall be the predefined address NULL or …") and neither
        // direction screens.
        var senderRestriction = address is { } addrSend ? StrongTypeModel.AddressOfRestriction(addrSend.Item)
                              : source is { } ptrSend ? StrongTypeModel.PointerRestriction(ptrSend.Item)
                              : default;
        string senderText = toNull ? "NULL"
            : DataBinder.WrittenText(send.dataAddressIdentifier()?.dataReference() ?? send.dataReference());

        var receivers = new List<BoundPointerReceiver>(sa.setAddressReceiver().Length);
        foreach (var r in sa.setAddressReceiver())
        {
            var dref = r.dataReference();
            if (r.ADDRESS() is not null)
            {
                // data-name-1 — §14.9.39.3 SR18 "Data-name-1 shall be a based data item" (the IBM
                // non-BASED-LINKAGE idiom is a rejected non-ISO extension); GR13 assigns the address to it.
                if (PtrResolveBased(dref) is not { } based) return BoundRejected.Reported(ctx.Edition);   // 0869 reported
                // §14.9.39.3 SR19, second sentence: "If data-name-1 is a strongly-typed group item or a
                // restricted pointer, identifier-6 shall reference a data-pointer restricted to the type of
                // data-name-1." Asked PER RECEIVER, because the restriction is data-name-1's, not the
                // statement's — a mixed list may hold a restricted receiver beside an unrestricted one.
                // ⛔ AND AGAINST IDENTIFIER-6 IN EITHER SPELLING. The predecessor arm compared only against a
                // PLAIN pointer sender, because `SET ADDRESS OF based TO ADDRESS OF x` was a parse error and
                // the case could not arise; it can now, and §8.4.3.11.4 GR2 gives that sender a restriction of
                // its own, so leaving it out would under-reject exactly the shape this landing opened.
                if (!toNull && !ScreenBasedReceiverRestriction(based, senderRestriction, senderText,
                                                               addressSender: address is not null))
                    return BoundRejected.Reported(ctx.Edition);
                receivers.Add(new BoundPointerReceiver(null, based));
                continue;
            }
            // identifier-5 — SR17 "Identifier-5 shall reference a data item of category data-pointer";
            // GR12 stores the address into it.
            // ⛔ THE RULE'S OWN WORDS, AND NO U+2026 (kb/Work PB388 — the diagnostic renderer transliterates
            // an ellipsis to ASCII, so a message spelling the statement with one reads as `SET . TO …`). The
            // former text named the SENDER's spelling ("the receiver of SET … TO ADDRESS OF"), which is false
            // for every Format-7 sender that is a plain pointer or NULL — and those now reach this screen,
            // because the receiving LIST may mix the two spellings.
            if (PtrResolvePointer(dref, "identifier-5, a receiving operand of SET Format 7 "
                                      + "(ISO §14.9.39.2; §14.9.39.3 SR17)", receiving: true) is not { } tp) return BoundRejected.Reported(ctx.Edition);
            // §14.9.39.3 SR19's first sentence over identifier-5 ("If identifier-5 references a restricted
            // data-pointer, identifier-6 shall be the predefined address NULL or shall reference a data-pointer
            // restricted to the same type") and SR20's converse, which the ADDRESS OF sender supplies through
            // §8.4.3.11.4 GR2. ⛔ TO NULL is admitted by SR19's own words and is screened by neither.
            if (!toNull && !ScreenPointerReceiverRestriction(tp, DataBinder.WrittenText(dref), senderRestriction, senderText,
                                                            addressSender: address is not null))
                return BoundRejected.Reported(ctx.Edition);
            receivers.Add(new BoundPointerReceiver(tp, null));
        }
        return new BoundSetPointer(receivers, source, toNull, address);
    }

    /// <summary>§14.9.39.3 SR19's first and THIRD sentences over ONE identifier-5 receiver of a Format-7
    /// statement whose sender is a pointer item or a data-address-identifier. Both directions are asked here
    /// because they are one rule about one (receiver, sender) pair, and a Format-7 statement may now hold
    /// several receivers — a per-statement screen would have answered for operand zero only.
    /// <para>⛔ ONE SCREEN, EVERY ARM THAT STORES INTO IDENTIFIER-5 (kb/Work PB548). Format 7 reaches the binder
    /// by TWO routes: <see cref="BindSetAddress"/> (setAddressStatement — an <c>ADDRESS OF</c> on either side) and
    /// <c>SetBinder.BindSetPointer</c> (setToValueStatement / setObjectReferenceStatement re-routed on the
    /// receivers' category — the plain <c>SET p TO q</c>). The second route never asked this question, so an
    /// UNRESTRICTED pointer was stored into a RESTRICTED one and a based item rebased through it read another
    /// type's storage — exactly what §13.18.60.4 GR23's last sentence ("A restricted data-pointer shall contain
    /// only the predefined address NULL or the address of a data item of the specified type") forbids. Both
    /// routes now call THIS method; there is no second copy of the rule.</para></summary>
    internal bool ScreenPointerReceiverRestriction(
        Place receiver, string receiverText, StrongTypeModel.TypeRestriction sourceRestriction,
        string senderText, bool addressSender)
    {
        var receiverRestriction = StrongTypeModel.PointerRestriction(receiver.Item);
        if (receiverRestriction.IsRestricted
            && !StrongTypeModel.SameRestriction(receiverRestriction, sourceRestriction))
        {
            RejectRestriction(senderText,
                $"the receiving data-pointer is restricted to type '{receiverRestriction}', so the sender shall "
                + "be NULL or a data-pointer restricted to the same type (ISO §14.9.39.3 SR19)");
            return false;
        }
        if (sourceRestriction.IsRestricted && !receiverRestriction.IsRestricted)
        {
            RejectRestriction(receiverText,
                $"{(addressSender ? $"ADDRESS OF '{senderText}'" : $"'{senderText}'")} is a RESTRICTED "
                + $"data-pointer of type '{sourceRestriction}' "
                + (addressSender
                   ? "(ISO §8.4.3.11.4 GR2 — the operand is a strongly-typed group item or a restricted pointer), "
                   : "(ISO §13.18.60.4 GR23 — its USAGE POINTER clause specifies a type-name-1), ")
                + "so the receiver shall be a data-pointer restricted to the same type "
                + "(ISO §14.9.39.3 SR19, third sentence)");
            return false;
        }
        return true;
    }

    /// <summary>§14.9.39.3 SR19 over ONE <c>ADDRESS OF data-name-1</c> receiver of a Format-7 statement — BOTH of the
    /// rule's sentences that name data-name-1, written once (kb/Work PB665; the identifier-5 receivers' pair is
    /// <see cref="ScreenPointerReceiverRestriction"/>). Asked PER RECEIVER, because the restriction is data-name-1's,
    /// not the statement's — a mixed list may hold a restricted receiver beside an unrestricted one — and against
    /// identifier-6 in EITHER spelling (<paramref name="senderRestriction"/> is §13.18.60.4 GR23's for a pointer
    /// item, §8.4.3.11.4 GR2's for an <c>ADDRESS OF</c>). The caller never asks it for TO NULL, which SR19 admits.
    /// <list type="number">
    ///   <item>"If data-name-1 is a strongly-typed group item or a restricted pointer, identifier-6 shall reference a
    ///         data-pointer restricted to the type of data-name-1."</item>
    ///   <item>"If identifier-6 references a restricted data-pointer, either identifier-5 shall reference a
    ///         data-pointer restricted to the same type or data-name-1 shall be a typed item of the type to which
    ///         identifier-6 is restricted." With data-name-1 as the receiver there IS no identifier-5, so the second
    ///         leg is the only one: an UNTYPED based item over a restricted pointer would otherwise read the typed
    ///         storage through a window with no type, which is exactly the Annex D.9.2.2 guarantee a restricted
    ///         data-pointer exists to give. "A typed item of the type" is data-name-1's TYPE clause — for a
    ///         restricted pointer too, whose own type is the TYPEDEF it was declared with.</item>
    /// </list></summary>
    private bool ScreenBasedReceiverRestriction(DataItem based, StrongTypeModel.TypeRestriction senderRestriction,
        string senderText, bool addressSender)
    {
        // "The type of data-name-1" — the SAME derivation §8.4.3.11.4 GR2 spends on "the type of identifier-1", because
        // the address SET ADDRESS OF stores is exactly what ADDRESS OF data-name-1 would yield: a strongly-typed
        // group's type, or, for a restricted pointer, the pointer item's OWN type (the TYPEDEF it was declared with),
        // never the type its value addresses (kb/Work PB1408).
        var needed = StrongTypeModel.AddressOfRestriction(based);
        if (needed.IsRestricted && !StrongTypeModel.SameRestriction(needed, senderRestriction))
        {
            RejectRestriction(senderText,
                $"the receiver of SET ADDRESS OF is restricted to type '{needed}', so the sender shall "
                + "reference a data-pointer restricted to the same type (ISO §14.9.39.3 SR19)");
            return false;
        }
        if (senderRestriction.IsRestricted
            && !StrongTypeModel.SameRestriction(StrongTypeModel.TypedItemType(based), senderRestriction))
        {
            RejectRestriction(senderText,
                $"the sender{(addressSender ? " (ADDRESS OF a strongly-typed group — §8.4.3.11.4 GR2)" : "")} is a "
                + $"data-pointer restricted to type '{senderRestriction}', and the receiving based "
                + $"item '{based.CobolName}' is "
                + (StrongTypeModel.TypedItemType(based).IsRestricted
                    ? $"a typed item of type '{StrongTypeModel.TypedItemType(based)}'" : "untyped")
                + " — SET ADDRESS OF has no identifier-5, so data-name-1 shall be a typed item of the type to which "
                + "the sender is restricted (ISO §14.9.39.3 SR19, third sentence)");
            return false;
        }
        return true;
    }

    /// <summary>Report a restricted-data-pointer type-safety violation. The 0869 pointer band, where PtrBinder
    /// already reports every other §14.9.39 / §14.9.3 operand rule (SR1, SR2, SR3, SR17, SR18, SR23) — these are
    /// operand rules of the same statements, so they belong to the same band rather than to new codes.</summary>
    private void RejectRestriction(string operandText, string what) =>
        ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape, $"'{operandText}': {what}. Annex D.9.2.2: a restricted data-pointer "
            + "\"shall contain only the predefined address NULL or the address of a data item of the specified "
            + "type\"");

    /// <summary>Resolve an <c>ADDRESS OF identifier</c> operand (ISO §8.4.3.11): a BASED item's value is its
    /// implicit pointer (§8.6.5); an ordinary record must have been storage-forced onto a cell by the data
    /// pass (or be an EXTERNAL record — already cell-backed). A qualified operand resolves through the ONE
    /// §8.4.2.2 qualification machinery; a subscripted operand addresses THE OCCURRENCE (GR1) — the resolver
    /// returns its in-class occurrence displacement (<c>ReferenceResolver.ResolveForAddressOf</c>). Un-forcible
    /// shapes (a class rejected for a national or pointer-class leaf, an OCCURS-resident anchor, a
    /// carrier-resident LINKAGE formal) and reference-modified operands stage LOUD — never a pointer to the
    /// wrong storage, and the diagnostic INTERPOLATES the class's own RejectReason rather than listing the
    /// reasons it might have been (kb/Work PB231). A COMP/float/INDEX leaf is NOT un-forcible any more:
    /// every numeric byte form rides the cell (kb/Work PB164), and neither is a USAGE BIT leaf: the
    /// §8.5.1.6.3 packing rides it too (kb/Work PB231).
    /// <para>⛔ THE ONE BINDER FOR EVERY <c>dataAddressIdentifier</c> SURFACE (kb/Work PB239) — the SET Format-7
    /// sender and the CALL argument (§14.9.4.3 SR3/SR4) both reach it, so the operand rules and the
    /// cell-backing check are stated once.</para></summary>
    internal BoundAddressOf? BindDataAddress(Core.DataAddressIdentifierContext dai) => PtrBindAddressOf(dai.dataReference());

    /// <summary>⛔ THE ONE BINDER OF AN <c>addressIdentifier</c> OPERAND — ISO §8.4.3.1.2 identifier FORMAT 9,
    /// either arm (kb/Work PB1021). The data arm goes through <see cref="BindDataAddress"/> (the cell-backing check,
    /// §8.4.3.11.3), the program arm through <c>SetBinder.BindProgramAddressOperand</c> (§8.4.3.13.3 SR1–SR3), the
    /// same binders the SET senders use — so every surface that takes the identifier (the CALL argument, the INVOKE
    /// argument, the relation operand) states its operand rules once. <paramref name="site"/> names the surface in
    /// a diagnostic. Null having reported.</summary>
    internal BoundAddressOperand? BindAddressIdentifier(Core.AddressIdentifierContext ai, string site)
    {
        if (ai.dataAddressIdentifier() is { } dai)
            return BindDataAddress(dai) is { } addr ? new BoundAddressOperand(addr, null) : null;
        return host.Set.BindProgramAddressOperand(ai.programAddressIdentifier(), site) is { } pa
            ? new BoundAddressOperand(null, pa)
            : null;
    }

    /// <summary>§14.8.2's verdict for ONE address-identifier ARGUMENT against its formal parameter — the CALL
    /// argument (kb/Work PB239) and the INVOKE argument (kb/Work PB1021) alike, because §14.9.23.3 SR5 c) sends
    /// INVOKE to the same "14.8.2, Parameters" CALL reads. Both passing regimes reach the same law: BY REFERENCE is
    /// §14.8.2.3.2's class-pointer paragraph — "If either the argument or the formal parameter is of class
    /// pointer, the corresponding formal parameter or argument shall be of class pointer and the corresponding
    /// items shall be of the same category. If either is a restricted pointer, both shall be restricted and of the
    /// same type" — and BY CONTENT / BY VALUE is §14.8.2.3.3's "as if a SET statement were performed … with the
    /// argument as the sending operand", whose Format 7 / Format 9 rules (§14.9.39.3 SR17/SR19, SR21/SR22) demand
    /// the same category and the same restriction. The category is §8.4.3.11.4 GR1 (data-pointer) or §8.4.3.13.4
    /// GR1 (program-pointer); the restriction is §8.4.3.11.4 GR2 (the type of a strongly-typed identifier-1) or
    /// §8.4.3.13.4 GR3 (program-prototype-name-1). Null when conformant.</summary>
    internal string? AddressConformanceReason(DataItem formal, BoundAddressOf? data, BoundProgramAddress? program)
    {
        var fcat = formal.Pic?.Category;
        if (data is { } da)
        {
            if (fcat is not PicCategory.Pointer)
                return "a data-address-identifier is a data item of category data-pointer (ISO §8.4.3.11.4 GR1), "
                    + "so the formal parameter shall be of category data-pointer (ISO §14.8.2.3.2 / §14.8.2.3.3)";
            var argR = StrongTypeModel.AddressOfRestriction(da.Item);
            var formalR = StrongTypeModel.PointerRestriction(formal);
            return (argR.IsRestricted || formalR.IsRestricted) && !StrongTypeModel.SameRestriction(argR, formalR)
                ? $"one is a RESTRICTED data-pointer and the other is not restricted to the same type (argument: "
                  + $"{argR}; formal: {formalR}) — if either is a restricted pointer, both shall be restricted and "
                  + "of the same type (ISO §14.8.2.3.2; §8.4.3.11.4 GR2)"
                : null;
        }
        if (program is { } pa)
        {
            if (fcat is not PicCategory.ProgramPointer)
                return "a program-address-identifier is a data item of category program-pointer (ISO §8.4.3.13.4 "
                    + "GR1), so the formal parameter shall be of category program-pointer (ISO §14.8.2.3.2 / §14.8.2.3.3)";
            string? formalProto = formal.Pic?.RestrictedPrototypeName;
            if (formalProto is null && pa.Prototype is null) return null;
            return formalProto is null || pa.Prototype is null
                   || !PrototypeSignatures.Same(host.ProgramSignatureOf(formalProto), host.ProgramSignatureOf(pa.Prototype))
                ? $"one is a RESTRICTED program-pointer and the other is not restricted to a program-prototype of "
                  + $"the same signature (argument: {pa.Prototype ?? "unrestricted"}; formal: "
                  + $"{formalProto ?? "unrestricted"}) — ISO §14.8.2.3.2; §8.4.3.13.4 GR3"
                : null;
        }
        return null;
    }

    /// <summary>The operand half of <see cref="BindDataAddress"/>: resolve (ISO §8.4.3.11.3 SR1's sections, the
    /// subscripts, a reference modifier), screen the resolved operand once (<see cref="AddressOfOperandScreen"/> — SR1
    /// second sentence, SR2, SR3, SR4, SR6) and check it is cell-backed.</summary>
    private BoundAddressOf? PtrBindAddressOf(Core.DataReferenceContext addrRef)
    {
        if (ctx.Refs.ResolveForAddressOf(addrRef) is not { } r)
        {
            // The resolver's own §8.4.2.3.3 screen already named the rule (kb/Work PB681) — once per reference.
            if (ctx.Refs.WasDiagnosed(addrRef)) return null;
            ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                $"ADDRESS OF '{DataBinder.WrittenText(addrRef)}': identifier-1 shall reference a data item defined in the file, "
                + "working-storage, local-storage or linkage section (ISO §8.4.3.11.3 SR1) — the operand names "
                + "none (an undeclared name, a SCREEN SECTION or REPORT SECTION entry, a constant-name or a special "
                + "register) or is mis-subscripted (ISO §8.4.2.3)");
            return null;
        }
        if (!_addressScreen.Admit(addrRef, r)) return null;
        var item = r.Item;
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        // The CLASS says whether it is cell-backed (RedefinesClass.IsCellBacked — set by the one forcer for all three
        // surfaces), never this unit's own tables: a contained program's ADDRESS OF a GLOBAL name addresses the
        // CONTAINER's cell, which the container's tables list and this unit's do not (kb/Work PB1009).
        bool cellBacked = root.Class is { Tier: RedefinesTier.StringCanonical, IsCellBacked: true };
        if (!cellBacked)
        {
            // ⛔ NAME THE ACTUAL REASON. The cell forcer already recorded WHY it refused — the residue clause
            // ByteWindowResidueOf produced, carried on the class as its RejectReason (kb/Work PB231) — and
            // this site used to print a HAND-WRITTEN LIST of every reason it might have been instead, so the
            // one surface that could not say what was wrong was the one the user reads. Its two siblings
            // (CallMakeExternal, PtrBindBasedAndAddressables) had always interpolated it; this was the third
            // arm of that dispatch. The list survives only as the fallback for the shapes that never reach
            // the forcer at all: an OCCURS-resident anchor. (A carrier-resident LINKAGE formal no longer reaches
            // here — an addressed formal is never resident, kb/Work PB1019.)
            string why = root.Class is { Tier: RedefinesTier.Rejected, RejectReason: { } reason }
                ? reason
                : "an OCCURS-resident anchor — named increment residue";
            ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                $"ADDRESS OF '{DataBinder.WrittenText(addrRef)}': the operand's record could not be placed on addressable "
                + $"cell storage ({why}; ISO §8.4.3.11)");
            return null;
        }
        return new BoundAddressOf(item, r.OccursDisplacement, AddressRefModOf(r));
    }

    /// <summary>The leftmost-position facts of a reference-modified operand (<see cref="AddressRefMod"/>), or null when
    /// none is written. §8.4.3.11.4 GR1 makes the address that of identifier-1, which with a reference modifier is
    /// the unique data item §8.4.3.3.4 GR5 creates — so it starts at the leftmost position, in the unit
    /// <see cref="RefModPlace.PositionBits"/> says a position is. A bit item also starts at some bit of its first
    /// byte; SR4 b)'s proof (<see cref="AddressOfOperandScreen"/>) has already established that the slice itself is
    /// byte-aligned, so (lead + (leftmost − 1)) divides exactly.</summary>
    private static AddressRefMod? AddressRefModOf(ReferenceResolver.AddressOfOperand r)
    {
        if (r.RefMod is not { } spec) return null;
        var item = r.Item;
        // The size a reference modifier is range-checked against is the positions of identifier-1's DESCRIPTION; an
        // ANY LENGTH item's is a run-time fact (the lower bound alone is checked then).
        int? positions = item.IsAnyLength ? null : RefModPlace.PositionCount(item);
        int lead = r.SubscriptedPlace is { } bit && ParameterConformance.BitStartOf(bit).Bits is { } start
            ? (int)(start % BitLayout.BitsPerCharacter) : 0;
        return new AddressRefMod(spec, positions, RefModPlace.PositionBits(item), lead, r.CurrentExtent);
    }

    /// <summary>Bind ALLOCATE (ISO §14.9.3, both formats). The INITIALIZED based form lowers per GR7 to the
    /// allocation followed by EXACTLY the spec's <c>INITIALIZE data-name-1 WITH FILLER ALL TO VALUE THEN TO
    /// DEFAULT</c> expansion (the ONE INITIALIZE mechanism, <see cref="InitializeBinder.BindAllocateInitialized"/>),
    /// carried as a <see cref="BoundSequence"/>.</summary>
    public BoundStatement BindAllocate(Core.AllocateStatementContext al)
    {
        // ALLOCATE (§14.9.3) is a COBOL-2002 INTRODUCTION gate, now gated on RECOGNITION by the
        // VersionConformancePass parse-arm (VisitAllocateStatement, Step 14h.2) — so a below-2002 ALLOCATE is an
        // edition violation even when its RETURNING fails to resolve (SR3/0869), which a bound-arm gate lost when
        // binding errored to a BoundNop before a BoundAllocate was produced (the DEVLOG-724 CI-red finding).
        var drefs = al.dataReference();
        Place? returning = null;
        if (al.RETURNING() is not null)
        {
            if (PtrResolvePointer(drefs[^1], "ALLOCATE RETURNING (ISO §14.9.3.3 SR3 — category data-pointer)", receiving: true) is not { } rp)
                return BoundRejected.Reported(ctx.Edition);
            returning = rp;
        }

        if (al.CHARACTERS() is not null)
        {
            // Form 1: ALLOCATE arithmetic-expression CHARACTERS [INITIALIZED] RETURNING pointer.
            if (returning is null)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    "ALLOCATE … CHARACTERS requires the RETURNING phrase (ISO §14.9.3.3 SR2 — without a based "
                    + "item there is no other way to address the storage)");
            }
            // §14.9.3.3 SR4, the CHARACTERS arm: "If data-name-2 references a restricted data-pointer,
            // data-name-1 shall be specified …" — and in Format 1 there IS no data-name-1, so a restricted
            // RETURNING can never be satisfied here. Screening it in this branch too is what keeps the rule from
            // being half-enforced: Form 1 and Form 2 are separate code paths.
            if (StrongTypeModel.PointerRestriction(returning.Item) is { IsRestricted: true } charsRestriction)
            {
                RejectRestriction(DataBinder.WrittenText(drefs[^1]),
                    $"the RETURNING data item is a data-pointer restricted to type '{charsRestriction}', which "
                    + "ALLOCATE … CHARACTERS cannot satisfy — it specifies no data-name-1 to supply that type "
                    + "(ISO §14.9.3.3 SR4)");
                return BoundRejected.Reported(ctx.Edition);
            }
            return new BoundAllocate(null, host.Expr.BindExpr(al.arithmeticExpression()), al.INITIALIZED() is not null, returning);
        }

        // Form 2: ALLOCATE based-item [INITIALIZED] [RETURNING pointer].
        var basedRef = drefs[0];
        if (PtrResolveBased(basedRef) is not { } based) return BoundRejected.Reported(ctx.Edition);
        // ⛔ §14.9.3.3 SR4 AND SR5 — the restricted-data-pointer type-safety pair, BOTH directions, and neither
        // existed before kb/Work PB153. Measured on this tree beforehand: `01 T TYPEDEF STRONG. 02 F PIC 9(4).
        // 01 V TYPE T BASED. 01 P USAGE POINTER. ALLOCATE V RETURNING P.` bound clean, silently defeating the
        // Annex D.9.2.2 type-safety guarantee this whole model exists to provide.
        //   SR5: "If both data-name-1 and data-name-2 are specified and data-name-1 references a strongly-typed
        //         group item, the data item referenced by data-name-2 shall be restricted to the type of
        //         data-name-1."
        //   SR4: "If data-name-2 references a restricted data-pointer, data-name-1 shall be specified and shall
        //         reference a typed data item, and the data item referenced by data-name-2 shall be restricted to
        //         the type of data-name-1."  — the converse, which catches a restricted RETURNING over an
        //         untyped or absent based item.
        if (returning is { } ret)
        {
            var returningRestriction = StrongTypeModel.PointerRestriction(ret.Item);
            // ⛔ SR4 AND SR5 ASK ABOUT DIFFERENT THINGS, and conflating them rejects legal source. SR5's
            // antecedent is "data-name-1 references a STRONGLY-TYPED GROUP ITEM"; SR4's is "data-name-2
            // references a restricted data-pointer", and its requirement on data-name-1 is only that it
            // "reference a TYPED DATA ITEM" — which a WEAK typedef satisfies. So the two tests take different
            // type accessors: StrongGroupType for SR5, TypedItemType (the plain TYPE anchor) for SR4.
            var strongType = StrongTypeModel.StrongGroupType(based);
            var basedType = StrongTypeModel.TypedItemType(based);
            if (strongType.IsRestricted && !StrongTypeModel.SameRestriction(strongType, returningRestriction))
            {
                RejectRestriction(DataBinder.WrittenText(drefs[^1]),
                    $"'{DataBinder.WrittenText(basedRef)}' is a strongly-typed group item of type '{strongType}', so the "
                    + "RETURNING data item shall be a data-pointer restricted to that type (ISO §14.9.3.3 SR5)");
                return BoundRejected.Reported(ctx.Edition);
            }
            if (returningRestriction.IsRestricted
                && !StrongTypeModel.SameRestriction(returningRestriction, basedType))
            {
                RejectRestriction(DataBinder.WrittenText(drefs[^1]),
                    $"the RETURNING data item is a data-pointer restricted to type '{returningRestriction}', so "
                    + $"'{DataBinder.WrittenText(basedRef)}' shall reference a typed data item of that type — it is "
                    + $"{(basedType.IsRestricted ? $"of type '{basedType}'" : "untyped")} (ISO §14.9.3.3 SR4)");
                return BoundRejected.Reported(ctx.Edition);
            }
        }
        var alloc = new BoundAllocate(based, null, al.INITIALIZED() is not null, returning);
        if (al.INITIALIZED() is null) return alloc;
        // GR7: "the allocated storage is initialized as if an INITIALIZE data-name-1 WITH FILLER ALL TO VALUE
        // THEN TO DEFAULT statement were executed" — the lowering IS that statement's bind-time expansion,
        // sequenced AFTER the allocation so each store windows the cell the implicit pointer now addresses
        // (GR4a). The GR5 not-available leg (no storage to initialize) is unreachable in this managed model —
        // a form-2 request is the template width (> 0), and CobolPtr.Allocate always satisfies a positive size.
        return new BoundSequence([alloc, host.Init.BindAllocateInitialized(basedRef)]);
    }

    /// <summary>Bind FREE (ISO §14.9.15 SR1 — every operand shall be category data-pointer; the vendor
    /// <c>FREE based-item</c> form is rejected, never silently mis-freed).</summary>
    public BoundStatement BindFree(Core.FreeStatementContext fr)
    {
        // FREE (§14.9.15) is a COBOL-2002 introduction; edition gate moved to VersionConformancePass (Step 14b),
        // firing on the self-identifying BoundFree node.
        // ISO §14.9.15.4 GR2 — one implicit FREE statement per data-name-1, in source order, and the resumption
        // point after an exception is "the next implicit FREE statement, if any" for BOTH the nonfatal and the
        // fatal arm ("If the exception condition is fatal and the applicable exception processing statements do
        // not result in abnormal run unit termination, processing resumes at the next implicit FREE statement").
        // The nonfatal EC-STORAGE-NOT-ALLOC arm already dispatches per operand at the operand's own site; the
        // FATAL arm unwinds to the statement's EC guard, which is a per-operand site only because of the series.
        var members = new List<BoundStatement>();
        foreach (var dref in fr.dataReference())
        {
            if (PtrResolvePointer(dref, "a FREE operand (ISO §14.9.15.3 SR1 — data-pointers only)", receiving: true) is not { } p)
                return BoundRejected.Reported(ctx.Edition);
            members.Add(new BoundFree([p]));
        }
        return BoundImplicitSeries.Of(members);
    }

    /// <summary>The SET UP/DOWN BY pointer arm (ISO §14.9.39.2 Format 10): every receiving operand shall be of
    /// category data-pointer (§14.9.39.3 SR23). GR19's non-integer-amount rule is a VALUE rule, realized EXACTLY
    /// at runtime (<c>CobolPtr.UpByScaled</c> → EC-SIZE-ADDRESS fatal; 2.0 moves by 2).
    /// <para>⛔ IT NO LONGER SNIFFS A FORMAT, AND THE SR23 SCREEN NOW COVERS EVERY OPERAND INCLUDING THE FIRST
    /// (kb/Work PB449). This used to peek <c>drefs[0]</c> and return null for a non-pointer, so the SAME two
    /// operands gave a correct SR23 diagnostic when the pointer was written first and a run-time crash when it
    /// was written second — the format decided by source order, which is a variable in no rule of §14.9.39. The
    /// selection is now <see cref="SetFormatSelection"/>'s, over the whole receiving list, and this method is
    /// entered only once Format 10 has been chosen; the screen it keeps is the RULE, run uniformly.</para>
    /// <para>The screen resolves through <see cref="PtrResolvePointer"/> (the committing form), which is also
    /// what kb/Work PB221 asked for: a probe's Place is unscreened and must never enter the bound tree, and
    /// committing the FIRST operand's probe made <c>SET P(XE) Q(XE) UP BY 4</c> diagnose COBOLNET0844 for Q and
    /// not for P — one statement, one rule, two verdicts.</para></summary>
    public BoundStatement BindSetUpDown(Core.SetIndexStatementContext ud)
    {
        var drefs = ud.dataReference();
        if (drefs.Length == 0)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "SET … UP BY / DOWN BY with no receiving operand (ISO §14.9.39.2)");
        // SET pointer UP/DOWN BY (§14.9.39 Format 10) is a COBOL-2002 introduction; edition gate moved to
        // VersionConformancePass (Step 14b), firing on the self-identifying BoundSetPointerUpDown node.
        var targets = new List<Place>(drefs.Length);
        foreach (var dref in drefs)
        {
            if (SetIndexNameOperand(dref)) return BoundRejected.Reported(ctx.Edition);
            if (PtrResolvePointer(dref, "a SET UP/DOWN BY receiver mixed with data-pointers (ISO §14.9.39.3 SR23)", receiving: true) is not { } p)
                return BoundRejected.Reported(ctx.Edition);
            // §14.9.39.3 SR24 — "Identifier-9 shall not be a data-pointer restricted to a type described with the
            // STRONG phrase" (kb/Work PB816). Pointer arithmetic leaves the type's storage, which is exactly what
            // such a restriction exists to prevent (Annex D.9.2.2); asked of EVERY receiver, like SR23 above.
            if (StrongTypeModel.IsRestrictedToStrongType(p.Item))
            {
                RejectRestriction(DataBinder.WrittenText(dref),
                    $"identifier-9 is a data-pointer restricted to type '{StrongTypeModel.PointerRestriction(p.Item)}', "
                    + "which is described with the STRONG phrase, so SET UP BY / DOWN BY shall not change it "
                    + "(ISO §14.9.39.3 SR24)");
                return BoundRejected.Reported(ctx.Edition);
            }
            targets.Add(p);
        }
        var amount = host.Expr.BindIndexWindowExpr(ud.arithmeticExpression());   // SET (pointer form) is an r7 window (kb/Work R29)
        return new BoundSetPointerUpDown(targets, amount, ud.DOWN() is not null);
    }

    /// <summary>An INDEX-NAME among Format 10's receiving operands is a CATEGORY error, not an undefined name
    /// (kb/Work PB388): <c>ctx.Refs.Resolve</c> answers for a DATA ITEM only and would report COBOLNET1639 on a
    /// name an INDEXED BY phrase legally declares. §13.18.38.3 r7 closes the list of contexts that may reference
    /// an index-name and SET Format 1/2 is one of them — so the statement is still refused, for the real
    /// reason.</summary>
    private bool SetIndexNameOperand(Core.DataReferenceContext dref)
    {
        if (host.Expr.IndexFieldOf(dref) is null) return false;
        ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
            $"SET '{DataBinder.WrittenText(dref)}': an index-name cannot be a receiving operand of a data-pointer SET — "
            + "identifier-9 shall be of category data-pointer (ISO §14.9.39.2 Format 10, §14.9.39.3 SR23). An "
            + "index-name operand belongs to Format 2, whose receiving operand is index-name-3");
        return true;
    }

    /// <summary>Resolve a reference that must be a USAGE POINTER item (the 0869 pointer band). A RECEIVING operand
    /// (SET's identifier-5, ALLOCATE RETURNING, FREE's operand, SET UP/DOWN's receiver) resolves through the one
    /// receiving chokepoint and a sending one through the sending entry — the caller states which (kb/Work PB881).</summary>
    /// <para>Null means REPORTED (kb/Work PB1030): a reference that did not resolve carries the resolver's or the
    /// receiving chokepoint's diagnostic, never a second "shall be a USAGE POINTER" one naming the wrong rule.</para>
    private Place? PtrResolvePointer(Core.DataReferenceContext dref, string what, bool receiving)
    {
        if ((receiving ? host.Expr.ResolveReceiving(dref) : host.Expr.ResolveSending(dref).PlaceOrReported(ctx.Edition))
            is not { } p) return null;
        if (p.Item.Pic?.Category is PicCategory.Pointer) return p;
        ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
            $"'{DataBinder.WrittenText(dref)}': {what} shall be a USAGE POINTER data item");
        return null;
    }

    /// <summary>Resolve a reference that must be a BASED 01/77 item (SR18 / §14.9.3 SR1).
    /// <para>⛔ THE LOOKUP IS THE RESOLVER'S, AND IT IS SCOPE-AWARE (kb/Work PB467). This used to read
    /// <c>ctx.Data.ByName</c> — the UNIT-WIDE multimap, which has no notion of the reference's scope — and the
    /// SR18 verdict then went wrong in BOTH directions inside a method definition: a legal method-local
    /// <c>01 MB BASED</c> was refused, and a method-local NON-based item that legally shadows an object-level
    /// BASED one was accepted for rebasing (§11.7.4 GR5 — "the use of that word in this method refers to the
    /// declaration in this method. The declaration in the containing object definition is inaccessible to this
    /// method"). It is <see cref="ReferenceResolver.DeclarationOf"/> now, so every scope the resolver learns
    /// reaches this screen for free; a special-case "also look in <c>scope.Method.ByName</c>" written here would
    /// have been <see cref="Model.SymbolTable.TryResolve"/>'s precedence copied into a second place.</para>
    /// <para>The BARE-NAME guard stays, and it is SR18's rule rather than the resolver's: a based entry is
    /// level 01 or 77 (§13.16.3 SR16), so no qualified, subscripted or reference-modified spelling of
    /// data-name-1 is legal here and none is turned away by refusing them.</para></summary>
    private DataItem? PtrResolveBased(Core.DataReferenceContext dref)
    {
        DataItem? item = dref.ChildCount == 1 ? ctx.Refs.DeclarationOf(dref, dref.GetText()) : null;
        if (item is { IsBased: true }) return item;
        // A name no declaration in scope carries — or one several carry — is an §8.4.2.1/§8.4.2.2.1 failure the
        // resolver has already stated precisely. Adding "shall be a BASED level-01/77 item" on top of it would
        // send the reader hunting a BASED clause for a name that is not declared at all (the PB457 shape).
        if (item is null && ctx.Refs.WasDiagnosed(dref)) return null;
        ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
            $"'{DataBinder.WrittenText(dref)}': the operand shall be a BASED level-01/77 item (ISO §14.9.39.3 SR18 / "
            + "§14.9.3.3 SR1 — rebasing or allocating a non-BASED item is not ISO COBOL)");
        return null;
    }
}
