using System.Collections.Generic;

namespace EasyLingo.Data.DTOs
{
    public class SetExportDto
    {
        public int Version { get; set; } = 1;

        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        public string? LanguageCode { get; set; }
        public string? CategoryName { get; set; }

        public List<TermExportDto> Terms { get; set; } = new();
    }

    public class TermExportDto
    {
        public string TermName { get; set; } = "";
        public string Definition { get; set; } = "";
    }
}
