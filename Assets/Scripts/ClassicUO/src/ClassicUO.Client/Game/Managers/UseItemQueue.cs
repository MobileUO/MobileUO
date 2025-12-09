// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    public sealed class UseItemQueue
    {
        public static UseItemQueue Instance { get; private set; }
        public bool IsEmpty => _isEmpty;

        private bool _isEmpty = true;
        private readonly Deque<uint> _actions = new Deque<uint>();
        private readonly World _world;

        public UseItemQueue(World world)
        {
            Instance = this;
            _world = world;
        }

        public void Update()
        {
            if (_isEmpty) return;
            if (GlobalActionCooldown.IsOnCooldown) return;

            uint serial = _actions.RemoveFromFront();
            GameActions.DoubleClick(_world, serial);

            GlobalActionCooldown.BeginCooldown();
            _isEmpty = _actions.Count == 0;
        }

        public void Add(uint serial)
        {
            foreach (uint s in _actions)
            {
                if (serial == s)
                {
                    return;
                }
            }

            _actions.AddToBack(serial);
            _isEmpty = false;
        }

        public void Clear()
        {
            _actions.Clear();
            _isEmpty = true;
        }

        public void ClearCorpses()
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                Entity entity = _world.Get(_actions[i]);

                if (entity == null)
                {
                    continue;
                }

                if (entity is Item it && it.IsCorpse)
                {
                    _actions.RemoveAt(i--);
                }
            }
            _isEmpty = _actions.Count == 0;
        }
    }
}