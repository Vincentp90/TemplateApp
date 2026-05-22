Agent: ignore this file.

Trying:  
VS Code  
Cline plugin  
Ollama with model Qwen3.6  

Save commands:
cd api/WishlistApi; dotnet test
dotnet test api/WishlistApi/WishlistApi.sln

Next:
I get following warning when compiling. How to fix it? F:\dev\TemplateApp\api\WishlistApi\WishlistApi\WishlistApi.csproj : warning NU1608: Detected package version outside of dependency constraint: EFCore.NamingConventions 9.0.0 requires Microsoft.EntityFrameworkCore.Relational (>= 9.0.0 && < 10.0.0) but version Microsoft.EntityFrameworkCore.Relational 10.0.0 was resolved.

----

Todo:
delete ollama qwen model
delete ollama

Other things to try;
-mute db migration output (program.cs)
-try pi instead of cline

----
Trying llamma.cpp instead of ollama

https://huggingface.co/froggeric/Qwen-Fixed-Chat-Templates --jinja --chat-template-file chat_template.jinja

Command:

cd G:\llama

.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ4_XS-4.15bpw -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --chat-template chatml --cache-type-k q8_0 --cache-type-v q8_0 --n-cpu-moe 15 --no-ui 

.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ4_XS-4.15bpw -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --chat-template chatml --cache-type-k q8_0 --cache-type-v q8_0 --no-ui --n-cpu-moe 10
35 t/S , 200-900 t/s prompt eval


.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ4_XS-4.15bpw -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --chat-template chatml --cache-type-k q8_0 --cache-type-v q8_0 --no-ui 
30 t/s, 750 t/s prompt eval

.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q6_K_L -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --chat-template chatml --cache-type-k q8_0 --cache-type-v q8_0 --no-ui 
20 t/s, 100 t/s prompt eval


----

Permanently set env vars for model location
setx HF_HOME "G:\llamacache\hf-cache"
setx LLAMA_CACHE "G:\llamacache\llama-cache"