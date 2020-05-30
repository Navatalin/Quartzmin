﻿#if ( NETSTANDARD || NETCOREAPP )

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Reflection;

namespace Quartzmin
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseQuartzmin( this IApplicationBuilder app, QuartzminOptions options, Action<Services> configure = null )
        {
            options = options ?? throw new ArgumentNullException( nameof( options ) );

            app.UseFileServer( options );

            var services = Services.Create( options );
            configure?.Invoke( services );

            app.Use( async ( context, next ) =>
             {
                 context.Items[typeof( Services )] = services;
                 await next.Invoke();
             } );

#if NETCOREAPP
            app.Use(async (context,next) =>
            {
                var url = context.Request.Path.Value;

                // Redirect to an external URL
                if (!string.IsNullOrWhiteSpace(options.VirtualPathRoot) && options.VirtualPathRoot!="/" && options.VirtualPathRoot!=$"/Quartzmin" && url.Contains($"{options.VirtualPathRoot}"))
                {
                     context.Request.Path = url.Replace($"{options.VirtualPathRoot}",($"{options.VirtualPathRoot}".StartsWith("/")?"/":"")+$"Quartzmin"+($"{options.VirtualPathRoot}".EndsWith("/")?"/":""));
                }
                await next();
            });
            //app.UseEndpoints( endpoints =>
            //{
            //    endpoints.MapControllerRoute( nameof( Quartzmin ),pattern:$"{options.VirtualPathRoot}/", $"{options.VirtualPathRoot}/{{controller=Scheduler}}/{{action=Index}}" );
            //} );
#else
            app.UseMvc( routes =>
            {
                routes.MapRoute(
                    name: nameof( Quartzmin ),
                    template: "{controller=Scheduler}/{action=Index}" );
            } );
#endif
        }

        private static void UseFileServer( this IApplicationBuilder app, QuartzminOptions options )
        {
            IFileProvider fs;
            if ( string.IsNullOrEmpty( options.ContentRootDirectory ) )
                fs = new ManifestEmbeddedFileProvider( Assembly.GetExecutingAssembly(), "Content" );
            else
                fs = new PhysicalFileProvider( options.ContentRootDirectory );

            var fsOptions = new FileServerOptions()
            {
                RequestPath = new PathString( $"{options.VirtualPathRoot}/Content" ),
                EnableDefaultFiles = false,
                EnableDirectoryBrowsing = false,
                FileProvider = fs
            };

            app.UseFileServer( fsOptions );
        }

#if NETCOREAPP
        public static void AddQuartzmin( this IServiceCollection services )
        {
            services.AddControllers()
                .AddApplicationPart( Assembly.GetExecutingAssembly() )
                .AddNewtonsoftJson();
        }
#else
        public static void AddQuartzmin( this IServiceCollection services )
        {
            services.AddMvcCore()
                .AddApplicationPart( Assembly.GetExecutingAssembly() )
                .AddJsonFormatters();
        }
#endif

    }
}

#endif
