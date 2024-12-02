using System.Security.Cryptography;
namespace API.Services;
public class TotpGenerator
{
    private const int TOTP_LENGTH = 6;
    private const int TIME_STEP = 30;
    private static Dictionary<string, (string code, DateTime expires)> _activeCodes = new();

    public static string GenerateAndStoreTotp(string username)
    {
        var code = GenerateTotp();
        _activeCodes[username] = (code, DateTime.UtcNow.AddSeconds(TIME_STEP));
        return code;
    }

    public static bool ValidateTotp(string username, string code)
    {
        if (!_activeCodes.ContainsKey(username)) return false;
        var (storedCode, expires) = _activeCodes[username];
        if (DateTime.UtcNow > expires)
        {
            _activeCodes.Remove(username);
            return false;
        }
        return storedCode == code;
    }

    private static string GenerateTotp()
    {
        var random = new Random();
        return random.Next(0, 1000000).ToString("D6");
    }
}