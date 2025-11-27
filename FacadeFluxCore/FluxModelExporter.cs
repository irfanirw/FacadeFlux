using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FacadeFluxCore
{
    public class FluxModelExporter
    {
        /// <summary>
        /// Export an FluxModel to a JSON file.
        /// </summary>
        /// <param name="model">The FluxModel to export</param>
        /// <param name="filePath">The file path to save the JSON</param>
        public static void ExportToJson(FluxModel model, string filePath)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model), "FluxModel cannot be null.");

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
