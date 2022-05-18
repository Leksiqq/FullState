using System.Collections.Concurrent;

public class StatHolder
{
    public ConcurrentQueue<AssertHolder> Asserts { get; init; } = new();
    public Dictionary<string, AssertHolder> CommonValues { get; init; } = new();

}
