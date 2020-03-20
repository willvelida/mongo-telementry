using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Bogus;
using MongoTelementry.Models;
using System.Collections.Generic;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs;
using System.Text;

namespace MongoTelementry.Functions
{
    public class GenerateEvents
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly EventHubProducerClient _eventHubProducerClient;

        public GenerateEvents(
            ILogger<GenerateEvents> logger,
            IConfiguration config,
            EventHubProducerClient eventHubProducerClient)
        {
            _logger = logger;
            _config = config;
            _eventHubProducerClient = eventHubProducerClient;
        }

        [FunctionName(nameof(GenerateEvents))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "GenerateEvents")] HttpRequest req)
        {
            IActionResult result = null;

            try
            {
                var deviceIterations = new Faker<DeviceReading>()
                    .RuleFor(i => i.id, (fake) => Guid.NewGuid())
                    .RuleFor(i => i.DeviceTemperature, (fake) => fake.Random.Decimal(0.00m, 60.00m))
                    .RuleFor(i => i.DeviceAge, (fake) => fake.Random.Number(0, 60))
                    .RuleFor(i => i.DamageLevel, (fake) => fake.PickRandom(new List<string> { "Low", "Medium", "High" }))
                    .GenerateLazy(10);

                foreach (var reading in deviceIterations)
                {
                    // send to event hub
                    EventDataBatch eventDataBatch = await _eventHubProducerClient.CreateBatchAsync();
                    var eventReading = JsonConvert.SerializeObject(reading);
                    eventDataBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(eventReading)));
                    await _eventHubProducerClient.SendAsync(eventDataBatch);
                    _logger.LogInformation($"Reading {reading.id} sent to event hub");
                }

                result = new StatusCodeResult(StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong. Exception thrown: {ex.Message}");
                result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return result;
        }
    }
}
