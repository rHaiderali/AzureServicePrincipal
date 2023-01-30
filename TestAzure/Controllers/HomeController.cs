using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TestAzure.Models;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Azure.Identity;
using Azure.Storage.Blobs.Models;

namespace TestAzure.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private string _connectionString = "DefaultEndpointsProtocol=https;AccountName=azuressi;AccountKey=4cH5gmBkzQ2ZWLBMsYmB10o/ZoIj57JPCERsMu3yr8GealP4kt7+k+2wRlTJmevVz9ET/lX/zFYs+AStE0ecHg==;EndpointSuffix=core.windows.net";
        private string _containerName = "privatesamplecontainer";
        private string tenantId = "8481bada-56c8-4a93-bddc-96a4a7a73139";
        private string clientId = "b5e4f765-c400-41d9-9327-3a6e3aac2574";
        private string clientSecret = "u3i8Q~2zv9oI9YTu_Q0JvmqiDpc3wpLCytGzvcxv";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            IList<BlobDTO> blobList = new List<BlobDTO>();
            try
            {
                _logger.LogError("Started Azure Storage listing Method.");
                BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClients = containerClient.GetBlobs();
                foreach (var blob in blobClients)
                {
                    var NameExt = blob.Name.Split('.');
                    var data = new BlobDTO
                    {
                        Name = NameExt[0],
                        Extension = NameExt[1]
                    };
                    blobList.Add(data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Found in Azure Storage listing Method.");
                return RedirectToAction("Error");

            }
            return View(blobList);

        }
        public IActionResult Detail(string FileName)
        {
            try
            {
                _logger.LogError("Started Azure Storage Detail Method.");
                BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                BlobClient blobClient = containerClient.GetBlobClient(FileName);
                MemoryStream memoryStream = new MemoryStream();
                blobClient.DownloadTo(memoryStream);
                memoryStream.Position = 0;
                string content = new StreamReader(memoryStream).ReadToEnd();
                string[] str = content.Split(';');
                ViewBag.showValue = str;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Found in Azure Storage Detail Method.");
                return RedirectToAction("Error");

            }
            return View();
        }
        public IActionResult Delete(string FileName)
        {
            try
            {
                _logger.LogError("Started Azure Storage Delete Method.");
                BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                BlobClient blobClient = containerClient.GetBlobClient(FileName);
                blobClient.Delete();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Found in Azure Storage Delete Method.");
                return RedirectToAction("Error");

            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult UploadFile(IFormFile FileName)
        {
            try
            {
                _logger.LogError("Started Azure Storage UploadFile Method.");
                BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                MemoryStream memoryStream = new MemoryStream();
                FileName.CopyTo(memoryStream);
                memoryStream.Position = 0;
                containerClient.UploadBlob(Path.GetFileName(FileName.FileName), memoryStream);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Found in Azure UploadFile Method.");
                return RedirectToAction("Error");

            }

            return RedirectToAction("Index");
        }
        //Using service principal
        public IActionResult ListOfStorage()
        {
            IList<BlobDTO> blobList = new List<BlobDTO>();
            var tokenCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var blobServiceClient = new BlobServiceClient(
                new Uri("https://azuressi.blob.core.windows.net/"),
                tokenCredential);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobs();
            foreach (var blob in blobClient)
            {
                var NameExt = blob.Name.Split('.');
                var data = new BlobDTO
                {
                    Name = NameExt[0],
                    Extension = NameExt[1]
                };
                blobList.Add(data);
            }
            return View(blobList);
        }
        public async Task<IActionResult> VMachines()
        {
            IList<BlobDTO> VMList = new List<BlobDTO>();
            try
            {
                _logger.LogError("Started Azure VMachines Method.");
                AzureCredentials credentials = new AzureCredentials(new ServicePrincipalLoginInformation
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                tenantId, AzureEnvironment.AzureGlobalCloud);

                ComputeManagementClient computeManagementClient = new ComputeManagementClient(credentials)
                {
                    SubscriptionId = "f1a54680-3338-4df9-aaf9-fc6d6d5aac41"
                };
                var vms = await computeManagementClient.VirtualMachines.ListAllAsync();
                foreach (var vm in vms)
                {
                    var data = new BlobDTO
                    {
                        Name = vm.Name
                    };
                    VMList.Add(data);
                }
                if (VMList.Count < 1)
                {
                    ViewBag.ErrorNoFound = "No Virtual Machines are found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Found in Azure VMachines listing Method.");
                return RedirectToAction("Error");

            }
            return View(VMList);
        }
  

    [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel{RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}