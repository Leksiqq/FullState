public class AssertHolder
{
    public int Client { get; set; }
    public int Request { get; set; }
    public string Session { get; set; }
    public string Selector { get; set; }
    public int ObjectId { get; set; }
    public string? Error { get; set; }

    public override string ToString()
    {
        return $"{{Client: {Client}, Request: {Request}, Session: {Session}, Selector: {Selector}, ObjectId: {ObjectId}{(Error is { } ? $", Error: {Error}" : string.Empty)}}}";
    }
}
