using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Verse;

namespace RimTalkHealthEnhance
{
    public static class SimpleAIClient
    {
        public static async Task<string> CallAI(string prompt)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            
            // Player2 doesn't require API key (uses local app or optional key)
            if (settings.SynthesisProvider != AIProvider.Player2 && string.IsNullOrEmpty(settings.CustomApiKey))
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
                    case AIProvider.Google: url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent"; break;
                    case AIProvider.DeepSeek: url = "https://api.deepseek.com/chat/completions"; break;
                    case AIProvider.Player2: url = "https://api.player2.game/v1/chat/completions"; break;
                    default: url = "https://api.openai.com/v1/chat/completions"; break;
                }
            }
            else
            {
                // Auto-complete URL for OpenAI compatible providers if user only provided base URL
                if (settings.SynthesisProvider != AIProvider.Google)
                {
                    url = url.TrimEnd('/');
                    if (!url.EndsWith("/chat/completions"))
                    {
                        if (url.EndsWith("/v1"))
                            url += "/chat/completions";
                        else
                            url += "/v1/chat/completions";
                    }
                }
            }
            
            // Append API key for Gemini if using default URL structure (it uses query param)
            if (settings.SynthesisProvider == AIProvider.Google && !url.Contains("key="))
            {
                url += $"?key={settings.CustomApiKey}";
            }
            
            using (var client = new HttpClient())
            {
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
                
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                
                try
                {
                    var response = await client.PostAsync(url, content);
                    var responseJson = await response.Content.ReadAsStringAsync();
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Error($"[RimTalk Enhance] AI Call Failed: {response.StatusCode}\nResponse: {responseJson}");
                        return null;
                    }
                    
                    var json = JObject.Parse(responseJson);
                    
                    if (settings.SynthesisProvider == AIProvider.Google)
                    {
                        // Parse Gemini Response
                        // candidates[0].content.parts[0].text
                        var text = json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                        return text;
                    }
                    else
                    {
                        // Parse OpenAI Response
                        // choices[0].message.content
                        var text = json["choices"]?[0]?["message"]?["content"]?.ToString();
                        return text;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[RimTalk Enhance] AI Call Exception: {ex.Message}");
                    return null;
                }
            }
        }
    }
}
