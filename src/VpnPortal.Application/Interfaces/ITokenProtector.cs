namespace VpnPortal.Application.Interfaces;

public interface ITokenProtector
{
    string GenerateRawToken(int sizeInBytes = 32);
    string Hash(string token);
}
