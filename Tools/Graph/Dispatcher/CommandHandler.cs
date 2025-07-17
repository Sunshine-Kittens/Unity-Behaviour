namespace Unity.Behavior.GraphFramework
{
    public abstract class BaseCommandHandler
    {
        protected internal IDispatcherContext DispatcherContext { get; internal set; }
        protected GraphView GraphView => DispatcherContext.GraphView;
        protected BlackboardView BlackboardView => DispatcherContext.BlackboardView;
        protected GraphAsset Asset => DispatcherContext.GraphAsset;
        protected BlackboardAsset BlackboardAsset => DispatcherContext.BlackboardAsset;

        public abstract bool Process(Command command);
    }

    public abstract class CommandHandler<CommandType> : BaseCommandHandler where CommandType : Command
    {
        public sealed override bool Process(Command command)
        {
            bool result = Process(command as CommandType);
            return result;
        }

        public abstract bool Process(CommandType command);
    }
}
