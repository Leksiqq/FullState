using System.Collections.Concurrent;

public class StatHolder
{
    public ConcurrentQueue<AssertHolder> Asserts { get; init; } = new();

}
