using BenchmarkDotNet.Running;

namespace Tailbook.Performance.Tests;

public sealed class EntryPoint
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(EntryPoint).Assembly).Run(args);
    }
}