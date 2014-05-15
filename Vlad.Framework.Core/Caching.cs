﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aspectacular
{
    /// <summary>
    /// Similar to Lazy[T], allows explicit flushing of loaded value to repeat slow loading/initialization.
    /// Caches values in-process, in-memory.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Cacheable<T>
    {
        private Lazy<T> lazy;
        protected readonly Func<T> slowLoader;
        private readonly bool isThreadSafe;

        public Cacheable(Func<T> slowLoader, bool isThreadSafe = false)
        {
            this.slowLoader = slowLoader;
            this.isThreadSafe = isThreadSafe;

            this.Expire();
        }

        /// <summary>
        /// Flushes cached value, setting stage for re-loading/initialization
        /// of the value next time it's requested.
        /// </summary>
        public virtual void Expire()
        {
            this.Reset();
        }

        /// <summary>
        /// Flushes cached value, setting stage for re-loading/initialization
        /// of the value next time it's requested.
        /// </summary>
        public void Reset()
        {
            if (this.lazy == null || this.lazy.IsValueCreated)
                this.lazy = new Lazy<T>(this.slowLoader, this.isThreadSafe);
        }

        /// <summary>
        /// Returns cached value.
        /// Loads it first if value was not loaded before or was expired.
        /// </summary>
        [Obsolete("Use implicit conversion operator instead.")]
        public virtual T Value
        {
            get { return this.lazy.Value; }
        }

        public static implicit operator T(Cacheable<T> cacheable)
        {
            if (cacheable == null)
                throw new ArgumentNullException("cacheable");

            return cacheable.Value;
        }

        /// <summary>
        /// Returns true if value was has already been loaded.
        /// </summary>
        public bool IsValueLoaded
        {
            get { return this.lazy.IsValueCreated; }
        }
    }

    /// <summary>
    /// Similar to Lazy[T], allows explicit flushing of loaded value to repeat slow loading/initialization.
    /// Caches values in-process, in-memory.
    /// Loaded value expires automatically in a given amount of time.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AutoExpireCacheable<T> : Cacheable<T>
    {
        protected readonly int expireInMillisec;
        private DateTime loadedAt = default(DateTime);

        public AutoExpireCacheable(Func<T> slowLoader, uint expireInMillisec, bool isThreadSafe = false)
            : base(slowLoader, isThreadSafe)
        {
            if (expireInMillisec == 0)
                throw new ArgumentOutOfRangeException("expireInMillisec must be greater than 0.");

            this.expireInMillisec = (int)expireInMillisec;
        }

        /// <summary>
        /// Returns cached value.
        /// Loads it first if value was not loaded before or was expired.
        /// </summary>
        [Obsolete("Use implicit conversion operator instead.")]
        public override T Value
        {
            get
            {
                if (this.IsValueLoaded && this.Expired)
                    this.Expire();

                T val = base.Value; // Give slow loader a chance to throw an exception here.

                this.loadedAt = DateTime.UtcNow;
                return val;
            }
        }

        /// <summary>
        /// Returns true if value has expired.
        /// </summary>
        public bool Expired
        {
            get { return this.loadedAt.AddMilliseconds(this.expireInMillisec) < DateTime.UtcNow; }
        }

        /// <summary>
        /// Returns UTC time when value was slow-loaded.
        /// If IsValueLoaded is false, return blank/default DateTime.
        /// </summary>
        public DateTime LoadedAtUtc
        {
            get { return this.loadedAt; }
        }
    }
}