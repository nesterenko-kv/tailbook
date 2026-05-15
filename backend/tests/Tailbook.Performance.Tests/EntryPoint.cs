using BenchmarkDotNet.Running;
using Tailbook.Performance.Tests.Benchmarks;

public sealed class EntryPoint
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(EntryPoint).Assembly).Run(args);
    }
}
