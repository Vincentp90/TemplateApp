Agent: ignore this file.

Trying:  
VS Code  
Cline plugin  
Ollama with model Qwen3.6  

Save commands:
cd api/WishlistApi; dotnet test
dotnet test api/WishlistApi/WishlistApi.sln

Next:
Add integration test for each userscontroller api call
move the userscontroller unit tests back into a single file

How can i write a test for SteamUpdaterService UpdateAppListingsIfEmptyAsync?
SteamUpdaterService UpdateAppListingsIfEmptyAsync is currently ignoring the DDD architecture. Move it's functionality to the AppListingService in the Application layer, in SteamUpdaterService simply call the new applistingservice method. Run tests at the end for verification

----

Todo:
Stop agent from testing authorization when making controller unit tests (authorization not active during unit tests)
    -> This is what skills files are used for?

Other things to try;
-qwen-coder-next
-larger context (both model and in cline)
-try pi or opencode instead of cline

----
Trying llamma.cpp instead of ollama

Command:

cd G:\llama

opencode
.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ4_XS-4.15bpw -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --cache-type-v q8_0 --n-cpu-moe 24 --no-ui

GPU optimised byteshape, works best with cline
.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ4_XS-4.15bpw -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --cache-type-v q8_0 --n-cpu-moe 21 --chat-template chatml --no-ui

Big boy Q6_K_L 
.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q6_K_L -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --cache-type-v q8_0 --chat-template chatml --n-cpu-moe 32 --no-ui 


When running with cline (not needed for opencode):
--chat-template chatml
Supposedly not good for coding:
--cache-type-k q8_0

Crashes all the time: byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-Q4_K_S-4.22bpw

----

Permanently set env vars for model location
setx HF_HOME "G:\llamacache\hf-cache"
setx LLAMA_CACHE "G:\llamacache\llama-cache"

----