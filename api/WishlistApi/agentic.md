Agent: ignore this file.

Trying:  
VS Code  
Cline plugin  
Ollama with model Qwen3.6  

Save commands:
cd api/WishlistApi; dotnet test
dotnet test api/WishlistApi/WishlistApi.sln

Next:
The current Domain.Auction constructor is good for mapping like in AuctionRepository. However, it's not suited well for making new Domain.Auction instances, like in AuctionServices. Add a second constructor better suited for this. Check all calls to the current constructor if they better fit the second constructor. Run all tests at the end for verification.

----

Todo:
delete ollama qwen model
delete ollama

Other things to try;
-fix warnings to waste less context with build logs
-mute db migration output (program.cs)
-try pi instead of cline

----
Trying llamma.cpp instead of ollama

https://huggingface.co/froggeric/Qwen-Fixed-Chat-Templates --jinja --chat-template-file chat_template.jinja

Command:

cd G:\llama

TODO:
.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q6_K_L -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --chat-template chatml --cache-type-k q8_0 --cache-type-v q8_0 --no-ui 

.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ4_XS-4.15bpw -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --chat-template chatml --cache-type-k q8_0 --cache-type-v q8_0 --no-ui 
30 t/s, 750 t/s prompt eval

.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ4_XS-4.15bpw -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --chat-template chatml --no-ui
28 t/s

.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ3_S-3.48bpw -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --jinja --chat-template chatml --no-ui
37 t/S met 32k context, 33 t/s met 64k context

Back to full context, check if speed is lower
.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q6_K_L -c 262144 --temp 1.0 --top-p 0.95 --min-p 0.0 --flash-attn on --presence_penalty 1.5 --jinja --chat-template chatml --api-key anything --cache-type-k q8_0 --cache-type-v q8_0 --parallel 1 --no-mmap --no-ui
TODO t/s

Lower context, higher temp+ presence_penaly, remove top-k, min-p =0, no jinja
.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q6_K_L -c 65536 --temp 1.0 --top-p 0.95 --min-p 0.0 --flash-attn on --presence_penalty 1.5 --jinja --chat-template chatml --api-key anything --cache-type-k q8_0 --cache-type-v q8_0 --parallel 1 --no-mmap --no-ui
19 t/s needs more testing

.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q6_K_L -c 131072 --temp 0.9 --top-p 0.95 --min-p 0.01 --top-k 40 --flash-attn on --presence_penalty 1.2 --jinja --chat-template chatml --api-key anything --cache-type-k q8_0 --cache-type-v q8_0 --parallel 1 --no-mmap --no-ui 
19 t/s but wasn't explorative, needed more prompting to do complete job

.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q5_K_L -c 262144 --temp 1.0 --top-p 0.95 --min-p 0.01 --top-k 40 --n-gpu-layers 999 --flash-attn on --presence_penalty 1.5 --jinja --chat-template chatml --api-key anything
12.50 t/s

.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q4_K_L -c 262144 --jinja --temp 1.0 --top-p 0.95 --min-p 0.01 --top-k 40 --n-gpu-layers 999 --flash-attn on --presence_penalty 1.5 --chat-template chatml --special --api-key anything

----

Permanently set env vars for model location
setx HF_HOME "G:\llamacache\hf-cache"
setx LLAMA_CACHE "G:\llamacache\llama-cache"