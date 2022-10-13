namespace Zebble
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Olive;

    public class OptionsDataSource
    {
        IList<object> dataSource;

        /// <summary>The current items for selection on the UI.</summary>
        public List<DataItem> Items = new();

        /// <summary>The items currently selected on the UI.</summary>
        public IEnumerable<DataItem> SelectedItems => Items.Where(x => x.Selected);

        /// <summary>The values of the items currently selected on the UI.</summary>
        public IEnumerable<object> SelectedValues => SelectedItems.Select(x => x.Value);

        /// <summary>The value of the first item currently selected on the UI.</summary>
        public object SelectedValue => SelectedValues.FirstOrDefault();

        /// <summary>
        /// The selected values should be maintained seperately,
        /// so DataSource and Value can be set time-independently.
        /// </summary>
        List<object> apiSelectedValues;

        public bool MultiSelect { get; set; }

        public IEnumerable<object> DataSource
        {
            get => dataSource;
            set
            {
                dataSource = value.ToList();
                Items = dataSource?.Select(x => new DataItem(x)).ToList();

                // Re-select the value:
                Value = apiSelectedValues;
            }
        }

        public object Value
        {
            get => MultiSelect ? SelectedValues : SelectedValue;
            set
            {
                if (value is IEnumerable && !(value is string))
                    apiSelectedValues = (value as IEnumerable).Cast<object>().ExceptNull().ToList();
                else
                    apiSelectedValues = new List<object> { value }.ExceptNull().ToList();

                // Select them also for the UI:
                Items.Do(x => x.Selected = false);
                var existingItems = Items.Where(x => apiSelectedValues.Contains(x.Value));
                if (!existingItems.Any())
                {
                    var apiSelectedStringValues = apiSelectedValues.Select(x => x.ToString());
                    existingItems = Items.Where(x => apiSelectedStringValues.Contains(x.Value.ToString()));
                }

                existingItems.Do(x => x.Selected = true);
            }
        }

        public class DataItem
        {
            public object Value { get; set; }
            public string Text { get; set; }
            public bool Selected { get; set; }
            public DataItem(object item) : this(item, item?.ToString()) { }
            public DataItem(object value, string text) { Value = value; Text = text; }
        }
    }
}