``` ini

BenchmarkDotNet=v0.10.1, OS=Windows
Processor=?, ProcessorCount=8
Frequency=3328125 Hz, Resolution=300.4695 ns, Timer=TSC
dotnet cli version=1.0.0-preview2-1-003177
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.24628.01, 64bit RyuJIT


```
   Method |        Mean |    StdDev | Scaled | Scaled-StdDev |  Gen 0 | Allocated |
--------- |------------ |---------- |------- |-------------- |------- |---------- |
 Allocate | 203.4379 ns | 1.8158 ns |   1.00 |          0.00 | 0.2618 |   1.15 kB |
   Pooled | 219.2415 ns | 0.8282 ns |   1.08 |          0.01 | 0.0176 |     127 B |
