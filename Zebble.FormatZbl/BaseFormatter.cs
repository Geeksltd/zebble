using System;
using System.Collections.Generic;
using Olive;
using Zebble.Tooling;

namespace Zebble.FormatZbl
{
    abstract class BaseFormatter
    {
        public List<string> Errors = new();
        protected string Error(string error)
        {
            Errors.Add(error);
            ConsoleHelpers.Error(error);
            return null;
        }

        protected abstract void FormatFiles();

        static void Validate()
        {
            if (!DirectoryContext.AppUIFolder.Exists())
                throw new Exception("App.UI Directory not found: " + DirectoryContext.AppUIFolder.FullName);
        }

        protected abstract string GetFileName();

        internal void Run()
        {
            try
            {
                Validate();
                FormatFiles();
            }
            catch (Exception ex)
            {
                Error(ex.Message); return;
            }
        }
    }
}