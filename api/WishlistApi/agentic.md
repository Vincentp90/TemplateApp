Trying:  
VS Code  
Cline plugin  
Ollama with model Qwen3.6  

Save commands:
cd api/WishlistApi; dotnet test
dotnet test api/WishlistApi/WishlistApi.sln

Next:
In AuctionRepository GetLatestAuctionAsync, when mapping to Domain.Auction, use a constructor instead of the object initializer syntax. Add a fitting constructor for this to Domain.auction. Set Domain.Auction setters to private or remove them. Other usage of object initializer syntax for creating Domain.Auction should use a suitable constructor instead. Run tests at the end to verify everything still works.

----

Todo:
delete ollama qwen model
delete ollama
delete C++ vulkan SDK build tools


Other things to try;
-fix warnings to waste less context with build logs
-mute db migration output (program.cs)
-try pi instead of cline

----
Trying llamma.cpp instead of ollama

https://unsloth.ai/docs/models/qwen3.6#mtp-qwen3.6-27b
https://huggingface.co/unsloth/Qwen3.6-35B-A3B-GGUF?utm_source=chatgpt.com&show_file_info=Qwen3.6-35B-A3B-UD-Q4_K_XL.gguf

Command:

Permanently set env vars for model location
setx HF_HOME "G:\llamacache\hf-cache"
setx LLAMA_CACHE "G:\llamacache\llama-cache"

cd G:\llama

Back to full context, check if speed is lower
.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q6_K_L -c 262144 --temp 1.0 --top-p 0.95 --min-p 0.0 --flash-attn on --presence_penalty 1.5 --chat-template chatml --api-key anything --cache-type-k q8_0 --cache-type-v q8_0 --parallel 1 --no-mmap --no-ui
TODO t/s

Lower context, higher temp+ presence_penaly, remove top-k, min-p =0, no jinja
.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q6_K_L -c 65536 --temp 1.0 --top-p 0.95 --min-p 0.0 --flash-attn on --presence_penalty 1.5 --chat-template chatml --api-key anything --cache-type-k q8_0 --cache-type-v q8_0 --parallel 1 --no-mmap --no-ui
19 t/s needs more testing

.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q6_K_L -c 131072 --jinja --temp 0.9 --top-p 0.95 --min-p 0.01 --top-k 40 --flash-attn on --presence_penalty 1.2 --chat-template chatml --api-key anything --cache-type-k q8_0 --cache-type-v q8_0 --parallel 1 --no-mmap --no-ui 
19 t/s but wasn't explorative, needed more prompting to do complete job, speed drops as context grows

.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q5_K_L -c 262144 --jinja --temp 1.0 --top-p 0.95 --min-p 0.01 --top-k 40 --n-gpu-layers 999 --flash-attn on --presence_penalty 1.5 --chat-template chatml --api-key anything
12.50 t/s

----
