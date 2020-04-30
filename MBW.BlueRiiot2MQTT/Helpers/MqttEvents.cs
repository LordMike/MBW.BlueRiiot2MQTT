using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;

namespace MBW.BlueRiiot2MQTT.Helpers
{
    internal class MqttEvents
    {
        public event Func<MqttClientConnectedEventArgs, CancellationToken, Task> OnConnect;
        public event Func<MqttClientDisconnectedEventArgs, CancellationToken, Task> OnDisconnect;

        public Task InvokeConnectHandler(MqttClientConnectedEventArgs args, CancellationToken token = default)
        {
            Delegate[] invocations = OnConnect?.GetInvocationList();
            if (invocations == null)
                return Task.CompletedTask;

            Task[] tasks = new Task[invocations.Length];
            for (int index = 0; index < invocations.Length; index++)
            {
                Delegate invocation = invocations[index];
                tasks[index] = (Task)invocation.DynamicInvoke(args, token);
            }

            return Task.WhenAll(tasks);
        }

        public Task InvokeDisconnectHandler(MqttClientDisconnectedEventArgs args, CancellationToken token = default)
        {
            Delegate[] invocations = OnDisconnect?.GetInvocationList();
            if (invocations == null)
                return Task.CompletedTask;

            Task[] tasks = new Task[invocations.Length];
            for (int index = 0; index < invocations.Length; index++)
            {
                Delegate invocation = invocations[index];
                tasks[index] = (Task)invocation.DynamicInvoke(args, token);
            }

            return Task.WhenAll(tasks);
        }
    }
}