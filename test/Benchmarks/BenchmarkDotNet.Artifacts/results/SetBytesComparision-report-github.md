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
        ForLoop |    92.7253 ns |  1.4805 ns |   0.99 |          0.02 | 0.0114 |
      BlockCopy |    93.2930 ns |  0.3047 ns |   1.00 |          0.00 | 0.0109 |
   ForLoopLarge | 2,216.8763 ns | 15.2380 ns |  23.76 |          0.17 |      - |
 BlockCopyLarge |   112.5041 ns |  0.5461 ns |   1.21 |          0.01 | 0.0114 |
