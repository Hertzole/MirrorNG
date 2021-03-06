using System;
using UnityEngine;

namespace Mirage.SocketLayer
{
    /// <summary>
    /// Warpper around a byte[] that belongs to a <see cref="BufferPool"/>
    /// </summary>
    public sealed class ByteBuffer : IDisposable
    {
        public readonly byte[] array;
        readonly BufferPool pool;

        public ByteBuffer(int bufferSize, BufferPool pool)
        {
            this.pool = pool ?? throw new ArgumentNullException(nameof(pool));

            array = new byte[bufferSize];
        }

        public void Release()
        {
            pool.Put(this);
        }

        void IDisposable.Dispose() => Release();
    }

    /// <summary>
    /// Holds a collection of <see cref="ByteBuffer"/> so they can be re-used without allocations
    /// </summary>
    public class BufferPool
    {
        const int PoolEmpty = -1;

        readonly int bufferSize;
        readonly int maxPoolSize;
        readonly ILogger logger;

        readonly ByteBuffer[] pool;
        int next = -1;
        int created = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferSize">size of each buffer</param>
        /// <param name="startPoolSize">how many buffers to create at start</param>
        /// <param name="maxPoolSize">max number of buffers in pool</param>
        /// <param name="logger"></param>
        public BufferPool(int bufferSize, int startPoolSize, int maxPoolSize, ILogger logger = null)
        {
            if (startPoolSize > maxPoolSize) throw new ArgumentException("Start Size must be less than max size", nameof(startPoolSize));

            this.bufferSize = bufferSize;
            this.maxPoolSize = maxPoolSize;
            this.logger = logger ?? Debug.unityLogger;

            pool = new ByteBuffer[maxPoolSize];
            for (int i = 0; i < startPoolSize; i++)
            {
                Put(CreateNewBuffer());
            }
        }

        private ByteBuffer CreateNewBuffer()
        {
            if (created >= maxPoolSize && logger.IsLogTypeAllowed(LogType.Warning)) logger.Log(LogType.Warning, $"Buffer Max Size reached, created:{created} max:{maxPoolSize}");
            created++;
            return new ByteBuffer(bufferSize, this);
        }

        public ByteBuffer Take()
        {
            if (next == PoolEmpty)
            {
                return CreateNewBuffer();
            }
            else
            {
                // todo is it a security risk to now clear buffer?

                // take then decriment
                return pool[next--];
            }
        }

        public void Put(ByteBuffer buffer)
        {
            if (next < maxPoolSize - 1)
            {
                // increment then put
                pool[++next] = buffer;
            }
            else
            {
                if (logger.IsLogTypeAllowed(LogType.Warning)) logger.Log(LogType.Warning, $"Cant Put buffer into full pool, leaving for GC");
                // buffer is left for GC, so decrement created
                created--;
            }
        }
    }
}
