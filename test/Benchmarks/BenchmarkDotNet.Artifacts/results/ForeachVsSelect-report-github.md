``` ini

BenchmarkDotNet=v0.10.1, OS=Windows
Processor=?, ProcessorCount=8
Frequency=2648440 Hz, Resolution=377.5808 ns, Timer=TSC
dotnet cli version=1.0.0-preview2-1-003177
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.25009.03, 64bit RyuJIT


```
           Method |        Mean |    StdDev | Scaled | Scaled-StdDev |   Gen 0 |   Gen 1 | Allocated |
----------------- |------------ |---------- |------- |-------------- |-------- |-------- |---------- |
 ForeachTransform | 202.6234 us | 1.8825 us |   1.00 |          0.00 | 68.6849 | 20.8333 |  399.1 kB |
     ForTransform | 142.1287 us | 1.3339 us |   0.70 |          0.01 | 74.1536 | 26.3021 | 399.06 kB |
  SelectTransform | 208.7250 us | 9.2663 us |   1.03 |          0.05 | 69.7266 | 21.8750 | 399.18 kB |
