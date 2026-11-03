using System;
using Xunit;

namespace EnterpriseIntegrationHub.Infrastructure.Tests;

public sealed class KafkaIntegrationFactAttribute : FactAttribute
{
    public KafkaIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EIH_KAFKA_TEST_BOOTSTRAP")))
            Skip = "Set EIH_KAFKA_TEST_BOOTSTRAP to run the Kafka integration test.";
    }
}
