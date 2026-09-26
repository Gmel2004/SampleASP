namespace SampleASP.Models;

public class ApiResponse
{
    public int Is_error { get; set; }
    public string Error_code { get; set; } = string.Empty;
    public string Error_message { get; set; } = string.Empty;
    public int Elements_count { get; set; }
    public int Emails_count { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Decrypted_plain_text { get; set; } = string.Empty;
    public List<string> Elements_attr_list { get; set; } = new();
    public List<string> Emails_list { get; set; } = new();
}
