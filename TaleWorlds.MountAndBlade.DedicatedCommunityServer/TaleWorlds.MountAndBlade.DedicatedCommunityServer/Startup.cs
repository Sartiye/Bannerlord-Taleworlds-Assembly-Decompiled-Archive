using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TaleWorlds.MountAndBlade.DedicatedCommunityServer;

public class Startup
{
	public IConfiguration Configuration { get; }

	public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddMemoryCache();
		NewtonsoftJsonMvcBuilderExtensions.AddNewtonsoftJson(services.AddMvc().AddSessionStateTempDataProvider());
		services.AddAuthorization();
		services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}
		Console.SetOut(new CacheTextWriter((IMemoryCache)app.ApplicationServices.GetService(typeof(IMemoryCache)), Console.Out));
		app.UseStaticFiles();
		app.UseRouting();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseEndpoints(delegate(IEndpointRouteBuilder endpoints)
		{
			endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
		});
	}
}
