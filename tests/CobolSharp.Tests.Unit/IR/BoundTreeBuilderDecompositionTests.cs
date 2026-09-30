// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
//
// M004 Stage 1: Structural tests verifying that all binding infrastructure
// exists, BoundTreeBuilder still contains all methods, and BindingContext exposes
// all required fields and binder references.
using System.Reflection;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics.Bound;
using CobolSharp.Compiler.Semantics.Bound.Binding;
using Xunit;

namespace CobolSharp.Tests.Unit.IR;

public class BoundTreeBuilderDecompositionTests
{
    private static readonly Assembly CompilerAssembly = typeof(IrInstruction).Assembly;

    // ── All 10 binding classes exist ──

    [Theory]
    [InlineData("CobolSharp.Compiler.Semantics.Bound.Binding.BindingContext")]
    [InlineData("CobolSharp.Compiler.Semantics.Bound.Binding.ProcedureNameResolver")]
    [InlineData("CobolSharp.Compiler.Semantics.Bound.Binding.ExpressionBinder")]
    [InlineData("CobolSharp.Compiler.Semantics.Bound.Binding.ConditionBinder")]
    [InlineData("CobolSharp.Compiler.Semantics.Bound.Binding.ArithmeticStatementBinder")]
    [InlineData("CobolSharp.Compiler.Semantics.Bound.Binding.DataStatementBinder")]
    [InlineData("CobolSharp.Compiler.Semantics.Bound.Binding.ControlFlowBinder")]
    [InlineData("CobolSharp.Compiler.Semantics.Bound.Binding.FileIoBinder")]
    [InlineData("CobolSharp.Compiler.Semantics.Bound.Binding.CallBinder")]
    [InlineData("CobolSharp.Compiler.Semantics.Bound.Binding.StringStatementBinder")]
    public void Binding_class_exists(string typeName)
    {
        var type = CompilerAssembly.GetType(typeName);
        Assert.NotNull(type);
        Assert.True(type!.IsClass);
        Assert.True(type.IsSealed, $"{typeName} should be sealed");
    }

    // ── All binding classes are internal, not public ──

    [Theory]
    [InlineData(typeof(BindingContext))]
    [InlineData(typeof(ProcedureNameResolver))]
    [InlineData(typeof(ExpressionBinder))]
    [InlineData(typeof(ConditionBinder))]
    [InlineData(typeof(ArithmeticStatementBinder))]
    [InlineData(typeof(DataStatementBinder))]
    [InlineData(typeof(ControlFlowBinder))]
    [InlineData(typeof(FileIoBinder))]
    [InlineData(typeof(CallBinder))]
    [InlineData(typeof(StringStatementBinder))]
    public void Binding_class_is_internal(Type type)
    {
        Assert.False(type.IsPublic, $"{type.Name} should be internal, not public");
    }

    // ── BindingContext exposes all required state properties ──

    [Theory]
    [InlineData("Semantic", typeof(CobolSharp.Compiler.Semantics.SemanticModel))]
    [InlineData("Diagnostics", typeof(CobolNet.Frontend.Diagnostics.DiagnosticBag))]
    [InlineData("Options", typeof(CobolSharp.Compiler.Semantics.CompilationOptions))]
    public void BindingContext_has_state_property(string propName, Type expectedType)
    {
        var prop = typeof(BindingContext).GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.Equal(expectedType, prop!.PropertyType);
    }

    [Fact]
    public void BindingContext_has_Paragraphs_property()
    {
        var prop = typeof(BindingContext).GetProperty("Paragraphs",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
    }

    [Fact]
    public void BindingContext_has_AlphanumericFunctions_static()
    {
        var field = typeof(BindingContext).GetField("AlphanumericFunctions",
            BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(field);
    }

    // ── BindingContext references all binders ──

    [Theory]
    [InlineData("ProcedureName", typeof(ProcedureNameResolver))]
    [InlineData("Expression", typeof(ExpressionBinder))]
    [InlineData("Condition", typeof(ConditionBinder))]
    [InlineData("Arithmetic", typeof(ArithmeticStatementBinder))]
    [InlineData("Data", typeof(DataStatementBinder))]
    [InlineData("ControlFlow", typeof(ControlFlowBinder))]
    [InlineData("FileIo", typeof(FileIoBinder))]
    [InlineData("Call", typeof(CallBinder))]
    [InlineData("String", typeof(StringStatementBinder))]
    public void BindingContext_has_binder_reference(string propName, Type expectedType)
    {
        var prop = typeof(BindingContext).GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.Equal(expectedType, prop!.PropertyType);
    }

    // ── BindingContext has delegates ──

    [Fact]
    public void BindingContext_has_BindStatement_delegate()
    {
        var prop = typeof(BindingContext).GetProperty("BindStatement",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
    }

    [Fact]
    public void BindingContext_has_Typed_delegate()
    {
        var prop = typeof(BindingContext).GetProperty("Typed",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
    }

    // ── BoundTreeBuilder has _ctx field ──

    [Fact]
    public void BoundTreeBuilder_has_BindingContext_field()
    {
        var field = typeof(BoundTreeBuilder).GetField("_ctx",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(field);
        Assert.Equal(typeof(BindingContext), field!.FieldType);
    }

    // ── Stage 5: BoundTreeBuilder contains ONLY orchestration methods ──

    [Theory]
    [InlineData("Build")]
    [InlineData("BindStatement")]
    [InlineData("VisitParagraphDefinition")]
    [InlineData("VisitDeclarativeSection")]
    [InlineData("VisitDeclarativeParagraph")]
    public void BoundTreeBuilder_contains_orchestration_method(string methodName)
    {
        var methods = typeof(BoundTreeBuilder).GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 5: BoundTreeBuilder has NO forwarding wrappers ──

    [Fact]
    public void BoundTreeBuilder_has_no_forwarding_wrappers()
    {
        var movedMethods = new HashSet<string>
        {
            "BindDisplay", "BindMove", "BindPerform", "BindIf", "BindEvaluate",
            "BindCall", "BindCondition", "BindComparison", "BindWrite", "BindRead",
            "BindSort", "BindString", "BindUnstring", "BindInspect",
            "BindDataReferenceWithSubscripts", "BindFunctionCall",
            "ResolveProcedureName", "BindGoTo", "BindAlter", "BindAccept",
            "BindMultiply", "BindAdd", "BindSubtract", "BindDivide", "BindCompute",
            "BindSearch", "BindSearchAll", "BindOpen", "BindClose",
        };
        var methods = typeof(BoundTreeBuilder).GetMethods(
            BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var m in methods)
            Assert.DoesNotContain(m.Name, movedMethods);
    }

    // ── Stage 3: ConditionBinder contains moved methods ──

    [Theory]
    [InlineData("BindCondition")]
    [InlineData("BindLogicalOr")]
    [InlineData("BindLogicalAnd")]
    [InlineData("BindAbbreviatedRelation")]
    [InlineData("BindSucceedingCondition")]
    [InlineData("BindUnaryLogical")]
    [InlineData("BindPrimaryCondition")]
    [InlineData("BindSignConditionFromComparison")]
    [InlineData("BindComparison")]
    [InlineData("ParseComparisonOperator")]
    [InlineData("NegateOperator")]
    [InlineData("ExpandAbbreviatedConditions")]
    [InlineData("ExpandAbbrev")]
    [InlineData("ExtractContext")]
    [InlineData("IsRelational")]
    [InlineData("IsArithmeticOp")]
    [InlineData("BindComparisonOperand")]
    [InlineData("TryResolveConditionName")]
    public void ConditionBinder_contains_method(string methodName)
    {
        var type = typeof(ConditionBinder);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 3: ArithmeticStatementBinder contains moved methods ──

    [Theory]
    [InlineData("BindMultiply")]
    [InlineData("BindAdd")]
    [InlineData("BindSubtract")]
    [InlineData("BindDivide")]
    [InlineData("BindCompute")]
    [InlineData("BindCorresponding")]
    [InlineData("ValidatedArithmetic")]
    [InlineData("BindArithmeticTargets")]
    [InlineData("BindSizeErrorClause")]
    public void ArithmeticStatementBinder_contains_method(string methodName)
    {
        var type = typeof(ArithmeticStatementBinder);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 4: ControlFlowBinder contains moved methods ──

    [Theory]
    [InlineData("BindPerform")]
    [InlineData("BindPerformVaryingOption")]
    [InlineData("ValidatePerformIndex")]
    [InlineData("BindEvaluate")]
    [InlineData("BindEvaluateWhenGroup")]
    [InlineData("BindValueOperand")]
    [InlineData("BindIf")]
    [InlineData("BindGoTo")]
    [InlineData("BindAlter")]
    [InlineData("BindSearch")]
    [InlineData("BindSearchAll")]
    [InlineData("ExtractSearchIndex")]
    [InlineData("FindSubscriptOnTable")]
    [InlineData("IsTableElement")]
    [InlineData("ValidateSearchStatement")]
    [InlineData("ValidateSearchAllStatement")]
    [InlineData("IsSearchAllEqualityCondition")]
    public void ControlFlowBinder_contains_method(string methodName)
    {
        var type = typeof(ControlFlowBinder);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 4: FileIoBinder contains moved methods ──

    [Theory]
    [InlineData("BindWrite")]
    [InlineData("BindOpen")]
    [InlineData("BindClose")]
    [InlineData("BindRead")]
    [InlineData("BindRewrite")]
    [InlineData("BindDelete")]
    [InlineData("BindStart")]
    [InlineData("BindReturn")]
    [InlineData("BindSort")]
    [InlineData("BindMerge")]
    [InlineData("BindRelease")]
    [InlineData("BindSortKeys")]
    [InlineData("BindMergeKeys")]
    [InlineData("ResolveFileList")]
    [InlineData("BindUse")]
    public void FileIoBinder_contains_method(string methodName)
    {
        var type = typeof(FileIoBinder);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 4: DataStatementBinder contains moved methods ──

    [Theory]
    [InlineData("BindDisplay")]
    [InlineData("BindMove")]
    [InlineData("BindMoveSendingOperand")]
    [InlineData("BindSet")]
    [InlineData("BindSetSwitch")]
    [InlineData("BindSetBoolean")]
    [InlineData("BindSetToValue")]
    [InlineData("BindSetIndex")]
    [InlineData("BindInitialize")]
    [InlineData("ClassifyReplacingItem")]
    [InlineData("BindReplacingValue")]
    [InlineData("BindAccept")]
    public void DataStatementBinder_contains_method(string methodName)
    {
        var type = typeof(DataStatementBinder);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 4: StringStatementBinder contains moved methods ──

    [Theory]
    [InlineData("BindInspect")]
    [InlineData("ExtractInspectPattern")]
    [InlineData("ExtractStringValue")]
    [InlineData("ExtractNthStringValue")]
    [InlineData("ExtractLiteralString")]
    [InlineData("BindInspectDelimiters")]
    [InlineData("BindString")]
    [InlineData("BindUnstring")]
    [InlineData("ValidateStringStatement")]
    [InlineData("ValidateUnstringStatement")]
    [InlineData("ValidateInspectStatement")]
    public void StringStatementBinder_contains_method(string methodName)
    {
        var type = typeof(StringStatementBinder);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 4: CallBinder contains moved methods ──

    [Theory]
    [InlineData("BindCall")]
    [InlineData("BindCancel")]
    [InlineData("BindEntry")]
    public void CallBinder_contains_method(string methodName)
    {
        var type = typeof(CallBinder);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 2: ExpressionBinder contains moved methods ──

    [Theory]
    [InlineData("BindAdditiveExpression")]
    [InlineData("BindMultiplicativeExpression")]
    [InlineData("BindPowerExpression")]
    [InlineData("BindUnaryExpression")]
    [InlineData("BindPrimaryExpression")]
    [InlineData("BindFunctionCall")]
    [InlineData("BindLiteral")]
    [InlineData("BindNumericLiteral")]
    [InlineData("BindNonNumericLiteral")]
    [InlineData("BindFigurativeConstantExpression")]
    [InlineData("BindDataReferenceWithSubscripts")]
    [InlineData("InterpretSubscriptTokens")]
    [InlineData("CollectLeafTokens")]
    [InlineData("SplitSubscriptTokens")]
    [InlineData("BindSubscriptSegment")]
    [InlineData("BindSubscriptTokensAsArithmetic")]
    [InlineData("BindSubscriptEntry")]
    [InlineData("ResolveQualifiedName")]
    [InlineData("FindChild")]
    [InlineData("BindReferenceModification")]
    [InlineData("BindReceivingOperand")]
    [InlineData("BindSimpleOperand")]
    [InlineData("BindDataReferenceOrLiteral")]
    [InlineData("BindArithmeticExpr")]
    public void ExpressionBinder_contains_method(string methodName)
    {
        var type = typeof(ExpressionBinder);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 2: ProcedureNameResolver contains moved methods ──

    [Theory]
    [InlineData("ExtractProcedureNameText")]
    [InlineData("ResolveProcedureName")]
    [InlineData("ResolveProcedureNameForThruEnd")]
    [InlineData("ResolveProcedureNameForPerform")]
    public void ProcedureNameResolver_contains_method(string methodName)
    {
        var type = typeof(ProcedureNameResolver);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }
}
