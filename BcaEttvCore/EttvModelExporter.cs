using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BcaEttvCore
{
    public class EttvModelExporter
    {
        /// <summary>
        /// Export an EttvModel to a JSON file.
        /// </summary>
        /// <param name="model">The EttvModel to export</param>
        /// <param name="filePath">The file path to save the JSON</param>
        public static void ExportToJson(EttvModel model, string filePath)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model), "EttvModel cannot be null.");

            var jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            };

            string json = JsonConvert.SerializeObject(model, jsonSettings);
            File.WriteAllText(filePath, json);
        }
    }
}
