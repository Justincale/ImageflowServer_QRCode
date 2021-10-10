using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
//using Azure;
//using Azure.Storage.Blobs;
//using Azure.Storage.Blobs.Models;
using Imageflow.Server.Storage.AzureBlob;
using Imazen.Common.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageflowServer
{

    public static class QRBlobServiceExtensions
    {
        public static IServiceCollection AddImageflowCustomBlobService(this IServiceCollection services,
            QRBlobServiceOptions options)
        {
            services.AddSingleton<IBlobProvider>((container) =>
            {
                var logger = container.GetRequiredService<ILogger<QRBlobService>>();
                return new QRBlobService(options, logger);
            });

            return services;
        }
    }

    public class QRBlobServiceOptions
    {

        //public BlobClientOptions BlobClientOptions { get; set; } = new BlobClientOptions();

        /// <summary>
        /// Can block container/key pairs by returning null
        /// </summary>
        public Func<string, string, Tuple<string, string>> ContainerKeyFilterFunction { get; set; }
            = Tuple.Create;
        //public string ConnectionString { get; set; }
        public bool IgnorePrefixCase { get; set; } = true;

        private string prefix = "/qrcode/";

        //public DataMisalignedException {get;set }


        /// <summary>
        /// Ensures the prefix begins and ends with a slash
        /// </summary>
        /// <exception cref="ArgumentException">If prefix is / </exception>
        public string Prefix
        {
            get => prefix;
            set
            {
                var p = value.TrimStart('/').TrimEnd('/');
                if (p.Length == 0)
                {
                    throw new ArgumentException("Prefix cannot be /", nameof(p));
                }
                prefix = '/' + p + '/';
            }
        }
    }


    public class QRBlobService : IBlobProvider
    {
        //private readonly BlobServiceClient client;

        private QRBlobServiceOptions options;

        public QRBlobService(QRBlobServiceOptions options, ILogger<QRBlobService> logger)
        {
            this.options = options;

            //client = new BlobServiceClient(options.ConnectionString, options.BlobClientOptions);
        }

        public IEnumerable<string> GetPrefixes()
        {
            return Enumerable.Repeat(options.Prefix, 1);
        }

        public bool SupportsPath(string virtualPath)
            => virtualPath.StartsWith(options.Prefix,
                options.IgnorePrefixCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);


        public async Task<IBlobData> Fetch(string virtualPath)
        {
            if (!SupportsPath(virtualPath))
            {
                return null;
            }
            var path = virtualPath.Substring(options.Prefix.Length).TrimStart('/');
            var indexOfSlash = path.IndexOf('/');
            if (indexOfSlash < 1) return null;

            //var container = path.Substring(0, indexOfSlash);
            var data = path.Substring(indexOfSlash + 1);

            //var filtered = options.ContainerKeyFilterFunction(container, blobKey);

            //if (filtered == null)
            //{
            //    return null;
            //}

            //container = filtered.Item1;
            //blobKey = filtered.Item2;

            try
            {
                //var blobClient = client.GetBlobContainerClient(container).GetBlobClient(blobKey);


                //Generate QRCode
                //BlobDownloadInfo qrCOdeInfo = new BlobDownloadInfo(new BlobContentInfo());

                //var s = new ();// await blobClient.DownloadAsync();
                //Respons

                return new QRBlob(data);

            }
            catch (RequestFailedException e)
            {
                if (e.Status == 404)
                {
                    throw new BlobMissingException($"QRCode \"{data}\" not found.", e);
                }

                throw;

            }
        }
    }
    internal class QRBlob : IBlobData, IDisposable
    {
        public readonly string _code;

        internal QRBlob(string code)
        {
            _code = code;
        }

        public bool? Exists => true;
        public DateTime? LastModifiedDateUtc => DateTime.UtcNow;// response.Value.Details.LastModified.UtcDateTime;
        public Stream OpenRead()
        {
            return Open();
        }

        public void Dispose()
        {
            Dispose();
        }


        public System.IO.Stream Open()
        {
            MemoryStream ms = new MemoryStream();
            using (Bitmap b = GetBitmap())
            {
                b.Save(ms, ImageFormat.Png);
            }
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        public Bitmap GetBitmap()
        {
            Bitmap qrImg = null;
            try
            {
                qrImg = QRCodeService.GenerateQRCode(_code, 200, 200);
            }
            catch
            {
                if (qrImg != null) qrImg.Dispose();
                throw;
            }
            return qrImg;
        }

    }
}