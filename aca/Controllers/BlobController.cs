using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage.Blobs.Models;
using Azure;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;

namespace aca;

[ApiController]
[Route("api/[controller]")]
public class BlobController : ControllerBase
{
    private readonly ILogger<BlobController> _logger;
    private IConfiguration _configuration;

    private IBackgroundTaskQueue _queue;

    const string BLOB = "b";
    const string CONTAINER = "c";
    const string SAMPLE = "s";

    const string TEMP_LOC = "sample";

    const int MAX_SAMPLE_SIZE = 125;

    public BlobController(ILogger<BlobController> logger, IConfiguration configuration, IBackgroundTaskQueue queue)
    {
        _logger = logger;
        _configuration = configuration;
        _queue = queue;
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
      
        BlobContainerClient sourceBlobClient = new BlobContainerClient(sourceCS,item.SourceContainer);
        sourceBlobClient.CreateIfNotExists();
        BlobContainerClient targetBlobClient = new BlobContainerClient(targetCS,item.TargetContainer);
        targetBlobClient.CreateIfNotExists();
        string sas = GetServiceSasUriForContainer(sourceBlobClient);
        // create uri for the download
        if(BLOB.Equals(item.RequestType) && !string.IsNullOrEmpty(sas)){
            BlobClient sourceBlob = sourceBlobClient.GetBlobClient(item.BlobName);
            BlobClient destBlob = targetBlobClient.GetBlobClient(item.BlobName);
            await CopySingle(sourceBlob,destBlob,sas);            
            
            return $"Copied single blob {item.BlobName} from source:{item.SourceContainer}";
            // copy single file
        }else if(CONTAINER.Equals(item.RequestType)){
            _logger.LogInformation($"BlobController::CopyBlob::Copy entire container content.");
            // creating a background task
            var workItem = new Func<CancellationToken, ValueTask>(async token =>
                {
                    _logger.LogInformation(
                        $"Starting work item {item.RequestType} at: {DateTimeOffset.Now}");
                    // do the copy here
                    await CopyContainer(sourceBlobClient,targetBlobClient,5000,sas);
                    _logger.LogInformation($"Work item {item.RequestType} completed at: {DateTimeOffset.Now}");
   
                });
            await _queue.QueueBackgroundWorkItemAsync(workItem);
        
            return Accepted($"Copy container task Created: {item.SourceContainer} to {item.TargetContainer}");
            // copy entire container
        }else if(SAMPLE.Equals(item.RequestType) && !string.IsNullOrEmpty(item.BlobName)){
            _logger.LogInformation($"BlobController::CopyBlob::Creating samples.");   
            if(item.SampleSize > MAX_SAMPLE_SIZE){
                _logger.LogInformation($"BlobController::Sample size {item.SampleSize} is bigger than threshold {MAX_SAMPLE_SIZE}, dividing by 2 and creating two tasks again.");
                int newSize = item.SampleSize/2;
                BlobRequest newItem = new BlobRequest();
                newItem.CallonUrl = item.CallonUrl;
                newItem.SourceCS = item.SourceCS;
                newItem.TargetCS = item.TargetCS;
                newItem.SourceContainer = item.SourceContainer;
                newItem.SampleSize = newSize;
                newItem.TargetContainer = item.TargetContainer;
                newItem.BlobName = item.BlobName;
                newItem.RequestType = SAMPLE;
                var CallOnItem = new Func<CancellationToken, ValueTask>(async token =>
                {
                    _logger.LogInformation($"Starting work item {item.RequestType} at: {DateTimeOffset.Now}");
                    // do the copy here
                    await CallOn(newItem);
                    _logger.LogInformation($"Work item {item.RequestType} completed at: {DateTimeOffset.Now}");
   
                });
                await _queue.QueueBackgroundWorkItemAsync(CallOnItem);
                newItem.SampleSize = item.SampleSize - newSize; // remaining size might not be even, so we need to calculate the remaining size
                var CallOnItem2 = new Func<CancellationToken, ValueTask>(async token =>
                {
                    _logger.LogInformation($"Starting work item {item.RequestType} at: {DateTimeOffset.Now}");
                    // do the copy here
                    await CallOn(newItem);
                    _logger.LogInformation($"Work item {item.RequestType} completed at: {DateTimeOffset.Now}");
   
                });
                await _queue.QueueBackgroundWorkItemAsync(CallOnItem2);
                return Accepted($"Sample task Created: {item.BlobName} with size {item.SampleSize}");
            }else{
                // download the file to a temporary location (sample container)        
                BlobContainerClient localBlobClient = new BlobContainerClient(sourceCS,TEMP_LOC);
                localBlobClient.CreateIfNotExists();
                // using a unique name for the file
                string localFileTemp = Guid.NewGuid().ToString();
                BlobClient localBlob = localBlobClient.GetBlobClient(localFileTemp);            
                Uri uri = new Uri(item.BlobName);            
                localBlob.StartCopyFromUri(uri);     
                _logger.LogInformation($"BlobController::CopyBlob::Creating samples::local copy completed - copy to designated container task starting.");  
                // creating a background task
                await CreateSample(localBlob,targetBlobClient,item);            
                return Accepted($"Sample task Created: creating {item.SampleSize} samples in {item.TargetContainer}");
            }

        }else{
            
            // wrong type passed
            return "Invalid input provided. ";
        }
      
        }else{
            _logger.LogInformation($"BlobController::CopyBlob::Validation error. ");            
            return "Invalid input provided. ";
        }
        

    }

    private string GenerateString(int length){
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[new Random().Next(s.Length)]).ToArray());        
    }

    private async Task CallOn(BlobRequest item)
    {
        string content = JsonConvert.SerializeObject(item);
        // log the json content
        _logger.LogInformation($"json item: {content}");
        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header

        // HttpResponseMessage response = await client.PostAsJsonAsync($"{item.CallonUrl}/api/blob", content);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{item.CallonUrl}/api/blob");
        
        request.Content = new StringContent(JsonConvert.SerializeObject(item), Encoding.UTF8, "application/json");
        // _logger.LogInformation($"BlobController::CallOn::Calling {item.CallonUrl}/api/blob with size {item.SampleSize}");
        HttpResponseMessage response = await client.SendAsync(request);
        _logger.LogInformation($"BlobController::CallOn::Response {response.StatusCode} for size {item.SampleSize}");
        client.Dispose();
    }
    private async Task CreateSample(BlobClient localBlob, BlobContainerClient destBlobContainer, BlobRequest item)
    {
        // use the sample file as stream to create multiple files
        _logger.LogInformation($"BlobController::CreateSample::Creating {item.SampleSize} samples in {item.TargetContainer}.");
        Stream content = localBlob.OpenRead();
        string prefix = GenerateString(5);
        for (int i = 0; i < item.SampleSize; i++)
        {
            var tempfile = $"{prefix}-{i}.json";
            content.Position = 0;
            await destBlobContainer.GetBlobClient(tempfile).UploadAsync(content);
        }
    }
    

    private async Task CopySingle(BlobClient sourceBlob,BlobClient destBlob, string sas)
    {
        Uri uri = new Uri ($"{sourceBlob.Uri.AbsoluteUri}?{sas}");        
        await destBlob.StartCopyFromUriAsync(uri);        
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


