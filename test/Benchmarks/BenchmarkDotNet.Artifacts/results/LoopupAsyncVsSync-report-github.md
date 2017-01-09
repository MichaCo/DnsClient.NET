``` ini

BenchmarkDotNet=v0.10.1, OS=Windows
Processor=?, ProcessorCount=8
Frequency=3328125 Hz, Resolution=300.4695 ns, Timer=TSC
dotnet cli version=1.0.0-preview2-1-003177
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.24628.01, 64bit RyuJIT


```
     Method |          Mean |     StdDev | Scaled | Scaled-StdDev | Allocated |
----------- |-------------- |----------- |------- |-------------- |---------- |
      Async | 1,204.8150 us | 23.9004 us |   1.00 |          0.00 |    4.3 kB |
 AsyncNoTtl | 1,168.5444 us | 25.8570 us |   0.97 |          0.03 |    4.3 kB |
       Sync |   765.6009 us |  9.8240 us |   0.64 |          0.01 |   3.18 kB |
