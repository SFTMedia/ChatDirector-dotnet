namespace Oxide.Ext.ChatDirector.core
{
    public interface IDaemon : ILoadable
    {
        void addItem(IItem item);
    }
}