// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>The KIND of subject a VALUE clause is written on. ISO §13.18.63.3's syntax rules do not address one
/// subject: they address three, and which one a clause has decides which sentences of SR4 / SR5 / SR10 bind it.
/// <list type="bullet">
///   <item><see cref="Elementary"/> — the VALUE clause of an elementary data item (§13.18.63 formats 1, 2 and 4).
///     SR4/SR5/SR10 <b>sentence 2</b> is its size rule: "… in the VALUE clause of an elementary item shall not
///     exceed the size indicated by an explicit PICTURE clause".</item>
///   <item><see cref="Group"/> — a group-level VALUE (§13.18.63.3 SR13 sentence 1). SR4/SR5/SR10 <b>sentence 3</b>
///     is its size rule: "… shall not exceed the size of the group item".</item>
///   <item><see cref="ConditionName"/> — a Format-3 (condition-name) VALUE clause. It has <b>NO</b> size rule; see
///     the remarks on <see cref="ForConditionName"/>.</item>
/// </list></summary>
internal enum ValueSubjectKind
{
    /// <summary>An elementary data item carrying its own VALUE clause (§13.18.63 formats 1, 2, 4).</summary>
    Elementary,

    /// <summary>A group item carrying a group-level VALUE clause (§13.18.63.3 SR13).</summary>
    Group,

    /// <summary>A condition-name — the subject of a Format-3 entry (§13.18.63.3 SR33).</summary>
    ConditionName,
}

/// <summary>⛔ THE SUBJECT OF A VALUE CLAUSE, as ISO §13.18.63.3 addresses it — the ONE currency
/// <see cref="DataBinder.ScreenValueLiteral"/> and <see cref="DataBinder.ValidateValueCategory"/> take, so that
/// "what size do the SR4/SR5/SR10 size sentences measure this literal against" is decided ONCE PER SUBJECT KIND,
/// here, and never at a call site.
///
/// <para><b>Why a descriptor rather than the two parameters it replaced.</b> The screen used to take an
/// <c>int? sizePositions</c> and a <c>bool groupSubject</c>. Those are not two facts; they are one fact — the
/// subject's kind — spelled twice, and a caller was free to spell a combination the standard does not have. It
/// did: the three level-88 call sites in <c>BindCondition</c> each computed a size from the CONDITIONAL
/// VARIABLE's picture and handed it to the screen, so `01 XV PIC X. 88 XC VALUE "cd".` was rejected
/// (COBOLNET1740 / COBOLNET0898) where the standard makes it a legal, permanently-false condition
/// (kb/Work PB598). A new subject kind now has to add a factory below and state its size rule with its citation;
/// it cannot inherit one by accident.</para></summary>
/// <param name="Kind">Which of §13.18.63.3's three subjects this is.</param>
/// <param name="SizePositions">The number of positions the SR4/SR5/SR10 size sentences measure a literal against,
/// or <see langword="null"/> when the subject indicates NO size. Never computed by a caller — see the factories.</param>
internal readonly record struct ValueSubject(ValueSubjectKind Kind, int? SizePositions)
{
    /// <summary>True when the subject is a GROUP carrying a group-level VALUE (§13.18.63.3 SR13 sentence 1). It
    /// withholds ONE thing from the screen: the SR4-sentence-1 vendor leniency that stores a numeric literal's
    /// digits on an alphanumeric subject. That leniency is a statement about a store the compiler can perform —
    /// for a group the store is §13.18.63.4 GR5's area deposit, defined over the operand's CHARACTERS, and a
    /// numeric literal has none (measured: <c>01 GN VALUE 1234. 05 N1 PIC X(2). 05 N2 PIC X(2).</c> seeded
    /// SPACES). A warning plus the wrong area is worse than the rejection SR13 asks for, so the group arm is an
    /// error on both dialect axes (kb/Work PB206).</summary>
    public bool IsGroup => Kind is ValueSubjectKind.Group;

    /// <summary>An ELEMENTARY item's VALUE clause (§13.18.63 formats 1, 2 and 4). Its size is "the size indicated
    /// by an explicit PICTURE clause" (§13.18.63.3 SR4/SR5/SR10 sentence 2).
    /// <para>⛔ The <see langword="null"/> arm is not a defensive nicety: for a DYNAMIC LENGTH item,
    /// §13.18.19.3 SR1 — "The character-string specified in that PICTURE clause shall be one instance of the
    /// picture symbol 'N', or 'X'" — and §13.18.19.4 GR1 — "The picture symbol determines the class." That one
    /// symbol indicates a CLASS, never a size: the maximum is the LIMIT phrase's, or implementor-defined (GR2),
    /// never the picture's one position. ANY LENGTH (§13.18.2) is the same shape.
    /// <c>01 UN PIC N DYNAMIC LENGTH VALUE N"SEED".</c> was rejected as exceeding "the item's 1 national
    /// positions" while its alphanumeric twin was accepted and ran — the [[two_arm_dispatch]] shape, one arm
    /// sized and one not (kb/Work PB206).</para>
    /// <para>⛔ <paramref name="implicitPicture"/> is the same sentence's other word: the bound is the size an
    /// EXPLICIT PICTURE clause indicates, and a §13.16.3 SR9 / §13.15.3 SR14 VALUE-implied PICTURE is not one.
    /// That matters only when the implied clause reaches an entry whose OWN literal differs from the one that
    /// implied it — a TYPE subject over a VALUE-implied template (§13.18.57.4 GR3: "that implicit PICTURE clause
    /// becomes part of the description of the subject of the entry"; kb/Work PB1300) — and there no syntax rule
    /// sizes the literal (⚠ DETERMINATION, the word "explicit" read as written). Every caller states the
    /// provenance; none defaults it.</para></summary>
    public static ValueSubject ForElementary(PicInfo pic, bool isDynamicLength, bool isAnyLength, bool implicitPicture) =>
        new(ValueSubjectKind.Elementary, isDynamicLength || isAnyLength || implicitPicture ? null : pic.Length);

    /// <summary>A GROUP item's group-level VALUE (§13.18.63.3 SR13 sentence 1). Its size is "the size of the group
    /// item" (SR4/SR5/SR10 sentence 3) — the group's own positions: §8.5.2.1 gives a group a class and a category,
    /// and §13.18.29.4 GR1b/GR2b give a bit / national group an as-if PICTURE whose length is its extent. A group
    /// whose subordinate is dynamic-length is a variable-length group, which §13.18.63.3 SR1 already bars from
    /// being a VALUE subject at all, so the size is never absent here.</summary>
    public static ValueSubject ForGroup(int sizePositions) =>
        new(ValueSubjectKind.Group, sizePositions);

    /// <summary>⛔ A CONDITION-NAME — the subject of a Format-3 VALUE clause (§13.18.63.3 SR33: "Formats 3 and 5
    /// may be specified only when the level-number of the subject of the entry is 88"). It indicates NO SIZE, so
    /// the SR4/SR5/SR10 size sentences do not reach it and the size arms of the screen stay dark.
    ///
    /// <para><b>The derivation</b> (kb/Work PB598). Each of the three size sentences names the subject it bounds,
    /// and names only two: "Alphanumeric literals in the VALUE clause of <b>an elementary item</b> shall not exceed
    /// the size indicated by an <b>explicit</b> PICTURE clause. Alphanumeric literals in the VALUE clause of an
    /// alphanumeric <b>group item</b> shall not exceed the size of the group item" (§13.18.63.3 SR4; SR5 and SR10
    /// are the same pair over national and boolean positions). A Format-3 entry is
    /// <c>88 condition-name-1 value-clause .</c> (§13.16.2) — no PICTURE clause is writable in it, so no EXPLICIT
    /// picture indicates a size — and its subject is a condition-name (§13.16.4 GR3), an entry for which there is
    /// "no true concept of level" (§8.5.1.3.2 item 3), hence neither an elementary item nor a group item
    /// (§8.5.1.3.1 defines elementary items as a record's undivided subdivisions). §13.18.63.4 GR19 gives a
    /// condition-name its conditional variable's characteristics <b>implicitly</b> — which is precisely what the
    /// word "explicit" excludes — so the CLASS half of SR4/SR5 (ALL FORMATS) and of SR10 (Format 1, carried in by
    /// SR24's "Syntax rules 10 and 17 above apply") still binds, and only the SIZE half does not.</para>
    ///
    /// <para><b>And the standard says what an oversize one MEANS</b>, which a size rule would make dead text: the
    /// condition-name condition compares "the same as those specified for relation conditions" (§8.8.4.5.3 item 2),
    /// so a literal longer than the conditional variable simply never compares equal — a permanently-false
    /// condition; and <c>SET condition-name TO TRUE</c> places the literal "according to the rules for the VALUE
    /// clause" (§14.9.39.4 GR6) → aligned per §13.18.63.4 GR7 → §14.6.8.5, "aligned at the leftmost character
    /// position in the data item with space fill or <b>truncation to the right</b>, as required".</para></summary>
    public static ValueSubject ForConditionName() =>
        new(ValueSubjectKind.ConditionName, null);
}
