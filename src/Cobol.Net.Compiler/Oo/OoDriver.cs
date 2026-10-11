// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Compiler.Oo;

using Core = CobolParserCore;

/// <summary>
/// The OO bind driver (P9 R1 — the OO orchestration is a real BINDER collaborator, owned and sequenced by
/// <c>BinderDriver.Bind</c>; the former emitter-hosted <c>IOoBindHost</c> seam is DELETED): binds each
/// INTERFACE's prototype formals, each class's OBJECT + FACTORY data halves and method signatures (pass-1
/// discipline — every signature before any body, deep-dive D1), and each class's method bodies into the
/// class's one pc-dispatch space. Consumes ONLY binder state (<see cref="BindSession"/> + the binder types);
/// it emits nothing — emission renders the bound facts from <c>BoundCompilation</c>.
/// </summary>
internal sealed class OoDriver(BindSession session)
{
    private readonly Dictionary<OoInterfaceSymbol, DataBinder> _ifaceData = [];

    /// <summary>The per-interface DATA forests (prototype LINKAGE formals — bound so ValidateImplements has
    /// resolved descriptions, and so the interface emission can render the formals' numeric profiles and
    /// group struct types as INTERFACE statics, which CONTENT conversions through interface-typed receivers
    /// qualify as <c>{IFACE}._P_n</c>; C# 8+ interfaces carry static members natively).</summary>
    public IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> InterfaceData => _ifaceData;

    /// <summary>Bind one INTERFACE's prototype formals (§10.6.2 SR4 — LINKAGE-only data divisions; the
    /// prototypes reuse the whole OoBindMethodData machinery with no bodies).</summary>
    public void BindInterfaceData(OoInterfaceSymbol iface) => _ifaceData[iface] = BindInterface(iface);

    /// <summary>⛔ A PARAMETERIZED definition binds AS ITSELF, for its own rules, and nothing reads the result
    /// (kb/Work PB2051; OO deep-dive D12). §9.3.12 makes it a skeleton whose expansions are the classes, but it is a
    /// class definition all the same: every syntax rule of its data, environment and method headers — a BASED entry's
    /// level (§13.16.3 SR16), a data-division section's placement (§13.7.3 SR1), a duplicate METHOD-ID — governs its
    /// text whether or not anything expands it, and until this bind those rules were asked of an expansion only, so an
    /// unexpanded definition compiled whatever its body held. Its parameter-names resolve through the one funnel as
    /// formals (<c>OoNameResolution.Result.IsFormal</c>), and a reference typed by a formal takes the universal
    /// description: what the formal names is known only in an expansion, where §9.3.8.2.4 asks every conformance
    /// question "as if the actual parameter classes or interfaces were substituted". The uid band it draws is its
    /// own and comes after every emitted class's, so emission is unchanged. Its method BODIES bind only in each
    /// expansion: a statement through a formal-typed reference obeys rules that differ between the universal
    /// description and the typed one an expansion binds (a literal INVOKE argument, SUPER over a formal base), so
    /// they wait on a formal description kind of their own (D12 determination 6).</summary>
    public void BindParameterized(OoClassSymbol skeleton) => BindClassData(new OoClassUnit { Symbol = skeleton });

    /// <summary>The interface twin of <see cref="BindParameterized(OoClassSymbol)"/>: the prototypes' formals bind
    /// and the forest is dropped (nothing renders an interface skeleton).</summary>
    public void BindParameterized(OoInterfaceSymbol skeleton) => _ = BindInterface(skeleton);

    private DataBinder BindInterface(OoInterfaceSymbol iface)
    {
        var data = new DataBinder(session.Edition) { OoClasses = session.OoClasses, OoIsClassUnit = true, RefModZeroLength = session.RefModZeroLength, CobolWords = session.CobolWords, Retypes = session.Retypes, LeapSecond = session.LeapSecond.IsOnAt(iface.Ctx.Start.Line), CompilationVariables = session.CompilationVariables };
        data.CallSeedUids(session.TakeUidBand());
        // §10.6.1 / §11.9.4 GR1: the INTERFACE skeleton carries its own OPTIONS paragraph, and nothing bound it
        // — so its clauses had no effect on the prototypes AND, because OptionsBinder is where the
        // processor-dependence screens live, an interface could select a declined arithmetic mode in silence
        // (kb/Work PB197: the second hollow arm, found by sweeping all six optionsParagraph-bearing productions
        // rather than only the METHOD one the finding named).
        data.CallInheritOptions(Binding.OptionsBinder.BindParagraph(iface.Ctx.optionsParagraph(), session.Edition, null));
        var synthetic = new Core.ProgramUnitContext(null!, -1);
        OoEnvironmentRules.Screen(OoDefinition.Interface, $"interface '{iface.Name}'",
            iface.Ctx.environmentDivision(), session.Edition);
        if (iface.Ctx.environmentDivision() is { } env) synthetic.AddChild(env);
        data.BindDeclarations(synthetic);
        using (session.Edition.At(iface.Ctx)) data.DeclareUserWord(iface.Name, UserWordKind.InterfaceName);
        DeclareMethodWords(data, iface.Prototypes, session.Edition);
        foreach (var proto in iface.Prototypes)
            data.OoBindMethodData(proto);
        data.BindResolve(synthetic);
        return data;
    }

    /// <summary>Declare each written METHOD-ID's name through the one user-defined-word funnel (§8.3.2.2; kb/Work
    /// PB1083): a method-name, or for a GET/SET PROPERTY method the property-name it names. The accessors a PROPERTY
    /// clause synthesizes carry no METHOD-ID (their word is declared by <c>OoBindPropertyClauses</c>).</summary>
    private static void DeclareMethodWords(DataBinder data, IEnumerable<OoMethodSymbol> methods,
                                           EditionContext edition)
    {
        foreach (var m in methods)
        {
            if (m.Ctx is null) continue;   // a synthesized PROPERTY-clause accessor
            using (edition.At(m.Ctx))
                data.DeclareUserWord(m.PropertyName ?? m.Name,
                    m.PropertyName is null ? UserWordKind.MethodName : UserWordKind.PropertyName);
        }
    }

    /// <summary>Phase A of class binding — the DATA + SIGNATURES: the OBJECT paragraph's data division binds
    /// through the STANDARD DataBinder over a synthetic program-unit context (the <c>CallReparent</c>
    /// discipline — direct-children accessors see exactly the class's own divisions) producing INSTANCE
    /// fields; each METHOD's LINKAGE/LOCAL-STORAGE/WS sections and PD USING/RETURNING formals bind between
    /// the declaration and resolve halves (slice 2 — <c>DataBinder.OoBindMethodData</c>). Runs for EVERY class
    /// before ANY body binds, so a method of class A INVOKEing class B sees B's full signature regardless of
    /// source order (the pass-1 discipline, deep-dive D1).</summary>
    public void BindClassData(OoClassUnit cls)
    {
        var edition = session.Edition;
        // The environment-division placement of each definition of the class, judged once (kb/Work PB1076 + PB813).
        var classCtx = cls.Symbol.Ctx;
        OoEnvironmentRules.Screen(OoDefinition.Class, $"class '{cls.Name}'", classCtx.environmentDivision(), edition);
        OoEnvironmentRules.Screen(OoDefinition.Factory, $"class '{cls.Name}' FACTORY paragraph",
            classCtx.factoryParagraph()?.environmentDivision(), edition);
        OoEnvironmentRules.Screen(OoDefinition.Instance, $"class '{cls.Name}' OBJECT paragraph",
            classCtx.objectParagraph()?.environmentDivision(), edition);
        // The DATA-division twin: a factory or instance definition carries no LINKAGE or LOCAL-STORAGE SECTION
        // (§13.7.3 SR1, §13.6.3 SR1; kb/Work PB1251) and no GLOBAL clause in any entry (§13.18.27.3 SR4; kb/Work
        // PB1045). Its methods' own data divisions are asked in OoBindMethodData.
        OoDefinitionRules.Screen(OoDefinition.Factory, $"class '{cls.Name}' FACTORY paragraph",
            classCtx.factoryParagraph()?.dataDivision(), edition);
        OoDefinitionRules.Screen(OoDefinition.Instance, $"class '{cls.Name}' OBJECT paragraph",
            classCtx.objectParagraph()?.dataDivision(), edition);
        // OoOwnerClassName is what USAGE OBJECT REFERENCE [FACTORY OF] ACTIVE-CLASS binds to — §13.18.60.4
        // GR22 e), the class of the object that invoked the containing method (kb/Work PB389).
        var data = new DataBinder(edition) { OoClasses = session.OoClasses, OoIsClassUnit = true, OoOwnerClassName = cls.Name, RefModZeroLength = session.RefModZeroLength, CobolWords = session.CobolWords, Retypes = session.Retypes, LeapSecond = session.LeapSecond.IsOnAt(cls.Symbol.Ctx.Start.Line), CompilationVariables = session.CompilationVariables };
        data.CallSeedUids(session.TakeUidBand());
        // §10.6.1 / §11.9.4 GR1 (kb/Work PB135): the class skeleton's OPTIONS paragraph is the contained
        // definitions' baseline; the OBJECT paragraph's own overrides it clause by clause.
        var clsOptions = Binding.OptionsBinder.BindParagraph(cls.Symbol.Ctx.optionsParagraph(), edition, null);
        data.CallInheritOptions(Binding.OptionsBinder.BindParagraph(
            cls.Symbol.Ctx.objectParagraph()?.optionsParagraph(), edition, clsOptions));
        var synthetic = OoReparentClassData(cls.Symbol.Ctx);
        data.BindDeclarations(synthetic);
        using (edition.At(cls.Symbol.Ctx)) data.DeclareUserWord(cls.Name, UserWordKind.ObjectClassName);
        DeclareMethodWords(data, cls.Symbol.Methods, edition);
        foreach (var m in cls.Symbol.Methods.ToList())   // snapshot — property synthesis appends accessors
            data.OoBindMethodData(m);
        data.OoBindPropertyClauses(cls.Symbol, factory: false);
        data.BindResolve(synthetic);
        cls.Data = data;
        cls.Refs = new ReferenceResolver(data);

        // The FACTORY half (§11.4; brief D11/D13): its OWN forest + uid band — factory data names are
        // invisible to instance methods and vice versa (separate source elements, §10.6), realized exactly
        // like method scoping: a second binder, never a merged namespace. SR 10 (INVOKE-argument ban on
        // factory WS) works free: the factory binder's WS roots are not method-scoped → OoIsObjectData.
        var fdata = new DataBinder(edition) { OoClasses = session.OoClasses, OoIsClassUnit = true, OoOwnerClassName = cls.Name, RefModZeroLength = session.RefModZeroLength, CobolWords = session.CobolWords, Retypes = session.Retypes, LeapSecond = session.LeapSecond.IsOnAt(cls.Symbol.Ctx.Start.Line), CompilationVariables = session.CompilationVariables };
        fdata.CallSeedUids(session.TakeUidBand());
        fdata.CallInheritOptions(Binding.OptionsBinder.BindParagraph(
            cls.Symbol.Ctx.factoryParagraph()?.optionsParagraph(), edition, clsOptions));   // §11.9.4 GR1 (kb/Work PB135)
        var fsynthetic = OoReparentFactoryData(cls.Symbol.Ctx);
        fdata.BindDeclarations(fsynthetic);
        DeclareMethodWords(fdata, cls.Symbol.FactoryMethods, edition);
        foreach (var m in cls.Symbol.FactoryMethods.ToList())
            fdata.OoBindMethodData(m);
        fdata.OoBindPropertyClauses(cls.Symbol, factory: true);
        fdata.BindResolve(fsynthetic);
        cls.FactoryData = fdata;
        cls.FactoryRefs = new ReferenceResolver(fdata);
    }

    /// <summary>Phase B — the method BODIES bind into the class's one pc space (per-method paragraph AND data
    /// scopes — §11.7; <c>StatementBinder.BindMethodRoster</c>). Runs after every program unit's DATA division and
    /// the group's REPOSITORY tables (<see cref="BindSession.Repository"/>): the class definition's REPOSITORY
    /// paragraph applies to its factory, its object and every method (ISO §12.3.4 GR1: "The entries explicitly or
    /// implicitly specified in the configuration section of a source unit that contains other source units apply to
    /// each directly or indirectly contained source unit"), so a method's function-identifier and CALL
    /// program-prototype-name resolve through the SAME two lookups a program's do (kb/Work PB1100).</summary>
    public void BindClassBody(OoClassUnit cls)
    {
        var repository = session.Repository;
        // §12.3.8.4 GR10 a) / GR11 a): "specified PREVIOUSLY" is relative to the class definition whose REPOSITORY
        // paragraph holds the specifier (kb/Work PB989) — an expansion's text starts where its skeleton does.
        int position = cls.Symbol.Ctx.Start.StartIndex;
        var binder = new StatementBinder(cls.Data, cls.Refs)
        {
            OoClasses = session.OoClasses,
            OoCurrentClass = cls.Symbol,   // the SELF/SUPER resolution root (§8.4.3.8; slice 3b)
            UserFunctions = Binding.BinderDriver.UserFunctionsOf(cls.Data, unit: null, repository, position),
            ProgramPrototypes = Binding.BinderDriver.ProgramPrototypesOf(cls.Data, unit: null, repository, position),
            SignatureClasses = session.SignatureClasses,
        };
        binder.ConfigureEc(session.Turn, session.DirectiveSites, cls.Name);   // methods fold the same source-ordered >>TURN state (§7.3.25 GR6)
        // kb/Work PB971 — the method formals' EC-OO-ARG-OMITTED guards (§14.9.23.4 GR10), before any body binds.
        Binding.Procedure.EcBinder.MarkFormals(cls.Symbol.Methods, session.Turn);
        Binding.Procedure.EcBinder.MarkFormals(cls.Symbol.FactoryMethods, session.Turn);
        cls.Bound = binder.BindMethodRoster(cls.Symbol, cls.Symbol.Methods);

        // The FACTORY roster binds through a SEPARATE binder over the factory forest, with the factory
        // SELF/SUPER context (§14.9.23.3 SR4f/h; §16.2.1 SELF|SUPER "NEW" — OoInFactory).
        var fbinder = new StatementBinder(cls.FactoryData, cls.FactoryRefs)
        {
            OoClasses = session.OoClasses,
            OoCurrentClass = cls.Symbol,
            OoInFactory = true,
            UserFunctions = Binding.BinderDriver.UserFunctionsOf(cls.FactoryData, unit: null, repository, position),
            ProgramPrototypes = Binding.BinderDriver.ProgramPrototypesOf(cls.FactoryData, unit: null, repository, position),
            SignatureClasses = session.SignatureClasses,
        };
        fbinder.ConfigureEc(session.Turn, session.DirectiveSites, cls.Name);
        cls.FactoryBound = fbinder.BindMethodRoster(cls.Symbol, cls.Symbol.FactoryMethods);
    }

    /// <summary>Re-shape a class definition's data surface into a synthetic <c>programUnit</c> context for the
    /// per-unit DataBinder (the CallReparent pattern — accessors scan DIRECT children only): the OBJECT
    /// paragraph's environment/data divisions, then the class-level environment division (the singular
    /// <c>environmentDivision()</c> accessor returns the FIRST child, so the nearer OBJECT scope wins).</summary>
    private static Core.ProgramUnitContext OoReparentClassData(Core.ClassDefinitionContext ctx)
    {
        var unit = new Core.ProgramUnitContext(null!, -1);
        var obj = ctx.objectParagraph();
        if (obj?.environmentDivision() is { } envObj) unit.AddChild(envObj);
        if (ctx.environmentDivision() is { } envCls) unit.AddChild(envCls);
        if (obj?.dataDivision() is { } dd) unit.AddChild(dd);
        return unit;
    }

    /// <summary>Re-shape the FACTORY paragraph's data surface into a synthetic <c>programUnit</c> (the
    /// CallReparent discipline): factory env → class env → factory data division.</summary>
    private static Core.ProgramUnitContext OoReparentFactoryData(Core.ClassDefinitionContext ctx)
    {
        var unit = new Core.ProgramUnitContext(null!, -1);
        var fac = ctx.factoryParagraph();
        if (fac?.environmentDivision() is { } envFac) unit.AddChild(envFac);
        if (ctx.environmentDivision() is { } envCls) unit.AddChild(envCls);
        if (fac?.dataDivision() is { } dd) unit.AddChild(dd);
        return unit;
    }

}
