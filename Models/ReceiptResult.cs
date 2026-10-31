namespace NevAPI.Models;

public class ReceiptResult
{
    public DateTimeOffset? TransactionDate { get; set; }
    public string MerchantDetails { get; set; }
    public string AmountDetails { get; set; }
    public string Error { get; set; }
}