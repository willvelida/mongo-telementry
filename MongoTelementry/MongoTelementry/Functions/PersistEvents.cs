using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoTelementry.Helpers;
using MongoTelementry.Models;
using Newtonsoft.Json;

namespace MongoTelementry.Functions
{
    public class PersistEvents
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private MongoClient _mongoClient;

        private IMongoCollection<DeviceReading> _deviceReadings;

        public PersistEvents(
            ILogger<PersistEvents> logger,
            IConfiguration config,
            MongoClient mongoClient,
            IMongoCollection<DeviceReading> deviceReadings)
        {
            _logger = logger;
            _config = config;
            _mongoClient = mongoClient;
            _deviceReadings = deviceReadings;

            var database = _mongoClient.GetDatabase(_config[Settings.DB_NAME]);
            _deviceReadings = database.GetCollection<DeviceReading>(_config[Settings.COLLECTION_NAME]);

        }

        [FunctionName(nameof(PersistEvents))]
        public async Task Run([EventHubTrigger("telementryreadings",
            Connection = "EVENT_HUB_CONNECTION_STRING")] EventData[] events)
        {
            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    var telementryEvent = JsonConvert.DeserializeObject<DeviceReading>(messageBody);

                    await _deviceReadings.InsertOneAsync(telementryEvent);
                    _logger.LogInformation($"{telementryEvent.id} has been persisted");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something went wrong. Exception thrown: {ex.Message}");
                }
            }
        }
    }
}
