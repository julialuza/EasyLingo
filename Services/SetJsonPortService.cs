using EasyLingo.Data;
using EasyLingo.Data.DTOs;
using EasyLingo.Data.Entities;
using EasyLingo.Domain.Exceptions;
using EasyLingo.Services.Interfaces;
using EasyLingo.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasyLingo.Services
{
    public class SetJsonPortService
    {
        private readonly AppDbContext _db;

        private readonly IRepository<Set> _sets;
        private readonly IRepository<Term> _terms;
        private readonly IRepository<Language> _langs;
        private readonly IRepository<UserSetCategory> _cats;

        public SetJsonPortService()
        {
            _db = new AppDbContext();
            _sets = new EfRepository<Set>(_db);
            _terms = new EfRepository<Term>(_db);
            _langs = new EfRepository<Language>(_db);
            _cats = new EfRepository<UserSetCategory>(_db);
        }

        public async Task ExportSetToJsonFileAsync(int userId, int setId, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ValidationException("Nieprawidłowa ścieżka pliku.");

            var set = await _sets.Query()
                .Include(s => s.Language)
                .Include(s => s.SetCategory)
                .FirstOrDefaultAsync(s => s.SetId == setId);

            if (set == null)
                throw new NotFoundException("Nie znaleziono zestawu.");

            if (set.UserId != userId)
                throw new UnauthorizedActionException("Nie masz dostępu do tego zestawu.");

            var terms = await _terms.Query()
                .Where(t => t.SetId == setId)
                .AsNoTracking()
                .ToListAsync();

            var dto = new SetExportDto
            {
                Version = 1,
                Name = set.Name,
                Description = set.Description,
                LanguageCode = set.Language?.Code,
                CategoryName = set.SetCategory?.Name,
                Terms = terms
                    .Where(t => !string.IsNullOrWhiteSpace(t.TermName) && !string.IsNullOrWhiteSpace(t.Definition))
                    .Select(t => new TermExportDto { TermName = t.TermName, Definition = t.Definition })
                    .ToList()
            };

            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<int> ImportSetFromJsonFileAsync(int userId, int langId, string filePath)
        {
            if (!File.Exists(filePath))
                throw new NotFoundException("Nie znaleziono pliku do importu.");

            var json = await File.ReadAllTextAsync(filePath);

            SetExportDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<SetExportDto>(json);
            }
            catch
            {
                throw new ValidationException("Plik JSON ma nieprawidłowy format.");
            }

            if (dto == null)
                throw new ValidationException("Plik JSON jest pusty lub nieprawidłowy.");

            var name = (dto.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("W JSON brakuje nazwy zestawu.");

            var langExists = await _langs.Query().AnyAsync(l => l.LangId == langId);
            if (!langExists) throw new NotFoundException("Nie znaleziono języka.");

            int? catId = null;
            var catName = (dto.CategoryName ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(catName))
            {
                var existing = await _cats.Query()
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.Name.ToLower() == catName.ToLower());

                if (existing != null) catId = existing.SetCategoryId;
                else
                {
                    var created = new UserSetCategory { UserId = userId, Name = catName };
                    await _cats.AddAsync(created);
                    await _cats.SaveChangesAsync();
                    catId = created.SetCategoryId;
                }
            }

            var newSet = new Set
            {
                UserId = userId,
                LangId = langId,
                Name = name,
                Description = (dto.Description ?? "").Trim(),
                SetCategoryId = catId
            };

            await _sets.AddAsync(newSet);
            await _sets.SaveChangesAsync();

            _db.UserSetProgresses.Add(new UserSetProgress { UserId = userId, SetId = newSet.SetId, ProgressPercent = 0 });
            await _db.SaveChangesAsync();

            var termEntities = (dto.Terms ?? new())
                .Where(t => !string.IsNullOrWhiteSpace(t.TermName) && !string.IsNullOrWhiteSpace(t.Definition))
                .Select(t => new Term
                {
                    SetId = newSet.SetId,
                    TermName = t.TermName.Trim(),
                    Definition = t.Definition.Trim()
                })
                .ToList();

            if (termEntities.Count > 0)
            {
                await _terms.AddRangeAsync(termEntities);
                await _terms.SaveChangesAsync();

                _db.UserTermStatuses.AddRange(termEntities.Select(t => new UserTermStatus
                {
                    UserId = userId,
                    TermId = t.TermId,
                    Status = 0
                }));
                await _db.SaveChangesAsync();
            }

            return newSet.SetId;
        }
    }
}
