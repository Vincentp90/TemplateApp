using BenchmarkDotNet.Running;

public class BenchmarkProgram
{
    public static void Main(string[] args)
        => BenchmarkSwitcher.FromAssembly(typeof(BenchmarkProgram).Assembly).Run(args);
}
