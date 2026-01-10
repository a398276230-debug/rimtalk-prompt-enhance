using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Verse;

namespace RimTalkHealthEnhance
{
    public static class SimpleAIClient
    {
        // 默认超时时间（秒）
        private const int DEFAULT_TIMEOUT_SECONDS = 120;
        // 最大重试次数
        private const int MAX_RETRY_COUNT = 2;
        // 重试延迟（毫秒）
        private const int RETRY_DELAY_MS = 2000;
        
        public static async Task<string> CallAI(string prompt)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            
            // Player2 and Custom (local LLM like Ollama) don't require API key
            if (settings.SynthesisProvider != AIProvider.Player2 &&
                settings.SynthesisProvider != AIProvider.Custom &&
                string.IsNullOrEmpty(settings.CustomApiKey))
            {
                Log.Warning("[RimTalk Enhance] AI API Key not set.");
                return null;
            }
            
            string url = settings.CustomApiUrl;
            if (string.IsNullOrEmpty(url))
            {
                // Fallback to default URLs if not set
                switch (settings.SynthesisProvider)
                {
                    case AIProvider.OpenAI: url = "https://api.openai.com/v1/chat/completions"; break;
                    case AIProvider.Google:
                        // Use CustomModelName to build dynamic Gemini URL
                        string geminiModel = string.IsNullOrEmpty(settings.CustomModelName) ? "gemini-pro" : settings.CustomModelName;
                        url = $"https://generativelanguage.googleapis.com/v1beta/models/{geminiModel}:generateContent";
                        break;
                    case AIProvider.DeepSeek: url = "https://api.deepseek.com/chat/completions"; break;
                    case AIProvider.Player2: url = "https://api.player2.game/v1/chat/completions"; break;
                    default: url = "https://api.openai.com/v1/chat/completions"; break;
                }
            }
            else
            {
                url = url.TrimEnd('/');
                
                if (settings.SynthesisProvider == AIProvider.Google)
                {
                    // Auto-complete URL for Google Gemini
                    // Support various base URL formats:
                    // - https://generativelanguage.googleapis.com
                    // - https://generativelanguage.googleapis.com/v1beta
                    // - https://generativelanguage.googleapis.com/v1beta/models
                    string geminiModel = string.IsNullOrEmpty(settings.CustomModelName) ? "gemini-pro" : settings.CustomModelName;
                    
                    if (!url.Contains(":generateContent"))
                    {
                        if (url.EndsWith("/models"))
                        {
                            // User entered: .../v1beta/models
                            url = $"{url}/{geminiModel}:generateContent";
                        }
                        else if (url.EndsWith("/v1beta"))
                        {
                            // User entered: .../v1beta
                            url = $"{url}/models/{geminiModel}:generateContent";
                        }
                        else if (url.Contains("generativelanguage.googleapis.com") && !url.Contains("/v1beta"))
                        {
                            // User entered just the base domain
                            url = $"{url}/v1beta/models/{geminiModel}:generateContent";
                        }
                        // else: user entered full URL with model, use as-is
                    }
                }
                else
                {
                    // Auto-complete URL for OpenAI compatible providers if user only provided base URL
                    // Skip auto-complete if user already specified a specific endpoint path
                    // Supports: OpenAI, DeepSeek, OpenRouter, and other OpenAI-compatible APIs
                    bool hasSpecificEndpoint = url.EndsWith("/chat/completions") ||
                                               url.EndsWith("/api/chat") ||
                                               url.EndsWith("/api/generate") ||
                                               url.EndsWith("/completions") ||
                                               url.Contains("/chat/completions?") ||   // with query params
                                               url.Contains("/v1/chat/completions") || // full path already present
                                               url.Contains("/api/v1/chat/completions"); // OpenRouter style
                    
                    if (!hasSpecificEndpoint)
                    {
                        // Normalize: remove trailing slash to avoid double slashes
                        url = url.TrimEnd('/');
                        
                        // Check if URL already contains /v1 path segment
                        if (url.EndsWith("/v1") || url.Contains("/v1/"))
                        {
                            // URL already has /v1, just append /chat/completions
                            if (url.EndsWith("/v1"))
                                url += "/chat/completions";
                            // else: URL contains /v1/ but doesn't end with it - likely already complete, don't modify
                        }
                        else
                        {
                            // URL doesn't have /v1, append full path
                            url += "/v1/chat/completions";
                        }
                    }
                }
            }
            
            // Append API key for Gemini if using default URL structure (it uses query param)
            if (settings.SynthesisProvider == AIProvider.Google && !url.Contains("key="))
            {
                url += $"?key={settings.CustomApiKey}";
            }
            
            // Configure HttpClientHandler to minimize system environment interference
            var handler = new HttpClientHandler
            {
                UseDefaultCredentials = false,
                PreAuthenticate = false,
                UseCookies = false,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };

            using (var client = new HttpClient(handler))
            {
                // 设置超时时间
                client.Timeout = TimeSpan.FromSeconds(DEFAULT_TIMEOUT_SECONDS);
                
                // Clear default headers to avoid auto-injection of system info
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "RimTalk-Enhance/1.0");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                // Set headers
                if (settings.SynthesisProvider != AIProvider.Google)
                {
                    // Player2 may not have API key if using local app
                    if (!string.IsNullOrEmpty(settings.CustomApiKey))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.CustomApiKey}");
                    }
                }
                
                // Player2 requires special game client ID header
                if (settings.SynthesisProvider == AIProvider.Player2)
                {
                    client.DefaultRequestHeaders.Add("X-Game-Client-Id", "019a8368-b00b-72bc-b367-2825079dc6fb");
                }
                
                string requestJson = "";
                
                if (settings.SynthesisProvider == AIProvider.Google)
                {
                    // Gemini Format
                    var request = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[]
                                {
                                    new { text = $"System: 你是 RimWorld 殖民地的史官，负责记录殖民地发展。\n\nUser: {prompt}" }
                                }
                            }
                        }
                    };
                    requestJson = JsonConvert.SerializeObject(request);
                }
                else
                {
                    // OpenAI / DeepSeek / Custom Format
                    var request = new
                    {
                        model = settings.CustomModelName,
                        messages = new[]
                        {
                            new { role = "system", content = "你是 RimWorld 殖民地的日志记录系统。请客观、准确地记录殖民地发展，不要使用任何文学修辞。" },
                            new { role = "user", content = prompt }
                        },
                        temperature = 0.5,
                        max_tokens = 2000
                    };
                    requestJson = JsonConvert.SerializeObject(request);
                }
                
                // 使用重试机制
                Exception lastException = null;
                for (int attempt = 0; attempt <= MAX_RETRY_COUNT; attempt++)
                {
                    if (attempt > 0)
                    {
                        Log.Warning($"[RimTalk Enhance] Retrying AI call (attempt {attempt + 1}/{MAX_RETRY_COUNT + 1})...");
                        await Task.Delay(RETRY_DELAY_MS);
                    }
                    
                    try
                    {
                        // 每次重试需要创建新的 content，因为它可能已被消耗
                        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                        
                        // 使用 CancellationToken 进行超时控制
                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(DEFAULT_TIMEOUT_SECONDS)))
                        {
                            var response = await client.PostAsync(url, content, cts.Token);
                            var responseJson = await response.Content.ReadAsStringAsync();
                            
                            if (!response.IsSuccessStatusCode)
                            {
                                Log.Error($"[RimTalk Enhance] AI Call Failed: {response.StatusCode}\nResponse: {responseJson}");
                                // 4xx 错误不重试（客户端错误）
                                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                                {
                                    return null;
                                }
                                // 5xx 错误可以重试（服务器错误）
                                lastException = new Exception($"HTTP {response.StatusCode}: {responseJson}");
                                continue;
                            }
                            
                            var json = JObject.Parse(responseJson);
                            
                            if (settings.SynthesisProvider == AIProvider.Google)
                            {
                                // Parse Gemini Response
                                // candidates[0].content.parts[0].text
                                var text = json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                                if (attempt > 0)
                                {
                                    Log.Message($"[RimTalk Enhance] AI call succeeded after {attempt + 1} attempts.");
                                }
                                return text;
                            }
                            else
                            {
                                // Parse OpenAI Response
                                // choices[0].message.content
                                var text = json["choices"]?[0]?["message"]?["content"]?.ToString();
                                if (attempt > 0)
                                {
                                    Log.Message($"[RimTalk Enhance] AI call succeeded after {attempt + 1} attempts.");
                                }
                                return text;
                            }
                        }
                    }
                    catch (TaskCanceledException ex)
                    {
                        // 超时
                        lastException = ex;
                        Log.Warning($"[RimTalk Enhance] AI call timed out (attempt {attempt + 1}/{MAX_RETRY_COUNT + 1}).");
                    }
                    catch (HttpRequestException ex)
                    {
                        // 网络错误
                        lastException = ex;
                        Log.Warning($"[RimTalk Enhance] Network error (attempt {attempt + 1}/{MAX_RETRY_COUNT + 1}): {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 其他错误
                        lastException = ex;
                        
                        // Enhanced error handling for encoding issues
                        if (ex is System.Text.DecoderFallbackException ||
                            ex.Message.Contains("Illegal byte sequence") ||
                            ex.Message.Contains("encounted in the input"))
                        {
                            Log.Error("[RimTalk Enhance] Character encoding error detected.");
                            Log.Warning($"[RimTalk Enhance] This is likely caused by non-ASCII characters in your computer name: {System.Environment.MachineName}");
                            Log.Warning("[RimTalk Enhance] Possible solutions:");
                            Log.Warning("[RimTalk Enhance] 1. Change your computer name to English characters (Control Panel > System > Rename this PC)");
                            Log.Warning("[RimTalk Enhance] 2. Try using a different AI provider");
                            // 编码错误不重试
                            break;
                        }
                        
                        Log.Warning($"[RimTalk Enhance] AI call error (attempt {attempt + 1}/{MAX_RETRY_COUNT + 1}): {ex.Message}");
                    }
                }
                
                // 所有重试都失败
                Log.Error($"[RimTalk Enhance] AI Call Exception: All {MAX_RETRY_COUNT + 1} attempts failed. Last error: {lastException?.Message}");
                return null;
            }
        }
    }
}
