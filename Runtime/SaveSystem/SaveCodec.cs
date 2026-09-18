using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JadedBelles.Util.SaveSystem
{
    internal sealed class SaveCodec<T> where T : class
    {
        private const string FormatId = "jadedbelles.save";
        private readonly SaveDefinition<T> _definition;

        internal SaveCodec(SaveDefinition<T> definition) { _definition = definition; }

        private JsonSerializerSettings Settings() => new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            MaxDepth = _definition.MaximumDepth,
            DateParseHandling = DateParseHandling.None,
            Culture = CultureInfo.InvariantCulture,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        internal string Encode(T data)
        {
            _definition.ValidateData(data);
            var root = new JObject
            {
                ["format"] = FormatId,
                ["schemaId"] = _definition.SchemaId,
                ["schemaVersion"] = _definition.CurrentVersion,
                ["savedAtUtc"] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                ["data"] = JObject.FromObject(data, JsonSerializer.Create(Settings()))
            };
            return root.ToString(Formatting.None);
        }

        internal T Decode(string json)
        {
            JObject root;
            try
            {
                using (var text = new StringReader(json))
                using (var reader = new JsonTextReader(text)
                {
                    MaxDepth = _definition.MaximumDepth,
                    DateParseHandling = DateParseHandling.None
                })
                {
                    root = JObject.Load(reader, new JsonLoadSettings
                    {
                        DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                    });
                    if (reader.Read()) throw new InvalidSaveException("Trailing JSON content.");
                }
            }
            catch (JsonException exception)
            {
                throw new InvalidSaveException("Cannot parse save JSON.", exception);
            }

            // A wrong format/schema is not corruption and must not select an older backup.
            if (root["format"]?.Type != JTokenType.String || root["format"].Value<string>() != FormatId)
                throw new SaveCompatibilityException("Unrecognized save envelope.");
            if (root["schemaId"]?.Type != JTokenType.String ||
                root["schemaId"].Value<string>() != _definition.SchemaId)
                throw new SaveCompatibilityException("Save belongs to a different schema.");
            var versionToken = root["schemaVersion"];
            if (versionToken == null || versionToken.Type != JTokenType.Integer)
                throw new InvalidSaveException("Missing or invalid schemaVersion.");
            // Decimal avoids overflowing normal integer versions. Unrepresentable versions
            // fail closed as incompatible rather than being mistaken for corrupt old data.
            decimal versionNumber;
            try { versionNumber = versionToken.Value<decimal>(); }
            catch (Exception exception) when (exception is OverflowException || exception is InvalidCastException)
            {
                throw new SaveCompatibilityException("Schema version is outside the supported range.");
            }
            if (versionNumber > _definition.CurrentVersion || versionNumber < 1)
                throw new SaveCompatibilityException("Unsupported schema version " + versionNumber + ".");
            var data = root["data"] as JObject;
            if (data == null) throw new InvalidSaveException("Missing data object.");
            for (int version = (int)versionNumber; version < _definition.CurrentVersion; version++)
                _definition.Migrate(version, data);

            T result;
            try { result = data.ToObject<T>(JsonSerializer.Create(Settings())); }
            catch (JsonException exception)
            {
                throw new InvalidSaveException("Cannot deserialize save payload.", exception);
            }
            // Game callbacks are outside JSON exception catches: coding mistakes must
            // propagate, not masquerade as corruption and trigger a rollback.
            _definition.ValidateData(result);
            return result;
        }
    }
}
