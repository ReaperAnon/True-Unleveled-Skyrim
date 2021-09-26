using System;
using System.IO;

using Newtonsoft.Json;


namespace TrueUnleveledSkyrim.Config
{
    public static class JsonHelper
    {
        public static T LoadConfig<T>(string configPath) where T : ConfigType
        {
            T? configObject = null;
            try
            {
                configObject = JsonConvert.DeserializeObject<T>(File.ReadAllText(configPath));
            }
            catch(Exception ex)
            {
                if (ex is JsonSerializationException)
                    Console.WriteLine("Incorrect config format for file: " + configPath + " \nMake sure to check and compare with the format in the original files provided in the patcher.");
            }

            if (configObject is null)
                throw new FileNotFoundException(configPath + " is missing or empty.");

            return configObject;
        }
    }
}
