using System.ComponentModel.DataAnnotations;

namespace aca;
public class BlobRequest
{
    
    [Required]
    public string? SourceCS { get; set; }
    [Required]
    public string? TargetCS { get; set; }
    [Required]    
    public string? BlobName { get; set; }
    [Required]
    public string? SourceContainer { get; set; }
    [Required]
    public string? TargetContainer { get; set; }
    [Required]
    public string? RequestType { get; set;}

    public string? CallonUrl { get; set; }

    public int SampleSize { get; set; }

    public bool Validate()
    {
         return !(string.IsNullOrEmpty(TargetCS) || 
                 string.IsNullOrEmpty(SourceCS) || 
                 string.IsNullOrEmpty(SourceContainer) || 
                 string.IsNullOrEmpty(TargetContainer) ||
                 string.IsNullOrEmpty(RequestType));
        
    }
    // empty constructor
    public BlobRequest() {}
    
    // constructor from another BlobRequest

    public BlobRequest(BlobRequest br)
    {
        SourceCS = br.SourceCS;
        TargetCS = br.TargetCS;
        BlobName = br.BlobName;
        SourceContainer = br.SourceContainer;
        TargetContainer = br.TargetContainer;
        RequestType = br.RequestType;
        CallonUrl = br.CallonUrl;
        SampleSize = br.SampleSize;
    }
    
}