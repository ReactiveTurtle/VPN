using Microsoft.Extensions.Options;
using VpnPortal.Application.Contracts.Users;
using VpnPortal.Application.Interfaces;
using VpnPortal.Infrastructure.Options;

namespace VpnPortal.Infrastructure.Services;

public sealed class VpnOnboardingInstructionService(IOptions<VpnAccessOptions> vpnAccessOptions) : IVpnOnboardingInstructionService
{
    private const string CatalogUsernamePlaceholder = "<device-username>";
    private static readonly string[] SupportedPlatforms = ["ios", "android", "windows", "macos"];

    public VpnOnboardingInstructionDto Create(string platform, string vpnUsername)
    {
        var normalized = NormalizePlatform(platform);
        var serverAddress = vpnAccessOptions.Value.ServerAddress.Trim();
        var fields = CreateFields(normalized, serverAddress, vpnUsername);

        return normalized switch
        {
            "manual" => new VpnOnboardingInstructionDto(
                "manual",
                "Ручная настройка VPN",
                "Создайте IKEv2-подключение вручную и используйте логин с паролем устройства из портала.",
                [
                    $"Сервер или адрес подключения: {serverAddress}.",
                    "Тип подключения: IKEv2.",
                    $"Имя пользователя: {vpnUsername}. Пароль: пароль устройства, который портал показывает один раз после создания или смены.",
                    $"Если клиент запрашивает Remote ID, укажите {serverAddress}.",
                    "После сохранения профиля выполните первое подключение: этот вход автоматически привяжет текущий IP к устройству."
                ],
                "Используйте логин и пароль устройства из портала.",
                fields),
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
                "Используйте VPN-имя пользователя и пароль устройства, указанные выше.",
                fields),
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
                "Используйте VPN-имя пользователя и пароль устройства, указанные выше.",
                fields),
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
                "Используйте VPN-имя пользователя и пароль устройства, указанные выше.",
                fields),
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
                "Используйте VPN-имя пользователя и пароль устройства, указанные выше.",
                fields)
        };
    }

    public IReadOnlyCollection<VpnOnboardingInstructionDto> CreateCatalog()
    {
        return SupportedPlatforms.Select(platform => Create(platform, CatalogUsernamePlaceholder)).ToArray();
    }

    private static IReadOnlyCollection<VpnOnboardingFieldDto> CreateFields(string platform, string serverAddress, string vpnUsername)
    {
        var fields = new List<VpnOnboardingFieldDto>();

        if (!string.IsNullOrWhiteSpace(serverAddress))
        {
            fields.Add(new VpnOnboardingFieldDto("Сервер", serverAddress));

            if (platform is "manual" or "ios" or "android" or "macos")
            {
                fields.Add(new VpnOnboardingFieldDto("Remote ID", serverAddress));
            }
        }

        if (!string.IsNullOrWhiteSpace(vpnUsername) && !string.Equals(vpnUsername, CatalogUsernamePlaceholder, StringComparison.Ordinal))
        {
            fields.Add(new VpnOnboardingFieldDto("Имя пользователя", vpnUsername));
        }

        return fields;
    }

    private static string NormalizePlatform(string platform)
    {
        var normalized = string.IsNullOrWhiteSpace(platform) ? "manual" : platform.Trim().ToLowerInvariant();
        return normalized switch
        {
            "iphone" or "ipad" => "ios",
            "mac" or "osx" => "macos",
            _ => normalized
        };
    }
}
