
    BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
    Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
    .NET Core SDK=2.1.300-rc1-008673
      [Host]     : .NET Core 2.0.7 (CoreCLR 4.6.26328.01, CoreFX 4.6.26403.03), 64bit RyuJIT
  Job-AUWKAQ : .NET Framework 4.6.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3110.0
  Job-DMZITG : .NET Core 2.0.7 (CoreCLR 4.6.26328.01, CoreFX 4.6.26403.03), 64bit RyuJIT

    LaunchCount=1  TargetCount=50  WarmupCount=10  

           Method | Runtime |     Mean |     Error |    StdDev | Scaled | ScaledSD |  Gen 0 | Allocated |
    ------------- |-------- |---------:|----------:|----------:|-------:|---------:|-------:|----------:|
      RequestSync |     Clr | 39.50 us | 0.3031 us | 0.5465 us |   1.00 |     0.00 | 0.6104 |   2.55 KB |
     RequestAsync |     Clr | 59.17 us | 0.1765 us | 0.3442 us |   1.50 |     0.02 | 1.5259 |   6.44 KB |
                  |         |          |           |           |        |          |        |           |
      RequestSync |    Core | 40.36 us | 0.1614 us | 0.3148 us |   1.00 |     0.00 | 0.5493 |   2.43 KB |
     RequestAsync |    Core | 56.95 us | 0.4608 us | 0.9096 us |   1.41 |     0.02 | 1.0376 |    3.2 KB |
