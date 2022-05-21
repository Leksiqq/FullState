
# Net.Leksi.FullState

The library provides the standard use of the Dependency Injection mechanism in ASP.NET Core applications with persistence state between requests during the session (full state). 
Allows session context services to be registered with the DI container and retrieved from the container in the normal way. 
Also provides the ability to access request context services in session context service methods.

## Prerequisites
1. Target platform: .NET 6.0
2. Target CPU: Any

## Usage

	var builder = WebApplication.CreateBuilder(args);

	...

	builder.Services.AddFullState();

	// For interfaces being used as sessional or request scoped
	builder.Services.AddScoped<...>();
	builder.Services.AddScoped<...>();

	...

	var app = builder.Build();

	app.UseFullState();

To obtain a sessional object is is necessary to obtain session by: 

    IFullState session = service.GetFullState();

on any IServiceProvider you have, then

    ISomeIterface obj = session.SessionServices.GetService<ISomeIterface>();


## API
See [Documentation](https://fullstate.sourceforge.io/index_en.html)
