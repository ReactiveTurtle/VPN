using Microsoft.Extensions.Options;
using VpnPortal.Application.Contracts.Users;
using VpnPortal.Application.Interfaces;
using VpnPortal.Infrastructure.Options;

namespace VpnPortal.Infrastructure.Services;

public sealed class VpnOnboardingInstructionService(IOptions<VpnAccessOptions> vpnAccessOptions) : IVpnOnboardingInstructionService
{
    private static readonly string[] SupportedPlatforms = ["ios", "android", "windows", "macos"];

    public VpnOnboardingInstructionDto Create(string platform, string vpnUsername)
    {
        var normalized = NormalizePlatform(platform);
        var serverAddress = string.IsNullOrWhiteSpace(vpnAccessOptions.Value.ServerAddress)
            ? "vpn.example.com"
            : vpnAccessOptions.Value.ServerAddress.Trim();

        return normalized switch
        {
            "ios" => new VpnOnboardingInstructionDto(
                "ios",
                "Ручная настройка IKEv2 на iPhone или iPad",
                "Создайте встроенный IKEv2-профиль VPN и войдите с учетными данными устройства, указанными ниже.",
                [
                    "Откройте Настройки -> VPN -> Добавить конфигурацию VPN -> Тип: IKEv2.",
                    $"Сервер: {serverAddress}.",
                    $"Remote ID: {serverAddress}. Local ID можно оставить пустым, если ваша схема развертывания не требует иного.",
                    $"Имя пользователя: {vpnUsername}. Пароль: VPN-пароль устройства, выданный в портале.",
                    "Сохраните профиль и подключитесь один раз, чтобы убедиться, что туннель поднимается."
                ],
                "Используйте VPN-имя пользователя и пароль устройства, указанные выше."),
            "android" => new VpnOnboardingInstructionDto(
                "android",
                "Настройка IKEv2 на Android",
                "Используйте системные настройки VPN или совместимый IKEv2-клиент и войдите с выданными учетными данными устройства.",
                [
                    "Откройте Настройки -> Сеть и Интернет -> VPN и добавьте новый профиль, либо используйте одобренный IKEv2-клиент.",
                    $"Сервер: {serverAddress}. Тип VPN: IKEv2/IPSec с именем пользователя и паролем.",
                    $"Имя пользователя: {vpnUsername}. Пароль: VPN-пароль устройства, выданный в портале.",
                    "Сохраните профиль и подключитесь. Если Android-клиент запрашивает IPSec-идентификаторы, используйте адрес сервера как Remote ID.",
                    "Если позже в развертывании появятся QR-коды или управляемые профили, используйте их вместо ручного ввода."
                ],
                "Используйте VPN-имя пользователя и пароль устройства, указанные выше."),
            "windows" => new VpnOnboardingInstructionDto(
                "windows",
                "Настройка встроенного VPN в Windows",
                "Создайте встроенное VPN-подключение Windows и выполните аутентификацию с выданными учетными данными устройства.",
                [
                    "Откройте Параметры -> Сеть и Интернет -> VPN -> Добавить VPN-подключение.",
                    "Поставщик VPN: Windows (встроенный). Имя подключения: любое понятное название.",
                    $"Имя или адрес сервера: {serverAddress}. Тип VPN: IKEv2.",
                    "Тип данных для входа: имя пользователя и пароль.",
                    $"Имя пользователя: {vpnUsername}. Пароль: VPN-пароль устройства, выданный в портале.",
                    "Сохраните профиль, затем подключитесь из экрана настроек VPN в Windows."
                ],
                "Используйте VPN-имя пользователя и пароль устройства, указанные выше."),
            _ => new VpnOnboardingInstructionDto(
                "macos",
                "Настройка встроенного IKEv2 в macOS",
                "Создайте встроенный IKEv2-профиль VPN в macOS и выполните аутентификацию с выданными учетными данными устройства.",
                [
                    "Откройте Системные настройки -> VPN -> Добавить конфигурацию VPN -> IKEv2.",
                    $"Адрес сервера: {serverAddress}. Remote ID: {serverAddress}.",
                    $"Имя пользователя: {vpnUsername}. Пароль: VPN-пароль устройства, выданный в портале.",
                    "Оставьте Local ID пустым, если ваше развертывание не требует конкретного значения.",
                    "Сохраните профиль и подключитесь один раз, чтобы проверить доступ."
                ],
                "Используйте VPN-имя пользователя и пароль устройства, указанные выше.")
        };
    }

    public IReadOnlyCollection<VpnOnboardingInstructionDto> CreateCatalog()
    {
        return SupportedPlatforms.Select(platform => Create(platform, "<device-username>")).ToArray();
    }

    private static string NormalizePlatform(string platform)
    {
        var normalized = platform.Trim().ToLowerInvariant();
        return normalized switch
        {
            "iphone" or "ipad" => "ios",
            "mac" or "osx" => "macos",
            _ => normalized
        };
    }
}
