using System;
using System.Collections.Generic;

namespace MessageSystem
{
    public delegate bool MessageHandler<in TMessage>(TMessage message) where TMessage : IMessage;

    public interface IMessage : IDisposable
    {
    }

    public abstract class Message : IMessage
    {
        public abstract void Dispose();
        
        protected class MessagePool
        {
        }

        protected class MessagePool<TMessage> : MessagePool
            where TMessage : Message, new()
        {
            private Stack<TMessage> _pool = new();

            public TMessage Get() => 0 < _pool.Count ? _pool.Pop() : new TMessage();

            public void Return(TMessage message) => _pool.Push(message);
        }
    }

    public abstract class PooledMessage<TMessage> : Message
        where TMessage : PooledMessage<TMessage>, new()
    {
        protected static TMessage Get() => _messagePool.Get();

        protected abstract void Clear();

        public override void Dispose()
        {
            Clear();
            _messagePool.Return((TMessage)this);
        }

        private static readonly MessagePool<TMessage> _messagePool = new();
    }

    public interface IMessageListener
    {
        public bool OnMessage(IMessage message);
    }

    public static class MessageListenerExtension
    {
        public static bool Send(this IMessageListener target, IMessage message)
        {
            var result = target.OnMessage(message);
            message.Dispose();
            return result;
        }
    }
}