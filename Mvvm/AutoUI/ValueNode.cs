using System;
using Olive;

namespace Zebble.Mvvm.AutoUI
{
    partial class ValueNode : Node
    {
        public object Value;
        public string ValueString;

        public ValueNode(string label, object value, Node parent) : base(label, parent)
        {
            Value = value;

            if (value is null) return;

            if (value is Exception ex)
            {
                ValueString = "ERR: " + ex.Message;
                return;
            }

            try { ValueString = value.ToString(); }
            catch (Exception e) { ValueString = "ToString failed: " + e.Message; }

            if (ValueString == value.GetType().FullName)
                ValueString = "{" + value.GetType().GetProgrammingName(useGlobal: false, useNamespace: false, useNamespaceForParams: false) + "}";
        }
    }
}
