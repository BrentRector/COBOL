// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
using Antlr4.Runtime;
using CobolSharp.Compiler.CodeGen;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
// Alias for the namespace-relative `Generated.` shorthand this file also uses (the parser moved to
// CobolNet.Frontend.Generated at rearch P1; formerly reachable via the CobolSharp.Compiler root).
using Generated = CobolNet.Frontend.Generated;
using Common = CobolNet.Frontend.Common;   // alias for the namespace-relative Common. shorthand (rearch P1 rename)

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Pass 1: Walk the ANTLR parse tree and collect all symbol declarations.
/// Builds a proper parent/child DataSymbol tree using level numbers.
/// </summary>
public sealed class SemanticBuilder : CobolParserCoreBaseVisitor<object?>
{
    private readonly SymbolTable _symbols;
    private readonly DiagnosticBag _diagnostics = new();
    private int _fillerCounter;

    // Data items in declaration order (preserves all FILLERs)
    private readonly List<DataSymbol> _dataItemsInOrder = [];

    // Current section name (null if paragraphs are orphans)
    private string? _currentSectionName;

    // OO (ISO §11.7): the per-method paragraph scope while inside a METHOD-ID's PROCEDURE DIVISION (null otherwise).
    // Each method's paragraphs declare into their OWN scope so duplicate paragraph names across sibling methods
    // (e.g. every method having a MAIN paragraph) do not collide in the program's ProcedureDivisionScope, and a
    // method's PERFORM/GO TO resolves method-locally. The ordered (name, scope) list drives the IR method roster.
    private Scope? _currentMethodScope;
    private readonly List<(string Name, Scope Scope, IReadOnlyList<string> Using)> _classMethodScopes = [];

    /// <summary>OO class methods, in source order: the method name, the scope its paragraphs were declared in, and
    /// its PROCEDURE DIVISION USING names + the trailing RETURNING name (the ManagedPointer[] ABI order). Empty for a
    /// non-class unit. Consumed by the Binder to bound each method's slice of the paragraph dispatch order.</summary>
    public IReadOnlyList<(string Name, Scope Scope, IReadOnlyList<string> Using)> ClassMethodScopes => _classMethodScopes;

    // Section → paragraph membership (built during visit)
    private readonly Dictionary<string, List<string>> _sectionParagraphs =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Section-to-paragraph membership map built during parsing.</summary>
    public IReadOnlyDictionary<string, List<string>> SectionParagraphs => _sectionParagraphs;

    // Parent stack for level-number-based tree building
    private readonly Stack<DataSymbol> _dataStack = [];

    // Tracks which data division section we're currently visiting
    private StorageAreaKind _currentArea = StorageAreaKind.WorkingStorage;

    // Temporary: holds the REDEFINES target name during clause parsing
    private string? _deferredRedefinesName;
    private Runtime.SignStorageKind? _deferredSignStorage;
    private FigurativeKind? _deferredFigurativeInit;
    private string? _deferredAllLiteralPattern;

    // Screen items from SCREEN SECTION
    private readonly List<Bound.BoundScreenItem> _screenItems = [];
    public IReadOnlyList<Bound.BoundScreenItem> ScreenItems => _screenItems;

    // Extension clauses captured during parsing (vendor extensions, unrecognized clauses)
    private readonly List<ExtensionClauseNode> _extensionClauses = [];
    public IReadOnlyList<ExtensionClauseNode> ExtensionClauses => _extensionClauses;

    private void CaptureGenericClause(CobolParserCore.GenericClauseContext? ctx, GenericClauseContext context)
    {
        if (ctx == null) return;
        _extensionClauses.Add(ExtensionClauseNode.FromParseTree(ctx, context, _sourceName));
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.Diagnostics;
    public SymbolTable Symbols => _symbols;
    public IReadOnlyList<DataSymbol> DataItemsInOrder => _dataItemsInOrder;

    private readonly CompilationOptions _options;
    private readonly string _sourceName;

    public SemanticBuilder(string programName, int line, CompilationOptions? options = null,
        string sourceName = "<source>")
    {
        _symbols = new SymbolTable(programName, line);
        _options = options ?? new CompilationOptions();
        _sourceName = sourceName;
    }

    // The parse-tree node this builder was entered on (the program being built). A contained (nested)
    // program is its OWN source element with its own SemanticModel — Compilation builds each separately —
    // so the per-program walk must NOT descend into contained programs (their names are LOCAL to their own
    // element, ISO §8.4.6). Absorbing them re-declares the container's file/record/USING names (IC235A:
    // PRINT-FILE/PRINT-REC → CBL3107/3101; contained USING params → CBL3108). GLOBAL inheritance into a
    // contained program is handled separately on the independently-built ancestor models.
    private Antlr4.Runtime.Tree.IParseTree? _walkRoot;

    public override object? Visit(Antlr4.Runtime.Tree.IParseTree tree)
    {
        _walkRoot ??= tree;
        return base.Visit(tree);
    }

    public override object? VisitNestedProgram(CobolParserCore.NestedProgramContext context)
        => ReferenceEquals(context, _walkRoot) ? base.VisitNestedProgram(context) : DefaultResult;

    /// <summary>
    /// Pass 2: Resolve all deferred REDEFINES references now that all data items
    /// are registered in the symbol table.
    /// </summary>
    public void ResolveRedefines()
    {
        foreach (var data in _dataItemsInOrder)
        {
            if (data.RedefinesName == null) continue;

            // COBOL-85 §13.18.44: REDEFINES target must be at the same level
            // and precede the redefining item in the same record. When multiple
            // items share a name, prefer the one at the same level within the
            // same parent group (scan backwards from current position).
            DataSymbol? target = null;
            int idx = _dataItemsInOrder.IndexOf(data);
            for (int i = idx - 1; i >= 0; i--)
            {
                var candidate = _dataItemsInOrder[i];
                if (string.Equals(candidate.Name, data.RedefinesName, StringComparison.OrdinalIgnoreCase)
                    && candidate.LevelNumber == data.LevelNumber)
                {
                    target = candidate;
                    break;
                }
                // Stop searching when we pass a level-01 boundary
                if (candidate.LevelNumber == 1 && data.LevelNumber != 1)
                    break;
            }

            // Fallback to global scope lookup if no same-level sibling found
            target ??= _symbols.Program.DataDivisionScope.Resolve<DataSymbol>(data.RedefinesName);

            if (target != null)
                data.Redefines = target;
        }
    }

    /// <summary>
    /// Pass 2b: Resolve all deferred RENAMES FROM/THRU references now that all data items
    /// are registered in the symbol table.
    /// </summary>
    public void ResolveRenames()
    {
        foreach (var data in _dataItemsInOrder)
        {
            if (data.Renames == null) continue;
            var from = ResolveQualifiedDataName(data.Renames.FromName, data.Renames.FromQualifier);
            if (from != null)
            {
                if (from.LevelNumber is 66 or 88)
                    RenamesError(data, DiagnosticDescriptors.CBL0812, from.DisplayName);
                else
                    data.Renames.FromSymbol = from;
            }
            else
            {
                RenamesError(data, DiagnosticDescriptors.CBL0810, data.Renames.FromName, data.DisplayName);
            }

            if (data.Renames.ThruName != null)
            {
                var thru = ResolveQualifiedDataName(data.Renames.ThruName, data.Renames.ThruQualifier);
                if (thru != null)
                {
                    if (thru.LevelNumber is 66 or 88)
                        RenamesError(data, DiagnosticDescriptors.CBL0812, thru.DisplayName);
                    else
                        data.Renames.ThruSymbol = thru;
                }
                else
                {
                    RenamesError(data, DiagnosticDescriptors.CBL0811, data.Renames.ThruName, data.DisplayName);
                }
            }
        }
    }

    /// <summary>Resolve a data name with optional OF/IN qualifier.</summary>
    private DataSymbol? ResolveQualifiedDataName(string name, string? qualifier)
    {
        if (qualifier == null)
            return _symbols.Program.DataDivisionScope.Resolve<DataSymbol>(name);

        // Find items matching the name, then filter by qualifier (parent hierarchy)
        foreach (var candidate in _dataItemsInOrder)
        {
            if (!string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;
            // Walk up parent chain to find qualifier match
            var parent = candidate.Parent;
            while (parent != null)
            {
                if (string.Equals(parent.Name, qualifier, StringComparison.OrdinalIgnoreCase))
                    return candidate;
                parent = parent.Parent;
            }
        }
        // Fallback to unqualified
        return _symbols.Program.DataDivisionScope.Resolve<DataSymbol>(name);
    }

    private void RenamesError(DataSymbol data, DiagnosticDescriptor desc, params object[] args)
    {
        _diagnostics.Report(desc,
            new Common.SourceLocation(_sourceName, 0, data.Line, 0),
            Common.TextSpan.Empty, args);
    }

    /// <summary>
    /// Pass 3: Propagate group-level SIGN clauses to elementary children.
    /// Per ISO, a SIGN clause on a group item applies to all subordinate
    /// elementary numeric DISPLAY items that don't have their own SIGN clause.
    /// </summary>
    public void PropagateGroupSignClauses()
    {
        foreach (var root in _dataItemsInOrder)
        {
            if (root.LevelNumber == 1 || root.LevelNumber == 77)
                PropagateSignRecursive(root, inherited: null);
        }
    }

    private void PropagateSignRecursive(DataSymbol sym, Runtime.SignStorageKind? inherited)
    {
        var effective = sym.ExplicitSignStorage ?? inherited;

        if (sym.IsElementary &&
            sym.ResolvedType?.Pic is PicLayout pic &&
            pic.IsSigned &&
            sym.Usage == Runtime.UsageKind.Display &&
            sym.ExplicitSignStorage == null)
        {
            sym.ExplicitSignStorage = effective;
        }

        foreach (var child in sym.Children)
            PropagateSignRecursive(child, effective);
    }

    private void Error(ParserRuleContext ctx, DiagnosticDescriptor descriptor, params object[] args)
    {
        _diagnostics.Report(descriptor,
            new Common.SourceLocation(_sourceName, 0, ctx.Start.Line, ctx.Start.Column),
            new Common.TextSpan(ctx.Start.StartIndex, ctx.Stop?.StopIndex ?? ctx.Start.StopIndex),
            args);
    }

    // COMP-n where n is not a supported width (1–5) is not a defined USAGE keyword. Matches COMP-<digits> /
    // COMPUTATIONAL-<digits> exactly, so genuine vendor clauses and data-names (e.g. F-WHENCOMP-01) are untouched.
    private static bool IsUnsupportedCompUsage(string text)
    {
        string t = text.Trim().ToUpperInvariant();
        int dash = t.IndexOf('-');
        if (dash < 0)
            return false;
        string head = t.Substring(0, dash);
        if (head != "COMP" && head != "COMPUTATIONAL")
            return false;
        string suffix = t.Substring(dash + 1);
        if (suffix.Length == 0)
            return false;
        foreach (char c in suffix)
            if (c < '0' || c > '9')
                return false;   // not an all-digit COMP-n form → leave it to the generic vendor clause
        return suffix is not ("1" or "2" or "3" or "4" or "5");
    }

    // ── SPECIAL-NAMES ──

    private char _currencySign = '$';
    private char _currencyOutputChar = '$';
    private bool _decimalPointIsComma = false;
    private readonly List<ImplementorSwitch> _implementorSwitches = [];
    private readonly List<ClassDefinition> _classDefinitions = [];
    private readonly List<(string Name, byte Value)> _symbolicCharacters = [];
    private readonly List<AlphabetDefinition> _alphabetDefinitions = [];
    private string? _programCollatingSequenceAlphabetName = null;
    private int _defaultRoundingMode = 1;   // NEAREST-AWAY-FROM-ZERO (ISO default for a bare ROUNDED)

    public char CurrencySign => _currencySign;
    public char CurrencyOutputChar => _currencyOutputChar;
    public bool DecimalPointIsComma => _decimalPointIsComma;
    public IReadOnlyList<ImplementorSwitch> ImplementorSwitches => _implementorSwitches;
    public IReadOnlyList<ClassDefinition> ClassDefinitions => _classDefinitions;
    public IReadOnlyList<(string Name, byte Value)> SymbolicCharacters => _symbolicCharacters;
    public IReadOnlyList<AlphabetDefinition> AlphabetDefinitions => _alphabetDefinitions;
    public string? ProgramCollatingSequenceAlphabetName => _programCollatingSequenceAlphabetName;
    public int DefaultRoundingMode => _defaultRoundingMode;

    /// <summary>
    /// OPTIONS paragraph (ISO §11.9, COBOL-2002), now structurally parsed (optionsClause+). Read the DEFAULT
    /// ROUNDED clause's mode and record its ordinal so a bare ROUNDED phrase uses it as its default (ISO §11.9.6).
    /// The other clauses are parsed but not yet applied by this (legacy oracle) compiler.
    /// </summary>
    public override object? VisitOptionsParagraph(CobolParserCore.OptionsParagraphContext ctx)
    {
        foreach (var clause in ctx.optionsClause())
            if (clause.defaultRoundedClause()?.roundingModeName() is { } rm
                && MapRoundingModeName(rm.GetText().ToUpperInvariant()) is >= 0 and var m)
                _defaultRoundingMode = m;
        return base.VisitOptionsParagraph(ctx);
    }

    /// <summary>Map a ROUNDED-mode name to its ordinal (must match ArithmeticStatementBinder.BindRounded /
    /// PicRuntime.RoundToIntegerByMode); -1 if not a recognized mode name.</summary>
    private static int MapRoundingModeName(string name) => name switch
    {
        "TRUNCATION" => 0,
        "NEAREST-AWAY-FROM-ZERO" => 1,
        "AWAY-FROM-ZERO" => 2,
        "NEAREST-EVEN" => 3,
        "NEAREST-TOWARD-ZERO" => 4,
        "PROHIBITED" => 5,
        "TOWARD-GREATER" => 6,
        "TOWARD-LESSER" => 7,
        _ => -1
    };

    public override object? VisitSpecialNamesParagraph(CobolParserCore.SpecialNamesParagraphContext ctx)
    {
        foreach (var entry in ctx.specialNameEntry())
        {
            // CURRENCY SIGN IS literal [WITH PICTURE SYMBOL literal]
            if (entry.currencySignClause() is { } currClause)
            {
                var literals = currClause.literal();
                // literal-7: the currency string (output char)
                if (literals.Length >= 1)
                {
                    var nonNum = literals[0].nonNumericLiteral();
                    if (nonNum?.STRINGLIT() is { } slit)
                    {
                        var text = slit.GetText();
                        if (text.Length >= 3)
                        {
                            char lit7 = text[1];
                            _currencySign = lit7;
                            _currencyOutputChar = lit7;
                        }
                    }
                }

                // WITH PICTURE SYMBOL literal-8 (optional)
                if (currClause.PIC_STRING() is { } picStr)
                {
                    // Validate PIC_STRING == "SYMBOL"
                    if (!string.Equals(picStr.GetText(), "SYMBOL", StringComparison.OrdinalIgnoreCase))
                    {
                        Error(currClause, DiagnosticDescriptors.CBL3123, picStr.GetText());
                    }
                    else if (literals.Length >= 2)
                    {
                        // literal-8: the PIC symbol
                        var nonNum8 = literals[1].nonNumericLiteral();
                        if (nonNum8?.STRINGLIT() is { } slit8)
                        {
                            var text8 = slit8.GetText();
                            if (text8.Length >= 3)
                            {
                                char lit8 = text8[1];
                                // Validate: literal-8 must not be digit, letter, or space
                                if (char.IsDigit(lit8))
                                    Error(currClause, DiagnosticDescriptors.CBL3124, "a digit");
                                // ISO §13.18.3 rule 27: only these letters are forbidden as the currency
                                // PICTURE SYMBOL (A B C D E N P R S V X Z, case-insensitive); other letters
                                // (F G H I J K L M O Q T U W Y) are valid — the spec's own examples use 'U'/'q'.
                                else if ("ABCDENPRSVXZ".Contains(char.ToUpperInvariant(lit8)))
                                    Error(currClause, DiagnosticDescriptors.CBL3124,
                                        "a reserved picture letter (A B C D E N P R S V X Z)");
                                else if (lit8 == ' ')
                                    Error(currClause, DiagnosticDescriptors.CBL3124, "a space");
                                else
                                {
                                    // literal-8 = PIC symbol, literal-7 = output char
                                    _currencySign = lit8;
                                    // _currencyOutputChar stays as literal-7 (already set above)
                                }
                            }
                        }
                    }
                }
            }

            // DECIMAL-POINT IS COMMA
            if (entry.decimalPointClause() is { } dpClause)
            {
                var word = dpClause.IDENTIFIER()?.GetText();
                if (string.Equals(word, "COMMA", StringComparison.OrdinalIgnoreCase))
                    _decimalPointIsComma = true;
            }

            // Implementor switch: IDENTIFIER IS IDENTIFIER switchOnClause? switchOffClause?
            if (entry.implementorSwitchEntry() is { } swClause)
            {
                var ids = swClause.cobolWord();
                if (ids.Length >= 1)
                {
                    string implName = ids[0].GetText();
                    // Option 1 (ISO §12.3): 'switch-name IS mnemonic [ON/OFF STATUS ...]' — ids[1] is the
                    // mnemonic. Option 2: 'switch-name ON STATUS IS cond-1 [OFF STATUS IS cond-2]' with NO
                    // mnemonic (ids.Length == 1) — the ON/OFF condition-names are referenced directly. Both
                    // forms must register so the condition-names bind and are whitelisted (DEVLOG 310).
                    string mnemonicName = ids.Length >= 2 ? ids[1].GetText() : implName;
                    string? onName = swClause.switchStatusPhrases()?.switchOnClause()?.cobolWord()?.GetText();
                    string? offName = swClause.switchStatusPhrases()?.switchOffClause()?.cobolWord()?.GetText();

                    // Register only a genuine switch: a mnemonic (Option 1) or a status condition (Option 2).
                    if (ids.Length >= 2 || onName != null || offName != null)
                        _implementorSwitches.Add(
                            new ImplementorSwitch(mnemonicName, implName, onName, offName));
                }
            }

            // CHANNEL integer IS mnemonic (§12.4.4): a printer-channel mnemonic-name, referenced in
            // WRITE ... AFTER ADVANCING mnemonic. Capture the mnemonic (reusing the implementor-switch
            // carrier) so it is whitelisted and not flagged undefined (CBL3128). (DEVLOG 310)
            if (entry.channelClause() is { } chClause && chClause.dataReference()?.GetText() is { } chName)
                _implementorSwitches.Add(new ImplementorSwitch(chName, "CHANNEL", null, null));

            // CLASS class-name IS literal [THRU literal] [, literal [THRU literal]]...
            if (entry.classDefinitionClause() is { } classClause)
            {
                var className = classClause.cobolWord(0)?.GetText();
                if (className != null && classClause.classValueSet() is { } valueSet)
                {
                    var validBytes = new HashSet<byte>();
                    foreach (var item in valueSet.classValueItem())
                    {
                        var literals = item.literal();
                        if (literals.Length >= 1)
                        {
                            int fromVal = ExtractCharacterOrdinal(literals[0]);
                            if (literals.Length >= 2)
                            {
                                // THRU range
                                int toVal = ExtractCharacterOrdinal(literals[1]);
                                int lo = Math.Min(fromVal, toVal);
                                int hi = Math.Max(fromVal, toVal);
                                for (int i = lo; i <= hi && i < 256; i++)
                                    validBytes.Add((byte)i);
                            }
                            else if (fromVal >= 0 && fromVal < 256)
                            {
                                // Single literal — might be a string with multiple chars
                                var text = ExtractLiteralText(literals[0]);
                                if (text != null)
                                {
                                    foreach (char c in text)
                                        validBytes.Add((byte)c);
                                }
                                else
                                {
                                    validBytes.Add((byte)fromVal);
                                }
                            }
                        }
                    }
                    _classDefinitions.Add(new ClassDefinition(className, [.. validBytes]));
                }
            }

            // SYMBOLIC CHARACTERS name IS ordinal [, name IS ordinal]...
            if (entry.symbolicCharactersClause() is { } symClause)
            {
                foreach (var symEntry in symClause.symbolicCharacterEntry())
                {
                    // N:N positional mapping: cobolWord[i] ↔ integerLiteral[i]
                    // ISO §12.3.7 rule 16c: counts must be equal
                    var names = symEntry.cobolWord();
                    var ordinals = symEntry.integerLiteral();
                    if (names.Length != ordinals.Length)
                        Error(symEntry, DiagnosticDescriptors.CBL3125, names.Length, ordinals.Length);
                    int count = Math.Min(names.Length, ordinals.Length);
                    for (int i = 0; i < count; i++)
                    {
                        string symName = names[i].GetText();
                        int ordinal = int.Parse(ordinals[i].GetText());
                        if (ordinal >= 1 && ordinal <= 256)
                        {
                            // COBOL ordinal is 1-based: ordinal 1 = character 0
                            _symbolicCharacters.Add((symName, (byte)(ordinal - 1)));
                        }
                    }
                }
            }

            // ALPHABET alphabet-name IS ...
            if (entry.alphabetClause() is { } alphaClause)
            {
                var alphaName = alphaClause.cobolWord()?.GetText();
                if (alphaName != null && alphaClause.alphabetDefinition() is { } alphaDef)
                {
                    var sequence = BuildAlphabetCollatingSequence(alphaDef);
                    _alphabetDefinitions.Add(new AlphabetDefinition(alphaName, sequence));
                }
            }

        }

        return base.VisitSpecialNamesParagraph(ctx);
    }

    // ── OBJECT-COMPUTER (PROGRAM COLLATING SEQUENCE) ──

    public override object? VisitObjectComputerParagraph(CobolParserCore.ObjectComputerParagraphContext ctx)
    {
        // The clause is one of the paragraph's objectComputerClause list since kb/Work PB78 (the greenfield grammar's
        // shape — either clause, either order, with or without a computer-name); the legacy oracle keeps its 85 view.
        var pcsClause = ctx.objectComputerClause().Select(c => c.programCollatingSequenceClause()).FirstOrDefault(c => c is not null);
        if (pcsClause is not null)
        {
            // Shape-only update for the 2002 PCS grammar superset (cobolWord is an array now; the FOR
            // forms / alphabet-name-2 are greenfield-only — the legacy oracle keeps the 85 single-name view).
            var alphaName = pcsClause.cobolWord() is { Length: > 0 } ws ? ws[0].GetText() : null;
            if (alphaName != null)
                _programCollatingSequenceAlphabetName = alphaName;
        }
        return base.VisitObjectComputerParagraph(ctx);
    }

    // ── SPECIAL-NAMES helper methods ──

    /// <summary>Extract a character's ordinal value from a literal context.</summary>
    private static int ExtractCharacterOrdinal(CobolParserCore.LiteralContext lit)
    {
        // Non-numeric literal (string): first character's ordinal
        if (lit.nonNumericLiteral()?.STRINGLIT() is { } slit)
        {
            var text = slit.GetText();
            if (text.Length >= 3) // at least "X" (quotes + char)
                return (byte)text[1];
        }
        // Figurative constant in non-numeric literal
        if (lit.nonNumericLiteral()?.figurativeConstant() is { } fig)
        {
            var figText = fig.GetText().ToUpperInvariant();
            return figText switch
            {
                "HIGH-VALUE" or "HIGH-VALUES" => 0xFF,
                "LOW-VALUE" or "LOW-VALUES" => 0x00,
                "SPACE" or "SPACES" => 0x20,
                "ZERO" or "ZEROS" or "ZEROES" => 0x30,
                "QUOTE" or "QUOTES" => 0x22,
                _ => -1
            };
        }
        // Numeric literal (includes integers)
        if (lit.numericLiteral() is { } numLit)
        {
            if (int.TryParse(numLit.GetText(), out int val))
                return val;
        }
        return -1;
    }

    /// <summary>Extract the first OF/IN qualifier from a dataReference.</summary>
    private static string? ExtractQualifier(CobolParserCore.DataReferenceContext dataRef)
    {
        foreach (var suffix in dataRef.dataReferenceSuffix())
        {
            if (suffix.qualification() is { } qual)
                return qual.cobolWord()?.GetText();
        }
        return null;
    }

    /// <summary>Extract ALL OF/IN qualifiers from a dataReference, in source order (innermost-first), e.g.
    /// for <c>A OF B OF C</c> returns [B, C]. Empty when the reference is an unqualified data-name.</summary>
    private static List<string> ExtractQualifiers(CobolParserCore.DataReferenceContext dataRef)
    {
        var qualifiers = new List<string>();
        foreach (var suffix in dataRef.dataReferenceSuffix())
        {
            if (suffix.qualification()?.cobolWord()?.GetText() is { } q)
                qualifiers.Add(q);
        }
        return qualifiers;
    }

    /// <summary>Extract the text content of a string literal (without quotes).</summary>
    private static string? ExtractLiteralText(CobolParserCore.LiteralContext lit)
    {
        if (lit.nonNumericLiteral()?.STRINGLIT() is { } slit)
        {
            var text = slit.GetText();
            if (text.Length >= 2)
                return text[1..^1]; // strip quotes
        }
        return null;
    }

    /// <summary>Extract an integer value from a literal context.</summary>
    private static int ExtractIntegerValue(CobolParserCore.LiteralContext lit)
    {
        if (lit.numericLiteral() is { } numLit)
        {
            if (int.TryParse(numLit.GetText(), out int val))
                return val;
        }
        return -1;
    }

    /// <summary>
    /// Build a 256-byte collating sequence from an ALPHABET definition.
    /// Handles STANDARD-1 / STANDARD-2 / NATIVE (identity) and literal sequences.
    /// </summary>
    private byte[] BuildAlphabetCollatingSequence(CobolParserCore.AlphabetDefinitionContext alphaDef)
    {
        // NATIVE / STANDARD-1 / STANDARD-2 are dedicated lexer tokens forming their own
        // alphabetDefinition alternatives (NOT an alphabetEntry/cobolWord). On this ASCII host all
        // three ARE the native (identity) collating sequence. Detect them via the token accessors —
        // otherwise STANDARD-* fell through to the user-defined branch with zero entries and built an
        // all-255 table (every char weight 255 → every string compares equal to every other, so even
        // "ABCD" = SPACE became vacuously true).
        if (alphaDef.NATIVE() != null || alphaDef.STANDARD_1() != null || alphaDef.STANDARD_2() != null)
            return AlphabetDefinition.NativeCollatingSequence();

        var entries = alphaDef.alphabetEntry();
        if (entries.Length == 1)
        {
            // Defensive fallback: a predefined name that reached the parser as a cobolWord.
            var firstEntry = entries[0];
            var words = firstEntry.cobolWord();
            if (words.Length == 1)
            {
                var name = words[0].GetText().ToUpperInvariant();
                if (name is "STANDARD-1" or "STANDARD-2" or "NATIVE")
                    return AlphabetDefinition.NativeCollatingSequence();
            }
        }

        // User-defined: assign weights in the order specified
        var sequence = new byte[256];
        // Initialize all to 255 (unspecified characters sort last)
        Array.Fill(sequence, (byte)255);

        byte nextWeight = 0;
        foreach (var entry in entries)
        {
            var lits = entry.literal();
            var words = entry.cobolWord();

            // Get the primary value
            int primaryVal = -1;
            if (lits.Length >= 1)
                primaryVal = ExtractCharacterOrdinal(lits[0]);
            else if (words.Length >= 1)
            {
                // Could be a single character identifier treated as literal
                var txt = words[0].GetText();
                if (txt.Length == 1)
                    primaryVal = (byte)txt[0];
            }

            if (primaryVal >= 0 && primaryVal < 256)
            {
                // THRU range (only when THRU/THROUGH keyword is present)
                if (entry.THRU() != null || entry.THROUGH() != null)
                {
                    int endVal = lits.Length >= 2 ? ExtractCharacterOrdinal(lits[1]) : -1;
                    if (endVal >= 0 && endVal < 256)
                    {
                        int lo = Math.Min(primaryVal, endVal);
                        int hi = Math.Max(primaryVal, endVal);
                        if (primaryVal <= endVal)
                        {
                            for (int i = lo; i <= hi && i < 256; i++)
                                sequence[i] = nextWeight++;
                        }
                        else
                        {
                            for (int i = hi; i >= lo && i >= 0; i--)
                                sequence[i] = nextWeight++;
                        }
                    }
                    else
                    {
                        sequence[primaryVal] = nextWeight++;
                    }
                }
                else
                {
                    // Single value or ALSO group — all share the same weight
                    sequence[primaryVal] = nextWeight;

                    // Process ALSO entries: literals after the first share the same weight
                    var alsoTokens = entry.ALSO();
                    if (alsoTokens != null && alsoTokens.Length > 0)
                    {
                        // lits[0] is primary (already assigned above).
                        // lits[1..] are ALSO literals; cobolWords after the first
                        // may also be ALSO entries. Process all ALSO values.
                        for (int alsoIdx = 1; alsoIdx < lits.Length; alsoIdx++)
                        {
                            int alsoVal = ExtractCharacterOrdinal(lits[alsoIdx]);
                            if (alsoVal >= 0 && alsoVal < 256)
                                sequence[alsoVal] = nextWeight;
                        }
                        // Also handle ALSO cobolWords (if primary was a literal,
                        // ALSO entries could be cobolWords)
                        // Grammar: (ALSO (cobolWord | literal))* — each ALSO has
                        // exactly one word or literal. Words after the primary
                        // that aren't the first word are ALSO entries.
                        int wordStart = (lits.Length >= 1) ? 0 : 1; // if primary was a literal, all words are ALSO
                        if (lits.Length >= 1)
                        {
                            // Primary was a literal, so all cobolWords are ALSO entries
                            for (int wi = 0; wi < words.Length; wi++)
                            {
                                int wval = ResolveAlphabetWordOrdinal(words[wi].GetText());
                                if (wval >= 0 && wval < 256) sequence[wval] = nextWeight;
                            }
                        }
                        else
                        {
                            // Primary was a cobolWord (words[0]), ALSO words are words[1..]
                            for (int wi = 1; wi < words.Length; wi++)
                            {
                                int wval = ResolveAlphabetWordOrdinal(words[wi].GetText());
                                if (wval >= 0 && wval < 256) sequence[wval] = nextWeight;
                            }
                        }
                    }

                    nextWeight++;
                }
            }
            else
            {
                nextWeight++;
            }
        }

        return sequence;
    }

    /// <summary>
    /// Resolves a cobolWord text to its character ordinal for alphabet definitions.
    /// Handles figurative constant names (HIGH-VALUE, LOW-VALUE, SPACE, ZERO, QUOTE)
    /// and single-character identifiers.
    /// </summary>
    private static int ResolveAlphabetWordOrdinal(string text)
    {
        var upper = text.ToUpperInvariant();
        return upper switch
        {
            "HIGH-VALUE" or "HIGH-VALUES" => 0xFF,
            "LOW-VALUE" or "LOW-VALUES" => 0x00,
            "SPACE" or "SPACES" => 0x20,
            "ZERO" or "ZEROS" or "ZEROES" => 0x30,
            "QUOTE" or "QUOTES" => 0x22,
            _ when text.Length == 1 => (byte)text[0],
            _ => -1
        };
    }

    // ── Data Division ──

    public override object? VisitWorkingStorageSection(CobolParserCore.WorkingStorageSectionContext ctx)
    {
        _dataStack.Clear();
        _currentArea = StorageAreaKind.WorkingStorage;
        return base.VisitWorkingStorageSection(ctx);
    }

    public override object? VisitLocalStorageSection(CobolParserCore.LocalStorageSectionContext ctx)
    {
        _dataStack.Clear();
        _currentArea = StorageAreaKind.LocalStorage;
        return base.VisitLocalStorageSection(ctx);
    }

    public override object? VisitLinkageSection(CobolParserCore.LinkageSectionContext ctx)
    {
        _dataStack.Clear();
        _currentArea = StorageAreaKind.LinkageSection;
        return base.VisitLinkageSection(ctx);
    }

    // ═══════════════════════════════════
    // FILE-CONTROL — extract SELECT/ASSIGN/clauses into FileSymbol
    // ═══════════════════════════════════

    public override object? VisitFileControlClauseGroup(CobolParserCore.FileControlClauseGroupContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return base.VisitFileControlClauseGroup(ctx);

        string name = fileNameCtx.GetText();
        var fileSym = new FileSymbol(name, fileNameCtx.Start.Line);

        // SELECT OPTIONAL
        if (ctx.OPTIONAL() != null)
            fileSym.IsOptional = true;

        // Clauses (ASSIGN, ORGANIZATION, ACCESS, keys, FILE STATUS) — any order per ISO §12.4.5.2 SR1.
        foreach (var clause in ctx.fileControlClauses())
        {
            // The shared grammar's TO phrase is a list (ISO §12.4.5.1; kb/Work PB829): the oracle takes the operand that
            // names the file - the last - which is WiseOwl COBOL's DOC-A.1-71 reading of every list it allows.
            if (clause.assignClause()?.assignTarget() is { Length: > 0 } assignCtxs)
            {
                string assignText = assignCtxs[^1].GetText();
                // String literal → explicit host path; identifier → implementor-defined
                if (assignText.Length >= 2 &&
                    (assignText[0] == '"' || assignText[0] == '\''))
                {
                    fileSym.AssignTarget = assignText[1..^1];
                    fileSym.AssignIsLiteral = true;
                }
                else
                {
                    fileSym.AssignTarget = assignText;
                    fileSym.AssignIsLiteral = false;
                }
            }
            if (clause.organizationClause() is { } orgClause)
            {
                var orgType = orgClause.organizationType();
                // LINE SEQUENTIAL is two tokens; GetText() gives "LINESEQUENTIAL"
                if (orgType.LINE() != null)
                    fileSym.Organization = "LINE SEQUENTIAL";
                else if (orgType.RECORD() != null)      // `RECORD SEQUENTIAL` is record sequential (kb/Work PB706)
                    fileSym.Organization = "SEQUENTIAL";
                else
                    fileSym.Organization = orgType.GetText().ToUpperInvariant();
            }
            if (clause.accessModeClause() is { } accessClause)
                fileSym.AccessMode = accessClause.accessMode().GetText().ToUpperInvariant();
            if (clause.recordKeyClause() is { } keyClause)
            {
                // Store the base data-name and its OF/IN qualifiers separately (not GetText(), which would
                // concatenate them) so a qualified key like `IX-FD3-KEY IN IX-FD3-RECKEY-AREA` resolves to
                // the correct record position even when several keys share the base name (ISO §12.4.5.12).
                fileSym.RecordKey = keyClause.dataReference().cobolWord().GetText();
                fileSym.RecordKeyQualifiers.AddRange(ExtractQualifiers(keyClause.dataReference()));
                // Leniency L3 (docs/dialect-strictness.md): ISO §12.4.5.12 requires the KEY keyword.
                CheckRecordKeyNoiseWord(keyClause, keyClause.KEY() == null);
            }
            if (clause.fileStatusClause() is { } statusClause)
                // Use the base data-name only — a "STATUS data-name IN group" qualifier is a
                // dataReferenceSuffix that GetText() would concatenate; the (flat) data scope
                // resolves the item by its own name.
                fileSym.FileStatus = statusClause.dataReference().cobolWord().GetText();
            if (clause.alternateKeyClause() is { } altKeyClause)
            {
                string altKeyName = altKeyClause.dataReference().cobolWord().GetText();
                var altQualifiers = ExtractQualifiers(altKeyClause.dataReference());
                bool duplicates = altKeyClause.DUPLICATES() != null;
                fileSym.AlternateKeys.Add(new AlternateKeyInfo(altKeyName, altQualifiers, duplicates));
                // Leniency L3: KEY is also required in ALTERNATE RECORD KEY (§12.4.5.12).
                CheckRecordKeyNoiseWord(altKeyClause, altKeyClause.KEY() == null);
            }
            if (clause.relativeKeyClause() is { } relKeyClause)
            {
                fileSym.RelativeKey = relKeyClause.dataReference().GetText();
                // Leniency L2 (docs/dialect-strictness.md): ISO §12.4.5.13 requires the KEY keyword.
                // The data-name is captured either way (so the file works in all modes); the no-KEY form
                // is accepted in Default and diagnosed under named-strict modes.
                if (relKeyClause.KEY() == null)
                {
                    var tok = relKeyClause.Start;
                    var loc = new Common.SourceLocation(_sourceName, 0, tok.Line, tok.Column);
                    var span = new Common.TextSpan(tok.StartIndex, relKeyClause.Stop?.StopIndex ?? tok.StopIndex);
                    if (_options.Config.IsStrict)
                        _diagnostics.Report(DiagnosticDescriptors.CBL3613, loc, span, _options.Config.DisplayName);
                    else if (_options.WarnNonStandard)
                        _diagnostics.Report(DiagnosticDescriptors.CBL3614, loc, span);
                }
            }
        }

        _symbols.Program.GlobalScope.TryDeclare(fileSym, out _);
        return base.VisitFileControlClauseGroup(ctx);
    }

    /// <summary>
    /// Leniency L3 (docs/dialect-strictness.md): the RECORD KEY / ALTERNATE RECORD KEY clause with the
    /// required KEY keyword omitted (ISO §12.4.5.12 — KEY is unbracketed → required; the CCVS suite writes
    /// `RECORD data-name`). The data-name is captured as the key either way (so the file works in all
    /// modes); the no-KEY form is accepted in Default and diagnosed under named-strict modes.
    /// </summary>
    private void CheckRecordKeyNoiseWord(Antlr4.Runtime.ParserRuleContext clause, bool noKey)
    {
        if (!noKey) return;
        var tok = clause.Start;
        var loc = new Common.SourceLocation(_sourceName, 0, tok.Line, tok.Column);
        var span = new Common.TextSpan(tok.StartIndex, clause.Stop?.StopIndex ?? tok.StopIndex);
        if (_options.Config.IsStrict)
            _diagnostics.Report(DiagnosticDescriptors.CBL3615, loc, span, _options.Config.DisplayName);
        else if (_options.WarnNonStandard)
            _diagnostics.Report(DiagnosticDescriptors.CBL3616, loc, span);
    }

    // ═══════════════════════════════════
    // FILE SECTION / FD
    // ═══════════════════════════════════

    public override object? VisitFileSection(CobolParserCore.FileSectionContext ctx)
    {
        _dataStack.Clear();
        _currentArea = StorageAreaKind.FileSection;
        return base.VisitFileSection(ctx);
    }

    /// <summary>Current FD file symbol — set during FD visiting so 01-level records can be linked.</summary>
    private FileSymbol? _currentFdFile;

    public override object? VisitFileDescriptionEntry(CobolParserCore.FileDescriptionEntryContext ctx)
    {
        var nameCtx = ctx.fileName();
        if (nameCtx != null)
        {
            string name = nameCtx.GetText();
            // Look up existing FileSymbol created by FILE-CONTROL visitor.
            // If not found (no SELECT), create one as a fallback with a warning.
            var fileSym = _symbols.Program.GlobalScope.Resolve<FileSymbol>(name);
            if (fileSym == null)
            {
                fileSym = new FileSymbol(name, nameCtx.Start.Line);
                _symbols.Program.GlobalScope.TryDeclare(fileSym, out _);
                _diagnostics.Report(DiagnosticDescriptors.CBL0601,
                    new Common.SourceLocation(_sourceName, 0, nameCtx.Start.Line, nameCtx.Start.Column),
                    new Common.TextSpan(nameCtx.Start.StartIndex, nameCtx.Stop?.StopIndex ?? nameCtx.Start.StopIndex),
                    name);
            }
            // FD ... IS GLOBAL (§13.18.30): mark the file so its record + subordinate names are inherited
            // by contained programs (and not falsely flagged as undefined there). (DEVLOG 310)
            // FD ... IS EXTERNAL (§13.18.22): the file and its record AREA are shared across the run unit;
            // mark the file so the 01-record below is externalized (record-area sharing across separately
            // compiled programs — IC227A). GLOBAL and EXTERNAL are alternatives of the same FD clause.
            if (ctx.fileDescriptionClauses() is { } fdClauses)
                foreach (var c in fdClauses.fileDescriptionClause())
                {
                    if (c.fileGlobalExternalClause() is { } gx)
                    {
                        if (gx.GLOBAL() != null) fileSym.IsGlobal = true;
                        if (gx.EXTERNAL() != null) fileSym.IsExternal = true;
                    }
                    // REPORT(S) IS/ARE report-name… (§13.18.46): record which file each report is written to,
                    // applied to the ReportSymbol's HostFileName when the REPORT SECTION's RD is visited.
                    if (c.reportClause() is { } rc)
                        foreach (var rn in rc.reportName())
                            _reportToFile[rn.GetText()] = name;
                }
            _currentFdFile = fileSym;
        }
        _dataStack.Clear();
        var result = base.VisitFileDescriptionEntry(ctx);
        _currentFdFile = null;
        return result;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // REPORT SECTION (ISO §13.14/§13.15) — build the report symbol model (RD + report group tree).
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>report-name → host file-name, captured from each FD's REPORT IS clause.</summary>
    private readonly Dictionary<string, string> _reportToFile = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Current RD report symbol — set during RD visiting so report group entries can be linked.</summary>
    private ReportSymbol? _currentReport;
    /// <summary>Level-number stack for building the report group tree (mirrors <see cref="_dataStack"/>).</summary>
    private readonly Stack<ReportGroupSymbol> _reportGroupStack = [];

    public override object? VisitReportDescriptionEntry(CobolParserCore.ReportDescriptionEntryContext ctx)
    {
        var nameCtx = ctx.reportName();
        if (nameCtx == null) return base.VisitReportDescriptionEntry(ctx);

        string name = nameCtx.GetText();
        var report = new ReportSymbol(name, nameCtx.Start.Line);
        if (_reportToFile.TryGetValue(name, out var host)) report.HostFileName = host;

        foreach (var clause in ctx.reportDescriptionClause())
        {
            if (clause.reportGlobalClause() != null) report.IsGlobal = true;
            if (clause.reportCodeClause()?.literal() is { } codeLit) report.CodeValue = codeLit.GetText();
            if (clause.reportControlClause() is { } cc)
            {
                if (cc.FINAL() != null) report.ControlFields.Add("FINAL");
                foreach (var dr in cc.dataReference()) report.ControlFields.Add(dr.GetText());
            }
            if (clause.reportPageClause() is { } pc)
            {
                if (pc.integerLiteral() is { } pl && int.TryParse(pl.GetText(), out int lines))
                    report.PageLimitLines = lines;
                foreach (var sub in pc.reportPageSubclause())
                {
                    if (sub.integerLiteral() is not { } si || !int.TryParse(si.GetText(), out int v)) continue;
                    if (sub.HEADING() != null) report.HeadingLine = v;
                    else if (sub.FIRST() != null) report.FirstDetailLine = v;
                    else if (sub.LAST() != null) report.LastDetailLine = v;
                    else if (sub.FOOTING() != null) report.FootingLine = v;
                }
            }
        }
        if (report.LastDetailLine == 0) report.LastDetailLine = report.PageLimitLines;
        if (report.FootingLine == 0) report.FootingLine = report.PageLimitLines;

        _symbols.Program.GlobalScope.TryDeclare(report, out _);
        _currentReport = report;
        _reportGroupStack.Clear();
        var result = base.VisitReportDescriptionEntry(ctx);
        // Compute the composed line width: the widest column-end over all elementary fields (default 132).
        int width = 0;
        foreach (var g in report.AllGroups())
            if (g.HasColumn && g.FieldWidth > 0)
                width = Math.Max(width, g.ColumnValue - 1 + g.FieldWidth);
        report.LineWidth = width > 0 ? width : 132;
        _currentReport = null;
        return result;
    }

    public override object? VisitReportGroupEntry(CobolParserCore.ReportGroupEntryContext ctx)
    {
        if (_currentReport == null || ctx.levelNumber() == null
            || !int.TryParse(ctx.levelNumber().GetText(), out int level))
            return base.VisitReportGroupEntry(ctx);

        string name = ctx.reportGroupName()?.GetText() ?? $"FILLER${_fillerCounter++}";
        var group = new ReportGroupSymbol(name, level, ctx.levelNumber().Start.Line) { OwningReport = _currentReport };

        foreach (var clause in ctx.reportGroupClause())
        {
            if (clause.reportGroupIndicateClause() != null)
                group.GroupIndicate = true;
            if (clause.reportTypeClause()?.reportGroupType() is { } gt)
            {
                group.GroupKind = ReportGroupTypeOf(gt);
                if (gt.dataReference() is { } cf) group.ControlField = cf.GetText();
                else if (gt.FINAL() != null) group.ControlField = "FINAL";
            }
            if (clause.reportLineClause() is { } lc)
            {
                // Shape-only migration (P10 Step 13): the greenfield grammar factored the LINE operand into
                // reportLineOperand+ (the 2002 multiple-LINE form); the legacy reads the FIRST operand exactly
                // as it read the former single-operand rule.
                group.HasLine = true;
                var lop = lc.reportLineOperand(0);
                if (lop.NEXT() != null && lop.PAGE() != null) group.LineNextPage = true;
                else if (lop.integerLiteral() is { } li && int.TryParse(li.GetText(), out int lv))
                {
                    group.LineValue = lv;
                    group.LineRelative = lop.reportRelativeSign() != null;
                }
            }
            if (clause.reportColumnClause()?.reportColumnOperand(0)?.integerLiteral() is { } colLit
                && int.TryParse(colLit.GetText(), out int col))
            {
                group.HasColumn = true;
                group.ColumnValue = col;
            }
            // The SOURCE clause is an operand LIST (ISO §13.18.53.2; kb/Work PB506 gave it its grammar surface).
            // The legacy oracle carries ONE source name per printable item and no repetition model at all, so it
            // reads the FIRST operand — the only shape it could ever compile — and the differential simply has no
            // opinion on a multi-operand clause. It is a regression net, not authority (CLAUDE.md rule 1).
            // The operand is now `reportValueOperand` (an arithmetic expression whose degenerate case is the bare
            // identifier — kb/Work PB852/PB883); the legacy oracle models only the identifier form, so it reads
            // the operand's TEXT exactly as before and has no opinion on an expression operand either.
            if (clause.reportSourceClause()?.reportValueOperand() is { Length: > 0 } src) group.SourceName = src[0].GetText();
            if (clause.reportSumClause() is { } sumc)
                foreach (var op in sumc.reportValueOperand()) group.SumFields.Add(op.GetText());
            if (clause.pictureClause()?.PIC_STRING() is { } pic)
            {
                group.PicString = pic.GetText();
                group.FieldWidth = ReportFieldWidth(group.PicString);
            }
            if (clause.valueClause() is { } vc && vc.GetText() is { } vtext) group.ValueLiteral = vtext;
        }

        // Build the level-number tree (mirrors VisitDataDescriptionEntry's _dataStack logic).
        while (_reportGroupStack.Count > 0 && _reportGroupStack.Peek().Level >= level)
            _reportGroupStack.Pop();
        if (_reportGroupStack.Count > 0)
        {
            var parent = _reportGroupStack.Peek();
            group.Parent = parent;
            parent.Children.Add(group);
        }
        else
        {
            _currentReport.TopGroups.Add(group);
        }
        _reportGroupStack.Push(group);

        // Report group names are referenceable by GENERATE; declare them so name resolution sees them.
        if (ctx.reportGroupName() != null)
            _symbols.Program.DataDivisionScope.TryDeclare(group, out _);

        return base.VisitReportGroupEntry(ctx);
    }

    private static ReportGroupKind ReportGroupTypeOf(CobolParserCore.ReportGroupTypeContext gt)
    {
        if (gt.RH() != null || gt.REPORT() != null && gt.HEADING() != null) return ReportGroupKind.ReportHeading;
        if (gt.PH() != null || gt.PAGE() != null && gt.HEADING() != null) return ReportGroupKind.PageHeading;
        if (gt.CH() != null || gt.CONTROL() != null && gt.HEADING() != null) return ReportGroupKind.ControlHeading;
        if (gt.DE() != null || gt.DETAIL() != null) return ReportGroupKind.Detail;
        if (gt.CF() != null || gt.CONTROL() != null && gt.FOOTING() != null) return ReportGroupKind.ControlFooting;
        if (gt.PF() != null || gt.PAGE() != null && gt.FOOTING() != null) return ReportGroupKind.PageFooting;
        if (gt.RF() != null || gt.REPORT() != null && gt.FOOTING() != null) return ReportGroupKind.ReportFooting;
        return ReportGroupKind.None;
    }

    /// <summary>Receiving width (bytes) of a report field from its PIC string (DISPLAY usage).</summary>
    private static int ReportFieldWidth(string picString)
    {
        try
        {
            var desc = Runtime.PicDescriptorFactory.FromPicBody(
                picString.Trim(), Runtime.UsageKind.Display, isSigned: false,
                signStorage: Runtime.SignStorageKind.None);
            return desc.StorageLength;
        }
        catch { return 0; }
    }

    public override object? VisitRecordClause(CobolParserCore.RecordClauseContext ctx)
    {
        // RECORD IS VARYING IN SIZE [FROM m] [TO n] CHARACTERS [DEPENDING ON data-name] (§13.18.43).
        // Capture the DEPENDING-ON name (and min/max) so READ can set the actual record length and
        // WRITE/REWRITE can use it. Only the VARYING form carries this; the fixed-length form does not.
        if (_currentFdFile != null && ctx.VARYING() != null)
        {
            _currentFdFile.IsRecordVarying = true;
            var lits = ctx.integerLiteral();
            // FROM m TO n → lits[0]=min, lits[1]=max; a lone literal is treated as the max.
            if (lits.Length >= 2)
            {
                if (int.TryParse(lits[0].GetText(), out int min)) _currentFdFile.RecordVaryingMin = min;
                if (int.TryParse(lits[1].GetText(), out int max)) _currentFdFile.RecordVaryingMax = max;
            }
            else if (lits.Length == 1 && int.TryParse(lits[0].GetText(), out int only))
            {
                _currentFdFile.RecordVaryingMax = only;
            }
            if (ctx.dataReference() is { } dref)
                _currentFdFile.RecordVaryingDependingOn = dref.cobolWord().GetText();
        }
        // Format 2: RECORD CONTAINS integer-1 TO integer-2 CHARACTERS (no VARYING keyword) ALSO declares a
        // variable-length record when integer-1 ≠ integer-2 (ISO §13.18.43). Unlike the explicit VARYING
        // form there is no DEPENDING ON object: the record's length is determined by its own content — e.g.
        // an embedded OCCURS DEPENDING ON group, where the WRITE/REWRITE length is the record's runtime size
        // (the IrOdoGroupLocation computes it) and a READ recovers the length from the length-framed store.
        // Flag it varying so the same length-framed I/O path the explicit VARYING form uses applies; a
        // min == max range (e.g. "100 TO 100") is fixed and left alone.
        else if (_currentFdFile != null && ctx.TO() != null)
        {
            var lits = ctx.integerLiteral();
            if (lits.Length >= 2
                && int.TryParse(lits[0].GetText(), out int min)
                && int.TryParse(lits[1].GetText(), out int max)
                && min != max)
            {
                _currentFdFile.IsRecordVarying = true;
                _currentFdFile.RecordVaryingMin = min;
                _currentFdFile.RecordVaryingMax = max;
            }
        }
        return base.VisitRecordClause(ctx);
    }

    public override object? VisitLinageClause(CobolParserCore.LinageClauseContext ctx)
    {
        if (_currentFdFile != null)
        {
            _currentFdFile.HasLinage = true;
            var intLit = ctx.integerLiteral();
            if (intLit != null && int.TryParse(intLit.GetText(), out int body))
                _currentFdFile.LinageBody = body;
            else if (ctx.dataReference() is { } bodyRef)
                _currentFdFile.LinageBodyName = bodyRef.GetText();

            if (ctx.linageFootingPhrase() is { } footCtx)
            {
                var footInt = footCtx.integerLiteral();
                if (footInt != null && int.TryParse(footInt.GetText(), out int foot))
                    _currentFdFile.LinageFooting = foot;
                else if (footCtx.dataReference() is { } footRef)
                    _currentFdFile.LinageFootingName = footRef.GetText();
            }

            if (ctx.linageLinesAtTopPhrase() is { } topCtx)
            {
                var topInt = topCtx.integerLiteral();
                if (topInt != null && int.TryParse(topInt.GetText(), out int top))
                    _currentFdFile.LinageTop = top;
                else if (topCtx.dataReference() is { } topRef)
                    _currentFdFile.LinageTopName = topRef.GetText();
            }

            if (ctx.linageLinesAtBottomPhrase() is { } botCtx)
            {
                var botInt = botCtx.integerLiteral();
                if (botInt != null && int.TryParse(botInt.GetText(), out int bot))
                    _currentFdFile.LinageBottom = bot;
                else if (botCtx.dataReference() is { } botRef)
                    _currentFdFile.LinageBottomName = botRef.GetText();
            }
        }
        return base.VisitLinageClause(ctx);
    }

    public override object? VisitSortMergeDescriptionEntry(CobolParserCore.SortMergeDescriptionEntryContext ctx)
    {
        var nameCtx = ctx.fileName();
        if (nameCtx != null)
        {
            string name = nameCtx.GetText();
            var fileSym = _symbols.Program.GlobalScope.Resolve<FileSymbol>(name);
            if (fileSym == null)
            {
                fileSym = new FileSymbol(name, nameCtx.Start.Line);
                _symbols.Program.GlobalScope.TryDeclare(fileSym, out _);
            }
            fileSym.IsSortMerge = true;
            _currentFdFile = fileSym;
        }
        _dataStack.Clear();
        var result = base.VisitSortMergeDescriptionEntry(ctx);
        _currentFdFile = null;
        return result;
    }

    public override object? VisitDataDescriptionEntry(CobolParserCore.DataDescriptionEntryContext ctx)
    {
        var levelCtx = ctx.levelNumber();
        var nameCtx = ctx.dataName();
        if (levelCtx == null) return base.VisitDataDescriptionEntry(ctx);

        // ISO §8.5.1.2: valid level numbers are 1-49, 66, 77, 88. The grammar's INTEGERLIT permits any
        // digit run, so int.Parse would OverflowException on a huge level; TryParse + range check
        // diagnoses bad input gracefully instead of crashing. Unconditional (no valid program has an
        // out-of-range level, so it never fires on the corpus). (DEVLOG 306)
        if (!int.TryParse(levelCtx.GetText(), out int level)
            || !(level is (>= 1 and <= 49) or 66 or 77 or 88))
        {
            _diagnostics.Report(DiagnosticDescriptors.CBL0815,
                new Common.SourceLocation(_sourceName, 0, levelCtx.Start.Line, levelCtx.Start.Column),
                new Common.TextSpan(levelCtx.Start.StartIndex, levelCtx.Stop?.StopIndex ?? levelCtx.Start.StopIndex),
                levelCtx.GetText(), nameCtx?.GetText() ?? "FILLER");
            return base.VisitDataDescriptionEntry(ctx); // skip malformed entry; do not crash
        }
        string displayName = nameCtx?.GetText() ?? "FILLER";
        int line = levelCtx.Start.Line;

        // Make FILLER unique for symbol table
        bool isFiller = string.Equals(displayName, "FILLER", StringComparison.OrdinalIgnoreCase);
        string internalName = isFiller ? $"FILLER${_fillerCounter++}" : displayName;

        // Extract PIC, USAGE, VALUE from clauses
        string? picString = null;
        var usage = UsageKind.Display;
        bool hasExplicitUsage = false;
        bool isUnsignedBinary = false;
        string? objectClassName = null; // OO: USAGE OBJECT REFERENCE class-name
        string? typeName = null;
        string? initialValue = null;
        var body = ctx.dataDescriptionBody();
        if (body?.dataDescriptionClauses() != null)
        {
            foreach (var clause in body.dataDescriptionClauses().dataDescriptionClause())
            {
                var picClause = clause.pictureClause();
                if (picClause != null)
                    picString = picClause.PIC_STRING()?.GetText()?.Trim();

                var usageClause = clause.usageClause();
                if (usageClause != null)
                {
                    // OO (COBOL-2002, ISO §13.18.60.4): USAGE OBJECT REFERENCE [class-name] — a managed .NET
                    // reference, not a byte-keyword usage. Detect the dedicated grammar alt and capture the
                    // declared class so the _OBJ_ field can be typed and INVOKE method targets resolved.
                    if (usageClause.usageKeyword()?.objectReferenceUsage() is { } objRef)
                    {
                        usage = UsageKind.Object;
                        objectClassName = objRef.className()?.cobolWord()?.GetText();
                        hasExplicitUsage = true;
                    }
                    else
                    {
                        // `[USAGE IS] usageKeyword` is ONE alternative (kb/Work PB95): the keyword node is always
                        // present, bare or prefixed, and binarySign / noSignPhrase are its siblings — never glued
                        // into its text.
                        var kwText = usageClause.usageKeyword().GetText();
                        usage = UsageMapper.FromUsageKeyword(kwText);
                        // SIGNED (default) / UNSIGNED on a BINARY-xxx usage (ISO §13.18.60).
                        isUnsignedBinary = usageClause.binarySign()?.UNSIGNED() != null;
                        hasExplicitUsage = true;
                    }
                }

                // An unknown COMP-n (COMP-9, COMPUTATIONAL-7, …) has no usage token, so it lexes as an
                // IDENTIFIER and lands on the §13.16.2 tail production; reject it rather than silently treating
                // the item as DISPLAY (ISO §13.18.60.2 — the USAGE format has no such form).
                // ⛔ kb/Work PB487 renamed that production `genericDataClause` → `unrecognizedDataClause` and
                // CLOSED the Format-1 clause list; kb/Work PB829 then unified it with the other five catch-alls
                // into the ONE `unrecognizedClause` error production. In the GREENFIELD every instance is refused
                // by name (COBOLNET1941), of which this legacy COMP-n arm is the narrow precursor. The legacy
                // tree keeps its own behaviour byte-for-byte — it is the differential ORACLE until P15.
                var genericClause = clause.unrecognizedClause()?.genericClause();
                if (genericClause != null)
                {
                    var ids = genericClause.IDENTIFIER();
                    if (ids.Length > 0 && IsUnsupportedCompUsage(ids[0].GetText()))
                        Error(genericClause, DiagnosticDescriptors.CBL0816, ids[0].GetText(), displayName);
                }

                var redefinesClause = clause.redefinesClause();
                if (redefinesClause != null)
                {
                    // Store the unresolved name; actual resolution happens in pass 2
                    // after all data items are registered in the symbol table
                    _deferredRedefinesName = redefinesClause.dataReference()?.GetText() ?? "";
                }

                var typeClause = clause.typeClause();
                if (typeClause != null)
                    typeName = typeClause.IDENTIFIER()?.GetText();

                var signCl = clause.signClause();
                if (signCl != null)
                {
                    bool isLeading = signCl.LEADING() != null;
                    bool isSeparate = signCl.SEPARATE() != null;
                    _deferredSignStorage = (isLeading, isSeparate) switch
                    {
                        (true, true) => Runtime.SignStorageKind.LeadingSeparate,
                        (true, false) => Runtime.SignStorageKind.LeadingOverpunch,
                        (false, true) => Runtime.SignStorageKind.TrailingSeparate,
                        (false, false) => Runtime.SignStorageKind.TrailingOverpunch,
                    };
                }

                // VALUE clause — extract the first literal from the first valueItem
                var valClause = clause.valueClause();
                if (valClause != null)
                {
                    var items = valClause.valueItem();
                    var voCtx = items.Length > 0
                        ? (items[0].valueClauseRange()?.valueClauseOperand(0) ?? items[0].valueClauseOperand(0))
                        : null;
                    if (voCtx != null)
                    {
                        // valueClauseOperand: unaryExpression | nonNumericLiteral
                        // For numeric VALUE, find numericLiteral and detect unary minus
                        var unaryCtx = voCtx.unaryExpression();
                        var (numLit, isNegated) = FindNumericLiteralInUnary(unaryCtx);
                        if (numLit != null)
                        {
                            var text = NormalizeNumericLiteralText(numLit);
                            initialValue = isNegated ? "-" + text : text;
                        }
                        else
                        {
                            var nonNum = voCtx.nonNumericLiteral();
                            if (nonNum?.STRINGLIT() is { } slit)
                            {
                                var text = slit.GetText();
                                if (text.Length >= 2)
                                {
                                    char q = text[0];
                                    initialValue = text[1..^1].Replace(new string(q, 2), new string(q, 1));
                                }
                            }
                            else if (nonNum?.NATLIT() is { } nlit)
                            {
                                // National VALUE literal N"…": strip the N prefix and quotes, un-escape
                                // doubled quotes. The init emitter encodes it UTF-16 for the national item.
                                var text = nlit.GetText();
                                if (text.Length >= 3 && (text[0] is 'N' or 'n'))
                                {
                                    char q = text[1];
                                    initialValue = text[2..^1].Replace(new string(q, 2), new string(q, 1));
                                }
                            }
                            else if (nonNum?.BOOLLIT() is { } blit)
                            {
                                // Boolean VALUE literal B"0101": strip the B prefix and quotes (binary
                                // digits, no doubled-quote). Stored as the '0'/'1' characters.
                                var text = blit.GetText();
                                if (text.Length >= 3 && (text[0] is 'B' or 'b'))
                                    initialValue = text[2..^1];
                            }
                            else if (nonNum?.figurativeConstant() is { } fig)
                            {
                                string figText = fig.GetText().ToUpperInvariant();
                                // Strip ALL prefix — ANTLR concatenates tokens without space
                                // so "ALL ZEROS" becomes "ALLZEROS", "ALL SPACES" becomes "ALLSPACES"
                                if (figText.StartsWith("ALL"))
                                    figText = figText[3..];
                                // Handle ALL "X" (literal) — figText is now the quoted string
                                if (figText.Length >= 2 && (figText[0] == '"' || figText[0] == '\''))
                                {
                                    char q = figText[0];
                                    string allChar = figText[1..^1].Replace(new string(q, 2), new string(q, 1));
                                    // Store the ALL literal pattern; it will be expanded to fill
                                    // the PIC width when the field size is known (ExpandAllLiteralInit)
                                    _deferredAllLiteralPattern = allChar;
                                    initialValue = allChar;
                                    _deferredFigurativeInit = null;
                                }
                                else
                                {
                                    // For ZERO, keep as "0" so numeric VALUE parsing still works
                                    initialValue = figText switch
                                    {
                                        "SPACE" or "SPACES" => " ",
                                        "ZERO" or "ZEROS" or "ZEROES" => "0",
                                        "HIGH-VALUE" or "HIGH-VALUES" => null,
                                        "LOW-VALUE" or "LOW-VALUES" => null,
                                        "QUOTE" or "QUOTES" => null,
                                        _ => figText
                                    };
                                    // Store figurative kind for field-filling initialization
                                    _deferredFigurativeInit = figText switch
                                    {
                                        "SPACE" or "SPACES" => FigurativeKind.Space,
                                        "ZERO" or "ZEROS" or "ZEROES" => FigurativeKind.Zero,
                                        "HIGH-VALUE" or "HIGH-VALUES" => FigurativeKind.HighValue,
                                        "LOW-VALUE" or "LOW-VALUES" => FigurativeKind.LowValue,
                                        "QUOTE" or "QUOTES" => FigurativeKind.Quote,
                                        _ => null
                                    };
                                }
                            }
                        }
                    }
                }
            }
        }

        // Level-88 condition names: attach to parent data item, extract VALUE clauses
        if (level == 88)
        {
            // Parent is the most recent data item on the stack
            DataSymbol? parent = _dataStack.Count > 0 ? _dataStack.Peek() : null;
            if (parent == null)
                throw new InvalidOperationException(
                    $"Level-88 condition name '{displayName}' has no parent data item (line {line})");

            var condSym = new ConditionSymbol(displayName, parent, line);

            // Extract values from VALUE clause — now unified with THRU support via valueItem
            var condBody = ctx.dataDescriptionBody();
            if (condBody?.dataDescriptionClauses() != null)
            {
                foreach (var clause in condBody.dataDescriptionClauses().dataDescriptionClause())
                {
                    var valClause = clause.valueClause();
                    if (valClause != null)
                    {
                        foreach (var item in valClause.valueItem())
                        {
                            var rangeCtx = item.valueClauseRange();
                            if (rangeCtx != null)
                            {
                                var fromVal = ParseConditionClauseOperand(rangeCtx.valueClauseOperand(0));
                                var toVal = ParseConditionClauseOperand(rangeCtx.valueClauseOperand(1));
                                condSym.AddRange(fromVal, toVal);
                            }
                            else
                            {
                                foreach (var vco in item.valueClauseOperand())
                                {
                                    var fromVal = ParseConditionClauseOperand(vco);
                                    condSym.AddRange(fromVal, null);
                                }
                            }
                        }
                    }
                }
            }

            _symbols.Program.DataDivisionScope.TryDeclare(condSym, out _);
            return null;
        }

        // Extract OCCURS clause, BLANK WHEN ZERO, JUSTIFIED, EXTERNAL, GLOBAL from clauses
        OccursInfo? occursInfo = null;
        bool blankWhenZero = false;
        bool justifiedRight = false;
        bool isSynchronized = false;
        bool isExternal = false;
        bool isGlobal = false;
        bool isBased = false;
        if (body?.dataDescriptionClauses() != null)
        {
            foreach (var clause in body.dataDescriptionClauses().dataDescriptionClause())
            {
                var occClause = clause.occursClause();
                if (occClause != null)
                {
                    // Each occursBound is an integer literal or (2002+, greenfield-only) a constant-name;
                    // GetText() on the bound context is the literal text, and a constant-name simply fails
                    // TryParse below — COBOL-85 legacy input never carries one.
                    var intLits = occClause.occursBound();
                    int maxOccurs = 1;
                    int minOccurs = 0;
                    string? dependingOn = null;

                    if (intLits.Length > 0 && int.TryParse(intLits[0].GetText(), out int oc))
                        maxOccurs = oc;

                    // OCCURS m TO n DEPENDING ON: first int = min, second int = max
                    if (intLits.Length > 1 && int.TryParse(intLits[1].GetText(), out int oc2))
                    {
                        minOccurs = maxOccurs;
                        maxOccurs = oc2;
                    }
                    else
                    {
                        minOccurs = maxOccurs;
                    }

                    // DEPENDING ON data-name
                    var depRef = occClause.dataReference();
                    if (depRef != null)
                        dependingOn = depRef.GetText();

                    // ASCENDING/DESCENDING KEY data-names
                    var ascKeys = new List<string>();
                    var descKeys = new List<string>();
                    foreach (var keyClause in occClause.occursKeyClause())
                    {
                        bool isAscending = keyClause.ASCENDING() != null;
                        foreach (var keyRef in keyClause.dataReference())
                        {
                            string keyName = keyRef.GetText();
                            if (isAscending)
                                ascKeys.Add(keyName);
                            else
                                descKeys.Add(keyName);
                        }
                    }

                    // INDEXED BY names (captured here; index DataSymbols created below)
                    var indexNames = new List<string>();
                    if (occClause.dataReferenceList() is { } indexList)
                    {
                        foreach (var idCtx in indexList.dataReference())
                            indexNames.Add(idCtx.GetText());
                    }

                    occursInfo = new OccursInfo(
                        minOccurs, maxOccurs, dependingOn,
                        ascKeys.Count > 0 ? ascKeys : null,
                        descKeys.Count > 0 ? descKeys : null,
                        indexNames.Count > 0 ? indexNames : null);
                }

                if (clause.blankWhenZeroClause() != null)
                    blankWhenZero = true;

                if (clause.justifiedClause() != null)
                    justifiedRight = true;

                if (clause.syncClause() != null)
                    isSynchronized = true;

                if (clause.externalClause() != null)
                    isExternal = true;

                if (clause.globalClause() != null)
                    isGlobal = true;

                if (clause.basedClause() != null)
                    isBased = true;
            }
        }

        // §13.18.44 SR 1: REDEFINES must be the first clause after the entry-name.
        // Emit a warning (not error) since many mainframe programs violate this ordering.
        if (_deferredRedefinesName != null && body?.dataDescriptionClauses() != null)
        {
            var clauses = body.dataDescriptionClauses().dataDescriptionClause();
            if (clauses.Length > 0 && clauses[0].redefinesClause() == null)
            {
                _diagnostics.Report(DiagnosticDescriptors.CBL0808,
                    new Common.SourceLocation(_sourceName, 0, line, 0),
                    Common.TextSpan.Empty,
                    displayName);
            }
        }

        // Level-77 USAGE INDEX with no PIC: normalize to S9(9) COMP here (always elementary).
        // Sub-level USAGE INDEX items are handled at layout time by FieldSizeCalculator +
        // CompilerPicDescriptorFactory, since we don't know here whether they'll be groups.
        if (usage == Runtime.UsageKind.Index && picString == null && level == 77)
        {
            picString = "S9(9)";
            usage = Runtime.UsageKind.Comp;
        }

        // Create DataSymbol (REDEFINES resolved in pass 2 after all items registered)
        var data = new DataSymbol(internalName, displayName, level, picString, usage, typeName, redefines: null, line);
        data.HasExplicitUsage = hasExplicitUsage;
        data.IsUnsignedBinary = isUnsignedBinary;
        data.ObjectClassName = objectClassName;
        data.Occurs = occursInfo;
        data.IsJustifiedRight = justifiedRight;
        data.IsSynchronized = isSynchronized;
        data.IsExternal = isExternal;
        data.IsGlobal = isGlobal;
        data.IsBased = isBased;
        data.RedefinesName = _deferredRedefinesName;
        _deferredRedefinesName = null;
        data.ExplicitSignStorage = _deferredSignStorage;
        _deferredSignStorage = null;
        data.FigurativeInit = _deferredFigurativeInit;
        _deferredFigurativeInit = null;
        data.AllLiteralPattern = _deferredAllLiteralPattern;
        _deferredAllLiteralPattern = null;

        // Resolve PIC/USAGE → ITypeSymbol
        var diagBag = new DiagnosticBag();
        var picEnv = new Runtime.PicEnvironment(_currencySign, _currencyOutputChar, _decimalPointIsComma);
        data.ResolvedType = PicUsageResolver.ResolveForDataItem(
            displayName, picString, usage, diagBag, line, blankWhenZero, picEnv);
        foreach (var d in diagBag.Diagnostics)
            _diagnostics.Add(d);

        // PICTURE-validity (ISO §13.18.40.3 SR2/SR8): the runtime PIC parser silently skips unrecognized
        // symbols (its default arm), so a mixed picture like 9Q9 compiles with the Q swallowed. Flag the
        // illegal symbol. Dialect-gated to named-strict modes (staged like CBL3128) so the permissive
        // Default / --nist corpus is unaffected by construction. (DEVLOG 306)
        if (picString != null && _options.Config.IsStrict
            && PicUsageResolver.FindIllegalPicSymbol(picString, picEnv) is { } badSym)
        {
            _diagnostics.Report(DiagnosticDescriptors.CBL0814,
                new Common.SourceLocation(_sourceName, 0, line, 0), Common.TextSpan.Empty,
                picString, displayName, badSym);
        }

        data.InitialValue = initialValue;
        data.Area = _currentArea;

        // Build parent/child tree using level numbers
        if (level == 1 || level == 77)
        {
            // Root-level item
            _dataStack.Clear();
            _dataStack.Push(data);

            // Link 01-level record to FD file symbol (first 01 under an FD)
            if (_currentFdFile != null && _currentFdFile.Record == null && level == 1)
            {
                _currentFdFile.Record = data;

                // FD ... IS EXTERNAL (§13.18.22): the externalized entity is the file's record AREA, keyed
                // by the 01 record-NAME. Propagate the FD's EXTERNAL flag onto the record DataSymbol so
                // storage emission allocates a shared ExternalStorage byte[] for it — the same record name
                // in a separately-compiled program then resolves to the same bytes (IC227A record sharing).
                if (_currentFdFile.IsExternal)
                    data.IsExternal = true;
            }

            // Tag every FILE SECTION 01 record with its owning FD so storage layout can give
            // each file a distinct record buffer (records within one FD share; across FDs they
            // must not alias). _currentArea guards against tagging WORKING-STORAGE 01s.
            if (_currentFdFile != null && level == 1 && _currentArea == StorageAreaKind.FileSection)
            {
                data.OwningFile = _currentFdFile;
            }
        }
        else if (level == 66)
        {
            // RENAMES — extract FROM/THRU targets from renamesClause
            // Add to parent 01-level record's Children so qualified name resolution works
            // (FindChild traverses Children). The 01-level is at the bottom of the stack.
            if (_dataStack.Count > 0)
            {
                // Walk to the bottom of the stack (01-level root)
                var stackItems = _dataStack.ToArray(); // top-to-bottom order
                var root = stackItems[^1]; // bottom = 01-level
                root.AddChild(data);
            }
            // Do NOT clear the stack — subsequent level-66 items for the same
            // record need the stack intact to find their parent 01-level.
            var renamesCtx = body?.renamesClause();
            if (renamesCtx != null)
            {
                var refs = renamesCtx.dataReference();
                string fromName = refs.Length > 0 ? refs[0].cobolWord()?.GetText() ?? "" : "";
                string? fromQual = refs.Length > 0 ? ExtractQualifier(refs[0]) : null;
                string? thruName = refs.Length > 1 ? refs[1].cobolWord()?.GetText() : null;
                string? thruQual = refs.Length > 1 ? ExtractQualifier(refs[1]) : null;
                data.Renames = new RenamesInfo(fromName, thruName, fromQual, thruQual);
            }
        }
        else
        {
            // Subordinate: pop stack until we find a parent with lower level
            while (_dataStack.Count > 0 && _dataStack.Peek().LevelNumber >= level)
                _dataStack.Pop();

            if (_dataStack.Count > 0)
                _dataStack.Peek().AddChild(data);

            _dataStack.Push(data);
        }

        // Add to declaration-order list (preserves ALL items including FILLERs)
        _dataItemsInOrder.Add(data);

        // Declare in scope (for name resolution)
        _symbols.Program.DataDivisionScope.TryDeclare(data, out _);

        // Declare INDEXED BY index-names from OCCURS clause.
        // INDEX items are implicit level-77 items stored as PIC S9(9) COMP
        // holding 1-based element numbers.
        if (body?.dataDescriptionClauses() != null)
        {
            foreach (var clause in body.dataDescriptionClauses().dataDescriptionClause())
            {
                var occClause = clause.occursClause();
                if (occClause?.dataReferenceList() is { } indexList)
                {
                    foreach (var idCtx in indexList.dataReference())
                    {
                        string indexName = idCtx.cobolWord().GetText();
                        var indexSym = new DataSymbol(indexName, indexName, 77,
                            "S9(9)", Runtime.UsageKind.Comp, null, null, ctx.Start.Line);
                        indexSym.HasExplicitUsage = true;
                        // An index-name (from INDEXED BY) is implementation storage owned by
                        // THIS program, never a passable USING parameter — so it always needs
                        // real backing storage in WORKING-STORAGE, regardless of where the
                        // associated OCCURS table is declared. A LINKAGE/FILE-SECTION table's
                        // index-name must NOT inherit that section's area: LINKAGE has no backing
                        // storage (its area is bound only for actual USING params, so a
                        // non-parameter index-name resolves to a null area → NRE in the encode
                        // path), and FILE-SECTION storage is the shared record buffer. (ISO §8.5.1.2:
                        // an index-name names a compiler-allocated index, distinct from any data item.)
                        indexSym.Area = StorageAreaKind.WorkingStorage;
                        // Resolve PIC for the INDEX item so it gets proper storage layout
                        var idxDiagBag = new DiagnosticBag();
                        var idxPicEnv = new Runtime.PicEnvironment(_currencySign, _currencyOutputChar, _decimalPointIsComma);
                        indexSym.ResolvedType = PicUsageResolver.ResolveForDataItem(
                            indexName, "S9(9)", Runtime.UsageKind.Comp, idxDiagBag, ctx.Start.Line, false, idxPicEnv);
                        _dataItemsInOrder.Add(indexSym);
                        _symbols.Program.DataDivisionScope.TryDeclare(indexSym, out _);
                    }
                }
            }
        }

        return null;
    }

    // ── Procedure Division ──

    // Captured during VisitProcedureDivision for use by Compilation.BuildSemanticModel
    private readonly List<string> _procedureUsingNames = [];
    private string? _procedureReturningName;

    /// <summary>Ordered USING parameter names from PROCEDURE DIVISION USING.</summary>
    public IReadOnlyList<string> ProcedureUsingNames => _procedureUsingNames;
    /// <summary>RETURNING parameter name from PROCEDURE DIVISION RETURNING.</summary>
    public string? ProcedureReturningName => _procedureReturningName;

    /// <summary>OO (ISO §11.7): a METHOD-ID definition. Each method gets its OWN paragraph scope so sibling methods
    /// may reuse paragraph names (e.g. every method has a MAIN) without colliding, and a method's PERFORM/GO TO
    /// resolves within its own paragraphs. The method's PROCEDURE DIVISION USING/RETURNING names (+ trailing
    /// RETURNING) are captured for the IR method roster (the INVOKE ManagedPointer[] ABI order).</summary>
    public override object? VisitMethodDefinition(CobolParserCore.MethodDefinitionContext ctx)
    {
        var nameCtxs = ctx.methodName();
        string methodName = nameCtxs is { Length: > 0 } ? nameCtxs[0].GetText() : "";
        var scope = new Scope(ScopeKind.Section, _symbols.Program.ProcedureDivisionScope);

        // USING data-names + the trailing RETURNING name, in ABI order (mirrors how the Binder orders
        // ProcedureUsingParameters then the RETURNING item).
        var usingNames = new List<string>();
        var pd = ctx.procedureDivision();
        // The usingClause is per-parameter since the greenfield P10 Step-10 BY VALUE header grammar; the
        // legacy oracle treats every parameter as BY REFERENCE (its corpus predates BY VALUE headers).
        if (pd?.usingClause()?.usingParameter() is { } prms)
            foreach (var prm in prms)
            {
                var dr = prm.usingByReference()?.dataReference()
                    ?? prm.usingByValue()?.dataReference()
                    ?? prm.dataReference();
                usingNames.Add(dr.cobolWord().GetText());
            }
        if (pd?.returningClause()?.dataReference() is { } ret)
            usingNames.Add(ret.cobolWord().GetText());

        _classMethodScopes.Add((methodName, scope, usingNames));

        var prevMethodScope = _currentMethodScope;
        _currentMethodScope = scope;
        var result = base.VisitMethodDefinition(ctx);
        _currentMethodScope = prevMethodScope;
        return result;
    }

    public override object? VisitProcedureDivision(CobolParserCore.ProcedureDivisionContext ctx)
    {
        // Parse PROCEDURE DIVISION USING data-name-1 data-name-2 ... (per-parameter since the greenfield
        // P10 Step-10 BY VALUE header grammar; the legacy oracle treats every parameter as BY REFERENCE —
        // its corpus predates BY VALUE headers).
        if (ctx.usingClause() is { } usingCtx)
        {
            foreach (var prm in usingCtx.usingParameter())
            {
                var dr = prm.usingByReference()?.dataReference()
                    ?? prm.usingByValue()?.dataReference()
                    ?? prm.dataReference();
                _procedureUsingNames.Add(dr.cobolWord().GetText());
            }
        }

        // Parse PROCEDURE DIVISION RETURNING data-name (COBOL-2002+)
        if (ctx.returningClause() is { } retCtx)
        {
            _procedureReturningName = retCtx.dataReference().cobolWord().GetText();
        }

        // Inside a METHOD-ID, the method's paragraphs declare into the per-method scope (so sibling methods may
        // reuse paragraph names); otherwise the program-wide ProcedureDivisionScope.
        using var _ = _symbols.PushScope(_currentMethodScope ?? _symbols.Program.ProcedureDivisionScope);
        return base.VisitProcedureDivision(ctx);
    }

    public override object? VisitSectionDefinition(CobolParserCore.SectionDefinitionContext ctx)
    {
        var nameCtx = ctx.sectionName();
        if (nameCtx == null) return base.VisitSectionDefinition(ctx);

        string name = nameCtx.GetText();

        // OO (ISO §11.7): a SECTION inside a METHOD-ID is not yet method-scoped — its paragraphs would land in the
        // section scope (parented to the program-wide ProcedureDivisionScope), so they'd be excluded from the
        // method's dispatch range and SILENTLY SKIPPED at run time (adversarial review, DEVLOG 455). Reject loudly
        // and skip the section rather than emit wrong output; method-scoped sections are a later OO slice.
        if (_currentMethodScope != null)
        {
            Error(ctx, DiagnosticDescriptors.COBOL0116, name);
            return DefaultResult;
        }

        var section = new SectionSymbol(name,
            _symbols.Program.ProcedureDivisionScope, ctx.Start.Line);

        _symbols.Program.ProcedureDivisionScope.TryDeclare(section, out _);

        _currentSectionName = name;
        using var scopeGuard = _symbols.PushScope(section.Scope);
        foreach (var para in ctx.paragraphDefinition())
            VisitParagraphDefinition(para);
        _currentSectionName = null;

        return null;
    }

    public override object? VisitDeclarativeSection(CobolParserCore.DeclarativeSectionContext ctx)
    {
        var nameCtx = ctx.sectionName();
        if (nameCtx == null) return base.VisitDeclarativeSection(ctx);

        string name = nameCtx.GetText();
        var section = new SectionSymbol(name,
            _symbols.Program.ProcedureDivisionScope, ctx.Start.Line);

        _symbols.Program.ProcedureDivisionScope.TryDeclare(section, out _);

        _currentSectionName = name;
        using var scopeGuard = _symbols.PushScope(section.Scope);

        // Visit declarative paragraphs within this section
        foreach (var para in ctx.declarativeParagraph())
            VisitDeclarativeParagraph(para);

        _currentSectionName = null;
        return null;
    }

    public override object? VisitDeclarativeParagraph(CobolParserCore.DeclarativeParagraphContext ctx)
    {
        var nameCtx = ctx.paragraphName();
        if (nameCtx == null) return base.VisitDeclarativeParagraph(ctx);

        string name = nameCtx.GetText();
        var paragraph = new ParagraphSymbol(name, _symbols.CurrentScope, ctx.Start.Line);

        _symbols.CurrentScope.TryDeclare(paragraph, out _);
        if (_symbols.CurrentScope != _symbols.Program.ProcedureDivisionScope)
            _symbols.Program.ProcedureDivisionScope.TryDeclare(paragraph, out _);

        // Track section membership
        if (_currentSectionName != null)
        {
            if (!_sectionParagraphs.TryGetValue(_currentSectionName, out var list))
            {
                list = new List<string>();
                _sectionParagraphs[_currentSectionName] = list;
            }
            list.Add(name);
        }

        using var paraScope = _symbols.PushScope(paragraph.Scope);
        foreach (var sentence in ctx.sentence())
            foreach (var stmt in sentence.statement())
                Visit(stmt);

        return null;
    }

    public override object? VisitParagraphDefinition(CobolParserCore.ParagraphDefinitionContext ctx)
    {
        var nameCtx = ctx.paragraphName();
        if (nameCtx == null) return base.VisitParagraphDefinition(ctx);

        string name = nameCtx.GetText();
        var paragraph = new ParagraphSymbol(name, _symbols.CurrentScope, ctx.Start.Line);

        _symbols.CurrentScope.TryDeclare(paragraph, out _);
        // Also declare in ProcedureDivisionScope for global resolution, but only for a SECTION paragraph (not when
        // already in ProcedureDivisionScope, and NOT inside a METHOD-ID — a method's paragraphs are method-local, so
        // declaring them program-wide would (a) collide across sibling methods that reuse a name like MAIN and
        // (b) overwrite DeclaringScope, breaking the per-method roster grouping). OO §11.7.
        if (_currentMethodScope == null && _symbols.CurrentScope != _symbols.Program.ProcedureDivisionScope)
            _symbols.Program.ProcedureDivisionScope.TryDeclare(paragraph, out _);

        // Track section membership
        if (_currentSectionName != null)
        {
            if (!_sectionParagraphs.TryGetValue(_currentSectionName, out var list))
            {
                list = new List<string>();
                _sectionParagraphs[_currentSectionName] = list;
            }
            list.Add(name);
        }

        using var paraScope = _symbols.PushScope(paragraph.Scope);
        foreach (var sentence in ctx.sentence())
            foreach (var stmt in sentence.statement())
                Visit(stmt);

        return null;
    }

    /// <summary>
    /// Parse a literal context into a typed value for level-88 condition entries.
    /// Returns decimal for numeric literals, string for string literals.
    /// </summary>
    private static object ParseConditionLiteralValue(Generated.CobolParserCore.LiteralContext lit)
    {
        var numLit = lit.numericLiteral();
        if (numLit != null)
        {
            var text = NormalizeNumericLiteralText(numLit);
            if (decimal.TryParse(text,
                System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
                return d;
            return text;
        }
        var nonNum = lit.nonNumericLiteral();
        if (nonNum?.STRINGLIT() is { } slit)
        {
            var text = slit.GetText();
            if (text.Length >= 2)
            {
                char q = text[0];
                return text[1..^1].Replace(new string(q, 2), new string(q, 1));
            }
            return text;
        }
        return lit.GetText();
    }

    /// <summary>
    /// Parse a valueOperand context into a typed value for level-88 condition entries.
    /// Navigates through the arithmeticExpression chain to reach numericLiteral for numeric values.
    /// </summary>
    private static object ParseConditionValueOperand(Generated.CobolParserCore.ValueOperandContext vo)
    {
        var nonNum = vo.nonNumericLiteral();
        if (nonNum?.STRINGLIT() is { } slit)
        {
            var text = slit.GetText();
            if (text.Length >= 2)
            {
                char q = text[0];
                return text[1..^1].Replace(new string(q, 2), new string(q, 1));
            }
            return text;
        }
        if (nonNum?.figurativeConstant() is { } fig2)
        {
            var figText = fig2.GetText().ToUpperInvariant();
            if (figText.StartsWith("ALL")) figText = figText[3..];
            return figText switch
            {
                "ZERO" or "ZEROS" or "ZEROES" => (object)0m,
                "SPACE" or "SPACES" => " ",
                "QUOTE" or "QUOTES" => "\"",
                "HIGH-VALUE" or "HIGH-VALUES" => "\xFF",
                "LOW-VALUE" or "LOW-VALUES" => "\x00",
                _ => nonNum.GetText()
            };
        }

        // Navigate arithmeticExpression chain to reach numericLiteral (handles unary minus)
        var (numLit, isNegated) = FindNumericLiteralInArith(vo.arithmeticExpression());
        if (numLit != null)
        {
            var text = NormalizeNumericLiteralText(numLit);
            if (isNegated) text = "-" + text;
            if (decimal.TryParse(text,
                System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
                return d;
            return text;
        }
        return vo.GetText();
    }

    /// <summary>
    /// Navigate the arithmeticExpression chain to find a numericLiteral in a VALUE clause context.
    /// Returns the numericLiteral and whether a unary MINUS wraps it (for signed VALUE like -42).
    /// </summary>
    private static (Generated.CobolParserCore.NumericLiteralContext? numLit, bool isNegated)
        FindNumericLiteralInArith(Generated.CobolParserCore.ArithmeticExpressionContext? arith)
    {
        if (arith == null) return (null, false);
        var unary = arith.additiveExpression()?.multiplicativeExpression(0)
            ?.powerExpression(0)?.unaryExpression(0);
        if (unary == null) return (null, false);

        // Check for unary sign: addOp unaryExpression (PLUS or MINUS)
        bool negated = false;
        var addOp = unary.addOp();
        if (addOp != null)
        {
            if (addOp.MINUS() != null)
                negated = true;
            // Both +N and -N have a nested unaryExpression containing the literal
            unary = unary.unaryExpression();
        }
        var numLit = unary?.primaryExpression()?.numericLiteral();
        return (numLit, negated);
    }

    private static (Generated.CobolParserCore.NumericLiteralContext? numLit, bool isNegated)
        FindNumericLiteralInUnary(Generated.CobolParserCore.UnaryExpressionContext? unary)
    {
        if (unary == null) return (null, false);
        bool negated = false;
        var addOp = unary.addOp();
        if (addOp != null)
        {
            if (addOp.MINUS() != null) negated = true;
            unary = unary.unaryExpression();
        }
        var numLit = unary?.primaryExpression()?.numericLiteral();
        return (numLit, negated);
    }

    private static ConditionValue ParseConditionClauseOperand(Generated.CobolParserCore.ValueClauseOperandContext vo)
    {
        var nonNum = vo.nonNumericLiteral();
        if (nonNum?.STRINGLIT() is { } slit)
        {
            var text = slit.GetText();
            if (text.Length >= 2)
            {
                char q = text[0];
                return ConditionValue.FromString(text[1..^1].Replace(new string(q, 2), new string(q, 1)));
            }
            return ConditionValue.FromString(text);
        }
        if (nonNum?.figurativeConstant() is { } fig)
        {
            // ALL STRINGLIT: the pattern string must be repeated to fill parent at comparison time
            if (fig.ALL() != null && fig.allLiteral()?.STRINGLIT() is { } allSlit)
            {
                var allText = allSlit.GetText();
                if (allText.Length >= 2)
                {
                    char q = allText[0];
                    return ConditionValue.FromAllString(allText[1..^1].Replace(new string(q, 2), new string(q, 1)));
                }
                return ConditionValue.FromAllString(allText);
            }

            var figText = fig.GetText().ToUpperInvariant();
            if (figText.StartsWith("ALL")) figText = figText[3..];
            // Character figuratives assume the size of the parent field — i.e. the
            // single character repeated to fill it (ISO/IEC 1989:2023 §8.3.3.6.4 r2). Mark them
            // IsAllLiteral so the lowerer expands the pattern to the parent's length;
            // otherwise QUOTE on a PIC X(4) field would compare a 1-char '"' (space
            // padded) against the 4-char field and never match. ZERO stays numeric so
            // it compares correctly against numeric condition-name parents.
            return figText switch
            {
                "ZERO" or "ZEROS" or "ZEROES" => ConditionValue.FromNumeric(0m),
                "SPACE" or "SPACES" => ConditionValue.FromAllString(" "),
                "QUOTE" or "QUOTES" => ConditionValue.FromAllString("\""),
                "HIGH-VALUE" or "HIGH-VALUES" => ConditionValue.FromAllString("\xFF"),
                "LOW-VALUE" or "LOW-VALUES" => ConditionValue.FromAllString("\x00"),
                _ => ConditionValue.FromString(nonNum.GetText())
            };
        }

        var (numLit, isNegated) = FindNumericLiteralInUnary(vo.unaryExpression());
        if (numLit != null)
        {
            var text = NormalizeNumericLiteralText(numLit);
            if (isNegated) text = "-" + text;
            if (decimal.TryParse(text,
                System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
                return ConditionValue.FromNumeric(d);
            return ConditionValue.FromString(text);
        }
        return ConditionValue.FromString(vo.GetText());
    }

    /// <summary>
    /// Normalize a numericLiteral parse tree into a string suitable for decimal.Parse
    /// with InvariantCulture. Replaces comma decimal points with dots.
    /// </summary>
    internal static string NormalizeNumericLiteralText(Generated.CobolParserCore.NumericLiteralContext numLit)
    {
        var signed = numLit.signedNumericLiteral();
        var core = signed.numericLiteralCore();
        var sign = signed.MINUS() != null ? "-" : "";

        // DECIMALLIT from the lexer (dot-based: 123.45 or .45)
        if (core.DECIMALLIT() != null)
            return sign + core.DECIMALLIT().GetText();

        // COMMA-based decimals (for DECIMAL-POINT IS COMMA)
        var integers = core.INTEGERLIT();
        if (core.COMMA() != null && integers.Length == 2)
            return sign + integers[0].GetText() + "." + integers[1].GetText();
        if (core.COMMA() != null && integers.Length == 1)
            return sign + "." + integers[0].GetText();

        // Plain integer
        return sign + integers[0].GetText();
    }

    // ═══════════════════════════════════
    // Generic clause capture (vendor extensions)
    // ═══════════════════════════════════

    // kb/Work PB829 unified SIX vendor/extension catch-all productions into the ONE `unrecognizedClause` error
    // production the greenfield refuses by name (COBOLNET1941/1970/1971, Validation/ClosedFormatPass.cs). The
    // legacy tree keeps CAPTURING every instance as an extension clause — its behaviour is frozen until the P15
    // cut-over — so the five per-site overrides collapse into this one, which reads the same fact they encoded
    // in their names: the extension's context is the alternative list the production was written into.
    public override object? VisitUnrecognizedClause(CobolParserCore.UnrecognizedClauseContext ctx)
    {
        GenericClauseContext? where = ctx.Parent switch
        {
            CobolParserCore.IdentificationParagraphContext => GenericClauseContext.IdentificationParagraph,
            CobolParserCore.ConfigurationParagraphContext => GenericClauseContext.ConfigurationVendor,
            CobolParserCore.SpecialNameEntryContext => GenericClauseContext.SpecialNames,
            // The FD and SD arms shared ONE production (`genericFileDescriptionClause`) before the unification,
            // so both kept the FileDescription context; preserved verbatim rather than split.
            CobolParserCore.FileDescriptionClauseContext => GenericClauseContext.FileDescription,
            CobolParserCore.SortMergeDescriptionClauseContext => GenericClauseContext.FileDescription,
            CobolParserCore.DataDescriptionClauseContext => GenericClauseContext.DataDescription,
            CobolParserCore.FileControlClausesContext => GenericClauseContext.FileControl,
            CobolParserCore.IoControlClauseContext => GenericClauseContext.IOControl,
            _ => null,
        };
        if (where is { } w) CaptureGenericClause(ctx.genericClause(), w);
        return base.VisitUnrecognizedClause(ctx);
    }

    public override object? VisitIoControlClause(
        CobolParserCore.IoControlClauseContext ctx)
    {
        // SAME [RECORD] AREA (ISO §12.4.6.4): the named files share one record storage area, so their 01
        // records must be laid out at a single base (reading one file's record overwrites the others' —
        // IX206A). SAME SORT / SORT-MERGE AREA concerns sort work areas, not record areas, so it is ignored
        // (CobolSharp sorts in memory). MULTIPLE FILE is a tape hint with no effect.
        if (ctx.sameClause() is { } same && same.SORT() == null && same.SORT_MERGE() == null)
            RecordSameRecordAreaGroup(same);

        // MULTIPLE FILE TAPE is an obsolete element (removed in COBOL-2002); flag it under cobol85/Default.
        // No runtime effect (it is a tape-positioning hint). Satisfies the NIST SQ303M OBSOLETE flagging module.
        if (ctx.multipleFileClause() is { } mft && !_options.Config.IsCobol2002OrLater)
        {
            var tok = mft.Start;
            _diagnostics.Report(DiagnosticDescriptors.CBL3607,
                new Common.SourceLocation(_sourceName, 0, tok.Line, tok.Column),
                new Common.TextSpan(tok.StartIndex, mft.Stop?.StopIndex ?? tok.StopIndex),
                "MULTIPLE FILE TAPE");
        }

        return base.VisitIoControlClause(ctx);
    }

    /// <summary>Union the files of a SAME [RECORD] AREA clause into one record-area group by stamping each
    /// FileSymbol's <see cref="FileSymbol.SameRecordAreaLeader"/> with a shared representative name. If any
    /// file is already in a group, that group's leader is adopted and the groups merge (re-pointing every
    /// file that referenced the absorbed leader), so chained SAME clauses coalesce.</summary>
    private void RecordSameRecordAreaGroup(CobolParserCore.SameClauseContext same)
    {
        var files = same.fileName()
            .Select(fn => _symbols.Program.GlobalScope.Resolve<FileSymbol>(fn.GetText()))
            .Where(f => f != null).Cast<FileSymbol>().ToList();
        if (files.Count < 2) return; // a single file shares with nothing

        string leader = files.Select(f => f.SameRecordAreaLeader).FirstOrDefault(l => l != null)
            ?? files[0].Name;
        var allFiles = _symbols.Program.GlobalScope.GetAllSymbols<FileSymbol>().ToList();
        foreach (var f in files)
        {
            string? old = f.SameRecordAreaLeader;
            if (old != null && !string.Equals(old, leader, StringComparison.OrdinalIgnoreCase))
                foreach (var g in allFiles)
                    if (string.Equals(g.SameRecordAreaLeader, old, StringComparison.OrdinalIgnoreCase))
                        g.SameRecordAreaLeader = leader;
            f.SameRecordAreaLeader = leader;
        }
    }

    // ── SCREEN SECTION ──

    public override object? VisitScreenSection(CobolParserCore.ScreenSectionContext ctx)
    {
        var rootItems = new List<Bound.BoundScreenItem>();
        var stack = new Stack<Bound.BoundScreenItem>();

        foreach (var entryCtx in ctx.screenDescriptionEntry())
        {
            var levelText = entryCtx.levelNumber()?.GetText();
            if (!int.TryParse(levelText, out int level))
                continue;

            var name = entryCtx.screenName()?.GetText();
            if (string.Equals(name, "FILLER", StringComparison.OrdinalIgnoreCase))
                name = null;

            var item = new Bound.BoundScreenItem { Name = name, Level = level };

            // Parse screen clauses
            if (entryCtx.screenDescriptionBody() is { } body)
            {
                foreach (var clause in body.screenClause())
                {
                    if (clause.screenLineClause() is { } lineClause)
                    {
                        bool isPlus = lineClause.PLUS() != null;
                        int val = 1;
                        if (lineClause.integerLiteral() is { } intLit)
                            int.TryParse(intLit.GetText(), out val);
                        item.Line = new Bound.ScreenPosition(isPlus, val);
                    }
                    else if (clause.screenColumnClause() is { } colClause)
                    {
                        bool isPlus = colClause.PLUS() != null;
                        int val = 1;
                        if (colClause.integerLiteral() is { } intLit)
                            int.TryParse(intLit.GetText(), out val);
                        item.Column = new Bound.ScreenPosition(isPlus, val);
                    }
                    else if (clause.screenBlankClause() is { } blankClause)
                    {
                        if (blankClause.SCREEN() != null)
                            item.BlankScreen = true;
                        else if (blankClause.LINE() != null)
                            item.BlankLine = true;
                    }
                    else if (clause.screenEraseClause() is { } eraseClause)
                    {
                        if (eraseClause.EOL() != null)
                            item.EraseEol = true;
                        else if (eraseClause.EOS() != null)
                            item.EraseEos = true;
                    }
                    else if (clause.screenBellClause() != null) item.Bell = true;
                    else if (clause.screenBlinkClause() != null) item.Blink = true;
                    else if (clause.screenHighlightClause() != null) item.Highlight = true;
                    else if (clause.screenLowlightClause() != null) item.Lowlight = true;
                    else if (clause.screenReverseVideoClause() != null) item.ReverseVideo = true;
                    else if (clause.screenUnderlineClause() != null) item.Underline = true;
                    else if (clause.screenAutoClause() != null) item.Auto = true;
                    else if (clause.screenSecureClause() != null) item.Secure = true;
                    else if (clause.screenFullClause() != null) item.Full = true;
                    else if (clause.screenRequiredClause() != null) item.Required = true;
                    else if (clause.screenForegroundColorClause() is { } fgClause)
                    {
                        if (fgClause.integerLiteral() is { } intLit && int.TryParse(intLit.GetText(), out int fg))
                            item.ForegroundColor = fg;
                    }
                    else if (clause.screenBackgroundColorClause() is { } bgClause)
                    {
                        if (bgClause.integerLiteral() is { } intLit && int.TryParse(intLit.GetText(), out int bg))
                            item.BackgroundColor = bg;
                    }
                    else if (clause.pictureClause() is { } picClause)
                    {
                        item.PicString = picClause.PIC_STRING()?.GetText();
                    }
                    else if (clause.screenFromClause() is { } fromClause)
                    {
                        item.FromSource = fromClause.dataReference()?.GetText()
                                          ?? fromClause.literal()?.GetText();
                    }
                    else if (clause.screenToClause() is { } toClause)
                    {
                        item.ToTarget = toClause.dataReference()?.GetText();
                    }
                    else if (clause.screenUsingClause() is { } usingClause)
                    {
                        item.UsingField = usingClause.dataReference()?.GetText();
                    }
                    else if (clause.valueClause() is { } valClause)
                    {
                        var valItems = valClause.valueItem();
                        if (valItems.Length > 0)
                        {
                            var operands = valItems[0].valueClauseOperand();
                            if (operands != null && operands.Length > 0)
                                item.Value = operands[0].GetText();
                        }
                    }
                }

                // Validate mutual exclusivity
                if (item.Highlight && item.Lowlight)
                    Error(entryCtx, DiagnosticDescriptors.CBL3126);
                if (item.UsingField != null && (item.FromSource != null || item.ToTarget != null))
                    Error(entryCtx, DiagnosticDescriptors.CBL3127);
            }

            // Build hierarchy using level numbers (same pattern as data items)
            while (stack.Count > 0 && stack.Peek().Level >= level)
                stack.Pop();

            if (stack.Count > 0)
                stack.Peek().AddChild(item);
            else
                rootItems.Add(item);

            stack.Push(item);
        }

        _screenItems.AddRange(rootItems);
        return null;
    }
}
