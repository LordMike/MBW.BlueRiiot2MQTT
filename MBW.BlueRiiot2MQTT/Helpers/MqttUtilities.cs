using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using uPLibrary.Networking.M2Mqtt;

namespace MBW.BlueRiiot2MQTT.Helpers
{
    internal static class MqttUtilities
    {
        private static readonly Encoding Encoding = new UTF8Encoding(false);

        private static byte[] ConvertJson(JToken token)
        {
            using MemoryStream ms = new MemoryStream();

            using (StreamWriter sw = new StreamWriter(ms, Encoding))
            using (JsonTextWriter jw = new JsonTextWriter(sw))
            {
                token.WriteTo(jw);
            }

            return ms.ToArray();
        }

        public static void SendJson(this MqttClient mqttClient, string topic, JToken doc)
        {
            mqttClient.Publish(topic, ConvertJson(doc), 0, true);
        }

        public static void SendValue(this MqttClient mqttClient, string topic, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);

            mqttClient.Publish(topic, bytes, 0, true);
        }

        public static void SendValue(this MqttClient mqttClient, string topic, JToken value)
        {
            mqttClient.Publish(topic, ConvertJson(value), 0, true);
        }
    }
}