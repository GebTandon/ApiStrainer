
## ToDo ##
- Add Sliding Window Token Repo.
- Use Fody for Aspect Weaving around Flurl, WebClient and such high level API Call objects.
- Create another ITokenRepo implementation that uses sliding window concept.
- The projects have hard references, instead of Nuget package reference among themselves.
- THe 'TokenGen.ConsoleApp' project is the example of how a client can utilize the services.
- The 'TokenDispnser' is the gRPC server that issues tokens and protects the target APIs from rate/ call overloading.
- THe IKeepStats implementation needs some rethinking, so that 
  - it can keep separate counts based on whether the event source is ILimitRate or ILimitWindow
  - this will also require some re-thinking of ITokenRepository interface especially to distribute the events in other marker interfaces (ILimitRate or ILimitWindow).
  - 