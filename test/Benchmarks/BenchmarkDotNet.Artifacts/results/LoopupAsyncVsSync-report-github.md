``` ini

BenchmarkDotNet=v0.10.1, OS=Windows
Processor=?, ProcessorCount=8
Frequency=3328116 Hz, Resolution=300.4703 ns, Timer=TSC
dotnet cli version=1.0.0-preview2-1-003177
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.24628.01, 64bit RyuJIT


```

Method |          Mean |    StdErr |     StdDev | Scaled | Scaled-StdDev | Allocated |
----------- |-------------- |---------- |----------- |------- |-------------- |---------- |
Async | 1,215.3005 us | 4.0189 us | 15.5650 us |   1.00 |          0.00 |   4.33 kB |
AsyncNoTtl | 1,171.2286 us | 6.7385 us | 26.0980 us |   0.96 |          0.02 |   4.33 kB |
Sync |   766.2499 us | 8.8416 us | 40.5173 us |   0.63 |          0.03 |   3.27 kB |

