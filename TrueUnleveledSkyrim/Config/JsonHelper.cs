using System.IO;

using Newtonsoft.Json;

namespace TrueUnleveledSkyrim.Config
{
    public static class JsonHelper
    {
        public static T LoadConfig<T>(string configPath) where T : ConfigType
        {
            T? configObject = JsonConvert.DeserializeObject<T>(File.ReadAllText(configPath));
            if (configObject is null)
                throw new FileNotFoundException(configPath + " is missing or empty.");

            return configObject;
        }
    }
}
