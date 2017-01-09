``` ini

BenchmarkDotNet=v0.10.1, OS=Windows
Processor=?, ProcessorCount=8
Frequency=3328125 Hz, Resolution=300.4695 ns, Timer=TSC
dotnet cli version=1.0.0-preview2-1-003177
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.24628.01, 64bit RyuJIT


```
 Method |        Mean |    StdDev | Scaled | Scaled-StdDev |  Gen 0 | Allocated |
------- |------------ |---------- |------- |-------------- |------- |---------- |
  Async | 406.9623 ns | 3.7535 ns |   1.00 |          0.00 | 0.1207 |     598 B |
   Sync | 332.7869 ns | 3.4209 ns |   0.82 |          0.01 | 0.0985 |     462 B |
 |     598 B |
       Sync | 352.6764 ns | 1.8713 ns |   0.85 |          0.01 | 0.0980 |     462 B |
