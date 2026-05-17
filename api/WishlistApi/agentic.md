Trying:  
VS Code  
Cline plugin  
Ollama with model Qwen3.6  

Save commands:
cd api/WishlistApi; dotnet test
dotnet test api/WishlistApi/WishlistApi.sln

Next:
AddWishlistItemAsync: make a unit test at the WishlistService level that would catch that DateAdded was not correctly set to UtcNow. 

Add WishlistItem domain class. Refactor where necessary to use the domain class instead of WishlistItem entity class
