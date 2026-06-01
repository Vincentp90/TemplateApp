Agent: ignore this file.

Next prompts:  
Continue going through DDD analysis points

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