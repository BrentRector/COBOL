// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>
/// The types of user-defined words — ISO §8.3.2.2, one member per entry of its list, in the list's order. A
/// declaration of any of them is announced through <see cref="DataBinder.DeclareUserWord"/>, the one funnel the
/// rules about a user-defined word AS DECLARED are asked from (today: §8.3.2.1 rule 5 and §12.3.8.3 SR12/SR13 —
/// an intrinsic-function-name the REPOSITORY paragraph identifies shall not be a user-defined word in its scope —
/// and §8.3.2.2's one-type-per-word rule, kb/Work PB990, whose exceptions are <see cref="UserWordKinds.MayBeOneWord"/>).
/// <para>⛔ THE LIST IS THE SPEC'S, NOT OURS (kb/Work PB1083). The screen used to be called from six hand-picked
/// declaration funnels, so an alphabet-name, a class-name, a symbolic-character or a contained program's name
/// spelling a REPOSITORY intrinsic compiled clean. <c>UserWordDeclarationDriftTests</c> parses §8.3.2.2's list from
/// <c>specs/ISO_COBOL.md</c> and fails when a type has no member here, and when a member is neither declared at some
/// call site nor carried on its <c>NotDeclaredInASourceElement</c> exemption list with the reason.</para>
/// </summary>
public enum UserWordKind
{
    AlphabetName,
    ClassName,
    CompilationVariableName,
    ConditionName,
    ConstantName,
    DataName,
    DirectiveName,
    DynamicLengthStructureName,
    FileName,
    FunctionPrototypeName,
    IndexName,
    InterfaceName,
    LevelNumber,
    LocaleName,
    MethodName,
    MnemonicName,
    ObjectClassName,
    OrderingName,
    ParagraphName,
    ParameterName,
    ProgramName,
    ProgramPrototypeName,
    PropertyName,
    RecordKeyName,
    RecordName,
    ReportName,
    ScreenName,
    SectionName,
    SymbolicCharacter,
    TypeName,
    UserFunctionName,
}

/// <summary>The §8.3.2.2 spelling of a <see cref="UserWordKind"/> — for diagnostics, and the key the drift test
/// matches against the standard's list — and the ONE statement of which types may SHARE a word.</summary>
public static class UserWordKinds
{
    /// <summary>⛔ §8.3.2.2 EXCEPTION 3 AS DATA: "<i>the same name may be used as any of the following types of
    /// user-defined words — constant-name, data-name, property-name, record-key-name, record-name</i>". The drift test
    /// parses the standard's list and fails when this set and the list disagree (kb/Work PB990).</summary>
    public static IReadOnlySet<UserWordKind> MayShareOneName { get; } = new HashSet<UserWordKind>
    {
        UserWordKind.ConstantName, UserWordKind.DataName, UserWordKind.PropertyName, UserWordKind.RecordKeyName,
        UserWordKind.RecordName,
    };

    /// <summary>§8.3.2.2 EXCEPTION 2 AS DATA: "<i>a level-number may be the same as a paragraph-name or a
    /// section-name</i>".</summary>
    public static IReadOnlySet<UserWordKind> MayShareWithALevelNumber { get; } = new HashSet<UserWordKind>
    {
        UserWordKind.ParagraphName, UserWordKind.SectionName,
    };

    /// <summary>⛔ THE TYPES THAT MAY BE LETTERLESS, AS DATA (§8.3.2.2: "<i>With the exception of section-names,
    /// paragraph-names, and level-numbers, each user-defined word shall contain at least one basic letter or extended
    /// letter</i>"). A paragraph-name <c>1-2</c> is legal, so the lexer must admit the token, and every OTHER type is
    /// held to the rule where it is declared (<c>DataBinder.DeclareUserWord</c>; kb/Work PB1403). The drift test reads
    /// the exception list from the standard's text.</summary>
    public static IReadOnlySet<UserWordKind> MayBeLetterless { get; } = new HashSet<UserWordKind>
    {
        UserWordKind.SectionName, UserWordKind.ParagraphName, UserWordKind.LevelNumber,
    };

    /// <summary>⛔ THE ONE ANSWER to "may one word be used as BOTH of these types of user-defined word in a source
    /// element?" (ISO §8.3.2.2: "<i>a given user-defined word may be used as only one type of user-defined word</i>"
    /// with three exceptions): the same type (a word declared twice as one type is a uniqueness question,
    /// §8.4.2, asked elsewhere), exception 1 (a compilation-variable-name may be the same as ANY other type),
    /// exception 2 (<see cref="MayShareWithALevelNumber"/>) and exception 3 (<see cref="MayShareOneName"/>).</summary>
    public static bool MayBeOneWord(UserWordKind a, UserWordKind b) =>
        a == b
        || a == UserWordKind.CompilationVariableName || b == UserWordKind.CompilationVariableName
        || (a == UserWordKind.LevelNumber && MayShareWithALevelNumber.Contains(b))
        || (b == UserWordKind.LevelNumber && MayShareWithALevelNumber.Contains(a))
        || (MayShareOneName.Contains(a) && MayShareOneName.Contains(b));

    /// <summary>The standard's own name for the type (<c>AlphabetName</c> → <c>alphabet-name</c>).</summary>
    public static string Spelling(this UserWordKind kind) =>
        string.Concat(kind.ToString().Select((c, i) =>
            char.IsUpper(c) && i > 0 ? "-" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
