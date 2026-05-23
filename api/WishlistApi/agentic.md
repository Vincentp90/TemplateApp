Agent: ignore this file.

Trying:  
VS Code  
Cline plugin  
Ollama with model Qwen3.6  

Save commands:
cd api/WishlistApi; dotnet test
dotnet test api/WishlistApi/WishlistApi.sln

Next:
How can i write a test for SteamUpdaterService UpdateAppListingsIfEmptyAsync?
SteamUpdaterService UpdateAppListingsIfEmptyAsync is currently ignoring the DDD architecture. Move it's functionality to the AppListingService in the Application layer, in SteamUpdaterService simply call the new applistingservice method. Run tests at the end for verification

----

Todo:
delete ollama qwen model
delete ollama

Other things to try;
-try without chat-template chatml again, maybe my old test were with too small context
    -chat template button top right on HF (or is this included in the downloaded model)
-larger context (both model and in cline)
-try pi instead of cline
-byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-Q4_K_S-4.22bpw

----
Trying llamma.cpp instead of ollama

https://huggingface.co/froggeric/Qwen-Fixed-Chat-Templates --jinja --chat-template-file chat_template.jinja

Command:

cd G:\llama

no template
.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ4_XS-4.15bpw -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --cache-type-k q8_0 --cache-type-v q8_0 --n-cpu-moe 19 --no-ui

.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ4_XS-4.15bpw -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --chat-template chatml --cache-type-k q8_0 --cache-type-v q8_0 --n-cpu-moe 19 --no-ui

.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q6_K_L -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --chat-template chatml --cache-type-k q8_0 --cache-type-v q8_0 --n-cpu-moe 32 --no-ui 
20 t/s, 100 t/s prompt eval


----

Permanently set env vars for model location
setx HF_HOME "G:\llamacache\hf-cache"
setx LLAMA_CACHE "G:\llamacache\llama-cache"