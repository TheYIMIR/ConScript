namespace ConScript.Models
{
    /// <summary>
    /// Configuration value change event arguments
    /// </summary>
    public class ConfigValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Key of the changed value
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Old value
        /// </summary>
        public object OldValue { get; }

        /// <summary>
        /// New value
        /// </summary>
        public object NewValue { get; }

        /// <summary>
        /// Creates a new instance of ConfigValueChangedEventArgs
        /// </summary>
        public ConfigValueChangedEventArgs(string key, object oldValue, object newValue)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
