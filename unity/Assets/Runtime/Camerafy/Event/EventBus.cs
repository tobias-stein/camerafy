using System;
using System.Collections.Generic;

namespace Camerafy.Event
{
    /// <summary>
    /// The global event-bus, which is responsible for managing all event participants
    /// and dispatching all events.
    /// </summary>
    public partial class EventBus : IDisposable
    {
        #region ALIAS

        // Helper class to hold an event participants eligible event callbacks
        private class ParticipantEventRoutes : Dictionary<string /* Event-route */, KeyValuePair<object /* Target Instance */, EventLibrary.ClientEventHandler /* Target Event-handler */>> { }

        private class ParticipantRegisterEntry
        {
            public Participant              Participant { get; private set; } = null;
            public ParticipantEventRoutes   EventRoutes { get; private set; } = null;

            public ParticipantRegisterEntry(Participant InParticipant, ParticipantEventRoutes InParticipantEventRoutes)
            {
                this.Participant = InParticipant;
                this.EventRoutes = InParticipantEventRoutes;
            }
        }

        // Helper class to hold all registered event participants with their event callbacks
        private class ParticipantRegister : Dictionary<string /* Participant id */, ParticipantRegisterEntry> { }

        #endregion

        public enum EventResponseDataStatus { ok, error };

        /// <summary>
        /// Event response data returned back to the client.
        /// </summary>
        public class EventResponseData
        {
            [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
            public EventResponseDataStatus status { get; set; } = EventResponseDataStatus.ok;
            public object result { get; set; } = null;
        }

        /// <summary>
        /// Singleton instance of the current event-bus instance.
        /// </summary>
        public static EventBus Current { get; private set; } = null;

        /// <summary>
        /// Register of all currently active event participants.
        /// </summary>
        private ParticipantRegister Participants = new ParticipantRegister();

        public EventBus()
        {
            // prevent event-bus beeing initialized twice.
            if (EventBus.Current != null)
                return;

            EventBus.Current = this;
        }

        /// <summary>
        /// Clean-up current event-bus instance.
        /// </summary>
        public void Dispose()
        {
            EventBus.Current = null;
        }

        /// <summary>
        /// Register a new event participant.
        /// </summary>
        /// <param name="InParticipant"></param>
        /// <returns></returns>
        public bool Register(Participant InParticipant)
        {
            // make sure we never register a participant twice
            if (this.Participants.ContainsKey(InParticipant.Id))
            {
                Logger.Error($"Trying to register an event participant (ID: {InParticipant.Id}) twice.");
                return false;
            }

            // Retrieve a instnace-method map of all CamerafyEventAttribute' annotated methods in any of the participants gameObject hierarchy classes
            var InstanceMethodMap = this.GetInstanceMethodMap(InParticipant);

            // add all eligible event handler for this participant to the registry
            ParticipantEventRoutes Routes = new ParticipantEventRoutes();
            foreach (var M in EventLibrary.ClientEvents)
            {
                object instance;
                if (InstanceMethodMap.TryGetValue(M.Key, out instance))
                {
                    Routes.Add(M.Key /* Event Method Address */, new KeyValuePair<object, EventLibrary.ClientEventHandler>(instance, M.Value));
                }
            }

            // add this client and its registered event routes to the registry.
            this.Participants.Add(InParticipant.Id, new ParticipantRegisterEntry(InParticipant, Routes));

            // successfully registered new participant
            return true;
        }

        /// <summary>
        /// Removes an event participant from event-bus.
        /// </summary>
        /// <param name="InParticipant"></param>
        public void Unregister(Participant InParticipant)
        {
            this.Participants.Remove(InParticipant.Id);
        }

        /// <summary>
        /// Dispatch reveived client data and call internal event handler methods.
        /// </summary>
        /// <param name="InData"></param>
        /// <param name="InTarget"></param>
        public async void ProcessClientEventData(byte[] InData, string InTarget = null)
        {
            // 36bit message id
            // 36bit method id
            if (InData.Length < 68)
            {
                Logger.Warning("Received invalid event data.");
                return;
            }

            string MessageId = System.Text.Encoding.UTF8.GetString(InData, 0, 36);
            string MethodId = System.Text.Encoding.UTF8.GetString(InData, 36, 32);

            // send to all
            if (InTarget == null)
            { 
                // todo: invoke for all
            }
            // send to specific participant
            else
            {
                ParticipantRegisterEntry Entry = null;
                if (this.Participants.TryGetValue(InTarget, out Entry))
                {
                    KeyValuePair<object, EventLibrary.ClientEventHandler> Executor = default;
                    if (Entry.EventRoutes.TryGetValue(MethodId, out Executor))
                    {
                        try
                        {
                            // Invoke and await result
                            var result = await Executor.Value.Target.Invoke(Executor.Key, InData);

                            if (!Executor.Value.NoResponse)
                            {
                                // create new response data
                                var eventResponseData = Serialization.EntityManager.ToString(new
                                {
                                    status = EventResponseDataStatus.ok,
                                    result = result
                                }, false, true);

                                // Send response
                                SendClientEventResponse(Entry.Participant, MessageId, eventResponseData);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Failed to execute event. [MessageId: {MessageId}, MethodId: {MethodId}]. Reason: {ex}");

                            if (!Executor.Value.NoResponse)
                            {
                                // create new response data
                                var eventResponseData = Serialization.EntityManager.ToString(new
                                {
                                    status = EventResponseDataStatus.error,
                                    result = "Server side execution error."
                                }, false, true);

                                // Send response
                                SendClientEventResponse(Entry.Participant, MessageId, eventResponseData);
                            }
                        }
                    }
                    else
                    {
                        Logger.Warning($"'{InTarget}' is trying to invoke an invalid method.");
                    }
                }
                else
                {
                    Logger.Warning($"Received event data for unknown target (ID: {InTarget})");
                }
            }
        }

        /// <summary>
        /// Send response to previously processed client event.
        /// </summary>
        /// <param name="InParticipant"></param>
        /// <param name="InMessageReferenceId"></param>
        /// <param name="InEventData"></param>
        private void SendClientEventResponse(Participant InParticipant, string InMessageReferenceId, string InEventData)
        {
            // special method address, which is reserved on the client-side to process event responses
            string Address = "00000000000000000000000000000000";

            // payload to send
            List<byte> data = new List<byte>();

            // first write the method address
            data.AddRange(System.Text.Encoding.UTF8.GetBytes(Address));

            // message id length
            data.AddRange(BitConverter.GetBytes(System.Text.Encoding.UTF8.GetByteCount(InMessageReferenceId)));
            // message id
            data.AddRange(System.Text.Encoding.UTF8.GetBytes(InMessageReferenceId));

            // return value json length
            data.AddRange(BitConverter.GetBytes(System.Text.Encoding.UTF8.GetByteCount(InEventData)));
            // json payload
            data.AddRange(System.Text.Encoding.UTF8.GetBytes(InEventData));

            // send data to client
            InParticipant.Send(data.ToArray());
        }

        /// <summary>
        /// Send new server event to client
        /// </summary>
        /// <param name="InParticipant"></param>
        /// <param name="InParams"></param>
        public void SendServerEventData(Participant InParticipant, params object[] InParams)
        {
            // payload to send
            List<byte> Payload = new List<byte>();

            var EventName = EventLibrary.CalculateMethodAddress(new System.Diagnostics.StackFrame(1).GetMethod());
            var EventData = EventLibrary.ServerEvents[EventName].Target.Invoke(InParams);

            // first write the method address
            Payload.AddRange(System.Text.Encoding.UTF8.GetBytes(EventName));
            Payload.AddRange(EventData);
            byte[] PayloadArr = Payload.ToArray();

            // send event
            if (InParticipant != null)
            {
                InParticipant.Send(PayloadArr);
            }
            else
            {
                foreach (var participant in this.Participants.Values)
                {
                    participant.Participant.Send(PayloadArr);
                }
            }
        }
    }
}
