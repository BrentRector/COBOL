// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
//
// ⛔ RECORDING BUILDS ONLY (kb/Work PB1683) — compiled into the three test assemblies by
// tools/impact/ImpactRecording.targets, which also names ImpactTestFramework in the assembly's
// [assembly: Xunit.TestFramework]. The design is docs/rearchitecture/DESIGN-test-build-ci.md §3.13.
#nullable enable
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CobolNet.Impact;

/// <summary>
/// xunit's own framework with ONE change: the message bus is wrapped so that, when xunit reports a test collection,
/// class or test STARTING, a fresh hit context is installed on the logical flow that is about to run it
/// (<see cref="ImpactProbe.Enter"/>), and when it reports it FINISHED the context's hits are saved against its name.
///
/// <para>⛔ WHY THE BUS AND NOT A BEFORE/AFTER ATTRIBUTE. xunit queues <c>ITestStarting</c> from inside
/// <c>TestRunner.RunAsync</c>, the same async method that then awaits the test's invocation, so an
/// <see cref="AsyncLocal{T}"/> set synchronously there flows into the test class's constructor, the test body and
/// every task it starts, while parallel tests on other flows see their own. <c>BeforeAfterTestAttribute</c> is handed
/// only the <see cref="MethodInfo"/>, which cannot tell the rows of a theory apart — and the corpus theories are
/// 3,400 of the Conformance assembly's 9,300 tests. Collection and class contexts exist because xunit creates
/// fixtures there, outside any test, and a fixture's work is a dependency of every test in its scope.</para>
/// </summary>
public sealed class ImpactTestFramework(IMessageSink messageSink) : XunitTestFramework(messageSink)
{
    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
        new Executor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);

    private sealed class Executor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider,
        IMessageSink diagnosticMessageSink)
        : XunitTestFrameworkExecutor(assemblyName, sourceInformationProvider, diagnosticMessageSink)
    {
        protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases,
            IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            using var runner = new Runner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink,
                executionOptions);
            await runner.RunAsync();
        }
    }

    private sealed class Runner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions)
        : XunitTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink,
            executionOptions)
    {
        protected override IMessageBus CreateMessageBus() => new ContextBus(base.CreateMessageBus());
    }

    /// <summary>The interception: starts and finishes contexts, then forwards every message unchanged.</summary>
    private sealed class ContextBus(IMessageBus inner) : IMessageBus
    {
        private readonly ConcurrentDictionary<object, Tuple<byte[], ConcurrentQueue<string>>> _open =
            new(ReferenceEqualityComparer.Instance);

        public bool QueueMessage(IMessageSinkMessage message)
        {
            switch (message)
            {
                case ITestCollectionStarting m: Open(m.TestCollection); break;
                case ITestClassStarting m: Open(m.TestClass); break;
                case ITestStarting m: Open(m.Test); break;
                case ITestFinished m: Close(m.Test, Recorder.TestRecord(m.Test, m.ExecutionTime)); break;
                case ITestClassFinished m:
                    Close(m.TestClass, new ContextRecord("class", m.TestClass.Class.Name, null, null,
                        m.TestClass.TestCollection.UniqueID.ToString(), m.ExecutionTime));
                    break;
                case ITestCollectionFinished m:
                    Close(m.TestCollection, new ContextRecord("collection", m.TestCollection.DisplayName, null, null,
                        m.TestCollection.UniqueID.ToString(), m.ExecutionTime));
                    break;
                case ITestAssemblyFinished: Recorder.Flush(); break;
            }

            return inner.QueueMessage(message);
        }

        public void Dispose() => inner.Dispose();

        private void Open(object key)
        {
            if (ImpactProbe.Enter() is { } ctx)
            {
                _open[key] = ctx;
            }
        }

        private void Close(object key, ContextRecord record)
        {
            if (_open.TryRemove(key, out var ctx))
            {
                Recorder.Save(record, ctx);
            }
        }
    }

    /// <summary>One recorded context: a test (fqn + display name + class + collection), a class or a collection.</summary>
    internal sealed record ContextRecord(string Kind, string Name, string? DisplayName, string? Class,
        string Collection, decimal Seconds);

    /// <summary>Collects every finished context and writes them, one JSON object per line, when the assembly
    /// finishes.</summary>
    private static class Recorder
    {
        private static readonly ConcurrentQueue<string> s_lines = new();

        internal static ContextRecord TestRecord(ITest test, decimal seconds)
        {
            var method = test.TestCase.TestMethod;
            string cls = method.TestClass.Class.Name;
            return new ContextRecord("test", cls + "." + method.Method.Name, test.DisplayName, cls,
                method.TestClass.TestCollection.UniqueID.ToString(), seconds);
        }

        internal static void Save(ContextRecord record, Tuple<byte[], ConcurrentQueue<string>> ctx)
        {
            // Fold in every child process the context started (they have exited: ProcessObserver waits).
            var missing = new List<string>();
            foreach (string child in ctx.Item2)
            {
                if (!MergeChild(child, ctx.Item1))
                {
                    missing.Add(child);
                }
            }

            s_lines.Enqueue(JsonSerializer.Serialize(new
            {
                kind = record.Kind,
                name = record.Name,
                display = record.DisplayName,
                @class = record.Class,
                collection = record.Collection,
                seconds = record.Seconds,
                bits = PackBits(ctx.Item1),
                children = ctx.Item2.Count,
                missing_children = missing.Count,
            }));
        }

        /// <summary>The hit array as a bitset (bit i = probe entry i, little-endian), zlib-compressed and base64'd:
        /// a Conformance test reaches thousands of the ~45,000 methods, and one JSON number per hit would make the
        /// raw contexts file hundreds of megabytes.</summary>
        private static string PackBits(byte[] hits)
        {
            byte[] packed = new byte[(hits.Length + 7) / 8];
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != 0)
                {
                    packed[i >> 3] |= (byte)(1 << (i & 7));
                }
            }

            using var ms = new MemoryStream();
            using (var z = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionLevel.Fastest))
            {
                z.Write(packed);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        private static bool MergeChild(string prefix, byte[] into)
        {
            string? dir = Path.GetDirectoryName(prefix);
            if (dir is null || !Directory.Exists(dir))
            {
                return false;
            }

            bool any = false;
            foreach (string file in Directory.GetFiles(dir, Path.GetFileName(prefix) + ".*.hits"))
            {
                any = true;
                foreach (string line in File.ReadLines(file))
                {
                    if (int.TryParse(line, out int id) && (uint)id < (uint)into.Length)
                    {
                        into[id] = 1;
                    }
                }

                File.Delete(file);
            }

            return any;
        }

        internal static void Flush()
        {
            string? dir = Environment.GetEnvironmentVariable(ImpactProbe.DirVariable);
            if (string.IsNullOrEmpty(dir))
            {
                return;
            }

            string asm = typeof(Recorder).Assembly.GetName().Name ?? "unknown";
            string contexts = Path.Combine(dir, "contexts");
            Directory.CreateDirectory(contexts);
            var sb = new StringBuilder();
            while (s_lines.TryDequeue(out string? line))
            {
                sb.Append(line).Append('\n');
            }

            File.AppendAllText(Path.Combine(contexts, asm + ".jsonl"), sb.ToString());
            if (ImpactProbe.Ambient is { } ambient)
            {
                ImpactProbe.WriteHits(Path.Combine(dir, "ambient", $"{Environment.ProcessId}.{asm}.hits"), ambient);
            }
        }
    }
}
