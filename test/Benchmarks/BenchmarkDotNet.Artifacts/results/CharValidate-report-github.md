``` ini

BenchmarkDotNet=v0.10.1, OS=Windows
Processor=?, ProcessorCount=8
Frequency=3328125 Hz, Resolution=300.4695 ns, Timer=TSC
dotnet cli version=1.0.0-preview2-1-003177
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.24628.01, 64bit RyuJIT


```
          Method |          Mean |     StdDev | Scaled | Scaled-StdDev | Allocated |
---------------- |-------------- |----------- |------- |-------------- |---------- |
 ForeachLongName |   909.8184 ns |  7.8330 ns |  19.34 |          0.25 |       0 B |
         Foreach |    47.0548 ns |  0.4984 ns |   1.00 |          0.00 |       0 B |
 LinqAnyLongName | 2,646.9445 ns | 17.7688 ns |  56.26 |          0.68 |      31 B |
         LinqAny |   212.1548 ns |  1.6318 ns |   4.51 |          0.06 |      31 B |
   RegExLongName | 3,659.4354 ns | 16.4830 ns |  77.78 |          0.86 |       0 B |
           RegEx |   370.8919 ns |  6.2250 ns |   7.88 |          0.15 |       0 B |
