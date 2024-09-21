using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
Console.ReadLine();

[MemoryDiagnoser]
public class Tests
{
	[Params(1_000, 10_000, 100_000)]
	public int Iterations { get; set; }

	[Benchmark(Baseline = true)]
	public List<object> GetDefaultRanges()
	{
		List<object> ranges = new List<object>();
		for (int i = 0; i < Iterations; i++)
		{
			ranges.Add(SharedCache.DefaultRange);
		}
		return ranges;
	}

	[Benchmark]
	public List<object> GetBoxedDefaultRanges()
	{
		List<object> ranges = new List<object>();
		for (int i = 0; i < Iterations; i++)
		{
			ranges.Add(SharedCache.BoxedDefaultRange);
		}
		return ranges;
	}
}

public static class SharedCache
{
	public static readonly Range DefaultRange = new Range(new Index(1), new Index(2));

	public static readonly object BoxedDefaultRange = new Range(new Index(1), new Index(2));
}