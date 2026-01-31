using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EasyLingo.Infrastructure.StudyModes
{
    public static class StudyModeRegistry
    {
        public static List<StudyModeInfo> DiscoverModes(Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            return assembly.GetTypes()
                .Where(t => !t.IsAbstract && t.IsClass)
                .Select(t => new { Type = t, Attr = t.GetCustomAttribute<StudyModeAttribute>() })
                .Where(x => x.Attr != null)
                .Select(x => new StudyModeInfo
                {
                    Key = x.Attr!.Key,
                    DisplayName = x.Attr!.DisplayName,
                    ViewModelType = x.Type
                })
                .OrderBy(x => x.DisplayName)
                .ToList();
        }
        public class StudyModeInfo
        {
            public string Key { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public Type ViewModelType { get; set; } = null!;
        }
    }
}
