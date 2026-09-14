namespace EventHub.Api.Service;

public static class UserService
{
    public static async Task<List<User>> GetAllAsync(CancellationToken ct)
    {
        await Task.Delay(1000, ct);

        return
            [
                new User { Name = "Yuvraj", Email = "ab@gmail.com" },
                new User { Name = "Tushar", Email = "tss@gmail.com"}
            ];
    }
}

public class User
{
    public required string Name { get; set; }
    public required string Email { get; set; }
}
