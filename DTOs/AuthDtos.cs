namespace IDV_Templates_Mongo_API.DTOs;

public class RegisterRequest
{
    public string firstName { get; set; } = string.Empty;
    public string lastName { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
    public string? confirmPassword { get; set; }
    public object? documentVerification { get; set; }
}
public class LoginRequest
{
    public string email { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
}
public class ApiResponse<T>
{
    public T? data { get; set; }
    public string? message { get; set; }
}
public class UserLite
{
    public string firstName { get; set; } = "";
    public string lastName { get; set; } = "";
    public string email { get; set; } = "";
}
