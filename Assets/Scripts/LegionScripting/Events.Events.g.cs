using System;
using ClassicUO.Game.Managers;

namespace ClassicUO.LegionScripting.PyClasses
{

    public partial class Events
    {
        private EventHandler<int> _onPlayerHitsChangedHandler;
        private EventHandler<ClassicUO.LegionScripting.PyClasses.Buff> _pyOnBuffAddedHandler;
        private EventHandler<ClassicUO.LegionScripting.PyClasses.Buff> _pyOnBuffRemovedHandler;
        private EventHandler<uint> _onPlayerDeathHandler;
        private EventHandler<uint> _onOpenContainerHandler;
        private EventHandler<ClassicUO.Game.Managers.PositionChangedArgs> _onPositionChangedHandler;
        private EventHandler<uint> _pyOnItemCreatedHandler;

        public partial void OnPlayerHitsChanged(object callback)
        {
            UnsubscribeOnPlayerHitsChanged();

            if (callback == null || !_engine.Operations.IsCallable(callback))
                return;

            _onPlayerHitsChangedHandler = (sender, arg) =>
            {
                _api?.ScheduleCallback(callback, arg);
            };

            EventSink.OnPlayerHitsChanged += _onPlayerHitsChangedHandler;
        }

        private void UnsubscribeOnPlayerHitsChanged()
        {
            if (_onPlayerHitsChangedHandler != null)
            {
                EventSink.OnPlayerHitsChanged -= _onPlayerHitsChangedHandler;
                _onPlayerHitsChangedHandler = null;
            }
        }

        public partial void OnBuffAdded(object callback)
        {
            UnsubscribePyOnBuffAdded();

            if (callback == null || !_engine.Operations.IsCallable(callback))
                return;

            _pyOnBuffAddedHandler = (sender, arg) =>
            {
                _api?.ScheduleCallback(callback, arg);
            };

            EventSink.PyOnBuffAdded += _pyOnBuffAddedHandler;
        }

        private void UnsubscribePyOnBuffAdded()
        {
            if (_pyOnBuffAddedHandler != null)
            {
                EventSink.PyOnBuffAdded -= _pyOnBuffAddedHandler;
                _pyOnBuffAddedHandler = null;
            }
        }

        public partial void OnBuffRemoved(object callback)
        {
            UnsubscribePyOnBuffRemoved();

            if (callback == null || !_engine.Operations.IsCallable(callback))
                return;

            _pyOnBuffRemovedHandler = (sender, arg) =>
            {
                _api?.ScheduleCallback(callback, arg);
            };

            EventSink.PyOnBuffRemoved += _pyOnBuffRemovedHandler;
        }

        private void UnsubscribePyOnBuffRemoved()
        {
            if (_pyOnBuffRemovedHandler != null)
            {
                EventSink.PyOnBuffRemoved -= _pyOnBuffRemovedHandler;
                _pyOnBuffRemovedHandler = null;
            }
        }

        public partial void OnPlayerDeath(object callback)
        {
            UnsubscribeOnPlayerDeath();

            if (callback == null || !_engine.Operations.IsCallable(callback))
                return;

            _onPlayerDeathHandler = (sender, arg) =>
            {
                _api?.ScheduleCallback(callback, arg);
            };

            EventSink.OnPlayerDeath += _onPlayerDeathHandler;
        }

        private void UnsubscribeOnPlayerDeath()
        {
            if (_onPlayerDeathHandler != null)
            {
                EventSink.OnPlayerDeath -= _onPlayerDeathHandler;
                _onPlayerDeathHandler = null;
            }
        }

        public partial void OnOpenContainer(object callback)
        {
            UnsubscribeOnOpenContainer();

            if (callback == null || !_engine.Operations.IsCallable(callback))
                return;

            _onOpenContainerHandler = (sender, arg) =>
            {
                _api?.ScheduleCallback(callback, arg);
            };

            EventSink.OnOpenContainer += _onOpenContainerHandler;
        }

        private void UnsubscribeOnOpenContainer()
        {
            if (_onOpenContainerHandler != null)
            {
                EventSink.OnOpenContainer -= _onOpenContainerHandler;
                _onOpenContainerHandler = null;
            }
        }

        public partial void OnPlayerMoved(object callback)
        {
            UnsubscribeOnPositionChanged();

            if (callback == null || !_engine.Operations.IsCallable(callback))
                return;

            _onPositionChangedHandler = (sender, arg) =>
            {
                _api?.ScheduleCallback(callback, arg);
            };

            EventSink.OnPositionChanged += _onPositionChangedHandler;
        }

        private void UnsubscribeOnPositionChanged()
        {
            if (_onPositionChangedHandler != null)
            {
                EventSink.OnPositionChanged -= _onPositionChangedHandler;
                _onPositionChangedHandler = null;
            }
        }

        public partial void OnItemCreated(object callback)
        {
            UnsubscribePyOnItemCreated();

            if (callback == null || !_engine.Operations.IsCallable(callback))
                return;

            _pyOnItemCreatedHandler = (sender, arg) =>
            {
                _api?.ScheduleCallback(callback, arg);
            };

            EventSink.PyOnItemCreated += _pyOnItemCreatedHandler;
        }

        private void UnsubscribePyOnItemCreated()
        {
            if (_pyOnItemCreatedHandler != null)
            {
                EventSink.PyOnItemCreated -= _pyOnItemCreatedHandler;
                _pyOnItemCreatedHandler = null;
            }
        }
    }
}