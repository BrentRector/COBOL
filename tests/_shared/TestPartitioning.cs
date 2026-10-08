// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Reflection;

namespace CobolNet.Tests.Shared;

/// <summary>
/// THE mechanism for running one theory-heavy test class across SEVERAL xUnit collections — one rule, one place.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THIS EXISTS (plan §11 A13, measured). xUnit <b>2.9.2 parallelizes at TEST-COLLECTION granularity, and by
/// default each test CLASS is one collection</b> — so every test in a class, including every row of a
/// <c>[Theory]</c>, runs <b>SERIALLY ON ONE THREAD</b>. A single fat class therefore caps an entire assembly's wall
/// clock while the other cores idle, and nothing in the normal output says so. Measured on the battery #41 trx
/// (2026-09-01, 32-core box, <c>scripts/profile-test-parallelism.py</c>): the Conformance assembly ran 5,241 tests
/// in <b>721 s wall for 1,948 s of test time — 2.7x average concurrency</b>, of which
/// <c>VersionMatrixTests</c> alone was <b>2,127 tests running serially for 720.5 s</b>, i.e. essentially the WHOLE
/// leg was one class on one thread; the Unit assembly was <b>171 s wall / 329 s test time — 1.9x</b>, and one class
/// (<c>StorageFormEquivalenceTests</c>' NIST sweep, 171.5 s) <i>was</i> that wall clock.
/// </para>
/// <para>
/// ⭐ THE SHAPE, and why it is this one. xUnit v2 offers exactly two levers — genuine class splits, or
/// <c>[Collection]</c> attributes — and <c>[Collection]</c> only ever <i>merges</i> classes into a shared
/// collection, so it cannot split one. That leaves class splits, and the naive form of a class split DUPLICATES
/// THE TEST BODIES, which is how a split silently rots. So: the tests live ONCE in an <b>abstract generic base</b>
/// <c>Family&lt;TSlot&gt;</c>, its <c>[MemberData]</c> sources slice the family's full row set with
/// <see cref="SliceRows{TSlot}"/>, and each partition is a <b>one-line</b> concrete class
/// <c>sealed class Family_P3 : FamilyBase&lt;Slot3&gt;;</c>. Every concrete class is its own default collection, so
/// the partitions run concurrently.
/// </para>
/// <para>
/// This works because <b>static members of a CLOSED generic type are per-type-argument</b>, and xUnit resolves
/// <c>[MemberData]</c> against <c>testMethod.DeclaringType</c> — which for a method inherited from
/// <c>FamilyBase&lt;Slot3&gt;</c> is the <i>closed</i> <c>FamilyBase&lt;Slot3&gt;</c>, not the open definition. That
/// is not assumed: it was proved on xunit 2.9.2 / xunit.runner.visualstudio 2.8.2 with a standalone probe (3
/// partitions over 9 rows produced 9 tests, 3 per partition, each asserting its OWN slot — an open-type resolution
/// would have produced 27 tests and 18 failures).
/// </para>
/// <para>
/// ⚠ THE INVARIANT THAT MATTERS is that the partitions cover the family's row set <b>exactly once</b> — a family
/// whose partition classes go stale (one deleted, one duplicated, the count bumped without a new class) drops rows
/// SILENTLY and stays green. <see cref="TestPartitionAudit"/> is the drift gate that makes that impossible, and it
/// is generic: it discovers every family in an assembly by shape, so a NEW partitioned family is covered the
/// moment it is written, with no registration step.
/// </para>
/// </remarks>
internal static class TestPartitioning
{
    /// <summary>Slot <paramref name="index"/>'s share of <paramref name="rows"/> under a stride of
    /// <paramref name="partitions"/> — row <c>i</c> belongs to slot <c>i % partitions</c>, so the slots are
    /// disjoint and their union is the whole set by construction.</summary>
    /// <remarks>A stride (not a contiguous block) is deliberate: theory rows are usually ordered by construct or
    /// program name, and adjacent rows have correlated cost, so a stride balances the partitions where a block
    /// would concentrate the expensive rows in one of them.</remarks>
    internal static IEnumerable<T> Slice<T>(IEnumerable<T> rows, int index, int partitions)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentOutOfRangeException.ThrowIfLessThan(partitions, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, partitions);
        return rows.Where((_, i) => i % partitions == index);
    }

    /// <summary>The <typeparamref name="TSlot"/> partition's share of a theory's rows. This is the ONE call a
    /// partitioned family's <c>[MemberData]</c> source makes.</summary>
    /// <remarks>The row type is fixed to <c>object[]</c> — xUnit's data-source shape — rather than generic,
    /// because C# takes explicit type arguments all-or-nothing: a <c>Slice&lt;TSlot, T&gt;</c> could not be called
    /// as <c>Slice&lt;Slot3&gt;(rows, 12)</c>, and spelling out both at every call site is exactly the kind of
    /// noise that makes a family adopt something else instead. It also means the drift audit has ONE row shape to
    /// key on, so a family that slices something else (a list of program paths, say) still routes through here and
    /// is still covered.</remarks>
    internal static IEnumerable<object[]> SliceRows<TSlot>(IEnumerable<object[]> rows, int partitions)
        where TSlot : ITestPartitionSlot
        => Slice(rows, TSlot.Index, partitions);
}

/// <summary>One partition slot: a compile-time integer, carried as a type argument so a partition class can be a
/// single line. Adding a slot is one line here; nothing else in the mechanism has a fixed upper bound.</summary>
public interface ITestPartitionSlot
{
    /// <summary>This slot's zero-based index. Must equal the numeric suffix of the implementing type's name —
    /// <see cref="TestPartitionAudit"/> enforces it, because a slot whose name and index disagree would make every
    /// partition class read as the wrong partition.</summary>
    static abstract int Index { get; }
}

// The ladder: 24 identical one-line declarations, kept honest by TestPartitionAudit.AuditSlotLadder. Its top is set
// by the widest family, CorpusRunnerTests (24 partitions, kb/Work PB2527).
public readonly struct Slot0 : ITestPartitionSlot { public static int Index => 0; }
public readonly struct Slot1 : ITestPartitionSlot { public static int Index => 1; }
public readonly struct Slot2 : ITestPartitionSlot { public static int Index => 2; }
public readonly struct Slot3 : ITestPartitionSlot { public static int Index => 3; }
public readonly struct Slot4 : ITestPartitionSlot { public static int Index => 4; }
public readonly struct Slot5 : ITestPartitionSlot { public static int Index => 5; }
public readonly struct Slot6 : ITestPartitionSlot { public static int Index => 6; }
public readonly struct Slot7 : ITestPartitionSlot { public static int Index => 7; }
public readonly struct Slot8 : ITestPartitionSlot { public static int Index => 8; }
public readonly struct Slot9 : ITestPartitionSlot { public static int Index => 9; }
public readonly struct Slot10 : ITestPartitionSlot { public static int Index => 10; }
public readonly struct Slot11 : ITestPartitionSlot { public static int Index => 11; }
public readonly struct Slot12 : ITestPartitionSlot { public static int Index => 12; }
public readonly struct Slot13 : ITestPartitionSlot { public static int Index => 13; }
public readonly struct Slot14 : ITestPartitionSlot { public static int Index => 14; }
public readonly struct Slot15 : ITestPartitionSlot { public static int Index => 15; }
public readonly struct Slot16 : ITestPartitionSlot { public static int Index => 16; }
public readonly struct Slot17 : ITestPartitionSlot { public static int Index => 17; }
public readonly struct Slot18 : ITestPartitionSlot { public static int Index => 18; }
public readonly struct Slot19 : ITestPartitionSlot { public static int Index => 19; }
public readonly struct Slot20 : ITestPartitionSlot { public static int Index => 20; }
public readonly struct Slot21 : ITestPartitionSlot { public static int Index => 21; }
public readonly struct Slot22 : ITestPartitionSlot { public static int Index => 22; }
public readonly struct Slot23 : ITestPartitionSlot { public static int Index => 23; }

/// <summary>
/// Marks the UNPARTITIONED row source of a partitioned family, naming the sliced member the family's theories
/// actually consume. <see cref="TestPartitionAudit"/> reads both and proves the partitions reconstruct the source
/// exactly once — the whole point of the mechanism, and the one thing a class split can silently lose.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class PartitionedRowSourceAttribute(string slicedMember) : Attribute
{
    /// <summary>Name of the sliced member (the one carrying <c>[MemberData]</c>) fed from this source.</summary>
    public string SlicedMember { get; } = slicedMember;
}

/// <summary>
/// The DRIFT GATE for <see cref="TestPartitioning"/>: proves, per assembly and by shape alone, that every
/// partitioned family's concrete partitions cover its row set exactly once.
/// </summary>
/// <remarks>
/// ⛔ A partitioned family fails OPEN if nothing checks it: delete one partition class and the rows it owned are
/// simply never run, with a green leg and a smaller — but still plausible — test count. Every check below exists
/// for one such way to lose rows silently:
/// <list type="bullet">
///   <item>a row source that yields NOTHING — every check below would then compare an empty union against an
///         empty source and report green;</item>
///   <item>slot indices ≠ {0 … <c>Partitions</c>−1} — a deleted, duplicated or mis-slotted partition class;</item>
///   <item>an EMPTY partition over a non-empty source — more partitions declared than the source can fill, the
///         one waste a pure union check cannot see because the surviving slots still cover the whole set;</item>
///   <item>the union of the partitions ≠ the source as a MULTISET — rows dropped or double-run.</item>
/// </list>
/// ⚠ Both of the middle checks have been fired deliberately (a partition class commented out; a slice count
/// desynced from the const) rather than trusted silent — and the discovery filter itself went RED on its first
/// real run, on Roslyn's compiler-generated <c>&lt;&gt;O</c> type.
/// It is deliberately shape-driven rather than registered: a new family is covered with no edit here, which is
/// what keeps "automatic" true.
/// </remarks>
internal static class TestPartitionAudit
{
    /// <summary>Name of the <c>public const int</c> every partitioned family base declares.</summary>
    internal const string PartitionCountMember = "Partitions";

    /// <summary>One discovered family: its base definition, declared partition count, concrete partitions and
    /// the row sources audited.</summary>
    internal sealed record Family(Type BaseDefinition, int DeclaredPartitions, IReadOnlyList<Type> Partitions,
        IReadOnlyList<string> RowSources)
    {
        /// <summary>The base's short name, for reporting.</summary>
        public string Name => BaseDefinition.Name;
    }

    /// <summary>The audit result: what was found, and every way it is wrong.</summary>
    internal sealed record Report(IReadOnlyList<Family> Families, IReadOnlyList<string> Violations);

    /// <summary>Run <see cref="Audit"/> and THROW with the full report if anything is wrong — the one line each
    /// test assembly's drift test consists of.</summary>
    /// <remarks>This throws rather than asserting so that <c>tests/_shared</c> stays free of an xUnit reference:
    /// <c>tests/Directory.Build.props</c> links these files into EVERY project under <c>tests/</c>, and
    /// <c>Cobol.Net.Benchmarks</c> is not a test project and has no xUnit package.
    /// <para>⛔ Zero families is a FAILURE, not a pass. A selector that returns nothing is evidence about what it
    /// returned, never about what it dropped, and a coverage gate that quietly found nothing to cover is the
    /// exact shape of a green test holding a gap open.</para></remarks>
    internal static void AssertClean(Assembly assembly)
    {
        var report = Audit(assembly);
        var problems = new List<string>(report.Violations);
        if (report.Families.Count == 0)
        {
            problems.Add($"NO partitioned family was found in {assembly.GetName().Name} — this drift test is "
                + "asserting nothing. Either a family lost its abstract generic base (and its rows are no longer "
                + "partitioned) or this test class belongs in a different assembly.");
        }

        if (problems.Count == 0) return;

        string census = string.Join("\n", report.Families.Select(f =>
            $"  {f.Name}: Partitions={f.DeclaredPartitions}, classes=[{string.Join(", ", f.Partitions.Select(t => t.Name))}], "
            + $"sources=[{string.Join(", ", f.RowSources)}]"));
        throw new InvalidOperationException(
            $"{problems.Count} partitioned-test coverage violation(s) in {assembly.GetName().Name}:\n"
            + string.Join("\n", problems.Select(p => "  ⛔ " + p))
            + (census.Length > 0 ? $"\nFamilies found:\n{census}" : string.Empty));
    }

    /// <summary>Audit every partitioned family in <paramref name="assembly"/>.</summary>
    internal static Report Audit(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var families = new List<Family>();
        var violations = new List<string>();

        AuditSlotLadder(violations);

        foreach (var baseDefinition in assembly.GetTypes()
                     .Where(IsFamilyBaseDefinition)
                     .OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            families.Add(AuditFamily(assembly, baseDefinition, violations));
        }

        return new Report(families, violations);
    }

    /// <summary>A family base is a TOP-LEVEL, author-written, ABSTRACT generic definition with exactly one type
    /// parameter, constrained to <see cref="ITestPartitionSlot"/>. That shape is the registration — there is no
    /// list to keep current.</summary>
    /// <remarks>⚠ The two negative clauses are not defensive padding — they are why this gate went RED on its very
    /// first run. Roslyn emits a nested <c>&lt;&gt;O</c> delegate-cache class inside any generic type that caches a
    /// lambda, and a nested type INHERITS its enclosing type's generic parameter <i>with its constraints</i>. So
    /// every partitioned family produced a phantom "family" called <c>&lt;&gt;O</c> with no
    /// <c>Partitions</c> const and no partition classes. Compiler-generated and nested types are excluded on
    /// principle rather than by name.</remarks>
    private static bool IsFamilyBaseDefinition(Type t)
        => t is { IsAbstract: true, IsGenericTypeDefinition: true, IsNested: false }
           && !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)
           && t.GetGenericArguments() is [{ } p]
           && p.GetGenericParameterConstraints().Contains(typeof(ITestPartitionSlot));

    private static Family AuditFamily(Assembly assembly, Type baseDefinition, List<string> violations)
    {
        string name = baseDefinition.Name;

        int declared = -1;
        if (baseDefinition.GetField(PartitionCountMember, BindingFlags.Public | BindingFlags.Static) is { } f
            && f.GetRawConstantValue() is int n)
        {
            declared = n;
        }
        else
        {
            violations.Add($"{name}: no `public const int {PartitionCountMember}` — a partitioned family must "
                + "declare how many partitions it slices into, so the audit can prove the classes match it.");
        }

        // The concrete partitions: every non-abstract class whose immediate base is this definition, closed.
        var partitions = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.BaseType is { IsGenericType: true } b
                        && b.GetGenericTypeDefinition() == baseDefinition)
            .OrderBy(t => SlotIndexOf(t), Comparer<int>.Default)
            .ToList();

        var rowSources = baseDefinition.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetCustomAttribute<PartitionedRowSourceAttribute>() is not null)
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ToList();

        if (partitions.Count == 0)
        {
            violations.Add($"{name}: declares {declared} partitions but NO concrete partition class exists — "
                + "every row of this family is unreachable and the leg is silently green.");
            return new Family(baseDefinition, declared, partitions, [.. rowSources.Select(m => m.Name)]);
        }

        if (rowSources.Count == 0)
        {
            violations.Add($"{name}: no [{nameof(PartitionedRowSourceAttribute)}] method — the family's row set "
                + "is unstated, so coverage cannot be proved. Mark the unpartitioned source.");
        }

        // ① The slot indices must be EXACTLY {0 .. declared-1}.
        var slots = partitions.Select(SlotIndexOf).ToList();
        if (declared >= 0 && (slots.Distinct().Count() != slots.Count
                              || !slots.Order().SequenceEqual(Enumerable.Range(0, declared))))
        {
            violations.Add($"{name}: {PartitionCountMember} = {declared} but the concrete partition classes carry "
                + $"slots [{string.Join(", ", slots.Order())}] — expected exactly [0 … {declared - 1}], each once. "
                + $"Classes: {string.Join(", ", partitions.Select(t => t.Name))}.");
        }

        foreach (var source in rowSources)
        {
            AuditRowSource(name, declared, partitions, source, violations);
        }

        return new Family(baseDefinition, declared, partitions, [.. rowSources.Select(m => m.Name)]);
    }

    private static void AuditRowSource(string familyName, int declared, IReadOnlyList<Type> partitions,
        MethodInfo source, List<string> violations)
    {
        string sliced = source.GetCustomAttribute<PartitionedRowSourceAttribute>()!.SlicedMember;
        var whole = Invoke(partitions[0].BaseType!, source.Name, familyName, violations);
        if (whole is null) return;

        // ⓪ An EMPTY source makes every check below compare nothing to nothing and report green — the exact shape
        //    of a zero fan-out that looks clean. A family exists because it has rows; none is a failure.
        if (whole.Count == 0)
        {
            violations.Add($"{familyName}.{source.Name} yielded NO rows. Every coverage check below would then "
                + "compare an empty union against an empty source and pass, so the family would be partitioned, "
                + "green, and testing nothing.");
            return;
        }

        var expected = Multiset(whole);
        var actual = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var partition in partitions)
        {
            var rows = Invoke(partition.BaseType!, sliced, familyName, violations);
            if (rows is null) return;

            // ② An EMPTY partition over a non-empty source. The union check below CANNOT see this: the remaining
            //    slots still cover the whole set, and an empty collection looks exactly like a fast one. It means
            //    the family declares more partitions than its source has rows — those collections cost a test
            //    class each and run nothing — or that the sliced member filters by something other than the
            //    stride, in which case the coverage the union check just proved is accidental.
            //    (The opposite desync — slicing with a count SMALLER than the highest slot in use — cannot reach
            //    here at all: TestPartitioning.Slice's own range guard throws first, which was verified by
            //    deliberately desyncing it.)
            if (rows.Count == 0)
            {
                violations.Add($"{familyName}.{sliced}: partition {partition.Name} selected 0 of {whole.Count} "
                    + $"rows — {PartitionCountMember} = {declared} exceeds what this source can fill, so that "
                    + "collection runs nothing.");
            }

            foreach (var key in rows)
            {
                actual[key] = actual.GetValueOrDefault(key) + 1;
            }
        }

        // ③ The union must equal the source as a MULTISET — nothing dropped, nothing run twice.
        var missing = expected.Where(kv => actual.GetValueOrDefault(kv.Key) < kv.Value)
            .Select(kv => kv.Key).Order(StringComparer.Ordinal).ToList();
        var extra = actual.Where(kv => kv.Value > expected.GetValueOrDefault(kv.Key))
            .Select(kv => kv.Key).Order(StringComparer.Ordinal).ToList();
        if (missing.Count > 0 || extra.Count > 0)
        {
            violations.Add($"{familyName}.{sliced}: the {partitions.Count} partitions do not cover "
                + $"{source.Name}'s {whole.Count} rows exactly once — {missing.Count} row(s) NEVER RUN, "
                + $"{extra.Count} row(s) run more than once. "
                + $"First missing: {string.Join(" | ", missing.Take(5))}. "
                + $"First duplicated: {string.Join(" | ", extra.Take(5))}.");
        }
    }

    /// <summary>Invoke a public static parameterless row member on a CLOSED family base and key its rows.</summary>
    private static List<string>? Invoke(Type closedBase, string member, string familyName, List<string> violations)
    {
        var method = closedBase.GetMethod(member, BindingFlags.Public | BindingFlags.Static,
            binder: null, types: Type.EmptyTypes, modifiers: null);
        if (method is null)
        {
            violations.Add($"{familyName}: `{member}` is not a public static parameterless member of "
                + $"{closedBase.Name} — the [{nameof(PartitionedRowSourceAttribute)}] pairing is stale.");
            return null;
        }

        if (method.Invoke(null, null) is not IEnumerable<object[]> rows)
        {
            violations.Add($"{familyName}: `{member}` did not return IEnumerable<object[]>.");
            return null;
        }

        return [.. rows.Select(Key)];
    }

    /// <summary>A row's identity, for multiset comparison. Rows are theory arguments — scalars and strings — so a
    /// unit-separated invariant rendering is a faithful key.</summary>
    private static string Key(object[] row)
        => string.Join('␟', row.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture) ?? "<null>"));

    private static Dictionary<string, int> Multiset(IEnumerable<string> keys)
    {
        var m = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string k in keys)
        {
            m[k] = m.GetValueOrDefault(k) + 1;
        }

        return m;
    }

    /// <summary>The slot index carried by a concrete partition class, via its closed base's type argument.</summary>
    private static int SlotIndexOf(Type partition)
        => (int)partition.BaseType!.GetGenericArguments()[0]
            .GetProperty(nameof(ITestPartitionSlot.Index), BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;

    /// <summary>⛔ The slot ladder itself is drift-prone in the one way that would corrupt EVERY family at once: a
    /// copy-paste that leaves <c>Slot9.Index =&gt; 8</c>. Then two classes claim slot 8, a ninth of the rows never
    /// run, and every family using that slot is wrong together. Checked here so it is checked once.</summary>
    private static void AuditSlotLadder(List<string> violations)
    {
        var slots = typeof(ITestPartitionSlot).Assembly.GetTypes()
            .Where(t => t.IsValueType && t.IsAssignableTo(typeof(ITestPartitionSlot)))
            .ToList();
        if (slots.Count == 0)
        {
            violations.Add("the slot ladder is EMPTY — no ITestPartitionSlot implementation exists.");
            return;
        }

        foreach (var slot in slots.OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            int index = (int)slot.GetProperty(nameof(ITestPartitionSlot.Index),
                BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
            string digits = new(slot.Name.SkipWhile(char.IsLetter).ToArray());
            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int fromName)
                || fromName != index)
            {
                violations.Add($"slot {slot.Name} reports Index = {index} — the name and the index disagree, so "
                    + "every partition class using it is in a different partition than it reads as.");
            }
        }

        var indices = slots.Select(t => (int)t.GetProperty(nameof(ITestPartitionSlot.Index),
            BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!).Order().ToList();
        if (!indices.SequenceEqual(Enumerable.Range(0, slots.Count)))
        {
            violations.Add($"the slot ladder is not contiguous from 0: [{string.Join(", ", indices)}].");
        }
    }
}
