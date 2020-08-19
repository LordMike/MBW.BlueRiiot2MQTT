using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MBW.BlueRiiot2MQTT.Features;
using MBW.Client.BlueRiiotApi;
using MBW.Client.BlueRiiotApi.Objects;
using MBW.HassMQTT;
using MBW.HassMQTT.CommonServices.Commands;
using MQTTnet;
using Newtonsoft.Json;

namespace MBW.BlueRiiot2MQTT.Commands
{
    internal class SetPumpScheduleCommand : IMqttCommandHandler
    {
        private readonly HassMqttManager _hassMqttManager;
        private readonly FeatureUpdateManager _updateManager;
        private readonly BlueClient _blueClient;

        public SetPumpScheduleCommand(HassMqttManager hassMqttManager, FeatureUpdateManager updateManager, BlueClient blueClient)
        {
            _hassMqttManager = hassMqttManager;
            _updateManager = updateManager;
            _blueClient = blueClient;
        }

        public string[] GetFilter()
        {
            return new[] { "commands", "pool", null, "set_pump_schedule" };
        }

        public async Task Handle(string[] topicLevels, MqttApplicationMessage message, CancellationToken token = new CancellationToken())
        {
            string poolId = topicLevels[2];
            string newScheduleString = message.ConvertPayloadToString();

            // Get pool details
            SwimmingPool pool = await _blueClient.GetSwimmingPool(poolId, token);

            if (string.IsNullOrEmpty(newScheduleString) ||
                "none".Equals(newScheduleString, StringComparison.OrdinalIgnoreCase))
            {
                // Set none
                pool.Characteristics.FilterPump = new SwimmingPoolCharacteristicsFilterPump
                {
                    IsPresent = false,
                    OperatingType = "Manual"
                };
            }
            else if ("manual".Equals(newScheduleString, StringComparison.OrdinalIgnoreCase))
            {
                // Set manual
                pool.Characteristics.FilterPump = new SwimmingPoolCharacteristicsFilterPump
                {
                    IsPresent = true,
                    OperatingType = "Manual"
                };
            }
            else
            {
                // Set schedule
                TimeSpan[][] newRanges = JsonConvert.DeserializeObject<TimeSpan[][]>(newScheduleString);

                pool.Characteristics.FilterPump = new SwimmingPoolCharacteristicsFilterPump
                {
                    IsPresent = true,
                    OperatingType = "Scheduled",
                    OperatingHours = newRanges.Select(s => new TimeRange
                    {
                        Start = s[0],
                        End = s[1]
                    }).ToList()
                };
            }

            SwimmingPool newPool = await _blueClient.PutSwimmingPool(poolId, pool, token);

            _updateManager.Process(newPool, newPool);

            await _hassMqttManager.FlushAll(token);
        }
    }
}