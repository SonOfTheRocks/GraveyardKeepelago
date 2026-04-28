using System;

namespace GraveyardKeepelago.Items.Traps
{
    public class QueuedItemTrap
    {
        public string Name { get; set; }
        private Action _action;

        public QueuedItemTrap(string name, Action action)
        {
            Name = name;
            _action = action;
        }

        public void ExecuteNow()
        {
            _action?.Invoke();
        }
    }
}