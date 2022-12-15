using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage.Blobs.Models;
using Azure;

namespace aca;

[ApiController]
[Route("api/[controller]")]
public class BlobController : ControllerBase
{
    private readonly ILogger<BlobController> _logger;
    private IConfiguration _configuration;

    const string BLOB = "b";
    const string CONTAINER = "c";
    const string SAMPLE = "s";

    const string TEMP_LOC = "tempContainer";

    public BlobController(ILogger<BlobController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

   

    [HttpPost]
    public async Task<ActionResult<string>> CopyBlob(BlobRequest item)
    {        
        // validate inputs
        if(item.Validate())
        {
        _logger.LogInformation($"BlobController::CopyBlob with type: {item.RequestType} starts");
        string sourceCS = _configuration.GetValue<string>(item.SourceCS);
        string targetCS = _configuration.GetValue<string>(item.TargetCS);

        string sampleFileUri = _configuration.GetValue<string>("SampleFileUri");

        BlobContainerClient sourceBlobClient = new BlobContainerClient(sourceCS,item.SourceContainer);
        BlobContainerClient targetBlobClient = new BlobContainerClient(targetCS,item.TargetContainer);
        string sas = GetServiceSasUriForContainer(sourceBlobClient);
        // create uri for the download
        if(BLOB.Equals(item.RequestType) && !string.IsNullOrEmpty(sas)){
            BlobClient sourceBlob = sourceBlobClient.GetBlobClient(item.BlobName);
            BlobClient destBlob = targetBlobClient.GetBlobClient(item.BlobName);
            await CopySingle(sourceBlob,destBlob,sas);            
            
            return $"Copied single blob {item.BlobName} from source:{item.SourceContainer}";
            // copy single file
        }else if(CONTAINER.Equals(item.RequestType)){
            // copy entire container
            await CopyContainer(sourceBlobClient,targetBlobClient,5000,sas);
            return $"Copied container {item.SourceContainer} to {item.TargetContainer}";
        }else if(SAMPLE.Equals(item.RequestType)){
            _logger.LogInformation($"BlobController::CopyBlob::Creating samples.");
            BlobContainerClient localBlobClient = new BlobContainerClient(sourceCS,TEMP_LOC);
            _logger.LogInformation($"BlobController::CopyBlob::Creating samples. local blob client created");
            BlobClient localBlob = localBlobClient.GetBlobClient("temp.data");
            // Uri uri = new Uri(sampleFileUri);
            // _logger.LogInformation($"BlobController::CopyBlob::Creating samples. uri created: {uri.AbsoluteUri}");
            // localBlob.StartCopyFromUri(uri);          
            _logger.LogInformation($"BlobController::CopyBlob::Creating samples. copy started");  
            await CreateSample(localBlob,targetBlobClient,item.SampleSize);
            
            // creating data samples
            return $"Created {item.SampleSize} samples in {item.TargetContainer} ";
        }else{
            
            // wrong type passed
            return "Invalid input provided. ";
        }
      
        }else{
            _logger.LogInformation($"BlobController::CopyBlob::Validation error. ");            
            return "Invalid input provided. ";
        }
        

    }

   

    private async Task CreateSample(BlobClient localBlob, BlobContainerClient destBlobContainer, int sampleSize)
    {
        for (int i = 0; i < sampleSize; i++)
        {
            BlobClient destBlob = destBlobContainer.GetBlobClient($"datafile{i}.json");
            await destBlob.StartCopyFromUriAsync(localBlob.Uri);
        }
    }

    private async Task CopySingle(BlobClient sourceBlob,BlobClient destBlob, string sas)
    {
        Uri uri = new Uri ($"{sourceBlob.Uri.AbsoluteUri}?{sas}");
        // _logger.LogInformation($"the uri is:{uri}");
        await destBlob.StartCopyFromUriAsync(uri);
        // _logger.LogInformation($"BlobController::Copied single blob {sourceBlob.Name}");
    }

    private async Task CopyContainer(BlobContainerClient sourceBlobClient, BlobContainerClient targetBlobClient,  int? segmentSize, string sas)
                                               
    {
        try
        {
            _logger.LogInformation("BlobController::CopyMulti starts");
            // Call the listing operation and return pages of the specified size.
            var resultSegment = sourceBlobClient.GetBlobsAsync()
                .AsPages(default, segmentSize);

            // Enumerate the blobs returned for each page.
            await foreach (Azure.Page<BlobItem> blobPage in resultSegment)
            {                
                foreach (BlobItem blobItem in blobPage.Values)
                {                    
                    if(blobItem.Properties.ContentLength>0)
                        await CopySingle(sourceBlobClient.GetBlobClient(blobItem.Name),
                                         targetBlobClient.GetBlobClient(blobItem.Name),sas);    
                }
                _logger.LogInformation("BlobController::CopyMulti copied page of blobs");
                
            }
        }
        catch (RequestFailedException e)
        {
            // might need to contain (not throw up) the exceptio, allowing failure copies to continue
           _logger.LogInformation($"BlobController::CopyMulti failed with exception:{e}");
           throw;
        }
    }

    private string GetServiceSasUriForContainer(BlobContainerClient containerClient )
    {
        // Check whether this BlobContainerClient object has been authorized with Shared Key.
        if (containerClient.CanGenerateSasUri)
        {
            // Create a SAS token that's valid for one hour.
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = containerClient.Name,
                Resource = "b"
            };

            sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(10);
            sasBuilder.SetPermissions(BlobSasPermissions.All);        
            

            Uri sasUri = containerClient.GenerateSasUri(sasBuilder);

            return sasUri.AbsoluteUri.Split('?')[1];;
        }
        else
        {
            _logger.LogInformation(@"BlobContainerClient must be authorized with Shared Key 
                            credentials to create a service SAS.");
            return string.Empty;
        }
    }

}


