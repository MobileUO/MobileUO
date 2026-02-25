#region License
// Copyright (C) 2022-2025 Sascha Puligheddu
// 
// This project is a complete reproduction of AssistUO for MobileUO and ClassicUO.
// Developed as a lightweight, native assistant.
// 
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// 
// SPECIAL PERMISSION: Integration with projects under BSD 2-Clause (like ClassicUO)
// is permitted, provided that the integrated result remains publicly accessible 
// and the AGPL-3.0 terms are respected for this specific module.
//
// This program is distributed WITHOUT ANY WARRANTY. 
// See <https://www.gnu.org/licenses/agpl-3.0.html> for details.
#endregion

using System;
using System.Collections.Generic;

namespace Assistant
{
    public class MinHeap
    {
        private List<Timer> _List;
        private int _Size;

        public MinHeap() : this(20)
        {
        }

        public MinHeap(int capacity)
        {
            _List = new List<Timer>(capacity + 1)
            {
                null
            };
            _Size = 0;
        }

        public void Heapify()
        {
            for (int i = _Size >> 1; i > 0; i--)
                PercolateDown(i);
        }

        private void PercolateDown(int hole)
        {
            Timer tmp = _List[hole];
            int child;

            for (; hole * 2 <= _Size; hole = child)
            {
                child = hole * 2;
                if (child != _Size && (_List[child + 1]).CompareTo(_List[child]) < 0)
                    child++;

                if (tmp.CompareTo(_List[child]) >= 0)
                    _List[hole] = _List[child];
                else
                    break;
            }

            _List[hole] = tmp;
        }

        public Timer Peek()
        {
            return _List[1];
        }

        public Timer Pop()
        {
            Timer top = Peek();

            _List[1] = _List[_Size--];
            PercolateDown(1);

            return top;
        }

        public void Remove(Timer o)
        {
            for (int i = 1; i <= _Size; i++)
            {
                if (_List[i] == o)
                {
                    _List[i] = _List[_Size--];
                    PercolateDown(i);
                    return;
                }
            }
        }

        public void Clear()
        {
            int capacity = _List.Count >> 1;
            if (capacity < 2)
                capacity = 2;
            _Size = 0;
            _List = new List<Timer>(capacity)
            {
                null
            };
        }

        public void Add(Timer o)
        {
            // PercolateUp
            int hole = ++_Size;

            // Grow the list if needed
            while (_List.Count <= _Size)
                _List.Add(null);

            for (; hole > 1 && o.CompareTo(_List[(hole >> 1)]) < 0; hole >>= 1)
                _List[hole] = _List[(hole >> 1)];
            _List[hole] = o;
        }

        public void AddMultiple(ICollection<Timer> col)
        {
            if (col != null && col.Count > 0)
            {
                foreach (Timer o in col)
                {
                    int hole = ++_Size;

                    // Grow the list as needed
                    while (_List.Count <= _Size)
                        _List.Add(null);

                    _List[hole] = o;
                }

                Heapify();
            }
        }

        public int Count
        {
            get { return _Size; }
        }

        public bool IsEmpty
        {
            get { return Count <= 0; }
        }

        public List<Timer> GetRawList()
        {
            var copy = new List<Timer>(_Size);
            for (int i = 1; i <= _Size; i++)
                copy.Add(_List[i]);
            return copy;
        }
    }

    public delegate void TimerCallback();

    public delegate void TimerCallbackState<in T>(T state);

    public abstract class Timer : IComparable<Timer>
    {
        private DateTime _Next;
        private TimeSpan _Delay;
        private TimeSpan _Interval;
        private bool _Running;
        private int _Index, _Count;

        protected abstract void OnTick();

        public Timer(TimeSpan delay) : this(delay, TimeSpan.Zero, 1)
        {
        }

        public Timer(TimeSpan interval, int count) : this(interval, interval, count)
        {
        }

        public Timer(TimeSpan delay, TimeSpan interval) : this(delay, interval, 0)
        {
        }

        public Timer(TimeSpan delay, TimeSpan interval, int count)
        {
            _Delay = delay;
            _Interval = interval;
            _Count = count;
        }

        public void Start()
        {
            if (!_Running)
            {
                _Index = 0;
                _Next = DateTime.UtcNow + _Delay;
                _Running = true;
                _Heap.Add(this);
                ChangedNextTick(true);
            }
        }

        public void Stop()
        {
            if (_Running)
            {
                _Running = false;
                _Heap.Remove(this);
            }
        }

        public int CompareTo(Timer t)
        {
            if (t != null)
                return TimeUntilTick.CompareTo(t.TimeUntilTick);
            else
                return -1;
        }

        public TimeSpan TimeUntilTick
        {
            get { return _Running ? _Next - DateTime.UtcNow : TimeSpan.MaxValue; }
        }

        public bool Running
        {
            get { return _Running; }
        }

        public TimeSpan Delay
        {
            get { return _Delay; }
            set { _Delay = value; }
        }

        public TimeSpan Interval
        {
            get { return _Interval; }
            set { _Interval = value; }
        }

        private static MinHeap _Heap = new MinHeap();

        private static void ChangedNextTick()
        {
            ChangedNextTick(false);
        }

        private static void ChangedNextTick(bool allowImmediate)
        {
            if (!_Heap.IsEmpty)
            {
                Timer t = _Heap.Peek();
                int interval = (int)Math.Round(t.TimeUntilTick.TotalMilliseconds);
                if (allowImmediate && interval <= 0)
                {
                    Slice();
                }
            }
        }

        public static void Slice()
        {
            int breakCount = 100;
            List<Timer> readd = new List<Timer>();

            while (!_Heap.IsEmpty && _Heap.Peek().TimeUntilTick < TimeSpan.Zero)
            {
                if (breakCount-- <= 0)
                    break;

                Timer t = _Heap.Pop();

                if (t != null && t.Running)
                {
                    t.OnTick();

                    if (t.Running && (t._Count == 0 || (++t._Index) < t._Count))
                    {
                        t._Next = DateTime.UtcNow + t._Interval;
                        readd.Add(t);
                    }
                    else
                    {
                        t.Stop();
                    }
                }
            }

            _Heap.AddMultiple(readd);

            ChangedNextTick();
        }

        private class OneTimeTimer : Timer
        {
            private TimerCallback _Call;

            public OneTimeTimer(TimeSpan d, TimerCallback call) : base(d)
            {
                _Call = call;
            }

            protected override void OnTick()
            {
                _Call();
            }
        }

        public static Timer DelayedCallback(TimeSpan delay, TimerCallback call)
        {
            return new OneTimeTimer(delay, call);
        }

        private class OneTimeTimerState<T> : Timer
        {
            private TimerCallbackState<T> _Call;
            private T _State;

            public OneTimeTimerState(TimeSpan d, TimerCallbackState<T> call, T state) : base(d)
            {
                _Call = call;
                _State = state;
            }

            protected override void OnTick()
            {
                _Call(_State);
            }
        }

        public static Timer DelayedCallbackState<T>(TimeSpan delay, TimerCallbackState<T> call, T state)
        {
            return new OneTimeTimerState<T>(delay, call, state);
        }
    }
}
