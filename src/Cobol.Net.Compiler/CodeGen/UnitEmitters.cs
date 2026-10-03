// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The PER-UNIT composition root (P7 Step 9n — the phase doc's AS-BUILT PLAN): one instance per
/// emitted unit (program class, OO class half, interface unit), constructing the per-unit
/// <see cref="EmitContext"/>/renderer quadruple and EVERY collaborator emitter over it, with the cyclic
/// edges (verbs↔<see cref="StatementEmitter"/>↔<see cref="EcEmitter"/>, KeyedIo↔SequentialIo) property-wired
/// post-construction — the EC↔statement cycle means no pure bottom-up construction order exists (the
/// Step-9 coupling census <c>wf_d677d614-5fb</c>). Replaces the transitional CSharpEmitter host edges: the
/// collaborators now hold DIRECT references to each other; the former god class survives only as the
/// bind-host facade (the P6→P9 seam; the OO bind bodies live on <c>Oo/OoDriver</c> since P9 Step 4). <see cref="ProgramEmitter.BeginUnit"/> re-creates
/// this root at each unit switch — consumers that must track the switch mid-run (OoEmitter) read through
/// <see cref="ProgramEmitter.Current"/>, never a captured copy.</summary>
internal sealed class UnitEmitters
{
    public EmitContext Ctx { get; }
    public NumericRenderer Num { get; }
    public ConditionRenderer Cond { get; }
    public ReferenceResolver Refs { get; }

    public MoveEmitter Move { get; }
    public EcEmitter Ec { get; }
    public ArithmeticEmitter Arith { get; }
    public AlterSwitchEmitter AlterSwitch { get; }
    public AcceptDisplayEmitter AcceptDisplay { get; }
    public EvaluateEmitter Evaluate { get; }
    public InitializeEmitter Initialize { get; }
    public CorrespondingEmitter Corresponding { get; }
    public InspectEmitter Inspect { get; }
    public StringEmitter Strings { get; }
    public PtrEmitter Ptr { get; }
    public SetEmitter Set { get; }
    public KeyedIoEmitter KeyedIo { get; }
    public SequentialIoEmitter SeqIo { get; }
    public SortEmitter Sort { get; }
    public ReportWriterEmitter ReportWriter { get; }
    public ControlFlowEmitter ControlFlow { get; }
    public CallEmitter Call { get; }
    public StatementEmitter Statements { get; }
    public DispatchEmitter Dispatch { get; }

    public UnitEmitters(CodeWriter w, DataBinder data, ReferenceResolver refs, NameAllocator names,
        DispatchState dispatchState, EcState ecState, CallUnitState callState, OoEmitter oo,
        Lazy<string> whenCompiledStamp)
    {
        Refs = refs;
        Ctx = new EmitContext(w, data, names, whenCompiledStamp);
        Num = new NumericRenderer(Ctx, ecState);
        Cond = new ConditionRenderer(Num, Ctx);

        // Acyclic construction order (each ctor takes only already-built collaborators); the cycles close
        // via the property wiring below.
        Move = new MoveEmitter(Ctx, Num, Refs);
        Ec = new EcEmitter(Ctx, ecState, dispatchState);
        Arith = new ArithmeticEmitter(Ctx, Num, ecState, Ec);
        AlterSwitch = new AlterSwitchEmitter(Ctx, dispatchState);
        AcceptDisplay = new AcceptDisplayEmitter(Ctx, Num, Move);
        Evaluate = new EvaluateEmitter(Ctx, Cond);
        Initialize = new InitializeEmitter(Ctx, Move);
        Corresponding = new CorrespondingEmitter(Ctx, Num, Move, Arith, Ec);
        Inspect = new InspectEmitter(Ctx, Num, Arith);
        Strings = new StringEmitter(Ctx, Num, Arith, Ec, Move);
        Ptr = new PtrEmitter(Ctx, Num, ecState, Ec);
        Set = new SetEmitter(Ctx, Num, Arith, Ptr, Move);
        KeyedIo = new KeyedIoEmitter(Ctx, Num, Refs, Arith, Move);
        SeqIo = new SequentialIoEmitter(Ctx, Num, Refs, dispatchState, ecState, callState, KeyedIo, Arith, Ec, Move);
        Sort = new SortEmitter(Ctx, Refs, SeqIo, Move, Arith, Ec);   // no NumericRenderer since RELEASE moved to the ONE record-area channel (kb/Work PB327)
        ReportWriter = new ReportWriterEmitter(Ctx, Num, Refs, Move, Cond, dispatchState);
        ControlFlow = new ControlFlowEmitter(Ctx, Num, Cond, dispatchState, Set);
        Call = new CallEmitter(Ctx, Num, ecState, callState, Ec, Move, dispatchState, Ptr);
        Statements = new StatementEmitter(this, oo, dispatchState);
        Dispatch = new DispatchEmitter(Ctx, dispatchState, ecState, AlterSwitch, ReportWriter, SeqIo, Ec, Statements);

        // The cyclic edges (statement lists nest arbitrarily inside verb phrases; EcEmitChecked re-enters
        // EmitStatement; KeyedIo consumes SequentialIo's file-I/O common services although SeqIo's ctor
        // already took KeyedIo for the shared READ/REWRITE bodies).
        Ec.Statements = Statements;
        Arith.Statements = Statements;
        Evaluate.Statements = Statements;
        Strings.Statements = Statements;
        KeyedIo.Statements = Statements;
        KeyedIo.SeqIo = SeqIo;
        SeqIo.Statements = Statements;
        Sort.Statements = Statements;
        ControlFlow.Statements = Statements;
        ControlFlow.Ec = Ec;   // CA36: SEARCH with EC-RANGE checking + no AT END dispatches via EcEmitter.EcDispatchExpr
        Call.Statements = Statements;
        Cond.Calls = Call;   // BoundUdfEvaluated — the per-evaluation function-activation text (P10 Step 10)
        Cond.Statements = Statements;
        Cond.Ptr = Ptr;   // an address-identifier relation operand (kb/Work PB1021)   // …and its NON-call pre-ops (a D18 subscript temp store), captured as text
    }
}
