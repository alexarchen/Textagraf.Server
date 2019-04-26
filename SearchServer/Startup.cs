using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SearchServer.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Docodo;
using Newtonsoft.Json.Serialization;
using System.IO;
using Microsoft.AspNetCore.DataProtection;

namespace SearchServer
{
    public static class Utils
    {

        public static string FormatDate(this DateTime dateTime)
        {
            var t = DateTime.Now;
            if (t.Date.Equals(dateTime.Date)) { return dateTime.ToShortTimeString(); }
            TimeSpan ts = new TimeSpan(t.Date.Ticks - dateTime.Date.Ticks);
            if (ts.TotalDays<=1)
            {
                return "Yesterday "+dateTime.ToShortTimeString(); 
            }

            if (ts.TotalDays < 3) return dateTime.ToString("d MMM") + " " + dateTime.ToShortTimeString();

            if (dateTime.Year == t.Year) return dateTime.ToString("d MMM");
            
            return dateTime.ToShortDateString();
        }

    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });



            string connection = Configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<UserContext>((optbld) => { optbld.UseSqlServer(connection); /*optbld.UseMySQL(connection); */});


            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = true;
            });

            Directory.CreateDirectory(Path.Combine(Configuration["Folders:Storage"],"keys"));
            services.AddDataProtection()
           // This helps surviving a restart: a same app will find back its keys. Just ensure to create the folder.
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Configuration["Folders:Storage"], "keys")))
           // This helps surviving a site update: each app has its own store, building the site creates a new app
           .SetApplicationName("Textagraf")
           .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

            //Microsoft.AspNetCore.Identity.UI.Services.IEmailSende

            /*services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie((opt)=> {
                opt.LoginPath = "Account/Login";
                opt.AccessDeniedPath = "Account/Login";
                opt.LogoutPath = "Account/Logout";
                opt.ExpireTimeSpan = TimeSpan.FromSeconds(300000);
            });
            */
            services.AddIdentity<User, IdentityRole<int>>(opts => {
                opts.User.RequireUniqueEmail = true;
                opts.Password.RequiredLength = Configuration.GetValue<int>("Password:Length", 5);   
                opts.Password.RequireNonAlphanumeric = Configuration.GetValue<bool>("Password:Alphanumeric", false);
                opts.Password.RequireLowercase = false; 
                opts.Password.RequireUppercase = Configuration.GetValue<bool>("Password:Register", false); 
                opts.Password.RequireDigit = Configuration.GetValue<bool>("Password:RequireDigit", false);
                opts.Lockout.AllowedForNewUsers = false;
                opts.SignIn.RequireConfirmedPhoneNumber = false;
                opts.SignIn.RequireConfirmedEmail = false;
                
            }).AddEntityFrameworkStores<UserContext>().AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options => options.LoginPath = "/Identity/Account/LogIn");

            services.AddAuthorization();
/*            (opt)=> {
                opt.AddPolicy("Confirmed",policy=>policy.RequireAssertion(async (c)=> {
                    return (await app.ApplicationServices.GetRequiredService<UserManager<User>>().GetUserAsync(c.User)).IsEmailConfirmed;
                    }
                )
            });*/

            services.AddTransient<IEmailSender, EmailSender>(i =>
                new EmailSender(
                    Configuration["Email:Title"],
                    Configuration["Email:Email"],
                    Configuration["Email:Host"],
                    Configuration.GetValue<int>("Email:Port"),
                    Configuration.GetValue<bool>("Email:EnableSSL"),
                    Configuration["Email:UserName"],
                    Configuration["Email:Password"]
                )
            );


            /*
            services.AddEFSecondLevelCache();

            // Add an in-memory cache service provider
            services.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));
            services.AddSingleton(typeof(ICacheManagerConfiguration),
                new CacheManager.Core.ConfigurationBuilder()
                        .WithJsonSerializer()
                        .WithMicrosoftMemoryCacheHandle(instanceName: "MemoryCache1")
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                        .Build());
                        */

            services.AddSingleton<IDocumentProcessor,DocumentProcessor>();
            services.AddHttpsRedirection(o=>o.HttpsPort = 443);
            services.AddMemoryCache();//opt=>opt.SizeLimit = long.Parse(Configuration["MaxMemCache"])
            // size doesn't work with Entity Framework

            services.AddDocodo(Configuration["Folders:Index"]).AddEFDataSource<UserContext,Models.Document>("doc", (c)=>c.Document.Include(d=>d.Group).Where(d=>((d.Group==null) || (d.Group.Type!=Group.GroupType.Private))),Docodo.DBDataSourceBase.IndexType.File,Configuration["Folders:Docs"],"File").EnsureCreated();
            services.AddSingleton<IFileUploader,FileUploader>(i=>new FileUploader(Configuration["Folders:Temp"]));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1).AddJsonOptions(options =>
            {
                options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            }); 

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            //app.UseEFSecondLevelCache();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSession();

            app.UseAuthentication();
            app.UseHttpsRedirection();

            app.UseMvc(routes =>
            {
                routes.MapRoute("search", "search/{action}", new { controller = "Search", action = "Search" });

                routes.MapRoute("docs", "docs/{action=Index}/{Id?}/{*pg}", new { controller = "Documents" });

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapRoute("userbyname", "@{name}", new { Controller = "users", Action = "Name" });
                routes.MapRoute("groupbyname", ":{name}", new { Controller = "groups", Action = "Name" });
            });

            

            Controllers.DocumentsController.DocFolder = Configuration["Folders:Docs"] + "/";
            Controllers.StorageController.Folder = Configuration["Folders:Storage"];
            Controllers.HomeController.HelpUrl = Configuration["Help"];

            await Init(app.ApplicationServices.GetRequiredService<RoleManager<IdentityRole<int>>>(),
                 app.ApplicationServices.GetRequiredService<UserManager<User>>()
                );

            if (env.IsDevelopment())
            {
               // await app.ApplicationServices.GetRequiredService<ITest>().Test();
            }
            
        }

        async Task Init(RoleManager<IdentityRole<int>> mng, UserManager<User> mngu)
        {
            // create roles
            
            string[] Roles = Configuration.GetValue<string>("Init:Roles").Split(",");
            foreach (string s in Roles)
            {
                if (!await mng.RoleExistsAsync(s))
                {
                    IdentityRole<int> role = new IdentityRole<int>(){ Name = s };
                    if (!(await mng.CreateAsync(role)).Succeeded)
                    {
                        throw new Exception($"Could not create '{role.Name}' role.");
                    }
                }
            }

            User user = await mngu.FindByEmailAsync(Configuration["Init:Admin:Email"]);
//            if (user!=null) { await mngu.DeleteAsync(user); user = null; }
            if (user == null)
            {
                user = new User() { UserName = "Admin", Email = Configuration["Init:Admin:Email"].ToString(), EmailConfirmed = true, LockoutEnabled =true };
                await mngu.CreateAsync(user, Configuration["Init:Admin:Password"]);
                await mngu.AddToRoleAsync(user, Roles[0]);
            }
            
        }

    }
}
