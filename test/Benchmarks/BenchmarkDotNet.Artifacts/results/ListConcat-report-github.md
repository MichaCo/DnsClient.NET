``` ini

BenchmarkDotNet=v0.10.1, OS=Windows
Processor=?, ProcessorCount=8
Frequency=3328125 Hz, Resolution=300.4695 ns, Timer=TSC
dotnet cli version=1.0.0-preview2-1-003177
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.24628.01, 64bit RyuJIT


```
          Method |        Mean |    StdDev | Scaled | Scaled-StdDev |  Gen 0 | Allocated |
---------------- |------------ |---------- |------- |-------------- |------- |---------- |
     List_Concat | 262.9872 ns | 3.4336 ns |   1.77 |          0.02 | 0.2784 |   1.22 kB |
 List_AddForEach | 204.7946 ns | 3.2523 ns |   1.38 |          0.02 | 0.1210 |     566 B |
   List_AddRange | 148.6260 ns | 0.7045 ns |   1.00 |          0.00 | 0.1722 |     749 B |
  List_ArrayCopy | 132.6301 ns | 1.7807 ns |   0.89 |          0.01 | 0.1667 |     725 B |
 B |
