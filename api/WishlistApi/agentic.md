Trying:  
VS Code  
Cline plugin  
Ollama with model Qwen3.6  

Save commands:
cd api/WishlistApi; dotnet test
dotnet test api/WishlistApi/WishlistApi.sln

Next:
Delete unused using statements from the files that are currently changed in git. Don't ignore AGENTS.md.

----

Trying llamma.cpp instead of ollama
Quantisation: UD-Q4_K_XL
Maybe try Q6 later

https://unsloth.ai/docs/models/qwen3.6#mtp-qwen3.6-27b
https://huggingface.co/unsloth/Qwen3.6-35B-A3B-GGUF?utm_source=chatgpt.com&show_file_info=Qwen3.6-35B-A3B-UD-Q4_K_XL.gguf

Command:

Permanently set env vars for model location
setx HF_HOME "G:\llamacache\hf-cache"
setx LLAMA_CACHE "G:\llamacache\llama-cache"

cd G:\llama

Try next:
bartowski Q5_K_L

--chat-template chatml --special

.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q4_K_L -c 262144 --jinja --temp 1.0 --top-p 0.95 --min-p 0.01 --top-k 40 --n-gpu-layers 999 --flash-attn on --presence_penalty 1.5 --repeat-penalty 1.0 --spec-type draft-mtp

.\llama-server.exe -hf unsloth/Qwen3.6-35B-A3B-GGUF:UD-Q4_K_M -c 262144 --jinja --temp 1.0 --top-p 0.95 --min-p 0.01 --top-k 40 --n-gpu-layers 999 --flash-attn on --presence_penalty 1.5 --repeat-penalty 1.0

.\llama-server.exe -hf unsloth/Qwen3.6-35B-A3B-GGUF:UD-Q4_K_M -c 262144 --jinja --temp 1.0 --top-p 0.95 --min-p 0.01 --top-k 40 --n-gpu-layers 999 --flash-attn on --presence_penalty 1.5 --repeat-penalty 1.0 --no-mmap --chat-template-kwargs '{\"enable_thinking\": false}'


.\llama-server.exe -hf unsloth/Qwen3.6-35B-A3B-GGUF:UD-Q4_K_M -c 262144 --jinja --temp 1.0 --top-p 0.95 --min-p 0.01 --top-k 40


.\llama-server.exe -hf unsloth/Qwen3.6-35B-A3B-GGUF:UD-Q4_K_M -c 262144 --rope-scaling yarn --rope-scale 8 --chat-template qwen --temp 1 --top-p 0.95 --top-k 20 --repeat-penalty 1 --presence-penalty 1.5

Problem: below ones keep getting stuck in loops

.\llama-server.exe `
  -hf unsloth/Qwen3.6-35B-A3B-GGUF:UD-Q4_K_XL `
  --ctx-size 260000 `
  --temp 0.6 `
  --top-p 0.95 `
  --top-k 20 `
  --presence-penalty 0.0 `
  --chat-template chatml `
  --min-p 0.00 --jinja --chat-template-file chat_template.jinja



MTP attempt, gets stuck in loops constantly
.\llama-server.exe `
  -hf unsloth/Qwen3.6-35B-A3B-MTP-GGUF:UD-Q4_K_XL `
  --ctx-size 260000 `
  --temp 0.6 `
  --top-p 0.95 `
  --top-k 20 `
  --presence-penalty 0.0 `
  --spec-type draft-mtp `
  --spec-draft-n-max 6 `
  --min-p 0.00 --jinja --chat-template-file chat_template.jinja
  
