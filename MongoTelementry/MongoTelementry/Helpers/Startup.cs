using Azure.Messaging.EventHubs.Producer;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoTelementry.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;

[assembly: WebJobsStartup(typeof(Startup))]
namespace MongoTelementry.Helpers
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddFilter(level => true);
            });

            var config = (IConfiguration)builder.Services.First(d => d.ServiceType == typeof(IConfiguration)).ImplementationInstance;

            builder.Services.AddSingleton(
                s => new EventHubProducerClient(config[Settings.EVENT_HUB_CONNECTION_STRING], config[Settings.EVENT_HUB]));

            builder.Services.AddSingleton((s) =>
            {
                MongoClientSettings settings = new MongoClientSettings();
                settings.Server = new MongoServerAddress(config[Settings.MONGO_HOST], 10255);
                settings.UseSsl = true;
                settings.RetryWrites = false;
                settings.SslSettings = new SslSettings();
                settings.SslSettings.EnabledSslProtocols = SslProtocols.Tls12;

                MongoIdentity identity = new MongoInternalIdentity(config[Settings.DB_NAME], config[Settings.USER_NAME]);
                MongoIdentityEvidence evidence = new PasswordEvidence(config[Settings.MONGO_PASSWORD]);

                settings.Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence);

                MongoClient client = new MongoClient(settings);

                return client;
            });
        }
    }
}
