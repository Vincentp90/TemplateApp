Trying:  
VS Code  
Cline plugin  
Ollama with model Qwen3.6  

Save commands:
cd api/WishlistApi; dotnet test
dotnet test api/WishlistApi/WishlistApi.sln

Next:
In AuctionRepository GetLatestAuctionAsync, when mapping to Domain.Auction, use a constructor instead of the curly brackets notation. Add a fitting constructor for this to Domain.auction.

----

Other things to try;
-bash instead of powershell
-fix warnings to waste less context with build logs
-mute db migration output (program.cs)
-try pi instead of cline
-Q6_K_L
-131072 context

----
Trying llamma.cpp instead of ollama

https://unsloth.ai/docs/models/qwen3.6#mtp-qwen3.6-27b
https://huggingface.co/unsloth/Qwen3.6-35B-A3B-GGUF?utm_source=chatgpt.com&show_file_info=Qwen3.6-35B-A3B-UD-Q4_K_XL.gguf

Command:

Permanently set env vars for model location
setx HF_HOME "G:\llamacache\hf-cache"
setx LLAMA_CACHE "G:\llamacache\llama-cache"

cd G:\llama

Try next:
lower temp, lower presence_penalty, no more qpu layers specifying (better split cpu and gpu?)
Claude says to keep --n-gpu-layers 999
.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q5_K_L -c 131072 --jinja --temp 0.8 --top-p 0.95 --min-p 0.01 --top-k 40 --flash-attn on --presence_penalty 0.8 --chat-template chatml --api-key anything --cache-type-k q8_0 --cache-type-v q8_0 --n-parallel 1


.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q5_K_L -c 262144 --jinja --temp 1.0 --top-p 0.95 --min-p 0.01 --top-k 40 --n-gpu-layers 999 --flash-attn on --presence_penalty 1.5 --chat-template chatml --api-key anything
12.50 t/s

.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q4_K_L -c 262144 --jinja --temp 1.0 --top-p 0.95 --min-p 0.01 --top-k 40 --n-gpu-layers 999 --flash-attn on --presence_penalty 1.5 --chat-template chatml --special --api-key anything

----
