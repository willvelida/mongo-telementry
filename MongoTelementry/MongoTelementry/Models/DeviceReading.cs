using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoTelementry.Models
{
    public class DeviceReading
    {
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        public Guid id { get; set; }
        [BsonElement("DeviceTemperature")]
        public decimal DeviceTemperature { get; set; }
        [BsonElement("DeviceAge")]
        public int DeviceAge { get; set; }
        [BsonElement("DamageLevel")]
        public string DamageLevel { get; set; }
    }
}
