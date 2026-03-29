using VpnPortal.Application.Contracts.Users;

namespace VpnPortal.Application.Interfaces;

public interface IVpnPasswordMaterialService
{
    VpnPasswordMaterial Create(string password);
}
