using System;

namespace Utilla
{
    public class Events
    {
        public static Events Instance = new();

        public static event EventHandler<RoomJoinedArgs> RoomJoined;

        public static event EventHandler<RoomJoinedArgs> RoomLeft;

        public static event EventHandler GameInitialized;

        public virtual void TriggerRoomJoin(RoomJoinedArgs e)
        {
            RoomJoined?.SafeInvoke(this, e);
        }

        public virtual void TriggerRoomLeft(RoomJoinedArgs e)
        {
            RoomLeft?.SafeInvoke(this, e);
        }

        public virtual void TriggerGameInitialized()
        {
            GameInitialized?.SafeInvoke(this, EventArgs.Empty);
        }

        public class RoomJoinedArgs : EventArgs
        {
            public bool isPrivate { get; set; }

            public string Gamemode { get; set; }
        }
    }

    public static class EventHandlerExtensions
    {
        public static void SafeInvoke(this EventHandler handler, object sender, EventArgs e)
        {
            if (handler is null) return;

            foreach (EventHandler invocation in handler.GetInvocationList())
            {
                try
                {
                    invocation(sender, e);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Handler in {invocation.Method.DeclaringType?.FullName} threw");
                    Plugin.Log.LogError(ex);
                }
            }
        }

        public static void SafeInvoke<T>(this EventHandler<T> handler, object sender, T e)
        {
            if (handler is null) return;

            foreach (EventHandler<T> invocation in handler.GetInvocationList())
            {
                try
                {
                    invocation(sender, e);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Handler in {invocation.Method.DeclaringType?.FullName} threw");
                    Plugin.Log.LogError(ex);
                }
            }
        }
    }
}
