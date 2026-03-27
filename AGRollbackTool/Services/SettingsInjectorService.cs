using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service that modifies Google Antigravity's settings.json to block updates.
    /// </summary>
    public class SettingsInjectorService : ISettingsInjectorService
    {
        private readonly IPathResolver _pathResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsInjectorService"/> class.
        /// </summary>
        /// <param name="pathResolver">The path resolver used to locate the Antigravity settings file.</param>
        public SettingsInjectorService(IPathResolver pathResolver)
        {
            _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        }

        /// <summary>
        /// Modifies the Antigravity settings.json file to block updates.
        /// </summary>
        /// <returns>A SettingsChangeResult indicating what was changed and whether the operation succeeded.</returns>
        public async Task<SettingsChangeResult> InjectSettingsAsync()
        {
            var result = new SettingsChangeResult();

            try
            {
                // Get the user settings path
                AntigravityPathInfo pathInfo = _pathResolver.GetUserSettingsPath();
                string settingsFilePath = pathInfo.Path;

                // Initialize the result with default values
                result.NewUpdateMode = "none";
                result.NewShowReleaseNotes = false;

                // Check if the file exists
                if (!pathInfo.Exists)
                {
                    // File doesn't exist, we'll create it with the required settings
                    result.Message = "Settings file did not exist. Created a new settings file with update blocking.";
                    await CreateDefaultSettingsFileAsync(settingsFilePath, result);
                    result.Success = true;
                    return result;
                }

                // Read the existing file
                string jsonContent = await File.ReadAllTextAsync(settingsFilePath);

                // Parse the JSON
                JsonDocument? doc = null;
                try
                {
                    doc = JsonDocument.Parse(jsonContent);
                }
                catch (JsonException ex)
                {
                    // Malformed JSON
                    result.Success = false;
                    result.Message = "Failed to parse existing settings.json due to invalid JSON format.";
                    result.Errors.Add($"JSON parsing error: {ex.Message}");
                    return result;
                }

                // We'll work with a mutable dictionary for simplicity
                var settings = new Dictionary<string, JsonElement>();

                // Helper to recursively copy JsonElement to dictionary (simplified for our needs)
                void CopyJsonElement(JsonElement element, Dictionary<string, JsonElement> dict, string? parentPropertyName = null)
                {
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in element.EnumerateObject())
                        {
                            CopyJsonElement(property.Value, dict, property.Name);
                        }
                    }
                    else
                    {
                        // For simplicity, we only store leaf values in the dictionary with their full path
                        // But we need to handle nested objects. Let's change approach: we'll work with the document directly.
                    }
                }

                // Instead, let's use a different approach: we'll update the document in memory and then serialize it.
                // We'll create a root object if it doesn't exist.

                // Get the root object
                JsonElement root = doc.RootElement;

                // Function to get or create a nested object
                JsonElement GetOrCreateObject(JsonElement parent, string propertyName)
                {
                    if (parent.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.Object)
                    {
                        return value;
                    }
                    else
                    {
                        // Create a new object and replace the property
                        var newObject = new JsonObject();
                        // We need to modify the parent, but JsonElement is read-only.
                        // So we'll have to rebuild the document.
                        // Let's change strategy: we'll deserialize to a mutable class, update, then serialize back.
                    }
                }

                // Given the complexity of modifying JsonDocument in-place, let's deserialize to a dictionary.
                // We'll use JsonSerializer.Deserialize<Dictionary<string, object>> but note that it may not preserve nested structure perfectly.
                // Alternatively, we can use a class for the settings, but we don't know the full schema.
                // Since we only care about two specific properties, we can do:
                // 1. Deserialize to a JsonDocument.
                // 2. Traverse to the "update" object and set the properties.
                // 3. If the "update" object doesn't exist, create it.
                // 4. Then serialize the entire document back.

                // We'll create a mutable copy of the JSON as a string? Not efficient but simple for small files.
                // Instead, let's use System.Text.Json's JsonDocument and then build a new JSON string with the changes.

                // We'll use a JsonWriter to write the updated JSON.
                // Steps:
                // 1. Parse the existing JSON into a JsonDocument.
                // 2. Create a MemoryStream and Utf8JsonWriter to write the updated JSON.
                // 3. Traverse the original JSON and write it out, but when we encounter the "update" object, we write our desired properties.
                // 4. If there's no "update" object, we add it at the appropriate place (assuming it's a root property).

                // However, note that the settings.json might have other properties and we want to preserve them.

                // Let's implement a recursive writer that writes the original JSON but overrides the "update.mode" and "update.showReleaseNotes".

                using (var memoryStream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true }))
                    {
                        WriteUpdatedJson(writer, root);
                    }

                    // Get the updated JSON as a string
                    string updatedJson = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());

                    // Write the updated JSON back to the file
                    await File.WriteAllTextAsync(settingsFilePath, updatedJson);

                    result.Success = true;
                    result.Message = "Successfully updated settings.json to block updates.";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"An unexpected error occurred while updating settings.json: {ex.Message}";
                result.Errors.Add(ex.ToString());
            }

            return result;
        }

        private void WriteUpdatedJson(Utf8JsonWriter writer, JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        // Check if this is the "update" property
                        if (property.NameEquals("update"))
                        {
                            writer.WritePropertyName(property.Name);
                            WriteUpdatedUpdateObject(writer, property.Value);
                        }
                        else
                        {
                            property.WriteTo(writer);
                        }
                    }
                    writer.WriteEndObject();
                    break;
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                    {
                        WriteUpdatedJson(writer, item);
                    }
                    writer.WriteEndArray();
                    break;
                default:
                    element.WriteTo(writer);
                    break;
            }
        }

        private void WriteUpdatedUpdateObject(Utf8JsonWriter writer, JsonElement updateElement)
        {
            writer.WriteStartObject();
            bool modeWritten = false;
            bool showReleaseNotesWritten = false;

            foreach (var property in updateElement.EnumerateObject())
            {
                if (property.NameEquals("mode"))
                {
                    writer.WriteString("mode", "none");
                    modeWritten = true;
                }
                else if (property.NameEquals("showReleaseNotes"))
                {
                    writer.WriteBoolean("showReleaseNotes", false);
                    showReleaseNotesWritten = true;
                }
                else
                {
                    property.WriteTo(writer);
                }
            }

            // If the properties weren't in the original object, add them
            if (!modeWritten)
            {
                writer.WriteString("mode", "none");
            }
            if (!showReleaseNotesWritten)
            {
                writer.WriteBoolean("showReleaseNotes", false);
            }

            writer.WriteEndObject();
        }

        private async Task CreateDefaultSettingsFileAsync(string filePath, SettingsChangeResult result)
        {
            // Ensure the directory exists
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create a default settings object with the update blocking
            var defaultSettings = new
            {
                update = new
                {
                    mode = "none",
                    showReleaseNotes = false
                }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(defaultSettings, options);

            await File.WriteAllTextAsync(filePath, json);

            result.UpdateModeChanged = true;
            result.ShowReleaseNotesChanged = true;
        }
    }
}
