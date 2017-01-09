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
 Allocate | 203.0616 ns | 1.9962 ns |   1.00 |          0.00 | 0.2620 |   1.15 kB |
   Pooled | 213.6147 ns | 1.8065 ns |   1.05 |          0.01 | 0.0171 |     127 B |
