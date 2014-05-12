using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VC.Utils
{
    /// <summary>
    /// Represents a queue of items to be processed through the 
    /// provided delegate, and guarantees that only one item will 
    /// be called to process at a given time.
    /// Items are processed via the default Task scheduler (e.g. Threadpool).
    /// </summary>
    public class ThreadedQueue<T> : IDisposable
    {
        protected readonly object _syncRoot = new object();
        protected bool _isActive = true;
        protected readonly Action<T> _processingDelegate;
        protected readonly Queue<T> _queue = new Queue<T>();
        protected Task _processingTask = null;
        
        public ThreadedQueue(Action<T> processingDelegate)
        {
            _processingDelegate = processingDelegate;
        }

        public void Dispose()
        {
            if (_isActive)
            {
                _isActive = false;

                lock (_syncRoot)
                {
                    _queue.Clear();
                }
            }
        }

        public void Add(T item)
        {
            if (!_isActive)
            {
                throw new InvalidOperationException("Cannot add an item to a disposed ThreadedQueue");
            }

            lock (_syncRoot)
            {
                _queue.Enqueue(item);

                if (_processingTask == null)
                {
                    _processingTask = Task.Factory.StartNew(Process);
                }
            }
        }

        protected void Process()
        {
            while (_isActive)
            {
                T item;

                lock (_syncRoot)
                {
                    if (_queue.Count == 0)
                    {
                        _processingTask = null;
                        return;
                    }

                    item = _queue.Dequeue();
                }

                try
                {
                    _processingDelegate(item);
                }
                catch (Exception ex)
                {
                    ExceptionHandler.HandleException("ThreadedQueue Unhandled Exception: ", ex, true);
                }
            }
        }
    }
}
