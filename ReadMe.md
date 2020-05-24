
## ToDo ##
- Add Sliding Window Token Repo.
- Use Fody for Aspect Weaving around Flurl, WebClient and such high level API Call objects.
- Create another ITokenRepo implementation that uses sliding window concept.
- The projects have hard references, instead of Nuget package reference among themselves.
- THe 'TokenGen.ConsoleApp' project is the example of how a client can utilize the services.
- The 'TokenDispnser' is the gRPC server that issues tokens and protects the target APIs from rate/ call overloading.
- The IKeepStats implementation needs some rethinking, so that 
  - it can keep separate counts based on whether the event source is ILimitRate or ILimitWindow
  - this will also require some re-thinking of ITokenRepository interface especially to distribute the events in other marker interfaces (ILimitRate or ILimitWindow).
- Add Unit tests for Validation logic
- Add Integration tests for the server project.
- Add Integration tests for the client project.


## How to Use ##
- The TokeGenLib is the library that is core of the project.
  - Keeps track of the APIs, tracks Instantaneous API call counts and Total counts within a time frame.
- The tracking of APIs can be done in process as well as out of process.
  - InProcess - add reference to the TokenGenLib directly in the project
    - To track only one API server, use RegisterTokenMonitor.Register calls with that server name.
    - To Track multiple API servers, call RegisterTokenMonitor.Register multiple times for each server tracked.
  - Out of Process - Run the TockenDispenser instance, and the TockenDispenser.ClientLib library.
    - To track one server, instantiate only one TockenDispenser server.
    - To track multiple servers, instantiate multiple of such servers.
- 
- The Reporting APIs are under development, and not fully flushed out.

