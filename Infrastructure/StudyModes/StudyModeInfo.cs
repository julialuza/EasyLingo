using System;

namespace EasyLingo.Infrastructure.StudyModes
{
    public sealed class StudyModeInfo
    {
        public string Key { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public Type ViewModelType { get; init; } = typeof(object);
    }
}
