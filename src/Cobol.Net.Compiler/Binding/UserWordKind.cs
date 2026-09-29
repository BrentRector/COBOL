// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>
/// The types of user-defined words — ISO §8.3.2.2, one member per entry of its list, in the list's order. A
/// declaration of any of them is announced through <see cref="DataBinder.DeclareUserWord"/>, the one funnel the
/// rules about a user-defined word AS DECLARED are asked from (today: §8.3.2.1 rule 5 and §12.3.8.3 SR12/SR13 —
/// an intrinsic-function-name the REPOSITORY paragraph identifies shall not be a user-defined word in its scope).
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
/// matches against the standard's list.</summary>
public static class UserWordKinds
{
    /// <summary>The standard's own name for the type (<c>AlphabetName</c> → <c>alphabet-name</c>).</summary>
    public static string Spelling(this UserWordKind kind) =>
        string.Concat(kind.ToString().Select((c, i) =>
            char.IsUpper(c) && i > 0 ? "-" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
