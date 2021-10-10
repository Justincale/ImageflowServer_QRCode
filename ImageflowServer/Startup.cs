using Azure.Storage.Blobs;
using Imageflow.Fluent;
using Imageflow.Server;
using Imageflow.Server.Storage.AzureBlob;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageflowServer
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Make Azure container available at /azure
            // You can call AddImageflowAzureBlobService multiple times for each connection string
            services.AddImageflowAzureBlobService(
                new AzureBlobServiceOptions(
                        "UseDevelopmentStorage=true;",
                        new BlobClientOptions())
                    .MapPrefix("/azure", "blobs"));


            services.AddImageflowCustomBlobService(new QRBlobServiceOptions()
            {
                Prefix = "/images/",
                IgnorePrefixCase = true,
                //ConnectionString = "UseDevelopmentStorage=true;",
                // Only allow 'my_container' to be accessed. /custom_blobs/my_container/key.jpg would be an example path.
                ContainerKeyFilterFunction = (container, key) =>
                    container == "qrcode" ? Tuple.Create(container, key) : null
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // You have a lot of configuration options
            app.UseImageflow(new ImageflowMiddlewareOptions()
                // Maps / to WebRootPath
                .SetMapWebRoot(true)
                // You can get a license key at https://imageresizing.net/
                .SetMyOpenSourceProjectUrl("https://github.com/imazen/imageflow-dotnet-server")
                // Maps /folder to WebRootPath/folder
                //.MapPath("/folder", Path.Combine(Env.ContentRootPath, "folder"))
                // Allow localhost to access the diagnostics page or remotely via /imageflow.debug?password=fuzzy_caterpillar
                //.SetDiagnosticsPageAccess(env.IsDevelopment() ? AccessDiagnosticsFrom.AnyHost : AccessDiagnosticsFrom.LocalHost)
                //.SetDiagnosticsPagePassword("1liket0W@lk")
                // Allow HybridCache or other registered IStreamCache to run
                //.SetAllowCaching(true)
                // Cache publicly (including on shared proxies and CDNs) for 30 days
                //.SetDefaultCacheControlString("public, max-age=2592000")
                // Allows extensionless images to be served within the given directory(ies)
                .HandleExtensionlessRequestsUnder("/images/", StringComparison.OrdinalIgnoreCase)
                // Force all paths under "/gallery" to be watermarked
                //.AddRewriteHandler("/gallery", args =>
                //{
                //    args.Query["watermark"] = "imazen";
                //})
                .AddCommandDefault("down.filter", "mitchell")
                .AddCommandDefault("f.sharpen", "15")
                .AddCommandDefault("webp.quality", "90")
                .AddCommandDefault("ignore_icc_errors", "true")
                //When set to true, this only allows ?preset=value URLs, returning 403 if you try to use any other commands. 
                .SetUsePresetsExclusively(false)
                .AddPreset(new PresetOptions("large", PresetPriority.DefaultValues)
                    .SetCommand("width", "1024")
                    .SetCommand("height", "1024")
                    .SetCommand("mode", "max"))
                // When set, this only allows urls with a &signature, returning 403 if missing/invalid. 
                // Use Imazen.Common.Helpers.Signatures.SignRequest(string pathAndQuery, string signingKey) to generate
                //.ForPrefix allows you to set less restrictive rules for subfolders. 
                // For example, you may want to allow unmodified requests through with SignatureRequired.ForQuerystringRequests
                // .SetRequestSignatureOptions(
                //     new RequestSignatureOptions(SignatureRequired.ForAllRequests, new []{"test key"})
                //         .ForPrefix("/logos/", StringComparison.Ordinal, 
                //             SignatureRequired.ForQuerystringRequests, new []{"test key"}))
                // It's a good idea to limit image sizes for security. Requests causing these to be exceeded will fail
                // The last argument to FrameSizeLimit() is the maximum number of megapixels
                .SetJobSecurityOptions(new SecurityOptions()
                    .SetMaxDecodeSize(new FrameSizeLimit(8000, 8000, 40))
                    .SetMaxFrameSize(new FrameSizeLimit(8000, 8000, 40))
                    .SetMaxEncodeSize(new FrameSizeLimit(8000, 8000, 20)))
                // Register a named watermark that floats 10% from the bottom-right corner of the image
                // With 70% opacity and some sharpness applied. 
                //.AddWatermark(
                //    new NamedWatermark("imazen",
                //        "/images/imazen_400.png",
                //        new WatermarkOptions()
                //            .SetFitBoxLayout(
                //                new WatermarkFitBox(WatermarkAlign.Image, 10, 10, 90, 90),
                //                WatermarkConstraintMode.Within,
                //                new ConstraintGravity(100, 100))
                //            .SetOpacity(0.7f)
                //            .SetHints(
                //                new ResampleHints()
                //                    .SetResampleFilters(InterpolationFilter.Robidoux_Sharp, null)
                //                    .SetSharpen(7, SharpenWhen.Downscaling))
                //            .SetMinCanvasSize(200, 150)))
                //.AddWatermarkingHandler("/", args =>
                //{
                //    if (args.Query.TryGetValue("water", out var value) && value == "mark")
                //    {
                //        args.AppliedWatermarks.Add(new NamedWatermark(null, "/images/imazen_400.png", new WatermarkOptions()));
                //    }
                //}
                );




            app.UseRouting();
            app.UseHttpsRedirection();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<img src=\"fire-umbrella-small.jpg?width=450\" />");
                });
            });
        }
    }
}
