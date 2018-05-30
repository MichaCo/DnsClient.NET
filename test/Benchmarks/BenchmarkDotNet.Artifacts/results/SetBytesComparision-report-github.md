``` ini

BenchmarkDotNet=v0.10.1, OS=Windows
Processor=?, ProcessorCount=8
Frequency=3328125 Hz, Resolution=300.4695 ns, Timer=TSC
dotnet cli version=1.0.0-preview2-1-003177
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.24628.01, 64bit RyuJIT

Allocated=71 B  

```
 Method |          Mean |     StdDev | Scaled | Scaled-StdDev |  Gen 0 |
--------------- |-------------- |----------- |------- |-------------- |------- |
        ForLoop |    94.5811 ns |  0.5073 ns |   0.98 |          0.02 | 0.0114 |
      BlockCopy |    96.0576 ns |  1.4434 ns |   1.00 |          0.00 | 0.0113 |
   ForLoopLarge | 2,187.2455 ns | 34.7684 ns |  22.77 |          0.48 |      - |
 BlockCopyLarge |   113.3799 ns |  0.5767 ns |   1.18 |          0.02 | 0.0112 |
