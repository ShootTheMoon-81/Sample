using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MessageSystem
{
    public class MessageService : Singleton<MessageService>
    {
        private Dictionary<Type, IPublisher> _publishers = new();

        private List<PublishData> _publishOnThisFrame = new();
        private List<PublishData> _publishOnNextFrame = new();
        
        private struct PublishData
        {
            public IMessage Message;
            public IMessageListener Listener;
        }

        public void Subscribe<TMessage>(MessageHandler<TMessage> messageHandler)
            where TMessage : IMessage
        {
            GetPublisher<TMessage>().Subscribe(messageHandler);
#if UNITY_EDITOR
            EventLogManager.AddOrRemoveKey($"{typeof(TMessage).Name}", "Subscribe", true);
#endif
        }
        
        public void Unsubscribe<TMessage>(MessageHandler<TMessage> messageHandler)
            where TMessage : IMessage
        {
            GetPublisher<TMessage>().Unsubscribe(messageHandler);
#if UNITY_EDITOR
            EventLogManager.AddOrRemoveKey($"{typeof(TMessage).Name}", "Subscribe", false);
#endif
        }

        public void Publish<TMessage>(TMessage message, IMessageListener target = null)
            where TMessage : IMessage
        {
            _publishOnThisFrame.Add(new PublishData()
            {
                Message = message,
                Listener = target ?? GetPublisher<TMessage>()
            });

#if UNITY_EDITOR
            EventWatcherWindow.AddLogMessage($"[Publish]", $"{typeof(TMessage).Name} 발행", Color.yellow);
#endif
        }

        public void PublishImmediately<TMessage>(TMessage message)
            where TMessage : IMessage
        {
            GetPublisher<TMessage>().Publish(message);
            message.Dispose();
#if UNITY_EDITOR
            EventWatcherWindow.AddLogMessage($"[PublishImmediately]", $"{typeof(TMessage).Name} 발행", Color.yellow);
#endif
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            Instance.UpdateLoopAsync().Forget();
        }

        private async UniTaskVoid UpdateLoopAsync()
        {
            while (IsInstanceExists == true)
            {
                await UniTask.Yield(PlayerLoopTiming.LastUpdate);
                Update();
            }
        }
        
        private void Update()
        {
            foreach (var publisher in _publishers.Values)
            {
                publisher.Update();
            }
            
            var publishOnThisFrame = _publishOnThisFrame;
            _publishOnThisFrame = _publishOnNextFrame;

            foreach (var publishData in publishOnThisFrame)
            {
                publishData.Listener?.OnMessage(publishData.Message);
                publishData.Message.Dispose();
            }
            
            _publishOnNextFrame = publishOnThisFrame;
            _publishOnNextFrame.Clear();
        }

        private IPublisher<TMessage> GetPublisher<TMessage>()
            where TMessage : IMessage
        {
            var topic = typeof(TMessage);
            if (_publishers.TryGetValue(topic, out var publisher) == false)
            {
                publisher = new MessagePublisher<TMessage>();
                _publishers[topic] = publisher;
            }

            return (IPublisher<TMessage>)publisher;
        }
        
        private interface IPublisher : IMessageListener
        {
            public void Update();
        }

        private interface IPublisher<TMessage> : IPublisher
            where TMessage : IMessage
        {
            public void Subscribe(MessageHandler<TMessage> messageHandler);
            public void Unsubscribe(MessageHandler<TMessage> messageHandler);
            public void Publish(TMessage message);

            bool IMessageListener.OnMessage(IMessage message)
            {
                Publish((TMessage)message);

                return false;
            }
        }
        
        private class MessagePublisher<TMessage> : IPublisher<TMessage>
            where TMessage : IMessage
        {
            private readonly List<MessageHandler<TMessage>> _subscribeOnThisFrame = new();
            private readonly List<MessageHandler<TMessage>> _unsubscribeOnThisFrame = new();
            
            private readonly HashSet<MessageHandler<TMessage>> _subscribers = new();
            
            public void Update()
            {
                foreach (var unsubscribe in _unsubscribeOnThisFrame)
                {
                    _subscribers.Remove(unsubscribe);
                }
                
                _unsubscribeOnThisFrame.Clear();
                
                foreach (var subscribe in _subscribeOnThisFrame)
                {
                    _subscribers.Add(subscribe);
                }
                
                _subscribeOnThisFrame.Clear();
            }

            public void Subscribe(MessageHandler<TMessage> messageHandler)
            {
                _subscribeOnThisFrame.Add(messageHandler);
            }
            
            public void Unsubscribe(MessageHandler<TMessage> messageHandler)
            {
                _unsubscribeOnThisFrame.Add(messageHandler);
            }

            public void Publish(TMessage message)
            {
                foreach (var subscriber in _subscribers)
                {
                    subscriber?.Invoke(message);
                }
            }
        }
    }
}