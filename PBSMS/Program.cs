// Copyright Rob Latour, 2026

using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// to create an executable for this project: in Solution Explorer right click on PBSMS, select Publish ..., and click on the 'Publish' button
// results found in:  .....\PBSMS\PBSMS\bin\Release\net10.0\publish\

namespace PBSMS
{
    [JsonSerializable(typeof(DevicesResponse))]
    [JsonSerializable(typeof(ErrorResponse))]
    [JsonSerializable(typeof(SmsData))]
    [JsonSerializable(typeof(CurrentUserResponse))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }

    [SupportedOSPlatform("windows")]
    internal class Program
    {
        private const string API_BASE_URL = "https://api.pushbullet.com/v2";
        private static ConsoleColor ConsoleColorOriginal;

        static async Task<int> Main(string[] args)
        {

            ConsoleColorOriginal = Console.ForegroundColor;

            if (args.Length == 0)
            {
                ShowHelp();
                return 0;
            }

            if (args.Length == 1 && args[0].StartsWith("APIKey=", StringComparison.OrdinalIgnoreCase))
            {
                return await HandleApiKeyStorage(args[0]);
            }

            if (args.Length != 2)
            {
                WriteToConsoleInColour(ConsoleColor.Red, "Error: Invalid number of arguments.");

                ShowHelp();
                return 1;
            }

            string phoneNumber = args[0];
            string message = args[1];

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                WriteToConsoleInColour(ConsoleColor.Red, "Error: Phone number cannot be empty.");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                WriteToConsoleInColour(ConsoleColor.Red, "Error: Message cannot be empty.");
                return 1;
            }

            if (!phoneNumber.StartsWith("+"))
            {
                WriteToConsoleInColour(ConsoleColor.Red, "Error: Phone number must be in international format (e.g., +1234567890).");
                return 1;
            }

            if (!phoneNumber.Substring(1).All(char.IsDigit))
            {
                WriteToConsoleInColour(ConsoleColor.Red, "Error: Phone number must contain only digits after the + sign.");
                return 1;
            }

            if ((phoneNumber.Length < 7) || (phoneNumber.Length > 16)) // note phone number includes the + symbol so this if statement is adjust for that
            {
                WriteToConsoleInColour(ConsoleColor.Red, "Error: Phone number must be between 6 and 15 digits (inclusive).");
                return 1;
            }

            try
            {
                await SendSMS(phoneNumber, message);
                return 0;
            }
            catch (Exception ex)
            {
                WriteToConsoleInColour(ConsoleColor.Red, "Error:");
                WriteToConsoleInColour(ConsoleColor.Red, ex.Message);
                return 1;
            }
        }



        [SupportedOSPlatform("windows")]

        static void WriteToConsoleInColour(ConsoleColor writeColour, string message)
        {
            Console.ForegroundColor = writeColour;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColorOriginal;
        }

        static async Task<int> HandleApiKeyStorage(string arg)
        {
            const string prefix = "APIKey=";
            string apiKey = arg.Substring(prefix.Length).Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                WriteToConsoleInColour(ConsoleColor.Red, "Error: Pushbullet API key is missing.");
                return 1;
            }

            if (apiKey.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    bool removed = SettingsManager.DeleteApiKey();
                    if (removed)
                    {
                        WriteToConsoleInColour(ConsoleColor.Green, "Success: Pushbullet API key has been removed.");
                    }
                    else
                    {
                        WriteToConsoleInColour(ConsoleColor.Yellow, "Info: No stored Pushbullet API key was found to remove.");
                    }
                    return 0;
                }
                catch (Exception ex)
                {
                    WriteToConsoleInColour(ConsoleColor.Red, "Error: Failed to remove API key.");
                    WriteToConsoleInColour(ConsoleColor.Red, ex.Message);
                    return 1;
                }
            }

            if (!apiKey.StartsWith("o."))
            {
                WriteToConsoleInColour(ConsoleColor.Red, "Error: Invalid Pushbullet API key. Key must start with 'o.'");
                return 1;
            }

            try
            {
                bool isValid = await ValidateApiKey(apiKey);
                if (!isValid)
                {
                    WriteToConsoleInColour(ConsoleColor.Red, "Error: The API key could not be validated by Pushbullet.");
                    return 1;
                }

                SettingsManager.SaveApiKey(apiKey);
                WriteToConsoleInColour(ConsoleColor.Green, "Success: Pushbullet API key has been validated and saved.");
                return 0;
            }
            catch (Exception ex)
            {
                WriteToConsoleInColour(ConsoleColor.Red, "Error: Failed to save API key.");
                WriteToConsoleInColour(ConsoleColor.Red, ex.Message);
                return 1;
            }
        }

        static async Task<bool> ValidateApiKey(string apiKey)
        {
            try
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };
                client.DefaultRequestHeaders.Add("Access-Token", apiKey);

                var response = await client.GetAsync($"{API_BASE_URL}/users/me");
                return response.IsSuccessStatusCode;
            }
            catch (TaskCanceledException)
            {
                if (!await CheckInternetConnectivity())
                {
                    WriteToConsoleInColour(ConsoleColor.Red, "Error: Unable to reach the internet. Please check your connection.");
                }
                else
                {
                    WriteToConsoleInColour(ConsoleColor.Red, "Error: Pushbullet did not respond within ten seconds.");
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        static async Task<bool> CheckInternetConnectivity()
        {
            try
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };
                var response = await client.GetAsync("https://www.google.com");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        static string GetVersionLabel()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version == null)
            {
                return "Unknown";
            }

            var parts = version.ToString().Split('.');
            while (parts.Length > 1 && parts[^1] == "0")
            {
                parts = parts[..^1];
            }

            return string.Join('.', parts);
        }

        static void ShowHelp()
        {

            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine();
            Console.WriteLine($"PBSMS v{GetVersionLabel()} - A Windows Command Line Interface program to send SMS messages using Pushbullet");

            Console.WriteLine();
            WriteToConsoleInColour(ConsoleColor.White, "Usage:");
            WriteToConsoleInColour(ConsoleColorOriginal, "For first use (and for later to change the Pushbullet API Key):");
            WriteToConsoleInColour(ConsoleColorOriginal, "   pbsms APIKey=<Pushbullet API key>");
            Console.WriteLine();
            WriteToConsoleInColour(ConsoleColorOriginal, "To remove (delete) the API key:");
            WriteToConsoleInColour(ConsoleColorOriginal, "   pbsms APIKey=remove");

            Console.WriteLine();
            WriteToConsoleInColour(ConsoleColorOriginal, "To send a SMS:");
            WriteToConsoleInColour(ConsoleColorOriginal, "   pbsms <phone number> <message>");

            Console.WriteLine();
            WriteToConsoleInColour(ConsoleColor.White, "Arguments:");
            WriteToConsoleInColour(ConsoleColorOriginal, "Pushbullet API key      Pushbullet API key");
            WriteToConsoleInColour(ConsoleColorOriginal, "phone number            Destination phone number in international format (e.g., +1234567890)");
            WriteToConsoleInColour(ConsoleColorOriginal, "message                 Text message to send (surrounded by double quotes)");

            Console.WriteLine();
            WriteToConsoleInColour(ConsoleColor.White, "Examples:");
            WriteToConsoleInColour(ConsoleColorOriginal, "pbsms APIKey=o.abc1def2ghi3klm5mno6pqr7stu8vwx9");
            WriteToConsoleInColour(ConsoleColorOriginal, "pbsms +15551234567 \"Hello world\"");

            Console.WriteLine();
            WriteToConsoleInColour(ConsoleColor.White, "Return codes:");
            WriteToConsoleInColour(ConsoleColorOriginal, "If no errors are reported the program will provide a return code of zero (0), otherwise");
            WriteToConsoleInColour(ConsoleColorOriginal, "a one (1) will be provided.");

            Console.WriteLine();
            WriteToConsoleInColour(ConsoleColor.White, "Notes:");
            WriteToConsoleInColour(ConsoleColorOriginal, "1. For more information on Pushbullet please see:");
            WriteToConsoleInColour(ConsoleColorOriginal, "https://www.pushbullet.com/");
            Console.WriteLine();
            WriteToConsoleInColour(ConsoleColorOriginal, "2. For more information on how PBSMS encrypts and stores the Pushbullet API Key please see:");
            WriteToConsoleInColour(ConsoleColorOriginal, "https://github.com/roblatour/PBSMS/PBPSMSAPIKeySecurity.md");

            Console.WriteLine();
            WriteToConsoleInColour(ConsoleColor.Yellow, "PBSMS");
            WriteToConsoleInColour(ConsoleColor.Yellow, "Copyright Rob Latour, 2026");
            WriteToConsoleInColour(ConsoleColor.Yellow, "License: MIT");
            WriteToConsoleInColour(ConsoleColor.Yellow, "https://github.com/roblatour/PBSMS");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColorOriginal;
        }

        [SupportedOSPlatform("windows")]
        static async Task SendSMS(string phoneNumber, string message)
        {
            string apiKey;
            try
            {
                apiKey = SettingsManager.GetApiKey();
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not find a stored Pushbullet API key.\nDetails: {ex.Message}");
            }

            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(7)
            };
            client.DefaultRequestHeaders.Add("Access-Token", apiKey);

            string? deviceIden;
            try
            {
                deviceIden = await GetSmsCapableDevice(client);
            }
            catch (TaskCanceledException)
            {
                if (!await CheckInternetConnectivity())
                {
                    throw new Exception("Unable to reach the internet. Please check your connection.");
                }
                else
                {
                    throw new Exception("Pushbullet did not respond within ten seconds.");
                }
            }

            if (deviceIden == null)
            {
                throw new Exception("No SMS-capable device found. Please ensure you have an Android device with SMS permissions connected to your Pushbullet account.");
            }

            var smsData = new SmsData
            {
                Data = new SmsDataInner
                {
                    TargetDeviceIden = deviceIden,
                    Addresses = new[] { phoneNumber },
                    Message = message
                }
            };

            var jsonContent = JsonSerializer.Serialize(smsData, AppJsonSerializerContext.Default.SmsData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync($"{API_BASE_URL}/texts", content);
            }
            catch (TaskCanceledException)
            {
                if (!await CheckInternetConnectivity())
                {
                    throw new Exception("Unable to reach the internet. Please check your connection.");
                }
                else
                {
                    throw new Exception("Pushbullet did not respond within ten seconds.");
                }
            }

            if (response.IsSuccessStatusCode)
            {
                WriteToConsoleInColour(ConsoleColor.Green, "Success: SMS message sent successfully.");
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                int statusCode = (int)response.StatusCode;

                try
                {
                    var errorJson = JsonSerializer.Deserialize(errorContent, AppJsonSerializerContext.Default.ErrorResponse);
                    if (errorJson?.Error != null)
                    {
                        Console.WriteLine($"Failed: {errorJson.Error.Message} (HTTP {statusCode})");
                    }
                    else
                    {
                        Console.WriteLine($"Failed: HTTP {statusCode} - {response.ReasonPhrase}");
                    }
                }
                catch
                {
                    Console.WriteLine($"Failed: HTTP {statusCode} - {response.ReasonPhrase}");
                }
            }
        }

        static async Task<string?> GetSmsCapableDevice(HttpClient client)
        {
            try
            {
                var response = await client.GetAsync($"{API_BASE_URL}/devices");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to retrieve devices: HTTP {(int)response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var devicesResponse = JsonSerializer.Deserialize(responseContent, AppJsonSerializerContext.Default.DevicesResponse);

                var smsDevice = devicesResponse?.Devices?.FirstOrDefault(d => d.Active && d.HasSms);

                return smsDevice?.Iden;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving SMS-capable device: {ex.Message}", ex);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    internal static class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PBSMS",
            "settings.dat"
        );

        public static void SaveApiKey(string apiKey)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(apiKey);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

            string? directory = Path.GetDirectoryName(SettingsPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(SettingsPath, encryptedBytes);
        }

        public static string GetApiKey()
        {
            if (!File.Exists(SettingsPath))
            {
                throw new Exception("To store a Pushbullet API Key please use: PBSMS APIKey=<Pusbullet API Key>");
            }

            byte[] encryptedBytes = File.ReadAllBytes(SettingsPath);
            byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(plainBytes);
        }

        public static bool DeleteApiKey()
        {
            if (!File.Exists(SettingsPath))
            {
                return false;
            }

            File.Delete(SettingsPath);

            string? directory = Path.GetDirectoryName(SettingsPath);
            if (directory != null && Directory.Exists(directory) && Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
            {
                Directory.Delete(directory);
            }

            return true;
        }
    }

    internal class DevicesResponse
    {
        [JsonPropertyName("devices")]
        public List<Device>? Devices { get; set; }
    }

    internal class Device
    {
        [JsonPropertyName("iden")]
        public string? Iden { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("has_sms")]
        public bool HasSms { get; set; }

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }
    }

    internal class ErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorDetail? Error { get; set; }
    }

    internal class ErrorDetail
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("cat")]
        public string? Cat { get; set; }
    }

    internal class SmsData
    {
        [JsonPropertyName("data")]
        public SmsDataInner? Data { get; set; }
    }

    internal class SmsDataInner
    {
        [JsonPropertyName("target_device_iden")]
        public string? TargetDeviceIden { get; set; }

        [JsonPropertyName("addresses")]
        public string[]? Addresses { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    internal class CurrentUserResponse
    {
        [JsonPropertyName("iden")]
        public string? Iden { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}