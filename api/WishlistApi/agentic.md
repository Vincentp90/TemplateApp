Agent: ignore this file.

Next prompts:  
warnings Benchmarks build
    f:\dev\TemplateApp\api\WishlistApi\Benchmarks\Program.cs(2,39): warning CS0436: The type 'Program' in 'f:\dev\TemplateApp\api\WishlistApi\Benchmarks\Program.cs' conflicts with the imported type 'Program' in 'WishlistApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'f:\dev\TemplateApp\api\WishlistApi\Benchmarks\Program.cs'. [f:\dev\TemplateApp\api\WishlistApi\Benchmarks\Benchmarks.csproj]
review UserContextBenchmarks.cs

UserContext, UserService see the ValueTask todo. Do you see possible issues with changing this to a valuetask?

Test performance with ValueTask

| Method                              | Job        | InvocationCount | UnrollFactor | Mean            | Error         | StdDev        | Median          | Allocated |
|------------------------------------ |----------- |---------------- |------------- |----------------:|--------------:|--------------:|----------------:|----------:|
| GetIdAsync_UserContextFieldCacheHit | DefaultJob | Default         | 16           |        10.46 ns |      0.226 ns |      0.261 ns |        10.32 ns |         - |
| GetIdAsync_CacheMiss                | Job-CNUJVU | 1               | 1            | 1,096,795.92 ns | 30,521.101 ns | 89,031.492 ns | 1,073,900.00 ns |   54968 B |
| GetIdAsync_MemoryCacheHit           | Job-CNUJVU | 1               | 1            |     8,334.04 ns |    225.827 ns |    644.297 ns |     7,950.00 ns |      72 B |

$env:DOTNET_ENVIRONMENT="Test"; dotnet run --project api/WishlistApi/Benchmarks -c Release --filter "*"
$env:DOTNET_ENVIRONMENT=Test; dotnet run --project api/WishlistApi/Benchmarks -c Release
set DOTNET_ENVIRONMENT=Test && dotnet run --project api/WishlistApi/Benchmarks -c Release
----

Todo:
devcontainers with pi https://claude.ai/chat/fa57f1e4-cc56-478d-9a73-ffd6bea99933
Stop agent from testing authorization when making controller unit tests (authorization not active during unit tests) (make controller test dev SKILL.md)

----
llamma.cpp commands:

cd G:\llama

Best working model:
.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ4_XS-4.15bpw -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --cache-type-k q8_0 --cache-type-v q5_1 --flash-attn on -b 2048 -ub 2048 --n-cpu-moe 21

----

Permanently set env vars for model location
setx HF_HOME "G:\llamacache\hf-cache"
setx LLAMA_CACHE "G:\llamacache\llama-cache"

----