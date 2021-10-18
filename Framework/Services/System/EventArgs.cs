namespace System
{
    public class EventArgs<T> : EventArgs
    {
        #region Data property
        /// <summary>
        /// Gets or sets the Data property of this EventArgs.
        /// </summary>
        public T Data { get; set; }
        #endregion

        /// <summary>
        /// Creates a new EventArgs instance.
        /// </summary>
        public EventArgs(T data)
        {
            Data = data;
        }

        /// <summary>
        /// Creates a new EventArgs instance.
        /// </summary>
        public EventArgs()
        {
        }
    }

    public class EventArgs<T, K> : EventArgs
    {
        #region Data property
        /// <summary>
        /// Gets or sets the Data property of this EventArgs.
        /// </summary>
        public T Data1 { get; set; }
        #endregion

        #region Data property
        /// <summary>
        /// Gets or sets the Data property of this EventArgs.
        /// </summary>
        public K Data2 { get; set; }
        #endregion

        /// <summary>
        /// Creates a new EventArgs instance.
        /// </summary>
        public EventArgs(T data1, K data2)
        {
            Data1 = data1;
            Data2 = data2;
        }

        /// <summary>
        /// Creates a new EventArgs instance.
        /// </summary>
        public EventArgs(T data1)
        {
            Data1 = data1;
        }

        /// <summary>
        /// Creates a new EventArgs instance.
        /// </summary>
        public EventArgs()
        {
        }
    }
}