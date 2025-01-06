# Disclaimer
I am __not__ a performance expert, so take everything with a grain of salt and do your own testing.

# Intentional boxing of value types
As a beginner to programming - for me this was mainly C# and .NET - I was always told to avoid [boxing (and thus unboxing)](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing) when possible. As a general rule of thumb, this may be true, but from what I have observed in some library, and also from testing, there are exceptions to this.

Consider you want to cache a value type. For example in a WPF application this could be a `Thickness` you want to cache.
In the following example the cached value type is a `Range`:

```C#
public static class SharedCache
{
	public static readonly Range DefaultRange = new Range(new Index(1), new Index(2));
}
```

Now let's say you use the `DefaultRange` multiple times in your app. To reduce noise of other objects and to make a point, I will be using/accessing the `DefaultRange` 10.000 times in this example.

Because this is a value type that is being accessed 10.000 times, and value types are stored on the stack rather than the heap, there is actually a new object created every time the "shared" object is called.
Consider the following code to fake 10.000 usages of `DefaultRange`:
```C#
public static class SharedCache
{
	public static readonly Range DefaultRange = new Range(new Index(1), new Index(2));
}

public static List<object> GetDefaultRanges()
{
	List<object> ranges = new List<object>();
	for (int i = 0; i < 10_000; i++)
	{
		ranges.Add(SharedCache.DefaultRange);
	}
	return ranges;
}
```
When running the code above with Visual Studios [Performance Profiler](https://learn.microsoft.com/visualstudio/profiling/profiling-feature-tour?view=vs-2022), it shows that more than 10.000 `Ranges` were allocated. This kind of defeats the purpose of having a shared object to cache data:
![image](https://github.com/user-attachments/assets/762a36ed-1f3a-4e3b-bef0-52c9187a1868)


If you now switch the type of `DefaultRange` to an `object`, you force the value type (`Range`) to get boxed and stored on the heap, unlike before, where it was stored on the stack.
```C#
public static class SharedCache
{
	public static readonly object DefaultRange = new Range(new Index(1), new Index(2));
}

public static List<object> GetDefaultRanges()
{
	List<object> ranges = new List<object>();
	for (int i = 0; i < 10_000; i++)
	{
		ranges.Add(SharedCache.DefaultRange);
	}
	return ranges;
}
```

Running the Performance Profiler again, shows that only 1 `Range` object is created:
![image](https://github.com/user-attachments/assets/e8245bbd-66a0-4a69-a262-ffde75b1571c)

# Benchmarking
This behavior can also be observed when running tests against the code shown below:
```C#
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
```

Results:
![image](https://github.com/user-attachments/assets/de25517a-39b3-4bc7-acdc-7252fb38e117)

Notice that the performace is degrading linearly. Now whether this is something you should worry about is up to you. 
Something that is not taken into consideration in the above benchmark, is the fact that the objects have to eventually get unboxed again. But even with unboxing inplace, the benefit of only 1 object being created should still be given.
