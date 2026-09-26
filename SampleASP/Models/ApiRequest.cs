namespace SampleASP.Models;

public class ApiRequest
{
    public string Selector { get; set; } = string.Empty;
    public string Attribute { get; set; } = string.Empty;
    public string Url_b64 { get; set; } = string.Empty;
    public string Encrypted_text_bytes_b64 { get; set; } = string.Empty;
    public string Key_bytes_b64 { get; set; } = string.Empty;
    public string Page_b64 { get; set; } = string.Empty;
}
