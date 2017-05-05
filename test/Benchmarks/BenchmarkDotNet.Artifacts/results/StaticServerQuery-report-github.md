``` ini

BenchmarkDotNet=v0.10.5, OS=Windows 10.0.15063
Processor=Intel Core i7-6700 CPU 3.40GHz (Skylake), ProcessorCount=8
Frequency=3328126 Hz, Resolution=300.4694 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.7.2046.0
  Job-IMJKFH : Clr 4.0.30319.42000, 64bit RyuJIT-v4.7.2046.0
  Job-XSFCHD : .NET Core 4.6.25009.03, 64bit RyuJIT

LaunchCount=1  TargetCount=10  WarmupCount=5  

```
 |       Method | Runtime |     Mean |    Error |   StdDev | Scaled | ScaledSD | Allocated |
 |------------- |-------- |---------:|---------:|---------:|-------:|---------:|----------:|
 |  RequestSync |     Clr | 37.30 us | 2.007 us | 1.327 us |   1.00 |     0.00 |   2.54 kB |
 | RequestAsync |     Clr | 61.04 us | 6.826 us | 4.515 us |   1.64 |     0.13 |   6.16 kB |
 |  RequestSync |    Core | 40.78 us | 4.331 us | 2.865 us |   1.00 |     0.00 |   2.48 kB |
 | RequestAsync |    Core | 68.60 us | 7.335 us | 4.851 us |   1.69 |     0.16 |    2.9 kB |
