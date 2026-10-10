using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace EvoSim
{
    /// <summary>Reads a model's digest from Ollama's /api/tags (first 12 characters), for cache keys and run info (DEC-33, GENE-12).</summary>
    public static class OllamaTags
    {
        /// <summary>The digest, or "unknown" when the server or the model isn't there.</summary>
        public static async Task<string> DigestAsync(HttpCaller c, string model)
        {
            try
            {
                var tags = await c.GetAsync("/api/tags").ConfigureAwait(false);
                if (tags["models"] is JArray models)
                    foreach (var m in models)
                    {
                        string name = (string)m["name"] ?? "";
                        if (name == model || name == model + ":latest")
                        {
                            string d = (string)m["digest"] ?? "";
                            return d.Length > 12 ? d.Substring(0, 12) : d.Length > 0 ? d : "unknown";
                        }
                    }
            }
            catch (Exception) { }
            return "unknown";
        }
    }
}
