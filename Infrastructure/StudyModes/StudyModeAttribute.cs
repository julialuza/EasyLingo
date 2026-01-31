using System;

namespace EasyLingo.Infrastructure.StudyModes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class StudyModeAttribute : Attribute
    {
        public string Key { get; }
        public string DisplayName { get; }

        public StudyModeAttribute(string key, string displayName)
        {
            Key = key;
            DisplayName = displayName;
        }
    }
}
