Agent: ignore this file.

Next prompts:  
Add Tests\ControllerTests\UsersControllerIntegrationTests.cs. Here create one (and only one) integration test for every public method in WishlistApi\Controllers\UsersController.cs. In each unit test, do a happy path test.   
In the arrange step:  
1 Create a dbcontext with an inmemory database  
2 Create UserRepository with the dbcontext  
3 Create UserService with the created UserRepository  
4 Create UserControllerFixture with the UserService
5 Get a UserController object from the fixture
In the Act step you call the controller method on the UserController method

Create a dbcontext with an inmemory database, you can use UserQueriesTests.cs as an example for this. For calling the controller methods use UserControllerFixture, you can use UsersControllerUnitTests.cs as an example.

auction placebid concurrency test, can you check if i already have this kind of test?

How can i write a test for SteamUpdaterService UpdateAppListingsIfEmptyAsync?
SteamUpdaterService UpdateAppListingsIfEmptyAsync is currently ignoring the DDD architecture. Move it's functionality to the AppListingService in the Application layer, in SteamUpdaterService simply call the new applistingservice method. Run tests at the end for verification

----

Todo:
devcontainers with pi https://claude.ai/chat/fa57f1e4-cc56-478d-9a73-ffd6bea99933
Stop agent from testing authorization when making controller unit tests (authorization not active during unit tests) (make controller test dev SKILL.md)

----
llamma.cpp commands:

cd G:\llama

Best working model:
.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ4_XS-4.15bpw -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --cache-type-k q8_0 --cache-type-v q5_1 --n-cpu-moe 22 --flash-attn on -b 2048 -ub 2048

Very fast but needs more testing if not too stupid:
.\llama-server -hf byteshape/Qwen3.6-35B-A3B-GGUF:Qwen3.6-35B-A3B-IQ2_S-2.17bpw -c 32768 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --cache-type-k q5_0 --cache-type-v q4_1 --flash-attn on -b 2048 -ub 2048

continue codecompletion (not working well, much worse than windsurf)
.\llama-server -hf bartowski/Qwen2.5-Coder-1.5B-Instruct-GGUF:Q4_K_M -c 4096 --no-mmap --cache-type-k q8_0 --cache-type-v q4_0 -ngl 99 --flash-attn on --no-ui

Q6_K_L 
.\llama-server.exe -hf bartowski/Qwen_Qwen3.6-35B-A3B-GGUF:Q6_K_L -c 65536 --mmproj-auto --temp 0.6 --top-k 20 --top-p 0.95 --min-p 0 --presence-penalty 0 --repeat-penalty 1 --parallel 1 --no-mmap --api-key anything --no-context-shift --flash-attn -b 2048 -ub 2048 --cache-type-k q8_0 --cache-type-v q5_1 --n-cpu-moe 32 --no-ui 

----

Permanently set env vars for model location
setx HF_HOME "G:\llamacache\hf-cache"
setx LLAMA_CACHE "G:\llamacache\llama-cache"

----