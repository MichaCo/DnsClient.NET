``` ini

BenchmarkDotNet=v0.10.1, OS=Windows
Processor=?, ProcessorCount=8
Frequency=2648440 Hz, Resolution=377.5808 ns, Timer=TSC
dotnet cli version=1.0.0-preview2-1-003177
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.25009.03, 64bit RyuJIT


```
           Method |        Mean |    StdErr |     StdDev | Scaled | Scaled-StdDev |   Gen 0 |   Gen 1 | Allocated |
----------------- |------------ |---------- |----------- |------- |-------------- |-------- |-------- |---------- |
 ForeachTransform | 202.6711 us | 0.6228 us |  2.3304 us |   1.00 |          0.00 | 69.9870 | 22.1354 |  399.1 kB |
  SelectTransform | 215.0337 us | 3.2331 us | 15.5056 us |   1.06 |          0.08 | 68.4245 | 20.5729 | 399.18 kB |
