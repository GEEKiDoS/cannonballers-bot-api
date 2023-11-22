using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace BotApi.Types
{
    [BsonIgnoreExtraElements]
    public class IIDXSong
    {
        [JsonProperty("id")]
        [BsonId]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("englishTitle")]
        public string EnglishTitle { get; set; }

        [JsonProperty("genre")]
        public string Genre { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("fontId")]
        public long FontId { get; set; }

        [JsonProperty("version")]
        public long Version { get; set; }

        [JsonProperty("level")]
        public Difficulty Level { get; set; }

        [JsonProperty("volume")]
        public long Volume { get; set; }

        [JsonProperty("bgaDelay")]
        public long BgaDelay { get; set; }

        [JsonProperty("bgaFile")]
        public string BgaFile { get; set; }
    }

    public class Difficulty
    {
        [JsonProperty("sp")]
        public DifficultyDetail Sp { get; set; }

        [JsonProperty("dp")]
        public DifficultyDetail Dp { get; set; }
    }

    public class DifficultyDetail
    {
        [JsonProperty("beginner")]
        public long Beginner { get; set; }

        [JsonProperty("normal")]
        public long Normal { get; set; }

        [JsonProperty("hyper")]
        public long Hyper { get; set; }

        [JsonProperty("another")]
        public long Another { get; set; }

        [JsonProperty("leggendaria")]
        public long Leggendaria { get; set; }
    }
}
