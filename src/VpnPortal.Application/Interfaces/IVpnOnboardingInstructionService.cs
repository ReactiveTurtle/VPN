using VpnPortal.Application.Contracts.Users;

namespace VpnPortal.Application.Interfaces;

public interface IVpnOnboardingInstructionService
{
    VpnOnboardingInstructionDto Create(string platform, string vpnUsername);
    IReadOnlyCollection<VpnOnboardingInstructionDto> CreateCatalog();
}
