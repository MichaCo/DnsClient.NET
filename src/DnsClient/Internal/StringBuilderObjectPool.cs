#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace DnsClient.Internal
{
    public class StringBuilderObjectPool
    {
        private readonly ObjectPool<StringBuilder> _pool;

        public static StringBuilderObjectPool Default { get; } = new StringBuilderObjectPool();

        public StringBuilderObjectPool(int initialCapacity = 200, int maxPooledCapacity = 1024 * 2)
        {
            _pool = new DefaultObjectPoolProvider().CreateStringBuilderPool(initialCapacity, maxPooledCapacity);
        }

        public StringBuilder Get()
        {
            return _pool.Get();
        }

        public void Return(StringBuilder value)
        {
            _pool.Return(value);
        }
    }

    /* copy of MS extensions object pool implementation minus disposable object pool
     * because it does not support the targets this library supports and fixes some issues I have with my StringBuilderObjectPool
     * Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
     */

    internal interface IPooledObjectPolicy<T>
    {
        T Create();

        bool Return(T obj);
    }

    internal class DefaultObjectPool<T> : ObjectPool<T> where T : class
    {
        private protected readonly ObjectWrapper[] _items;
        private protected readonly IPooledObjectPolicy<T> _policy;
        private protected readonly bool _isDefaultPolicy;
        private protected T _firstItem;

        private protected readonly PooledObjectPolicy<T> _fastPolicy;

        public DefaultObjectPool(IPooledObjectPolicy<T> policy)
            : this(policy, Environment.ProcessorCount * 2)
        {
        }

        public DefaultObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _fastPolicy = policy as PooledObjectPolicy<T>;
            _isDefaultPolicy = IsDefaultPolicy();

            // -1 due to _firstItem
            _items = new ObjectWrapper[maximumRetained - 1];

            bool IsDefaultPolicy()
            {
                var type = policy.GetType();

#if NETSTANDARD1_3
                return type.GenericTypeArguments?.Length > 0 && type.GetGenericTypeDefinition() == typeof(DefaultPooledObjectPolicy<>);
#else
                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DefaultPooledObjectPolicy<>);
#endif
            }
        }

        public override T Get()
        {
            var item = _firstItem;
            if (item == null || Interlocked.CompareExchange(ref _firstItem, null, item) != item)
            {
                var items = _items;
                for (var i = 0; i < items.Length; i++)
                {
                    item = items[i].Element;
                    if (item != null && Interlocked.CompareExchange(ref items[i].Element, null, item) == item)
                    {
                        return item;
                    }
                }

                item = Create();
            }

            return item;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private T Create() => _fastPolicy?.Create() ?? _policy.Create();

        public override void Return(T obj)
        {
            if (_isDefaultPolicy || (_fastPolicy?.Return(obj) ?? _policy.Return(obj)))
            {
                if (_firstItem != null || Interlocked.CompareExchange(ref _firstItem, obj, null) != null)
                {
                    var items = _items;
                    for (var i = 0; i < items.Length && Interlocked.CompareExchange(ref items[i].Element, obj, null) != null; ++i)
                    {
                    }
                }
            }
        }

        [DebuggerDisplay("{Element}")]
        private protected struct ObjectWrapper
        {
            public T Element;
        }
    }

    internal class DefaultObjectPoolProvider : ObjectPoolProvider
    {
        public int MaximumRetained { get; set; } = Environment.ProcessorCount * 2;

        public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            return new DefaultObjectPool<T>(policy, MaximumRetained);
        }
    }

    internal class DefaultPooledObjectPolicy<T> : PooledObjectPolicy<T> where T : class, new()
    {
        public override T Create()
        {
            return new T();
        }

        public override bool Return(T obj)
        {
            return true;
        }
    }

    internal abstract class ObjectPool<T> where T : class
    {
        public abstract T Get();

        public abstract void Return(T obj);
    }

    internal static class ObjectPool
    {
        public static ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy = null) where T : class, new()
        {
            var provider = new DefaultObjectPoolProvider();
            return provider.Create(policy ?? new DefaultPooledObjectPolicy<T>());
        }
    }

    internal abstract class ObjectPoolProvider
    {
        public ObjectPool<T> Create<T>() where T : class, new()
        {
            return Create(new DefaultPooledObjectPolicy<T>());
        }

        public abstract ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy) where T : class;
    }

    internal abstract class PooledObjectPolicy<T> : IPooledObjectPolicy<T>
    {
        public abstract T Create();

        public abstract bool Return(T obj);
    }

    internal class StringBuilderPooledObjectPolicy : PooledObjectPolicy<StringBuilder>
    {
        public int InitialCapacity { get; set; } = 100;

        public int MaximumRetainedCapacity { get; set; } = 4 * 1024;

        public override StringBuilder Create()
        {
            return new StringBuilder(InitialCapacity);
        }

        public override bool Return(StringBuilder obj)
        {
            if (obj.Capacity > MaximumRetainedCapacity)
            {
                return false;
            }

            obj.Clear();
            return true;
        }
    }

    internal static class ObjectPoolProviderExtensions
    {
        public static ObjectPool<StringBuilder> CreateStringBuilderPool(this ObjectPoolProvider provider)
        {
            return provider.Create(new StringBuilderPooledObjectPolicy());
        }

        public static ObjectPool<StringBuilder> CreateStringBuilderPool(
            this ObjectPoolProvider provider,
            int initialCapacity,
            int maximumRetainedCapacity)
        {
            var policy = new StringBuilderPooledObjectPolicy()
            {
                InitialCapacity = initialCapacity,
                MaximumRetainedCapacity = maximumRetainedCapacity,
            };

            return provider.Create(policy);
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
