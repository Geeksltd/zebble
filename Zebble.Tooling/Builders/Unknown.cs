namespace Zebble.Tooling
{
    class Unknown : Builder
    {
        public Unknown() => Log("Command not supported.");

        protected override void AddTasks() { }
    }
}