namespace Zebble
{
    using System;
    using System.Threading.Tasks;

    partial class Renderer
    {
        public void Dispose() => GC.SuppressFinalize(this);
		
        public async Task<object> Render() => throw new NotSupportedException();

        internal void Apply(string property, UIChangedEventArgs change) => throw new NotSupportedException();
    }
}